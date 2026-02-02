// =============================================================================
// File: IndexManagementResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for index management operations.
// Version: v0.4.7b
// =============================================================================
// LOGIC: Immutable result type for index management operations.
//   - Success indicates whether the operation completed without errors.
//   - DocumentId is null for bulk operations (ReindexAll).
//   - ProcessedCount/FailedCount provide aggregate metrics for bulk operations.
//   - Duration enables performance monitoring and telemetry.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the result of an index management operation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexManagementResult"/> is an immutable record that encapsulates
/// the outcome of index management operations such as re-indexing or removing
/// documents from the index.
/// </para>
/// <para>
/// <b>Single Document Operations:</b> For operations targeting a single document
/// (e.g., <c>ReindexDocumentAsync</c>, <c>RemoveFromIndexAsync</c>):
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DocumentId"/> contains the target document's ID.</description></item>
///   <item><description><see cref="ProcessedCount"/> is 1 on success, 0 on failure.</description></item>
///   <item><description><see cref="FailedCount"/> is 0 on success, 1 on failure.</description></item>
/// </list>
/// <para>
/// <b>Bulk Operations:</b> For operations targeting multiple documents
/// (e.g., <c>ReindexAllAsync</c>):
/// </para>
/// <list type="bullet">
///   <item><description><see cref="DocumentId"/> is <c>null</c>.</description></item>
///   <item><description><see cref="ProcessedCount"/> reflects total successful operations.</description></item>
///   <item><description><see cref="FailedCount"/> reflects total failed operations.</description></item>
///   <item><description><see cref="Success"/> is <c>true</c> only if <see cref="FailedCount"/> is 0.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.4.7b as part of Manual Indexing Controls.
/// </para>
/// </remarks>
/// <param name="Success">
/// Indicates whether the operation completed successfully.
/// For bulk operations, <c>true</c> only if all documents were processed without error.
/// </param>
/// <param name="DocumentId">
/// The unique identifier of the document affected by the operation.
/// <c>null</c> for bulk operations that affect multiple documents.
/// </param>
/// <param name="Message">
/// A human-readable message describing the operation outcome.
/// Contains error details on failure or a success summary otherwise.
/// </param>
/// <param name="ProcessedCount">
/// The number of documents successfully processed.
/// For single-document operations, this is 1 on success and 0 on failure.
/// </param>
/// <param name="FailedCount">
/// The number of documents that failed to process.
/// For single-document operations, this is 0 on success and 1 on failure.
/// </param>
/// <param name="Duration">
/// The total time elapsed during the operation.
/// Useful for performance monitoring and telemetry.
/// </param>
public record IndexManagementResult(
    bool Success,
    Guid? DocumentId,
    string Message,
    int ProcessedCount,
    int FailedCount,
    TimeSpan Duration)
{
    /// <summary>
    /// Creates a successful result for a single-document operation.
    /// </summary>
    /// <param name="documentId">The ID of the processed document.</param>
    /// <param name="message">A success message.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A successful <see cref="IndexManagementResult"/>.</returns>
    public static IndexManagementResult SuccessSingle(Guid documentId, string message, TimeSpan duration) =>
        new(Success: true, DocumentId: documentId, Message: message, ProcessedCount: 1, FailedCount: 0, Duration: duration);

    /// <summary>
    /// Creates a failed result for a single-document operation.
    /// </summary>
    /// <param name="documentId">The ID of the document that failed.</param>
    /// <param name="errorMessage">A description of the failure.</param>
    /// <param name="duration">The operation duration.</param>
    /// <returns>A failed <see cref="IndexManagementResult"/>.</returns>
    public static IndexManagementResult FailureSingle(Guid documentId, string errorMessage, TimeSpan duration) =>
        new(Success: false, DocumentId: documentId, Message: errorMessage, ProcessedCount: 0, FailedCount: 1, Duration: duration);

    /// <summary>
    /// Creates a result for a document not found scenario.
    /// </summary>
    /// <param name="documentId">The ID of the document that was not found.</param>
    /// <returns>A failed <see cref="IndexManagementResult"/> indicating document not found.</returns>
    public static IndexManagementResult NotFound(Guid documentId) =>
        new(Success: false, DocumentId: documentId, Message: $"Document {documentId} not found in index.", ProcessedCount: 0, FailedCount: 1, Duration: TimeSpan.Zero);

    /// <summary>
    /// Creates a result for a bulk operation.
    /// </summary>
    /// <param name="processedCount">The number of successfully processed documents.</param>
    /// <param name="failedCount">The number of failed documents.</param>
    /// <param name="duration">The total operation duration.</param>
    /// <returns>An <see cref="IndexManagementResult"/> for the bulk operation.</returns>
    public static IndexManagementResult Bulk(int processedCount, int failedCount, TimeSpan duration)
    {
        var success = failedCount == 0;
        var message = success
            ? $"Successfully processed {processedCount} document(s)."
            : $"Processed {processedCount} document(s) with {failedCount} failure(s).";

        return new(Success: success, DocumentId: null, Message: message, ProcessedCount: processedCount, FailedCount: failedCount, Duration: duration);
    }
}
