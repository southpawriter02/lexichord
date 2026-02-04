// -----------------------------------------------------------------------
// <copyright file="ChatSerializationTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatSerialization"/> class.
/// </summary>
public class ChatSerializationTests
{
    /// <summary>
    /// Tests that ToJson serializes ChatMessage correctly.
    /// </summary>
    [Fact]
    public void ToJson_ChatMessage_ShouldSerializeCorrectly()
    {
        // Arrange
        var message = ChatMessage.User("Hello!");

        // Act
        var json = ChatSerialization.ToJson(message);

        // Assert
        json.Should().Contain("\"role\"");
        json.Should().Contain("\"content\"");
        json.Should().Contain("\"Hello!\"");
    }

    /// <summary>
    /// Tests that ToJson throws on null.
    /// </summary>
    [Fact]
    public void ToJson_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => ChatSerialization.ToJson<ChatMessage>(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that FromJson deserializes ChatMessage correctly.
    /// </summary>
    [Fact]
    public void FromJson_ChatMessage_ShouldDeserializeCorrectly()
    {
        // Arrange
        var json = "{\"role\":\"user\",\"content\":\"Hello!\"}";

        // Act
        var message = ChatSerialization.FromJson<ChatMessage>(json);

        // Assert
        message.Role.Should().Be(ChatRole.User);
        message.Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Tests that FromJson throws on null.
    /// </summary>
    [Fact]
    public void FromJson_WithNull_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatSerialization.FromJson<ChatMessage>(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that FromJson throws on empty string.
    /// </summary>
    [Fact]
    public void FromJson_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatSerialization.FromJson<ChatMessage>("");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that FromJson throws on invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_WithInvalidJson_ShouldThrowJsonException()
    {
        // Act
        var action = () => ChatSerialization.FromJson<ChatMessage>("not valid json");

        // Assert
        action.Should().Throw<JsonException>();
    }

    /// <summary>
    /// Tests that TryFromJson returns true on valid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_WithValidJson_ShouldReturnTrue()
    {
        // Arrange
        var json = "{\"role\":\"user\",\"content\":\"Hello!\"}";

        // Act
        var result = ChatSerialization.TryFromJson<ChatMessage>(json, out var message);

        // Assert
        result.Should().BeTrue();
        message.Should().NotBeNull();
        message!.Content.Should().Be("Hello!");
    }

    /// <summary>
    /// Tests that TryFromJson returns false on invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_WithInvalidJson_ShouldReturnFalse()
    {
        // Act
        var result = ChatSerialization.TryFromJson<ChatMessage>("not valid", out var message);

        // Assert
        result.Should().BeFalse();
        message.Should().BeNull();
    }

    /// <summary>
    /// Tests that TryFromJson returns false on null.
    /// </summary>
    [Fact]
    public void TryFromJson_WithNull_ShouldReturnFalse()
    {
        // Act
        var result = ChatSerialization.TryFromJson<ChatMessage>(null!, out var message);

        // Assert
        result.Should().BeFalse();
        message.Should().BeNull();
    }

    /// <summary>
    /// Tests that CreateOptions creates options with expected defaults.
    /// </summary>
    [Fact]
    public void CreateOptions_ShouldCreateOptionsWithExpectedDefaults()
    {
        // Act
        var options = ChatSerialization.CreateOptions();

        // Assert
        options.PropertyNameCaseInsensitive.Should().BeTrue();
        options.WriteIndented.Should().BeFalse();
    }

    /// <summary>
    /// Tests that CreateOptions respects writeIndented parameter.
    /// </summary>
    [Fact]
    public void CreateOptions_WithWriteIndented_ShouldSetWriteIndented()
    {
        // Act
        var options = ChatSerialization.CreateOptions(writeIndented: true);

        // Assert
        options.WriteIndented.Should().BeTrue();
    }

    /// <summary>
    /// Tests that SerializeRequest serializes ChatRequest.
    /// </summary>
    [Fact]
    public void SerializeRequest_ShouldSerializeRequest()
    {
        // Arrange
        var request = ChatRequest.FromUserMessage("Hello!");

        // Act
        var json = ChatSerialization.SerializeRequest(request);

        // Assert
        json.Should().Contain("messages");
        json.Should().Contain("Hello!");
    }

    /// <summary>
    /// Tests that SerializeRequest throws on null.
    /// </summary>
    [Fact]
    public void SerializeRequest_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => ChatSerialization.SerializeRequest(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that SerializeResponse serializes ChatResponse.
    /// </summary>
    [Fact]
    public void SerializeResponse_ShouldSerializeResponse()
    {
        // Arrange
        var response = new ChatResponse(
            "Response content",
            10,
            20,
            TimeSpan.FromMilliseconds(100),
            "stop");

        // Act
        var json = ChatSerialization.SerializeResponse(response);

        // Assert
        json.Should().Contain("content");
        json.Should().Contain("Response content");
        json.Should().Contain("promptTokens");
    }

    /// <summary>
    /// Tests roundtrip serialization/deserialization.
    /// </summary>
    [Fact]
    public void RoundTrip_ChatMessage_ShouldPreserveData()
    {
        // Arrange
        var original = ChatMessage.User("Hello, world!");

        // Act
        var json = ChatSerialization.ToJson(original);
        var restored = ChatSerialization.FromJson<ChatMessage>(json);

        // Assert
        restored.Should().Be(original);
    }
}
