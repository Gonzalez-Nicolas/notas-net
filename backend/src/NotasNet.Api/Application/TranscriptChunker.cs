using System.Text;

namespace NotasNet.Api.Application;

public static class TranscriptChunker
{
    public static IReadOnlyList<string> Split(string text, int chunkChars)
    {
        var parts = new List<string>();
        var current = new StringBuilder();

        foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var piece in word.Chunk(chunkChars).Select(c => new string(c)))
            {
                var needed = current.Length == 0 ? piece.Length : current.Length + 1 + piece.Length;
                if (needed > chunkChars)
                {
                    parts.Add(current.ToString());
                    current.Clear();
                }
                if (current.Length > 0) current.Append(' ');
                current.Append(piece);
            }
        }

        if (current.Length > 0) parts.Add(current.ToString());
        return parts;
    }
}
