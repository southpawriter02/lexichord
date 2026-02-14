// -----------------------------------------------------------------------
// <copyright file="KnowledgeContextStrategy.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Copilot;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Provides knowledge graph entities, relationships, and axioms as context
/// for AI agents. Bridges the v0.6.6e <see cref="IKnowledgeContextProvider"/>
/// pipeline into the v0.7.2 Context Assembler strategy interface.
/// </summary>
/// <remarks>
/// <para>
/// This strategy leverages the existing knowledge graph infrastructure to
/// provide domain-relevant entities, their relationships, and applicable
/// axioms (domain rules) to AI agents. It uses the user's selected text
/// or document file name as the search query, then delegates entity search,
/// relevance ranking, budget selection, and formatting to the
/// <see cref="IKnowledgeContextProvider"/>.
/// </para>
/// <para>
/// <strong>Pipeline:</strong>
/// <list type="number">
///   <item><description>Build search query from selected text (preferred) or document path.</description></item>
///   <item><description>Resolve agent-specific configuration from <see cref="AgentConfigs"/> dictionary.</description></item>
///   <item><description>Apply hint overrides from <see cref="ContextGatheringRequest.Hints"/>.</description></item>
///   <item><description>Apply license-tier restrictions (WriterPro: max 10 entities, no axioms/relationships).</description></item>
///   <item><description>Delegate to <see cref="IKnowledgeContextProvider.GetContextAsync"/> for search, ranking, and formatting.</description></item>
///   <item><description>Apply base class token truncation via <see cref="ContextStrategyBase.TruncateToMaxTokens"/>.</description></item>
///   <item><description>Create fragment with averaged relevance score.</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Priority:</strong> 30 — After Document (100), Selection (80), Cursor (70),
/// Heading (70), and RAG (60), but before Style (50) in some configurations.
/// Knowledge context provides supplementary domain information.
/// </para>
/// <para>
/// <strong>License:</strong> Requires <see cref="LicenseTier.Teams"/> or higher
/// for full access (entities + relationships + axioms). WriterPro tier receives
/// entities-only context with a maximum of 10 entities.
/// </para>
/// <para>
/// <strong>Max Tokens:</strong> 4000 — Sufficient for ~20 entities with
/// relationships and axioms in YAML format.
/// </para>
/// <para>
/// <strong>Error Handling:</strong>
/// The strategy catches <see cref="FeatureNotLicensedException"/> and general exceptions
/// gracefully, returning <c>null</c> instead of propagating errors. Only
/// <see cref="OperationCanceledException"/> is re-thrown to respect cancellation.
/// </para>
/// <para>
/// <strong>Configurable Hints:</strong>
/// <list type="bullet">
///   <item><description><c>MaxEntities</c> (int): Override maximum entity count.</description></item>
///   <item><description><c>MinRelevanceScore</c> (float): Override minimum relevance threshold.</description></item>
///   <item><description><c>IncludeAxioms</c> (bool): Override axiom inclusion.</description></item>
///   <item><description><c>IncludeRelationships</c> (bool): Override relationship inclusion.</description></item>
///   <item><description><c>KnowledgeFormat</c> (string): Override output format ("Markdown", "Yaml", "Json", "Plain").</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2e as part of the Knowledge Context Strategy.
/// </para>
/// </remarks>
[RequiresLicense(LicenseTier.Teams)]
internal sealed class KnowledgeContextStrategy : ContextStrategyBase
{
    private readonly IKnowledgeContextProvider _contextProvider;
    private readonly ILicenseContext _license;

    /// <summary>
    /// Hint key for overriding the maximum number of entities.
    /// </summary>
    internal const string MaxEntitiesHintKey = "MaxEntities";

    /// <summary>
    /// Hint key for overriding the minimum relevance score threshold.
    /// </summary>
    internal const string MinRelevanceScoreHintKey = "MinRelevanceScore";

    /// <summary>
    /// Hint key for overriding axiom inclusion.
    /// </summary>
    internal const string IncludeAxiomsHintKey = "IncludeAxioms";

