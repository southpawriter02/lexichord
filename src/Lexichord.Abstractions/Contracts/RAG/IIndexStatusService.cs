// =============================================================================
// File: IIndexStatusService.cs
// Project: Lexichord.Abstractions
// Description: Interface for retrieving index status and statistics.
// Version: v0.4.7a
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides methods for retrieving document indexing status and aggregate statistics.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IIndexStatusService"/> is the primary contract for the Index Status View,
/// providing access to document-level status information and aggregate statistics.
/// The implementation queries <see cref="IDocumentRepository"/> and
/// <see cref="IChunkRepository"/> to gather status data.
/// </para>
/// <para>
/// <b>Dependencies:</b>
/// <list type="bullet">
/// <item><see cref="IDocumentRepository"/> (v0.4.1c) - Document data access</item>
/// <item><see cref="IChunkRepository"/> (v0.4.1c) - Chunk counts and storage</item>
/// <item><c>IFileHashService</c> (v0.4.2b) - Stale detection via hash comparison</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public interface IIndexStatusService
{
    /// <summary>
    /// Retrieves status information for all indexed documents.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// A list of <see cref="IndexedDocumentInfo"/> records for all documents
    /// in the index, regardless of status.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries all documents from the repository, enriches each with
    /// chunk count and current status (including stale detection via hash
    /// comparison for indexed documents).
    /// </remarks>
    Task<IReadOnlyList<IndexedDocumentInfo>> GetAllDocumentsAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves status information for a specific document.
    /// </summary>
    /// <param name="documentId">The unique identifier of the document.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// The <see cref="IndexedDocumentInfo"/> for the document, or null if not found.
    /// </returns>
    /// <remarks>
    /// LOGIC: Queries the document by ID, enriches with chunk count and
    /// performs stale detection if the document is currently indexed.
    /// </remarks>
    Task<IndexedDocumentInfo?> GetDocumentStatusAsync(Guid documentId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves aggregate statistics for the entire index.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>
    /// An <see cref="IndexStatistics"/> record with document counts,
    /// chunk counts, storage usage, and status breakdowns.
    /// </returns>
    /// <remarks>
    /// LOGIC: Aggregates data across all documents, calculating totals
    /// and per-status counts. Does not perform stale detection (use
    /// <see cref="RefreshStaleStatusAsync"/> first if needed).
    /// </remarks>
    Task<IndexStatistics> GetStatisticsAsync(CancellationToken ct = default);

    /// <summary>
    /// Refreshes the status of all documents, detecting stale entries.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: For each document with <see cref="IndexingStatus.Indexed"/> status,
    /// compares the stored file hash with the current file hash. If different,
    /// updates the document status to <see cref="IndexingStatus.Stale"/>.
    /// </para>
    /// <para>
    /// This operation may be expensive for large indices. Consider calling
    /// it on a background timer or user request rather than on every view load.
    /// </para>
    /// </remarks>
    Task RefreshStaleStatusAsync(CancellationToken ct = default);
}
