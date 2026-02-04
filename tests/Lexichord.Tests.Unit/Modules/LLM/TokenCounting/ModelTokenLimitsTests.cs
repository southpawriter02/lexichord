// -----------------------------------------------------------------------
// <copyright file="ModelTokenLimitsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.TokenCounting;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.TokenCounting;

/// <summary>
/// Unit tests for <see cref="ModelTokenLimits"/>.
/// </summary>
public class ModelTokenLimitsTests
{
    #region GetContextWindow Tests

    /// <summary>
    /// Tests that GPT-4o models return 128K context window.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-4o-mini")]
    [InlineData("gpt-4o-2024-05-13")]
    public void GetContextWindow_WithGpt4oModels_ShouldReturn128K(string model)
    {
        // Act
        var contextWindow = ModelTokenLimits.GetContextWindow(model);

        // Assert
        contextWindow.Should().Be(128000);
    }

    /// <summary>
    /// Tests that Claude models return 200K context window.
    /// </summary>
    [Theory]
    [InlineData("claude-3-opus-20240229")]
    [InlineData("claude-3-5-sonnet-20241022")]
    [InlineData("claude-3-haiku-20240307")]
    public void GetContextWindow_WithClaudeModels_ShouldReturn200K(string model)
    {
        // Act
        var contextWindow = ModelTokenLimits.GetContextWindow(model);

        // Assert
        contextWindow.Should().Be(200000);
    }

    /// <summary>
    /// Tests that GPT-3.5 models return 16K context window.
    /// </summary>
    [Fact]
    public void GetContextWindow_WithGpt35Turbo_ShouldReturn16K()
    {
        // Act
        var contextWindow = ModelTokenLimits.GetContextWindow("gpt-3.5-turbo");

        // Assert
        contextWindow.Should().Be(16385);
    }

    /// <summary>
    /// Tests that unknown models return default context window.
    /// </summary>
    [Fact]
    public void GetContextWindow_WithUnknownModel_ShouldReturnDefault()
    {
        // Act
        var contextWindow = ModelTokenLimits.GetContextWindow("unknown-model");

        // Assert
        contextWindow.Should().Be(ModelTokenLimits.DefaultContextWindow);
    }

    /// <summary>
    /// Tests that null model returns default context window.
    /// </summary>
    [Fact]
    public void GetContextWindow_WithNullModel_ShouldReturnDefault()
    {
        // Act
        var contextWindow = ModelTokenLimits.GetContextWindow(null!);

        // Assert
        contextWindow.Should().Be(ModelTokenLimits.DefaultContextWindow);
    }

    #endregion

    #region GetMaxOutputTokens Tests

    /// <summary>
    /// Tests that GPT-4o returns 16K max output.
    /// </summary>
    [Fact]
    public void GetMaxOutputTokens_WithGpt4o_ShouldReturn16K()
    {
        // Act
        var maxOutput = ModelTokenLimits.GetMaxOutputTokens("gpt-4o");

        // Assert
        maxOutput.Should().Be(16384);
    }

    /// <summary>
    /// Tests that Claude 3.5 Sonnet returns 8K max output.
    /// </summary>
    [Fact]
    public void GetMaxOutputTokens_WithClaude35Sonnet_ShouldReturn8K()
    {
        // Act
        var maxOutput = ModelTokenLimits.GetMaxOutputTokens("claude-3-5-sonnet-20241022");

        // Assert
        maxOutput.Should().Be(8192);
    }

    /// <summary>
    /// Tests that Claude 3 Haiku returns 4K max output.
    /// </summary>
    [Fact]
    public void GetMaxOutputTokens_WithClaude3Haiku_ShouldReturn4K()
    {
        // Act
        var maxOutput = ModelTokenLimits.GetMaxOutputTokens("claude-3-haiku-20240307");

        // Assert
        maxOutput.Should().Be(4096);
    }

    /// <summary>
    /// Tests that unknown models return default max output.
    /// </summary>
    [Fact]
    public void GetMaxOutputTokens_WithUnknownModel_ShouldReturnDefault()
    {
        // Act
        var maxOutput = ModelTokenLimits.GetMaxOutputTokens("unknown-model");

        // Assert
        maxOutput.Should().Be(ModelTokenLimits.DefaultMaxOutputTokens);
    }

    #endregion

    #region GetPricing Tests

    /// <summary>
    /// Tests that GPT-4o returns correct pricing.
    /// </summary>
    [Fact]
    public void GetPricing_WithGpt4o_ShouldReturnCorrectPricing()
    {
        // Act
        var pricing = ModelTokenLimits.GetPricing("gpt-4o");

        // Assert
        pricing.Should().NotBeNull();
        pricing!.Value.InputPricePerMillion.Should().Be(2.50m);
        pricing.Value.OutputPricePerMillion.Should().Be(10.00m);
    }

    /// <summary>
    /// Tests that GPT-4o-mini returns lower pricing.
    /// </summary>
    [Fact]
    public void GetPricing_WithGpt4oMini_ShouldReturnLowerPricing()
    {
        // Act
        var pricing = ModelTokenLimits.GetPricing("gpt-4o-mini");

        // Assert
        pricing.Should().NotBeNull();
        pricing!.Value.InputPricePerMillion.Should().Be(0.15m);
        pricing.Value.OutputPricePerMillion.Should().Be(0.60m);
    }

