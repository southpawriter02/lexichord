// <copyright file="FeatureCodes.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Constants;

/// <summary>
/// Constants for feature codes used in license gating throughout Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1d - Feature codes provide granular control over feature enablement.
/// These codes are used with <see cref="Contracts.ILicenseContext.IsFeatureEnabled"/> to
/// check if specific features are available to the current license.
///
/// Feature Code Naming Convention:
/// - Format: {module}.{feature}
/// - Module: The primary module that owns the feature
/// - Feature: The specific feature within that module
///
/// Usage:
/// <code>
/// if (!licenseContext.IsFeatureEnabled(FeatureCodes.FuzzyMatching))
/// {
///     // Show upgrade prompt or disable feature UI
/// }
/// </code>
///
/// Design Decisions:
/// - Codes are dot-separated for readability and namespace-like organization
/// - All codes are compile-time constants for performance and IntelliSense support
/// - New feature codes should be documented with their required tier
/// </remarks>
public static class FeatureCodes
{
    #region Fuzzy Engine Features (v0.3.1)

    /// <summary>
    /// The Fuzzy Matching feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Levenshtein-based fuzzy matching algorithm.
    /// This feature enables approximate string matching for terminology detection.
    /// </remarks>
    public const string FuzzyMatching = "Feature.FuzzyMatching";

    /// <summary>
    /// The Fuzzy Scanning feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the fuzzy document scanner.
    /// When enabled, documents are scanned for approximate terminology matches
    /// in addition to exact regex matches.
    /// </remarks>
    public const string FuzzyScanning = "Feature.FuzzyScanning";

    /// <summary>
    /// The Fuzzy Threshold Configuration feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to per-term fuzzy threshold configuration.
    /// Users can customize the match sensitivity for individual terminology entries.
    /// </remarks>
    public const string FuzzyThresholdConfig = "Feature.FuzzyThresholdConfig";

    #endregion

    #region Feature Code Utilities

    /// <summary>
    /// Gets all fuzzy-related feature codes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Used for bulk feature checks in UI to determine if any fuzzy features are enabled.
    /// </remarks>
    public static readonly string[] FuzzyFeatures =
    [
        FuzzyMatching,
        FuzzyScanning,
        FuzzyThresholdConfig
    ];

    #endregion

    #region Readability Engine Features (v0.3.3)

    /// <summary>
    /// The Readability HUD feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Readability HUD Widget for displaying
    /// Flesch-Kincaid Grade Level, Gunning Fog Index, and Flesch Reading Ease.
    /// </remarks>
    public const string ReadabilityHud = "Feature.ReadabilityHud";

    #endregion

    #region Voice Profile Features (v0.3.4)

    /// <summary>
    /// The Custom Profiles feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: Teams
    /// Controls access to creating and managing custom Voice Profiles.
    /// Built-in profiles are available to all Writer Pro users.
    /// </remarks>
    public const string CustomProfiles = "Feature.CustomProfiles";

    #endregion

    #region Resonance Dashboard Features (v0.3.5)

    /// <summary>
    /// The Resonance Dashboard feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Resonance Dashboard spider chart visualization.
    /// This feature provides a 6-axis chart showing writing style metrics
    /// compared against target values from the active Voice Profile.
    /// </remarks>
    public const string ResonanceDashboard = "Feature.ResonanceDashboard";

    #endregion

    #region Global Dictionary Features (v0.3.6)

    /// <summary>
    /// The Global Dictionary feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to project-level configuration settings.
    /// This feature enables .lexichord/style.yaml project configuration,
    /// .lexichordignore file patterns, and per-project rule overrides.
    /// </remarks>
    public const string GlobalDictionary = "Feature.GlobalDictionary";

    #endregion

    #region Hybrid Search Features (v0.5.1)

