# Changelog: v0.5.9g – Batch Retroactive Deduplication

**Feature ID:** RAG-DEDUP-07  
**Version:** 0.5.9g  
**Date:** 2026-02-04  
**Status:** ✅ Complete

---

## Overview

This release implements **Batch Retroactive Deduplication**, a background job system for processing existing chunks in bulk to identify and merge historical duplicates. The feature provides resumable jobs with progress tracking, dry-run preview mode, checkpoint-based recovery, and Teams license gating.

---

## What's New

### 🆕 New Features

#### IBatchDeduplicationJob Interface
A comprehensive interface for managing batch deduplication jobs:

- **ExecuteAsync**: Runs a new batch job with configurable options and progress reporting
- **GetStatusAsync**: Retrieves current status of any job by ID
- **CancelAsync**: Gracefully cancels a running job with checkpoint save
- **ResumeAsync**: Resumes a paused or interrupted job from its last checkpoint
- **ListJobsAsync**: Lists job history with filtering by state, project, and date range

#### BatchDeduplicationOptions Record
Immutable configuration for controlling batch job behavior:

- **SimilarityThreshold** (default: 0.90): Minimum similarity score for duplicate consideration
- **BatchSize** (default: 100): Chunks processed per batch (valid: 10-1000)
- **BatchDelayMs** (default: 100): Throttling delay between batches (valid: 0-5000)
- **MaxChunks** (default: unlimited): Optional limit on total chunks to process
- **DryRun** (default: false): Preview mode that analyzes without making changes
- **ProjectId**: Optional scope limiter for project-specific processing
- **RequireLlmConfirmation**: Force LLM classification for all pairs
- **EnableContradictionDetection**: Flag contradictory chunks for review
- **AutoMergeConfidenceThreshold**: Minimum confidence for auto-merge decisions
- **CheckpointFrequency**: Chunks between checkpoint saves

#### BatchProgress Record
Real-time progress reporting with:

- **ChunksProcessed / TotalChunks**: Current and total counts
- **PercentComplete**: Calculated completion percentage (0.0-1.0)
- **Elapsed / EstimatedRemaining**: Timing information
- **CurrentOperation**: Human-readable status description
- **Factory Methods**: `Initial`, `Create`, `Completed`

#### BatchDeduplicationResult Record
Comprehensive job result with statistics:

- **JobId / FinalState**: Job identification and terminal state
- **ChunksProcessed / DuplicatesFound**: Processing counts
- **MergedCount / LinkedCount / ContradictionsFound**: Action counts
- **QueuedForReview / ErrorCount / SkippedCount**: Secondary counts
- **Duration / BytesSaved**: Performance metrics
- **IsSuccess / IsCancelled / IsFailed**: State helper properties
- **Factory Methods**: `Success`, `Cancelled`, `Failed`, `LicenseError`

#### BatchJobStatus Record
Complete job status snapshot for status queries:

- All progress counters and configuration
- **IsRunning / IsTerminal / CanResume**: State helpers
- **PercentComplete / Elapsed**: Calculated properties
- **LastCheckpoint**: Resume point for interrupted jobs

#### BatchJobFilter Record
Flexible job history filtering:

- **ProjectId / States / CreatedAfter / CreatedBefore / Limit**: Filter criteria
- **Static Factories**: `All`, `Running`, `Resumable`, `Failed`, `ForProject`

#### BatchJobRecord Record
Lightweight job summary for history listings:

- Essential job metadata (ID, state, project, label)
- **Duration** calculation from start/completion times
- **IsSuccess** helper property

#### MediatR Events
Three notification types for system integration:

- **BatchDeduplicationCompletedEvent**: Published when any job completes (success, cancelled, or failed)
- **BatchDeduplicationCancelledEvent**: Published on explicit cancellation with resume capability info
- **BatchDeduplicationProgressEvent**: Published periodically during execution with milestone detection

#### Database Migration
- **`Migration_012_BatchDeduplicationJobs`**: Creates job tracking infrastructure
  - `BatchDedupJobs` table for job records and state
  - `BatchDedupProcessed` table for tracking processed chunk pairs
  - Indexes for efficient status queries and resume operations

### 🔒 License Gating

- **Teams Tier Required**: All batch deduplication operations require Teams license
- **Feature Code**: `RAG.Dedup.BatchRetroactive` / `FeatureCodes.BatchDeduplication`
- **Graceful Degradation**: Returns `LicenseError` result for unlicensed users

