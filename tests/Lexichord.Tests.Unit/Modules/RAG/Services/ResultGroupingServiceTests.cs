// =============================================================================
// File: ResultGroupingServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ResultGroupingService (v0.5.7b).
// =============================================================================
// LOGIC: Verifies result grouping functionality:
//   - Constructor null-parameter validation.
//   - Empty hits handling.
//   - Single and multiple document grouping.
//   - Sort mode ordering (Relevance, DocumentPath, MatchCount).
//   - MaxHitsPerGroup limiting.
//   - CollapseByDefault expansion state.
//   - Hits within group ordered by score.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="ResultGroupingService"/>.
/// Verifies constructor validation, grouping logic, sort modes, and edge cases.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7b")]
public class ResultGroupingServiceTests
{
    private readonly Mock<ILogger<ResultGroupingService>> _loggerMock;

    public ResultGroupingServiceTests()
    {
        _loggerMock = new Mock<ILogger<ResultGroupingService>>();
    }

    /// <summary>
    /// Creates a <see cref="ResultGroupingService"/> using the test mocks.
    /// </summary>
    private ResultGroupingService CreateService() =>
        new(_loggerMock.Object);

    /// <summary>
    /// Creates a test <see cref="SearchHit"/> with the given document path and score.
    /// </summary>
    private static SearchHit CreateHit(string documentPath, float score, string content = "Test content")
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
            content,
            StartOffset: 0,
            EndOffset: content.Length,
            new ChunkMetadata(Index: 0));

        return new SearchHit
        {
            Document = document,
            Chunk = chunk,
            Score = score
        };
    }

    /// <summary>
    /// Creates a <see cref="SearchResult"/> with the given hits.
    /// </summary>
    private static SearchResult CreateSearchResult(params SearchHit[] hits) =>
        new()
        {
            Hits = hits.ToList(),
            Query = "test query",
            Duration = TimeSpan.FromMilliseconds(50)
        };

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResultGroupingService(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateService();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region GroupByDocument Tests

    [Fact]
    public void GroupByDocument_NullResult_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var act = () => service.GroupByDocument(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("result");
    }

    [Fact]
    public void GroupByDocument_EmptyHits_ReturnsEmptyGroups()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult();

        // Act
        var grouped = service.GroupByDocument(result);

        // Assert
        grouped.Groups.Should().BeEmpty();
        grouped.TotalHits.Should().Be(0);
        grouped.TotalDocuments.Should().Be(0);
        grouped.Query.Should().Be("test query");
    }

    [Fact]
    public void GroupByDocument_SingleDocument_CreatesOneGroup()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/file1.md", 0.9f),
            CreateHit("/docs/file1.md", 0.8f),
            CreateHit("/docs/file1.md", 0.7f));

        // Act
        var grouped = service.GroupByDocument(result);

        // Assert
        grouped.Groups.Should().HaveCount(1);
        grouped.TotalHits.Should().Be(3);
        grouped.TotalDocuments.Should().Be(1);

        var group = grouped.Groups[0];
        group.DocumentPath.Should().Be("/docs/file1.md");
        group.DocumentTitle.Should().Be("file1");
        group.MatchCount.Should().Be(3);
        group.MaxScore.Should().Be(0.9f);
        group.Hits.Should().HaveCount(3);
    }

    [Fact]
    public void GroupByDocument_MultipleDocuments_GroupsByPath()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/file1.md", 0.9f),
            CreateHit("/docs/file2.md", 0.85f),
            CreateHit("/docs/file1.md", 0.7f),
            CreateHit("/docs/file3.md", 0.6f),
            CreateHit("/docs/file2.md", 0.5f));

        // Act
        var grouped = service.GroupByDocument(result);

        // Assert
        grouped.Groups.Should().HaveCount(3);
        grouped.TotalHits.Should().Be(5);
        grouped.TotalDocuments.Should().Be(3);
    }

    [Fact]
    public void GroupByDocument_SortMode_ByRelevance_OrdersByMaxScoreDescending()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/low.md", 0.5f),
            CreateHit("/docs/high.md", 0.9f),
            CreateHit("/docs/mid.md", 0.7f));

        var options = new ResultGroupingOptions(SortMode: ResultSortMode.ByRelevance);

        // Act
        var grouped = service.GroupByDocument(result, options);

        // Assert
        grouped.Groups.Should().HaveCount(3);
        grouped.Groups[0].DocumentPath.Should().Be("/docs/high.md");
        grouped.Groups[1].DocumentPath.Should().Be("/docs/mid.md");
        grouped.Groups[2].DocumentPath.Should().Be("/docs/low.md");
    }

    [Fact]
    public void GroupByDocument_SortMode_ByDocumentPath_OrdersAlphabetically()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/charlie.md", 0.9f),
            CreateHit("/docs/alpha.md", 0.7f),
            CreateHit("/docs/bravo.md", 0.5f));

        var options = new ResultGroupingOptions(SortMode: ResultSortMode.ByDocumentPath);

        // Act
        var grouped = service.GroupByDocument(result, options);

        // Assert
        grouped.Groups.Should().HaveCount(3);
        grouped.Groups[0].DocumentPath.Should().Be("/docs/alpha.md");
        grouped.Groups[1].DocumentPath.Should().Be("/docs/bravo.md");
        grouped.Groups[2].DocumentPath.Should().Be("/docs/charlie.md");
    }

    [Fact]
    public void GroupByDocument_SortMode_ByMatchCount_OrdersByCountDescending()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/few.md", 0.9f),
            CreateHit("/docs/many.md", 0.8f),
            CreateHit("/docs/many.md", 0.7f),
            CreateHit("/docs/many.md", 0.6f),
            CreateHit("/docs/some.md", 0.5f),
            CreateHit("/docs/some.md", 0.4f));

        var options = new ResultGroupingOptions(SortMode: ResultSortMode.ByMatchCount);

        // Act
        var grouped = service.GroupByDocument(result, options);

        // Assert
        grouped.Groups.Should().HaveCount(3);
        grouped.Groups[0].DocumentPath.Should().Be("/docs/many.md");
        grouped.Groups[0].MatchCount.Should().Be(3);
        grouped.Groups[1].DocumentPath.Should().Be("/docs/some.md");
        grouped.Groups[1].MatchCount.Should().Be(2);
        grouped.Groups[2].DocumentPath.Should().Be("/docs/few.md");
        grouped.Groups[2].MatchCount.Should().Be(1);
    }

    [Fact]
    public void GroupByDocument_MaxHitsPerGroup_LimitsHits()
    {
        // Arrange
        var service = CreateService();
        var hits = Enumerable.Range(0, 15)
            .Select(i => CreateHit("/docs/file.md", 0.9f - (i * 0.01f)))
            .ToArray();
        var result = CreateSearchResult(hits);

        var options = new ResultGroupingOptions(MaxHitsPerGroup: 5);

        // Act
        var grouped = service.GroupByDocument(result, options);

        // Assert
        grouped.Groups.Should().HaveCount(1);
        var group = grouped.Groups[0];
        group.Hits.Should().HaveCount(5);
        group.MatchCount.Should().Be(15);
        group.HasMoreHits.Should().BeTrue();
        group.HiddenHitCount.Should().Be(10);
    }

    [Fact]
    public void GroupByDocument_CollapseByDefault_SetsExpansionState()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/file1.md", 0.9f),
            CreateHit("/docs/file2.md", 0.8f));

        var collapsedOptions = new ResultGroupingOptions(CollapseByDefault: true);
        var expandedOptions = new ResultGroupingOptions(CollapseByDefault: false);

        // Act
        var collapsedGrouped = service.GroupByDocument(result, collapsedOptions);
        var expandedGrouped = service.GroupByDocument(result, expandedOptions);

        // Assert
        collapsedGrouped.Groups.Should().AllSatisfy(g => g.IsExpanded.Should().BeFalse());
        expandedGrouped.Groups.Should().AllSatisfy(g => g.IsExpanded.Should().BeTrue());
    }

    [Fact]
    public void GroupByDocument_HitsWithinGroup_OrderedByScoreDescending()
    {
        // Arrange
        var service = CreateService();
        var result = CreateSearchResult(
            CreateHit("/docs/file.md", 0.3f),
            CreateHit("/docs/file.md", 0.9f),
            CreateHit("/docs/file.md", 0.6f),
            CreateHit("/docs/file.md", 0.1f));

        // Act
        var grouped = service.GroupByDocument(result);

        // Assert
        var group = grouped.Groups[0];
        group.Hits.Should().BeInDescendingOrder(h => h.Score);
        group.Hits[0].Score.Should().Be(0.9f);
        group.Hits[3].Score.Should().Be(0.1f);
    }

    [Fact]
    public void GroupByDocument_PreservesQueryAndDuration()
    {
        // Arrange
        var service = CreateService();
        var result = new SearchResult
        {
            Hits = new[] { CreateHit("/docs/file.md", 0.9f) },
            Query = "specific query",
            Duration = TimeSpan.FromMilliseconds(123)
        };

        // Act
        var grouped = service.GroupByDocument(result);

        // Assert
        grouped.Query.Should().Be("specific query");
        grouped.SearchDuration.Should().Be(TimeSpan.FromMilliseconds(123));
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void Service_ImplementsIResultGroupingService()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<IResultGroupingService>(
            because: "ResultGroupingService implements the IResultGroupingService interface");
    }

    #endregion
}
