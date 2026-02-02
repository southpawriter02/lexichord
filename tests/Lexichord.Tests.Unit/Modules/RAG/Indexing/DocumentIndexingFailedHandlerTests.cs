// =============================================================================
// File: DocumentIndexingFailedHandlerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DocumentIndexingFailedHandler.
// Version: v0.4.7d
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Indexing;
using Lexichord.Modules.RAG.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Indexing;

/// <summary>
/// Unit tests for <see cref="DocumentIndexingFailedHandler"/>.
/// </summary>
public class DocumentIndexingFailedHandlerTests
{
    private readonly Mock<IDocumentRepository> _documentRepoMock;
    private readonly Mock<ILogger<DocumentIndexingFailedHandler>> _loggerMock;

    public DocumentIndexingFailedHandlerTests()
    {
        _documentRepoMock = new Mock<IDocumentRepository>();
        _loggerMock = new Mock<ILogger<DocumentIndexingFailedHandler>>();
    }

    private DocumentIndexingFailedHandler CreateSut() =>
        new(_documentRepoMock.Object, _loggerMock.Object);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullRepository_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("documentRepository", () =>
            new DocumentIndexingFailedHandler(null!, _loggerMock.Object));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>("logger", () =>
            new DocumentIndexingFailedHandler(_documentRepoMock.Object, null!));
    }

    #endregion

    #region Handle Tests

    [Fact]
    public async Task Handle_WithNullNotification_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>("notification", async () =>
            await sut.Handle(null!, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_WithException_CategorizesError()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new FileNotFoundException("File not found", "/path/to/file.md");
        var notification = new DocumentIndexingFailedEvent("/path/to/file.md", "Error", exception);

        // Act
        await sut.Handle(notification, CancellationToken.None);

        // Assert - Verify logging occurred (category should be FileNotFound)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("FileNotFound")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutException_UsesUnknownCategory()
    {
        // Arrange
        var sut = CreateSut();
        var notification = new DocumentIndexingFailedEvent("/path/to/file.md", "Some error");

        // Act
        await sut.Handle(notification, CancellationToken.None);

        // Assert - Handler should still work without exception
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("/path/to/file.md")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_LogsAtWarningLevel()
    {
        // Arrange
        var sut = CreateSut();
        var notification = new DocumentIndexingFailedEvent("/path/to/file.md", "Error");

        // Act
        await sut.Handle(notification, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithHttpException_LogsRateLimitCategory()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new HttpRequestException("Rate limited", null, System.Net.HttpStatusCode.TooManyRequests);
        var notification = new DocumentIndexingFailedEvent("/path/to/file.md", "Rate limited", exception);

        // Act
        await sut.Handle(notification, CancellationToken.None);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("RateLimit")),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region Integration Scenario Tests

    [Fact]
    public async Task Handle_WithNetworkError_LogsAsRetryable()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new HttpRequestException("Connection failed");
        var notification = new DocumentIndexingFailedEvent("/path/to/file.md", "Network error", exception);

        // Act
        await sut.Handle(notification, CancellationToken.None);

        // Assert - Should log IsRetryable: True for network errors
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IsRetryable") && v.ToString()!.Contains("True")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithFileNotFound_LogsAsNotRetryable()
    {
        // Arrange
        var sut = CreateSut();
        var exception = new FileNotFoundException();
        var notification = new DocumentIndexingFailedEvent("/missing.md", "File not found", exception);

        // Act
        await sut.Handle(notification, CancellationToken.None);

        // Assert - Should log IsRetryable: False for file not found
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("IsRetryable") && v.ToString()!.Contains("False")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion
}
