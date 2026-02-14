// -----------------------------------------------------------------------
// <copyright file="ContextGatheringRequestTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="ContextGatheringRequest"/> record.
/// </summary>
/// <remarks>
/// Tests cover factory methods, GetHint helper, computed properties,
/// and record equality behavior. Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class ContextGatheringRequestTests
{
    #region Factory Methods

    [Fact]
    public void Empty_CreatesRequestWithOnlyAgentId()
    {
        // Arrange & Act
        var request = ContextGatheringRequest.Empty("test-agent");

        // Assert
        request.AgentId.Should().Be("test-agent");
        request.DocumentPath.Should().BeNull();
        request.CursorPosition.Should().BeNull();
        request.SelectedText.Should().BeNull();
        request.Hints.Should().BeNull();
    }

    [Fact]
    public void Empty_ThrowsWhenAgentIdNull()
    {
        // Act
        var act = () => ContextGatheringRequest.Empty(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("agentId");
    }

    #endregion

    #region GetHint Method

    [Fact]
    public void GetHint_ReturnsDefaultWhenNoHints()
    {
        // Arrange
        var request = ContextGatheringRequest.Empty("agent");

        // Act
        var result = request.GetHint("MaxResults", 10);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void GetHint_ReturnsDefaultWhenKeyNotFound()
    {
        // Arrange
        var hints = new Dictionary<string, object> { ["Other"] = 5 };
        var request = new ContextGatheringRequest(null, null, null, "agent", hints);

        // Act
        var result = request.GetHint("MaxResults", 10);

        // Assert
        result.Should().Be(10);
    }

    [Fact]
    public void GetHint_ReturnsValueWhenFound()
    {
        // Arrange
        var hints = new Dictionary<string, object> { ["MaxResults"] = 20 };
        var request = new ContextGatheringRequest(null, null, null, "agent", hints);

        // Act
        var result = request.GetHint("MaxResults", 10);

        // Assert
        result.Should().Be(20);
    }

    [Fact]
    public void GetHint_ReturnsDefaultWhenWrongType()
    {
        // Arrange
        var hints = new Dictionary<string, object> { ["MaxResults"] = "not-a-number" };
        var request = new ContextGatheringRequest(null, null, null, "agent", hints);

        // Act
        var result = request.GetHint("MaxResults", 10);

        // Assert
        result.Should().Be(10);  // Returns default, not the string
    }

    [Fact]
    public void GetHint_WorksWithDifferentTypes()
    {
        // Arrange
        var hints = new Dictionary<string, object>
        {
            ["MaxResults"] = 20,
            ["IncludeHeadings"] = true,
            ["ContextRadius"] = 500
        };
        var request = new ContextGatheringRequest(null, null, null, "agent", hints);

        // Act & Assert
        request.GetHint("MaxResults", 0).Should().Be(20);
        request.GetHint("IncludeHeadings", false).Should().BeTrue();
        request.GetHint("ContextRadius", 0).Should().Be(500);
    }

    #endregion

    #region Computed Properties

    [Theory]
    [InlineData("/path/to/file.md", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasDocument_ReflectsDocumentPathPresence(string? documentPath, bool expected)
    {
        // Arrange
        var request = new ContextGatheringRequest(documentPath, null, null, "agent", null);

        // Act & Assert
        request.HasDocument.Should().Be(expected);
    }

    [Theory]
    [InlineData("some selected text", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void HasSelection_ReflectsSelectedTextPresence(string? selectedText, bool expected)
    {
        // Arrange
        var request = new ContextGatheringRequest(null, null, selectedText, "agent", null);

        // Act & Assert
        request.HasSelection.Should().Be(expected);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(100, true)]
    [InlineData(null, false)]
    public void HasCursor_ReflectsCursorPositionPresence(int? cursorPosition, bool expected)
    {
        // Arrange
        var request = new ContextGatheringRequest(null, cursorPosition, null, "agent", null);

        // Act & Assert
        request.HasCursor.Should().Be(expected);
    }

    #endregion

    #region Record Equality

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        // Arrange
        var hints = new Dictionary<string, object> { ["test"] = 1 };
        var request1 = new ContextGatheringRequest("/doc.md", 100, "text", "agent", hints);
        var request2 = new ContextGatheringRequest("/doc.md", 100, "text", "agent", hints);

        // Assert
        request1.Should().Be(request2);
    }

    [Fact]
    public void WithExpression_CreatesNewInstanceWithChangedProperty()
    {
        // Arrange
        var original = new ContextGatheringRequest("/doc.md", 100, "text", "agent", null);

        // Act
        var modified = original with { SelectedText = "new text" };

        // Assert
        modified.SelectedText.Should().Be("new text");
        modified.DocumentPath.Should().Be(original.DocumentPath);
        modified.CursorPosition.Should().Be(original.CursorPosition);
        original.SelectedText.Should().Be("text");  // Unchanged
    }

    #endregion
}
