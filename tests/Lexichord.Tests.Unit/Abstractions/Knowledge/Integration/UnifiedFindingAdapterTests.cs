// =============================================================================
// File: UnifiedFindingAdapterTests.cs
// Description: Unit tests for UnifiedFindingAdapter (v0.6.5j).
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Modules.Knowledge.Validation.Integration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Knowledge.Integration;

/// <summary>
/// Tests for <see cref="UnifiedFindingAdapter"/>.
/// </summary>
/// <remarks>Feature: v0.6.5j — Linter Integration.</remarks>
[Trait("Feature", "v0.6.5j")]
public sealed class UnifiedFindingAdapterTests
{
    // =========================================================================
    // Fields
    // =========================================================================

    private readonly UnifiedFindingAdapter _adapter;

    // =========================================================================
    // Constructor
    // =========================================================================

    public UnifiedFindingAdapterTests()
    {
        var logger = Substitute.For<ILogger<UnifiedFindingAdapter>>();
        _adapter = new UnifiedFindingAdapter(logger);
    }

    // =========================================================================
    // FromValidationFinding Tests
    // =========================================================================

    [Fact]
    public void FromValidationFinding_MapsErrorSeverity()
    {
        // Arrange
        var finding = ValidationFinding.Error("schema", "SCH001", "Missing field");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.Source.Should().Be(FindingSource.Validation);
        result.Severity.Should().Be(UnifiedSeverity.Error);
        result.Code.Should().Be("SCH001");
        result.Message.Should().Be("Missing field");
        result.Category.Should().Be(FindingCategory.Schema);
        result.OriginalValidationFinding.Should().BeSameAs(finding);
        result.OriginalStyleViolation.Should().BeNull();
    }

    [Fact]
    public void FromValidationFinding_MapsWarningSeverity()
    {
        // Arrange
        var finding = ValidationFinding.Warn("axiom.core", "AXM001", "Rule mismatch");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.Severity.Should().Be(UnifiedSeverity.Warning);
        result.Category.Should().Be(FindingCategory.Axiom);
    }

    [Fact]
    public void FromValidationFinding_MapsInfoSeverity()
    {
        // Arrange
        var finding = ValidationFinding.Information("consistency", "CON001", "Info");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.Severity.Should().Be(UnifiedSeverity.Info);
        result.Category.Should().Be(FindingCategory.Consistency);
    }

    [Fact]
    public void FromValidationFinding_WithSuggestedFix_CreatesUnifiedFix()
    {
        // Arrange
        var finding = new ValidationFinding(
            "schema", ValidationSeverity.Warning, "SCH002", "Fix this",
            PropertyPath: "root.name", SuggestedFix: "Add name field");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.SuggestedFix.Should().NotBeNull();
        result.SuggestedFix!.Source.Should().Be(FindingSource.Validation);
        result.SuggestedFix.Description.Should().Be("Add name field");
        result.SuggestedFix.ReplacementText.Should().Be("Add name field");
        result.SuggestedFix.Confidence.Should().Be(0.5f);
        result.SuggestedFix.CanAutoApply.Should().BeFalse();
        result.SuggestedFix.FindingId.Should().Be(result.Id);
    }

    [Fact]
    public void FromValidationFinding_WithoutSuggestedFix_NoUnifiedFix()
    {
        // Arrange
        var finding = ValidationFinding.Error("schema", "SCH001", "No fix available");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.SuggestedFix.Should().BeNull();
    }

    [Fact]
    public void FromValidationFinding_MapsPropertyPath()
    {
        // Arrange
        var finding = new ValidationFinding(
            "schema", ValidationSeverity.Error, "SCH001", "Bad type",
            PropertyPath: "root.items[0].type");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.PropertyPath.Should().Be("root.items[0].type");
    }

    [Fact]
    public void FromValidationFinding_UnknownValidator_DefaultsToSchema()
    {
        // Arrange
        var finding = ValidationFinding.Error("custom-validator", "CUS001", "Custom issue");

        // Act
        var result = _adapter.FromValidationFinding(finding);

        // Assert
        result.Category.Should().Be(FindingCategory.Schema);
    }

    // =========================================================================
    // FromStyleViolation Tests
    // =========================================================================

