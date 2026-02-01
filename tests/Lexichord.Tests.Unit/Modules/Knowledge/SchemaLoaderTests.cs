// =============================================================================
// File: SchemaLoaderTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the YAML schema file parser.
// =============================================================================
// LOGIC: Validates that SchemaLoader correctly parses YAML schema files into
//   domain records, handles edge cases, and produces appropriate defaults for
//   missing or invalid values.
//
// Test Categories:
//   - Valid YAML parsing (entities, relationships, properties, constraints)
//   - From/To as string vs list handling
//   - Missing fields default handling
//   - Unknown property type → defaults to String
//   - Invalid cardinality → defaults to ManyToMany
//   - Error handling (empty file, invalid YAML, nonexistent file)
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Schema;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="SchemaLoader"/> YAML parsing.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5f")]
public sealed class SchemaLoaderTests : IDisposable
{
    private readonly FakeLogger<SchemaRegistry> _logger = new();
    private readonly SchemaLoader _loader;
    private readonly string _tempDir;

    public SchemaLoaderTests()
    {
        _loader = new SchemaLoader(_logger);
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-schema-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region Helper Methods

    private async Task<SchemaDocument> LoadYaml(string yaml)
    {
        var filePath = Path.Combine(_tempDir, $"test-{Guid.NewGuid():N}.yaml");
        await File.WriteAllTextAsync(filePath, yaml);
        return await _loader.LoadSchemaFileAsync(filePath);
    }

    #endregion

    #region Valid YAML Parsing Tests

    [Fact]
    public async Task LoadSchemaFileAsync_ValidEntityTypes_ParsedCorrectly()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test Schema"
            description: "A test schema"
            entity_types:
              - name: Product
                description: "A product"
                icon: "package"
                color: "#3b82f6"
                properties:
                  - name: name
                    type: string
                    required: true
                    max_length: 200
                  - name: version
                    type: string
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.SchemaVersion.Should().Be("1.0");
        doc.Name.Should().Be("Test Schema");
        doc.Description.Should().Be("A test schema");
        doc.EntityTypes.Should().ContainSingle();

        var product = doc.EntityTypes[0];
        product.Name.Should().Be("Product");
        product.Description.Should().Be("A product");
        product.Icon.Should().Be("package");
        product.Color.Should().Be("#3b82f6");
        product.Properties.Should().HaveCount(2);
        product.RequiredProperties.Should().ContainSingle("name");
    }

