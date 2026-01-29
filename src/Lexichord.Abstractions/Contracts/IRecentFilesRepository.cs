using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Repository interface for recent files data access.
/// </summary>
/// <remarks>
/// LOGIC: Provides CRUD operations for the RecentFiles table with MRU semantics:
/// - Upsert: Insert or update existing entry (increment open count)
/// - Ordered retrieval: Always by LastOpenedAt descending
/// - Trim: Remove oldest entries beyond the configured maximum
///
/// Implementation uses Dapper with PostgreSQL-specific upsert (ON CONFLICT).
/// </remarks>
public interface IRecentFilesRepository
{
    /// <summary>
    /// Gets recent files ordered by last opened time (most recent first).
    /// </summary>
    /// <param name="maxCount">Maximum entries to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent file entries.</returns>
    Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific entry by file path.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The entry if found, null otherwise.</returns>
    Task<RecentFileEntry?> GetByFilePathAsync(
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a new entry or updates an existing one.
    /// </summary>
    /// <param name="entry">Entry to upsert.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: If FilePath already exists:
    /// - Updates LastOpenedAt to current time
    /// - Increments OpenCount by 1
    /// Otherwise, inserts a new row.
    /// </remarks>
    Task UpsertAsync(RecentFileEntry entry, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an entry by file path.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted, false if not found.</returns>
    Task<bool> DeleteAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes all entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Keeps only the most recent entries up to the specified count.
    /// </summary>
    /// <param name="keepCount">Number of entries to keep.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Number of entries deleted.</returns>
    /// <remarks>
    /// LOGIC: Deletes all entries except the top N ordered by LastOpenedAt descending.
    /// </remarks>
    Task<int> TrimToCountAsync(int keepCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total number of entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Entry count.</returns>
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
}
