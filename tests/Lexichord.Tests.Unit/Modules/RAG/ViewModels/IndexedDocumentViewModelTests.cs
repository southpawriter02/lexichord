// =============================================================================
// File: IndexedDocumentViewModelTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexedDocumentViewModel.
// Version: v0.4.7a
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="IndexedDocumentViewModel"/>.
/// </summary>
/// <remarks>
/// Tests cover property mapping, display formatting, and command availability.
/// </remarks>
public class IndexedDocumentViewModelTests
{
    #region Property Mapping Tests

    [Fact]
    public void Constructor_MapsIdFromInfo()
    {
        var id = Guid.NewGuid();
        var info = CreateTestInfo(id: id);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal(id, vm.Id);
    }

    [Fact]
    public void Constructor_MapsFilePathFromInfo()
    {
        var info = CreateTestInfo(filePath: "/path/to/file.txt");

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal("/path/to/file.txt", vm.FilePath);
    }

    [Fact]
    public void FileName_ExtractsFromFilePath()
    {
        var info = CreateTestInfo(filePath: "/some/directory/document.md");

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal("document.md", vm.FileName);
    }

    [Fact]
    public void Status_MapsFromInfo()
    {
        var info = CreateTestInfo(status: IndexingStatus.Indexed);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal(IndexingStatus.Indexed, vm.Status);
    }

    [Fact]
    public void ChunkCount_MapsFromInfo()
    {
        var info = CreateTestInfo(chunkCount: 15);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal(15, vm.ChunkCount);
    }

    [Fact]
    public void ErrorMessage_MapsFromInfo()
    {
        var info = CreateTestInfo(errorMessage: "Connection failed");

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal("Connection failed", vm.ErrorMessage);
    }

    #endregion

    #region Status Display Tests

    [Theory]
    [InlineData(IndexingStatus.Indexed, "Indexed")]
    [InlineData(IndexingStatus.Pending, "Pending")]
    [InlineData(IndexingStatus.Indexing, "Indexing...")]
    [InlineData(IndexingStatus.Stale, "Stale")]
    [InlineData(IndexingStatus.Failed, "Failed")]
    [InlineData(IndexingStatus.NotIndexed, "Not Indexed")]
    public void StatusDisplay_ReturnsCorrectText(IndexingStatus status, string expected)
    {
        var info = CreateTestInfo(status: status);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal(expected, vm.StatusDisplay);
    }

    [Theory]
    [InlineData(IndexingStatus.Indexed, "StatusSuccess")]
    [InlineData(IndexingStatus.Pending, "StatusPending")]
    [InlineData(IndexingStatus.Indexing, "StatusActive")]
    [InlineData(IndexingStatus.Stale, "StatusWarning")]
    [InlineData(IndexingStatus.Failed, "StatusError")]
    public void StatusColor_ReturnsSemanticColorName(IndexingStatus status, string expected)
    {
        var info = CreateTestInfo(status: status);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal(expected, vm.StatusColor);
    }

    #endregion

    #region ChunkCountDisplay Tests

    [Theory]
    [InlineData(0, "0 chunks")]
    [InlineData(1, "1 chunk")]
    [InlineData(5, "5 chunks")]
    [InlineData(100, "100 chunks")]
    public void ChunkCountDisplay_FormatsCorrectly(int count, string expected)
    {
        var info = CreateTestInfo(chunkCount: count);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal(expected, vm.ChunkCountDisplay);
    }

    #endregion

    #region IndexedAtDisplay Tests

    [Fact]
    public void IndexedAtDisplay_WithNull_ReturnsNever()
    {
        var info = CreateTestInfo(indexedAt: null);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal("Never", vm.IndexedAtDisplay);
    }

    [Fact]
    public void IndexedAtDisplay_WithValue_ReturnsFormattedDate()
    {
        var indexedAt = new DateTimeOffset(2026, 2, 1, 10, 30, 0, TimeSpan.Zero);
        var info = CreateTestInfo(indexedAt: indexedAt);

        var vm = new IndexedDocumentViewModel(info);

        // Should contain the date in some formatted form
        Assert.NotEqual("Never", vm.IndexedAtDisplay);
        Assert.Contains("2026", vm.IndexedAtDisplay);
    }

    #endregion

    #region RelativeTime Tests

    [Fact]
    public void RelativeTime_WithNull_ReturnsNever()
    {
        var info = CreateTestInfo(indexedAt: null);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Equal("Never", vm.RelativeTime);
    }

    [Fact]
    public void RelativeTime_WithRecentTime_IncludesAgo()
    {
        var recentTime = DateTimeOffset.UtcNow.AddHours(-1);
        var info = CreateTestInfo(indexedAt: recentTime);

        var vm = new IndexedDocumentViewModel(info);

        Assert.Contains("ago", vm.RelativeTime);
    }

    #endregion

    #region Boolean Indicator Tests

    [Fact]
    public void HasError_WithErrorMessage_ReturnsTrue()
    {
        var info = CreateTestInfo(errorMessage: "Some error");

        var vm = new IndexedDocumentViewModel(info);

        Assert.True(vm.HasError);
    }

    [Fact]
    public void HasError_WithNullErrorMessage_ReturnsFalse()
    {
        var info = CreateTestInfo(errorMessage: null);

        var vm = new IndexedDocumentViewModel(info);

        Assert.False(vm.HasError);
    }

    [Fact]
    public void HasError_WithEmptyErrorMessage_ReturnsFalse()
    {
        var info = CreateTestInfo(errorMessage: "");

        var vm = new IndexedDocumentViewModel(info);

        Assert.False(vm.HasError);
    }

    [Theory]
    [InlineData(IndexingStatus.Failed, true)]
    [InlineData(IndexingStatus.Indexed, false)]
    [InlineData(IndexingStatus.Pending, false)]
    public void CanRetry_DependsOnStatusAndAction(IndexingStatus status, bool expected)
    {
        var info = CreateTestInfo(status: status);
        // Provide a retryAction to ensure status check is the determining factor
        var vm = new IndexedDocumentViewModel(info, retryAction: id => { });

        Assert.Equal(expected, vm.CanRetry);
    }

    [Theory]
    [InlineData(IndexingStatus.Indexed, true)]
    [InlineData(IndexingStatus.Stale, true)]
    [InlineData(IndexingStatus.Pending, false)]
    [InlineData(IndexingStatus.Indexing, false)]
    [InlineData(IndexingStatus.Failed, false)]
    public void CanReindex_DependsOnStatusAndAction(IndexingStatus status, bool expected)
    {
        var info = CreateTestInfo(status: status);
        // Provide a reindexAction to ensure status check is the determining factor
        var vm = new IndexedDocumentViewModel(info, reindexAction: id => { });

        Assert.Equal(expected, vm.CanReindex);
    }

    #endregion

    #region Helper Methods

    private IndexedDocumentInfo CreateTestInfo(
        Guid? id = null,
        string filePath = "/test/file.txt",
        IndexingStatus status = IndexingStatus.Indexed,
        int chunkCount = 5,
        DateTimeOffset? indexedAt = null,
        string? errorMessage = null)
    {
        return new IndexedDocumentInfo
        {
            Id = id ?? Guid.NewGuid(),
            FilePath = filePath,
            Status = status,
            ChunkCount = chunkCount,
            IndexedAt = indexedAt,
            ErrorMessage = errorMessage,
            EstimatedSizeBytes = chunkCount * 2048
        };
    }

    #endregion
}
