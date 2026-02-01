# Changelog: v0.4.2d - Ingestion Queue

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.2d](../../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2d.md)

---

## Summary

Implements a priority-based ingestion queue using `System.Threading.Channels` for thread-safe producer-consumer patterns. The queue supports priority ordering, duplicate detection with configurable windows, backpressure handling, and graceful shutdown. A background service continuously processes queued items through the ingestion pipeline.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/Ingestion/

| File                       | Description                                                          |
| :------------------------- | :------------------------------------------------------------------- |
| `IngestionQueueItem.cs`    | Record representing a queued file with priority and correlation ID   |
| `IngestionQueueOptions.cs` | Configuration record with capacity, throttling, and duplicate window |
| `IIngestionQueue.cs`       | Interface defining enqueue, dequeue, complete, and clear operations  |

#### Lexichord.Modules.RAG/Services/

| File                            | Description                                                        |
| :------------------------------ | :----------------------------------------------------------------- |
| `IngestionQueue.cs`             | Channel-based queue implementation with priority queue integration |
| `IngestionBackgroundService.cs` | BackgroundService that continuously processes queue items          |

### Modified

#### Lexichord.Modules.RAG/

| File                           | Description                                                      |
| :----------------------------- | :--------------------------------------------------------------- |
| `RAGModule.cs`                 | Added DI registration for queue, background service, and options |
| `Lexichord.Modules.RAG.csproj` | Added Microsoft.Extensions.Hosting.Abstractions package          |

### Unit Tests

#### Lexichord.Tests.Unit/Abstractions/Ingestion/

| File                            | Tests                                                |
| :------------------------------ | :--------------------------------------------------- |
| `IngestionQueueItemTests.cs`    | Factory methods, priority constants, record equality |
| `IngestionQueueOptionsTests.cs` | Defaults, presets, validation, equality              |

#### Lexichord.Tests.Unit/Modules/RAG/

| File                                 | Tests                                                       |
| :----------------------------------- | :---------------------------------------------------------- |
| `IngestionQueueTests.cs`             | Enqueue/dequeue, priority ordering, duplicates, threading   |
| `IngestionBackgroundServiceTests.cs` | Lifecycle, processing, error handling, throttling, shutdown |

---

## Technical Details

### Queue Architecture

```
Producers                    IngestionQueue                    IngestionBackgroundService
    │                             │                                      │
    │ EnqueueAsync(item)          │                                      │
    ├────────────────────────────>│                                      │
    │                             │ Channel + PriorityQueue              │
    │                             ├──────────────────────────────────────>│
    │                             │                     DequeueAsync()   │
    │                             │                                      │
    │                             │                                      ├──> IIngestionService
```

### Priority Levels

| Constant               | Value | Use Case                             |
| :--------------------- | :---- | :----------------------------------- |
| `PriorityUserAction`   | 0     | User-initiated indexing (highest)    |
| `PriorityRecentChange` | 1     | Files changed while workspace active |
| `PriorityNormal`       | 2     | Default/background indexing          |
| `PriorityBackground`   | 3     | Bulk re-indexing operations (lowest) |

### Duplicate Detection

- Uses `ConcurrentDictionary<string, DateTimeOffset>` for path tracking
- Paths are normalized (case-insensitive, forward slashes)
- Configurable duplicate window (default: 60 seconds)
- Periodic cleanup of stale entries via `Timer`

### IngestionQueueOptions Defaults

| Property                   | Default Value |
| :------------------------- | :------------ |
| `MaxQueueSize`             | 1000          |
| `ThrottleDelayMs`          | 100           |
| `EnableDuplicateDetection` | true          |
| `DuplicateWindowSeconds`   | 60            |
| `MaxConcurrentProcessing`  | 1             |
| `ShutdownTimeoutSeconds`   | 30            |

### Preset Configurations

| Preset           | MaxQueueSize | ThrottleDelayMs | DuplicateWindow | MaxConcurrent |
| :--------------- | :----------- | :-------------- | :-------------- | :------------ |
| `Default`        | 1000         | 100             | 60s             | 1             |
| `HighThroughput` | 5000         | 0               | 30s             | 4             |
| `LowLatency`     | 100          | 50              | 10s             | 1             |

---

## Verification

```bash
# Build RAG module
dotnet build src/Lexichord.Modules.RAG

# Run v0.4.2d tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.2d"
# Result: 80 tests passed

# Run all unit tests
dotnet test tests/Lexichord.Tests.Unit
# Result: All tests passing
```

---

## Dependencies

| Dependency                                  | Version | Purpose                               |
| :------------------------------------------ | :------ | :------------------------------------ |
| `System.Threading.Channels`                 | (BCL)   | Thread-safe producer-consumer pattern |
| `Microsoft.Extensions.Hosting.Abstractions` | 9.0.0   | BackgroundService base class          |
| `Microsoft.Extensions.Options`              | 9.0.0   | IOptions pattern for configuration    |
| `IIngestionService`                         | v0.4.2a | File processing service               |
| `FileIndexingRequestedEvent`                | v0.4.2c | Source of indexing requests           |

---

## Related Documents

- [LCS-DES-v0.4.2d](../../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2d.md) - Design specification
- [LCS-SBD-v0.4.2 §3.4](../../specs/v0.4.x/v0.4.2/LCS-SBD-v0.4.2.md#34-v042d-ingestion-queue) - Scope breakdown
- [LCS-CL-v0.4.2c](./LCS-CL-v0.4.2c.md) - Previous sub-part (File Watcher Integration)
