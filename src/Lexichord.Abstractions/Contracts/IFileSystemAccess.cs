namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Abstraction for file system access operations.
/// </summary>
/// <remarks>
/// LOGIC: Provides a testable interface for file system operations.
/// This allows the Tree View UI to:
/// - Be unit tested without real file system access
/// - Support virtual or remote file systems in the future
/// - Perform asynchronous directory enumeration
/// </remarks>
public interface IFileSystemAccess
{
    /// <summary>
    /// Gets the contents of a directory asynchronously.
    /// </summary>
    /// <param name="path">The directory path to enumerate.</param>
    /// <returns>A read-only list of directory entries (files and subdirectories).</returns>
    /// <remarks>
    /// LOGIC: Returns both files and directories in the specified path.
    /// Hidden files (starting with '.' on Unix or with Hidden attribute on Windows)
    /// are excluded. Results are not sorted - consumers should sort as needed.
    /// </remarks>
    Task<IReadOnlyList<DirectoryEntry>> GetDirectoryContentsAsync(string path);

    /// <summary>
    /// Checks if a file or directory exists at the specified path.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path exists; otherwise, false.</returns>
    bool Exists(string path);

    /// <summary>
    /// Checks if the specified path is a directory.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a directory; otherwise, false.</returns>
    bool IsDirectory(string path);
}

/// <summary>
/// Represents a file or directory entry in a file system listing.
/// </summary>
/// <param name="Name">The file or directory name (without path).</param>
/// <param name="FullPath">The absolute path to the file or directory.</param>
/// <param name="IsDirectory">True if this entry is a directory; false if it's a file.</param>
public record DirectoryEntry(
    string Name,
    string FullPath,
    bool IsDirectory
);
