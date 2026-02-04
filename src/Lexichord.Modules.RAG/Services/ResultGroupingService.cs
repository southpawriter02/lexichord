// =============================================================================
// File: ResultGroupingService.cs
// Project: Lexichord.Modules.RAG
// Description: Service for grouping search results by source document.
// =============================================================================
// LOGIC: Implements IResultGroupingService with LINQ-based grouping.
//   1. Validate input (null check).
//   2. Early return for empty hits.
//   3. Group hits by Document.FilePath using LINQ GroupBy.
//   4. Create DocumentResultGroup for each group with:
//      - Title extracted from file name (sans extension).
//      - MatchCount = total hits in group.
//      - MaxScore = max(hit.Score) in group.
//      - Hits limited to MaxHitsPerGroup, ordered by descending score.
//      - IsExpanded based on CollapseByDefault option.
//   5. Sort groups according to SortMode.
//   6. Return GroupedSearchResults with aggregate metadata.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchResult, SearchHit (input data).
//   - v0.5.7b: GroupedSearchResults, DocumentResultGroup, ResultGroupingOptions.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Models;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Groups search results by their source document.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ResultGroupingService"/> transforms flat <see cref="SearchResult"/> hits
/// into a hierarchical <see cref="GroupedSearchResults"/> structure organized by document.
/// This supports the collapsible grouped results UI in the Reference Panel.
/// </para>
/// <para>
/// <b>Algorithm:</b>
/// <list type="number">
///   <item><description>Group hits by <see cref="SearchHit.Document"/>.<c>FilePath</c>.</description></item>
///   <item><description>For each group, calculate metadata (count, max score).</description></item>
///   <item><description>Limit hits per group to <see cref="ResultGroupingOptions.MaxHitsPerGroup"/>.</description></item>
///   <item><description>Sort groups by the specified <see cref="ResultSortMode"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe and stateless, suitable for singleton
/// registration.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
public sealed class ResultGroupingService : IResultGroupingService
{
    private readonly ILogger<ResultGroupingService> _logger;

    /// <summary>
    /// Creates a new <see cref="ResultGroupingService"/> instance.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> is null.
    /// </exception>
    public ResultGroupingService(ILogger<ResultGroupingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Groups hits using LINQ GroupBy on Document.FilePath, then transforms
    /// each group into a DocumentResultGroup with calculated metadata.
    /// </para>
    /// <para>
    /// The grouping algorithm:
    /// <list type="number">
    ///   <item><description>Validate input; throw if null.</description></item>
    ///   <item><description>Return empty result if no hits.</description></item>
    ///   <item><description>Group by Document.FilePath (case-sensitive).</description></item>
    ///   <item><description>Create groups with limited, score-ordered hits.</description></item>
    ///   <item><description>Sort groups according to SortMode.</description></item>
    ///   <item><description>Return assembled GroupedSearchResults.</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public GroupedSearchResults GroupByDocument(SearchResult result, ResultGroupingOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        options ??= ResultGroupingOptions.Default;

        _logger.LogDebug(
            "Grouping {HitCount} hits with SortMode={SortMode}, MaxHitsPerGroup={MaxHits}, CollapseByDefault={Collapse}",
            result.Hits.Count,
            options.SortMode,
            options.MaxHitsPerGroup,
            options.CollapseByDefault);

        // LOGIC: Early return for empty results.
        if (result.Hits.Count == 0)
        {
            _logger.LogDebug("No hits to group, returning empty result");
            return GroupedSearchResults.Empty(result.Query);
        }

        // LOGIC: Group hits by document path.
        var groupedHits = result.Hits
            .GroupBy(hit => hit.Document.FilePath)
            .Select(group => CreateDocumentGroup(group, options))
            .ToList();

        // LOGIC: Sort groups according to the specified mode.
        var sortedGroups = SortGroups(groupedHits, options.SortMode);

        _logger.LogDebug(
            "Created {GroupCount} document groups from {HitCount} hits",
            sortedGroups.Count,
            result.Hits.Count);

        return new GroupedSearchResults(
            Groups: sortedGroups,
            TotalHits: result.Hits.Count,
            TotalDocuments: sortedGroups.Count,
            Query: result.Query,
            SearchDuration: result.Duration);
    }

    /// <summary>
    /// Creates a <see cref="DocumentResultGroup"/> from a group of hits.
    /// </summary>
    /// <param name="group">The grouped hits from a single document.</param>
    /// <param name="options">Grouping options for hit limiting and expansion state.</param>
    /// <returns>A new <see cref="DocumentResultGroup"/> instance.</returns>
    /// <remarks>
    /// LOGIC: Extracts document title from file name (sans extension), calculates
    /// max score, limits hits to MaxHitsPerGroup, and sets initial expansion state.
    /// </remarks>
    private static DocumentResultGroup CreateDocumentGroup(
        IGrouping<string, SearchHit> group,
        ResultGroupingOptions options)
    {
        var documentPath = group.Key;
        var allHits = group.ToList();

        // LOGIC: Extract title from file name without extension.
        var fileName = Path.GetFileName(documentPath);
        var title = Path.GetFileNameWithoutExtension(fileName);

        // LOGIC: Calculate max score for relevance sorting.
        var maxScore = allHits.Max(h => h.Score);

        // LOGIC: Order by score descending and limit to MaxHitsPerGroup.
        var limitedHits = allHits
            .OrderByDescending(h => h.Score)
            .Take(options.MaxHitsPerGroup)
            .ToList();

        return new DocumentResultGroup(
            DocumentPath: documentPath,
            DocumentTitle: title,
            MatchCount: allHits.Count,
            MaxScore: maxScore,
            Hits: limitedHits,
            IsExpanded: !options.CollapseByDefault);
    }

    /// <summary>
    /// Sorts document groups according to the specified sort mode.
    /// </summary>
    /// <param name="groups">The groups to sort.</param>
    /// <param name="sortMode">The sort mode to apply.</param>
    /// <returns>A sorted read-only list of groups.</returns>
    /// <remarks>
    /// LOGIC: Applies the appropriate ordering based on SortMode:
    /// - ByRelevance: Descending by MaxScore.
    /// - ByDocumentPath: Ascending by DocumentPath (ordinal, case-insensitive).
    /// - ByMatchCount: Descending by MatchCount.
    /// </remarks>
    private static IReadOnlyList<DocumentResultGroup> SortGroups(
        List<DocumentResultGroup> groups,
        ResultSortMode sortMode)
    {
        return sortMode switch
        {
            ResultSortMode.ByRelevance => groups
                .OrderByDescending(g => g.MaxScore)
                .ToList(),

            ResultSortMode.ByDocumentPath => groups
                .OrderBy(g => g.DocumentPath, StringComparer.OrdinalIgnoreCase)
                .ToList(),

            ResultSortMode.ByMatchCount => groups
                .OrderByDescending(g => g.MatchCount)
                .ToList(),

            _ => groups // Fallback to original order (should not occur).
        };
    }
}
