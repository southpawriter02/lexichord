namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Information about the current workspace.
/// </summary>
/// <param name="RootPath">Absolute, normalized path to the workspace root folder.</param>
/// <param name="Name">Display name for the workspace (folder name).</param>
/// <param name="OpenedAt">Timestamp when the workspace was opened.</param>
/// <remarks>
/// LOGIC: WorkspaceInfo is immutable. If workspace state changes (e.g., renamed),
/// a new WorkspaceInfo instance is created. This simplifies change detection.
///
/// RootPath is always:
/// - Absolute (not relative)
/// - Normalized via Path.GetFullPath()
/// - Uses platform-appropriate separators
/// </remarks>
public record WorkspaceInfo(
    string RootPath,
    string Name,
    DateTimeOffset OpenedAt
)
{
    /// <summary>
    /// Gets the root path as a DirectoryInfo.
    /// </summary>
    public DirectoryInfo Directory => new(RootPath);

    /// <summary>
    /// Checks if a given path is within this workspace.
    /// </summary>
    /// <param name="path">Path to check.</param>
    /// <returns>True if the path is within the workspace root.</returns>
    /// <remarks>
    /// LOGIC: Used for security validation and path filtering.
    /// Normalizes both paths before comparison.
    /// </remarks>
    public bool ContainsPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        var normalizedPath = Path.GetFullPath(path);
        var normalizedRoot = Path.GetFullPath(RootPath);

        // Ensure root ends with separator for correct prefix matching
        if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar))
            normalizedRoot += Path.DirectorySeparatorChar;

        return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase)
            || normalizedPath.Equals(RootPath, StringComparison.OrdinalIgnoreCase);
    }
}
