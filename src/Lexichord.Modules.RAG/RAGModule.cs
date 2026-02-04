// =============================================================================
// File: RAGModule.cs
// Project: Lexichord.Modules.RAG
// Description: Module registration for the RAG subsystem.
// =============================================================================
// LOGIC: Registers all RAG-related services and initializes the vector type handler.
//   - VectorTypeHandler must be registered before any repository operations.
//   - DocumentRepository and ChunkRepository are scoped for per-request lifecycle.
//   - IngestionQueue and IngestionBackgroundService handle queued file processing (v0.4.2d).
//   - Embedding services and document indexing pipeline (v0.4.4).
//   - Semantic search service with license gating (v0.4.5).
// =============================================================================

using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Services;
using Lexichord.Modules.RAG.Chunking;
using Lexichord.Modules.RAG.Configuration;
using Lexichord.Modules.RAG.Data;
using Lexichord.Modules.RAG.Embedding;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Modules.RAG.Search;
using Lexichord.Modules.RAG.Services;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Lexichord.Modules.RAG;


/// <summary>
/// Module registration for the RAG (Retrieval-Augmented Generation) subsystem.
/// </summary>
/// <remarks>
/// <para>
/// The RAG module provides semantic search capabilities for the Lexichord platform,
/// enabling context-aware writing assistance through vector similarity matching.
/// </para>
/// <para>
/// <b>Components:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="VectorTypeHandler"/>: Dapper type mapping for pgvector.</description></item>
///   <item><description><see cref="DocumentRepository"/>: Document CRUD operations.</description></item>
///   <item><description><see cref="ChunkRepository"/>: Chunk storage and vector search.</description></item>
///   <item><description><see cref="FileWatcherIngestionHandler"/>: File change detection for auto-indexing (v0.4.2c).</description></item>
///   <item><description><see cref="IngestionQueue"/>: Priority-based processing queue for file ingestion (v0.4.2d).</description></item>
///   <item><description><see cref="IngestionBackgroundService"/>: Background processor for the ingestion queue (v0.4.2d).</description></item>
///   <item><description><see cref="TiktokenTokenCounter"/>: Token counting using tiktoken encoding (v0.4.4c).</description></item>
///   <item><description><see cref="OpenAIEmbeddingService"/>: OpenAI embeddings with Polly retry (v0.4.4b).</description></item>
///   <item><description><see cref="ChunkingStrategyFactory"/>: Strategy selection for document chunking (v0.4.4d).</description></item>
///   <item><description><see cref="DocumentIndexingPipeline"/>: Full indexing workflow orchestration (v0.4.4d).</description></item>
///   <item><description><see cref="SearchLicenseGuard"/>: License validation for semantic search (v0.4.5b).</description></item>
///   <item><description><see cref="PgVectorSearchService"/>: pgvector cosine similarity search (v0.4.5b).</description></item>
/// </list>
/// <para>
/// <b>Database Requirements:</b> The PostgreSQL database must have the pgvector
/// extension enabled and the schema migrations from v0.4.1b applied.
/// </para>
/// </remarks>
public sealed class RAGModule : IModule
{
    private ILogger<RAGModule>? _logger;

    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "rag",
        Name: "RAG Subsystem",
        Version: new Version(0, 5, 6),
        Author: "Lexichord Team",
        Description: "Retrieval-Augmented Generation subsystem for semantic search, query understanding, and context-aware assistance"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// <b>CRITICAL:</b> The <see cref="VectorTypeHandler"/> is registered with Dapper's
    /// <see cref="SqlMapper"/> during service registration. This is a static operation
    /// that affects all Dapper queries globally and must happen before any repository
    /// operations.
    /// </para>
    /// <para>
    /// Repositories are registered as scoped services to align with database connection
    /// lifecycle patterns (one connection per unit of work).
    /// </para>
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // CRITICAL: Register the vector type handler with Dapper's static mapper.
        // This must happen before any repository operations that involve vector columns.
        SqlMapper.AddTypeHandler(new VectorTypeHandler());

        // LOGIC: Register repositories as scoped to match connection-per-request pattern.
        // IDbConnectionFactory is expected to be registered by Infrastructure.
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IChunkRepository, ChunkRepository>();

        // LOGIC: Register FileHashService as singleton - it is stateless and thread-safe.
        // Used for hash-based change detection in the ingestion pipeline (v0.4.2b).
        services.AddSingleton<IFileHashService, FileHashService>();

