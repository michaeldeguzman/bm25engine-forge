using BM25Engine.Models;
using OutSystems.ExternalLibraries.SDK;

namespace BM25Engine;

[OSInterface(
    Name = "BM25Engine",
    Description = "Native BM25 lexical search scoring for OutSystems Developer Cloud (ODC): tokenization, Porter stemming, and BM25 ranking math, with zero external dependencies.")]
public interface IBM25EngineActions
{
    [OSAction(
        Description = "Lowercases, strips punctuation, removes stop words, and Porter-stems the input text. Must be called with identical logic at both ingestion time (indexing a chunk) and query time (parsing a search query) so terms actually match — never fork this logic.",
        ReturnName = "Tokens")]
    List<string> TokenizeText(
        [OSParameter(Description = "The text to tokenize — a document chunk at ingestion time, or a search query at query time.")]
        string text);

    [OSAction(
        Description = "Indexes a single chunk of text: tokenizes it and returns the token count plus per-term frequencies needed to build BM25Posting and BM25Term rows. Call once per chunk during ingestion or backfill.",
        ReturnName = "IndexResult")]
    ChunkIndexResult IndexChunk(
        [OSParameter(Description = "The chunk of text to index.")]
        string chunkText);

    [OSAction(
        Description = "Scores a set of candidate chunks against a query using BM25. ODC tokenizes the query via TokenizeText, looks up matching BM25Posting/BM25Term rows for those query terms, and passes the resulting candidate postings plus corpus stats from BM25Stats and tuning parameters from Site Properties (K1, B) into this method. Returns all scored chunks ranked descending by score.",
        ReturnName = "Results")]
    List<BM25ScoreResult> ScoreQuery(
        [OSParameter(Description = "The tokenized, stemmed query terms — the output of TokenizeText run on the user's search query.")]
        List<string> queryTerms,
        [OSParameter(Description = "Candidate (term, chunk) postings for every chunk that matches at least one query term.")]
        List<PostingCandidate> postings,
        [OSParameter(Description = "Total number of chunks in the corpus (N in the IDF formula), sourced from BM25Stats. Never derived from the postings list.")]
        int totalChunks,
        [OSParameter(Description = "Average chunk length (token count) across the corpus, sourced from BM25Stats.")]
        double avgChunkLength,
        [OSParameter(Description = "BM25 term-frequency saturation parameter, sourced from an ODC Site Property. Typical default: 1.2.")]
        double k1,
        [OSParameter(Description = "BM25 length-normalization parameter, sourced from an ODC Site Property. Typical default: 0.75.")]
        double b,
        [OSParameter(Description = "Optional maximum number of results to return. Leave unset to return every scored chunk.")]
        int? topK = null);
}
