// =============================================================================
// File: IndexStatistics.cs
// Project: Lexichord.Abstractions
// Description: Record aggregating index statistics for the Index Status View.
// Version: v0.4.7a
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Aggregates index statistics for display in the Index Status View.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexStatistics"/> provides a summary view of the entire document
/// index, including document counts, chunk counts, storage usage, and status
/// breakdowns. This record is displayed in the Index Status View header.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7a as part of the Index Status View.
/// </para>
/// </remarks>
public record IndexStatistics
{
    /// <summary>
    /// Gets the total number of documents in the index.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// Gets the total number of chunks across all documents.
    /// </summary>
    public int ChunkCount { get; init; }

    /// <summary>
    /// Gets the total estimated storage size in bytes.
    /// </summary>
    public long StorageSizeBytes { get; init; }

    /// <summary>
    /// Gets the count of documents per <see cref="IndexingStatus"/>.
    /// </summary>
    /// <remarks>
    /// Dictionary keyed by status with count values. Statuses with zero
    /// documents may be omitted.
    /// </remarks>
    public IReadOnlyDictionary<IndexingStatus, int> StatusCounts { get; init; }
        = new Dictionary<IndexingStatus, int>();

    /// <summary>
    /// Gets the timestamp of the most recent indexing operation.
    /// </summary>
    /// <remarks>
    /// Null if no documents have been indexed.
    /// </remarks>
    public DateTimeOffset? LastIndexedAt { get; init; }

    /// <summary>
    /// Gets the number of documents pending indexing.
    /// </summary>
    /// <remarks>
    /// Derived from <see cref="StatusCounts"/> for the Pending status.
    /// </remarks>
    public int PendingCount => StatusCounts is not null && StatusCounts.TryGetValue(IndexingStatus.Pending, out var count) ? count : 0;

    /// <summary>
    /// Gets a human-readable representation of <see cref="StorageSizeBytes"/>.
    /// </summary>
    /// <remarks>
    /// Formats bytes as KB, MB, or GB as appropriate.
    /// </remarks>
    public string StorageSizeDisplay => FormatBytes(StorageSizeBytes);

    /// <summary>
    /// Formats byte count as a human-readable string.
    /// </summary>
    /// <param name="bytes">The byte count to format.</param>
    /// <returns>Formatted string (e.g., "1.5 MB").</returns>
    private static string FormatBytes(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F1} GB",
            >= MB => $"{bytes / (double)MB:F1} MB",
            >= KB => $"{bytes / (double)KB:F1} KB",
            _ => $"{bytes} B"
        };
    }
}
