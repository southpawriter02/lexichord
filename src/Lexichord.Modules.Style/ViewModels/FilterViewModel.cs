using System.Reactive.Linq;
using System.Reactive.Subjects;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for the filter bar UI controlling terminology grid filtering.
/// </summary>
/// <remarks>
/// LOGIC: Manages filter state with:
/// - Debounced text search (300ms default)
/// - Toggle for showing inactive terms
/// - Dropdown filters for category and severity
/// - Filter state persistence via ISettingsService
///
/// Version: v0.2.5b
/// </remarks>
public partial class FilterViewModel : ObservableObject, IDisposable
{
    private readonly ITermFilterService _filterService;
    private readonly ILogger<FilterViewModel> _logger;
    private readonly int _debounceMs;
    private readonly Subject<string> _searchSubject = new();
    private readonly IDisposable _searchSubscription;
    private bool _isDisposed;

    /// <summary>
    /// Initializes a new instance of the FilterViewModel.
    /// </summary>
    public FilterViewModel(
        ITermFilterService filterService,
        IOptions<FilterOptions> options,
        ILogger<FilterViewModel> logger)
    {
        _filterService = filterService ?? throw new ArgumentNullException(nameof(filterService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var filterOptions = options?.Value ?? new FilterOptions();
        _debounceMs = filterOptions.DebounceMilliseconds;

        // Initialize defaults from options
        ShowInactive = filterOptions.ShowInactive;

        // LOGIC: Set up debounced search - only trigger after user stops typing
        _searchSubscription = _searchSubject
            .Throttle(TimeSpan.FromMilliseconds(_debounceMs))
            .DistinctUntilChanged()
            .Subscribe(OnDebouncedSearchTextChanged);

        _logger.LogDebug("FilterViewModel initialized with debounce={DebounceMs}ms", _debounceMs);
    }

    #region Properties

    /// <summary>
    /// Gets or sets the search text for filtering.
    /// </summary>
    /// <remarks>
    /// LOGIC: Changes are debounced before triggering FilterChanged.
    /// </remarks>
    [ObservableProperty]
    private string _searchText = string.Empty;

    /// <summary>
    /// Gets or sets whether inactive terms are visible.
    /// </summary>
    /// <remarks>
    /// LOGIC: When false (default), only active terms are shown.
    /// </remarks>
    [ObservableProperty]
    private bool _showInactive;

    /// <summary>
    /// Gets or sets the selected category filter, or null for all categories.
    /// </summary>
    [ObservableProperty]
    private string? _selectedCategory;

    /// <summary>
    /// Gets or sets the selected severity filter, or null for all severities.
    /// </summary>
    [ObservableProperty]
    private string? _selectedSeverity;

    /// <summary>
    /// Gets the available categories for the dropdown.
    /// </summary>
    /// <remarks>
    /// LOGIC: Populated from loaded terms. Includes "All" option.
    /// </remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _availableCategories = Array.Empty<string>();

    /// <summary>
    /// Gets the available severities for the dropdown.
    /// </summary>
    /// <remarks>
    /// LOGIC: Populated from loaded terms. Includes "All" option.
    /// </remarks>
    [ObservableProperty]
    private IReadOnlyList<string> _availableSeverities = Array.Empty<string>();

    /// <summary>
    /// Gets whether any filter is currently active.
    /// </summary>
    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(SearchText) ||
        ShowInactive ||
        SelectedCategory is not null ||
        SelectedSeverity is not null;

    #endregion

    #region Events

    /// <summary>
    /// Event raised when any filter value changes.
    /// </summary>
    public event EventHandler? FilterChanged;

    #endregion

    #region Commands

    /// <summary>
    /// Command to clear all filter values to defaults.
    /// </summary>
    [RelayCommand]
    private void ClearFilters()
    {
        _logger.LogDebug("Clearing all filters");

        // LOGIC: Use property setters to clear values
        // This avoids MVVMTK0034 errors about direct field access
        SearchText = string.Empty;
        ShowInactive = false;
        SelectedCategory = null;
        SelectedSeverity = null;

        OnPropertyChanged(nameof(HasActiveFilters));

        // LOGIC: Single filter changed event for bulk update
        RaiseFilterChanged();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the current filter criteria.
    /// </summary>
    public FilterCriteria GetCriteria() => new()
    {
        SearchText = SearchText,
        ShowInactive = ShowInactive,
        CategoryFilter = SelectedCategory,
        SeverityFilter = SelectedSeverity
    };

    /// <summary>
    /// Updates the available filter options from loaded terms.
    /// </summary>
    /// <param name="categories">Distinct category values from terms.</param>
    /// <param name="severities">Distinct severity values from terms.</param>
    public void UpdateAvailableOptions(IEnumerable<string> categories, IEnumerable<string> severities)
    {
        AvailableCategories = categories.OrderBy(c => c, StringComparer.OrdinalIgnoreCase).ToList();
        AvailableSeverities = severities.ToList();

        _logger.LogDebug("Updated available options: {CategoryCount} categories, {SeverityCount} severities",
            AvailableCategories.Count, AvailableSeverities.Count);
    }

    #endregion

    #region Property Change Handlers

    partial void OnSearchTextChanged(string value)
    {
        // LOGIC: Push to debounced stream
        _searchSubject.OnNext(value);
    }

    partial void OnShowInactiveChanged(bool value)
    {
        _logger.LogDebug("ShowInactive changed to {Value}", value);
        OnPropertyChanged(nameof(HasActiveFilters));
        RaiseFilterChanged();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        _logger.LogDebug("SelectedCategory changed to '{Value}'", value ?? "(all)");
        OnPropertyChanged(nameof(HasActiveFilters));
        RaiseFilterChanged();
    }

    partial void OnSelectedSeverityChanged(string? value)
    {
        _logger.LogDebug("SelectedSeverity changed to '{Value}'", value ?? "(all)");
        OnPropertyChanged(nameof(HasActiveFilters));
        RaiseFilterChanged();
    }

    private void OnDebouncedSearchTextChanged(string value)
    {
        _logger.LogDebug("Debounced search text: '{Value}'", value);
        OnPropertyChanged(nameof(HasActiveFilters));
        RaiseFilterChanged();
    }

    private void RaiseFilterChanged()
    {
        FilterChanged?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region IDisposable

    /// <summary>
    /// Disposes the ViewModel.
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _searchSubscription.Dispose();
        _searchSubject.Dispose();

        GC.SuppressFinalize(this);
    }

    #endregion
}
