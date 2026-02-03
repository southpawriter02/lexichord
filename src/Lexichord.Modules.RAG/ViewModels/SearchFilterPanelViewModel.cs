// =============================================================================
// File: SearchFilterPanelViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for the search filter panel with folder tree, extension
//              toggles, date range picker, and saved presets.
// =============================================================================
// LOGIC: SearchFilterPanelViewModel orchestrates the filter panel UI:
//   1. Folder tree: Workspace folders with checkbox selection.
//   2. Extension toggles: Quick toggles for common file types.
//   3. Date range picker: Time-based filtering (license-gated to WriterPro+).
//   4. Saved presets: Filter preset management (license-gated to WriterPro+).
//   5. Filter chips: Visual display of active filters with removal support.
//   6. CurrentFilter: Builds SearchFilter from UI state for search services.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.5a: SearchFilter, DateRange, FilterPreset, IFilterValidator
//   - v0.1.2a: IWorkspaceService (workspace root)
//   - v0.0.4c: ILicenseContext (feature gating)
// =============================================================================
// VERSION: v0.5.5b (Filter UI Component)
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Enums;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for the search filter panel with folder tree, extension toggles,
/// date range picker, and saved presets.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchFilterPanelViewModel"/> manages the search filter panel UI,
/// enabling users to narrow search results by folder path, file extension,
/// modification date, and saved presets.
/// </para>
/// <para>
/// <b>License Gating:</b>
/// <list type="bullet">
///   <item><description>Core tier: Path filtering, extension filtering.</description></item>
///   <item><description>WriterPro+ tier: Date range filtering, saved presets.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Filter Building:</b>
/// The <see cref="CurrentFilter"/> property builds a <see cref="SearchFilter"/>
/// from the current UI state. This filter is consumed by search services
/// (v0.5.5c Filter Query Builder).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.5b as part of The Filter System feature.
/// </para>
/// </remarks>
public partial class SearchFilterPanelViewModel : ObservableObject
{
    // =========================================================================
    // Dependencies
    // =========================================================================

    private readonly IWorkspaceService _workspaceService;
    private readonly IFilterValidator _filterValidator;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<SearchFilterPanelViewModel> _logger;

