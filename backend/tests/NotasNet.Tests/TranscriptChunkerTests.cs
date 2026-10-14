using NotasNet.Api.Application;

namespace NotasNet.Tests;

public class TranscriptChunkerTests
{
    private static string Words(int n) => string.Join(" ", Enumerable.Range(0, n).Select(i => $"w{i}"));

    [Fact]
    public void No_part_exceeds_chunk_size_and_all_words_are_preserved()
    {
        var text = Words(200);
        var parts = TranscriptChunker.Split(text, 100);

        Assert.True(parts.Count > 1);
        Assert.All(parts, p => Assert.True(p.Length <= 100));
        Assert.Equal(text, string.Join(" ", parts));
    }

    [Fact]
    public void Short_text_is_a_single_part()
    {
        Assert.Equal(["hola mundo"], TranscriptChunker.Split("hola mundo", 100));
    }

    [Fact]
    public void A_single_token_longer_than_chunk_size_is_hard_split()
    {
        var parts = TranscriptChunker.Split(new string('x', 250), 100);

        Assert.All(parts, p => Assert.True(p.Length <= 100));
        Assert.Equal(250, parts.Sum(p => p.Length));
    }
}
