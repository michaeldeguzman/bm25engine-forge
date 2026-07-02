using BM25Engine.Models;

namespace BM25Engine;

/// <summary>
/// Public entry points exposed as ODC Server Actions via the OutSystems External Logic SDK.
/// Stateless — every method takes all data it needs as parameters and returns a complete result.
/// </summary>
public static class BM25EngineLibrary
{
    /// <summary>
    /// Tokenizes, removes stop words, and stems the input text. Used identically at ingestion
    /// time (indexing a chunk) and query time (parsing a search query), guaranteeing consistent
    /// term matching between the two paths.
    /// </summary>
    public static List<string> TokenizeText(string text) => Tokenizer.Tokenize(text);

    /// <summary>
    /// Indexes a single chunk of text: tokenizes it and returns the token count plus per-term
    /// frequencies needed to build BM25Posting and BM25Term rows in ODC. Stateless — call once
    /// per chunk during ingestion or backfill.
    /// </summary>
    public static ChunkIndexResult IndexChunk(string chunkText)
    {
        var tokens = Tokenizer.Tokenize(chunkText);

        var frequencies = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var token in tokens)
        {
            frequencies[token] = frequencies.TryGetValue(token, out var count) ? count + 1 : 1;
        }

        var terms = new List<TermFrequency>(frequencies.Count);
        foreach (var (term, frequency) in frequencies)
        {
            terms.Add(new TermFrequency { Term = term, Frequency = frequency });
        }

        return new ChunkIndexResult { TokenCount = tokens.Count, Terms = terms };
    }

    /// <summary>
    /// Scores a set of candidate chunks against a query using BM25. ODC is responsible for
    /// tokenizing the query via TokenizeText, looking up matching BM25Posting/BM25Term rows for
    /// those query terms, and passing the resulting candidate postings plus corpus stats from
    /// BM25Stats and tuning parameters from Site Properties (K1, B) into this method. Returns all
    /// scored chunks ranked descending; ODC applies any final top-K truncation via the optional
    /// topK parameter (null returns everything).
    /// </summary>
    public static List<BM25ScoreResult> ScoreQuery(
        List<string> queryTerms,
        List<PostingCandidate> postings,
        int totalChunks,
        double avgChunkLength,
        double k1,
        double b,
        int? topK = null)
    {
        var results = BM25Scorer.Score(queryTerms, postings, totalChunks, avgChunkLength, k1, b);
        return topK.HasValue ? results.Take(topK.Value).ToList() : results;
    }
}
