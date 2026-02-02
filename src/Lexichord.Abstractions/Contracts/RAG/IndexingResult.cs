// =============================================================================
// File: IndexingResult.cs
// Project: Lexichord.Abstractions
// Description: Result record for document indexing operations in the RAG pipeline.
// Version: v0.4.7b - Moved to Abstractions for IDocumentIndexingPipeline contract.
// =============================================================================
// LOGIC: Immutable record carrying the outcome of a document indexing operation.
//   - Success indicates whether indexing completed without errors.
//   - DocumentId is populated on success, null on failure.
//   - ChunkCount reflects the number of chunks created (0 if failed).
//   - Duration tracks total operation time for performance monitoring.
//   - TruncationOccurred flags token truncation for visibility.
//   - ErrorMessage contains diagnostic info on failure.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the result of a document indexing operation.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="IndexingResult"/> record encapsulates the outcome of attempting
/// to index a document through the RAG pipeline. It provides comprehensive feedback
/// about the operation's success, the artifacts created, performance metrics, and
/// any errors encountered.
/// </para>
/// <para>
/// <b>Success Semantics:</b> When <see cref="Success"/> is <c>true</c>, the
/// document has been successfully indexed with embeddings stored. The <see cref="DocumentId"/>
/// and <see cref="ChunkCount"/> fields provide details about the created artifacts.
/// </para>
/// <para>
/// <b>Failure Semantics:</b> When <see cref="Success"/> is <c>false</c>,
/// <see cref="DocumentId"/> is null and <see cref="ChunkCount"/> is 0.
/// The <see cref="ErrorMessage"/> field explains the failure reason.
/// </para>
/// <para>
/// <b>Truncation Awareness:</b> The <see cref="TruncationOccurred"/> field indicates
/// whether text truncation occurred during embedding. This is useful for diagnostics
/// and alerting users to potential information loss.
/// </para>
/// </remarks>
/// <param name="Success">
/// Whether the indexing operation completed successfully. When <c>true</c>,
/// the document has been indexed and chunks have been created with embeddings.
/// </param>
/// <param name="DocumentId">
/// The unique identifier of the indexed document. Only populated when
/// <paramref name="Success"/> is <c>true</c>; otherwise <c>null</c>.
/// </param>
/// <param name="ChunkCount">
/// The number of chunks created during indexing. Will be 0 if the operation
/// failed or if the document content was empty.
/// </param>
/// <param name="Duration">
/// The total time spent on the indexing operation, from start to completion.
/// Useful for performance monitoring and optimization.
/// </param>
/// <param name="TruncationOccurred">
/// Whether any chunk had to be truncated due to token limit constraints
/// during embedding generation. <c>false</c> means all chunks fit within
/// token limits without truncation.
/// </param>
/// <param name="ErrorMessage">
/// Diagnostic message explaining failure reasons. Only populated when
/// <paramref name="Success"/> is <c>false</c>; otherwise <c>null</c>.
/// </param>
public record IndexingResult(
    bool Success,
    Guid? DocumentId,
    int ChunkCount,
    TimeSpan Duration,
    bool TruncationOccurred,
    string? ErrorMessage)
{
    /// <summary>
    /// Creates a successful indexing result.
    /// </summary>
    /// <param name="documentId">The ID of the successfully indexed document.</param>
    /// <param name="chunkCount">The number of chunks created.</param>
    /// <param name="duration">The total operation duration.</param>
    /// <param name="truncationOccurred">Whether truncation occurred during embedding.</param>
    /// <returns>A successful <see cref="IndexingResult"/>.</returns>
    /// <remarks>
    /// This factory method is used when indexing completes successfully.
    /// <see cref="ErrorMessage"/> is set to <c>null</c>.
    /// </remarks>
    public static IndexingResult CreateSuccess(Guid documentId, int chunkCount, TimeSpan duration, bool truncationOccurred = false)
    {
        return new IndexingResult(
            Success: true,
            DocumentId: documentId,
            ChunkCount: chunkCount,
            Duration: duration,
            TruncationOccurred: truncationOccurred,
            ErrorMessage: null);
    }

    /// <summary>
    /// Creates a failed indexing result.
    /// </summary>
    /// <param name="errorMessage">The diagnostic message explaining the failure.</param>
    /// <param name="duration">The total operation duration before failure.</param>
    /// <returns>A failed <see cref="IndexingResult"/>.</returns>
    /// <remarks>
    /// This factory method is used when indexing fails. <see cref="DocumentId"/>
    /// is set to <c>null</c> and <see cref="ChunkCount"/> is set to 0.
    /// </remarks>
    public static IndexingResult CreateFailure(string errorMessage, TimeSpan duration)
    {
        return new IndexingResult(
            Success: false,
            DocumentId: null,
            ChunkCount: 0,
            Duration: duration,
            TruncationOccurred: false,
            ErrorMessage: errorMessage);
    }
}
