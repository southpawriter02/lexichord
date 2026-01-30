namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Filters document content before scanning to identify exclusion zones.
/// </summary>
/// <remarks>
/// LOGIC: IContentFilter is the abstraction for pre-scan content analysis.
/// Implementations detect regions that should be excluded from linting:
/// - MarkdownCodeBlockFilter: Fenced and inline code blocks
/// - YamlFrontmatterFilter: Document frontmatter (v0.2.7c)
///
/// Filters are applied in Priority order (lower first) by the
/// ContentFilterPipeline. Each filter adds its exclusions to the
/// combined list.
///
/// Design notes:
/// - Filters are stateless and thread-safe
/// - CanFilter allows file-type specific filtering
/// - Priority enables ordered filter execution
///
/// Version: v0.2.7b
/// </remarks>
public interface IContentFilter
{
    /// <summary>
    /// Gets the filter name for logging and debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the filter priority (lower numbers run first).
    /// </summary>
    /// <remarks>
    /// LOGIC: Priority ordering ensures consistent filter application:
    /// - 100: Frontmatter (must run first to detect document header)
    /// - 200: Code blocks (runs on remaining content)
    /// - 300+: Future filters
    /// </remarks>
    int Priority { get; }

    /// <summary>
    /// Checks if this filter can process the given file type.
    /// </summary>
    /// <param name="fileExtension">File extension including dot (e.g., ".md").</param>
    /// <returns>True if this filter applies to the file type.</returns>
    /// <remarks>
    /// LOGIC: Allows file-type specific filtering. For example,
    /// MarkdownCodeBlockFilter only applies to .md, .markdown, .mdx files.
    /// </remarks>
    bool CanFilter(string fileExtension);

    /// <summary>
    /// Analyzes content and returns exclusion zones.
    /// </summary>
    /// <param name="content">The document content to analyze.</param>
    /// <param name="options">Filter configuration options.</param>
    /// <returns>Filtered content with exclusion regions.</returns>
    /// <remarks>
    /// LOGIC: Main filter entry point. Analyzes content and returns:
    /// - ProcessedContent: Unchanged in v0.2.7b
    /// - ExcludedRegions: List of regions to skip during scanning
    /// - OriginalContent: Preserved for reference
    /// </remarks>
    FilteredContent Filter(string content, ContentFilterOptions options);
}