    /// <summary>
    /// Tests that Claude 3 Haiku returns low pricing.
    /// </summary>
    [Fact]
    public void GetPricing_WithClaude3Haiku_ShouldReturnLowPricing()
    {
        // Act
        var pricing = ModelTokenLimits.GetPricing("claude-3-haiku-20240307");

        // Assert
        pricing.Should().NotBeNull();
        pricing!.Value.InputPricePerMillion.Should().Be(0.25m);
        pricing.Value.OutputPricePerMillion.Should().Be(1.25m);
    }

    /// <summary>
    /// Tests that unknown models return null pricing.
    /// </summary>
    [Fact]
    public void GetPricing_WithUnknownModel_ShouldReturnNull()
    {
        // Act
        var pricing = ModelTokenLimits.GetPricing("unknown-model");

        // Assert
        pricing.Should().BeNull();
    }

    /// <summary>
    /// Tests that model variant matches via prefix.
    /// </summary>
    [Fact]
    public void GetPricing_WithModelVariant_ShouldMatchViaPrefix()
    {
        // Act - variant should match base model
        var pricing = ModelTokenLimits.GetPricing("gpt-4o-2024-05-13");

        // Assert
        pricing.Should().NotBeNull();
        pricing!.Value.InputPricePerMillion.Should().Be(2.50m);
    }

    #endregion

    #region CalculateCost Tests

    /// <summary>
    /// Tests that cost is calculated correctly for GPT-4o.
    /// </summary>
    [Fact]
    public void CalculateCost_WithGpt4o_ShouldCalculateCorrectly()
    {
        // Arrange
        // GPT-4o: $2.50/1M input, $10.00/1M output
        var inputTokens = 1000;
        var outputTokens = 500;

        // Expected: (1000 * 2.50 + 500 * 10.00) / 1,000,000 = 0.0075
        var expected = (1000m * 2.50m + 500m * 10.00m) / 1_000_000m;

        // Act
        var cost = ModelTokenLimits.CalculateCost("gpt-4o", inputTokens, outputTokens);

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
        var cost = ModelTokenLimits.CalculateCost("unknown-model", 1000, 500);

        // Assert
        cost.Should().Be(0m);
    }

    /// <summary>
    /// Tests that negative input tokens return zero cost.
    /// </summary>
    [Fact]
    public void CalculateCost_WithNegativeInputTokens_ShouldReturnZero()
    {
        // Act
        var cost = ModelTokenLimits.CalculateCost("gpt-4o", -100, 500);

        // Assert
        cost.Should().Be(0m);
    }

    /// <summary>
    /// Tests that negative output tokens return zero cost.
    /// </summary>
    [Fact]
    public void CalculateCost_WithNegativeOutputTokens_ShouldReturnZero()
    {
        // Act
        var cost = ModelTokenLimits.CalculateCost("gpt-4o", 1000, -500);

        // Assert
        cost.Should().Be(0m);
    }

    /// <summary>
    /// Tests that zero tokens return zero cost.
    /// </summary>
    [Fact]
    public void CalculateCost_WithZeroTokens_ShouldReturnZero()
    {
        // Act
        var cost = ModelTokenLimits.CalculateCost("gpt-4o", 0, 0);

        // Assert
        cost.Should().Be(0m);
    }

    #endregion

    #region HasPricing Tests

    /// <summary>
    /// Tests that known models have pricing.
    /// </summary>
    [Theory]
    [InlineData("gpt-4o", true)]
    [InlineData("gpt-4o-mini", true)]
    [InlineData("claude-3-opus-20240229", true)]
    [InlineData("claude-3-haiku-20240307", true)]
    public void HasPricing_WithKnownModels_ShouldReturnTrue(string model, bool expected)
    {
        // Act
        var hasPricing = ModelTokenLimits.HasPricing(model);

        // Assert
        hasPricing.Should().Be(expected);
    }

    /// <summary>
    /// Tests that unknown models do not have pricing.
    /// </summary>
    [Theory]
    [InlineData("llama-3-70b")]
    [InlineData("mistral-7b")]
    [InlineData("unknown-model")]
    public void HasPricing_WithUnknownModels_ShouldReturnFalse(string model)
    {
        // Act
        var hasPricing = ModelTokenLimits.HasPricing(model);

        // Assert
        hasPricing.Should().BeFalse();
    }

    #endregion

    #region GetAllKnownModels Tests

    /// <summary>
    /// Tests that GetAllKnownModels returns non-empty list.
    /// </summary>
    [Fact]
    public void GetAllKnownModels_ShouldReturnNonEmptyList()
    {
        // Act
        var models = ModelTokenLimits.GetAllKnownModels();

        // Assert
        models.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that GetAllKnownModels includes GPT and Claude models.
    /// </summary>
    [Fact]
    public void GetAllKnownModels_ShouldIncludeExpectedModels()
    {
        // Act
        var models = ModelTokenLimits.GetAllKnownModels();

        // Assert
        models.Should().Contain("gpt-4o");
        models.Should().Contain("gpt-4o-mini");
        models.Should().Contain("claude-3-opus-20240229");
    }

    #endregion

    #region Constants Tests

    /// <summary>
    /// Tests default context window constant.
    /// </summary>
    [Fact]
    public void DefaultContextWindow_ShouldBe8192()
    {
        // Assert
        ModelTokenLimits.DefaultContextWindow.Should().Be(8192);
    }

    /// <summary>
    /// Tests default max output tokens constant.
    /// </summary>
    [Fact]
    public void DefaultMaxOutputTokens_ShouldBe4096()
    {
        // Assert
        ModelTokenLimits.DefaultMaxOutputTokens.Should().Be(4096);
    }

    #endregion
}
