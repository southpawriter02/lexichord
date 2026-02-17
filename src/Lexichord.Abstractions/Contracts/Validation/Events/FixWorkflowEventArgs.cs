// =============================================================================
// File: FixWorkflowEventArgs.cs
// Project: Lexichord.Abstractions
// Description: Event arguments for fix workflow events.
// =============================================================================
// LOGIC: Provides event args for FixesApplied and ConflictDetected events
//   raised by the fix orchestrator during fix application.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation.Events;

/// <summary>
/// Event arguments raised when fixes are successfully applied.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Raised by <see cref="IUnifiedFixWorkflow.FixesApplied"/> after
/// one or more fixes are applied to the document. Includes the full result
/// with resolved issues, remaining issues, and metrics.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is immutable after construction and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="IUnifiedFixWorkflow.FixesApplied"/>
/// <seealso cref="FixApplyResult"/>
public class FixesAppliedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the document that was fixed.
    /// </summary>
    public string DocumentPath { get; }

    /// <summary>
    /// Gets the fix application result with detailed metrics.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the full <see cref="FixApplyResult"/> including
    /// applied count, resolved issues, remaining issues from re-validation,
    /// and any conflicts that were skipped.
    /// </remarks>
    public FixApplyResult Result { get; }

    /// <summary>
    /// Gets the UTC timestamp when fixes were applied.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixesAppliedEventArgs"/> class.
    /// </summary>
    /// <param name="documentPath">The path to the document that was fixed.</param>
    /// <param name="result">The fix application result.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="result"/> is null.
    /// </exception>
    public FixesAppliedEventArgs(string documentPath, FixApplyResult result)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(result);
        DocumentPath = documentPath;
        Result = result;
        Timestamp = DateTime.UtcNow;
    }
}

/// <summary>
/// Event arguments raised when conflicts are detected between fixes.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Raised by <see cref="IUnifiedFixWorkflow.ConflictDetected"/>
/// when the conflict detector finds overlapping, contradictory, or dependent
/// fixes. UI components can subscribe to display conflict resolution dialogs.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is immutable after construction and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="IUnifiedFixWorkflow.ConflictDetected"/>
/// <seealso cref="FixConflictCase"/>
public class FixConflictDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the document where conflicts were detected.
    /// </summary>
    public string DocumentPath { get; }

    /// <summary>
    /// Gets the list of detected conflicts.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains all <see cref="FixConflictCase"/> instances detected
    /// during pre-application analysis. May include both blocking (Error) and
    /// non-blocking (Warning, Info) conflicts.
    /// </remarks>
    public IReadOnlyList<FixConflictCase> Conflicts { get; }

    /// <summary>
    /// Gets the UTC timestamp when conflicts were detected.
    /// </summary>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Gets the number of blocking conflicts.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Blocking conflicts have severity <see cref="FixConflictSeverity.Error"/>
    /// and prevent fix application when using
    /// <see cref="ConflictHandlingStrategy.ThrowException"/>.
    /// </remarks>
    public int BlockingConflictCount => Conflicts.Count(c => c.IsBlocking);

    /// <summary>
    /// Gets whether any blocking conflicts were detected.
    /// </summary>
    public bool HasBlockingConflicts => Conflicts.Any(c => c.IsBlocking);

    /// <summary>
    /// Initializes a new instance of the <see cref="FixConflictDetectedEventArgs"/> class.
    /// </summary>
    /// <param name="documentPath">The path to the document.</param>
    /// <param name="conflicts">The detected conflicts.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/> or <paramref name="conflicts"/> is null.
    /// </exception>
    public FixConflictDetectedEventArgs(string documentPath, IReadOnlyList<FixConflictCase> conflicts)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(conflicts);
        DocumentPath = documentPath;
        Conflicts = conflicts;
        Timestamp = DateTime.UtcNow;
    }
}
