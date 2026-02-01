// =============================================================================
// File: IngestionResult.cs
// Project: Lexichord.Abstractions
// Description: Record encapsulating the outcome of an ingestion operation.
// =============================================================================
// LOGIC: Immutable result type providing comprehensive operation feedback.
//   - Factory methods enforce correct state combinations.
//   - SkippedFiles enables batch operation transparency.
//   - Errors collection supports partial failure reporting.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Ingestion;

/// <summary>
/// Represents the result of a file or directory ingestion operation.
/// </summary>
/// <remarks>
/// <para>
/// This record encapsulates all relevant information about an ingestion attempt,
/// including success status, document metadata, timing, and any errors encountered.
/// </para>
/// <para>
/// For directory ingestion operations, the result aggregates outcomes from all
/// processed files, with individual failures recorded in <see cref="Errors"/>
/// and skipped files listed in <see cref="SkippedFiles"/>.
/// </para>
/// </remarks>
/// <param name="Success">
/// Indicates whether the ingestion operation completed successfully.
/// For directory operations, this is <c>true</c> if at least one file was indexed.
/// </param>
/// <param name="DocumentId">
/// The unique identifier of the indexed document.
/// Null for directory operations or when <see cref="Success"/> is <c>false</c>.
/// </param>
/// <param name="ChunkCount">
/// The total number of chunks created during ingestion.
/// Zero if the operation failed or no content was indexed.
/// </param>
/// <param name="Duration">
/// The elapsed time for the complete ingestion operation.
/// </param>
/// <param name="SkippedFiles">
/// Collection of file paths that were skipped during directory ingestion.
/// Files may be skipped due to unsupported extensions, size limits, or exclusion patterns.
/// </param>
/// <param name="Errors">
/// Collection of error messages encountered during ingestion.
/// For partial failures, this contains details for each failed file.
/// </param>
public record IngestionResult(
    bool Success,
    Guid? DocumentId,
    int ChunkCount,
    TimeSpan Duration,
    IReadOnlyList<string> SkippedFiles,
    IReadOnlyList<string> Errors)
{
    /// <summary>
    /// Creates a successful single-file ingestion result.
    /// </summary>
    /// <param name="documentId">The ID of the successfully indexed document.</param>
    /// <param name="chunkCount">The number of chunks created.</param>
    /// <param name="duration">The time taken for ingestion.</param>
    /// <returns>A successful <see cref="IngestionResult"/>.</returns>
    public static IngestionResult CreateSuccess(Guid documentId, int chunkCount, TimeSpan duration)
    {
        return new IngestionResult(
            Success: true,
            DocumentId: documentId,
            ChunkCount: chunkCount,
            Duration: duration,
            SkippedFiles: [],
            Errors: []);
    }

    /// <summary>
    /// Creates a successful directory ingestion result.
    /// </summary>
    /// <param name="chunkCount">The total number of chunks created across all files.</param>
    /// <param name="duration">The time taken for the entire operation.</param>
    /// <param name="skippedFiles">Files that were skipped (optional).</param>
    /// <returns>A successful <see cref="IngestionResult"/> for batch operations.</returns>
    public static IngestionResult CreateBatchSuccess(
        int chunkCount,
        TimeSpan duration,
        IReadOnlyList<string>? skippedFiles = null)
    {
        return new IngestionResult(
            Success: true,
            DocumentId: null,
            ChunkCount: chunkCount,
            Duration: duration,
            SkippedFiles: skippedFiles ?? [],
            Errors: []);
    }

    /// <summary>
    /// Creates a failed ingestion result.
    /// </summary>
    /// <param name="error">The error message describing the failure.</param>
    /// <param name="duration">The time elapsed before failure.</param>
    /// <returns>A failed <see cref="IngestionResult"/>.</returns>
    public static IngestionResult CreateFailure(string error, TimeSpan duration)
    {
        return new IngestionResult(
            Success: false,
            DocumentId: null,
            ChunkCount: 0,
            Duration: duration,
            SkippedFiles: [],
            Errors: [error]);
    }

    /// <summary>
    /// Creates a failed ingestion result with multiple errors.
    /// </summary>
    /// <param name="errors">Collection of error messages.</param>
    /// <param name="duration">The time elapsed before failure.</param>
    /// <returns>A failed <see cref="IngestionResult"/>.</returns>
    public static IngestionResult CreateFailure(IReadOnlyList<string> errors, TimeSpan duration)
    {
        return new IngestionResult(
            Success: false,
            DocumentId: null,
            ChunkCount: 0,
            Duration: duration,
            SkippedFiles: [],
            Errors: errors);
    }

    /// <summary>
    /// Creates a result indicating the file was skipped.
    /// </summary>
    /// <param name="filePath">The path of the skipped file.</param>
    /// <param name="reason">The reason the file was skipped.</param>
    /// <returns>A skipped <see cref="IngestionResult"/>.</returns>
    public static IngestionResult CreateSkipped(string filePath, string reason)
    {
        return new IngestionResult(
            Success: false,
            DocumentId: null,
            ChunkCount: 0,
            Duration: TimeSpan.Zero,
            SkippedFiles: [filePath],
            Errors: [reason]);
    }
}
