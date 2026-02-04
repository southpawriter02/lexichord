// -----------------------------------------------------------------------
// <copyright file="LLMTokenCounterTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.TokenCounting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.TokenCounting;

/// <summary>
/// Unit tests for <see cref="LLMTokenCounter"/>.
/// </summary>
public class LLMTokenCounterTests
{
    private readonly ILogger<LLMTokenCounter> _logger;
    private readonly ILogger<TokenizerCache> _cacheLogger;
    private readonly ILogger<TokenizerFactory> _factoryLogger;
    private readonly TokenizerCache _cache;
    private readonly TokenizerFactory _factory;
    private readonly LLMTokenCounter _counter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMTokenCounterTests"/> class.
    /// </summary>
    public LLMTokenCounterTests()
    {
        _logger = Substitute.For<ILogger<LLMTokenCounter>>();
        _cacheLogger = Substitute.For<ILogger<TokenizerCache>>();
        _factoryLogger = Substitute.For<ILogger<TokenizerFactory>>();
        _cache = new TokenizerCache(_cacheLogger);
        _factory = new TokenizerFactory(_factoryLogger);
        _counter = new LLMTokenCounter(_cache, _factory, _logger);
    }

    #region Constructor Tests

    /// <summary>
    /// Tests that null cache throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullCache_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new LLMTokenCounter(null!, _factory, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("cache");
    }

    /// <summary>
    /// Tests that null factory throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullFactory_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new LLMTokenCounter(_cache, null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("factory");
    }

    /// <summary>
    /// Tests that null logger throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => new LLMTokenCounter(_cache, _factory, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region CountTokens (Text) Tests

    /// <summary>
    /// Tests that null text returns zero tokens.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithNullText_ShouldReturnZero()
    {
        // Arrange
        string? nullText = null;

        // Act
        var count = _counter.CountTokens(nullText, "gpt-4o");

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that empty text returns zero tokens.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithEmptyText_ShouldReturnZero()
    {
        // Act
        var count = _counter.CountTokens(string.Empty, "gpt-4o");

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that valid text returns positive token count.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithValidText_ShouldReturnPositiveCount()
    {
        // Act
        var count = _counter.CountTokens("Hello, world!", "gpt-4o");

        // Assert
        count.Should().BeGreaterThan(0);
    }

    /// <summary>
    /// Tests that null model throws ArgumentException.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithNullModel_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _counter.CountTokens("Hello", null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    /// <summary>
    /// Tests that empty model throws ArgumentException.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithEmptyModel_ShouldThrowArgumentException()
    {
        // Act
        var act = () => _counter.CountTokens("Hello", string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    /// <summary>
    /// Tests that GPT-4o uses exact tokenization.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithGpt4o_ShouldUseExactTokenization()
    {
        // Arrange
        var text = "Hello, world!";

        // Act
        var count = _counter.CountTokens(text, "gpt-4o");

        // Assert
        // Exact tokenization should give consistent results
        count.Should().BeGreaterThan(0);
        _counter.IsExactTokenizer("gpt-4o").Should().BeTrue();
    }

    /// <summary>
    /// Tests that Claude uses approximate tokenization.
    /// </summary>
    [Fact]
    public void CountTokens_Text_WithClaude_ShouldUseApproximateTokenization()
    {
        // Arrange
        var text = "Hello, world!";

        // Act
        var count = _counter.CountTokens(text, "claude-3-opus-20240229");

        // Assert
        // Approximate tokenization: 13 chars / 4 = ~4 tokens
        count.Should().BeGreaterThan(0);
        _counter.IsExactTokenizer("claude-3-opus-20240229").Should().BeFalse();
    }

    #endregion

    #region CountTokens (Messages) Tests

    /// <summary>
    /// Tests that null messages returns zero tokens.
    /// </summary>
    [Fact]
    public void CountTokens_Messages_WithNullMessages_ShouldReturnZero()
    {
        // Act
        var count = _counter.CountTokens((IEnumerable<ChatMessage>?)null, "gpt-4o");

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that empty messages returns zero tokens.
    /// </summary>
    [Fact]
    public void CountTokens_Messages_WithEmptyMessages_ShouldReturnZero()
    {
        // Act
        var count = _counter.CountTokens(Array.Empty<ChatMessage>(), "gpt-4o");

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Tests that messages include overhead tokens.
    /// </summary>
    [Fact]
    public void CountTokens_Messages_ShouldIncludeOverhead()
    {
        // Arrange
        var messages = new[]
        {
            ChatMessage.System("You are helpful."),
            ChatMessage.User("Hello!"),
        };

        // Act
        var count = _counter.CountTokens(messages, "gpt-4o");

        // Assert
        // Should be content tokens + overhead (4 tokens per message * 2 messages = 8)
        count.Should().BeGreaterThan(8); // At least overhead for 2 messages
    }

    /// <summary>
    /// Tests that more messages result in more tokens.
    /// </summary>
    [Fact]
    public void CountTokens_Messages_MoreMessages_ShouldHaveMoreTokens()
    {
        // Arrange
        var oneMessage = new[] { ChatMessage.User("Hello!") };
        var threeMessages = new[]
        {
            ChatMessage.System("Be helpful."),
            ChatMessage.User("Hello!"),
            ChatMessage.Assistant("Hi there!"),
        };

        // Act
        var oneCount = _counter.CountTokens(oneMessage, "gpt-4o");
        var threeCount = _counter.CountTokens(threeMessages, "gpt-4o");

        // Assert
        threeCount.Should().BeGreaterThan(oneCount);
    }

    /// <summary>
    /// Tests that null model throws ArgumentException.
    /// </summary>
    [Fact]
    public void CountTokens_Messages_WithNullModel_ShouldThrowArgumentException()
    {
        // Arrange
        var messages = new[] { ChatMessage.User("Hello!") };

        // Act
        var act = () => _counter.CountTokens(messages, null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("model");
    }

    #endregion

    #region EstimateResponseTokens Tests

    /// <summary>
    /// Tests that response estimate is approximately 60% of max tokens.
    /// </summary>
    [Fact]
    public void EstimateResponseTokens_ShouldReturn60PercentOfMaxTokens()
    {
        // Act
        var estimate = _counter.EstimateResponseTokens(promptTokens: 500, maxTokens: 1000);

        // Assert
        estimate.Should().Be(600); // 60% of 1000
    }

    /// <summary>
    /// Tests that zero max tokens returns zero estimate.
    /// </summary>
    [Fact]
    public void EstimateResponseTokens_WithZeroMaxTokens_ShouldReturnZero()
    {
        // Act
        var estimate = _counter.EstimateResponseTokens(promptTokens: 500, maxTokens: 0);

        // Assert
        estimate.Should().Be(0);
    }

    /// <summary>
    /// Tests that negative prompt tokens returns zero estimate.
    /// </summary>
    [Fact]
    public void EstimateResponseTokens_WithNegativePromptTokens_ShouldReturnZero()
    {
        // Act
        var estimate = _counter.EstimateResponseTokens(promptTokens: -100, maxTokens: 1000);

        // Assert
        estimate.Should().Be(0);
    }

    /// <summary>
    /// Tests that negative max tokens returns zero estimate.
    /// </summary>
    [Fact]
    public void EstimateResponseTokens_WithNegativeMaxTokens_ShouldReturnZero()
    {
        // Act
        var estimate = _counter.EstimateResponseTokens(promptTokens: 500, maxTokens: -100);

        // Assert
        estimate.Should().Be(0);
    }

    #endregion

    #region GetModelLimit Tests

    /// <summary>
    /// Tests that known models return correct context window.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", 128000)]
    [InlineData("claude-3-opus-20240229", 200000)]
    [InlineData("gpt-3.5-turbo", 16385)]
    public void GetModelLimit_WithKnownModel_ShouldReturnCorrectLimit(string model, int expected)
    {
        // Act
        var limit = _counter.GetModelLimit(model);

        // Assert
        limit.Should().Be(expected);
    }

    /// <summary>
    /// Tests that unknown models return default limit.
    /// </summary>
    [Fact]
    public void GetModelLimit_WithUnknownModel_ShouldReturnDefault()
    {
        // Act
        var limit = _counter.GetModelLimit("unknown-model");

        // Assert
        limit.Should().Be(ModelTokenLimits.DefaultContextWindow);
    }

    #endregion

    #region GetMaxOutputTokens Tests

    /// <summary>
    /// Tests that known models return correct max output tokens.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", 16384)]
    [InlineData("claude-3-5-sonnet-20241022", 8192)]
    [InlineData("claude-3-haiku-20240307", 4096)]
    public void GetMaxOutputTokens_WithKnownModel_ShouldReturnCorrectMax(string model, int expected)
    {
        // Act
        var maxOutput = _counter.GetMaxOutputTokens(model);

        // Assert
        maxOutput.Should().Be(expected);
    }

    /// <summary>
    /// Tests that unknown models return default max output.
    /// </summary>
    [Fact]
    public void GetMaxOutputTokens_WithUnknownModel_ShouldReturnDefault()
    {
        // Act
        var maxOutput = _counter.GetMaxOutputTokens("unknown-model");

        // Assert
        maxOutput.Should().Be(ModelTokenLimits.DefaultMaxOutputTokens);
    }

    #endregion

    #region CalculateCost Tests

    /// <summary>
    /// Tests that cost is calculated correctly for known models.
    /// </summary>
    [Fact]
    public void CalculateCost_WithKnownModel_ShouldCalculateCorrectly()
    {
        // Arrange
        // GPT-4o: $2.50/1M input, $10.00/1M output
        var inputTokens = 1000;
        var outputTokens = 500;
        var expected = (1000m * 2.50m + 500m * 10.00m) / 1_000_000m;

        // Act
        var cost = _counter.CalculateCost("gpt-4o", inputTokens, outputTokens);

        // Assert
        cost.Should().Be(expected);
    }

    /// <summary>
    /// Tests that cost is zero for unknown models.
    /// </summary>
    [Fact]
    public void CalculateCost_WithUnknownModel_ShouldReturnZero()
    {
        // Act
        var cost = _counter.CalculateCost("unknown-model", 1000, 500);

        // Assert
        cost.Should().Be(0m);
    }

    #endregion

    #region IsExactTokenizer Tests

    /// <summary>
    /// Tests that GPT models are identified as exact tokenizers.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", true)]
    [InlineData("gpt-4", true)]
    [InlineData("gpt-3.5-turbo", true)]
    public void IsExactTokenizer_WithGptModels_ShouldReturnTrue(string model, bool expected)
    {
        // Act
        var isExact = _counter.IsExactTokenizer(model);

        // Assert
        isExact.Should().Be(expected);
    }

    /// <summary>
    /// Tests that Claude models are identified as approximate tokenizers.
    /// </summary>
    [Theory]
    [InlineData("claude-3-opus-20240229", false)]
    [InlineData("claude-3-5-sonnet-20241022", false)]
    [InlineData("unknown-model", false)]
    public void IsExactTokenizer_WithNonGptModels_ShouldReturnFalse(string model, bool expected)
    {
        // Act
        var isExact = _counter.IsExactTokenizer(model);

        // Assert
        isExact.Should().Be(expected);
    }

    #endregion

    #region Constants Tests

    /// <summary>
    /// Tests that default message overhead tokens is 4.
    /// </summary>
    [Fact]
    public void DefaultMessageOverheadTokens_ShouldBeFour()
    {
        // Assert
        LLMTokenCounter.DefaultMessageOverheadTokens.Should().Be(4);
    }

    /// <summary>
    /// Tests that default response estimate factor is 0.6.
    /// </summary>
    [Fact]
    public void DefaultResponseEstimateFactor_ShouldBe0Point6()
    {
        // Assert
        LLMTokenCounter.DefaultResponseEstimateFactor.Should().Be(0.6);
    }

    #endregion
}