        // LOGIC: Configure FileWatcherOptions with defaults using Options pattern.
        // The defaults are set via ConfigureOptions which allows proper IOptions<T> injection.
        // These can be overridden via configuration binding in the host.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(FileWatcherOptions.Default));

        // LOGIC: Register FileWatcherIngestionHandler as singleton.
        // It subscribes to ExternalFileChangesEvent and publishes FileIndexingRequestedEvent.
        // Singleton ensures a single debounce state across the application lifetime (v0.4.2c).
        // Also register as INotificationHandler for MediatR discovery.
        services.AddSingleton<FileWatcherIngestionHandler>();
        services.AddSingleton<MediatR.INotificationHandler<Lexichord.Abstractions.Events.ExternalFileChangesEvent>>(
            sp => sp.GetRequiredService<FileWatcherIngestionHandler>());

        // LOGIC: Configure IngestionQueueOptions with defaults using Options pattern.
        // The defaults are set via Options.Create which allows proper IOptions<T> injection.
        // These can be overridden via configuration binding in the host (v0.4.2d).
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(IngestionQueueOptions.Default));

        // LOGIC: Register IngestionQueue as singleton.
        // The queue must be singleton to maintain state across the application lifetime.
        // It handles priority ordering, duplicate detection, and capacity management (v0.4.2d).
        services.AddSingleton<IIngestionQueue, IngestionQueue>();

        // LOGIC: Register IngestionBackgroundService as a hosted service.
        // It continuously processes items from the queue using IIngestionService (v0.4.2d).
        // Note: IIngestionService itself is registered by downstream consumers (e.g., v0.4.2e).
        services.AddHostedService<IngestionBackgroundService>();

        // LOGIC: Register FixedSizeChunkingStrategy as singleton - it is stateless and thread-safe.
        // This is the foundational chunking algorithm for the RAG pipeline (v0.4.3b).
        services.AddSingleton<FixedSizeChunkingStrategy>();

        // LOGIC: Register ParagraphChunkingStrategy as singleton - it is stateless and thread-safe.
        // This strategy splits on paragraph boundaries and uses FixedSizeChunkingStrategy as fallback (v0.4.3c).
        services.AddSingleton<ParagraphChunkingStrategy>();

        // LOGIC: Register MarkdownHeaderChunkingStrategy as singleton - it is stateless and thread-safe.
        // This strategy splits on Markdown header boundaries with ParagraphChunkingStrategy as
        // fallback for no-header content and FixedSizeChunkingStrategy for oversized sections (v0.4.3d).
        services.AddSingleton<MarkdownHeaderChunkingStrategy>();

        // =============================================================================
        // v0.4.4: Embedding Infrastructure
        // =============================================================================

        // LOGIC: Configure EmbeddingOptions with defaults using Options pattern (v0.4.4a).
        // The defaults are set via Options.Create which allows proper IOptions<T> injection.
        // These can be overridden via configuration binding in the host.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(EmbeddingOptions.Default));

        // LOGIC: Register HttpClient for OpenAI API with default JSON accept header (v0.4.4b).
        // Using IHttpClientFactory for proper connection pooling and lifecycle management.
        services.AddHttpClient("OpenAI", client =>
        {
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        });

        // LOGIC: Register TiktokenTokenCounter as singleton - it is stateless and thread-safe (v0.4.4c).
        // Uses cl100k_base encoding for compatibility with OpenAI GPT-4 and embedding models.
        services.AddSingleton<TiktokenTokenCounter>();
        services.AddSingleton<ITokenCounter>(sp => sp.GetRequiredService<TiktokenTokenCounter>());

        // =============================================================================
        // v0.4.8d: Embedding Cache
        // =============================================================================

        // LOGIC: Configure EmbeddingCacheOptions with defaults using Options pattern (v0.4.8d).
        // Provides local SQLite-based caching of embeddings to reduce API costs and latency.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(EmbeddingCacheOptions.Default));

        // LOGIC: Register SqliteEmbeddingCache as singleton (v0.4.8d).
        // Provides local storage for embeddings with LRU eviction.
        services.AddSingleton<IEmbeddingCache, SqliteEmbeddingCache>();

        // LOGIC: Register OpenAIEmbeddingService as singleton (v0.4.4b).
        // Uses ISecureVault for API key retrieval and Polly for retry policies.
        // Thread-safe and stateless, suitable for singleton lifetime.
        services.AddSingleton<OpenAIEmbeddingService>();

        // LOGIC: Register CachedEmbeddingService as the IEmbeddingService (v0.4.8d).
        // Decorates OpenAIEmbeddingService with transparent caching via SqliteEmbeddingCache.
        services.AddSingleton<IEmbeddingService>(sp =>
        {
            var inner = sp.GetRequiredService<OpenAIEmbeddingService>();
            var cache = sp.GetRequiredService<IEmbeddingCache>();
            var options = sp.GetRequiredService<IOptions<EmbeddingCacheOptions>>();
            var logger = sp.GetRequiredService<ILogger<CachedEmbeddingService>>();
            return new CachedEmbeddingService(inner, cache, options, logger);
        });

        // LOGIC: Register ChunkingStrategyFactory as singleton (v0.4.4d).
        // Factory for selecting appropriate chunking strategy based on content or mode.
        services.AddSingleton<ChunkingStrategyFactory>();

        // LOGIC: Register DocumentIndexingPipeline as scoped (v0.4.4d).
        // The pipeline orchestrates chunking, embedding, and storage.
        // Scoped to align with repository lifetimes and database transaction boundaries.
        // v0.4.7b: Register via interface for testability.
        services.AddScoped<IDocumentIndexingPipeline, DocumentIndexingPipeline>();

        // =============================================================================
        // v0.4.5: Semantic Search (The Searcher)
        // =============================================================================

        // LOGIC: Register SearchLicenseGuard as singleton (v0.4.5b).
        // Thread-safe and stateless — reads license tier on each check via ILicenseContext.
        // Used by PgVectorSearchService to enforce WriterPro+ tier before search execution.
        services.AddSingleton<SearchLicenseGuard>();

        // LOGIC: v0.4.5c: IMemoryCache for query embedding caching.
        // AddMemoryCache() is idempotent — safe even if StyleModule also calls it.
        // Required by QueryPreprocessor for 5-minute sliding expiration cache.
        services.AddMemoryCache();

        // LOGIC: Register QueryPreprocessor as singleton (v0.4.5c).
        // Provides whitespace normalization, Unicode NFC normalization, optional
        // abbreviation expansion, and SHA256-based query embedding caching with
        // 5-minute sliding expiration via IMemoryCache.
        // Replaces PassthroughQueryPreprocessor from v0.4.5b.
        services.AddSingleton<IQueryPreprocessor, QueryPreprocessor>();

        // LOGIC: Register PgVectorSearchService as scoped (v0.4.5b).
        // Scoped to align with IDbConnectionFactory and repository lifetimes.
        // Executes pgvector cosine similarity search against indexed document chunks.
        services.AddScoped<ISemanticSearchService, PgVectorSearchService>();

        // =============================================================================
        // v0.5.1b: BM25 Search (Keyword Search)
        // =============================================================================

        // LOGIC: Register BM25SearchService as scoped (v0.5.1b).
        // Scoped to align with IDbConnectionFactory and repository lifetimes.
        // Executes PostgreSQL full-text search against indexed document chunks
        // using ts_rank() for BM25-style relevance ranking.
        services.AddScoped<IBM25SearchService, BM25SearchService>();

        // =============================================================================
        // v0.5.1c: Hybrid Search (Hybrid Fusion Algorithm)
        // =============================================================================

        // LOGIC: Configure HybridSearchOptions with defaults using Options pattern (v0.5.1c).
        // The defaults (SemanticWeight=0.7, BM25Weight=0.3, RRFConstant=60) are calibrated
        // for general-purpose retrieval. These can be overridden via configuration binding.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(HybridSearchOptions.Default));

        // LOGIC: Register HybridSearchService as scoped (v0.5.1c).
        // Scoped to align with ISemanticSearchService and IBM25SearchService lifetimes.
        // Executes both sub-searches in parallel and merges results via Reciprocal Rank Fusion.
        services.AddScoped<IHybridSearchService, HybridSearchService>();

        // =============================================================================
        // v0.4.6: Reference Panel (The Reference View)
        // =============================================================================

        // LOGIC: Register SearchHistoryService as singleton (v0.4.6a, enhanced v0.4.6d).
        // Thread-safe in-memory history for recent search queries with persistence support.
        // Used by ReferenceViewModel for autocomplete and history dropdown.
        // ISystemSettingsRepository provides persistence; if unavailable, operates in-memory only.
        services.AddSingleton<ISearchHistoryService>(sp =>
        {
            var logger = sp.GetRequiredService<ILogger<SearchHistoryService>>();
            var settingsRepository = sp.GetService<ISystemSettingsRepository>();
            var service = new SearchHistoryService(logger, settingsRepository, maxSize: 10);
            // Load history on startup (fire-and-forget)
            _ = service.LoadAsync();
            return service;
        });

        // LOGIC: Register ReferenceViewModel as transient (v0.4.6a, enhanced v0.5.1d).
        // Each panel instance gets its own ViewModel.
        // v0.5.1d: Constructor now requires ISemanticSearchService, IBM25SearchService,
        // IHybridSearchService, ILicenseContext, and optional ISystemSettingsRepository
        // for search mode toggle support. ISystemSettingsRepository may be null if
        // the Infrastructure module is not loaded (resolved via GetService).
        services.AddTransient<ViewModels.ReferenceViewModel>();

        // =============================================================================
        // v0.4.6c: Source Navigation
        // =============================================================================

        // LOGIC: Register ReferenceNavigationService as singleton (v0.4.6c).
        // Stateless service that bridges RAG search results with editor navigation.
        // Depends on IEditorService (v0.1.3a) and IEditorNavigationService (v0.2.6b).
        services.AddSingleton<IReferenceNavigationService, Services.ReferenceNavigationService>();

        // =============================================================================
        // v0.4.7a: Index Status View
        // =============================================================================

        // LOGIC: Register IndexStatusService as scoped (v0.4.7a).
        // Scoped to align with repository lifetimes for document and chunk queries.
        // Provides real-time visibility into document indexing status and statistics.
        services.AddScoped<IIndexStatusService, Services.IndexStatusService>();

        // LOGIC: Register IndexManagementService as scoped (v0.4.7b).
        // Scoped to align with repository and pipeline lifetimes.
        // Provides manual control over index operations: re-index, remove, re-index all.
        services.AddScoped<IIndexManagementService, Services.IndexManagementService>();

        // LOGIC: Register IndexStatusViewModel as transient (v0.4.7a).
        // Each settings page instance gets its own ViewModel.
        services.AddTransient<ViewModels.IndexStatusViewModel>();

        // LOGIC: Register IndexingProgressViewModel as transient (v0.4.7c).
        // Each progress toast instance gets its own ViewModel.
        // Also register as INotificationHandler for MediatR event subscription.
        services.AddTransient<ViewModels.IndexingProgressViewModel>();
        services.AddTransient<MediatR.INotificationHandler<Events.IndexingProgressUpdatedEvent>>(
            sp => sp.GetRequiredService<ViewModels.IndexingProgressViewModel>());

        // LOGIC: Register IndexStatusSettingsPage as singleton (v0.4.7a).
        // Settings page registration for the Index Status View in Settings dialog.
        // Registered as ISettingsPage for discovery by the settings page registry.
        services.AddSingleton<ISettingsPage, Settings.IndexStatusSettingsPage>();

        // =============================================================================
        // v0.5.2a: Citation Engine (Citation Model)
        // =============================================================================

        // LOGIC: Register CitationService as singleton (v0.5.2a).
        // Thread-safe and stateless — creates citations on-demand from search hits.
        // Uses IWorkspaceService for relative path resolution, ILicenseContext for
        // license gating at the formatting layer, and IMediator for event publishing.
        services.AddSingleton<ICitationService, CitationService>();

        // =============================================================================
        // v0.5.2b: Citation Engine (Citation Styles)
        // =============================================================================

        // LOGIC: Register the three built-in citation formatters as singletons (v0.5.2b).
        // Each formatter handles a single CitationStyle and is stateless/thread-safe.
        // They are collected by CitationFormatterRegistry via IEnumerable<ICitationFormatter>.
        services.AddSingleton<ICitationFormatter, Formatters.InlineCitationFormatter>();
        services.AddSingleton<ICitationFormatter, Formatters.FootnoteCitationFormatter>();
        services.AddSingleton<ICitationFormatter, Formatters.MarkdownCitationFormatter>();

        // LOGIC: Register CitationFormatterRegistry as singleton (v0.5.2b).
        // Provides style lookup, user preference management (via ISystemSettingsRepository),
        // and preferred formatter access for downstream consumers (v0.5.2d copy actions).
        services.AddSingleton<CitationFormatterRegistry>();

        // =============================================================================
        // v0.5.2c: Citation Engine (Stale Citation Detection)
        // =============================================================================

        // LOGIC: Register CitationValidator as singleton (v0.5.2c).
        // Validates citations against current file state to detect staleness by
        // comparing file LastWriteTimeUtc against Citation.IndexedAt. Publishes
        // CitationValidationFailedEvent for stale/missing citations. License-gated
        // via FeatureCodes.CitationValidation (WriterPro+).
        services.AddSingleton<ICitationValidator, CitationValidator>();

        // LOGIC: Register StaleIndicatorViewModel as transient (v0.5.2c).
        // Each search result item gets its own stale indicator instance to
        // independently track validation state and display the stale/missing
        // indicator with re-verify and dismiss actions.
        services.AddTransient<ViewModels.StaleIndicatorViewModel>();

        // =============================================================================
        // v0.5.2d: Citation Engine (Citation Copy Actions)
        // =============================================================================

        // LOGIC: Register CitationClipboardService as singleton (v0.5.2d).
        // Provides clipboard operations for copying formatted citations, chunk text,
        // and document paths. Uses ICitationService for formatting (license-gated),
        // CitationFormatterRegistry for user style preferences, and IMediator for
        // publishing CitationCopiedEvent telemetry notifications.
        services.AddSingleton<ICitationClipboardService, CitationClipboardService>();

        // =============================================================================
        // v0.5.3b: Context Window (Sibling Chunk Retrieval)
        // =============================================================================

        // LOGIC: Register SiblingCache as singleton (v0.5.3b).
        // LRU cache for sibling chunk queries with document-level invalidation.
        // MaxEntries=500, EvictionBatch=50 for optimal memory/performance tradeoff.
        // Thread-safe via ConcurrentDictionary. Automatically invalidates on
        // DocumentIndexedEvent and DocumentRemovedFromIndexEvent via MediatR handlers.
        services.AddSingleton<SiblingCache>();
        services.AddSingleton<MediatR.INotificationHandler<Indexing.DocumentIndexedEvent>>(
            sp => sp.GetRequiredService<SiblingCache>());
        services.AddSingleton<MediatR.INotificationHandler<Indexing.DocumentRemovedFromIndexEvent>>(
            sp => sp.GetRequiredService<SiblingCache>());

        // =============================================================================
        // v0.5.3a: Context Window (Context Expansion Service)
        // =============================================================================

        // LOGIC: Register ContextExpansionService as scoped (v0.5.3a).
        // Scoped to align with IChunkRepository lifetime for sibling chunk queries.
        // Provides context expansion with LRU caching (100 entries, FIFO eviction).
        // License-gated via FeatureFlags.RAG.ContextWindow (WriterPro+).
        services.AddScoped<IContextExpansionService, ContextExpansionService>();

        // =============================================================================
        // v0.5.3c: Context Window (Heading Hierarchy)
        // =============================================================================

        // LOGIC: Register HeadingHierarchyService as singleton (v0.5.3c).
        // Provides heading breadcrumb resolution for search results via tree-based
        // heading hierarchy. Caches heading trees per document (MaxCacheSize=50).
        // Subscribes to DocumentIndexedEvent and DocumentRemovedFromIndexEvent for
        // automatic cache invalidation. Thread-safe via ConcurrentDictionary.
        // License-gated via FeatureFlags.RAG.ContextWindow (WriterPro+).
        services.AddSingleton<HeadingHierarchyService>();
        services.AddSingleton<IHeadingHierarchyService>(sp => sp.GetRequiredService<HeadingHierarchyService>());
        services.AddSingleton<MediatR.INotificationHandler<Indexing.DocumentIndexedEvent>>(
            sp => sp.GetRequiredService<HeadingHierarchyService>());
        services.AddSingleton<MediatR.INotificationHandler<Indexing.DocumentRemovedFromIndexEvent>>(
            sp => sp.GetRequiredService<HeadingHierarchyService>());

        // =============================================================================
        // v0.5.3d: Context Window (Context Preview UI)
        // =============================================================================

        // LOGIC: Register ContextPreviewViewModelFactory as singleton (v0.5.3d).
        // Factory for creating ContextPreviewViewModel instances from SearchHit data.
        // Converts TextChunk to RAG Chunk for IContextExpansionService compatibility.
        // Injects IContextExpansionService, ILicenseContext, and ILoggerFactory into
        // created ViewModels. Stateless and thread-safe.
        services.AddSingleton<IContextPreviewViewModelFactory, ViewModels.ContextPreviewViewModelFactory>();

        // =============================================================================
        // v0.5.4a: Relevance Tuner (Query Analyzer)
        // =============================================================================

        // LOGIC: Register QueryAnalyzer as singleton (v0.5.4a).
        // Analyzes search queries to extract keywords, entities, intent, and specificity.
        // Stateless and thread-safe. Uses pre-compiled regex patterns and in-memory
        // stop-word lists for < 20ms analysis latency.
        services.AddSingleton<IQueryAnalyzer, Search.QueryAnalyzer>();

        // =============================================================================
        // v0.5.4b: Relevance Tuner (Query Expansion)
        // =============================================================================

        // LOGIC: Register QueryExpander as singleton (v0.5.4b).
        // Expands queries with synonyms from built-in technical abbreviations and
        // Porter stemming. Uses ConcurrentDictionary for thread-safe caching.
        // License-gated via FeatureFlags.RAG.RelevanceTuner (WriterPro+).
        services.AddSingleton<IQueryExpander, Search.QueryExpander>();

        // =============================================================================
        // v0.5.4c: Relevance Tuner (Query Suggestions)
        // =============================================================================

        // LOGIC: Register QuerySuggestionService as scoped (v0.5.4c).
        // Scoped to align with IDbConnectionFactory lifetime for database queries.
        // Provides autocomplete suggestions from query history, document headings,
        // content n-grams, and domain terms. Uses in-memory cache with 30s TTL.
        // License-gated via FeatureFlags.RAG.RelevanceTuner (WriterPro+).
        services.AddScoped<IQuerySuggestionService, Search.QuerySuggestionService>();

        // =============================================================================
        // v0.5.4d: Relevance Tuner (Query History & Analytics)
        // =============================================================================

        // LOGIC: Register QueryHistoryService as scoped (v0.5.4d).
        // Scoped to align with IDbConnectionFactory lifetime for database queries.
        // Tracks executed queries for recent queries panel and zero-result analysis.
        // Publishes QueryAnalyticsEvent via MediatR for opt-in telemetry.
        // License-gated via FeatureFlags.RAG.RelevanceTuner (WriterPro+).
        services.AddScoped<IQueryHistoryService, Search.QueryHistoryService>();

        // =============================================================================
        // v0.5.5a: Filter System (Filter Model)
        // =============================================================================

        // LOGIC: Register FilterValidator as singleton (v0.5.5a).
        // Stateless validation service for SearchFilter instances.
        // Validates path patterns, file extensions, and date ranges.
        services.AddSingleton<IFilterValidator, FilterValidator>();

        // =============================================================================
        // v0.5.5b: Filter System (Filter UI Component)
        // =============================================================================

        // LOGIC: Register SearchFilterPanelViewModel as transient (v0.5.5b).
        // Each filter panel instance gets its own ViewModel.
        // Manages folder tree, extension toggles, date range picker, and saved presets.
        // License-gated features: DateRangeFilter and SavedPresets (WriterPro+).
        services.AddTransient<SearchFilterPanelViewModel>();

        // =============================================================================
        // v0.5.5c: Filter System (Filter Query Builder)
        // =============================================================================

        // LOGIC: Register FilterQueryBuilder as singleton (v0.5.5c).
        // Stateless service that builds SQL queries from SearchFilter criteria.
        // Generates CTEs for efficient filtered vector search.
        services.AddSingleton<IFilterQueryBuilder, FilterQueryBuilder>();

        // =============================================================================
        // v0.5.6a: Answer Preview (Snippet Extraction)
        // =============================================================================

        // LOGIC: Register SentenceBoundaryDetector as singleton (v0.5.6c).
        // Provides abbreviation-aware sentence boundary detection for smart truncation.
        // Replaces PassthroughSentenceBoundaryDetector from v0.5.6a.
        services.AddSingleton<ISentenceBoundaryDetector, SentenceBoundaryDetector>();

        // LOGIC: Register SnippetService as singleton (v0.5.6a).
        // Extracts contextual snippets from chunks with query highlighting.
        // Thread-safe and stateless. Uses IQueryAnalyzer for keyword extraction
        // and ISentenceBoundaryDetector for natural boundary snapping.
        services.AddSingleton<ISnippetService, SnippetService>();

        // =============================================================================
        // v0.5.6b: Answer Preview (Query Term Highlighting)
        // =============================================================================

        // LOGIC: Register HighlightRenderer as singleton (v0.5.6b).
        // Converts Snippet with HighlightSpan positions into styled text runs.
        // Thread-safe and stateless. Platform-agnostic output enables unit testing
        // without UI dependencies and potential reuse across UI frameworks.
        services.AddSingleton<Rendering.IHighlightRenderer, Rendering.HighlightRenderer>();

        // =============================================================================
        // v0.5.7b: Result Grouping (Document-Grouped Results)
        // =============================================================================

        // LOGIC: Register ResultGroupingService as singleton (v0.5.7b).
        // Thread-safe and stateless — pure transformation from SearchResult to
        // GroupedSearchResults. Groups hits by document path, calculates metadata,
        // limits hits per group, and sorts groups according to ResultSortMode.
        services.AddSingleton<IResultGroupingService, ResultGroupingService>();

        // LOGIC: Register GroupedResultsViewModel as transient (v0.5.7b).
        // Each grouped results display gets its own ViewModel for independent
        // expansion state and sort mode management.
        services.AddTransient<GroupedResultsViewModel>();

        // =============================================================================
        // v0.5.7c: Preview Pane (Split-View Preview)
        // =============================================================================

        // LOGIC: Register PreviewContentBuilder as singleton (v0.5.7c).
        // Thread-safe and stateless — coordinates between IContextExpansionService
        // and ISnippetService to build preview content for search hits.
        services.AddSingleton<IPreviewContentBuilder, PreviewContentBuilder>();

        // LOGIC: Register PreviewPaneViewModel as transient (v0.5.7c).
        // Each preview pane instance gets its own ViewModel for independent
        // loading state, visibility, and selection management.
        services.AddTransient<PreviewPaneViewModel>();

        // =============================================================================
        // v0.5.7d: Search Actions (Copy, Export, Open All)
        // =============================================================================

        // LOGIC: Register SearchActionsService as singleton (v0.5.7d).
        // Thread-safe service providing copy, export, and open-all operations
        // for grouped search results. Integrates with ICitationService for
        // formatted output and IEditorService for document navigation.
        // License-gated: Export and CitationFormatted copy require Writer Pro.
        services.AddSingleton<ISearchActionsService, SearchActionsService>();

        // =============================================================================
        // v0.5.8c: Multi-Layer Caching System
        // =============================================================================

        // LOGIC: Configure QueryCacheOptions with defaults using Options pattern (v0.5.8c).
        // Provides in-memory caching for search results with LRU+TTL eviction.
        // MaxEntries=100, TTL=5 minutes by default.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new QueryCacheOptions()));

        // LOGIC: Configure ContextCacheOptions with defaults using Options pattern (v0.5.8c).
        // Provides session-scoped caching for context expansion results.
        // MaxEntriesPerSession=50 by default.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ContextCacheOptions()));

        // LOGIC: Register CacheKeyGenerator as singleton (v0.5.8c).
        // Generates deterministic SHA256 cache keys from query text, options, and filters.
        // Thread-safe and stateless.
        services.AddSingleton<CacheKeyGenerator>();

        // LOGIC: Register QueryResultCache as singleton (v0.5.8c).
        // In-memory cache for SearchResult objects with LRU+TTL eviction.
        // Uses ReaderWriterLockSlim for thread-safe concurrent access.
        // Document-aware invalidation removes stale entries on re-indexing.
        services.AddSingleton<IQueryResultCache, QueryResultCache>();

        // LOGIC: Register ContextExpansionCacheService as singleton (v0.5.8c).
        // Session-isolated in-memory cache for ExpandedChunk objects.
        // Uses nested ConcurrentDictionary for lock-free concurrent access.
        // Sessions are isolated to prevent cross-user data leakage.
        services.AddSingleton<IContextExpansionCache, ContextExpansionCacheService>();

        // LOGIC: Register CacheInvalidationHandler as singleton (v0.5.8c).
        // Subscribes to DocumentIndexedEvent and DocumentRemovedFromIndexEvent
        // to automatically invalidate stale cache entries across both caches.
        services.AddSingleton<CacheInvalidationHandler>();
        services.AddSingleton<MediatR.INotificationHandler<Indexing.DocumentIndexedEvent>>(
            sp => sp.GetRequiredService<CacheInvalidationHandler>());
        services.AddSingleton<MediatR.INotificationHandler<Indexing.DocumentRemovedFromIndexEvent>>(
            sp => sp.GetRequiredService<CacheInvalidationHandler>());

        // =============================================================================
        // v0.5.8d: Error Resilience
        // =============================================================================

        // LOGIC: Configure ResilienceOptions with defaults using Options pattern (v0.5.8d).
        // Provides configuration for Polly resilience policies:
        // - Retry: 3 attempts with exponential backoff + jitter
        // - Timeout: 5 seconds per operation
        // - Circuit Breaker: Opens after 5 failures, 30s break duration
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new ResilienceOptions()));

        // LOGIC: Register ResilientSearchService as scoped (v0.5.8d).
        // Decorates IHybridSearchService with resilience policies.
        // Provides automatic fallback to BM25 when embedding API is unavailable.
        // Falls back to cached results when database is unavailable.
        // Circuit breaker prevents cascading failures during outages.
        services.AddScoped<IResilientSearchService, Resilience.ResilientSearchService>();

        // =============================================================================
        // v0.5.9a: Similarity Detection Infrastructure
        // =============================================================================

        // LOGIC: Configure SimilarityDetectorOptions with defaults using Options pattern (v0.5.9a).
        // Provides configuration for similarity detection thresholds and batch processing:
        // - SimilarityThreshold: 0.95 (conservative, avoiding false positives)
        // - MaxResultsPerChunk: 5
        // - BatchSize: 10 (optimized for pgvector query performance)
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(SimilarityDetectorOptions.Default));

        // LOGIC: Register SimilarityDetector as scoped (v0.5.9a).
        // Scoped to align with IChunkRepository and IDocumentRepository lifetimes.
        // Provides semantic similarity detection for deduplication pipeline.
        services.AddScoped<ISimilarityDetector, Services.SimilarityDetector>();

        // =============================================================================
        // v0.5.9b: Relationship Classification
        // =============================================================================

        // LOGIC: Configure ClassificationOptions with defaults using Options pattern (v0.5.9b).
        // Provides configuration for relationship classification thresholds and caching:
        // - RuleBasedThreshold: 0.95 (high-confidence fast-path)
        // - EnableLlmClassification: true (when LLM service available)
        // - CacheDuration: 1 hour
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(ClassificationOptions.Default));

        // LOGIC: Register RelationshipClassifier as scoped (v0.5.9b).
        // Scoped to align with repository and cache lifetimes.
        // Provides hybrid rule-based and LLM-based classification for chunk pairs.
        services.AddScoped<IRelationshipClassifier, Services.RelationshipClassifier>();

        // =============================================================================
        // v0.5.9c: Canonical Record Management
        // =============================================================================

        // LOGIC: Register CanonicalManager as scoped (v0.5.9c).
        // Scoped to align with repository and database connection lifetimes.
        // Provides atomic operations for canonical records, variant merging,
        // promotion, detachment, and provenance tracking.
        services.AddScoped<ICanonicalManager, Services.CanonicalManager>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// RAG module initialization is minimal as the repositories are stateless
    /// and database schema is managed through migrations.
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<RAGModule>>();
        _logger.LogInformation("Initializing RAG module v{Version}", Info.Version);

        // LOGIC: Verify that IDbConnectionFactory is available.
        // This is a sanity check - the actual connection test happens on first use.
        var connectionFactory = provider.GetService<IDbConnectionFactory>();
        if (connectionFactory is null)
        {
            _logger.LogWarning(
                "IDbConnectionFactory not registered. RAG repositories will fail at runtime. " +
                "Ensure Infrastructure module is loaded before RAG module.");
        }
        else
        {
            _logger.LogDebug("IDbConnectionFactory available. RAG repositories ready.");
        }

        await Task.CompletedTask;
    }
}
