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
}
