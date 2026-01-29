using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service interface for managing the Most Recently Used (MRU) files list.
/// </summary>
/// <remarks>
/// LOGIC: This service handles:
/// - Tracking recently opened files with timestamps and open counts
/// - Persisting history to the database via IRecentFilesRepository
/// - Checking file existence when retrieving the list
/// - Auto-trimming to MaxEntries after additions
/// - Raising events on list mutations for UI updates
///
/// The service also implements INotificationHandler&lt;FileOpenedEvent&gt; to
/// automatically track files when they are opened via any source.
/// </remarks>
public interface IRecentFilesService
{
    /// <summary>
    /// Gets recent files ordered by last opened time (most recent first).
    /// </summary>
    /// <param name="maxCount">Maximum entries to return (capped at MaxEntries).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of recent file entries with existence status populated.</returns>
    /// <remarks>
    /// LOGIC: Checks File.Exists() for each entry and populates the Exists property.
    /// This allows the UI to show missing files as disabled.
    /// </remarks>
    Task<IReadOnlyList<RecentFileEntry>> GetRecentFilesAsync(
        int maxCount = 10,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds or updates a file in the recent history.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: If the file already exists in history, updates LastOpenedAt and
    /// increments OpenCount. Otherwise, creates a new entry. Trims to MaxEntries
    /// after insertion.
    /// </remarks>
    Task AddRecentFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a specific file from recent history.
    /// </summary>
    /// <param name="filePath">Absolute path to the file to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveRecentFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all recent file history.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ClearHistoryAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes entries for files that no longer exist on disk.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file paths that were removed.</returns>
    Task<IReadOnlyList<string>> PruneInvalidEntriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Maximum number of recent files to retain.
    /// </summary>
    int MaxEntries { get; }

    /// <summary>
    /// Raised when the recent files list changes.
    /// </summary>
    event EventHandler<RecentFilesChangedEventArgs>? RecentFilesChanged;
}

/// <summary>
/// Record representing a recent file entry.
/// </summary>
/// <param name="FilePath">Absolute path to the file.</param>
/// <param name="FileName">Display name (typically file name without path).</param>
/// <param name="LastOpenedAt">When the file was last opened.</param>
/// <param name="OpenCount">Number of times the file has been opened.</param>
/// <param name="Exists">Whether the file currently exists on disk.</param>
public record RecentFileEntry(
    string FilePath,
    string FileName,
    DateTimeOffset LastOpenedAt,
    int OpenCount,
    bool Exists);

/// <summary>
/// Event arguments for recent files list changes.
/// </summary>
public class RecentFilesChangedEventArgs : EventArgs
{
    /// <summary>
    /// Type of change that occurred.
    /// </summary>
    public required RecentFilesChangeType ChangeType { get; init; }

    /// <summary>
    /// File path affected (null for Clear operations).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Entry details (null for Remove/Clear operations).
    /// </summary>
    public RecentFileEntry? Entry { get; init; }
}

/// <summary>
/// Types of changes to the recent files list.
/// </summary>
public enum RecentFilesChangeType
{
    /// <summary>A file was added or updated in the list.</summary>
    Added,

    /// <summary>A specific file was removed from the list.</summary>
    Removed,

    /// <summary>An existing entry was updated.</summary>
    Updated,

    /// <summary>All entries were cleared.</summary>
    Cleared,

    /// <summary>Invalid entries were pruned.</summary>
    Pruned
}
