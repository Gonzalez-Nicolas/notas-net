using System.Text.Json;

namespace NotasNet.Api.Infrastructure;

public static class SseLineParser
{
    public static string? ExtractGeminiText(string dataLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(dataLine);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates)
                || candidates.GetArrayLength() == 0
                || !candidates[0].TryGetProperty("content", out var content)
                || !content.TryGetProperty("parts", out var parts)
                || parts.GetArrayLength() == 0
                || !parts[0].TryGetProperty("text", out var text))
                return null;
            return text.GetString();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