    [Fact]
    public async Task LoadSchemaFileAsync_ValidRelationshipTypes_ParsedCorrectly()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            relationship_types:
              - name: CONTAINS
                description: "Parent contains child"
                from:
                  - Product
                to:
                  - Component
                  - Endpoint
                cardinality: one_to_many
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.RelationshipTypes.Should().ContainSingle();
        var rel = doc.RelationshipTypes[0];
        rel.Name.Should().Be("CONTAINS");
        rel.FromEntityTypes.Should().ContainSingle("Product");
        rel.ToEntityTypes.Should().HaveCount(2);
        rel.Cardinality.Should().Be(Cardinality.OneToMany);
        rel.Directional.Should().BeTrue();
    }

    [Fact]
    public async Task LoadSchemaFileAsync_PropertyConstraints_ParsedCorrectly()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            entity_types:
              - name: Response
                properties:
                  - name: status_code
                    type: number
                    required: true
                    min: 100
                    max: 599
                  - name: method
                    type: enum
                    values: [GET, POST, PUT, DELETE]
                  - name: path
                    type: string
                    pattern: "^\\/[\\w\\-\\/]*$"
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        var props = doc.EntityTypes[0].Properties;
        props.Should().HaveCount(3);

        var statusCode = props.First(p => p.Name == "status_code");
        statusCode.Type.Should().Be(PropertyType.Number);
        statusCode.MinValue.Should().Be(100);
        statusCode.MaxValue.Should().Be(599);

        var method = props.First(p => p.Name == "method");
        method.Type.Should().Be(PropertyType.Enum);
        method.EnumValues.Should().HaveCount(4);
        method.EnumValues.Should().Contain("GET");

        var path = props.First(p => p.Name == "path");
        path.Pattern.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task LoadSchemaFileAsync_FromToAsSingleString_NormalizedToList()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            relationship_types:
              - name: EXPOSES
                from: Component
                to: Endpoint
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        var rel = doc.RelationshipTypes[0];
        rel.FromEntityTypes.Should().ContainSingle("Component");
        rel.ToEntityTypes.Should().ContainSingle("Endpoint");
    }

    [Fact]
    public async Task LoadSchemaFileAsync_PropertyDefaultValue_ParsedCorrectly()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            entity_types:
              - name: Endpoint
                properties:
                  - name: deprecated
                    type: boolean
                    default: false
                  - name: content_type
                    type: string
                    default: "application/json"
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        var props = doc.EntityTypes[0].Properties;
        props.First(p => p.Name == "deprecated").DefaultValue.Should().Be("false");
        props.First(p => p.Name == "content_type").DefaultValue.Should().Be("application/json");
    }

    [Fact]
    public async Task LoadSchemaFileAsync_BidirectionalRelationship_DirectionalFalse()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            relationship_types:
              - name: RELATED_TO
                from: [Concept]
                to: [Concept]
                directional: false
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.RelationshipTypes[0].Directional.Should().BeFalse();
    }

    #endregion

    #region Default Handling Tests

    [Fact]
    public async Task LoadSchemaFileAsync_MissingSchemaVersion_DefaultsTo1_0()
    {
        // Arrange
        var yaml = """
            name: "Test"
            entity_types:
              - name: Widget
                properties: []
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public async Task LoadSchemaFileAsync_UnknownPropertyType_DefaultsToString()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            entity_types:
              - name: Widget
                properties:
                  - name: custom
                    type: foobar
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.EntityTypes[0].Properties[0].Type.Should().Be(PropertyType.String);
    }

    [Fact]
    public async Task LoadSchemaFileAsync_InvalidCardinality_DefaultsToManyToMany()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            relationship_types:
              - name: LINKS
                from: [A]
                to: [B]
                cardinality: invalid_value
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.RelationshipTypes[0].Cardinality.Should().Be(Cardinality.ManyToMany);
    }

    [Fact]
    public async Task LoadSchemaFileAsync_NoEntityTypes_EmptyList()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Empty"
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        doc.EntityTypes.Should().BeEmpty();
        doc.RelationshipTypes.Should().BeEmpty();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task LoadSchemaFileAsync_EmptyFile_ReturnsEmptyDocument()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "empty.yaml");
        await File.WriteAllTextAsync(filePath, "");

        // Act
        var doc = await _loader.LoadSchemaFileAsync(filePath);

        // Assert
        doc.EntityTypes.Should().BeEmpty();
        doc.RelationshipTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSchemaFileAsync_NonexistentFile_ThrowsFileNotFound()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "does-not-exist.yaml");

        // Act & Assert
        await Assert.ThrowsAsync<FileNotFoundException>(
            () => _loader.LoadSchemaFileAsync(filePath));
    }

    [Fact]
    public async Task LoadSchemaFileAsync_MalformedYaml_ThrowsYamlException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "bad.yaml");
        await File.WriteAllTextAsync(filePath, "{{invalid: yaml: content:");

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(
            () => _loader.LoadSchemaFileAsync(filePath));
    }

    [Fact]
    public async Task LoadSchemaFileAsync_RelationshipWithProperties_ParsedCorrectly()
    {
        // Arrange
        var yaml = """
            schema_version: "1.0"
            name: "Test"
            relationship_types:
              - name: ACCEPTS
                from: [Endpoint]
                to: [Parameter]
                properties:
                  - name: location
                    type: enum
                    values: [path, query, header, body]
            """;

        // Act
        var doc = await LoadYaml(yaml);

        // Assert
        var rel = doc.RelationshipTypes[0];
        rel.Properties.Should().ContainSingle();
        rel.Properties![0].Name.Should().Be("location");
        rel.Properties[0].Type.Should().Be(PropertyType.Enum);
        rel.Properties[0].EnumValues.Should().HaveCount(4);
    }

    #endregion
}
