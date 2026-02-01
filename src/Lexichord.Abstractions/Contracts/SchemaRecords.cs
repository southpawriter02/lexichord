// =============================================================================
// File: SchemaRecords.cs
// Project: Lexichord.Abstractions
// Description: Domain records and enums for the knowledge graph schema system.
// =============================================================================
// LOGIC: Defines the schema type system used by the Schema Registry (v0.4.5f)
//   to govern entity types, relationship types, and their validation. These
//   records are consumed by SchemaRegistry, SchemaValidator, and entity
//   extraction pipelines.
//
// Records defined:
//   - PropertySchema: Property definition with type, constraints, and metadata
//   - EntityTypeSchema: Entity type definition with properties and hierarchy
//   - RelationshipTypeSchema: Relationship type with from/to constraints
//   - SchemaValidationResult: Outcome of schema validation (errors + warnings)
//   - SchemaValidationError: A blocking validation error
//   - SchemaValidationWarning: A non-blocking validation warning
//
// Enums defined:
//   - PropertyType: Data types for schema properties
//   - Cardinality: Relationship cardinality constraints
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// Dependencies: None (pure abstraction)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Data types available for schema property definitions.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="PropertyType"/> defines the valid data types for properties in
/// <see cref="EntityTypeSchema"/> and <see cref="RelationshipTypeSchema"/>
/// definitions. Used by the Schema Validator to enforce type correctness
/// when creating or updating <see cref="KnowledgeEntity"/> instances.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
public enum PropertyType
{
    /// <summary>Short text value with optional length constraints.</summary>
    String,

    /// <summary>Long text value with no length limit.</summary>
    Text,

    /// <summary>Numeric value (int, long, float, double, or decimal).</summary>
    Number,

    /// <summary>Boolean true/false value.</summary>
    Boolean,

    /// <summary>Enumerated value from a predefined list of valid options.</summary>
    Enum,

    /// <summary>Array/list of values with an optional element type.</summary>
    Array,

    /// <summary>Date and time value (ISO 8601 format).</summary>
    DateTime,

    /// <summary>Reference to another entity by ID.</summary>
    Reference
}

/// <summary>
/// Relationship cardinality constraints for schema definitions.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="Cardinality"/> defines how many entities can participate on
/// each side of a <see cref="RelationshipTypeSchema"/>. Used for schema
/// governance to enforce relationship multiplicity rules.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
public enum Cardinality
{
    /// <summary>Exactly one source entity to exactly one target entity.</summary>
    OneToOne,

    /// <summary>One source entity to many target entities.</summary>
    OneToMany,

    /// <summary>Many source entities to one target entity.</summary>
    ManyToOne,

    /// <summary>Many source entities to many target entities.</summary>
    ManyToMany
}

/// <summary>
/// Schema definition for a property within an entity or relationship type.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="PropertySchema"/> defines a single property's name, data type,
/// and validation constraints. Properties are declared in <see cref="EntityTypeSchema"/>
/// and <see cref="RelationshipTypeSchema"/> definitions.
/// </para>
/// <para>
/// <b>Constraint Validation:</b>
/// <list type="bullet">
///   <item><see cref="Required"/>: Property must be present and non-empty.</item>
///   <item><see cref="MaxLength"/>: Maximum string length (String type only).</item>
///   <item><see cref="Pattern"/>: Regex pattern match (String type only).</item>
///   <item><see cref="MinValue"/>/<see cref="MaxValue"/>: Numeric range (Number type only).</item>
///   <item><see cref="EnumValues"/>: Valid values list (Enum type only).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var pathProperty = new PropertySchema
/// {
///     Name = "path",
///     Type = PropertyType.String,
///     Required = true,
///     Pattern = @"^\/[\w\-\/\{\}]*$"
/// };
/// </code>
/// </example>
public record PropertySchema
{
    /// <summary>
    /// Property name (used as the key in entity/relationship properties dictionaries).
    /// </summary>
    /// <value>The property name (e.g., "path", "method", "description").</value>
    public required string Name { get; init; }

