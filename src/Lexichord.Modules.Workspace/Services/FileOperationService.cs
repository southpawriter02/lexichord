using System.Diagnostics;
using System.Runtime.InteropServices;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Workspace.Services;

/// <summary>
/// Service for performing file and folder operations with validation and event publishing.
/// </summary>
/// <remarks>
/// LOGIC: FileOperationService centralizes all file modifications in the workspace.
/// Key responsibilities:
/// - Validate names and paths before operations
/// - Enforce protected path restrictions (workspace root, .git)
/// - Publish events via MediatR after successful operations
/// - Provide cross-platform "Reveal in Explorer" functionality
///
/// Threading: All async methods run I/O on thread pool to avoid blocking UI.
/// </remarks>
public sealed class FileOperationService : IFileOperationService
{
    private readonly IMediator _mediator;
    private readonly ILogger<FileOperationService> _logger;

    /// <summary>
    /// Characters that are invalid in file/folder names (from OS).
    /// </summary>
    private static readonly char[] InvalidNameChars = Path.GetInvalidFileNameChars();

    /// <summary>
    /// Reserved Windows device names that cannot be used as file/folder names.
    /// </summary>
    private static readonly string[] ReservedNames =
    [
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    ];

    /// <summary>
    /// Maximum allowed length for file/folder names.
    /// </summary>
    private const int MaxNameLength = 255;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileOperationService"/> class.
    /// </summary>
    /// <param name="mediator">The MediatR mediator for publishing events.</param>
    /// <param name="logger">The logger instance.</param>
    public FileOperationService(IMediator mediator, ILogger<FileOperationService> logger)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public NameValidationResult ValidateName(string name)
    {
        // Empty or null check
        if (string.IsNullOrEmpty(name))
        {
            return NameValidationResult.Invalid("Name cannot be empty.");
        }

        // Whitespace-only check
        if (string.IsNullOrWhiteSpace(name))
        {
            return NameValidationResult.Invalid("Name cannot be only whitespace.");
        }

        // Leading/trailing whitespace check
        if (name != name.Trim())
        {
            return NameValidationResult.Invalid("Name cannot have leading or trailing whitespace.");
        }

        // Length check
        if (name.Length > MaxNameLength)
        {
            return NameValidationResult.Invalid($"Name cannot exceed {MaxNameLength} characters.");
        }

        // Path separators check (explicit, in addition to InvalidNameChars)
        if (name.Contains('/') || name.Contains('\\'))
        {
            return NameValidationResult.Invalid("Name cannot contain path separators.");
        }

        // Directory traversal check
        if (name == ".." || name.Contains(".."))
        {
            return NameValidationResult.Invalid("Name cannot contain directory traversal patterns.");
        }

        // Invalid characters check
        if (name.IndexOfAny(InvalidNameChars) >= 0)
        {
            return NameValidationResult.Invalid("Name contains invalid characters.");
        }

        // Reserved names check (case-insensitive, with or without extension)
        var nameWithoutExtension = Path.GetFileNameWithoutExtension(name);
        if (ReservedNames.Any(r => r.Equals(nameWithoutExtension, StringComparison.OrdinalIgnoreCase)))
        {
            return NameValidationResult.Invalid($"'{nameWithoutExtension}' is a reserved name.");
        }

        return NameValidationResult.Valid;
    }

