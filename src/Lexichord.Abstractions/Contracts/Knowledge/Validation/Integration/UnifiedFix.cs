// =============================================================================
// File: UnifiedFix.cs
// Project: Lexichord.Abstractions
// Description: A fix that can originate from validation or linter.
// =============================================================================
// LOGIC: Wraps fix suggestions from either source. Validation findings have
//   a string SuggestedFix; linter violations have a string? Suggestion.
//   Both are presented as a single ReplacementText field.
//
// SPEC ADAPTATION:
//   - IReadOnlyList<TextEdit> Edits → string? ReplacementText
//     (neither source produces structured text edits at this level)
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// A fix suggestion that can originate from the CKVS Validation Engine
/// or the Lexichord style linter.
/// </summary>
/// <remarks>
/// <para>
/// Created by <see cref="IUnifiedFindingAdapter"/> when converting findings
/// that have suggested fixes. The fix is associated with a specific
/// <see cref="UnifiedFinding"/> via <see cref="FindingId"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public record UnifiedFix
{
    /// <summary>
    /// Unique identifier for this fix.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Which system produced this fix.
    /// </summary>
    public required FindingSource Source { get; init; }

    /// <summary>
    /// Human-readable description of what this fix does.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Replacement text to apply (null if the fix is descriptive only).
    /// </summary>
    public string? ReplacementText { get; init; }

    /// <summary>
    /// Confidence in the fix (0.0–1.0). Higher means more likely correct.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Whether this fix can be automatically applied without user review.
    /// </summary>
    public bool CanAutoApply { get; init; }

    /// <summary>
    /// The <see cref="UnifiedFinding.Id"/> this fix addresses.
    /// </summary>
    public Guid FindingId { get; init; }
}
