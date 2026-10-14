using System.Text.Json;
using NotasNet.Api.Application;

namespace NotasNet.Api.Endpoints;

public record NotesRequest(string? Url);

public static class NotesEndpoint
{
    public static void MapNotesEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/notes", HandleAsync).RequireRateLimiting("notes");
    }

    private static async Task HandleAsync(
        NotesRequest request, HttpContext http, GenerateNotesUseCase useCase, DailyQuotaLimiter quota)
    {
        var ct = http.RequestAborted;
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers["X-Accel-Buffering"] = "no";

        try
        {
            if (!quota.TryAcquire())
            {
                await WriteAsync(http, "error", new { code = ErrorCode(NotesErrorCode.RateLimited) }, ct);
                return;
            }

            await foreach (var e in useCase.ExecuteAsync(request.Url ?? "", ct))
            {
                switch (e)
                {
                    case StatusEvent s: await WriteAsync(http, "status", new { message = s.Message }, ct); break;
                    case ChunkEvent c: await WriteAsync(http, "chunk", new { text = c.Text }, ct); break;
                    case DoneEvent: await WriteAsync(http, "done", new { }, ct); break;
                }
            }
        }
        catch (NotesException ex)
        {
            await WriteAsync(http, "error", new { code = ErrorCode(ex.Code) }, ct);
        }
        catch (OperationCanceledException)
        {
            // client disconnected; the cancellation token already stopped upstream calls
        }
    }

    private static async Task WriteAsync(HttpContext http, string name, object data, CancellationToken ct)
    {
        var payload = $"event: {name}\ndata: {JsonSerializer.Serialize(data, JsonSerializerOptions.Web)}\n\n";
        await http.Response.WriteAsync(payload, ct);
        await http.Response.Body.FlushAsync(ct);
    }

    private static string ErrorCode(NotesErrorCode code) => code switch
    {
        NotesErrorCode.InvalidUrl => "invalid_url",
        NotesErrorCode.NoCaptions => "no_captions",
        NotesErrorCode.VideoUnavailable => "video_unavailable",
        NotesErrorCode.RateLimited => "rate_limited",
        _ => "llm_failure"
    };
}
