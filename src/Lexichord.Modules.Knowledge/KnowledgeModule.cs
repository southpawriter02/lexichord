// =============================================================================
// File: KnowledgeModule.cs
// Project: Lexichord.Modules.Knowledge
// Description: Module registration for the Knowledge Graph subsystem.
// =============================================================================
// LOGIC: Registers all Knowledge Graph-related services and initializes the
//   Neo4j graph database connection. This module implements CKVS Phase 1:
//   - v0.4.5e: Graph Database Integration
//   - v0.4.5f: Schema Registry Service (this version)
//   - v0.4.5g: Entity Abstraction Layer (this version)
//
// Service Registrations:
//   - Neo4jConnectionFactory: Singleton (manages connection pool)
//   - Neo4jHealthCheck: Transient (created per health check invocation)
//   - GraphConfiguration: Bound to IOptions<GraphConfiguration>
//   - SchemaRegistry: Singleton (in-memory schema registry for entity/relationship validation)
//   - EntityExtractionPipeline: Singleton (coordinates extractors, deduplicates, aggregates)
//
// v0.4.5g: Entity Abstraction Layer (CKVS Phase 1)
// Dependencies: ISecureVault (v0.0.6a), ILicenseContext (v0.0.4c),
//               IConfiguration (v0.0.3d), ILogger<T> (v0.0.3b)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Extraction;
using Lexichord.Modules.Knowledge.Extraction.Extractors;
using Lexichord.Modules.Knowledge.Graph;
using Lexichord.Modules.Knowledge.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge;

/// <summary>
/// Module registration for the Knowledge Graph subsystem (CKVS Phase 1).
/// </summary>
/// <remarks>
/// <para>
/// The Knowledge module provides the graph database infrastructure for storing
/// structured knowledge entities and their relationships. This is the foundation
/// for the Canonical Knowledge Validation System (CKVS).
/// </para>
/// <para>
/// <b>Components (v0.4.5e):</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Neo4jConnectionFactory"/>: Neo4j driver management with connection pooling and license gating.</description></item>
///   <item><description><see cref="Neo4jGraphSession"/>: Cypher query execution with timing and logging.</description></item>
///   <item><description><see cref="Neo4jHealthCheck"/>: Application health monitoring for Neo4j connectivity.</description></item>
///   <item><description><see cref="GraphConfiguration"/>: Typed configuration for Neo4j connection settings.</description></item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: Cannot access Knowledge Graph features.</item>
///   <item>WriterPro: Read-only access to the knowledge graph.</item>
///   <item>Teams: Full read-write access.</item>
///   <item>Enterprise: Full access plus custom ontology schemas (future).</item>
/// </list>
/// </para>
/// <para>
/// <b>Database Requirements:</b> Requires a running Neo4j 5.x instance.
/// The Docker Compose configuration provides a pre-configured Neo4j container
/// for local development.
/// </para>
/// </remarks>
public sealed class KnowledgeModule : IModule
{
    private ILogger<KnowledgeModule>? _logger;

    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "knowledge",
        Name: "Knowledge Graph",
        Version: new Version(0, 4, 5),
        Author: "Lexichord Team",
        Description: "Knowledge Graph Foundation — Neo4j integration for structured knowledge storage (CKVS Phase 1)"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Registers all Knowledge Graph services in the DI container. The
    /// <see cref="Neo4jConnectionFactory"/> is registered as a singleton to
    /// share the connection pool across the application lifetime.
    /// </para>
    /// <para>
    /// <b>Configuration Binding:</b> <see cref="GraphConfiguration"/> is bound
    /// to the <c>Knowledge:Graph</c> section of appsettings.json via
    /// <c>Options.Create()</c> with sensible defaults. Configuration can be
    /// overridden via the standard configuration hierarchy.
    /// </para>
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // =============================================================================
        // v0.4.5e: Graph Database Integration
        // =============================================================================

        // LOGIC: Configure GraphConfiguration with defaults using Options pattern.
        // Defaults match the Docker Compose Neo4j container configuration.
        // These can be overridden via appsettings.json or environment variables.
        services.AddSingleton(Microsoft.Extensions.Options.Options.Create(new GraphConfiguration()));

