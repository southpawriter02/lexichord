// =============================================================================
// File: PropertyItemViewModel.cs
// Project: Lexichord.Modules.Knowledge
// Description: View model representing a property in the Entity Detail View.
// =============================================================================
// LOGIC: Provides a display-ready representation of an entity property with
//   metadata from the schema registry (type, description, required flag).
//
// v0.4.7f: Entity Detail View (Knowledge Graph Browser)
// Dependencies: None (pure view model record)
// =============================================================================

namespace Lexichord.Modules.Knowledge.UI.ViewModels;

/// <summary>
/// View model representing a single property in the Entity Detail View.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PropertyItemViewModel"/> wraps an entity property with
/// display-ready values and schema metadata. Used in the Properties section
/// of the <see cref="EntityDetailView"/>.
/// </para>
/// <para>
/// <b>Schema Integration:</b> The <see cref="Type"/>, <see cref="Description"/>,
/// and <see cref="IsRequired"/> properties are derived from the
/// <see cref="PropertySchema"/> registered in the Schema Registry (v0.4.5f).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7f as part of the Entity Detail View.
/// </para>
/// </remarks>
public sealed record PropertyItemViewModel
{
    /// <summary>
    /// Gets the property name (key).
    /// </summary>
    /// <value>The property key as defined in the entity's Properties dictionary.</value>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the property value formatted as a display string.
    /// </summary>
    /// <value>
    /// The property value converted to a human-readable string.
    /// Complex types (arrays, objects) are formatted appropriately.
    /// </value>
    public required string Value { get; init; }

    /// <summary>
    /// Gets the property type from the schema.
    /// </summary>
    /// <value>
    /// The type name (e.g., "string", "number", "boolean", "array").
    /// Defaults to "string" if not specified in the schema.
    /// </value>
    public string Type { get; init; } = "string";

    /// <summary>
    /// Gets the property description from the schema.
    /// </summary>
    /// <value>
    /// A human-readable description of the property's purpose.
    /// Null if not specified in the schema.
    /// </value>
    public string? Description { get; init; }

    /// <summary>
    /// Gets whether this property is required by the schema.
    /// </summary>
    /// <value>
    /// True if the property is marked as required in the schema; otherwise false.
    /// </value>
    public bool IsRequired { get; init; }
}
