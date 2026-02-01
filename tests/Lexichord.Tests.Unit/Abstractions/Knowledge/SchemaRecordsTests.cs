// =============================================================================
// File: SchemaRecordsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Knowledge Graph schema records and enums.
// =============================================================================
// LOGIC: Validates the correctness of all schema abstraction types defined in
//   Lexichord.Abstractions.Contracts: PropertyType, Cardinality, PropertySchema,
//   EntityTypeSchema, RelationshipTypeSchema, SchemaValidationError,
//   SchemaValidationWarning, and SchemaValidationResult.
//
// Test Categories:
//   - PropertyType: Enum values and count
//   - Cardinality: Enum values and count
//   - PropertySchema: Defaults, required properties, constraint fields
//   - EntityTypeSchema: Defaults, required properties, hierarchy fields
//   - RelationshipTypeSchema: Defaults, required properties, from/to types
//   - SchemaValidationError: Required properties, optional fields
//   - SchemaValidationWarning: Required properties, optional fields
//   - SchemaValidationResult: IsValid logic, factory methods, warnings-only
//
// v0.4.5f: Schema Registry Service (CKVS Phase 1)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge;

/// <summary>
/// Unit tests for Knowledge Graph schema records, enums, and validation result types.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5f")]
public sealed class SchemaRecordsTests
{
    #region PropertyType Enum Tests

