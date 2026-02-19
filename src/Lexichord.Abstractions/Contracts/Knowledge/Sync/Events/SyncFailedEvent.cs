// =============================================================================
// File: SyncFailedEvent.cs
// Project: Lexichord.Abstractions
// Description: Event published when a sync operation fails.
// =============================================================================
// LOGIC: Published when a sync operation fails due to errors, timeouts, or
//   validation issues. Handlers can notify users, trigger retries, or log
//   failures for diagnostics.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: ISyncEvent (v0.7.6j), SyncDirection (v0.7.6e)
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;

/// <summary>
/// Event published when a sync operation fails.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b> Notifies subscribers when a synchronization operation fails,
/// providing error details and recovery guidance.
/// </para>
/// <para>
/// <b>Failure Types:</b>
/// </para>
/// <list type="bullet">
///   <item>Validation errors during extraction.</item>
///   <item>Graph database connectivity issues.</item>
///   <item>Timeout during long-running operations.</item>
///   <item>License tier restrictions.</item>
///   <item>Unrecoverable conflicts.</item>
/// </list>
/// <para>
/// <b>Common Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item>Display error notification to user.</item>
///   <item>Trigger automatic retry workflow.</item>
///   <item>Log failure for diagnostics.</item>
///   <item>Update sync status to error state.</item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class FailureAlertHandler : INotificationHandler&lt;SyncFailedEvent&gt;
/// {
///     public async Task Handle(SyncFailedEvent notification, CancellationToken ct)
///     {
///         Console.WriteLine($"Sync failed for document {notification.DocumentId}");
///         Console.WriteLine($"  Error: {notification.ErrorMessage}");
///         if (notification.RetryRecommended)
///         {
///             Console.WriteLine("  Retry recommended.");
///         }
///     }
/// }
/// </code>
/// </example>
public record SyncFailedEvent : ISyncEvent
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
    /// LOGIC: The document that failed to sync.
    /// </remarks>
    public required Guid DocumentId { get; init; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extensible metadata for correlation and context.
    /// </remarks>
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Error message describing the failure.
    /// </summary>
    /// <value>Human-readable error description.</value>
    /// <remarks>
    /// LOGIC: Primary error message suitable for display to users.
    /// </remarks>
    public required string ErrorMessage { get; init; }

    /// <summary>
    /// Machine-readable error code.
    /// </summary>
    /// <value>Error code for programmatic handling, or null.</value>
    /// <remarks>
    /// LOGIC: Enables programmatic error categorization.
    /// Example: "SYNC-001", "GRAPH-CONN-ERR", "VALIDATION-FAILED".
    /// </remarks>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Exception details for diagnostics.
    /// </summary>
    /// <value>Stack trace or exception details, or null.</value>
    /// <remarks>
    /// LOGIC: Detailed exception information for debugging.
    /// Should not be displayed to end users.
    /// </remarks>
    public string? ExceptionDetails { get; init; }

    /// <summary>
    /// The sync direction that failed.
    /// </summary>
    /// <value>Which sync direction encountered the failure.</value>
    /// <remarks>
    /// LOGIC: Indicates whether document-to-graph or graph-to-document failed.
    /// </remarks>
    public SyncDirection FailedDirection { get; init; }

    /// <summary>
    /// Whether retry is recommended.
    /// </summary>
    /// <value>True if the failure is transient and retry may succeed.</value>
    /// <remarks>
    /// LOGIC: True for transient failures (network, timeout).
    /// False for permanent failures (validation, license).
    /// </remarks>
    public bool RetryRecommended { get; init; }

    /// <summary>
    /// Creates a new <see cref="SyncFailedEvent"/> with the specified parameters.
    /// </summary>
    /// <param name="documentId">The document that failed to sync.</param>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="failedDirection">The sync direction that failed.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <param name="exceptionDetails">Optional exception details.</param>
    /// <param name="retryRecommended">Whether retry is recommended.</param>
    /// <returns>A new <see cref="SyncFailedEvent"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Factory method for consistent event creation.
    /// </remarks>
    public static SyncFailedEvent Create(
        Guid documentId,
        string errorMessage,
        SyncDirection failedDirection,
        string? errorCode = null,
        string? exceptionDetails = null,
        bool retryRecommended = false) => new()
    {
        DocumentId = documentId,
        ErrorMessage = errorMessage,
        FailedDirection = failedDirection,
        ErrorCode = errorCode,
        ExceptionDetails = exceptionDetails,
        RetryRecommended = retryRecommended
    };
}
