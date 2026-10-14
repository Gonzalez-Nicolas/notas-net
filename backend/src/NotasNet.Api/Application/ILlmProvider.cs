namespace NotasNet.Api.Application;

public interface ILlmProvider
{
    IAsyncEnumerable<string> StreamAsync(string systemPrompt, string content, CancellationToken ct);
}
