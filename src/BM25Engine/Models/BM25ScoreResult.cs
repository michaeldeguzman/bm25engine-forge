using OutSystems.ExternalLibraries.SDK;

namespace BM25Engine.Models;

/// <summary>A chunk's BM25 score against a query.</summary>
[OSStructure(Description = "A chunk's BM25 score against a query.")]
public struct BM25ScoreResult
{
    [OSStructureField(Description = "Id of the scored chunk.")]
    public long ChunkId { get; set; }

    [OSStructureField(Description = "The chunk's BM25 score against the query. Higher is more relevant.")]
    public double Score { get; set; }
}
