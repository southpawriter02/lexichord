# Lexichord Changelog

All notable changes to the Lexichord project are documented here.

This changelog is written for stakeholders and users, focusing on **what changed** rather than technical implementation details. For detailed technical changes, see the version-specific changelogs in this directory.

---

## [v0.2.4] - 2026-01 (In Progress)

### Red Pen (Editor Integration)

This release integrates the linting engine with the editor, providing visual feedback for style violations.

#### What's New

- **Style Violation Renderer** — DocumentColorizingTransformer that identifies violations within visible lines and registers underline segments for rendering. Subscribes to violation changes and invalidates the view for real-time updates.

- **Wavy Underline Renderer** — AvaloniaEdit IBackgroundRenderer that draws wavy underlines using quadratic bezier curves. Supports severity-based colors (red for errors, yellow for warnings, blue for info, gray for hints).

- **Theme-Aware Underline Colors** — Underlines automatically adapt to light and dark themes with distinct color palettes optimized for visibility. Light theme uses high-contrast colors on white; dark theme uses softer tones that remain visible without being harsh. Includes pen caching for performance and severity-based z-ordering (errors always draw on top).

- **Violation Provider** — Thread-safe bridge between the linting pipeline and editor rendering. Maintains current violations per document with change notification events for efficient UI updates.

- **Violation Color Provider** — Maps violation severity to theme-aware underline colors following IDE conventions (matching VS Code/Roslyn color schemes). Provides background colors, tooltip borders, and Material Design severity icons.

- **Hover Tooltips** — Contextual tooltips that appear when hovering over style violations. Displays rule name, message, and optional recommendation. Supports multi-violation navigation when violations overlap, and can be triggered via keyboard shortcuts (Ctrl+K, Ctrl+I).

- **Context Menu Quick-Fixes** — Right-click on style violations or press Ctrl+. to see available fixes. Selecting a fix applies the suggested replacement with full undo support. Works with overlapping violations, showing all available fixes in the menu.

#### Sub-Part Changelogs

| Version                          | Title                        | Status      |
| -------------------------------- | ---------------------------- | ----------- |
| [v0.2.4a](v0.2.x/LCS-CL-024a.md) | Rendering Transformer        | ✅ Complete |
| [v0.2.4b](v0.2.x/LCS-CL-024b.md) | Theme-Aware Underline Colors | ✅ Complete |
| [v0.2.4c](v0.2.x/LCS-CL-024c.md) | Hover Tooltips               | ✅ Complete |
| [v0.2.4d](v0.2.x/LCS-CL-024d.md) | Context Menu Quick-Fixes     | ✅ Complete |

---

## [v0.4.1] - 2026-01 (In Progress)

### The Vector Foundation

This release introduces PostgreSQL with pgvector for vector storage, enabling semantic search capabilities.

#### What's New

- **pgvector Docker Configuration** — PostgreSQL 16 with pgvector extension for high-dimensional vector storage. Includes automated extension setup, health checks, and performance tuning for vector workloads.

- **Schema Migration** — FluentMigrator migration creating `Documents` and `Chunks` tables with pgvector embeddings. Features HNSW index for efficient similarity search, cascade delete behavior, and auto-updating timestamps.

- **Repository Abstractions** — `IDocumentRepository` and `IChunkRepository` interfaces defining the data access contracts for the RAG subsystem. Includes `Document`, `Chunk`, `ChunkSearchResult` records and `DocumentStatus` enum with comprehensive XML documentation.

#### Sub-Part Changelogs

| Version                             | Title                   | Status      |
| ----------------------------------- | ----------------------- | ----------- |
| [v0.4.1a](v0.4.x/LCS-CL-v0.4.1a.md) | pgvector Docker Config  | ✅ Complete |
| [v0.4.1b](v0.4.x/LCS-CL-v0.4.1b.md) | Schema Migration        | ✅ Complete |
| [v0.4.1c](v0.4.x/LCS-CL-v0.4.1c.md) | Repository Abstractions | ✅ Complete |
| v0.4.1d                             | Dapper Implementation   | 🔜 Planned  |

---

## [v0.4.2] - 2026-01 (In Progress)

### The Watcher (File Ingestion Pipeline)

This release establishes the file ingestion pipeline for processing documents into the RAG system.

#### What's New

- **Ingestion Service Interface** — `IIngestionService` contract defining single file and directory ingestion, document removal, and real-time progress reporting via events.

- **Ingestion Data Contracts** — `IngestionResult` record with factory methods for success/failure/skipped outcomes, `IngestionProgressEventArgs` with auto-calculated completion percentage, and `IngestionOptions` for configurable file filtering and concurrency.

- **Pipeline Phase Model** — `IngestionPhase` enum defining 7 discrete stages (Scanning → Hashing → Reading → Chunking → Embedding → Storing → Complete) enabling granular progress tracking.

- **Hash-Based Change Detection** — `IFileHashService` with SHA-256 streaming hash computation and tiered change detection (size → timestamp → hash) to minimize unnecessary re-indexing of unchanged files.

- **File Watcher Integration** — `FileWatcherIngestionHandler` bridges the workspace file watcher with the RAG pipeline, publishing `FileIndexingRequestedEvent` for files matching configured extensions. Features per-file debouncing and directory exclusion.

- **Ingestion Queue** — Priority-based queue using `System.Threading.Channels` for thread-safe file processing. Features configurable priority levels (user action → recent change → normal → background), duplicate detection with sliding window, backpressure handling, and graceful shutdown. Background service continuously processes queue items through the ingestion pipeline.

#### Sub-Part Changelogs

| Version                             | Title                       | Status      |
| ----------------------------------- | --------------------------- | ----------- |
| [v0.4.2a](v0.4.x/LCS-CL-v0.4.2a.md) | Ingestion Service Interface | ✅ Complete |
| [v0.4.2b](v0.4.x/LCS-CL-v0.4.2b.md) | Hash-Based Change Detection | ✅ Complete |
| [v0.4.2c](v0.4.x/LCS-CL-v0.4.2c.md) | File Watcher Integration    | ✅ Complete |
| [v0.4.2d](v0.4.x/LCS-CL-v0.4.2d.md) | Ingestion Queue             | ✅ Complete |

---

## [v0.4.7] - 2026-02 (In Progress)

### The Index Manager (Corpus Administration)

This release introduces index status visibility and manual control over the document corpus.

#### What's New

- **Index Status View** — Settings page displaying all indexed documents with status (Indexed, Stale, Failed, Pending), file metadata, and aggregate statistics. Supports filtering by text and status. (v0.4.7a)

- **Manual Indexing Controls** — Commands for managing the document index: Re-index Document, Remove from Index, and Re-index All. Includes confirmation dialogs for destructive operations and progress reporting for bulk operations. (v0.4.7b)

- **Indexing Progress Toast** — Real-time progress overlay for indexing operations with document name, progress bar, elapsed time, and cancellation support. Auto-dismisses after completion with success/cancelled icons. (v0.4.7c)

- **Indexing Error Categorization** — Error handling for failed indexing operations with 10 error categories (RateLimit, NetworkError, FileNotFound, etc.). `IndexingErrorCategorizer` classifies exceptions for targeted retry logic and user-friendly messaging. Failed documents show `IsRetryable` status and `SuggestedAction` guidance. (v0.4.7d)

- **Index Management Telemetry** — MediatR events for tracking manual indexing operations: `DocumentReindexedEvent`, `DocumentRemovedFromIndexEvent`, and `AllDocumentsReindexedEvent`.

#### Sub-Part Changelogs

| Version                             | Title                    | Status      |
| ----------------------------------- | ------------------------ | ----------- |
| [v0.4.7a](v0.4.x/LCS-CL-v0.4.7a.md) | Index Status View        | ✅ Complete |
| [v0.4.7b](v0.4.x/LCS-CL-v0.4.7b.md) | Manual Indexing Controls | ✅ Complete |
| [v0.4.7c](v0.4.x/LCS-CL-v0.4.7c.md) | Indexing Progress        | ✅ Complete |
| [v0.4.7d](v0.4.x/LCS-CL-v0.4.7d.md) | Indexing Errors          | ✅ Complete |

---

## [v0.4.6] - 2026-02 (In Progress)

### The Reference Panel (Search UI)

This release implements the user-facing interface for semantic search, completing the RAG subsystem's search experience.

#### What's New

- **Search History Service** — `ISearchHistoryService` with thread-safe LinkedList-based implementation for O(1) operations. Case-insensitive deduplication, automatic eviction at max size (10), and query trimming.

- **Reference Panel View** — `ReferenceView.axaml` with AutoCompleteBox for search with history dropdown, virtualized results list using `VirtualizingStackPanel`, loading indicator, no-results state, and status bar showing result count and duration. License gating displays warning for unlicensed users.

- **ViewModels** — `ReferenceViewModel` orchestrates search execution, history management, and license checking via `SearchLicenseGuard`. `SearchResultItemViewModel` wraps `SearchHit` with formatted display properties including document name, preview text, relevance score, and section heading.

- **Search Result Item View** — Dedicated `SearchResultItemView` UserControl with score badge coloring (green/amber/orange/gray based on relevance), query term highlighting via `HighlightedTextBlock` custom control, document icon, section heading, and double-click navigation to source.

- **Source Navigation** — `IReferenceNavigationService` with `ReferenceNavigationService` implementation that bridges RAG search results with the editor's navigation infrastructure. Opens closed documents, scrolls to chunk offsets, and highlights matched text spans. Publishes `ReferenceNavigatedEvent` for telemetry tracking. `HighlightStyle` enum for categorizing editor highlights (SearchResult, Error, Warning, Reference).

- **Search History Service** — Enhanced `ISearchHistoryService` with persistence via `ISearchHistoryRepository`, query removal, and `IObservable<string>` change notifications. Thread-safe LinkedList implementation with O(1) operations, case-insensitive deduplication, and automatic eviction.

- **Axiom Data Model** — Foundational data model for the Axiom Store (CKVS Phase 1b). Defines `Axiom`, `AxiomRule`, `AxiomViolation`, and `AxiomValidationResult` records with 4 supporting enums (`AxiomSeverity`, `AxiomTargetKind`, `AxiomConstraintType`, `ConditionOperator`) for domain rule governance.