    /// <summary>
    /// The Hybrid Search feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Hybrid Search mode which combines BM25 keyword
    /// search and semantic vector search using Reciprocal Rank Fusion (RRF).
    /// When this feature is not enabled, the Hybrid option in the search mode
    /// toggle is locked and the user is prompted to upgrade.
    /// Semantic and Keyword modes remain available to all tiers.
    /// Introduced in v0.5.1d.
    /// </remarks>
    public const string HybridSearch = "Feature.HybridSearch";

    #endregion

    #region Citation Engine Features (v0.5.2)

    /// <summary>
    /// The Citation feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to formatted citation creation and copy actions.
    /// When this feature is not enabled, users see basic document paths only
    /// and are prompted to upgrade for formatted citations.
    /// Introduced in v0.5.2a.
    /// </remarks>
    public const string Citation = "Feature.Citation";

    /// <summary>
    /// The Citation Validation feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to stale citation detection and validation.
    /// When this feature is not enabled, ValidateIfLicensedAsync returns null
    /// and stale indicators are hidden in the UI.
    /// Introduced in v0.5.2c.
    /// </remarks>
    public const string CitationValidation = "Feature.CitationValidation";

    #endregion

    #region Context Window Features (v0.5.3)

    /// <summary>
    /// The Context Expansion feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the context expansion preview in search results.
    /// When this feature is not enabled, the expand button shows a lock icon
    /// and clicking it triggers an upgrade prompt instead of expanding context.
    /// Context expansion retrieves sibling chunks (before/after) and heading
    /// breadcrumbs for a retrieved search result.
    /// Introduced in v0.5.3d.
    /// </remarks>
    public const string ContextExpansion = "Feature.ContextExpansion";

    #endregion

    #region Relevance Tuner Features (v0.5.4)

    /// <summary>
    /// The Knowledge Hub feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Knowledge Hub dashboard including statistics,
    /// recent queries, and query history tracking. When this feature is not
    /// enabled, query history is not recorded and hub features are unavailable.
    /// Introduced in v0.5.4d.
    /// </remarks>
    public const string KnowledgeHub = "Feature.KnowledgeHub";

    #endregion

    #region Filter System Features (v0.5.5)

    /// <summary>
    /// The Date Range Filter feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to date range filtering in the search filter panel.
    /// When this feature is not enabled, the date range section shows a lock
    /// icon and clicking it triggers an upgrade prompt. Core tier users can
    /// still use path and extension filtering.
    /// Introduced in v0.5.5b.
    /// </remarks>
    public const string DateRangeFilter = "Feature.DateRangeFilter";

    /// <summary>
    /// The Saved Presets feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to saving and managing filter presets. When this feature
    /// is not enabled, the presets section shows a lock icon and preset
    /// save/apply functionality is disabled. Users can still manually configure
    /// filters but cannot save them for quick reuse.
    /// Introduced in v0.5.5b.
    /// </remarks>
    public const string SavedPresets = "Feature.SavedPresets";

    #endregion

    #region Semantic Deduplication Features (v0.5.9)

    /// <summary>
    /// The Semantic Deduplication feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to semantic deduplication features including similarity
    /// detection and relationship classification. When this feature is not enabled,
    /// classification returns Unknown and deduplication suggestions are hidden.
    /// Introduced in v0.5.9a.
    /// </remarks>
    public const string SemanticDeduplication = "Feature.SemanticDeduplication";

    /// <summary>
    /// The Deduplication Service feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the main deduplication orchestrator service.
    /// When this feature is not enabled, ProcessChunkAsync bypasses deduplication
    /// and returns StoredAsNew without performing any checks.
    /// Introduced in v0.5.9d.
    /// </remarks>
    public const string DeduplicationService = "RAG.Dedup.Service";

    /// <summary>
    /// The Batch Deduplication feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: Teams
    /// Controls access to batch retroactive deduplication job execution.
    /// When this feature is not enabled, batch job execution throws
    /// InvalidOperationException requiring Teams tier upgrade.
    /// Introduced in v0.5.9g.
    /// </remarks>
    public const string BatchDeduplication = "RAG.Dedup.Batch";

