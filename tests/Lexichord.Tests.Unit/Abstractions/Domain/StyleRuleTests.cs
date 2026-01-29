using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Domain;

/// <summary>
/// Unit tests for StyleRule record.
/// </summary>
/// <remarks>
/// LOGIC: Verifies pattern matching, violation detection, and helper methods.
/// </remarks>
public class StyleRuleTests
{
    [Fact]
    public async Task FindViolationsAsync_FindsRegexMatches()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "test",
            Name: "Test",
            Description: "Test rule",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: @"\btest\b",
            PatternType: PatternType.Regex,
            Suggestion: "Use 'example'",
            IsEnabled: true);

        var content = "This is a test of the test system.";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindViolationsAsync_ReturnsEmpty_WhenDisabled()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "test",
            Name: "Test",
            Description: "Test rule",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: @"\btest\b",
            PatternType: PatternType.Regex,
            Suggestion: null,
            IsEnabled: false);

        var content = "This is a test.";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public async Task FindViolationsAsync_ComputesCorrectPositions()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "test",
            Name: "Test",
            Description: "Test rule",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: null,
            IsEnabled: true);

        var content = "Line 1\nLine 2 has test here\nLine 3";
        //             0123456 789...

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().HaveCount(1);
        violations[0].StartLine.Should().Be(2);
        violations[0].StartColumn.Should().Be(12);
    }

    [Fact]
    public async Task FindViolationsAsync_HandlesLiteralIgnoreCase()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "todo",
            Name: "TODO Found",
            Description: "Remove TODO comments",
            Category: RuleCategory.Formatting,
            DefaultSeverity: ViolationSeverity.Info,
            Pattern: "TODO",
            PatternType: PatternType.LiteralIgnoreCase,
            Suggestion: "Address this TODO item",
            IsEnabled: true);

        var content = "TODO: fix this\ntodo: and this\nToDo: also this";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().HaveCount(3);
    }

    [Fact]
    public async Task FindViolationsAsync_ReturnsEmpty_WhenNoMatches()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "test",
            Name: "Test",
            Description: "Test rule",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "notfound",
            PatternType: PatternType.Literal,
            Suggestion: null,
            IsEnabled: true);

        var content = "This content has no matches.";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public async Task FindViolationsAsync_ReturnsEmpty_WhenContentIsEmpty()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "test",
            Name: "Test",
            Description: "Test rule",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "test",
            PatternType: PatternType.Literal,
            Suggestion: null,
            IsEnabled: true);

        // Act
        var violations = await rule.FindViolationsAsync(string.Empty);

        // Assert
        violations.Should().BeEmpty();
    }

    [Fact]
    public async Task FindViolationsAsync_HandlesInvalidRegex()
    {
        // Arrange - invalid regex pattern
        var rule = new StyleRule(
            Id: "bad-regex",
            Name: "Bad Regex",
            Description: "Invalid pattern",
            Category: RuleCategory.Terminology,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: "[invalid",
            PatternType: PatternType.Regex,
            Suggestion: null,
            IsEnabled: true);

        var content = "Some content";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert - should not throw, just return empty
        violations.Should().BeEmpty();
    }

    [Fact]
    public void Disable_CreatesDisabledCopy()
    {
        // Arrange
        var rule = new StyleRule("id", "name", "desc",
            RuleCategory.Terminology, ViolationSeverity.Warning,
            "pattern", PatternType.Literal, null, IsEnabled: true);

        // Act
        var disabled = rule.Disable();

        // Assert
        disabled.IsEnabled.Should().BeFalse();
        rule.IsEnabled.Should().BeTrue(); // Original unchanged
    }

    [Fact]
    public void Enable_CreatesEnabledCopy()
    {
        // Arrange
        var rule = new StyleRule("id", "name", "desc",
            RuleCategory.Terminology, ViolationSeverity.Warning,
            "pattern", PatternType.Literal, null, IsEnabled: false);

        // Act
        var enabled = rule.Enable();

        // Assert
        enabled.IsEnabled.Should().BeTrue();
        rule.IsEnabled.Should().BeFalse(); // Original unchanged
    }

    [Fact]
    public void WithSeverity_CreatesCopyWithNewSeverity()
    {
        // Arrange
        var rule = new StyleRule("id", "name", "desc",
            RuleCategory.Terminology, ViolationSeverity.Warning,
            "pattern", PatternType.Literal, null);

        // Act
        var elevated = rule.WithSeverity(ViolationSeverity.Error);

        // Assert
        elevated.DefaultSeverity.Should().Be(ViolationSeverity.Error);
        rule.DefaultSeverity.Should().Be(ViolationSeverity.Warning);
    }

    [Fact]
    public void WithPattern_CreatesCopyWithNewPattern()
    {
        // Arrange
        var rule = new StyleRule("id", "name", "desc",
            RuleCategory.Terminology, ViolationSeverity.Warning,
            "oldpattern", PatternType.Literal, null);

        // Act
        var updated = rule.WithPattern("newpattern", PatternType.Regex);

        // Assert
        updated.Pattern.Should().Be("newpattern");
        updated.PatternType.Should().Be(PatternType.Regex);
        rule.Pattern.Should().Be("oldpattern");
    }

    [Fact]
    public async Task FindViolationsAsync_HandlesStartsWith()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "header",
            Name: "Header Check",
            Description: "Check for header pattern",
            Category: RuleCategory.Formatting,
            DefaultSeverity: ViolationSeverity.Info,
            Pattern: "##",
            PatternType: PatternType.StartsWith,
            Suggestion: null,
            IsEnabled: true);

        var content = "## Header 1\nSome text\n## Header 2";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindViolationsAsync_HandlesEndsWith()
    {
        // Arrange
        var rule = new StyleRule(
            Id: "trailing",
            Name: "Trailing Check",
            Description: "Check for trailing pattern",
            Category: RuleCategory.Formatting,
            DefaultSeverity: ViolationSeverity.Info,
            Pattern: ".",
            PatternType: PatternType.EndsWith,
            Suggestion: null,
            IsEnabled: true);

        var content = "Sentence one.\nNo period here\nSentence two.";

        // Act
        var violations = await rule.FindViolationsAsync(content);

        // Assert
        violations.Should().HaveCount(2);
    }
}
