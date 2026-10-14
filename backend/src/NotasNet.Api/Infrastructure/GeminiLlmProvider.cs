using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using NotasNet.Api.Application;

namespace NotasNet.Api.Infrastructure;

public class GeminiLlmProvider(
    HttpClient http, GeminiOptions options, ILogger<GeminiLlmProvider>? logger = null) : ILlmProvider
{
    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt, string content, [EnumeratorCancellation] CancellationToken ct)
    {
        using var response = await SendWithResilienceAsync(systemPrompt, content, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
            throw new NotesException(NotesErrorCode.RateLimited);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            logger?.LogError("Gemini returned {Status}: {Body}",
                (int)response.StatusCode, body.Length > 500 ? body[..500] : body);
            throw new NotesException(NotesErrorCode.LlmFailure);
        }

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
            var text = SseLineParser.ExtractGeminiText(line["data:".Length..].Trim());
            if (!string.IsNullOrEmpty(text)) yield return text;
        }
    }

    /// <summary>
    /// Retries on 503 (model overloaded) with the configured waits, then tries the fallback model.
    /// Runs before any text is streamed, so a retry never duplicates output.
    /// Returns the first non-503 response, or the last 503 when every attempt is exhausted.
    /// </summary>
    private async Task<HttpResponseMessage> SendWithResilienceAsync(
        string systemPrompt, string content, CancellationToken ct)
    {
        var models = new List<string> { options.Model };
        if (!string.IsNullOrWhiteSpace(options.FallbackModel) && options.FallbackModel != options.Model)
            models.Add(options.FallbackModel);

        HttpResponseMessage? last = null;
        foreach (var model in models)
        {
            for (var attempt = 0; attempt <= options.Delays.Count; attempt++)
            {
                last?.Dispose();
                last = await SendAsync(model, systemPrompt, content, ct);
                if (last.StatusCode != HttpStatusCode.ServiceUnavailable) return last;

                logger?.LogWarning("Gemini model {Model} overloaded (503), attempt {Attempt}", model, attempt + 1);
                if (attempt < options.Delays.Count) await Task.Delay(options.Delays[attempt], ct);
            }
        }
        return last!;
    }

    private async Task<HttpResponseMessage> SendAsync(
        string model, string systemPrompt, string content, CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:streamGenerateContent?alt=sse";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = content } } } }
            })
        };
        request.Headers.Add("x-goog-api-key", options.ApiKey);

        try
        {
            return await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException)
        {
            throw new NotesException(NotesErrorCode.LlmFailure);
        }
    }
}
