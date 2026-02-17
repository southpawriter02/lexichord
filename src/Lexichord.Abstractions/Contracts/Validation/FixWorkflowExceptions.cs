// =============================================================================
// File: FixWorkflowExceptions.cs
// Project: Lexichord.Abstractions
// Description: Exception types for the Combined Fix Workflow.
// =============================================================================
// LOGIC: Defines specialized exception types for conflict detection, timeout,
//   and document corruption scenarios during fix application.
//
// v0.7.5h: Combined Fix Workflow (Unified Validation Feature)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Raised when unresolvable conflicts are detected between fixes.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Thrown by the fix orchestrator when
/// <see cref="FixWorkflowOptions.ConflictStrategy"/> is
/// <see cref="ConflictHandlingStrategy.ThrowException"/> and one or more
/// blocking conflicts (overlapping positions, contradictory suggestions) are
/// detected. The <see cref="Conflicts"/> property contains the full list of
/// detected conflicts for the caller to inspect and resolve.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="FixConflictCase"/>
/// <seealso cref="ConflictHandlingStrategy.ThrowException"/>
public class FixConflictException : Exception
{
    /// <summary>
    /// Gets the list of detected conflicts that caused this exception.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Contains all <see cref="FixConflictCase"/> instances detected
    /// during pre-application analysis. Callers can inspect these to determine
    /// which fixes conflict and decide how to proceed.
    /// </remarks>
    public IReadOnlyList<FixConflictCase> Conflicts { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixConflictException"/> class.
    /// </summary>
    /// <param name="conflicts">The detected conflicts.</param>
    /// <param name="message">A human-readable description of the conflicts.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="conflicts"/> or <paramref name="message"/> is null.
    /// </exception>
    public FixConflictException(
        IReadOnlyList<FixConflictCase> conflicts,
        string message) : base(message)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        Conflicts = conflicts;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixConflictException"/> class
    /// with an inner exception.
    /// </summary>
    /// <param name="conflicts">The detected conflicts.</param>
    /// <param name="message">A human-readable description of the conflicts.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public FixConflictException(
        IReadOnlyList<FixConflictCase> conflicts,
        string message,
        Exception innerException) : base(message, innerException)
    {
        ArgumentNullException.ThrowIfNull(conflicts);
        Conflicts = conflicts;
    }
}

/// <summary>
/// Raised when fix application exceeds the configured timeout.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Thrown when the fix workflow exceeds
/// <see cref="FixWorkflowOptions.Timeout"/>. The <see cref="AppliedCount"/>
/// property indicates how many fixes were applied before the timeout,
/// allowing the caller to decide whether to keep partial results.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
/// <seealso cref="FixWorkflowOptions.Timeout"/>
public class FixApplicationTimeoutException : OperationCanceledException
{
    /// <summary>
    /// Gets the number of fixes that were applied before the timeout.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Indicates partial progress. If non-zero, some fixes
    /// were applied to the document before the timeout occurred.
    /// </remarks>
    public int AppliedCount { get; }

    /// <summary>
    /// Gets the duration that elapsed before the timeout.
    /// </summary>
    public TimeSpan ElapsedDuration { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FixApplicationTimeoutException"/> class.
    /// </summary>
    /// <param name="appliedCount">Number of fixes applied before timeout.</param>
    /// <param name="duration">Duration that elapsed before timeout.</param>
    public FixApplicationTimeoutException(int appliedCount, TimeSpan duration)
        : base($"Fix application timed out after {duration.TotalSeconds:F1}s " +
               $"with {appliedCount} fixes applied")
    {
        AppliedCount = appliedCount;
        ElapsedDuration = duration;
    }
}

/// <summary>
/// Raised when the document becomes invalid during fix application.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Thrown when the document content is in an unexpected state
/// during fix application — for example, when a fix's location references
/// text that no longer matches the expected <see cref="UnifiedFix.OldText"/>.
/// This typically indicates that the document was modified externally between
/// validation and fix application.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5h as part of the Combined Fix Workflow feature.
/// </para>
/// </remarks>
public class DocumentCorruptionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentCorruptionException"/> class.
    /// </summary>
    /// <param name="message">A description of the corruption detected.</param>
    public DocumentCorruptionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentCorruptionException"/> class
    /// with an inner exception.
    /// </summary>
    /// <param name="message">A description of the corruption detected.</param>
    /// <param name="innerException">The exception that caused this exception.</param>
    public DocumentCorruptionException(string message, Exception innerException)
        : base(message, innerException) { }
}
