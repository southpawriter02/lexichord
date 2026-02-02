// =============================================================================
// File: AxiomFix.cs
// Project: Lexichord.Abstractions
// Description: Represents a suggested fix for an axiom violation.
// =============================================================================
// LOGIC: When violations are detected, the system can suggest fixes. This
//   record captures the suggestion including confidence level and whether
//   it can be automatically applied.
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge;

/// <summary>
/// A suggested fix for an <see cref="AxiomViolation"/>.
/// </summary>
/// <remarks>
/// <para>
/// Provides actionable suggestions to resolve violations. The UI can display
/// these as quick-fix options, and some may be auto-applied if
/// <see cref="CanAutoApply"/> is true.
/// </para>
/// </remarks>
public record AxiomFix
{
    /// <summary>
    /// Human-readable description of the fix action.
    /// </summary>
    /// <example>"Add method property with value 'GET'"</example>
    public required string Description { get; init; }

    /// <summary>
    /// The property that should be modified, if applicable.
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// The suggested new value for the property.
    /// </summary>
    public object? NewValue { get; init; }

    /// <summary>
    /// Confidence score that this fix is correct (0.0 to 1.0).
    /// </summary>
    /// <value>
    /// A value between 0.0 (low confidence) and 1.0 (high confidence).
    /// Fixes with higher confidence may be prioritized or auto-applied.
    /// </value>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether this fix can be automatically applied without user review.
    /// </summary>
    /// <value>
    /// <c>true</c> if the fix is safe to auto-apply (high confidence, no side effects);
    /// otherwise, <c>false</c>.
    /// </value>
    public bool CanAutoApply { get; init; }
}
