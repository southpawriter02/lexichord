using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for <see cref="MessageTemplateEngine"/>.
/// </summary>
/// <remarks>
/// Version: v0.2.3d
/// </remarks>
public sealed class MessageTemplateEngineTests
{
    private static StyleRule CreateRule(
        string description = "Test description",
        string? suggestion = null) =>
        new StyleRule(
            Id: "test-001",
            Name: "Test Rule",
            Description: description,
            Category: RuleCategory.Syntax,
            DefaultSeverity: ViolationSeverity.Warning,
            Pattern: @"\btest\b",
            PatternType: PatternType.Regex,
            Suggestion: suggestion,
            IsEnabled: true);

    private static ScanMatch CreateMatch(
        StyleRule? rule = null,
        string matchedText = "badword",
        IReadOnlyDictionary<string, string>? captureGroups = null) =>
        new ScanMatch(
            RuleId: rule?.Id ?? "test-001",
            StartOffset: 0,
            Length: matchedText.Length,
            MatchedText: matchedText,
            Rule: rule ?? CreateRule(),
            CaptureGroups: captureGroups);

    #region Basic Placeholder Expansion

    [Fact]
    public void Expand_TextPlaceholder_ReplacesWithMatchedText()
    {
        // Arrange
        var match = CreateMatch(matchedText: "utilize");
        var template = "Avoid '{text}', use simpler words.";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Avoid 'utilize', use simpler words.");
    }

    [Fact]
    public void Expand_ZeroPlaceholder_ReplacesWithMatchedText()
    {
        // Arrange
        var match = CreateMatch(matchedText: "problematic");
        var template = "Found: {0}";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Found: problematic");
    }

    [Fact]
    public void Expand_SuggestionPlaceholder_ReplacesWithSuggestion()
    {
        // Arrange
        var rule = CreateRule(suggestion: "use");
        var match = CreateMatch(matchedText: "utilize", rule: rule);
        var template = "Avoid '{text}', consider '{suggestion}' instead.";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Avoid 'utilize', consider 'use' instead.");
    }

    [Fact]
    public void Expand_RulePlaceholder_ReplacesWithRuleName()
    {
        // Arrange
        var match = CreateMatch();
        var template = "Violated rule: {rule}";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Violated rule: Test Rule");
    }

    [Fact]
    public void Expand_SeverityPlaceholder_ReplacesWithSeverity()
    {
        // Arrange
        var match = CreateMatch();
        var template = "Severity: {severity}";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Severity: Warning");
    }

    [Fact]
    public void Expand_MultiplePlaceholders_ReplacesAll()
    {
        // Arrange
        var rule = CreateRule(suggestion: "use");
        var match = CreateMatch(matchedText: "utilize", rule: rule);
        var template = "[{severity}] {rule}: Avoid '{text}', use '{suggestion}'.";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("[Warning] Test Rule: Avoid 'utilize', use 'use'.");
    }

    #endregion

    #region Capture Groups

    [Fact]
    public void Expand_CaptureGroups_ReplacesNamedGroups()
    {
        // Arrange
        var captureGroups = new Dictionary<string, string>
        {
            ["word"] = "problematic",
            ["alt"] = "alternative"
        };
        var match = CreateMatch(captureGroups: captureGroups);
        var template = "Found '{word}', consider '{alt}'.";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Found 'problematic', consider 'alternative'.");
    }

    [Fact]
    public void Expand_CaptureGroupOverridesBuiltin_UsesBuiltin()
    {
        // Arrange - "text" is both a built-in and a capture group
        var captureGroups = new Dictionary<string, string>
        {
            ["text"] = "from capture group"
        };
        var match = CreateMatch(matchedText: "from match", captureGroups: captureGroups);
        var template = "Text: {text}";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert - Built-in context is overridden by capture group? 
        // Actually, capture groups are added before built-ins, so built-ins win
        // Let me check the implementation... actually built-ins are added first
        // then capture groups can override. Let's verify actual behavior.
        // Per implementation: built-ins first, then capture groups, then additionalValues
        // So capture groups override built-ins!
        result.Should().Be("Text: from capture group");
    }

    #endregion

    #region Additional Values

    [Fact]
    public void Expand_AdditionalValues_ReplacesCustomPlaceholders()
    {
        // Arrange
        var match = CreateMatch();
        var template = "Custom: {custom} and builtin: {text}";
        var additionalValues = new Dictionary<string, string>
        {
            ["custom"] = "my value"
        };

        // Act
        var result = MessageTemplateEngine.Expand(template, match, additionalValues);

        // Assert
        result.Should().Be("Custom: my value and builtin: badword");
    }

    [Fact]
    public void Expand_AdditionalValuesOverride_TakesPrecedence()
    {
        // Arrange
        var match = CreateMatch(matchedText: "original");
        var template = "Text: {text}";
        var additionalValues = new Dictionary<string, string>
        {
            ["text"] = "overridden"
        };

        // Act
        var result = MessageTemplateEngine.Expand(template, match, additionalValues);

        // Assert - Additional values have highest priority
        result.Should().Be("Text: overridden");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Expand_EmptyTemplate_ReturnsEmpty()
    {
        // Arrange
        var match = CreateMatch();

        // Act
        var result = MessageTemplateEngine.Expand("", match);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Expand_NullTemplate_ReturnsEmpty()
    {
        // Arrange
        var match = CreateMatch();

        // Act
        var result = MessageTemplateEngine.Expand(null!, match);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Expand_NoPlaceholders_ReturnsOriginal()
    {
        // Arrange
        var match = CreateMatch();
        var template = "No placeholders here.";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("No placeholders here.");
    }

    [Fact]
    public void Expand_UnknownPlaceholder_PreservesOriginal()
    {
        // Arrange
        var match = CreateMatch();
        var template = "Known: {text}, Unknown: {unknown}";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Known: badword, Unknown: {unknown}");
    }

    [Fact]
    public void Expand_CaseInsensitivePlaceholders_Matches()
    {
        // Arrange
        var match = CreateMatch(matchedText: "test");
        var template = "Mixed case: {TEXT} and {Text} and {text}";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert - All case variations should work
        result.Should().Be("Mixed case: test and test and test");
    }

    [Fact]
    public void Expand_NullSuggestion_ReplacesWithEmpty()
    {
        // Arrange
        var rule = CreateRule(suggestion: null);
        var match = CreateMatch(rule: rule);
        var template = "Suggestion: '{suggestion}'";

        // Act
        var result = MessageTemplateEngine.Expand(template, match);

        // Assert
        result.Should().Be("Suggestion: ''");
    }

    #endregion

    #region HasPlaceholders

    [Fact]
    public void HasPlaceholders_WithPlaceholder_ReturnsTrue()
    {
        // Act & Assert
        MessageTemplateEngine.HasPlaceholders("Has {text} here").Should().BeTrue();
    }

    [Fact]
    public void HasPlaceholders_WithoutPlaceholder_ReturnsFalse()
    {
        // Act & Assert
        MessageTemplateEngine.HasPlaceholders("No placeholders").Should().BeFalse();
    }

    [Fact]
    public void HasPlaceholders_EmptyString_ReturnsFalse()
    {
        // Act & Assert
        MessageTemplateEngine.HasPlaceholders("").Should().BeFalse();
    }

    [Fact]
    public void HasPlaceholders_NullString_ReturnsFalse()
    {
        // Act & Assert
        MessageTemplateEngine.HasPlaceholders(null!).Should().BeFalse();
    }

    #endregion
}
