using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NotasNet.Api.Application;

namespace NotasNet.Tests;

public class NotesEndpointTests
{
    private const string ValidUrl = "https://youtu.be/dQw4w9WgXcQ";
    private const string AllowedOrigin = "https://app.example";

    private sealed class FakeTranscript(Exception? error = null) : ITranscriptProvider
    {
        public Task<string> GetAsync(string videoId, CancellationToken ct) =>
            error is null
                ? Task.FromResult(string.Join(" ", Enumerable.Range(0, 60).Select(i => $"w{i}")))
                : throw error;
    }

    private sealed class FakeLlm : ILlmProvider
    {
        public async IAsyncEnumerable<string> StreamAsync(
            string systemPrompt, string content, [EnumeratorCancellation] CancellationToken ct)
        {
            await Task.Yield();
            yield return "# Hola";
            yield return " mundo";
        }
    }

    private static HttpClient Client(
        Exception? transcriptError = null, int perMinute = 100, int dailyLimit = 1000)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("RateLimit:PerMinute", perMinute.ToString());
            b.UseSetting("Notes:DailyLimit", dailyLimit.ToString());
            b.UseSetting("Cors:AllowedOrigin", AllowedOrigin);
            b.UseSetting("Gemini:ApiKey", "test");
            b.ConfigureTestServices(s =>
            {
                s.AddSingleton<ITranscriptProvider>(new FakeTranscript(transcriptError));
                s.AddSingleton<ILlmProvider>(new FakeLlm());
            });
        });
        return factory.CreateClient();
    }

    private static Task<HttpResponseMessage> Post(HttpClient c, string url) =>
        c.PostAsJsonAsync("/api/notes", new { url });

    [Fact]
    public async Task Happy_path_streams_status_chunks_and_done_as_sse()
    {
        var response = await Post(Client(), ValidUrl);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);
        var status = body.IndexOf("event: status", StringComparison.Ordinal);
        var chunk = body.IndexOf("event: chunk", StringComparison.Ordinal);
        var done = body.IndexOf("event: done", StringComparison.Ordinal);
        Assert.True(status >= 0 && status < chunk && chunk < done, body);
        Assert.Contains("\"text\":\"# Hola\"", body);
    }

    [Fact]
    public async Task Invalid_url_emits_invalid_url_error()
    {
        var body = await (await Post(Client(), "hola")).Content.ReadAsStringAsync();

        Assert.Contains("event: error", body);
        Assert.Contains("\"code\":\"invalid_url\"", body);
    }

    [Fact]
    public async Task Provider_NoCaptions_emits_no_captions_error()
    {
        var client = Client(new NotesException(NotesErrorCode.NoCaptions));

        var body = await (await Post(client, ValidUrl)).Content.ReadAsStringAsync();

        Assert.Contains("\"code\":\"no_captions\"", body);
    }

    [Fact]
    public async Task Exceeding_per_minute_limit_returns_429()
    {
        var client = Client(perMinute: 2);

        await Post(client, ValidUrl);
        await Post(client, ValidUrl);
        var third = await Post(client, ValidUrl);

        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    [Fact]
    public async Task Exhausted_daily_quota_emits_rate_limited_error()
    {
        var client = Client(dailyLimit: 1);

        await (await Post(client, ValidUrl)).Content.ReadAsStringAsync();
        var body = await (await Post(client, ValidUrl)).Content.ReadAsStringAsync();

        Assert.Contains("\"code\":\"rate_limited\"", body);
    }

    [Fact]
    public async Task Cors_allows_configured_origin_only()
    {
        var client = Client();

        var allowed = new HttpRequestMessage(HttpMethod.Post, "/api/notes")
        { Content = JsonContent.Create(new { url = ValidUrl }) };
        allowed.Headers.Add("Origin", AllowedOrigin);
        var denied = new HttpRequestMessage(HttpMethod.Post, "/api/notes")
        { Content = JsonContent.Create(new { url = ValidUrl }) };
        denied.Headers.Add("Origin", "https://evil.example");

        var allowedResponse = await client.SendAsync(allowed);
        var deniedResponse = await client.SendAsync(denied);

        Assert.Equal(AllowedOrigin, allowedResponse.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Rate_limit_is_partitioned_by_forwarded_client_ip()
    {
        var client = Client(perMinute: 1);

        HttpRequestMessage From(string ip)
        {
            var r = new HttpRequestMessage(HttpMethod.Post, "/api/notes")
            { Content = JsonContent.Create(new { url = ValidUrl }) };
            r.Headers.Add("X-Forwarded-For", ip);
            return r;
        }

        var first = await client.SendAsync(From("1.1.1.1"));
        var otherClient = await client.SendAsync(From("2.2.2.2"));
        var sameClient = await client.SendAsync(From("1.1.1.1"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, otherClient.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, sameClient.StatusCode);
    }

    [Fact]
    public async Task Unexpected_provider_exception_becomes_an_error_event()
    {
        var client = Client(new InvalidOperationException("boom"));

        var body = await (await Post(client, ValidUrl)).Content.ReadAsStringAsync();

        Assert.Contains("event: error", body);
        Assert.Contains("\"code\":\"llm_failure\"", body);
    }

    [Fact]
    public async Task Invalid_urls_do_not_consume_the_daily_quota()
    {
        var client = Client(dailyLimit: 1);

        await (await Post(client, "hola")).Content.ReadAsStringAsync();
        var body = await (await Post(client, ValidUrl)).Content.ReadAsStringAsync();

        Assert.Contains("event: done", body);
        Assert.DoesNotContain("rate_limited", body);
    }
}