---

## Technical Details

### Processing Algorithm

```
1. Validate license and options
2. Acquire project lock (prevents concurrent jobs on same scope)
3. Count total chunks in scope
4. Create job record in database
5. For each batch:
   a. Load next batch of unprocessed chunks
   b. For each chunk, find similar candidates
   c. Classify relationships (ISimilarityDetector -> IRelationshipClassifier)
   d. Process results:
      - Duplicates: Merge via ICanonicalManager
      - Complementary: Link as related
      - Contradictory: Flag via IContradictionService
      - Distinct: Skip
   e. Record processed pairs
   f. Publish progress event
   g. Save checkpoint if at frequency boundary
   h. Apply throttle delay
6. Publish completion event
7. Return final result
```

### Checkpoint & Resume

Jobs are resumable after interruption:

1. **Checkpoint Data**: Last processed chunk ID, all counters, elapsed time
2. **Checkpoint Frequency**: Configurable (default: every 500 chunks)
3. **Resume Detection**: Checks `BatchDedupJobs` for paused state
4. **Skip Already-Processed**: Uses `BatchDedupProcessed` to avoid reprocessing

### Concurrency Control

- **Project-Level Locking**: Only one batch job per project scope at a time
- **Global-Level Option**: Pass `null` ProjectId for cross-project deduplication
- **Lock Implementation**: Uses `SemaphoreSlim` per project ID

### Throttling Strategy

| Option | Purpose | Default |
|--------|---------|---------|
| BatchSize | Chunks per database transaction | 100 |
| BatchDelayMs | Pause between batches | 100ms |
| Priority | Job ordering (not yet implemented) | 50 |

---

## Files Changed

### New Files

| File | Description |
|------|-------------|
| `Lexichord.Abstractions/Contracts/RAG/BatchJobState.cs` | Job lifecycle state enum |
| `Lexichord.Abstractions/Contracts/RAG/BatchDeduplicationOptions.cs` | Job configuration record |
| `Lexichord.Abstractions/Contracts/RAG/BatchProgress.cs` | Progress reporting record |
| `Lexichord.Abstractions/Contracts/RAG/BatchDeduplicationResult.cs` | Job result with statistics |
| `Lexichord.Abstractions/Contracts/RAG/BatchJobStatus.cs` | Full job status snapshot |
| `Lexichord.Abstractions/Contracts/RAG/BatchJobFilter.cs` | Job history filter record |
| `Lexichord.Abstractions/Contracts/RAG/BatchJobRecord.cs` | Lightweight job summary |
| `Lexichord.Abstractions/Contracts/RAG/IBatchDeduplicationJob.cs` | Service interface |
| `Lexichord.Abstractions/Events/BatchDeduplicationCompletedEvent.cs` | Completion notification |
| `Lexichord.Abstractions/Events/BatchDeduplicationCancelledEvent.cs` | Cancellation notification |
| `Lexichord.Abstractions/Events/BatchDeduplicationProgressEvent.cs` | Progress notification |
| `Lexichord.Infrastructure/Migrations/Migration_012_BatchDeduplicationJobs.cs` | Database migration |
| `Lexichord.Modules.RAG/Services/BatchDeduplicationJob.cs` | Service implementation (~800 lines) |
| `Lexichord.Tests.Unit/Abstractions/RAG/BatchDeduplicationContractsTests.cs` | Contract unit tests (35 tests) |
| `Lexichord.Tests.Unit/Abstractions/Events/BatchDeduplicationEventsTests.cs` | Event unit tests (14 tests) |
| `Lexichord.Tests.Unit/Modules/RAG/Services/BatchDeduplicationJobTests.cs` | Service unit tests (10 tests) |

### Modified Files

| File | Change |
|------|--------|
| `Lexichord.Abstractions/Constants/FeatureCodes.cs` | Added `BatchDeduplication` constant |
| `Lexichord.Modules.RAG/RAGModule.cs` | Registered `IBatchDeduplicationJob` |

---

## Testing

### Unit Tests Added

**BatchDeduplicationContractsTests** (35 tests):
- BatchJobState enum value verification
- BatchDeduplicationOptions default and custom values
- BatchProgress factory methods and calculations
- BatchDeduplicationResult factory methods (Success, Cancelled, Failed)
- BatchJobStatus terminal state and resume capability
- BatchJobFilter static factories and custom construction
- BatchJobRecord duration calculation and success detection

