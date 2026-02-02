// =============================================================================
// File: RelationshipViewerPanelViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main ViewModel for the Relationship Viewer panel.
// =============================================================================
// LOGIC: Orchestrates the relationship tree visualization including loading,
//   filtering by direction and type, and tree structure building. Uses a
//   hierarchical tree structure grouped by direction and relationship type.
//
// v0.4.7h: Relationship Viewer (Knowledge Graph Browser)
// Dependencies: IGraphRepository (v0.4.7f), ILogger<T> (v0.0.3b),
//               CommunityToolkit.Mvvm
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model for the Relationship Viewer panel in the Entity Browser.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelationshipViewerPanelViewModel"/> manages the hierarchical
/// tree display of entity relationships. It supports filtering by direction
/// (incoming/outgoing/both) and relationship type.
/// </para>
/// <para>
/// <b>Tree Structure:</b> Relationships are organized in a three-level hierarchy:
/// <list type="number">
///   <item>Direction headers (Incoming ← / Outgoing →)</item>
///   <item>Relationship type groups (e.g., "CONTAINS (3)")</item>
///   <item>Entity leaf nodes (e.g., "Endpoint: GET /api/users")</item>
/// </list>
/// </para>
/// <para>
/// <b>Performance:</b> The tree uses virtualization for efficient rendering
/// of large relationship sets (100+ relationships).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7h as part of the Relationship Viewer.
/// </para>
/// </remarks>
public partial class RelationshipViewerPanelViewModel : ObservableObject
{
    private readonly IGraphRepository _graphRepository;
    private readonly ILogger<RelationshipViewerPanelViewModel> _logger;