    /// <summary>
    /// Data type for this property.
    /// </summary>
    /// <value>One of the <see cref="PropertyType"/> enum values.</value>
    /// <remarks>
    /// LOGIC: The type determines which validation rules are applied. For example,
    /// <see cref="PropertyType.Enum"/> requires <see cref="EnumValues"/> to be set,
    /// and <see cref="PropertyType.Number"/> enables <see cref="MinValue"/>/<see cref="MaxValue"/>.
    /// </remarks>
    public required PropertyType Type { get; init; }

    /// <summary>
    /// Human-readable description of the property.
    /// </summary>
    /// <value>Optional description for documentation and UI display.</value>
    public string? Description { get; init; }

    /// <summary>
    /// Whether this property is required when creating an entity.
    /// </summary>
    /// <value><c>true</c> if the property must be present and non-empty; otherwise <c>false</c>.</value>
    /// <remarks>
    /// LOGIC: Required properties are validated during entity creation. Missing or
    /// empty/whitespace values for required properties produce a REQUIRED_PROPERTY_MISSING error.
    /// </remarks>
    public bool Required { get; init; } = false;

    /// <summary>
    /// Default value for the property (as a string, parsed by type).
    /// </summary>
    /// <value>Optional default value string representation.</value>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// Valid values for <see cref="PropertyType.Enum"/> type properties.
    /// </summary>
    /// <value>List of valid enum string values, or <c>null</c> for non-enum types.</value>
    /// <remarks>
    /// LOGIC: Enum validation is case-insensitive. If set, the property value's
    /// string representation must match one of these values.
    /// </remarks>
    public IReadOnlyList<string>? EnumValues { get; init; }

    /// <summary>
    /// Minimum value for <see cref="PropertyType.Number"/> type properties.
    /// </summary>
    /// <value>Minimum allowed numeric value, or <c>null</c> for no lower bound.</value>
    public double? MinValue { get; init; }

    /// <summary>
    /// Maximum value for <see cref="PropertyType.Number"/> type properties.
    /// </summary>
    /// <value>Maximum allowed numeric value, or <c>null</c> for no upper bound.</value>
    public double? MaxValue { get; init; }

    /// <summary>
    /// Maximum length for <see cref="PropertyType.String"/> type properties.
    /// </summary>
    /// <value>Maximum allowed string length, or <c>null</c> for no length limit.</value>
    public int? MaxLength { get; init; }

    /// <summary>
    /// Regex pattern for <see cref="PropertyType.String"/> type validation.
    /// </summary>
    /// <value>A regular expression pattern the value must match, or <c>null</c> for no pattern.</value>
    public string? Pattern { get; init; }

    /// <summary>
    /// Element type for <see cref="PropertyType.Array"/> properties.
    /// </summary>
    /// <value>The type of elements in the array, or <c>null</c> for untyped arrays.</value>
    public PropertyType? ArrayElementType { get; init; }
}

/// <summary>
/// Schema definition for an entity type in the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="EntityTypeSchema"/> defines a type of node that can exist in the
/// knowledge graph. Entity types are loaded from YAML schema files and registered
/// in the <see cref="ISchemaRegistry"/>. All <see cref="KnowledgeEntity"/> instances
/// are validated against their type schema before storage.
/// </para>
/// <para>
/// <b>Type Hierarchy:</b> Entity types support single inheritance via
/// <see cref="Extends"/>. Abstract types (<see cref="IsAbstract"/> = true) cannot
/// be instantiated directly but can serve as base types.
/// </para>
/// <para>
/// <b>Built-in Types:</b> The technical documentation schema includes:
/// Product, Component, Endpoint, Parameter, Response, Concept.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var endpointSchema = new EntityTypeSchema
/// {
///     Name = "Endpoint",
///     Description = "An API endpoint",
///     Icon = "link",
///     Color = "#f59e0b",
///     Properties = new[]
///     {
///         new PropertySchema { Name = "path", Type = PropertyType.String, Required = true },
///         new PropertySchema { Name = "method", Type = PropertyType.Enum, Required = true,
///             EnumValues = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" } }
///     },
///     RequiredProperties = new[] { "path", "method" }
/// };
/// </code>
/// </example>
public record EntityTypeSchema
{
    /// <summary>
    /// Entity type name (e.g., "Product", "Endpoint", "Parameter").
    /// </summary>
    /// <value>
    /// The type name used as the key in the <see cref="ISchemaRegistry.EntityTypes"/>
    /// dictionary and as the Neo4j node label.
    /// </value>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the entity type.
    /// </summary>
    /// <value>Optional description for documentation and UI display.</value>
    public string? Description { get; init; }

