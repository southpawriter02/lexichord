// =============================================================================
// File: ConflictHandlingStrategy.cs
// Project: Lexichord.Abstractions
// Description: Strategy for handling conflicts between fixes in the fix workflow.
// =============================================================================
// LOGIC: Defines how the fix orchestrator should handle detected conflicts
//   between fixes. Options range from fail-fast (ThrowException) to automatic
//   resolution (PriorityBased).
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Strategy for handling conflicts between fixes during the fix workflow.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> When the <see cref="IUnifiedFixWorkflow"/> detects conflicts
/// between fixes (overlapping positions, contradictory suggestions, etc.), this
/// enum determines how to proceed:
/// <list type="bullet">
///   <item><description><see cref="ThrowException"/>: Fail-fast, no fixes applied</description></item>
///   <item><description><see cref="SkipConflicting"/>: Apply non-conflicting fixes only</description></item>
///   <item><description><see cref="PromptUser"/>: Defer to user for resolution</description></item>
///   <item><description><see cref="PriorityBased"/>: Auto-resolve by severity priority</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Enum values are immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="FixWorkflowOptions"/>
/// <seealso cref="FixConflictCase"/>
/// <seealso cref="IUnifiedFixWorkflow"/>
public enum ConflictHandlingStrategy
{
    /// <summary>
    /// Throw a <see cref="FixConflictException"/> when conflicts are detected.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Fail-fast strategy. No fixes are applied. The caller receives
    /// the full list of conflicts in the exception and can decide how to proceed.
    /// This is the default strategy.
    /// </remarks>
    ThrowException = 0,

    /// <summary>
    /// Skip conflicting fixes and apply the rest.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Non-conflicting fixes are applied normally. Conflicting fixes
    /// are reported in <see cref="FixApplyResult.ConflictingIssues"/> and
    /// <see cref="FixApplyResult.SkippedCount"/>.
    /// </remarks>
    SkipConflicting = 1,

    /// <summary>
    /// Ask the user to resolve conflicts (requires interactive mode).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Raises the <see cref="IUnifiedFixWorkflow.ConflictDetected"/>
    /// event for each conflict, allowing the UI to present resolution options.
    /// Currently treated as <see cref="SkipConflicting"/> since no interactive
    /// resolution UI exists yet.
    /// </remarks>
    PromptUser = 2,

    /// <summary>
    /// Automatically resolve conflicts by applying fixes in severity priority order.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When two fixes conflict, the fix for the higher-severity issue
    /// (Error > Warning > Info > Hint) is kept and the other is skipped. For equal
    /// severity, the fix with higher confidence is preferred.
    /// </remarks>
    PriorityBased = 3
}
