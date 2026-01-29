namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for performing file and folder operations with validation and event publishing.
/// </summary>
/// <remarks>
/// LOGIC: IFileOperationService centralizes all file modification operations,
/// ensuring consistent validation, event publishing, and error handling.
///
/// Key responsibilities:
/// - Path validation (within workspace, no traversal attacks)
/// - Name validation (no illegal characters, reserved names, etc.)
/// - Protected path enforcement (workspace root, .git folders)
/// - Event publishing via MediatR after successful operations
/// - Cross-platform "Reveal in Explorer" functionality
///
/// Threading: All async methods are safe to call from any thread.
/// IO operations are performed asynchronously to avoid blocking the UI.
/// </remarks>
public interface IFileOperationService
{
    /// <summary>
    /// Creates a new file with the specified name in the parent directory.
    /// </summary>
    /// <param name="parentPath">The absolute path of the parent directory.</param>
    /// <param name="fileName">The name for the new file.</param>
    /// <param name="content">Optional initial content for the file.</param>
    /// <returns>A result indicating success or failure with the new file path.</returns>
    /// <remarks>
    /// LOGIC:
    /// 1. Validate fileName via ValidateName()
    /// 2. Ensure parentPath exists and is a directory
    /// 3. Ensure target path does not already exist
    /// 4. Create file with content
    /// 5. Publish FileCreatedEvent via MediatR
    /// </remarks>
    Task<FileOperationResult> CreateFileAsync(string parentPath, string fileName, string content = "");

    /// <summary>
    /// Creates a new folder with the specified name in the parent directory.
    /// </summary>
    /// <param name="parentPath">The absolute path of the parent directory.</param>
    /// <param name="folderName">The name for the new folder.</param>
    /// <returns>A result indicating success or failure with the new folder path.</returns>
    /// <remarks>
    /// LOGIC:
    /// 1. Validate folderName via ValidateName()
    /// 2. Ensure parentPath exists and is a directory
    /// 3. Ensure target path does not already exist
    /// 4. Create folder
    /// 5. Publish FileCreatedEvent via MediatR
    /// </remarks>
    Task<FileOperationResult> CreateFolderAsync(string parentPath, string folderName);

    /// <summary>
    /// Renames a file or folder to a new name.
    /// </summary>
    /// <param name="path">The absolute path of the item to rename.</param>
    /// <param name="newName">The new name for the item.</param>
    /// <returns>A result indicating success or failure with the new path.</returns>
    /// <remarks>
    /// LOGIC:
    /// 1. Validate newName via ValidateName()
    /// 2. Check if path is protected (workspace root, .git)
    /// 3. Ensure target path does not already exist
    /// 4. Perform rename (File.Move or Directory.Move)
    /// 5. Publish FileRenamedEvent via MediatR
    /// </remarks>
    Task<FileOperationResult> RenameAsync(string path, string newName);

    /// <summary>
    /// Deletes a file or folder.
    /// </summary>
    /// <param name="path">The absolute path of the item to delete.</param>
    /// <param name="recursive">If true, delete non-empty directories; otherwise fail.</param>
    /// <returns>A result indicating success or failure.</returns>
    /// <remarks>
    /// LOGIC:
    /// 1. Check if path is protected (workspace root, .git)
    /// 2. If directory and not recursive, ensure it's empty
    /// 3. Delete file or directory
    /// 4. Publish FileDeletedEvent via MediatR
    /// </remarks>
    Task<FileOperationResult> DeleteAsync(string path, bool recursive = false);

    /// <summary>
    /// Opens the system file explorer and selects the specified item.
    /// </summary>
    /// <param name="path">The absolute path of the item to reveal.</param>
    /// <remarks>
    /// LOGIC: Uses platform-specific commands:
    /// - Windows: explorer.exe /select,"path"
    /// - macOS: open -R "path"
    /// - Linux: xdg-open "parent-path" (folder containing item)
    /// </remarks>
    Task RevealInExplorerAsync(string path);

    /// <summary>
    /// Generates a unique name by appending a numbered suffix if necessary.
    /// </summary>
    /// <param name="parentPath">The parent directory path.</param>
    /// <param name="baseName">The desired base name (e.g., "untitled.md").</param>
    /// <returns>A unique name that doesn't conflict with existing items.</returns>
    /// <remarks>
    /// LOGIC: If "untitled.md" exists, returns "untitled (1).md", then
    /// "untitled (2).md", etc. Handles names without extensions as well.
    /// </remarks>
    string GenerateUniqueName(string parentPath, string baseName);

    /// <summary>
    /// Validates a file or folder name for illegal characters and reserved names.
    /// </summary>
    /// <param name="name">The name to validate.</param>
    /// <returns>A validation result indicating if the name is valid.</returns>
    /// <remarks>
    /// LOGIC: Checks for:
    /// - Empty or whitespace-only names
    /// - Leading/trailing whitespace
    /// - Illegal filename characters (from Path.GetInvalidFileNameChars())
    /// - Path separators (/ and \)
    /// - Directory traversal patterns (..)
    /// - Reserved Windows names (CON, PRN, AUX, NUL, COM1-9, LPT1-9)
    /// - Names exceeding 255 characters
    /// </remarks>
    NameValidationResult ValidateName(string name);
}
