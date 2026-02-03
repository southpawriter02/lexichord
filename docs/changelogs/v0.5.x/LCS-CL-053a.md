# LCS-CL-053a: Context Expansion Service

**Version:** v0.5.3a  
**Date:** 2026-02  
**Status:** ✅ Complete

## Summary

Implemented the Context Expansion Service for retrieving surrounding chunks and heading hierarchy for a given search result chunk.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                          | Change                                                          |
| ----------------------------- | --------------------------------------------------------------- |
| `IContextExpansionService.cs` | New interface for expanding chunks with surrounding context     |
| `ContextOptions.cs`           | Options record with validation (0-5 chunks per direction)       |
| `ExpandedChunk.cs`            | Result record with computed properties and `FormatBreadcrumb()` |
| `IHeadingHierarchyService.cs` | Interface for heading resolution (stub implementation)          |
| `HeadingNode.cs`              | Tree node for document heading structure                        |
| `IChunkRepository.cs`         | Extended with `GetSiblingsAsync` method                         |

### Events (`Lexichord.Modules.RAG`)

| File                      | Change                             |
| ------------------------- | ---------------------------------- |
| `ContextExpandedEvent.cs` | MediatR notification for telemetry |

### Services (`Lexichord.Modules.RAG`)

| File                             | Change                                                     |
| -------------------------------- | ---------------------------------------------------------- |
| `ContextExpansionService.cs`     | Core service with LRU caching (100 entries, FIFO eviction) |
| `StubHeadingHierarchyService.cs` | Placeholder until v0.5.3c                                  |
| `ChunkRepository.cs`             | Added `GetSiblingsAsync` implementation                    |
| `RAGModule.cs`                   | Service registration, version bump to v0.5.3               |

## Tests

| File                              | Tests                                                |
| --------------------------------- | ---------------------------------------------------- |
| `ContextOptionsTests.cs`          | 12 tests - Validation, clamping, defaults            |
| `ExpandedChunkTests.cs`           | 9 tests - Computed properties, breadcrumb formatting |
| `ContextExpansionServiceTests.cs` | 15 tests - Caching, events, graceful degradation     |

**Total: 36 unit tests**

## License Gating

- Feature Code: `FeatureCodes.ContextExpansion`
- Minimum Tier: Writer Pro

## Dependencies

- MediatR (existing)
- Microsoft.Extensions.Logging (existing)
