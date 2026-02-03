# LCS-CL-053c: Heading Hierarchy Service

**Version:** v0.5.3c
**Date:** 2026-02
**Status:** ✅ Complete

## Summary

Implemented `HeadingHierarchyService` to resolve heading breadcrumb trails for document chunks, replacing the stub implementation from v0.5.3a. Uses stack-based tree construction from chunk heading metadata and recursive depth-first search for breadcrumb resolution. Includes document-level caching with automatic invalidation via MediatR event handlers.

## Changes

### Abstractions (`Lexichord.Abstractions`)

| File                           | Change                                                           |
| ------------------------------ | ---------------------------------------------------------------- |
| `Chunk.cs`                     | Added `Heading` and `HeadingLevel` properties for breadcrumb support |
| `ChunkHeadingInfo.cs`          | New lightweight record for heading hierarchy queries             |
| `IChunkRepository.cs`          | Added `GetChunksWithHeadingsAsync()` method                      |
| `IHeadingHierarchyService.cs`  | Added `InvalidateCache()` and `ClearCache()` methods; updated `HeadingNode` record |

### Data (`Lexichord.Modules.RAG`)

| File                   | Change                                                                      |
| ---------------------- | --------------------------------------------------------------------------- |
| `ChunkRepository.cs`   | Updated all SQL queries to include Heading/HeadingLevel; added `GetChunksWithHeadingsAsync()` |

### Services (`Lexichord.Modules.RAG`)

| File                          | Change                                                    |
| ----------------------------- | --------------------------------------------------------- |
| `HeadingHierarchyService.cs`  | New full implementation replacing stub                    |
| `StubHeadingHierarchyService.cs` | Deleted (replaced by full implementation)              |

### Indexing (`Lexichord.Modules.RAG`)

| File                           | Change                                                    |
| ------------------------------ | --------------------------------------------------------- |
| `DocumentIndexingPipeline.cs`  | Transfer heading metadata from `TextChunk.Metadata` to `Chunk` |

### Module (`Lexichord.Modules.RAG`)

| File           | Change                                                       |
| -------------- | ------------------------------------------------------------ |
| `RAGModule.cs` | Replaced stub with `HeadingHierarchyService` and MediatR handlers |

## Key Features

### HeadingHierarchyService

- **Tree Building**: Stack-based algorithm for constructing heading hierarchy from chunk metadata
- **Breadcrumb Resolution**: Recursive depth-first search to find path from root to chunk
- **Caching**: `ConcurrentDictionary<Guid, HeadingNode?>` with MaxCacheSize = 50, EvictionBatch = 10
- **Event Handlers**: Subscribes to `DocumentIndexedEvent` and `DocumentRemovedFromIndexEvent` for automatic cache invalidation
- **Multiple H1 Support**: Virtual root structure handles documents with multiple top-level headings

### HeadingNode Record

- **Immutable Structure**: Record with `Id`, `Text`, `Level`, `ChunkIndex`, `Children`
- **Factory Method**: `HeadingNode.Leaf()` for creating nodes without children
- **Computed Property**: `HasChildren` for easy tree traversal

### ChunkHeadingInfo Record

- **Lightweight Query**: Contains only fields needed for tree building (Id, DocumentId, ChunkIndex, Heading, HeadingLevel)
- **Efficient Loading**: Avoids loading full chunk content and embeddings for heading queries

### Data Model Updates

- **Chunk**: Extended with `Heading` (string?) and `HeadingLevel` (int) properties
- **SQL Queries**: All chunk queries now SELECT heading/heading_level columns
- **IndexingPipeline**: Transfers heading metadata from chunking stage to storage

## Algorithm Details

### Tree Building (Stack-Based)

```
1. Create virtual root at level 0
2. For each heading (ordered by ChunkIndex):
   a. Pop stack until parent level < current level
   b. Attach current heading as child of stack top
   c. Push current heading onto stack
3. Convert mutable builders to immutable HeadingNode tree
```

### Breadcrumb Resolution (Recursive DFS)

```
1. If target < root.ChunkIndex: return empty
2. If root.Level > 0: add root.Text to path
3. For each child (with computed scope end):
   a. Recursively search child's subtree
   b. If found, return true
4. Return true (target is in this scope)
```

## Tests

| File                              | Tests                                                  |
| --------------------------------- | ------------------------------------------------------ |
| `HeadingHierarchyServiceTests.cs` | 30 tests - Constructor, GetBreadcrumbAsync (nested, before/after, skipped levels, multiple H1s), BuildHeadingTreeAsync, cache operations, MediatR handlers, HeadingNode, constants |

**Total: 30 unit tests**

## Dependencies

- `DocumentIndexedEvent` (v0.4.4d) - Triggers cache invalidation on re-indexing
- `DocumentRemovedFromIndexEvent` (v0.4.7b) - Triggers cache invalidation on removal
- `MediatR.INotificationHandler<T>` (NuGet) - Event subscription pattern
- `IChunkRepository.GetChunksWithHeadingsAsync()` - Heading data access
- `Microsoft.Extensions.Logging` (existing) - Diagnostic logging

## Acceptance Criteria Met

| # | Criterion | Verification |
|---|-----------|--------------|
| 1 | `GetBreadcrumbAsync` returns correct path for nested headings | Unit test |
| 2 | Chunk before first heading returns empty breadcrumb | Unit test |
| 3 | Chunk after last heading belongs to last heading | Unit test |
| 4 | Skipped levels (H1 → H3) handled correctly | Unit test |
| 5 | Documents without headings return null tree, empty breadcrumb | Unit test |
| 6 | Negative chunk index throws ArgumentException | Unit test |
| 7 | Heading trees are cached per document | Unit test |
| 8 | DocumentIndexedEvent invalidates document cache | Unit test |
| 9 | DocumentRemovedFromIndexEvent invalidates document cache | Unit test |
| 10 | Chunk model includes Heading and HeadingLevel properties | Code inspection |
| 11 | ChunkRepository reads/writes heading columns | Code inspection |
| 12 | Pipeline transfers heading metadata to chunks | Code inspection |

## Edge Cases Handled

| Case | Behavior |
|------|----------|
| No headings in document | Return empty breadcrumb, null tree |
| Chunk before first heading | Return empty breadcrumb |
| Chunk after last heading | Belongs to last heading in scope |
| Skipped levels (H1→H3) | H3 is direct child of H1 |
| Multiple H1 headings | Virtual root holds all H1s; each H1 has its own scope |
| Empty/whitespace heading text | Skipped during tree building |
| Negative chunk index | Throws `ArgumentException` |
| Deep nesting (H1→H6) | Full 6-level path returned |

## Related Documentation

- [v0.5.3 Scope Breakdown](../../specs/v0.5.x/v0.5.3/LCS-SBD-v0.5.3.md)
- [Context Window Design Spec](../../specs/v0.5.x/v0.5.3/LCS-DES-v0.5.3c.md)
