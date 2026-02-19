// =============================================================================
// File: EntityComparison.cs
// Project: Lexichord.Abstractions
// Description: Record representing the comparison result between two entities.
// =============================================================================
// LOGIC: When comparing a document entity against a graph entity,
//   the EntityComparison captures all property-level differences.
//   This enables detailed conflict analysis and targeted resolution.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: PropertyDifference (v0.7.6h), KnowledgeEntity (v0.4.5e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Represents the comparison result between document and graph entities.
/// </summary>
/// <remarks>
/// <para>
/// Captures the full comparison between two entity versions:
/// </para>
/// <list type="bullet">
///   <item><b>DocumentEntity:</b> The entity from document extraction.</item>
///   <item><b>GraphEntity:</b> The entity from the knowledge graph.</item>
///   <item><b>PropertyDifferences:</b> List of property-level differences.</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var comparison = await entityComparer.CompareAsync(docEntity, graphEntity);
/// if (comparison.HasDifferences)
/// {
///     foreach (var diff in comparison.PropertyDifferences)
///     {
///         Console.WriteLine($"Property {diff.PropertyName} differs");
///     }
/// }
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record EntityComparison
{
    /// <summary>
    /// The entity from document extraction.
    /// </summary>
    /// <value>
    /// The KnowledgeEntity as extracted from the document.
    /// </value>
    /// <remarks>
    /// LOGIC: Represents the "current" or "proposed" state from the document.
    /// Used as the source of truth when applying DocumentFirst strategy.
    /// </remarks>
    public required KnowledgeEntity DocumentEntity { get; init; }

    /// <summary>
    /// The entity from the knowledge graph.
    /// </summary>
    /// <value>
    /// The KnowledgeEntity as stored in the graph.
    /// </value>
    /// <remarks>
    /// LOGIC: Represents the "existing" state in the graph.
    /// Used as the source of truth when applying GraphFirst strategy.
    /// </remarks>
    public required KnowledgeEntity GraphEntity { get; init; }

    /// <summary>
    /// List of property-level differences between the entities.
    /// </summary>
    /// <value>
    /// A read-only list of <see cref="PropertyDifference"/> records.
    /// Empty if the entities are identical.
    /// </value>
    /// <remarks>
    /// LOGIC: Each difference represents a single property conflict.
    /// The list is ordered by property name for consistent processing.
    /// </remarks>
    public IReadOnlyList<PropertyDifference> PropertyDifferences { get; init; } = [];

    /// <summary>
    /// Indicates whether there are any differences between the entities.
    /// </summary>
    /// <value>True if any property differences exist; otherwise, false.</value>
    /// <remarks>
    /// LOGIC: Quick check to determine if conflict resolution is needed.
    /// When false, the entities are considered identical.
    /// </remarks>
    public bool HasDifferences => PropertyDifferences.Count > 0;

    /// <summary>
    /// The number of property differences detected.
    /// </summary>
    /// <value>Count of differences in <see cref="PropertyDifferences"/>.</value>
    public int DifferenceCount => PropertyDifferences.Count;
}