    [Fact]
    public void PropertyType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<PropertyType>().Should().HaveCount(8);
    }

    [Theory]
    [InlineData(PropertyType.String, 0)]
    [InlineData(PropertyType.Text, 1)]
    [InlineData(PropertyType.Number, 2)]
    [InlineData(PropertyType.Boolean, 3)]
    [InlineData(PropertyType.Enum, 4)]
    [InlineData(PropertyType.Array, 5)]
    [InlineData(PropertyType.DateTime, 6)]
    [InlineData(PropertyType.Reference, 7)]
    public void PropertyType_Values_HaveExpectedOrdinals(PropertyType value, int expected)
    {
        // Assert
        ((int)value).Should().Be(expected);
    }

    #endregion

    #region Cardinality Enum Tests

    [Fact]
    public void Cardinality_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<Cardinality>().Should().HaveCount(4);
    }

    [Theory]
    [InlineData(Cardinality.OneToOne, 0)]
    [InlineData(Cardinality.OneToMany, 1)]
    [InlineData(Cardinality.ManyToOne, 2)]
    [InlineData(Cardinality.ManyToMany, 3)]
    public void Cardinality_Values_HaveExpectedOrdinals(Cardinality value, int expected)
    {
        // Assert
        ((int)value).Should().Be(expected);
    }

    #endregion

    #region PropertySchema Tests

    [Fact]
    public void PropertySchema_DefaultValues_Correct()
    {
        // Arrange & Act
        var schema = new PropertySchema { Name = "test", Type = PropertyType.String };

        // Assert
        schema.Name.Should().Be("test");
        schema.Type.Should().Be(PropertyType.String);
        schema.Description.Should().BeNull();
        schema.Required.Should().BeFalse();
        schema.DefaultValue.Should().BeNull();
        schema.EnumValues.Should().BeNull();
        schema.MinValue.Should().BeNull();
        schema.MaxValue.Should().BeNull();
        schema.MaxLength.Should().BeNull();
        schema.Pattern.Should().BeNull();
        schema.ArrayElementType.Should().BeNull();
    }

    [Fact]
    public void PropertySchema_WithAllFields_Populated()
    {
        // Arrange & Act
        var schema = new PropertySchema
        {
            Name = "status_code",
            Type = PropertyType.Number,
            Description = "HTTP status code",
            Required = true,
            DefaultValue = "200",
            MinValue = 100,
            MaxValue = 599,
            MaxLength = 3
        };

        // Assert
        schema.Name.Should().Be("status_code");
        schema.Type.Should().Be(PropertyType.Number);
        schema.Description.Should().Be("HTTP status code");
        schema.Required.Should().BeTrue();
        schema.DefaultValue.Should().Be("200");
        schema.MinValue.Should().Be(100);
        schema.MaxValue.Should().Be(599);
        schema.MaxLength.Should().Be(3);
    }

    [Fact]
    public void PropertySchema_WithEnumValues_Populated()
    {
        // Arrange & Act
        var schema = new PropertySchema
        {
            Name = "method",
            Type = PropertyType.Enum,
            Required = true,
            EnumValues = new[] { "GET", "POST", "PUT", "DELETE" }
        };

        // Assert
        schema.EnumValues.Should().HaveCount(4);
        schema.EnumValues.Should().Contain("GET");
    }

    [Fact]
    public void PropertySchema_Equality_SameValues()
    {
        // Arrange
        var a = new PropertySchema { Name = "path", Type = PropertyType.String, Required = true };
        var b = new PropertySchema { Name = "path", Type = PropertyType.String, Required = true };

        // Assert
        a.Should().Be(b);
    }

    [Fact]
    public void PropertySchema_Equality_DifferentValues()
    {
        // Arrange
        var a = new PropertySchema { Name = "path", Type = PropertyType.String };
        var b = new PropertySchema { Name = "method", Type = PropertyType.Enum };

        // Assert
        a.Should().NotBe(b);
    }

    #endregion

    #region EntityTypeSchema Tests

    [Fact]
    public void EntityTypeSchema_DefaultValues_Correct()
    {
        // Arrange & Act
        var schema = new EntityTypeSchema
        {
            Name = "Product",
            Properties = Array.Empty<PropertySchema>()
        };

        // Assert
        schema.Name.Should().Be("Product");
        schema.Description.Should().BeNull();
        schema.Properties.Should().BeEmpty();
        schema.RequiredProperties.Should().BeEmpty();
        schema.Extends.Should().BeNull();
        schema.IsAbstract.Should().BeFalse();
        schema.Icon.Should().BeNull();
        schema.Color.Should().BeNull();
    }

    [Fact]
    public void EntityTypeSchema_WithAllFields_Populated()
    {
        // Arrange & Act
        var schema = new EntityTypeSchema
        {
            Name = "Endpoint",
            Description = "An API endpoint",
            Properties = new[]
            {
                new PropertySchema { Name = "path", Type = PropertyType.String, Required = true },
                new PropertySchema { Name = "method", Type = PropertyType.Enum, Required = true }
            },
            RequiredProperties = new[] { "path", "method" },
            Icon = "link",
            Color = "#f59e0b"
        };

        // Assert
        schema.Properties.Should().HaveCount(2);
        schema.RequiredProperties.Should().HaveCount(2);
        schema.Icon.Should().Be("link");
        schema.Color.Should().Be("#f59e0b");
    }

    [Fact]
    public void EntityTypeSchema_AbstractType_IsAbstractTrue()
    {
        // Arrange & Act
        var schema = new EntityTypeSchema
        {
            Name = "BaseEntity",
            Properties = Array.Empty<PropertySchema>(),
            IsAbstract = true,
            Extends = null
        };

        // Assert
        schema.IsAbstract.Should().BeTrue();
    }

    [Fact]
    public void EntityTypeSchema_WithInheritance_ExtendsPopulated()
    {
        // Arrange & Act
        var schema = new EntityTypeSchema
        {
            Name = "RestEndpoint",
            Properties = Array.Empty<PropertySchema>(),
            Extends = "Endpoint"
        };

        // Assert
        schema.Extends.Should().Be("Endpoint");
    }

    [Fact]
    public void EntityTypeSchema_Equality_SameValues()
    {
        // Arrange
        var props = Array.Empty<PropertySchema>();
        var a = new EntityTypeSchema { Name = "Product", Properties = props };
        var b = new EntityTypeSchema { Name = "Product", Properties = props };

        // Assert
        a.Should().Be(b);
    }

    #endregion

    #region RelationshipTypeSchema Tests

    [Fact]
    public void RelationshipTypeSchema_DefaultValues_Correct()
    {
        // Arrange & Act
        var schema = new RelationshipTypeSchema
        {
            Name = "CONTAINS",
            FromEntityTypes = new[] { "Product" },
            ToEntityTypes = new[] { "Component" }
        };

        // Assert
        schema.Name.Should().Be("CONTAINS");
        schema.Description.Should().BeNull();
        schema.Properties.Should().BeNull();
        schema.Cardinality.Should().Be(Cardinality.ManyToMany);
        schema.Directional.Should().BeTrue();
    }

    [Fact]
    public void RelationshipTypeSchema_WithAllFields_Populated()
    {
        // Arrange & Act
        var schema = new RelationshipTypeSchema
        {
            Name = "ACCEPTS",
            Description = "Endpoint accepts parameter",
            FromEntityTypes = new[] { "Endpoint" },
            ToEntityTypes = new[] { "Parameter" },
            Properties = new[]
            {
                new PropertySchema
                {
                    Name = "location",
                    Type = PropertyType.Enum,
                    EnumValues = new[] { "path", "query", "header", "body" }
                }
            },
            Cardinality = Cardinality.OneToMany,
            Directional = true
        };

        // Assert
        schema.Properties.Should().HaveCount(1);
        schema.Cardinality.Should().Be(Cardinality.OneToMany);
        schema.FromEntityTypes.Should().ContainSingle("Endpoint");
        schema.ToEntityTypes.Should().ContainSingle("Parameter");
    }

    [Fact]
    public void RelationshipTypeSchema_Bidirectional_DirectionalFalse()
    {
        // Arrange & Act
        var schema = new RelationshipTypeSchema
        {
            Name = "RELATED_TO",
            FromEntityTypes = new[] { "Concept" },
            ToEntityTypes = new[] { "Concept" },
            Directional = false
        };

        // Assert
        schema.Directional.Should().BeFalse();
    }

    #endregion

    #region SchemaValidationError Tests

    [Fact]
    public void SchemaValidationError_RequiredFields_Populated()
    {
        // Arrange & Act
        var error = new SchemaValidationError
        {
            Code = "UNKNOWN_ENTITY_TYPE",
            Message = "Entity type 'Widget' is not defined"
        };

        // Assert
        error.Code.Should().Be("UNKNOWN_ENTITY_TYPE");
        error.Message.Should().Contain("Widget");
        error.PropertyName.Should().BeNull();
        error.ActualValue.Should().BeNull();
    }

    [Fact]
    public void SchemaValidationError_WithPropertyContext_Populated()
    {
        // Arrange & Act
        var error = new SchemaValidationError
        {
            Code = "TYPE_MISMATCH",
            Message = "Property 'status_code' must be a number",
            PropertyName = "status_code",
            ActualValue = "not_a_number"
        };

        // Assert
        error.PropertyName.Should().Be("status_code");
        error.ActualValue.Should().Be("not_a_number");
    }

    [Fact]
    public void SchemaValidationError_Equality_SameValues()
    {
        // Arrange
        var a = new SchemaValidationError { Code = "X", Message = "Y" };
        var b = new SchemaValidationError { Code = "X", Message = "Y" };

        // Assert
        a.Should().Be(b);
    }

    #endregion

    #region SchemaValidationWarning Tests

    [Fact]
    public void SchemaValidationWarning_RequiredFields_Populated()
    {
        // Arrange & Act
        var warning = new SchemaValidationWarning
        {
            Code = "UNKNOWN_PROPERTY",
            Message = "Property 'extra' is not defined"
        };

        // Assert
        warning.Code.Should().Be("UNKNOWN_PROPERTY");
        warning.PropertyName.Should().BeNull();
    }

    [Fact]
    public void SchemaValidationWarning_WithPropertyName_Populated()
    {
        // Arrange & Act
        var warning = new SchemaValidationWarning
        {
            Code = "UNKNOWN_PROPERTY",
            Message = "Extra property",
            PropertyName = "extra"
        };

        // Assert
        warning.PropertyName.Should().Be("extra");
    }

    #endregion

    #region SchemaValidationResult Tests

    [Fact]
    public void SchemaValidationResult_DefaultInstance_IsValid()
    {
        // Arrange & Act
        var result = new SchemaValidationResult();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void SchemaValidationResult_Valid_FactoryMethod_ReturnsValid()
    {
        // Arrange & Act
        var result = SchemaValidationResult.Valid();

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void SchemaValidationResult_Invalid_FactoryMethod_ReturnsInvalid()
    {
        // Arrange
        var error = new SchemaValidationError { Code = "TEST", Message = "Test error" };

        // Act
        var result = SchemaValidationResult.Invalid(error);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Errors[0].Code.Should().Be("TEST");
    }

    [Fact]
    public void SchemaValidationResult_Invalid_MultipleErrors()
    {
        // Arrange
        var error1 = new SchemaValidationError { Code = "A", Message = "A" };
        var error2 = new SchemaValidationError { Code = "B", Message = "B" };

        // Act
        var result = SchemaValidationResult.Invalid(error1, error2);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void SchemaValidationResult_WarningsOnly_StillValid()
    {
        // Arrange & Act
        var result = new SchemaValidationResult
        {
            Warnings = new[]
            {
                new SchemaValidationWarning { Code = "UNKNOWN_PROPERTY", Message = "Extra prop" }
            }
        };

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle();
    }

    [Fact]
    public void SchemaValidationResult_ErrorsAndWarnings_NotValid()
    {
        // Arrange & Act
        var result = new SchemaValidationResult
        {
            Errors = new[]
            {
                new SchemaValidationError { Code = "ERR", Message = "Error" }
            },
            Warnings = new[]
            {
                new SchemaValidationWarning { Code = "WARN", Message = "Warning" }
            }
        };

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle();
        result.Warnings.Should().ContainSingle();
    }

    #endregion
}
