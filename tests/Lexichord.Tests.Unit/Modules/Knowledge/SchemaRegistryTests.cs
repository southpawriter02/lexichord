// =============================================================================
// File: SchemaRegistryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the SchemaRegistry orchestrator.
// =============================================================================
// LOGIC: Validates the SchemaRegistry's loading, querying, validation delegation,
//   and lifecycle behaviors. Tests cover schema loading from directories,
//   duplicate handling, version tracking, case-insensitive lookups, reload,
//   and logging verification.
//
// Test Categories:
//   - Constructor validation
//   - LoadSchemasAsync: valid, empty dir, invalid YAML, duplicates, versions
//   - GetEntityType / GetRelationshipType: known, unknown, case-insensitive
//   - GetValidRelationships: matches, no matches, case-insensitive
//   - ReloadAsync: with/without prior load
//   - SchemaVersion: default, after load
//   - Logging verification via FakeLogger
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Schema;
using Lexichord.Tests.Unit.TestUtilities;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="SchemaRegistry"/> orchestrator.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5f")]
public sealed class SchemaRegistryTests : IDisposable
{
    private readonly FakeLogger<SchemaRegistry> _logger = new();
    private readonly SchemaRegistry _registry;
    private readonly string _tempDir;

    public SchemaRegistryTests()
    {
        _registry = new SchemaRegistry(_logger);
        _tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-registry-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    #region Helper Methods

    private async Task WriteYaml(string fileName, string yaml)
    {
        await File.WriteAllTextAsync(Path.Combine(_tempDir, fileName), yaml);
    }

    private static string BasicSchema => """
        schema_version: "1.0"
        name: "Test Schema"
        entity_types:
          - name: Product
            description: "A product"
            properties:
              - name: name
                type: string
                required: true
          - name: Component
            description: "A component"
            properties:
              - name: name
                type: string
                required: true
        relationship_types:
          - name: CONTAINS
            from: [Product]
            to: [Component]
            cardinality: one_to_many
        """;

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SchemaRegistry(null!));
    }

    [Fact]
    public void Constructor_ValidLogger_InitializesEmpty()
    {
        // Assert
        _registry.EntityTypes.Should().BeEmpty();
        _registry.RelationshipTypes.Should().BeEmpty();
        _registry.SchemaVersion.Should().Be("0.0.0");
    }

    #endregion

    #region LoadSchemasAsync Tests

    [Fact]
    public async Task LoadSchemasAsync_ValidYaml_RegistersTypes()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().HaveCount(2);
        _registry.RelationshipTypes.Should().ContainSingle();
    }

    [Fact]
    public async Task LoadSchemasAsync_EmptyDirectory_NoTypesRegistered()
    {
        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().BeEmpty();
        _registry.RelationshipTypes.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadSchemasAsync_NonexistentDirectory_LogsWarning()
    {
        // Arrange
        var nonexistent = Path.Combine(_tempDir, "does-not-exist");

        // Act
        await _registry.LoadSchemasAsync(nonexistent);

        // Assert
        _registry.EntityTypes.Should().BeEmpty();
        _logger.Logs.Should().Contain(l =>
            l.Level == LogLevel.Warning && l.Message.Contains("does not exist"));
    }

    [Fact]
    public async Task LoadSchemasAsync_InvalidYaml_LogsErrorContinues()
    {
        // Arrange
        await WriteYaml("valid.yaml", BasicSchema);
        await WriteYaml("invalid.yaml", "{{bad yaml content");

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().HaveCount(2); // Valid file still loaded
        _logger.Logs.Should().Contain(l => l.Level == LogLevel.Error);
    }

    [Fact]
    public async Task LoadSchemasAsync_DuplicateEntityType_FirstWins_LogsWarning()
    {
        // Arrange
        await WriteYaml("schema1.yaml", """
            schema_version: "1.0"
            name: "Schema 1"
            entity_types:
              - name: Product
                description: "First Product"
                properties:
                  - name: name
                    type: string
            """);
        await WriteYaml("schema2.yaml", """
            schema_version: "1.0"
            name: "Schema 2"
            entity_types:
              - name: Product
                description: "Second Product"
                properties:
                  - name: name
                    type: string
            """);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().ContainSingle();
        _registry.EntityTypes["Product"].Description.Should().Be("First Product");
        _logger.Logs.Should().Contain(l =>
            l.Level == LogLevel.Warning && l.Message.Contains("Duplicate"));
    }

    [Fact]
    public async Task LoadSchemasAsync_TracksHighestVersion()
    {
        // Arrange
        await WriteYaml("v1.yaml", """
            schema_version: "1.0"
            name: "V1"
            entity_types:
              - name: TypeA
                properties: []
            """);
        await WriteYaml("v2.yaml", """
            schema_version: "2.0"
            name: "V2"
            entity_types:
              - name: TypeB
                properties: []
            """);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.SchemaVersion.Should().Be("2.0");
    }

    [Fact]
    public async Task LoadSchemasAsync_YmlExtension_AlsoLoaded()
    {
        // Arrange
        await WriteYaml("test.yml", """
            schema_version: "1.0"
            name: "YML Test"
            entity_types:
              - name: YmlType
                properties:
                  - name: name
                    type: string
            """);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().ContainKey("YmlType");
    }

    [Fact]
    public async Task LoadSchemasAsync_ClearsExistingSchemas()
    {
        // Arrange — Load initial schemas
        await WriteYaml("first.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);
        _registry.EntityTypes.Should().HaveCount(2);

        // Arrange — Replace with different schema
        File.Delete(Path.Combine(_tempDir, "first.yaml"));
        await WriteYaml("second.yaml", """
            schema_version: "1.0"
            name: "New Schema"
            entity_types:
              - name: NewType
                properties: []
            """);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().ContainSingle();
        _registry.EntityTypes.Should().ContainKey("NewType");
        _registry.EntityTypes.Should().NotContainKey("Product");
    }

    [Fact]
    public async Task LoadSchemasAsync_LogsInfoOnStartAndComplete()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _logger.Logs.Should().Contain(l =>
            l.Level == LogLevel.Information && l.Message.Contains("Loading schemas"));
        _logger.Logs.Should().Contain(l =>
            l.Level == LogLevel.Information && l.Message.Contains("Schema loading complete"));
    }

    #endregion

    #region GetEntityType Tests

    [Fact]
    public async Task GetEntityType_KnownType_ReturnsSchema()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var schema = _registry.GetEntityType("Product");

        // Assert
        schema.Should().NotBeNull();
        schema!.Name.Should().Be("Product");
    }

    [Fact]
    public async Task GetEntityType_UnknownType_ReturnsNull()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var schema = _registry.GetEntityType("Widget");

        // Assert
        schema.Should().BeNull();
    }

    [Fact]
    public async Task GetEntityType_CaseInsensitive_ReturnsSchema()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var schema = _registry.GetEntityType("product");

        // Assert
        schema.Should().NotBeNull();
        schema!.Name.Should().Be("Product");
    }

    #endregion

    #region GetRelationshipType Tests

    [Fact]
    public async Task GetRelationshipType_KnownType_ReturnsSchema()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var schema = _registry.GetRelationshipType("CONTAINS");

        // Assert
        schema.Should().NotBeNull();
        schema!.Name.Should().Be("CONTAINS");
    }

    [Fact]
    public async Task GetRelationshipType_UnknownType_ReturnsNull()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var schema = _registry.GetRelationshipType("LINKS_TO");

        // Assert
        schema.Should().BeNull();
    }

    [Fact]
    public async Task GetRelationshipType_CaseInsensitive_ReturnsSchema()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var schema = _registry.GetRelationshipType("contains");

        // Assert
        schema.Should().NotBeNull();
    }

    #endregion

    #region GetValidRelationships Tests

    [Fact]
    public async Task GetValidRelationships_MatchingTypes_ReturnsRelationships()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var relationships = _registry.GetValidRelationships("Product", "Component");

        // Assert
        relationships.Should().ContainSingle("CONTAINS");
    }

    [Fact]
    public async Task GetValidRelationships_NoMatch_ReturnsEmpty()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var relationships = _registry.GetValidRelationships("Component", "Product");

        // Assert
        relationships.Should().BeEmpty();
    }

    [Fact]
    public async Task GetValidRelationships_CaseInsensitive_ReturnsRelationships()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);

        // Act
        var relationships = _registry.GetValidRelationships("product", "component");

        // Assert
        relationships.Should().ContainSingle("CONTAINS");
    }

    #endregion

    #region ReloadAsync Tests

    [Fact]
    public async Task ReloadAsync_AfterLoad_ReloadsSchemas()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);
        _registry.EntityTypes.Should().HaveCount(2);

        // Modify the directory content
        await WriteYaml("test.yaml", """
            schema_version: "1.0"
            name: "Updated"
            entity_types:
              - name: NewProduct
                properties:
                  - name: name
                    type: string
            """);

        // Act
        await _registry.ReloadAsync();

        // Assert
        _registry.EntityTypes.Should().ContainSingle();
        _registry.EntityTypes.Should().ContainKey("NewProduct");
    }

    [Fact]
    public async Task ReloadAsync_WithoutPriorLoad_NoOp()
    {
        // Act
        await _registry.ReloadAsync();

        // Assert
        _registry.EntityTypes.Should().BeEmpty();
        _logger.Logs.Should().Contain(l =>
            l.Level == LogLevel.Debug && l.Message.Contains("no schema directory"));
    }

    #endregion

    #region SchemaVersion Tests

    [Fact]
    public void SchemaVersion_BeforeLoad_DefaultValue()
    {
        // Assert
        _registry.SchemaVersion.Should().Be("0.0.0");
    }

    [Fact]
    public async Task SchemaVersion_AfterLoad_ReflectsHighest()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema); // version "1.0"

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.SchemaVersion.Should().Be("1.0");
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task LoadSchemasAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _registry.LoadSchemasAsync(_tempDir, cts.Token));
    }

    #endregion

    #region Validation Delegation Tests

    [Fact]
    public async Task ValidateEntity_DelegatesToValidator()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);
        var entity = new KnowledgeEntity
        {
            Type = "Product",
            Name = "Test Product",
            Properties = new() { ["name"] = "Test Product" }
        };

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRelationship_DelegatesToValidator()
    {
        // Arrange
        await WriteYaml("test.yaml", BasicSchema);
        await _registry.LoadSchemasAsync(_tempDir);
        var rel = new KnowledgeRelationship
        {
            Type = "CONTAINS",
            FromEntityId = Guid.NewGuid(),
            ToEntityId = Guid.NewGuid()
        };
        var from = new KnowledgeEntity { Type = "Product", Name = "P1" };
        var to = new KnowledgeEntity { Type = "Component", Name = "C1" };

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Multiple Files Tests

    [Fact]
    public async Task LoadSchemasAsync_MultipleFiles_MergesTypes()
    {
        // Arrange
        await WriteYaml("entities.yaml", """
            schema_version: "1.0"
            name: "Entities"
            entity_types:
              - name: TypeA
                properties: []
              - name: TypeB
                properties: []
            """);
        await WriteYaml("relationships.yaml", """
            schema_version: "1.0"
            name: "Relationships"
            relationship_types:
              - name: LINKS
                from: [TypeA]
                to: [TypeB]
            """);

        // Act
        await _registry.LoadSchemasAsync(_tempDir);

        // Assert
        _registry.EntityTypes.Should().HaveCount(2);
        _registry.RelationshipTypes.Should().ContainSingle();
    }

    #endregion
}
