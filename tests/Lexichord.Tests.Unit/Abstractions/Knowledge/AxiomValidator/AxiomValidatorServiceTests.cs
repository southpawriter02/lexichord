// =============================================================================
// File: AxiomValidatorServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Tests for AxiomValidatorService IValidator bridge.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Axioms;
using Lexichord.Modules.Knowledge.Validation.Validators.Axiom;
using Lexichord.Tests.Unit.TestUtilities;
using NSubstitute;
using ValSeverity = Lexichord.Abstractions.Contracts.Knowledge.Validation.ValidationSeverity;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.AxiomValidator;

/// <summary>
/// Tests for <see cref="AxiomValidatorService"/> IValidator bridge and axiom validation.
/// </summary>
public sealed class AxiomValidatorServiceTests
{
    private readonly IAxiomStore _axiomStore;
    private readonly IAxiomEvaluator _axiomEvaluator;
    private readonly AxiomValidatorService _sut;

    public AxiomValidatorServiceTests()
    {
        _axiomStore = Substitute.For<IAxiomStore>();
        _axiomEvaluator = Substitute.For<IAxiomEvaluator>();
        _sut = new AxiomValidatorService(
            _axiomStore,
            _axiomEvaluator,
            new FakeLogger<AxiomValidatorService>());
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

    private static Lexichord.Abstractions.Contracts.Knowledge.Axiom CreateAxiom(
        string targetType = "Endpoint",
        AxiomTargetKind targetKind = AxiomTargetKind.Entity,
        AxiomSeverity severity = AxiomSeverity.Error,
        bool isEnabled = true,
        List<AxiomRule>? rules = null) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = $"Test axiom for {targetType}",
        Description = "Test axiom",
        TargetType = targetType,
        TargetKind = targetKind,
        Severity = severity,
        IsEnabled = isEnabled,
        Rules = rules ?? new List<AxiomRule>
        {
            new()
            {
                Property = "method",
                Constraint = AxiomConstraintType.Required
            }
        }
    };

    // =========================================================================
    // IValidator identity
    // =========================================================================

    [Fact]
    public void Id_ReturnsAxiomValidator()
    {
        _sut.Id.Should().Be("axiom-validator");
    }

    [Fact]
    public void DisplayName_ReturnsAxiomValidator()
    {
        _sut.DisplayName.Should().Be("Axiom Validator");
    }

    [Fact]
    public void SupportedModes_ReturnsAll()
    {
        _sut.SupportedModes.Should().Be(ValidationMode.All);
    }

