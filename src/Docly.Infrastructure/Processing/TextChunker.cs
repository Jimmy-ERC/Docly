namespace Docly.Infrastructure.Processing;

/// <summary>
/// Splits extracted text into overlapping, roughly fixed-size fragments. Pure algorithm, no external
/// dependency, so it isn't exposed behind an Application interface (see <see cref="Docly.Application.Processing.ITextExtractionService"/>
/// for the seam that does need one).
/// </summary>
public static class TextChunker
{
    /// <summary>
    /// Splits <paramref name="text"/> into chunks of at most <paramref name="chunkSize"/> characters,
    /// each one (except the first) starting <paramref name="overlap"/> characters before the previous
    /// chunk ended. Chunk boundaries snap to the nearest preceding whitespace so words aren't split.
    /// </summary>
    public static IReadOnlyList<string> Chunk(string text, int chunkSize, int overlap)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            return [];
        }

        var chunks = new List<string>();
        var start = 0;

        while (start < text.Length)
        {
            var end = Math.Min(start + chunkSize, text.Length);

            if (end < text.Length)
            {
                var lastWhitespace = text.LastIndexOfAny([' ', '\n', '\r', '\t'], end - 1, end - start);
                if (lastWhitespace > start)
                {
                    end = lastWhitespace;
                }
            }

            var chunk = text[start..end].Trim();
            if (chunk.Length > 0)
            {
                chunks.Add(chunk);
            }

            if (end >= text.Length)
            {
                break;
            }

            start = Math.Max(end - overlap, start + 1);
        }

        return chunks;
    }
}
