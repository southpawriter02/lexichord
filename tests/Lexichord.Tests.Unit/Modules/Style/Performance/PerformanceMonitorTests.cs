using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Performance;

/// <summary>
/// Unit tests for <see cref="PerformanceMonitor"/>.
/// </summary>
/// <remarks>
/// LOGIC: Tests verify metrics collection, adaptive debounce,
/// and performance degradation detection.
///
/// Version: v0.2.7d
/// </remarks>
public sealed class PerformanceMonitorTests : IDisposable
{
    private readonly PerformanceMonitor _sut;
    private readonly Mock<ILogger<PerformanceMonitor>> _mockLogger;

    public PerformanceMonitorTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceMonitor>>();
        _sut = new PerformanceMonitor(_mockLogger.Object);
    }

    [Fact]
    public void GetMetrics_NoOperations_ReturnsZeroValues()
    {
        // Act
        var metrics = _sut.GetMetrics();

        // Assert
        metrics.AverageScanDurationMs.Should().Be(0);
        metrics.ScansCompleted.Should().Be(0);
    }

    [Fact]
    public void RecordOperation_TracksAverage()
    {
        // Arrange
        _sut.RecordOperation("scan1", TimeSpan.FromMilliseconds(100));
        _sut.RecordOperation("scan2", TimeSpan.FromMilliseconds(200));
        _sut.RecordOperation("scan3", TimeSpan.FromMilliseconds(300));

        // Act
        var metrics = _sut.GetMetrics();

        // Assert
        metrics.AverageScanDurationMs.Should().Be(200);
        metrics.ScansCompleted.Should().Be(3);
    }

    [Fact]
    public void RecommendedDebounceInterval_FastScans_ReturnsMinimum()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            _sut.RecordOperation($"scan{i}", TimeSpan.FromMilliseconds(50));
        }

        // Act
        var debounce = _sut.RecommendedDebounceInterval;

        // Assert
        debounce.TotalMilliseconds.Should().Be(200);
    }

    [Fact]
    public void RecommendedDebounceInterval_SlowScans_ReturnsHigher()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            _sut.RecordOperation($"scan{i}", TimeSpan.FromMilliseconds(600));
        }

        // Act
        var debounce = _sut.RecommendedDebounceInterval;

        // Assert
        debounce.TotalMilliseconds.Should().BeGreaterThan(500);
    }

    [Fact]
    public void IsPerformanceDegraded_SlowScans_ReturnsTrue()
    {
        // Arrange
        for (var i = 0; i < 10; i++)
        {
            _sut.RecordOperation($"scan{i}", TimeSpan.FromMilliseconds(600));
        }

        // Assert
        _sut.IsPerformanceDegraded.Should().BeTrue();
    }

    [Fact]
    public void Reset_ClearsAllMetrics()
    {
        // Arrange
        _sut.RecordOperation("scan1", TimeSpan.FromMilliseconds(100));
        _sut.ReportFrameDrop(5);

        // Act
        _sut.Reset();
        var metrics = _sut.GetMetrics();

        // Assert
        metrics.ScansCompleted.Should().Be(0);
        metrics.FrameDropCount.Should().Be(0);
    }

    [Fact]
    public void ReportFrameDrop_IncreasesCount()
    {
        // Act
        _sut.ReportFrameDrop(3);
        _sut.ReportFrameDrop(2);

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.FrameDropCount.Should().Be(5);
    }

    [Fact]
    public void StartOperation_TracksElapsedTime()
    {
        // Act
        using (_sut.StartOperation("test"))
        {
            Thread.Sleep(50);
        }

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.ScansCompleted.Should().Be(1);
        metrics.AverageScanDurationMs.Should().BeGreaterOrEqualTo(40); // Allow some tolerance
    }

    [Fact]
    public void GetMetrics_CalculatesP95Correctly()
    {
        // Arrange - Add 20 samples with one outlier
        for (var i = 0; i < 19; i++)
        {
            _sut.RecordOperation($"scan{i}", TimeSpan.FromMilliseconds(100));
        }
        _sut.RecordOperation("outlier", TimeSpan.FromMilliseconds(500));

        // Act
        var metrics = _sut.GetMetrics();

        // Assert
        metrics.P95ScanDurationMs.Should().BeGreaterOrEqualTo(100);
        metrics.MaxScanDurationMs.Should().Be(500);
    }

    [Fact]
    public void RecordCancellation_TracksCount()
    {
        // Act
        _sut.RecordCancellation();
        _sut.RecordCancellation();

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.ScansCancelled.Should().Be(2);
    }

    [Fact]
    public void IsPerformanceDegraded_FrameDrops_ReturnsTrue()
    {
        // Arrange
        _sut.ReportFrameDrop(10);

        // Act & Assert
        _sut.IsPerformanceDegraded.Should().BeTrue();
    }

    public void Dispose()
    {
        _sut.Dispose();
    }
}
