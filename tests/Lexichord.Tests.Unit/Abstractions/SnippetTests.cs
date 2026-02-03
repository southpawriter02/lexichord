// =============================================================================
// File: SnippetTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for Snippet record (v0.5.6a).
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Unit tests for <see cref="Snippet"/> record.
/// Verifies computed properties and factory methods.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6a")]
public class SnippetTests
{
    #region Empty Tests

    [Fact]
    public void Empty_HasNoContent()
    {
        // Act
        var snippet = Snippet.Empty;

        // Assert
        snippet.Text.Should().BeEmpty();
        snippet.Length.Should().Be(0);
    }

    [Fact]
    public void Empty_HasNoHighlights()
    {
        // Act
        var snippet = Snippet.Empty;

        // Assert
        snippet.Highlights.Should().BeEmpty();
        snippet.HasHighlights.Should().BeFalse();
    }

    [Fact]
    public void Empty_IsNotTruncated()
    {
        // Act
        var snippet = Snippet.Empty;

        // Assert
        snippet.IsTruncated.Should().BeFalse();
        snippet.IsTruncatedStart.Should().BeFalse();
        snippet.IsTruncatedEnd.Should().BeFalse();
    }

    #endregion

    #region FromPlainText Tests

    [Fact]
    public void FromPlainText_CreatesSnippetWithText()
    {
        // Act
        var snippet = Snippet.FromPlainText("Test content");

        // Assert
        snippet.Text.Should().Be("Test content");
        snippet.Length.Should().Be(12);
    }

    [Fact]
    public void FromPlainText_HasNoHighlights()
    {
        // Act
        var snippet = Snippet.FromPlainText("Test content");

        // Assert
        snippet.Highlights.Should().BeEmpty();
        snippet.HasHighlights.Should().BeFalse();
    }

    [Fact]
    public void FromPlainText_IsNotTruncated()
    {
        // Act
        var snippet = Snippet.FromPlainText("Test content");

        // Assert
        snippet.IsTruncated.Should().BeFalse();
    }

    [Fact]
    public void FromPlainText_HasZeroStartOffset()
    {
        // Act
        var snippet = Snippet.FromPlainText("Test content");

        // Assert
        snippet.StartOffset.Should().Be(0);
    }

    #endregion

    #region Computed Properties Tests

    [Fact]
    public void Length_EqualsTextLength()
    {
        // Arrange
        var snippet = new Snippet(
            "Hello World",
            Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Assert
        snippet.Length.Should().Be(11);
    }

    [Fact]
    public void HasHighlights_TrueWhenHighlightsExist()
    {
        // Arrange
        var highlights = new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch) };
        var snippet = new Snippet(
            "Hello World",
            highlights,
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Assert
        snippet.HasHighlights.Should().BeTrue();
    }

    [Fact]
    public void HasHighlights_FalseWhenEmpty()
    {
        // Arrange
        var snippet = new Snippet(
            "Hello World",
            Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Assert
        snippet.HasHighlights.Should().BeFalse();
    }

    [Fact]
    public void IsTruncated_TrueWhenStartTruncated()
    {
        // Arrange
        var snippet = new Snippet(
            "...Hello World",
            Array.Empty<HighlightSpan>(),
            StartOffset: 10,
            IsTruncatedStart: true,
            IsTruncatedEnd: false);

        // Assert
        snippet.IsTruncated.Should().BeTrue();
    }

    [Fact]
    public void IsTruncated_TrueWhenEndTruncated()
    {
        // Arrange
        var snippet = new Snippet(
            "Hello World...",
            Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: true);

        // Assert
        snippet.IsTruncated.Should().BeTrue();
    }

    [Fact]
    public void IsTruncated_TrueWhenBothTruncated()
    {
        // Arrange
        var snippet = new Snippet(
            "...Hello World...",
            Array.Empty<HighlightSpan>(),
            StartOffset: 10,
            IsTruncatedStart: true,
            IsTruncatedEnd: true);

        // Assert
        snippet.IsTruncated.Should().BeTrue();
    }

    [Fact]
    public void IsTruncated_FalseWhenNeitherTruncated()
    {
        // Arrange
        var snippet = new Snippet(
            "Hello World",
            Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Assert
        snippet.IsTruncated.Should().BeFalse();
    }

    #endregion
}
