namespace NotasNet.Api.Application;

public interface ITranscriptProvider
{
    /// <exception cref="NotesException">NoCaptions or VideoUnavailable.</exception>
    Task<string> GetAsync(string videoId, CancellationToken ct);
}
