// =============================================================================
// File: FixApplyResult.cs
// Project: Lexichord.Abstractions
// Description: Result of a fix workflow operation with metrics and traces.
// =============================================================================
// LOGIC: Captures the outcome of fix application including counts, resolved
//   issues, remaining issues, conflicts, errors, and optional operation traces.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Result of a fix workflow operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record captures the complete outcome of a fix workflow
/// operation, including:
/// <list type="bullet">
///   <item><description>Success/failure status and counts</description></item>
///   <item><description>Which issues were resolved vs. remaining</description></item>
///   <item><description>Detected conflicts and per-issue errors</description></item>
///   <item><description>Modified document content (if not dry-run)</description></item>
///   <item><description>Transaction ID for undo operations</description></item>
///   <item><description>Optional verbose operation trace</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="IUnifiedFixWorkflow"/>
/// <seealso cref="FixWorkflowOptions"/>
/// <seealso cref="FixOperationTrace"/>
public record FixApplyResult
{
    /// <summary>
    /// Gets whether all requested fixes were applied successfully.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> True when <see cref="FailedCount"/> is 0 and no blocking
    /// conflicts prevented application. False if any fixes failed, were skipped
    /// due to conflicts, or the operation timed out.
    /// </remarks>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the number of fixes actually applied to the document.
    /// </summary>
    public required int AppliedCount { get; init; }

    /// <summary>
    /// Gets the number of fixes skipped due to conflicts or filtering.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Includes fixes skipped by
    /// <see cref="ConflictHandlingStrategy.SkipConflicting"/> and fixes
    /// excluded by category/severity/ID filtering.
    /// </remarks>
    public required int SkippedCount { get; init; }

    /// <summary>
    /// Gets the number of fixes that could not be applied due to errors.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Fixes that threw exceptions during application.
    /// Details are available in <see cref="ErrorsByIssueId"/>.
    /// </remarks>
    public required int FailedCount { get; init; }

    /// <summary>
    /// Gets the modified document content after fixes, or null if dry-run or no fixes applied.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the full document text after all fixes were applied.
    /// Null when <see cref="FixWorkflowOptions.DryRun"/> is true or when no fixes
    /// were applied. For dry-run, use <see cref="AppliedCount"/> and
    /// <see cref="ResolvedIssues"/> to see what would have been fixed.
    /// </remarks>
    public string? ModifiedContent { get; init; }

    /// <summary>
    /// Gets the issues that were resolved by applied fixes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the <see cref="UnifiedIssue"/> instances whose fixes
    /// were successfully applied to the document.
    /// </remarks>
    public IReadOnlyList<UnifiedIssue> ResolvedIssues { get; init; } = [];

    /// <summary>
    /// Gets new issues detected after fixes (from re-validation).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When <see cref="FixWorkflowOptions.ReValidateAfterFixes"/> is true,
    /// this contains issues found by re-running validation on the modified document.
    /// Empty when re-validation is disabled or finds no new issues.
    /// </remarks>
    public IReadOnlyList<UnifiedIssue> RemainingIssues { get; init; } = [];

    /// <summary>
    /// Gets issues that were involved in conflicts and not applied.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Populated when <see cref="FixWorkflowOptions.ConflictStrategy"/>
    /// is <see cref="ConflictHandlingStrategy.SkipConflicting"/> or
    /// <see cref="ConflictHandlingStrategy.PriorityBased"/>.
    /// </remarks>
    public IReadOnlyList<UnifiedIssue> ConflictingIssues { get; init; } = [];

    /// <summary>
    /// Gets detailed error messages for fixes that failed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Maps <see cref="UnifiedIssue.IssueId"/> to the exception
    /// message that occurred during fix application.
    /// </remarks>
    public IReadOnlyDictionary<Guid, string> ErrorsByIssueId { get; init; } =
        new Dictionary<Guid, string>();

    /// <summary>
    /// Gets all conflicts detected between fixes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains every <see cref="FixConflictCase"/> found during
    /// pre-application analysis, regardless of conflict strategy.
    /// </remarks>
    public IReadOnlyList<FixConflictCase> DetectedConflicts { get; init; } = [];

    /// <summary>
    /// Gets the total duration of fix application.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Gets the transaction ID for undo operations.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Unique ID for this fix operation. Used to correlate
    /// with <see cref="FixTransaction"/> entries on the undo stack.
    /// </remarks>
    public Guid TransactionId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Gets the detailed trace of fix operations (populated when <see cref="FixWorkflowOptions.Verbose"/> is true).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains a per-fix trace of operations performed, including
    /// timestamps, operation descriptions, success/failure status, and error messages.
    /// Empty when verbose mode is disabled.
    /// </remarks>
    public IReadOnlyList<FixOperationTrace> OperationTrace { get; init; } = [];

    /// <summary>
    /// Gets the total number of fixes that were candidates for application.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Computed as <c>AppliedCount + SkippedCount + FailedCount</c>.
    /// </remarks>
    public int TotalCount => AppliedCount + SkippedCount + FailedCount;

    /// <summary>
    /// Gets whether any fixes were applied.
    /// </summary>
    public bool HasAppliedFixes => AppliedCount > 0;

    /// <summary>
    /// Gets whether re-validation found new issues.
    /// </summary>
    public bool HasRemainingIssues => RemainingIssues.Count > 0;

    /// <summary>
    /// Gets whether any conflicts were detected.
    /// </summary>
    public bool HasConflicts => DetectedConflicts.Count > 0;

    /// <summary>
    /// Creates an empty result for when there are no fixes to apply.
    /// </summary>
    /// <param name="duration">The duration of the operation.</param>
    /// <returns>An empty <see cref="FixApplyResult"/> with zero counts and success.</returns>
    public static FixApplyResult Empty(TimeSpan duration) => new()
    {
        Success = true,
        AppliedCount = 0,
        SkippedCount = 0,
        FailedCount = 0,
        Duration = duration
    };
}

/// <summary>
/// Trace of a single fix operation for debugging.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Recorded when <see cref="FixWorkflowOptions.Verbose"/> is true.
/// Provides a detailed timeline of what happened during fix application.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This record is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <param name="IssueId">The ID of the issue being fixed.</param>
/// <param name="Timestamp">When the operation occurred.</param>
/// <param name="Operation">Description of the operation performed.</param>
/// <param name="Status">Status of the operation (e.g., "Success", "Failed", "Skipped").</param>
/// <param name="ErrorMessage">Error message if the operation failed; null otherwise.</param>
public record FixOperationTrace(
    Guid IssueId,
    DateTime Timestamp,
    string Operation,
    string? Status,
    string? ErrorMessage);
