// =============================================================================
// File: AxiomFilter.cs
// Project: Lexichord.Abstractions
// Description: Filter criteria for querying axioms from the repository.
// =============================================================================
// LOGIC: Used with IAxiomRepository.GetAsync to filter axioms by:
//   - TargetType: Entity type the axiom applies to (e.g., "Endpoint").
//   - Category: Logical grouping of axioms (e.g., "API Documentation").
//   - Tags: Optional tags for fine-grained filtering.
//   - IsEnabled: Filter by enabled/disabled status.
//
// v0.4.6f: Axiom Repository (CKVS Phase 1c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// Filter criteria for querying axioms from the repository.
/// </summary>
/// <remarks>
/// <para>
/// Use this record with <see cref="IAxiomRepository.GetAsync"/> to query
/// axioms matching specific criteria. All properties are optional; unset
/// properties are not included in the filter.
/// </para>
/// <para>
/// <b>Example:</b>
/// <code>
/// var filter = new AxiomFilter
/// {
///     TargetType = "Endpoint",
///     IsEnabled = true
/// };
/// var axioms = await repository.GetAsync(filter);
/// </code>
/// </para>
/// </remarks>
public record AxiomFilter
{
    /// <summary>
    /// Gets the target entity type to filter by.
    /// </summary>
    /// <value>
    /// The entity type name (e.g., "Endpoint", "Concept"), or <c>null</c> for any type.
    /// </value>
    public string? TargetType { get; init; }

    /// <summary>
    /// Gets the category to filter by.
    /// </summary>
    /// <value>
    /// The category name (e.g., "API Documentation"), or <c>null</c> for any category.
    /// </value>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the tags to filter by.
    /// </summary>
    /// <value>
    /// A list of tags where all must match, or <c>null</c> for any tags.
    /// </value>
    /// <remarks>
    /// LOGIC: Tag matching uses JSONB containment. The axiom must have ALL
    /// specified tags (AND logic), but may have additional tags.
    /// </remarks>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Gets the enabled status to filter by.
    /// </summary>
    /// <value>
    /// <c>true</c> for enabled only, <c>false</c> for disabled only,
    /// or <c>null</c> for any status.
    /// </value>
    public bool? IsEnabled { get; init; }
}
