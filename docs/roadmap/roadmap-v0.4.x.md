# Lexichord Archive Roadmap (v0.4.1 - v0.4.8)

In v0.3.x, we built a "Writing Coach" with algorithmic analysis. In v0.4.x, we give the system **Memory**. This phase introduces vector storage, document ingestion, and semantic search — the foundation for AI-powered features in v0.6.x+.

**Architectural Note:** This version introduces `Lexichord.Modules.RAG` (Retrieval-Augmented Generation). The module depends on `pgvector` for PostgreSQL and external embedding APIs. **License Gating:** Basic file watching is Core, but vector search and embedding require **WriterPro** tier.

**Total Sub-Parts:** 32 distinct implementation steps.

---

## v0.4.1: The Vector Foundation (pgvector Setup)
**Goal:** Extend the PostgreSQL database to support high-dimensional vector storage for semantic search.

*   **v0.4.1a:** **pgvector Extension.** Update Docker Compose to use a PostgreSQL image with `pgvector` pre-installed (`ankane/pgvector` or official with extension). Verify extension loads on container startup.
    *   *Verification:* `SELECT * FROM pg_extension WHERE extname = 'vector';` returns a row.
*   **v0.4.1b:** **Schema Migration.** Create `Migration_003_VectorSchema.cs` via FluentMigrator. Define tables:
    *   `documents` (id UUID, file_path TEXT UNIQUE, file_hash TEXT, title TEXT, indexed_at TIMESTAMP, chunk_count INT)
    *   `chunks` (id UUID, document_id UUID FK, content TEXT, chunk_index INT, embedding VECTOR(1536), metadata JSONB)
    *   Create HNSW index on `chunks.embedding` for approximate nearest neighbor search.
*   **v0.4.1c:** **Repository Abstractions.** Define `IDocumentRepository` and `IChunkRepository` interfaces in Abstractions:
    ```csharp
    public interface IDocumentRepository
    {
        Task<Document?> GetByPathAsync(string filePath);
        Task<Document> UpsertAsync(Document document);
        Task<bool> DeleteAsync(Guid id);
        Task<IReadOnlyList<Document>> GetAllAsync();
    }
    ```
*   **v0.4.1d:** **Dapper Implementation.** Implement `DocumentRepository` and `ChunkRepository` using Dapper. Handle `pgvector` type mapping with custom type handler for `Vector` to `float[]` conversion.

---

## v0.4.2: The Watcher (File Ingestion Pipeline)
**Goal:** Automatically detect new and modified files in the workspace for indexing.

*   **v0.4.2a:** **Ingestion Service Interface.** Define `IIngestionService` in Abstractions:
    ```csharp
    public interface IIngestionService
    {
        Task<IngestionResult> IngestFileAsync(string filePath, CancellationToken ct = default);
        Task<IngestionResult> IngestDirectoryAsync(string directoryPath, bool recursive, CancellationToken ct = default);
        Task<bool> RemoveDocumentAsync(string filePath);
        event EventHandler<IngestionProgressEventArgs> ProgressChanged;
    }
    ```
*   **v0.4.2b:** **Hash-Based Change Detection.** Implement `FileHashService` using SHA-256 to detect file modifications. Compare stored `file_hash` in `documents` table against current file hash to skip unchanged files.
    *   *Optimization:* Store file size and last modified timestamp for quick pre-filtering before computing hash.
*   **v0.4.2c:** **File Watcher Integration.** Extend `IRobustFileSystemWatcher` (from v0.1.2b) to publish `FileIndexingRequestedEvent` when `.md`, `.txt`, `.json`, or `.yaml` files are created/modified in the workspace.
*   **v0.4.2d:** **Ingestion Queue.** Implement `IngestionQueue` using `System.Threading.Channels`. Files are enqueued by the watcher and processed sequentially by a background `HostedService` to prevent overwhelming the embedding API.
    *   *Configuration:* `MaxConcurrentIngestions` (default: 2), `ThrottleDelayMs` (default: 500).

---

## v0.4.3: The Splitter (Chunking Strategies)
**Goal:** Break documents into semantically meaningful chunks suitable for embedding.

*   **v0.4.3a:** **Chunking Abstractions.** Define `IChunkingStrategy` interface and `ChunkingMode` enum in Abstractions:
    ```csharp
    public enum ChunkingMode { FixedSize, Paragraph, MarkdownHeader, Semantic }

    public interface IChunkingStrategy
    {
        ChunkingMode Mode { get; }
        IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options);
    }

    public record TextChunk(string Content, int StartOffset, int EndOffset, ChunkMetadata Metadata);
    public record ChunkMetadata(int Index, string? Heading, int Level);
    ```
