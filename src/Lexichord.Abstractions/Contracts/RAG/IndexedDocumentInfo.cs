// =============================================================================
// File: IndexedDocumentInfo.cs
// Project: Lexichord.Abstractions
// Description: Record containing per-document indexing metadata.
// Version: v0.4.7a
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Contains indexing information for a single document in the RAG system.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexedDocumentInfo"/> provides a comprehensive view of a document's
/// indexing state, including status, chunk count, timestamps, and error information.
/// This record is used by the Index Status View to display document details.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public record IndexedDocumentInfo
{
    /// <summary>
    /// Gets the unique identifier for the document.
    /// </summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Gets the full file path of the document.
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// Gets the file name extracted from <see cref="FilePath"/>.
    /// </summary>
    /// <remarks>
    /// Computed property that extracts the file name from the full path
    /// for display purposes.
    /// </remarks>
    public string FileName => Path.GetFileName(FilePath);

    /// <summary>
    /// Gets the current indexing status of the document.
    /// </summary>
    public required IndexingStatus Status { get; init; }

    /// <summary>
    /// Gets the number of chunks generated from this document.
    /// </summary>
    /// <remarks>
    /// Zero if the document has not been indexed or indexing failed.
    /// </remarks>
    public int ChunkCount { get; init; }

    /// <summary>
    /// Gets the timestamp when the document was last indexed.
    /// </summary>
    /// <remarks>
    /// Null if the document has never been successfully indexed.
    /// </remarks>
    public DateTimeOffset? IndexedAt { get; init; }

    /// <summary>
    /// Gets the SHA-256 hash of the file content at last indexing.
    /// </summary>
    /// <remarks>
    /// Used for stale detection. Null if never indexed.
    /// </remarks>
    public string? FileHash { get; init; }

    /// <summary>
    /// Gets the error message if indexing failed.
    /// </summary>
    /// <remarks>
    /// Null if the document has not failed indexing.
    /// </remarks>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the estimated storage size in bytes for this document's chunks.
    /// </summary>
    /// <remarks>
    /// Calculated as <c>ChunkCount * average chunk size</c>.
    /// Used for aggregate storage statistics.
    /// </remarks>
    public long EstimatedSizeBytes { get; init; }
}
