// -----------------------------------------------------------------------
// <copyright file="AnthropicParameterMapperTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Providers.Anthropic;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Providers;

/// <summary>
/// Unit tests for <see cref="AnthropicParameterMapper"/>.
/// </summary>
public class AnthropicParameterMapperTests
{
    #region ToRequestBody Tests

    /// <summary>
    /// Tests that a simple request is mapped correctly.
    /// </summary>
    [Fact]
    public void ToRequestBody_WithSimpleRequest_ShouldMapCorrectly()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage(
            "Hello",
            new ChatOptions(Model: "claude-3-haiku-20240307", MaxTokens: 1024));

        // Act
        var body = AnthropicParameterMapper.ToRequestBody(request);

        // Assert
        body["model"]!.GetValue<string>().Should().Be("claude-3-haiku-20240307");
        body["max_tokens"]!.GetValue<int>().Should().Be(1024);
        body["messages"].Should().NotBeNull();
    }

    /// <summary>
    /// Tests that system messages are extracted to separate field.
    /// </summary>
    [Fact]
    public void ToRequestBody_WithSystemMessage_ShouldExtractToSystemField()
    {
        // Arrange
        var request = ChatRequest.WithSystemPrompt(
            "You are a helpful assistant.",
            "Hello",
            new ChatOptions(Model: "claude-3-haiku-20240307"));

        // Act
        var body = AnthropicParameterMapper.ToRequestBody(request);

        // Assert
        body["system"]!.GetValue<string>().Should().Be("You are a helpful assistant.");

        // Messages array should not contain system message
        var messages = body["messages"]!.AsArray();
        messages.Should().HaveCount(1);
        messages[0]!["role"]!.GetValue<string>().Should().Be("user");
    }

    /// <summary>
    /// Tests that stop sequences are mapped to "stop_sequences" (not "stop").
    /// </summary>
    [Fact]
    public void ToRequestBody_WithStopSequences_ShouldMapToStopSequences()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage(
            "Hello",
            new ChatOptions(
                Model: "claude-3-haiku-20240307",
                StopSequences: ImmutableArray.Create("END", "STOP")));

        // Act
        var body = AnthropicParameterMapper.ToRequestBody(request);

        // Assert
        body.ContainsKey("stop_sequences").Should().BeTrue();
        body.ContainsKey("stop").Should().BeFalse();

        var stopSequences = body["stop_sequences"]!.AsArray();
        stopSequences.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that default max_tokens is included (required by Anthropic).
    /// </summary>
    [Fact]
    public void ToRequestBody_WithNoMaxTokens_ShouldIncludeDefault()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage(
            "Hello",
            new ChatOptions(Model: "claude-3-haiku-20240307"));

        // Act
        var body = AnthropicParameterMapper.ToRequestBody(request);

        // Assert
        body["max_tokens"].Should().NotBeNull();
        body["max_tokens"]!.GetValue<int>().Should().BeGreaterThan(0);
    }

    #endregion

    #region ClampTemperature Tests

    /// <summary>
    /// Tests that temperature is scaled from 0-2 to 0-1 range.
    /// </summary>
    [Theory]
    [InlineData(0.0, 0.0)]
    [InlineData(0.7, 0.35)]
    [InlineData(1.0, 0.5)]
    [InlineData(1.4, 0.7)]
    [InlineData(2.0, 1.0)]
    public void ClampTemperature_ShouldScaleCorrectly(double input, double expected)
    {
        // Act
        var result = AnthropicParameterMapper.ClampTemperature(input);

        // Assert
        result.Should().BeApproximately(expected, 0.001);
    }

    /// <summary>
    /// Tests that temperature above 2.0 is clamped to 1.0.
    /// </summary>
    [Theory]
    [InlineData(2.5)]
    [InlineData(3.0)]
    [InlineData(5.0)]
    public void ClampTemperature_WithValueAbove2_ShouldClampTo1(double input)
    {
        // Act
        var result = AnthropicParameterMapper.ClampTemperature(input);

        // Assert
        result.Should().Be(1.0);
    }

    /// <summary>
    /// Tests that negative temperature is clamped to 0.0.
    /// </summary>
    [Theory]
    [InlineData(-0.5)]
    [InlineData(-1.0)]
    public void ClampTemperature_WithNegativeValue_ShouldClampTo0(double input)
    {
        // Act
        var result = AnthropicParameterMapper.ClampTemperature(input);

        // Assert
        result.Should().Be(0.0);
    }

    #endregion

    #region ParseUsage Tests

    /// <summary>
    /// Tests that usage parsing extracts input and output tokens.
    /// </summary>
    [Fact]
    public void ParseUsage_WithValidUsage_ShouldExtractTokens()
    {
        // Arrange
        var usage = new System.Text.Json.Nodes.JsonObject
        {
            ["input_tokens"] = 100,
            ["output_tokens"] = 50
        };

        // Act
        var (inputTokens, outputTokens) = AnthropicParameterMapper.ParseUsage(usage);

        // Assert
        inputTokens.Should().Be(100);
        outputTokens.Should().Be(50);
    }

    /// <summary>
    /// Tests that null usage returns zeros.
    /// </summary>
    [Fact]
    public void ParseUsage_WithNullUsage_ShouldReturnZeros()
    {
        // Act
        var (inputTokens, outputTokens) = AnthropicParameterMapper.ParseUsage(null);

        // Assert
        inputTokens.Should().Be(0);
        outputTokens.Should().Be(0);
    }

    #endregion

    #region WithStreaming Tests

    /// <summary>
    /// Tests that streaming is enabled in the request body.
    /// </summary>
    [Fact]
    public void WithStreaming_ShouldAddStreamFlag()
    {
        // Arrange
        var body = new System.Text.Json.Nodes.JsonObject();

        // Act
        var result = AnthropicParameterMapper.WithStreaming(body);

        // Assert
        result["stream"]!.GetValue<bool>().Should().BeTrue();
    }

    #endregion
}