*   **v0.4.3b:** **Fixed-Size Chunker.** Implement `FixedSizeChunkingStrategy`:
    *   Split by character count (default: 1000 chars).
    *   Overlap support (default: 100 chars) to preserve context across chunk boundaries.
    *   Respect word boundaries (don't split mid-word).
*   **v0.4.3c:** **Paragraph Chunker.** Implement `ParagraphChunkingStrategy`:
    *   Split on double-newline (`\n\n`).
    *   Merge short paragraphs (<200 chars) with the next paragraph.
    *   Split long paragraphs (>2000 chars) using fixed-size fallback.
*   **v0.4.3d:** **Markdown Header Chunker.** Implement `MarkdownHeaderChunkingStrategy`:
    *   Parse Markdown headers (`#`, `##`, `###`) to create hierarchical chunks.
    *   Each chunk includes content under a heading until the next heading of same or higher level.
    *   Store header text in `ChunkMetadata.Heading` for context injection during retrieval.
    *   *Dependency:* Use `Markdig` (already in v0.1.3b) for parsing.

---

## v0.4.4: The Embedder (Vector Generation)
**Goal:** Convert text chunks into high-dimensional vectors using external embedding APIs.

*   **v0.4.4a:** **Embedding Abstractions.** Define `IEmbeddingService` interface in Abstractions:
    ```csharp
    public interface IEmbeddingService
    {
        string ModelName { get; }
        int Dimensions { get; }
        Task<float[]> EmbedAsync(string text, CancellationToken ct = default);
        Task<IReadOnlyList<float[]>> EmbedBatchAsync(IReadOnlyList<string> texts, CancellationToken ct = default);
    }

    public record EmbeddingOptions(string Model, int MaxTokens, bool Normalize);
    ```
*   **v0.4.4b:** **OpenAI Connector.** Implement `OpenAIEmbeddingService`:
    *   Use `text-embedding-3-small` model (1536 dimensions, cost-effective).
    *   Integrate with `ISecureVault` (v0.0.6a) to retrieve API key.
    *   Implement retry logic with Polly (exponential backoff on 429 rate limits).
    *   Batch requests (max 100 texts per request per OpenAI limits).
*   **v0.4.4c:** **Token Counting.** Implement `ITokenCounter` using `Microsoft.ML.Tokenizers` or `tiktoken` port:
    *   Pre-check chunk size before embedding to avoid API errors.
    *   Truncate chunks exceeding model's max tokens (8191 for `text-embedding-3-small`).
    *   Log warning when truncation occurs.
*   **v0.4.4d:** **Embedding Pipeline.** Wire chunking and embedding together in `DocumentIndexingPipeline`:
    ```csharp
    public class DocumentIndexingPipeline(
        IChunkingStrategy chunker,
        IEmbeddingService embedder,
        IChunkRepository chunkRepo,
        IDocumentRepository docRepo)
    {
        public async Task IndexDocumentAsync(string filePath, string content);
    }
    ```
    *   Publish `DocumentIndexedEvent` on completion via MediatR.

---

## v0.4.5: The Searcher (Semantic Query)
**Goal:** Enable natural language queries against the indexed document corpus.

*   **v0.4.5a:** **Search Abstractions.** Define `ISemanticSearchService` in Abstractions:
    ```csharp
    public interface ISemanticSearchService
    {
        Task<SearchResult> SearchAsync(string query, SearchOptions options, CancellationToken ct = default);
    }

    public record SearchOptions(int TopK = 10, float MinScore = 0.7f, Guid? DocumentFilter = null);
    public record SearchResult(IReadOnlyList<SearchHit> Hits, TimeSpan Duration);
    public record SearchHit(TextChunk Chunk, Document Document, float Score, float[] QueryEmbedding);
    ```
*   **v0.4.5b:** **Vector Search Query.** Implement `PgVectorSearchService`:
    *   Use cosine similarity: `SELECT *, 1 - (embedding <=> @query) AS score FROM chunks ORDER BY embedding <=> @query LIMIT @topK`.
    *   Filter by `document_id` if `DocumentFilter` is set.
    *   Return results above `MinScore` threshold.
*   **v0.4.5c:** **Query Preprocessing.** Implement `QueryPreprocessor`:
    *   Trim whitespace, normalize Unicode.
    *   Optionally expand abbreviations (e.g., "API" → "Application Programming Interface") using a lookup table.
    *   Cache query embeddings for repeated searches (short-lived cache, 5 minutes).
*   **v0.4.5d:** **License Gating.** Wrap semantic search in license check:
    ```csharp
    if (licenseContext.Tier < LicenseTier.WriterPro)
    {
        throw new FeatureNotLicensedException("Semantic Search", LicenseTier.WriterPro);
    }
    ```
    *   Core tier users see "Upgrade to WriterPro" prompt when attempting search.

---

## v0.4.6: The Reference Panel (Search UI)
**Goal:** User-facing interface for semantic search integrated into the workspace.

*   **v0.4.6a:** **Reference Panel View.** Create `ReferenceView.axaml` with:
    *   Search input (TextBox with placeholder "Ask your documents...").
    *   Results list (ItemsControl with virtualization).
    *   Loading indicator during search.
    *   Register in `ShellRegion.Right` via `IRegionManager` (v0.1.1b).
*   **v0.4.6b:** **Search Result Item.** Create `SearchResultItemView` UserControl:
    *   Display chunk preview (first 200 chars with query terms highlighted).
    *   Show source document name and relevance score (e.g., "87% match").
    *   Show chunk heading if available from metadata.
*   **v0.4.6c:** **Source Navigation.** Implement "Click to Open" functionality:
    *   Double-click result opens source document in Editor (via `IEditorService` from v0.1.3a).
    *   Scroll to chunk's `StartOffset` and highlight the text span.
    *   Publish `ReferenceNavigatedEvent` for telemetry.
*   **v0.4.6d:** **Search History.** Store recent queries in memory (last 10) with dropdown for quick re-execution. Persist to user settings if enabled.

---

## v0.4.7: The Index Manager (Corpus Administration)
**Goal:** Allow users to view and manage indexed documents.

*   **v0.4.7a:** **Index Status View.** Create `IndexStatusView.axaml` in Settings dialog:
    *   List all indexed documents with status (Indexed, Pending, Failed, Stale).
    *   Show total chunk count and estimated storage size.
    *   Display last indexing timestamp per document.
*   **v0.4.7b:** **Manual Indexing Controls.** Add UI actions:
    *   "Re-index Document" button to force re-processing.
    *   "Remove from Index" to delete document and its chunks.
    *   "Re-index All" for full corpus rebuild.
    *   *Safety:* Confirm dialog for destructive operations.
*   **v0.4.7c:** **Indexing Progress.** Create `IndexingProgressView` (toast/overlay):
    *   Show current file being processed.
    *   Display progress bar for batch operations.
    *   Bind to `IngestionService.ProgressChanged` event.
    *   Allow cancellation of long-running operations.
*   **v0.4.7d:** **Indexing Errors.** Implement error handling and reporting:
    *   Store failed ingestion attempts with error message in `documents.error_message`.
    *   Display failures in Index Status View with "Retry" action.
    *   Log errors with full stack trace at Warning level.

---

## v0.4.8: The Hardening (Performance & Testing)
**Goal:** Ensure the RAG system is production-ready with proper tests and optimization.

*   **v0.4.8a:** **Unit Test Suite.** Create comprehensive tests:
    *   `ChunkingStrategyTests`: Verify each strategy produces expected chunk count and boundaries.
    *   `EmbeddingServiceTests`: Mock HTTP responses, verify batch handling and error recovery.
    *   `SearchServiceTests`: Test score calculation, filtering, and result ordering.
*   **v0.4.8b:** **Integration Tests.** Test end-to-end flows:
    *   Ingest a test document → Verify chunks in database → Search and retrieve.
    *   Test change detection (modify file → re-index only changed content).
    *   Test deletion cascade (remove document → chunks deleted).
*   **v0.4.8c:** **Performance Benchmarks.** Establish baselines:
    *   Chunking: <100ms for 100KB document.
    *   Search latency: <200ms for query against 10,000 chunks.
    *   Indexing throughput: >10 documents/minute (limited by embedding API).
    *   Memory usage: <50MB for 1,000 indexed documents metadata.
*   **v0.4.8d:** **Embedding Cache.** Implement optional local embedding cache:
    *   Store embeddings in SQLite file to avoid re-embedding unchanged chunks.
    *   Key by content hash to handle identical text across documents.
    *   Configurable cache size limit with LRU eviction.
    *   *Configuration:* `EmbeddingCacheEnabled`, `EmbeddingCacheMaxSizeMB`.

---

## Dependencies on Prior Versions

| Component | Source Version | Usage in v0.4.x |
|:----------|:---------------|:----------------|
| `IDbConnectionFactory` | v0.0.5b | PostgreSQL connection for pgvector queries |
| `FluentMigrator` | v0.0.5c | Schema migrations for vector tables |
| `ISecureVault` | v0.0.6a | Store OpenAI API key securely |
| `IMediator` | v0.0.7a | Publish indexing/search events |
| `IRobustFileSystemWatcher` | v0.1.2b | Detect file changes for auto-indexing |
| `IWorkspaceService` | v0.1.2a | Get current workspace root for ingestion |
| `IEditorService` | v0.1.3a | Open documents from search results |
| `IRegionManager` | v0.1.1b | Register Reference Panel in Right region |
| `ISettingsPage` | v0.1.6a | Index Status in Settings dialog |
| `ILicenseContext` | v0.0.4c | Gate WriterPro features |
| `Markdig` | v0.1.3b | Parse Markdown for header-based chunking |
| `Polly` | v0.0.5d | Retry policies for embedding API |

---

## MediatR Events Introduced

| Event | Description |
|:------|:------------|
| `FileIndexingRequestedEvent` | File watcher requests indexing of new/modified file |
| `DocumentIndexingStartedEvent` | Document ingestion pipeline begins |
| `DocumentIndexedEvent` | Document successfully indexed with chunk count |
| `DocumentIndexingFailedEvent` | Indexing failed with error details |
| `DocumentRemovedFromIndexEvent` | Document and chunks deleted from index |
| `SemanticSearchExecutedEvent` | Search query completed with result count and duration |
| `ReferenceNavigatedEvent` | User navigated to search result in editor |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Npgsql.Pgvector` | 0.2.x | pgvector type mapping for Npgsql |
| `Microsoft.ML.Tokenizers` | 0.22.x | Token counting for embedding truncation |
| `System.Threading.Channels` | 9.0.x | Bounded ingestion queue |

---

## License Gating Summary

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| File watching / change detection | ✓ | ✓ | ✓ | ✓ |
| Manual document indexing | — | ✓ | ✓ | ✓ |
| Semantic search | — | ✓ | ✓ | ✓ |
| Batch indexing | — | ✓ | ✓ | ✓ |
| Embedding cache | — | — | ✓ | ✓ |
| Custom embedding models | — | — | — | ✓ |

---

## Implementation Guide: Sample Workflow for v0.4.4b (OpenAI Connector)

**LCS-01 (Design Composition)**
*   **Interface:** `IEmbeddingService` with `EmbedAsync` and `EmbedBatchAsync`.
*   **Logic:** Use `HttpClient` to POST to `https://api.openai.com/v1/embeddings`.
*   **Authentication:** Retrieve API key from `ISecureVault.GetSecretAsync("openai:api-key")`.

**LCS-02 (Rehearsal Strategy)**
*   **Test (Unit):** Mock `HttpClient` to return canned embedding response. Assert `float[]` has 1536 dimensions.
*   **Test (Integration):** With real API key, embed "Hello world" and verify non-zero vector returned.
*   **Test (Error):** Mock 429 response. Assert Polly retries 3 times with backoff.

**LCS-03 (Performance Log)**
1.  **Implement `OpenAIEmbeddingService`:**
    ```csharp
    public class OpenAIEmbeddingService(
        IHttpClientFactory httpFactory,
        ISecureVault vault,
        ILogger<OpenAIEmbeddingService> logger) : IEmbeddingService
    {
        public string ModelName => "text-embedding-3-small";
        public int Dimensions => 1536;

        public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
        {
            var apiKey = await vault.GetSecretAsync("openai:api-key")
                ?? throw new InvalidOperationException("OpenAI API key not configured");

            var request = new { model = ModelName, input = text };
            // ... HTTP POST with retry policy
        }
    }
    ```
2.  **Polly Policy:**
    ```csharp
    var retryPolicy = Policy
        .Handle<HttpRequestException>()
        .OrResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.TooManyRequests)
        .WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    ```
3.  **Register:** Add to DI in `RAGModule.RegisterServices()`.
