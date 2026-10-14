namespace NotasNet.Api.Application;

public enum NotesErrorCode { InvalidUrl, NoCaptions, VideoUnavailable, RateLimited, LlmFailure }

public class NotesException(NotesErrorCode code) : Exception(code.ToString())
{
    public NotesErrorCode Code { get; } = code;
}