    [Fact]
    public void RequiredLicenseTier_ReturnsTeams()
    {
        _sut.RequiredLicenseTier.Should().Be(LicenseTier.Teams);
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
        var entity = CreateEntity(
            properties: new Dictionary<string, object> { ["method"] = "GET", ["path"] = "/users" });
        var entities = new List<KnowledgeEntity> { entity };

        var axiom = CreateAxiom();
        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom });
        _axiomEvaluator.Evaluate(axiom, entity)
            .Returns(Array.Empty<AxiomViolation>());

        var metadata = new Dictionary<string, object> { ["entities"] = entities };
        var options = ValidationOptions.Default();
        var context = new ValidationContext("doc-1", "markdown", "content", metadata, options);

        var findings = await _sut.ValidateAsync(context);

        // No violations → no findings
        findings.Should().BeEmpty();
    }

    // =========================================================================
    // ValidateEntityAsync — no matching axioms
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_NoApplicableAxioms_ReturnsEmpty()
    {
        // Axiom targets "Parameter", entity is "Endpoint" → no match
        var axiom = CreateAxiom(targetType: "Parameter");
        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom });

        var entity = CreateEntity(type: "Endpoint");
        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Should().BeEmpty();
    }

    // =========================================================================
    // ValidateEntityAsync — violation detection
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_ViolationDetected_ReturnsFinding()
    {
        var entity = CreateEntity();
        var axiom = CreateAxiom();

        var violation = new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = axiom.Rules[0],
            PropertyName = "method",
            Message = "Required property 'method' is missing",
            Severity = AxiomSeverity.Error
        };

        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom });
        _axiomEvaluator.Evaluate(axiom, entity)
            .Returns(new List<AxiomViolation> { violation });

        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Should().ContainSingle();
        var finding = findings[0];
        finding.ValidatorId.Should().Be("axiom-validator");
        finding.Code.Should().Be(AxiomFindingCodes.RequiredViolation);
        finding.PropertyPath.Should().Be("method");
        finding.Message.Should().Contain("method");
    }

    // =========================================================================
    // ValidateEntityAsync — severity mapping
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_ViolationSeverityMapped_Error()
    {
        var entity = CreateEntity();
        var axiom = CreateAxiom(severity: AxiomSeverity.Error);

        var violation = new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = axiom.Rules[0],
            Message = "Error violation",
            Severity = AxiomSeverity.Error
        };

        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom });
        _axiomEvaluator.Evaluate(axiom, entity)
            .Returns(new List<AxiomViolation> { violation });

        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Should().ContainSingle()
            .Which.Severity.Should().Be(ValSeverity.Error);
    }

    [Fact]
    public async Task ValidateEntityAsync_ViolationSeverityMapped_Warning()
    {
        var entity = CreateEntity();
        var axiom = CreateAxiom(severity: AxiomSeverity.Warning);

        var violation = new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = new AxiomRule
            {
                Property = "deprecated",
                Constraint = AxiomConstraintType.Equals
            },
            Message = "Warning violation",
            Severity = AxiomSeverity.Warning
        };

        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom });
        _axiomEvaluator.Evaluate(axiom, entity)
            .Returns(new List<AxiomViolation> { violation });

        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Should().ContainSingle()
            .Which.Severity.Should().Be(ValSeverity.Warning);
    }

    // =========================================================================
    // ValidateEntityAsync — multiple axioms
    // =========================================================================

    [Fact]
    public async Task ValidateEntityAsync_MultipleAxioms_AggregatesFindings()
    {
        var entity = CreateEntity();

        var axiom1 = CreateAxiom();
        var axiom2 = CreateAxiom(rules: new List<AxiomRule>
        {
            new() { Property = "path", Constraint = AxiomConstraintType.Pattern, Pattern = "^/.*" }
        });

        var violation1 = new AxiomViolation
        {
            Axiom = axiom1,
            ViolatedRule = axiom1.Rules[0],
            PropertyName = "method",
            Message = "Missing method",
            Severity = AxiomSeverity.Error
        };
        var violation2 = new AxiomViolation
        {
            Axiom = axiom2,
            ViolatedRule = axiom2.Rules[0],
            PropertyName = "path",
            Message = "Invalid path pattern",
            Severity = AxiomSeverity.Error
        };

        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom1, axiom2 });
        _axiomEvaluator.Evaluate(axiom1, entity).Returns(new List<AxiomViolation> { violation1 });
        _axiomEvaluator.Evaluate(axiom2, entity).Returns(new List<AxiomViolation> { violation2 });

        var findings = await _sut.ValidateEntityAsync(entity);

        findings.Should().HaveCount(2);
        findings.Should().Contain(f => f.Code == AxiomFindingCodes.RequiredViolation);
        findings.Should().Contain(f => f.Code == AxiomFindingCodes.PatternViolation);
    }

    // =========================================================================
    // ValidateEntitiesAsync — batch
    // =========================================================================

    [Fact]
    public async Task ValidateEntitiesAsync_MultipleEntities_AggregatesFindings()
    {
        var entity1 = CreateEntity(name: "E1");
        var entity2 = CreateEntity(name: "E2");

        var axiom = CreateAxiom();

        var violation1 = new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = axiom.Rules[0],
            PropertyName = "method",
            Message = "Missing method on E1",
            Severity = AxiomSeverity.Error
        };

        _axiomStore.GetAllAxioms().Returns(new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { axiom });
        _axiomEvaluator.Evaluate(axiom, entity1).Returns(new List<AxiomViolation> { violation1 });
        _axiomEvaluator.Evaluate(axiom, entity2).Returns(Array.Empty<AxiomViolation>());

        var findings = await _sut.ValidateEntitiesAsync(new List<KnowledgeEntity> { entity1, entity2 });

        // Only entity1 has a violation
        findings.Should().ContainSingle();
        findings[0].Message.Should().Contain("E1");
    }

    // =========================================================================
    // GetApplicableAxiomsAsync
    // =========================================================================

    [Fact]
    public async Task GetApplicableAxiomsAsync_MatchesByTargetType()
    {
        var endpointAxiom = CreateAxiom(targetType: "Endpoint");
        var paramAxiom = CreateAxiom(targetType: "Parameter");

        _axiomStore.GetAllAxioms().Returns(
            new List<Lexichord.Abstractions.Contracts.Knowledge.Axiom> { endpointAxiom, paramAxiom });

        var entity = CreateEntity(type: "Endpoint");
        var axioms = await _sut.GetApplicableAxiomsAsync(entity);

        axioms.Should().ContainSingle();
        axioms[0].TargetType.Should().Be("Endpoint");
    }

    // =========================================================================
    // Constructor null checks
    // =========================================================================

    [Fact]
    public void Constructor_NullStore_Throws()
    {
        var act = () => new AxiomValidatorService(
            null!,
            Substitute.For<IAxiomEvaluator>(),
            new FakeLogger<AxiomValidatorService>());
        act.Should().Throw<ArgumentNullException>();
    }

    // =========================================================================
    // Internal mapping helpers
    // =========================================================================

    [Theory]
    [InlineData(AxiomConstraintType.Required, AxiomFindingCodes.RequiredViolation)]
    [InlineData(AxiomConstraintType.OneOf, AxiomFindingCodes.PropertyConstraint)]
    [InlineData(AxiomConstraintType.Pattern, AxiomFindingCodes.PatternViolation)]
    [InlineData(AxiomConstraintType.Range, AxiomFindingCodes.RangeViolation)]
    [InlineData(AxiomConstraintType.Cardinality, AxiomFindingCodes.CardinalityViolation)]
    [InlineData(AxiomConstraintType.NotBoth, AxiomFindingCodes.MutualExclusionViolation)]
    [InlineData(AxiomConstraintType.Equals, AxiomFindingCodes.EqualityViolation)]
    public void MapConstraintToCode_ReturnsExpectedCode(AxiomConstraintType constraint, string expected)
    {
        AxiomValidatorService.MapConstraintToCode(constraint).Should().Be(expected);
    }
}
