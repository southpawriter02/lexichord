// =============================================================================
// File: EntityDetailViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main view model for the Entity Detail View.
// =============================================================================
// LOGIC: Manages loading and displaying entity details including properties,
//   relationships, and source documents. Provides navigation to related
//   entities and source documents. Supports license-gated editing.
//
// v0.4.7f: Entity Detail View (Knowledge Graph Browser)
// Dependencies: IGraphRepository (v0.4.7f), ISchemaRegistry (v0.4.5f),
//               IDocumentRepository (v0.4.1c), IEditorService (v0.1.3a),
//               ILicenseContext (v0.0.4c), ILogger<T> (v0.0.3b)
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// ViewModel for the Entity Detail View.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityDetailViewModel"/> displays comprehensive information
/// about a selected <see cref="KnowledgeEntity"/> including all properties,
/// relationships to other entities, and source documents that mention it.
/// </para>
/// <para>
/// <b>Data Loading:</b> When <see cref="Entity"/> is set, the ViewModel
/// asynchronously loads all related data (properties, relationships, documents).
/// The <see cref="IsLoading"/> property indicates loading state for UI feedback.
/// </para>
/// <para>
/// <b>Navigation:</b> Users can navigate to related entities via
/// <see cref="NavigateToRelatedEntityCommand"/> or open source documents in the
/// editor via <see cref="NavigateToSourceCommand"/>.
/// </para>
/// <para>
/// <b>License Gating:</b> Edit operations (Edit, Merge, Delete) require Teams
/// tier or higher. The <see cref="CanEdit"/> property indicates permission.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
/// </para>
/// </remarks>
public partial class EntityDetailViewModel : ObservableObject
{
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly IDocumentRepository _documentRepository;
    private readonly IEditorService _editorService;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<EntityDetailViewModel> _logger;

    #region Observable Properties

    /// <summary>
    /// The currently displayed entity.
    /// </summary>
    [ObservableProperty]
    private KnowledgeEntity? _entity;

    /// <summary>
    /// The entity's display name.
    /// </summary>
    [ObservableProperty]
    private string _name = "";

    /// <summary>
    /// The entity's type name.
    /// </summary>
    [ObservableProperty]
    private string _type = "";

    /// <summary>
    /// The entity type's icon.
    /// </summary>
    [ObservableProperty]
    private string _icon = "📦";

    /// <summary>
    /// The entity's extraction confidence (0.0 to 1.0).
    /// </summary>
    [ObservableProperty]
    private float _confidence;

    /// <summary>
    /// Indicates whether data is currently being loaded.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>
    /// Indicates whether the user has edit permissions.
    /// </summary>
    [ObservableProperty]
    private bool _canEdit;

    #endregion

    #region Collections

    /// <summary>
    /// Gets the entity's properties as view models.
    /// </summary>
    public ObservableCollection<PropertyItemViewModel> Properties { get; } = new();

    /// <summary>
    /// Gets the entity's relationships as view models.
    /// </summary>
    public ObservableCollection<RelationshipItemViewModel> Relationships { get; } = new();

    /// <summary>
    /// Gets the entity's source documents as view models.
    /// </summary>
    public ObservableCollection<SourceDocumentItemViewModel> SourceDocuments { get; } = new();

    /// <summary>
    /// Gets the Relationship Viewer panel ViewModel.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Introduced in:</b> v0.4.7h as part of the Relationship Viewer.
    /// </para>
    /// </remarks>
    public RelationshipViewerPanelViewModel? RelationshipViewerPanel { get; private set; }

