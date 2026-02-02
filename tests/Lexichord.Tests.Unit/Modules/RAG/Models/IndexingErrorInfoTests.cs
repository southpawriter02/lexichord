// =============================================================================
// File: IndexingErrorInfoTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexingErrorInfo record.
// Version: v0.4.7d
// =============================================================================

using Lexichord.Modules.RAG.Models;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Models;

/// <summary>
/// Unit tests for <see cref="IndexingErrorInfo"/> record.
/// </summary>
public class IndexingErrorInfoTests
{
    #region IsRetryable Tests

    [Theory]
    [InlineData(IndexingErrorCategory.RateLimit, true)]
    [InlineData(IndexingErrorCategory.NetworkError, true)]
    [InlineData(IndexingErrorCategory.ServiceUnavailable, true)]
    [InlineData(IndexingErrorCategory.Unknown, false)]
    [InlineData(IndexingErrorCategory.InvalidContent, false)]
    [InlineData(IndexingErrorCategory.FileTooLarge, false)]
    [InlineData(IndexingErrorCategory.FileNotFound, false)]
    [InlineData(IndexingErrorCategory.PermissionDenied, false)]
    [InlineData(IndexingErrorCategory.TokenLimitExceeded, false)]
    [InlineData(IndexingErrorCategory.ApiKeyInvalid, false)]
    public void IsRetryable_ReturnsCorrectValue(IndexingErrorCategory category, bool expected)
    {
        // Arrange
        var info = CreateTestInfo(category);

        // Act & Assert
        Assert.Equal(expected, info.IsRetryable);
    }

    #endregion

    #region SuggestedAction Tests

    [Fact]
    public void SuggestedAction_WithRateLimit_SuggestsWaiting()
    {
        // Arrange
        var info = CreateTestInfo(IndexingErrorCategory.RateLimit);

        // Act & Assert
        Assert.Contains("wait", info.SuggestedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SuggestedAction_WithNetworkError_SuggestsCheckingConnection()
    {
        // Arrange
        var info = CreateTestInfo(IndexingErrorCategory.NetworkError);

        // Act & Assert
        Assert.Contains("connection", info.SuggestedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SuggestedAction_WithFileNotFound_SuggestsCheckingFile()
    {
        // Arrange
        var info = CreateTestInfo(IndexingErrorCategory.FileNotFound);

        // Act & Assert
        Assert.Contains("file exists", info.SuggestedAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SuggestedAction_WithApiKeyInvalid_SuggestsCheckingSettings()
    {
        // Arrange
        var info = CreateTestInfo(IndexingErrorCategory.ApiKeyInvalid);

        // Act & Assert
        Assert.Contains("Settings", info.SuggestedAction);
    }

    [Fact]
    public void SuggestedAction_WithUnknown_SuggestsReindexing()
    {
        // Arrange
        var info = CreateTestInfo(IndexingErrorCategory.Unknown);

        // Act & Assert
        Assert.Contains("re-indexing", info.SuggestedAction, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region Create Factory Method Tests

    [Fact]
    public void Create_SetsAllProperties()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var filePath = "/path/to/test.md";
        var message = "Test error";
        var category = IndexingErrorCategory.RateLimit;
        var retryCount = 3;

        // Act
        var info = IndexingErrorInfo.Create(documentId, filePath, message, category, retryCount);

        // Assert
        Assert.Equal(documentId, info.DocumentId);
        Assert.Equal(filePath, info.FilePath);
        Assert.Equal(message, info.Message);
        Assert.Equal(category, info.Category);
        Assert.Equal(retryCount, info.RetryCount);
        Assert.True(info.Timestamp <= DateTimeOffset.UtcNow);
        Assert.True(info.Timestamp > DateTimeOffset.UtcNow.AddSeconds(-5));
    }

    [Fact]
    public void Create_WithDefaultRetryCount_SetsZero()
    {
        // Arrange & Act
        var info = IndexingErrorInfo.Create(
            Guid.NewGuid(),
            "/path/to/file.md",
            "Test",
            IndexingErrorCategory.Unknown);

        // Assert
        Assert.Equal(0, info.RetryCount);
    }

    #endregion

    #region Record Equality Tests

    [Fact]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var info1 = new IndexingErrorInfo(id, "/path/file.md", "Error", timestamp, 1, IndexingErrorCategory.RateLimit);
        var info2 = new IndexingErrorInfo(id, "/path/file.md", "Error", timestamp, 1, IndexingErrorCategory.RateLimit);

        // Act & Assert
        Assert.Equal(info1, info2);
    }

    [Fact]
    public void Equals_WithDifferentCategory_ReturnsFalse()
    {
        // Arrange
        var id = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var info1 = new IndexingErrorInfo(id, "/path/file.md", "Error", timestamp, 1, IndexingErrorCategory.RateLimit);
        var info2 = new IndexingErrorInfo(id, "/path/file.md", "Error", timestamp, 1, IndexingErrorCategory.NetworkError);

        // Act & Assert
        Assert.NotEqual(info1, info2);
    }

    #endregion

    #region Helper Methods

    private static IndexingErrorInfo CreateTestInfo(IndexingErrorCategory category) =>
        new(
            Guid.NewGuid(),
            "/path/to/test.md",
            "Test error message",
            DateTimeOffset.UtcNow,
            0,
            category);

    #endregion
}
