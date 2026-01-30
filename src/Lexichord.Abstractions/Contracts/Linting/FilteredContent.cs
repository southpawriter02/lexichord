namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Result of content filtering, containing exclusion zones for the scanner.
/// </summary>
/// <remarks>
/// LOGIC: FilteredContent is the output of IContentFilter.Filter().
/// It carries the content (unchanged for now) plus a list of regions
/// to skip during scanning.
///
/// Design notes:
/// - ProcessedContent equals OriginalContent in v0.2.7b (no content modification)
/// - Future versions could mask or remove excluded content
/// - ExcludedRegions are consumed by scanner to filter matches
///
/// Version: v0.2.7b
/// </remarks>
/// <param name="ProcessedContent">The content after filtering (unchanged in v0.2.7b).</param>
/// <param name="ExcludedRegions">List of regions to exclude from scanning.</param>
/// <param name="OriginalContent">The original unmodified content.</param>
public sealed record FilteredContent(
    string ProcessedContent,
    IReadOnlyList<ExcludedRegion> ExcludedRegions,
    string OriginalContent)
{
    /// <summary>
    /// Gets whether any exclusion zones were detected.
    /// </summary>
    public bool HasExclusions => ExcludedRegions.Count > 0;

    /// <summary>
    /// Gets the total number of characters excluded.
    /// </summary>
    public int TotalExcludedLength => ExcludedRegions.Sum(r => r.Length);

    /// <summary>
    /// Creates a FilteredContent with no exclusions.
    /// </summary>
    /// <param name="content">The original content.</param>
    /// <returns>FilteredContent with empty exclusion list.</returns>
    public static FilteredContent None(string content) =>
        new(content, Array.Empty<ExcludedRegion>(), content);

    /// <summary>
    /// Checks if an offset falls within any excluded region.
    /// </summary>
    /// <param name="offset">The offset to check.</param>
    /// <returns>True if the offset is in any excluded region.</returns>
    public bool IsExcluded(int offset) =>
        ExcludedRegions.Any(r => r.Contains(offset));
}
