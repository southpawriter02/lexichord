namespace Lexichord.Abstractions.Contracts.Editor;

using System.Text;

/// <summary>
/// Service for file system operations with atomic save support.
/// </summary>
/// <remarks>
/// LOGIC: All write operations use atomic save strategy to prevent corruption.
///
/// Atomic Save Strategy (Write-Temp-Delete-Rename):
/// 1. Write content to {filename}.tmp
/// 2. Flush stream to ensure data on disk
/// 3. Delete original {filename} (if exists)
/// 4. Rename {filename}.tmp to {filename}
///
/// This guarantees:
/// - Original file preserved until new content fully written
/// - No partial writes visible to other processes
/// - On failure, original file remains intact
///
/// Platform Notes:
/// - POSIX: rename() is atomic within same filesystem
/// - Windows: MoveFileEx is atomic within same volume
/// - Network/cloud drives may have additional latency
/// </remarks>
public interface IFileService
{
    /// <summary>
    /// Saves content to the specified file path using atomic write.
    /// </summary>
    /// <param name="filePath">The target file path.</param>
    /// <param name="content">The content to save.</param>
    /// <param name="encoding">Text encoding (default: UTF-8 without BOM).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    /// <remarks>
    /// LOGIC: Uses three-phase atomic write strategy.
    /// On success, clears dirty state via published event.
    /// </remarks>
    Task<SaveResult> SaveAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves content to a new file path (Save As operation).
    /// </summary>
    /// <param name="filePath">The new target file path.</param>
    /// <param name="content">The content to save.</param>
    /// <param name="encoding">Text encoding (default: UTF-8 without BOM).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating success or failure with details.</returns>
    /// <remarks>
    /// LOGIC: Same atomic strategy as SaveAsync.
    /// Does not require original file to exist.
    /// </remarks>
    Task<SaveResult> SaveAsAsync(
        string filePath,
        string content,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads content from the specified file path.
    /// </summary>
    /// <param name="filePath">The source file path.</param>
    /// <param name="encoding">Text encoding (null = auto-detect).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing content or error details.</returns>
    Task<LoadResult> LoadAsync(
        string filePath,
        Encoding? encoding = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if the specified file can be written to.
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <returns>True if writable, false if read-only or inaccessible.</returns>
    /// <remarks>
    /// LOGIC: Checks:
    /// - Directory exists and writable
    /// - File not read-only (if exists)
    /// - File not locked by another process
    /// </remarks>
    bool CanWrite(string filePath);

    /// <summary>
    /// Checks if the specified file exists.
    /// </summary>
    bool Exists(string filePath);

    /// <summary>
    /// Gets file information without loading content.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    /// <returns>File information or null if not exists.</returns>
    FileServiceMetadata? GetMetadata(string filePath);
}

/// <summary>
/// Result of a save operation.
/// </summary>
/// <param name="Success">Whether the save succeeded.</param>
/// <param name="FilePath">The path where the file was saved.</param>
/// <param name="BytesWritten">Number of bytes written.</param>
/// <param name="Duration">Time taken for the save operation.</param>
/// <param name="Error">Error details if save failed.</param>
public record SaveResult(
    bool Success,
    string FilePath,
    long BytesWritten = 0,
    TimeSpan Duration = default,
    SaveError? Error = null
)
{
    /// <summary>
    /// Creates a successful save result.
    /// </summary>
    public static SaveResult Succeeded(string filePath, long bytes, TimeSpan duration)
        => new(true, filePath, bytes, duration);

    /// <summary>
    /// Creates a failed save result.
    /// </summary>
    public static SaveResult Failed(string filePath, SaveError error)
        => new(false, filePath, Error: error);
}

/// <summary>
/// Error details for a failed save operation.
/// </summary>
/// <param name="Code">Error code for programmatic handling.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Exception">The underlying exception, if any.</param>
/// <param name="RecoveryHint">Suggestion for recovery, if applicable.</param>
public record SaveError(
    SaveErrorCode Code,
    string Message,
    Exception? Exception = null,
    string? RecoveryHint = null
);

/// <summary>
/// Error codes for save operations.
/// </summary>
public enum SaveErrorCode
{
    /// <summary>Unknown error.</summary>
    Unknown,

    /// <summary>File is in use by another process.</summary>
    FileInUse,

    /// <summary>Access denied to file or directory.</summary>
    AccessDenied,

    /// <summary>File path exceeds maximum length.</summary>
    PathTooLong,

    /// <summary>Not enough disk space.</summary>
    DiskFull,

    /// <summary>File is marked read-only.</summary>
    ReadOnly,

    /// <summary>Target directory does not exist.</summary>
    DirectoryNotFound,

    /// <summary>File path contains invalid characters.</summary>
    InvalidPath,

    /// <summary>General I/O error during write.</summary>
    IoError,

    /// <summary>Save was cancelled by user.</summary>
    Cancelled,

    /// <summary>Failed during temp file write phase.</summary>
    TempWriteFailed,

    /// <summary>Failed during original delete phase.</summary>
    DeleteFailed,

    /// <summary>Failed during rename phase (critical).</summary>
    RenameFailed
}

/// <summary>
/// Result of a load operation.
/// </summary>
/// <param name="Success">Whether the load succeeded.</param>
/// <param name="FilePath">The source file path.</param>
/// <param name="Content">The file content if successful.</param>
/// <param name="Encoding">The detected/used encoding.</param>
/// <param name="Error">Error details if load failed.</param>
public record LoadResult(
    bool Success,
    string FilePath,
    string? Content = null,
    Encoding? Encoding = null,
    LoadError? Error = null
)
{
    /// <summary>
    /// Creates a successful load result.
    /// </summary>
    public static LoadResult Succeeded(string filePath, string content, Encoding encoding)
        => new(true, filePath, content, encoding);

    /// <summary>
    /// Creates a failed load result.
    /// </summary>
    public static LoadResult Failed(string filePath, LoadError error)
        => new(false, filePath, Error: error);
}

/// <summary>
/// Error details for a failed load operation.
/// </summary>
/// <param name="Code">Error code for programmatic handling.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Exception">The underlying exception, if any.</param>
public record LoadError(
    LoadErrorCode Code,
    string Message,
    Exception? Exception = null
);

/// <summary>
/// Error codes for load operations.
/// </summary>
public enum LoadErrorCode
{
    /// <summary>Unknown error.</summary>
    Unknown,

    /// <summary>File does not exist.</summary>
    FileNotFound,

    /// <summary>File exceeds maximum allowed size.</summary>
    FileTooLarge,

    /// <summary>Access denied to file.</summary>
    AccessDenied,

    /// <summary>General I/O error during read.</summary>
    IoError
}

/// <summary>
/// Metadata about a file.
/// </summary>
/// <param name="FilePath">Full path to the file.</param>
/// <param name="FileName">File name without path.</param>
/// <param name="SizeBytes">File size in bytes.</param>
/// <param name="IsReadOnly">Whether the file is read-only.</param>
/// <param name="LastModifiedUtc">Last modification time.</param>
/// <param name="CreatedUtc">Creation time.</param>
/// <remarks>
/// Named FileServiceMetadata to avoid collision with FileMetadata in v0.4.2.
/// </remarks>
public record FileServiceMetadata(
    string FilePath,
    string FileName,
    long SizeBytes,
    bool IsReadOnly,
    DateTime LastModifiedUtc,
    DateTime CreatedUtc
);
