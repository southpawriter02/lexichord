using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Validation;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for TermPatternValidator.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify pattern validation logic including:
/// - Empty/null pattern rejection
/// - Length limit enforcement
/// - Regex detection
/// - Regex safety validation
/// </remarks>
[Trait("Category", "Unit")]
public class TermPatternValidatorTests
{
    #region Empty Pattern Tests

    [Fact]
    public void Validate_NullPattern_ReturnsFailure()
    {
        // Act
        var result = TermPatternValidator.Validate(null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be empty");
    }

    [Fact]
    public void Validate_EmptyPattern_ReturnsFailure()
    {
        // Act
        var result = TermPatternValidator.Validate(string.Empty);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be empty");
    }

    [Fact]
    public void Validate_WhitespacePattern_ReturnsFailure()
    {
        // Act
        var result = TermPatternValidator.Validate("   ");

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be empty");
    }

    #endregion

    #region Length Limit Tests

    [Fact]
    public void Validate_PatternAtMaxLength_ReturnsSuccess()
    {
        // Arrange
        var pattern = new string('a', TermPatternValidator.MaxPatternLength);

        // Act
        var result = TermPatternValidator.Validate(pattern);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_PatternOverMaxLength_ReturnsFailure()
    {
        // Arrange
        var pattern = new string('a', TermPatternValidator.MaxPatternLength + 1);

        // Act
        var result = TermPatternValidator.Validate(pattern);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("500 characters or less");
    }

    #endregion

    #region Literal Pattern Tests

    [Fact]
    public void Validate_SimpleLiteralPattern_ReturnsSuccess()
    {
        // Act
        var result = TermPatternValidator.Validate("simple text");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData("hello world")]
    [InlineData("utilize")]
    [InlineData("in order to")]
    [InlineData("per se")]
    public void Validate_CommonLiteralPatterns_ReturnsSuccess(string pattern)
    {
        // Act
        var result = TermPatternValidator.Validate(pattern);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Regex Detection Tests

    [Theory]
    [InlineData("test*")]
    [InlineData("test+")]
    [InlineData("test?")]
    [InlineData("^start")]
    [InlineData("end$")]
    [InlineData("[abc]")]
    [InlineData("(group)")]
    [InlineData("{1,3}")]
    [InlineData("a|b")]
    [InlineData("\\d")]
    [InlineData("test.test")]
    public void LooksLikeRegex_PatternWithMetacharacter_ReturnsTrue(string pattern)
    {
        // Act
        var result = TermPatternValidator.LooksLikeRegex(pattern);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void LooksLikeRegex_PlainText_ReturnsFalse()
    {
        // Act
        var result = TermPatternValidator.LooksLikeRegex("simple text");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Regex Validation Tests

    [Fact]
    public void Validate_ValidSimpleRegex_ReturnsSuccess()
    {
        // Act
        var result = TermPatternValidator.Validate(@"\b\w+\b");

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Validate_InvalidRegexSyntax_ReturnsFailure()
    {
        // Arrange - unbalanced parentheses
        var pattern = "(unclosed";

        // Act
        var result = TermPatternValidator.Validate(pattern);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid regex pattern");
    }

    [Fact]
    public void Validate_InvalidRegexBracket_ReturnsFailure()
    {
        // Arrange - unbalanced brackets
        var pattern = "[abc";

        // Act
        var result = TermPatternValidator.Validate(pattern);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid regex pattern");
    }

    [Theory]
    [InlineData(@"\bword\b")]
    [InlineData(@"^start.*end$")]
    [InlineData(@"[A-Z][a-z]+")]
    [InlineData(@"\d{2,4}")]
    [InlineData(@"(foo|bar|baz)")]
    public void Validate_CommonValidRegexPatterns_ReturnsSuccess(string pattern)
    {
        // Act
        var result = TermPatternValidator.Validate(pattern);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    #endregion

    #region Result<T> Tests

    [Fact]
    public void Result_Success_HasCorrectProperties()
    {
        // Act
        var result = Result<string>.Success("test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be("test");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Result_Failure_HasCorrectProperties()
    {
        // Act
        var result = Result<string>.Failure("error message");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("error message");
    }

    [Fact]
    public void Result_Failure_AccessingValueThrows()
    {
        // Arrange
        var result = Result<string>.Failure("error");

        // Act & Assert
        var action = () => _ = result.Value;
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*failed result*");
    }

    #endregion
}
