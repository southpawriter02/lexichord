// =============================================================================
// File: DeduplicationMetricsServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DeduplicationMetricsService.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Metrics;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="DeduplicationMetricsService"/>.
/// </summary>
public sealed class DeduplicationMetricsServiceTests : IDisposable
{
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IDeduplicationService> _deduplicationServiceMock;
    private readonly Mock<IContradictionService> _contradictionServiceMock;
    private readonly Mock<ILogger<DeduplicationMetricsService>> _loggerMock;
    private readonly DeduplicationMetricsService _sut;

    public DeduplicationMetricsServiceTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _deduplicationServiceMock = new Mock<IDeduplicationService>();
        _contradictionServiceMock = new Mock<IContradictionService>();
        _loggerMock = new Mock<ILogger<DeduplicationMetricsService>>();

        _sut = new DeduplicationMetricsService(
            _licenseContextMock.Object,
            _deduplicationServiceMock.Object,
            _contradictionServiceMock.Object,
            _loggerMock.Object);

        // Reset static metrics before each test.
        DeduplicationMetrics.Reset();
    }

    public void Dispose()
    {
        // Clean up static state after each test.
        DeduplicationMetrics.Reset();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMetricsService(
                null!,
                _deduplicationServiceMock.Object,
                _contradictionServiceMock.Object,
                _loggerMock.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullDeduplicationService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMetricsService(
                _licenseContextMock.Object,
                null!,
                _contradictionServiceMock.Object,
                _loggerMock.Object));

        Assert.Equal("deduplicationService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContradictionService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMetricsService(
                _licenseContextMock.Object,
                _deduplicationServiceMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("contradictionService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new DeduplicationMetricsService(
                _licenseContextMock.Object,
                _deduplicationServiceMock.Object,
                _contradictionServiceMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    #endregion

    #region RecordChunkProcessed Tests

    [Fact]
    public void RecordChunkProcessed_IncrementsStaticCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ChunksProcessedTotal;

        // Act
        _sut.RecordChunkProcessed(DeduplicationAction.StoredAsNew, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ChunksProcessedTotal);
    }

    [Theory]
    [InlineData(DeduplicationAction.StoredAsNew)]
    [InlineData(DeduplicationAction.MergedIntoExisting)]
    [InlineData(DeduplicationAction.LinkedToExisting)]
    [InlineData(DeduplicationAction.FlaggedAsContradiction)]
    [InlineData(DeduplicationAction.QueuedForReview)]
    [InlineData(DeduplicationAction.SupersededExisting)]
    public void RecordChunkProcessed_RecordsAllActionTypes(DeduplicationAction action)
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ChunksProcessedTotal;

        // Act
        _sut.RecordChunkProcessed(action, TimeSpan.FromMilliseconds(15));

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ChunksProcessedTotal);
    }

    [Fact]
    public void RecordChunkProcessed_TracksMultipleChunks()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ChunksProcessedTotal;

        // Act
        _sut.RecordChunkProcessed(DeduplicationAction.StoredAsNew, TimeSpan.FromMilliseconds(10));
        _sut.RecordChunkProcessed(DeduplicationAction.MergedIntoExisting, TimeSpan.FromMilliseconds(20));
        _sut.RecordChunkProcessed(DeduplicationAction.LinkedToExisting, TimeSpan.FromMilliseconds(15));

        // Assert
        Assert.Equal(initialCount + 3, DeduplicationMetrics.ChunksProcessedTotal);
    }

    #endregion

    #region RecordSimilarityQuery Tests

    [Fact]
    public void RecordSimilarityQuery_IncrementsSimilarityQueryCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.SimilarityQueriesTotal;

        // Act
        _sut.RecordSimilarityQuery(TimeSpan.FromMilliseconds(5), 3);

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.SimilarityQueriesTotal);
    }

    [Fact]
    public void RecordSimilarityQuery_TracksMatchCount()
    {
        // Arrange
        var initialMatches = DeduplicationMetrics.SimilarityMatchesTotal;

        // Act
        _sut.RecordSimilarityQuery(TimeSpan.FromMilliseconds(5), 5);
        _sut.RecordSimilarityQuery(TimeSpan.FromMilliseconds(10), 3);

        // Assert
        Assert.Equal(initialMatches + 8, DeduplicationMetrics.SimilarityMatchesTotal);
    }

    #endregion

    #region RecordClassification Tests

    [Fact]
    public void RecordClassification_IncrementsClassificationCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ClassificationRequestsTotal;

        // Act
        _sut.RecordClassification(ClassificationMethod.RuleBased, RelationshipType.Equivalent, TimeSpan.FromMilliseconds(8));

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ClassificationRequestsTotal);
    }

    [Theory]
    [InlineData(ClassificationMethod.RuleBased)]
    [InlineData(ClassificationMethod.LlmBased)]
    [InlineData(ClassificationMethod.Cached)]
    public void RecordClassification_RecordsAllMethodTypes(ClassificationMethod method)
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ClassificationRequestsTotal;

        // Act
        _sut.RecordClassification(method, RelationshipType.Equivalent, TimeSpan.FromMilliseconds(10));

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ClassificationRequestsTotal);
    }

    #endregion

    #region RecordContradictionDetected Tests

    [Fact]
    public void RecordContradictionDetected_IncrementsContradictionCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ContradictionsDetectedTotal;

        // Act
        _sut.RecordContradictionDetected(ContradictionSeverity.High);

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ContradictionsDetectedTotal);
    }

    [Theory]
    [InlineData(ContradictionSeverity.Low)]
    [InlineData(ContradictionSeverity.Medium)]
    [InlineData(ContradictionSeverity.High)]
    [InlineData(ContradictionSeverity.Critical)]
    public void RecordContradictionDetected_RecordsAllSeverityLevels(ContradictionSeverity severity)
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ContradictionsDetectedTotal;

        // Act
        _sut.RecordContradictionDetected(severity);

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ContradictionsDetectedTotal);
    }

    #endregion

    #region RecordBatchJobCompleted Tests

    [Fact]
    public void RecordBatchJobCompleted_IncrementsBatchJobCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.BatchJobsCompleted;
        var mockResult = BatchDeduplicationResult.Success(
            Guid.NewGuid(), 100, 20, 15, 5, 2, 1, 0, TimeSpan.FromSeconds(30), 10000);

        // Act
        _sut.RecordBatchJobCompleted(mockResult);
        _sut.RecordBatchJobCompleted(mockResult);

        // Assert
        Assert.Equal(initialCount + 2, DeduplicationMetrics.BatchJobsCompleted);
    }

    #endregion

    #region GetDashboardDataAsync Tests

    [Fact]
    public async Task GetDashboardDataAsync_WithoutLicense_ReturnsEmptyData()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationMetrics))
            .Returns(false);

        // Act
        var result = await _sut.GetDashboardDataAsync();

        // Assert
        Assert.Equal(DeduplicationDashboardData.Empty, result);
    }

    [Fact]
    public async Task GetDashboardDataAsync_WithLicense_ReturnsData()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationMetrics))
            .Returns(true);

        _deduplicationServiceMock
            .Setup(d => d.GetPendingReviewsAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PendingReview>());

        _contradictionServiceMock
            .Setup(c => c.GetPendingAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Contradiction>());

        // Record some metrics first.
        _sut.RecordChunkProcessed(DeduplicationAction.StoredAsNew, TimeSpan.FromMilliseconds(10));
        _sut.RecordChunkProcessed(DeduplicationAction.MergedIntoExisting, TimeSpan.FromMilliseconds(15));
        _sut.RecordSimilarityQuery(TimeSpan.FromMilliseconds(5), 2);
        _sut.RecordClassification(ClassificationMethod.RuleBased, RelationshipType.Equivalent, TimeSpan.FromMilliseconds(8));

        // Act
        var result = await _sut.GetDashboardDataAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ChunksProcessedToday >= 2);
    }

    #endregion

    #region GetHealthStatusAsync Tests

    [Fact]
    public async Task GetHealthStatusAsync_WithNoMetrics_ReturnsHealthy()
    {
        // Act
        var result = await _sut.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(HealthLevel.Healthy, result.Level);
    }

    [Fact]
    public async Task GetHealthStatusAsync_IncludesMessageDescription()
    {
        // Act
        var result = await _sut.GetHealthStatusAsync();

        // Assert
        Assert.NotNull(result.Message);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public async Task GetHealthStatusAsync_ReturnsPerformanceMetrics()
    {
        // Arrange - add some metrics
        _sut.RecordChunkProcessed(DeduplicationAction.StoredAsNew, TimeSpan.FromMilliseconds(50));
        _sut.RecordSimilarityQuery(TimeSpan.FromMilliseconds(20), 5);
        _sut.RecordClassification(ClassificationMethod.RuleBased, RelationshipType.Equivalent, TimeSpan.FromMilliseconds(30));

        // Act
        var result = await _sut.GetHealthStatusAsync();

        // Assert
        Assert.True(result.ProcessingP99Ms >= 0);
        Assert.True(result.SimilarityQueryP99Ms >= 0);
        Assert.True(result.ClassificationP99Ms >= 0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task RecordChunkProcessed_IsThreadSafe()
    {
        // Arrange
        const int iterations = 1000;
        var initialCount = DeduplicationMetrics.ChunksProcessedTotal;
        var tasks = new List<Task>();

        // Act - run many parallel increments
        for (var i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() =>
                _sut.RecordChunkProcessed(DeduplicationAction.StoredAsNew, TimeSpan.FromMilliseconds(1))));
        }

        await Task.WhenAll(tasks);

        // Assert - all increments should be recorded
        Assert.Equal(initialCount + iterations, DeduplicationMetrics.ChunksProcessedTotal);
    }

    [Fact]
    public void RecordSimilarityQuery_WithZeroMatches_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _sut.RecordSimilarityQuery(TimeSpan.FromMilliseconds(5), 0);
        Assert.Equal(1, DeduplicationMetrics.SimilarityQueriesTotal);
    }

    [Fact]
    public void RecordClassification_WithZeroDuration_DoesNotThrow()
    {
        // Act & Assert - should not throw
        _sut.RecordClassification(ClassificationMethod.RuleBased, RelationshipType.Equivalent, TimeSpan.Zero);
        Assert.Equal(1, DeduplicationMetrics.ClassificationRequestsTotal);
    }

    #endregion

    #region GetTrendsAsync Tests

    [Fact]
    public async Task GetTrendsAsync_WithoutLicense_ReturnsEmptyList()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationMetrics))
            .Returns(false);

        // Act
        var result = await _sut.GetTrendsAsync(TimeSpan.FromHours(24));

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetTrendsAsync_WithLicense_ReturnsTrendData()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.DeduplicationMetrics))
            .Returns(true);

        // Act
        var result = await _sut.GetTrendsAsync(TimeSpan.FromHours(24));

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, trend => Assert.True(trend.Timestamp <= DateTimeOffset.UtcNow));
    }

    #endregion
}
