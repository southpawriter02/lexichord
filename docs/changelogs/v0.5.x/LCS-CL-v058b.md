# v0.5.8b Changelog — Search Performance Tests

**Version:** v0.5.8b  
**Status:** ✅ Complete  
**Parent:** [v0.5.8 — The Hardening](LCS-CL-v058-INDEX.md)

---

## Summary

Establishes performance baselines for all search operations using BenchmarkDotNet with Testcontainers PostgreSQL. Enables automatic regression detection in CI pipelines.

---

## New Components

### Benchmark Configuration

| File | Description |
|------|-------------|
| [BenchmarkConfig.cs](file:///Users/ryan/Documents/GitHub/lexichord/benchmarks/Lexichord.Benchmarks/BenchmarkConfig.cs) | CI-aware BenchmarkDotNet configuration with P95/Max columns, JSON/Markdown exporters |

### Data Seeding

| File | Description |
|------|-------------|
| [BenchmarkDataSeeder.cs](file:///Users/ryan/Documents/GitHub/lexichord/benchmarks/Lexichord.Benchmarks/Setup/BenchmarkDataSeeder.cs) | Synthetic corpus generator using Testcontainers PostgreSQL with pgvector |

### Search Benchmarks

| File | Description |
|------|-------------|
| [SearchBenchmarks.cs](file:///Users/ryan/Documents/GitHub/lexichord/benchmarks/Lexichord.Benchmarks/Search/SearchBenchmarks.cs) | 6 benchmark methods across 3 corpus sizes (1K, 10K, 50K) |

---

## Benchmark Methods

| Method | Description | Target P95 (10K) |
|--------|-------------|------------------|
| `HybridSearch` (baseline) | Combined BM25 + semantic with RRF fusion | 150ms |
| `BM25Search` | PostgreSQL full-text search ranking | 100ms |
| `SemanticSearchOnly` | pgvector cosine similarity search | 120ms |
| `FilteredSearch` | Semantic search with document filter | 180ms |
| `QuerySuggestions` | Prefix-based autocomplete | 50ms |
| `ContextExpansion` | Surrounding chunk retrieval | 40ms |

---

## CI Integration

### Files Created

| File | Description |
|------|-------------|
| [check-performance-regression.py](file:///Users/ryan/Documents/GitHub/lexichord/scripts/check-performance-regression.py) | Python script for regression detection (10% threshold) |
| [performance-baselines.json](file:///Users/ryan/Documents/GitHub/lexichord/benchmarks/baselines/performance-baselines.json) | Initial baseline placeholder with target values |
| [performance-tests.yml](file:///Users/ryan/Documents/GitHub/lexichord/.github/workflows/performance-tests.yml) | GitHub Actions workflow for automated benchmarking |

### Workflow Triggers

- Push to `main` with changes in `benchmarks/**` or `src/Lexichord.Modules.RAG/**`
- Pull requests targeting `main` with same path filters
- Manual workflow dispatch with optional full run mode

---

## Verification Commands

```bash
# Build benchmarks
dotnet build benchmarks/Lexichord.Benchmarks -c Release

# List available benchmarks
dotnet run --project benchmarks/Lexichord.Benchmarks -c Release -- --list flat

# Run benchmarks (CI mode)
CI=true dotnet run --project benchmarks/Lexichord.Benchmarks -c Release -- --filter "*SearchBenchmarks*"

# Check for regressions
python3 scripts/check-performance-regression.py \
  benchmarks/baselines/performance-baselines.json \
  BenchmarkDotNet.Artifacts/results/Lexichord.Benchmarks.Search.SearchBenchmarks-report.json
```

---

## Related Documents

- [v0.5.8b Design Spec](../specs/v0.5.x/v0.5.8/LCS-DES-v0.5.8b.md)
- [v0.5.8 Scope Breakdown](../specs/v0.5.x/v0.5.8/LCS-SBD-v0.5.8.md)
- [Dependency Matrix](../specs/DEPENDENCY-MATRIX.md)
