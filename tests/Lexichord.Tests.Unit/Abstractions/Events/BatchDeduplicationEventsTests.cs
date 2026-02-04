// =============================================================================
// File: BatchDeduplicationEventsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for batch deduplication MediatR events.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Events;

/// <summary>
/// Unit tests for batch deduplication MediatR events.
/// </summary>
public sealed class BatchDeduplicationEventsTests
{
    #region BatchDeduplicationCompletedEvent Tests

    [Fact]
    public void BatchDeduplicationCompletedEvent_CanBeConstructed()
    {
        var result = BatchDeduplicationResult.Success(
            Guid.NewGuid(), 100, 10, 8, 2, 0, 0, 0, TimeSpan.FromMinutes(5), 5000);

        var ev = new BatchDeduplicationCompletedEvent(result);

        Assert.Equal(result.JobId, ev.JobId);
        Assert.True(ev.IsSuccess);
        Assert.False(ev.IsCancelled);
        Assert.False(ev.IsFailed);
    }

    [Fact]
    public void BatchDeduplicationCompletedEvent_ImplementsINotification()
    {
        var result = BatchDeduplicationResult.Success(
            Guid.NewGuid(), 100, 10, 8, 2, 0, 0, 0, TimeSpan.FromMinutes(5), 5000);

        var ev = new BatchDeduplicationCompletedEvent(result);

        Assert.IsAssignableFrom<MediatR.INotification>(ev);
    }

    [Fact]
    public void BatchDeduplicationCompletedEvent_WithSuccess_HasCorrectProperties()
    {
        var jobId = Guid.NewGuid();
        var result = BatchDeduplicationResult.Success(
            jobId, 100, 10, 8, 2, 0, 0, 0, TimeSpan.FromMinutes(5), 5000);

        var ev = new BatchDeduplicationCompletedEvent(result);

        Assert.Equal(jobId, ev.JobId);
        Assert.True(ev.IsSuccess);
        Assert.False(ev.IsCancelled);
        Assert.False(ev.IsFailed);
        Assert.Equal(result, ev.Result);
    }

    [Fact]
    public void BatchDeduplicationCompletedEvent_WithCancelled_HasCorrectProperties()
    {
        var jobId = Guid.NewGuid();
        var result = BatchDeduplicationResult.Cancelled(
            jobId, 50, 5, 4, 1, 0, 0, TimeSpan.FromMinutes(2), 2000);

        var ev = new BatchDeduplicationCompletedEvent(result);

        Assert.Equal(jobId, ev.JobId);
        Assert.False(ev.IsSuccess);
        Assert.True(ev.IsCancelled);
        Assert.False(ev.IsFailed);
    }

    [Fact]
    public void BatchDeduplicationCompletedEvent_WithFailed_HasCorrectProperties()
    {
        var jobId = Guid.NewGuid();
        var result = BatchDeduplicationResult.Failed(
            jobId, 25, 5, 3, 1, 0, 0, 1, TimeSpan.FromMinutes(1), 1000, "Database error");

        var ev = new BatchDeduplicationCompletedEvent(result);

        Assert.Equal(jobId, ev.JobId);
        Assert.False(ev.IsSuccess);
        Assert.False(ev.IsCancelled);
        Assert.True(ev.IsFailed);
    }

    #endregion

    #region BatchDeduplicationCancelledEvent Tests

    [Fact]
    public void BatchDeduplicationCancelledEvent_CanBeConstructed()
    {
        var jobId = Guid.NewGuid();
        var chunksProcessed = 50;
        var cancelledAt = DateTime.UtcNow;
        var canResume = true;

        var ev = new BatchDeduplicationCancelledEvent(jobId, chunksProcessed, cancelledAt, canResume);

        Assert.Equal(jobId, ev.JobId);
        Assert.Equal(chunksProcessed, ev.ChunksProcessedBeforeCancellation);
        Assert.Equal(cancelledAt, ev.CancelledAt);
        Assert.True(ev.CanResume);
    }

    [Fact]
    public void BatchDeduplicationCancelledEvent_ImplementsINotification()
    {
        var ev = new BatchDeduplicationCancelledEvent(
            Guid.NewGuid(), 50, DateTime.UtcNow, true);

        Assert.IsAssignableFrom<MediatR.INotification>(ev);
    }