    /// <summary>
    /// Hint key for overriding relationship inclusion.
    /// </summary>
    internal const string IncludeRelationshipsHintKey = "IncludeRelationships";

    /// <summary>
    /// Hint key for overriding the output format.
    /// </summary>
    internal const string KnowledgeFormatHintKey = "KnowledgeFormat";

    /// <summary>
    /// Maximum number of entities for WriterPro tier (entities-only access).
    /// </summary>
    private const int WriterProMaxEntities = 10;

    /// <summary>
    /// Maximum length of the search query extracted from selected text.
    /// </summary>
    private const int MaxQueryLength = 200;

    // LOGIC: Agent-specific configurations that tailor knowledge context
    // to each agent type's needs. Keys are string-based AgentId values
    // matching the codebase convention (not the spec's AgentType enum).
    //
    // The configurations balance context richness against token budget:
    //   - Editor: Full context (30 entities, axioms, relationships) for editing
    //   - Simplifier: Minimal context (10 concepts, no axioms) for simplification
    //   - Tuning: Technical context (20 endpoint/parameter entities) for tuning
    //   - Summarizer: Overview context (15 product/component entities) for summaries
    //   - Co-pilot: Broad context (25 entities, axioms) for general assistance
    private static readonly Dictionary<string, KnowledgeContextConfig> AgentConfigs = new()
    {
        ["editor"] = new KnowledgeContextConfig
        {
            MaxEntities = 30,
            MaxTokens = 5000,
            IncludeAxioms = true,
            IncludeRelationships = true,
            MinRelevanceScore = 0.4f
        },
        ["simplifier"] = new KnowledgeContextConfig
        {
            MaxEntities = 10,
            MaxTokens = 2000,
            IncludeEntityTypes = ["Concept", "Term", "Definition"],
            IncludeAxioms = false,
            IncludeRelationships = false,
            MinRelevanceScore = 0.6f
        },
        ["tuning"] = new KnowledgeContextConfig
        {
            MaxEntities = 20,
            MaxTokens = 4000,
            IncludeEntityTypes = ["Endpoint", "Parameter", "Response", "Schema"],
            IncludeAxioms = true,
            IncludeRelationships = true,
            MinRelevanceScore = 0.5f
        },
        ["summarizer"] = new KnowledgeContextConfig
        {
            MaxEntities = 15,
            MaxTokens = 3000,
            IncludeEntityTypes = ["Product", "Component", "Feature", "Service"],
            IncludeAxioms = false,
            IncludeRelationships = true,
            MinRelevanceScore = 0.5f
        },
        ["co-pilot"] = new KnowledgeContextConfig
        {
            MaxEntities = 25,
            MaxTokens = 4000,
            IncludeAxioms = true,
            IncludeRelationships = true,
            MinRelevanceScore = 0.4f
        }
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeContextStrategy"/> class.
    /// </summary>
    /// <param name="contextProvider">Knowledge context provider for entity search, ranking, and formatting.</param>
    /// <param name="license">License context for runtime tier checks.</param>
    /// <param name="tokenCounter">Token counter for content estimation.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="contextProvider"/> or <paramref name="license"/> is null.
    /// </exception>
    public KnowledgeContextStrategy(
        IKnowledgeContextProvider contextProvider,
        ILicenseContext license,
        ITokenCounter tokenCounter,
        ILogger<KnowledgeContextStrategy> logger)
        : base(tokenCounter, logger)
    {
        _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
        _license = license ?? throw new ArgumentNullException(nameof(license));
    }

    /// <inheritdoc />
    public override string StrategyId => "knowledge";

    /// <inheritdoc />
    public override string DisplayName => "Knowledge Graph";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Priority 30 places knowledge context after Document (100),
    /// Selection (80), Cursor (70), Heading (70), RAG (60), and Style (50).
    /// Knowledge graph data is supplementary domain information that enriches
    /// the agent's understanding of the subject matter.
    /// </remarks>
    public override int Priority => 30;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: 4000 tokens provides room for ~20 entities with relationships
    /// and axioms in YAML format. Agent-specific configs may request more
    /// or fewer tokens via <see cref="KnowledgeContextConfig.MaxTokens"/>.
    /// </remarks>
    public override int MaxTokens => 4000;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Knowledge context gathering pipeline:
    /// </para>
    /// <list type="number">
    ///   <item><description>Determines search query from selected text (preferred) or falls back
    ///     to document path file name as a last resort.</description></item>
    ///   <item><description>Resolves agent-specific <see cref="KnowledgeContextConfig"/> from the
    ///     <see cref="AgentConfigs"/> dictionary using <see cref="ContextGatheringRequest.AgentId"/>.</description></item>
    ///   <item><description>Applies hint overrides from <see cref="ContextGatheringRequest.Hints"/>
    ///     for per-request customization.</description></item>
    ///   <item><description>Applies license-tier restrictions for WriterPro users
    ///     (max 10 entities, no axioms, no relationships).</description></item>
    ///   <item><description>Builds <see cref="KnowledgeContextOptions"/> from the resolved config
    ///     and delegates to <see cref="IKnowledgeContextProvider.GetContextAsync"/>.</description></item>
    ///   <item><description>Applies base class token truncation via <see cref="ContextStrategyBase.TruncateToMaxTokens"/>.</description></item>
    ///   <item><description>Creates a <see cref="ContextFragment"/> with the formatted content
    ///     and a relevance score derived from the entity count ratio.</description></item>
    /// </list>
    /// <para>
    /// Error handling: <see cref="FeatureNotLicensedException"/> and general exceptions
    /// are caught and result in <c>null</c> return. <see cref="OperationCanceledException"/>
    /// is re-thrown.
    /// </para>
    /// </remarks>
    public override async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        // LOGIC: Need either selection or document to derive a search query
        if (!request.HasSelection && !request.HasDocument)
        {
            _logger.LogDebug("{Strategy} no query source available (no selection or document)", StrategyId);
            return null;
        }

        // LOGIC: Build search query from available context
        var query = BuildSearchQuery(request);
        if (string.IsNullOrWhiteSpace(query))
        {
            _logger.LogDebug("{Strategy} could not build search query", StrategyId);
            return null;
        }

        // LOGIC: Resolve agent-specific configuration
        var config = GetConfigForAgent(request.AgentId);

        // LOGIC: Apply per-request hint overrides
        config = ApplyHintOverrides(config, request);

        // LOGIC: Apply license-tier restrictions for WriterPro users
        config = ApplyLicenseRestrictions(config);

        _logger.LogDebug(
            "{Strategy} querying knowledge graph for: '{Query}' (max {MaxEntities} entities, min score {MinScore:F2}, axioms: {Axioms}, rels: {Rels})",
            StrategyId, Truncate(query, 50), config.MaxEntities, config.MinRelevanceScore,
            config.IncludeAxioms, config.IncludeRelationships);

        try
        {
            // LOGIC: Build KnowledgeContextOptions from the resolved config
            var options = BuildOptions(config);

            // LOGIC: Delegate to the existing v0.6.6e knowledge context pipeline
            // (entity search → relevance ranking → budget selection → formatting)
            var knowledgeContext = await _contextProvider.GetContextAsync(query, options, ct);

            // LOGIC: Check if any entities were found
            if (knowledgeContext.Entities.Count == 0)
            {
                _logger.LogDebug("{Strategy} no knowledge entities found for query", StrategyId);
                return null;
            }

            // LOGIC: Check if formatted content is available
            if (string.IsNullOrWhiteSpace(knowledgeContext.FormattedContext))
            {
                _logger.LogDebug("{Strategy} knowledge context was formatted as empty", StrategyId);
                return null;
            }

            // LOGIC: Apply base class token truncation
            var content = TruncateToMaxTokens(knowledgeContext.FormattedContext);

            // LOGIC: Calculate relevance score based on how many entities survived
            // the relevance filtering and budget selection pipeline. A higher ratio
            // indicates the query was highly relevant to the knowledge graph content.
            var relevance = knowledgeContext.WasTruncated
                ? 0.7f  // Budget-constrained: good relevance but capped
                : Math.Min(1.0f, knowledgeContext.Entities.Count / 10.0f); // Scale by entity count

            _logger.LogInformation(
                "{Strategy} gathered {EntityCount} entities, {RelCount} relationships, {AxiomCount} axioms " +
                "(tokens: {TokenCount}, truncated: {Truncated}, relevance: {Relevance:F2})",
                StrategyId,
                knowledgeContext.Entities.Count,
                knowledgeContext.Relationships?.Count ?? 0,
                knowledgeContext.Axioms?.Count ?? 0,
                knowledgeContext.TokenCount,
                knowledgeContext.WasTruncated,
                relevance);

            return CreateFragment(content, relevance);
        }
        catch (FeatureNotLicensedException ex)
        {
            // LOGIC: Graceful handling when knowledge graph features are not licensed
            _logger.LogDebug(ex, "{Strategy} knowledge graph not licensed", StrategyId);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LOGIC: Graceful handling of knowledge graph failures
            // OperationCanceledException is re-thrown to respect cancellation
            _logger.LogWarning(ex, "{Strategy} knowledge graph query failed", StrategyId);
            return null;
        }
    }

