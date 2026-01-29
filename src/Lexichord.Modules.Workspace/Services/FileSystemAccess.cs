namespace Lexichord.Modules.Workspace.Services;

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

/// <summary>
/// Implementation of <see cref="IFileSystemAccess"/> using System.IO.
/// </summary>
/// <remarks>
/// LOGIC: Provides async-friendly file system access for the Tree View.
/// Filters out hidden files and handles access exceptions gracefully.
///
/// Thread-safety: All methods are thread-safe.
/// </remarks>
public sealed class FileSystemAccess : IFileSystemAccess
{
    private readonly ILogger<FileSystemAccess> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemAccess"/> class.
    /// </summary>
    /// <param name="logger">Logger for recording operations.</param>
    public FileSystemAccess(ILogger<FileSystemAccess> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DirectoryEntry>> GetDirectoryContentsAsync(string path)
    {
        _logger.LogDebug("Enumerating directory: {Path}", path);

        return await Task.Run(() =>
        {
            var results = new List<DirectoryEntry>();

            try
            {
                if (!Directory.Exists(path))
                {
                    _logger.LogWarning("Directory does not exist: {Path}", path);
                    return results;
                }

                var options = new EnumerationOptions
                {
                    IgnoreInaccessible = true,
                    AttributesToSkip = FileAttributes.System
                };

                foreach (var fullPath in Directory.EnumerateFileSystemEntries(path, "*", options))
                {
                    try
                    {
                        var name = Path.GetFileName(fullPath);

                        // Skip hidden files (Unix convention: starting with '.')
                        if (name.StartsWith('.'))
                            continue;

                        // Skip hidden files (Windows convention: Hidden attribute)
                        if (OperatingSystem.IsWindows())
                        {
                            var attributes = File.GetAttributes(fullPath);
                            if ((attributes & FileAttributes.Hidden) != 0)
                                continue;
                        }

                        var isDirectory = Directory.Exists(fullPath);

                        results.Add(new DirectoryEntry(name, fullPath, isDirectory));
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        _logger.LogTrace("Skipping inaccessible entry: {Path}", fullPath);
                    }
                }

                _logger.LogDebug("Enumerated {Count} entries in {Path}", results.Count, path);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or DirectoryNotFoundException)
            {
                _logger.LogWarning(ex, "Failed to enumerate directory: {Path}", path);
            }

            return results;
        });
    }

    /// <inheritdoc />
    public bool Exists(string path)
    {
        return File.Exists(path) || Directory.Exists(path);
    }

    /// <inheritdoc />
    public bool IsDirectory(string path)
    {
        return Directory.Exists(path);
    }
}