    /// <summary>
    /// The Deduplication Metrics Dashboard feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the full deduplication metrics dashboard with trends,
    /// statistics, and detailed breakdowns. When this feature is not enabled,
    /// GetDashboardDataAsync returns empty data and GetTrendsAsync returns an
    /// empty list. Basic health status via GetHealthStatusAsync remains available
    /// to all tiers.
    /// Introduced in v0.5.9h.
    /// </remarks>
    public const string DeduplicationMetrics = "RAG.Dedup.Metrics";

    #endregion

    #region Context Injection Features (v0.6.3)

    /// <summary>
    /// The RAG Context feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to RAG (Retrieval-Augmented Generation) context injection
    /// in prompt templates. When this feature is not enabled, the RAGContextProvider
    /// is skipped during context assembly, and only document and style context
    /// are available for prompt injection.
    /// Introduced in v0.6.3d.
    /// </remarks>
    public const string RAGContext = "Feature.RAGContext";

    #endregion

    #region Editor Agent Features (v0.7.3)

    /// <summary>
    /// The Editor Agent feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Editor Agent features including AI-powered rewriting
    /// via the context menu (Rewrite Formally, Simplify, Expand, Custom Rewrite).
    /// When this feature is not enabled, rewrite menu items show a lock icon and
    /// clicking them triggers an upgrade prompt instead of executing the rewrite.
    /// Introduced in v0.7.3a.
    /// </remarks>
    public const string EditorAgent = "Feature.EditorAgent";

    #endregion

    #region Simplifier Agent Features (v0.7.4)

    /// <summary>
    /// The Simplifier Agent feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Simplifier Agent features including readability-targeted
    /// text simplification, audience presets (General Public, Technical, Executive,
    /// International/ESL), and custom preset creation. When this feature is not enabled,
    /// simplifier menu items show a lock icon and clicking them triggers an upgrade prompt
    /// instead of executing the simplification.
    /// Introduced in v0.7.4a.
    /// </remarks>
    public const string SimplifierAgent = "Feature.SimplifierAgent";

    /// <summary>
    /// The Custom Audience Presets feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to creating, updating, and deleting custom audience presets.
    /// Built-in presets (General Public, Technical, Executive, International/ESL) are
    /// available to all users. When this feature is not enabled, custom preset CRUD
    /// operations throw <see cref="Agents.LicenseTierException"/>.
    /// Introduced in v0.7.4a.
    /// </remarks>
    public const string CustomAudiencePresets = "Feature.CustomAudiencePresets";

    #endregion

    #region Tuning Agent Features (v0.7.5)

    /// <summary>
    /// The Tuning Agent feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Tuning Agent features including proactive style deviation
    /// scanning, AI-powered fix suggestions, and the Accept/Reject review UI.
    /// When this feature is not enabled, the Tuning Panel shows an upgrade prompt
    /// instead of scanning for deviations.
    /// Introduced in v0.7.5c.
    /// </remarks>
    public const string TuningAgent = "Feature.TuningAgent";

    #endregion

    #region Learning Loop Features (v0.7.5d)

    /// <summary>
    /// The Learning Loop feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: Teams
    /// Controls access to the Learning Loop feedback system that captures user
    /// accept/reject/modify decisions and uses them to improve future fix suggestions.
    /// When this feature is not enabled, the Tuning Agent still works but without
    /// personalized learning from past decisions.
    /// Introduced in v0.7.5d.
    /// </remarks>
    public const string LearningLoop = "Feature.LearningLoop";

    #endregion

    #region Summarizer Agent Features (v0.7.6)

