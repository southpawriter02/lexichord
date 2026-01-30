namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Configuration options for content filtering.
/// </summary>
/// <remarks>
/// LOGIC: ContentFilterOptions controls which filters are enabled.
/// Individual filters check these flags to determine if they should run.
///
/// Design notes:
/// - All filters enabled by default for comprehensive coverage
/// - Users can disable specific filters via configuration
/// - Future versions may add per-filter configuration
///
/// Version: v0.2.7b
/// </remarks>
public sealed record ContentFilterOptions
{
    /// <summary>
    /// Gets or sets whether code block filtering is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true, fenced and inline code blocks are excluded.
    /// Disable for files where code blocks should be linted.
    /// </remarks>
    public bool EnableCodeBlockFilter { get; init; } = true;

    /// <summary>
    /// Gets or sets whether frontmatter filtering is enabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true, YAML frontmatter is excluded.
    /// Reserved for v0.2.7c implementation.
    /// </remarks>
    public bool EnableFrontmatterFilter { get; init; } = true;

    /// <summary>
    /// Gets the default filter options with all filters enabled.
    /// </summary>
    public static ContentFilterOptions Default { get; } = new();

    /// <summary>
    /// Gets filter options with all filters disabled.
    /// </summary>
    public static ContentFilterOptions None { get; } = new()
    {
        EnableCodeBlockFilter = false,
        EnableFrontmatterFilter = false
    };
}
