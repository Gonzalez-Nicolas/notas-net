using NotasNet.Api.Application;
using YoutubeExplode;
using YoutubeExplode.Exceptions;

namespace NotasNet.Api.Infrastructure;

public class YoutubeTranscriptProvider : ITranscriptProvider
{
    private readonly YoutubeClient _youtube = new();

    public async Task<string> GetAsync(string videoId, CancellationToken ct)
    {
        try
        {
            var manifest = await _youtube.Videos.ClosedCaptions.GetManifestAsync(videoId, ct);
            var track = manifest.TryGetByLanguage("es")
                        ?? manifest.TryGetByLanguage("en")
                        ?? manifest.Tracks.FirstOrDefault();
            if (track is null) throw new NotesException(NotesErrorCode.NoCaptions);

            var captions = await _youtube.Videos.ClosedCaptions.GetAsync(track, ct);
            return string.Join(" ", captions.Captions.Select(c => c.Text.Trim()).Where(t => t.Length > 0));
        }
        catch (YoutubeExplodeException)
        {
            // unavailable/unplayable videos and YouTube request-limit errors all end here
            throw new NotesException(NotesErrorCode.VideoUnavailable);
        }
    }
}