    /// <summary>
    /// The Summarizer Agent feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Summarizer Agent features including multi-mode document
    /// summarization (Abstract, TLDR, BulletPoints, KeyTakeaways, Executive, Custom),
    /// natural language command parsing, and intelligent document chunking for long texts.
    /// When this feature is not enabled, summarizer menu items show a lock icon and
    /// clicking them triggers an upgrade prompt instead of executing the summarization.
    /// Introduced in v0.7.6a.
    /// </remarks>
    public const string SummarizerAgent = "Feature.SummarizerAgent";

    /// <summary>
    /// The Metadata Extraction feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Metadata Extraction features including:
    /// - Key term extraction with importance scoring and frequency analysis
    /// - High-level concept identification for categorization
    /// - Tag suggestions consistent with existing workspace taxonomy
    /// - Reading time calculation based on word count and complexity
    /// - Target audience inference from vocabulary and style
    /// - Document complexity scoring on a 1-10 scale
    /// - Document type classification (Tutorial, Reference, Report, etc.)
    /// - Named entity extraction (people, organizations, products)
    /// When this feature is not enabled, metadata extraction menu items show a lock icon
    /// and clicking them triggers an upgrade prompt instead of executing extraction.
    /// Introduced in v0.7.6b.
    /// </remarks>
    public const string MetadataExtraction = "Feature.MetadataExtraction";

    /// <summary>
    /// The Summary Export feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Summary Export features including:
    /// - Export to Summary Panel UI
    /// - Export to YAML frontmatter with intelligent field merging
    /// - Export to standalone Markdown file
    /// - Export to system clipboard
    /// - Export to inline cursor position with callout formatting
    /// - Summary caching with content hash invalidation
    /// When this feature is not enabled, export actions return Failed result
    /// with an upgrade prompt message. Cached summaries are viewable but
    /// not exportable or regeneratable.
    /// Introduced in v0.7.6c.
    /// </remarks>
    public const string SummaryExport = "Feature.SummaryExport";

    /// <summary>
    /// The Document Comparison feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro
    /// Controls access to the Document Comparison features including:
    /// - Semantic document version comparison with LLM analysis
    /// - Change categorization (Added, Removed, Modified, Restructured, etc.)
    /// - Significance scoring (Critical, High, Medium, Low)
    /// - Affected section identification
    /// - Git history version comparison
    /// - Text diff generation via DiffPlex
    /// - Natural language change summaries
    /// When this feature is not enabled, comparison actions return Failed result
    /// with an upgrade prompt message. Basic text diff via GetTextDiff remains
    /// available to all tiers.
    /// Introduced in v0.7.6d.
    /// </remarks>
    public const string DocumentComparison = "Feature.DocumentComparison";

    #endregion

    #region Sync Service Features (v0.7.6e)

    /// <summary>
    /// The Sync Service feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro (doc-to-graph only), Teams (full bidirectional)
    /// Controls access to document-graph synchronization features including:
    /// - Document-to-graph sync (extract entities/claims, upsert to graph)
    /// - Graph-to-document propagation (find affected documents when graph changes)
    /// - Sync status tracking (pending, in-sync, conflict states)
    /// - Conflict detection and resolution workflows
    /// - Sync completion events
    /// When this feature is not enabled (Core tier), sync actions return
    /// UnauthorizedAccessException and sync UI elements are hidden.
    /// Introduced in v0.7.6e.
    /// </remarks>
    public const string SyncService = "Feature.SyncService";

    #endregion

    #region Doc-to-Graph Sync Features (v0.7.6f)

    /// <summary>
    /// The Doc-to-Graph Sync feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro (basic), Teams (with enrichment)
    /// Controls access to document-to-graph synchronization features including:
    /// - Entity extraction from documents and transformation to graph format
    /// - Extraction validation against graph schema
    /// - Entity enrichment with existing graph context (Teams+)
    /// - Extraction lineage tracking and rollback capability
    /// - Claim extraction and relationship derivation
    /// - Sync completion events and progress tracking
    /// When this feature is not enabled (Core tier), doc-to-graph sync returns
    /// UnauthorizedAccessException and the sync UI shows an upgrade prompt.
    /// Introduced in v0.7.6f.
    /// </remarks>
    public const string DocToGraphSync = "Feature.DocToGraphSync";