    /// <summary>
    /// Property definitions for this entity type.
    /// </summary>
    /// <value>A read-only list of <see cref="PropertySchema"/> definitions.</value>
    /// <remarks>
    /// LOGIC: Properties define the valid key-value pairs for entities of this type.
    /// Properties not defined in the schema generate UNKNOWN_PROPERTY warnings during
    /// validation (non-blocking).
    /// </remarks>
    public required IReadOnlyList<PropertySchema> Properties { get; init; }

    /// <summary>
    /// Names of required properties (derived from <see cref="PropertySchema.Required"/>).
    /// </summary>
    /// <value>
    /// A list of property names that must be present and non-empty on entities
    /// of this type. Defaults to an empty list.
    /// </value>
    /// <remarks>
    /// LOGIC: Populated by the SchemaLoader from properties with Required = true.
    /// Used by the SchemaValidator for fast required-property checking.
    /// </remarks>
    public IReadOnlyList<string> RequiredProperties { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Parent entity type for single inheritance (optional).
    /// </summary>
    /// <value>The name of the parent entity type, or <c>null</c> for root types.</value>
    public string? Extends { get; init; }

    /// <summary>
    /// Whether this type is abstract (cannot be instantiated directly).
    /// </summary>
    /// <value><c>true</c> if abstract; otherwise <c>false</c>. Defaults to <c>false</c>.</value>
    /// <remarks>
    /// LOGIC: Abstract types can serve as base types for inheritance but cannot
    /// be used as the Type of a <see cref="KnowledgeEntity"/>. Attempting to create
    /// an entity with an abstract type produces an ABSTRACT_TYPE validation error.
    /// </remarks>
    public bool IsAbstract { get; init; } = false;

    /// <summary>
    /// Icon identifier for UI display (e.g., "package", "link", "variable").
    /// </summary>
    /// <value>Optional icon name for the Entity Browser (v0.4.7).</value>
    public string? Icon { get; init; }

    /// <summary>
    /// Color for UI display (hex code, e.g., "#3b82f6").
    /// </summary>
    /// <value>Optional hex color code for the Entity Browser (v0.4.7).</value>
    public string? Color { get; init; }
}

/// <summary>
/// Schema definition for a relationship type between entities in the knowledge graph.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RelationshipTypeSchema"/> defines a valid type of edge that can
/// connect two <see cref="KnowledgeEntity"/> instances. Relationship types specify
/// which entity types can appear as source (<see cref="FromEntityTypes"/>) and
/// target (<see cref="ToEntityTypes"/>), along with optional properties.
/// </para>
/// <para>
/// <b>Built-in Relationship Types:</b> CONTAINS, EXPOSES, ACCEPTS, RETURNS,
/// REQUIRES, RELATED_TO.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var acceptsSchema = new RelationshipTypeSchema
/// {
///     Name = "ACCEPTS",
///     Description = "Endpoint accepts parameter",
///     FromEntityTypes = new[] { "Endpoint" },
///     ToEntityTypes = new[] { "Parameter" },
///     Cardinality = Cardinality.OneToMany,
///     Properties = new[]
///     {
///         new PropertySchema { Name = "location", Type = PropertyType.Enum,
///             EnumValues = new[] { "path", "query", "header", "body" } }
///     }
/// };
/// </code>
/// </example>
public record RelationshipTypeSchema
{
    /// <summary>
    /// Relationship type name (e.g., "CONTAINS", "ACCEPTS", "RETURNS").
    /// </summary>
    /// <value>
    /// The type name used as the key in the <see cref="ISchemaRegistry.RelationshipTypes"/>
    /// dictionary and as the Neo4j relationship type.
    /// </value>
    public required string Name { get; init; }

