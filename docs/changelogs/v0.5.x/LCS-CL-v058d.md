# v0.5.8d — Error Resilience

**Released:** 2026-02-04  
**Status:** ✅ Complete  
**Scope:** RAG Module Hardening  

---

## Overview

Implements Polly-based resilience patterns for the RAG search subsystem, ensuring graceful degradation when external dependencies (embedding API, database) are unavailable.

## New Components

### Abstractions Layer

| Component | Path | Description |
|-----------|------|-------------|
| `DegradedSearchMode` | `Contracts/RAG/DegradedSearchMode.cs` | Enum: Full, KeywordOnly, CachedOnly, Unavailable |
| `CircuitBreakerState` | `Contracts/RAG/CircuitBreakerState.cs` | Enum: Closed, Open, HalfOpen |
| `SearchHealthStatus` | `Contracts/RAG/SearchHealthStatus.cs` | Health snapshot record with factory methods |
| `ResilientSearchResult` | `Contracts/RAG/ResilientSearchResult.cs` | Search result wrapper with resilience metadata |
| `IResilientSearchService` | `Contracts/RAG/IResilientSearchService.cs` | Resilient search interface |

### RAG Module

| Component | Path | Description |
|-----------|------|-------------|
| `ResilienceOptions` | `Configuration/ResilienceOptions.cs` | Polly policy configuration |
| `ResiliencePipelineBuilder` | `Resilience/ResiliencePipelineBuilder.cs` | Polly pipeline factory |
| `ResilientSearchService` | `Resilience/ResilientSearchService.cs` | Main service implementation |

## Fallback Hierarchy

```
Hybrid (Full) → BM25 (KeywordOnly) → Cache (CachedOnly) → Unavailable
```

- **Hybrid failure** (HttpRequestException, TimeoutRejectedException): Falls back to BM25 keyword-only search
- **Database failure** (NpgsqlException): Falls back to cached results
- **All failures**: Returns unavailable status with empty results

## Polly Policies

| Strategy | Default | Description |
|----------|---------|-------------|
| Timeout | 5s | Per-operation timeout |
| Retry | 3 attempts | Exponential backoff with jitter |
| Circuit Breaker | 5 failures / 30s break | Prevents cascading failures |

## Dependencies

- `IHybridSearchService` (v0.5.1c): Primary search service
- `IBM25SearchService` (v0.5.1b): BM25 fallback
- `IQueryResultCache` (v0.5.8c): Cache fallback
- `CacheKeyGenerator` (v0.5.8c): Cache key generation
- `Polly` v8.2.0: Resilience library

## Tests Added

| File | Tests | Description |
|------|-------|-------------|
| `ResilientSearchServiceTests.cs` | 16 | Service behavior and fallback tests |
| `ResiliencePipelineBuilderTests.cs` | 11 | Pipeline configuration tests |

## Verification

```bash
# Run resilience tests
dotnet test tests/Lexichord.Tests.Unit/ --filter "FullyQualifiedName~Resilience"

# Build verification
dotnet build
```
