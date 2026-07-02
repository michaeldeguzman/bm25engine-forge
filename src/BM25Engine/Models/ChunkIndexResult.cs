using OutSystems.ExternalLibraries.SDK;

namespace BM25Engine.Models;

/// <summary>Result of indexing one chunk: its total stemmed token count plus per-term frequencies.</summary>
[OSStructure(Description = "Result of indexing one chunk: its total stemmed token count plus per-term frequencies.")]
public struct ChunkIndexResult
{
    [OSStructureField(Description = "Total stemmed token count in this chunk, post stop-word removal.")]
    public int TokenCount { get; set; }

    [OSStructureField(Description = "One entry per unique stemmed term in this chunk.")]
    public List<TermFrequency> Terms { get; set; }
}
