// =============================================================================
// File: AxiomItemViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomItemViewModel.
// =============================================================================
// v0.4.7i: Tests for rule formatting in the Axiom Viewer.
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.UI.ViewModels;

namespace Lexichord.Tests.Unit.Modules.Knowledge.ViewModels;

/// <summary>
/// Unit tests for <see cref="AxiomItemViewModel"/>.
/// </summary>
public class AxiomItemViewModelTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullAxiom_ThrowsArgumentNullException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => new AxiomItemViewModel(null!));
        Assert.Equal("axiom", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidAxiom_MapsProperties()
    {
        // Arrange
        var axiom = CreateTestAxiom();

        // Act
        var sut = new AxiomItemViewModel(axiom);

        // Assert
        Assert.Equal("test-axiom-001", sut.Id);
        Assert.Equal("Test Axiom", sut.Name);
        Assert.Equal("Test description", sut.Description);
        Assert.Equal("Endpoint", sut.TargetType);
        Assert.Equal(AxiomTargetKind.Entity, sut.TargetKind);
        Assert.Equal(AxiomSeverity.Error, sut.Severity);
        Assert.Equal("Validation", sut.Category);
        Assert.True(sut.IsEnabled);
        Assert.Equal(2, sut.RuleCount);
    }

    [Fact]
    public void Constructor_WithTags_ExposesTagsCorrectly()
    {
        // Arrange
        var axiom = CreateTestAxiom() with
        {
            Tags = new List<string> { "required", "api", "validation" }
        };

        // Act
        var sut = new AxiomItemViewModel(axiom);

        // Assert
        Assert.Equal(3, sut.Tags.Count);
        Assert.Contains("required", sut.Tags);
        Assert.Contains("api", sut.Tags);
        Assert.Contains("validation", sut.Tags);
    }

    #endregion

    #region SeverityDisplay Tests

    [Theory]
    [InlineData(AxiomSeverity.Error, "Error")]
    [InlineData(AxiomSeverity.Warning, "Warning")]
    [InlineData(AxiomSeverity.Info, "Info")]
    public void SeverityDisplay_ReturnsFriendlyString(AxiomSeverity severity, string expected)
    {
        // Arrange
        var axiom = CreateTestAxiom() with { Severity = severity };
        var sut = new AxiomItemViewModel(axiom);

        // Act
        var result = sut.SeverityDisplay;

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region TargetKindDisplay Tests

    [Theory]
    [InlineData(AxiomTargetKind.Entity, "Entity")]
    [InlineData(AxiomTargetKind.Relationship, "Relationship")]
    [InlineData(AxiomTargetKind.Claim, "Claim")]
    public void TargetKindDisplay_ReturnsFriendlyString(AxiomTargetKind kind, string expected)
    {
        // Arrange
        var axiom = CreateTestAxiom() with { TargetKind = kind };
        var sut = new AxiomItemViewModel(axiom);

        // Act
        var result = sut.TargetKindDisplay;

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion

    #region FormatRuleDescription Tests - All Constraint Types

    [Fact]
    public void FormatRuleDescription_Required_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "method",
            Constraint = AxiomConstraintType.Required
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'method' is required", result);
    }

    [Fact]
    public void FormatRuleDescription_OneOf_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "method",
            Constraint = AxiomConstraintType.OneOf,
            Values = new List<object> { "GET", "POST", "PUT", "DELETE" }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'method' must be one of: \"GET\", \"POST\", \"PUT\", \"DELETE\"", result);
    }

    [Fact]
    public void FormatRuleDescription_NotOneOf_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "status",
            Constraint = AxiomConstraintType.NotOneOf,
            Values = new List<object> { "deprecated", "obsolete" }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'status' must not be one of: \"deprecated\", \"obsolete\"", result);
    }

    [Fact]
    public void FormatRuleDescription_Range_WithBothMinAndMax_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "confidence",
            Constraint = AxiomConstraintType.Range,
            Min = 0.0,
            Max = 1.0
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'confidence' must be between 0 and 1", result);
    }

    [Fact]
    public void FormatRuleDescription_Range_WithMinOnly_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "count",
            Constraint = AxiomConstraintType.Range,
            Min = 1
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'count' must be at least 1", result);
    }

    [Fact]
    public void FormatRuleDescription_Range_WithMaxOnly_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "count",
            Constraint = AxiomConstraintType.Range,
            Max = 100
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'count' must be at most 100", result);
    }

    [Fact]
    public void FormatRuleDescription_Pattern_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "path",
            Constraint = AxiomConstraintType.Pattern,
            Pattern = "^/api/.*"
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'path' must match pattern: ^/api/.*", result);
    }

    [Fact]
    public void FormatRuleDescription_Cardinality_WithMinAndMax_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "parameters",
            Constraint = AxiomConstraintType.Cardinality,
            MinCount = 1,
            MaxCount = 10
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'parameters' must have 1-10 items", result);
    }

    [Fact]
    public void FormatRuleDescription_Cardinality_WithMinOnly_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "items",
            Constraint = AxiomConstraintType.Cardinality,
            MinCount = 2
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'items' must have at least 2 item(s)", result);
    }

    [Fact]
    public void FormatRuleDescription_Cardinality_WithMaxOnly_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "items",
            Constraint = AxiomConstraintType.Cardinality,
            MinCount = 0,
            MaxCount = 5
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'items' must have at most 5 item(s)", result);
    }

    [Fact]
    public void FormatRuleDescription_NotBoth_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Constraint = AxiomConstraintType.NotBoth,
            Properties = new List<string> { "deprecated", "required" }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'deprecated' and 'required' cannot both be set", result);
    }

    [Fact]
    public void FormatRuleDescription_RequiresTogether_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Constraint = AxiomConstraintType.RequiresTogether,
            Properties = new List<string> { "minValue", "maxValue" }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'minValue' and 'maxValue' must be set together", result);
    }

    [Fact]
    public void FormatRuleDescription_Equals_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "version",
            Constraint = AxiomConstraintType.Equals,
            Values = new List<object> { "1.0" }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'version' must equal \"1.0\"", result);
    }

    [Fact]
    public void FormatRuleDescription_NotEquals_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "status",
            Constraint = AxiomConstraintType.NotEquals,
            Values = new List<object> { "deleted" }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'status' must not equal \"deleted\"", result);
    }

    [Fact]
    public void FormatRuleDescription_Unique_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "id",
            Constraint = AxiomConstraintType.Unique
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'id' must be unique", result);
    }

    [Fact]
    public void FormatRuleDescription_ReferenceExists_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "parentId",
            Constraint = AxiomConstraintType.ReferenceExists,
            ReferenceType = "Document"
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'parentId' must reference an existing Document", result);
    }

    [Fact]
    public void FormatRuleDescription_TypeValid_FormatsCorrectly()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "entityType",
            Constraint = AxiomConstraintType.TypeValid
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'entityType' must have a valid type", result);
    }

    [Fact]
    public void FormatRuleDescription_Custom_WithErrorMessage_ReturnsErrorMessage()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "email",
            Constraint = AxiomConstraintType.Custom,
            ErrorMessage = "Email must be a valid corporate address"
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("Email must be a valid corporate address", result);
    }

    [Fact]
    public void FormatRuleDescription_Custom_WithoutErrorMessage_ReturnsDefault()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "data",
            Constraint = AxiomConstraintType.Custom
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'data' must satisfy custom validation", result);
    }

    #endregion

    #region FormatRuleDescription Tests - Conditional Clauses

    [Fact]
    public void FormatRuleDescription_WithWhenCondition_AppendsCondition()
    {
        // Arrange
        var rule = new AxiomRule
        {
            Property = "method",
            Constraint = AxiomConstraintType.Required,
            When = new AxiomCondition
            {
                Property = "isPublic",
                Operator = ConditionOperator.Equals,
                Value = true
            }
        };

        // Act
        var result = AxiomItemViewModel.FormatRuleDescription(rule);

        // Assert
        Assert.Equal("'method' is required (when 'isPublic' = true)", result);
    }

    #endregion

    #region FormatCondition Tests

    [Fact]
    public void FormatCondition_Equals_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "status",
            Operator = ConditionOperator.Equals,
            Value = "active"
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'status' = \"active\"", result);
    }

    [Fact]
    public void FormatCondition_NotEquals_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "status",
            Operator = ConditionOperator.NotEquals,
            Value = "deleted"
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'status' ≠ \"deleted\"", result);
    }

    [Fact]
    public void FormatCondition_Contains_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "path",
            Operator = ConditionOperator.Contains,
            Value = "/api/"
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'path' contains \"/api/\"", result);
    }

    [Fact]
    public void FormatCondition_StartsWith_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "name",
            Operator = ConditionOperator.StartsWith,
            Value = "Test"
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'name' starts with \"Test\"", result);
    }

    [Fact]
    public void FormatCondition_EndsWith_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "file",
            Operator = ConditionOperator.EndsWith,
            Value = ".md"
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'file' ends with \".md\"", result);
    }

    [Fact]
    public void FormatCondition_GreaterThan_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "count",
            Operator = ConditionOperator.GreaterThan,
            Value = 10
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'count' > 10", result);
    }

    [Fact]
    public void FormatCondition_LessThan_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "priority",
            Operator = ConditionOperator.LessThan,
            Value = 5
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'priority' < 5", result);
    }

    [Fact]
    public void FormatCondition_IsNull_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "parentId",
            Operator = ConditionOperator.IsNull,
            Value = ""
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'parentId' is null", result);
    }

    [Fact]
    public void FormatCondition_IsNotNull_FormatsCorrectly()
    {
        // Arrange
        var condition = new AxiomCondition
        {
            Property = "updatedAt",
            Operator = ConditionOperator.IsNotNull,
            Value = ""
        };

        // Act
        var result = AxiomItemViewModel.FormatCondition(condition);

        // Assert
        Assert.Equal("'updatedAt' is not null", result);
    }

    #endregion

    #region FormattedRules Tests

    [Fact]
    public void FormattedRules_ReturnsAllRulesFormatted()
    {
        // Arrange
        var axiom = CreateTestAxiom();
        var sut = new AxiomItemViewModel(axiom);

        // Act
        var rules = sut.FormattedRules;

        // Assert
        Assert.Equal(2, rules.Count);
        Assert.Equal("'method' is required", rules[0]);
        Assert.Contains("must be one of", rules[1]);
    }

    [Fact]
    public void FormattedRules_IsLazyEvaluated()
    {
        // Arrange
        var axiom = CreateTestAxiom();
        var sut = new AxiomItemViewModel(axiom);

        // Act - Access twice
        var rules1 = sut.FormattedRules;
        var rules2 = sut.FormattedRules;

        // Assert - Same instance
        Assert.Same(rules1, rules2);
    }

    #endregion

    #region Test Helpers

    private static Axiom CreateTestAxiom()
    {
        return new Axiom
        {
            Id = "test-axiom-001",
            Name = "Test Axiom",
            Description = "Test description",
            TargetType = "Endpoint",
            TargetKind = AxiomTargetKind.Entity,
            Severity = AxiomSeverity.Error,
            Category = "Validation",
            Tags = new List<string> { "test" },
            IsEnabled = true,
            SourceFile = "axioms/test.yaml",
            Rules = new List<AxiomRule>
            {
                new AxiomRule
                {
                    Property = "method",
                    Constraint = AxiomConstraintType.Required
                },
                new AxiomRule
                {
                    Property = "method",
                    Constraint = AxiomConstraintType.OneOf,
                    Values = new List<object> { "GET", "POST", "PUT", "DELETE" }
                }
            }
        };
    }

    #endregion
}
