using NotasNet.Api.Infrastructure;

namespace NotasNet.Tests;

public class SseLineParserTests
{
    [Fact]
    public void Extracts_text_from_gemini_chunk()
    {
        const string json = """{"candidates":[{"content":{"parts":[{"text":"Hola"}],"role":"model"},"index":0}]}""";
        Assert.Equal("Hola", SseLineParser.ExtractGeminiText(json));
    }

    [Theory]
    [InlineData("""{"usageMetadata":{"totalTokenCount":3}}""")]
    [InlineData("""{"candidates":[{"finishReason":"STOP"}]}""")]
    [InlineData("no es json")]
    public void Returns_null_when_there_is_no_text(string json)
    {
        Assert.Null(SseLineParser.ExtractGeminiText(json));
    }
}
