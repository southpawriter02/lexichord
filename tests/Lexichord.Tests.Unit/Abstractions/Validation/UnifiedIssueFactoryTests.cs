// -----------------------------------------------------------------------
// <copyright file="UnifiedIssueFactoryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;

// Alias to disambiguate from Integration.UnifiedFix
using UnifiedFix = Lexichord.Abstractions.Contracts.Validation.UnifiedFix;

namespace Lexichord.Tests.Unit.Abstractions.Validation;

/// <summary>
/// Unit tests for <see cref="UnifiedIssueFactory"/> static factory class.
/// </summary>
/// <remarks>
/// <para>
/// Test Groups:
/// <list type="bullet">
///   <item><description>FromStyleDeviation — Conversion from Tuning Agent deviations</description></item>
///   <item><description>FromStyleViolation — Conversion from Style Linter violations</description></item>
///   <item><description>FromUnifiedFinding — Conversion from CKVS unified findings</description></item>
///   <item><description>FromValidationFinding — Conversion from CKVS validation findings</description></item>
///   <item><description>Batch conversions — FromDeviations, FromViolations, FromFindings</description></item>
///   <item><description>Null handling — ArgumentNullException tests</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.5e")]
public class UnifiedIssueFactoryTests
{
    #region FromStyleDeviation Tests

    /// <summary>
    /// Verifies that FromStyleDeviation correctly converts a deviation to an issue.
    /// </summary>
    [Fact]
    public void FromStyleDeviation_ConvertsCorrectly()
    {
        // Arrange
        var deviation = CreateTestDeviation();

        // Act
        var issue = UnifiedIssueFactory.FromStyleDeviation(deviation);

        // Assert
        issue.IssueId.Should().Be(deviation.DeviationId);
        issue.SourceId.Should().Be(deviation.RuleId);
        issue.Category.Should().Be(IssueCategory.Style);
        issue.Message.Should().Be(deviation.Message);
        issue.Location.Should().Be(deviation.Location);
        issue.OriginalText.Should().Be(deviation.OriginalText);
        issue.SourceType.Should().Be("StyleLinter");
        issue.OriginalSource.Should().Be(deviation);
    }

    /// <summary>
    /// Verifies that FromStyleDeviation maps priority to severity correctly.
    /// </summary>
    [Theory]
    [InlineData(DeviationPriority.Critical, UnifiedSeverity.Error)]
    [InlineData(DeviationPriority.High, UnifiedSeverity.Warning)]
    [InlineData(DeviationPriority.Normal, UnifiedSeverity.Info)]
    [InlineData(DeviationPriority.Low, UnifiedSeverity.Hint)]
    public void FromStyleDeviation_MapsPriorityToSeverity(
        DeviationPriority priority, UnifiedSeverity expectedSeverity)
    {
        // Arrange
        var deviation = CreateTestDeviation(priority: priority);

        // Act
        var issue = UnifiedIssueFactory.FromStyleDeviation(deviation);

        // Assert
        issue.Severity.Should().Be(expectedSeverity);
    }

    /// <summary>
    /// Verifies that FromStyleDeviation creates fix from linter suggestion.
    /// </summary>
    [Fact]
    public void FromStyleDeviation_WithSuggestion_CreatesFix()
    {
        // Arrange
        var deviation = CreateTestDeviation(suggestion: "suggested fix");

        // Act
        var issue = UnifiedIssueFactory.FromStyleDeviation(deviation);

        // Assert
        issue.Fixes.Should().HaveCount(1);
        issue.Fixes[0].NewText.Should().Be("suggested fix");
        issue.Fixes[0].Type.Should().Be(FixType.Replacement);
    }

    /// <summary>
    /// Verifies that FromStyleDeviation returns empty fixes when no suggestion and auto-fixable.
    /// </summary>
    [Fact]
    public void FromStyleDeviation_AutoFixableNoSuggestion_ReturnsEmptyFixes()
    {
        // Arrange
        var deviation = CreateTestDeviation(suggestion: null, isAutoFixable: true);

        // Act
        var issue = UnifiedIssueFactory.FromStyleDeviation(deviation);

        // Assert
        issue.Fixes.Should().BeEmpty(); // AI will generate fixes
    }

    /// <summary>
    /// Verifies that FromStyleDeviation returns NoFix when not auto-fixable.
    /// </summary>
    [Fact]
    public void FromStyleDeviation_NotAutoFixable_ReturnsNoFix()
    {
        // Arrange
        var deviation = CreateTestDeviation(suggestion: null, isAutoFixable: false);

        // Act
        var issue = UnifiedIssueFactory.FromStyleDeviation(deviation);

        // Assert
        issue.Fixes.Should().HaveCount(1);
        issue.Fixes[0].Type.Should().Be(FixType.NoFix);
    }

