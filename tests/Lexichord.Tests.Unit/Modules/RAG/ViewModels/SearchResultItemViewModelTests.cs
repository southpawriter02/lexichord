// =============================================================================
// File: SearchResultItemViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SearchResultItemViewModel.
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="SearchResultItemViewModel"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.6b")]
public class SearchResultItemViewModelTests
{
    // =========================================================================
    // Constructor Tests
    // =========================================================================

    [Fact]
    public void Constructor_NullHit_ThrowsArgumentNullException()
    {
        // Act & Assert
        FluentActions.Invoking(() => new SearchResultItemViewModel(null!))
            .Should().Throw<ArgumentNullException>()
            .And.ParamName.Should().Be("hit");
    }

    [Fact]
    public void Constructor_ValidHit_SetsProperties()
    {
        // Arrange
        var hit = CreateHit(score: 0.85f, filePath: "/docs/test.md", content: "Test content");

        // Act
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.Hit.Should().BeSameAs(hit);
        sut.Score.Should().Be(0.85f);
        sut.DocumentName.Should().Be("test.md");
    }

    [Fact]
    public void Constructor_NullQuery_SetsEmptyQuery()
    {
        // Arrange
        var hit = CreateHit();

        // Act
        var sut = new SearchResultItemViewModel(hit, query: null);

        // Assert
        sut.Query.Should().BeEmpty();
    }

    // =========================================================================
    // DocumentName Tests
    // =========================================================================

    [Theory]
    [InlineData("/path/to/document.md", "document.md")]
    [InlineData("/path/to/My File.txt", "My File.txt")]
    [InlineData("file.md", "file.md")]
    public void DocumentName_ReturnsFileBasename(string path, string expected)
    {
        // Arrange
        var hit = CreateHit(filePath: path);
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.DocumentName.Should().Be(expected);
    }

    [Fact]
    public void DocumentPath_ReturnsFullPath()
    {
        // Arrange
        var hit = CreateHit(filePath: "/path/to/document.md");
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.DocumentPath.Should().Be("/path/to/document.md");
    }

    // =========================================================================
    // Score Tests
    // =========================================================================

    [Theory]
    [InlineData(0.95f, 95)]
    [InlineData(0.85f, 85)]
    [InlineData(0.756f, 76)]  // Rounds to nearest
    [InlineData(0.754f, 75)]  // Rounds to nearest
    [InlineData(0.0f, 0)]
    [InlineData(1.0f, 100)]
    public void ScorePercent_CalculatesCorrectly(float score, int expected)
    {
        // Arrange
        var hit = CreateHit(score: score);
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.ScorePercent.Should().Be(expected);
    }

    // =========================================================================
    // ScoreColor Tests (v0.4.6b)
    // =========================================================================

    [Theory]
    [InlineData(1.0f, "HighRelevance")]
    [InlineData(0.95f, "HighRelevance")]
    [InlineData(0.90f, "HighRelevance")]
    [InlineData(0.899f, "MediumRelevance")]
    [InlineData(0.85f, "MediumRelevance")]
    [InlineData(0.80f, "MediumRelevance")]
    [InlineData(0.799f, "LowRelevance")]
    [InlineData(0.75f, "LowRelevance")]
    [InlineData(0.70f, "LowRelevance")]
    [InlineData(0.699f, "MinimalRelevance")]
    [InlineData(0.50f, "MinimalRelevance")]
    [InlineData(0.0f, "MinimalRelevance")]
    public void ScoreColor_ReturnsCorrectCategory(float score, string expectedColor)
    {
        // Arrange
        var hit = CreateHit(score: score);
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.ScoreColor.Should().Be(expectedColor);
    }

    // =========================================================================
    // QueryTerms Tests (v0.4.6b)
    // =========================================================================

    [Fact]
    public void QueryTerms_EmptyQuery_ReturnsEmptyList()
    {
        // Arrange
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, query: "");

