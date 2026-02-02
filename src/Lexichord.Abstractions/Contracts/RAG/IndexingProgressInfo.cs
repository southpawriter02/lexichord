// =============================================================================
// File: IndexingProgressInfo.cs
// Project: Lexichord.Abstractions
// Description: Progress information for indexing operations.
// Version: v0.4.7c
// =============================================================================
// LOGIC: Record representing progress state during indexing operations.
//   - CurrentDocument: The file currently being processed.
//   - ProcessedCount/TotalCount: Progress counters.
//   - PercentComplete: Calculated percentage for UI binding.
//   - IsComplete/WasCancelled: Completion state flags.
//   - Used by IndexingProgressViewModel for UI updates.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Progress information for indexing operations.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexingProgressInfo"/> provides a snapshot of indexing progress
/// for UI display purposes. It is published via <c>IndexingProgressUpdatedEvent</c>
/// and consumed by <c>IndexingProgressViewModel</c> in the RAG module.
/// </para>
/// <para>
/// <b>Properties:</b>
/// </para>
/// <list type="bullet">
///   <item><description><see cref="CurrentDocument"/>: Path or filename being processed.</description></item>
///   <item><description><see cref="ProcessedCount"/>: Number of documents completed.</description></item>
///   <item><description><see cref="TotalCount"/>: Total documents to process.</description></item>
///   <item><description><see cref="PercentComplete"/>: Pre-calculated percentage (0-100).</description></item>
///   <item><description><see cref="IsComplete"/>: True when all documents processed.</description></item>
///   <item><description><see cref="WasCancelled"/>: True if operation was cancelled by user.</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.4.7c as part of Indexing Progress.
/// </para>
/// </remarks>
public record IndexingProgressInfo
{
    /// <summary>
    /// Gets the path or filename of the document currently being processed.
    /// </summary>
    /// <remarks>
    /// May be null if no specific document is being processed (e.g., during initialization).
    /// </remarks>
    public string? CurrentDocument { get; init; }

    /// <summary>
    /// Gets the number of documents that have been processed so far.
    /// </summary>
    public int ProcessedCount { get; init; }

    /// <summary>
    /// Gets the total number of documents to be processed.
    /// </summary>
    /// <remarks>
    /// May be zero if the total is not yet known (indeterminate progress).
    /// </remarks>
    public int TotalCount { get; init; }

    /// <summary>
    /// Gets the completion percentage (0-100).
    /// </summary>
    /// <remarks>
    /// Pre-calculated to avoid division in UI layer. Returns 0 if <see cref="TotalCount"/> is zero.
    /// </remarks>
    public int PercentComplete { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation has completed.
    /// </summary>
    /// <remarks>
    /// True when all documents have been processed (successfully or with failures).
    /// </remarks>
    public bool IsComplete { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was cancelled by the user.
    /// </summary>
    public bool WasCancelled { get; init; }

    /// <summary>
    /// Creates a progress info for an in-progress operation.
    /// </summary>
    /// <param name="currentDocument">The document currently being processed.</param>
    /// <param name="processedCount">Number of documents processed so far.</param>
    /// <param name="totalCount">Total number of documents to process.</param>
    /// <returns>A new <see cref="IndexingProgressInfo"/> with calculated percentage.</returns>
    public static IndexingProgressInfo InProgress(string currentDocument, int processedCount, int totalCount)
    {
        var percent = totalCount > 0 ? (int)((processedCount * 100.0) / totalCount) : 0;
        return new IndexingProgressInfo
        {
            CurrentDocument = currentDocument,
            ProcessedCount = processedCount,
            TotalCount = totalCount,
            PercentComplete = percent,
            IsComplete = false,
            WasCancelled = false
        };
    }

    /// <summary>
    /// Creates a progress info indicating successful completion.
    /// </summary>
    /// <param name="totalProcessed">Total number of documents processed.</param>
    /// <returns>A new <see cref="IndexingProgressInfo"/> marked as complete.</returns>
    public static IndexingProgressInfo Complete(int totalProcessed)
    {
        return new IndexingProgressInfo
        {
            CurrentDocument = null,
            ProcessedCount = totalProcessed,
            TotalCount = totalProcessed,
            PercentComplete = 100,
            IsComplete = true,
            WasCancelled = false
        };
    }

    /// <summary>
    /// Creates a progress info indicating the operation was cancelled.
    /// </summary>
    /// <param name="processedCount">Number of documents processed before cancellation.</param>
    /// <param name="totalCount">Total number of documents that were to be processed.</param>
    /// <returns>A new <see cref="IndexingProgressInfo"/> marked as cancelled.</returns>
    public static IndexingProgressInfo Cancelled(int processedCount, int totalCount)
    {
        var percent = totalCount > 0 ? (int)((processedCount * 100.0) / totalCount) : 0;
        return new IndexingProgressInfo
        {
            CurrentDocument = null,
            ProcessedCount = processedCount,
            TotalCount = totalCount,
            PercentComplete = percent,
            IsComplete = true,
            WasCancelled = true
        };
    }
}
