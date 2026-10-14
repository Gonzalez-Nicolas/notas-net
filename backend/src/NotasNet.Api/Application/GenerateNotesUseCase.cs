using System.Runtime.CompilerServices;
using System.Text;

namespace NotasNet.Api.Application;

public record NotesOptions(
    int MaxTranscriptChars = 400_000,
    int ChunkChars = 60_000,
    int MinWords = 30,
    string SystemPrompt = "");

public class GenerateNotesUseCase(
    ITranscriptProvider transcripts, ILlmProvider llm, NotesOptions options, IUsageQuota? quota = null)
{
    private const string PartialPrompt =
        "Resume fielmente esta parte de un transcript de video en viñetas concisas, conservando datos, cifras y conceptos. Responde en el idioma del transcript.";

    public async IAsyncEnumerable<NotesEvent> ExecuteAsync(
        string url, [EnumeratorCancellation] CancellationToken ct)
    {
        if (!VideoUrlParser.TryExtractVideoId(url, out var videoId))
            throw new NotesException(NotesErrorCode.InvalidUrl);

        yield return new StatusEvent("Obteniendo transcript…");
        var transcript = await transcripts.GetAsync(videoId, ct);

        if (transcript.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length < options.MinWords)
            throw new NotesException(NotesErrorCode.NoCaptions);

        if (transcript.Length > options.MaxTranscriptChars)
            transcript = transcript[..options.MaxTranscriptChars];

        if (quota is not null && !quota.TryAcquire())
            throw new NotesException(NotesErrorCode.RateLimited);

        var parts = TranscriptChunker.Split(transcript, options.ChunkChars);
        var content = transcript;

        if (parts.Count > 1)
        {
            var summaries = new List<string>();
            for (var i = 0; i < parts.Count; i++)
            {
                yield return new StatusEvent($"Resumiendo parte {i + 1} de {parts.Count}…");
                var sb = new StringBuilder();
                await foreach (var piece in llm.StreamAsync(PartialPrompt, parts[i], ct))
                    sb.Append(piece);
                summaries.Add(sb.ToString());
            }
            content = string.Join("\n\n", summaries);
        }

        yield return new StatusEvent("Generando notas…");
        var produced = false;
        await foreach (var chunk in llm.StreamAsync(options.SystemPrompt, content, ct))
        {
            produced = true;
            yield return new ChunkEvent(chunk);
        }

        if (!produced) throw new NotesException(NotesErrorCode.LlmFailure);
        yield return new DoneEvent();
    }
}
