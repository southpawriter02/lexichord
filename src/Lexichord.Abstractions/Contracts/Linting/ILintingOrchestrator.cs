namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Orchestrates reactive linting for open documents.
/// </summary>
/// <remarks>
/// LOGIC: The LintingOrchestrator manages document subscriptions and
/// coordinates debounced, concurrent lint operations. It serves as the
/// central hub for the reactive linting pipeline.
///
/// Lifecycle:
/// 1. Document opens → Subscribe() with content observable
/// 2. Content changes → Debounce → Scan → Publish result
/// 3. Document closes → Dispose subscription
///
/// Threading:
/// - Subscribe/Unsubscribe calls must be thread-safe
/// - Results observable can be subscribed from any thread
/// - Scans run on thread pool, not UI thread
///
/// Version: v0.2.3a
/// </remarks>
public interface ILintingOrchestrator : IDisposable
{
    /// <summary>
    /// Subscribes a document to the linting pipeline.
    /// </summary>
    /// <param name="documentId">Unique identifier for the document.</param>
    /// <param name="filePath">File path for logging/display.</param>
    /// <param name="contentStream">Observable stream of document content.</param>
    /// <returns>A disposable to cancel the subscription.</returns>
    /// <remarks>
    /// LOGIC: The contentStream should emit the full document content
    /// whenever it changes. The orchestrator handles debouncing.
    /// Disposing the returned IDisposable stops monitoring.
    /// </remarks>
    IDisposable Subscribe(
        string documentId,
        string? filePath,
        IObservable<string> contentStream);

    /// <summary>
    /// Removes a document subscription by ID.
    /// </summary>
    /// <param name="documentId">The document identifier to unsubscribe.</param>
    /// <remarks>
    /// LOGIC: Cancels any pending/in-progress scan and removes state.
    /// Idempotent - safe to call if not subscribed.
    /// </remarks>
    void Unsubscribe(string documentId);

    /// <summary>
    /// Gets the current lint state for a document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <returns>The current state, or null if not subscribed.</returns>
    DocumentLintState? GetDocumentState(string documentId);

    /// <summary>
    /// Triggers an immediate lint scan, bypassing debounce.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <param name="content">The content to lint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The lint result.</returns>
    /// <remarks>
    /// LOGIC: Used for "Lint Now" command or on-save scanning.
    /// Does not affect the normal debounce pipeline.
    /// Throws InvalidOperationException if documentId not subscribed.
    /// </remarks>
    Task<LintResult> TriggerManualScanAsync(
        string documentId,
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Observable stream of lint results from all documents.
    /// </summary>
    /// <remarks>
    /// LOGIC: Subscribe to receive results as they complete.
    /// Emits for both debounced and manual scans.
    /// Hot observable - late subscribers miss earlier results.
    /// </remarks>
    IObservable<LintResult> Results { get; }

    /// <summary>
    /// Gets the number of currently active document subscriptions.
    /// </summary>
    int ActiveDocumentCount { get; }
}
