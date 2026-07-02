using BM25Engine.Models;

namespace BM25Engine;

/// <summary>
/// ODC-facing implementation of <see cref="IBM25EngineActions"/>. Thin delegation layer over
/// <see cref="BM25EngineLibrary"/>, which remains directly unit-testable without any SDK wiring.
/// </summary>
public class BM25EngineActions : IBM25EngineActions
{
    public List<string> TokenizeText(string text)
        => BM25EngineLibrary.TokenizeText(text);

    public ChunkIndexResult IndexChunk(string chunkText)
        => BM25EngineLibrary.IndexChunk(chunkText);

    public List<BM25ScoreResult> ScoreQuery(
        List<string> queryTerms,
        List<PostingCandidate> postings,
        int totalChunks,
        double avgChunkLength,
        double k1,
        double b,
        int? topK = null)
        => BM25EngineLibrary.ScoreQuery(queryTerms, postings, totalChunks, avgChunkLength, k1, b, topK);
}
