# Changelog: v0.2.7d Large File Stress Test

**Version:** 0.2.7d  
**Codename:** The Turbo (Part 4)  
**Date:** 2026-01-30  
**Design Spec:** [LCS-DES-027d](../../specs/v0.2.x/v0.2.7/LCS-DES-027d.md)

---

## Overview

Implements performance testing infrastructure for validating linting performance with documents 5MB or larger. Includes chunked scanning with viewport prioritization, adaptive debounce monitoring, and synthetic test corpus generation.

---

## Changes

### Abstraction Layer

#### IPerformanceBenchmark (NEW)

- **File:** `Lexichord.Abstractions/Contracts/Performance/IPerformanceBenchmark.cs`
- **Purpose:** Contract for performance benchmark execution
- **Methods:**
    - `RunLintingBenchmarkAsync` - Full document scan benchmark
    - `RunTypingBenchmarkAsync` - Typing simulation benchmark
    - `RunScrollBenchmarkAsync` - Scroll/viewport change benchmark

#### Benchmark Result Records (NEW)

- **File:** `Lexichord.Abstractions/Contracts/Performance/BenchmarkResult.cs`
- **Records:**
    - `BenchmarkResult` - Base result with duration, success, error
    - `LintingBenchmarkResult` - Extended with scan metrics
    - `TypingBenchmarkResult` - Extended with FPS metrics
    - `ScrollBenchmarkResult` - Extended with viewport metrics

#### IPerformanceMonitor (NEW)

- **File:** `Lexichord.Abstractions/Contracts/Performance/IPerformanceMonitor.cs`
- **Purpose:** Metrics collection for adaptive debounce
- **Methods:**
    - `StartOperation` - Begin timing an operation
    - `RecordOperation` - Record completed operation duration
    - `ReportFrameDrop` - Track UI frame drops
    - `Reset` - Clear all collected metrics
- **Properties:**
    - `RecommendedDebounceInterval` - Adaptive interval (200ms-1000ms)
    - `IsPerformanceDegraded` - True if scans exceed thresholds
    - `PeakMemoryBytes` - Maximum memory observed

#### PerformanceMetrics (NEW)

- **File:** `Lexichord.Abstractions/Contracts/Performance/PerformanceMetrics.cs`
- **Purpose:** Immutable snapshot of current performance state

---

### Implementation Layer

#### PerformanceMonitor (NEW)

- **File:** `Lexichord.Modules.Style/Services/PerformanceMonitor.cs`
- **Purpose:** Thread-safe performance metrics collection
- **Features:**
    - Lock-free concurrent collections
    - Adaptive debounce scaling (200ms-1000ms based on scan cost)
    - P95 percentile calculation for latency analysis
    - Memory usage tracking via GC.GetTotalMemory
    - Frame drop detection threshold (16ms for 60fps)
- **Thread Safety:** Uses ConcurrentDictionary and ConcurrentQueue

#### ChunkedScanner (NEW)

- **File:** `Lexichord.Modules.Style/Services/Linting/ChunkedScanner.cs`
- **Purpose:** Progressive chunked scanning for large documents
- **Features:**
    - Documents >1MB split into ~100KB chunks
    - Line-boundary aligned chunk boundaries
    - Viewport-priority chunk ordering (visible content first)
    - 100-character chunk overlap for boundary violations
    - IAsyncEnumerable streaming of results
- **Constants:**
    - `DefaultChunkSizeBytes`: 100KB
    - `ChunkingThresholdBytes`: 1MB
    - `ChunkOverlap`: 100 characters

#### ChunkScanResult (NEW)

- **File:** `Lexichord.Modules.Style/Services/Linting/ChunkScanResult.cs`
- **Purpose:** Result record for individual chunk scans
- **Properties:**
    - `ChunkIndex`, `TotalChunks` - Progress tracking
    - `StartOffset`, `EndOffset` - Chunk boundaries
    - `IsViewportChunk` - Viewport priority indicator
    - `Violations` - StyleViolation list with absolute positions
    - `ScanDuration` - Timing for this chunk
    - `ProgressPercent` - Calculated completion percentage

---

### Test Infrastructure

#### TestCorpusGenerator (NEW)

- **File:** `Lexichord.Tests.Unit/Modules/Style/Performance/TestCorpusGenerator.cs`
- **Purpose:** Synthetic document generation for stress tests
- **Features:**
    - Deterministic generation via seed parameter
    - Configurable document size (MB)
    - Optional YAML frontmatter inclusion
    - Code block density control (0-100%)
    - Violation-triggering term density control
- **Configuration:** `DocumentGenerationOptions` record

#### LintingStressTests (NEW)

- **File:** `Lexichord.Tests.Unit/Modules/Style/Performance/LintingStressTests.cs`
- **Purpose:** Performance validation test fixture
- **Tests (marked Skip for explicit execution):**
    - `FullScan_5MBDocument_CompletesUnder2Seconds`
    - `ViewportScan_5MBDocument_CompletesUnder100Ms`
    - `TypingSimulation_MaintainsSixtyFPS`
    - `MemoryUsage_5MBDocument_StaysUnder100MB`
    - `ChunkedScan_ProcessesViewportFirst`
- **Unit Tests (always run):**
    - `DebounceAdaptation_UnderLoad_IncreasesInterval`
    - `FilteredScan_ExcludesCodeBlocks`

---

### Unit Tests

#### PerformanceMonitorTests (NEW)

