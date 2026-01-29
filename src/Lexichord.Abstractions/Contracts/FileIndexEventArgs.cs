namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Types of changes that can occur in the file index.
/// </summary>
public enum FileIndexChangeType
{
    /// <summary>A file was added to the index.</summary>
    FileAdded,

    /// <summary>A file's metadata was updated.</summary>
    FileUpdated,

    /// <summary>A file was removed from the index.</summary>
    FileRemoved,

    /// <summary>The entire index was cleared.</summary>
    IndexCleared,

    /// <summary>The index was rebuilt from scratch.</summary>
    IndexRebuilt
}

/// <summary>
/// Event arguments for file index changes.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): Published when the file index is modified.
/// FilePath is null for IndexCleared and IndexRebuilt events.
/// </remarks>
public class FileIndexChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the type of change that occurred.
    /// </summary>
    public required FileIndexChangeType ChangeType { get; init; }

    /// <summary>
    /// Gets the path of the affected file (null for bulk operations).
    /// </summary>
    public string? FilePath { get; init; }

    /// <summary>
    /// Gets the total number of files in the index after this change.
    /// </summary>
    public int TotalFileCount { get; init; }
}

/// <summary>
/// Event arguments for file indexing progress updates.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): Published during RebuildIndexAsync to report progress.
/// Useful for showing progress bars or status messages.
/// </remarks>
public class FileIndexProgressEventArgs : EventArgs
{
    /// <summary>
    /// Gets the number of files indexed so far.
    /// </summary>
    public int FilesIndexed { get; init; }

    /// <summary>
    /// Gets the current directory being processed.
    /// </summary>
    public string? CurrentDirectory { get; init; }
}
