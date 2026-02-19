// =============================================================================
// File: IEntityComparer.cs
// Project: Lexichord.Abstractions
// Description: Interface for comparing entities to detect property differences.
// =============================================================================
// LOGIC: IEntityComparer provides entity-level comparison capabilities.
//   It compares document entities against graph entities to produce
//   a detailed list of property differences for conflict detection.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: EntityComparison, KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Service for comparing entities to detect property differences.
/// </summary>
/// <remarks>
/// <para>
/// Compares document entities against graph entities:
/// </para>
/// <list type="bullet">
///   <item>Identifies property-level differences.</item>
///   <item>Computes confidence scores for comparisons.</item>
///   <item>Produces structured comparison results.</item>
/// </list>
/// <para>
/// <b>Implementation:</b> See <c>EntityComparer</c> in Lexichord.Modules.Knowledge.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public interface IEntityComparer
{
    /// <summary>
    /// Compares a document entity against a graph entity.
    /// </summary>
    /// <param name="documentEntity">The entity from document extraction.</param>
    /// <param name="graphEntity">The entity from the knowledge graph.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// An <see cref="EntityComparison"/> containing all property differences.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Performs property-by-property comparison:
    /// </para>
    /// <list type="number">
    ///   <item>Compare standard properties (Name, Type, Value).</item>
    ///   <item>Compare custom properties from Properties dictionary.</item>
    ///   <item>Compute confidence for each difference.</item>
    ///   <item>Return structured comparison result.</item>
    /// </list>
    /// </remarks>
    Task<EntityComparison> CompareAsync(
        KnowledgeEntity documentEntity,
        KnowledgeEntity graphEntity,
        CancellationToken ct = default);
}