        // LOGIC: Register Neo4jConnectionFactory as singleton.
        // The factory manages the Neo4j driver and its connection pool.
        // Singleton ensures a single connection pool shared across the application.
        // Also registers as IAsyncDisposable for proper shutdown cleanup.
        services.AddSingleton<Neo4jConnectionFactory>();
        services.AddSingleton<IGraphConnectionFactory>(sp =>
            sp.GetRequiredService<Neo4jConnectionFactory>());

        // LOGIC: Register Neo4jHealthCheck as transient.
        // Health checks are created per invocation by the health check framework.
        services.AddTransient<Neo4jHealthCheck>();

        // =============================================================================
        // v0.4.5f: Schema Registry Service
        // =============================================================================

        // LOGIC: Register SchemaRegistry as singleton.
        // The registry holds all loaded entity and relationship type schemas in memory.
        // Singleton ensures consistent schema state across the application lifetime.
        // Also registered as ISchemaRegistry for consumer injection.
        services.AddSingleton<SchemaRegistry>();
        services.AddSingleton<ISchemaRegistry>(sp =>
            sp.GetRequiredService<SchemaRegistry>());

        // =============================================================================
        // v0.4.5g: Entity Abstraction Layer
        // =============================================================================

        // LOGIC: Register EntityExtractionPipeline as singleton.
        // The pipeline coordinates multiple entity extractors, handling priority-ordered
        // execution, error isolation, confidence filtering, mention deduplication, and
        // entity aggregation. Singleton ensures consistent extractor registration and
        // shared state across the application lifetime.
        // Also registered as IEntityExtractionPipeline for consumer injection.
        services.AddSingleton<EntityExtractionPipeline>();
        services.AddSingleton<IEntityExtractionPipeline>(sp =>
        {
            var pipeline = sp.GetRequiredService<EntityExtractionPipeline>();

            // LOGIC: Register built-in extractors in priority order (descending).
            // EndpointExtractor (100): API endpoint detection — most specific, runs first.
            // ParameterExtractor (90): API parameter detection — often co-located with endpoints.
            // ConceptExtractor (50): Domain term detection — broadest, runs last.
            pipeline.Register(new EndpointExtractor());
            pipeline.Register(new ParameterExtractor());
            pipeline.Register(new ConceptExtractor());

            return pipeline;
        });

        // =============================================================================
        // v0.4.6f: Axiom Repository
        // =============================================================================

        // LOGIC: Register AxiomCacheService as singleton.
        // The cache service uses IMemoryCache with 5-minute sliding / 30-minute absolute
        // expiration. Singleton ensures consistent cache state across the application.
        services.AddSingleton<Axioms.IAxiomCacheService, Axioms.AxiomCacheService>();

        // LOGIC: Register AxiomRepository as scoped.
        // Scoped lifetime aligns with per-request database connections.
        // The repository uses IDbConnectionFactory for PostgreSQL access.
        services.AddScoped<Lexichord.Abstractions.Contracts.Knowledge.IAxiomRepository, Axioms.AxiomRepository>();

        // =============================================================================
        // v0.4.6g: Axiom Loader
        // =============================================================================

        // LOGIC: Register AxiomLoader as singleton.
        // The loader manages file watching and caches parsed axiom files.
        // Singleton ensures consistent state and single FileSystemWatcher instance.
        // Implements IDisposable for cleanup of FileSystemWatcher resources.
        services.AddSingleton<Axioms.IAxiomLoader, Axioms.AxiomLoader>();

        // =============================================================================
        // v0.4.6h: Axiom Query API
        // =============================================================================

        // LOGIC: Register AxiomEvaluator as singleton.
        // The evaluator is stateless and thread-safe, can be shared across requests.
        services.AddSingleton<Axioms.IAxiomEvaluator, Axioms.AxiomEvaluator>();

        // LOGIC: Register AxiomStore as scoped.
        // Scoped lifetime aligns with per-request license context and database access.
        // The store integrates with IAxiomRepository (scoped) and IAxiomLoader (singleton).
        services.AddScoped<Axioms.IAxiomStore, Axioms.AxiomStore>();

        // =============================================================================
        // v0.4.7e: Entity List View
        // =============================================================================

