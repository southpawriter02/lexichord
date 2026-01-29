using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for YamlStyleSheetLoader.
/// </summary>
/// <remarks>
/// LOGIC: Tests YAML parsing, validation, DTO conversion, and embedded resource loading.
/// </remarks>
public class YamlStyleSheetLoaderTests
{
    private readonly YamlStyleSheetLoader _loader;

    public YamlStyleSheetLoaderTests()
    {
        var loggerMock = new Mock<ILogger<YamlStyleSheetLoader>>();
        _loader = new YamlStyleSheetLoader(loggerMock.Object);
    }

    #region LoadFromStreamAsync Tests

    [Fact]
    public async Task LoadFromStreamAsync_ParsesValidYaml()
    {
        // Arrange
        var yaml = """
            name: Test Style
            version: "1.0"
            author: Test Author
            description: A test style sheet
            rules:
              - id: test-rule
                name: Test Rule
                description: A test rule
                pattern: test
                pattern_type: literal
                category: terminology
                severity: warning
            """;
        using var stream = CreateStream(yaml);

        // Act
        var sheet = await _loader.LoadFromStreamAsync(stream);

        // Assert
        sheet.Name.Should().Be("Test Style");
        sheet.Version.Should().Be("1.0");
        sheet.Author.Should().Be("Test Author");
        sheet.Description.Should().Be("A test style sheet");
        sheet.Rules.Should().HaveCount(1);
        sheet.Rules[0].Id.Should().Be("test-rule");
    }

