// =============================================================================
// File: HighlightRendererTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for HighlightRenderer.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Rendering;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Rendering;

/// <summary>
/// Unit tests for <see cref="HighlightRenderer"/>.
/// </summary>
[Trait("Feature", "v0.5.6b")]
[Trait("Category", "Unit")]
public sealed class HighlightRendererTests
{
    private readonly Mock<ILogger<HighlightRenderer>> _loggerMock = new();
    private readonly HighlightRenderer _renderer;

    public HighlightRendererTests()
    {
        _renderer = new HighlightRenderer(_loggerMock.Object);
    }

    #region Render Tests

    [Fact]
    public void Render_NoHighlights_ReturnsSingleRun()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "This is plain text without highlights.",
            Highlights: Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(1);
        runs[0].Text.Should().Be("This is plain text without highlights.");
        runs[0].Style.IsBold.Should().BeFalse();
        runs[0].Style.IsItalic.Should().BeFalse();
    }

    [Fact]
    public void Render_SingleHighlight_ReturnsThreeRuns()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "The authentication flow requires tokens.",
            Highlights: new[] { new HighlightSpan(4, 14, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(3);
        runs[0].Text.Should().Be("The ");
        runs[0].Style.IsBold.Should().BeFalse();
        runs[1].Text.Should().Be("authentication");
        runs[1].Style.IsBold.Should().BeTrue();
        runs[2].Text.Should().Be(" flow requires tokens.");
        runs[2].Style.IsBold.Should().BeFalse();
    }

    [Fact]
    public void Render_ExactMatch_AppliesBoldStyle()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Test query term here.",
            Highlights: new[] { new HighlightSpan(5, 5, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        var highlightRun = runs.First(r => r.Text == "query");
        highlightRun.Style.IsBold.Should().BeTrue();
        highlightRun.Style.IsItalic.Should().BeFalse();
        highlightRun.Style.ForegroundColor.Should().Be(HighlightTheme.Light.ExactMatchForeground);
        highlightRun.Style.BackgroundColor.Should().Be(HighlightTheme.Light.ExactMatchBackground);
    }

    [Fact]
    public void Render_FuzzyMatch_AppliesItalicStyle()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Test fuzzy term here.",
            Highlights: new[] { new HighlightSpan(5, 5, HighlightType.FuzzyMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        var highlightRun = runs.First(r => r.Text == "fuzzy");
        highlightRun.Style.IsBold.Should().BeFalse();
        highlightRun.Style.IsItalic.Should().BeTrue();
        highlightRun.Style.ForegroundColor.Should().Be(HighlightTheme.Light.FuzzyMatchForeground);
        highlightRun.Style.BackgroundColor.Should().Be(HighlightTheme.Light.FuzzyMatchBackground);
    }

    [Fact]
    public void Render_MultipleHighlights_ReturnsSortedRuns()
    {
        // Arrange
        // Text: "First match and second match here."
        //        0     6          17     23
        var snippet = new Snippet(
            Text: "First match and second match here.",
            Highlights: new[]
            {
                new HighlightSpan(23, 5, HighlightType.QueryMatch), // "match" (second)
                new HighlightSpan(6, 5, HighlightType.QueryMatch),  // "match" (first)
            },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(5);
        runs[0].Text.Should().Be("First ");
        runs[1].Text.Should().Be("match");
        runs[1].Style.IsBold.Should().BeTrue();
        runs[2].Text.Should().Be(" and second ");
        runs[3].Text.Should().Be("match");
        runs[3].Style.IsBold.Should().BeTrue();
        runs[4].Text.Should().Be(" here.");
    }

    [Fact]
    public void Render_TruncatedStart_IncludesEllipsis()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "...continued text here.",
            Highlights: Array.Empty<HighlightSpan>(),
            StartOffset: 100,
            IsTruncatedStart: true,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCountGreaterOrEqualTo(2);
        runs[0].Text.Should().Be("...");
        runs[0].Style.ForegroundColor.Should().Be(HighlightTheme.Light.EllipsisColor);
    }

    [Fact]
    public void Render_TruncatedEnd_IncludesEllipsis()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Some text that continues",
            Highlights: Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: true);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCountGreaterOrEqualTo(2);
        runs[^1].Text.Should().Be("...");
        runs[^1].Style.ForegroundColor.Should().Be(HighlightTheme.Light.EllipsisColor);
    }

    [Fact]
    public void Render_BothTruncated_IncludesBothEllipses()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "middle text only",
            Highlights: Array.Empty<HighlightSpan>(),
            StartOffset: 50,
            IsTruncatedStart: true,
            IsTruncatedEnd: true);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(3); // ellipsis + text + ellipsis
        runs[0].Text.Should().Be("...");
        runs[1].Text.Should().Be("middle text only");
        runs[2].Text.Should().Be("...");
    }

    [Fact]
    public void Render_EmptyText_ReturnsEmptyList()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "",
            Highlights: Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().BeEmpty();
    }

    [Fact]
    public void Render_HighlightAtStart_ReturnsCorrectRuns()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Query at start of text.",
            Highlights: new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(2);
        runs[0].Text.Should().Be("Query");
        runs[0].Style.IsBold.Should().BeTrue();
        runs[1].Text.Should().Be(" at start of text.");
    }

    [Fact]
    public void Render_HighlightAtEnd_ReturnsCorrectRuns()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Text ending with match",
            Highlights: new[] { new HighlightSpan(17, 5, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(2);
        runs[0].Text.Should().Be("Text ending with ");
        runs[1].Text.Should().Be("match");
        runs[1].Style.IsBold.Should().BeTrue();
    }

    [Fact]
    public void Render_DarkTheme_UsesDarkColors()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Dark theme test",
            Highlights: new[] { new HighlightSpan(5, 5, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Dark);

        // Assert
        var highlightRun = runs.First(r => r.Text == "theme");
        highlightRun.Style.ForegroundColor.Should().Be(HighlightTheme.Dark.ExactMatchForeground);
        highlightRun.Style.BackgroundColor.Should().Be(HighlightTheme.Dark.ExactMatchBackground);
    }

    [Fact]
    public void Render_KeyPhraseHighlight_AppliesKeyPhraseStyle()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "This is a key phrase here.",
            Highlights: new[] { new HighlightSpan(10, 10, HighlightType.KeyPhrase) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        var highlightRun = runs.First(r => r.Text == "key phrase");
        highlightRun.Style.IsBold.Should().BeFalse();
        highlightRun.Style.IsItalic.Should().BeFalse();
        highlightRun.Style.ForegroundColor.Should().Be(HighlightTheme.Light.KeyPhraseForeground);
    }

    [Fact]
    public void Render_InvalidHighlight_SkipsAndLogs()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Short text",
            Highlights: new[] { new HighlightSpan(100, 5, HighlightType.QueryMatch) }, // Out of bounds
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var runs = _renderer.Render(snippet, HighlightTheme.Light);

        // Assert
        runs.Should().HaveCount(1);
        runs[0].Text.Should().Be("Short text");
        runs[0].Style.IsBold.Should().BeFalse(); // No highlight applied
    }

    #endregion

    #region ValidateHighlights Tests

    [Fact]
    public void ValidateHighlights_ValidSpans_ReturnsTrue()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Test validation here.",
            Highlights: new[]
            {
                new HighlightSpan(0, 4, HighlightType.QueryMatch),
                new HighlightSpan(5, 10, HighlightType.FuzzyMatch)
            },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var result = _renderer.ValidateHighlights(snippet);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateHighlights_OutOfBounds_ReturnsFalse()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Short",
            Highlights: new[] { new HighlightSpan(0, 100, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var result = _renderer.ValidateHighlights(snippet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHighlights_NegativeStart_ReturnsFalse()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Test",
            Highlights: new[] { new HighlightSpan(-1, 2, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var result = _renderer.ValidateHighlights(snippet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHighlights_ZeroLength_ReturnsFalse()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "Test",
            Highlights: new[] { new HighlightSpan(0, 0, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var result = _renderer.ValidateHighlights(snippet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHighlights_EmptyTextWithHighlights_ReturnsFalse()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "",
            Highlights: new[] { new HighlightSpan(0, 1, HighlightType.QueryMatch) },
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var result = _renderer.ValidateHighlights(snippet);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void ValidateHighlights_EmptyTextNoHighlights_ReturnsTrue()
    {
        // Arrange
        var snippet = new Snippet(
            Text: "",
            Highlights: Array.Empty<HighlightSpan>(),
            StartOffset: 0,
            IsTruncatedStart: false,
            IsTruncatedEnd: false);

        // Act
        var result = _renderer.ValidateHighlights(snippet);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new HighlightRenderer(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void Render_NullSnippet_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _renderer.Render(null!, HighlightTheme.Light);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snippet");
    }

    [Fact]
    public void Render_NullTheme_ThrowsArgumentNullException()
    {
        // Arrange
        var snippet = new Snippet("Test", Array.Empty<HighlightSpan>(), 0, false, false);

        // Act
        var act = () => _renderer.Render(snippet, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("theme");
    }

    [Fact]
    public void ValidateHighlights_NullSnippet_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _renderer.ValidateHighlights(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snippet");
    }

    #endregion
}
