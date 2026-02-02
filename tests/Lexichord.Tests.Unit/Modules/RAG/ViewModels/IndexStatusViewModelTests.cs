// =============================================================================
// File: IndexStatusViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexStatusViewModel.
// Version: v0.4.7a
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="IndexStatusViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover loading, filtering, refresh, and property change notifications.
/// </remarks>
public class IndexStatusViewModelTests
{
    private readonly Mock<IIndexStatusService> _statusServiceMock;
    private readonly Mock<ILogger<IndexStatusViewModel>> _loggerMock;
    private readonly IndexStatusViewModel _sut;

    public IndexStatusViewModelTests()
    {
        _statusServiceMock = new Mock<IIndexStatusService>();
        _loggerMock = new Mock<ILogger<IndexStatusViewModel>>();

        _sut = new IndexStatusViewModel(
            _statusServiceMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullStatusService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexStatusViewModel(
                null!,
                _loggerMock.Object));

        Assert.Equal("statusService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new IndexStatusViewModel(
                _statusServiceMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_InitializesWithDefaults()
    {
        Assert.False(_sut.IsLoading);
        Assert.Null(_sut.Statistics);
        Assert.Empty(_sut.FilterText);
        Assert.Null(_sut.StatusFilter);
        Assert.Empty(_sut.Documents);
        Assert.Empty(_sut.FilteredDocuments);
    }

    #endregion

    #region LoadAsync Tests

    [Fact]
    public async Task LoadAsync_SetsIsLoadingDuringLoad()
    {
        // Arrange
        var loadingStates = new List<bool>();
        _sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IndexStatusViewModel.IsLoading))
                loadingStates.Add(_sut.IsLoading);
        };

