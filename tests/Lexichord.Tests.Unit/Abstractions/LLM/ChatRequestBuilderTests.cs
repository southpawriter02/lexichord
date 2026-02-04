// -----------------------------------------------------------------------
// <copyright file="ChatRequestBuilderTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatRequestBuilder"/> class.
/// </summary>
public class ChatRequestBuilderTests
{
    /// <summary>
    /// Tests that Create returns a new builder instance.
    /// </summary>
    [Fact]
    public void Create_ShouldReturnNewBuilderInstance()
    {
        // Act
        var builder1 = ChatRequestBuilder.Create();
        var builder2 = ChatRequestBuilder.Create();

        // Assert
        builder1.Should().NotBeNull();
        builder2.Should().NotBeNull();
        builder1.Should().NotBeSameAs(builder2);
    }

    /// <summary>
    /// Tests that WithSystemPrompt adds a system message.
    /// </summary>
    [Fact]
    public void WithSystemPrompt_ShouldAddSystemMessage()
    {
        // Arrange
        var builder = ChatRequestBuilder.Create();

        // Act
        var request = builder
            .WithSystemPrompt("You are helpful.")
            .AddUserMessage("Hello!")
            .Build();

        // Assert
        request.Messages.Should().HaveCount(2);
        request.Messages[0].Role.Should().Be(ChatRole.System);
        request.Messages[0].Content.Should().Be("You are helpful.");
    }

    /// <summary>
    /// Tests that WithSystemPrompt throws on null content.
    /// </summary>
    [Fact]
    public void WithSystemPrompt_WithNullContent_ShouldThrowArgumentException()
    {
        // Arrange
        var builder = ChatRequestBuilder.Create();

        // Act
        var action = () => builder.WithSystemPrompt(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that WithSystemPrompt throws if called twice.
    /// </summary>
    [Fact]
    public void WithSystemPrompt_CalledTwice_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = ChatRequestBuilder.Create()
            .WithSystemPrompt("First prompt");

        // Act
        var action = () => builder.WithSystemPrompt("Second prompt");

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*already been added*");
    }

    /// <summary>
    /// Tests that AddUserMessage adds a user message.
    /// </summary>
    [Fact]
    public void AddUserMessage_ShouldAddUserMessage()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .Build();

        // Assert
        request.Messages.Should().HaveCount(1);
        request.Messages[0].Role.Should().Be(ChatRole.User);
        request.Messages[0].Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Tests that AddAssistantMessage adds an assistant message.
    /// </summary>
    [Fact]
    public void AddAssistantMessage_ShouldAddAssistantMessage()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .AddAssistantMessage("Hi there!")
            .Build();

        // Assert
        request.Messages.Should().HaveCount(2);
        request.Messages[1].Role.Should().Be(ChatRole.Assistant);
    }

    /// <summary>
    /// Tests that AddToolMessage adds a tool message.
    /// </summary>
    [Fact]
    public void AddToolMessage_ShouldAddToolMessage()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .AddToolMessage("{\"result\": 42}")
            .Build();

        // Assert
        request.Messages.Should().HaveCount(2);
        request.Messages[1].Role.Should().Be(ChatRole.Tool);
    }

    /// <summary>
    /// Tests that AddMessage adds an arbitrary message.
    /// </summary>
    [Fact]
    public void AddMessage_ShouldAddArbitraryMessage()
    {
        // Arrange
        var message = new ChatMessage(ChatRole.User, "Hello!");

        // Act
        var request = ChatRequestBuilder.Create()
            .AddMessage(message)
            .Build();

        // Assert
        request.Messages.Should().HaveCount(1);
        request.Messages[0].Should().Be(message);
    }

    /// <summary>
    /// Tests that AddMessages adds multiple messages.
    /// </summary>
    [Fact]
    public void AddMessages_ShouldAddMultipleMessages()
    {
        // Arrange
        var messages = new[]
        {
            ChatMessage.User("Hello!"),
            ChatMessage.Assistant("Hi there!")
        };

        // Act
        var request = ChatRequestBuilder.Create()
            .AddMessages(messages)
            .Build();

        // Assert
        request.Messages.Should().HaveCount(2);
    }

    /// <summary>
    /// Tests that WithOptions sets the options.
    /// </summary>
    [Fact]
    public void WithOptions_ShouldSetOptions()
    {
        // Arrange
        var options = ChatOptions.Creative;

        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .WithOptions(options)
            .Build();

        // Assert
        request.Options.Should().Be(options);
    }

    /// <summary>
    /// Tests that WithModel sets the model in options.
    /// </summary>
    [Fact]
    public void WithModel_ShouldSetModelInOptions()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .WithModel("gpt-4")
            .Build();

        // Assert
        request.Options.Should().NotBeNull();
        request.Options!.Model.Should().Be("gpt-4");
    }

    /// <summary>
    /// Tests that WithTemperature sets temperature in options.
    /// </summary>
    [Fact]
    public void WithTemperature_ShouldSetTemperatureInOptions()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .WithTemperature(0.7)
            .Build();

        // Assert
        request.Options.Should().NotBeNull();
        request.Options!.Temperature.Should().Be(0.7);
    }

    /// <summary>
    /// Tests that WithMaxTokens sets max tokens in options.
    /// </summary>
    [Fact]
    public void WithMaxTokens_ShouldSetMaxTokensInOptions()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .WithMaxTokens(1000)
            .Build();

        // Assert
        request.Options.Should().NotBeNull();
        request.Options!.MaxTokens.Should().Be(1000);
    }

    /// <summary>
    /// Tests that ClearMessages removes all messages.
    /// </summary>
    [Fact]
    public void ClearMessages_ShouldRemoveAllMessages()
    {
        // Arrange
        var builder = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .AddAssistantMessage("Hi!");

        // Act
        builder.ClearMessages();

        // Assert - Build should throw because no messages
        var action = () => builder.Build();
        action.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Tests that Reset clears messages and options.
    /// </summary>
    [Fact]
    public void Reset_ShouldClearMessagesAndOptions()
    {
        // Arrange
        var builder = ChatRequestBuilder.Create()
            .AddUserMessage("Hello!")
            .WithOptions(ChatOptions.Creative);

        // Act
        builder.Reset();
        builder.AddUserMessage("New message");
        var request = builder.Build();

        // Assert
        request.Messages.Should().HaveCount(1);
        request.Messages[0].Content.Should().Be("New message");
        request.Options.Should().BeNull();
    }

    /// <summary>
    /// Tests that Build throws when no messages added.
    /// </summary>
    [Fact]
    public void Build_WithNoMessages_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var builder = ChatRequestBuilder.Create();

        // Act
        var action = () => builder.Build();

        // Assert
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one message*");
    }

    /// <summary>
    /// Tests that methods are chainable.
    /// </summary>
    [Fact]
    public void Methods_ShouldBeChainable()
    {
        // Act
        var request = ChatRequestBuilder.Create()
            .WithSystemPrompt("You are helpful.")
            .AddUserMessage("Hello!")
            .AddAssistantMessage("Hi there!")
            .AddUserMessage("How are you?")
            .WithModel("gpt-4")
            .WithTemperature(0.7)
            .WithMaxTokens(1000)
            .Build();

        // Assert
        request.Messages.Should().HaveCount(4);
        request.Options.Should().NotBeNull();
        request.Options!.Model.Should().Be("gpt-4");
        request.Options!.Temperature.Should().Be(0.7);
        request.Options!.MaxTokens.Should().Be(1000);
    }
}
