// =============================================================================
// File: AxiomDataModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Axiom Data Model records and enums.
// =============================================================================
// LOGIC: Validates the correctness of all axiom abstraction types defined in
//   Lexichord.Abstractions.Contracts.Knowledge: AxiomSeverity, AxiomTargetKind,
//   AxiomConstraintType, ConditionOperator, TextSpan, AxiomCondition, AxiomFix,
//   AxiomRule, Axiom, AxiomViolation, and AxiomValidationResult.
//
// Test Categories:
//   - Enum values and ordinal verification
//   - Record default values and required properties
//   - Computed properties (TextSpan.Length, AxiomValidationResult.IsValid)
//   - Factory methods (AxiomValidationResult.Valid, WithViolations)
//   - Record equality
//
// v0.4.6e: Axiom Data Model (CKVS Phase 1b)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge;

/// <summary>
/// Unit tests for Axiom Data Model records, enums, and validation result types.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6e")]
public sealed class AxiomDataModelTests
{
    #region AxiomSeverity Enum Tests

    [Fact]
    public void AxiomSeverity_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<AxiomSeverity>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(AxiomSeverity.Error, 0)]
    [InlineData(AxiomSeverity.Warning, 1)]
    [InlineData(AxiomSeverity.Info, 2)]
    public void AxiomSeverity_Values_HaveExpectedOrdinals(AxiomSeverity value, int expected)
    {
        // Assert
        ((int)value).Should().Be(expected);
    }

    #endregion

    #region AxiomTargetKind Enum Tests

    [Fact]
    public void AxiomTargetKind_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<AxiomTargetKind>().Should().HaveCount(3);
    }

    [Theory]
    [InlineData(AxiomTargetKind.Entity, 0)]
    [InlineData(AxiomTargetKind.Relationship, 1)]
    [InlineData(AxiomTargetKind.Claim, 2)]
    public void AxiomTargetKind_Values_HaveExpectedOrdinals(AxiomTargetKind value, int expected)
    {
        // Assert
        ((int)value).Should().Be(expected);
    }

    #endregion

    #region AxiomConstraintType Enum Tests

    [Fact]
    public void AxiomConstraintType_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<AxiomConstraintType>().Should().HaveCount(14);
    }

    [Theory]
    [InlineData(AxiomConstraintType.Required, 0)]
    [InlineData(AxiomConstraintType.OneOf, 1)]
    [InlineData(AxiomConstraintType.NotOneOf, 2)]
    [InlineData(AxiomConstraintType.Range, 3)]
    [InlineData(AxiomConstraintType.Pattern, 4)]
    [InlineData(AxiomConstraintType.Cardinality, 5)]
    [InlineData(AxiomConstraintType.NotBoth, 6)]
    [InlineData(AxiomConstraintType.RequiresTogether, 7)]
    [InlineData(AxiomConstraintType.Equals, 8)]
    [InlineData(AxiomConstraintType.NotEquals, 9)]
    [InlineData(AxiomConstraintType.Unique, 10)]
    [InlineData(AxiomConstraintType.ReferenceExists, 11)]
    [InlineData(AxiomConstraintType.TypeValid, 12)]
    [InlineData(AxiomConstraintType.Custom, 13)]
    public void AxiomConstraintType_Values_HaveExpectedOrdinals(AxiomConstraintType value, int expected)
    {
        // Assert
        ((int)value).Should().Be(expected);
    }

    #endregion

    #region ConditionOperator Enum Tests

    [Fact]
    public void ConditionOperator_HasExpectedValues()
    {
        // Assert
        Enum.GetValues<ConditionOperator>().Should().HaveCount(9);
    }

    [Theory]
    [InlineData(ConditionOperator.Equals, 0)]
    [InlineData(ConditionOperator.NotEquals, 1)]
    [InlineData(ConditionOperator.Contains, 2)]
    [InlineData(ConditionOperator.StartsWith, 3)]
    [InlineData(ConditionOperator.EndsWith, 4)]
    [InlineData(ConditionOperator.GreaterThan, 5)]
    [InlineData(ConditionOperator.LessThan, 6)]
    [InlineData(ConditionOperator.IsNull, 7)]
    [InlineData(ConditionOperator.IsNotNull, 8)]
    public void ConditionOperator_Values_HaveExpectedOrdinals(ConditionOperator value, int expected)
    {
        // Assert
        ((int)value).Should().Be(expected);
    }

    #endregion

    #region TextSpan Tests

    [Fact]
    public void TextSpan_DefaultValues_Correct()
    {
        // Arrange & Act
        var span = new TextSpan();

        // Assert
        span.Start.Should().Be(0);
        span.End.Should().Be(0);
        span.Line.Should().Be(0);
        span.Column.Should().Be(0);
    }

    [Fact]
    public void TextSpan_Length_CalculatesCorrectly()
    {
        // Arrange
        var span = new TextSpan { Start = 10, End = 25, Line = 1, Column = 10 };

        // Act & Assert
        span.Length.Should().Be(15);
    }

    [Fact]
    public void TextSpan_WithAllFields_Populated()
    {
        // Arrange & Act
        var span = new TextSpan { Start = 100, End = 150, Line = 5, Column = 20 };

        // Assert
        span.Start.Should().Be(100);
        span.End.Should().Be(150);
        span.Line.Should().Be(5);
        span.Column.Should().Be(20);
        span.Length.Should().Be(50);
    }

    [Fact]
    public void TextSpan_Equality_SameValues()
    {
        // Arrange
        var a = new TextSpan { Start = 10, End = 20, Line = 1, Column = 10 };
        var b = new TextSpan { Start = 10, End = 20, Line = 1, Column = 10 };

        // Assert
        a.Should().Be(b);
    }

    #endregion

    #region AxiomCondition Tests

    [Fact]
    public void AxiomCondition_WithRequiredFields_CreatesSuccessfully()
    {
        // Arrange & Act
        var condition = new AxiomCondition
        {
            Property = "status",
            Value = "active"
        };

        // Assert
        condition.Property.Should().Be("status");
        condition.Value.Should().Be("active");
        condition.Operator.Should().Be(ConditionOperator.Equals); // Default
    }

    [Fact]
    public void AxiomCondition_WithOperator_Populated()
    {
        // Arrange & Act
        var condition = new AxiomCondition
        {
            Property = "count",
            Operator = ConditionOperator.GreaterThan,
            Value = 5
        };

        // Assert
        condition.Operator.Should().Be(ConditionOperator.GreaterThan);
        condition.Value.Should().Be(5);
    }

    #endregion

    #region AxiomFix Tests

    [Fact]
    public void AxiomFix_DefaultValues_Correct()
    {
        // Arrange & Act
        var fix = new AxiomFix { Description = "Test fix" };

        // Assert
        fix.Description.Should().Be("Test fix");
        fix.PropertyName.Should().BeNull();
        fix.NewValue.Should().BeNull();
        fix.Confidence.Should().Be(0f);
        fix.CanAutoApply.Should().BeFalse();
    }

    [Fact]
    public void AxiomFix_WithAllFields_Populated()
    {
        // Arrange & Act
        var fix = new AxiomFix
        {
            Description = "Add method property",
            PropertyName = "method",
            NewValue = "GET",
            Confidence = 0.95f,
            CanAutoApply = true
        };

        // Assert
        fix.Description.Should().Be("Add method property");
        fix.PropertyName.Should().Be("method");
        fix.NewValue.Should().Be("GET");
        fix.Confidence.Should().Be(0.95f);
        fix.CanAutoApply.Should().BeTrue();
    }

    #endregion

    #region AxiomRule Tests

    [Fact]
    public void AxiomRule_WithRequiredConstraint_CreatesSuccessfully()
    {
        // Arrange & Act
        var rule = new AxiomRule
        {
            Property = "name",
            Constraint = AxiomConstraintType.Required
        };

        // Assert
        rule.Property.Should().Be("name");
        rule.Constraint.Should().Be(AxiomConstraintType.Required);
        rule.Properties.Should().BeNull();
        rule.Values.Should().BeNull();
    }

    [Fact]
    public void AxiomRule_WithOneOfConstraint_ValuesPopulated()
    {
        // Arrange & Act
        var rule = new AxiomRule
        {
            Property = "method",
            Constraint = AxiomConstraintType.OneOf,
            Values = new object[] { "GET", "POST", "PUT", "DELETE" }
        };

        // Assert
        rule.Values.Should().HaveCount(4);
        rule.Values.Should().Contain("GET");
    }

    [Fact]
    public void AxiomRule_WithRangeConstraint_MinMaxPopulated()
    {
        // Arrange & Act
        var rule = new AxiomRule
        {
            Property = "priority",
            Constraint = AxiomConstraintType.Range,
            Min = 1,
            Max = 10
        };

        // Assert
        rule.Min.Should().Be(1);
        rule.Max.Should().Be(10);
    }

    [Fact]
    public void AxiomRule_WithCardinalityConstraint_CountsPopulated()
    {
        // Arrange & Act
        var rule = new AxiomRule
        {
            Property = "tags",
            Constraint = AxiomConstraintType.Cardinality,
            MinCount = 1,
            MaxCount = 5
        };

        // Assert
        rule.MinCount.Should().Be(1);
        rule.MaxCount.Should().Be(5);
    }

    [Fact]
    public void AxiomRule_WithMultiPropertyConstraint_PropertiesPopulated()
    {
        // Arrange & Act
        var rule = new AxiomRule
        {
            Constraint = AxiomConstraintType.NotBoth,
            Properties = new[] { "deprecated", "required" }
        };

        // Assert
        rule.Properties.Should().HaveCount(2);
        rule.Properties.Should().Contain("deprecated");
        rule.Properties.Should().Contain("required");
    }

    [Fact]
    public void AxiomRule_WithCondition_WhenPopulated()
    {
        // Arrange & Act
        var rule = new AxiomRule
        {
            Property = "default_value",
            Constraint = AxiomConstraintType.Required,
            When = new AxiomCondition
            {
                Property = "optional",
                Operator = ConditionOperator.Equals,
                Value = true
            }
        };

        // Assert
        rule.When.Should().NotBeNull();
        rule.When!.Property.Should().Be("optional");
    }

    #endregion

    #region Axiom Tests

    [Fact]
    public void Axiom_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange & Act
        var axiom = new Axiom
        {
            Id = "test-axiom",
            Name = "Test Axiom",
            TargetType = "TestEntity",
            Rules = new[] { new AxiomRule { Constraint = AxiomConstraintType.Required, Property = "name" } }
        };

        // Assert
        axiom.Id.Should().Be("test-axiom");
        axiom.Name.Should().Be("Test Axiom");
        axiom.TargetType.Should().Be("TestEntity");
        axiom.Rules.Should().ContainSingle();
    }

    [Fact]
    public void Axiom_DefaultValues_Correct()
    {
        // Arrange & Act
        var axiom = new Axiom
        {
            Id = "test",
            Name = "Test",
            TargetType = "Entity",
            Rules = Array.Empty<AxiomRule>()
        };

        // Assert
        axiom.IsEnabled.Should().BeTrue();
        axiom.Severity.Should().Be(AxiomSeverity.Error);
        axiom.TargetKind.Should().Be(AxiomTargetKind.Entity);
        axiom.Tags.Should().BeEmpty();
        axiom.Category.Should().BeNull();
        axiom.Description.Should().BeNull();
        axiom.SourceFile.Should().BeNull();
        axiom.SchemaVersion.Should().Be("1.0");
    }

    [Fact]
    public void Axiom_WithAllFields_Populated()
    {
        // Arrange & Act
        var axiom = new Axiom
        {
            Id = "endpoint-method-required",
            Name = "Endpoint Method Required",
            Description = "Every endpoint must have an HTTP method",
            TargetType = "Endpoint",
            TargetKind = AxiomTargetKind.Entity,
            Severity = AxiomSeverity.Error,
            Category = "API Documentation",
            Tags = new[] { "api", "required" },
            IsEnabled = true,
            SourceFile = "/schemas/api-rules.yaml",
            SchemaVersion = "1.0",
            Rules = new[]
            {
                new AxiomRule { Property = "method", Constraint = AxiomConstraintType.Required }
            }
        };

        // Assert
        axiom.Category.Should().Be("API Documentation");
        axiom.Tags.Should().HaveCount(2);
        axiom.SourceFile.Should().Be("/schemas/api-rules.yaml");
    }

    #endregion

    #region AxiomViolation Tests

    [Fact]
    public void AxiomViolation_WithRequiredProperties_CreatesSuccessfully()
    {
        // Arrange
        var axiom = CreateTestAxiom();
        var rule = axiom.Rules[0];

        // Act
        var violation = new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = rule,
            Message = "Property 'name' is required"
        };

        // Assert
        violation.Axiom.Should().BeSameAs(axiom);
        violation.ViolatedRule.Should().BeSameAs(rule);
        violation.Message.Should().Contain("required");
        violation.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AxiomViolation_DefaultValues_Correct()
    {
        // Arrange & Act
        var violation = new AxiomViolation
        {
            Axiom = CreateTestAxiom(),
            ViolatedRule = CreateTestAxiom().Rules[0],
            Message = "Test violation"
        };

        // Assert
        violation.EntityId.Should().BeNull();
        violation.RelationshipId.Should().BeNull();
        violation.ClaimId.Should().BeNull();
        violation.PropertyName.Should().BeNull();
        violation.ActualValue.Should().BeNull();
        violation.ExpectedValue.Should().BeNull();
        violation.DocumentId.Should().BeNull();
        violation.Location.Should().BeNull();
        violation.SuggestedFix.Should().BeNull();
        violation.Severity.Should().Be(AxiomSeverity.Error); // enum default
    }

    [Fact]
    public void AxiomViolation_WithAllContext_Populated()
    {
        // Arrange
        var axiom = CreateTestAxiom();
        var entityId = Guid.NewGuid();

        // Act
        var violation = new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = axiom.Rules[0],
            EntityId = entityId,
            PropertyName = "name",
            ActualValue = null,
            ExpectedValue = "non-null string",
            Message = "Missing required property",
            Severity = AxiomSeverity.Error,
            Location = new TextSpan { Start = 10, End = 20, Line = 1, Column = 10 },
            SuggestedFix = new AxiomFix
            {
                Description = "Add name property",
                PropertyName = "name",
                NewValue = "Untitled",
                Confidence = 0.5f
            }
        };

        // Assert
        violation.EntityId.Should().Be(entityId);
        violation.PropertyName.Should().Be("name");
        violation.Location.Should().NotBeNull();
        violation.SuggestedFix.Should().NotBeNull();
    }

    #endregion

    #region AxiomValidationResult Tests

    [Fact]
    public void AxiomValidationResult_Valid_FactoryMethod_ReturnsValid()
    {
        // Arrange & Act
        var result = AxiomValidationResult.Valid(axiomsEvaluated: 5, rulesEvaluated: 12);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Violations.Should().BeEmpty();
        result.AxiomsEvaluated.Should().Be(5);
        result.RulesEvaluated.Should().Be(12);
        result.ErrorCount.Should().Be(0);
        result.WarningCount.Should().Be(0);
        result.InfoCount.Should().Be(0);
    }

    [Fact]
    public void AxiomValidationResult_WithErrors_IsNotValid()
    {
        // Arrange
        var violation = CreateViolation(AxiomSeverity.Error);

        // Act
        var result = AxiomValidationResult.WithViolations(new[] { violation });

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorCount.Should().Be(1);
    }

    [Fact]
    public void AxiomValidationResult_WithWarningsOnly_IsValid()
    {
        // Arrange
        var violation = CreateViolation(AxiomSeverity.Warning);

        // Act
        var result = AxiomValidationResult.WithViolations(new[] { violation });

        // Assert
        result.IsValid.Should().BeTrue();
        result.WarningCount.Should().Be(1);
        result.ErrorCount.Should().Be(0);
    }

    [Fact]
    public void AxiomValidationResult_WithInfoOnly_IsValid()
    {
        // Arrange
        var violation = CreateViolation(AxiomSeverity.Info);

        // Act
        var result = AxiomValidationResult.WithViolations(new[] { violation });

        // Assert
        result.IsValid.Should().BeTrue();
        result.InfoCount.Should().Be(1);
    }

    [Fact]
    public void AxiomValidationResult_MixedSeverities_CountsCorrectly()
    {
        // Arrange
        var violations = new[]
        {
            CreateViolation(AxiomSeverity.Error),
            CreateViolation(AxiomSeverity.Warning),
            CreateViolation(AxiomSeverity.Warning),
            CreateViolation(AxiomSeverity.Info)
        };

        // Act
        var result = AxiomValidationResult.WithViolations(violations);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Violations.Should().HaveCount(4);
        result.ErrorCount.Should().Be(1);
        result.WarningCount.Should().Be(2);
        result.InfoCount.Should().Be(1);
    }

    [Fact]
    public void AxiomValidationResult_BySeverity_GroupsCorrectly()
    {
        // Arrange
        var violations = new[]
        {
            CreateViolation(AxiomSeverity.Error),
            CreateViolation(AxiomSeverity.Warning),
            CreateViolation(AxiomSeverity.Warning)
        };

        // Act
        var result = AxiomValidationResult.WithViolations(violations);
        var bySeverity = result.BySeverity;

        // Assert
        bySeverity.Should().ContainKey(AxiomSeverity.Error);
        bySeverity.Should().ContainKey(AxiomSeverity.Warning);
        bySeverity[AxiomSeverity.Error].Should().ContainSingle();
        bySeverity[AxiomSeverity.Warning].Should().HaveCount(2);
    }

    #endregion

    #region Helper Methods

    private static Axiom CreateTestAxiom() => new()
    {
        Id = "test-axiom",
        Name = "Test Axiom",
        TargetType = "TestEntity",
        Rules = new[] { new AxiomRule { Property = "name", Constraint = AxiomConstraintType.Required } }
    };

    private static AxiomViolation CreateViolation(AxiomSeverity severity)
    {
        var axiom = CreateTestAxiom();
        return new AxiomViolation
        {
            Axiom = axiom,
            ViolatedRule = axiom.Rules[0],
            Message = $"Test {severity} violation",
            Severity = severity
        };
    }

    #endregion
}
