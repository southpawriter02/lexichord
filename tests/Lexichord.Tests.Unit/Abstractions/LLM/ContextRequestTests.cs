// -----------------------------------------------------------------------
// <copyright file="ContextRequestTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ContextRequest"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3a")]
public class ContextRequestTests
{
    /// <summary>
    /// Tests that ForUserInput creates a minimal request.
    /// </summary>
    [Fact]
    public void ForUserInput_ShouldCreateMinimalRequest()
    {
        // Act
        var request = ContextRequest.ForUserInput("Hello");

        // Assert
        request.SelectedText.Should().Be("Hello");
        request.CurrentDocumentPath.Should().BeNull();
        request.CursorPosition.Should().BeNull();
        request.IncludeStyleRules.Should().BeFalse();
        request.IncludeRAGContext.Should().BeFalse();
        request.HasContextSources.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Full creates a request with all sources enabled.
    /// </summary>
    [Fact]
    public void Full_ShouldEnableAllSources()
    {
        // Act
        var request = ContextRequest.Full("/doc.md", "selected text");

        // Assert
        request.CurrentDocumentPath.Should().Be("/doc.md");
        request.SelectedText.Should().Be("selected text");
        request.IncludeStyleRules.Should().BeTrue();
        request.IncludeRAGContext.Should().BeTrue();
        request.HasContextSources.Should().BeTrue();
    }

    /// <summary>
    /// Tests that StyleOnly creates a style-only request.
    /// </summary>
    [Fact]
    public void StyleOnly_ShouldEnableOnlyStyleRules()
    {
        // Act
        var request = ContextRequest.StyleOnly("/doc.md");

        // Assert
        request.CurrentDocumentPath.Should().Be("/doc.md");
        request.SelectedText.Should().BeNull();
        request.IncludeStyleRules.Should().BeTrue();
        request.IncludeRAGContext.Should().BeFalse();
        request.HasContextSources.Should().BeTrue();
    }

    /// <summary>
    /// Tests that RAGOnly creates a RAG-only request.
    /// </summary>
    [Fact]
    public void RAGOnly_ShouldEnableOnlyRAGContext()
    {
        // Act
        var request = ContextRequest.RAGOnly("query text", 5);

        // Assert
        request.SelectedText.Should().Be("query text");
        request.MaxRAGChunks.Should().Be(5);
        request.IncludeRAGContext.Should().BeTrue();
        request.IncludeStyleRules.Should().BeFalse();
        request.CurrentDocumentPath.Should().BeNull();
    }

    /// <summary>
    /// Tests that RAGOnly uses default max chunks.
    /// </summary>
    [Fact]
    public void RAGOnly_WithoutMaxChunks_ShouldUseDefault()
    {
        // Act
        var request = ContextRequest.RAGOnly("query");

        // Assert
        request.MaxRAGChunks.Should().Be(3);
    }

    /// <summary>
    /// Tests that HasDocumentContext returns true when path is provided.
    /// </summary>
    [Fact]
    public void HasDocumentContext_WithPath_ShouldReturnTrue()
    {
        // Arrange
        var request = new ContextRequest("/doc.md", null, null, false, false);

        // Assert
        request.HasDocumentContext.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasDocumentContext returns false when path is null.
    /// </summary>
    [Fact]
    public void HasDocumentContext_WithNullPath_ShouldReturnFalse()
    {
        // Arrange
        var request = ContextRequest.ForUserInput("test");

        // Assert
        request.HasDocumentContext.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasContextSources returns true when style rules enabled.
    /// </summary>
    [Fact]
    public void HasContextSources_WithStyleRules_ShouldReturnTrue()
    {
        // Arrange
        var request = new ContextRequest(null, null, null, true, false);

        // Assert
        request.HasContextSources.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasContextSources returns true when RAG context enabled.
    /// </summary>
    [Fact]
    public void HasContextSources_WithRAGContext_ShouldReturnTrue()
    {
        // Arrange
        var request = new ContextRequest(null, null, null, false, true);

        // Assert
        request.HasContextSources.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasContextSources returns false when both disabled.
    /// </summary>
    [Fact]
    public void HasContextSources_WithBothDisabled_ShouldReturnFalse()
    {
        // Arrange
        var request = new ContextRequest(null, null, null, false, false);

        // Assert
        request.HasContextSources.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasSelectedText returns true when text is provided.
    /// </summary>
    [Fact]
    public void HasSelectedText_WithText_ShouldReturnTrue()
    {
        // Arrange
        var request = ContextRequest.ForUserInput("some text");

        // Assert
        request.HasSelectedText.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasSelectedText returns false when text is null or whitespace.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasSelectedText_WithNullOrWhitespace_ShouldReturnFalse(string? text)
    {
        // Arrange
        var request = new ContextRequest(null, null, text, false, false);

        // Assert
        request.HasSelectedText.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasCursorPosition returns true when position is provided.
    /// </summary>
    [Fact]
    public void HasCursorPosition_WithPosition_ShouldReturnTrue()
    {
        // Arrange
        var request = new ContextRequest("/doc.md", 42, null, false, false);

        // Assert
        request.HasCursorPosition.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasCursorPosition returns false when position is null.
    /// </summary>
    [Fact]
    public void HasCursorPosition_WithNullPosition_ShouldReturnFalse()
    {
        // Arrange
        var request = new ContextRequest("/doc.md", null, null, false, false);

        // Assert
        request.HasCursorPosition.Should().BeFalse();
    }

    /// <summary>
    /// Tests that MaxRAGChunks defaults to 3 when not specified.
    /// </summary>
    [Fact]
    public void MaxRAGChunks_WithDefaultValue_ShouldBe3()
    {
        // Arrange
        var request = new ContextRequest(null, null, null, false, true);

        // Assert
        request.MaxRAGChunks.Should().Be(3);
    }

    /// <summary>
    /// Tests that MaxRAGChunks clamps to 3 when non-positive value provided.
    /// </summary>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void MaxRAGChunks_WithNonPositiveValue_ShouldDefaultTo3(int value)
    {
        // Arrange
        var request = new ContextRequest(null, null, null, false, true, value);

        // Assert
        request.MaxRAGChunks.Should().Be(3);
    }

    /// <summary>
    /// Tests that ContextRequest has value equality.
    /// </summary>
    [Fact]
    public void ContextRequest_ShouldHaveValueEquality()
    {
        // Arrange
        var request1 = ContextRequest.ForUserInput("test");
        var request2 = ContextRequest.ForUserInput("test");

        // Assert
        request1.Should().Be(request2);
        request1.GetHashCode().Should().Be(request2.GetHashCode());
    }

    /// <summary>
    /// Tests that different ContextRequests are not equal.
    /// </summary>
    [Fact]
    public void ContextRequest_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var request1 = ContextRequest.ForUserInput("test1");
        var request2 = ContextRequest.ForUserInput("test2");

        // Assert
        request1.Should().NotBe(request2);
    }
}
