using OutSystems.ExternalLibraries.SDK;

namespace BM25Engine.Models;

/// <summary>One stemmed term and how many times it occurs within a single chunk.</summary>
[OSStructure(Description = "One stemmed term and how many times it occurs within a single chunk.")]
public struct TermFrequency
{
    [OSStructureField(Description = "The stemmed term.")]
    public string Term { get; set; }

    [OSStructureField(Description = "Count of this term within the chunk.")]
    public int Frequency { get; set; }
}
