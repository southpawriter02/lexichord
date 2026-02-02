// =============================================================================
// File: IndexingErrorCategorizerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for IndexingErrorCategorizer.
// Version: v0.4.7d
// =============================================================================

using System.Net;
using System.Net.Http;
using Lexichord.Modules.RAG.Models;
using Lexichord.Modules.RAG.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="IndexingErrorCategorizer"/>.
/// </summary>
public class IndexingErrorCategorizerTests
{
    #region Categorize Tests - HTTP Exceptions

    [Fact]
    public void Categorize_WithTooManyRequests_ReturnsRateLimit()
    {
        // Arrange
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.RateLimit, category);
    }

    [Fact]
    public void Categorize_WithServiceUnavailable_ReturnsServiceUnavailable()
    {
        // Arrange
        var exception = new HttpRequestException("Service down", null, HttpStatusCode.ServiceUnavailable);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.ServiceUnavailable, category);
    }

    [Fact]
    public void Categorize_WithUnauthorized_ReturnsApiKeyInvalid()
    {
        // Arrange
        var exception = new HttpRequestException("Unauthorized", null, HttpStatusCode.Unauthorized);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.ApiKeyInvalid, category);
    }

    [Fact]
    public void Categorize_WithForbidden_ReturnsApiKeyInvalid()
    {
        // Arrange
        var exception = new HttpRequestException("Forbidden", null, HttpStatusCode.Forbidden);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.ApiKeyInvalid, category);
    }

    [Fact]
    public void Categorize_WithGenericHttpException_ReturnsNetworkError()
    {
        // Arrange
        var exception = new HttpRequestException("Connection failed");

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.NetworkError, category);
    }

    #endregion

    #region Categorize Tests - File System Exceptions

    [Fact]
    public void Categorize_WithFileNotFoundException_ReturnsFileNotFound()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found", "/path/to/file.md");

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.FileNotFound, category);
    }

    [Fact]
    public void Categorize_WithDirectoryNotFoundException_ReturnsFileNotFound()
    {
        // Arrange
        var exception = new DirectoryNotFoundException("Directory not found");

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.FileNotFound, category);
    }

    [Fact]
    public void Categorize_WithUnauthorizedAccessException_ReturnsPermissionDenied()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Access denied");

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.PermissionDenied, category);
    }

    #endregion

    #region Categorize Tests - Message Pattern Matching

    [Theory]
    [InlineData("Rate limit exceeded")]
    [InlineData("rate limit")]
    [InlineData("RATE LIMIT reached")]
    public void Categorize_WithRateLimitMessage_ReturnsRateLimit(string message)
    {
        // Arrange
        var exception = new InvalidOperationException(message);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.RateLimit, category);
    }

    [Theory]
    [InlineData("File too large")]
    [InlineData("Content is TOO LARGE")]
    public void Categorize_WithTooLargeMessage_ReturnsFileTooLarge(string message)
    {
        // Arrange
        var exception = new InvalidOperationException(message);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.FileTooLarge, category);
    }

    [Theory]
    [InlineData("Token limit exceeded")]
    [InlineData("max tokens reached")]
    public void Categorize_WithTokenLimitMessage_ReturnsTokenLimitExceeded(string message)
    {
        // Arrange
        var exception = new InvalidOperationException(message);

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.TokenLimitExceeded, category);
    }

    [Fact]
    public void Categorize_WithUnknownException_ReturnsUnknown()
    {
        // Arrange
        var exception = new InvalidOperationException("Something unexpected happened");

        // Act
        var category = IndexingErrorCategorizer.Categorize(exception);

        // Assert
        Assert.Equal(IndexingErrorCategory.Unknown, category);
    }

    #endregion

    #region GetUserFriendlyMessage Tests

    [Fact]
    public void GetUserFriendlyMessage_WithRateLimit_ReturnsAppropriateMessage()
    {
        // Arrange
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);
        var category = IndexingErrorCategory.RateLimit;

        // Act
        var message = IndexingErrorCategorizer.GetUserFriendlyMessage(exception, category);

        // Assert
        Assert.Contains("Rate limit", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetUserFriendlyMessage_WithFileNotFound_IncludesFileName()
    {
        // Arrange
        var exception = new FileNotFoundException("File not found", "test.md");
        var category = IndexingErrorCategory.FileNotFound;

        // Act
        var message = IndexingErrorCategorizer.GetUserFriendlyMessage(exception, category);

        // Assert
        Assert.Contains("test.md", message);
    }

    [Fact]
    public void GetUserFriendlyMessage_WithUnknown_TruncatesLongMessage()
    {
        // Arrange
        var longMessage = new string('x', 300);
        var exception = new InvalidOperationException(longMessage);
        var category = IndexingErrorCategory.Unknown;

        // Act
        var message = IndexingErrorCategorizer.GetUserFriendlyMessage(exception, category);

        // Assert
        Assert.True(message.Length <= 203); // 200 + "..."
        Assert.EndsWith("...", message);
    }

    #endregion

    #region CreateErrorInfo Tests

    [Fact]
    public void CreateErrorInfo_ReturnsFullyPopulatedInfo()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var filePath = "/path/to/file.md";
        var exception = new HttpRequestException("Rate limited", null, HttpStatusCode.TooManyRequests);

        // Act
        var info = IndexingErrorCategorizer.CreateErrorInfo(documentId, filePath, exception, 2);

        // Assert
        Assert.Equal(documentId, info.DocumentId);
        Assert.Equal(filePath, info.FilePath);
        Assert.Equal(IndexingErrorCategory.RateLimit, info.Category);
        Assert.Equal(2, info.RetryCount);
        Assert.True(info.IsRetryable);
        Assert.NotEmpty(info.Message);
        Assert.NotEmpty(info.SuggestedAction);
    }

    #endregion

    #region Null Parameter Tests

    [Fact]
    public void Categorize_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => IndexingErrorCategorizer.Categorize(null!));
    }

    [Fact]
    public void GetUserFriendlyMessage_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            IndexingErrorCategorizer.GetUserFriendlyMessage(null!, IndexingErrorCategory.Unknown));
    }

    #endregion
}
