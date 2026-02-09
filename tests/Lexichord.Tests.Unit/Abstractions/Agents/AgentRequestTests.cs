// -----------------------------------------------------------------------
// <copyright file="AgentRequestTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents;

/// <summary>
/// Unit tests for <see cref="AgentRequest"/> record.
/// </summary>
/// <remarks>
/// Tests cover construction, validation, helper properties, and record equality.
/// Introduced in v0.6.6a.
/// </remarks>
public class AgentRequestTests
{
    // -----------------------------------------------------------------------
    // Constructor Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that the constructor creates a request with default optional values.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Constructor_ValidMessage_CreatesRequest()
    {
        // Act
        var request = new AgentRequest("Hello");

        // Assert
        request.UserMessage.Should().Be("Hello");
        request.History.Should().BeNull();
        request.DocumentPath.Should().BeNull();
        request.Selection.Should().BeNull();
    }

    /// <summary>
    /// Verifies that the constructor creates a request with all parameters specified.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Constructor_AllParameters_CreatesFullRequest()
    {
        // Arrange
        var history = new[]
        {
            new ChatMessage(ChatRole.User, "Hi"),
            new ChatMessage(ChatRole.Assistant, "Hello!")
        };

        // Act
        var request = new AgentRequest(
            "How can I improve this paragraph?",
            History: history,
            DocumentPath: "/path/to/novel.md",
            Selection: "The sun set slowly...");

        // Assert
        request.UserMessage.Should().Be("How can I improve this paragraph?");
        request.History.Should().HaveCount(2);
        request.DocumentPath.Should().Be("/path/to/novel.md");
        request.Selection.Should().Be("The sun set slowly...");
    }

    // -----------------------------------------------------------------------
    // Validate Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that Validate succeeds for a valid message.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void Validate_ValidMessage_DoesNotThrow()
    {
        // Arrange
        var request = new AgentRequest("What is the hero's journey?");

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Verifies that Validate throws ArgumentException for empty, whitespace, or null-like messages.
    /// </summary>
    [Theory]
    [Trait("SubPart", "v0.6.6a")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void Validate_EmptyOrWhitespaceMessage_ThrowsArgumentException(string message)
    {
        // Arrange
        var request = new AgentRequest(message);

        // Act
        var act = () => request.Validate();

        // Assert
        act.Should().Throw<ArgumentException>()
           .WithParameterName("UserMessage");
    }

    // -----------------------------------------------------------------------
    // HasDocumentContext Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that HasDocumentContext returns true when a document path is provided.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasDocumentContext_WithPath_ReturnsTrue()
    {
        // Arrange
        var request = new AgentRequest("Hello", DocumentPath: "/path/to/doc.md");

        // Assert
        request.HasDocumentContext.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that HasDocumentContext returns false when no document path is provided.
    /// </summary>
    [Theory]
    [Trait("SubPart", "v0.6.6a")]
    [InlineData(null)]
    [InlineData("")]
    public void HasDocumentContext_WithoutPath_ReturnsFalse(string? path)
    {
        // Arrange
        var request = new AgentRequest("Hello", DocumentPath: path);

        // Assert
        request.HasDocumentContext.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // HasSelection Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that HasSelection returns true when a selection is provided.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasSelection_WithSelection_ReturnsTrue()
    {
        // Arrange
        var request = new AgentRequest("Hello", Selection: "selected text");

        // Assert
        request.HasSelection.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that HasSelection returns false when no selection is provided.
    /// </summary>
    [Theory]
    [Trait("SubPart", "v0.6.6a")]
    [InlineData(null)]
    [InlineData("")]
    public void HasSelection_WithoutSelection_ReturnsFalse(string? selection)
    {
        // Arrange
        var request = new AgentRequest("Hello", Selection: selection);

        // Assert
        request.HasSelection.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // HasHistory / HistoryCount Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that HasHistory returns true and HistoryCount is correct when history is provided.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasHistory_WithMessages_ReturnsTrue()
    {
        // Arrange
        var history = new[] { new ChatMessage(ChatRole.User, "Hi") };
        var request = new AgentRequest("Hello", History: history);

        // Assert
        request.HasHistory.Should().BeTrue();
        request.HistoryCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that HasHistory returns false when history is null.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasHistory_NullHistory_ReturnsFalse()
    {
        // Arrange
        var request = new AgentRequest("Hello");

        // Assert
        request.HasHistory.Should().BeFalse();
        request.HistoryCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that HasHistory returns false when history is an empty list.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void HasHistory_EmptyHistory_ReturnsFalse()
    {
        // Arrange
        var request = new AgentRequest("Hello", History: Array.Empty<ChatMessage>());

        // Assert
        request.HasHistory.Should().BeFalse();
        request.HistoryCount.Should().Be(0);
    }

    // -----------------------------------------------------------------------
    // Record Equality Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that record equality works as expected for AgentRequest.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var request1 = new AgentRequest("Hello", DocumentPath: "/path/doc.md");
        var request2 = new AgentRequest("Hello", DocumentPath: "/path/doc.md");

        // Assert
        request1.Should().Be(request2);
    }

    /// <summary>
    /// Verifies that record with-expression creates a modified copy.
    /// </summary>
    [Fact]
    [Trait("SubPart", "v0.6.6a")]
    public void WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        var original = new AgentRequest("Hello");

        // Act
        var modified = original with { DocumentPath = "/path/doc.md" };

        // Assert
        modified.UserMessage.Should().Be("Hello");
        modified.DocumentPath.Should().Be("/path/doc.md");
        original.DocumentPath.Should().BeNull(); // original unchanged
    }
}
