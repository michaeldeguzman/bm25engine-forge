using OutSystems.ExternalLibraries.SDK;

namespace BM25Engine.Models;

/// <summary>
/// One (term, chunk) posting fetched from ODC's BM25Posting/BM25Term entities, passed in at
/// query time. Note: <see cref="DocumentFrequency"/> is a property of the TERM, not this
/// individual posting. Every posting row for the same term carries an identical DF value.
/// BM25Scorer must read it once per unique term, not once per row.
/// </summary>
[OSStructure(Description = "One (term, chunk) posting fetched from BM25Posting/BM25Term, passed in at query time.")]
public struct PostingCandidate
{
    [OSStructureField(Description = "Id of the chunk this posting belongs to.")]
    public long ChunkId { get; set; }

    [OSStructureField(Description = "The stemmed term this posting is for.")]
    public string Term { get; set; }

    [OSStructureField(Description = "How many times this term occurs in this chunk.")]
    public int TermFrequencyInChunk { get; set; }

    [OSStructureField(Description = "How many chunks in the whole corpus contain this term. A property of the term, not this individual posting: identical across every posting row for the same term.")]
    public int DocumentFrequency { get; set; }

    [OSStructureField(Description = "This chunk's total token count, used for BM25 length normalization.")]
    public int ChunkTokenCount { get; set; }
}
