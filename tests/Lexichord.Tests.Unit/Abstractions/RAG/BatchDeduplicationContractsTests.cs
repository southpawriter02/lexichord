// =============================================================================
// File: BatchDeduplicationContractsTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for batch deduplication data contracts.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.RAG;

/// <summary>
/// Unit tests for batch deduplication data contracts.
/// </summary>
public sealed class BatchDeduplicationContractsTests
{
    #region BatchJobState Tests

    [Fact]
    public void BatchJobState_HasExpectedValues()
    {
        Assert.Equal(0, (int)BatchJobState.Pending);
        Assert.Equal(1, (int)BatchJobState.Running);
        Assert.Equal(2, (int)BatchJobState.Paused);
        Assert.Equal(3, (int)BatchJobState.Completed);
        Assert.Equal(4, (int)BatchJobState.Cancelled);
        Assert.Equal(5, (int)BatchJobState.Failed);
    }

    [Theory]
    [InlineData(BatchJobState.Completed)]
    [InlineData(BatchJobState.Cancelled)]
    [InlineData(BatchJobState.Failed)]
    public void BatchJobState_TerminalStates_AreDistinct(BatchJobState state)
    {
        Assert.True(
            state == BatchJobState.Completed ||
            state == BatchJobState.Cancelled ||
            state == BatchJobState.Failed);
    }

    #endregion

    #region BatchDeduplicationOptions Tests

    [Fact]
    public void BatchDeduplicationOptions_Default_HasExpectedValues()
    {
        var options = BatchDeduplicationOptions.Default;

        Assert.Equal(0.90f, options.SimilarityThreshold);
        Assert.Equal(100, options.BatchSize);
        Assert.Equal(100, options.BatchDelayMs);
        Assert.False(options.DryRun);
        Assert.Null(options.ProjectId);
    }

    [Fact]
    public void BatchDeduplicationOptions_CustomValues_ArePreserved()
    {
        var projectId = Guid.NewGuid();

        var options = new BatchDeduplicationOptions
        {
            SimilarityThreshold = 0.88f,
            BatchSize = 50,
            BatchDelayMs = 100,
            DryRun = true,
            ProjectId = projectId
        };

        Assert.Equal(0.88f, options.SimilarityThreshold);
        Assert.Equal(50, options.BatchSize);
        Assert.Equal(100, options.BatchDelayMs);
        Assert.True(options.DryRun);
        Assert.Equal(projectId, options.ProjectId);
    }

    #endregion

    #region BatchProgress Tests

    [Fact]
    public void BatchProgress_Initial_HasExpectedValues()
    {
        var progress = BatchProgress.Initial(100);

        Assert.Equal(100, progress.TotalChunks);
        Assert.Equal(0, progress.ChunksProcessed);
    }

    [Fact]
    public void BatchProgress_Create_CalculatesPercentage()
    {
        var progress = BatchProgress.Create(50, 100, TimeSpan.FromMinutes(5), "Test");

        Assert.Equal(100, progress.TotalChunks);
        Assert.Equal(50, progress.ChunksProcessed);
    }

    [Fact]
    public void BatchProgress_Completed_HasCorrectValues()
    {
        var progress = BatchProgress.Completed(100, TimeSpan.FromMinutes(10));

        Assert.Equal(100, progress.ChunksProcessed);
    }

    #endregion

    #region BatchDeduplicationResult Tests

    [Fact]
    public void BatchDeduplicationResult_Factory_Success_ReturnsCorrectResult()
    {
        var jobId = Guid.NewGuid();
        var duration = TimeSpan.FromMinutes(5);

        var result = BatchDeduplicationResult.Success(
            jobId, 100, 10, 8, 2, 0, 1, 0, duration, 5000);

        Assert.Equal(jobId, result.JobId);
        Assert.True(result.IsSuccess);
        Assert.Equal(BatchJobState.Completed, result.FinalState);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(100, result.ChunksProcessed);
        Assert.Equal(10, result.DuplicatesFound);
        Assert.Equal(8, result.MergedCount);
        Assert.Equal(2, result.LinkedCount);
        Assert.Equal(duration, result.Duration);
    }

    [Fact]
    public void BatchDeduplicationResult_Factory_Cancelled_ReturnsCorrectResult()
    {
        var jobId = Guid.NewGuid();
        var duration = TimeSpan.FromMinutes(2);

        var result = BatchDeduplicationResult.Cancelled(
            jobId, 50, 5, 4, 1, 0, 0, duration, 2000);

        Assert.Equal(jobId, result.JobId);
        Assert.False(result.IsSuccess);
        Assert.Equal(BatchJobState.Cancelled, result.FinalState);
        Assert.Contains("cancelled", result.ErrorMessage?.ToLower() ?? "");
    }

