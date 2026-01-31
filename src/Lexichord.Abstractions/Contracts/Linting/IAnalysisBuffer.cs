namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Buffers and debounces analysis requests to prevent excessive processing.
/// </summary>
/// <remarks>
/// LOGIC: The analysis buffer sits between document change events and the
/// analysis engine. It implements "latest-wins" semantics where rapid edits
/// to the same document result in only the most recent content being analyzed.
///
/// Key behaviors:
/// - Per-document debouncing: Each document has its own debounce timer
/// - Latest-wins: During debounce window, only latest request survives
/// - Configurable idle period: Waits for typing pause before processing
/// - Cancellation support: Pending requests can be cancelled by document/all
///
/// Threading:
/// - Submit() is thread-safe and can be called from any thread
/// - Requests observable emits on a background thread (not UI thread)
/// - Callers should marshal to UI thread if needed
///
/// Version: v0.3.7a
/// </remarks>
public interface IAnalysisBuffer : IDisposable
{
    /// <summary>
    /// Gets an observable stream of debounced analysis requests.
    /// </summary>
    /// <remarks>
    /// LOGIC: Subscribe to receive requests that have passed the debounce
    /// window. Each emission represents the latest content for a document
    /// that hasn't changed for the configured idle period.
    ///
    /// Hot observable - late subscribers miss earlier requests.
    /// Emissions occur on a background thread.
    /// </remarks>
    IObservable<AnalysisRequest> Requests { get; }

    /// <summary>
    /// Submits an analysis request to the buffer.
    /// </summary>
    /// <param name="request">The analysis request to buffer.</param>
    /// <remarks>
    /// LOGIC: The request enters the per-document debounce pipeline.
    /// If another request for the same document is pending, this one
    /// replaces it (latest-wins). After the idle period elapses with
    /// no new requests for this document, the request is emitted.
    ///
    /// Thread-safe: Can be called from any thread.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="request"/> is null.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the buffer has been disposed.
    /// </exception>
    void Submit(AnalysisRequest request);

    /// <summary>
    /// Cancels any pending request for the specified document.
    /// </summary>
    /// <param name="documentId">The document identifier.</param>
    /// <remarks>
    /// LOGIC: Removes the document from the debounce pipeline and
    /// signals cancellation on any associated cancellation token.
    /// Idempotent - safe to call if document has no pending request.
    /// </remarks>
    void Cancel(string documentId);

    /// <summary>
    /// Cancels all pending requests in the buffer.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears the entire buffer and signals cancellation on
    /// all associated cancellation tokens. Typically called during
    /// shutdown or when switching workspaces.
    /// </remarks>
    void CancelAll();

    /// <summary>
    /// Gets the number of documents with pending requests.
    /// </summary>
    /// <remarks>
    /// LOGIC: Counts documents currently in the debounce pipeline
    /// waiting for their idle period to elapse. Useful for monitoring
    /// and diagnostics.
    /// </remarks>
    int PendingCount { get; }
}
