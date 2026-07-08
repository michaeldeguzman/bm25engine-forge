# BM25Engine

Stateless C# External Logic component for OutSystems Developer Cloud (ODC). Implements native
BM25 lexical search scoring: tokenization, Porter stemming, and BM25 ranking math. Has zero
external dependencies. It does not do hybrid search, reranking, or query expansion. Those live
above this component, not inside it.

## ODC surface

`IBM25EngineActions` is the `[OSInterface]`-decorated contract the External Logic SDK requires.
This is what ODC actually discovers on upload. `BM25EngineActions` implements it and delegates to
`BM25EngineLibrary`, a plain static class with the same three methods that's directly
unit-testable without any SDK wiring. The four model types, `TermFrequency`, `ChunkIndexResult`,
`PostingCandidate`, `BM25ScoreResult`, are `[OSStructure]`-decorated `struct`s. The SDK requires
`OSStructure` on structs, not classes.

## Public entry points (ODC Server Actions)

### `TokenizeText(string text) -> List<string>`
Lowercases, strips punctuation, removes stop words, and Porter-stems the input.

**MUST be called with identical logic at both ingestion time and query time.** ODC guarantees
this automatically by calling this same Server Action in both the `EmbedFile` ingestion workflow
and the query-parsing path. Never fork or reimplement this logic elsewhere. If ingestion and
query tokenization ever diverge, terms silently stop matching and BM25 recall degrades with no
visible error.

### `IndexChunk(string chunkText) -> ChunkIndexResult`
Tokenizes a chunk and returns its token count plus per-term frequencies, used to build
`BM25Posting` and `BM25Term` rows. Call once per chunk during ingestion or backfill.

### `ScoreQuery(queryTerms, postings, totalChunks, avgChunkLength, k1, b, topK?) -> List<BM25ScoreResult>`
Computes BM25 scores for a candidate set ODC already fetched from `BM25Posting`/`BM25Term`, using
corpus stats from `BM25Stats` and tuning parameters from Site Properties (K1, B). Returns results
sorted descending by score. `topK` is optional; `null` returns every scored chunk.

## Tokenizer versioning

**v2 (current):** stripped punctuation is replaced with a whitespace boundary, so it always
splits adjacent alphanumeric runs into separate tokens. Hyphens, colons, periods, and other
punctuation are all treated identically. `ERR-BM25-003` tokenizes to `err`, `bm25`, `003`
instead of fusing into one token.

**v1 (deprecated):** stripped punctuation was deleted instead of replaced, so tokens with no
surrounding whitespace in the source text could fuse across word boundaries. For example,
`retry.ERR-BM25-003` (no space before the code) tokenized to `retryerrbm25003`, a token that
could never match a clean query for `ERR-BM25-003`. This was a real, reproducible retrieval bug,
not a documentation footnote.

**Upgrading from v1 to v2 requires a full reindex of your corpus.** v1 and v2 postings for the
same source text use different token boundaries and are not compatible. Run a full backfill,
not a partial or incremental one.

Known v2 side effect: contractions now split at the apostrophe. `don't` becomes `don`, `t`.
Neither fragment matches a stop-word entry (the stop list has whole contracted forms like
`"don't"`), so short fragments pass through as low-value index noise. Accepted tradeoff, not fixed.

## Project layout

```
src/BM25Engine/            production code: the assembly that ships to ODC
tests/BM25Engine.Tests/    xUnit tests, separate project with a ProjectReference back to src
BM25Engine.slnx            solution tying the two together
```

Tests live in a separate project, not alongside production code, specifically so the shipped
`BM25Engine.dll` has zero xunit/TestPlatform references baked into its assembly manifest. This is
verified via `Assembly.GetReferencedAssemblies()` after a Release build.

## Testing

- `tests/BM25Engine.Tests/PorterStemmerTests.cs`: 162 word/stem pairs. 75 from the original 1980
  Porter paper's worked examples, spanning all five algorithm steps, plus 87 independently
  sourced from Porter's own official reference vocabulary
  (`tartarus.org/martin/PorterStemmer/voc.txt` + `output.txt`).
- `tests/BM25Engine.Tests/TokenizerTests.cs`: empty input, all-stop-word input, mixed case,
  punctuation-heavy input, realistic paragraph, v2 split-on-punctuation behavior. The deprecated
  v1 fused-token test is kept, renamed, marked `Skip`, rather than deleted, for audit history.
- `tests/BM25Engine.Tests/BM25ScorerTests.cs`: hand-calculated 3-chunk / 2-query-term example,
  verified to 1e-6 tolerance.

Run all tests: `dotnet test BM25Engine.slnx`

## Packaging for ODC upload

```
bash scripts/package.sh
```

Builds `src/BM25Engine` in Release and zips just `BM25Engine.dll` + `BM25Engine.pdb` into
`publish/BM25Engine.zip`. No `deps.json` is shipped. BM25Engine has zero third-party runtime
dependencies. `OutSystems.ExternalLibraries.SDK.dll` itself is a compile-time-only reference
that ODC supplies, excluded from the zip, matching the pattern used by sibling ODC components.

## What's next

Hybrid retrieval, reranking, and query expansion are planned as separate components that sit
above this one. BM25Engine stays a single-purpose scorer: tokenize, index, rank. Nothing more.
