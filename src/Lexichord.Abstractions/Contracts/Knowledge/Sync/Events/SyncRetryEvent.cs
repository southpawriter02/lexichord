// =============================================================================
// File: SyncRetryEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a sync operation is retried after failure.
// =============================================================================
// LOGIC: Published when a previously failed sync operation is being retried.
//   Handlers can update UI to show retry progress, log retry attempts, or
//   adjust retry strategy based on attempt count.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event published when a sync operation is retried after failure.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a failed sync operation is being
/// retried, providing retry attempt information.
/// </para>
/// <para>
/// <b>Retry Strategy:</b> Typical retry behavior uses exponential backoff
/// with a maximum number of attempts configured in sync options.
/// </para>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Update UI with retry progress.</item>
///   <item>Log retry attempts for monitoring.</item>
///   <item>Adjust retry strategy based on failure pattern.</item>
///   <item>Notify user of ongoing recovery attempts.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class RetryMonitor : INotificationHandler&lt;SyncRetryEvent&gt;
/// {
///     public Task Handle(SyncRetryEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Retrying sync for document {notification.DocumentId}");
///         Console.WriteLine($"  Attempt {notification.AttemptNumber} of {notification.MaxAttempts}");
///         Console.WriteLine($"  Waiting {notification.RetryDelay.TotalSeconds}s before retry");
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
public record SyncRetryEvent : ISyncEvent
{
    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Generated at event creation. Used for deduplication and tracking.
    /// </remarks>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Recorded at event creation for ordering and audit.
    /// </remarks>
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: The document being retried.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation and context.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Current attempt number.
    /// </summary>
    /// <value>The retry attempt number (1-based).</value>
    /// <remarks>
    /// LOGIC: First retry is attempt 1 (initial attempt was 0).
    /// </remarks>
    public int AttemptNumber { get; init; }

    /// <summary>
    /// Maximum retry attempts allowed.
    /// </summary>
    /// <value>The maximum number of retry attempts.</value>
    /// <remarks>
    /// LOGIC: Configured in sync options or system defaults.
    /// Typically 3-5 attempts.
    /// </remarks>
    public int MaxAttempts { get; init; }

    /// <summary>
    /// Delay before the retry attempt.
    /// </summary>
    /// <value>Time to wait before retrying.</value>
    /// <remarks>
    /// LOGIC: Typically increases with each attempt (exponential backoff).
    /// </remarks>
    public TimeSpan RetryDelay { get; init; }

    /// <summary>
    /// Original failure reason.
    /// </summary>
    /// <value>Why the original operation failed, or null.</value>
    /// <remarks>
    /// LOGIC: Provides context for the retry.
    /// </remarks>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Creates a new <see cref="SyncRetryEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document being retried.</param>
    /// <param name="attemptNumber">Current attempt number.</param>
    /// <param name="maxAttempts">Maximum retry attempts.</param>
    /// <param name="retryDelay">Delay before retry.</param>
    /// <param name="failureReason">Optional original failure reason.</param>
    /// <returns>A new <see cref="SyncRetryEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// </remarks>
    public static SyncRetryEvent Create(
        Guid documentId,
        int attemptNumber,
        int maxAttempts,
        TimeSpan retryDelay,
        string? failureReason = null) => new()
    {
        DocumentId = documentId,
        AttemptNumber = attemptNumber,
        MaxAttempts = maxAttempts,
        RetryDelay = retryDelay,
        FailureReason = failureReason
    };
}
