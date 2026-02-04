// -----------------------------------------------------------------------
// <copyright file="RenderedPromptTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="RenderedPrompt"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3a")]
public class RenderedPromptTests
{
    /// <summary>
    /// Tests that EstimatedTokens calculates correctly.
    /// </summary>
    [Fact]
    public void EstimatedTokens_ShouldCalculateBasedOnCharacterCount()
    {
        // Arrange - 20 chars in system, 20 chars in user = 40 chars / 4 = 10 tokens
        var rendered = new RenderedPrompt(
            "12345678901234567890",  // 20 chars
            "12345678901234567890",  // 20 chars
            Array.Empty<ChatMessage>(),
            TimeSpan.Zero);

        // Assert
        rendered.EstimatedTokens.Should().Be(10);
    }

    /// <summary>
    /// Tests that TotalCharacters calculates correctly.
    /// </summary>
    [Fact]
    public void TotalCharacters_ShouldSumPromptLengths()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System",    // 6 chars
            "User",      // 4 chars
            Array.Empty<ChatMessage>(),
            TimeSpan.Zero);

        // Assert
        rendered.TotalCharacters.Should().Be(10);
    }

    /// <summary>
    /// Tests that WasFastRender returns true for fast renders.
    /// </summary>
    [Fact]
    public void WasFastRender_WithDurationUnder10ms_ShouldReturnTrue()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System",
            "User",
            Array.Empty<ChatMessage>(),
            TimeSpan.FromMilliseconds(5));

        // Assert
        rendered.WasFastRender.Should().BeTrue();
    }

    /// <summary>
    /// Tests that WasFastRender returns false for slow renders.
    /// </summary>
    [Fact]
    public void WasFastRender_WithDurationOver10ms_ShouldReturnFalse()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System",
            "User",
            Array.Empty<ChatMessage>(),
            TimeSpan.FromMilliseconds(15));

        // Assert
        rendered.WasFastRender.Should().BeFalse();
    }

    /// <summary>
    /// Tests that WasFastRender returns false at exactly 10ms.
    /// </summary>
    [Fact]
    public void WasFastRender_WithDurationExactly10ms_ShouldReturnFalse()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System",
            "User",
            Array.Empty<ChatMessage>(),
            TimeSpan.FromMilliseconds(10));

        // Assert
        rendered.WasFastRender.Should().BeFalse();
    }

    /// <summary>
    /// Tests that MessageCount returns correct count.
    /// </summary>
    [Fact]
    public void MessageCount_ShouldReturnNumberOfMessages()
    {
        // Arrange
        var messages = new[]
        {
            ChatMessage.System("System prompt"),
            ChatMessage.User("User prompt")
        };
        var rendered = new RenderedPrompt("System", "User", messages, TimeSpan.Zero);

        // Assert
        rendered.MessageCount.Should().Be(2);
    }

    /// <summary>
    /// Tests that HasSystemPrompt returns true when system prompt exists.
    /// </summary>
    [Fact]
    public void HasSystemPrompt_WithContent_ShouldReturnTrue()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System prompt",
            "User",
            Array.Empty<ChatMessage>(),
            TimeSpan.Zero);

        // Assert
        rendered.HasSystemPrompt.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasSystemPrompt returns false when system prompt is empty.
    /// </summary>
    [Fact]
    public void HasSystemPrompt_WithEmptyContent_ShouldReturnFalse()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "",
            "User",
            Array.Empty<ChatMessage>(),
            TimeSpan.Zero);

        // Assert
        rendered.HasSystemPrompt.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasUserPrompt returns true when user prompt exists.
    /// </summary>
    [Fact]
    public void HasUserPrompt_WithContent_ShouldReturnTrue()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System",
            "User prompt",
            Array.Empty<ChatMessage>(),
            TimeSpan.Zero);

        // Assert
        rendered.HasUserPrompt.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasUserPrompt returns false when user prompt is empty.
    /// </summary>
    [Fact]
    public void HasUserPrompt_WithEmptyContent_ShouldReturnFalse()
    {
        // Arrange
        var rendered = new RenderedPrompt(
            "System",
            "",
            Array.Empty<ChatMessage>(),
            TimeSpan.Zero);

        // Assert
        rendered.HasUserPrompt.Should().BeFalse();
    }

    /// <summary>
    /// Tests that null values default to safe values.
    /// </summary>
    [Fact]
    public void Constructor_WithNullValues_ShouldDefaultToSafeValues()
    {
        // Act
        var rendered = new RenderedPrompt(null!, null!, null!, TimeSpan.Zero);

        // Assert
        rendered.SystemPrompt.Should().BeEmpty();
        rendered.UserPrompt.Should().BeEmpty();
        rendered.Messages.Should().NotBeNull();
        rendered.Messages.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that RenderedPrompt has value equality.
    /// </summary>
    [Fact]
    public void RenderedPrompt_ShouldHaveValueEquality()
    {
        // Arrange
        var messages = new[] { ChatMessage.User("test") };
        var duration = TimeSpan.FromMilliseconds(5);

        var rendered1 = new RenderedPrompt("sys", "user", messages, duration);
        var rendered2 = new RenderedPrompt("sys", "user", messages, duration);

        // Assert
        rendered1.Should().Be(rendered2);
    }

    /// <summary>
    /// Tests that different RenderedPrompts are not equal.
    /// </summary>
    [Fact]
    public void RenderedPrompt_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var rendered1 = new RenderedPrompt("sys1", "user", Array.Empty<ChatMessage>(), TimeSpan.Zero);
        var rendered2 = new RenderedPrompt("sys2", "user", Array.Empty<ChatMessage>(), TimeSpan.Zero);

        // Assert
        rendered1.Should().NotBe(rendered2);
    }

    /// <summary>
    /// Tests that EstimatedTokens handles empty prompts.
    /// </summary>
    [Fact]
    public void EstimatedTokens_WithEmptyPrompts_ShouldReturnZero()
    {
        // Arrange
        var rendered = new RenderedPrompt("", "", Array.Empty<ChatMessage>(), TimeSpan.Zero);

        // Assert
        rendered.EstimatedTokens.Should().Be(0);
    }

    /// <summary>
    /// Tests that RenderDuration is accessible.
    /// </summary>
    [Fact]
    public void RenderDuration_ShouldBeAccessible()
    {
        // Arrange
        var duration = TimeSpan.FromMilliseconds(25);
        var rendered = new RenderedPrompt("sys", "user", Array.Empty<ChatMessage>(), duration);

        // Assert
        rendered.RenderDuration.Should().Be(duration);
    }
}
