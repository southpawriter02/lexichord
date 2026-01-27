# Lexichord Retrieval Roadmap (v0.5.1 - v0.5.8)

In v0.4.x, we built "The Archive" — vector storage, document ingestion, and basic semantic search. In v0.5.x, we make that knowledge **accessible and actionable**. This phase introduces advanced retrieval features: hybrid search (BM25 + semantic), source citations, contextual relevance, and the foundation for AI-powered document assistance.

**Architectural Note:** This version extends `Lexichord.Modules.RAG` with sophisticated retrieval capabilities. The module bridges the gap between raw vector search and meaningful, citation-backed answers.

**Total Sub-Parts:** 32 distinct implementation steps.

---

## v0.5.1: The Hybrid Engine (BM25 + Semantic)
**Goal:** Combine keyword-based (BM25) and semantic (vector) search for superior retrieval quality.

*   **v0.5.1a:** **BM25 Index Schema.** Extend `chunks` table with full-text search support:
    *   Add `content_tsvector TSVECTOR` column with GIN index.
    *   Create trigger to auto-update tsvector on INSERT/UPDATE.
    *   Migration: `Migration_004_FullTextSearch.cs`.
*   **v0.5.1b:** **BM25 Search Implementation.** Implement `IBM25SearchService`:
    ```csharp
    public interface IBM25SearchService
    {
        Task<IReadOnlyList<BM25Hit>> SearchAsync(string query, int topK, CancellationToken ct = default);
    }

    public record BM25Hit(Guid ChunkId, float Score, IReadOnlyList<string> MatchedTerms);
    ```
    *   Use PostgreSQL `ts_rank_cd()` for scoring.
    *   Support phrase queries with `plainto_tsquery()` and `phraseto_tsquery()`.
*   **v0.5.1c:** **Hybrid Fusion Algorithm.** Implement `HybridSearchService` that combines results:
    *   Execute BM25 and semantic search in parallel (`Task.WhenAll`).
    *   Apply Reciprocal Rank Fusion (RRF) to merge ranked lists:
      ```
      RRF_score = Σ 1/(k + rank_i) for each ranking system
      ```
    *   Configurable weights: `SemanticWeight` (default 0.7), `BM25Weight` (default 0.3).
*   **v0.5.1d:** **Search Mode Toggle.** Add `SearchMode` enum and UI toggle:
    ```csharp
    public enum SearchMode { Semantic, Keyword, Hybrid }
    ```
    *   Default to `Hybrid` for WriterPro users.
    *   Core tier limited to basic semantic search (from v0.4.5).

---

## v0.5.2: The Citation Engine (Source Attribution)
**Goal:** Every retrieved chunk must be traceable back to its source with precise location.

*   **v0.5.2a:** **Citation Model.** Define citation data structures in Abstractions:
    ```csharp
    public record Citation(
        Guid ChunkId,
        string DocumentPath,
        string DocumentTitle,
        int StartOffset,
        int EndOffset,
        string? Heading,
        int? LineNumber,
        DateTime IndexedAt
    );

    public interface ICitationService
    {
        Citation CreateCitation(SearchHit hit);
        string FormatCitation(Citation citation, CitationStyle style);
        Task<bool> ValidateCitationAsync(Citation citation);
    }
    ```
*   **v0.5.2b:** **Citation Styles.** Implement multiple formatting options:
    *   `Inline`: "According to [auth-guide.md, §Authentication]..."
    *   `Footnote`: Numbered references with full path at document end.
    *   `Markdown`: `[^1]` style with hover preview support.
    *   User preference stored via `ISettingsService` (v0.1.6a).
*   **v0.5.2c:** **Stale Citation Detection.** Implement validation logic:
    *   Compare `Citation.IndexedAt` with file's current modified timestamp.
    *   If file changed, mark citation as "Potentially Stale" with warning icon.
    *   Offer "Re-verify" action to re-index and update citation.
*   **v0.5.2d:** **Citation Copy Actions.** Add clipboard operations:
    *   "Copy as Markdown Link" → `[Title](file:///path#L42)`
    *   "Copy as Citation" → Formatted per active style.
    *   "Copy Chunk Text" → Raw content for pasting.