    #endregion

    #region Graph-to-Doc Sync Features (v0.7.6g)

    /// <summary>
    /// The Graph-to-Doc Sync feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: Teams (full functionality)
    /// Controls access to graph-to-document synchronization features including:
    /// - Detection of documents affected by graph changes
    /// - Document flagging for review workflow
    /// - Flag resolution and tracking
    /// - Graph change subscriptions
    /// - Notification management
    /// When this feature is not enabled (Core/WriterPro tier), graph-to-doc sync returns
    /// UnauthorizedAccessException and the sync UI shows an upgrade prompt.
    /// Introduced in v0.7.6g.
    /// </remarks>
    public const string GraphToDocSync = "Feature.GraphToDocSync";

    #endregion

    #region Conflict Resolver Features (v0.7.6h)

    /// <summary>
    /// The Conflict Resolver feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro (basic), Teams (full merge), Enterprise (advanced)
    /// Controls access to enhanced conflict resolution features including:
    /// - Enhanced conflict detection with value and structural analysis
    /// - Detailed ConflictDetail records with entity-level tracking
    /// - Intelligent conflict merging via IConflictMerger
    /// - Multiple merge strategies (DocumentFirst, GraphFirst, Combine, MostRecent, HighestConfidence)
    /// - Entity comparison infrastructure via IEntityComparer
    /// - Conflict resolution history and audit trail
    /// - MediatR event publishing for resolution completion
    /// License tiers:
    /// - Core: No conflict resolution access
    /// - WriterPro: Basic resolution (Low severity auto-resolve, Manual)
    /// - Teams: Full resolution (Low/Medium auto-resolve, Merge support)
    /// - Enterprise: Advanced resolution (all auto-resolve, advanced merge)
    /// Introduced in v0.7.6h.
    /// </remarks>
    public const string ConflictResolver = "Feature.ConflictResolver";

    #endregion

    #region Sync Status Tracker Features (v0.7.6i)

    /// <summary>
    /// The Sync Status Tracker feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro (basic), Teams (full), Enterprise (advanced)
    /// Controls access to sync status tracking features including:
    /// - Sync status querying and updates (get/update status)
    /// - Batch status updates for multiple documents
    /// - Status change history with audit trail
    /// - Sync operation recording and tracking
    /// - Aggregated sync metrics (success rates, durations, conflict stats)
    /// - Document filtering by sync state
    /// License tiers:
    /// - Core: No status tracking access
    /// - WriterPro: Basic status (get/update), 30-day history
    /// - Teams: Full status (batch operations, metrics), 90-day history
    /// - Enterprise: Advanced status (unlimited history, custom metrics)
    /// Introduced in v0.7.6i.
    /// </remarks>
    public const string SyncStatusTracker = "Feature.SyncStatusTracker";

    #endregion

    #region Sync Event Publisher Features (v0.7.6j)

    /// <summary>
    /// The Sync Event Publisher feature code.
    /// </summary>
    /// <remarks>
    /// LOGIC: Required tier: WriterPro (basic), Teams (full), Enterprise (advanced)
    /// Controls access to sync event publishing features including:
    /// - Event publishing via MediatR with history storage
    /// - Batch event publishing with deduplication
    /// - Dynamic event subscriptions with filtering
    /// - Event history queries with retention limits
    /// - Event metadata and context tracking
    /// License tiers:
    /// - Core: No event publishing access
    /// - WriterPro: Publish events, 7-day history
    /// - Teams: Full access, 30-day history, subscriptions, batching
    /// - Enterprise: Unlimited history, advanced features
    /// Introduced in v0.7.6j.
    /// </remarks>
    public const string SyncEventPublisher = "Feature.SyncEventPublisher";

    #endregion
}
