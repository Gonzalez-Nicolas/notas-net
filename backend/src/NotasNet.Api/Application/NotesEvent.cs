namespace NotasNet.Api.Application;

public abstract record NotesEvent;
public sealed record StatusEvent(string Message) : NotesEvent;
public sealed record ChunkEvent(string Text) : NotesEvent;
public sealed record DoneEvent : NotesEvent;
