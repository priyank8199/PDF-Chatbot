namespace PdfChatbot;

/// <summary>
/// Splits text into ~300-token chunks. Rule of thumb: 1 token ≈ 0.75 words,
/// so ~225 words per chunk, with a small overlap so sentences aren't cut in half.
/// </summary>
public static class Chunker
{
    public static List<string> Split(string text, int wordsPerChunk = 225, int overlap = 30)
    {
        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var chunks = new List<string>();
        for (int start = 0; start < words.Length; start += wordsPerChunk - overlap)
        {
            chunks.Add(string.Join(' ', words.Skip(start).Take(wordsPerChunk)));
            if (start + wordsPerChunk >= words.Length) break;
        }
        return chunks;
    }
}
