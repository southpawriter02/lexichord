// =============================================================================
// File: ConsolidatedFix.cs
// Project: Lexichord.Abstractions
// Description: A consolidated fix combining multiple related fix suggestions.
// =============================================================================
// LOGIC: Groups multiple ValidationFinding instances that share the same
//   SuggestedFix text into a single actionable fix. The TextEdit record is
//   defined here for future span-based editing but is not populated by the
//   current consolidator (SuggestedFix is string?, not a ValidationFix record).
//
// v0.6.5i: Validation Result Aggregator (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Aggregation;

/// <summary>
/// A consolidated fix that addresses multiple related <see cref="ValidationFinding"/>
/// instances sharing a common resolution.
/// </summary>
/// <remarks>
/// <para>
/// When multiple validators produce findings with identical or similar suggested
/// fixes, the <see cref="IFixConsolidator"/> merges them into a single
/// <see cref="ConsolidatedFix"/> to reduce noise and simplify the user's fix workflow.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public record ConsolidatedFix
{
    /// <summary>
    /// Unique identifier for this consolidated fix.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable description of the fix action.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// The findings that this fix addresses.
    /// </summary>
    public required IReadOnlyList<ValidationFinding> AffectedFindings { get; init; }

    /// <summary>
    /// Text edits to apply for this fix. Defaults to empty when span-based
    /// editing data is unavailable.
    /// </summary>
    public IReadOnlyList<TextEdit> Edits { get; init; } = [];

    /// <summary>
    /// Overall confidence in this fix (0.0–1.0).
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether this fix can be automatically applied without user confirmation.
    /// </summary>
    public bool CanAutoApply { get; init; }

    /// <summary>
    /// Number of findings resolved by applying this fix.
    /// </summary>
    public int FindingsResolved => AffectedFindings.Count;
}

/// <summary>
/// A text edit operation representing a span replacement in a document.
/// </summary>
/// <remarks>
/// <para>
/// Reserved for future use when <see cref="ValidationFinding"/> gains span-based
/// fix data. Currently, <see cref="ConsolidatedFix.Edits"/> defaults to empty.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5i as part of the Validation Result Aggregator.
/// </para>
/// </remarks>
public record TextEdit
{
    /// <summary>
    /// The text span to replace.
    /// </summary>
    public required TextSpan Span { get; init; }

    /// <summary>
    /// The replacement text to insert at the span location.
    /// </summary>
    public required string NewText { get; init; }
}
