using System.Net;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using NotasNet.Api.Application;

namespace NotasNet.Api.Infrastructure;

public class GeminiLlmProvider(HttpClient http, GeminiOptions options) : ILlmProvider
{
    public async IAsyncEnumerable<string> StreamAsync(
        string systemPrompt, string content, [EnumeratorCancellation] CancellationToken ct)
    {
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{options.Model}:streamGenerateContent?alt=sse";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new
            {
                systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
                contents = new[] { new { role = "user", parts = new[] { new { text = content } } } }
            })
        };
        request.Headers.Add("x-goog-api-key", options.ApiKey);

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        }
        catch (HttpRequestException)
        {
            throw new NotesException(NotesErrorCode.LlmFailure);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
                throw new NotesException(NotesErrorCode.RateLimited);
            if (!response.IsSuccessStatusCode)
                throw new NotesException(NotesErrorCode.LlmFailure);

            using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
            while (await reader.ReadLineAsync(ct) is { } line)
            {
                if (!line.StartsWith("data:", StringComparison.Ordinal)) continue;
                var text = SseLineParser.ExtractGeminiText(line["data:".Length..].Trim());
                if (!string.IsNullOrEmpty(text)) yield return text;
            }
        }
    }
}
