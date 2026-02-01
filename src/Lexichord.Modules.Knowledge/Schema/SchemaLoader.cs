// =============================================================================
// File: SchemaLoader.cs
// Project: Lexichord.Modules.Knowledge
// Description: YAML schema file parser for the knowledge graph schema system.
// =============================================================================
// LOGIC: Parses YAML schema files into domain records using YamlDotNet.
//   Each YAML file can define multiple entity types and relationship types.
//   The loader handles:
//   - YAML deserialization with underscore naming convention
//   - Mapping from raw YAML DTOs to domain records (EntityTypeSchema, etc.)
//   - Flexible From/To handling (single string or list of strings)
//   - Graceful defaults for missing or invalid values
//
// Raw YAML classes are private nested types to avoid leaking implementation
// details. The public API returns SchemaDocument records containing fully
// mapped domain types.
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// Dependencies: YamlDotNet (16.x), SchemaRecords (v0.4.5f)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Lexichord.Modules.Knowledge.Schema;

/// <summary>
/// Loads schema definitions from YAML files.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SchemaLoader"/> reads YAML schema files and converts them
/// into <see cref="SchemaDocument"/> records containing <see cref="EntityTypeSchema"/>
/// and <see cref="RelationshipTypeSchema"/> definitions.
/// </para>
/// <para>
/// <b>YAML Format:</b> Uses underscore naming convention (e.g., <c>schema_version</c>,
/// <c>entity_types</c>, <c>max_length</c>). Unknown properties are silently ignored
/// for forward compatibility.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
internal sealed class SchemaLoader
{
    private readonly ILogger _logger;
    private readonly IDeserializer _deserializer;

    /// <summary>
    /// Initializes a new instance of the <see cref="SchemaLoader"/> class.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public SchemaLoader(ILogger logger)
    {
        _logger = logger;

        // LOGIC: Configure YamlDotNet with underscore naming convention to match
        // the YAML format (schema_version, entity_types, etc.). Unknown properties
        // are ignored for forward compatibility with future schema versions.
        _deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();
    }

    /// <summary>
    /// Loads and parses a single YAML schema file.
    /// </summary>
    /// <param name="filePath">Absolute path to the YAML file.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="SchemaDocument"/> containing the parsed types.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the file does not exist.</exception>
    /// <exception cref="YamlDotNet.Core.YamlException">Thrown if the YAML is malformed.</exception>
    public async Task<SchemaDocument> LoadSchemaFileAsync(string filePath, CancellationToken ct = default)
    {
        _logger.LogDebug("Loading schema file: {FilePath}", filePath);

        var yaml = await File.ReadAllTextAsync(filePath, ct);

        // LOGIC: Deserialize raw YAML into intermediate DTO classes.
        // These are then mapped to domain records with proper defaults and type handling.
        var rawDoc = _deserializer.Deserialize<RawSchemaDocument>(yaml);

        if (rawDoc == null)
        {
            _logger.LogWarning("Schema file is empty or null: {FilePath}", filePath);
            return new SchemaDocument
            {
                SchemaVersion = "1.0",
                Name = Path.GetFileNameWithoutExtension(filePath)
            };
        }

        var entityTypes = rawDoc.EntityTypes?.Select(MapEntityType).ToList()
            ?? new List<EntityTypeSchema>();

        var relationshipTypes = rawDoc.RelationshipTypes?.Select(MapRelationshipType).ToList()
            ?? new List<RelationshipTypeSchema>();

        _logger.LogDebug(
            "Parsed schema file {FileName}: {EntityCount} entity types, {RelCount} relationship types",
            Path.GetFileName(filePath), entityTypes.Count, relationshipTypes.Count);

        return new SchemaDocument
        {
            SchemaVersion = rawDoc.SchemaVersion ?? "1.0",
            Name = rawDoc.Name ?? Path.GetFileNameWithoutExtension(filePath),
            Description = rawDoc.Description,
            EntityTypes = entityTypes,
            RelationshipTypes = relationshipTypes
        };
    }

    /// <summary>
    /// Maps a raw YAML entity type to a domain <see cref="EntityTypeSchema"/> record.
    /// </summary>
    /// <param name="raw">The raw YAML entity type DTO.</param>
    /// <returns>A fully mapped <see cref="EntityTypeSchema"/>.</returns>
    private EntityTypeSchema MapEntityType(RawEntityType raw)
    {
        var properties = raw.Properties?.Select(MapProperty).ToList()
            ?? new List<PropertySchema>();

        // LOGIC: Derive RequiredProperties from properties with Required = true.
        // This avoids requiring authors to declare required properties in two places.
        var requiredProperties = properties
            .Where(p => p.Required)
            .Select(p => p.Name)
            .ToList();

        return new EntityTypeSchema
        {
            Name = raw.Name,
            Description = raw.Description,
            Properties = properties,
            RequiredProperties = requiredProperties,
            Extends = raw.Extends,
            IsAbstract = raw.IsAbstract,
            Icon = raw.Icon,
            Color = raw.Color
        };
    }

