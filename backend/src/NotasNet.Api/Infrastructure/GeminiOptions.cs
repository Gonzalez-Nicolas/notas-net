namespace NotasNet.Api.Infrastructure;

public record GeminiOptions(
    string ApiKey,
    string Model,
    string? FallbackModel = null,
    IReadOnlyList<TimeSpan>? RetryDelays = null)
{
    /// <summary>Waits between attempts on the same model when Gemini answers 503 (overloaded).</summary>
    public IReadOnlyList<TimeSpan> Delays => RetryDelays ?? [TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(3)];
}
