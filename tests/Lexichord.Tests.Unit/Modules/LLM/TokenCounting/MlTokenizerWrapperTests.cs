// -----------------------------------------------------------------------
// <copyright file="MlTokenizerWrapperTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.TokenCounting;
using Microsoft.ML.Tokenizers;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.TokenCounting;

/// <summary>
/// Unit tests for <see cref="MlTokenizerWrapper"/>.
/// </summary>
public class MlTokenizerWrapperTests
{
    /// <summary>
    /// Gets a tokenizer instance for tests using lazy initialization.
    /// </summary>
    private static readonly Lazy<Tokenizer> LazyGpt4oTokenizer = new(
        () => TiktokenTokenizer.CreateForModel("gpt-4o"),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static Tokenizer Gpt4oTokenizer => LazyGpt4oTokenizer.Value;

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor with valid parameters creates wrapper.
    /// </summary>
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateWrapper()
    {
        // Act
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, "gpt-4o");

        // Assert
        wrapper.Should().NotBeNull();
        wrapper.ModelFamily.Should().Be("gpt-4o");
        wrapper.IsExact.Should().BeTrue();
    }

    /// <summary>
    /// Tests that null tokenizer throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTokenizer_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new MlTokenizerWrapper(null!, "gpt-4o");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("tokenizer");
    }

    /// <summary>
    /// Tests that null model family throws ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullModelFamily_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MlTokenizerWrapper(Gpt4oTokenizer, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelFamily");
    }

    /// <summary>
    /// Tests that empty model family throws ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyModelFamily_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MlTokenizerWrapper(Gpt4oTokenizer, string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelFamily");
    }

    /// <summary>
    /// Tests that whitespace model family throws ArgumentException.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceModelFamily_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new MlTokenizerWrapper(Gpt4oTokenizer, "   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("modelFamily");
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
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, "gpt-4o");

        // Act
        var count = wrapper.CountTokens(null!);

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
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, "gpt-4o");

        // Act
        var count = wrapper.CountTokens(string.Empty);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that "Hello, world!" returns expected token count.
    /// </summary>
    [Fact]
    public void CountTokens_WithHelloWorld_ShouldReturnCorrectCount()
    {
        // Arrange
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, "gpt-4o");

        // Act
        var count = wrapper.CountTokens("Hello, world!");

        // Assert
        // "Hello, world!" typically tokenizes to 4 tokens in GPT-4o
        count.Should().BeGreaterThan(0);
        count.Should().BeLessThan(10);
    }

    /// <summary>
    /// Tests that longer text returns more tokens.
    /// </summary>
    [Fact]
    public void CountTokens_WithLongerText_ShouldReturnMoreTokens()
    {
        // Arrange
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, "gpt-4o");
        var shortText = "Hello";
        var longText = "Hello, this is a much longer piece of text that should have more tokens.";

        // Act
        var shortCount = wrapper.CountTokens(shortText);
        var longCount = wrapper.CountTokens(longText);

        // Assert
        longCount.Should().BeGreaterThan(shortCount);
    }

    /// <summary>
    /// Tests that IsExact returns true for ML-based tokenization.
    /// </summary>
    [Fact]
    public void IsExact_ShouldReturnTrue()
    {
        // Arrange
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, "gpt-4o");

        // Act & Assert
        wrapper.IsExact.Should().BeTrue();
    }

    #endregion

    #region ModelFamily Tests

    /// <summary>
    /// Tests that model family is correctly stored and returned.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4")]
    [InlineData("gpt-3.5")]
    public void ModelFamily_ShouldReturnConstructorValue(string modelFamily)
    {
        // Arrange
        var wrapper = new MlTokenizerWrapper(Gpt4oTokenizer, modelFamily);

        // Act & Assert
        wrapper.ModelFamily.Should().Be(modelFamily);
    }

    #endregion
}
