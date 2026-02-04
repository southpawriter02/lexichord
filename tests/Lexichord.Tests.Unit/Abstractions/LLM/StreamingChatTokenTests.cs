// -----------------------------------------------------------------------
// <copyright file="StreamingChatTokenTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="StreamingChatToken"/> record.
/// </summary>
public class StreamingChatTokenTests
{
    /// <summary>
    /// Tests that Complete factory creates a completion token.
    /// </summary>
    [Fact]
    public void Complete_ShouldCreateCompletionToken()
    {
        // Act
        var token = StreamingChatToken.Complete("stop");

        // Assert
        token.Token.Should().BeEmpty();
        token.IsComplete.Should().BeTrue();
        token.FinishReason.Should().Be("stop");
    }

    /// <summary>
    /// Tests that Complete factory works without finish reason.
    /// </summary>
    [Fact]
    public void Complete_WithoutFinishReason_ShouldCreateCompletionToken()
    {
        // Act
        var token = StreamingChatToken.Complete();

        // Assert
        token.Token.Should().BeEmpty();
        token.IsComplete.Should().BeTrue();
        token.FinishReason.Should().BeNull();
    }

    /// <summary>
    /// Tests that Content factory creates a content token.
    /// </summary>
    [Fact]
    public void Content_ShouldCreateContentToken()
    {
        // Arrange
        const string content = "Hello";

        // Act
        var token = StreamingChatToken.Content(content);

        // Assert
        token.Token.Should().Be(content);
        token.IsComplete.Should().BeFalse();
        token.FinishReason.Should().BeNull();
    }

    /// <summary>
    /// Tests that Content factory throws on null.
    /// </summary>
    [Fact]
    public void Content_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => StreamingChatToken.Content(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that constructor throws on null token.
    /// </summary>
    [Fact]
    public void Constructor_WithNullToken_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new StreamingChatToken(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests HasContent property for various token values.
    /// </summary>
    [Theory]
    [InlineData("Hello", true)]
    [InlineData("", false)]
    [InlineData(" ", true)]
    public void HasContent_ShouldReturnCorrectValue(string tokenContent, bool expectedHasContent)
    {
        // Arrange
        var token = new StreamingChatToken(tokenContent);

        // Assert
        token.HasContent.Should().Be(expectedHasContent);
    }

    /// <summary>
    /// Tests default values for IsComplete and FinishReason.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultValues_ShouldHaveCorrectDefaults()
    {
        // Act
        var token = new StreamingChatToken("Hello");

        // Assert
        token.IsComplete.Should().BeFalse();
        token.FinishReason.Should().BeNull();
    }

    /// <summary>
    /// Tests that StreamingChatToken is a record with value equality.
    /// </summary>
    [Fact]
    public void StreamingChatToken_ShouldHaveValueEquality()
    {
        // Arrange
        var token1 = new StreamingChatToken("Hello", true, "stop");
        var token2 = new StreamingChatToken("Hello", true, "stop");

        // Assert
        token1.Should().Be(token2);
    }
}
