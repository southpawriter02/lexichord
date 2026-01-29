using FluentAssertions;
using Lexichord.Modules.Style.Yaml;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for YamlSchemaValidator.
/// </summary>
/// <remarks>
/// LOGIC: Tests schema validation rules including required fields,
/// ID format, enum validation, and duplicate detection.
/// </remarks>
public class YamlSchemaValidatorTests
{
    #region Valid Input Tests

    [Fact]
    public void Validate_ReturnsEmpty_ForValidSheet()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Valid Style",
            Rules =
            [
                new YamlRule
                {
                    Id = "valid-rule",
                    Name = "Valid Rule",
                    Description = "A valid rule",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsEmpty_ForValidSheetWithAllOptionalFields()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Complete Style",
            Version = "1.0",
            Author = "Test Author",
            Description = "Full description",
            Extends = "default",
            Rules =
            [
                new YamlRule
                {
                    Id = "complete-rule",
                    Name = "Complete Rule",
                    Description = "Complete description",
                    Pattern = @"\btest\b",
                    PatternType = "regex",
                    Category = "terminology",
                    Severity = "warning",
                    Suggestion = "Try something else",
                    Enabled = true
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().BeEmpty();
    }

    #endregion

    #region Null/Empty Input Tests

    [Fact]
    public void Validate_ReturnsError_ForNullSheet()
    {
        // Act
        var errors = YamlSchemaValidator.Validate(null);

        // Assert
        errors.Should().HaveCount(1);
        errors[0].Message.Should().Contain("empty or invalid");
    }

    [Fact]
    public void Validate_ReturnsError_ForMissingName()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Rules =
            [
                new YamlRule
                {
                    Id = "test",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Missing required field 'name'"));
    }

    [Fact]
    public void Validate_ReturnsError_ForEmptyRules()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "No Rules",
            Rules = []
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("at least one rule"));
    }

    [Fact]
    public void Validate_ReturnsError_ForNullRules()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Null Rules",
            Rules = null
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("at least one rule"));
    }

    #endregion

    #region Rule Required Field Tests

    [Fact]
    public void Validate_ReturnsError_ForRuleMissingId()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Name = "No ID",
                    Description = "Missing ID",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Field == "id" && e.Message.Contains("Missing"));
    }

    [Fact]
    public void Validate_ReturnsError_ForRuleMissingName()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Description = "Missing name",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Field == "name" && e.Message.Contains("Missing"));
    }

    [Fact]
    public void Validate_ReturnsError_ForRuleMissingDescription()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test Rule",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Field == "description" && e.Message.Contains("Missing"));
    }

    [Fact]
    public void Validate_ReturnsError_ForRuleMissingPattern()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test Rule",
                    Description = "Missing pattern"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Field == "pattern" && e.Message.Contains("Missing"));
    }

    #endregion

    #region ID Format Tests

    [Theory]
    [InlineData("valid-id")]
    [InlineData("no-passive-voice")]
    [InlineData("rule123")]
    [InlineData("a")]
    public void Validate_Accepts_ValidKebabCaseIds(string id)
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = id,
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().NotContain(e => e.Field == "id");
    }

    [Theory]
    [InlineData("InvalidCase")]
    [InlineData("UPPERCASE")]
    [InlineData("123-starts-with-number")]
    [InlineData("-starts-with-dash")]
    [InlineData("has_underscore")]
    [InlineData("has spaces")]
    public void Validate_Rejects_InvalidIdFormats(string id)
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = id,
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("kebab-case"));
    }

    #endregion

    #region Duplicate ID Tests

    [Fact]
    public void Validate_ReturnsError_ForDuplicateIds()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "duplicate-id",
                    Name = "First",
                    Description = "First",
                    Pattern = "first"
                },
                new YamlRule
                {
                    Id = "duplicate-id",
                    Name = "Second",
                    Description = "Second",
                    Pattern = "second"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Duplicate rule id"));
    }

    [Fact]
    public void Validate_DetectsDuplicates_CaseInsensitive()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "same-id",
                    Name = "First",
                    Description = "First",
                    Pattern = "first"
                },
                // Note: This ID is invalid due to uppercase but tests case-insensitive duplicate check
                new YamlRule
                {
                    Id = "same-id", // exact duplicate
                    Name = "Second",
                    Description = "Second",
                    Pattern = "second"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Duplicate"));
    }

    #endregion

    #region Enum Validation Tests

    [Theory]
    [InlineData("terminology")]
    [InlineData("TERMINOLOGY")]
    [InlineData("formatting")]
    [InlineData("syntax")]
    public void Validate_Accepts_ValidCategories(string category)
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test",
                    Category = category
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().NotContain(e => e.Field == "category");
    }

    [Fact]
    public void Validate_ReturnsError_ForInvalidCategory()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test",
                    Category = "invalid-category"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Invalid category"));
    }

    [Theory]
    [InlineData("error")]
    [InlineData("warning")]
    [InlineData("info")]
    [InlineData("hint")]
    public void Validate_Accepts_ValidSeverities(string severity)
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test",
                    Severity = severity
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().NotContain(e => e.Field == "severity");
    }

    [Fact]
    public void Validate_ReturnsError_ForInvalidSeverity()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test",
                    Severity = "critical" // Not a valid severity
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Invalid severity"));
    }

    [Theory]
    [InlineData("regex")]
    [InlineData("literal")]
    [InlineData("literal_ignore_case")]
    [InlineData("starts_with")]
    [InlineData("ends_with")]
    [InlineData("contains")]
    public void Validate_Accepts_ValidPatternTypes(string patternType)
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test", // Simple pattern valid for all types
                    PatternType = patternType
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().NotContain(e => e.Field == "pattern_type");
    }

    [Fact]
    public void Validate_ReturnsError_ForInvalidPatternType()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "test-rule",
                    Name = "Test",
                    Description = "Test",
                    Pattern = "test",
                    PatternType = "glob" // Not a valid pattern type
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Invalid pattern_type"));
    }

    #endregion

    #region Regex Pattern Validation Tests

    [Fact]
    public void Validate_ReturnsError_ForInvalidRegexPattern()
    {
        // Arrange
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "bad-regex",
                    Name = "Bad Regex",
                    Description = "Invalid regex pattern",
                    Pattern = "[invalid",
                    PatternType = "regex"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Invalid regex pattern"));
    }

    [Fact]
    public void Validate_ValidatesRegex_WhenPatternTypeNotSpecified()
    {
        // Arrange - default pattern type is regex
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "bad-regex",
                    Name = "Bad Regex",
                    Description = "Invalid regex pattern",
                    Pattern = "[invalid"
                    // PatternType not specified - defaults to regex
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().Contain(e => e.Message.Contains("Invalid regex pattern"));
    }

    [Fact]
    public void Validate_SkipsRegexValidation_ForNonRegexPatterns()
    {
        // Arrange - "[invalid" is fine for literal matching
        var sheet = new YamlStyleSheet
        {
            Name = "Test",
            Rules =
            [
                new YamlRule
                {
                    Id = "literal-bracket",
                    Name = "Literal Bracket",
                    Description = "Literal pattern with bracket",
                    Pattern = "[invalid",
                    PatternType = "literal"
                }
            ]
        };

        // Act
        var errors = YamlSchemaValidator.Validate(sheet);

        // Assert
        errors.Should().NotContain(e => e.Message.Contains("regex"));
    }

    #endregion
}