    /// <summary>
    /// Verifies that FromStyleDeviation throws on null input.
    /// </summary>
    [Fact]
    public void FromStyleDeviation_NullInput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => UnifiedIssueFactory.FromStyleDeviation(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("deviation");
    }

    #endregion

    #region FromStyleViolation Tests

    /// <summary>
    /// Verifies that FromStyleViolation correctly converts a violation to an issue.
    /// </summary>
    [Fact]
    public void FromStyleViolation_ConvertsCorrectly()
    {
        // Arrange
        var violation = CreateTestViolation();

        // Act
        var issue = UnifiedIssueFactory.FromStyleViolation(violation);

        // Assert
        issue.SourceId.Should().Be(violation.Rule.Id);
        issue.Message.Should().Be(violation.Message);
        issue.Location.Start.Should().Be(violation.StartOffset);
        issue.Location.End.Should().Be(violation.EndOffset);
        issue.OriginalText.Should().Be(violation.MatchedText);
        issue.SourceType.Should().Be("StyleLinter");
        issue.OriginalSource.Should().Be(violation);
    }

    /// <summary>
    /// Verifies that FromStyleViolation maps severity correctly.
    /// </summary>
    [Theory]
    [InlineData(ViolationSeverity.Error, UnifiedSeverity.Error)]
    [InlineData(ViolationSeverity.Warning, UnifiedSeverity.Warning)]
    [InlineData(ViolationSeverity.Info, UnifiedSeverity.Info)]
    [InlineData(ViolationSeverity.Hint, UnifiedSeverity.Hint)]
    public void FromStyleViolation_MapsSeverityCorrectly(
        ViolationSeverity violationSeverity, UnifiedSeverity expectedSeverity)
    {
        // Arrange
        var violation = CreateTestViolation(severity: violationSeverity);

        // Act
        var issue = UnifiedIssueFactory.FromStyleViolation(violation);

        // Assert
        issue.Severity.Should().Be(expectedSeverity);
    }

    /// <summary>
    /// Verifies that FromStyleViolation creates fix from suggestion.
    /// </summary>
    [Fact]
    public void FromStyleViolation_WithSuggestion_CreatesFix()
    {
        // Arrange
        var violation = CreateTestViolation(suggestion: "replacement text");

        // Act
        var issue = UnifiedIssueFactory.FromStyleViolation(violation);

        // Assert
        issue.Fixes.Should().HaveCount(1);
        issue.Fixes[0].NewText.Should().Be("replacement text");
    }