    /// <summary>
    /// Gets the agent-specific configuration for the given agent ID.
    /// </summary>
    /// <param name="agentId">The agent ID to look up (e.g., "editor", "co-pilot").</param>
    /// <returns>
    /// The agent-specific <see cref="KnowledgeContextConfig"/> if found;
    /// otherwise, a default configuration.
    /// </returns>
    /// <remarks>
    /// LOGIC: Agent configurations are stored in the static <see cref="AgentConfigs"/>
    /// dictionary. Unknown agent IDs receive the default configuration, which provides
    /// a balanced set of defaults suitable for general-purpose agents.
    /// </remarks>
    internal static KnowledgeContextConfig GetConfigForAgent(string agentId)
    {
        return AgentConfigs.TryGetValue(agentId, out var config)
            ? config
            : new KnowledgeContextConfig();
    }

    /// <summary>
    /// Builds a search query from the context gathering request.
    /// </summary>
    /// <param name="request">The context gathering request.</param>
    /// <returns>A search query string, or null if no suitable query source is available.</returns>
    /// <remarks>
    /// LOGIC: Query source priority:
    /// <list type="number">
    ///   <item><description>Selected text (highest signal — user's explicit focus).</description></item>
    ///   <item><description>Document path file name as a fallback (file name may provide topic hints).</description></item>
    /// </list>
    /// Query text is truncated to <see cref="MaxQueryLength"/> characters to keep search efficient.
    /// </remarks>
    private static string? BuildSearchQuery(ContextGatheringRequest request)
    {
        // LOGIC: Prefer selected text — highest signal for user intent
        if (request.HasSelection)
        {
            return Truncate(request.SelectedText!, MaxQueryLength);
        }

        // LOGIC: Fall back to document path (file name can hint at topic)
        if (request.HasDocument)
        {
            return Path.GetFileNameWithoutExtension(request.DocumentPath!);
        }

        return null;
    }

