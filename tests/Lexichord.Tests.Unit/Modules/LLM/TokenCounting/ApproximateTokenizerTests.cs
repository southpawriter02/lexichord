// -----------------------------------------------------------------------
// <copyright file="ApproximateTokenizerTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.TokenCounting;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.TokenCounting;

/// <summary>
/// Unit tests for <see cref="ApproximateTokenizer"/>.
/// </summary>
public class ApproximateTokenizerTests
{
    #region Constructor Tests

    /// <summary>
    /// Tests that default constructor creates a tokenizer with default settings.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaults_ShouldCreateTokenizerWithDefaultValues()
    {
        // Act
        var tokenizer = new ApproximateTokenizer();

        // Assert
        tokenizer.CharsPerToken.Should().Be(ApproximateTokenizer.DefaultCharsPerToken);
        tokenizer.ModelFamily.Should().Be("unknown");
        tokenizer.IsExact.Should().BeFalse();
    }

    /// <summary>
    /// Tests that custom chars per token is applied correctly.
    /// </summary>
    [Theory]
    [InlineData(2.0)]
    [InlineData(3.5)]
    [InlineData(5.0)]
    public void Constructor_WithCustomCharsPerToken_ShouldUseCustomValue(double charsPerToken)
    {
        // Act
        var tokenizer = new ApproximateTokenizer(charsPerToken: charsPerToken);

        // Assert
        tokenizer.CharsPerToken.Should().Be(charsPerToken);
    }

    /// <summary>
    /// Tests that custom model family is applied correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithCustomModelFamily_ShouldUseCustomValue()
    {
        // Act
        var tokenizer = new ApproximateTokenizer(modelFamily: "claude");

        // Assert
        tokenizer.ModelFamily.Should().Be("claude");
    }

    /// <summary>
    /// Tests that zero chars per token throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Constructor_WithZeroCharsPerToken_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = () => new ApproximateTokenizer(charsPerToken: 0);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("charsPerToken");
    }

    /// <summary>
    /// Tests that negative chars per token throws ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Constructor_WithNegativeCharsPerToken_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var act = () => new ApproximateTokenizer(charsPerToken: -1);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("charsPerToken");
    }

    /// <summary>
    /// Tests that null model family defaults to "unknown".
    /// </summary>
    [Fact]
    public void Constructor_WithNullModelFamily_ShouldDefaultToUnknown()
    {
        // Act
        var tokenizer = new ApproximateTokenizer(modelFamily: null!);

        // Assert
        tokenizer.ModelFamily.Should().Be("unknown");
    }

    #endregion

    #region CountTokens Tests

    /// <summary>
    /// Tests that null text returns zero tokens.
    /// </summary>
    [Fact]
    public void CountTokens_WithNullText_ShouldReturnZero()
    {
        // Arrange
        var tokenizer = new ApproximateTokenizer();

        // Act
        var count = tokenizer.CountTokens(null!);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that empty text returns zero tokens.
    /// </summary>
    [Fact]
    public void CountTokens_WithEmptyText_ShouldReturnZero()
    {
        // Arrange
        var tokenizer = new ApproximateTokenizer();

        // Act
        var count = tokenizer.CountTokens(string.Empty);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that token count is calculated correctly using ceiling.
    /// </summary>
    [Theory]
    [InlineData("Hi", 4.0, 1)]      // 2 chars / 4 = 0.5, ceil = 1
    [InlineData("Hello", 4.0, 2)]   // 5 chars / 4 = 1.25, ceil = 2
    [InlineData("Hello World", 4.0, 3)] // 11 chars / 4 = 2.75, ceil = 3
    [InlineData("1234", 4.0, 1)]    // 4 chars / 4 = 1, ceil = 1
    [InlineData("12345678", 4.0, 2)] // 8 chars / 4 = 2, ceil = 2
    public void CountTokens_ShouldCalculateCorrectCount(string text, double charsPerToken, int expectedCount)
    {
        // Arrange
        var tokenizer = new ApproximateTokenizer(charsPerToken: charsPerToken);

        // Act
        var count = tokenizer.CountTokens(text);

        // Assert
        count.Should().Be(expectedCount);
    }

    /// <summary>
    /// Tests that different chars per token ratios produce different counts.
    /// </summary>
    [Fact]
    public void CountTokens_WithDifferentRatios_ShouldProduceDifferentCounts()
    {
        // Arrange
        var text = "Hello World!"; // 12 characters
        var tokenizer2 = new ApproximateTokenizer(charsPerToken: 2.0);
        var tokenizer4 = new ApproximateTokenizer(charsPerToken: 4.0);
        var tokenizer6 = new ApproximateTokenizer(charsPerToken: 6.0);

        // Act
        var count2 = tokenizer2.CountTokens(text); // 12/2 = 6
        var count4 = tokenizer4.CountTokens(text); // 12/4 = 3
        var count6 = tokenizer6.CountTokens(text); // 12/6 = 2

        // Assert
        count2.Should().Be(6);
        count4.Should().Be(3);
        count6.Should().Be(2);
    }

    /// <summary>
    /// Tests that the tokenizer reports IsExact as false.
    /// </summary>
    [Fact]
    public void IsExact_ShouldReturnFalse()
    {
        // Arrange
        var tokenizer = new ApproximateTokenizer();

        // Act & Assert
        tokenizer.IsExact.Should().BeFalse();
    }

    #endregion

    #region Constants Tests

    /// <summary>
    /// Tests that default chars per token is 4.0.
    /// </summary>
    [Fact]
    public void DefaultCharsPerToken_ShouldBeFour()
    {
        // Assert
        ApproximateTokenizer.DefaultCharsPerToken.Should().Be(4.0);
    }

    /// <summary>
    /// Tests that min chars per token constant is defined.
    /// </summary>
    [Fact]
    public void MinCharsPerToken_ShouldBePoint5()
    {
        // Assert
        ApproximateTokenizer.MinCharsPerToken.Should().Be(0.5);
    }

    /// <summary>
    /// Tests that max chars per token constant is defined.
    /// </summary>
    [Fact]
    public void MaxCharsPerToken_ShouldBeTen()
    {
        // Assert
        ApproximateTokenizer.MaxCharsPerToken.Should().Be(10.0);
    }

    #endregion
}
