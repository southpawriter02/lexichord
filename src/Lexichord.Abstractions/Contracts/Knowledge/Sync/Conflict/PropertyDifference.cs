// =============================================================================
// File: PropertyDifference.cs
// Project: Lexichord.Abstractions
// Description: Record representing a difference in a single property.
// =============================================================================
// LOGIC: When comparing entities, individual property differences are
//   captured in PropertyDifference records. This enables granular
//   conflict detection and resolution at the property level.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;

/// <summary>
/// Represents a difference in a single property between document and graph values.
/// </summary>
/// <remarks>
/// <para>
/// Captures details about a property-level difference:
/// </para>
/// <list type="bullet">
///   <item><b>PropertyName:</b> The name of the property that differs.</item>
///   <item><b>DocumentValue:</b> The value from the document extraction.</item>
///   <item><b>GraphValue:</b> The value from the knowledge graph.</item>
///   <item><b>Confidence:</b> Confidence score for the comparison (0.0-1.0).</item>
/// </list>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var diff = new PropertyDifference
/// {
///     PropertyName = "Description",
///     DocumentValue = "Updated description",
///     GraphValue = "Original description",
///     Confidence = 0.85f
/// };
/// </code>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public record PropertyDifference
{
    /// <summary>
    /// The name of the property that differs.
    /// </summary>
    /// <value>
    /// The property name as a string. Must not be null or empty.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to identify which property on the entity is in conflict.
    /// Should match the property name in the KnowledgeEntity.Properties dictionary.
    /// </remarks>
    public required string PropertyName { get; init; }

    /// <summary>
    /// The value from the document extraction.
    /// </summary>
    /// <value>
    /// The property value as extracted from the document.
    /// May be null if the property was not present in the document.
    /// </value>
    /// <remarks>
    /// LOGIC: Boxed as object to support any value type (string, number, etc.).
    /// Null indicates the property was not found in the document extraction.
    /// </remarks>
    public object? DocumentValue { get; init; }

    /// <summary>
    /// The value from the knowledge graph.
    /// </summary>
    /// <value>
    /// The property value as stored in the graph.
    /// May be null if the property was not present in the graph entity.
    /// </value>
    /// <remarks>
    /// LOGIC: Boxed as object to support any value type.
    /// Null indicates the property was not found in the graph entity.
    /// </remarks>
    public object? GraphValue { get; init; }

    /// <summary>
    /// Confidence score for the comparison.
    /// </summary>
    /// <value>
    /// A value between 0.0 and 1.0 indicating comparison confidence.
    /// Higher values indicate higher confidence in the detected difference.
    /// </value>
    /// <remarks>
    /// LOGIC: Used to determine resolution strategy.
    /// High confidence (≥0.8) differences may be auto-resolved.
    /// Low confidence differences require manual review.
    /// </remarks>
    public float Confidence { get; init; }
}