    #endregion

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityDetailViewModel"/> class.
    /// </summary>
    /// <param name="graphRepository">Repository for entity and relationship queries.</param>
    /// <param name="schemaRegistry">Registry for entity/relationship type metadata.</param>
    /// <param name="documentRepository">Repository for source document lookup.</param>
    /// <param name="editorService">Service for opening documents in the editor.</param>
    /// <param name="licenseContext">Context for checking license tier.</param>
    /// <param name="relationshipViewerPanel">ViewModel for the relationship tree panel (v0.4.7h).</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public EntityDetailViewModel(
        IGraphRepository graphRepository,
        ISchemaRegistry schemaRegistry,
        IDocumentRepository documentRepository,
        IEditorService editorService,
        ILicenseContext licenseContext,
        RelationshipViewerPanelViewModel relationshipViewerPanel,
        ILogger<EntityDetailViewModel> logger)
    {
        _graphRepository = graphRepository ?? throw new ArgumentNullException(nameof(graphRepository));
        _schemaRegistry = schemaRegistry ?? throw new ArgumentNullException(nameof(schemaRegistry));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        RelationshipViewerPanel = relationshipViewerPanel ?? throw new ArgumentNullException(nameof(relationshipViewerPanel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Check license tier for edit permissions.
        // Teams tier and above can edit entities.
        CanEdit = _licenseContext.GetCurrentTier() >= LicenseTier.Teams;

        _logger.LogDebug(
            "EntityDetailViewModel initialized (CanEdit: {CanEdit}, Tier: {Tier})",
            CanEdit,
            _licenseContext.GetCurrentTier());
    }

    /// <summary>
    /// Called when the <see cref="Entity"/> property changes.
    /// </summary>
    /// <param name="value">The new entity value.</param>
    /// <remarks>
    /// LOGIC: Triggers async loading of entity details when a new entity is set.
    /// Clears all details when entity is set to null.
    /// </remarks>
    partial void OnEntityChanged(KnowledgeEntity? value)
    {
        if (value is not null)
        {
            _logger.LogDebug("Entity changed to {EntityId}: {EntityName}", value.Id, value.Name);
            _ = LoadEntityDetailsAsync(value);
            
            // v0.4.7h: Load relationships into tree viewer
            _ = RelationshipViewerPanel?.LoadRelationshipsAsync(value.Id);
        }
        else
        {
            _logger.LogDebug("Entity cleared");
            ClearDetails();
        }
    }

    #region Commands

    /// <summary>
    /// Navigates to a source document in the editor.
    /// </summary>
    /// <param name="source">The source document to open.</param>
    [RelayCommand]
    private async Task NavigateToSourceAsync(SourceDocumentItemViewModel source)
    {
        if (source is null)
        {
            _logger.LogWarning("NavigateToSource called with null source");
            return;
        }

        _logger.LogInformation(
            "Navigating to source document {DocumentId}: {Path}",
            source.DocumentId,
            source.Path);

        try
        {
            await _editorService.OpenDocumentAsync(source.Path);
            // TODO: v0.4.8+ Navigate to first mention of entity in document
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open source document {Path}", source.Path);
        }
    }

    /// <summary>
    /// Navigates to a related entity.
    /// </summary>
    /// <param name="relationship">The relationship containing the target entity ID.</param>
    /// <remarks>
    /// LOGIC: Sets the Entity property to the related entity, triggering
    /// a reload of the detail view with the new entity's data.
    /// </remarks>
    [RelayCommand]
    private async Task NavigateToRelatedEntityAsync(RelationshipItemViewModel relationship)
    {
        if (relationship is null)
        {
            _logger.LogWarning("NavigateToRelatedEntity called with null relationship");
            return;
        }

        _logger.LogInformation(
            "Navigating to related entity {EntityId}: {EntityName}",
            relationship.OtherEntityId,
            relationship.OtherEntityName);

        try
        {
            var entity = await _graphRepository.GetByIdAsync(relationship.OtherEntityId);
            if (entity is not null)
            {
                Entity = entity;
            }
            else
            {
                _logger.LogWarning("Related entity {EntityId} not found", relationship.OtherEntityId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to navigate to related entity {EntityId}", relationship.OtherEntityId);
        }
    }

    /// <summary>
    /// Copies a property value to the clipboard.
    /// </summary>
    /// <param name="property">The property whose value should be copied.</param>
    [RelayCommand]
    private async Task CopyPropertyValueAsync(PropertyItemViewModel property)
    {
        if (property is null)
        {
            _logger.LogWarning("CopyPropertyValue called with null property");
            return;
        }

        _logger.LogDebug("Copying property {PropertyName} value to clipboard", property.Name);

        try
        {
            // LOGIC: Use Avalonia's clipboard API via TopLevel.
            // This is a simplification; production code would inject IClipboard.
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow is not null)
            {
                var clipboard = desktop.MainWindow.Clipboard;
                if (clipboard is not null)
                {
                    await clipboard.SetTextAsync(property.Value);
                    _logger.LogDebug("Copied value to clipboard: {Value}", property.Value);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy property value to clipboard");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Loads all details for the specified entity.
    /// </summary>
    /// <param name="entity">The entity to load details for.</param>
    private async Task LoadEntityDetailsAsync(KnowledgeEntity entity)
    {
        IsLoading = true;
        _logger.LogDebug("Loading details for entity {EntityId}", entity.Id);

        try
        {
            // LOGIC: Set basic info from entity
            Name = entity.Name;
            Type = entity.Type;
            Confidence = GetConfidence(entity);

            // LOGIC: Get type schema for icon and property metadata
            var schema = _schemaRegistry.GetEntityType(entity.Type);
            Icon = schema?.Icon ?? "📦";

            // LOGIC: Load properties
            Properties.Clear();
            foreach (var prop in entity.Properties)
            {
                // Skip the confidence property as it's displayed in the header
                if (prop.Key.Equals("confidence", StringComparison.OrdinalIgnoreCase))
                    continue;

                var propSchema = schema?.Properties?.FirstOrDefault(p =>
                    p.Name.Equals(prop.Key, StringComparison.OrdinalIgnoreCase));

                Properties.Add(new PropertyItemViewModel
                {
                    Name = prop.Key,
                    Value = FormatValue(prop.Value),
                    Type = propSchema?.Type.ToString() ?? "string",
                    Description = propSchema?.Description,
                    IsRequired = propSchema?.Required ?? false
                });
            }

            _logger.LogDebug("Loaded {Count} properties", Properties.Count);

            // LOGIC: Load relationships
            Relationships.Clear();
            var relationships = await _graphRepository.GetRelationshipsForEntityAsync(entity.Id);
            foreach (var rel in relationships)
            {
                var isOutgoing = rel.FromEntityId == entity.Id;
                var otherEntityId = isOutgoing ? rel.ToEntityId : rel.FromEntityId;
                var otherEntity = await _graphRepository.GetByIdAsync(otherEntityId);

                if (otherEntity is not null)
                {
                    Relationships.Add(new RelationshipItemViewModel
                    {
                        Id = rel.Id,
                        Type = rel.Type,
                        Direction = isOutgoing ? "→" : "←",
                        OtherEntityId = otherEntity.Id,
                        OtherEntityName = otherEntity.Name,
                        OtherEntityType = otherEntity.Type,
                        Icon = "↔"
                    });
                }
            }

            _logger.LogDebug("Loaded {Count} relationships", Relationships.Count);

            // LOGIC: Load source documents
            SourceDocuments.Clear();
            foreach (var docId in entity.SourceDocuments)
            {
                var doc = await _documentRepository.GetByIdAsync(docId);
                if (doc is not null)
                {
                    var mentionCount = await _graphRepository.GetMentionCountAsync(entity.Id, docId);
                    SourceDocuments.Add(new SourceDocumentItemViewModel
                    {
                        DocumentId = doc.Id,
                        Title = doc.Title ?? System.IO.Path.GetFileName(doc.FilePath),
                        Path = doc.FilePath,
                        MentionCount = mentionCount
                    });
                }
            }

            _logger.LogDebug("Loaded {Count} source documents", SourceDocuments.Count);

            _logger.LogInformation(
                "Loaded entity details for {EntityId}: {PropertyCount} properties, " +
                "{RelationshipCount} relationships, {DocumentCount} documents",
                entity.Id,
                Properties.Count,
                Relationships.Count,
                SourceDocuments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load entity details for {EntityId}", entity.Id);
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Clears all detail sections.
    /// </summary>
    private void ClearDetails()
    {
        Name = "";
        Type = "";
        Icon = "📦";
        Confidence = 0;
        Properties.Clear();
        Relationships.Clear();
        SourceDocuments.Clear();
    }

    /// <summary>
    /// Extracts the confidence score from an entity.
    /// </summary>
    /// <param name="entity">The entity to extract confidence from.</param>
    /// <returns>The confidence score (0.0 to 1.0), defaulting to 1.0.</returns>
    private static float GetConfidence(KnowledgeEntity entity)
    {
        if (entity.Properties.TryGetValue("confidence", out var value))
        {
            return value switch
            {
                float f => f,
                double d => (float)d,
                int i => i / 100f,
                _ => 1.0f
            };
        }
        return 1.0f;
    }

    /// <summary>
    /// Formats a property value for display.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>A human-readable string representation.</returns>
    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "(null)",
            string s => s,
            IEnumerable<object> list => string.Join(", ", list),
            _ => value.ToString() ?? ""
        };
    }

    #endregion
}