        // LOGIC: Register GraphRepository as singleton.
        // The repository wraps IGraphSession for domain-specific entity queries.
        // Singleton is appropriate as it's stateless and thread-safe.
        services.AddSingleton<Graph.GraphRepository>();
        services.AddSingleton<IGraphRepository>(sp =>
            sp.GetRequiredService<Graph.GraphRepository>());

        // LOGIC: Register EntityListViewModel as transient.
        // Each view instance should get its own ViewModel for independent state.
        services.AddTransient<UI.ViewModels.EntityListViewModel>();

        // =============================================================================
        // v0.4.7f: Entity Detail View
        // =============================================================================

        // LOGIC: Register EntityDetailViewModel as transient.
        // Each detail view needs independent state for entity display.
        services.AddTransient<UI.ViewModels.EntityDetailViewModel>();

        // =============================================================================
        // v0.4.7g: Entity CRUD Operations
        // =============================================================================

        // LOGIC: Register EntityCrudService as scoped.
        // Scoped lifetime aligns with per-request license context and database access.
        // The service integrates with IGraphRepository (singleton) and IAxiomStore (scoped).
        services.AddScoped<Abstractions.Contracts.Knowledge.IEntityCrudService, Services.EntityCrudService>();

        // =============================================================================
        // v0.4.7h: Relationship Viewer
        // =============================================================================

        // LOGIC: Register RelationshipViewerPanelViewModel as transient.
        // Each relationship panel instance needs independent state for filtering and display.
        services.AddTransient<UI.ViewModels.RelationshipViewerPanelViewModel>();

        // =============================================================================
        // v0.4.7i: Axiom Viewer
        // =============================================================================

        // LOGIC: Register AxiomViewerViewModel as transient.
        // Each axiom viewer instance needs independent state for filtering and grouping.
        services.AddTransient<UI.ViewModels.AxiomViewerViewModel>();

        // =============================================================================
        // v0.5.5c-i: Linking Review UI
        // =============================================================================

        // LOGIC: Register LinkingReviewViewModel as transient (v0.5.5c-i).
        // Each review panel instance gets its own ViewModel.
        // Manages pending link queue, review decisions, and statistics.
        // License-gated: WriterPro (view-only), Teams+ (full review).
        services.AddTransient<UI.ViewModels.LinkingReview.LinkingReviewViewModel>();

        // =============================================================================
        // v0.6.5e: Validation Orchestrator (CKVS Phase 3a)
        // =============================================================================

        // LOGIC: Register ValidatorRegistry as singleton.
        // The registry holds all registered IValidator instances and their metadata.
        // Singleton ensures consistent validator registration state across the application.
        // Thread-safe via ConcurrentDictionary.
        services.AddSingleton<Validation.ValidatorRegistry>();

        // LOGIC: Register ValidationPipeline as singleton.
        // The pipeline is stateless — it executes validators on demand with provided context.
        // Singleton is appropriate as it only depends on ILogger.
        services.AddSingleton<Validation.ValidationPipeline>();

        // LOGIC: Register ValidationEngine as singleton.
        // The engine orchestrates registry + pipeline and is the public entry point.
        // Also registered as IValidationEngine for consumer injection.
        services.AddSingleton<Validation.ValidationEngine>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IValidationEngine>(sp =>
            sp.GetRequiredService<Validation.ValidationEngine>());

        // =====================================================================
        // v0.6.5f: Schema Validator (CKVS Phase 3a)
        // =====================================================================

        // LOGIC: Register PropertyTypeChecker as singleton.
        // Stateless type-checking logic — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IPropertyTypeChecker,
            Validation.Validators.Schema.PropertyTypeChecker>();

        // LOGIC: Register ConstraintEvaluator as singleton.
        // Stateless constraint evaluation — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IConstraintEvaluator,
            Validation.Validators.Schema.ConstraintEvaluator>();

        // LOGIC: Register SchemaValidatorService as singleton.
        // Also registered as ISchemaValidatorService for direct entity validation.
        services.AddSingleton<Validation.Validators.Schema.SchemaValidatorService>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.ISchemaValidatorService>(sp =>
            sp.GetRequiredService<Validation.Validators.Schema.SchemaValidatorService>());