- **Axiom Repository** — Persistent storage for domain axioms using PostgreSQL via Dapper. `IAxiomRepository` contract defines CRUD operations with `AxiomFilter` for querying by type/category/tags and `AxiomStatistics` for aggregate counts. `AxiomCacheService` provides 5-minute sliding / 30-minute absolute expiration via `IMemoryCache`. License-gated: WriterPro+ for reads, Teams+ for writes.

- **Axiom Loader** — YAML-based axiom loading from embedded resources and workspace files (`.lexichord/knowledge/axioms/`). `AxiomYamlParser` deserializes axiom files with constraint type mapping, `AxiomSchemaValidator` validates target types against `ISchemaRegistry`, and `AxiomLoader` orchestrates loading with file watching for hot-reload. License-gated: WriterPro+ for built-in, Teams+ for workspace.

- **Axiom Query API** — In-memory axiom store with O(1) retrieval indexed by target type. `AxiomEvaluator` supports 9 constraint types (Required, OneOf, Range, Pattern, Cardinality, NotBoth, RequiresTogether, Equals, NotEquals) with 13 condition operators. `AxiomStore` provides validation, statistics tracking, and `AxiomsLoaded` event publishing for hot-reload. License-gated: WriterPro+ for queries, Teams+ for validation.

#### Sub-Part Changelogs

| Version                             | Title                  | Status      |
| ----------------------------------- | ---------------------- | ----------- |
| [v0.4.6a](v0.4.x/LCS-CL-v0.4.6a.md) | Reference Panel View   | ✅ Complete |
| [v0.4.6b](v0.4.x/LCS-CL-v0.4.6b.md) | Search Result Item     | ✅ Complete |
| [v0.4.6c](v0.4.x/LCS-CL-v0.4.6c.md) | Source Navigation      | ✅ Complete |
| [v0.4.6d](v0.4.x/LCS-CL-v0.4.6d.md) | Search History Service | ✅ Complete |
| [v0.4.6e](v0.4.x/LCS-CL-v0.4.6e.md) | Axiom Data Model       | ✅ Complete |
| [v0.4.6f](v0.4.x/LCS-CL-v0.4.6f.md) | Axiom Repository       | ✅ Complete |
| [v0.4.6g](v0.4.x/LCS-CL-v0.4.6g.md) | Axiom Loader           | ✅ Complete |
| [v0.4.6h](v0.4.x/LCS-CL-v0.4.6h.md) | Axiom Query API        | ✅ Complete |

---

## [v0.4.5] - 2026-02 (In Progress)

### The Searcher (Semantic Query)

This release implements semantic search capabilities enabling natural language queries against the indexed document corpus.

#### What's New

- **Search Abstractions** — `ISemanticSearchService` interface defining the semantic search contract with `SearchAsync` method. `SearchOptions` record for configurable TopK, MinScore, DocumentFilter, abbreviation expansion, and embedding caching. `SearchResult` container with ranked hits, timing, and truncation metadata. `SearchHit` for individual matches with score formatting helpers (`ScorePercent`, `ScoreDecimal`) and content previews.

- **Vector Search Query** — `PgVectorSearchService` implementing `ISemanticSearchService` using pgvector's cosine similarity (`<=>` operator) for semantic search against indexed document chunks. Includes `SearchLicenseGuard` for WriterPro tier enforcement with three validation modes (throw, try/publish, property check), `IQueryPreprocessor` interface with passthrough stub, `ChunkSearchRow` for Dapper result mapping, and MediatR events (`SemanticSearchExecutedEvent`, `SearchDeniedEvent`) for telemetry and license denial tracking.

- **Query Preprocessing** — Full `QueryPreprocessor` implementation replacing the `PassthroughQueryPreprocessor` stub from v0.4.5b. Four-stage normalization pipeline (whitespace trimming, whitespace collapsing, Unicode NFC normalization, optional abbreviation expansion) with 35 technical abbreviations across 6 categories. SHA256-based query embedding caching with 5-minute sliding expiration via `IMemoryCache`. Added `ClearCache()` method to `IQueryPreprocessor` interface.

- **License Gating** — Enhanced `SearchLicenseGuard` with publicly accessible `FeatureName` and `RequiredTier` constants for UI consumers, and `GetUpgradeMessage()` method providing tier-specific upgrade guidance for the Reference Panel (v0.4.6). Added `UsedCachedEmbedding` telemetry flag to `SemanticSearchExecutedEvent` for cache efficiency tracking. Updated `PgVectorSearchService` to track and report embedding cache hits in telemetry events.

- **Graph Database Integration** — Neo4j 5.x Community Edition integration for the Knowledge Graph Foundation (CKVS Phase 1). `IGraphConnectionFactory` with license-gated session creation (Teams for write, WriterPro for read-only, Core denied). `Neo4jGraphSession` with Cypher query execution, Stopwatch timing, slow query warnings (>100ms), and exception wrapping to `GraphQueryException`. `Neo4jHealthCheck` for application health monitoring. Docker Compose Neo4j container with APOC plugin. PostgreSQL metadata tables (`GraphMetadata`, `DocumentEntities`) for cross-system linkage.

- **Schema Registry Service** — YAML-driven schema system for knowledge graph entity and relationship type governance. `ISchemaRegistry` with in-memory registry, case-insensitive lookups, and comprehensive validation producing typed error codes (13 error codes, 1 warning code). `SchemaLoader` parses YAML files with underscore naming convention and flexible From/To handling. `SchemaValidator` enforces type existence, required properties, type correctness, and constraint validation (length, pattern, numeric range). Built-in technical documentation schema with 6 entity types (Product, Component, Endpoint, Parameter, Response, Concept) and 6 relationship types.

- **Entity Abstraction Layer** — Pluggable entity extraction pipeline for identifying structured entities in document text. `IEntityExtractor` interface with priority ordering and confidence scoring. `IEntityExtractionPipeline` coordinator with error isolation, confidence filtering, mention deduplication (overlapping spans resolved by highest confidence), and entity aggregation. Three built-in regex-based extractors: `EndpointExtractor` (API endpoints with 3 patterns, confidence 0.7–1.0), `ParameterExtractor` (parameters from paths, queries, code, JSON with 5 patterns, 0.6–1.0), and `ConceptExtractor` (domain terms, acronyms, glossary entries with 4 patterns, 0.5–0.95). `MentionAggregator` groups mentions by type and normalized value into `AggregatedEntity` records for graph node creation.

#### Sub-Part Changelogs

| Version                             | Title                      | Status      |
| ----------------------------------- | -------------------------- | ----------- |
| [v0.4.5a](v0.4.x/LCS-CL-v0.4.5a.md) | Search Abstractions        | ✅ Complete |
| [v0.4.5b](v0.4.x/LCS-CL-v0.4.5b.md) | Vector Search Query        | ✅ Complete |
| [v0.4.5c](v0.4.x/LCS-CL-v0.4.5c.md) | Query Preprocessing        | ✅ Complete |
| [v0.4.5d](v0.4.x/LCS-CL-v0.4.5d.md) | License Gating             | ✅ Complete |
| [v0.4.5e](v0.4.x/LCS-CL-v0.4.5e.md) | Graph Database Integration | ✅ Complete |
| [v0.4.5f](v0.4.x/LCS-CL-v0.4.5f.md) | Schema Registry Service    | ✅ Complete |
| [v0.4.5g](v0.4.x/LCS-CL-v0.4.5g.md) | Entity Abstraction Layer   | ✅ Complete |

---

## [v0.4.4] - 2026-02 (In Progress)

### The Embedder (Vector Generation)

This release introduces the embedding infrastructure for generating semantic vectors from text, enabling similarity search and RAG capabilities.

#### What's New

- **Embedding Abstractions** — Core `IEmbeddingService` interface defining vector generation operations with single and batch embedding support. `EmbeddingOptions` configuration record with validation, `EmbeddingResult` for operation outcomes with diagnostics, and `EmbeddingException` for error classification.

- **OpenAI Connector** — `OpenAIEmbeddingService` implementation using OpenAI's text-embedding-3-small model. Features Polly-based exponential backoff retry (2^attempt seconds) for rate limits (429) and server errors (5xx). Secure API key retrieval via `ISecureVault`.

- **Token Counting** — `ITokenCounter` interface with `TiktokenTokenCounter` implementation using cl100k_base encoding. Provides accurate token counting, truncation with word boundary preservation, and encode/decode round-trip support.

- **Document Indexing Pipeline** — `DocumentIndexingPipeline` orchestrates the complete indexing workflow: chunk → validate tokens → embed (batched) → store. Features license gating (WriterPro), MediatR events (`DocumentIndexedEvent`, `DocumentIndexingFailedEvent`), and graceful error handling.

#### Sub-Part Changelogs

| Version                             | Title                  | Status      |
| ----------------------------------- | ---------------------- | ----------- |
| [v0.4.4a](v0.4.x/LCS-CL-v0.4.4a.md) | Embedding Abstractions | ✅ Complete |
| [v0.4.4b](v0.4.x/LCS-CL-v0.4.4b.md) | OpenAI Connector       | ✅ Complete |
| [v0.4.4c](v0.4.x/LCS-CL-v0.4.4c.md) | Token Counting         | ✅ Complete |
| [v0.4.4d](v0.4.x/LCS-CL-v0.4.4d.md) | Embedding Pipeline     | ✅ Complete |

---

## [v0.4.3] - 2026-02 (In Progress)

### The Splitter (Chunking Strategies)

This release introduces a flexible chunking system for breaking documents into semantically meaningful text segments suitable for embedding.

#### What's New

- **Chunking Abstractions** — Core interfaces, records, and enums defining the chunking contract. `IChunkingStrategy` enables pluggable algorithms, `TextChunk` and `ChunkMetadata` carry output with position and context, `ChunkingOptions` provides configurable behavior with validation, and `ChunkingPresets` offers ready-to-use configurations for common use cases.

- **Fixed-Size Chunker** — `FixedSizeChunkingStrategy` splits text into chunks of configurable target size with overlap for context continuity. Two-phase word boundary search (backward 20%, forward 10%) avoids mid-word splits. Supports whitespace trimming, Unicode characters, and proper metadata (Index, TotalChunks, IsFirst/IsLast).

