namespace Lexichord.Modules.Style.Filters;

/// <summary>
/// Configuration options for the YAML frontmatter content filter.
/// </summary>
/// <remarks>
/// LOGIC: YamlFrontmatterOptions controls which frontmatter formats are detected
/// and how edge cases are handled. The filter supports three common formats:
/// - YAML: Delimited by --- (most common in Jekyll, Hugo, etc.)
/// - TOML: Delimited by +++ (common in Hugo)
/// - JSON: Object starting with { on first line (less common)
///
/// Design notes:
/// - All formats enabled by default for comprehensive coverage
/// - StripBom ensures BOM-prefixed files are handled correctly
/// - StrictPositioning ensures frontmatter is only detected at document start
///
/// Version: v0.2.7c
/// </remarks>
public sealed record YamlFrontmatterOptions
{
    /// <summary>
    /// Gets or sets whether YAML frontmatter detection is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: YAML frontmatter uses --- delimiters.
    /// Example:
    /// ---
    /// title: My Document
    /// author: John Doe
    /// ---
    /// </remarks>
    public bool DetectYamlFrontmatter { get; init; } = true;

    /// <summary>
    /// Gets or sets whether TOML frontmatter detection is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: TOML frontmatter uses +++ delimiters.
    /// Example:
    /// +++
    /// title = "My Document"
    /// author = "John Doe"
    /// +++
    /// </remarks>
    public bool DetectTomlFrontmatter { get; init; } = true;

    /// <summary>
    /// Gets or sets whether JSON frontmatter detection is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: JSON frontmatter starts with { on the first line.
    /// Example:
    /// {
    ///   "title": "My Document",
    ///   "author": "John Doe"
    /// }
    /// </remarks>
    public bool DetectJsonFrontmatter { get; init; } = true;

    /// <summary>
    /// Gets or sets whether to strip UTF-8 BOM before detection.
    /// </summary>
    /// <remarks>
    /// LOGIC: Some editors prepend UTF-8 BOM (\uFEFF) to files.
    /// This byte sequence appears before the frontmatter delimiter
    /// and must be stripped for detection to work correctly.
    /// Example: \uFEFF--- becomes --- after stripping.
    /// </remarks>
    public bool StripBom { get; init; } = true;

    /// <summary>
    /// Gets or sets whether frontmatter must be at the document start.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true (default), frontmatter is only detected if it
    /// starts at the very beginning of the document (after optional BOM).
    /// When false, --- anywhere could be considered frontmatter start,
    /// which may cause false positives with horizontal rules.
    /// </remarks>
    public bool StrictPositioning { get; init; } = true;

    /// <summary>
    /// Gets the default filter options with all formats enabled.
    /// </summary>
    public static YamlFrontmatterOptions Default { get; } = new();

    /// <summary>
    /// Gets filter options with only YAML detection enabled.
    /// </summary>
    public static YamlFrontmatterOptions YamlOnly { get; } = new()
    {
        DetectTomlFrontmatter = false,
        DetectJsonFrontmatter = false
    };
}
