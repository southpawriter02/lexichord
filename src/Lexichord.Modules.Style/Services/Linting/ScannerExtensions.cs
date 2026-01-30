using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Style.Services.Linting;

/// <summary>
/// Extension methods for scanner result filtering.
/// </summary>
/// <remarks>
/// LOGIC: ScannerExtensions provides utility methods for filtering
/// scan results based on exclusion regions. Used to remove matches
/// that fall within code blocks or other excluded content.
///
/// Performance:
/// - Uses binary search for large exclusion lists
/// - Sorts exclusions once, then O(log n) per match check
///
/// Version: v0.2.7b
/// </remarks>
public static class ScannerExtensions
{
    /// <summary>
    /// Filters matches to exclude those in excluded regions.
    /// </summary>
    /// <param name="matches">Raw matches from scanner.</param>
    /// <param name="excludedRegions">Regions to filter out.</param>
    /// <returns>Matches not falling within any excluded region.</returns>
    /// <remarks>
    /// LOGIC: For each match, check if its start offset falls
    /// within any excluded region. If so, skip the match.
    ///
    /// Performance: Uses binary search for large exclusion lists.
    /// </remarks>
    public static IEnumerable<ScanMatch> FilterByExclusions(
        this IEnumerable<ScanMatch> matches,
        IReadOnlyList<ExcludedRegion> excludedRegions)
    {
        if (excludedRegions.Count == 0)
        {
            return matches;
        }

        // LOGIC: Sort exclusions for binary search
        var sortedExclusions = excludedRegions
            .OrderBy(e => e.StartOffset)
            .ToList();

        return matches.Where(m => !IsExcluded(m.StartOffset, sortedExclusions));
    }

    /// <summary>
    /// Filters matches to exclude those in excluded regions from FilteredContent.
    /// </summary>
    /// <param name="matches">Raw matches from scanner.</param>
    /// <param name="filteredContent">Filtered content with exclusion regions.</param>
    /// <returns>Matches not falling within any excluded region.</returns>
    public static IEnumerable<ScanMatch> FilterByExclusions(
        this IEnumerable<ScanMatch> matches,
        FilteredContent filteredContent)
    {
        return matches.FilterByExclusions(filteredContent.ExcludedRegions);
    }

    /// <summary>
    /// Checks if an offset falls within any excluded region using binary search.
    /// </summary>
    /// <param name="offset">The offset to check.</param>
    /// <param name="sortedExclusions">Exclusions sorted by StartOffset.</param>
    /// <returns>True if the offset is within any excluded region.</returns>
    /// <remarks>
    /// LOGIC: Uses modified binary search to find if offset is contained
    /// in any region. First finds candidate regions, then checks bounds.
    ///
    /// Time complexity: O(log n) for n exclusion regions.
    /// </remarks>
    private static bool IsExcluded(
        int offset,
        IReadOnlyList<ExcludedRegion> sortedExclusions)
    {
        // LOGIC: Binary search for the first region that could contain offset
        var lo = 0;
        var hi = sortedExclusions.Count - 1;

        while (lo <= hi)
        {
            var mid = lo + (hi - lo) / 2;
            var region = sortedExclusions[mid];

            if (offset >= region.StartOffset && offset < region.EndOffset)
            {
                return true; // Found containing region
            }

            if (offset < region.StartOffset)
            {
                hi = mid - 1;
            }
            else
            {
                lo = mid + 1;
            }
        }

        return false;
    }

    /// <summary>
    /// Counts matches that would be filtered by exclusions.
    /// </summary>
    /// <param name="matches">Raw matches from scanner.</param>
    /// <param name="excludedRegions">Regions to filter out.</param>
    /// <returns>Count of matches falling within excluded regions.</returns>
    public static int CountExcludedMatches(
        this IEnumerable<ScanMatch> matches,
        IReadOnlyList<ExcludedRegion> excludedRegions)
    {
        if (excludedRegions.Count == 0)
        {
            return 0;
        }

        var sortedExclusions = excludedRegions
            .OrderBy(e => e.StartOffset)
            .ToList();

        return matches.Count(m => IsExcluded(m.StartOffset, sortedExclusions));
    }
}
