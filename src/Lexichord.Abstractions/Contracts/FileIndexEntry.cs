namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a single file entry in the workspace index.
/// </summary>
/// <param name="FileName">The file name without path.</param>
/// <param name="FullPath">The absolute path to the file.</param>
/// <param name="RelativePath">Path relative to the workspace root.</param>
/// <param name="LastModified">Last modification timestamp.</param>
/// <param name="FileSize">File size in bytes.</param>
/// <remarks>
/// LOGIC (v0.1.5c): Immutable record storing file metadata for search.
/// Computed properties provide UI-friendly values for icons and display.
/// </remarks>
public record FileIndexEntry(
    string FileName,
    string FullPath,
    string RelativePath,
    DateTime LastModified,
    long FileSize
)
{
    /// <summary>
    /// Gets the lowercase file extension (e.g., ".cs").
    /// </summary>
    public string Extension => Path.GetExtension(FileName).ToLowerInvariant();

    /// <summary>
    /// Gets the Material icon name for this file type.
    /// </summary>
    /// <remarks>
    /// LOGIC: Maps common extensions to Material.Icons names.
    /// Unknown extensions default to "FileDocument".
    /// </remarks>
    public string IconKind => GetIconForExtension(Extension);

    /// <summary>
    /// Gets the directory portion of the relative path.
    /// </summary>
    public string DirectoryName => Path.GetDirectoryName(RelativePath) ?? string.Empty;

    /// <summary>
    /// Gets a human-readable file size string (e.g., "1.5 KB").
    /// </summary>
    public string FileSizeDisplay => FormatFileSize(FileSize);

    /// <summary>
    /// Maps file extension to Material icon name.
    /// </summary>
    private static string GetIconForExtension(string extension) => extension switch
    {
        // Programming languages
        ".cs" => "LanguageCsharp",
        ".fs" => "LanguageFsharp",
        ".vb" => "LanguageVisualBasic",
        ".js" => "LanguageJavascript",
        ".ts" => "LanguageTypescript",
        ".jsx" or ".tsx" => "React",
        ".py" => "LanguagePython",
        ".java" => "LanguageJava",
        ".kt" or ".kts" => "LanguageKotlin",
        ".go" => "LanguageGo",
        ".rs" => "LanguageRust",
        ".cpp" or ".cc" or ".cxx" => "LanguageCpp",
        ".c" => "LanguageC",
        ".h" or ".hpp" => "FileCode",
        ".swift" => "LanguageSwift",
        ".rb" => "LanguageRuby",
        ".php" => "LanguagePhp",
        ".lua" => "LanguageLua",
        ".r" => "LanguageR",

        // Web technologies
        ".html" or ".htm" => "LanguageHtml5",
        ".css" => "LanguageCss3",
        ".scss" or ".sass" => "Sass",
        ".vue" => "Vuejs",
        ".svelte" => "Svelte",

        // Data formats
        ".json" => "CodeJson",
        ".xml" => "FileXml",
        ".yaml" or ".yml" => "FileDocument",
        ".toml" => "FileDocument",
        ".csv" => "FileDelimited",
        ".sql" => "Database",

        // Documents
        ".md" or ".markdown" => "LanguageMarkdown",
        ".txt" => "FileDocument",
        ".rtf" => "FileDocument",
        ".pdf" => "FilePdfBox",
        ".doc" or ".docx" => "FileWord",
        ".xls" or ".xlsx" => "FileExcel",
        ".ppt" or ".pptx" => "FilePowerpoint",

        // Config files
        ".config" or ".cfg" => "Cog",
        ".ini" => "Cog",
        ".env" => "Key",
        ".gitignore" or ".dockerignore" => "Git",

        // Shell and scripts
        ".sh" or ".bash" => "Console",
        ".ps1" or ".psm1" => "Powershell",
        ".bat" or ".cmd" => "Console",

        // Build files
        ".csproj" or ".fsproj" or ".vbproj" => "VisualStudio",
        ".sln" => "VisualStudio",
        ".gradle" => "Gradle",
        ".dockerfile" or "dockerfile" => "Docker",

        // Xaml/UI
        ".axaml" or ".xaml" => "Xaml",

        // Images (for reference, typically filtered out)
        ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".ico" or ".webp" => "Image",

        // Default
        _ => "FileDocument"
    };

    /// <summary>
    /// Formats file size to human-readable string.
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        return bytes switch
        {
            >= GB => $"{bytes / (double)GB:F1} GB",
            >= MB => $"{bytes / (double)MB:F1} MB",
            >= KB => $"{bytes / (double)KB:F1} KB",
            _ => $"{bytes} B"
        };
    }
}