    [Fact]
    public void BatchDeduplicationCancelledEvent_CanResume_SetToFalse()
    {
        var ev = new BatchDeduplicationCancelledEvent(
            Guid.NewGuid(), 100, DateTime.UtcNow, false);

        Assert.False(ev.CanResume);
    }

    #endregion

    #region BatchDeduplicationProgressEvent Tests

    [Fact]
    public void BatchDeduplicationProgressEvent_CanBeConstructed()
    {
        var jobId = Guid.NewGuid();
        var progress = BatchProgress.Create(50, 100, TimeSpan.FromMinutes(5), "Processing");
        var mergedInBatch = 3;
        var totalMerged = 15;

        var ev = new BatchDeduplicationProgressEvent(jobId, progress, mergedInBatch, totalMerged);

        Assert.Equal(jobId, ev.JobId);
        Assert.Equal(progress, ev.Progress);
        Assert.Equal(mergedInBatch, ev.MergedInThisBatch);
        Assert.Equal(totalMerged, ev.TotalMergedSoFar);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_ImplementsINotification()
    {
        var ev = new BatchDeduplicationProgressEvent(
            Guid.NewGuid(),
            BatchProgress.Initial(100),
            0,
            0);

        Assert.IsAssignableFrom<MediatR.INotification>(ev);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_PercentComplete_ReflectsProgress()
    {
        var progress = BatchProgress.Create(50, 100, TimeSpan.FromMinutes(5), "Processing");

        var ev = new BatchDeduplicationProgressEvent(
            Guid.NewGuid(), progress, 3, 15);

        Assert.NotNull(ev.PercentComplete);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_IsMilestone_TrueAt25Percent()
    {
        var progress = BatchProgress.Create(25, 100, TimeSpan.FromMinutes(2), "Processing");

        var ev = new BatchDeduplicationProgressEvent(
            Guid.NewGuid(), progress, 2, 8);

        Assert.True(ev.IsMilestone);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_IsMilestone_TrueAt50Percent()
    {
        var progress = BatchProgress.Create(50, 100, TimeSpan.FromMinutes(5), "Processing");

        var ev = new BatchDeduplicationProgressEvent(
            Guid.NewGuid(), progress, 3, 15);

        Assert.True(ev.IsMilestone);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_IsMilestone_TrueAt75Percent()
    {
        var progress = BatchProgress.Create(75, 100, TimeSpan.FromMinutes(7), "Processing");

        var ev = new BatchDeduplicationProgressEvent(
            Guid.NewGuid(), progress, 2, 22);

        Assert.True(ev.IsMilestone);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_IsMilestone_FalseAtOtherPercentages()
    {
        var progress = BatchProgress.Create(60, 100, TimeSpan.FromMinutes(6), "Processing");

        var ev = new BatchDeduplicationProgressEvent(
            Guid.NewGuid(), progress, 2, 18);

        Assert.False(ev.IsMilestone);
    }

    [Fact]
    public void BatchDeduplicationProgressEvent_TracksProgressOverTime()
    {
        var jobId = Guid.NewGuid();

        // Simulate progress events over time
        var events = new List<BatchDeduplicationProgressEvent>
        {
            new(jobId, BatchProgress.Create(100, 1000, TimeSpan.FromMinutes(1), "Processing"), 5, 5),
            new(jobId, BatchProgress.Create(200, 1000, TimeSpan.FromMinutes(2), "Processing"), 3, 8),
            new(jobId, BatchProgress.Create(300, 1000, TimeSpan.FromMinutes(3), "Processing"), 8, 16),
        };

        // Verify progression
        Assert.Equal(100, events[0].Progress.ChunksProcessed);
        Assert.Equal(200, events[1].Progress.ChunksProcessed);
        Assert.Equal(300, events[2].Progress.ChunksProcessed);

        // Verify merges accumulate
        Assert.Equal(5, events[0].TotalMergedSoFar);
        Assert.Equal(8, events[1].TotalMergedSoFar);
        Assert.Equal(16, events[2].TotalMergedSoFar);

        // All events should have the same job ID
        Assert.All(events, e => Assert.Equal(jobId, e.JobId));
    }

    #endregion
}
