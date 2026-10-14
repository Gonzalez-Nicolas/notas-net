using System.Text.RegularExpressions;

namespace NotasNet.Api.Application;

public static partial class VideoUrlParser
{
    private static readonly HashSet<string> YoutubeHosts =
        ["youtube.com", "www.youtube.com", "m.youtube.com"];

    [GeneratedRegex("^[A-Za-z0-9_-]{11}$")]
    private static partial Regex VideoIdRegex();

    public static bool TryExtractVideoId(string? input, out string videoId)
    {
        videoId = "";
        if (string.IsNullOrWhiteSpace(input)
            || !Uri.TryCreate(input.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            return false;

        var host = uri.Host.ToLowerInvariant();
        string? candidate = null;

        if (host == "youtu.be")
            candidate = uri.AbsolutePath.Trim('/');
        else if (YoutubeHosts.Contains(host))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments is ["shorts", var shortId])
                candidate = shortId;
            else if (segments is ["watch"])
                candidate = GetQueryValue(uri.Query, "v");
        }

        if (candidate is null || !VideoIdRegex().IsMatch(candidate)) return false;
        videoId = candidate;
        return true;
    }

    private static string? GetQueryValue(string query, string key)
    {
        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts[0] == key && parts.Length == 2) return Uri.UnescapeDataString(parts[1]);
        }
        return null;
    }
}
