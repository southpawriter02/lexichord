// =============================================================================
// File: ISchemaRegistry.cs
// Project: Lexichord.Abstractions
// Description: Interface for the knowledge graph schema registry.
// =============================================================================
// LOGIC: Defines the contract for managing entity type and relationship type
//   schemas in the knowledge graph. The registry loads schema definitions from
//   YAML files, validates entities and relationships before storage, and
//   provides query capabilities for discovering valid types and relationships.
//
// Key operations:
//   - Load schema definitions from a directory of YAML files
//   - Validate KnowledgeEntity instances against their type schema
//   - Validate KnowledgeRelationship instances against from/to constraints
//   - Query available entity types, relationship types, and valid relationships
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// Dependencies: KnowledgeEntity, KnowledgeRelationship (v0.4.5e),
//               SchemaRecords (v0.4.5f)
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Registry for knowledge graph entity and relationship type schemas.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ISchemaRegistry"/> manages schema definitions that govern
/// what entity types and relationship types are valid in the knowledge graph.
/// Schemas are loaded from YAML files and used to validate
/// <see cref="KnowledgeEntity"/> and <see cref="KnowledgeRelationship"/>
/// instances before storage in Neo4j.
/// </para>
/// <para>
/// <b>Schema Loading:</b> Call <see cref="LoadSchemasAsync"/> with a directory
/// path containing YAML schema files (*.yaml, *.yml). The registry scans
/// recursively and registers all entity types and relationship types found.
/// Duplicate type names are logged as warnings and the first registration wins.
/// </para>
/// <para>
/// <b>Validation:</b> Use <see cref="ValidateEntity"/> and
/// <see cref="ValidateRelationship"/> to check entities and relationships
/// against their type schemas before persistence. Validation checks include:
/// <list type="bullet">
///   <item>Entity type existence in schema.</item>
///   <item>Abstract type instantiation prevention.</item>
///   <item>Required property presence.</item>
///   <item>Property type correctness.</item>
///   <item>Property constraint enforcement (length, pattern, range).</item>
///   <item>Relationship from/to entity type constraints.</item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Teams: Full schema access (create, edit, validate).</item>
///   <item>Enterprise: Full access plus custom ontology schemas.</item>
///   <item>WriterPro: Read-only schema queries (no modifications).</item>
///   <item>Core: Not available.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Load schemas from workspace
/// await schemaRegistry.LoadSchemasAsync(".lexichord/knowledge/schema");
///
/// // Validate an entity before storage
/// var entity = new KnowledgeEntity { Type = "Endpoint", Name = "GET /users",
///     Properties = new() { ["path"] = "/users", ["method"] = "GET" } };
/// var result = schemaRegistry.ValidateEntity(entity);
/// if (!result.IsValid)
///     logger.LogWarning("Validation failed: {Errors}", result.Errors);
/// </code>
/// </example>
public interface ISchemaRegistry
{
    /// <summary>
    /// Gets all registered entity type schemas.
    /// </summary>
    /// <value>
    /// A read-only dictionary keyed by entity type name (case-insensitive).
    /// Empty until <see cref="LoadSchemasAsync"/> is called.
    /// </value>
    IReadOnlyDictionary<string, EntityTypeSchema> EntityTypes { get; }

    /// <summary>
    /// Gets all registered relationship type schemas.
    /// </summary>
    /// <value>
    /// A read-only dictionary keyed by relationship type name (case-insensitive).
    /// Empty until <see cref="LoadSchemasAsync"/> is called.
    /// </value>
    IReadOnlyDictionary<string, RelationshipTypeSchema> RelationshipTypes { get; }

    /// <summary>
    /// Gets the current schema version (highest version among loaded schemas).
    /// </summary>
    /// <value>A version string (e.g., "1.0"). Defaults to "0.0.0" before loading.</value>
    /// <remarks>
    /// LOGIC: The schema version is the highest <c>schema_version</c> value found
    /// across all loaded YAML files. Used for compatibility checking.
    /// </remarks>
    string SchemaVersion { get; }