    /// <summary>
    /// Maps a raw YAML relationship type to a domain <see cref="RelationshipTypeSchema"/> record.
    /// </summary>
    /// <param name="raw">The raw YAML relationship type DTO.</param>
    /// <returns>A fully mapped <see cref="RelationshipTypeSchema"/>.</returns>
    private RelationshipTypeSchema MapRelationshipType(RawRelationshipType raw)
    {
        // LOGIC: The From and To fields in YAML can be either a single string
        // or a list of strings. We normalize both cases to IReadOnlyList<string>.
        var fromTypes = NormalizeTypeList(raw.From);
        var toTypes = NormalizeTypeList(raw.To);

        // LOGIC: Parse cardinality with fallback to ManyToMany.
        // YAML uses snake_case (e.g., "one_to_many"), so normalize underscores.
        var cardinality = Cardinality.ManyToMany;
        if (!string.IsNullOrEmpty(raw.Cardinality))
        {
            var normalized = raw.Cardinality.Replace("_", "");
            if (Enum.TryParse<Cardinality>(normalized, true, out var parsed))
            {
                cardinality = parsed;
            }
        }

        return new RelationshipTypeSchema
        {
            Name = raw.Name,
            Description = raw.Description,
            FromEntityTypes = fromTypes,
            ToEntityTypes = toTypes,
            Properties = raw.Properties?.Select(MapProperty).ToList(),
            Cardinality = cardinality,
            Directional = raw.Directional ?? true
        };
    }

    /// <summary>
    /// Maps a raw YAML property to a domain <see cref="PropertySchema"/> record.
    /// </summary>
    /// <param name="raw">The raw YAML property DTO.</param>
    /// <returns>A fully mapped <see cref="PropertySchema"/>.</returns>
    private PropertySchema MapProperty(RawProperty raw)
    {
        // LOGIC: Parse PropertyType with fallback to String for unknown types.
        var propertyType = Enum.TryParse<PropertyType>(raw.Type, true, out var t)
            ? t
            : PropertyType.String;

        return new PropertySchema
        {
            Name = raw.Name,
            Type = propertyType,
            Description = raw.Description,
            Required = raw.Required,
            DefaultValue = raw.Default?.ToString(),
            EnumValues = raw.Values?.ToList(),
            MinValue = raw.Min,
            MaxValue = raw.Max,
            MaxLength = raw.MaxLength,
            Pattern = raw.Pattern
        };
    }

    /// <summary>
    /// Normalizes a YAML value that can be either a single string or a list of strings
    /// into a consistent <see cref="IReadOnlyList{T}"/> of strings.
    /// </summary>
    /// <param name="value">The YAML value (string, list, or null).</param>
    /// <returns>A list of type name strings.</returns>
    private static IReadOnlyList<string> NormalizeTypeList(object? value)
    {
        // LOGIC: YAML allows both "from: Product" (string) and
        // "from: [Product, Component]" (list). We normalize both to a list.
        return value switch
        {
            string s => new[] { s },
            IEnumerable<object> list => list.Select(x => x.ToString()!).ToList(),
            _ => Array.Empty<string>()
        };
    }

    // =========================================================================
    // Raw YAML Mapping Classes
    // =========================================================================
    // LOGIC: These private DTOs match the YAML structure using YamlDotNet's
    // underscore naming convention. They are intermediate representations
    // that get mapped to domain records with proper types and defaults.
    // =========================================================================

    private class RawSchemaDocument
    {
        public string? SchemaVersion { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public List<RawEntityType>? EntityTypes { get; set; }
        public List<RawRelationshipType>? RelationshipTypes { get; set; }
    }

    private class RawEntityType
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public List<RawProperty>? Properties { get; set; }
        public string? Extends { get; set; }
        public bool IsAbstract { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }

    private class RawRelationshipType
    {
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public object? From { get; set; }
        public object? To { get; set; }
        public List<RawProperty>? Properties { get; set; }
        public string? Cardinality { get; set; }
        public bool? Directional { get; set; }
    }

    private class RawProperty
    {
        public string Name { get; set; } = "";
        public string Type { get; set; } = "string";
        public string? Description { get; set; }
        public bool Required { get; set; }
        public object? Default { get; set; }
        public List<string>? Values { get; set; }
        public double? Min { get; set; }
        public double? Max { get; set; }
        public int? MaxLength { get; set; }
        public string? Pattern { get; set; }
    }
}

/// <summary>
/// Parsed schema document containing entity and relationship type definitions.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="SchemaDocument"/> is the output of <see cref="SchemaLoader.LoadSchemaFileAsync"/>,
/// representing a single parsed YAML schema file. Multiple schema documents
/// can be loaded into the <see cref="SchemaRegistry"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.5f as part of the Schema Registry Service.
/// </para>
/// </remarks>
internal record SchemaDocument
{
    /// <summary>Schema version from the YAML file. Defaults to "1.0".</summary>
    public string SchemaVersion { get; init; } = "1.0";

    /// <summary>Schema name from the YAML file or derived from filename.</summary>
    public string Name { get; init; } = "";

    /// <summary>Optional description from the YAML file.</summary>
    public string? Description { get; init; }

    /// <summary>Entity type definitions parsed from the YAML file.</summary>
    public IReadOnlyList<EntityTypeSchema> EntityTypes { get; init; } = Array.Empty<EntityTypeSchema>();

    /// <summary>Relationship type definitions parsed from the YAML file.</summary>
    public IReadOnlyList<RelationshipTypeSchema> RelationshipTypes { get; init; } = Array.Empty<RelationshipTypeSchema>();
}
