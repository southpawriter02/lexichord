// =============================================================================
// File: FieldChange.cs
// Project: Lexichord.Abstractions
// Description: A change to a specific field within a claim.
// =============================================================================
// LOGIC: Captures detailed field-level changes for claim modifications.
//   Provides old and new values plus human-readable descriptions.
//
// v0.5.6i: Claim Diff Service (Knowledge Graph Change Tracking)
// Dependencies: None (pure data contract)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Claims.Diff;

/// <summary>
/// A change to a specific field within a claim.
/// </summary>
/// <remarks>
/// <para>
/// When a claim is modified, individual field changes are tracked for:
/// </para>
/// <list type="bullet">
///   <item><b>Audit:</b> Detailed change history for compliance.</item>
///   <item><b>Review:</b> Precise understanding of what changed.</item>
///   <item><b>Display:</b> User-friendly change descriptions.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.5.6i as part of the Claim Diff Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var fieldChange = new FieldChange
/// {
///     FieldName = "Confidence",
///     OldValue = 0.75f,
///     NewValue = 0.92f,
///     Description = "Confidence changed from 75% to 92%"
/// };
/// </code>
/// </example>
public record FieldChange
{
    /// <summary>
    /// Name of the changed field.
    /// </summary>
    /// <value>
    /// The property name (e.g., "Subject", "Object", "Confidence").
    /// </value>
    public required string FieldName { get; init; }

    /// <summary>
    /// Previous value of the field.
    /// </summary>
    /// <value>
    /// The old value, or null if not applicable. Type varies by field.
    /// </value>
    public object? OldValue { get; init; }

    /// <summary>
    /// New value of the field.
    /// </summary>
    /// <value>
    /// The new value, or null if not applicable. Type varies by field.
    /// </value>
    public object? NewValue { get; init; }

    /// <summary>
    /// Human-readable description of the change.
    /// </summary>
    /// <value>
    /// A formatted string describing what changed, suitable for display.
    /// </value>
    public required string Description { get; init; }
}