---

## v0.5.3: The Context Window (Relevance Expansion)
**Goal:** Provide surrounding context for retrieved chunks to improve comprehension.

*   **v0.5.3a:** **Context Expansion Service.** Implement `IContextExpansionService`:
    ```csharp
    public interface IContextExpansionService
    {
        Task<ExpandedChunk> ExpandAsync(TextChunk chunk, ContextOptions options);
    }

    public record ContextOptions(int PrecedingChunks = 1, int FollowingChunks = 1, bool IncludeHeadings = true);
    public record ExpandedChunk(TextChunk Core, IReadOnlyList<TextChunk> Before, IReadOnlyList<TextChunk> After, string? ParentHeading);
    ```
*   **v0.5.3b:** **Sibling Chunk Retrieval.** Query adjacent chunks by `chunk_index`:
    ```sql
    SELECT * FROM chunks
    WHERE document_id = @docId
    AND chunk_index BETWEEN @index - @before AND @index + @after
    ORDER BY chunk_index;
    ```
    *   Cache expanded results to avoid repeated DB hits for same chunk.
*   **v0.5.3c:** **Heading Hierarchy.** Retrieve parent heading context:
    *   Store `parent_heading_id` in chunk metadata during indexing (v0.4.3d).
    *   Display breadcrumb: "Authentication > OAuth > Token Refresh".
    *   Helps user understand where the chunk fits in document structure.
*   **v0.5.3d:** **Context Preview UI.** Update `SearchResultItemView` (v0.4.6b):
    *   Add "Show More Context" expander below chunk preview.
    *   Display preceding/following chunks with visual separator.
    *   Fade effect at boundaries to indicate continuation.

---

## v0.5.4: The Relevance Tuner (Query Understanding)
**Goal:** Improve search quality through query analysis and enhancement.

*   **v0.5.4a:** **Query Analyzer.** Implement `IQueryAnalyzer`:
    ```csharp
    public interface IQueryAnalyzer
    {
        QueryAnalysis Analyze(string query);
    }

    public record QueryAnalysis(
        string OriginalQuery,
        IReadOnlyList<string> Keywords,
        IReadOnlyList<string> Entities,
        QueryIntent Intent,
        float Specificity
    );

    public enum QueryIntent { Factual, Procedural, Conceptual, Navigational }
    ```
*   **v0.5.4b:** **Query Expansion.** Implement synonym and related term expansion:
    *   Use terminology database from v0.2.2 for domain-specific synonyms.
    *   Example: "auth" → ["authentication", "authorization", "login"].
    *   Configurable expansion limit (default: 3 synonyms per term).
*   **v0.5.4c:** **Query Suggestions.** Implement autocomplete from indexed content:
    *   Extract common n-grams from chunk content during indexing.
    *   Store in `query_suggestions` table with frequency count.
    *   Show dropdown as user types (debounced, 200ms).
*   **v0.5.4d:** **Query History & Analytics.** Track search patterns:
    *   Store queries with timestamps and result counts.
    *   Identify "zero result" queries for content gap analysis.
    *   Publish `QueryAnalyticsEvent` for optional telemetry (opt-in).

---

## v0.5.5: The Filter System (Scoped Search)
**Goal:** Allow users to narrow search to specific documents, folders, or metadata.

*   **v0.5.5a:** **Filter Model.** Define filter data structures:
    ```csharp
    public record SearchFilter(
        IReadOnlyList<string>? PathPatterns,      // Glob patterns: "docs/**/*.md"
        IReadOnlyList<string>? FileExtensions,    // [".md", ".txt"]
        DateRange? ModifiedRange,                  // Files modified within range
        IReadOnlyList<string>? Tags,              // Document tags (future)
        bool? HasHeadings                          // Only chunks with heading context
    );

    public record DateRange(DateTime? Start, DateTime? End);
    ```
*   **v0.5.5b:** **Filter UI Component.** Create `SearchFilterPanel.axaml`:
    *   Folder tree with checkboxes for path selection.
    *   Extension toggles (Markdown, Text, JSON, YAML).
    *   Date range picker for "Modified in last X days".
    *   Collapsible panel below search input.
