using NotasNet.Api.Application;

namespace NotasNet.Tests;

public class VideoUrlParserTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ&t=30s&list=PL1")]
    public void Extracts_video_id_from_supported_urls(string url)
    {
        Assert.True(VideoUrlParser.TryExtractVideoId(url, out var id));
        Assert.Equal("dQw4w9WgXcQ", id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("hola")]
    [InlineData("https://vimeo.com/123")]
    [InlineData("https://www.youtube.com/watch?v=corto")]
    public void Rejects_invalid_urls(string? url)
    {
        Assert.False(VideoUrlParser.TryExtractVideoId(url, out var id));
        Assert.Equal("", id);
    }
}
