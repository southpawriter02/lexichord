using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Security;

/// <summary>
/// Unit tests for <see cref="KeyValidator"/>.
/// </summary>
[Trait("Category", "Unit")]
public class KeyValidatorTests
{
    [Fact]
    public void ValidateKey_WithValidKey_DoesNotThrow()
    {
        // Arrange
        var key = "llm:openai:api-key";

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateKey_WithNullKey_ThrowsArgumentNullException()
    {
        // Arrange
        string? key = null;

        // Act
        var action = () => KeyValidator.ValidateKey(key!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValidateKey_WithEmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var key = "";

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public void ValidateKey_WithWhitespaceKey_ThrowsArgumentException()
    {
        // Arrange
        var key = "   ";

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*empty*whitespace*");
    }

    [Fact]
    public void ValidateKey_WithTooLongKey_ThrowsArgumentException()
    {
        // Arrange
        var key = new string('a', KeyValidator.MaxKeyLength + 1);

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage($"*{KeyValidator.MaxKeyLength}*");
    }

    [Fact]
    public void ValidateKey_WithMaxLengthKey_DoesNotThrow()
    {
        // Arrange
        var key = new string('a', KeyValidator.MaxKeyLength);

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void ValidateKey_WithNonPrintableAscii_ThrowsArgumentException()
    {
        // Arrange - Contains tab character
        var key = "test\tkey";

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*invalid character*");
    }

    [Fact]
    public void ValidateKey_WithUnicodeCharacter_ThrowsArgumentException()
    {
        // Arrange
        var key = "test:key:émoji";

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*invalid character*");
    }

    [Fact]
    public void ValidateKey_WithColons_DoesNotThrow()
    {
        // Arrange - Colons are valid for namespacing
        var key = "module:provider:subkey:value";

        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().NotThrow();
    }

    [Theory]
    [InlineData("simple-key")]
    [InlineData("llm:openai:api-key")]
    [InlineData("storage:s3:access-key")]
    [InlineData("auth:oauth:github:token")]
    [InlineData("my_key_123")]
    [InlineData("KEY-WITH-DASHES")]
    [InlineData("key.with.dots")]
    [InlineData("key/with/slashes")]
    public void ValidateKey_WithValidFormats_DoesNotThrow(string key)
    {
        // Act
        var action = () => KeyValidator.ValidateKey(key);

        // Assert
        action.Should().NotThrow();
    }

    [Fact]
    public void MaxKeyLength_Is256()
    {
        // Assert
        KeyValidator.MaxKeyLength.Should().Be(256);
    }
}