        // Assert
        sut.QueryTerms.Should().BeEmpty();
    }

    [Fact]
    public void QueryTerms_WhitespaceQuery_ReturnsEmptyList()
    {
        // Arrange
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, query: "   ");

        // Assert
        sut.QueryTerms.Should().BeEmpty();
    }

    [Fact]
    public void QueryTerms_SimpleWords_ReturnsSeparateTerms()
    {
        // Arrange
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, query: "hello world");

        // Assert
        sut.QueryTerms.Should().HaveCount(2);
        sut.QueryTerms.Should().Contain("hello");
        sut.QueryTerms.Should().Contain("world");
    }

    [Fact]
    public void QueryTerms_QuotedPhrase_ReturnsSingleTerm()
    {
        // Arrange
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, query: "\"hello world\"");

        // Assert
        sut.QueryTerms.Should().ContainSingle()
            .Which.Should().Be("hello world");
    }

    [Fact]
    public void QueryTerms_MixedQuotedAndUnquoted_ReturnsAllTerms()
    {
        // Arrange
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, query: "foo \"bar baz\" qux");

        // Assert
        sut.QueryTerms.Should().HaveCount(3);
        sut.QueryTerms[0].Should().Be("foo");
        sut.QueryTerms[1].Should().Be("bar baz");
        sut.QueryTerms[2].Should().Be("qux");
    }

    // =========================================================================
    // Section Tests
    // =========================================================================

    [Fact]
    public void HasSectionHeading_WithHeading_ReturnsTrue()
    {
        // Arrange
        var hit = CreateHit(heading: "Introduction");
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.HasSectionHeading.Should().BeTrue();
        sut.SectionHeading.Should().Be("Introduction");
    }

    [Fact]
    public void HasSectionHeading_WithoutHeading_ReturnsFalse()
    {
        // Arrange
        var hit = CreateHit(heading: null);
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.HasSectionHeading.Should().BeFalse();
        sut.SectionHeading.Should().BeNull();
    }

    // =========================================================================
    // Offset Tests
    // =========================================================================

    [Fact]
    public void StartOffset_ReturnsChunkStartOffset()
    {
        // Arrange
        var hit = CreateHit(startOffset: 100, endOffset: 300);
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.StartOffset.Should().Be(100);
    }

    [Fact]
    public void EndOffset_ReturnsChunkEndOffset()
    {
        // Arrange
        var hit = CreateHit(startOffset: 100, endOffset: 300);
        var sut = new SearchResultItemViewModel(hit);

        // Assert
        sut.EndOffset.Should().Be(300);
    }

    // =========================================================================
    // Navigation Tests
    // =========================================================================

    [Fact]
    public void NavigateCommand_WithAction_CanExecute()
    {
        // Arrange
        var hit = CreateHit();
        SearchHit? navigatedHit = null;
        var sut = new SearchResultItemViewModel(hit, h => navigatedHit = h);

        // Assert
        sut.NavigateCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void NavigateCommand_WithoutAction_CannotExecute()
    {
        // Arrange
        var hit = CreateHit();
        var sut = new SearchResultItemViewModel(hit, navigateAction: null);

        // Assert
        sut.NavigateCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void NavigateCommand_Execute_InvokesAction()
    {
        // Arrange
        var hit = CreateHit();
        SearchHit? navigatedHit = null;
        var sut = new SearchResultItemViewModel(hit, h => navigatedHit = h);

        // Act
        sut.NavigateCommand.Execute(null);

        // Assert
        navigatedHit.Should().BeSameAs(hit);
    }

    // =========================================================================
    // Helper Methods
    // =========================================================================

    private static SearchHit CreateHit(
        float score = 0.85f,
        string name = "document.md",
        string filePath = "/docs/document.md",
        string content = "Sample content for testing.",
        string? heading = null,
        int startOffset = 0,
        int endOffset = 100)
    {
        var metadata = new ChunkMetadata(
            Index: 0,
            Heading: heading,
            Level: heading != null ? 1 : 0);

        var chunk = new TextChunk(
            Content: content,
            StartOffset: startOffset,
            EndOffset: endOffset,
            Metadata: metadata);

        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: filePath,
            Title: name,
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        return new SearchHit
        {
            Chunk = chunk,
            Document = document,
            Score = score
        };
    }
}
