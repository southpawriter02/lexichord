// -----------------------------------------------------------------------
// <copyright file="ChatRequestTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatRequest"/> record.
/// </summary>
public class ChatRequestTests
{
    /// <summary>
    /// Tests that FromUserMessage creates a request with a single user message.
    /// </summary>
    [Fact]
    public void FromUserMessage_WithValidContent_ShouldCreateRequest()
    {
        // Arrange
        const string content = "Hello!";

        // Act
        var request = ChatRequest.FromUserMessage(content);

        // Assert
        request.Messages.Should().HaveCount(1);
        request.Messages[0].Role.Should().Be(ChatRole.User);
        request.Messages[0].Content.Should().Be(content);
        request.Options.Should().BeNull();
    }

    /// <summary>
    /// Tests that FromUserMessage accepts optional options.
    /// </summary>
    [Fact]
    public void FromUserMessage_WithOptions_ShouldCreateRequestWithOptions()
    {
        // Arrange
        const string content = "Hello!";
        var options = ChatOptions.Creative;

        // Act
        var request = ChatRequest.FromUserMessage(content, options);

        // Assert
        request.Messages.Should().HaveCount(1);
        request.Options.Should().Be(options);
    }

    /// <summary>
    /// Tests that FromUserMessage throws on null content.
    /// </summary>
    [Fact]
    public void FromUserMessage_WithNullContent_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatRequest.FromUserMessage(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that FromUserMessage throws on empty content.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromUserMessage_WithEmptyContent_ShouldThrowArgumentException(string content)
    {
        // Act
        var action = () => ChatRequest.FromUserMessage(content);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that WithSystemPrompt creates a request with system and user messages.
    /// </summary>
    [Fact]
    public void WithSystemPrompt_WithValidContent_ShouldCreateRequestWithTwoMessages()
    {
        // Arrange
        const string systemPrompt = "You are helpful.";
        const string userMessage = "Hello!";

        // Act
        var request = ChatRequest.WithSystemPrompt(systemPrompt, userMessage);

        // Assert
        request.Messages.Should().HaveCount(2);
        request.Messages[0].Role.Should().Be(ChatRole.System);
        request.Messages[0].Content.Should().Be(systemPrompt);
        request.Messages[1].Role.Should().Be(ChatRole.User);
        request.Messages[1].Content.Should().Be(userMessage);
    }

    /// <summary>
    /// Tests that WithSystemPrompt throws on null system prompt.
    /// </summary>
    [Fact]
    public void WithSystemPrompt_WithNullSystemPrompt_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatRequest.WithSystemPrompt(null!, "Hello!");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that WithSystemPrompt throws on null user message.
    /// </summary>
    [Fact]
    public void WithSystemPrompt_WithNullUserMessage_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatRequest.WithSystemPrompt("You are helpful.", null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that WithMessages appends messages to existing request.
    /// </summary>
    [Fact]
    public void WithMessages_ShouldAppendMessages()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage("Hello!");

        // Act
        var newRequest = request.WithMessages(
            ChatMessage.Assistant("Hi there!"),
            ChatMessage.User("How are you?"));

        // Assert
        newRequest.Messages.Should().HaveCount(3);
        newRequest.Messages[0].Role.Should().Be(ChatRole.User);
        newRequest.Messages[1].Role.Should().Be(ChatRole.Assistant);
        newRequest.Messages[2].Role.Should().Be(ChatRole.User);
    }

    /// <summary>
    /// Tests that WithOptions creates a new request with options.
    /// </summary>
    [Fact]
    public void WithOptions_ShouldCreateNewRequestWithOptions()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage("Hello!");
        var options = ChatOptions.Precise;

        // Act
        var newRequest = request.WithOptions(options);

        // Assert
        newRequest.Options.Should().Be(options);
        request.Options.Should().BeNull(); // Original unchanged
    }

    /// <summary>
    /// Tests GetSystemPrompt returns system message content.
    /// </summary>
    [Fact]
    public void GetSystemPrompt_WithSystemMessage_ShouldReturnContent()
    {
        // Arrange
        var request = ChatRequest.WithSystemPrompt("You are helpful.", "Hello!");

        // Act
        var systemPrompt = request.GetSystemPrompt();

        // Assert
        systemPrompt.Should().Be("You are helpful.");
    }

    /// <summary>
    /// Tests GetSystemPrompt returns null when no system message.
    /// </summary>
    [Fact]
    public void GetSystemPrompt_WithoutSystemMessage_ShouldReturnNull()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage("Hello!");

        // Act
        var systemPrompt = request.GetSystemPrompt();

        // Assert
        systemPrompt.Should().BeNull();
    }

    /// <summary>
    /// Tests GetLastUserMessage returns the last user message.
    /// </summary>
    [Fact]
    public void GetLastUserMessage_WithMultipleUserMessages_ShouldReturnLast()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage("First");
        request = request.WithMessages(
            ChatMessage.Assistant("Response"),
            ChatMessage.User("Second"));

        // Act
        var lastUserMessage = request.GetLastUserMessage();

        // Assert
        lastUserMessage.Should().Be("Second");
    }

    /// <summary>
    /// Tests that constructor throws on empty messages.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyMessages_ShouldThrowArgumentException()
    {
        // Act
        var action = () => new ChatRequest([], null);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be empty*");
    }
}
