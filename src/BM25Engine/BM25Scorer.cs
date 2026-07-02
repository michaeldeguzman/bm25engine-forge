using BM25Engine.Models;

namespace BM25Engine;

/// <summary>
/// Computes Okapi BM25 scores for a candidate set of chunks against a query.
/// </summary>
public static class BM25Scorer
{
    /// <summary>
    /// Scores every distinct chunk present in <paramref name="postings"/> against
    /// <paramref name="queryTerms"/>, returning results sorted descending by score.
    /// </summary>
    public static List<BM25ScoreResult> Score(
        List<string> queryTerms,
        List<PostingCandidate> postings,
        int totalChunks,
        double avgChunkLength,
        double k1,
        double b)
    {
        var queryTermSet = new HashSet<string>(queryTerms, StringComparer.Ordinal);

        // DocumentFrequency is duplicated across every posting row for a term; read it once
        // per unique term rather than re-reading it per row.
        var documentFrequencyByTerm = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var posting in postings)
        {
            if (!queryTermSet.Contains(posting.Term)) continue;
            documentFrequencyByTerm[posting.Term] = posting.DocumentFrequency;
        }

        // IDF uses the always-positive Lucene/Elasticsearch variant (+1 inside the log) rather
        // than the original 1994 Robertson-Sparck Jones formula, which can go negative for very
        // common terms in a small corpus.
        var idfByTerm = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var term in queryTermSet)
        {
            int n = documentFrequencyByTerm.TryGetValue(term, out var df) ? df : 0;
            idfByTerm[term] = Math.Log((totalChunks - n + 0.5) / (n + 0.5) + 1);
        }

        var postingsByChunk = new Dictionary<long, List<PostingCandidate>>();
        foreach (var posting in postings)
        {
            if (!queryTermSet.Contains(posting.Term)) continue;
            if (!postingsByChunk.TryGetValue(posting.ChunkId, out var list))
            {
                list = new List<PostingCandidate>();
                postingsByChunk[posting.ChunkId] = list;
            }
            list.Add(posting);
        }

        var results = new List<BM25ScoreResult>(postingsByChunk.Count);
        foreach (var (chunkId, chunkPostings) in postingsByChunk)
        {
            double score = 0.0;
            foreach (var posting in chunkPostings)
            {
                double idf = idfByTerm[posting.Term];
                double f = posting.TermFrequencyInChunk;
                double lengthNorm = 1 - b + b * (posting.ChunkTokenCount / avgChunkLength);
                score += idf * (f * (k1 + 1)) / (f + k1 * lengthNorm);
            }

            results.Add(new BM25ScoreResult { ChunkId = chunkId, Score = score });
        }

        results.Sort((x, y) => y.Score.CompareTo(x.Score));
        return results;
    }
}
