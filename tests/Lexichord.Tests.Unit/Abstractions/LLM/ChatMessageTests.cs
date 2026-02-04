// -----------------------------------------------------------------------
// <copyright file="ChatMessageTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatMessage"/> record.
/// </summary>
public class ChatMessageTests
{
    /// <summary>
    /// Tests that System factory creates a message with System role.
    /// </summary>
    [Fact]
    public void System_WithValidContent_ShouldCreateSystemMessage()
    {
        // Arrange
        const string content = "You are a helpful assistant.";

        // Act
        var message = ChatMessage.System(content);

        // Assert
        message.Role.Should().Be(ChatRole.System);
        message.Content.Should().Be(content);
    }

    /// <summary>
    /// Tests that User factory creates a message with User role.
    /// </summary>
    [Fact]
    public void User_WithValidContent_ShouldCreateUserMessage()
    {
        // Arrange
        const string content = "Hello, how are you?";

        // Act
        var message = ChatMessage.User(content);

        // Assert
        message.Role.Should().Be(ChatRole.User);
        message.Content.Should().Be(content);
    }

    /// <summary>
    /// Tests that Assistant factory creates a message with Assistant role.
    /// </summary>
    [Fact]
    public void Assistant_WithValidContent_ShouldCreateAssistantMessage()
    {
        // Arrange
        const string content = "I'm doing well, thank you!";

        // Act
        var message = ChatMessage.Assistant(content);

        // Assert
        message.Role.Should().Be(ChatRole.Assistant);
        message.Content.Should().Be(content);
    }

    /// <summary>
    /// Tests that Tool factory creates a message with Tool role.
    /// </summary>
    [Fact]
    public void Tool_WithValidContent_ShouldCreateToolMessage()
    {
        // Arrange
        const string content = "{\"result\": 42}";

        // Act
        var message = ChatMessage.Tool(content);

        // Assert
        message.Role.Should().Be(ChatRole.Tool);
        message.Content.Should().Be(content);
    }

    /// <summary>
    /// Tests that factory methods throw on null content.
    /// </summary>
    [Fact]
    public void FactoryMethods_WithNullContent_ShouldThrowArgumentException()
    {
        // Act & Assert
        var systemAction = () => ChatMessage.System(null!);
        var userAction = () => ChatMessage.User(null!);
        var assistantAction = () => ChatMessage.Assistant(null!);
        var toolAction = () => ChatMessage.Tool(null!);

        systemAction.Should().Throw<ArgumentException>();
        userAction.Should().Throw<ArgumentException>();
        assistantAction.Should().Throw<ArgumentException>();
        toolAction.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that factory methods throw on empty content.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void FactoryMethods_WithEmptyOrWhitespaceContent_ShouldThrowArgumentException(string content)
    {
        // Act & Assert
        var systemAction = () => ChatMessage.System(content);
        var userAction = () => ChatMessage.User(content);
        var assistantAction = () => ChatMessage.Assistant(content);
        var toolAction = () => ChatMessage.Tool(content);

        systemAction.Should().Throw<ArgumentException>();
        userAction.Should().Throw<ArgumentException>();
        assistantAction.Should().Throw<ArgumentException>();
        toolAction.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that direct construction with null content throws.
    /// </summary>
    [Fact]
    public void Constructor_WithNullContent_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ChatMessage(ChatRole.User, null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that direct construction works with valid content.
    /// </summary>
    [Fact]
    public void Constructor_WithValidContent_ShouldCreateMessage()
    {
        // Arrange
        const string content = "Test message";

        // Act
        var message = new ChatMessage(ChatRole.User, content);

        // Assert
        message.Role.Should().Be(ChatRole.User);
        message.Content.Should().Be(content);
    }

    /// <summary>
    /// Tests that ChatMessage is a record with value equality.
    /// </summary>
    [Fact]
    public void ChatMessage_ShouldHaveValueEquality()
    {
        // Arrange
        var message1 = ChatMessage.User("Hello");
        var message2 = ChatMessage.User("Hello");

        // Assert
        message1.Should().Be(message2);
        (message1 == message2).Should().BeTrue();
    }

    /// <summary>
    /// Tests that different messages are not equal.
    /// </summary>
    [Fact]
    public void ChatMessage_WithDifferentContent_ShouldNotBeEqual()
    {
        // Arrange
        var message1 = ChatMessage.User("Hello");
        var message2 = ChatMessage.User("Goodbye");

        // Assert
        message1.Should().NotBe(message2);
    }
}
