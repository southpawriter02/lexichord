using Lexichord.Abstractions.Contracts.Ingestion;
using Moq;

namespace Lexichord.Tests.Unit.Abstractions.Ingestion;

/// <summary>
/// Contract tests for the <see cref="IIngestionService"/> interface.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify that the interface can be mocked and that
/// consumers can interact with it correctly. They establish the expected
/// behavior patterns without testing implementation details.
/// </remarks>
public class IIngestionServiceContractTests
{
    private readonly Mock<IIngestionService> _mockService;
    private readonly Guid _testProjectId = Guid.NewGuid();

    public IIngestionServiceContractTests()
    {
        _mockService = new Mock<IIngestionService>();
    }

    #region Interface Mockability Tests

    [Fact]
    public void IIngestionService_CanBeMocked()
    {
        // Assert
        _mockService.Object.Should().NotBeNull();
        _mockService.Object.Should().BeAssignableTo<IIngestionService>();
    }

    #endregion

    #region IngestFileAsync Contract Tests

    [Fact]
    public async Task IngestFileAsync_ReturnsIngestionResult()
    {
        // Arrange
        var expectedResult = IngestionResult.CreateSuccess(Guid.NewGuid(), 5, TimeSpan.FromSeconds(1));
        _mockService
            .Setup(s => s.IngestFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IngestionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockService.Object.IngestFileAsync(
            _testProjectId, "test.md", null, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task IngestFileAsync_AcceptsNullOptions()
    {
        // Arrange
        _mockService
            .Setup(s => s.IngestFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IngestionResult.CreateSuccess(Guid.NewGuid(), 1, TimeSpan.Zero));

        // Act
        var act = async () => await _mockService.Object.IngestFileAsync(
            _testProjectId, "file.md", null);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task IngestFileAsync_SupportsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockService
            .Setup(s => s.IngestFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IngestionOptions?>(),
                It.Is<CancellationToken>(ct => ct.IsCancellationRequested)))
            .ThrowsAsync(new OperationCanceledException());

        // Act
        var act = async () => await _mockService.Object.IngestFileAsync(
            _testProjectId, "file.md", null, cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region IngestDirectoryAsync Contract Tests

    [Fact]
    public async Task IngestDirectoryAsync_ReturnsIngestionResult()
    {
        // Arrange
        var expectedResult = IngestionResult.CreateBatchSuccess(100, TimeSpan.FromMinutes(1));
        _mockService
            .Setup(s => s.IngestDirectoryAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<IngestionOptions?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _mockService.Object.IngestDirectoryAsync(
            _testProjectId, "/path/to/dir", null, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public async Task IngestDirectoryAsync_AcceptsCustomOptions()
    {
        // Arrange
        var customOptions = IngestionOptions.Default with { MaxConcurrency = 1 };
        _mockService
            .Setup(s => s.IngestDirectoryAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                customOptions,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IngestionResult.CreateBatchSuccess(10, TimeSpan.FromSeconds(5)));

        // Act
        var result = await _mockService.Object.IngestDirectoryAsync(
            _testProjectId, "/path", customOptions);

        // Assert
        _mockService.Verify(s => s.IngestDirectoryAsync(
            _testProjectId, "/path", customOptions, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region RemoveDocumentAsync Contract Tests

    [Fact]
    public async Task RemoveDocumentAsync_CompletesSuccessfully()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockService
            .Setup(s => s.RemoveDocumentAsync(documentId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var act = async () => await _mockService.Object.RemoveDocumentAsync(documentId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region ProgressChanged Event Contract Tests

    [Fact]
    public void ProgressChanged_CanBeSubscribed()
    {
        // Arrange
        var eventRaised = false;
        _mockService.Object.ProgressChanged += (sender, args) => eventRaised = true;

        // Assert - no exception during subscription
        eventRaised.Should().BeFalse(because: "event hasn't been raised yet");
    }

    [Fact]
    public void ProgressChanged_EventArgsType_IsIngestionProgressEventArgs()
    {
        // Arrange
        var eventInfo = typeof(IIngestionService).GetEvent(nameof(IIngestionService.ProgressChanged));

        // Assert
        eventInfo.Should().NotBeNull();
        eventInfo!.EventHandlerType.Should().Be(typeof(EventHandler<IngestionProgressEventArgs>));
    }

    #endregion
}
