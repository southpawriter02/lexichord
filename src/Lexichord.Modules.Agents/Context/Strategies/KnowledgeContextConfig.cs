// -----------------------------------------------------------------------
// <copyright file="KnowledgeContextConfig.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Knowledge.Copilot;

namespace Lexichord.Modules.Agents.Context.Strategies;

/// <summary>
/// Agent-specific configuration for knowledge context retrieval.
/// Controls entity limits, relevance thresholds, content inclusion flags,
/// and output format for the <see cref="KnowledgeContextStrategy"/>.
/// </summary>
/// <remarks>
/// <para>
/// Each agent type can have a tailored configuration that balances
/// context richness against token budget. For example, the Editor agent
/// receives more entities and axioms than the Simplifier agent.
/// </para>
/// <para>
/// <strong>Design Pattern:</strong>
/// Immutable record with <c>init</c> setters for <c>with</c>-expression
/// composition. This enables license-tier restriction application via
/// <c>config with { MaxEntities = 10, IncludeAxioms = false }</c>.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2e as part of the Knowledge Context Strategy.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Default configuration
/// var config = new KnowledgeContextConfig();
///
/// // Agent-specific configuration
/// var editorConfig = new KnowledgeContextConfig
/// {
///     MaxEntities = 30,
///     MaxTokens = 5000,
///     IncludeAxioms = true,
///     IncludeRelationships = true,
///     MinRelevanceScore = 0.4f
/// };
///
/// // Applying license restrictions via with-expression
/// var restricted = editorConfig with
/// {
///     MaxEntities = Math.Min(editorConfig.MaxEntities, 10),
///     IncludeAxioms = false,
///     IncludeRelationships = false
/// };
/// </code>
/// </example>
internal sealed record KnowledgeContextConfig
{
    /// <summary>
    /// Maximum tokens for the formatted knowledge context output.
    /// </summary>
    /// <value>Defaults to 4000 tokens.</value>
    /// <remarks>
    /// LOGIC: Controls the token budget allocated to knowledge context.
    /// The ranker's <c>SelectWithinBudget</c> method uses this to determine
    /// how many entities can fit within the allotted space.
    /// </remarks>
    public int MaxTokens { get; init; } = 4000;

    /// <summary>
    /// Entity types to include in the search (null = all types).
    /// </summary>
    /// <value>
    /// A list of entity type names (e.g., "Concept", "Term", "Endpoint"),
    /// or null to include all types.
    /// </value>
    /// <remarks>
    /// LOGIC: Type filtering is applied during the search phase before
    /// relevance ranking. Converted to <c>IReadOnlySet&lt;string&gt;</c>
    /// for <see cref="EntitySearchQuery.EntityTypes"/> compatibility.
    /// Case-sensitive matching.
    /// </remarks>
    public IReadOnlyList<string>? IncludeEntityTypes { get; init; }

    /// <summary>
    /// Minimum relevance score threshold for entity inclusion.
    /// </summary>
    /// <value>Defaults to 0.5f.</value>
    /// <remarks>
    /// LOGIC: Entities with relevance scores (from <c>IEntityRelevanceRanker</c>)
    /// below this threshold are excluded from the context fragment.
    /// A higher threshold produces more focused but potentially sparser context.
    /// </remarks>
    public float MinRelevanceScore { get; init; } = 0.5f;

    /// <summary>
    /// Whether to include relationships between selected entities.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// LOGIC: When enabled, retrieves all relationships where both
    /// endpoints are in the selected entity set. Disabled for
    /// WriterPro tier (entities-only access) and for agents
    /// that prioritize simplicity (e.g., Simplifier).
    /// </remarks>
    public bool IncludeRelationships { get; init; } = true;

    /// <summary>
    /// Whether to include applicable axioms (domain rules).
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    /// <remarks>
    /// LOGIC: When enabled, retrieves axioms targeting the entity types
    /// present in the context via <c>IAxiomStore.GetAxiomsForType</c>.
    /// Requires Teams+ license tier. Disabled for WriterPro tier
    /// and for agents that don't need rule context (e.g., Simplifier, Summarizer).
    /// </remarks>
    public bool IncludeAxioms { get; init; } = true;

    /// <summary>
    /// Maximum number of entities to include in the context.
    /// </summary>
    /// <value>Defaults to 20 entities.</value>
    /// <remarks>
    /// LOGIC: The search over-fetches by 3x this value to give the
    /// relevance ranker sufficient candidates for selection and budget
    /// trimming. WriterPro tier caps this at 10 regardless of config.
    /// </remarks>
    public int MaxEntities { get; init; } = 20;

    /// <summary>
    /// Output format for the formatted knowledge context string.
    /// </summary>
    /// <value>Defaults to <see cref="ContextFormat.Yaml"/>.</value>
    /// <remarks>
    /// LOGIC: Determines the formatting style passed to
    /// <c>IKnowledgeContextFormatter.FormatContext</c> via
    /// <see cref="ContextFormatOptions.Format"/>.
    /// YAML is the default for structured yet token-efficient output.
    /// </remarks>
    public ContextFormat Format { get; init; } = ContextFormat.Yaml;

    /// <summary>
    /// Maximum number of properties to display per entity.
    /// </summary>
    /// <value>Defaults to 10 properties.</value>
    /// <remarks>
    /// LOGIC: Limits property display to conserve token budget.
    /// Passed to <see cref="ContextFormatOptions.MaxPropertiesPerEntity"/>.
    /// </remarks>
    public int MaxPropertiesPerEntity { get; init; } = 10;
}
