// =============================================================================
// File: SchemaRegistry.cs
// Project: Lexichord.Modules.Knowledge
// Description: In-memory registry of knowledge graph entity and relationship schemas.
// =============================================================================
// LOGIC: Implements ISchemaRegistry as the central orchestrator for schema
//   management. Delegates YAML parsing to SchemaLoader and validation to
//   SchemaValidator. Maintains case-insensitive dictionaries of entity types
//   and relationship types loaded from YAML files.
//
// Key behaviors:
//   - LoadSchemasAsync clears and reloads all schemas from a directory
//   - Duplicate type names log a warning; first registration wins
//   - Schema version tracks the highest version across loaded files
//   - All lookups are case-insensitive (StringComparer.OrdinalIgnoreCase)
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// Dependencies: SchemaLoader (v0.4.5f), SchemaValidator (v0.4.5f),
//               ISchemaRegistry (v0.4.5f), KnowledgeRecords (v0.4.5e)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Schema;

/// <summary>
/// In-memory registry of knowledge graph entity and relationship type schemas.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SchemaRegistry"/> implements <see cref="ISchemaRegistry"/> and
/// manages the lifecycle of schema definitions. Schemas are loaded from YAML files
/// via <see cref="SchemaLoader"/> and validated via <see cref="SchemaValidator"/>.
/// </para>
/// <para>
/// <b>Loading:</b> Call <see cref="LoadSchemasAsync"/> with a directory path.
/// All *.yaml and *.yml files are scanned recursively. Existing schemas are
/// cleared before loading. Duplicate type names are ignored with a warning.
/// </para>
/// <para>
/// <b>Validation:</b> <see cref="ValidateEntity"/> and <see cref="ValidateRelationship"/>
/// delegate to <see cref="SchemaValidator"/> which performs comprehensive checks
/// against the loaded schemas.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The registry is designed for load-once-at-startup usage.
/// Concurrent reads after loading are safe; concurrent writes are not supported.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
public sealed class SchemaRegistry : ISchemaRegistry
{
    private readonly ILogger<SchemaRegistry> _logger;
    private readonly SchemaLoader _loader;
    private readonly SchemaValidator _validator;

    // LOGIC: Case-insensitive dictionaries for entity and relationship type lookups.
    // This ensures "Endpoint" and "endpoint" resolve to the same schema.
    private readonly Dictionary<string, EntityTypeSchema> _entityTypes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, RelationshipTypeSchema> _relationshipTypes = new(StringComparer.OrdinalIgnoreCase);

    private string _schemaVersion = "0.0.0";
    private string? _schemaDirectory;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaRegistry"/> class.
    /// </summary>
    /// <param name="logger">Logger for schema loading and validation diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public SchemaRegistry(ILogger<SchemaRegistry> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loader = new SchemaLoader(logger);
        _validator = new SchemaValidator(this);
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, EntityTypeSchema> EntityTypes => _entityTypes;

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, RelationshipTypeSchema> RelationshipTypes => _relationshipTypes;

    /// <inheritdoc/>
    public string SchemaVersion => _schemaVersion;

    /// <inheritdoc/>
    public EntityTypeSchema? GetEntityType(string typeName) =>
        _entityTypes.TryGetValue(typeName, out var schema) ? schema : null;

    /// <inheritdoc/>
    public RelationshipTypeSchema? GetRelationshipType(string typeName) =>
        _relationshipTypes.TryGetValue(typeName, out var schema) ? schema : null;

    /// <inheritdoc/>
    public SchemaValidationResult ValidateEntity(KnowledgeEntity entity) =>
        _validator.ValidateEntity(entity);

    /// <inheritdoc/>
    public SchemaValidationResult ValidateRelationship(
        KnowledgeRelationship relationship,
        KnowledgeEntity fromEntity,
        KnowledgeEntity toEntity) =>
        _validator.ValidateRelationship(relationship, fromEntity, toEntity);

    /// <inheritdoc/>
    public async Task LoadSchemasAsync(string schemaDirectory, CancellationToken ct = default)
    {
        _schemaDirectory = schemaDirectory;

        _logger.LogInformation("Loading schemas from {Directory}", schemaDirectory);

        // LOGIC: Clear existing schemas before reloading.
        // This ensures a clean slate on reload and prevents stale types.
        _entityTypes.Clear();
        _relationshipTypes.Clear();
        _schemaVersion = "0.0.0";

        // LOGIC: Scan for all YAML files (*.yaml and *.yml) recursively.
        if (!Directory.Exists(schemaDirectory))
        {
            _logger.LogWarning("Schema directory does not exist: {Directory}", schemaDirectory);
            return;
        }

        var yamlFiles = Directory.GetFiles(schemaDirectory, "*.yaml", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(schemaDirectory, "*.yml", SearchOption.AllDirectories))
            .Distinct() // LOGIC: Avoid duplicates if a file matches both patterns (unlikely but safe).
            .OrderBy(f => f); // LOGIC: Deterministic load order.

        foreach (var file in yamlFiles)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var schema = await _loader.LoadSchemaFileAsync(file, ct);

                // LOGIC: Register entity types (first registration wins for duplicates).
                foreach (var entityType in schema.EntityTypes)
                {
                    if (_entityTypes.TryAdd(entityType.Name, entityType))
                    {
                        _logger.LogDebug("Registered entity type: {Type}", entityType.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Duplicate entity type ignored: {Type} in {File}",
                            entityType.Name, file);
                    }
                }

                // LOGIC: Register relationship types (first registration wins for duplicates).
                foreach (var relType in schema.RelationshipTypes)
                {
                    if (_relationshipTypes.TryAdd(relType.Name, relType))
                    {
                        _logger.LogDebug("Registered relationship type: {Type}", relType.Name);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Duplicate relationship type ignored: {Type} in {File}",
                            relType.Name, file);
                    }
                }

                // LOGIC: Track highest schema version across all loaded files.
                if (Version.TryParse(schema.SchemaVersion, out var version) &&
                    Version.TryParse(_schemaVersion, out var current) &&
                    version > current)
                {
                    _schemaVersion = schema.SchemaVersion;
                }
            }
            catch (OperationCanceledException)
            {
                throw; // LOGIC: Propagate cancellation without logging.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load schema file: {File}", file);
            }
        }

        _logger.LogInformation(
            "Schema loading complete: {EntityCount} entity types, {RelCount} relationship types",
            _entityTypes.Count, _relationshipTypes.Count);
    }

    /// <inheritdoc/>
    public async Task ReloadAsync(CancellationToken ct = default)
    {
        // LOGIC: No-op if LoadSchemasAsync has not been called previously.
        if (_schemaDirectory != null)
        {
            _logger.LogInformation("Reloading schemas from {Directory}", _schemaDirectory);
            await LoadSchemasAsync(_schemaDirectory, ct);
        }
        else
        {
            _logger.LogDebug("ReloadAsync called but no schema directory has been set");
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetValidRelationships(string fromType, string toType)
    {
        // LOGIC: Filter relationship types where both the from and to entity types match.
        // Comparison is case-insensitive to match the case-insensitive dictionaries.
        return _relationshipTypes.Values
            .Where(r => r.FromEntityTypes.Contains(fromType, StringComparer.OrdinalIgnoreCase) &&
                        r.ToEntityTypes.Contains(toType, StringComparer.OrdinalIgnoreCase))
            .Select(r => r.Name)
            .ToList();
    }
}
