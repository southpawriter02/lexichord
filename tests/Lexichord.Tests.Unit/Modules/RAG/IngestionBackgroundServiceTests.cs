using Lexichord.Abstractions.Contracts.Ingestion;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG;

/// <summary>
/// Unit tests for the <see cref="IngestionBackgroundService"/> implementation.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the background service lifecycle, item processing,
/// throttling behavior, and graceful shutdown characteristics.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.2d")]
public class IngestionBackgroundServiceTests : IDisposable
{
    private readonly Mock<IIngestionQueue> _mockQueue;
    private readonly Mock<IIngestionService> _mockIngestionService;
    private readonly IOptions<IngestionQueueOptions> _options;
    private readonly IngestionBackgroundService _sut;

    public IngestionBackgroundServiceTests()
    {
        _mockQueue = new Mock<IIngestionQueue>();
        _mockIngestionService = new Mock<IIngestionService>();
        _options = Options.Create(new IngestionQueueOptions(ThrottleDelayMs: 0, ShutdownTimeoutSeconds: 1));
        _sut = new IngestionBackgroundService(
            _mockQueue.Object,
            _mockIngestionService.Object,
            _options,
            NullLogger<IngestionBackgroundService>.Instance);
    }

    public void Dispose()
    {
        _sut.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullQueue_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngestionBackgroundService(
            null!,
            _mockIngestionService.Object,
            _options,
            NullLogger<IngestionBackgroundService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("queue");
    }

    [Fact]
    public void Constructor_NullIngestionService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngestionBackgroundService(
            _mockQueue.Object,
            null!,
            _options,
            NullLogger<IngestionBackgroundService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("ingestionService");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngestionBackgroundService(
            _mockQueue.Object,
            _mockIngestionService.Object,
            null!,
            NullLogger<IngestionBackgroundService>.Instance);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new IngestionBackgroundService(
            _mockQueue.Object,
            _mockIngestionService.Object,
            _options,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void ProcessedCount_InitiallyZero()
    {
        // Assert
        _sut.ProcessedCount.Should().Be(0);
    }

    [Fact]
    public void ErrorCount_InitiallyZero()
    {
        // Assert
        _sut.ErrorCount.Should().Be(0);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_ProcessesQueuedItem()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        var result = IngestionResult.CreateSuccess(Guid.NewGuid(), 5, TimeSpan.FromMilliseconds(100));

        var callCount = 0;
        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return item;
                }
                // Second call: wait for cancellation
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        _mockIngestionService.Setup(s => s.IngestFileAsync(
                item.ProjectId,
                item.FilePath,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(500); // Give it time to process
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _mockIngestionService.Verify(s => s.IngestFileAsync(
            item.ProjectId,
            item.FilePath,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_IncrementsProcessedCount_OnSuccess()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        var result = IngestionResult.CreateSuccess(Guid.NewGuid(), 5, TimeSpan.FromMilliseconds(100));

        var callCount = 0;
        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return item;
                }
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        _mockIngestionService.Setup(s => s.IngestFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);

        // Act
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _sut.ProcessedCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ExecuteAsync_IncrementsErrorCount_OnFailure()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");

        var callCount = 0;
        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                callCount++;
                if (callCount == 1)
                {
                    return item;
                }
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        _mockIngestionService.Setup(s => s.IngestFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File not found"));

        // Act
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        _sut.ErrorCount.Should().BeGreaterOrEqualTo(1);
    }

    [Fact]
    public async Task ExecuteAsync_ContinuesAfterError()
    {
        // Arrange
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var item1 = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/fail.md");
        var item2 = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/success.md");
        var successResult = IngestionResult.CreateSuccess(Guid.NewGuid(), 5, TimeSpan.FromMilliseconds(100));

        var callCount = 0;
        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                callCount++;
                if (callCount == 1) return item1;
                if (callCount == 2) return item2;
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        _mockIngestionService.Setup(s => s.IngestFileAsync(
                item1.ProjectId,
                item1.FilePath,
                null,
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("Failed"));

        _mockIngestionService.Setup(s => s.IngestFileAsync(
                item2.ProjectId,
                item2.FilePath,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(successResult);

        // Act
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(1000);
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - service continued after first error
        _mockIngestionService.Verify(s => s.IngestFileAsync(
            item2.ProjectId,
            item2.FilePath,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        var result = IngestionResult.CreateSuccess(Guid.NewGuid(), 5, TimeSpan.FromMilliseconds(100));

        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        // Act
        var task = _sut.StartAsync(cts.Token);
        await Task.Delay(100);
        cts.Cancel();

        // Assert - should complete without throwing
        await FluentActions.Invoking(async () => await task).Should().NotThrowAsync();
    }

    #endregion

    #region Throttling Tests

    [Fact]
    public async Task ExecuteAsync_WithThrottle_DelaysBetweenItems()
    {
        // Arrange
        var throttleMs = 100;
        var options = Options.Create(new IngestionQueueOptions(ThrottleDelayMs: throttleMs, ShutdownTimeoutSeconds: 1));
        using var service = new IngestionBackgroundService(
            _mockQueue.Object,
            _mockIngestionService.Object,
            options,
            NullLogger<IngestionBackgroundService>.Instance);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var item = IngestionQueueItem.Create(Guid.NewGuid(), "/path/to/file.md");
        var result = IngestionResult.CreateSuccess(Guid.NewGuid(), 5, TimeSpan.FromMilliseconds(10));

        var processTimes = new List<DateTimeOffset>();
        var callCount = 0;

        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                callCount++;
                if (callCount <= 2)
                {
                    return item;
                }
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        _mockIngestionService.Setup(s => s.IngestFileAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                null,
                It.IsAny<CancellationToken>()))
            .Callback(() => processTimes.Add(DateTimeOffset.UtcNow))
            .ReturnsAsync(result);

        // Act
        var task = service.StartAsync(cts.Token);
        await Task.Delay(500);
        cts.Cancel();

        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert
        if (processTimes.Count >= 2)
        {
            var gap = processTimes[1] - processTimes[0];
            gap.TotalMilliseconds.Should().BeGreaterOrEqualTo(throttleMs * 0.8); // Allow some tolerance
        }
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_CompletesWithinTimeout()
    {
        // Arrange
        using var cts = new CancellationTokenSource();

        _mockQueue.Setup(q => q.DequeueAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(async ct =>
            {
                await Task.Delay(Timeout.Infinite, ct);
                throw new OperationCanceledException(ct);
            });

        // Act
        var startTask = _sut.StartAsync(cts.Token);
        await Task.Delay(50);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _sut.StopAsync(CancellationToken.None);
        stopwatch.Stop();

        // Assert
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(5));
    }

    #endregion
}
