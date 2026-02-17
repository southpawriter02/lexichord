// =============================================================================
// File: FixWorkflowOptions.cs
// Project: Lexichord.Abstractions
// Description: Options for controlling fix workflow behavior.
// =============================================================================
// LOGIC: Configures how the fix orchestrator handles conflict resolution,
//   re-validation, undo support, timeouts, and verbose tracing.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Options for controlling fix workflow behavior.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record controls all aspects of fix workflow execution:
/// <list type="bullet">
///   <item><description><see cref="DryRun"/>: Simulate without modifying document</description></item>
///   <item><description><see cref="ConflictStrategy"/>: How to handle detected conflicts</description></item>
///   <item><description><see cref="ReValidateAfterFixes"/>: Re-check document after fixes</description></item>
///   <item><description><see cref="MaxFixIterations"/>: Limit re-validation cycles</description></item>
///   <item><description><see cref="EnableUndo"/>: Maintain undo stack</description></item>
///   <item><description><see cref="Timeout"/>: Maximum execution time</description></item>
///   <item><description><see cref="Verbose"/>: Enable detailed trace logging</description></item>
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
/// <seealso cref="FixApplyResult"/>
/// <seealso cref="ConflictHandlingStrategy"/>
public record FixWorkflowOptions
{
    /// <summary>
    /// If true, simulates fix application without modifying the document.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, the orchestrator calculates what would be applied
    /// but does not call <c>IEditorService.DeleteText()</c> or <c>InsertText()</c>.
    /// The resulting <see cref="FixApplyResult"/> shows which fixes would be applied
    /// and any detected conflicts.
    /// Default: <c>false</c>.
    /// </remarks>
    public bool DryRun { get; init; }

    /// <summary>
    /// How to handle detected conflicts between fixes.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Determines the orchestrator's behavior when
    /// <see cref="IUnifiedFixWorkflow.DetectConflicts"/> finds overlapping or
    /// contradictory fixes. Default: <see cref="ConflictHandlingStrategy.SkipConflicting"/>.
    /// </remarks>
    public ConflictHandlingStrategy ConflictStrategy { get; init; } = ConflictHandlingStrategy.SkipConflicting;

    /// <summary>
    /// If true, re-validates the document after fixes are applied.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> After applying fixes, the orchestrator calls
    /// <c>IUnifiedValidationService.ValidateAsync()</c> to detect any new issues
    /// introduced by the fixes. Results appear in <see cref="FixApplyResult.RemainingIssues"/>.
    /// Default: <c>true</c>.
    /// </remarks>
    public bool ReValidateAfterFixes { get; init; } = true;

    /// <summary>
    /// Maximum number of fix-then-revalidate iterations.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Prevents infinite loops when fixes continually create new issues.
    /// After this many iterations, the workflow stops and returns results with any
    /// remaining issues. Default: <c>3</c>.
    /// </remarks>
    public int MaxFixIterations { get; init; } = 3;

    /// <summary>
    /// If true, maintains an undo stack for this operation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, the orchestrator records a <see cref="FixTransaction"/>
    /// on the undo stack, allowing <see cref="IUnifiedFixWorkflow.UndoLastFixesAsync"/>
    /// to restore the document to its pre-fix state. Also pushes to
    /// <c>IUndoRedoService</c> if available.
    /// Default: <c>true</c>.
    /// </remarks>
    public bool EnableUndo { get; init; } = true;

    /// <summary>
    /// Timeout for the entire fix workflow.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> If the workflow exceeds this duration, it throws
    /// <see cref="FixApplicationTimeoutException"/> with the number of fixes
    /// applied so far. Default: 5 seconds.
    /// </remarks>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// If true, logs detailed fix application trace.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When enabled, each fix operation is recorded as a
    /// <see cref="FixOperationTrace"/> entry in <see cref="FixApplyResult.OperationTrace"/>.
    /// Useful for debugging fix application issues.
    /// Default: <c>false</c>.
    /// </remarks>
    public bool Verbose { get; init; }

    /// <summary>
    /// Gets the default options for the fix workflow.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Returns a new instance with all default values:
    /// <c>DryRun=false</c>, <c>ConflictStrategy=SkipConflicting</c>,
    /// <c>ReValidateAfterFixes=true</c>, <c>MaxFixIterations=3</c>,
    /// <c>EnableUndo=true</c>, <c>Timeout=5s</c>, <c>Verbose=false</c>.
    /// </remarks>
    public static FixWorkflowOptions Default { get; } = new();
}
