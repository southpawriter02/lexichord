// =============================================================================
// File: GroupedResultsViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for managing grouped search results display.
// =============================================================================
// LOGIC: MVVM ViewModel using CommunityToolkit.Mvvm for grouped results UI.
//   1. Results property holds current GroupedSearchResults.
//   2. SortMode property controls group ordering.
//   3. AllExpanded tracks global expansion state.
//   4. ExpandAll/CollapseAll commands toggle all groups.
//   5. ToggleGroup command toggles individual group expansion.
//   6. ChangeSortMode command updates sort and re-groups.
//   7. UpdateResults method accepts new SearchResult and applies grouping.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchResult (input data).
//   - v0.5.7b: IResultGroupingService, GroupedSearchResults, ResultSortMode.
//   - CommunityToolkit.Mvvm: ObservableProperty, RelayCommand.
// =============================================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for displaying and managing grouped search results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="GroupedResultsViewModel"/> provides the presentation logic for the
/// grouped results UI, including expansion state management, sort mode switching,
/// and group toggling. It delegates grouping logic to <see cref="IResultGroupingService"/>.
/// </para>
/// <para>
/// <b>State Management:</b>
/// <list type="bullet">
///   <item><description><see cref="Results"/>: Current grouped results for display.</description></item>
///   <item><description><see cref="SortMode"/>: Current sort order for groups.</description></item>
///   <item><description><see cref="AllExpanded"/>: Whether all groups are expanded.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Commands:</b>
/// <list type="bullet">
///   <item><description><see cref="ExpandAllCommand"/>: Expands all groups.</description></item>
///   <item><description><see cref="CollapseAllCommand"/>: Collapses all groups.</description></item>
///   <item><description><see cref="ToggleGroupCommand"/>: Toggles a single group's expansion.</description></item>
///   <item><description><see cref="ChangeSortModeCommand"/>: Changes the sort mode and re-groups.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.7b as part of the Result Grouping feature.
/// </para>
/// </remarks>
public partial class GroupedResultsViewModel : ObservableObject
{
    private readonly IResultGroupingService _groupingService;
    private readonly ILogger<GroupedResultsViewModel> _logger;
    private SearchResult? _rawResult;

