namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration settings for file indexing behavior.
/// </summary>
/// <remarks>
/// LOGIC (v0.1.5c): Defines which files to index and how:
/// - Ignore patterns exclude common non-content directories
/// - Size limits prevent indexing huge files
/// - Binary extensions filter out non-text files
/// - Recent file tracking has configurable limits
///
/// Bound from configuration section "FileIndex" in appsettings.json.
/// </remarks>
public record FileIndexSettings
{
    /// <summary>
    /// Configuration section name for binding.
    /// </summary>
    public const string SectionName = "FileIndex";

    /// <summary>
    /// Glob patterns for files and directories to ignore.
    /// </summary>
    /// <remarks>
    /// LOGIC: Patterns use glob-style matching:
    /// - "**" matches any directory depth
    /// - "*" matches any filename characters
    /// - Patterns are case-insensitive
    /// </remarks>
    public IReadOnlyList<string> IgnorePatterns { get; init; } = new[]
    {
        ".git/**", ".svn/**", ".hg/**",
        "node_modules/**", "bin/**", "obj/**",
        "dist/**", "build/**", ".idea/**",
        ".vs/**", ".vscode/**", "__pycache__/**",
        "*.pyc", "*.pyo", ".DS_Store", "Thumbs.db"
    };

    /// <summary>
    /// Whether to include hidden files (starting with .) in the index.
    /// </summary>
    public bool IncludeHiddenFiles { get; init; } = false;

    /// <summary>
    /// Maximum file size in bytes to include in the index.
    /// </summary>
    /// <remarks>
    /// LOGIC: Files larger than this are skipped to prevent memory issues.
    /// Default: 50 MB.
    /// </remarks>
    public long MaxFileSizeBytes { get; init; } = 50 * 1024 * 1024;

    /// <summary>
    /// File extensions to treat as binary (excluded from index).
    /// </summary>
    public IReadOnlyList<string> BinaryExtensions { get; init; } = new[]
    {
        ".exe", ".dll", ".so", ".dylib",
        ".zip", ".tar", ".gz", ".7z", ".rar",
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".ico", ".webp",
        ".mp3", ".mp4", ".wav", ".avi", ".mkv", ".mov",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".pdb", ".obj", ".o", ".a"
    };

    /// <summary>
    /// Maximum number of recent files to track.
    /// </summary>
    public int MaxRecentFiles { get; init; } = 50;

    /// <summary>
    /// Whether to index subdirectories recursively.
    /// </summary>
    public bool Recursive { get; init; } = true;

    /// <summary>
    /// Maximum directory depth to index (0 = unlimited).
    /// </summary>
    public int MaxDepth { get; init; } = 0;

    /// <summary>
    /// Debounce delay in milliseconds for file watcher updates.
    /// </summary>
    public int FileWatcherDebounceMs { get; init; } = 300;
}