    /// <summary>
    /// Human-readable description of the relationship type.
    /// </summary>
    /// <value>Optional description for documentation and UI display.</value>
    public string? Description { get; init; }

    /// <summary>
    /// Valid source entity types for this relationship.
    /// </summary>
    /// <value>
    /// A list of entity type names that can appear as the "from" (source) side
    /// of this relationship. Validated during relationship creation.
    /// </value>
    /// <remarks>
    /// LOGIC: Validation is case-insensitive. If the source entity's Type is not
    /// in this list, an INVALID_FROM_TYPE error is produced.
    /// </remarks>
    public required IReadOnlyList<string> FromEntityTypes { get; init; }

    /// <summary>
    /// Valid target entity types for this relationship.
    /// </summary>
    /// <value>
    /// A list of entity type names that can appear as the "to" (target) side
    /// of this relationship. Validated during relationship creation.
    /// </value>
    /// <remarks>
    /// LOGIC: Validation is case-insensitive. If the target entity's Type is not
    /// in this list, an INVALID_TO_TYPE error is produced.
    /// </remarks>
    public required IReadOnlyList<string> ToEntityTypes { get; init; }

    /// <summary>
    /// Property definitions for this relationship type.
    /// </summary>
    /// <value>Optional list of <see cref="PropertySchema"/> definitions, or <c>null</c>.</value>
    public IReadOnlyList<PropertySchema>? Properties { get; init; }

    /// <summary>
    /// Cardinality constraint for this relationship.
    /// </summary>
    /// <value>One of the <see cref="Contracts.Cardinality"/> values. Defaults to <see cref="Cardinality.ManyToMany"/>.</value>
    public Cardinality Cardinality { get; init; } = Cardinality.ManyToMany;

    /// <summary>
    /// Whether the relationship is directional.
    /// </summary>
    /// <value><c>true</c> if the relationship has a direction (most relationships); <c>false</c> for bidirectional.</value>
    public bool Directional { get; init; } = true;
}

/// <summary>
/// A blocking error produced during schema validation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SchemaValidationError"/> represents a validation failure that prevents
/// an entity or relationship from being stored. The <see cref="Code"/> field provides
/// a programmatic error identifier, while <see cref="Message"/> provides a
/// human-readable description.
/// </para>
/// <para>
/// <b>Error Codes:</b>
/// <list type="bullet">
///   <item>UNKNOWN_ENTITY_TYPE — Entity type not registered in schema.</item>
///   <item>ABSTRACT_TYPE — Cannot instantiate abstract entity types.</item>
///   <item>NAME_REQUIRED — Entity name must be non-empty.</item>
///   <item>REQUIRED_PROPERTY_MISSING — Required property is missing or empty.</item>
///   <item>TYPE_MISMATCH — Property value does not match expected type.</item>
///   <item>INVALID_ENUM_VALUE — Value not in allowed enum values list.</item>
///   <item>MAX_LENGTH_EXCEEDED — String exceeds maximum length constraint.</item>
///   <item>PATTERN_MISMATCH — String does not match regex pattern.</item>
///   <item>BELOW_MINIMUM — Number is below minimum value constraint.</item>
///   <item>ABOVE_MAXIMUM — Number is above maximum value constraint.</item>
///   <item>UNKNOWN_RELATIONSHIP_TYPE — Relationship type not registered.</item>
///   <item>INVALID_FROM_TYPE — Source entity type not valid for relationship.</item>
///   <item>INVALID_TO_TYPE — Target entity type not valid for relationship.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
public record SchemaValidationError
{
    /// <summary>
    /// Error code for programmatic handling.
    /// </summary>
    /// <value>A constant string identifier (e.g., "UNKNOWN_ENTITY_TYPE", "TYPE_MISMATCH").</value>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error message.
    /// </summary>
    /// <value>A descriptive message suitable for logging and display.</value>
    public required string Message { get; init; }