        // =====================================================================
        // v0.6.5g: Axiom Validator (CKVS Phase 3a)
        // =====================================================================

        // LOGIC: Register AxiomValidatorService as singleton.
        // The service bridges IValidator (v0.6.5e) with IAxiomEvaluator (v0.4.6h),
        // converting AxiomViolation instances to ValidationFinding for pipeline compatibility.
        // Also registered as IAxiomValidatorService for direct entity validation.
        // Requires LicenseTier.Teams for axiom evaluation features.
        services.AddSingleton<Validation.Validators.Axiom.AxiomValidatorService>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IAxiomValidatorService>(sp =>
            sp.GetRequiredService<Validation.Validators.Axiom.AxiomValidatorService>());

        // =====================================================================
        // v0.6.5h: Consistency Checker (CKVS Phase 3a)
        // =====================================================================

        // LOGIC: Register ConflictDetector as singleton.
        // Stateless claim comparison logic — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IConflictDetector,
            Validation.Validators.Consistency.ConflictDetector>();

        // LOGIC: Register ContradictionResolver as singleton.
        // Stateless resolution suggestion logic — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IContradictionResolver,
            Validation.Validators.Consistency.ContradictionResolver>();

        // LOGIC: Register ConsistencyChecker as singleton.
        // Orchestrates conflict detection and resolution via IClaimRepository.
        // Also registered as IConsistencyChecker for direct claim consistency checks.
        // Requires LicenseTier.Teams for full consistency checking.
        services.AddSingleton<Validation.Validators.Consistency.ConsistencyChecker>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.IConsistencyChecker>(sp =>
            sp.GetRequiredService<Validation.Validators.Consistency.ConsistencyChecker>());

        // =====================================================================
        // v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
        // =====================================================================

        // LOGIC: Register FindingDeduplicator as singleton.
        // Stateless deduplication logic — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.Aggregation.IFindingDeduplicator,
            Validation.Aggregation.FindingDeduplicator>();

        // LOGIC: Register FixConsolidator as singleton.
        // Stateless fix consolidation logic — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.Aggregation.IFixConsolidator,
            Validation.Aggregation.FixConsolidator>();

        // LOGIC: Register ResultAggregator as singleton.
        // Orchestrates deduplication, sorting, limiting, and result construction.
        // Also registered as IResultAggregator for interface-based injection.
        services.AddSingleton<Validation.Aggregation.ResultAggregator>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.Aggregation.IResultAggregator>(sp =>
            sp.GetRequiredService<Validation.Aggregation.ResultAggregator>());

        // =====================================================================
        // v0.6.5j: Linter Integration (CKVS Phase 3a)
        // =====================================================================

        // LOGIC: Register UnifiedFindingAdapter as singleton.
        // Stateless adapter — maps ValidationFinding and StyleViolation to UnifiedFinding.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.Integration.IUnifiedFindingAdapter,
            Validation.Integration.UnifiedFindingAdapter>();

        // LOGIC: Register CombinedFixWorkflow as singleton.
        // Stateless conflict detection and fix ordering — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.Integration.ICombinedFixWorkflow,
            Validation.Integration.CombinedFixWorkflow>();

        // LOGIC: Register LinterIntegration as singleton.
        // Orchestrates IValidationEngine + IStyleEngine and provides unified results.
        // Also registered as ILinterIntegration for interface-based injection.
        services.AddSingleton<Validation.Integration.LinterIntegration>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Validation.Integration.ILinterIntegration>(sp =>
            sp.GetRequiredService<Validation.Integration.LinterIntegration>());

        // =====================================================================
        // v0.6.6e: Graph Context Provider (CKVS Phase 3b)
        // =====================================================================

        // LOGIC: Register EntityRelevanceRanker as singleton.
        // Stateless term-based scoring — safe to share across threads.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Copilot.IEntityRelevanceRanker,
            Copilot.Context.EntityRelevanceRanker>();

