// =============================================================================
// File: AxiomViewerViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main view model for the Axiom Viewer panel.
// =============================================================================
// LOGIC: Orchestrates axiom loading, filtering, and grouping for display.
//   Supports multiple filter dimensions (search, type, severity, category)
//   and grouping modes (by TargetType, Category, or Severity).
//
// v0.4.7i: Axiom Viewer (CKVS Phase 1c)
// Dependencies: IAxiomStore (v0.4.6h), ISchemaRegistry (v0.4.5f),
//               AxiomItemViewModel (v0.4.7i)
// =============================================================================

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// Grouping modes for organizing axioms in the viewer.
/// </summary>
public enum AxiomGroupByMode
{
    /// <summary>
    /// Group by the target entity/relationship type.
    /// </summary>
    TargetType,

    /// <summary>
    /// Group by the axiom category.
    /// </summary>
    Category,

    /// <summary>
    /// Group by the severity level.
    /// </summary>
    Severity
}

/// <summary>
/// View model for the Axiom Viewer panel in the Knowledge Graph Browser.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="AxiomViewerViewModel"/> provides a read-only view of all axioms
/// loaded in the system. It supports filtering by search text, target type,
/// severity, and category, as well as grouping by target type, category, or severity.
/// </para>
/// <para>
/// <b>Filtering:</b> Supports four filter dimensions that combine with AND logic:
/// <list type="bullet">
///   <item><see cref="SearchText"/>: Text search on Id, Name, and Description.</item>
///   <item><see cref="TypeFilter"/>: Filter by target type (e.g., "Endpoint").</item>
///   <item><see cref="SeverityFilter"/>: Filter by severity level.</item>
///   <item><see cref="CategoryFilter"/>: Filter by category.</item>
///   <item><see cref="ShowDisabled"/>: Include/exclude disabled axioms.</item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirements:</b> WriterPro+ required for read-only access.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7i as part of the Axiom Viewer.
/// </para>
/// </remarks>
public class AxiomViewerViewModel : INotifyPropertyChanged
{
    private readonly IAxiomStore _axiomStore;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILogger<AxiomViewerViewModel> _logger;

    private bool _isLoading;
    private string? _searchText;
    private string? _typeFilter;
    private AxiomSeverity? _severityFilter;
    private string? _categoryFilter;
    private bool _showDisabled = true;
    private AxiomGroupByMode _groupByMode = AxiomGroupByMode.TargetType;

