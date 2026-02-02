// =============================================================================
// File: EntityListItemViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model for a single entity in the Entity List View.
// =============================================================================
// LOGIC: Wraps a KnowledgeEntity with display properties for the Entity Browser
//   list. Retrieves type metadata (icon, color) from ISchemaRegistry for
//   consistent visual presentation across entity types.
//
// v0.4.7e: Entity List View (Knowledge Graph Browser)
// Dependencies: KnowledgeEntity (v0.4.5e), EntityTypeSchema (v0.4.5f)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model representing a single entity in the Entity List View.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityListItemViewModel"/> wraps a <see cref="KnowledgeEntity"/>
/// with display-ready properties for the Entity Browser ListView. It provides
/// computed properties for confidence display, relationship/mention counts,
/// and type-specific visual styling (icon, color).
/// </para>
/// <para>
/// <b>Schema Integration:</b> The <see cref="Icon"/> and <see cref="Color"/>
/// properties are derived from the <see cref="EntityTypeSchema"/> registered
/// in the Schema Registry (v0.4.5f). Unknown types display with sensible defaults.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7e as part of the Entity List View.
/// </para>
/// </remarks>
public sealed class EntityListItemViewModel
{
    private readonly KnowledgeEntity _entity;

    /// <summary>
    /// Initializes a new instance of the <see cref="EntityListItemViewModel"/> class.
    /// </summary>
    /// <param name="entity">The underlying knowledge entity.</param>
    /// <param name="typeSchema">Optional type schema for icon and color. Null if type is unregistered.</param>
    /// <param name="relationshipCount">Pre-fetched relationship count.</param>
    /// <param name="mentionCount">Pre-fetched mention count (source documents).</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="entity"/> is null.
    /// </exception>
    public EntityListItemViewModel(
        KnowledgeEntity entity,
        EntityTypeSchema? typeSchema,
        int relationshipCount,
        int mentionCount)
    {
        _entity = entity ?? throw new ArgumentNullException(nameof(entity));
        RelationshipCount = relationshipCount;
        MentionCount = mentionCount;

        // LOGIC: Extract icon and color from type schema, with fallbacks.
        // Default icon is a generic node symbol; default color is neutral gray.
        Icon = typeSchema?.Icon ?? "⬢";
        Color = typeSchema?.Color ?? "#6b7280";
    }

    /// <summary>
    /// Gets the underlying knowledge entity.
    /// </summary>
    /// <value>The <see cref="KnowledgeEntity"/> this view model wraps.</value>
    public KnowledgeEntity Entity => _entity;

    /// <summary>
    /// Gets the entity's unique identifier.
    /// </summary>
    public Guid Id => _entity.Id;

    /// <summary>
    /// Gets the entity's display name.
    /// </summary>
    public string Name => _entity.Name;

    /// <summary>
    /// Gets the entity type name.
    /// </summary>
    public string Type => _entity.Type;

    /// <summary>
    /// Gets the extraction confidence score (0.0 to 1.0).
    /// </summary>
    /// <remarks>
    /// LOGIC: Confidence is derived from the entity's Properties dictionary
    /// if present, otherwise defaults to 1.0 (fully confident).
    /// </remarks>
    public float Confidence
    {
        get
        {
            if (_entity.Properties.TryGetValue("confidence", out var value))
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
    }

    /// <summary>
    /// Gets the confidence formatted as a percentage string.
    /// </summary>
    /// <value>Formatted as "85%" for 0.85 confidence.</value>
    public string ConfidenceDisplay => $"{Confidence:P0}";

    /// <summary>
    /// Gets the number of relationships connected to this entity.
    /// </summary>
    public int RelationshipCount { get; }

    /// <summary>
    /// Gets the number of source documents mentioning this entity.
    /// </summary>
    public int MentionCount { get; }

    /// <summary>
    /// Gets the icon identifier for this entity type.
    /// </summary>
    /// <value>
    /// The icon from <see cref="EntityTypeSchema.Icon"/>, or "⬢" if not specified.
    /// </value>
    public string Icon { get; }

    /// <summary>
    /// Gets the color for this entity type.
    /// </summary>
    /// <value>
    /// The hex color from <see cref="EntityTypeSchema.Color"/>, or "#6b7280" (gray) if not specified.
    /// </value>
    public string Color { get; }
}
