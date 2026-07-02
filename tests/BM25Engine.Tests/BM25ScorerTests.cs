using BM25Engine.Models;

namespace BM25Engine.Tests;

public class BM25ScorerTests
{
    // Hand-calculated example.
    // Corpus: totalChunks = 4, avgChunkLength = 10, k1 = 1.2, b = 0.75.
    // Query terms: "cach", "invalid".
    //
    // Chunk 1: length 8.  f(cach)=2, f(invalid)=1
    // Chunk 2: length 12. f(cach)=1, f(invalid)=0 (no posting row for "invalid" in chunk 2)
    // Chunk 3: length 10. f(cach)=0 (no posting row), f(invalid)=3
    //
    // DF: "cach" appears in 2 chunks (chunk1, chunk2) -> n=2
    //     "invalid" appears in 2 chunks (chunk1, chunk3) -> n=2
    //
    // IDF(t) = ln((N - n + 0.5) / (n + 0.5) + 1), N=4, n=2
    //        = ln((4 - 2 + 0.5) / (2 + 0.5) + 1) = ln(2.5/2.5 + 1) = ln(2) = 0.6931471805599453
    //
    // Chunk 1 score (lengthNorm = 1-0.75+0.75*(8/10) = 0.85):
    //   cach:    f=2, term = 0.6931471805599453 * (2*2.2) / (2+1.2*0.85) = 1.0098833094250859
    //   invalid: f=1, term = 0.6931471805599453 * (1*2.2) / (1+1.2*0.85) = 0.7549127709068711
    //   chunk1 total = 1.7647960803319571
    //
    // Chunk 2 score (lengthNorm = 1-0.75+0.75*(12/10) = 1.15):
    //   cach:    f=1, term = 0.6931471805599453 * (1*2.2) / (1+1.2*1.15) = 0.64072428455121
    //   invalid: no posting -> contributes 0
    //   chunk2 total = 0.64072428455121
    //
    // Chunk 3 score (lengthNorm = 1-0.75+0.75*(10/10) = 1.0):
    //   cach: no posting -> contributes 0
    //   invalid: f=3, term = 0.6931471805599453 * (3*2.2) / (3+1.2*1.0) = 1.089231283737057
    //   chunk3 total = 1.089231283737057
    //
    // (Values computed via `python3 -c "import math; ..."` — reproduce with the same
    // computation if these ever need re-deriving.)
    [Fact]
    public void Score_HandCalculatedExample_MatchesExpectedScores()
    {
        var queryTerms = new List<string> { "cach", "invalid" };
        var postings = new List<PostingCandidate>
        {
            new() { ChunkId = 1, Term = "cach", TermFrequencyInChunk = 2, DocumentFrequency = 2, ChunkTokenCount = 8 },
            new() { ChunkId = 1, Term = "invalid", TermFrequencyInChunk = 1, DocumentFrequency = 2, ChunkTokenCount = 8 },
            new() { ChunkId = 2, Term = "cach", TermFrequencyInChunk = 1, DocumentFrequency = 2, ChunkTokenCount = 12 },
            new() { ChunkId = 3, Term = "invalid", TermFrequencyInChunk = 3, DocumentFrequency = 2, ChunkTokenCount = 10 },
        };

        var results = BM25Scorer.Score(queryTerms, postings, totalChunks: 4, avgChunkLength: 10, k1: 1.2, b: 0.75);

        Assert.Equal(3, results.Count);

        var chunk1 = results.Single(r => r.ChunkId == 1);
        var chunk2 = results.Single(r => r.ChunkId == 2);
        var chunk3 = results.Single(r => r.ChunkId == 3);

        Assert.Equal(1.7647960803319571, chunk1.Score, 1e-6);
        Assert.Equal(0.64072428455121, chunk2.Score, 1e-6);
        Assert.Equal(1.089231283737057, chunk3.Score, 1e-6);

        // Descending sort: chunk1 > chunk3 > chunk2
        Assert.Equal(1L, results[0].ChunkId);
        Assert.Equal(3L, results[1].ChunkId);
        Assert.Equal(2L, results[2].ChunkId);
    }

    [Fact]
    public void Score_QueryTermWithNoPostingForChunk_ContributesZeroNotError()
    {
        var queryTerms = new List<string> { "cach", "ghost" };
        var postings = new List<PostingCandidate>
        {
            new() { ChunkId = 1, Term = "cach", TermFrequencyInChunk = 1, DocumentFrequency = 1, ChunkTokenCount = 10 },
        };

        var results = BM25Scorer.Score(queryTerms, postings, totalChunks: 1, avgChunkLength: 10, k1: 1.2, b: 0.75);

        Assert.Single(results);
        Assert.True(results[0].Score > 0);
    }

    [Fact]
    public void Score_ZeroLengthChunk_DoesNotThrow()
    {
        var queryTerms = new List<string> { "cach" };
        var postings = new List<PostingCandidate>
        {
            new() { ChunkId = 1, Term = "cach", TermFrequencyInChunk = 1, DocumentFrequency = 1, ChunkTokenCount = 0 },
        };

        var results = BM25Scorer.Score(queryTerms, postings, totalChunks: 5, avgChunkLength: 10, k1: 1.2, b: 0.75);

        Assert.Single(results);
        Assert.False(double.IsNaN(results[0].Score));
    }
}