*   **v0.5.5c:** **Filter Query Builder.** Translate filters to SQL WHERE clauses:
    ```csharp
    public class FilterQueryBuilder
    {
        public (string Sql, DynamicParameters Params) Build(SearchFilter filter);
    }
    ```
    *   Combine with vector search using subquery or CTE.
    *   Ensure HNSW index is still utilized (avoid full table scan).
*   **v0.5.5d:** **Saved Filters.** Allow users to save and name filter presets:
    *   Store in user settings as JSON.
    *   Quick access dropdown: "My Filters: [API Docs] [Release Notes] [All]".
    *   Publish `FilterAppliedEvent` when filter changes.

---

## v0.5.6: The Answer Preview (Snippet Generation)
**Goal:** Generate highlighted, contextual snippets that directly answer the query.

*   **v0.5.6a:** **Snippet Extraction.** Implement `ISnippetService`:
    ```csharp
    public interface ISnippetService
    {
        Snippet ExtractSnippet(TextChunk chunk, string query, SnippetOptions options);
    }

    public record Snippet(
        string Text,
        IReadOnlyList<HighlightSpan> Highlights,
        int StartOffset,
        bool IsTruncated
    );

    public record HighlightSpan(int Start, int Length, HighlightType Type);
    public enum HighlightType { QueryMatch, KeyPhrase, Entity }
    ```
*   **v0.5.6b:** **Query Term Highlighting.** Highlight matching terms in snippets:
    *   Exact matches: bold styling.
    *   Fuzzy matches (from BM25 stemming): italic styling.
    *   Use `FlowDocument` or custom `TextBlock` with inline formatting.