    /// <summary>
    /// Property name that failed validation (if applicable).
    /// </summary>
    /// <value>The property name, or <c>null</c> for entity/type-level errors.</value>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Actual value that was invalid (if applicable).
    /// </summary>
    /// <value>The invalid value for diagnostic purposes, or <c>null</c>.</value>
    public object? ActualValue { get; init; }
}

/// <summary>
/// A non-blocking warning produced during schema validation.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SchemaValidationWarning"/> represents a non-fatal validation concern
/// that does not prevent storage. Warnings are informational — for example, when
/// an entity has properties not defined in its schema (UNKNOWN_PROPERTY).
/// </para>
/// <para>
/// <b>Warning Codes:</b>
/// <list type="bullet">
///   <item>UNKNOWN_PROPERTY — Property not defined in schema (extra data).</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
public record SchemaValidationWarning
{
    /// <summary>
    /// Warning code for programmatic handling.
    /// </summary>
    /// <value>A constant string identifier (e.g., "UNKNOWN_PROPERTY").</value>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable warning message.
    /// </summary>
    /// <value>A descriptive message suitable for logging and display.</value>
    public required string Message { get; init; }

    /// <summary>
    /// Property name (if applicable).
    /// </summary>
    /// <value>The property name that triggered the warning, or <c>null</c>.</value>
    public string? PropertyName { get; init; }
}

/// <summary>
/// Result of validating an entity or relationship against its schema.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SchemaValidationResult"/> aggregates errors and warnings from
/// schema validation. An entity or relationship is considered valid if and only
/// if there are zero errors (<see cref="IsValid"/> = true). Warnings are
/// non-blocking and do not affect validity.
/// </para>
/// <para>
/// <b>Factory Methods:</b>
/// <list type="bullet">
///   <item><see cref="Valid"/>: Creates a result with no errors or warnings.</item>
///   <item><see cref="Invalid"/>: Creates a result with the specified errors.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = registry.ValidateEntity(entity);
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///         logger.LogWarning("Validation error [{Code}]: {Message}", error.Code, error.Message);
/// }
/// </code>
/// </example>
public record SchemaValidationResult
{
    /// <summary>
    /// Whether validation passed (no errors).
    /// </summary>
    /// <value><c>true</c> if <see cref="Errors"/> is empty; otherwise <c>false</c>.</value>
    /// <remarks>
    /// LOGIC: Only errors affect validity. A result with warnings but no errors
    /// is still considered valid.
    /// </remarks>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Validation errors (empty if valid).
    /// </summary>
    /// <value>A read-only list of <see cref="SchemaValidationError"/> instances.</value>
    public IReadOnlyList<SchemaValidationError> Errors { get; init; } = Array.Empty<SchemaValidationError>();

    /// <summary>
    /// Validation warnings (non-blocking).
    /// </summary>
    /// <value>A read-only list of <see cref="SchemaValidationWarning"/> instances.</value>
    public IReadOnlyList<SchemaValidationWarning> Warnings { get; init; } = Array.Empty<SchemaValidationWarning>();

    /// <summary>
    /// Creates a valid result with no errors or warnings.
    /// </summary>
    /// <returns>A <see cref="SchemaValidationResult"/> where <see cref="IsValid"/> is <c>true</c>.</returns>
    public static SchemaValidationResult Valid() => new();

    /// <summary>
    /// Creates an invalid result with the specified errors.
    /// </summary>
    /// <param name="errors">One or more <see cref="SchemaValidationError"/> instances.</param>
    /// <returns>A <see cref="SchemaValidationResult"/> where <see cref="IsValid"/> is <c>false</c>.</returns>
    public static SchemaValidationResult Invalid(params SchemaValidationError[] errors) =>
        new() { Errors = errors };
}
