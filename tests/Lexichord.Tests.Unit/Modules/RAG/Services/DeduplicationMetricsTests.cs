// =============================================================================
// File: DeduplicationMetricsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for DeduplicationMetrics static class.
// =============================================================================
// VERSION: v0.5.9h (Hardening & Metrics)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Metrics;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="DeduplicationMetrics"/>.
/// </summary>
public sealed class DeduplicationMetricsTests : IDisposable
{
    public DeduplicationMetricsTests()
    {
        // Reset static metrics before each test.
        DeduplicationMetrics.Reset();
    }

    public void Dispose()
    {
        // Clean up static state after each test.
        DeduplicationMetrics.Reset();
    }

    #region RecordChunkProcessed Tests

    [Fact]
    public void RecordChunkProcessed_StoredAsNew_IncrementsCounter()
    {
        // Arrange
        var initialProcessed = DeduplicationMetrics.ChunksProcessedTotal;
        var initialStoredNew = DeduplicationMetrics.ChunksStoredAsNew;

        // Act
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.StoredAsNew, 10.0);

        // Assert
        Assert.Equal(initialProcessed + 1, DeduplicationMetrics.ChunksProcessedTotal);
        Assert.Equal(initialStoredNew + 1, DeduplicationMetrics.ChunksStoredAsNew);
    }

    [Fact]
    public void RecordChunkProcessed_MergedIntoExisting_IncrementsCounter()
    {
        // Arrange
        var initialMerged = DeduplicationMetrics.ChunksMerged;

        // Act
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.MergedIntoExisting, 15.0);

        // Assert
        Assert.Equal(initialMerged + 1, DeduplicationMetrics.ChunksMerged);
    }

    [Fact]
    public void RecordChunkProcessed_LinkedToExisting_IncrementsCounter()
    {
        // Arrange
        var initialLinked = DeduplicationMetrics.ChunksLinked;

        // Act
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.LinkedToExisting, 12.0);

        // Assert
        Assert.Equal(initialLinked + 1, DeduplicationMetrics.ChunksLinked);
    }

    [Fact]
    public void RecordChunkProcessed_FlaggedAsContradiction_IncrementsCounter()
    {
        // Arrange
        var initialFlagged = DeduplicationMetrics.ChunksFlaggedContradiction;

        // Act
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.FlaggedAsContradiction, 20.0);

        // Assert
        Assert.Equal(initialFlagged + 1, DeduplicationMetrics.ChunksFlaggedContradiction);
    }

    [Fact]
    public void RecordChunkProcessed_QueuedForReview_IncrementsCounter()
    {
        // Arrange
        var initialQueued = DeduplicationMetrics.ChunksQueuedForReview;

        // Act
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.QueuedForReview, 25.0);

        // Assert
        Assert.Equal(initialQueued + 1, DeduplicationMetrics.ChunksQueuedForReview);
    }

    [Fact]
    public void RecordChunkProcessed_SupersededExisting_IncrementsTotalOnly()
    {
        // Arrange
        var initialProcessed = DeduplicationMetrics.ChunksProcessedTotal;

        // Act
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.SupersededExisting, 30.0);

        // Assert - only total is incremented for SupersededExisting
        Assert.Equal(initialProcessed + 1, DeduplicationMetrics.ChunksProcessedTotal);
    }

    [Fact]
    public async Task RecordChunkProcessed_IsThreadSafe()
    {
        // Arrange
        const int iterations = 1000;
        var tasks = new List<Task>();

        // Act
        for (var i = 0; i < iterations; i++)
        {
            tasks.Add(Task.Run(() =>
                DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.StoredAsNew, 1.0)));
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.Equal(iterations, DeduplicationMetrics.ChunksProcessedTotal);
        Assert.Equal(iterations, DeduplicationMetrics.ChunksStoredAsNew);
    }

    #endregion

    #region RecordSimilarityQuery Tests

    [Fact]
    public void RecordSimilarityQuery_IncrementsTotalCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.SimilarityQueriesTotal;

        // Act
        DeduplicationMetrics.RecordSimilarityQuery(5.0, 3);

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.SimilarityQueriesTotal);
    }

    [Fact]
    public void RecordSimilarityQuery_AccumulatesMatchCount()
    {
        // Arrange
        var initialMatches = DeduplicationMetrics.SimilarityMatchesTotal;

        // Act
        DeduplicationMetrics.RecordSimilarityQuery(5.0, 5);
        DeduplicationMetrics.RecordSimilarityQuery(10.0, 3);

        // Assert
        Assert.Equal(initialMatches + 8, DeduplicationMetrics.SimilarityMatchesTotal);
    }

    [Fact]
    public void RecordSimilarityQuery_WithZeroMatches_OnlyIncrementsQueryCount()
    {
        // Arrange
        var initialMatches = DeduplicationMetrics.SimilarityMatchesTotal;

        // Act
        DeduplicationMetrics.RecordSimilarityQuery(5.0, 0);

        // Assert
        Assert.Equal(initialMatches, DeduplicationMetrics.SimilarityMatchesTotal);
        Assert.Equal(1, DeduplicationMetrics.SimilarityQueriesTotal);
    }

    #endregion

    #region RecordClassification Tests

    [Fact]
    public void RecordClassification_IncrementsTotalCounter()
    {
        // Arrange
        var initialTotal = DeduplicationMetrics.ClassificationRequestsTotal;

        // Act
        DeduplicationMetrics.RecordClassification(ClassificationMethod.RuleBased, 5.0);
        DeduplicationMetrics.RecordClassification(ClassificationMethod.LlmBased, 100.0);
        DeduplicationMetrics.RecordClassification(ClassificationMethod.Cached, 0.5);

        // Assert
        Assert.Equal(initialTotal + 3, DeduplicationMetrics.ClassificationRequestsTotal);
    }

    #endregion

    #region RecordContradictionDetected Tests

    [Fact]
    public void RecordContradictionDetected_IncrementsTotalCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ContradictionsDetectedTotal;

        // Act
        DeduplicationMetrics.RecordContradictionDetected(ContradictionSeverity.Medium);

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ContradictionsDetectedTotal);
    }

    [Theory]
    [InlineData(ContradictionSeverity.Low)]
    [InlineData(ContradictionSeverity.Medium)]
    [InlineData(ContradictionSeverity.High)]
    [InlineData(ContradictionSeverity.Critical)]
    public void RecordContradictionDetected_AllSeveritiesWork(ContradictionSeverity severity)
    {
        // Arrange
        var initialCount = DeduplicationMetrics.ContradictionsDetectedTotal;

        // Act
        DeduplicationMetrics.RecordContradictionDetected(severity);

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.ContradictionsDetectedTotal);
    }

    #endregion

    #region RecordProcessingError Tests

    [Fact]
    public void RecordProcessingError_IncrementsErrorCounter()
    {
        // Arrange
        var initialErrors = DeduplicationMetrics.ProcessingErrors;

        // Act
        DeduplicationMetrics.RecordProcessingError();
        DeduplicationMetrics.RecordProcessingError();

        // Assert
        Assert.Equal(initialErrors + 2, DeduplicationMetrics.ProcessingErrors);
    }

    #endregion

    #region RecordBatchJobCompleted Tests

    [Fact]
    public void RecordBatchJobCompleted_IncrementsCounter()
    {
        // Arrange
        var initialCount = DeduplicationMetrics.BatchJobsCompleted;

        // Act
        DeduplicationMetrics.RecordBatchJobCompleted();

        // Assert
        Assert.Equal(initialCount + 1, DeduplicationMetrics.BatchJobsCompleted);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllCounters()
    {
        // Arrange - record various metrics
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.StoredAsNew, 10.0);
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.MergedIntoExisting, 15.0);
        DeduplicationMetrics.RecordSimilarityQuery(5.0, 3);
        DeduplicationMetrics.RecordClassification(ClassificationMethod.RuleBased, 8.0);
        DeduplicationMetrics.RecordContradictionDetected(ContradictionSeverity.High);
        DeduplicationMetrics.RecordProcessingError();
        DeduplicationMetrics.RecordBatchJobCompleted();

        // Act
        DeduplicationMetrics.Reset();

        // Assert
        Assert.Equal(0, DeduplicationMetrics.ChunksProcessedTotal);
        Assert.Equal(0, DeduplicationMetrics.ChunksStoredAsNew);
        Assert.Equal(0, DeduplicationMetrics.ChunksMerged);
        Assert.Equal(0, DeduplicationMetrics.SimilarityQueriesTotal);
        Assert.Equal(0, DeduplicationMetrics.ClassificationRequestsTotal);
        Assert.Equal(0, DeduplicationMetrics.ContradictionsDetectedTotal);
        Assert.Equal(0, DeduplicationMetrics.ProcessingErrors);
        Assert.Equal(0, DeduplicationMetrics.BatchJobsCompleted);
    }

    #endregion

    #region Percentile Calculation Tests

    [Fact]
    public void GetProcessingDurationP99_WithNoSamples_ReturnsZero()
    {
        // Assert
        Assert.Equal(0, DeduplicationMetrics.GetProcessingDurationP99());
    }

    [Fact]
    public void GetProcessingDurationP99_WithSamples_ReturnsP99Value()
    {
        // Arrange - add samples
        for (var i = 1; i <= 100; i++)
        {
            DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.StoredAsNew, i);
        }

        // Act
        var p99 = DeduplicationMetrics.GetProcessingDurationP99();

        // Assert - P99 should be around 99 for sequential 1-100 values
        Assert.True(p99 >= 95 && p99 <= 100);
    }

    [Fact]
    public void GetAverageProcessingDuration_WithNoSamples_ReturnsZero()
    {
        // Assert
        Assert.Equal(0, DeduplicationMetrics.GetAverageProcessingDuration());
    }

    [Fact]
    public void GetAverageProcessingDuration_WithSamples_ReturnsAverage()
    {
        // Arrange - add samples: 10, 20, 30
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.StoredAsNew, 10.0);
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.MergedIntoExisting, 20.0);
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.LinkedToExisting, 30.0);

        // Act
        var average = DeduplicationMetrics.GetAverageProcessingDuration();

        // Assert - average of 10, 20, 30 is 20
        Assert.Equal(20.0, average);
    }

    [Fact]
    public void GetDeduplicationRate_WithNoChunks_ReturnsZero()
    {
        // Assert
        Assert.Equal(0.0, DeduplicationMetrics.GetDeduplicationRate());
    }

    [Fact]
    public void GetDeduplicationRate_WithMixedActions_ReturnsCorrectRate()
    {
        // Arrange - 10 chunks: 6 stored new, 3 merged, 1 linked
        for (var i = 0; i < 6; i++)
            DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.StoredAsNew, 10.0);
        for (var i = 0; i < 3; i++)
            DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.MergedIntoExisting, 10.0);
        DeduplicationMetrics.RecordChunkProcessed(DeduplicationAction.LinkedToExisting, 10.0);

        // Act
        var rate = DeduplicationMetrics.GetDeduplicationRate();

        // Assert - 4 out of 10 = 40%
        Assert.Equal(40.0, rate);
    }

    #endregion
}