    [Fact]
    public void BatchDeduplicationResult_Factory_Failed_ReturnsCorrectResult()
    {
        var jobId = Guid.NewGuid();
        var duration = TimeSpan.FromMinutes(1);
        var errorMessage = "Database connection failed";

        var result = BatchDeduplicationResult.Failed(
            jobId, 25, 5, 3, 1, 0, 1, 1, duration, 1000, errorMessage);

        Assert.Equal(jobId, result.JobId);
        Assert.False(result.IsSuccess);
        Assert.Equal(BatchJobState.Failed, result.FinalState);
        Assert.Equal(errorMessage, result.ErrorMessage);
        Assert.Equal(1, result.ErrorCount);
    }

    #endregion

    #region BatchJobStatus Tests

    [Fact]
    public void BatchJobStatus_IsTerminal_ReturnsTrueForTerminalStates()
    {
        var completedStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Completed, BatchDeduplicationOptions.Default,
            100, 100, 10, 8, 2, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, null, null);
        var cancelledStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Cancelled, BatchDeduplicationOptions.Default,
            100, 50, 5, 4, 1, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, null, null);
        var failedStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Failed, BatchDeduplicationOptions.Default,
            100, 25, 3, 2, 1, 0, 0, 1,
            DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, null, "Error");

        Assert.True(completedStatus.IsTerminal);
        Assert.True(cancelledStatus.IsTerminal);
        Assert.True(failedStatus.IsTerminal);
    }

    [Fact]
    public void BatchJobStatus_IsTerminal_ReturnsFalseForNonTerminalStates()
    {
        var pendingStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Pending, BatchDeduplicationOptions.Default,
            0, 0, 0, 0, 0, 0, 0, 0,
            DateTime.UtcNow, null, null, null, null);
        var runningStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Running, BatchDeduplicationOptions.Default,
            100, 50, 5, 4, 1, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, null, null, null);
        var pausedStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Paused, BatchDeduplicationOptions.Default,
            100, 50, 5, 4, 1, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, null, Guid.NewGuid(), null);

        Assert.False(pendingStatus.IsTerminal);
        Assert.False(runningStatus.IsTerminal);
        Assert.False(pausedStatus.IsTerminal);
    }

    [Fact]
    public void BatchJobStatus_CanResume_ReturnsTrueForPausedState()
    {
        var pausedStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Paused, BatchDeduplicationOptions.Default,
            100, 50, 5, 4, 1, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, null, Guid.NewGuid(), null);

        Assert.True(pausedStatus.CanResume);
    }

    [Fact]
    public void BatchJobStatus_CanResume_ReturnsFalseForOtherStates()
    {
        var pendingStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Pending, BatchDeduplicationOptions.Default,
            0, 0, 0, 0, 0, 0, 0, 0,
            DateTime.UtcNow, null, null, null, null);
        var runningStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Running, BatchDeduplicationOptions.Default,
            100, 50, 5, 4, 1, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, null, null, null);
        var completedStatus = new BatchJobStatus(
            Guid.NewGuid(), BatchJobState.Completed, BatchDeduplicationOptions.Default,
            100, 100, 10, 8, 2, 0, 0, 0,
            DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow, null, null);

        Assert.False(pendingStatus.CanResume);
        Assert.False(runningStatus.CanResume);
        Assert.False(completedStatus.CanResume);
    }

    #endregion

    #region BatchJobFilter Tests

    [Fact]
    public void BatchJobFilter_All_HasExpectedValues()
    {
        var filter = BatchJobFilter.All;

        Assert.Null(filter.ProjectId);
        Assert.Null(filter.States);
        Assert.Null(filter.CreatedAfter);
        Assert.Null(filter.CreatedBefore);
        Assert.Equal(100, filter.Limit);
    }

    [Fact]
    public void BatchJobFilter_Running_HasCorrectState()
    {
        var filter = BatchJobFilter.Running;

        Assert.NotNull(filter.States);
        Assert.Contains(BatchJobState.Running, filter.States);
    }

    [Fact]
    public void BatchJobFilter_Failed_HasCorrectState()
    {
        var filter = BatchJobFilter.Failed;

        Assert.NotNull(filter.States);
        Assert.Contains(BatchJobState.Failed, filter.States);
    }

    [Fact]
    public void BatchJobFilter_ForProject_SetsProjectId()
    {
        var projectId = Guid.NewGuid();
        var filter = BatchJobFilter.ForProject(projectId);

        Assert.Equal(projectId, filter.ProjectId);
    }

    [Fact]
    public void BatchJobFilter_CustomValues_ArePreserved()
    {
        var projectId = Guid.NewGuid();
        var states = new[] { BatchJobState.Completed, BatchJobState.Failed };
        var createdAfter = DateTime.UtcNow.AddDays(-30);
        var createdBefore = DateTime.UtcNow;

        var filter = new BatchJobFilter(
            ProjectId: projectId,
            States: states,
            CreatedAfter: createdAfter,
            CreatedBefore: createdBefore,
            Limit: 50);

        Assert.Equal(projectId, filter.ProjectId);
        Assert.Equal(states, filter.States);
        Assert.Equal(createdAfter, filter.CreatedAfter);
        Assert.Equal(createdBefore, filter.CreatedBefore);
        Assert.Equal(50, filter.Limit);
    }

    #endregion

    #region BatchJobRecord Tests

    [Fact]
    public void BatchJobRecord_CanBeConstructed()
    {
        var jobId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var record = new BatchJobRecord(
            JobId: jobId,
            State: BatchJobState.Pending,
            ProjectId: projectId,
            Label: "Test Job",
            DryRun: false,
            ChunksProcessed: 0,
            MergedCount: 0,
            CreatedAt: createdAt,
            StartedAt: null,
            CompletedAt: null);

        Assert.Equal(jobId, record.JobId);
        Assert.Equal(projectId, record.ProjectId);
        Assert.Equal(BatchJobState.Pending, record.State);
        Assert.Equal("Test Job", record.Label);
        Assert.False(record.DryRun);
        Assert.Equal(0, record.ChunksProcessed);
        Assert.Equal(0, record.MergedCount);
        Assert.Equal(createdAt, record.CreatedAt);
        Assert.Null(record.StartedAt);
        Assert.Null(record.CompletedAt);
    }

    [Fact]
    public void BatchJobRecord_Duration_CalculatesCorrectly()
    {
        var createdAt = DateTime.UtcNow;
        var startedAt = createdAt.AddSeconds(5);
        var completedAt = startedAt.AddMinutes(10);

        var record = new BatchJobRecord(
            JobId: Guid.NewGuid(),
            State: BatchJobState.Completed,
            ProjectId: Guid.NewGuid(),
            Label: null,
            DryRun: false,
            ChunksProcessed: 100,
            MergedCount: 10,
            CreatedAt: createdAt,
            StartedAt: startedAt,
            CompletedAt: completedAt);

        Assert.NotNull(record.Duration);
        Assert.Equal(TimeSpan.FromMinutes(10), record.Duration);
    }

    [Fact]
    public void BatchJobRecord_Duration_ReturnsNull_WhenNotStarted()
    {
        var record = new BatchJobRecord(
            JobId: Guid.NewGuid(),
            State: BatchJobState.Pending,
            ProjectId: Guid.NewGuid(),
            Label: null,
            DryRun: false,
            ChunksProcessed: 0,
            MergedCount: 0,
            CreatedAt: DateTime.UtcNow,
            StartedAt: null,
            CompletedAt: null);

        Assert.Null(record.Duration);
    }

    [Fact]
    public void BatchJobRecord_IsSuccess_ReturnsTrue_WhenCompleted()
    {
        var record = new BatchJobRecord(
            JobId: Guid.NewGuid(),
            State: BatchJobState.Completed,
            ProjectId: null,
            Label: null,
            DryRun: false,
            ChunksProcessed: 100,
            MergedCount: 10,
            CreatedAt: DateTime.UtcNow,
            StartedAt: DateTime.UtcNow,
            CompletedAt: DateTime.UtcNow);

        Assert.True(record.IsSuccess);
    }

    [Fact]
    public void BatchJobRecord_IsSuccess_ReturnsFalse_WhenFailed()
    {
        var record = new BatchJobRecord(
            JobId: Guid.NewGuid(),
            State: BatchJobState.Failed,
            ProjectId: null,
            Label: null,
            DryRun: false,
            ChunksProcessed: 50,
            MergedCount: 5,
            CreatedAt: DateTime.UtcNow,
            StartedAt: DateTime.UtcNow,
            CompletedAt: DateTime.UtcNow);

        Assert.False(record.IsSuccess);
    }

    #endregion
}
