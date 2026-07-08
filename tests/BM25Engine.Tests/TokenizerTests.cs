namespace BM25Engine.Tests;

public class TokenizerTests
{
    [Fact]
    public void Tokenize_EmptyString_ReturnsEmptyList()
    {
        Assert.Empty(Tokenizer.Tokenize(""));
    }

    [Fact]
    public void Tokenize_Whitespace_ReturnsEmptyList()
    {
        Assert.Empty(Tokenizer.Tokenize("   \t\n  "));
    }

    [Fact]
    public void Tokenize_OnlyStopWords_ReturnsEmptyList()
    {
        Assert.Empty(Tokenizer.Tokenize("the a an of and or is are"));
    }

    [Fact]
    public void Tokenize_MixedCase_LowercasesBeforeStemming()
    {
        var result = Tokenizer.Tokenize("RUNNING Runner RUNS");
        Assert.Equal(new List<string> { "run", "runner", "run" }, result);
    }

    [Fact(Skip = "v1 tokenizer behavior (punctuation deleted, not space-replaced) was replaced by " +
                 "v2 in this commit. Kept, renamed and skipped rather than deleted so the v1 " +
                 "behavior stays in the test history for audit purposes. See " +
                 "Tokenize_PunctuationHeavy_SplitsOnPunctuationBoundaries_v2 for current behavior.")]
    public void Tokenize_PunctuationHeavy_StripsPunctuationAndFusesTokens_v1_deprecated()
    {
        // "ORA-01400" -> hyphen stripped (deleted, not replaced) -> "ora01400" as a single fused
        // alphanumeric token. This was the documented v1 behavior — exact codes weren't preserved
        // as distinct match targets. Deprecated: this fusion is what caused the ERR-BM25-003
        // retrieval anomaly (query token never matched a differently-fused indexed token).
        var result = Tokenizer.Tokenize("Error: ORA-01400, can't insert NULL!");
        Assert.Contains("ora01400", result);
    }

    [Fact]
    public void Tokenize_PunctuationHeavy_SplitsOnPunctuationBoundaries_v2()
    {
        // v2: stripped punctuation is replaced with a space, so it acts as a word boundary
        // instead of silently fusing adjacent alphanumeric runs. "ORA-01400" now splits into
        // "ora" and "01400" instead of fusing into "ora01400".
        var result = Tokenizer.Tokenize("Error: ORA-01400, can't insert NULL!");
        Assert.Contains("ora", result);
        Assert.Contains("01400", result);
        Assert.DoesNotContain("ora01400", result);
    }