    /// <summary>
    /// Creates a new <see cref="GroupedResultsViewModel"/> instance.
    /// </summary>
    /// <param name="groupingService">Service for grouping search results.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public GroupedResultsViewModel(
        IResultGroupingService groupingService,
        ILogger<GroupedResultsViewModel> logger)
    {
        _groupingService = groupingService ?? throw new ArgumentNullException(nameof(groupingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // =========================================================================
    // Observable Properties
    // =========================================================================

    /// <summary>
    /// Gets or sets the current grouped search results.
    /// </summary>
    /// <remarks>
    /// LOGIC: Updated when new search results are received via <see cref="UpdateResults"/>
    /// or when the sort mode changes. Null before any search is performed.
    /// </remarks>
    [ObservableProperty]
    private GroupedSearchResults? _results;

    /// <summary>
    /// Gets or sets the current sort mode for groups.
    /// </summary>
    /// <remarks>
    /// LOGIC: Changing this property triggers <see cref="ReapplyGrouping"/> to
    /// re-sort the current results without re-executing the search.
    /// </remarks>
    [ObservableProperty]
    private ResultSortMode _sortMode = ResultSortMode.ByRelevance;

    /// <summary>
    /// Gets or sets whether all groups are currently expanded.
    /// </summary>
    /// <remarks>
    /// LOGIC: Set to <c>true</c> after <see cref="ExpandAllCommand"/>, <c>false</c>
    /// after <see cref="CollapseAllCommand"/>. Individual toggles may desync this state.
    /// </remarks>
    [ObservableProperty]
    private bool _allExpanded = true;

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Gets whether results are available to display.
    /// </summary>
    public bool HasResults => Results?.HasResults == true;

    /// <summary>
    /// Gets the total number of groups.
    /// </summary>
    public int GroupCount => Results?.Groups.Count ?? 0;

    /// <summary>
    /// Gets all available sort mode values for the UI dropdown.
    /// </summary>
    public ResultSortMode[] AvailableSortModes { get; } = Enum.GetValues<ResultSortMode>();

    // =========================================================================
    // Commands
    // =========================================================================

    /// <summary>
    /// Expands all document groups.
    /// </summary>
    /// <remarks>
    /// LOGIC: Creates new group instances with IsExpanded=true and updates Results.
    /// This pattern preserves immutability of the record types.
    /// </remarks>
    [RelayCommand]
    private void ExpandAll()
    {
        if (Results is null || Results.Groups.Count == 0)
            return;

        _logger.LogDebug("Expanding all {Count} groups", Results.Groups.Count);

        var expandedGroups = Results.Groups
            .Select(g => g with { IsExpanded = true })
            .ToList();

        Results = Results with { Groups = expandedGroups };
        AllExpanded = true;
    }

    /// <summary>
    /// Collapses all document groups.
    /// </summary>
    /// <remarks>
    /// LOGIC: Creates new group instances with IsExpanded=false and updates Results.
    /// </remarks>
    [RelayCommand]
    private void CollapseAll()
    {
        if (Results is null || Results.Groups.Count == 0)
            return;

        _logger.LogDebug("Collapsing all {Count} groups", Results.Groups.Count);

        var collapsedGroups = Results.Groups
            .Select(g => g with { IsExpanded = false })
            .ToList();

        Results = Results with { Groups = collapsedGroups };
        AllExpanded = false;
    }

    /// <summary>
    /// Toggles the expansion state of a single group.
    /// </summary>
    /// <param name="group">The group to toggle.</param>
    /// <remarks>
    /// LOGIC: Finds the group by DocumentPath and creates a new instance with
    /// the inverted IsExpanded value. Updates AllExpanded state accordingly.
    /// </remarks>
    [RelayCommand]
    private void ToggleGroup(DocumentResultGroup? group)
    {
        if (group is null || Results is null)
            return;

        _logger.LogDebug(
            "Toggling group {Path}: {OldState} -> {NewState}",
            group.DocumentPath,
            group.IsExpanded,
            !group.IsExpanded);

        var updatedGroups = Results.Groups
            .Select(g => g.DocumentPath == group.DocumentPath
                ? g with { IsExpanded = !g.IsExpanded }
                : g)
            .ToList();

        Results = Results with { Groups = updatedGroups };

        // LOGIC: Update AllExpanded based on current state of all groups.
        AllExpanded = updatedGroups.All(g => g.IsExpanded);
    }

    /// <summary>
    /// Changes the sort mode and re-groups the results.
    /// </summary>
    /// <param name="mode">The new sort mode to apply.</param>
    /// <remarks>
    /// LOGIC: Updates SortMode and calls <see cref="ReapplyGrouping"/> to
    /// re-sort the current results without re-executing the search.
    /// </remarks>
    [RelayCommand]
    private void ChangeSortMode(ResultSortMode mode)
    {
        if (mode == SortMode)
            return;

        _logger.LogDebug("Sort mode changed: {OldMode} -> {NewMode}", SortMode, mode);
        SortMode = mode;
        ReapplyGrouping();
    }

    // =========================================================================
    // Public Methods
    // =========================================================================

    /// <summary>
    /// Updates the grouped results with new search data.
    /// </summary>
    /// <param name="result">The search result to group and display.</param>
    /// <remarks>
    /// LOGIC: Stores the raw result for re-grouping on sort mode changes,
    /// then applies the current grouping options.
    /// </remarks>
    public void UpdateResults(SearchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        _logger.LogDebug(
            "Updating results with {Count} hits, SortMode={Mode}",
            result.Hits.Count,
            SortMode);

        _rawResult = result;
        ReapplyGrouping();
    }

    /// <summary>
    /// Clears all results and resets state.
    /// </summary>
    public void Clear()
    {
        _logger.LogDebug("Clearing grouped results");
        _rawResult = null;
        Results = null;
        AllExpanded = true;
    }

    // =========================================================================
    // Private Methods
    // =========================================================================

    /// <summary>
    /// Re-applies grouping to the stored raw result with current options.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when SortMode changes or new results are received.
    /// Preserves expansion state by using CollapseByDefault = !AllExpanded.
    /// </remarks>
    private void ReapplyGrouping()
    {
        if (_rawResult is null)
        {
            Results = null;
            return;
        }

        var options = new ResultGroupingOptions(
            SortMode: SortMode,
            MaxHitsPerGroup: 10,
            CollapseByDefault: !AllExpanded);

        Results = _groupingService.GroupByDocument(_rawResult, options);

        _logger.LogDebug(
            "Regrouped results: {GroupCount} groups, {HitCount} total hits",
            Results.Groups.Count,
            Results.TotalHits);
    }
}
