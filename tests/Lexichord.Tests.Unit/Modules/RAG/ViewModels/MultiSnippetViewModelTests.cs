// =============================================================================
// File: MultiSnippetViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for MultiSnippetViewModel (v0.5.6d).
// =============================================================================
// VERSION: v0.5.6d (Multi-Snippet Results)
// =============================================================================

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.ViewModels;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="MultiSnippetViewModel"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6d")]
public class MultiSnippetViewModelTests
{
    private readonly Mock<ISnippetService> _snippetServiceMock;
    private readonly TextChunk _testChunk;
    private readonly MultiSnippetViewModel _sut;

    public MultiSnippetViewModelTests()
    {
        _snippetServiceMock = new Mock<ISnippetService>();
        _testChunk = new TextChunk(
            "Test content for multi-snippet extraction",
            0,
            42,
            new ChunkMetadata(Index: 0));
        _sut = new MultiSnippetViewModel(
            _snippetServiceMock.Object,
            _testChunk,
            "query",
            SnippetOptions.Default);
    }

    #region Initialize Tests

    [Fact]
    public void Initialize_WithMultipleSnippets_SetsPrimaryAndAdditional()
    {
        var snippets = new List<Snippet>
        {
            new("Primary", new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch) }, 0, false, false),
            new("Second", new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch) }, 100, false, false),
            new("Third", new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch) }, 200, false, false)
        };
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(snippets);

        _sut.Initialize();

        _sut.PrimarySnippet.Text.Should().Be("Primary");
        _sut.AdditionalSnippets.Should().HaveCount(2);
        _sut.HasAdditionalSnippets.Should().BeTrue();
    }

    [Fact]
    public void Initialize_WithSingleSnippet_HasNoAdditional()
    {
        var snippets = new List<Snippet>
        {
            new("Only one", Array.Empty<HighlightSpan>(), 0, false, false)
        };
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(snippets);

        _sut.Initialize();

        _sut.PrimarySnippet.Text.Should().Be("Only one");
        _sut.AdditionalSnippets.Should().BeEmpty();
        _sut.HasAdditionalSnippets.Should().BeFalse();
    }

    [Fact]
    public void Initialize_EmptySnippets_HasNoAdditional()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(Array.Empty<Snippet>());

        _sut.Initialize();

        _sut.PrimarySnippet.Should().Be(Snippet.Empty);
        _sut.AdditionalSnippets.Should().BeEmpty();
        _sut.HasAdditionalSnippets.Should().BeFalse();
    }

    [Fact]
    public void Initialize_CalculatesTotalMatchCount()
    {
        var snippets = new List<Snippet>
        {
            new("Primary", new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch), new HighlightSpan(10, 5, HighlightType.FuzzyMatch) }, 0, false, false),
            new("Second", new[] { new HighlightSpan(0, 5, HighlightType.QueryMatch) }, 100, false, false)
        };
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(snippets);

        _sut.Initialize();

        _sut.TotalMatchCount.Should().Be(3);
    }

    #endregion

    #region ToggleExpanded Tests

    [Fact]
    public void ToggleExpanded_TogglesState()
    {
        _sut.IsExpanded.Should().BeFalse();

        _sut.ToggleExpandedCommand.Execute(null);

        _sut.IsExpanded.Should().BeTrue();

        _sut.ToggleExpandedCommand.Execute(null);

        _sut.IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void ToggleExpanded_UpdatesExpanderText()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(new List<Snippet>
            {
                new("Primary", Array.Empty<HighlightSpan>(), 0, false, false),
                new("Second", Array.Empty<HighlightSpan>(), 100, false, false)
            });
        _sut.Initialize();

        _sut.ExpanderText.Should().Contain("Show 1 more match");

        _sut.ToggleExpandedCommand.Execute(null);

        _sut.ExpanderText.Should().Contain("Hide");
    }

    #endregion

    #region Collapse Tests

    [Fact]
    public void Collapse_SetsExpandedToFalse()
    {
        _sut.ToggleExpandedCommand.Execute(null);
        _sut.IsExpanded.Should().BeTrue();

        _sut.CollapseCommand.Execute(null);

        _sut.IsExpanded.Should().BeFalse();
    }

    #endregion

    #region ExpanderText Tests

    [Fact]
    public void ExpanderText_ReflectsState()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(new List<Snippet>
            {
                new("Primary", Array.Empty<HighlightSpan>(), 0, false, false),
                new("Second", Array.Empty<HighlightSpan>(), 100, false, false)
            });
        _sut.Initialize();

        _sut.ExpanderText.Should().Contain("1 more match");

        _sut.ToggleExpandedCommand.Execute(null);

        _sut.ExpanderText.Should().Contain("Hide");
    }

    [Fact]
    public void ExpanderText_PluralForMultiple()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(new List<Snippet>
            {
                new("Primary", Array.Empty<HighlightSpan>(), 0, false, false),
                new("Second", Array.Empty<HighlightSpan>(), 100, false, false),
                new("Third", Array.Empty<HighlightSpan>(), 200, false, false)
            });
        _sut.Initialize();

        _sut.ExpanderText.Should().Contain("2 more matches");
    }

    [Fact]
    public void ExpanderText_SingularForOne()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(new List<Snippet>
            {
                new("Primary", Array.Empty<HighlightSpan>(), 0, false, false),
                new("Second", Array.Empty<HighlightSpan>(), 100, false, false)
            });
        _sut.Initialize();

        _sut.ExpanderText.Should().Contain("1 more match");
        _sut.ExpanderText.Should().NotContain("matches");
    }

    #endregion

    #region HiddenSnippetCount Tests

    [Fact]
    public void HiddenSnippetCount_MatchesAdditionalSnippetsCount()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(new List<Snippet>
            {
                new("Primary", Array.Empty<HighlightSpan>(), 0, false, false),
                new("Second", Array.Empty<HighlightSpan>(), 100, false, false),
                new("Third", Array.Empty<HighlightSpan>(), 200, false, false)
            });
        _sut.Initialize();

        _sut.HiddenSnippetCount.Should().Be(2);
    }

    [Fact]
    public void HiddenSnippetCount_ZeroForSingleSnippet()
    {
        _snippetServiceMock
            .Setup(x => x.ExtractMultipleSnippets(It.IsAny<TextChunk>(), It.IsAny<string>(), It.IsAny<SnippetOptions>(), 3))
            .Returns(new List<Snippet>
            {
                new("Only one", Array.Empty<HighlightSpan>(), 0, false, false)
            });
        _sut.Initialize();

        _sut.HiddenSnippetCount.Should().Be(0);
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_NullSnippetService_ThrowsArgumentNullException()
    {
        var act = () => new MultiSnippetViewModel(
            null!,
            _testChunk,
            "query",
            SnippetOptions.Default);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("snippetService");
    }

    [Fact]
    public void Constructor_NullChunk_ThrowsArgumentNullException()
    {
        var act = () => new MultiSnippetViewModel(
            _snippetServiceMock.Object,
            null!,
            "query",
            SnippetOptions.Default);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("chunk");
    }

    [Fact]
    public void Constructor_NullQuery_ThrowsArgumentNullException()
    {
        var act = () => new MultiSnippetViewModel(
            _snippetServiceMock.Object,
            _testChunk,
            null!,
            SnippetOptions.Default);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("query");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new MultiSnippetViewModel(
            _snippetServiceMock.Object,
            _testChunk,
            "query",
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    #endregion
}