*   **v0.5.6c:** **Smart Truncation.** Implement intelligent snippet boundaries:
    *   Center snippet around highest-density query term region.
    *   Respect sentence boundaries (don't cut mid-sentence).
    *   Show "..." at truncation points.
    *   Configurable length: 150-300 characters (user setting).
*   **v0.5.6d:** **Multi-Snippet Results.** When chunk contains multiple relevant regions:
    *   Extract up to 3 snippets per chunk.
    *   Display as expandable list in search results.
    *   "Show all matches in this section" action.

---

## v0.5.7: The Reference Dock (Enhanced UI)
**Goal:** Polish the Reference Panel into a professional, feature-complete search experience.

*   **v0.5.7a:** **Panel Redesign.** Update `ReferenceView.axaml` (from v0.4.6a):
    *   Unified search bar with mode toggle (Semantic/Keyword/Hybrid).
    *   Inline filter chips showing active filters.
    *   Result count and search duration display.
    *   Keyboard navigation (↑↓ to select, Enter to open, Esc to clear).
*   **v0.5.7b:** **Result Grouping.** Group results by document:
    *   Collapsible document headers with match count.
    *   "Expand All" / "Collapse All" toggle.
    *   Option to sort by relevance (default) or by document path.
*   **v0.5.7c:** **Preview Pane.** Add split view option:
    *   Right side shows full chunk content with surrounding context.
    *   Updates as user navigates result list.
    *   Toggle between "Preview" and "Full Width Results" modes.
*   **v0.5.7d:** **Search Actions.** Add action buttons to search bar:
    *   "Copy All Results" → Markdown-formatted list.
    *   "Export Results" → JSON/CSV file for external use.
    *   "Open All in Editor" → Opens all source documents as tabs.

---

## v0.5.8: The Hardening (Quality & Performance)
**Goal:** Ensure retrieval system is production-ready with comprehensive testing and optimization.

*   **v0.5.8a:** **Retrieval Quality Tests.** Create test corpus and benchmarks:
    *   Curated test set: 50 queries with "gold standard" expected results.
    *   Metrics: Precision@K, Recall@K, Mean Reciprocal Rank (MRR).
    *   Automated regression testing in CI pipeline.
*   **v0.5.8b:** **Search Performance Tests.** Establish baselines:
    *   Hybrid search: <300ms for 50K chunks.
    *   BM25 alone: <100ms for 50K chunks.
    *   Filter application: <50ms additional overhead.
    *   Query suggestion: <50ms for autocomplete.
*   **v0.5.8c:** **Caching Strategy.** Implement multi-layer caching:
    *   Query result cache: LRU, 100 entries, 5-minute TTL.
    *   Embedding cache: Shared with v0.4.8d.
    *   Context expansion cache: Per-session, cleared on document change.
    *   Cache invalidation on index updates.
*   **v0.5.8d:** **Error Resilience.** Implement graceful degradation:
    *   If embedding API fails, fall back to BM25-only search.
    *   If database unavailable, show cached results with "Offline" indicator.
    *   Retry failed searches with exponential backoff.
    *   User-friendly error messages with "Try Again" action.

---

## Dependencies on Prior Versions

| Component | Source Version | Usage in v0.5.x |
|:----------|:---------------|:----------------|
| `ISemanticSearchService` | v0.4.5a | Base semantic search for hybrid fusion |
| `IChunkRepository` | v0.4.1c | Chunk retrieval and context expansion |
| `IDocumentRepository` | v0.4.1c | Document metadata for citations |
| `SearchHit` / `SearchResult` | v0.4.5a | Search result models extended |
| `ReferenceView` | v0.4.6a | UI foundation for enhanced panel |
| `ITerminologyRepository` | v0.2.2b | Synonym expansion from Lexicon |
| `ISettingsService` | v0.1.6a | User preferences for citation style, snippet length |
| `IEditorService` | v0.1.3a | Open documents from search results |
| `IRegionManager` | v0.1.1b | Panel registration |
| `ILicenseContext` | v0.0.4c | Feature gating (Hybrid search = WriterPro) |
| `IMediator` | v0.0.7a | Event publishing |
| `Polly` | v0.0.5d | Retry policies for search resilience |

---

## MediatR Events Introduced

| Event | Description |
|:------|:------------|
| `HybridSearchExecutedEvent` | Hybrid search completed with fusion details |
| `CitationCreatedEvent` | Citation generated from search result |
| `CitationValidationFailedEvent` | Source file changed since indexing |
| `QueryExpandedEvent` | Query enhanced with synonyms |
| `QuerySuggestionSelectedEvent` | User selected autocomplete suggestion |
| `FilterAppliedEvent` | Search filter configuration changed |
| `SearchResultsExportedEvent` | Results exported to file |
| `QueryAnalyticsEvent` | Search analytics for telemetry (opt-in) |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Npgsql.FullTextSearch` | (built-in) | PostgreSQL tsvector support |
| (No new packages) | — | v0.5.x builds on existing dependencies |

---

## License Gating Summary

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| Basic semantic search | ✓ | ✓ | ✓ | ✓ |
| BM25 keyword search | ✓ | ✓ | ✓ | ✓ |
| Hybrid search (fusion) | — | ✓ | ✓ | ✓ |
| Query expansion (synonyms) | — | ✓ | ✓ | ✓ |
| Saved filter presets | — | ✓ | ✓ | ✓ |
| Search analytics | — | — | ✓ | ✓ |
| Export results | — | ✓ | ✓ | ✓ |
| Custom citation styles | — | — | ✓ | ✓ |

---

## Implementation Guide: Sample Workflow for v0.5.1c (Hybrid Fusion)

**LCS-01 (Design Composition)**
*   **Interface:** `IHybridSearchService` combining `ISemanticSearchService` and `IBM25SearchService`.
*   **Algorithm:** Reciprocal Rank Fusion (RRF) for merging ranked lists.
*   **Configuration:** Tunable weights for semantic vs. keyword relevance.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Query "authentication flow" against corpus with auth docs.
    *   Assert: Results include both exact "authentication" matches (BM25) and conceptually similar "login process" (semantic).
*   **Test:** Query with typo "authentcation".
    *   Assert: BM25 may miss, but semantic still retrieves relevant docs.
*   **Test:** Verify RRF score calculation matches expected formula.

**LCS-03 (Performance Log)**
1.  **Implement `HybridSearchService`:**
    ```csharp
    public class HybridSearchService(
        ISemanticSearchService semanticSearch,
        IBM25SearchService bm25Search,
        IOptions<HybridSearchOptions> options,
        ILogger<HybridSearchService> logger) : IHybridSearchService
    {
        public async Task<SearchResult> SearchAsync(string query, SearchOptions opts, CancellationToken ct)
        {
            // Execute both searches in parallel
            var semanticTask = semanticSearch.SearchAsync(query, opts with { TopK = opts.TopK * 2 }, ct);
            var bm25Task = bm25Search.SearchAsync(query, opts.TopK * 2, ct);

            await Task.WhenAll(semanticTask, bm25Task);

            // Apply Reciprocal Rank Fusion
            var fused = ApplyRRF(semanticTask.Result.Hits, bm25Task.Result, options.Value);

            return new SearchResult(fused.Take(opts.TopK).ToList(), /* duration */);
        }

        private IEnumerable<SearchHit> ApplyRRF(
            IReadOnlyList<SearchHit> semanticHits,
            IReadOnlyList<BM25Hit> bm25Hits,
            HybridSearchOptions opts)
        {
            const int k = 60; // RRF constant
            var scores = new Dictionary<Guid, float>();

            // Score from semantic ranking
            for (int i = 0; i < semanticHits.Count; i++)
            {
                var chunkId = semanticHits[i].Chunk.Id;
                scores[chunkId] = opts.SemanticWeight / (k + i + 1);
            }

            // Add score from BM25 ranking
            for (int i = 0; i < bm25Hits.Count; i++)
            {
                var chunkId = bm25Hits[i].ChunkId;
                var rrfScore = opts.BM25Weight / (k + i + 1);
                scores[chunkId] = scores.GetValueOrDefault(chunkId) + rrfScore;
            }

            // Return merged results sorted by fused score
            // ...
        }
    }
    ```
2.  **Configuration:**
    ```csharp
    public record HybridSearchOptions
    {
        public float SemanticWeight { get; init; } = 0.7f;
        public float BM25Weight { get; init; } = 0.3f;
    }
    ```
3.  **Register:** Add to DI in `RAGModule.RegisterServices()`.

---

## Implementation Guide: Sample Workflow for v0.5.2a (Citation Model)

**LCS-01 (Design Composition)**
*   **Record:** `Citation` containing all provenance information.
*   **Service:** `ICitationService` for creation, formatting, and validation.
*   **Persistence:** Citations stored transiently (not in DB) unless user saves document.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Create citation from search hit, verify all fields populated.
*   **Test:** Format citation in each style, verify output format.
*   **Test:** Modify source file, validate citation returns `false`.

**LCS-03 (Performance Log)**
1.  **Implement `CitationService`:**
    ```csharp
    public class CitationService(
        IDocumentRepository docRepo,
        IWorkspaceService workspace,
        ILogger<CitationService> logger) : ICitationService
    {
        public Citation CreateCitation(SearchHit hit)
        {
            return new Citation(
                ChunkId: hit.Chunk.Id,
                DocumentPath: hit.Document.FilePath,
                DocumentTitle: hit.Document.Title ?? Path.GetFileName(hit.Document.FilePath),
                StartOffset: hit.Chunk.StartOffset,
                EndOffset: hit.Chunk.EndOffset,
                Heading: hit.Chunk.Metadata?.Heading,
                LineNumber: CalculateLineNumber(hit),
                IndexedAt: hit.Document.IndexedAt
            );
        }

        public string FormatCitation(Citation citation, CitationStyle style) => style switch
        {
            CitationStyle.Inline => $"[{citation.DocumentTitle}, §{citation.Heading ?? "Content"}]",
            CitationStyle.Footnote => $"[^{citation.ChunkId:N8}]: {citation.DocumentPath}:{citation.LineNumber}",
            CitationStyle.Markdown => $"[{citation.DocumentTitle}]({citation.DocumentPath}#L{citation.LineNumber})",
            _ => citation.DocumentPath
        };

        public async Task<bool> ValidateCitationAsync(Citation citation)
        {
            var doc = await docRepo.GetByPathAsync(citation.DocumentPath);
            if (doc is null) return false;

            var currentHash = await ComputeFileHashAsync(citation.DocumentPath);
            return doc.FileHash == currentHash;
        }
    }
    ```
2.  **Register:** Add to DI in `RAGModule.RegisterServices()`.
