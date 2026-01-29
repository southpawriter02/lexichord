using Lexichord.Abstractions.Contracts.Commands;

namespace Lexichord.Host.ViewModels.CommandPalette;

/// <summary>
/// Represents a file search result in the Command Palette.
/// </summary>
/// <remarks>
/// LOGIC: FileSearchResult is a stub for v0.1.5c File Jumper.
/// Full implementation will integrate with IFileIndexService.
/// </remarks>
public record FileSearchResult
{
    /// <summary>
    /// Gets the full path to the file.
    /// </summary>
    public required string FullPath { get; init; }

    /// <summary>
    /// Gets the filename for display.
    /// </summary>
    public string FileName => Path.GetFileName(FullPath);

    /// <summary>
    /// Gets the relative path for display.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the fuzzy match score (0-100).
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Gets the positions of matched characters in the filename.
    /// </summary>
    public IReadOnlyList<MatchPosition> FileNameMatches { get; init; } = [];

    /// <summary>
    /// Gets the icon kind based on file extension.
    /// </summary>
    public string IconKind => GetIconForExtension(Path.GetExtension(FullPath));

    private static string GetIconForExtension(string extension) => extension.ToLowerInvariant() switch
    {
        ".md" => "LanguageMarkdown",
        ".txt" => "FileDocument",
        ".json" => "CodeJson",
        ".xml" => "Xml",
        ".yaml" or ".yml" => "Yaml",
        ".cs" => "LanguageCsharp",
        ".js" => "LanguageJavascript",
        ".ts" => "LanguageTypescript",
        ".html" => "LanguageHtml5",
        ".css" => "LanguageCss3",
        ".py" => "LanguagePython",
        _ => "FileDocument"
    };
}