    [Fact]
    public void FromStyleViolation_MapsCorrectly()
    {
        // Arrange
        var rule = new StyleRule(
            "no-jargon", "Avoid Jargon", "Jargon reduces clarity",
            RuleCategory.Terminology, ViolationSeverity.Warning,
            "leverage", PatternType.Literal, "use");
        var violation = new StyleViolation(
            rule, "Avoid Jargon: Jargon reduces clarity",
            StartOffset: 10, EndOffset: 18,
            StartLine: 2, StartColumn: 5,
            EndLine: 2, EndColumn: 13,
            MatchedText: "leverage", Suggestion: "use",
            Severity: ViolationSeverity.Warning);

        // Act
        var result = _adapter.FromStyleViolation(violation);

        // Assert
        result.Source.Should().Be(FindingSource.StyleLinter);
        result.Severity.Should().Be(UnifiedSeverity.Warning);
        result.Code.Should().Be("no-jargon");
        result.Category.Should().Be(FindingCategory.Style);
        result.PropertyPath.Should().Be("Line 2, Col 5");
        result.OriginalStyleViolation.Should().BeSameAs(violation);
        result.OriginalValidationFinding.Should().BeNull();
    }

    [Fact]
    public void FromStyleViolation_WithSuggestion_CreatesUnifiedFix()
    {
        // Arrange
        var rule = new StyleRule(
            "prefer-simple", "Prefer Simple", "Use simpler words",
            RuleCategory.Terminology, ViolationSeverity.Info,
            "utilize", PatternType.Literal, "use");
        var violation = new StyleViolation(
            rule, "Prefer Simple: Use simpler words",
            StartOffset: 0, EndOffset: 7,
            StartLine: 1, StartColumn: 1,
            EndLine: 1, EndColumn: 8,
            MatchedText: "utilize", Suggestion: "use",
            Severity: ViolationSeverity.Info);

        // Act
        var result = _adapter.FromStyleViolation(violation);

        // Assert
        result.SuggestedFix.Should().NotBeNull();
        result.SuggestedFix!.Source.Should().Be(FindingSource.StyleLinter);
        result.SuggestedFix.Description.Should().Contain("utilize");
        result.SuggestedFix.Description.Should().Contain("use");
        result.SuggestedFix.ReplacementText.Should().Be("use");
        result.SuggestedFix.Confidence.Should().Be(0.7f);
        result.SuggestedFix.CanAutoApply.Should().BeTrue();
    }

    [Fact]
    public void FromStyleViolation_WithoutSuggestion_NoUnifiedFix()
    {
        // Arrange
        var rule = new StyleRule(
            "line-length", "Line Length", "Line too long",
            RuleCategory.Formatting, ViolationSeverity.Hint,
            ".*", PatternType.Regex, null);
        var violation = new StyleViolation(
            rule, "Line too long",
            StartOffset: 0, EndOffset: 100,
            StartLine: 1, StartColumn: 1,
            EndLine: 1, EndColumn: 101,
            MatchedText: "some long line...", Suggestion: null,
            Severity: ViolationSeverity.Hint);

        // Act
        var result = _adapter.FromStyleViolation(violation);

        // Assert
        result.SuggestedFix.Should().BeNull();
        result.Severity.Should().Be(UnifiedSeverity.Hint);
    }

    // =========================================================================
    // NormalizeSeverity Tests
    // =========================================================================

    [Theory]
    [InlineData(ValidationSeverity.Error, UnifiedSeverity.Error)]
    [InlineData(ValidationSeverity.Warning, UnifiedSeverity.Warning)]
    [InlineData(ValidationSeverity.Info, UnifiedSeverity.Info)]
    public void NormalizeSeverity_ValidationSeverity_MapsCorrectly(
        ValidationSeverity input, UnifiedSeverity expected)
    {
        _adapter.NormalizeSeverity(input).Should().Be(expected);
    }

    [Theory]
    [InlineData(ViolationSeverity.Error, UnifiedSeverity.Error)]
    [InlineData(ViolationSeverity.Warning, UnifiedSeverity.Warning)]
    [InlineData(ViolationSeverity.Info, UnifiedSeverity.Info)]
    [InlineData(ViolationSeverity.Hint, UnifiedSeverity.Hint)]
    public void NormalizeSeverity_ViolationSeverity_MapsCorrectly(
        ViolationSeverity input, UnifiedSeverity expected)
    {
        _adapter.NormalizeSeverity(input).Should().Be(expected);
    }
}