    private Guid? _currentEntityId;
    private IReadOnlyList<KnowledgeRelationship> _allRelationships = Array.Empty<KnowledgeRelationship>();
    private Dictionary<Guid, KnowledgeEntity> _entityCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="RelationshipViewerPanelViewModel"/> class.
    /// </summary>
    /// <param name="graphRepository">Repository for relationship queries.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public RelationshipViewerPanelViewModel(
        IGraphRepository graphRepository,
        ILogger<RelationshipViewerPanelViewModel> logger)
    {
        _graphRepository = graphRepository ?? throw new ArgumentNullException(nameof(graphRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("RelationshipViewerPanelViewModel initialized");
    }

    #region Properties

    /// <summary>
    /// Gets the root nodes of the relationship tree.
    /// </summary>
    /// <value>
    /// The top-level tree nodes (direction headers when showing both,
    /// or type groups when direction is filtered).
    /// </value>
    public ObservableCollection<RelationshipTreeNodeViewModel> RootNodes { get; } = new();

    /// <summary>
    /// Gets or sets the direction filter.
    /// </summary>
    /// <value>
    /// <see cref="RelationshipDirection.Both"/> (default),
    /// <see cref="RelationshipDirection.Incoming"/>, or
    /// <see cref="RelationshipDirection.Outgoing"/>.
    /// </value>
    [ObservableProperty]
    private RelationshipDirection _directionFilter = RelationshipDirection.Both;

    /// <summary>
    /// Gets or sets the relationship type filter.
    /// </summary>
    /// <value>
    /// A specific relationship type to filter by, or <c>null</c> for all types.
    /// </value>
    [ObservableProperty]
    private string? _typeFilter;

    /// <summary>
    /// Gets or sets the loading state.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Gets the distinct relationship types for the filter dropdown.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<string> _availableTypes = Array.Empty<string>();

    /// <summary>
    /// Gets the total count of relationships (unfiltered).
    /// </summary>
    public int TotalCount => _allRelationships.Count;

    /// <summary>
    /// Gets the count of filtered relationships.
    /// </summary>
    public int FilteredCount => CountFilteredRelationships();

    #endregion

    #region Commands

    /// <summary>
    /// Loads relationships for the specified entity.
    /// </summary>
    /// <param name="entityId">The entity ID to load relationships for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    [RelayCommand]
    public async Task LoadRelationshipsAsync(Guid entityId, CancellationToken ct = default)
    {
        _logger.LogDebug("Loading relationships for entity {EntityId}", entityId);
        IsLoading = true;
        _currentEntityId = entityId;

        try
        {
            // LOGIC: Load all relationships for the entity
            _allRelationships = await _graphRepository.GetRelationshipsForEntityAsync(entityId, ct);
            _logger.LogDebug("Loaded {Count} relationships", _allRelationships.Count);

            // LOGIC: Cache related entities for display (batch load for efficiency)
            await LoadRelatedEntitiesAsync(ct);

            // LOGIC: Extract distinct types for filter dropdown
            AvailableTypes = _allRelationships
                .Select(r => r.Type)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            // LOGIC: Build the tree structure
            BuildTree();

            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(FilteredCount));

            _logger.LogInformation(
                "Loaded {Count} relationships with {TypeCount} distinct types for entity {EntityId}",
                _allRelationships.Count,
                AvailableTypes.Count,
                entityId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load relationships for entity {EntityId}: {Message}",
                entityId, ex.Message);
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
        _logger.LogDebug("Clearing relationship filters");

        DirectionFilter = RelationshipDirection.Both;
        TypeFilter = null;

        // Rebuild tree will be triggered by property change handlers
    }

    #endregion

    #region Filter Change Handlers

    partial void OnDirectionFilterChanged(RelationshipDirection value)
    {
        _logger.LogDebug("Direction filter changed to {Direction}", value);
        BuildTree();
        OnPropertyChanged(nameof(FilteredCount));
    }

    partial void OnTypeFilterChanged(string? value)
    {
        _logger.LogDebug("Type filter changed to {Type}", value ?? "(none)");
        BuildTree();
        OnPropertyChanged(nameof(FilteredCount));
    }

    #endregion

    #region Tree Building

    /// <summary>
    /// Builds the hierarchical tree structure from loaded relationships.
    /// </summary>
    private void BuildTree()
    {
        if (_currentEntityId is null)
        {
            _logger.LogTrace("No entity selected, skipping tree build");
            return;
        }

        _logger.LogDebug("Building relationship tree with filters: Direction={Direction}, Type={Type}",
            DirectionFilter, TypeFilter ?? "(all)");

        RootNodes.Clear();

        var entityId = _currentEntityId.Value;
        var filteredRelationships = ApplyFilters(_allRelationships, entityId);

        if (!filteredRelationships.Any())
        {
            _logger.LogDebug("No relationships match current filters");
            return;
        }

        // LOGIC: Group by direction first, then by type
        if (DirectionFilter == RelationshipDirection.Both)
        {
            // Show both directions with headers
            var incomingHeader = RelationshipTreeNodeViewModel.CreateDirectionHeader(RelationshipDirection.Incoming);
            var outgoingHeader = RelationshipTreeNodeViewModel.CreateDirectionHeader(RelationshipDirection.Outgoing);

            var incoming = filteredRelationships.Where(r => r.ToEntityId == entityId).ToList();
            var outgoing = filteredRelationships.Where(r => r.FromEntityId == entityId).ToList();

            if (incoming.Any())
            {
                AddTypeGroupsToNode(incomingHeader, incoming, entityId, RelationshipDirection.Incoming);
                RootNodes.Add(incomingHeader);
            }

            if (outgoing.Any())
            {
                AddTypeGroupsToNode(outgoingHeader, outgoing, entityId, RelationshipDirection.Outgoing);
                RootNodes.Add(outgoingHeader);
            }
        }
        else
        {
            // Single direction - show type groups directly at root
            var direction = DirectionFilter;
            AddTypeGroupsToNode(null, filteredRelationships.ToList(), entityId, direction);
        }

        _logger.LogDebug("Built tree with {RootCount} root nodes", RootNodes.Count);
    }

    /// <summary>
    /// Adds type group nodes to a parent (or root if parent is null).
    /// </summary>
    private void AddTypeGroupsToNode(
        RelationshipTreeNodeViewModel? parent,
        List<KnowledgeRelationship> relationships,
        Guid currentEntityId,
        RelationshipDirection direction)
    {
        var typeGroups = relationships
            .GroupBy(r => r.Type)
            .OrderBy(g => g.Key);

        foreach (var group in typeGroups)
        {
            var groupNode = RelationshipTreeNodeViewModel.CreateGroupNode(
                group.Key,
                direction,
                group.Count());

            foreach (var relationship in group)
            {
                var otherEntityId = direction == RelationshipDirection.Incoming
                    ? relationship.FromEntityId
                    : relationship.ToEntityId;

                var otherEntity = _entityCache.GetValueOrDefault(otherEntityId);
                var entityName = otherEntity?.Name ?? "(unknown)";
                var entityType = otherEntity?.Type ?? "Entity";

                var leafNode = RelationshipTreeNodeViewModel.CreateLeafNode(
                    otherEntityId,
                    entityName,
                    entityType,
                    group.Key,
                    direction);

                groupNode.Children.Add(leafNode);
            }

            if (parent is not null)
            {
                parent.Children.Add(groupNode);
            }
            else
            {
                RootNodes.Add(groupNode);
            }
        }
    }

    #endregion

    #region Filter Logic

    /// <summary>
    /// Applies direction and type filters to the relationships.
    /// </summary>
    private IEnumerable<KnowledgeRelationship> ApplyFilters(
        IReadOnlyList<KnowledgeRelationship> relationships,
        Guid entityId)
    {
        IEnumerable<KnowledgeRelationship> filtered = relationships;

        // LOGIC: Apply direction filter
        filtered = DirectionFilter switch
        {
            RelationshipDirection.Incoming => filtered.Where(r => r.ToEntityId == entityId),
            RelationshipDirection.Outgoing => filtered.Where(r => r.FromEntityId == entityId),
            _ => filtered // Both - no direction filter
        };

        // LOGIC: Apply type filter
        if (!string.IsNullOrEmpty(TypeFilter))
        {
            filtered = filtered.Where(r => r.Type == TypeFilter);
        }

        return filtered;
    }

    /// <summary>
    /// Counts the relationships matching current filters.
    /// </summary>
    private int CountFilteredRelationships()
    {
        if (_currentEntityId is null)
            return 0;

        return ApplyFilters(_allRelationships, _currentEntityId.Value).Count();
    }

    #endregion

    #region Entity Loading

    /// <summary>
    /// Loads related entities into the cache for display.
    /// </summary>
    private async Task LoadRelatedEntitiesAsync(CancellationToken ct)
    {
        var relatedEntityIds = _allRelationships
            .SelectMany(r => new[] { r.FromEntityId, r.ToEntityId })
            .Where(id => id != _currentEntityId)
            .Distinct()
            .ToList();

        _logger.LogDebug("Loading {Count} related entities", relatedEntityIds.Count);

        _entityCache.Clear();

        // LOGIC: Load each related entity into cache
        // Future optimization: Add batch GetByIdsAsync method
        foreach (var id in relatedEntityIds)
        {
            try
            {
                var entity = await _graphRepository.GetByIdAsync(id, ct);
                if (entity is not null)
                {
                    _entityCache[id] = entity;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load related entity {EntityId}", id);
            }
        }

        _logger.LogDebug("Cached {Count} related entities", _entityCache.Count);
    }

    #endregion
}
