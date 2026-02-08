// =============================================================================
// File: SchemaValidatorServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Tests for SchemaValidatorService IValidator bridge.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Validation.Validators.Schema;
using Lexichord.Tests.Unit.TestUtilities;
using NSubstitute;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Schema;

/// <summary>
/// Tests for <see cref="SchemaValidatorService"/> IValidator bridge and entity validation.
/// </summary>
public sealed class SchemaValidatorServiceTests
{
    private readonly ISchemaRegistry _registry;
    private readonly SchemaValidatorService _sut;

    public SchemaValidatorServiceTests()
    {
        _registry = Substitute.For<ISchemaRegistry>();
        var typeChecker = new PropertyTypeChecker(new FakeLogger<PropertyTypeChecker>());
        var constraintEvaluator = new ConstraintEvaluator(new FakeLogger<ConstraintEvaluator>());
        _sut = new SchemaValidatorService(
            _registry,
            typeChecker,
            constraintEvaluator,
            new FakeLogger<SchemaValidatorService>());
    }

    private static KnowledgeEntity CreateEntity(
        string type = "Endpoint",
        string name = "GET /users",
        Dictionary<string, object>? properties = null) => new()
    {
        Type = type,
        Name = name,
        Properties = properties ?? new Dictionary<string, object>()
    };

    // =========================================================================
    // IValidator identity
    // =========================================================================

    [Fact]
    public void Id_ReturnsSchemaValidator()
    {
        _sut.Id.Should().Be("schema-validator");
    }

    [Fact]
    public void DisplayName_ReturnsSchemaValidator()
    {
        _sut.DisplayName.Should().Be("Schema Validator");
    }

    [Fact]
    public void SupportedModes_ReturnsAll()
    {
        _sut.SupportedModes.Should().Be(ValidationMode.All);
    }

    [Fact]
    public void RequiredLicenseTier_ReturnsWriterPro()
    {
        _sut.RequiredLicenseTier.Should().Be(LicenseTier.WriterPro);
    }

    // =========================================================================
    // ValidateAsync — pipeline integration
    // =========================================================================

    [Fact]
    public async Task ValidateAsync_NoEntitiesInMetadata_ReturnsEmpty()
    {
        var context = ValidationContext.Create("doc-1", "markdown", "content");
        var findings = await _sut.ValidateAsync(context);
        findings.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WithEntitiesInMetadata_ValidatesEach()
    {
        var entities = new List<KnowledgeEntity>
        {
            CreateEntity(properties: new Dictionary<string, object> { ["method"] = "GET", ["path"] = "/users" })
        };

        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var metadata = new Dictionary<string, object> { ["entities"] = entities };
        var options = ValidationOptions.Default();
        var context = new ValidationContext("doc-1", "markdown", "content", metadata, options);

        var findings = await _sut.ValidateAsync(context);

        // Should have zero errors with valid entity
        findings.Where(f => f.Severity == ValidationSeverity.Error).Should().BeEmpty();
    }

    // =========================================================================
    // ValidateEntityAsync — schema not found
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_SchemaNotFound_ReturnsWarning()
    {
        _registry.GetEntityType("Unknown").Returns((EntityTypeSchema?)null);
        var entity = CreateEntity(type: "Unknown");

        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Should().ContainSingle()
            .Which.Code.Should().Be(SchemaFindingCodes.SchemaNotFound);
    }

    [Fact]
    public async Task ValidateEntityAsync_SchemaNotFound_SeverityIsWarning()
    {
        _registry.GetEntityType("Unknown").Returns((EntityTypeSchema?)null);
        var entity = CreateEntity(type: "Unknown");

        var findings = await _sut.ValidateEntityAsync(entity);
        findings.Should().ContainSingle()
            .Which.Severity.Should().Be(ValidationSeverity.Warning);
    }

    // =========================================================================
    // ValidateEntityAsync — required properties
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_MissingRequiredProperty_ReturnsError()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        // Missing both "method" and "path"
        var entity = CreateEntity(properties: new Dictionary<string, object>());

        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Where(f => f.Code == SchemaFindingCodes.RequiredPropertyMissing)
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task ValidateEntityAsync_EmptyRequiredStringProperty_ReturnsError()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entity = CreateEntity(properties: new Dictionary<string, object>
        {
            ["method"] = "GET",
            ["path"] = "  " // Empty/whitespace
        });

        var findings = await _sut.ValidateEntityAsync(entity);
        findings.Should().Contain(f => f.Code == SchemaFindingCodes.RequiredPropertyMissing);
    }

