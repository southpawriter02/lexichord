// =============================================================================
// File: SchemaValidatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for the schema validation logic.
// =============================================================================
// LOGIC: Validates that SchemaValidator correctly enforces all validation rules
//   for entities and relationships. Tests cover all error codes and warning
//   codes defined in the Schema Registry specification.
//
// Test Categories:
//   - Entity type checks (unknown, abstract, empty name)
//   - Required property validation (missing, null, whitespace)
//   - Property type validation (string, number, boolean, enum)
//   - Constraint validation (maxLength, pattern, min/max range)
//   - Unknown property warnings
//   - Relationship validation (unknown type, invalid from/to, valid, required props)
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Knowledge.Schema;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="SchemaValidator"/> validation logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5f")]
public sealed class SchemaValidatorTests
{
    private readonly FakeLogger<SchemaRegistry> _logger = new();
    private readonly SchemaRegistry _registry;

    public SchemaValidatorTests()
    {
        _registry = new SchemaRegistry(_logger);
    }

    #region Test Helpers

    /// <summary>
    /// Loads a standard test schema into the registry with common entity and relationship types.
    /// </summary>
    private async Task LoadTestSchema()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"lexichord-validator-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var yaml = """
                schema_version: "1.0"
                name: "Test Schema"
                entity_types:
                  - name: Product
                    description: "A product"
                    properties:
                      - name: name
                        type: string
                        required: true
                        max_length: 200
                      - name: version
                        type: string
                        pattern: "^\\d+\\.\\d+$"
                      - name: description
                        type: text
                  - name: Endpoint
                    description: "An API endpoint"
                    properties:
                      - name: path
                        type: string
                        required: true
                      - name: method
                        type: enum
                        values: [GET, POST, PUT, DELETE]
                        required: true
                      - name: deprecated
                        type: boolean
                      - name: priority
                        type: number
                        min: 1
                        max: 10
                  - name: AbstractBase
                    is_abstract: true
                    properties:
                      - name: name
                        type: string
                        required: true
                  - name: Concept
                    description: "A concept"
                    properties:
                      - name: name
                        type: string
                        required: true
                      - name: definition
                        type: text
                        required: true
                relationship_types:
                  - name: CONTAINS
                    from: [Product]
                    to: [Endpoint]
                    cardinality: one_to_many
                  - name: RELATED_TO
                    from: [Concept]
                    to: [Concept]
                    directional: false
                  - name: ACCEPTS
                    from: [Endpoint]
                    to: [Concept]
                    properties:
                      - name: location
                        type: enum
                        values: [path, query, header, body]
                        required: true
                """;

            var filePath = Path.Combine(tempDir, "test.yaml");
            await File.WriteAllTextAsync(filePath, yaml);
            await _registry.LoadSchemasAsync(tempDir);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    private static KnowledgeEntity MakeEntity(string type, string name, Dictionary<string, object>? props = null) =>
        new()
        {
            Type = type,
            Name = name,
            Properties = props ?? new()
        };

    private static KnowledgeRelationship MakeRelationship(string type, Dictionary<string, object>? props = null) =>
        new()
        {
            Type = type,
            FromEntityId = Guid.NewGuid(),
            ToEntityId = Guid.NewGuid(),
            Properties = props ?? new()
        };

    #endregion

    #region Entity Type Checks

    [Fact]
    public async Task ValidateEntity_UnknownType_ReturnsUnknownEntityTypeError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Widget", "Test Widget");

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_ENTITY_TYPE");
    }

    [Fact]
    public async Task ValidateEntity_AbstractType_ReturnsAbstractTypeError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("AbstractBase", "Test", new() { ["name"] = "Test" });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "ABSTRACT_TYPE");
    }

    [Fact]
    public async Task ValidateEntity_EmptyName_ReturnsNameRequiredError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "", new() { ["name"] = "Test" });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "NAME_REQUIRED");
    }

    [Fact]
    public async Task ValidateEntity_WhitespaceName_ReturnsNameRequiredError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "   ", new() { ["name"] = "Test" });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e => e.Code == "NAME_REQUIRED");
    }

    #endregion

    #region Required Property Validation

    [Fact]
    public async Task ValidateEntity_MissingRequiredProperty_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "My Product");
        // "name" is required but not in Properties

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "REQUIRED_PROPERTY_MISSING" && e.PropertyName == "name");
    }

    [Fact]
    public async Task ValidateEntity_RequiredPropertyEmpty_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "My Product", new() { ["name"] = "" });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "REQUIRED_PROPERTY_MISSING" && e.PropertyName == "name");
    }

    [Fact]
    public async Task ValidateEntity_RequiredPropertyWhitespace_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "My Product", new() { ["name"] = "   " });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "REQUIRED_PROPERTY_MISSING" && e.PropertyName == "name");
    }

    [Fact]
    public async Task ValidateEntity_AllRequiredPropertiesPresent_NoRequiredPropertyErrors()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "My Product", new() { ["name"] = "My Product" });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "REQUIRED_PROPERTY_MISSING");
    }

    #endregion

    #region Property Type Validation

    [Fact]
    public async Task ValidateEntity_StringPropertyWithNonString_ReturnsTypeMismatch()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "P1", new() { ["name"] = "Valid", ["version"] = 123 });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "TYPE_MISMATCH" && e.PropertyName == "version");
    }

    [Fact]
    public async Task ValidateEntity_NumberPropertyWithString_ReturnsTypeMismatch()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["priority"] = "not_a_number"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "TYPE_MISMATCH" && e.PropertyName == "priority");
    }

    [Fact]
    public async Task ValidateEntity_BooleanPropertyWithString_ReturnsTypeMismatch()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["deprecated"] = "yes"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "TYPE_MISMATCH" && e.PropertyName == "deprecated");
    }

    [Fact]
    public async Task ValidateEntity_InvalidEnumValue_ReturnsInvalidEnumError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "PATCH /test", new()
        {
            ["path"] = "/test",
            ["method"] = "PATCH"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "INVALID_ENUM_VALUE" && e.PropertyName == "method");
    }

    [Fact]
    public async Task ValidateEntity_ValidEnumValue_CaseInsensitive_NoError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "get"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "INVALID_ENUM_VALUE");
    }

    [Fact]
    public async Task ValidateEntity_ValidNumberProperty_NoTypeMismatch()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["priority"] = 5
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "TYPE_MISMATCH");
    }

    [Fact]
    public async Task ValidateEntity_ValidBooleanProperty_NoTypeMismatch()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["deprecated"] = false
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "TYPE_MISMATCH");
    }

    #endregion

    #region Constraint Validation

    [Fact]
    public async Task ValidateEntity_MaxLengthExceeded_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var longName = new string('x', 201);
        var entity = MakeEntity("Product", "P1", new() { ["name"] = longName });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "MAX_LENGTH_EXCEEDED" && e.PropertyName == "name");
    }

    [Fact]
    public async Task ValidateEntity_MaxLengthNotExceeded_NoError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "P1", new() { ["name"] = "Valid Name" });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "MAX_LENGTH_EXCEEDED");
    }

    [Fact]
    public async Task ValidateEntity_PatternMismatch_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "P1", new()
        {
            ["name"] = "Product",
            ["version"] = "not-a-version"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "PATTERN_MISMATCH" && e.PropertyName == "version");
    }

    [Fact]
    public async Task ValidateEntity_PatternMatch_NoError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "P1", new()
        {
            ["name"] = "Product",
            ["version"] = "1.0"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "PATTERN_MISMATCH");
    }

    [Fact]
    public async Task ValidateEntity_BelowMinimum_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["priority"] = 0
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "BELOW_MINIMUM" && e.PropertyName == "priority");
    }

    [Fact]
    public async Task ValidateEntity_AboveMaximum_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["priority"] = 11
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().Contain(e =>
            e.Code == "ABOVE_MAXIMUM" && e.PropertyName == "priority");
    }

    [Fact]
    public async Task ValidateEntity_NumberInRange_NoError()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /test", new()
        {
            ["path"] = "/test",
            ["method"] = "GET",
            ["priority"] = 5
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Errors.Should().NotContain(e => e.Code == "BELOW_MINIMUM");
        result.Errors.Should().NotContain(e => e.Code == "ABOVE_MAXIMUM");
    }

    #endregion

    #region Unknown Property Warnings

    [Fact]
    public async Task ValidateEntity_UnknownProperty_ProducesWarning()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "P1", new()
        {
            ["name"] = "Product",
            ["extra_field"] = "something"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.Warnings.Should().Contain(w =>
            w.Code == "UNKNOWN_PROPERTY" && w.PropertyName == "extra_field");
    }

    [Fact]
    public async Task ValidateEntity_UnknownProperty_DoesNotCauseInvalid()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "P1", new()
        {
            ["name"] = "Product",
            ["extra"] = "value"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        // Should be valid (no errors) even though there's a warning
        result.Errors.Should().NotContain(e => e.PropertyName == "extra");
    }

    #endregion

    #region Valid Entity Tests

    [Fact]
    public async Task ValidateEntity_FullyValidEntity_IsValid()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Endpoint", "GET /users", new()
        {
            ["path"] = "/users",
            ["method"] = "GET",
            ["deprecated"] = false,
            ["priority"] = 5
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateEntity_MinimalValidEntity_IsValid()
    {
        // Arrange
        await LoadTestSchema();
        var entity = MakeEntity("Product", "My Product", new()
        {
            ["name"] = "My Product"
        });

        // Act
        var result = _registry.ValidateEntity(entity);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Relationship Validation

    [Fact]
    public async Task ValidateRelationship_UnknownType_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("UNKNOWN_REL");
        var from = MakeEntity("Product", "P1");
        var to = MakeEntity("Endpoint", "E1");

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Code == "UNKNOWN_RELATIONSHIP_TYPE");
    }

    [Fact]
    public async Task ValidateRelationship_InvalidFromType_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("CONTAINS");
        var from = MakeEntity("Endpoint", "E1"); // Should be Product
        var to = MakeEntity("Endpoint", "E2");

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_FROM_TYPE");
    }

    [Fact]
    public async Task ValidateRelationship_InvalidToType_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("CONTAINS");
        var from = MakeEntity("Product", "P1");
        var to = MakeEntity("Product", "P2"); // Should be Endpoint

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_TO_TYPE");
    }

    [Fact]
    public async Task ValidateRelationship_ValidRelationship_IsValid()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("CONTAINS");
        var from = MakeEntity("Product", "P1");
        var to = MakeEntity("Endpoint", "GET /test");

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRelationship_MissingRequiredProperty_ReturnsError()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("ACCEPTS"); // "location" is required
        var from = MakeEntity("Endpoint", "GET /test");
        var to = MakeEntity("Concept", "Auth");

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.Code == "REQUIRED_PROPERTY_MISSING" && e.PropertyName == "location");
    }

    [Fact]
    public async Task ValidateRelationship_RequiredPropertyPresent_NoError()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("ACCEPTS", new() { ["location"] = "query" });
        var from = MakeEntity("Endpoint", "GET /test");
        var to = MakeEntity("Concept", "Auth");

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateRelationship_BothFromAndToInvalid_ReturnsBothErrors()
    {
        // Arrange
        await LoadTestSchema();
        var rel = MakeRelationship("CONTAINS");
        var from = MakeEntity("Concept", "C1"); // Not Product
        var to = MakeEntity("Concept", "C2");   // Not Endpoint

        // Act
        var result = _registry.ValidateRelationship(rel, from, to);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "INVALID_FROM_TYPE");
        result.Errors.Should().Contain(e => e.Code == "INVALID_TO_TYPE");
    }

    #endregion
}