    /// <summary>
    /// Gets an entity type schema by name.
    /// </summary>
    /// <param name="typeName">Entity type name (case-insensitive).</param>
    /// <returns>
    /// The <see cref="EntityTypeSchema"/> if found; otherwise <c>null</c>.
    /// </returns>
    EntityTypeSchema? GetEntityType(string typeName);

    /// <summary>
    /// Gets a relationship type schema by name.
    /// </summary>
    /// <param name="typeName">Relationship type name (case-insensitive).</param>
    /// <returns>
    /// The <see cref="RelationshipTypeSchema"/> if found; otherwise <c>null</c>.
    /// </returns>
    RelationshipTypeSchema? GetRelationshipType(string typeName);

    /// <summary>
    /// Validates an entity against its type schema.
    /// </summary>
    /// <param name="entity">The <see cref="KnowledgeEntity"/> to validate.</param>
    /// <returns>
    /// A <see cref="SchemaValidationResult"/> containing any errors and warnings.
    /// The result's <see cref="SchemaValidationResult.IsValid"/> is <c>true</c>
    /// if there are no errors.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Validation checks performed (in order):
    /// <list type="number">
    ///   <item>Entity type exists in schema.</item>
    ///   <item>Entity type is not abstract.</item>
    ///   <item>Entity name is non-empty.</item>
    ///   <item>All required properties are present and non-empty.</item>
    ///   <item>Property values match their declared types.</item>
    ///   <item>Property values satisfy constraints (length, pattern, range).</item>
    /// </list>
    /// </para>
    /// </remarks>
    SchemaValidationResult ValidateEntity(KnowledgeEntity entity);

    /// <summary>
    /// Validates a relationship against its type schema.
    /// </summary>
    /// <param name="relationship">The <see cref="KnowledgeRelationship"/> to validate.</param>
    /// <param name="fromEntity">The source <see cref="KnowledgeEntity"/>.</param>
    /// <param name="toEntity">The target <see cref="KnowledgeEntity"/>.</param>
    /// <returns>
    /// A <see cref="SchemaValidationResult"/> containing any errors and warnings.
    /// </returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Validation checks performed (in order):
    /// <list type="number">
    ///   <item>Relationship type exists in schema.</item>
    ///   <item>Source entity type is in the schema's <see cref="RelationshipTypeSchema.FromEntityTypes"/>.</item>
    ///   <item>Target entity type is in the schema's <see cref="RelationshipTypeSchema.ToEntityTypes"/>.</item>
    ///   <item>All required relationship properties are present.</item>
    /// </list>
    /// </para>
    /// </remarks>
    SchemaValidationResult ValidateRelationship(
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity);

    /// <summary>
    /// Loads schemas from a directory of YAML files.
    /// </summary>
    /// <param name="schemaDirectory">
    /// Path to a directory containing schema YAML files (*.yaml, *.yml).
    /// Searched recursively.
    /// </param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous load operation.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Clears all existing schemas before loading. Files are processed
    /// sequentially. Duplicate type names within a file or across files are
    /// logged as warnings; the first registration wins. Errors in individual
    /// files are logged but do not prevent loading of other files.
    /// </para>
    /// </remarks>
    Task LoadSchemasAsync(string schemaDirectory, CancellationToken ct = default);

    /// <summary>
    /// Reloads all schemas from the previously loaded directory.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous reload operation.</returns>
    /// <remarks>
    /// LOGIC: No-op if <see cref="LoadSchemasAsync"/> has not been called previously.
    /// Useful for refreshing schemas after file changes.
    /// </remarks>
    Task ReloadAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets valid relationship types between two entity types.
    /// </summary>
    /// <param name="fromType">Source entity type name (case-insensitive).</param>
    /// <param name="toType">Target entity type name (case-insensitive).</param>
    /// <returns>
    /// A list of relationship type names that are valid between the specified
    /// entity types. Empty if no valid relationships exist.
    /// </returns>
    IReadOnlyList<string> GetValidRelationships(string fromType, string toType);
}