        // LOGIC: Register KnowledgeContextFormatter as singleton via factory lambda.
        // Stateless multi-format formatter — safe to share across threads.
        // v0.7.2g: Factory resolution for optional ITokenCounter (accurate token estimation).
        // When ITokenCounter is not registered, the formatter falls back to the
        // ~4 characters per token heuristic.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Copilot.IKnowledgeContextFormatter>(sp =>
            new Copilot.Context.KnowledgeContextFormatter(
                sp.GetRequiredService<ILogger<Copilot.Context.KnowledgeContextFormatter>>(),
                sp.GetService<ITokenCounter>()));

        // LOGIC: Register KnowledgeContextProvider as scoped.
        // Depends on IAxiomStore (scoped) and IClaimRepository (scoped),
        // so must be scoped to match the shortest dependency lifetime.
        services.AddScoped<Abstractions.Contracts.Knowledge.Copilot.IKnowledgeContextProvider,
            Copilot.Context.KnowledgeContextProvider>();

        // LOGIC: Register KnowledgePromptBuilder as singleton.
        // Maintains an in-memory template registry loaded at construction.
        // Depends on IKnowledgeContextFormatter (singleton) and IPromptRenderer (singleton).
        // v0.6.6i: Knowledge-Aware Prompts
        services.AddSingleton<Abstractions.Contracts.Knowledge.Copilot.IKnowledgePromptBuilder,
            Copilot.Prompts.KnowledgePromptBuilder>();

        // =====================================================================
        // v0.7.2f: Entity Relevance Scorer (CKVS Phase 4a)
        // =====================================================================

        // LOGIC: Register ScoringConfig with defaults using Options pattern.
        // Defaults: SemanticWeight=0.35, MentionWeight=0.25, TypeWeight=0.20,
        // RecencyWeight=0.10, NameMatchWeight=0.10, RecencyDecayDays=365.
        // These can be overridden via IConfiguration binding if needed.
        services.AddSingleton(
            Microsoft.Extensions.Options.Options.Create(
                new Abstractions.Contracts.Knowledge.Copilot.ScoringConfig()));

        // LOGIC: Register EntityRelevanceScorer as singleton via factory lambda.
        // Uses factory resolution to optionally resolve IEmbeddingService (nullable).
        // When IEmbeddingService is not registered, the scorer gracefully falls back
        // to non-semantic scoring by redistributing the semantic weight.
        // Singleton is appropriate: scorer is stateless and thread-safe.
        services.AddSingleton<Abstractions.Contracts.Knowledge.Copilot.IEntityRelevanceScorer>(sp =>
            new Copilot.Context.Scoring.EntityRelevanceScorer(
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<
                    Abstractions.Contracts.Knowledge.Copilot.ScoringConfig>>(),
                sp.GetRequiredService<ILogger<Copilot.Context.Scoring.EntityRelevanceScorer>>(),
                sp.GetService<IEmbeddingService>()));

        // =====================================================================
        // v0.7.6e: Sync Service Core (CKVS Phase 4c)
        // =====================================================================

        // LOGIC: Register SyncStatusTracker as singleton.
        // Maintains per-document sync state in memory (thread-safe via ConcurrentDictionary).
        // Singleton ensures consistent state across sync operations.
        services.AddSingleton<Sync.SyncStatusTracker>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.ISyncStatusTracker>(sp =>
            sp.GetRequiredService<Sync.SyncStatusTracker>());

        // LOGIC: Register SyncConflictDetector as singleton.
        // Compares document extractions against graph state to detect conflicts.
        // Stateless operation using IGraphRepository for queries.
        services.AddSingleton<Sync.SyncConflictDetector>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.ISyncConflictDetector>(sp =>
            sp.GetRequiredService<Sync.SyncConflictDetector>());

        // LOGIC: Register ConflictResolver as singleton.
        // Applies resolution strategies to sync conflicts.
        // Maintains pending conflicts in memory per document.
        services.AddSingleton<Sync.ConflictResolver>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.IConflictResolver>(sp =>
            sp.GetRequiredService<Sync.ConflictResolver>());

        // LOGIC: Register SyncOrchestrator as singleton.
        // Coordinates the sync pipeline (extraction, conflict detection, graph upsert).
        // Depends on IEntityExtractionPipeline, IGraphRepository, ISyncConflictDetector.
        services.AddSingleton<Sync.Core.SyncOrchestrator>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.ISyncOrchestrator>(sp =>
            sp.GetRequiredService<Sync.Core.SyncOrchestrator>());

        // LOGIC: Register SyncService as singleton.
        // Main entry point for sync operations with license gating and event publishing.
        // Depends on ISyncOrchestrator, ISyncStatusTracker, IConflictResolver,
        // ILicenseContext, IMediator.
        services.AddSingleton<Sync.Core.SyncService>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.ISyncService>(sp =>
            sp.GetRequiredService<Sync.Core.SyncService>());

        // =====================================================================
        // v0.7.6f: Doc-to-Graph Sync (CKVS Phase 4c)
        // =====================================================================

        // LOGIC: Register ExtractionLineageStore as singleton.
        // Maintains extraction history per document in memory (thread-safe).
        // Enables lineage queries and rollback target identification.
        services.AddSingleton<Sync.DocToGraph.ExtractionLineageStore>();

        // LOGIC: Register ExtractionValidator as singleton.
        // Validates extracted entities and relationships against graph schema.
        // Stateless validation using known entity types list.
        services.AddSingleton<Sync.DocToGraph.ExtractionValidator>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.DocToGraph.IExtractionValidator>(sp =>
            sp.GetRequiredService<Sync.DocToGraph.ExtractionValidator>());

        // LOGIC: Register ExtractionTransformer as singleton.
        // Transforms AggregatedEntity to KnowledgeEntity, derives relationships.
        // Depends on IGraphRepository for entity enrichment.
        services.AddSingleton<Sync.DocToGraph.ExtractionTransformer>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.DocToGraph.IExtractionTransformer>(sp =>
            sp.GetRequiredService<Sync.DocToGraph.ExtractionTransformer>());

        // LOGIC: Register DocumentToGraphSyncProvider as singleton.
        // Main orchestrator for doc-to-graph sync with license gating and event publishing.
        // Depends on IEntityExtractionPipeline, IExtractionTransformer, IExtractionValidator,
        // IGraphRepository, IClaimExtractionService, ExtractionLineageStore,
        // ILicenseContext, IMediator.
        services.AddSingleton<Sync.DocToGraph.DocumentToGraphSyncProvider>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.DocToGraph.IDocumentToGraphSyncProvider>(sp =>
            sp.GetRequiredService<Sync.DocToGraph.DocumentToGraphSyncProvider>());

        // =====================================================================
        // v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
        // =====================================================================

        // LOGIC: Register DocumentFlagStore as singleton.
        // Maintains document flags in memory (thread-safe via ConcurrentDictionary).
        // Enables flag storage, retrieval, and resolution tracking.
        services.AddSingleton<Sync.GraphToDoc.DocumentFlagStore>();

        // LOGIC: Register AffectedDocumentDetector as singleton.
        // Detects documents referencing changed graph entities.
        // Depends on IDocumentRepository and IGraphRepository for queries.
        services.AddSingleton<Sync.GraphToDoc.AffectedDocumentDetector>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.GraphToDoc.IAffectedDocumentDetector>(sp =>
            sp.GetRequiredService<Sync.GraphToDoc.AffectedDocumentDetector>());

        // LOGIC: Register DocumentFlagger as singleton.
        // Creates and manages document flags for review workflows.
        // Depends on DocumentFlagStore and IMediator for event publishing.
        services.AddSingleton<Sync.GraphToDoc.DocumentFlagger>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.GraphToDoc.IDocumentFlagger>(sp =>
            sp.GetRequiredService<Sync.GraphToDoc.DocumentFlagger>());

        // LOGIC: Register GraphToDocumentSyncProvider as singleton.
        // Main orchestrator for graph-to-doc sync with license gating and event publishing.
        // Depends on IAffectedDocumentDetector, IDocumentFlagger, ILicenseContext, IMediator.
        // License requirement: Teams+ (Core/WriterPro blocked).
        services.AddSingleton<Sync.GraphToDoc.GraphToDocumentSyncProvider>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.GraphToDoc.IGraphToDocumentSyncProvider>(sp =>
            sp.GetRequiredService<Sync.GraphToDoc.GraphToDocumentSyncProvider>());

        // =====================================================================
        // v0.7.6h: Conflict Resolver (CKVS Phase 4c)
        // =====================================================================

        // LOGIC: Register EntityComparer as singleton.
        // Compares document entities against graph entities property-by-property.
        // Stateless comparison with confidence scoring — safe to share across threads.
        services.AddSingleton<Sync.Conflict.EntityComparer>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.Conflict.IEntityComparer>(sp =>
            sp.GetRequiredService<Sync.Conflict.EntityComparer>());

        // LOGIC: Register ConflictStore as singleton.
        // Thread-safe in-memory storage for conflict details via ConcurrentDictionary.
        // Provides document-based conflict grouping and resolution history tracking.
        services.AddSingleton<Sync.Conflict.ConflictStore>();

        // LOGIC: Register ConflictDetector as singleton.
        // Enhanced conflict detection with value and structural conflict analysis.
        // Extends ISyncConflictDetector (v0.7.6e) with detailed ConflictDetail records.
        // Depends on IEntityComparer, IGraphRepository for entity lookups.
        services.AddSingleton<Sync.Conflict.ConflictDetector>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.Conflict.IConflictDetector>(sp =>
            sp.GetRequiredService<Sync.Conflict.ConflictDetector>());

        // LOGIC: Register ConflictMerger as singleton.
        // Provides intelligent value merging with multiple strategies:
        // DocumentFirst, GraphFirst, Combine, MostRecent, HighestConfidence, RequiresManualMerge.
        // Stateless merge logic — safe to share across threads.
        services.AddSingleton<Sync.Conflict.ConflictMerger>();
        services.AddSingleton<Abstractions.Contracts.Knowledge.Sync.Conflict.IConflictMerger>(sp =>
            sp.GetRequiredService<Sync.Conflict.ConflictMerger>());

        // LOGIC: Register EnhancedConflictResolver as singleton.
        // Extended conflict resolver with detailed resolution results and merge support.
        // Depends on IConflictMerger, ConflictStore, ILicenseContext, IMediator.
        // License tiers:
        // - Core: No resolution access
        // - WriterPro: Basic resolution (Low severity auto-resolve, Manual only)
        // - Teams: Full resolution (Low/Medium auto-resolve, Merge support)
        // - Enterprise: Advanced resolution (all auto-resolve, advanced merge)
        services.AddSingleton<Sync.Conflict.EnhancedConflictResolver>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Initializes the Knowledge module after the DI container is built.
    /// Performs a connectivity check against Neo4j and logs the result.
    /// </para>
    /// <para>
    /// <b>Failure Handling:</b> Neo4j connectivity failure during initialization
    /// is logged as a warning but does not prevent module loading. The graph
    /// database may not be available during development if Docker is not running.
    /// Runtime operations will fail with appropriate error messages when
    /// Neo4j is unavailable.
    /// </para>
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<KnowledgeModule>>();
        _logger.LogInformation("Initializing Knowledge module v{Version}", Info.Version);

        // LOGIC: Verify ISecureVault is available (required for Neo4j password).
        var vault = provider.GetService<Lexichord.Abstractions.Contracts.Security.ISecureVault>();
        if (vault is null)
        {
            _logger.LogWarning(
                "ISecureVault not registered. Neo4j password must be provided via configuration. " +
                "For production, ensure ISecureVault is available.");
        }

        // LOGIC: Attempt Neo4j connectivity verification.
        // This is a best-effort check — failure is logged but not fatal.
        var factory = provider.GetService<IGraphConnectionFactory>();
        if (factory is not null)
        {
            try
            {
                var connected = await factory.TestConnectionAsync();
                if (connected)
                {
                    _logger.LogInformation(
                        "Neo4j connection verified (database: {Database})",
                        factory.DatabaseName);
                }
                else
                {
                    _logger.LogWarning(
                        "Neo4j connection test failed. Knowledge Graph features will be unavailable " +
                        "until Neo4j is accessible. Ensure Docker Compose is running.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Neo4j initialization check failed. Knowledge Graph features will be " +
                    "unavailable until Neo4j is accessible. Error: {Message}", ex.Message);
            }
        }
        else
        {
            _logger.LogWarning(
                "IGraphConnectionFactory not available. This may indicate a DI registration issue.");
        }
    }
}
