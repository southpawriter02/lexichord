// =============================================================================
// File: GroupedResultsViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for GroupedResultsViewModel (v0.5.7b).
// =============================================================================
// LOGIC: Verifies ViewModel functionality:
//   - Constructor null-parameter validation.
//   - ExpandAll command expands all groups.
//   - CollapseAll command collapses all groups.
//   - ToggleGroup command toggles individual groups.
//   - ChangeSortMode command updates mode and re-groups.
//   - UpdateResults applies grouping.
//   - Clear resets state.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="GroupedResultsViewModel"/>.
/// Verifies constructor validation and command behavior.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7b")]
public class GroupedResultsViewModelTests
{
    private readonly Mock<IResultGroupingService> _groupingServiceMock;
    private readonly Mock<ILogger<GroupedResultsViewModel>> _loggerMock;

    public GroupedResultsViewModelTests()
    {
        _groupingServiceMock = new Mock<IResultGroupingService>();
        _loggerMock = new Mock<ILogger<GroupedResultsViewModel>>();
    }

    /// <summary>
    /// Creates a <see cref="GroupedResultsViewModel"/> using the test mocks.
    /// </summary>
    private GroupedResultsViewModel CreateViewModel() =>
        new(_groupingServiceMock.Object, _loggerMock.Object);

    /// <summary>
    /// Creates a test <see cref="SearchHit"/> with the given document path.
    /// </summary>
    private static SearchHit CreateHit(string documentPath, float score = 0.8f)
    {
        var document = new Document(
            Id: Guid.NewGuid(),
            ProjectId: Guid.NewGuid(),
            FilePath: documentPath,
            Title: Path.GetFileNameWithoutExtension(documentPath),
            Hash: "test-hash",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        var chunk = new TextChunk(
            "Test content",
            StartOffset: 0,
            EndOffset: 12,
            new ChunkMetadata(Index: 0));

        return new SearchHit
        {
            Document = document,
            Chunk = chunk,
            Score = score
        };
    }

    /// <summary>
    /// Creates a test <see cref="GroupedSearchResults"/> with two groups.
    /// </summary>
    private static GroupedSearchResults CreateGroupedResults(bool expanded = true) =>
        new(
            Groups: new[]
            {
                new DocumentResultGroup(
                    "/docs/file1.md",
                    "file1",
                    MatchCount: 2,
                    MaxScore: 0.9f,
                    Hits: new[] { CreateHit("/docs/file1.md", 0.9f), CreateHit("/docs/file1.md", 0.8f) },
                    IsExpanded: expanded),
                new DocumentResultGroup(
                    "/docs/file2.md",
                    "file2",
                    MatchCount: 1,
                    MaxScore: 0.7f,
                    Hits: new[] { CreateHit("/docs/file2.md", 0.7f) },
                    IsExpanded: expanded)
            },
            TotalHits: 3,
            TotalDocuments: 2,
            Query: "test",
            SearchDuration: TimeSpan.FromMilliseconds(50));

    #region Constructor Tests

    [Fact]
    public void Constructor_NullGroupingService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new GroupedResultsViewModel(null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("groupingService");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new GroupedResultsViewModel(_groupingServiceMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateViewModel();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    [Fact]
    public void Constructor_InitializesDefaults()
    {
        // Act
        var viewModel = CreateViewModel();

        // Assert
        viewModel.Results.Should().BeNull();
        viewModel.SortMode.Should().Be(ResultSortMode.ByRelevance);
        viewModel.AllExpanded.Should().BeTrue();
    }

    #endregion

    #region ExpandAll Tests

    [Fact]
    public void ExpandAllCommand_WithResults_ExpandsAllGroups()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = CreateGroupedResults(expanded: false);

        // Act
        viewModel.ExpandAllCommand.Execute(null);

        // Assert
        viewModel.Results.Groups.Should().AllSatisfy(g => g.IsExpanded.Should().BeTrue());
        viewModel.AllExpanded.Should().BeTrue();
    }

    [Fact]
    public void ExpandAllCommand_WithNullResults_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = () => viewModel.ExpandAllCommand.Execute(null);

        act.Should().NotThrow();
    }

    #endregion

    #region CollapseAll Tests

    [Fact]
    public void CollapseAllCommand_WithResults_CollapsesAllGroups()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = CreateGroupedResults(expanded: true);

        // Act
        viewModel.CollapseAllCommand.Execute(null);

        // Assert
        viewModel.Results.Groups.Should().AllSatisfy(g => g.IsExpanded.Should().BeFalse());
        viewModel.AllExpanded.Should().BeFalse();
    }

    [Fact]
    public void CollapseAllCommand_WithNullResults_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = () => viewModel.CollapseAllCommand.Execute(null);