    // =========================================================================
    // ValidateEntityAsync — type checking
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_TypeMismatch_ReturnsError()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entity = CreateEntity(properties: new Dictionary<string, object>
        {
            ["method"] = "GET",
            ["path"] = "/users",
            ["deprecated"] = "not-a-boolean" // Should be bool
        });

        var findings = await _sut.ValidateEntityAsync(entity);
        findings.Should().Contain(f => f.Code == SchemaFindingCodes.TypeMismatch);
    }

    // =========================================================================
    // ValidateEntityAsync — enum validation
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_InvalidEnumValue_ReturnsError()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entity = CreateEntity(properties: new Dictionary<string, object>
        {
            ["method"] = "INVALID",
            ["path"] = "/users"
        });

        var findings = await _sut.ValidateEntityAsync(entity);
        findings.Should().Contain(f => f.Code == SchemaFindingCodes.InvalidEnumValue);
    }

    [Fact]
    public async Task ValidateEntityAsync_ValidEnumValue_NoError()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entity = CreateEntity(properties: new Dictionary<string, object>
        {
            ["method"] = "GET",
            ["path"] = "/users"
        });

        var findings = await _sut.ValidateEntityAsync(entity);
        findings.Should().NotContain(f => f.Code == SchemaFindingCodes.InvalidEnumValue);
    }

    // =========================================================================
    // ValidateEntityAsync — unknown properties
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_UnknownProperty_ReturnsInfoFinding()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entity = CreateEntity(properties: new Dictionary<string, object>
        {
            ["method"] = "GET",
            ["path"] = "/users",
            ["unknown_prop"] = "value"
        });

        var findings = await _sut.ValidateEntityAsync(entity);
        var unknown = findings.FirstOrDefault(f => f.Code == SchemaFindingCodes.UnknownProperty);
        unknown.Should().NotBeNull();
        unknown!.Severity.Should().Be(ValidationSeverity.Info);
    }

    // =========================================================================
    // ValidateEntitiesAsync — batch
    // =========================================================================

    [Fact]
    public async Task ValidateEntitiesAsync_MultiplEntities_AggregatesFindings()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entities = new List<KnowledgeEntity>
        {
            CreateEntity(name: "E1", properties: new Dictionary<string, object>()), // Missing required
            CreateEntity(name: "E2", properties: new Dictionary<string, object>
            {
                ["method"] = "GET",
                ["path"] = "/ok"
            })
        };

        var findings = await _sut.ValidateEntitiesAsync(entities);

        // First entity should have required property errors, second should not
        findings.Should().Contain(f => f.Code == SchemaFindingCodes.RequiredPropertyMissing);
    }

    // =========================================================================
    // Levenshtein distance (internal)
    // =========================================================================

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("a", "", 1)]
    [InlineData("", "b", 1)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("GET", "GE", 1)]
    [InlineData("POST", "POSTS", 1)]
    public void LevenshteinDistance_ReturnsExpectedDistance(string a, string b, int expected)
    {
        SchemaValidatorService.LevenshteinDistance(a, b).Should().Be(expected);
    }

    // =========================================================================
    // Constraint integration
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_PatternViolation_ReturnsConstraintFinding()
    {
        _registry.GetEntityType("Endpoint").Returns(PredefinedSchemas.Endpoint);

        var entity = CreateEntity(properties: new Dictionary<string, object>
        {
            ["method"] = "GET",
            ["path"] = "no-leading-slash" // Violates ^/.* pattern
        });

        var findings = await _sut.ValidateEntityAsync(entity);
        findings.Should().Contain(f => f.Code == SchemaFindingCodes.PatternMismatch);
    }

    // =========================================================================
    // Constructor null checks
    // =========================================================================

    [Fact]
    public void Constructor_NullSchemaRegistry_Throws()
    {
        var act = () => new SchemaValidatorService(
            null!,
            new PropertyTypeChecker(new FakeLogger<PropertyTypeChecker>()),
            new ConstraintEvaluator(new FakeLogger<ConstraintEvaluator>()),
            new FakeLogger<SchemaValidatorService>());
        act.Should().Throw<ArgumentNullException>();
    }
}