    [Fact]
    public async Task LoadFromStreamAsync_AppliesDefaultValues()
    {
        // Arrange - minimal YAML with only required fields
        var yaml = """
            name: Minimal
            rules:
              - id: min-rule
                name: Minimal Rule
                description: Minimal description
                pattern: test
            """;
        using var stream = CreateStream(yaml);

        // Act
        var sheet = await _loader.LoadFromStreamAsync(stream);

        // Assert - verify defaults
        sheet.Rules[0].Category.Should().Be(RuleCategory.Terminology);
        sheet.Rules[0].DefaultSeverity.Should().Be(ViolationSeverity.Warning);
        sheet.Rules[0].PatternType.Should().Be(PatternType.Regex);
        sheet.Rules[0].IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task LoadFromStreamAsync_ParsesAllPatternTypes()
    {
        // Arrange
        var yaml = """
            name: Pattern Types Test
            rules:
              - id: regex-rule
                name: Regex
                description: Regex pattern
                pattern: \btest\b
                pattern_type: regex

              - id: literal-rule
                name: Literal
                description: Literal pattern
                pattern: test
                pattern_type: literal

              - id: ignore-case-rule
                name: Ignore Case
                description: Case insensitive
                pattern: TEST
                pattern_type: literal_ignore_case

              - id: starts-with-rule
                name: Starts With
                description: Line start
                pattern: "##"
                pattern_type: starts_with

              - id: ends-with-rule
                name: Ends With
                description: Line end
                pattern: "."
                pattern_type: ends_with

              - id: contains-rule
                name: Contains
                description: Substring
                pattern: jargon
                pattern_type: contains
            """;
        using var stream = CreateStream(yaml);

        // Act
        var sheet = await _loader.LoadFromStreamAsync(stream);

        // Assert
        sheet.Rules.Should().HaveCount(6);
        sheet.Rules[0].PatternType.Should().Be(PatternType.Regex);
        sheet.Rules[1].PatternType.Should().Be(PatternType.Literal);
        sheet.Rules[2].PatternType.Should().Be(PatternType.LiteralIgnoreCase);
        sheet.Rules[3].PatternType.Should().Be(PatternType.StartsWith);
        sheet.Rules[4].PatternType.Should().Be(PatternType.EndsWith);
        sheet.Rules[5].PatternType.Should().Be(PatternType.Contains);
    }

    [Fact]
    public async Task LoadFromStreamAsync_ParsesAllSeverities()
    {
        // Arrange
        var yaml = """
            name: Severities Test
            rules:
              - id: error-rule
                name: Error
                description: Error severity
                pattern: error
                severity: error

              - id: warning-rule
                name: Warning
                description: Warning severity
                pattern: warning
                severity: warning

              - id: info-rule
                name: Info
                description: Info severity
                pattern: info
                severity: info

              - id: hint-rule
                name: Hint
                description: Hint severity
                pattern: hint
                severity: hint
            """;
        using var stream = CreateStream(yaml);

        // Act
        var sheet = await _loader.LoadFromStreamAsync(stream);

        // Assert
        sheet.Rules[0].DefaultSeverity.Should().Be(ViolationSeverity.Error);
        sheet.Rules[1].DefaultSeverity.Should().Be(ViolationSeverity.Warning);
        sheet.Rules[2].DefaultSeverity.Should().Be(ViolationSeverity.Info);
        sheet.Rules[3].DefaultSeverity.Should().Be(ViolationSeverity.Hint);
    }

    [Fact]
    public async Task LoadFromStreamAsync_ThrowsOnMissingRequiredFields()
    {
        // Arrange - missing 'pattern' field
        var yaml = """
            name: Invalid
            rules:
              - id: incomplete
                name: Incomplete Rule
                description: Missing pattern
            """;
        using var stream = CreateStream(yaml);

        // Act & Assert
        var action = async () => await _loader.LoadFromStreamAsync(stream);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Missing required field 'pattern'*");
    }

    [Fact]
    public async Task LoadFromStreamAsync_ThrowsOnInvalidYamlSyntax()
    {
        // Arrange - invalid YAML syntax
        var yaml = """
            name: Invalid
            rules:
              - id: bad-indent
                  name: Wrong indent
            """;
        using var stream = CreateStream(yaml);

        // Act & Assert
        var action = async () => await _loader.LoadFromStreamAsync(stream);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*YAML syntax error*");
    }

    [Fact]
    public async Task LoadFromStreamAsync_ThrowsOnDuplicateIds()
    {
        // Arrange
        var yaml = """
            name: Duplicate IDs
            rules:
              - id: same-id
                name: First Rule
                description: First
                pattern: first

              - id: same-id
                name: Second Rule
                description: Second
                pattern: second
            """;
        using var stream = CreateStream(yaml);

        // Act & Assert
        var action = async () => await _loader.LoadFromStreamAsync(stream);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Duplicate rule id 'same-id'*");
    }

    [Fact]
    public async Task LoadFromStreamAsync_ThrowsOnInvalidIdFormat()
    {
        // Arrange - ID with uppercase (not kebab-case)
        var yaml = """
            name: Invalid ID
            rules:
              - id: InvalidCase
                name: Bad ID
                description: Not kebab-case
                pattern: test
            """;
        using var stream = CreateStream(yaml);

        // Act & Assert
        var action = async () => await _loader.LoadFromStreamAsync(stream);
        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*'id' must be kebab-case*");
    }

    #endregion

    #region LoadEmbeddedDefaultAsync Tests

    [Fact]
    public async Task LoadEmbeddedDefaultAsync_LoadsDefaultStyleSheet()
    {
        // Act
        var sheet = await _loader.LoadEmbeddedDefaultAsync();

        // Assert
        sheet.Name.Should().Be("Lexichord Default");
        sheet.Version.Should().Be("0.2.1");
        sheet.Rules.Should().HaveCountGreaterThan(20);
    }

    [Fact]
    public async Task LoadEmbeddedDefaultAsync_ContainsExpectedRuleCategories()
    {
        // Act
        var sheet = await _loader.LoadEmbeddedDefaultAsync();

        // Assert - should have rules in all categories
        sheet.GetRulesByCategory(RuleCategory.Terminology).Should().NotBeEmpty();
        sheet.GetRulesByCategory(RuleCategory.Formatting).Should().NotBeEmpty();
        sheet.GetRulesByCategory(RuleCategory.Syntax).Should().NotBeEmpty();
    }

    [Fact]
    public async Task LoadEmbeddedDefaultAsync_AllRulesAreEnabled()
    {
        // Act
        var sheet = await _loader.LoadEmbeddedDefaultAsync();

        // Assert
        sheet.Rules.Should().OnlyContain(r => r.IsEnabled);
    }

    [Fact]
    public async Task LoadEmbeddedDefaultAsync_ContainsNoJargonRule()
    {
        // Act
        var sheet = await _loader.LoadEmbeddedDefaultAsync();

        // Assert
        var rule = sheet.FindRuleById("no-jargon");
        rule.Should().NotBeNull();
        rule!.Category.Should().Be(RuleCategory.Terminology);
        rule.PatternType.Should().Be(PatternType.Regex);
    }

    #endregion

    #region ValidateYaml Tests

    [Fact]
    public void ValidateYaml_ReturnsSuccess_ForValidYaml()
    {
        // Arrange
        var yaml = """
            name: Valid
            rules:
              - id: valid-rule
                name: Valid Rule
                description: Valid description
                pattern: test
            """;

        // Act
        var result = _loader.ValidateYaml(yaml);

        // Assert
        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void ValidateYaml_ReturnsFailure_ForEmptyContent()
    {
        // Act
        var result = _loader.ValidateYaml("");

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("empty");
    }

    [Fact]
    public void ValidateYaml_ReturnsFailure_ForMissingName()
    {
        // Arrange
        var yaml = """
            rules:
              - id: test-rule
                name: Test
                description: Test
                pattern: test
            """;

        // Act
        var result = _loader.ValidateYaml(yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("name");
    }

    [Fact]
    public void ValidateYaml_ReturnsFailure_ForInvalidCategory()
    {
        // Arrange
        var yaml = """
            name: Test
            rules:
              - id: test-rule
                name: Test
                description: Test
                pattern: test
                category: invalid-category
            """;

        // Act
        var result = _loader.ValidateYaml(yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid category");
    }

    [Fact]
    public void ValidateYaml_ReturnsFailure_ForInvalidRegexPattern()
    {
        // Arrange
        var yaml = """
            name: Test
            rules:
              - id: test-rule
                name: Test
                description: Test
                pattern: "[invalid"
                pattern_type: regex
            """;

        // Act
        var result = _loader.ValidateYaml(yaml);

        // Assert
        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Invalid regex pattern");
    }

    #endregion

    #region Helper Methods

    private static MemoryStream CreateStream(string content)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(content);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    #endregion
}
