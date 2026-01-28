using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Security;

/// <summary>
/// Unit tests for <see cref="KeyHasher"/>.
/// </summary>
[Trait("Category", "Unit")]
public class KeyHasherTests
{
    [Fact]
    public void ComputeFileName_ReturnsLowercaseHexString()
    {
        // Arrange
        var key = "test:api-key";

        // Act
        var result = KeyHasher.ComputeFileName(key);

        // Assert
        result.Should().MatchRegex("^[0-9a-f]+$");
    }

    [Fact]
    public void ComputeFileName_Returns32Characters()
    {
        // Arrange
        var key = "test:api-key";

        // Act
        var result = KeyHasher.ComputeFileName(key);

        // Assert
        result.Length.Should().Be(32, "because we use 16 bytes = 32 hex characters");
    }

    [Fact]
    public void ComputeFileName_SameKey_ReturnsSameHash()
    {
        // Arrange
        var key = "llm:openai:api-key";

        // Act
        var result1 = KeyHasher.ComputeFileName(key);
        var result2 = KeyHasher.ComputeFileName(key);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void ComputeFileName_DifferentKeys_ReturnDifferentHashes()
    {
        // Arrange
        var key1 = "llm:openai:api-key";
        var key2 = "llm:anthropic:api-key";

        // Act
        var result1 = KeyHasher.ComputeFileName(key1);
        var result2 = KeyHasher.ComputeFileName(key2);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void ComputeFileName_CaseSensitive()
    {
        // Arrange
        var key1 = "Test:Key";
        var key2 = "test:key";

        // Act
        var result1 = KeyHasher.ComputeFileName(key1);
        var result2 = KeyHasher.ComputeFileName(key2);

        // Assert
        result1.Should().NotBe(result2, "because hash should be case-sensitive");
    }

    [Theory]
    [InlineData("simple")]
    [InlineData("key:with:colons")]
    [InlineData("key/with/slashes")]
    [InlineData("key with spaces")]
    [InlineData("key!@#$%^&*()special")]
    public void ComputeFileName_ValidForFileSystem_AllCases(string key)
    {
        // Act
        var result = KeyHasher.ComputeFileName(key);

        // Assert
        result.Should().MatchRegex("^[0-9a-f]{32}$");
        // Verify no invalid filesystem characters
        result.Should().NotContain("/");
        result.Should().NotContain("\\");
        result.Should().NotContain(":");
        result.Should().NotContain(" ");
    }

    [Fact]
    public void ComputeFileName_EmptyString_ReturnsValidHash()
    {
        // Arrange
        var key = "";

        // Act
        var result = KeyHasher.ComputeFileName(key);

        // Assert
        result.Should().MatchRegex("^[0-9a-f]{32}$");
    }

    [Fact]
    public void ComputeFileName_LongKey_ReturnsFixedLengthHash()
    {
        // Arrange
        var key = new string('x', 1000);

        // Act
        var result = KeyHasher.ComputeFileName(key);

        // Assert
        result.Length.Should().Be(32);
    }
}