    /// <summary>
    /// Verifies that FromStyleViolation returns empty fixes when no suggestion.
    /// </summary>
    [Fact]
    public void FromStyleViolation_NoSuggestion_ReturnsEmptyFixes()
    {
        // Arrange
        var violation = CreateTestViolation(suggestion: null);

        // Act
        var issue = UnifiedIssueFactory.FromStyleViolation(violation);

        // Assert
        issue.Fixes.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FromStyleViolation throws on null input.
    /// </summary>
    [Fact]
    public void FromStyleViolation_NullInput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => UnifiedIssueFactory.FromStyleViolation(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("violation");
    }

    #endregion

    #region FromUnifiedFinding Tests

    /// <summary>
    /// Verifies that FromUnifiedFinding correctly converts a finding to an issue.
    /// </summary>
    [Fact]
    public void FromUnifiedFinding_ConvertsCorrectly()
    {
        // Arrange
        var finding = CreateTestUnifiedFinding();

        // Act
        var issue = UnifiedIssueFactory.FromUnifiedFinding(finding);

        // Assert
        issue.IssueId.Should().Be(finding.Id);
        issue.SourceId.Should().Be(finding.Code);
        issue.Severity.Should().Be(finding.Severity);
        issue.Message.Should().Be(finding.Message);
        issue.SourceType.Should().Be("StyleLinter");
        issue.OriginalSource.Should().Be(finding);
    }

    /// <summary>
    /// Verifies that FromUnifiedFinding maps FindingCategory correctly.
    /// </summary>
    [Theory]
    [InlineData(FindingCategory.Schema, IssueCategory.Knowledge)]
    [InlineData(FindingCategory.Axiom, IssueCategory.Knowledge)]
    [InlineData(FindingCategory.Consistency, IssueCategory.Knowledge)]
    [InlineData(FindingCategory.Style, IssueCategory.Style)]
    [InlineData(FindingCategory.Grammar, IssueCategory.Grammar)]
    [InlineData(FindingCategory.Spelling, IssueCategory.Grammar)]
    public void FromUnifiedFinding_MapsCategoryCorrectly(
        FindingCategory findingCategory, IssueCategory expectedCategory)
    {
        // Arrange
        var finding = CreateTestUnifiedFinding(category: findingCategory);

        // Act
        var issue = UnifiedIssueFactory.FromUnifiedFinding(finding);

        // Assert
        issue.Category.Should().Be(expectedCategory);
    }

    /// <summary>
    /// Verifies that FromUnifiedFinding maps FindingSource to SourceType correctly.
    /// </summary>
    [Theory]
    [InlineData(FindingSource.Validation, "Validation")]
    [InlineData(FindingSource.StyleLinter, "StyleLinter")]
    [InlineData(FindingSource.GrammarLinter, "GrammarLinter")]
    public void FromUnifiedFinding_MapsSourceTypeCorrectly(
        FindingSource source, string expectedSourceType)
    {
        // Arrange
        var finding = CreateTestUnifiedFinding(source: source);

        // Act
        var issue = UnifiedIssueFactory.FromUnifiedFinding(finding);

        // Assert
        issue.SourceType.Should().Be(expectedSourceType);
    }

    /// <summary>
    /// Verifies that FromUnifiedFinding extracts location from original violation.
    /// </summary>
    [Fact]
    public void FromUnifiedFinding_WithViolation_ExtractsLocation()
    {
        // Arrange
        var violation = CreateTestViolation();
        var finding = CreateTestUnifiedFinding(originalViolation: violation);

        // Act
        var issue = UnifiedIssueFactory.FromUnifiedFinding(finding);

        // Assert
        issue.Location.Start.Should().Be(violation.StartOffset);
        issue.Location.End.Should().Be(violation.EndOffset);
        issue.OriginalText.Should().Be(violation.MatchedText);
    }

    /// <summary>
    /// Verifies that FromUnifiedFinding throws on null input.
    /// </summary>
    [Fact]
    public void FromUnifiedFinding_NullInput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => UnifiedIssueFactory.FromUnifiedFinding(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("finding");
    }

    #endregion

    #region FromValidationFinding Tests

    /// <summary>
    /// Verifies that FromValidationFinding correctly converts a finding to an issue.
    /// </summary>
    [Fact]
    public void FromValidationFinding_ConvertsCorrectly()
    {
        // Arrange
        var finding = CreateTestValidationFinding();

        // Act
        var issue = UnifiedIssueFactory.FromValidationFinding(finding);

        // Assert
        issue.SourceId.Should().Be(finding.Code);
        issue.Category.Should().Be(IssueCategory.Knowledge);
        issue.Message.Should().Be(finding.Message);
        issue.SourceType.Should().Be("Validation");
        issue.OriginalSource.Should().Be(finding);
    }

    /// <summary>
    /// Verifies that FromValidationFinding maps severity correctly.
    /// </summary>
    [Theory]
    [InlineData(ValidationSeverity.Error, UnifiedSeverity.Error)]
    [InlineData(ValidationSeverity.Warning, UnifiedSeverity.Warning)]
    [InlineData(ValidationSeverity.Info, UnifiedSeverity.Info)]
    public void FromValidationFinding_MapsSeverityCorrectly(
        ValidationSeverity validationSeverity, UnifiedSeverity expectedSeverity)
    {
        // Arrange
        var finding = CreateTestValidationFinding(severity: validationSeverity);

        // Act
        var issue = UnifiedIssueFactory.FromValidationFinding(finding);

        // Assert
        issue.Severity.Should().Be(expectedSeverity);
    }

    /// <summary>
    /// Verifies that FromValidationFinding creates fix from suggested fix.
    /// </summary>
    [Fact]
    public void FromValidationFinding_WithSuggestedFix_CreatesFix()
    {
        // Arrange
        var finding = CreateTestValidationFinding(suggestedFix: "suggested repair");

        // Act
        var issue = UnifiedIssueFactory.FromValidationFinding(finding);

        // Assert
        issue.Fixes.Should().HaveCount(1);
        issue.Fixes[0].NewText.Should().Be("suggested repair");
        issue.Fixes[0].Type.Should().Be(FixType.Rewrite);
        issue.Fixes[0].CanAutoApply.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that FromValidationFinding throws on null input.
    /// </summary>
    [Fact]
    public void FromValidationFinding_NullInput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => UnifiedIssueFactory.FromValidationFinding(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("finding");
    }

    #endregion

    #region Batch Conversion Tests

    /// <summary>
    /// Verifies that FromDeviations converts multiple deviations.
    /// </summary>
    [Fact]
    public void FromDeviations_ConvertsMultipleDeviations()
    {
        // Arrange
        var deviations = new[]
        {
            CreateTestDeviation(deviationId: Guid.NewGuid()),
            CreateTestDeviation(deviationId: Guid.NewGuid()),
            CreateTestDeviation(deviationId: Guid.NewGuid())
        };

        // Act
        var issues = UnifiedIssueFactory.FromDeviations(deviations);

        // Assert
        issues.Should().HaveCount(3);
        issues[0].IssueId.Should().Be(deviations[0].DeviationId);
        issues[1].IssueId.Should().Be(deviations[1].DeviationId);
        issues[2].IssueId.Should().Be(deviations[2].DeviationId);
    }

    /// <summary>
    /// Verifies that FromDeviations returns empty list for empty input.
    /// </summary>
    [Fact]
    public void FromDeviations_EmptyInput_ReturnsEmptyList()
    {
        // Act
        var issues = UnifiedIssueFactory.FromDeviations(Array.Empty<StyleDeviation>());

        // Assert
        issues.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that FromDeviations throws on null input.
    /// </summary>
    [Fact]
    public void FromDeviations_NullInput_ThrowsArgumentNullException()
    {
        // Act
        var act = () => UnifiedIssueFactory.FromDeviations(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("deviations");
    }

    /// <summary>
    /// Verifies that FromViolations converts multiple violations.
    /// </summary>
    [Fact]
    public void FromViolations_ConvertsMultipleViolations()
    {
        // Arrange
        var violations = new[]
        {
            CreateTestViolation(matchedText: "text1"),
            CreateTestViolation(matchedText: "text2")
        };

        // Act
        var issues = UnifiedIssueFactory.FromViolations(violations);

        // Assert
        issues.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that FromFindings converts multiple findings.
    /// </summary>
    [Fact]
    public void FromFindings_ConvertsMultipleFindings()
    {
        // Arrange
        var findings = new[]
        {
            CreateTestUnifiedFinding(code: "CODE1"),
            CreateTestUnifiedFinding(code: "CODE2")
        };

        // Act
        var issues = UnifiedIssueFactory.FromFindings(findings);

        // Assert
        issues.Should().HaveCount(2);
        issues[0].SourceId.Should().Be("CODE1");
        issues[1].SourceId.Should().Be("CODE2");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test StyleDeviation.
    /// </summary>
    private static StyleDeviation CreateTestDeviation(
        Guid? deviationId = null,
        DeviationPriority priority = DeviationPriority.Normal,
        string? suggestion = null,
        bool isAutoFixable = true)
    {
        var rule = CreateTestRule(suggestion: suggestion);
        var violation = CreateTestViolation(rule: rule, suggestion: suggestion);

        return new StyleDeviation
        {
            DeviationId = deviationId ?? Guid.NewGuid(),
            Violation = violation,
            Location = new TextSpan(violation.StartOffset, violation.Length),
            OriginalText = violation.MatchedText,
            SurroundingContext = "context around the " + violation.MatchedText + " text",
            ViolatedRule = rule,
            IsAutoFixable = isAutoFixable,
            Priority = priority
        };
    }

    /// <summary>
    /// Creates a test StyleRule.
    /// </summary>
    private static StyleRule CreateTestRule(
        string id = "test-rule",
        RuleCategory category = RuleCategory.Terminology,
        ViolationSeverity severity = ViolationSeverity.Warning,
        string? suggestion = null) =>
        new(
            Id: id,
            Name: "Test Rule",
            Description: "Test description",
            Category: category,
            DefaultSeverity: severity,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: suggestion);

    /// <summary>
    /// Creates a test StyleViolation.
    /// </summary>
    private static StyleViolation CreateTestViolation(
        StyleRule? rule = null,
        ViolationSeverity severity = ViolationSeverity.Warning,
        string? suggestion = null,
        string matchedText = "test")
    {
        rule ??= CreateTestRule(severity: severity, suggestion: suggestion);

        return new StyleViolation(
            Rule: rule,
            Message: "Test violation message",
            StartOffset: 10,
            EndOffset: 14,
            StartLine: 1,
            StartColumn: 11,
            EndLine: 1,
            EndColumn: 15,
            MatchedText: matchedText,
            Suggestion: suggestion,
            Severity: severity);
    }

    /// <summary>
    /// Creates a test UnifiedFinding.
    /// </summary>
    private static UnifiedFinding CreateTestUnifiedFinding(
        FindingSource source = FindingSource.StyleLinter,
        FindingCategory category = FindingCategory.Style,
        string code = "TEST001",
        StyleViolation? originalViolation = null)
    {
        return new UnifiedFinding
        {
            Id = Guid.NewGuid(),
            Source = source,
            Severity = UnifiedSeverity.Warning,
            Code = code,
            Message = "Test finding message",
            Category = category,
            OriginalStyleViolation = originalViolation
        };
    }

    /// <summary>
    /// Creates a test ValidationFinding.
    /// </summary>
    private static ValidationFinding CreateTestValidationFinding(
        ValidationSeverity severity = ValidationSeverity.Warning,
        string? suggestedFix = null)
    {
        return new ValidationFinding(
            ValidatorId: "test-validator",
            Severity: severity,
            Code: "VAL001",
            Message: "Validation finding message",
            PropertyPath: "document.field",
            SuggestedFix: suggestedFix);
    }

    #endregion
}