- **Paragraph Chunker** — `ParagraphChunkingStrategy` splits text on paragraph boundaries (double newlines). Short paragraphs are merged until reaching `TargetSize`, while oversized paragraphs use `FixedSizeChunkingStrategy` as fallback.

#### Sub-Part Changelogs

| Version                             | Title                   | Status      |
| ----------------------------------- | ----------------------- | ----------- |
| [v0.4.3a](v0.4.x/LCS-CL-v0.4.3a.md) | Chunking Abstractions   | ✅ Complete |
| [v0.4.3b](v0.4.x/LCS-CL-v0.4.3b.md) | Fixed-Size Chunker      | ✅ Complete |
| [v0.4.3c](v0.4.x/LCS-CL-v0.4.3c.md) | Paragraph Chunker       | ✅ Complete |
| [v0.4.3d](v0.4.x/LCS-CL-v0.4.3d.md) | Markdown Header Chunker | ✅ Complete |

---

## [v0.3.8] - 2026-01 (In Progress)

### The Hardening (Unit Testing)

This release introduces comprehensive unit testing to verify algorithm accuracy and prevent regressions.

#### What's New

- **Fuzzy Algorithm Accuracy Tests** — 20 tests verifying Levenshtein distance calculations including symmetry, edge cases, known distance values, and Unicode handling.

- **Threshold Behavior Tests** — 14 tests covering boundary precision, threshold sweep at 0-100%, and partial ratio matching behavior.

- **Typo Detection Pattern Tests** — 22 tests validating real-world typo detection including substitution, transposition, omission, insertion, and common misspelling patterns.

- **Scanner Accuracy Tests** — 5 new tests for multi-term scanning, position calculation, and ratio reporting accuracy.