    /// <summary>
    /// Initializes a new instance of the <see cref="AxiomViewerViewModel"/> class.
    /// </summary>
    /// <param name="axiomStore">The axiom store for retrieving axioms.</param>
    /// <param name="schemaRegistry">The schema registry for type metadata.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public AxiomViewerViewModel(
        IAxiomStore axiomStore,
        ISchemaRegistry schemaRegistry,
        ILogger<AxiomViewerViewModel> logger)
    {
        _axiomStore = axiomStore ?? throw new ArgumentNullException(nameof(axiomStore));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Axioms = new ObservableCollection<AxiomItemViewModel>();
        FilteredAxioms = new ObservableCollection<AxiomItemViewModel>();
        AvailableTypes = new ObservableCollection<string>();
        AvailableCategories = new ObservableCollection<string>();

        _logger.LogDebug("AxiomViewerViewModel initialized");
    }

    #region Observable Collections

    /// <summary>
    /// Gets all loaded axiom view models (unfiltered).
    /// </summary>
    public ObservableCollection<AxiomItemViewModel> Axioms { get; }

    /// <summary>
    /// Gets the filtered axiom view models based on current filter settings.
    /// </summary>
    public ObservableCollection<AxiomItemViewModel> FilteredAxioms { get; }

    /// <summary>
    /// Gets distinct target types for the type filter dropdown.
    /// </summary>
    public ObservableCollection<string> AvailableTypes { get; }

    /// <summary>
    /// Gets distinct categories for the category filter dropdown.
    /// </summary>
    public ObservableCollection<string> AvailableCategories { get; }

    #endregion

    #region Loading State

    /// <summary>
    /// Gets a value indicating whether axioms are being loaded.
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Gets the total number of loaded axioms.
    /// </summary>
    public int TotalCount => Axioms.Count;

    /// <summary>
    /// Gets the number of axioms matching current filters.
    /// </summary>
    public int FilteredCount => FilteredAxioms.Count;

    #endregion

    #region Filter Properties

    /// <summary>
    /// Gets or sets the search text for filtering axioms.
    /// </summary>
    /// <remarks>
    /// LOGIC: Searches across Id, Name, and Description fields (case-insensitive).
    /// </remarks>
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnSearchTextChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the target type filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Null means no filter (show all types).
    /// </remarks>
    public string? TypeFilter
    {
        get => _typeFilter;
        set
        {
            if (SetProperty(ref _typeFilter, value))
            {
                OnTypeFilterChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the severity filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Null means no filter (show all severities).
    /// </remarks>
    public AxiomSeverity? SeverityFilter
    {
        get => _severityFilter;
        set
        {
            if (SetProperty(ref _severityFilter, value))
            {
                OnSeverityFilterChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the category filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Null means no filter (show all categories).
    /// </remarks>
    public string? CategoryFilter
    {
        get => _categoryFilter;
        set
        {
            if (SetProperty(ref _categoryFilter, value))
            {
                OnCategoryFilterChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets whether to show disabled axioms.
    /// </summary>
    /// <value>Defaults to <c>true</c>.</value>
    public bool ShowDisabled
    {
        get => _showDisabled;
        set
        {
            if (SetProperty(ref _showDisabled, value))
            {
                OnShowDisabledChanged();
            }
        }
    }

    /// <summary>
    /// Gets or sets the grouping mode for the axiom display.
    /// </summary>
    public AxiomGroupByMode GroupByMode
    {
        get => _groupByMode;
        set
        {
            if (SetProperty(ref _groupByMode, value))
            {
                OnGroupByModeChanged();
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads all axioms from the axiom store.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// LOGIC:
    /// 1. Set IsLoading to true.
    /// 2. Clear existing collections.
    /// 3. Retrieve all axioms from IAxiomStore.GetAllAxioms().
    /// 4. Wrap each in AxiomItemViewModel.
    /// 5. Populate AvailableTypes and AvailableCategories.
    /// 6. Apply filters.
    /// 7. Set IsLoading to false.
    /// </remarks>
    public async Task LoadAxiomsAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Loading axioms for Axiom Viewer...");
        IsLoading = true;

        try
        {
            // LOGIC: Yield to allow UI to update before heavy work.
            await Task.Yield();

            Axioms.Clear();
            FilteredAxioms.Clear();
            AvailableTypes.Clear();
            AvailableCategories.Clear();

            // LOGIC: Retrieve all axioms (includes enabled and disabled).
            var axioms = _axiomStore.GetAllAxioms();
            _logger.LogDebug("Retrieved {Count} axioms from store", axioms.Count);

            // LOGIC: Collect distinct types and categories.
            var types = new HashSet<string>();
            var categories = new HashSet<string>();

            foreach (var axiom in axioms)
            {
                ct.ThrowIfCancellationRequested();

                var vm = new AxiomItemViewModel(axiom);
                Axioms.Add(vm);

                types.Add(axiom.TargetType);
                if (!string.IsNullOrEmpty(axiom.Category))
                {
                    categories.Add(axiom.Category);
                }
            }

            // LOGIC: Populate filter dropdowns.
            foreach (var type in types.OrderBy(t => t))
            {
                AvailableTypes.Add(type);
            }
            foreach (var category in categories.OrderBy(c => c))
            {
                AvailableCategories.Add(category);
            }

            // LOGIC: Apply current filters to populate FilteredAxioms.
            RefreshFilteredAxioms();

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));

            _logger.LogInformation(
                "Loaded {Total} axioms ({Filtered} after filters)",
                TotalCount, FilteredCount);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Axiom loading cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load axioms");
            throw;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Clears all filter properties to their default values.
    /// </summary>
    public void ClearFilters()
    {
        _logger.LogDebug("Clearing all filters");

        _searchText = null;
        _typeFilter = null;
        _severityFilter = null;
        _categoryFilter = null;
        _showDisabled = true;

        OnPropertyChanged(nameof(SearchText));
        OnPropertyChanged(nameof(TypeFilter));
        OnPropertyChanged(nameof(SeverityFilter));
        OnPropertyChanged(nameof(CategoryFilter));
        OnPropertyChanged(nameof(ShowDisabled));

        RefreshFilteredAxioms();
    }

    #endregion

    #region Filter Application

    /// <summary>
    /// Applies all active filters to determine if an axiom should be visible.
    /// </summary>
    /// <param name="item">The axiom view model to filter.</param>
    /// <returns><c>true</c> if the axiom passes all filters; otherwise <c>false</c>.</returns>
    public bool ApplyFilters(AxiomItemViewModel item)
    {
        // LOGIC: Filter by enabled/disabled status.
        if (!ShowDisabled && !item.IsEnabled)
        {
            return false;
        }

        // LOGIC: Filter by target type.
        if (!string.IsNullOrEmpty(TypeFilter) &&
            !string.Equals(item.TargetType, TypeFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // LOGIC: Filter by severity.
        if (SeverityFilter.HasValue && item.Severity != SeverityFilter.Value)
        {
            return false;
        }

        // LOGIC: Filter by category.
        if (!string.IsNullOrEmpty(CategoryFilter) &&
            !string.Equals(item.Category, CategoryFilter, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // LOGIC: Filter by search text (Id, Name, Description).
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchLower = SearchText.Trim().ToLowerInvariant();

            var idMatch = item.Id.ToLowerInvariant().Contains(searchLower);
            var nameMatch = item.Name.ToLowerInvariant().Contains(searchLower);
            var descMatch = item.Description?.ToLowerInvariant().Contains(searchLower) ?? false;

            if (!idMatch && !nameMatch && !descMatch)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Refreshes the FilteredAxioms collection based on current filter settings.
    /// </summary>
    private void RefreshFilteredAxioms()
    {
        FilteredAxioms.Clear();

        foreach (var axiom in Axioms)
        {
            if (ApplyFilters(axiom))
            {
                FilteredAxioms.Add(axiom);
            }
        }

        OnPropertyChanged(nameof(FilteredCount));
        _logger.LogDebug("Filtered to {Count} axioms", FilteredCount);
    }

    #endregion

    #region Filter Change Handlers

    private void OnSearchTextChanged()
    {
        _logger.LogDebug("SearchText changed to: {SearchText}", SearchText);
        RefreshFilteredAxioms();
    }

    private void OnTypeFilterChanged()
    {
        _logger.LogDebug("TypeFilter changed to: {TypeFilter}", TypeFilter);
        RefreshFilteredAxioms();
    }

    private void OnSeverityFilterChanged()
    {
        _logger.LogDebug("SeverityFilter changed to: {SeverityFilter}", SeverityFilter);
        RefreshFilteredAxioms();
    }

    private void OnCategoryFilterChanged()
    {
        _logger.LogDebug("CategoryFilter changed to: {CategoryFilter}", CategoryFilter);
        RefreshFilteredAxioms();
    }

    private void OnShowDisabledChanged()
    {
        _logger.LogDebug("ShowDisabled changed to: {ShowDisabled}", ShowDisabled);
        RefreshFilteredAxioms();
    }

    private void OnGroupByModeChanged()
    {
        _logger.LogDebug("GroupByMode changed to: {GroupByMode}", GroupByMode);
        // LOGIC: UI binding handles grouping via CollectionViewSource.
        OnPropertyChanged(nameof(GroupByMode));
    }

    #endregion

    #region INotifyPropertyChanged Implementation

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event.
    /// </summary>
    /// <param name="propertyName">Name of the changed property.</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">The property type.</typeparam>
    /// <param name="field">Reference to the backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="propertyName">Name of the property (auto-populated).</param>
    /// <returns><c>true</c> if the value changed; otherwise <c>false</c>.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    #endregion
}