- **File:** `Lexichord.Tests.Unit/Modules/Style/Performance/PerformanceMonitorTests.cs`
- **Test Count:** 11 tests
- **Coverage:**
    - Metrics initialization and reset
    - Operation timing and averaging
    - Debounce interval adaptation
    - Performance degradation detection
    - Frame drop counting

#### ChunkedScannerTests (NEW)

- **File:** `Lexichord.Tests.Unit/Modules/Style/Performance/ChunkedScannerTests.cs`
- **Test Count:** 6 tests
- **Coverage:**
    - Small file single-scan optimization
    - Large file chunking behavior
    - Viewport priority ordering
    - Line-boundary alignment
    - Cancellation handling
    - Progress calculation

#### TestCorpusGeneratorTests (NEW)

- **File:** `Lexichord.Tests.Unit/Modules/Style/Performance/TestCorpusGeneratorTests.cs`
- **Test Count:** 5 tests
- **Coverage:**
    - Target size generation
    - Frontmatter inclusion
    - Code block generation
    - Violation term inclusion
    - Deterministic seeding

---

## Technical Details

### Chunking Strategy

```
1. Check document size against 1MB threshold
2. If under threshold, scan as single chunk
3. If over threshold:
   a. Calculate chunk count (size / 100KB)
   b. Find line boundaries for each chunk end
   c. Add 100-char overlap between chunks
   d. Sort chunks by viewport overlap (viewport first)
4. Yield results as IAsyncEnumerable
5. Adjust violation positions from chunk-relative to absolute
```

### Adaptive Debounce Algorithm

| Condition            | Debounce Interval |
| -------------------- | ----------------- |
| Scan P95 < 100ms     | 200ms (minimum)   |
| Scan P95 100-200ms   | 300ms             |
| Scan P95 200-500ms   | 500ms             |
| Scan P95 > 500ms     | 1000ms (maximum)  |
| Frame drops detected | +200ms            |

### Performance Targets

| Metric              | Target  | Measured Via         |
| ------------------- | ------- | -------------------- |
| Full scan (5MB)     | < 2s    | Stress test          |
| Viewport scan (5MB) | < 100ms | First chunk timing   |
| FPS during typing   | ≥ 55fps | Frame drop detection |
| Memory usage (5MB)  | < 100MB | GC.GetTotalMemory    |

---

## Verification Results

### Build Status

- **Result:** Success
- **Warnings:** 0
- **Errors:** 0

### Test Results

- **Total Tests:** 2378
- **Passed:** 2320
- **Failed:** 0
- **Skipped:** 58 (platform-specific + performance)

### New Test Results

- **PerformanceMonitorTests:** 11/11 passed
- **ChunkedScannerTests:** 6/6 passed
- **TestCorpusGeneratorTests:** 5/5 passed
- **LintingStressTests (unit):** 2/2 passed
- **LintingStressTests (performance):** 5/5 skipped (explicit)

---

## Files Changed

### New Files

| File                                                                                | Lines | Purpose                  |
| ----------------------------------------------------------------------------------- | ----- | ------------------------ |
| `src/Lexichord.Abstractions/Contracts/Performance/IPerformanceBenchmark.cs`         | 58    | Benchmark contract       |
| `src/Lexichord.Abstractions/Contracts/Performance/BenchmarkResult.cs`               | 87    | Result records           |
| `src/Lexichord.Abstractions/Contracts/Performance/IPerformanceMonitor.cs`           | 63    | Monitor contract         |
| `src/Lexichord.Abstractions/Contracts/Performance/PerformanceMetrics.cs`            | 35    | Metrics snapshot         |
| `src/Lexichord.Modules.Style/Services/PerformanceMonitor.cs`                        | 198   | Monitor implementation   |
| `src/Lexichord.Modules.Style/Services/Linting/ChunkedScanner.cs`                    | 264   | Chunked scanning         |
| `src/Lexichord.Modules.Style/Services/Linting/ChunkScanResult.cs`                   | 42    | Chunk result record      |
| `tests/Lexichord.Tests.Unit/Modules/Style/Performance/PerformanceMonitorTests.cs`   | 156   | Monitor unit tests       |
| `tests/Lexichord.Tests.Unit/Modules/Style/Performance/ChunkedScannerTests.cs`       | 208   | Scanner unit tests       |
| `tests/Lexichord.Tests.Unit/Modules/Style/Performance/TestCorpusGeneratorTests.cs`  | 142   | Generator unit tests     |
| `tests/Lexichord.Tests.Unit/Modules/Style/Performance/TestCorpusGenerator.cs`       | 186   | Test document generator  |
| `tests/Lexichord.Tests.Unit/Modules/Style/Performance/DocumentGenerationOptions.cs` | 24    | Generator configuration  |
| `tests/Lexichord.Tests.Unit/Modules/Style/Performance/LintingStressTests.cs`        | 252   | Performance stress tests |

---

## Dependencies

### Consumes (from v0.2.7a-c)

- `IScannerService` interface (v0.2.3c)
- `StyleRule` and `StyleViolation` records
- `IContentFilter` pipeline
- `FilteredContent` with exclusion regions

### Provides (new in v0.2.7d)

- `IPerformanceBenchmark` interface
- `IPerformanceMonitor` interface
- `PerformanceMonitor` implementation
- `ChunkedScanner` service
- `ChunkScanResult` record
- `TestCorpusGenerator` test utility
- `DocumentGenerationOptions` record
- Benchmark and stress test infrastructure
