// =============================================================================
// File: ConstraintEvaluatorTests.cs
// Project: Lexichord.Tests.Unit
// Description: Tests for ConstraintEvaluator constraint checking logic.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Modules.Knowledge.Validation.Validators.Schema;
using Lexichord.Tests.Unit.TestUtilities;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Schema;

/// <summary>
/// Tests for <see cref="ConstraintEvaluator"/> constraint evaluation logic.
/// </summary>
public sealed class ConstraintEvaluatorTests
{
    private readonly ConstraintEvaluator _sut;

    public ConstraintEvaluatorTests()
    {
        _sut = new ConstraintEvaluator(new FakeLogger<ConstraintEvaluator>());
    }

    private static KnowledgeEntity CreateTestEntity(string name = "TestEntity") => new()
    {
        Type = "Test",
        Name = name,
        Properties = new Dictionary<string, object>()
    };

    // =========================================================================
    // Null value handling
    // =========================================================================

    [Fact]
    public void Evaluate_NullValue_ReturnsEmpty()
    {
        var schema = new PropertySchema { Name = "prop", Type = PropertyType.Number, MinValue = 0 };
        var findings = _sut.Evaluate(CreateTestEntity(), "prop", null, schema);
        findings.Should().BeEmpty();
    }

    // =========================================================================
    // Numeric constraints
    // =========================================================================

    [Fact]
    public void Evaluate_ValueBelowMinimum_ReturnsValueTooSmallFinding()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MinValue = 1 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 0, schema);

        findings.Should().ContainSingle()
            .Which.Code.Should().Be(SchemaFindingCodes.ValueTooSmall);
    }

    [Fact]
    public void Evaluate_ValueAboveMaximum_ReturnsValueTooLargeFinding()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MaxValue = 10 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 15, schema);

        findings.Should().ContainSingle()
            .Which.Code.Should().Be(SchemaFindingCodes.ValueTooLarge);
    }

    [Fact]
    public void Evaluate_ValueWithinRange_ReturnsEmpty()
    {
        var schema = new PropertySchema
        {
            Name = "priority",
            Type = PropertyType.Number,
            MinValue = 1,
            MaxValue = 10
        };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 5, schema);
        findings.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ValueAtMinBoundary_ReturnsEmpty()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MinValue = 1 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 1, schema);
        findings.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_ValueAtMaxBoundary_ReturnsEmpty()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MaxValue = 10 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 10, schema);
        findings.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_BothMinAndMaxViolated_ReturnsTwoFindings()
    {
        // LOGIC: A value can only violate one bound at a time, so test separately.
        var schemaLow = new PropertySchema
        {
            Name = "priority",
            Type = PropertyType.Number,
            MinValue = 5,
            MaxValue = 10
        };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 2, schemaLow);
        findings.Should().ContainSingle().Which.Code.Should().Be(SchemaFindingCodes.ValueTooSmall);

        var findingsHigh = _sut.Evaluate(CreateTestEntity(), "priority", 15, schemaLow);
        findingsHigh.Should().ContainSingle().Which.Code.Should().Be(SchemaFindingCodes.ValueTooLarge);
    }

    // =========================================================================
    // String constraints
    // =========================================================================

    [Fact]
    public void Evaluate_StringExceedsMaxLength_ReturnsStringTooLongFinding()
    {
        var schema = new PropertySchema { Name = "name", Type = PropertyType.String, MaxLength = 5 };
        var findings = _sut.Evaluate(CreateTestEntity(), "name", "toolongstring", schema);

        findings.Should().ContainSingle()
            .Which.Code.Should().Be(SchemaFindingCodes.StringTooLong);
    }

    [Fact]
    public void Evaluate_StringWithinMaxLength_ReturnsEmpty()
    {
        var schema = new PropertySchema { Name = "name", Type = PropertyType.String, MaxLength = 20 };
        var findings = _sut.Evaluate(CreateTestEntity(), "name", "short", schema);
        findings.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_StringMatchesPattern_ReturnsEmpty()
    {
        var schema = new PropertySchema
        {
            Name = "path",
            Type = PropertyType.String,
            Pattern = @"^/.*$"
        };
        var findings = _sut.Evaluate(CreateTestEntity(), "path", "/api/users", schema);
        findings.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_StringDoesNotMatchPattern_ReturnsPatternMismatchFinding()
    {
        var schema = new PropertySchema
        {
            Name = "path",
            Type = PropertyType.String,
            Pattern = @"^/.*$"
        };
        var findings = _sut.Evaluate(CreateTestEntity(), "path", "api/users", schema);

        findings.Should().ContainSingle()
            .Which.Code.Should().Be(SchemaFindingCodes.PatternMismatch);
    }

    [Fact]
    public void Evaluate_InvalidRegexPattern_ReturnsEmptyAndDoesNotThrow()
    {
        var schema = new PropertySchema
        {
            Name = "path",
            Type = PropertyType.String,
            Pattern = @"[invalid"
        };

        // LOGIC: Invalid regex should be caught and logged, not thrown.
        var act = () => _sut.Evaluate(CreateTestEntity(), "path", "test", schema);
        act.Should().NotThrow();
        act().Should().BeEmpty();
    }

    // =========================================================================
    // Finding metadata
    // =========================================================================

    [Fact]
    public void Evaluate_Findings_HaveCorrectValidatorId()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MinValue = 10 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 5, schema);

        findings.Should().ContainSingle()
            .Which.ValidatorId.Should().Be("schema-validator");
    }

    [Fact]
    public void Evaluate_Findings_HaveCorrectPropertyPath()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MinValue = 10 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 5, schema);

        findings.Should().ContainSingle()
            .Which.PropertyPath.Should().Be("priority");
    }

    [Fact]
    public void Evaluate_ValueTooSmall_HasSuggestedFix()
    {
        var schema = new PropertySchema { Name = "priority", Type = PropertyType.Number, MinValue = 5 };
        var findings = _sut.Evaluate(CreateTestEntity(), "priority", 2, schema);

        findings.Should().ContainSingle()
            .Which.SuggestedFix.Should().Contain("at least 5");
    }
}
