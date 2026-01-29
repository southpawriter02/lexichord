namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Action types for file index updates.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): Used by IFileIndexService.UpdateFile to indicate
/// what kind of change occurred to a file in the workspace.
/// </remarks>
public enum FileIndexAction
{
    /// <summary>A new file was created.</summary>
    Created,

    /// <summary>An existing file was modified.</summary>
    Modified,

    /// <summary>A file was deleted.</summary>
    Deleted
}
