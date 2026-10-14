using System.Net;
using NotasNet.Api.Application;
using NotasNet.Api.Infrastructure;

namespace NotasNet.Tests;

public class GeminiLlmProviderTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public HttpRequestMessage? Request;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static string Line(string text) =>
        "data: {\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"" + text + "\"}]}}]}\n\n";

    private static GeminiLlmProvider Make(StubHandler h) =>
        new(new HttpClient(h), new GeminiOptions("KEY", "test-model"));

    private static async Task<List<string>> Drain(GeminiLlmProvider p)
    {
        var list = new List<string>();
        await foreach (var s in p.StreamAsync("sys", "content", CancellationToken.None)) list.Add(s);
        return list;
    }

    [Fact]
    public async Task Streams_text_chunks_in_order_and_sends_key_and_model()
    {
        var h = new StubHandler(HttpStatusCode.OK, Line("uno") + Line("dos") + Line("tres"));

        var result = await Drain(Make(h));

        Assert.Equal(["uno", "dos", "tres"], result);
        Assert.Contains("test-model:streamGenerateContent", h.Request!.RequestUri!.ToString());
        Assert.Equal("KEY", h.Request.Headers.GetValues("x-goog-api-key").Single());
    }

    [Fact]
    public async Task Http_429_maps_to_RateLimited()
    {
        var ex = await Assert.ThrowsAsync<NotesException>(() => Drain(Make(new StubHandler(HttpStatusCode.TooManyRequests, ""))));
        Assert.Equal(NotesErrorCode.RateLimited, ex.Code);
    }

    [Fact]
    public async Task Http_500_maps_to_LlmFailure()
    {
        var ex = await Assert.ThrowsAsync<NotesException>(() => Drain(Make(new StubHandler(HttpStatusCode.InternalServerError, "boom"))));
        Assert.Equal(NotesErrorCode.LlmFailure, ex.Code);
    }

    private sealed class SequenceHandler(params (HttpStatusCode Status, string Body)[] responses) : HttpMessageHandler
    {
        public List<string> Urls { get; } = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var (status, body) = responses[Math.Min(Urls.Count, responses.Length - 1)];
            Urls.Add(request.RequestUri!.ToString());
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body) });
        }
    }

    private static GeminiLlmProvider MakeResilient(SequenceHandler h, string? fallback = null) =>
        new(new HttpClient(h),
            new GeminiOptions("KEY", "main-model", fallback, [TimeSpan.Zero, TimeSpan.Zero]));

    [Fact]
    public async Task Retries_on_503_and_succeeds_when_the_model_recovers()
    {
        var h = new SequenceHandler((HttpStatusCode.ServiceUnavailable, "busy"), (HttpStatusCode.OK, Line("ok")));

        var result = await Drain(MakeResilient(h));

        Assert.Equal(["ok"], result);
        Assert.Equal(2, h.Urls.Count);
        Assert.All(h.Urls, u => Assert.Contains("main-model", u));
    }

    [Fact]
    public async Task Falls_back_to_the_fallback_model_after_exhausting_retries()
    {
        var h = new SequenceHandler(
            (HttpStatusCode.ServiceUnavailable, "busy"),
            (HttpStatusCode.ServiceUnavailable, "busy"),
            (HttpStatusCode.ServiceUnavailable, "busy"),
            (HttpStatusCode.OK, Line("respaldo")));

        var result = await Drain(MakeResilient(h, fallback: "backup-model"));

        Assert.Equal(["respaldo"], result);
        Assert.Equal(4, h.Urls.Count);
        Assert.Equal(3, h.Urls.Count(u => u.Contains("main-model")));
        Assert.Contains("backup-model", h.Urls[3]);
    }

    [Fact]
    public async Task Persistent_503_without_fallback_throws_LlmFailure_after_all_attempts()
    {
        var h = new SequenceHandler((HttpStatusCode.ServiceUnavailable, "busy"));

        var ex = await Assert.ThrowsAsync<NotesException>(() => Drain(MakeResilient(h)));

        Assert.Equal(NotesErrorCode.LlmFailure, ex.Code);
        Assert.Equal(3, h.Urls.Count);
    }

    [Fact]
    public async Task Http_429_is_not_retried()
    {
        var h = new SequenceHandler((HttpStatusCode.TooManyRequests, ""));

        var ex = await Assert.ThrowsAsync<NotesException>(() => Drain(MakeResilient(h, fallback: "backup-model")));

        Assert.Equal(NotesErrorCode.RateLimited, ex.Code);
        Assert.Single(h.Urls);
    }
}
