// -----------------------------------------------------------------------
// <copyright file="TokenizerFactoryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.TokenCounting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.TokenCounting;

/// <summary>
/// Unit tests for <see cref="TokenizerFactory"/>.
/// </summary>
public class TokenizerFactoryTests
{
    private readonly ILogger<TokenizerFactory> _logger;
    private readonly TokenizerFactory _factory;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenizerFactoryTests"/> class.
    /// </summary>
    public TokenizerFactoryTests()
    {
        _logger = Substitute.For<ILogger<TokenizerFactory>>();
        _factory = new TokenizerFactory(_logger);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that constructor with null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new TokenizerFactory(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region CreateForModel Tests - GPT-4o Family

    /// <summary>
    /// Tests that GPT-4o models return exact tokenizer.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4o-2024-05-13")]
    [InlineData("GPT-4O")] // Case insensitive
    public void CreateForModel_WithGpt4oFamily_ShouldReturnExactTokenizer(string model)
    {
        // Act
        var tokenizer = _factory.CreateForModel(model);

        // Assert
        tokenizer.Should().NotBeNull();
        tokenizer.IsExact.Should().BeTrue();
        tokenizer.ModelFamily.Should().Be("gpt-4o");
    }

    #endregion

    #region CreateForModel Tests - GPT-4 Family

    /// <summary>
    /// Tests that GPT-4 (non-4o) models return exact tokenizer.
    /// </summary>
    [Theory]
    [InlineData("gpt-4")]
    [InlineData("gpt-4-turbo")]
    [InlineData("gpt-4-turbo-preview")]
    [InlineData("GPT-4-TURBO")] // Case insensitive
    public void CreateForModel_WithGpt4Family_ShouldReturnExactTokenizer(string model)
    {
        // Act
        var tokenizer = _factory.CreateForModel(model);

        // Assert
        tokenizer.Should().NotBeNull();
        tokenizer.IsExact.Should().BeTrue();
        tokenizer.ModelFamily.Should().Be("gpt-4");
    }

    #endregion

    #region CreateForModel Tests - GPT-3.5 Family

    /// <summary>
    /// Tests that GPT-3.5 models return exact tokenizer.
    /// </summary>
    [Theory]
    [InlineData("gpt-3.5-turbo")]
    [InlineData("gpt-3.5-turbo-16k")]
    [InlineData("GPT-3.5-TURBO")] // Case insensitive
    public void CreateForModel_WithGpt35Family_ShouldReturnExactTokenizer(string model)
    {
        // Act
        var tokenizer = _factory.CreateForModel(model);

        // Assert
        tokenizer.Should().NotBeNull();
        tokenizer.IsExact.Should().BeTrue();
        tokenizer.ModelFamily.Should().Be("gpt-3.5");
    }

    #endregion

    #region CreateForModel Tests - Claude Family

    /// <summary>
    /// Tests that Claude models return approximate tokenizer.
    /// </summary>
    [Theory]
    [InlineData("claude-3-opus-20240229")]
    [InlineData("claude-3-sonnet-20240229")]
    [InlineData("claude-3-haiku-20240307")]
    [InlineData("claude-3-5-sonnet-20241022")]
    [InlineData("CLAUDE-3-OPUS")] // Case insensitive
    public void CreateForModel_WithClaudeFamily_ShouldReturnApproximateTokenizer(string model)
    {
        // Act
        var tokenizer = _factory.CreateForModel(model);

        // Assert
        tokenizer.Should().NotBeNull();
        tokenizer.IsExact.Should().BeFalse();
        tokenizer.ModelFamily.Should().Be("claude");
    }

    #endregion

    #region CreateForModel Tests - Unknown Models

    /// <summary>
    /// Tests that unknown models return approximate tokenizer.
    /// </summary>
    [Theory]
    [InlineData("llama-3-70b")]
    [InlineData("mistral-7b")]
    [InlineData("unknown-model")]
    public void CreateForModel_WithUnknownModel_ShouldReturnApproximateTokenizer(string model)
    {
        // Act
        var tokenizer = _factory.CreateForModel(model);

        // Assert
        tokenizer.Should().NotBeNull();
        tokenizer.IsExact.Should().BeFalse();
        tokenizer.ModelFamily.Should().Be("unknown");
    }

    #endregion

    #region CreateForModel Tests - Validation

    /// <summary>
    /// Tests that null model throws ArgumentException.
    /// </summary>
    [Fact]
    public void CreateForModel_WithNullModel_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _factory.CreateForModel(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    /// <summary>
    /// Tests that empty model throws ArgumentException.
    /// </summary>
    [Fact]
    public void CreateForModel_WithEmptyModel_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _factory.CreateForModel(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    /// <summary>
    /// Tests that whitespace model throws ArgumentException.
    /// </summary>
    [Fact]
    public void CreateForModel_WithWhitespaceModel_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _factory.CreateForModel("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    #endregion

    #region IsExactTokenizer Tests

    /// <summary>
    /// Tests that GPT models are identified as exact tokenizers.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", true)]
    [InlineData("gpt-4o-mini", true)]
    [InlineData("gpt-4", true)]
    [InlineData("gpt-4-turbo", true)]
    [InlineData("gpt-3.5-turbo", true)]
    public void IsExactTokenizer_WithGptModels_ShouldReturnTrue(string model, bool expected)
    {
        // Act
        var isExact = _factory.IsExactTokenizer(model);

        // Assert
        isExact.Should().Be(expected);
    }

    /// <summary>
    /// Tests that Claude and unknown models are identified as approximate.
    /// </summary>
    [Theory]
    [InlineData("claude-3-opus-20240229", false)]
    [InlineData("claude-3-5-sonnet-20241022", false)]
    [InlineData("llama-3-70b", false)]
    [InlineData("unknown-model", false)]
    public void IsExactTokenizer_WithNonGptModels_ShouldReturnFalse(string model, bool expected)
    {
        // Act
        var isExact = _factory.IsExactTokenizer(model);

        // Assert
        isExact.Should().Be(expected);
    }

    /// <summary>
    /// Tests that null model returns false.
    /// </summary>
    [Fact]
    public void IsExactTokenizer_WithNullModel_ShouldReturnFalse()
    {
        // Act
        var isExact = _factory.IsExactTokenizer(null!);

        // Assert
        isExact.Should().BeFalse();
    }

    /// <summary>
    /// Tests that empty model returns false.
    /// </summary>
    [Fact]
    public void IsExactTokenizer_WithEmptyModel_ShouldReturnFalse()
    {
        // Act
        var isExact = _factory.IsExactTokenizer(string.Empty);

        // Assert
        isExact.Should().BeFalse();
    }

    #endregion
}
