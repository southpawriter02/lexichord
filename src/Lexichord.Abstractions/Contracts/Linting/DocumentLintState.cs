namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents the current linting state for a single document.
/// </summary>
/// <remarks>
/// LOGIC: Immutable state container tracking per-document lint lifecycle.
/// State transitions occur via builder methods that return new instances.
/// This enables safe concurrent access and state history if needed.
///
/// Version: v0.2.3a
/// </remarks>
public sealed record DocumentLintState
{
    /// <summary>
    /// The unique identifier for the document.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// The file path of the document (for logging/display).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// The current state of the document's lint lifecycle.
    /// </summary>
    public LintLifecycleState State { get; init; } = LintLifecycleState.Idle;

    /// <summary>
    /// When the most recent lint operation completed (if ever).
    /// </summary>
    public DateTimeOffset? LastLintTime { get; init; }

    /// <summary>
    /// Total number of completed lint operations for this document.
    /// </summary>
    public int LintCount { get; init; }

    /// <summary>
    /// Violations from the most recent completed lint (empty if never linted).
    /// </summary>
    public IReadOnlyList<StyleViolation> LastViolations { get; init; } = [];

    /// <summary>
    /// Creates a new state indicating a pending lint operation.
    /// </summary>
    /// <returns>A new state with State set to Pending.</returns>
    /// <remarks>
    /// LOGIC: Called when content changes are detected and debounce starts.
    /// </remarks>
    public DocumentLintState CreatePending()
        => this with { State = LintLifecycleState.Pending };

    /// <summary>
    /// Creates a new state indicating an active lint operation.
    /// </summary>
    /// <returns>A new state with State set to Analyzing.</returns>
    /// <remarks>
    /// LOGIC: Called when debounce completes and scan actually starts.
    /// </remarks>
    public DocumentLintState StartAnalyzing()
        => this with { State = LintLifecycleState.Analyzing };

    /// <summary>
    /// Creates a new state reflecting a completed lint operation.
    /// </summary>
    /// <param name="violations">The violations found during analysis.</param>
    /// <param name="timestamp">When the analysis completed.</param>
    /// <returns>A new state with updated results.</returns>
    /// <remarks>
    /// LOGIC: Called after successful scan completion.
    /// </remarks>
    public DocumentLintState CompleteWith(
        IReadOnlyList<StyleViolation> violations,
        DateTimeOffset timestamp)
        => this with
        {
            State = LintLifecycleState.Idle,
            LastLintTime = timestamp,
            LintCount = LintCount + 1,
            LastViolations = violations
        };

    /// <summary>
    /// Creates a new state reflecting a cancelled lint operation.
    /// </summary>
    /// <returns>A new state reset to Idle without updating results.</returns>
    /// <remarks>
    /// LOGIC: Called when a newer edit cancels an in-progress scan.
    /// Preserves previous violation results.
    /// </remarks>
    public DocumentLintState CancelToIdle()
        => this with { State = LintLifecycleState.Idle };
}

/// <summary>
/// The lifecycle state of a document's linting operation.
/// </summary>
public enum LintLifecycleState
{
    /// <summary>
    /// No lint operation pending or in progress.
    /// </summary>
    Idle,

    /// <summary>
    /// Content changed; waiting for debounce to complete.
    /// </summary>
    Pending,

    /// <summary>
    /// Active lint scan in progress.
    /// </summary>
    Analyzing
}