    /// <summary>
    /// Applies per-request hint overrides to the agent configuration.
    /// </summary>
    /// <param name="config">The base agent configuration.</param>
    /// <param name="request">The request containing optional hint overrides.</param>
    /// <returns>A new config with hint overrides applied, or the original if no hints.</returns>
    /// <remarks>
    /// LOGIC: Hint overrides allow per-request customization of the knowledge
    /// context retrieval. Each hint is independently applied only when present
    /// in the request's Hints dictionary. The <c>with</c> expression creates
    /// a new record instance, preserving immutability.
    /// </remarks>
    private static KnowledgeContextConfig ApplyHintOverrides(
        KnowledgeContextConfig config,
        ContextGatheringRequest request)
    {
        // LOGIC: Apply each hint override independently using GetHint<T>
        // with the current config value as the default (no-op when hint absent)
        var maxEntities = request.GetHint(MaxEntitiesHintKey, config.MaxEntities);
        var minRelevanceScore = request.GetHint(MinRelevanceScoreHintKey, config.MinRelevanceScore);
        var includeAxioms = request.GetHint(IncludeAxiomsHintKey, config.IncludeAxioms);
        var includeRelationships = request.GetHint(IncludeRelationshipsHintKey, config.IncludeRelationships);

        // LOGIC: Parse format hint string to ContextFormat enum
        var formatHint = request.GetHint<string?>(KnowledgeFormatHintKey, null);
        var format = formatHint is not null && Enum.TryParse<ContextFormat>(formatHint, ignoreCase: true, out var parsed)
            ? parsed
            : config.Format;

        return config with
        {
            MaxEntities = maxEntities,
            MinRelevanceScore = minRelevanceScore,
            IncludeAxioms = includeAxioms,
            IncludeRelationships = includeRelationships,
            Format = format
        };
    }

