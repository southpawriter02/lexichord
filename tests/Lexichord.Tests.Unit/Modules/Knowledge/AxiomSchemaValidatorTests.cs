// =============================================================================
// File: AxiomSchemaValidatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomSchemaValidator schema validation.
// =============================================================================
// LOGIC: Tests target type validation against ISchemaRegistry.
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="AxiomSchemaValidator"/> schema validation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6g")]
public class AxiomSchemaValidatorTests
{
    private readonly Mock<ISchemaRegistry> _mockSchemaRegistry;
    private readonly AxiomSchemaValidator _validator;

    public AxiomSchemaValidatorTests()
    {
        _mockSchemaRegistry = new Mock<ISchemaRegistry>();
        _validator = new AxiomSchemaValidator(_mockSchemaRegistry.Object);
    }

    #region Constructor Validation

    [Fact]
    public void Constructor_WithNullSchemaRegistry_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() => new AxiomSchemaValidator(null!));
        Assert.Equal("schemaRegistry", ex.ParamName);
    }

    #endregion

    #region Target Type Validation

    [Fact]
    public void Validate_WithKnownTargetType_ReturnsNoErrors()
    {
        // Arrange
        _mockSchemaRegistry.Setup(x => x.GetEntityType("Concept"))
            .Returns(CreateEntityTypeSchema("Concept"));

        var axioms = new List<Axiom>
        {
            CreateTestAxiom("test-001", "Concept")
        };

        // Act
        var result = _validator.Validate(axioms);

        // Assert
        Assert.NotNull(result);
        Assert.DoesNotContain(result.Errors, e => e.Severity == LoadErrorSeverity.Error);
        Assert.Empty(result.InvalidAxiomIds);
        Assert.Empty(result.UnknownTargetTypes);
    }

    [Fact]
    public void Validate_WithUnknownTargetType_ReturnsWarning()
    {
        // Arrange
        _mockSchemaRegistry.Setup(x => x.GetEntityType("UnknownType"))
            .Returns((EntityTypeSchema?)null);
        _mockSchemaRegistry.Setup(x => x.GetRelationshipType("UnknownType"))
            .Returns((RelationshipTypeSchema?)null);

        var axioms = new List<Axiom>
        {
            CreateTestAxiom("test-001", "UnknownType")
        };

        // Act
        var result = _validator.Validate(axioms);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("UnknownType", result.UnknownTargetTypes);
        // Unknown types generate warnings, not errors
        Assert.Contains(result.Errors, e => e.Severity == LoadErrorSeverity.Warning);
    }

    [Fact]
    public void Validate_WithMultipleAxioms_ValidatesEach()
    {
        // Arrange
        _mockSchemaRegistry.Setup(x => x.GetEntityType("Concept"))
            .Returns(CreateEntityTypeSchema("Concept"));
        _mockSchemaRegistry.Setup(x => x.GetEntityType("Endpoint"))
            .Returns(CreateEntityTypeSchema("Endpoint"));
        _mockSchemaRegistry.Setup(x => x.GetEntityType("UnknownType"))
            .Returns((EntityTypeSchema?)null);
        _mockSchemaRegistry.Setup(x => x.GetRelationshipType("UnknownType"))
            .Returns((RelationshipTypeSchema?)null);

        var axioms = new List<Axiom>
        {
            CreateTestAxiom("test-001", "Concept"),
            CreateTestAxiom("test-002", "Endpoint"),
            CreateTestAxiom("test-003", "UnknownType")
        };

        // Act
        var result = _validator.Validate(axioms);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.UnknownTargetTypes);
        Assert.Contains("UnknownType", result.UnknownTargetTypes);
    }

    [Fact]
    public void Validate_WithRelationshipTargetKind_ChecksRelationshipRegistry()
    {
        // Arrange
        _mockSchemaRegistry.Setup(x => x.GetRelationshipType("REFERENCES"))
            .Returns(CreateRelationshipTypeSchema("REFERENCES"));

        var axiom = new Axiom
        {
            Id = "rel-test-001",
            Name = "Relationship Test",
            TargetType = "REFERENCES",
            TargetKind = AxiomTargetKind.Relationship,
            Rules = new List<AxiomRule>
            {
                new() { Constraint = AxiomConstraintType.Required, Property = "source" }
            }
        };

        var axioms = new List<Axiom> { axiom };

        // Act
        var result = _validator.Validate(axioms);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.UnknownTargetTypes);
        _mockSchemaRegistry.Verify(x => x.GetRelationshipType("REFERENCES"), Times.Once);
    }

    #endregion

    #region Empty Input

    [Fact]
    public void Validate_WithEmptyList_ReturnsEmptyResult()
    {
        // Arrange
        var axioms = new List<Axiom>();

        // Act
        var result = _validator.Validate(axioms);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Errors);
        Assert.Empty(result.InvalidAxiomIds);
        Assert.Empty(result.UnknownTargetTypes);
    }

    [Fact]
    public void Validate_WithNull_ThrowsException()
    {
        // Act & Assert
        // Implementation throws NullReferenceException when given null input
        Assert.ThrowsAny<Exception>(() => _validator.Validate(null!));
    }

    #endregion

    #region Helper Methods

    private static Axiom CreateTestAxiom(string id, string targetType)
    {
        return new Axiom
        {
            Id = id,
            Name = $"Test Axiom {id}",
            TargetType = targetType,
            TargetKind = AxiomTargetKind.Entity,
            Rules = new List<AxiomRule>
            {
                new()
                {
                    Constraint = AxiomConstraintType.Required,
                    Property = "name"
                }
            }
        };
    }

    private static EntityTypeSchema CreateEntityTypeSchema(string name)
    {
        return new EntityTypeSchema
        {
            Name = name,
            Properties = Array.Empty<PropertySchema>()
        };
    }

    private static RelationshipTypeSchema CreateRelationshipTypeSchema(string name)
    {
        return new RelationshipTypeSchema
        {
            Name = name,
            FromEntityTypes = new List<string> { "Entity" },
            ToEntityTypes = new List<string> { "Entity" },
            Properties = Array.Empty<PropertySchema>()
        };
    }

    #endregion
}
