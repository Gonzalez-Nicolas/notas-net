using System.Runtime.CompilerServices;
using NotasNet.Api.Application;

namespace NotasNet.Tests;

public class GenerateNotesUseCaseTests
{
    private const string ValidUrl = "https://youtu.be/dQw4w9WgXcQ";

    private sealed class FakeTranscript(string text) : ITranscriptProvider
    {
        public int Calls;
        public Task<string> GetAsync(string videoId, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(text);
        }
    }

    private sealed class FakeLlm(params string[] chunks) : ILlmProvider
    {
        public List<(string System, string Content)> Calls { get; } = [];
        public async IAsyncEnumerable<string> StreamAsync(
            string systemPrompt, string content, [EnumeratorCancellation] CancellationToken ct)
        {
            Calls.Add((systemPrompt, content));
            foreach (var c in chunks)
            {
                ct.ThrowIfCancellationRequested();
                await Task.Yield();
                yield return c;
            }
        }
    }

    private static string Words(int n) => string.Join(" ", Enumerable.Range(0, n).Select(i => $"w{i}"));

    private static async Task<List<NotesEvent>> Collect(GenerateNotesUseCase uc, string url, CancellationToken ct = default)
    {
        var list = new List<NotesEvent>();
        await foreach (var e in uc.ExecuteAsync(url, ct)) list.Add(e);
        return list;
    }

    private static NotesOptions Opts(int max = 400_000, int chunk = 60_000, int minWords = 5) =>
        new(max, chunk, minWords, "SYSTEM");

    [Fact]
    public async Task Invalid_url_throws_InvalidUrl_without_calling_providers()
    {
        var t = new FakeTranscript(Words(50));
        var uc = new GenerateNotesUseCase(t, new FakeLlm("x"), Opts());

        var ex = await Assert.ThrowsAsync<NotesException>(() => Collect(uc, "hola"));

        Assert.Equal(NotesErrorCode.InvalidUrl, ex.Code);
        Assert.Equal(0, t.Calls);
    }

    [Fact]
    public async Task Transcript_with_too_few_words_throws_NoCaptions()
    {
        var uc = new GenerateNotesUseCase(new FakeTranscript("uno dos"), new FakeLlm("x"), Opts(minWords: 5));

        var ex = await Assert.ThrowsAsync<NotesException>(() => Collect(uc, ValidUrl));

        Assert.Equal(NotesErrorCode.NoCaptions, ex.Code);
    }

    [Fact]
    public async Task Happy_path_emits_status_then_chunks_in_order_then_done()
    {
        var uc = new GenerateNotesUseCase(new FakeTranscript(Words(50)), new FakeLlm("a", "b", "c"), Opts());

        var events = await Collect(uc, ValidUrl);

        Assert.IsType<StatusEvent>(events[0]);
        Assert.Equal(["a", "b", "c"], events.OfType<ChunkEvent>().Select(c => c.Text));
        Assert.IsType<DoneEvent>(events[^1]);
    }

    [Fact]
    public async Task Long_transcript_is_summarized_in_parts_and_only_final_pass_emits_chunks()
    {
        var llm = new FakeLlm("r");
        var uc = new GenerateNotesUseCase(new FakeTranscript(Words(100)), llm, Opts(chunk: 100));
        var expectedParts = TranscriptChunker.Split(Words(100), 100).Count;

        var events = await Collect(uc, ValidUrl);

        Assert.True(expectedParts > 1);
        Assert.Equal(expectedParts + 1, llm.Calls.Count);
        Assert.Single(events.OfType<ChunkEvent>());
    }

    [Fact]
    public async Task Transcript_over_max_is_truncated_before_being_sent()
    {
        var llm = new FakeLlm("r");
        var uc = new GenerateNotesUseCase(new FakeTranscript(Words(1000)), llm, Opts(max: 200, chunk: 100));

        await Collect(uc, ValidUrl);

        var partsSent = llm.Calls.Take(llm.Calls.Count - 1).Sum(c => c.Content.Length);
        Assert.True(partsSent <= 200);
    }

    [Fact]
    public async Task Cancelling_stops_enumeration()
    {
        using var cts = new CancellationTokenSource();
        var uc = new GenerateNotesUseCase(new FakeTranscript(Words(50)), new FakeLlm("a", "b", "c"), Opts());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await foreach (var e in uc.ExecuteAsync(ValidUrl, cts.Token))
                if (e is ChunkEvent) cts.Cancel();
        });
    }

    [Fact]
    public async Task Llm_that_returns_no_text_throws_LlmFailure_instead_of_empty_success()
    {
        var uc = new GenerateNotesUseCase(new FakeTranscript(Words(50)), new FakeLlm(), Opts());

        var ex = await Assert.ThrowsAsync<NotesException>(() => Collect(uc, ValidUrl));

        Assert.Equal(NotesErrorCode.LlmFailure, ex.Code);
    }

    private sealed class FakeQuota(bool allow) : IUsageQuota
    {
        public int Calls;
        public bool TryAcquire()
        {
            Calls++;
            return allow;
        }
    }

    [Fact]
    public async Task Quota_is_only_consumed_after_url_and_transcript_checks_pass()
    {
        var quota = new FakeQuota(true);

        var invalid = new GenerateNotesUseCase(new FakeTranscript(Words(50)), new FakeLlm("x"), Opts(), quota);
        await Assert.ThrowsAsync<NotesException>(() => Collect(invalid, "hola"));
        var noCaptions = new GenerateNotesUseCase(new FakeTranscript("uno"), new FakeLlm("x"), Opts(), quota);
        await Assert.ThrowsAsync<NotesException>(() => Collect(noCaptions, ValidUrl));
        Assert.Equal(0, quota.Calls);

        var ok = new GenerateNotesUseCase(new FakeTranscript(Words(50)), new FakeLlm("x"), Opts(), quota);
        await Collect(ok, ValidUrl);
        Assert.Equal(1, quota.Calls);
    }

    [Fact]
    public async Task Denied_quota_throws_RateLimited_without_calling_the_llm()
    {
        var llm = new FakeLlm("x");
        var uc = new GenerateNotesUseCase(new FakeTranscript(Words(50)), llm, Opts(), new FakeQuota(false));

        var ex = await Assert.ThrowsAsync<NotesException>(() => Collect(uc, ValidUrl));

        Assert.Equal(NotesErrorCode.RateLimited, ex.Code);
        Assert.Empty(llm.Calls);
    }
}
