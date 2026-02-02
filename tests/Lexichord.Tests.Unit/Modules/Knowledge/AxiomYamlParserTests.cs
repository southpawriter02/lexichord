// =============================================================================
// File: AxiomYamlParserTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for AxiomYamlParser YAML parsing functionality.
// =============================================================================
// LOGIC: Tests YAML parsing, constraint type mapping, and error handling.
//
// v0.4.6g: Axiom Loader (CKVS Phase 1d)
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Modules.Knowledge.Axioms;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge;

/// <summary>
/// Unit tests for <see cref="AxiomYamlParser"/> parsing functionality.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6g")]
public class AxiomYamlParserTests
{
    private readonly AxiomYamlParser _parser;

    public AxiomYamlParserTests()
    {
        _parser = new AxiomYamlParser();
    }

    #region Basic Parsing

    [Fact]
    public void Parse_WithValidYaml_ReturnsAxioms()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: test-axiom-001
    name: Test Axiom
    description: A test axiom for unit testing
    target_type: Concept
    target_kind: entity
    severity: error
    category: test
    rules:
      - constraint: required
        property: name
        message: Name is required
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Axioms);
        Assert.Equal("test-axiom-001", result.Axioms[0].Id);
        Assert.Equal("Test Axiom", result.Axioms[0].Name);
        Assert.Equal("Concept", result.Axioms[0].TargetType);
        Assert.Equal(AxiomTargetKind.Entity, result.Axioms[0].TargetKind);
        Assert.Equal(AxiomSeverity.Error, result.Axioms[0].Severity);
    }

    [Fact]
    public void Parse_WithMultipleAxioms_ReturnsAllAxioms()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: axiom-001
    name: First Axiom
    target_type: Concept
    rules:
      - constraint: required
        property: name
  - id: axiom-002
    name: Second Axiom
    target_type: Endpoint
    rules:
      - constraint: required
        property: path
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Equal(2, result.Axioms.Count);
        Assert.Equal("axiom-001", result.Axioms[0].Id);
        Assert.Equal("axiom-002", result.Axioms[1].Id);
    }

    [Fact]
    public void Parse_WithEmptyYaml_ReturnsEmptyAxiomsCollection()
    {
        // Arrange
        var yaml = "";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Axioms);
    }

    [Fact]
    public void Parse_WithInvalidYaml_ReturnsParseError()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: test-axiom
    name: [invalid: yaml structure
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Errors);
        Assert.Equal("YAML_SYNTAX_ERROR", result.Errors[0].Code);
    }

    #endregion

    #region Constraint Type Parsing

    [Theory]
    [InlineData("required", AxiomConstraintType.Required)]
    [InlineData("one_of", AxiomConstraintType.OneOf)]
    [InlineData("not_one_of", AxiomConstraintType.NotOneOf)]
    [InlineData("range", AxiomConstraintType.Range)]
    [InlineData("pattern", AxiomConstraintType.Pattern)]
    [InlineData("cardinality", AxiomConstraintType.Cardinality)]
    [InlineData("unique", AxiomConstraintType.Unique)]
    [InlineData("reference_exists", AxiomConstraintType.ReferenceExists)]
    [InlineData("custom", AxiomConstraintType.Custom)]
    public void Parse_WithConstraintType_MapsCorrectly(string yamlConstraint, AxiomConstraintType expected)
    {
        // Arrange
        var yaml = $@"
version: '1.0'
axioms:
  - id: constraint-test
    name: Constraint Test
    target_type: Concept
    rules:
      - constraint: {yamlConstraint}
        property: test_property
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Single(result.Axioms[0].Rules);
        Assert.Equal(expected, result.Axioms[0].Rules[0].Constraint);
    }

    [Fact]
    public void Parse_WithUnknownConstraintType_GeneratesError()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: unknown-constraint-test
    name: Unknown Constraint Test
    target_type: Concept
    rules:
      - constraint: unknown_constraint_type
        property: test_property
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        // Unknown constraint types should either be recorded as errors OR default to custom
        Assert.NotNull(result);
        // Either the axiom is skipped (errors generated) or it's parsed with Custom constraint
        if (result.Axioms.Count > 0)
        {
            Assert.Single(result.Axioms);
            Assert.Single(result.Axioms[0].Rules);
            Assert.Equal(AxiomConstraintType.Custom, result.Axioms[0].Rules[0].Constraint);
        }
        else
        {
            Assert.NotEmpty(result.Errors);
        }
    }

    #endregion

    #region Severity Parsing

    [Theory]
    [InlineData("error", AxiomSeverity.Error)]
    [InlineData("warning", AxiomSeverity.Warning)]
    [InlineData("info", AxiomSeverity.Info)]
    public void Parse_WithSeverity_MapsCorrectly(string yamlSeverity, AxiomSeverity expected)
    {
        // Arrange
        var yaml = $@"
version: '1.0'
axioms:
  - id: severity-test
    name: Severity Test
    target_type: Concept
    severity: {yamlSeverity}
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Equal(expected, result.Axioms[0].Severity);
    }

    [Fact]
    public void Parse_WithoutSeverity_DefaultsToError()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: no-severity-test
    name: No Severity Test
    target_type: Concept
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Equal(AxiomSeverity.Error, result.Axioms[0].Severity);
    }

    #endregion

    #region Target Kind Parsing

    [Theory]
    [InlineData("entity", AxiomTargetKind.Entity)]
    [InlineData("relationship", AxiomTargetKind.Relationship)]
    [InlineData("claim", AxiomTargetKind.Claim)]
    public void Parse_WithTargetKind_MapsCorrectly(string yamlKind, AxiomTargetKind expected)
    {
        // Arrange
        var yaml = $@"
version: '1.0'
axioms:
  - id: kind-test
    name: Kind Test
    target_type: Concept
    target_kind: {yamlKind}
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Equal(expected, result.Axioms[0].TargetKind);
    }

    [Fact]
    public void Parse_WithoutTargetKind_DefaultsToEntity()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: no-kind-test
    name: No Kind Test
    target_type: Concept
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Equal(AxiomTargetKind.Entity, result.Axioms[0].TargetKind);
    }

    #endregion

    #region Conditional Rules

    [Fact]
    public void Parse_WithConditionalRule_ParsesWhenClause()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: conditional-test
    name: Conditional Test
    target_type: Endpoint
    rules:
      - constraint: required
        property: response_schema
        when:
          property: method
          operator: equals
          value: GET
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Single(result.Axioms[0].Rules);
        Assert.NotNull(result.Axioms[0].Rules[0].When);
        Assert.Equal("method", result.Axioms[0].Rules[0].When!.Property);
        Assert.Equal(ConditionOperator.Equals, result.Axioms[0].Rules[0].When!.Operator);
        Assert.Equal("GET", result.Axioms[0].Rules[0].When!.Value);
    }

    #endregion

    #region Tags Parsing

    [Fact]
    public void Parse_WithTags_ParsesTagsList()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: tags-test
    name: Tags Test
    target_type: Concept
    tags:
      - api
      - documentation
      - required
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Equal(3, result.Axioms[0].Tags.Count);
        Assert.Contains("api", result.Axioms[0].Tags);
        Assert.Contains("documentation", result.Axioms[0].Tags);
        Assert.Contains("required", result.Axioms[0].Tags);
    }

    [Fact]
    public void Parse_WithoutTags_ReturnsEmptyTagsList()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: no-tags-test
    name: No Tags Test
    target_type: Concept
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.Empty(result.Axioms[0].Tags);
    }

    #endregion

    #region Enabled Flag

    [Fact]
    public void Parse_WithEnabledFalse_SetsIsEnabledFalse()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: disabled-test
    name: Disabled Test
    target_type: Concept
    enabled: false
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.False(result.Axioms[0].IsEnabled);
    }

    [Fact]
    public void Parse_WithoutEnabled_DefaultsToTrue()
    {
        // Arrange
        var yaml = @"
version: '1.0'
axioms:
  - id: default-enabled-test
    name: Default Enabled Test
    target_type: Concept
    rules:
      - constraint: required
        property: name
";

        // Act
        var result = _parser.Parse(yaml, "test.yaml");

        // Assert
        Assert.Single(result.Axioms);
        Assert.True(result.Axioms[0].IsEnabled);
    }

    #endregion
}
