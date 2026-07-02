using System.Text;

namespace BM25Engine;

/// <summary>
/// Tokenizes free text into stemmed terms for BM25 indexing and querying. This exact pipeline
/// must run identically at ingestion time and query time — that identity is what guarantees
/// ingestion and query terms actually match. Never fork this logic.
/// </summary>
public static class Tokenizer
{
    /// <summary>
    /// Lowercases, strips punctuation, removes stop words, and Porter-stems the input text.
    /// Duplicates are preserved — frequency counting is the caller's responsibility.
    /// </summary>
    public static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        var lowered = text.ToLowerInvariant();

        var stripped = new StringBuilder(lowered.Length);
        foreach (char c in lowered)
        {
            if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                stripped.Append(c);
        }

        var rawTokens = stripped.ToString()
            .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var result = new List<string>(rawTokens.Length);
        foreach (var token in rawTokens)
        {
            if (token.Length < 2 && IsPurelyNumeric(token))
                continue;

            if (StopWords.IsStopWord(token))
                continue;

            result.Add(PorterStemmer.Stem(token));
        }

        return result;
    }

    private static bool IsPurelyNumeric(string token)
    {
        foreach (char c in token)
            if (!char.IsDigit(c)) return false;
        return true;
    }
}