        act.Should().NotThrow();
    }

    #endregion

    #region ToggleGroup Tests

    [Fact]
    public void ToggleGroupCommand_ExpandedGroup_CollapsesIt()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = CreateGroupedResults(expanded: true);
        var groupToToggle = viewModel.Results.Groups[0];

        // Act
        viewModel.ToggleGroupCommand.Execute(groupToToggle);

        // Assert
        viewModel.Results.Groups[0].IsExpanded.Should().BeFalse();
        viewModel.Results.Groups[1].IsExpanded.Should().BeTrue();
    }

    [Fact]
    public void ToggleGroupCommand_CollapsedGroup_ExpandsIt()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = CreateGroupedResults(expanded: false);
        var groupToToggle = viewModel.Results.Groups[0];

        // Act
        viewModel.ToggleGroupCommand.Execute(groupToToggle);

        // Assert
        viewModel.Results.Groups[0].IsExpanded.Should().BeTrue();
        viewModel.Results.Groups[1].IsExpanded.Should().BeFalse();
    }

    [Fact]
    public void ToggleGroupCommand_NullGroup_DoesNotThrow()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = CreateGroupedResults();

        // Act & Assert
        var act = () => viewModel.ToggleGroupCommand.Execute(null);

        act.Should().NotThrow();
    }

    #endregion

    #region ChangeSortMode Tests

    [Fact]
    public void ChangeSortModeCommand_UpdatesSortMode()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var searchResult = new SearchResult
        {
            Hits = new[] { CreateHit("/docs/file.md") },
            Query = "test"
        };

        _groupingServiceMock
            .Setup(x => x.GroupByDocument(It.IsAny<SearchResult>(), It.IsAny<ResultGroupingOptions>()))
            .Returns(CreateGroupedResults());

        viewModel.UpdateResults(searchResult);

        // Act
        viewModel.ChangeSortModeCommand.Execute(ResultSortMode.ByMatchCount);

        // Assert
        viewModel.SortMode.Should().Be(ResultSortMode.ByMatchCount);
    }

    [Fact]
    public void ChangeSortModeCommand_SameMode_DoesNotRegroup()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var searchResult = new SearchResult
        {
            Hits = new[] { CreateHit("/docs/file.md") },
            Query = "test"
        };

        _groupingServiceMock
            .Setup(x => x.GroupByDocument(It.IsAny<SearchResult>(), It.IsAny<ResultGroupingOptions>()))
            .Returns(CreateGroupedResults());

        viewModel.UpdateResults(searchResult);
        _groupingServiceMock.Invocations.Clear();

        // Act
        viewModel.ChangeSortModeCommand.Execute(ResultSortMode.ByRelevance);

        // Assert
        _groupingServiceMock.Verify(
            x => x.GroupByDocument(It.IsAny<SearchResult>(), It.IsAny<ResultGroupingOptions>()),
            Times.Never);
    }

    [Theory]
    [InlineData(ResultSortMode.ByRelevance)]
    [InlineData(ResultSortMode.ByDocumentPath)]
    [InlineData(ResultSortMode.ByMatchCount)]
    public void ChangeSortModeCommand_AllModes_UpdatesSortMode(ResultSortMode mode)
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.SortMode = mode == ResultSortMode.ByRelevance
            ? ResultSortMode.ByMatchCount
            : ResultSortMode.ByRelevance;

        // Act
        viewModel.ChangeSortModeCommand.Execute(mode);

        // Assert
        viewModel.SortMode.Should().Be(mode);
    }

    #endregion

    #region UpdateResults Tests

    [Fact]
    public void UpdateResults_CallsGroupingService()
    {
        // Arrange
        var viewModel = CreateViewModel();
        var searchResult = new SearchResult
        {
            Hits = new[] { CreateHit("/docs/file.md") },
            Query = "test"
        };

        _groupingServiceMock
            .Setup(x => x.GroupByDocument(It.IsAny<SearchResult>(), It.IsAny<ResultGroupingOptions>()))
            .Returns(CreateGroupedResults());

        // Act
        viewModel.UpdateResults(searchResult);

        // Assert
        _groupingServiceMock.Verify(
            x => x.GroupByDocument(searchResult, It.IsAny<ResultGroupingOptions>()),
            Times.Once);
        viewModel.Results.Should().NotBeNull();
    }

    [Fact]
    public void UpdateResults_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        var act = () => viewModel.UpdateResults(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ResetsState()
    {
        // Arrange
        var viewModel = CreateViewModel();

        _groupingServiceMock
            .Setup(x => x.GroupByDocument(It.IsAny<SearchResult>(), It.IsAny<ResultGroupingOptions>()))
            .Returns(CreateGroupedResults());

        viewModel.UpdateResults(new SearchResult
        {
            Hits = new[] { CreateHit("/docs/file.md") },
            Query = "test"
        });
        viewModel.AllExpanded = false;

        // Act
        viewModel.Clear();

        // Assert
        viewModel.Results.Should().BeNull();
        viewModel.AllExpanded.Should().BeTrue();
    }

    #endregion

    #region HasResults Tests

    [Fact]
    public void HasResults_WithResults_ReturnsTrue()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = CreateGroupedResults();

        // Act & Assert
        viewModel.HasResults.Should().BeTrue();
    }

    [Fact]
    public void HasResults_WithNullResults_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();

        // Act & Assert
        viewModel.HasResults.Should().BeFalse();
    }

    [Fact]
    public void HasResults_WithEmptyGroups_ReturnsFalse()
    {
        // Arrange
        var viewModel = CreateViewModel();
        viewModel.Results = GroupedSearchResults.Empty("test");

        // Act & Assert
        viewModel.HasResults.Should().BeFalse();
    }

    #endregion
}