- **Readability Corpus Infrastructure** — Standard corpus with 5 entries (Gettysburg Address, simple children's text, academic prose, Hemingway-style, mixed complexity) containing pre-calculated reference values for validation.

- **Syllable Counter Accuracy Tests** — 19 tests validating syllable counting against CMU dictionary reference words, corpus entries, silent 'e' patterns, and compound words.

- **Sentence Tokenizer Accuracy Tests** — 17 tests verifying sentence and word count extraction accuracy across corpus entries.

- **Readability Formula Accuracy Tests** — 50 tests ensuring Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease calculations match reference values within tolerance.

- **Passive Voice Detection Accuracy Tests** — 9 new tests validating passive voice detection with 95% accuracy threshold on labeled corpus. Includes false positive prevention for linking verbs, adjectives, and active progressive patterns.

- **Weak Word Scanner Enhanced Tests** — 8 new tests verifying intensity adverbs, hedging words, and vague qualifier detection with word boundary matching.

- **LabeledSentenceCorpus Test Fixture** — Reusable corpus with 8 categories (ClearPassive, ClearActive, LinkingVerb, AdjectiveComplement, ProgressiveActive, GetPassive, ModalPassive, EdgeCase) for accuracy testing.

- **Performance Benchmark Baseline** — BenchmarkDotNet integration with benchmarks for Readability, Fuzzy Scanning, Voice Analysis, and Full Pipeline operations. Includes 32 CI threshold tests with 10% regression tolerance and throughput validation.

#### Sub-Part Changelogs

| Version                                    | Title                    | Status      |
| ------------------------------------------ | ------------------------ | ----------- |
| [v0.3.8a](v0.3.x/v0.3.8/LCS-CL-v0.3.8a.md) | Fuzzy Test Suite         | ✅ Complete |
| [v0.3.8b](v0.3.x/v0.3.8/LCS-CL-v0.3.8b.md) | Readability Test Suite   | ✅ Complete |
| [v0.3.8c](v0.3.x/v0.3.8/LCS-CL-v0.3.8c.md) | Passive Voice Test Suite | ✅ Complete |
| [v0.3.8d](v0.3.x/v0.3.8/LCS-CL-v0.3.8d.md) | Benchmark Baseline       | ✅ Complete |

---

## [v0.3.1] - 2026-01 (In Progress)

### The Fuzzy Engine (Advanced Matching)

This release introduces fuzzy string matching to detect typos and variations of forbidden terminology.

#### What's New

- **Fuzzy Match Service** — New `IFuzzyMatchService` provides Levenshtein distance-based string matching using FuzzySharp. Normalized inputs (trimmed, lowercase) ensure consistent matching. Thread-safe and stateless.

- **Similarity Calculation** — `CalculateRatio()` returns 0-100 similarity percentage. `CalculatePartialRatio()` finds best substring matches for pattern detection within longer text.

- **Threshold-Based Matching** — `IsMatch()` convenience method combines ratio calculation with threshold comparison. Threshold specified as decimal (0.0 to 1.0).

- **Fuzzy Term Properties** — `StyleTerm` entity now includes `FuzzyEnabled` and `FuzzyThreshold` properties for per-term fuzzy matching configuration. Default threshold is 80%.

- **Fuzzy Repository Methods** — `ITerminologyRepository` extended with `GetFuzzyEnabledTermsAsync()` for cached fuzzy term retrieval and `InvalidateFuzzyTermsCache()` for explicit cache control.

- **Inclusive Language Defaults** — Seeder includes 5 fuzzy-enabled terms for inclusive language (whitelist→allowlist, blacklist→denylist, master→main, slave→replica, sanity check→confidence check).

- **Fuzzy Scanner** — New `IFuzzyScanner` service scans documents for approximate terminology matches. Tokenizes text preserving hyphenated words, applies per-term thresholds, and prevents double-counting of exact matches. Requires Writer Pro license.

- **StyleViolation Fuzzy Metadata** — `StyleViolation` now includes `IsFuzzyMatch` flag and `FuzzyRatio` percentage for fuzzy match identification.

#### Sub-Part Changelogs

| Version                                 | Title                 | Status      |
| --------------------------------------- | --------------------- | ----------- |
| [v0.3.1a](v0.3.x/v0.3.1/LCS-CL-031a.md) | Algorithm Integration | ✅ Complete |
| [v0.3.1b](v0.3.x/v0.3.1/LCS-CL-031b.md) | Repository Update     | ✅ Complete |
| [v0.3.1c](LCS-CL-031c.md)               | The Fuzzy Scanner     | ✅ Complete |
| [v0.3.1d](LCS-CL-031d.md)               | License Gating        | ✅ Complete |

---

## [v0.3.3] - 2026-01 (In Progress)

### The Readability Engine (Text Analysis)

This release introduces algorithmic readability analysis to compute text complexity metrics.

#### What's New

- **Sentence Tokenizer** — New `ISentenceTokenizer` splits text into sentences while respecting abbreviations (Mr., Dr., Inc., U.S.A., etc.). Dictionary of 50+ common abbreviations avoids false sentence breaks. Handles ellipsis, initials, and edge cases.

- **Syllable Counter** — New `ISyllableCounter` counts syllables using vowel-group heuristics and an exception dictionary of 40+ irregular words. Applies rules for silent 'e', '-ed', and '-es' suffixes. Provides `IsComplexWord()` for Gunning Fog Index calculation (3+ meaningful syllables, excluding common suffix inflation).

- **Readability Calculator** — New `IReadabilityService` computes Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease scores. Uses Sentence Tokenizer and Syllable Counter for accurate analysis. Includes `ReadabilityMetrics` record with computed properties and human-readable interpretation of reading ease.

- **Readability HUD Widget** — New `ReadabilityHudViewModel` and `ReadabilityHudView` display real-time readability metrics in the Problems Panel header. Features color-coded grade levels, fog index, and reading ease with tooltip descriptions. Writer Pro tier license gating via `FeatureCodes.ReadabilityHud`.

#### Sub-Part Changelogs

| Version                                 | Title                  | Status      |
| --------------------------------------- | ---------------------- | ----------- |
| [v0.3.3a](v0.3.x/v0.3.3/LCS-CL-033a.md) | Sentence Tokenizer     | ✅ Complete |
| [v0.3.3b](v0.3.x/v0.3.3/LCS-CL-033b.md) | Syllable Counter       | ✅ Complete |
| [v0.3.3c](v0.3.x/v0.3.3/LCS-CL-033c.md) | Readability Calculator | ✅ Complete |
| [v0.3.3d](v0.3.x/v0.3.3/LCS-CL-033d.md) | HUD Widget             | ✅ Complete |

---

## [v0.3.4] - 2026-01 (In Progress)

### The Writing Coach (Voice Profiles)

This release introduces Voice Profiles for context-aware style enforcement based on content type.

#### What's New

- **Voice Profile Definition** — New `VoiceProfile` record defines target style constraints including grade level, sentence length, passive voice tolerance, and adverb/weasel word flagging. Includes validation for constraint boundaries.

- **Built-In Profiles** — 5 predefined profiles tailored to different writing styles:
    - **Technical** — Direct, precise documentation (Grade 11, no passive voice, flags adverbs/weasel words)
    - **Marketing** — Engaging, persuasive copy (Grade 9, allows passive voice, flags weasel words)
    - **Academic** — Formal, scholarly writing (Grade 13, allows passive voice, no style flags)
    - **Narrative** — Creative, flowing prose (Grade 9, allows passive voice, no style flags)
    - **Casual** — Conversational, friendly content (Grade 7, allows passive voice, no style flags)

- **Voice Profile Service** — New `IVoiceProfileService` manages profile selection and CRUD operations. Includes in-memory caching, license gating for custom profiles (Teams+), and MediatR event publishing on profile changes.

- **Profile Changed Event** — New `ProfileChangedEvent` notification enables reactive UI updates when the active profile changes.

- **Passive Voice Detector** — New `IPassiveVoiceDetector` uses regex pattern matching with confidence scoring to identify passive constructions (to-be, modal, progressive, get-passive). Distinguishes true passive voice from predicate adjectives like "The door is closed" using a confidence threshold (≥0.5).

- **Weak Word Scanner** — New `IWeakWordScanner` detects adverbs (~40), weasel words (~30), and filler expressions (~15) in text. Respects Voice Profile settings for selective flagging while always detecting fillers. Provides specific suggestions for over 50 common weak words.

- **Profile Selector UI** — New `ProfileSelectorWidget` in the status bar displays the active voice profile name with rich tooltips. Context menu enables quick profile switching. Auto-initializes on load with graceful error handling.

#### Sub-Part Changelogs

| Version                                 | Title                    | Status      |
| --------------------------------------- | ------------------------ | ----------- |
| [v0.3.4a](v0.3.x/v0.3.4/LCS-CL-034a.md) | Voice Profile Definition | ✅ Complete |
| [v0.3.4b](v0.3.x/v0.3.4/LCS-CL-034b.md) | Passive Voice Detector   | ✅ Complete |
| [v0.3.4c](v0.3.x/v0.3.4/LCS-CL-034c.md) | Weak Word Scanner        | ✅ Complete |
| [v0.3.4d](v0.3.x/v0.3.4/LCS-CL-034d.md) | Profile Selector UI      | ✅ Complete |

---

## [v0.3.5] - 2026-01 (In Progress)

### The Resonance Dashboard (Writing Metrics Visualization)

This release introduces the charting infrastructure for visualizing writing metrics in a spider chart format.

#### What's New

- **Chart Data Service** — New `IChartDataService` aggregates metrics from readability, passive voice, and weak word services into normalized 0-100 scale data points. Thread-safe caching with event notification on recomputation.

- **Axis Configuration** — New `IResonanceAxisProvider` and `DefaultAxisProvider` define 6 axes for the spider chart: Readability (Flesch Reading Ease), Clarity (Passive Voice%), Precision (Weak Word%), Accessibility (FK Grade), Density (Words/Sentence), and Flow (Sentence Variance).

- **Normalization Logic** — `ResonanceAxisDefinition` record handles value normalization with clamping, configurable ranges, and inverted scales for metrics where "lower is better" (passive voice, weak words, grade level).

- **Theme Configuration** — `ChartThemeConfiguration` provides light and dark color palettes for chart rendering, integrated with Lexichord's theme system.

- **LiveCharts2 Integration** — New dependency on `LiveChartsCore.SkiaSharpView.Avalonia` (2.0.0-rc6.1) for high-performance Avalonia-native charting. Registered in `StyleModule`.

- **Spider Chart with Target Overlay** — New `ResonanceDashboardView` displays writing metrics on an interactive polar chart. Features current values (solid fill) and target overlay (dashed line) based on active Voice Profile. Includes `ISpiderChartSeriesBuilder` for theme-aware series construction and `ITargetOverlayService` for profile-based target computation with caching.

- **Real-Time Chart Updates** — New `IResonanceUpdateService` provides a reactive pipeline for chart updates. Uses Rx-based debouncing (300ms) to coalesce rapid analysis events while providing immediate response for profile changes. License-gated to Writer Pro tier.

#### Sub-Part Changelogs

| Version                                 | Title                   | Status      |
| --------------------------------------- | ----------------------- | ----------- |
| [v0.3.5a](v0.3.x/v0.3.5/LCS-CL-035a.md) | Charting Infrastructure | ✅ Complete |
| [v0.3.5b](v0.3.x/v0.3.5/LCS-CL-035b.md) | Spider Chart            | ✅ Complete |
| [v0.3.5c](v0.3.x/v0.3.5/LCS-CL-035c.md) | Real-Time Updates       | ✅ Complete |
| [v0.3.5d](v0.3.x/v0.3.5/LCS-CL-035d.md) | Target Overlays         | ✅ Complete |

---

## [v0.3.6] - 2026-01 (In Progress)

### The Global Dictionary (Project Settings)

This release introduces project-level configuration management with hierarchical settings.

#### What's New

- **Layered Configuration Provider** — New `ILayeredConfigurationProvider` merges configuration from three sources (System → User → Project) with "higher wins" precedence. Enables project-specific style customization stored in `.lexichord/style.yaml`.

- **Style Configuration Record** — New `StyleConfiguration` record captures all style settings including readability constraints, voice analysis thresholds, and terminology overrides. Immutable design ensures thread safety.

- **License-Gated Project Config** — Project-level configuration is gated behind Writer Pro license (`Feature.GlobalDictionary`). Core users receive merged System + User configuration only.

- **Configuration Caching** — 5-second cache with manual invalidation via `InvalidateCache()`. Reduces file I/O overhead for frequently accessed configuration.

- **Security Safeguards** — 100KB file size limit prevents denial-of-service via oversized config files. Graceful YAML parse error handling with fallback to defaults.

- **Conflict Resolution** — New `IConflictResolver` detects and logs configuration conflicts between layers. Project-wins semantics ensure higher-priority sources always override. Term override logic handles exclusions vs additions with repository fallback. Rule ignore patterns support wildcards (`PASSIVE-*`, `*-WARNINGS`).

- **Override UI Infrastructure** — New `IProjectConfigurationWriter` enables users to ignore rules and exclude terms via project configuration. Atomic YAML writes prevent file corruption. `OverrideMenuViewModel` provides license-gated commands for context menu integration.

- **Ignored Files** — New `IIgnorePatternService` enables `.lexichordignore` files with glob pattern support (`*.log`, `build/**`, `**/temp/**`). Negation patterns (`!important.log`) un-ignore specific files. License-gated limits (Core: 5 patterns, Writer Pro: unlimited). Hot-reload via FileSystemWatcher with 100ms debounce.

#### Sub-Part Changelogs

| Version                                 | Title                 | Status      |
| --------------------------------------- | --------------------- | ----------- |
| [v0.3.6a](v0.3.x/v0.3.6/LCS-CL-036a.md) | Layered Configuration | ✅ Complete |
| [v0.3.6b](v0.3.x/v0.3.6/LCS-CL-036b.md) | Conflict Resolution   | ✅ Complete |
| [v0.3.6c](v0.3.x/v0.3.6/LCS-CL-036c.md) | Override UI           | ✅ Complete |
| [v0.3.6d](v0.3.x/v0.3.6/LCS-CL-036d.md) | Ignored Files         | ✅ Complete |

---

## [v0.3.7] - 2026-01 (In Progress)

### The Performance Tuning (Async Pipelines)

This release introduces infrastructure for optimizing typing responsiveness with reactive request buffering.

#### What's New

- **Analysis Request Buffering** — New `IAnalysisBuffer` service debounces document analysis requests using System.Reactive. Per-document debouncing ensures rapid edits only trigger analysis after a configurable idle period (default 300ms). Latest-wins semantics automatically discard intermediate requests.

- **Parallel Scanner Execution** — New `IParallelAnalysisPipeline` executes all four scanners (Regex, Fuzzy, Readability, Voice) concurrently via `Task.WhenAll()`. Reduces analysis latency from sequential sum to longest scanner duration. Error isolation ensures partial results when individual scanners fail.

- **Snapshot Semantics** — New `AnalysisRequest` record captures document content at request time with associated cancellation token. Immutable design ensures thread-safe sharing across the async pipeline.

- **Configurable Buffer Options** — New `AnalysisBufferOptions` configuration class controls idle period timing, maximum buffered documents, and enable/disable toggle for bypass mode.

- **Problems Panel Virtualization** — Added `ScrollOffset` property to preserve scroll position during list updates. Virtualization diagnostics logging helps monitor performance. Panel efficiently handles 5,000+ violations via collapsed severity groups.

- **Memory Leak Prevention** — New `DisposableViewModel` base class with automatic subscription cleanup via Composite Disposable pattern. Thread-safe `IDisposableTracker` ensures proper disposal on document close, preventing memory growth during long sessions.

#### Sub-Part Changelogs

| Version                                    | Title                  | Status      |
| ------------------------------------------ | ---------------------- | ----------- |
| [v0.3.7a](v0.3.x/v0.3.7/LCS-CL-037a.md)    | Background Buffering   | ✅ Complete |
| [v0.3.7b](v0.3.x/v0.3.7/LCS-CL-037b.md)    | Parallelization        | ✅ Complete |
| [v0.3.7c](v0.3.x/v0.3.7/LCS-CL-037c.md)    | Virtualization         | ✅ Complete |
| [v0.3.7d](v0.3.x/v0.3.7/LCS-CL-v0.3.7d.md) | Memory Leak Prevention | ✅ Complete |

---

## [v0.2.7] - 2026-01 (In Progress)

### The Turbo (Performance Optimization)

This release focuses on performance optimizations to ensure smooth UI responsiveness during intensive linting operations.

#### What's New

- **Async Offloading** — CPU-intensive regex scanning now runs on background threads via `Task.Run`, ensuring the UI never freezes during document analysis. Thread marshalling ensures violation updates are safely dispatched to the UI thread.

- **Thread Marshaller Abstraction** — New `IThreadMarshaller` interface abstracts Avalonia's `Dispatcher` for testable thread-aware code. Methods for async invocation, fire-and-forget posting, and DEBUG-only thread assertions.

- **Code Block Ignoring** — Markdown code blocks (fenced and inline) are now excluded from style linting, preventing false positives when code samples contain terms like "whitelist" or technical jargon. State machine detects fenced blocks (``` and ~~~), regex handles inline code spans.

- **Frontmatter Ignoring** — YAML, TOML, and JSON frontmatter at the start of Markdown documents is now automatically excluded from style scanning. Supports UTF-8 BOM handling, Windows/Unix line endings, and unclosed frontmatter detection. Priority ordering ensures frontmatter is detected before code blocks.

- **Content Filter Architecture** — New `IContentFilter` interface enables pluggable pre-scan content filtering. Binary-search-based match filtering ensures O(log n) performance for large documents.

- **Large File Stress Testing** — Performance testing infrastructure validates linting on 5MB+ documents. Chunked scanning with viewport prioritization (<100ms viewport response), adaptive debounce monitoring (200ms-1000ms), synthetic test corpus generation, and comprehensive stress test suite.

- **Module Version Update** — Style module version updated from 0.2.6 to 0.2.7.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.2.7a](v0.2.x/LCS-CL-027a.md) | Async Offloading       | ✅ Complete |
| [v0.2.7b](v0.2.x/LCS-CL-027b.md) | Code Block Ignoring    | ✅ Complete |
| [v0.2.7c](v0.2.x/LCS-CL-027c.md) | Frontmatter Ignoring   | ✅ Complete |
| [v0.2.7d](v0.2.x/LCS-CL-027d.md) | Large File Stress Test | ✅ Complete |

---

## [v0.2.6] - 2026-01 (In Progress)

### The Sentinel (Real-Time Feedback Sidebar)

This release introduces **The Sentinel**, a Problems Panel sidebar providing centralized visibility into style violations during editing.

#### What's New

- **Problems Panel** — Sidebar view displaying style violations grouped by severity (Error, Warning, Info, Hint). Shows header with scope badge ("Current File"), severity count badges, loading indicator, and empty state. Collapsible groups with violation details (location, message, rule ID).

- **Linting Event Integration** — Subscribes to `LintingCompletedEvent` via MediatR to receive real-time violation updates. Clears previous violations on new events, transforms aggregated violations to display items, and updates counts via PropertyChanged.

- **Severity Grouping** — Violations organized into four severity groups with Unicode icons (⛔ Error, ⚠ Warning, ℹ Info, 💡 Hint). Groups are collapsible with expand/collapse state tracking.

- **Navigation Sync** — Double-click any problem item to navigate directly to its location in the editor. The editor scrolls to the target line, positions the caret at the exact column, and applies a temporary highlight animation (2 second fade). Supports navigation within the active document.

- **Scorecard Widget** — Gamification widget displaying compliance score (0-100%), letter grade (A-F), and trend indicator. Score calculated from weighted penalties: errors (5 pts), warnings (2 pts), info (0.5 pts). Grade colors and trend arrows provide instant visual feedback on document health.

- **Dock Region Integration** — ProblemsPanelView registered in Right dock region alongside LexiconView.

#### Sub-Part Changelogs

| Version                          | Title          | Status      |
| -------------------------------- | -------------- | ----------- |
| [v0.2.6a](v0.2.x/LCS-CL-026a.md) | Problems Panel | ✅ Complete |
| [v0.2.6b](v0.2.x/LCS-CL-026b.md) | Navigation     | ✅ Complete |
| [v0.2.6c](v0.2.x/LCS-CL-026c.md) | Scorecard      | ✅ Complete |
| [v0.2.6d](v0.2.x/LCS-CL-026d.md) | Filter Scope   | ✅ Complete |

---

## [v0.2.5] - 2026-01 (In Progress)

### The Librarian (Terminology Management)

This release introduces **The Librarian**, a grid-based UI for managing style terminology with license-gated editing.

#### What's New

- **Terminology Grid View** — Avalonia DataGrid for viewing and managing style terms. Supports sorting by severity and category, row selection, and context menu operations.

- **Search & Filter** — Real-time filtering with debounced search (300ms) across term, replacement, and notes fields. Includes inactive term toggle, category dropdown, and severity dropdown. All filters apply as AND logic.

- **Term Editor Dialog** — Modal dialog for adding and editing terminology rules. Features real-time regex validation, pattern testing against sample text, MatchCase toggle for case-sensitive matching, and dirty state tracking.

- **Bulk Import/Export** — CSV and Excel import with validation, duplicate detection, and progress reporting. JSON and CSV export with optional metadata and category filtering. Protects against oversized files (10MB max) and excessive rows (10,000 max).

- **License-Gated Editing** — Edit, delete, and toggle active commands require WriterPro tier or higher. Copy Pattern is available to all tiers. Status bar displays current license tier with read-only indicator for lower tiers.

- **Default Sorting** — Terms are sorted by severity (Error → Warning → Suggestion → Info) then by category alphabetically, prioritizing the most critical issues.

- **Dock Region Integration** — LexiconView is registered in the Right dock region, accessible alongside the document editor.

#### Sub-Part Changelogs

| Version                          | Title                 | Status      |
| -------------------------------- | --------------------- | ----------- |
| [v0.2.5a](v0.2.x/LCS-CL-025a.md) | Terminology Grid View | ✅ Complete |
| [v0.2.5b](v0.2.x/LCS-CL-025b.md) | Search & Filter       | ✅ Complete |
| [v0.2.5c](v0.2.x/LCS-CL-025c.md) | Term Editor Dialog    | ✅ Complete |
| [v0.2.5d](v0.2.x/LCS-CL-025d.md) | Bulk Import/Export    | ✅ Complete |

---

## [v0.2.2] - 2026-01 (In Progress)

### The Lexicon (Terminology Database)

This release introduces **The Lexicon**, a persistent database for managing style terminology with full CRUD operations.

#### What's New

- **Style Terms Schema** — Database table for storing terminology entries within style sheets. Supports term-replacement pairs, categorization, severity levels, and soft-disable via `IsActive` flag. Includes fuzzy search via PostgreSQL's `pg_trgm` extension.

- **Terminology Repository** — High-performance data access layer with `IMemoryCache` integration. Provides O(1) active term lookups via cached HashSet, with automatic cache invalidation on write operations.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.2.2a](v0.2.x/LCS-CL-022a.md) | Style Schema Migration | ✅ Complete |
| [v0.2.2b](v0.2.x/LCS-CL-022b.md) | Repository Layer       | ✅ Complete |
| [v0.2.2c](v0.2.x/LCS-CL-022c.md) | Terminology Seeding    | ✅ Complete |
| [v0.2.2d](v0.2.x/LCS-CL-022d.md) | CRUD Service           | ✅ Complete |

---

## [v0.2.3] - 2026-01 (In Progress)

### The Critic (Linter Engine)

This release introduces **The Critic**, a reactive linting engine that provides real-time document analysis.

#### What's New

- **Reactive Pipeline** — System.Reactive-based infrastructure for debounced, concurrent document linting. Documents subscribe to the orchestrator and receive throttled analysis with configurable debounce (300ms default) and concurrency limits (2 concurrent scans default).

- **Linting Orchestrator** — Central hub managing document subscriptions, coordinating scans with IStyleEngine, and publishing results via observable stream and MediatR domain events.

- **Domain Contracts** — Immutable records for linting configuration (`LintingOptions`), per-document state tracking (`DocumentLintState`), and operation results (`LintResult`) with factory methods.

- **Violation Aggregator** — Transforms raw scanner matches into user-facing violations with line/column positions, deduplication (higher severity wins), sorting by document position, and per-document caching for efficient query support.

#### Sub-Part Changelogs

| Version                          | Title                | Status      |
| -------------------------------- | -------------------- | ----------- |
| [v0.2.3a](v0.2.x/LCS-CL-023a.md) | Reactive Pipeline    | ✅ Complete |
| [v0.2.3b](v0.2.x/LCS-CL-023b.md) | Debounce Logic       | ✅ Complete |
| [v0.2.3c](v0.2.x/LCS-CL-023c.md) | Scanner (Regex)      | ✅ Complete |
| [v0.2.3d](v0.2.x/LCS-CL-023d.md) | Violation Aggregator | ✅ Complete |

---

## [v0.2.1] - 2026-01 (In Progress)

### The Rulebook (Style Module)

This release introduces **The Rulebook**, the Style Module that enables governed writing environments with consistent style enforcement.

#### What's New

- **Style Module Foundation** — New `Lexichord.Modules.Style` project establishing the infrastructure for style checking. Implements `IModule` with "The Rulebook" identity and proper service registration.

- **IStyleEngine Interface** — Core contract for style analysis. Features active style sheet management, thread-safe analysis methods, and event-driven sheet change notifications.

- **Rule Object Model** — Full domain object implementations for style governance:
    - `RuleCategory` enum: Terminology, Formatting, Syntax
    - `ViolationSeverity` enum: Error, Warning, Info, Hint (ordered by importance)
    - `PatternType` enum: Regex, Literal, LiteralIgnoreCase, StartsWith, EndsWith, Contains
    - `StyleRule` record with `FindViolationsAsync()`, lazy regex compilation (100ms timeout), position calculation
    - `StyleViolation` record with line/column positions, `GetSurroundingContext()` for match preview
    - `StyleSheet` record with `MergeWith()` for rule inheritance, filtering methods, and `Empty` singleton

- **YAML Deserializer** — Full YamlDotNet integration for loading style sheets:
    - YAML DTOs with snake_case to PascalCase mapping
    - Schema validation with helpful error messages (required fields, kebab-case IDs, enum validation)
    - Embedded `lexichord.yaml` with 26 default rules across Terminology, Formatting, and Syntax categories
    - `ValidateYaml()` for editor-side syntax checking before save

- **Configuration Watcher** — Live reload of `.lexichord/style.yaml` without restart:
    - `FileSystemWatcher` with 300ms debouncing for rapid save handling
    - License-gated to WriterPro tier and above
    - Graceful fallback keeps previous valid rules on YAML errors
    - MediatR events (`StyleSheetReloadedEvent`, `StyleWatcherErrorEvent`) for system-wide notification

#### Philosophy: Concordance

> "Rules over improvisation"

The Rulebook provides governed writing environments where style guides are applied consistently across all content. Writers focus on content while the system enforces house style.

#### Sub-Part Changelogs

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.2.1a](v0.2.x/LCS-CL-021a.md) | Module Scaffolding | ✅ Complete |
| [v0.2.1b](v0.2.x/LCS-CL-021b.md) | Rule Object Model  | ✅ Complete |
| [v0.2.1c](v0.2.x/LCS-CL-021c.md) | YAML Deserializer  | ✅ Complete |
| [v0.2.1d](v0.2.x/LCS-CL-021d.md) | Hot Reload         | ✅ Complete |

---

## [v0.1.7] - 2026-01 (In Progress)

### Distribution & Updates

This release integrates Velopack for native platform packaging and auto-updates.

#### What's New

- **Velopack Integration** — Native auto-update framework replacing stub implementation. Supports delta updates for efficient downloads, channel-specific update feeds (Stable/Insider), and in-app update installation with automatic restart.

- **Update Service Enhancements** — Extended `IUpdateService` with real download/apply functionality including progress reporting, ready-state tracking, and platform-aware behavior (skips updates in development mode).

- **Build Scripts** — Cross-platform packaging scripts (`pack-windows.ps1`, `pack-macos.sh`) for creating distributable installers via Velopack CLI.

- **Signing Infrastructure** — CI pipeline for code signing Windows executables (PFX/SignTool) and macOS applications (Developer ID/Notarization). Signed binaries avoid SmartScreen and Gatekeeper security warnings.

- **Release Notes Viewer** — Automatic display of `CHANGELOG.md` after updates. First-run detection compares stored vs. current version, opening release notes in a new editor tab. Settings persist to track run history and installation ID.

- **Telemetry Hooks** — Opt-in crash reporting via Sentry SDK integration. User-configurable toggle in Settings > Privacy with clear data disclosure. Robust PII scrubbing ensures file paths and email addresses are never transmitted. Disabled by default until explicitly enabled.

#### Sub-Part Changelogs

| Version                          | Title                  | Status      |
| -------------------------------- | ---------------------- | ----------- |
| [v0.1.7a](v0.1.x/LCS-CL-017a.md) | Velopack Integration   | ✅ Complete |
| [v0.1.7b](v0.1.x/LCS-CL-017b.md) | Signing Infrastructure | ✅ Complete |
| [v0.1.7c](v0.1.x/LCS-CL-017c.md) | Release Notes Viewer   | ✅ Complete |
| [v0.1.7d](v0.1.x/LCS-CL-017d.md) | Telemetry Hooks        | ✅ Complete |

---

## [v0.1.6] - 2026-01 (In Progress)

### The Settings Framework

This release establishes the centralized settings UI framework, enabling modules to contribute configuration pages to a unified Settings dialog, with live theme preview as the first module-contributed settings page.

#### What's New

- **Settings Page Registry** — Central registry for module-contributed settings pages with hierarchical organization, license tier filtering, and full-text search capabilities.

- **Settings ViewModel** — State management for the Settings dialog with tree navigation, search filtering, error handling for page loading failures, and MediatR event integration.

- **Settings Window** — Modal dialog with split-panel layout (220px navigation tree, flexible content area), search box, TreeView navigation, and keyboard shortcuts (Ctrl+,, Escape).

- **Live Theme Preview** — Real-time theme switching from the Appearance settings page. Select Light, Dark, or System themes with instant visual feedback. OS theme synchronization for System mode.

- **License Management UI** — Account settings page for license activation and tier management. Three-stage validation (format, checksum, server), secure key storage via `ISecureVault`, tier-colored badges, expiration warnings, and feature availability matrix.

- **Update Channel Selector** — Updates settings page for switching between Stable and Insider channels. Assembly-driven version display, manual update checking (stub), and local settings persistence.

- **Module Integration** — Modules implement `ISettingsPage` interface and register via `ISettingsPageRegistry` during initialization. Pages support hierarchical nesting via `ParentCategoryId`.

#### Sub-Part Changelogs

| Version                   | Title                     | Status      |
| ------------------------- | ------------------------- | ----------- |
| [v0.1.6a](LCS-CL-016a.md) | Settings Dialog Framework | ✅ Complete |
| [v0.1.6b](LCS-CL-016b.md) | Live Theme Preview        | ✅ Complete |
| [v0.1.6c](LCS-CL-016c.md) | License Management UI     | ✅ Complete |
| [v0.1.6d](LCS-CL-016d.md) | Update Channel Selector   | ✅ Complete |

---

## [v0.1.5] - 2026-01 (In Progress)

### The Conductor's Baton (Command Palette)

This release establishes the keyboard-centric command system that enables rapid access to all application functionality through commands, shortcuts, and a searchable palette.

#### What's New

- **Command Registry** — Centralized system for registering, querying, and executing application commands. Commands use `module.action` naming convention with metadata for display titles, categories, icons, and keyboard shortcuts.

- **Command Palette UI** — Keyboard-centric modal overlay for discovering and executing commands. Features fuzzy search via FuzzySharp, keyboard navigation (↑/↓, Page Up/Down, Home/End), and mode switching between Commands and Files. Shortcuts: Ctrl+Shift+P (commands), Ctrl+P (files).

- **File Jumper** — Workspace file indexing service enabling fast file search via the Command Palette. Features background indexing with progress reporting, fuzzy file name matching, LRU-based recent files tracking, and incremental updates from file system watcher events. Configurable ignore patterns and binary file filtering.

- **Keybinding Service** — User-customizable keyboard shortcuts for commands with JSON-based configuration persistence. Features conflict detection with context-aware filtering, file watching for hot-reload, and platform-agnostic gesture processing. Gestures like Ctrl+S, Ctrl+Shift+P are routed to commands registered in the Command Registry.

#### Sub-Part Changelogs

| Version                          | Title              | Status      |
| -------------------------------- | ------------------ | ----------- |
| [v0.1.5a](LCS-CL-015a.md)        | Command Registry   | ✅ Complete |
| [v0.1.5b](LCS-CL-015b.md)        | Command Palette UI | ✅ Complete |
| [v0.1.5c](v0.1.x/LCS-CL-015c.md) | File Jumper        | ✅ Complete |
| [v0.1.5d](LCS-CL-015d.md)        | Keybinding Service | ✅ Complete |

---

## [v0.1.4] - 2026-01 (In Progress)

### Scribe (IO & Persistence)

This release establishes file I/O and document persistence capabilities, starting with dirty state tracking and atomic saves.

#### What's New

- **Dirty State Tracking** — Visual "\*" indicator in tab titles when documents have unsaved changes. Debounced updates (50ms) prevent excessive state changes during rapid typing. Content hashing enables detection when undo returns to saved state.

- **Atomic Saves** — Corruption-proof file saves using Write-Temp-Delete-Rename strategy. Three-phase execution ensures original file is preserved on any failure. Includes encoding detection, error recovery, and MediatR events for save success/failure.

- **Safe Close Workflow** — Window close interception prompts users to save unsaved changes. Modal dialog offers Save All, Discard All, or Cancel options. Prevents accidental data loss when closing with dirty documents.

- **Recent Files History** — MRU (Most Recently Used) file tracking with database persistence. Files automatically tracked when opened and displayed in "File > Open Recent" menu. Missing files shown as disabled with option to prune.

#### Sub-Part Changelogs

| Version                   | Title                | Status      |
| ------------------------- | -------------------- | ----------- |
| [v0.1.4a](LCS-CL-014a.md) | Dirty State Tracking | ✅ Complete |
| [v0.1.4b](LCS-CL-014b.md) | Atomic Saves         | ✅ Complete |
| [v0.1.4c](LCS-CL-014c.md) | Safe Close Workflow  | ✅ Complete |
| [v0.1.4d](LCS-CL-014d.md) | Recent Files History | ✅ Complete |

---

## [v0.1.3] - 2026-01 (In Progress)

### The Manuscript (Editor Module)

This release establishes the core text editing infrastructure using AvaloniaEdit, providing a high-performance editor foundation for manuscript creation and editing.

#### What's New

- **AvaloniaEdit Integration** — High-performance text editor based on AvaloniaEdit 11.1.0, supporting large documents with syntax highlighting.

- **Syntax Highlighting Service** — Theme-aware syntax highlighting for Markdown, JSON, YAML, and XML with automatic theme adaptation. XSHD definitions loaded from embedded resources with light/dark variants.

- **Search & Replace Overlay** — Inline search and replace with live highlighting, match navigation (F3/Shift+F3), and replace all. Supports case-sensitive, whole word, and regex modes with ReDoS protection.

- **ManuscriptViewModel** — Document ViewModel extending `DocumentViewModelBase` with full editor state management including caret position, selection tracking, and document statistics.

- **EditorService** — Document lifecycle management with open/create/save/close operations, thread-safe document tracking, and file path deduplication.

- **Editor Configuration** — Configurable editor settings including font family, font size, line numbers, word wrap, and whitespace display with JSON file persistence. Font fallback chain resolves missing fonts automatically.

- **Ctrl+Scroll Zoom** — Real-time font size adjustment via Ctrl+Mouse Wheel with debounced persistence. Keyboard shortcuts: Ctrl+0 (reset), Ctrl++/- (zoom in/out).

- **Keyboard Shortcuts** — Ctrl+S (save), Ctrl+F (search overlay), Ctrl+H (toggle replace), F3/Shift+F3 (find next/previous), Escape (hide search), Ctrl+0/+/- (zoom).

#### Why This Matters

This work enables:

- Professional-quality text editing experience for technical writers
- Syntax highlighting for common technical writing formats
- Foundation for search and other editor features
- Live document statistics (line/word/character counts) for writers
- Customizable editor appearance with persistent settings

#### Sub-Part Changelogs

| Version                   | Title                            | Status      |
| ------------------------- | -------------------------------- | ----------- |
| [v0.1.3a](LCS-CL-013a.md) | AvalonEdit Integration           | ✅ Complete |
| [v0.1.3b](LCS-CL-013b.md) | Syntax Highlighting              | ✅ Complete |
| [v0.1.3c](LCS-CL-013c.md) | Search Overlay                   | ✅ Complete |
| [v0.1.3d](LCS-CL-013d.md) | Editor Configuration Persistence | ✅ Complete |

---

## [v0.1.2] - 2026-01-28 (In Progress)

### The Explorer (Project Management)

This release establishes workspace and project management capabilities, enabling Lexichord to open folders and provide file system awareness.

#### What's New

- **Workspace Service** — Core service managing the current open folder state. Tracks active workspace path, persists recent workspaces (up to 10), and coordinates workspace lifecycle.

- **Workspace Events** — MediatR notifications (`WorkspaceOpenedEvent`, `WorkspaceClosedEvent`) enable cross-module reactions to workspace changes without direct dependencies.

- **Path Validation** — `WorkspaceInfo.ContainsPath()` provides security-aware path validation, preventing directory traversal attacks when filtering files.

- **Robust File System Watcher** — Production-ready file change detection with:
    - **Debouncing** — Accumulates rapid changes (default 100ms) to prevent event storms
    - **Batching** — Emits grouped changes as single events for efficiency
    - **Ignore Patterns** — Configurable glob patterns to filter `.git`, `node_modules`, temp files, etc.
    - **Buffer Overflow Detection** — Signals need for directory rescan when OS buffer overflows
    - **Error Recovery** — Automatic restart attempts (up to 3) on watcher failures

- **External File Changes Event** — `ExternalFileChangesEvent` MediatR notification enables modules to react to file system changes detected by the watcher.

- **Tree View UI (Project Explorer)** — Hierarchical file browser with:
    - **Material Design Icons** — File type icons based on extension (programming, documents, etc.)
    - **Lazy Loading** — Directory contents loaded on-demand when expanded
    - **Smart Sorting** — Directories before files, alphabetical within each category
    - **Live Updates** — Tree updates automatically when external file changes are detected
    - **MediatR Integration** — Publishes `FileOpenRequestedEvent` when files are opened

- **Context Menu Actions** — Full file and folder management in the Project Explorer:
    - **New File/Folder** — Create new items with inline name editing (Ctrl+N, Ctrl+Shift+N)
    - **Rename** — Inline renaming with validation (F2)
    - **Delete** — Remove files/folders with recursive support
    - **Reveal in Explorer** — Open containing folder in OS file browser (Windows/macOS/Linux)
    - **Protected Paths** — Prevents modification of `.git` folders and workspace root
    - **Name Validation** — Blocks invalid characters, reserved names, and path traversal

#### Why This Matters

This work enables:

1. **Project Context** — The application can track which project folder is open, enabling file-aware features.
2. **Recent Projects** — Users can quickly reopen previously used workspaces.
3. **Cross-Module Coordination** — Other modules can react to workspace and file changes via MediatR events.
4. **Reliable File Watching** — External edits (git checkout, npm install, IDE refactors) are detected reliably without event storms.
5. **File Management** — Users can create, rename, and delete files/folders without leaving the IDE.

> [!NOTE]
> For detailed technical changes, see [LCS-CL-012a.md](./LCS-CL-012a.md), [LCS-CL-012b.md](./LCS-CL-012b.md), [LCS-CL-012c.md](./LCS-CL-012c.md), and [LCS-CL-012d.md](./LCS-CL-012d.md).

---

## [v0.1.1] - 2026-01-28 (In Progress)

### The Layout Engine (Docking System)

This release establishes a flexible, IDE-like docking system using `Dock.Avalonia`. Modules can contribute documents and tool panes to resizable, draggable regions.

#### What's New

- **Dock Library Integration** — Integrated `Dock.Avalonia` to provide resizable splitters and draggable panels in the main window. Replaced static grid layout with a 4-region dock system (Left, Center, Right, Bottom).

- **Layout Abstractions** — Created `IDockFactory`, `IDocument`, `ITool`, and `DockRegionConfig` interfaces that decouple modules from the docking library implementation.

- **Region Injection Service** — Added `IRegionManager` service enabling modules to inject tool panes and documents into dock regions without direct Dock.Avalonia dependency. Includes MediatR integration for region change notifications.

- **Layout Serialization** — JSON-based layout persistence enables saving/loading workspace arrangements. Features named profiles, auto-save with debouncing, atomic file writes, and schema versioning for future migrations.

- **Tab Infrastructure** — Core abstractions for document tabs including `IDocumentTab`, `DocumentViewModelBase`, and `ITabService`. Features dirty state tracking with visual indicators, pinning with automatic tab reordering, save confirmation dialogs, and a standard IDE context menu. MediatR integration enables lifecycle notifications for close, pin, and dirty state changes.

- **MainWindowViewModel** — New ViewModel managing the dock layout lifecycle with proper DI integration.

#### Why This Matters

This work enables:

1. **Flexible Layouts** — Users can resize, drag, and rearrange panels to customize their workspace.
2. **Module Panel Registration** — Modules can contribute tool panes and documents without knowledge of the docking library.
3. **Document Management** — Documents can track unsaved changes, prompt for save on close, and respect pinned state.

> [!NOTE]
> For detailed technical changes, see [LCS-CL-011a.md](./LCS-CL-011a.md), [LCS-CL-011b.md](./LCS-CL-011b.md), [LCS-CL-011c.md](./LCS-CL-011c.md), and [LCS-CL-011d.md](./LCS-CL-011d.md).

---

## [v0.0.8] - 2026-01-29 (In Progress)

### The Hello World (Golden Skeleton)

This release establishes the first feature module as a reference implementation for all future Lexichord modules. The Status Bar module demonstrates the complete module lifecycle and introduces the Shell Region system for decoupled UI composition.

#### What's New

- **Shell Region Infrastructure** — Modules can now contribute UI to host window regions (Top, Left, Center, Right, Bottom) via `IShellRegionView` without direct host dependencies.

- **Status Bar Module** — The canonical "Golden Skeleton" reference implementation demonstrating proper module structure, service registration, and async initialization patterns.

- **Database Health Monitoring** — SQLite-based health tracking with `HealthRepository` for uptime tracking and `HeartbeatService` for 60-second heartbeat recording. Includes staleness detection and `SystemHealthChangedEvent` for cross-module health updates.

- **Vault Status Tracking** — Secure vault integration with `VaultStatusService` that verifies API key presence, handles platform support detection, and provides "Ready", "No Key", "Error", or "N/A" status display. Includes `ApiKeyDialog` for user key entry.

#### Why This Matters

This work enables:

1. **Module Development Pattern** — Future module developers have a complete working example to follow.
2. **Decoupled UI** — Modules contribute views without knowing about the host layout.
3. **End-to-End Validation** — Proves the module system works from discovery to rendering.

---

## [v0.0.7] - 2026-01-28 (In Progress)

### The Event Bus (Communication)

This release establishes MediatR-based in-process messaging that enables loose coupling between Lexichord modules. Commands, queries, and domain events now flow through a central mediator with pipeline behaviors for cross-cutting concerns.

#### What's New

- **MediatR Bootstrap** — Core messaging infrastructure with IMediator registration, assembly scanning for handler discovery, and CQRS marker interfaces (ICommand, IQuery, IDomainEvent).

- **Shared Domain Events** — DomainEventBase, ContentCreatedEvent, and SettingsChangedEvent enable modules to publish and subscribe to state changes without direct dependencies.

- **Logging Pipeline Behavior** — Automatic request/response logging with timing, slow request warnings, sensitive data redaction (`[SensitiveData]`, `[NoLog]`), and configurable thresholds.

- **Validation Pipeline Behavior** — FluentValidation integration with automatic request validation, structured error aggregation (`ValidationException`, `ValidationError`), and validator auto-discovery.

#### Why This Matters

This work enables:

1. **Loose Coupling** — Modules communicate via messages, not direct dependencies.
2. **CQRS Pattern** — Clear separation between commands (writes) and queries (reads).
3. **Extensibility** — Pipeline behaviors enable cross-cutting concerns (logging, validation).
4. **Input Validation** — Commands are validated before handlers execute, with structured error responses.

---

## [v0.0.6] - 2026-01-28 (In Progress)

### The Vault (Secure Secrets Storage)

This release establishes the secure secrets management infrastructure that protects sensitive credentials. Lexichord can now store API keys, connection strings, and OAuth tokens using OS-native encryption.

#### What's New

- **ISecureVault Interface** — Platform-agnostic contract for secure secret storage with CRUD operations, metadata access, and streaming key listing.

- **WindowsSecureVault (DPAPI)** — Windows-specific implementation using Data Protection API with user-scoped encryption, per-installation entropy, and secure file-based storage.

- **UnixSecureVault (libsecret/AES-256)** — Linux and macOS implementation with libsecret integration and robust AES-256-GCM file-based encryption fallback.

- **Integration Testing** — Comprehensive test suite verifying secrets survive restarts, CRUD lifecycle, metadata accuracy, and platform factory selection.

#### Why This Matters

This work enables:

1. **Security** — Credentials encrypted at rest using platform-native APIs.
2. **Cross-Platform** — Windows DPAPI, Linux/macOS libsecret support.
3. **Foundation** — Enables secure LLM API key storage (v0.3.x+).

---

## [v0.0.5] - 2026-01-28 (In Progress)

### The Memory (Data Layer)

This release establishes the persistent data layer that gives Lexichord its "memory." The application can now store and retrieve data across sessions using PostgreSQL.

#### What's New

- **Docker Orchestration** — A reproducible PostgreSQL 16 development environment via Docker Compose with health checks, data persistence, and optional pgAdmin administration UI.

- **Database Connector** — Npgsql-based connectivity with connection pooling and Polly resilience patterns (retry with exponential backoff, circuit breaker).

- **FluentMigrator Runner** — Database schema versioning with CLI commands (`--migrate`, `--migrate:down`, `--migrate:list`) and the initial `Migration_001_InitSystem` creating Users and SystemSettings tables.

- **Repository Base** — Generic repository pattern with Dapper for type-safe CRUD operations, including entity-specific repositories for Users and SystemSettings plus Unit of Work for transactions.

#### Why This Matters

This work enables:

1. **Data Persistence** — User data and settings survive application restarts.
2. **Schema Evolution** — Migrations enable safe database updates.
3. **Foundation** — Enables all data-dependent features (v0.0.6+).

---

## [v0.0.4] - 2026-01-28 (In Progress)

### The Module Protocol

This release establishes the module architecture that enables Lexichord's "modular monolith" — allowing features to be developed as independent modules while running in a single process.

#### What's New

- **Module Contract (`IModule`)** — A standardized interface defining how modules register services, initialize, and expose metadata.

- **Module Metadata (`ModuleInfo`)** — Immutable record containing module identity, version, author, and optional dependencies.

- **Module Loader (`IModuleLoader`)** — Discovers and loads modules from `./Modules/` at startup with two-phase loading (service registration before DI build, initialization after).

- **License Gate (Skeleton)** — Establishes license tier gating with `LicenseTier`, `ILicenseContext`, and `RequiresLicenseAttribute`. Stub implementation returns Core tier; v1.x will provide real validation.

- **Sandbox Module (Proof of Concept)** — First feature module validating the module architecture. Demonstrates discovery, service registration, and async initialization.

#### Why This Matters

This work enables:

1. **Extensibility** — Third-party and internal modules can be developed independently.
2. **Feature Isolation** — Each module encapsulates its own services and state.
3. **License Gating** — Future versions will enable per-module licensing tiers.

---

## [v0.0.3] - 2026-01-28 (In Progress)

### The Nervous System (Logging & DI)

This release establishes the runtime infrastructure that enables the application to "think" and report errors. This transforms the Avalonia shell into a properly instrumented, dependency-injectable application.

#### What's New

- **Dependency Injection Container** — Microsoft.Extensions.DependencyInjection is now the sole IoC container. All services are resolved from the DI container instead of manual instantiation.

- **Structured Logging** — Serilog provides comprehensive logging with colorized console output for development and rolling file logs for production diagnostics.

#### Why This Matters

This work enables:

1. **Testability** — Services can now be mocked and isolated for unit testing.
2. **Module Foundation** — Module registration (v0.0.4) will build on this DI container.
3. **Flexibility** — Services can be swapped or decorated without modifying consuming code.

---

## [v0.0.2] - 2026-01-28 (In Progress)

### The Host Shell & UI Foundation

This release introduces the visual foundation of Lexichord — the Avalonia-based desktop application that will host all writing features.

#### What's New

- **Avalonia UI Framework Bootstrapped** — The application now launches as a proper desktop window instead of a console application. This establishes the foundation for all future UI work.

- **Podium Layout Shell** — The main window now features a structured layout with a top bar, navigation rail, content host, and status bar — the "Podium" where all future writing tools will perform.

- **Runtime Theme Switching** — Users can toggle between Dark and Light themes via the StatusBar button, with the application detecting and respecting OS preferences on first launch.

- **Window State Persistence** — Window position, size, maximized state, and theme preference are now remembered between sessions.

#### Why This Matters

This work establishes:

1. **Visual Identity** — Lexichord now has a window, title, and theme infrastructure for building the interface.
2. **Cross-Platform Support** — Avalonia enables Windows, macOS, and Linux deployment from a single codebase.
3. **Modern UI Patterns** — The architecture supports compiled bindings, resource dictionaries, and theme switching.

---

## [v0.0.1] - 2026-01-28

### The Architecture Skeleton

This release establishes the foundation of the Lexichord application. While no visible features exist yet, this work creates the structural backbone that all future features will build upon.

#### What's New

- **Project Structure Established** — The codebase now follows a clean "Modular Monolith" architecture that separates core application code from future plugins and extensions.

- **Code Quality Safeguards Added** — Build-time checks ensure code consistency across the project, catching potential issues before they become problems.

- **Automated Testing Infrastructure** — A comprehensive test suite framework is in place, enabling developers to verify changes don't break existing functionality.

- **Continuous Integration Pipeline** — Every push and pull request now triggers automated builds and tests, ensuring code quality before it reaches the main branch.

#### Why This Matters

This foundational work ensures:

1. **Maintainability** — Clean separation of concerns prevents "spaghetti code" as the project grows.
2. **Extensibility** — The plugin architecture (coming in v0.0.4) will build directly on this work.
3. **Quality Assurance** — Automated tests catch regressions before they reach users.
4. **Developer Confidence** — Clear structure and testing make it easier for contributors to add new features safely.
5. **Continuous Integration** — Automated pipelines validate every change, preventing broken code from entering the main branch.

---

## Version History

| Version | Date       | Codename                          | Summary                                                    |
| :------ | :--------- | :-------------------------------- | :--------------------------------------------------------- |
| v0.1.5  | 2026-01-29 | The Conductor's Baton (Commands)  | Command Registry, command execution and discovery          |
| v0.1.2  | 2026-01-28 | The Explorer (Project Management) | Workspace service, file system watcher, project context    |
| v0.1.1  | 2026-01-28 | The Layout Engine (Docking)       | Dock.Avalonia integration, region injection, serialization |
| v0.0.8  | 2026-01-29 | The Hello World (Golden Skeleton) | Shell regions, Status Bar module reference implementation  |
| v0.0.7  | 2026-01-28 | The Event Bus (Communication)     | MediatR bootstrap, CQRS interfaces, handler discovery      |
| v0.0.6  | 2026-01-28 | The Vault (Secure Storage)        | Secure secrets interface, metadata, exception hierarchy    |
| v0.0.5  | 2026-01-28 | The Memory (Data Layer)           | Docker orchestration, database connector, migrations       |
| v0.0.4  | 2026-01-28 | The Module Protocol               | Module contract, metadata, loader, license gating          |
| v0.0.3  | 2026-01-28 | The Nervous System                | Dependency Injection, Logging, Crash Handling, Config      |
| v0.0.2  | 2026-01-28 | The Host Shell & UI Foundation    | Avalonia bootstrap, window stub, theme infrastructure      |
| v0.0.1  | 2026-01-28 | The Architecture Skeleton         | Modular Monolith foundation, test infrastructure, CI/CD    |

---

## Changelog Format

Each major version has detailed technical changelogs organized by sub-part:

### v0.1.5 Sub-Parts

| Document                               | Sub-Part | Title              |
| :------------------------------------- | :------- | :----------------- |
| [LCS-CL-015a](./LCS-CL-015a.md)        | v0.1.5a  | Command Registry   |
| [LCS-CL-015b](./LCS-CL-015b.md)        | v0.1.5b  | Command Palette UI |
| [LCS-CL-015c](./v0.1.x/LCS-CL-015c.md) | v0.1.5c  | File Jumper        |
| [LCS-CL-015d](./LCS-CL-015d.md)        | v0.1.5d  | Keybinding Service |

### v0.1.2 Sub-Parts

| Document                        | Sub-Part | Title               |
| :------------------------------ | :------- | :------------------ |
| [LCS-CL-012a](./LCS-CL-012a.md) | v0.1.2a  | Workspace Service   |
| [LCS-CL-012b](./LCS-CL-012b.md) | v0.1.2b  | File System Watcher |
| [LCS-CL-012c](./LCS-CL-012c.md) | v0.1.2c  | Tree View UI        |

### v0.1.1 Sub-Parts

| Document                        | Sub-Part | Title                    |
| :------------------------------ | :------- | :----------------------- |
| [LCS-CL-011a](./LCS-CL-011a.md) | v0.1.1a  | Dock Library Integration |
| [LCS-CL-011b](./LCS-CL-011b.md) | v0.1.1b  | Region Injection Service |
| [LCS-CL-011c](./LCS-CL-011c.md) | v0.1.1c  | Layout Serialization     |

### v0.0.8 Sub-Parts

| Document                        | Sub-Part | Title                   |
| :------------------------------ | :------- | :---------------------- |
| [LCS-CL-008a](./LCS-CL-008a.md) | v0.0.8a  | Status Bar Module       |
| [LCS-CL-008b](./LCS-CL-008b.md) | v0.0.8b  | Database Health Check   |
| [LCS-CL-008c](./LCS-CL-008c.md) | v0.0.8c  | Secure Vault Check      |
| [LCS-CL-008d](./LCS-CL-008d.md) | v0.0.8d  | Golden Skeleton Release |

### v0.0.7 Sub-Parts

| Document                        | Sub-Part | Title                        |
| :------------------------------ | :------- | :--------------------------- |
| [LCS-CL-007a](./LCS-CL-007a.md) | v0.0.7a  | MediatR Bootstrap            |
| [LCS-CL-007b](./LCS-CL-007b.md) | v0.0.7b  | Shared Domain Events         |
| [LCS-CL-007c](./LCS-CL-007c.md) | v0.0.7c  | Logging Pipeline Behavior    |
| [LCS-CL-007d](./LCS-CL-007d.md) | v0.0.7d  | Validation Pipeline Behavior |

### v0.0.6 Sub-Parts

| Document                        | Sub-Part | Title                               |
| :------------------------------ | :------- | :---------------------------------- |
| [LCS-CL-006a](./LCS-CL-006a.md) | v0.0.6a  | ISecureVault Interface              |
| [LCS-CL-006b](./LCS-CL-006b.md) | v0.0.6b  | WindowsSecureVault (DPAPI)          |
| [LCS-CL-006c](./LCS-CL-006c.md) | v0.0.6c  | UnixSecureVault (libsecret/AES-256) |
| [LCS-CL-006d](./LCS-CL-006d.md) | v0.0.6d  | Integration Testing                 |

### v0.0.5 Sub-Parts

| Document                        | Sub-Part | Title                 |
| :------------------------------ | :------- | :-------------------- |
| [LCS-CL-005a](./LCS-CL-005a.md) | v0.0.5a  | Docker Orchestration  |
| [LCS-CL-005b](./LCS-CL-005b.md) | v0.0.5b  | Database Connector    |
| [LCS-CL-005c](./LCS-CL-005c.md) | v0.0.5c  | FluentMigrator Runner |
| [LCS-CL-005d](./LCS-CL-005d.md) | v0.0.5d  | Repository Base       |

### v0.0.4 Sub-Parts

| Document                        | Sub-Part | Title             |
| :------------------------------ | :------- | :---------------- |
| [LCS-CL-004a](./LCS-CL-004a.md) | v0.0.4a  | IModule Interface |

### v0.0.3 Sub-Parts

| Document                        | Sub-Part | Title                     |
| :------------------------------ | :------- | :------------------------ |
| [LCS-CL-003a](./LCS-CL-003a.md) | v0.0.3a  | Dependency Injection Root |
| [LCS-CL-003b](./LCS-CL-003b.md) | v0.0.3b  | Serilog Pipeline          |
| [LCS-CL-003c](./LCS-CL-003c.md) | v0.0.3c  | Global Exception Trap     |
| [LCS-CL-003d](./LCS-CL-003d.md) | v0.0.3d  | Configuration Service     |

### v0.0.2 Sub-Parts

| Document                        | Sub-Part | Title                    |
| :------------------------------ | :------- | :----------------------- |
| [LCS-CL-002a](./LCS-CL-002a.md) | v0.0.2a  | Avalonia Bootstrap       |
| [LCS-CL-002b](./LCS-CL-002b.md) | v0.0.2b  | Podium Layout            |
| [LCS-CL-002c](./LCS-CL-002c.md) | v0.0.2c  | Runtime Theme Switching  |
| [LCS-CL-002d](./LCS-CL-002d.md) | v0.0.2d  | Window State Persistence |

### v0.0.1 Sub-Parts

| Document                        | Sub-Part | Title                           |
| :------------------------------ | :------- | :------------------------------ |
| [LCS-CL-001a](./LCS-CL-001a.md) | v0.0.1a  | Solution Scaffolding            |
| [LCS-CL-001b](./LCS-CL-001b.md) | v0.0.1b  | Dependency Graph Enforcement    |
| [LCS-CL-001c](./LCS-CL-001c.md) | v0.0.1c  | Test Suite Foundation           |
| [LCS-CL-001d](./LCS-CL-001d.md) | v0.0.1d  | Continuous Integration Pipeline |

Individual sub-part changelogs provide implementation-level detail for developers.