    /// <inheritdoc/>
    public string GenerateUniqueName(string parentPath, string baseName)
    {
        if (string.IsNullOrEmpty(baseName))
        {
            baseName = "untitled";
        }

        var targetPath = Path.Combine(parentPath, baseName);

        // If no conflict, return as-is
        if (!File.Exists(targetPath) && !Directory.Exists(targetPath))
        {
            return baseName;
        }

        // Extract name and extension for suffix insertion
        var extension = Path.GetExtension(baseName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(baseName);

        var counter = 1;
        string candidateName;

        do
        {
            candidateName = string.IsNullOrEmpty(extension)
                ? $"{nameWithoutExt} ({counter})"
                : $"{nameWithoutExt} ({counter}){extension}";

            targetPath = Path.Combine(parentPath, candidateName);
            counter++;
        }
        while (File.Exists(targetPath) || Directory.Exists(targetPath));

        return candidateName;
    }

    /// <inheritdoc/>
    public async Task<FileOperationResult> CreateFileAsync(string parentPath, string fileName, string content = "")
    {
        _logger.LogDebug("CreateFileAsync: parent={ParentPath}, name={FileName}", parentPath, fileName);

        // Validate name
        var validation = ValidateName(fileName);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateFileAsync failed: invalid name '{FileName}' - {Error}", fileName, validation.ErrorMessage);
            return FileOperationResult.Failed(FileOperationError.InvalidName, validation.ErrorMessage!);
        }

        // Check parent exists
        if (!Directory.Exists(parentPath))
        {
            _logger.LogWarning("CreateFileAsync failed: parent path not found '{ParentPath}'", parentPath);
            return FileOperationResult.Failed(FileOperationError.PathNotFound, $"Parent directory not found: {parentPath}");
        }

        var targetPath = Path.Combine(parentPath, fileName);

        // Check if already exists
        if (File.Exists(targetPath) || Directory.Exists(targetPath))
        {
            _logger.LogWarning("CreateFileAsync failed: '{TargetPath}' already exists", targetPath);
            return FileOperationResult.Failed(FileOperationError.AlreadyExists, $"'{fileName}' already exists.");
        }

        try
        {
            // Create file with content
            await Task.Run(() => File.WriteAllText(targetPath, content ?? string.Empty));

            _logger.LogInformation("Created file: {FilePath}", targetPath);

            // Publish event
            await _mediator.Publish(new FileCreatedEvent(targetPath, fileName, IsDirectory: false));

            return FileOperationResult.Succeeded(targetPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "CreateFileAsync access denied: {TargetPath}", targetPath);
            return FileOperationResult.Failed(FileOperationError.AccessDenied, "Access denied.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "CreateFileAsync IO error: {TargetPath}", targetPath);
            return FileOperationResult.Failed(FileOperationError.Unknown, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<FileOperationResult> CreateFolderAsync(string parentPath, string folderName)
    {
        _logger.LogDebug("CreateFolderAsync: parent={ParentPath}, name={FolderName}", parentPath, folderName);

        // Validate name
        var validation = ValidateName(folderName);
        if (!validation.IsValid)
        {
            _logger.LogWarning("CreateFolderAsync failed: invalid name '{FolderName}' - {Error}", folderName, validation.ErrorMessage);
            return FileOperationResult.Failed(FileOperationError.InvalidName, validation.ErrorMessage!);
        }

        // Check parent exists
        if (!Directory.Exists(parentPath))
        {
            _logger.LogWarning("CreateFolderAsync failed: parent path not found '{ParentPath}'", parentPath);
            return FileOperationResult.Failed(FileOperationError.PathNotFound, $"Parent directory not found: {parentPath}");
        }

        var targetPath = Path.Combine(parentPath, folderName);

        // Check if already exists
        if (File.Exists(targetPath) || Directory.Exists(targetPath))
        {
            _logger.LogWarning("CreateFolderAsync failed: '{TargetPath}' already exists", targetPath);
            return FileOperationResult.Failed(FileOperationError.AlreadyExists, $"'{folderName}' already exists.");
        }

        try
        {
            // Create folder
            await Task.Run(() => Directory.CreateDirectory(targetPath));

            _logger.LogInformation("Created folder: {FolderPath}", targetPath);

            // Publish event
            await _mediator.Publish(new FileCreatedEvent(targetPath, folderName, IsDirectory: true));

            return FileOperationResult.Succeeded(targetPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "CreateFolderAsync access denied: {TargetPath}", targetPath);
            return FileOperationResult.Failed(FileOperationError.AccessDenied, "Access denied.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "CreateFolderAsync IO error: {TargetPath}", targetPath);
            return FileOperationResult.Failed(FileOperationError.Unknown, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<FileOperationResult> RenameAsync(string path, string newName)
    {
        _logger.LogDebug("RenameAsync: path={Path}, newName={NewName}", path, newName);

        // Validate new name
        var validation = ValidateName(newName);
        if (!validation.IsValid)
        {
            _logger.LogWarning("RenameAsync failed: invalid name '{NewName}' - {Error}", newName, validation.ErrorMessage);
            return FileOperationResult.Failed(FileOperationError.InvalidName, validation.ErrorMessage!);
        }

        // Check source exists
        var isDirectory = Directory.Exists(path);
        var isFile = File.Exists(path);

        if (!isDirectory && !isFile)
        {
            _logger.LogWarning("RenameAsync failed: path not found '{Path}'", path);
            return FileOperationResult.Failed(FileOperationError.PathNotFound, $"Path not found: {path}");
        }

        // Check for protected paths (.git folder)
        var fileName = Path.GetFileName(path);
        if (fileName.Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("RenameAsync failed: cannot rename .git folder");
            return FileOperationResult.Failed(FileOperationError.ProtectedPath, "Cannot rename .git folder.");
        }

        var parentPath = Path.GetDirectoryName(path)!;
        var newPath = Path.Combine(parentPath, newName);

        // Check if same name (case-insensitive)
        if (path.Equals(newPath, StringComparison.OrdinalIgnoreCase))
        {
            // Allow case-only renames on case-insensitive filesystems
            if (path.Equals(newPath, StringComparison.Ordinal))
            {
                _logger.LogDebug("RenameAsync: name unchanged, skipping");
                return FileOperationResult.Succeeded(path);
            }
        }

        // Check if target already exists (different path)
        if (!path.Equals(newPath, StringComparison.OrdinalIgnoreCase) &&
            (File.Exists(newPath) || Directory.Exists(newPath)))
        {
            _logger.LogWarning("RenameAsync failed: '{NewPath}' already exists", newPath);
            return FileOperationResult.Failed(FileOperationError.AlreadyExists, $"'{newName}' already exists.");
        }

        var oldName = Path.GetFileName(path);

        try
        {
            await Task.Run(() =>
            {
                if (isDirectory)
                {
                    Directory.Move(path, newPath);
                }
                else
                {
                    File.Move(path, newPath);
                }
            });

            _logger.LogInformation("Renamed: {OldPath} -> {NewPath}", path, newPath);

            // Publish event
            await _mediator.Publish(new FileRenamedEvent(path, newPath, oldName, newName, isDirectory));

            return FileOperationResult.Succeeded(newPath);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "RenameAsync access denied: {Path}", path);
            return FileOperationResult.Failed(FileOperationError.AccessDenied, "Access denied.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "RenameAsync IO error: {Path}", path);
            return FileOperationResult.Failed(FileOperationError.Unknown, ex.Message);
        }
    }

    /// <inheritdoc/>
    public async Task<FileOperationResult> DeleteAsync(string path, bool recursive = false)
    {
        _logger.LogDebug("DeleteAsync: path={Path}, recursive={Recursive}", path, recursive);

        // Check source exists
        var isDirectory = Directory.Exists(path);
        var isFile = File.Exists(path);

        if (!isDirectory && !isFile)
        {
            _logger.LogWarning("DeleteAsync failed: path not found '{Path}'", path);
            return FileOperationResult.Failed(FileOperationError.PathNotFound, $"Path not found: {path}");
        }

        // Check for protected paths
        var fileName = Path.GetFileName(path);
        if (fileName.Equals(".git", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("DeleteAsync failed: cannot delete .git folder");
            return FileOperationResult.Failed(FileOperationError.ProtectedPath, "Cannot delete .git folder.");
        }

        // For directories, check if non-empty when recursive is false
        if (isDirectory && !recursive)
        {
            var hasContents = Directory.EnumerateFileSystemEntries(path).Any();
            if (hasContents)
            {
                _logger.LogWarning("DeleteAsync failed: directory not empty '{Path}'", path);
                return FileOperationResult.Failed(FileOperationError.DirectoryNotEmpty,
                    "Directory is not empty. Use recursive delete to remove non-empty directories.");
            }
        }

        try
        {
            await Task.Run(() =>
            {
                if (isDirectory)
                {
                    Directory.Delete(path, recursive);
                }
                else
                {
                    File.Delete(path);
                }
            });

            _logger.LogInformation("Deleted: {Path} (recursive={Recursive})", path, recursive);

            // Publish event
            await _mediator.Publish(new FileDeletedEvent(path, fileName, isDirectory));

            return FileOperationResult.Succeeded();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "DeleteAsync access denied: {Path}", path);
            return FileOperationResult.Failed(FileOperationError.AccessDenied, "Access denied.");
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "DeleteAsync IO error: {Path}", path);
            return FileOperationResult.Failed(FileOperationError.Unknown, ex.Message);
        }
    }

    /// <inheritdoc/>
    public Task RevealInExplorerAsync(string path)
    {
        _logger.LogDebug("RevealInExplorerAsync: {Path}", path);

        return Task.Run(() =>
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows: Select the item in Explorer
                    Process.Start("explorer.exe", $"/select,\"{path}\"");
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // macOS: Reveal in Finder
                    Process.Start("open", $"-R \"{path}\"");
                }
                else
                {
                    // Linux: Open the containing folder (can't select specific file)
                    var folder = File.Exists(path) ? Path.GetDirectoryName(path) : path;
                    if (!string.IsNullOrEmpty(folder))
                    {
                        Process.Start("xdg-open", $"\"{folder}\"");
                    }
                }

                _logger.LogInformation("Revealed in explorer: {Path}", path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reveal in explorer: {Path}", path);
                // Don't throw - this is a "nice to have" feature
            }
        });
    }
}
