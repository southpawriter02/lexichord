namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Categorizes errors that can occur during file operations.
/// </summary>
/// <remarks>
/// LOGIC: Used by <see cref="FileOperationResult"/> to provide structured
/// error information that can be handled programmatically by the UI layer.
/// </remarks>
public enum FileOperationError
{
    /// <summary>
    /// The specified path does not exist.
    /// </summary>
    PathNotFound,

    /// <summary>
    /// A file or folder with the target name already exists.
    /// </summary>
    AlreadyExists,

    /// <summary>
    /// The provided name contains invalid characters or is otherwise invalid.
    /// </summary>
    InvalidName,

    /// <summary>
    /// Access to the path was denied (permission issue).
    /// </summary>
    AccessDenied,

    /// <summary>
    /// Attempted to delete a non-empty directory without recursive flag.
    /// </summary>
    DirectoryNotEmpty,

    /// <summary>
    /// The operation targets a protected path (workspace root, .git folder).
    /// </summary>
    ProtectedPath,

    /// <summary>
    /// An unexpected error occurred.
    /// </summary>
    Unknown
}
