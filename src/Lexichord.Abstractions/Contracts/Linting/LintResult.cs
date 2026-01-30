namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Represents the result of a lint operation for a document.
/// </summary>
/// <remarks>
/// LOGIC: Immutable result container returned by the orchestrator.
/// Captures success, cancellation, or error states with timing info.
/// Published to the Results observable stream.
///
/// Version: v0.2.3a
/// </remarks>
public sealed record LintResult
{
    /// <summary>
    /// The unique identifier of the document that was linted.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// When the lint operation completed.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// How long the lint operation took.
    /// </summary>
    public required TimeSpan Duration { get; init; }

    /// <summary>
    /// The violations found (empty if cancelled or error).
    /// </summary>
    public IReadOnlyList<StyleViolation> Violations { get; init; } = [];

    /// <summary>
    /// Whether the operation was cancelled before completion.
    /// </summary>
    public bool WasCancelled { get; init; }

    /// <summary>
    /// Error message if the operation failed (null on success or cancel).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether the lint operation completed successfully.
    /// </summary>
    public bool IsSuccess => !WasCancelled && Error is null;

    /// <summary>
    /// Creates a successful lint result.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="violations">The violations found.</param>
    /// <param name="duration">How long the scan took.</param>
    /// <returns>A successful LintResult.</returns>
    public static LintResult Success(
        string documentId,
        IReadOnlyList<StyleViolation> violations,
        TimeSpan duration)
        => new()
        {
            DocumentId = documentId,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = duration,
            Violations = violations
        };

    /// <summary>
    /// Creates a cancelled lint result.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="elapsed">How long before cancellation.</param>
    /// <returns>A cancelled LintResult.</returns>
    public static LintResult Cancelled(string documentId, TimeSpan elapsed)
        => new()
        {
            DocumentId = documentId,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = elapsed,
            WasCancelled = true
        };

    /// <summary>
    /// Creates a failed lint result.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="error">The error message.</param>
    /// <param name="elapsed">How long before failure.</param>
    /// <returns>A failed LintResult.</returns>
    public static LintResult Failed(string documentId, string error, TimeSpan elapsed)
        => new()
        {
            DocumentId = documentId,
            Timestamp = DateTimeOffset.UtcNow,
            Duration = elapsed,
            Error = error
        };
}
