// =============================================================================
// File: KnowledgeContextOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for knowledge context retrieval and formatting.
// =============================================================================
// LOGIC: Defines configuration parameters for context retrieval including
//   token budget, entity limits, inclusion flags, relevance thresholds,
//   and output format. Also defines the ContextFormat enum and
//   ContextFormatOptions record for formatter configuration.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Options for knowledge context retrieval.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="KnowledgeContextOptions"/> record provides comprehensive
/// configuration for the context retrieval pipeline, controlling token budget,
/// entity selection, relationship depth, axiom inclusion, and output format.
/// </para>
/// <para>
/// <b>Defaults:</b> Sensible defaults are provided for all properties.
/// The default configuration retrieves up to 20 entities within a 2000-token
/// budget, including relationships (depth 1) and axioms, in Markdown format.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new KnowledgeContextOptions
/// {
///     MaxTokens = 1500,
///     MaxEntities = 10,
///     IncludeAxioms = false,
///     Format = ContextFormat.Yaml
/// };
/// </code>
/// </example>
public record KnowledgeContextOptions
{
    /// <summary>
    /// Maximum tokens for the formatted context output.
    /// </summary>
    /// <value>Defaults to 2000 tokens.</value>
    /// <remarks>
    /// LOGIC: Controls the token budget for entity selection. The ranker
    /// uses this to determine how many entities fit within the budget.
    /// Does not include relationship or axiom tokens in the budget.
    /// </remarks>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>
    /// Maximum number of entities to include in the context.
    /// </summary>
    /// <value>Defaults to 20 entities.</value>
    /// <remarks>
    /// LOGIC: The search over-fetches by 2x this value to give the
    /// relevance ranker sufficient candidates for selection.
    /// </remarks>
    public int MaxEntities { get; init; } = 20;

    /// <summary>
    /// Whether to include relationships between selected entities.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// LOGIC: When enabled, retrieves all relationships where both
    /// endpoints are in the selected entity set.
    /// </remarks>
    public bool IncludeRelationships { get; init; } = true;

    /// <summary>
    /// Maximum relationship traversal depth.
    /// </summary>
    /// <value>Defaults to 1 (direct relationships only).</value>
    /// <remarks>
    /// LOGIC: Reserved for future multi-hop relationship traversal.
    /// Currently only depth 1 (direct relationships) is supported.
    /// </remarks>
    public int RelationshipDepth { get; init; } = 1;

    /// <summary>
    /// Whether to include applicable axioms (domain rules).
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// LOGIC: Requires Teams+ license tier. When enabled, retrieves
    /// axioms targeting the entity types present in the context.
    /// </remarks>
    public bool IncludeAxioms { get; init; } = true;

    /// <summary>
    /// Whether to include related claims from source documents.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    /// <remarks>
    /// LOGIC: Claims add grounding evidence but increase token usage.
    /// Disabled by default for budget conservation.
    /// </remarks>
    public bool IncludeClaims { get; init; } = false;

    /// <summary>
    /// Entity types to include in the search (null = all types).
    /// </summary>
    /// <value>
    /// A set of entity type names (e.g., "Endpoint", "Parameter"),
    /// or null to include all types.
    /// </value>
    /// <remarks>
    /// LOGIC: Type filtering is applied during the search phase
    /// before relevance ranking. Case-sensitive matching.
    /// </remarks>
    public IReadOnlySet<string>? EntityTypes { get; init; }

    /// <summary>
    /// Minimum relevance score threshold for entity inclusion.
    /// </summary>
    /// <value>Defaults to 0.1f.</value>
    /// <remarks>
    /// LOGIC: Entities with relevance scores below this threshold
    /// are excluded from the results after ranking.
    /// </remarks>
    public float MinRelevanceScore { get; init; } = 0.1f;

    /// <summary>
    /// Output format for the formatted context string.
    /// </summary>
    /// <value>Defaults to <see cref="ContextFormat.Markdown"/>.</value>
    /// <remarks>
    /// LOGIC: Determines the formatting style used by
    /// <see cref="IKnowledgeContextFormatter.FormatContext"/>.
    /// </remarks>
    public ContextFormat Format { get; init; } = ContextFormat.Markdown;
}

/// <summary>
/// Output format for knowledge context.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
public enum ContextFormat
{
    /// <summary>Markdown format with headers and bullet lists.</summary>
    Markdown,

    /// <summary>YAML format for structured data.</summary>
    Yaml,

    /// <summary>JSON format with indentation.</summary>
    Json,

    /// <summary>Plain text format without markup.</summary>
    Plain
}

/// <summary>
/// Options for context formatting.
/// </summary>
/// <remarks>
/// <para>
/// Controls fine-grained formatting behavior for the context formatter,
/// including property display limits, entity ID visibility, and separators.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
public record ContextFormatOptions
{
    /// <summary>
    /// The output format to use.
    /// </summary>
    /// <value>Defaults to <see cref="ContextFormat.Markdown"/>.</value>
    public ContextFormat Format { get; init; } = ContextFormat.Markdown;

    /// <summary>
    /// Whether to include property descriptions in the output.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    public bool IncludePropertyDescriptions { get; init; } = false;

    /// <summary>
    /// Whether to include entity IDs in the output.
    /// </summary>
    /// <value>Defaults to <c>false</c>.</value>
    public bool IncludeEntityIds { get; init; } = false;

    /// <summary>
    /// Maximum number of properties to display per entity.
    /// </summary>
    /// <value>Defaults to 10 properties.</value>
    /// <remarks>
    /// LOGIC: Limits property display to conserve token budget.
    /// Properties are displayed in insertion order.
    /// </remarks>
    public int MaxPropertiesPerEntity { get; init; } = 10;

    /// <summary>
    /// Separator between entity sections.
    /// </summary>
    /// <value>Defaults to a newline character.</value>
    public string EntitySeparator { get; init; } = "\n";
}