    [Theory]
    [InlineData("ERR-BM25-003", new[] { "err", "bm25", "003" })]
    [InlineData("ERR-VEC-002 ChunkHash", new[] { "err", "vec", "002", "chunkhash" })]
    [InlineData("TokenizerVersion v1-stemmed-porter", new[] { "tokenizervers", "v1", "stem", "porter" })]
    [InlineData("PostingCandidate DocumentFrequency ChunkTokenCount",
        new[] { "postingcandid", "documentfrequ", "chunktokencount" })]
    [InlineData("BackfillBM25Index null TokenCount", new[] { "backfillbm25index", "null", "tokencount" })]
    public void Tokenize_SetAQueries_ProducesV2SplitTokens(string input, string[] expected)
    {
        // v2 regression coverage for the queries that surfaced the ERR-BM25-003 anomaly.
        // Hyphens now split ("v1-stemmed-porter" -> "v1", "stem", "porter") the same way
        // colons and periods do — there is no special-casing for any one punctuation mark.
        var result = Tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Tokenize_Chunk68_SplitsErrBm25003IntoSeparateTokens()
    {
        // Chunk 68 is missing whitespace after several sentence-ending periods (a PDF-extraction
        // artifact — e.g. "...on retry.ERR-BM25-003..."). Under v1 this fused into
        // "retryerrbm25003", which never matched the query token "errbm25003". Under v2 the
        // period is now a boundary too, so "err", "bm25", "003" tokenize the same regardless of
        // adjacent whitespace being present in the source text.
        var result = Tokenizer.Tokenize(Chunk68Text);
        Assert.Contains("err", result);
        Assert.Contains("bm25", result);
        Assert.Contains("003", result);
    }

    [Fact]
    public void Tokenize_Chunk69_SplitsErrBm25003IntoSeparateTokens()
    {
        var result = Tokenizer.Tokenize(Chunk69Text);
        Assert.Contains("err", result);
        Assert.Contains("bm25", result);
        Assert.Contains("003", result);
    }

    [Fact]
    public void Tokenize_Chunk72_SplitsErrBm25003IntoSeparateTokens()
    {
        var result = Tokenizer.Tokenize(Chunk72Text);
        Assert.Contains("err", result);
        Assert.Contains("bm25", result);
        Assert.Contains("003", result);
    }

    [Fact]
    public void Tokenize_RealisticParagraph_ProducesStemmedContentTerms()
    {
        var text = "The database connection was refused because the server was overloaded " +
                    "during the nightly indexing process.";
        var result = Tokenizer.Tokenize(text);

        Assert.DoesNotContain("the", result);
        Assert.DoesNotContain("was", result);
        Assert.DoesNotContain("because", result);
        Assert.Contains("databas", result);
        Assert.Contains("connect", result);
        Assert.Contains("refus", result);
        Assert.Contains("server", result);
        Assert.Contains("overload", result);
        Assert.Contains("nightli", result);
        Assert.Contains("index", result);
        Assert.Contains("process", result);
    }

    [Fact]
    public void Tokenize_PreservesDuplicateOccurrences()
    {
        var result = Tokenizer.Tokenize("cache cache cache miss");
        Assert.Equal(3, result.Count(t => t == "cach"));
    }

    [Fact]
    public void Tokenize_KeepsAlphanumericTechnicalTokens()
    {
        var result = Tokenizer.Tokenize("upgrade to v4 now, gpt4 is faster");
        Assert.Contains("v4", result);
        Assert.Contains("gpt4", result);
    }

    [Fact]
    public void Tokenize_DropsStraySingleDigits()
    {
        var result = Tokenizer.Tokenize("item 5 in the list");
        Assert.DoesNotContain("5", result);
    }

    // Real chunk text from the ODC RAG Pipeline Technical Reference document that surfaced the
    // ERR-BM25-003 retrieval anomaly (see tests/bm25_anomaly_test_data.json). Copied verbatim,
    // including the missing whitespace after several periods — that missing whitespace is what
    // caused the v1 fusion bug this test suite now guards against.
    private const string Chunk68Text =
        "Error Reference and TroubleshootingBM25Engine Ingestion ErrorsThe following error codes " +
        "are returned by the BM25Engine External Logic component during ingestion.Each code maps " +
        "to a specific failure condition in the tokenization or posting-write pipeline.ERR-BM25-001: " +
        "TokenCount is null on DocumentChunk after IndexChunk call. This indicates theBM25Engine." +
        "IndexChunk action returned successfully but the ODC orchestration layer failed to writethe " +
        "TokenCount attribute back to the DocumentChunk entity. Check the DocumentChunk Update stepin " +
        "the IndexChunks workflow for a missing or mis-mapped attribute assignment.ERR-BM25-002: " +
        "BM25Term upsert conflict on unique index. Two concurrent ingestion workflowsattempted to " +
        "create the same stemmed term simultaneously. The unique index on BM25Term.Termcorrectly " +
        "prevented the duplicate. The second workflow should retry the find-or-create with " +
        "aGetBM25TermByTerm aggregate. This error is transient and self-resolving on retry." +
        "ERR-BM25-003: ScoreQuery";

    private const string Chunk69Text =
        "should retry the find-or-create with aGetBM25TermByTerm aggregate. This error is " +
        "transient and self-resolving on retry.ERR-BM25-003: ScoreQuery returned empty results " +
        "despite non-empty postings. Most commonlycaused by a mismatch between the " +
        "TokenizerVersion Site Property and the BM25Engine revisioncurrently deployed. If " +
        "ingestion used v1-stemmed-porter and query-time TokenizeText is running adifferent " +
        "revision, query stems will not match indexed stems. Reindex the corpus after updating " +
        "theBM25Engine revision.ERR-BM25-004: avgChunkLength divide-by-zero in ScoreQuery. " +
        "BM25Stats.TotalChunks is zero,meaning the RecomputeBM25Stats timer has never run " +
        "successfully. Trigger the timer manually fromthe ODC Portal and verify BM25Stats " +
        "contains exactly one row with TotalChunks greater than zerobefore retrying the search.";

    private const string Chunk72Text =
        "Retrieval Quality and ArchitectureWhy Dense Retrieval Alone Is InsufficientSemantic " +
        "vector search encodes meaning rather than surface form. A query for 'document " +
        "ingestionfailure' will retrieve content about 'file processing errors' even if those " +
        "exact words never appeartogether. This generalisation is the core strength of dense " +
        "retrieval and the reason it outperformskeyword search on paraphrased or conceptual " +
        "queries.However, this same generalisation becomes a weakness when the query is already " +
        "precise. Adeveloper searching for ERR-BM25-003 is not expressing a concept — they are " +
        "expressing an identifier. The embedding model has no basis to place 'ERR-BM25-003' near " +
        "'ScoreQuery empty results' in the vector space.";
}
