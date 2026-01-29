namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for indexing and searching workspace files.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): IFileIndexService provides fast file lookup for the Command Palette.
/// 
/// Core responsibilities:
/// - Indexes all files in the workspace on open
/// - Provides fuzzy search across filenames and paths
/// - Tracks recently accessed files for quick access
/// - Handles incremental updates from file system events
/// 
/// Design decisions:
/// - Thread-safe for concurrent access
/// - Uses FuzzySharp for search scoring
/// - Respects ignore patterns from settings
/// - Skips binary and oversized files
/// - LRU tracking for recent files
/// </remarks>
public interface IFileIndexService
{
    /// <summary>
    /// Gets the number of files currently in the index.
    /// </summary>
    int IndexedFileCount { get; }

    /// <summary>
    /// Gets whether the index is currently being rebuilt.
    /// </summary>
    bool IsIndexing { get; }

    /// <summary>
    /// Gets the current workspace root path, or null if not indexing.
    /// </summary>
    string? WorkspaceRoot { get; }

    /// <summary>
    /// Rebuilds the file index from scratch.
    /// </summary>
    /// <param name="workspaceRoot">Absolute path to the workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of files indexed.</returns>
    /// <remarks>
    /// LOGIC: Clears existing index, walks directory tree, filters by settings,
    /// and adds all matching files. Reports progress via IndexProgress event.
    /// Publishes FileIndexRebuiltEvent on completion.
    /// </remarks>
    Task<int> RebuildIndexAsync(string workspaceRoot, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a single file in the index.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <param name="action">The type of change (Created, Modified, Deleted).</param>
    /// <remarks>
    /// LOGIC: For Created/Modified, adds or updates the entry if the file
    /// passes filter criteria. For Deleted, removes from index and recent files.
    /// </remarks>
    void UpdateFile(string filePath, FileIndexAction action);

    /// <summary>
    /// Updates the index for a renamed file.
    /// </summary>
    /// <param name="oldPath">The previous absolute path.</param>
    /// <param name="newPath">The new absolute path.</param>
    /// <remarks>
    /// LOGIC: Removes old entry, adds new entry if it passes filter criteria.
    /// Updates recent files list if the old path was tracked.
    /// </remarks>
    void UpdateFileRenamed(string oldPath, string newPath);

    /// <summary>
    /// Clears the entire index.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all entries from index and recent files.
    /// Called when workspace is closed.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Searches for files matching the query.
    /// </summary>
    /// <param name="query">The search query (fuzzy matched).</param>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>Matching files sorted by relevance score.</returns>
    /// <remarks>
    /// LOGIC: Uses FuzzySharp.Fuzz.PartialRatio for scoring.
    /// Matches against both filename and relative path.
    /// Results are sorted by score (descending), then by name.
    /// </remarks>
    IReadOnlyList<FileIndexEntry> Search(string query, int maxResults = 50);

    /// <summary>
    /// Gets the most recently accessed files.
    /// </summary>
    /// <param name="maxResults">Maximum number of results to return.</param>
    /// <returns>Recent files in order of access (most recent first).</returns>
    /// <remarks>
    /// LOGIC: Returns files that are still in the index.
    /// Used when Command Palette is opened with empty query.
    /// </remarks>
    IReadOnlyList<FileIndexEntry> GetRecentFiles(int maxResults = 20);

    /// <summary>
    /// Records that a file was accessed.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <remarks>
    /// LOGIC: Moves file to front of recent files list.
    /// Trims list if it exceeds MaxRecentFiles setting.
    /// </remarks>
    void RecordFileAccess(string filePath);

    /// <summary>
    /// Gets a file entry by path.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <returns>The file entry, or null if not indexed.</returns>
    FileIndexEntry? GetFile(string filePath);

    /// <summary>
    /// Checks if a file is in the index.
    /// </summary>
    /// <param name="filePath">Absolute path to the file.</param>
    /// <returns>True if the file is indexed.</returns>
    bool ContainsFile(string filePath);

    /// <summary>
    /// Gets all indexed files.
    /// </summary>
    /// <returns>All files in the index.</returns>
    IReadOnlyList<FileIndexEntry> GetAllFiles();

    /// <summary>
    /// Event raised when the index is modified.
    /// </summary>
    event EventHandler<FileIndexChangedEventArgs>? IndexChanged;

    /// <summary>
    /// Event raised during index rebuild to report progress.
    /// </summary>
    event EventHandler<FileIndexProgressEventArgs>? IndexProgress;
}