        _statusServiceMock.Setup(s => s.GetAllDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IndexedDocumentInfo>());
        _statusServiceMock.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmptyStatistics());

        // Act
        await _sut.LoadAsync();

        // Assert - should have set to true then false
        Assert.Contains(true, loadingStates);
        Assert.False(loadingStates.Last());
    }

    [Fact]
    public async Task LoadAsync_PopulatesDocuments()
    {
        // Arrange
        var documents = new List<IndexedDocumentInfo>
        {
            CreateTestDocumentInfo("file1.txt", IndexingStatus.Indexed),
            CreateTestDocumentInfo("file2.txt", IndexingStatus.Pending)
        };

        _statusServiceMock.Setup(s => s.GetAllDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents);
        _statusServiceMock.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmptyStatistics());

        // Act
        await _sut.LoadAsync();

        // Assert
        Assert.Equal(2, _sut.Documents.Count);
        Assert.Contains(_sut.Documents, d => d.FileName == "file1.txt");
        Assert.Contains(_sut.Documents, d => d.FileName == "file2.txt");
    }

    [Fact]
    public async Task LoadAsync_PopulatesStatistics()
    {
        // Arrange
        var stats = new IndexStatistics
        {
            DocumentCount = 10,
            ChunkCount = 100,
            StorageSizeBytes = 50000,
            StatusCounts = new Dictionary<IndexingStatus, int>
            {
                { IndexingStatus.Indexed, 8 },
                { IndexingStatus.Pending, 2 }
            }
        };

        _statusServiceMock.Setup(s => s.GetAllDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IndexedDocumentInfo>());
        _statusServiceMock.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);

        // Act
        await _sut.LoadAsync();

        // Assert
        Assert.NotNull(_sut.Statistics);
        Assert.Equal(10, _sut.Statistics.DocumentCount);
        Assert.Equal(100, _sut.Statistics.ChunkCount);
    }

    [Fact]
    public async Task LoadAsync_CanBeCalledMultipleTimes()
    {
        // Arrange
        var loadCount = 0;
        _statusServiceMock.Setup(s => s.GetAllDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                loadCount++;
                return new List<IndexedDocumentInfo>();
            });
        _statusServiceMock.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmptyStatistics());

        // Act - call load twice sequentially
        await _sut.LoadAsync();
        await _sut.LoadAsync();

        // Assert - both calls should complete
        Assert.Equal(2, loadCount);
    }

    #endregion

    #region Filtering Tests

    [Fact]
    public async Task FilteredDocuments_WithNoFilter_ReturnsAll()
    {
        // Arrange
        await SetupWithDocuments(
            CreateTestDocumentInfo("a.txt", IndexingStatus.Indexed),
            CreateTestDocumentInfo("b.txt", IndexingStatus.Pending));

        // Act
        var filtered = _sut.FilteredDocuments;

        // Assert
        Assert.Equal(2, filtered.Count);
    }

    [Fact]
    public async Task FilteredDocuments_WithTextFilter_FiltersMatchingFiles()
    {
        // Arrange
        await SetupWithDocuments(
            CreateTestDocumentInfo("apple.txt", IndexingStatus.Indexed),
            CreateTestDocumentInfo("banana.txt", IndexingStatus.Indexed));

        // Act
        _sut.FilterText = "apple";
        var filtered = _sut.FilteredDocuments;

        // Assert
        Assert.Single(filtered);
        Assert.Equal("apple.txt", filtered[0].FileName);
    }

    [Fact]
    public async Task FilteredDocuments_WithStatusFilter_FiltersMatchingStatus()
    {
        // Arrange
        await SetupWithDocuments(
            CreateTestDocumentInfo("a.txt", IndexingStatus.Indexed),
            CreateTestDocumentInfo("b.txt", IndexingStatus.Pending));

        // Act
        _sut.StatusFilter = IndexingStatus.Indexed;
        var filtered = _sut.FilteredDocuments;

        // Assert
        Assert.Single(filtered);
        Assert.Equal(IndexingStatus.Indexed, filtered[0].Status);
    }

    [Fact]
    public async Task FilteredDocuments_WithCombinedFilters_AppliesBoth()
    {
        // Arrange
        await SetupWithDocuments(
            CreateTestDocumentInfo("apple.txt", IndexingStatus.Indexed),
            CreateTestDocumentInfo("apricot.txt", IndexingStatus.Pending),
            CreateTestDocumentInfo("banana.txt", IndexingStatus.Indexed));

        // Act
        _sut.FilterText = "ap";
        _sut.StatusFilter = IndexingStatus.Indexed;
        var filtered = _sut.FilteredDocuments;

        // Assert
        Assert.Single(filtered);
        Assert.Equal("apple.txt", filtered[0].FileName);
    }

    [Fact]
    public async Task FilterText_Change_NotifiesFilteredDocuments()
    {
        // Arrange
        await SetupWithDocuments(CreateTestDocumentInfo("test.txt", IndexingStatus.Indexed));
        var propertyChanged = false;
        _sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IndexStatusViewModel.FilteredDocuments))
                propertyChanged = true;
        };

        // Act
        _sut.FilterText = "test";

        // Assert
        Assert.True(propertyChanged);
    }

    [Fact]
    public async Task StatusFilter_Change_NotifiesFilteredDocuments()
    {
        // Arrange
        await SetupWithDocuments(CreateTestDocumentInfo("test.txt", IndexingStatus.Indexed));
        var propertyChanged = false;
        _sut.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(IndexStatusViewModel.FilteredDocuments))
                propertyChanged = true;
        };

        // Act
        _sut.StatusFilter = IndexingStatus.Pending;

        // Assert
        Assert.True(propertyChanged);
    }

    #endregion

    #region Static Properties Tests

    [Fact]
    public void StatusFilterOptions_ContainsAllStatuses()
    {
        var options = IndexStatusViewModel.StatusFilterOptions;

        Assert.Contains(null, options); // All
        Assert.Contains(IndexingStatus.Indexed, options);
        Assert.Contains(IndexingStatus.Pending, options);
        Assert.Contains(IndexingStatus.Indexing, options);
        Assert.Contains(IndexingStatus.Stale, options);
        Assert.Contains(IndexingStatus.Failed, options);
    }

    [Theory]
    [InlineData(null, "All Statuses")]
    [InlineData(IndexingStatus.Indexed, "Indexed")]
    [InlineData(IndexingStatus.Pending, "Pending")]
    [InlineData(IndexingStatus.Failed, "Failed")]
    public void GetStatusFilterDisplay_ReturnsCorrectText(IndexingStatus? status, string expected)
    {
        var result = IndexStatusViewModel.GetStatusFilterDisplay(status);
        Assert.Equal(expected, result);
    }

    #endregion

    #region Helper Methods

    private IndexedDocumentInfo CreateTestDocumentInfo(string filename, IndexingStatus status)
    {
        return new IndexedDocumentInfo
        {
            Id = Guid.NewGuid(),
            FilePath = $"/test/{filename}",
            Status = status,
            ChunkCount = 5,
            IndexedAt = DateTimeOffset.UtcNow
        };
    }

    private IndexStatistics CreateEmptyStatistics()
    {
        return new IndexStatistics
        {
            DocumentCount = 0,
            ChunkCount = 0,
            StorageSizeBytes = 0,
            StatusCounts = new Dictionary<IndexingStatus, int>()
        };
    }

    private async Task SetupWithDocuments(params IndexedDocumentInfo[] documents)
    {
        _statusServiceMock.Setup(s => s.GetAllDocumentsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(documents.ToList());
        _statusServiceMock.Setup(s => s.GetStatisticsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateEmptyStatistics());

        await _sut.LoadAsync();
    }

    #endregion
}