    /// <summary>
    /// Applies license-tier restrictions to the configuration.
    /// </summary>
    /// <param name="config">The configuration to restrict.</param>
    /// <returns>A restricted configuration based on the current license tier.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: License restrictions per tier:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Core:</b> Not available (handled by factory-level
    ///     <see cref="RequiresLicenseAttribute"/> gating).</description></item>
    ///   <item><description><b>WriterPro:</b> Entities only — max 10 entities,
    ///     no axioms, no relationships.</description></item>
    ///   <item><description><b>Teams/Enterprise:</b> Full access — no restrictions applied.</description></item>
    /// </list>
    /// <para>
    /// The <c>[RequiresLicense(LicenseTier.Teams)]</c> attribute on this class
    /// prevents Core-tier users from reaching this code. WriterPro users may
    /// reach this code if the factory is configured to allow partial access.
    /// </para>
    /// </remarks>
    private KnowledgeContextConfig ApplyLicenseRestrictions(KnowledgeContextConfig config)
    {
        // LOGIC: WriterPro tier gets entities-only access with a cap
        if (_license.Tier == LicenseTier.WriterPro)
        {
            _logger.LogDebug(
                "{Strategy} applying WriterPro restrictions (max {Max} entities, no axioms/rels)",
                StrategyId, WriterProMaxEntities);

            return config with
            {
                MaxEntities = Math.Min(config.MaxEntities, WriterProMaxEntities),
                IncludeAxioms = false,
                IncludeRelationships = false
            };
        }

        // LOGIC: Teams+ tier gets full access, no restrictions needed
        return config;
    }

    /// <summary>
    /// Builds <see cref="KnowledgeContextOptions"/> from the resolved agent configuration.
    /// </summary>
    /// <param name="config">The resolved agent configuration.</param>
    /// <returns>A <see cref="KnowledgeContextOptions"/> suitable for the provider.</returns>
    /// <remarks>
    /// LOGIC: Maps the internal <see cref="KnowledgeContextConfig"/> record to the
    /// v0.6.6e <see cref="KnowledgeContextOptions"/> record used by
    /// <see cref="IKnowledgeContextProvider"/>. The config's <c>IncludeEntityTypes</c>
    /// (<c>IReadOnlyList&lt;string&gt;?</c>) is converted to <c>IReadOnlySet&lt;string&gt;?</c>
    /// for <see cref="KnowledgeContextOptions.EntityTypes"/> compatibility.
    /// </remarks>
    private static KnowledgeContextOptions BuildOptions(KnowledgeContextConfig config)
    {
        // LOGIC: Convert IReadOnlyList<string>? to IReadOnlySet<string>? for EntitySearchQuery
        IReadOnlySet<string>? entityTypes = config.IncludeEntityTypes is { Count: > 0 }
            ? new HashSet<string>(config.IncludeEntityTypes)
            : null;

        return new KnowledgeContextOptions
        {
            MaxTokens = config.MaxTokens,
            MaxEntities = config.MaxEntities,
            IncludeRelationships = config.IncludeRelationships,
            IncludeAxioms = config.IncludeAxioms,
            EntityTypes = entityTypes,
            MinRelevanceScore = config.MinRelevanceScore,
            Format = config.Format
        };
    }

    /// <summary>
    /// Truncates a string to a maximum length.
    /// </summary>
    /// <param name="text">The text to truncate.</param>
    /// <param name="maxLength">Maximum character count.</param>
    /// <returns>The original or truncated text.</returns>
    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;
        return text[..maxLength];
    }
}