    // =========================================================================
    // Constructor
    // =========================================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchFilterPanelViewModel"/> class.
    /// </summary>
    /// <param name="workspaceService">Service for accessing workspace information (v0.1.2a).</param>
    /// <param name="filterValidator">Validator for filter criteria (v0.5.5a).</param>
    /// <param name="licenseContext">License context for feature gating (v0.0.4c).</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public SearchFilterPanelViewModel(
        IWorkspaceService workspaceService,
        IFilterValidator filterValidator,
        ILicenseContext licenseContext,
        ILogger<SearchFilterPanelViewModel> logger)
    {
        _workspaceService = workspaceService ?? throw new ArgumentNullException(nameof(workspaceService));
        _filterValidator = filterValidator ?? throw new ArgumentNullException(nameof(filterValidator));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Initialize license-gated feature availability.
        InitializeLicenseFeatures();

        // LOGIC: Initialize extension toggles with default extensions.
        InitializeExtensionToggles();

        _logger.LogDebug(
            "SearchFilterPanelViewModel initialized: CanUseDateFilter={CanUseDateFilter}, CanUseSavedPresets={CanUseSavedPresets}",
            CanUseDateFilter,
            CanUseSavedPresets);
    }

    // =========================================================================
    // Observable Properties
    // =========================================================================

    /// <summary>
    /// Gets or sets whether the filter panel is expanded.
    /// </summary>
    /// <value><c>true</c> if expanded; otherwise, <c>false</c>.</value>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Gets the folder tree for path selection.
    /// </summary>
    /// <value>The root nodes of the folder tree.</value>
    [ObservableProperty]
    private ObservableCollection<FolderNodeViewModel> _folderTree = new();

    /// <summary>
    /// Gets the extension toggle buttons.
    /// </summary>
    /// <value>The available extension toggles.</value>
    [ObservableProperty]
    private ObservableCollection<ExtensionToggleViewModel> _extensionToggles = new();

    /// <summary>
    /// Gets or sets the currently selected date range option.
    /// </summary>
    /// <value>The selected date range option.</value>
    /// <remarks>
    /// Date range filtering requires WriterPro tier.
    /// </remarks>
    [ObservableProperty]
    private DateRangeOption _selectedDateRange = DateRangeOption.AnyTime;

    /// <summary>
    /// Gets or sets the custom start date for date range filtering.
    /// </summary>
    /// <value>The custom start date, or null if not set.</value>
    /// <remarks>
    /// Only used when <see cref="SelectedDateRange"/> is <see cref="DateRangeOption.Custom"/>.
    /// </remarks>
    [ObservableProperty]
    private DateTime? _customStartDate;

    /// <summary>
    /// Gets or sets the custom end date for date range filtering.
    /// </summary>
    /// <value>The custom end date, or null if not set.</value>
    /// <remarks>
    /// Only used when <see cref="SelectedDateRange"/> is <see cref="DateRangeOption.Custom"/>.
    /// </remarks>
    [ObservableProperty]
    private DateTime? _customEndDate;

    /// <summary>
    /// Gets the list of saved filter presets.
    /// </summary>
    /// <value>The available filter presets.</value>
    /// <remarks>
    /// Saved presets require WriterPro tier.
    /// Presets are loaded from <see cref="IFilterPresetService"/> (v0.5.5d).
    /// </remarks>
    [ObservableProperty]
    private ObservableCollection<FilterPreset> _savedPresets = new();

    /// <summary>
    /// Gets the active filter chips for display.
    /// </summary>
    /// <value>The currently active filter chips.</value>
    [ObservableProperty]
    private ObservableCollection<FilterChipViewModel> _activeChips = new();

    /// <summary>
    /// Gets whether the date range filter feature is available (WriterPro+).
    /// </summary>
    /// <value><c>true</c> if the user has WriterPro or higher tier; otherwise, <c>false</c>.</value>
    [ObservableProperty]
    private bool _canUseDateFilter;

    /// <summary>
    /// Gets whether saved presets are available (WriterPro+).
    /// </summary>
    /// <value><c>true</c> if the user has WriterPro or higher tier; otherwise, <c>false</c>.</value>
    [ObservableProperty]
    private bool _canUseSavedPresets;

    // =========================================================================
    // Computed Properties
    // =========================================================================

    /// <summary>
    /// Gets whether any filters are currently active.
    /// </summary>
    /// <value><c>true</c> if at least one filter is active; otherwise, <c>false</c>.</value>
    public bool HasActiveFilters => ActiveChips.Count > 0;

    /// <summary>
    /// Builds the current <see cref="SearchFilter"/> from UI state.
    /// </summary>
    /// <value>The current filter reflecting all selected criteria.</value>
    /// <remarks>
    /// <para>
    /// This property is consumed by search services to apply filtering.
    /// It is rebuilt each time it is accessed to reflect the current UI state.
    /// </para>
    /// <para>
    /// Date range filtering is only included if the user has WriterPro tier.
    /// </para>
    /// </remarks>
    public SearchFilter CurrentFilter => BuildFilter();

    // =========================================================================
    // Commands
    // =========================================================================

    /// <summary>
    /// Initializes the folder tree from the workspace.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Call this command when the filter panel is first expanded or when
    /// the workspace changes.
    /// </remarks>
    [RelayCommand]
    public async Task LoadFolderTreeAsync()
    {
        _logger.LogDebug("Loading folder tree from workspace");

        var workspace = _workspaceService.CurrentWorkspace;
        if (workspace is null)
        {
            _logger.LogDebug("No workspace open, clearing folder tree");
            FolderTree.Clear();
            return;
        }

        var rootPath = workspace.RootPath;
        _logger.LogDebug("Building folder tree from workspace root: {RootPath}", rootPath);

        FolderTree.Clear();

        try
        {
            var rootNode = await BuildFolderTreeAsync(rootPath, maxDepth: 3);
            if (rootNode is not null)
            {
                FolderTree.Add(rootNode);
            }

            _logger.LogDebug("Folder tree loaded with {NodeCount} root nodes", FolderTree.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load folder tree from workspace");
        }
    }

    /// <summary>
    /// Clears all active filters.
    /// </summary>
    /// <remarks>
    /// Resets folder selection, extension toggles, and date range to defaults.
    /// </remarks>
    [RelayCommand]
    public void ClearFilters()
    {
        _logger.LogDebug("Clearing all filters");

        // LOGIC: Clear folder selections.
        foreach (var folder in GetAllFolderNodes())
        {
            folder.IsSelected = false;
        }

        // LOGIC: Clear extension selections.
        foreach (var toggle in ExtensionToggles)
        {
            toggle.IsSelected = false;
        }

        // LOGIC: Reset date range to default.
        SelectedDateRange = DateRangeOption.AnyTime;
        CustomStartDate = null;
        CustomEndDate = null;

        // LOGIC: Update UI chips.
        UpdateActiveChips();

        _logger.LogDebug("All filters cleared");
    }

    /// <summary>
    /// Removes a specific filter chip.
    /// </summary>
    /// <param name="chip">The chip to remove.</param>
    /// <remarks>
    /// Deselects the corresponding filter criterion based on chip type.
    /// </remarks>
    [RelayCommand]
    public void RemoveChip(FilterChipViewModel chip)
    {
        if (chip is null)
        {
            return;
        }

        _logger.LogDebug("Removing filter chip: {ChipType}={ChipValue}", chip.Type, chip.Value);

        switch (chip.Type)
        {
            case FilterChipType.Path:
                var folder = GetAllFolderNodes()
                    .FirstOrDefault(f => f.Path == chip.Value || f.GetGlobPattern() == chip.Value);
                if (folder is not null)
                {
                    folder.IsSelected = false;
                }
                break;

            case FilterChipType.Extension:
                var toggle = ExtensionToggles
                    .FirstOrDefault(t => t.Extension.Equals(chip.Value, StringComparison.OrdinalIgnoreCase));
                if (toggle is not null)
                {
                    toggle.IsSelected = false;
                }
                break;

            case FilterChipType.DateRange:
                SelectedDateRange = DateRangeOption.AnyTime;
                CustomStartDate = null;
                CustomEndDate = null;
                break;

            case FilterChipType.Tag:
                // LOGIC: Tags are reserved for future implementation.
                break;
        }

        UpdateActiveChips();
    }

    /// <summary>
    /// Applies a saved preset.
    /// </summary>
    /// <param name="preset">The preset to apply.</param>
    /// <remarks>
    /// Clears current filters and applies all criteria from the preset.
    /// </remarks>
    [RelayCommand]
    public void ApplyPreset(FilterPreset preset)
    {
        if (preset is null)
        {
            return;
        }

        _logger.LogDebug("Applying preset: {PresetName}", preset.Name);

        // LOGIC: Clear existing filters first.
        ClearFilters();

        // LOGIC: Apply path patterns.
        if (preset.Filter.PathPatterns?.Count > 0)
        {
            foreach (var pattern in preset.Filter.PathPatterns)
            {
                var folder = GetAllFolderNodes()
                    .FirstOrDefault(f => MatchesPattern(f.Path, pattern) || f.GetGlobPattern() == pattern);
                if (folder is not null)
                {
                    folder.IsSelected = true;
                }
            }
        }

        // LOGIC: Apply extensions.
        if (preset.Filter.FileExtensions?.Count > 0)
        {
            foreach (var ext in preset.Filter.FileExtensions)
            {
                var toggle = ExtensionToggles
                    .FirstOrDefault(t => t.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase));
                if (toggle is not null)
                {
                    toggle.IsSelected = true;
                }
            }
        }

        // LOGIC: Apply date range (if licensed).
        if (preset.Filter.ModifiedRange is not null && CanUseDateFilter)
        {
            SelectedDateRange = DateRangeOption.Custom;
            CustomStartDate = preset.Filter.ModifiedRange.Start;
            CustomEndDate = preset.Filter.ModifiedRange.End;
        }

        UpdateActiveChips();
        _logger.LogDebug("Preset applied: {PresetName}", preset.Name);
    }

    // =========================================================================
    // Property Change Handlers
    // =========================================================================

    /// <summary>
    /// Called when <see cref="SelectedDateRange"/> changes.
    /// Updates filter chips to reflect the new selection.
    /// </summary>
    /// <param name="value">The new date range option.</param>
    partial void OnSelectedDateRangeChanged(DateRangeOption value)
    {
        _logger.LogDebug("Date range changed to: {DateRange}", value);
        UpdateActiveChips();
    }

    // =========================================================================
    // Private Methods
    // =========================================================================

    /// <summary>
    /// Builds a <see cref="SearchFilter"/> from the current UI state.
    /// </summary>
    /// <returns>The constructed filter.</returns>
    private SearchFilter BuildFilter()
    {
        var pathPatterns = GetSelectedPaths().ToList();
        var extensions = GetSelectedExtensions().ToList();
        var dateRange = BuildDateRange();

        var filter = new SearchFilter(
            PathPatterns: pathPatterns.Count > 0 ? pathPatterns : null,
            FileExtensions: extensions.Count > 0 ? extensions : null,
            ModifiedRange: dateRange);

        _logger.LogDebug(
            "Built filter: {PathCount} paths, {ExtCount} extensions, DateRange={HasDateRange}",
            pathPatterns.Count,
            extensions.Count,
            dateRange is not null);

        return filter;
    }

    /// <summary>
    /// Builds a <see cref="DateRange"/> from the current UI state.
    /// </summary>
    /// <returns>The date range, or null if no date filtering is applied.</returns>
    private DateRange? BuildDateRange()
    {
        // LOGIC: Date range filtering requires WriterPro tier.
        if (!CanUseDateFilter)
        {
            return null;
        }

        return SelectedDateRange switch
        {
            DateRangeOption.AnyTime => null,
            DateRangeOption.LastDay => DateRange.LastDays(1),
            DateRangeOption.Last7Days => DateRange.LastDays(7),
            DateRangeOption.Last30Days => DateRange.LastDays(30),
            DateRangeOption.Custom => new DateRange(CustomStartDate, CustomEndDate),
            _ => null
        };
    }

    /// <summary>
    /// Initializes license-gated feature availability.
    /// </summary>
    private void InitializeLicenseFeatures()
    {
        CanUseDateFilter = _licenseContext.IsFeatureEnabled(FeatureCodes.DateRangeFilter);
        CanUseSavedPresets = _licenseContext.IsFeatureEnabled(FeatureCodes.SavedPresets);

        _logger.LogDebug(
            "License features initialized: DateRangeFilter={DateFilter}, SavedPresets={Presets}",
            CanUseDateFilter,
            CanUseSavedPresets);
    }

    /// <summary>
    /// Initializes the extension toggles with default extensions.
    /// </summary>
    private void InitializeExtensionToggles()
    {
        ExtensionToggles = new ObservableCollection<ExtensionToggleViewModel>
        {
            new("md", "Markdown", "M↓"),
            new("txt", "Text", "T"),
            new("json", "JSON", "{}"),
            new("yaml", "YAML", "Y"),
            new("rst", "RST", "R")
        };

        // LOGIC: Subscribe to toggle changes to update filter chips.
        foreach (var toggle in ExtensionToggles)
        {
            toggle.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ExtensionToggleViewModel.IsSelected))
                {
                    UpdateActiveChips();
                }
            };
        }

        _logger.LogDebug("Extension toggles initialized with {Count} extensions", ExtensionToggles.Count);
    }

    /// <summary>
    /// Updates the active filter chips based on current selections.
    /// </summary>
    private void UpdateActiveChips()
    {
        ActiveChips.Clear();

        // LOGIC: Add path chips for selected folders.
        foreach (var path in GetSelectedPaths())
        {
            var folderName = System.IO.Path.GetFileName(path.TrimEnd('/', '*', '\\'));
            if (string.IsNullOrEmpty(folderName))
            {
                folderName = path;
            }

            ActiveChips.Add(new FilterChipViewModel(
                FilterChipType.Path,
                $"📁 {folderName}",
                path));
        }

        // LOGIC: Add extension chips for selected extensions.
        foreach (var ext in GetSelectedExtensions())
        {
            ActiveChips.Add(new FilterChipViewModel(
                FilterChipType.Extension,
                $".{ext}",
                ext));
        }

        // LOGIC: Add date range chip (if applicable and licensed).
        if (CanUseDateFilter && SelectedDateRange != DateRangeOption.AnyTime)
        {
            var dateLabel = SelectedDateRange switch
            {
                DateRangeOption.LastDay => "Last 24 hours",
                DateRangeOption.Last7Days => "Last 7 days",
                DateRangeOption.Last30Days => "Last 30 days",
                DateRangeOption.Custom => "Custom range",
                _ => "Date filter"
            };

            ActiveChips.Add(new FilterChipViewModel(
                FilterChipType.DateRange,
                $"📅 {dateLabel}",
                SelectedDateRange.ToString()));
        }

        // LOGIC: Notify computed properties.
        OnPropertyChanged(nameof(HasActiveFilters));
        OnPropertyChanged(nameof(CurrentFilter));

        _logger.LogDebug("Updated active chips: {ChipCount} active filters", ActiveChips.Count);
    }

    /// <summary>
    /// Gets all selected folder paths as glob patterns.
    /// </summary>
    /// <returns>The glob patterns for selected folders.</returns>
    private IEnumerable<string> GetSelectedPaths()
    {
        return GetAllFolderNodes()
            .Where(f => f.IsSelected && f.Children.Count == 0)
            .Select(f => f.GetGlobPattern());
    }

    /// <summary>
    /// Gets all selected extension names.
    /// </summary>
    /// <returns>The selected extension names (without dots).</returns>
    private IEnumerable<string> GetSelectedExtensions()
    {
        return ExtensionToggles
            .Where(t => t.IsSelected)
            .Select(t => t.Extension);
    }

    /// <summary>
    /// Gets all folder nodes in the tree (flattened).
    /// </summary>
    /// <returns>All folder nodes including nested children.</returns>
    private IEnumerable<FolderNodeViewModel> GetAllFolderNodes()
    {
        IEnumerable<FolderNodeViewModel> Flatten(IEnumerable<FolderNodeViewModel> nodes)
        {
            foreach (var node in nodes)
            {
                yield return node;
                foreach (var child in Flatten(node.Children))
                {
                    yield return child;
                }
            }
        }

        return Flatten(FolderTree);
    }

    /// <summary>
    /// Builds the folder tree recursively from a root path.
    /// </summary>
    /// <param name="path">The path to build the tree from.</param>
    /// <param name="maxDepth">Maximum depth to traverse.</param>
    /// <param name="currentDepth">Current traversal depth.</param>
    /// <returns>The folder node, or null if the path doesn't exist.</returns>
    private async Task<FolderNodeViewModel?> BuildFolderTreeAsync(string path, int maxDepth, int currentDepth = 0)
    {
        // LOGIC: Run on background thread to avoid blocking UI.
        return await Task.Run(() =>
        {
            if (!Directory.Exists(path))
            {
                return null;
            }

            var name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(name))
            {
                name = path; // Root path
            }

            var node = new FolderNodeViewModel(name, path)
            {
                IsExpanded = currentDepth < 2 // Expand first two levels
            };

            // LOGIC: Stop recursion at max depth.
            if (currentDepth >= maxDepth)
            {
                return node;
            }

            try
            {
                // LOGIC: Get subdirectories, excluding hidden folders.
                var subdirectories = Directory.GetDirectories(path)
                    .Where(d => !System.IO.Path.GetFileName(d).StartsWith('.'))
                    .OrderBy(d => System.IO.Path.GetFileName(d), StringComparer.OrdinalIgnoreCase);

                foreach (var subdir in subdirectories)
                {
                    var childNode = BuildFolderTreeSync(subdir, maxDepth, currentDepth + 1);
                    if (childNode is not null)
                    {
                        node.Children.Add(childNode);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                // LOGIC: Skip folders we don't have permission to access.
                _logger.LogDebug("Access denied to folder: {Path}", path);
            }
            catch (IOException ex)
            {
                _logger.LogDebug(ex, "IO error accessing folder: {Path}", path);
            }

            return node;
        });
    }

    /// <summary>
    /// Synchronous helper for building folder tree (called from async method).
    /// </summary>
    private FolderNodeViewModel? BuildFolderTreeSync(string path, int maxDepth, int currentDepth)
    {
        if (!Directory.Exists(path))
        {
            return null;
        }

        var name = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(name))
        {
            name = path;
        }

        var node = new FolderNodeViewModel(name, path)
        {
            IsExpanded = currentDepth < 2
        };

        if (currentDepth >= maxDepth)
        {
            return node;
        }

        try
        {
            var subdirectories = Directory.GetDirectories(path)
                .Where(d => !System.IO.Path.GetFileName(d).StartsWith('.'))
                .OrderBy(d => System.IO.Path.GetFileName(d), StringComparer.OrdinalIgnoreCase);

            foreach (var subdir in subdirectories)
            {
                var childNode = BuildFolderTreeSync(subdir, maxDepth, currentDepth + 1);
                if (childNode is not null)
                {
                    node.Children.Add(childNode);
                }
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Skip inaccessible folders
        }
        catch (IOException)
        {
            // Skip folders with IO errors
        }

        return node;
    }

    /// <summary>
    /// Checks if a folder path matches a glob pattern.
    /// </summary>
    /// <param name="folderPath">The folder path to check.</param>
    /// <param name="pattern">The glob pattern to match against.</param>
    /// <returns><c>true</c> if the path matches; otherwise, <c>false</c>.</returns>
    private static bool MatchesPattern(string folderPath, string pattern)
    {
        // LOGIC: Simple pattern matching for preset application.
        // Remove trailing wildcards for comparison.
        var normalizedPattern = pattern.TrimEnd('*', '/');
        return folderPath.StartsWith(normalizedPattern, StringComparison.OrdinalIgnoreCase) ||
               normalizedPattern.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase);
    }
}
