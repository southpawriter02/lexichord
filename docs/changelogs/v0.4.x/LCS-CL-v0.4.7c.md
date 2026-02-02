# v0.4.7c Changelog — Indexing Progress

**Released:** 2026-02-02  
**Part of:** v0.4.7 "The Index Manager"

## Overview

Adds a real-time progress toast overlay for indexing operations, providing visual feedback during document re-indexing with support for cancellation and auto-dismiss.

## Added

### Abstractions Layer

- **`IndexingProgressInfo`** record (`Contracts/RAG/IndexingProgressInfo.cs`)
    - Progress snapshot with `CurrentDocument`, `ProcessedCount`, `TotalCount`, `PercentComplete`
    - Completion flags: `IsComplete`, `WasCancelled`
    - Factory methods: `InProgress()`, `Complete()`, `Cancelled()`

### RAG Module

- **`IndexingProgressUpdatedEvent`** (`Events/IndexingProgressUpdatedEvent.cs`)
    - MediatR notification for real-time progress updates
    - Published during bulk indexing operations

- **`IndexingProgressViewModel`** (`ViewModels/IndexingProgressViewModel.cs`)
    - Observable properties for progress UI binding
    - Throttled UI updates (100ms) to prevent performance issues
    - `CancelCommand` with `CanCancel` guard
    - Auto-dismiss after completion (3s success, 2s cancelled)
    - Elapsed time formatting (`30s`, `1m 30s`, `1h 5m`)

- **`IndexingProgressView.axaml`** (`Views/IndexingProgressView.axaml`)
    - Bottom-right positioned toast overlay
    - Progress bar, current file display, cancel button
    - Success/warning icons on completion

### Unit Tests

- **`IndexingProgressViewModelTests`** (18 tests)
    - Constructor validation
    - `Show()` initial state
    - Progress text formatting
    - Cancel command behavior
    - Elapsed time formatting theory tests

## Modified

- **`RAGModule.cs`**: Registered `IndexingProgressViewModel` as transient with MediatR handler binding

## Dependencies

| Interface    | Source Version |
| ------------ | -------------- |
| `IMediator`  | v0.0.7a        |
| `ILogger<T>` | v0.0.3b        |

## Test Results

```
Passed!  - Failed: 0, Passed: 18, Total: 18 (IndexingProgressViewModelTests)
Passed!  - Failed: 0, Passed: 703, Total: 704 (Full RAG suite)
```
