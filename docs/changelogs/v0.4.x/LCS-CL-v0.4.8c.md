# LCS-CL-v0.4.8c: Performance Benchmarks

## Document Control

| Field           | Value                                    |
| :-------------- | :--------------------------------------- |
| **Document ID** | LCS-CL-048c                              |
| **Version**     | v0.4.8c                                  |
| **Title**       | Performance Benchmarks (BenchmarkDotNet) |
| **Date**        | 2026-02-02                               |
| **Author**      | Assistant                                |

---

## Summary

Implemented performance benchmark suite for the RAG module using BenchmarkDotNet. Provides 21 benchmarks covering chunking, token counting, vector search, and memory usage.

---

## Changes

### Added

- **Benchmark Project** (`benchmarks/Lexichord.Benchmarks/`): New console app for performance profiling
    - BenchmarkDotNet 0.14.0 with .NET 9 runtime
    - MemoryDiagnoser enabled for allocation tracking
    - BenchmarkSwitcher for command-line filtering

- **ChunkingBenchmarks** (`Chunking/ChunkingBenchmarks.cs`): 9 benchmarks
    - `FixedSize_1KB/10KB/100KB`: Fixed-size chunking throughput
    - `Paragraph_1KB/10KB/100KB`: Paragraph-aware chunking
    - `Markdown_1KB/10KB/100KB`: Header-structure-aware chunking
    - Deterministic document generation (seed=42)

- **TokenCounterBenchmarks** (`Embedding/TokenCounterBenchmarks.cs`): 7 benchmarks
    - `CountTokens_Short/1000Words/10000Words`: Token counting
    - `Encode_Short/1000Words`: Token encoding to IDs
    - `Truncate_LongText/NoOp`: Truncation performance

- **VectorSearchBenchmarks** (`Search/VectorSearchBenchmarks.cs`): 2 × 3 benchmarks
    - `CosineSimilarity_TopK`: Full corpus scan with top-K
    - `CosineSimilarity_WithThreshold`: Threshold filtering
    - Parameterized corpus: 100, 1000, 10000 vectors

- **MemoryUsageBenchmarks** (`Memory/MemoryUsageBenchmarks.cs`): 3 × 3 benchmarks
    - `DocumentMetadataAllocation`: Document record memory
    - `ChunkWithEmbeddingAllocation`: Chunk + 1536-float embedding
    - `FilePathAllocation`: String allocation overhead
    - Parameterized counts: 100, 500, 1000 items

---

## Test Summary

| Category     | Benchmarks | Target Baseline    |
| :----------- | ---------: | :----------------- |
| Chunking     |          9 | 100KB < 100ms      |
| TokenCounter |          7 | 10K words < 50ms   |
| VectorSearch |          6 | 10K corpus < 200ms |
| Memory       |          9 | 1K docs < 10MB     |
| **Total**    |     **21** | —                  |

---

## Usage

```bash
# Run all benchmarks
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks

# Run specific category
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --filter "*Chunking*"

# List all benchmarks
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --list flat

# Export results
dotnet run -c Release --project benchmarks/Lexichord.Benchmarks -- --exporters json html
```

---

## Acceptance Criteria

| #   | Criterion                       | Status |
| :-- | :------------------------------ | :----- |
| 1   | Benchmark project builds        | ✅     |
| 2   | Chunking benchmarks implemented | ✅     |
| 3   | Token counter benchmarks        | ✅     |
| 4   | Vector search benchmarks        | ✅     |
| 5   | Memory usage benchmarks         | ✅     |
| 6   | Command-line filtering works    | ✅     |
| 7   | All 21 benchmarks discoverable  | ✅     |

---

## Files Changed

| File                                                                  | Change  |
| :-------------------------------------------------------------------- | :------ |
| `benchmarks/Lexichord.Benchmarks/Lexichord.Benchmarks.csproj`         | Added   |
| `benchmarks/Lexichord.Benchmarks/Program.cs`                          | Added   |
| `benchmarks/Lexichord.Benchmarks/Chunking/ChunkingBenchmarks.cs`      | Added   |
| `benchmarks/Lexichord.Benchmarks/Embedding/TokenCounterBenchmarks.cs` | Added   |
| `benchmarks/Lexichord.Benchmarks/Search/VectorSearchBenchmarks.cs`    | Added   |
| `benchmarks/Lexichord.Benchmarks/Memory/MemoryUsageBenchmarks.cs`     | Added   |
| `Lexichord.sln`                                                       | Updated |
