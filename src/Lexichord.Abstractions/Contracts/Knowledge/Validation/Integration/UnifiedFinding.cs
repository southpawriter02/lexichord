// =============================================================================
// File: UnifiedFinding.cs
// Project: Lexichord.Abstractions
// Description: A finding that can originate from validation or linter.
// =============================================================================
// LOGIC: Wraps findings from either source into a common shape. Retains
//   a reference to the original finding for drill-down, while exposing
//   normalized severity, category, and location.
//
// SPEC ADAPTATION:
//   - TextSpan? Location  → string? PropertyPath  (matches ValidationFinding)
//   - LinterFinding?      → StyleViolation?       (actual codebase type)
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// A finding that can originate from the CKVS Validation Engine or the
/// Lexichord style linter, presented in a unified format.
/// </summary>
/// <remarks>
/// <para>
/// Created by <see cref="IUnifiedFindingAdapter"/> from either a
/// <see cref="ValidationFinding"/> or a <see cref="StyleViolation"/>.
/// </para>
/// <para>
/// <b>Design Decision:</b> The <c>PropertyPath</c> field is a string
/// rather than a structured <c>TextSpan</c> because <see cref="ValidationFinding"/>
/// uses <c>PropertyPath</c> (a JSON-like path) while <see cref="StyleViolation"/>
/// uses offset-based positions. A string provides a lowest-common-denominator
/// representation for display purposes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public record UnifiedFinding
{
    /// <summary>
    /// Unique identifier for this finding.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Which system produced this finding.
    /// </summary>
    public required FindingSource Source { get; init; }

    /// <summary>
    /// Unified severity after normalization.
    /// </summary>
    public required UnifiedSeverity Severity { get; init; }

    /// <summary>
    /// Finding code (e.g., "SCH001" for schema, rule ID for linter).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable message describing the finding.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Location in the document (property path for validation findings,
    /// line:column string for linter findings).
    /// </summary>
    public string? PropertyPath { get; init; }

    /// <summary>
    /// Category for grouping and filtering.
    /// </summary>
    public required FindingCategory Category { get; init; }

    /// <summary>
    /// Suggested fix, if available.
    /// </summary>
    public UnifiedFix? SuggestedFix { get; init; }

    /// <summary>
    /// Original validation finding (populated when <see cref="Source"/> is
    /// <see cref="FindingSource.Validation"/>).
    /// </summary>
    public ValidationFinding? OriginalValidationFinding { get; init; }

    /// <summary>
    /// Original style violation (populated when <see cref="Source"/> is
    /// <see cref="FindingSource.StyleLinter"/>).
    /// </summary>
    public StyleViolation? OriginalStyleViolation { get; init; }
}
