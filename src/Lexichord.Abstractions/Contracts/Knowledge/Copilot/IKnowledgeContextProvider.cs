// =============================================================================
// File: IKnowledgeContextProvider.cs
// Project: Lexichord.Abstractions
// Description: Interface for providing knowledge graph context to Co-pilot.
// =============================================================================
// LOGIC: Defines the contract for retrieving relevant knowledge context
//   from the Knowledge Graph for injection into Co-pilot prompts. Supports
//   query-based and entity-ID-based retrieval with configurable token budget,
//   relationship inclusion, and format options.
//
// v0.6.6e: Graph Context Provider (CKVS Phase 3b)
// Dependencies: IGraphRepository (v0.4.5e), IAxiomStore (v0.4.6-KG),
//               IClaimRepository (v0.5.6h), IEntityRelevanceRanker (v0.6.6e),
//               IKnowledgeContextFormatter (v0.6.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Copilot;

/// <summary>
/// Provides knowledge graph context for Co-pilot prompts.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IKnowledgeContextProvider"/> orchestrates knowledge retrieval
/// from the graph database, relevance ranking, and context formatting. It is
/// the primary entry point for the Co-pilot agent to obtain domain knowledge.
/// </para>
/// <para>
/// <b>Pipeline:</b>
/// <list type="number">
///   <item>Search for relevant entities via <see cref="IGraphRepository"/>.</item>
///   <item>Rank entities by relevance using <see cref="IEntityRelevanceRanker"/>.</item>
///   <item>Select entities within token budget.</item>
///   <item>Optionally retrieve relationships, axioms, and claims.</item>
///   <item>Format context using <see cref="IKnowledgeContextFormatter"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Error Handling:</b> If the graph is unavailable or the query returns
/// no results, <see cref="KnowledgeContext.Empty"/> is returned.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.6e as part of the Graph Context Provider.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var options = new KnowledgeContextOptions { MaxTokens = 1500 };
/// var context = await provider.GetContextAsync("GET /api/users", options, ct);
/// prompt.AppendLine(context.FormattedContext);
/// </code>
/// </example>
public interface IKnowledgeContextProvider
{
    /// <summary>
    /// Gets relevant knowledge context for a text query.
    /// </summary>
    /// <param name="query">The user's query text to match against entities.</param>
    /// <param name="options">Configuration for context retrieval.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="KnowledgeContext"/> containing matched entities, formatted
    /// context, and token count. Returns <see cref="KnowledgeContext.Empty"/>
    /// if no entities match.
    /// </returns>
    /// <remarks>
    /// LOGIC:
    /// <list type="number">
    ///   <item>Search entities with over-fetching (2x MaxEntities).</item>
    ///   <item>Rank by relevance using term matching.</item>
    ///   <item>Select within token budget.</item>
    ///   <item>Retrieve relationships/axioms/claims if requested.</item>
    ///   <item>Format using the configured output format.</item>
    /// </list>
    /// </remarks>
    Task<KnowledgeContext> GetContextAsync(
        string query,
        KnowledgeContextOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets knowledge context for specific entity IDs.
    /// </summary>
    /// <param name="entityIds">The entity IDs to retrieve context for.</param>
    /// <param name="options">Configuration for context retrieval.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="KnowledgeContext"/> containing the requested entities
    /// (up to <see cref="KnowledgeContextOptions.MaxEntities"/>), formatted
    /// context, and token count.
    /// </returns>
    /// <remarks>
    /// LOGIC: Retrieves entities by ID, skipping any that do not exist.
    /// No relevance ranking is performed since entities are explicitly
    /// requested. Relationships and axioms are included if requested.
    /// </remarks>
    Task<KnowledgeContext> GetContextForEntitiesAsync(
        IReadOnlyList<Guid> entityIds,
        KnowledgeContextOptions options,
        CancellationToken ct = default);
}
