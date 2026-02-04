// -----------------------------------------------------------------------
// <copyright file="ChatResponseTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatResponse"/> record.
/// </summary>
public class ChatResponseTests
{
    /// <summary>
    /// Tests that TotalTokens is correctly computed.
    /// </summary>
    [Theory]
    [InlineData(10, 20, 30)]
    [InlineData(0, 0, 0)]
    [InlineData(100, 50, 150)]
    [InlineData(1000, 2000, 3000)]
    public void TotalTokens_ShouldBeSumOfPromptAndCompletionTokens(
        int promptTokens,
        int completionTokens,
        int expectedTotal)
    {
        // Arrange
        var response = new ChatResponse(
            "Test content",
            promptTokens,
            completionTokens,
            TimeSpan.FromMilliseconds(100),
            "stop");

        // Assert
        response.TotalTokens.Should().Be(expectedTotal);
    }

    /// <summary>
    /// Tests that constructor throws on null content.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ChatResponse(
            null!,
            10,
            20,
            TimeSpan.FromMilliseconds(100),
            "stop");

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that constructor throws on negative prompt tokens.
    /// </summary>
    [Fact]
    public void Constructor_WithNegativePromptTokens_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var action = () => new ChatResponse(
            "Content",
            -1,
            20,
            TimeSpan.FromMilliseconds(100),
            "stop");

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests that constructor throws on negative completion tokens.
    /// </summary>
    [Fact]
    public void Constructor_WithNegativeCompletionTokens_ShouldThrowArgumentOutOfRangeException()
    {
        // Act
        var action = () => new ChatResponse(
            "Content",
            10,
            -1,
            TimeSpan.FromMilliseconds(100),
            "stop");

        // Assert
        action.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Tests IsComplete returns true when finish reason is "stop".
    /// </summary>
    [Theory]
    [InlineData("stop", true)]
    [InlineData("STOP", true)]
    [InlineData("Stop", true)]
    [InlineData("length", false)]
    [InlineData("content_filter", false)]
    [InlineData(null, false)]
    public void IsComplete_ShouldReturnTrueOnlyForStop(string? finishReason, bool expectedIsComplete)
    {
        // Arrange
        var response = new ChatResponse(
            "Content",
            10,
            20,
            TimeSpan.FromMilliseconds(100),
            finishReason);

        // Assert
        response.IsComplete.Should().Be(expectedIsComplete);
    }

    /// <summary>
    /// Tests IsTruncated returns true when finish reason is "length".
    /// </summary>
    [Theory]
    [InlineData("length", true)]
    [InlineData("LENGTH", true)]
    [InlineData("Length", true)]
    [InlineData("stop", false)]
    [InlineData("content_filter", false)]
    [InlineData(null, false)]
    public void IsTruncated_ShouldReturnTrueOnlyForLength(string? finishReason, bool expectedIsTruncated)
    {
        // Arrange
        var response = new ChatResponse(
            "Content",
            10,
            20,
            TimeSpan.FromMilliseconds(100),
            finishReason);

        // Assert
        response.IsTruncated.Should().Be(expectedIsTruncated);
    }

    /// <summary>
    /// Tests TokensPerSecond is correctly calculated.
    /// </summary>
    [Fact]
    public void TokensPerSecond_ShouldBeCorrectlyCalculated()
    {
        // Arrange - 100 tokens in 1 second = 100 tokens/sec
        var response = new ChatResponse(
            "Content",
            50,
            100,
            TimeSpan.FromSeconds(1),
            "stop");

        // Assert
        response.TokensPerSecond.Should().Be(100);
    }

    /// <summary>
    /// Tests TokensPerSecond returns 0 when duration is zero.
    /// </summary>
    [Fact]
    public void TokensPerSecond_WithZeroDuration_ShouldReturnZero()
    {
        // Arrange
        var response = new ChatResponse(
            "Content",
            50,
            100,
            TimeSpan.Zero,
            "stop");

        // Assert
        response.TokensPerSecond.Should().Be(0);
    }

    /// <summary>
    /// Tests that ChatResponse is a record with value equality.
    /// </summary>
    [Fact]
    public void ChatResponse_ShouldHaveValueEquality()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(100);
        var response1 = new ChatResponse("Content", 10, 20, duration, "stop");
        var response2 = new ChatResponse("Content", 10, 20, duration, "stop");

        // Assert
        response1.Should().Be(response2);
    }
}
