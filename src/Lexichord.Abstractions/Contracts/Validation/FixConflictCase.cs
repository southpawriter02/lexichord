// =============================================================================
// File: FixConflictCase.cs
// Project: Lexichord.Abstractions
// Description: Represents a conflict detected between two or more fixes.
// =============================================================================
// LOGIC: Captures conflict details including type, involved issues, description,
//   and severity for conflict resolution in the fix workflow.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Represents a conflict detected between two or more fixes.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record is produced by <see cref="IUnifiedFixWorkflow.DetectConflicts"/>
/// and consumed by the conflict handling strategy in <see cref="FixWorkflowOptions.ConflictStrategy"/>.
/// Each instance describes one conflict between a set of issues:
/// <list type="bullet">
///   <item><description><see cref="Type"/>: What kind of conflict was detected</description></item>
///   <item><description><see cref="ConflictingIssueIds"/>: Which issues are involved</description></item>
///   <item><description><see cref="Severity"/>: How critical the conflict is</description></item>
///   <item><description><see cref="SuggestedResolution"/>: Human-readable resolution hint</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="FixConflictType"/>
/// <seealso cref="FixConflictSeverity"/>
/// <seealso cref="IUnifiedFixWorkflow.DetectConflicts"/>
public record FixConflictCase
{
    /// <summary>
    /// Gets the type of conflict detected.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Categorizes the conflict for appropriate handling.
    /// <see cref="FixConflictType.OverlappingPositions"/> and
    /// <see cref="FixConflictType.ContradictorySuggestions"/> are the most
    /// common types detected during pre-application analysis.
    /// </remarks>
    public required FixConflictType Type { get; init; }

    /// <summary>
    /// Gets the IDs of issues involved in the conflict.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the <see cref="UnifiedIssue.IssueId"/> values of
    /// all issues participating in this conflict. Typically 2 issues, but
    /// contradictory suggestions at the same location may involve more.
    /// </remarks>
    public required IReadOnlyList<Guid> ConflictingIssueIds { get; init; }

    /// <summary>
    /// Gets a human-readable description of the conflict.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Describes the conflict in terms suitable for logging
    /// and UI display, including position information and affected rule IDs.
    /// </remarks>
    public required string Description { get; init; }

    /// <summary>
    /// Gets an optional suggestion for how to resolve the conflict.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When available, provides a human-readable suggestion
    /// for resolving the conflict. For example, "Apply Style fixes before
    /// Grammar fixes" for dependent fix conflicts.
    /// </remarks>
    public string? SuggestedResolution { get; init; }

    /// <summary>
    /// Gets the severity level of this conflict.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Determines whether the conflict blocks fix application:
    /// <list type="bullet">
    ///   <item><description><see cref="FixConflictSeverity.Error"/>: Blocks application (overlapping, contradictory)</description></item>
    ///   <item><description><see cref="FixConflictSeverity.Warning"/>: Logged but doesn't block (dependent)</description></item>
    ///   <item><description><see cref="FixConflictSeverity.Info"/>: Informational only</description></item>
    /// </list>
    /// </remarks>
    public FixConflictSeverity Severity { get; init; }

    /// <summary>
    /// Gets whether this conflict is an error that blocks fix application.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns true when <see cref="Severity"/> is
    /// <see cref="FixConflictSeverity.Error"/>. Used by the conflict handling
    /// strategy to determine which conflicts require resolution.
    /// </remarks>
    public bool IsBlocking => Severity == FixConflictSeverity.Error;
}
