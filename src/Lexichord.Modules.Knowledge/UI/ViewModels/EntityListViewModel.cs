// =============================================================================
// File: EntityListViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main view model for the Entity List View.
// =============================================================================
// LOGIC: Manages loading, filtering, sorting, and selection of entities for
//   the Entity Browser. Uses computed property for filtered view (Avalonia pattern).
//
// v0.4.7e: Entity List View (Knowledge Graph Browser)
// Dependencies: IGraphRepository (v0.4.7e), ISchemaRegistry (v0.4.5f),
//               ILogger<T> (v0.0.3b), CommunityToolkit.Mvvm
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model for the Entity List View in the Knowledge Graph Browser.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityListViewModel"/> orchestrates entity loading, filtering,
/// sorting, and selection for the Entity Browser UI. It uses a computed property
/// for filtered results, following Avalonia's MVVM patterns.
/// </para>
/// <para>
/// <b>Filtering:</b> Supports four filter dimensions that combine with AND logic:
/// <list type="bullet">
///   <item><see cref="SearchText"/>: Text search on name and type.</item>
///   <item><see cref="TypeFilter"/>: Entity type (e.g., "Endpoint", "Parameter").</item>
///   <item><see cref="MinConfidenceFilter"/>: Minimum confidence threshold (0.0–1.0).</item>
///   <item><see cref="DocumentFilter"/>: Source document GUID filter.</item>
/// </list>
/// </para>
/// <para>
/// <b>Virtual Scrolling:</b> The view uses virtualized panels for efficient
/// rendering of large entity collections (10,000+).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7e as part of the Entity List View.
/// </para>
/// </remarks>
public partial class EntityListViewModel : ObservableObject
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILogger<EntityListViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityListViewModel"/> class.
    /// </summary>
    /// <param name="graphRepository">Repository for entity queries.</param>
    /// <param name="schemaRegistry">Registry for entity type metadata.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public EntityListViewModel(
        IGraphRepository graphRepository,
        ISchemaRegistry schemaRegistry,
        ILogger<EntityListViewModel> logger)
    {
        _graphRepository = graphRepository ?? throw new ArgumentNullException(nameof(graphRepository));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region Properties

    /// <summary>
    /// Gets the collection of all loaded entities.
    /// </summary>
    public ObservableCollection<EntityListItemViewModel> Entities { get; } = new();

    /// <summary>
    /// Gets the filtered view of entities based on current filter settings.
    /// </summary>
    public IReadOnlyList<EntityListItemViewModel> FilteredEntities =>
        Entities.Where(ApplyFilters).ToList();

    /// <summary>
    /// Gets or sets the text search filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Filters on entity name and type (case-insensitive contains match).
    /// </remarks>
    [ObservableProperty]
    private string? _searchText;

    /// <summary>
    /// Gets or sets the entity type filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Exact type match. Null or empty means no type filter.
    /// </remarks>
    [ObservableProperty]
    private string? _typeFilter;

    /// <summary>
    /// Gets or sets the minimum confidence threshold filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Filters entities with Confidence &gt;= this value.
    /// Default is 0.0 (include all confidence levels).
    /// </remarks>
    [ObservableProperty]
    private float _minConfidenceFilter = 0.0f;

    /// <summary>
    /// Gets or sets the source document filter.
    /// </summary>
    /// <remarks>
    /// LOGIC: Filters entities that have this document GUID in their SourceDocuments list.
    /// Null means no document filter.
    /// </remarks>
    [ObservableProperty]
    private Guid? _documentFilter;

    /// <summary>
    /// Gets or sets the currently selected entity.
    /// </summary>
    [ObservableProperty]
    private EntityListItemViewModel? _selectedEntity;

    /// <summary>
    /// Gets or sets the loading state.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets the distinct entity types for the filter dropdown.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<string> _availableTypes = Array.Empty<string>();

    /// <summary>
    /// Gets the total count of loaded entities.
    /// </summary>
    public int TotalCount => Entities.Count;

    /// <summary>
    /// Gets the count of filtered entities.
    /// </summary>
    public int FilteredCount => FilteredEntities.Count;

    #endregion

    #region Commands

    /// <summary>
    /// Loads all entities from the knowledge graph.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task LoadEntitiesAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Loading entities from knowledge graph");
        IsLoading = true;

        try
        {
            Entities.Clear();

            var entities = await _graphRepository.GetAllEntitiesAsync(ct);
            _logger.LogDebug("Loaded {Count} entities", entities.Count);

            // LOGIC: Create view models for each entity, enriching with counts and schema metadata.
            // This is done sequentially to maintain ordering; for large datasets,
            // consider batching the count queries.
            var viewModels = new List<EntityListItemViewModel>(entities.Count);

            foreach (var entity in entities)
            {
                var typeSchema = _schemaRegistry.GetEntityType(entity.Type);
                var relationshipCount = await _graphRepository.GetRelationshipCountAsync(entity.Id, ct);
                var mentionCount = await _graphRepository.GetMentionCountAsync(entity.Id, ct);

                viewModels.Add(new EntityListItemViewModel(
                    entity,
                    typeSchema,
                    relationshipCount,
                    mentionCount));
            }

            foreach (var vm in viewModels)
            {
                Entities.Add(vm);
            }

            // LOGIC: Extract distinct types for filter dropdown.
            AvailableTypes = entities
                .Select(e => e.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredEntities));
            OnPropertyChanged(nameof(FilteredCount));

            _logger.LogInformation(
                "Loaded {Count} entities with {TypeCount} distinct types",
                entities.Count,
                AvailableTypes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load entities: {Message}", ex.Message);
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
    [RelayCommand]
    public void ClearFilters()
    {
        _logger.LogDebug("Clearing entity filters");

        SearchText = null;
        TypeFilter = null;
        MinConfidenceFilter = 0.0f;
        DocumentFilter = null;

        OnPropertyChanged(nameof(FilteredEntities));
        OnPropertyChanged(nameof(FilteredCount));
    }

    #endregion

    #region Filter Logic

    /// <summary>
    /// Applies all active filters to determine if an entity should be visible.
    /// </summary>
    /// <param name="item">The entity view model to filter.</param>
    /// <returns><c>true</c> if the entity passes all filters; otherwise <c>false</c>.</returns>
    private bool ApplyFilters(EntityListItemViewModel item)
    {
        // LOGIC: Type filter (exact match)
        if (!string.IsNullOrEmpty(TypeFilter) && item.Type != TypeFilter)
            return false;

        // LOGIC: Confidence filter (>= threshold)
        if (item.Confidence < MinConfidenceFilter)
            return false;

        // LOGIC: Document filter (entity must have document in SourceDocuments)
        if (DocumentFilter.HasValue &&
            !item.Entity.SourceDocuments.Contains(DocumentFilter.Value))
            return false;

        // LOGIC: Search text (case-insensitive contains on name and type)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLowerInvariant();
            if (!item.Name.ToLowerInvariant().Contains(search) &&
                !item.Type.ToLowerInvariant().Contains(search))
                return false;
        }

        return true;
    }

    /// <summary>
    /// Called when a filter property changes; refreshes the filtered view.
    /// </summary>
    partial void OnSearchTextChanged(string? value)
    {
        OnPropertyChanged(nameof(FilteredEntities));
        OnPropertyChanged(nameof(FilteredCount));
    }

    partial void OnTypeFilterChanged(string? value)
    {
        OnPropertyChanged(nameof(FilteredEntities));
        OnPropertyChanged(nameof(FilteredCount));
    }

    partial void OnMinConfidenceFilterChanged(float value)
    {
        OnPropertyChanged(nameof(FilteredEntities));
        OnPropertyChanged(nameof(FilteredCount));
    }

    partial void OnDocumentFilterChanged(Guid? value)
    {
        OnPropertyChanged(nameof(FilteredEntities));
        OnPropertyChanged(nameof(FilteredCount));
    }

    #endregion
}