**BatchDeduplicationEventsTests** (14 tests):
- BatchDeduplicationCompletedEvent construction and properties
- BatchDeduplicationCancelledEvent construction and resume flag
- BatchDeduplicationProgressEvent milestone detection (25%, 50%, 75%, 100%)
- Progress tracking across multiple events

**BatchDeduplicationJobTests** (10 tests):
- Constructor null argument validation for all 8 dependencies
- License gating for ExecuteAsync and ResumeAsync
- Options validation (SimilarityThreshold, BatchSize, BatchDelayMs ranges)

### Test Coverage

- All 59 new batch deduplication tests pass ✅
- No regressions in existing test suites ✅
- Build succeeds with zero warnings ✅

---

## Dependencies

### Prerequisites

- v0.5.9a: ISimilarityDetector ✅
- v0.5.9b: IRelationshipClassifier ✅
- v0.5.9c: ICanonicalManager ✅
- v0.5.9d: IDeduplicationService ✅
- v0.5.9e: IContradictionService ✅
- v0.5.9f: SearchSimilarWithDeduplicationAsync ✅

### Database

- Requires `Migration_012_BatchDeduplicationJobs` to be applied
- Depends on all previous v0.5.9 migrations (009-011)

### License

- **Teams tier** required for all batch operations
- Checked at method entry via `ILicenseContext.IsFeatureEnabled`

---

## Migration Notes

### Applying the Migration

```bash
dotnet run --project src/Lexichord.Host -- migrate up
```

### Rollback

```bash
dotnet run --project src/Lexichord.Host -- migrate down --version 12
```

---

## API Reference

### IBatchDeduplicationJob

```csharp
public interface IBatchDeduplicationJob
{
    /// <summary>Executes a new batch deduplication job.</summary>
    Task<BatchDeduplicationResult> ExecuteAsync(
        BatchDeduplicationOptions? options = null,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>Gets the current status of a job.</summary>
    Task<BatchJobStatus?> GetStatusAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Cancels a running job.</summary>
    Task<bool> CancelAsync(Guid jobId, CancellationToken ct = default);

    /// <summary>Resumes a paused job from its last checkpoint.</summary>
    Task<BatchDeduplicationResult> ResumeAsync(
        Guid jobId,
        IProgress<BatchProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>Lists jobs matching the filter criteria.</summary>
    Task<IReadOnlyList<BatchJobRecord>> ListJobsAsync(
        BatchJobFilter? filter = null,
        CancellationToken ct = default);
}
```

### BatchDeduplicationOptions

```csharp
public record BatchDeduplicationOptions
{
    public static BatchDeduplicationOptions Default { get; }
    
    public Guid? ProjectId { get; init; }
    public bool DryRun { get; init; }
    public float SimilarityThreshold { get; init; } = 0.90f;
    public int BatchSize { get; init; } = 100;
    public int BatchDelayMs { get; init; } = 100;
    public int MaxChunks { get; init; } = 0;
    public bool RequireLlmConfirmation { get; init; }
    public bool EnableContradictionDetection { get; init; } = true;
    public int Priority { get; init; } = 50;
    public string? Label { get; init; }
    public float AutoMergeConfidenceThreshold { get; init; } = 0.70f;
    public int CheckpointFrequency { get; init; } = 500;
}
```

### Usage Example

```csharp
// Standard batch job with progress reporting
var progress = new Progress<BatchProgress>(p => 
    Console.WriteLine($"Progress: {p.PercentComplete:P1} - {p.CurrentOperation}"));

var result = await batchJob.ExecuteAsync(
    new BatchDeduplicationOptions 
    { 
        ProjectId = myProjectId,
        SimilarityThreshold = 0.92f 
    },
    progress);

// Dry-run preview
var previewResult = await batchJob.ExecuteAsync(
    new BatchDeduplicationOptions { DryRun = true });

Console.WriteLine($"Would merge {previewResult.MergedCount} chunks");

// List failed jobs
var failedJobs = await batchJob.ListJobsAsync(BatchJobFilter.Failed);

// Resume an interrupted job
if (status.CanResume)
{
    var resumeResult = await batchJob.ResumeAsync(status.JobId);
}
```

---

**Author:** Lexichord Team  
**Reviewed:** Automated CI  
**Approved:** 2026-02-04
