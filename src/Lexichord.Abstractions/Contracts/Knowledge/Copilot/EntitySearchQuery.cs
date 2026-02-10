// =============================================================================
// File: EntitySearchQuery.cs
// Project: Lexichord.Abstractions
// Description: Search query parameters for knowledge graph entity search.
// =============================================================================
// LOGIC: Defines the query contract for searching entities in the knowledge
//   graph. Used by IGraphRepository.SearchEntitiesAsync to filter entities
//   by text query, maximum results, and optional entity type constraints.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Search query parameters for knowledge graph entity search.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntitySearchQuery"/> record defines the filtering criteria
/// for searching entities in the knowledge graph. It supports text-based
/// query matching, result limiting, and optional entity type filtering.
/// </para>
/// <para>
/// <b>Usage:</b> Used by <see cref="IGraphRepository.SearchEntitiesAsync"/>
/// to retrieve candidate entities for relevance ranking.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var query = new EntitySearchQuery
/// {
///     Query = "GET /api/users",
///     MaxResults = 40,
///     EntityTypes = new HashSet&lt;string&gt; { "Endpoint", "Parameter" }
/// };
/// var results = await graphRepository.SearchEntitiesAsync(query, ct);
/// </code>
/// </example>
public record EntitySearchQuery
{
    /// <summary>
    /// The text query to search for in entity names and properties.
    /// </summary>
    /// <value>
    /// A non-null search string. Entity names, types, and property values
    /// are matched against the terms extracted from this query.
    /// </value>
    /// <remarks>
    /// LOGIC: The query is split into terms by whitespace and common
    /// delimiters. Each term is matched case-insensitively against
    /// entity fields.
    /// </remarks>
    public required string Query { get; init; }

    /// <summary>
    /// Maximum number of entities to return.
    /// </summary>
    /// <value>
    /// A positive integer. Defaults to 40 to allow over-fetching for
    /// subsequent relevance ranking.
    /// </value>
    /// <remarks>
    /// LOGIC: Over-fetching (e.g., 2x the desired count) is recommended
    /// to ensure the relevance ranker has sufficient candidates.
    /// </remarks>
    public int MaxResults { get; init; } = 40;

    /// <summary>
    /// Optional set of entity types to filter by.
    /// </summary>
    /// <value>
    /// A set of entity type names (e.g., "Endpoint", "Parameter").
    /// When null, all entity types are included in the search.
    /// </value>
    /// <remarks>
    /// LOGIC: Type filtering is applied before text matching for
    /// performance. Type names are case-sensitive.
    /// </remarks>
    public IReadOnlySet<string>? EntityTypes { get; init; }
}
