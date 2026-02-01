# Changelog: v0.4.3c - Paragraph Chunker

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.3c](../../specs/v0.4.x/v0.4.3/LCS-DES-v0.4.3c.md)

---

## Summary

Implements the `ParagraphChunkingStrategy` that splits text based on paragraph boundaries (double newlines). Short paragraphs are merged until reaching `TargetSize`, while oversized paragraphs use `FixedSizeChunkingStrategy` as a fallback splitter.

---

## Changes

### Added

#### Lexichord.Modules.RAG/Chunking/

| File                           | Description                                                      |
| :----------------------------- | :--------------------------------------------------------------- |
| `ParagraphChunkingStrategy.cs` | Paragraph-boundary chunking with merging and fixed-size fallback |

### Modified

#### Lexichord.Modules.RAG/

| File           | Description                                  |
| :------------- | :------------------------------------------- |
| `RAGModule.cs` | Added singleton DI registration for strategy |

### Unit Tests

#### Lexichord.Tests.Unit/Modules/RAG/Chunking/

| File                                | Tests                                     |
| :---------------------------------- | :---------------------------------------- |
| `ParagraphChunkingStrategyTests.cs` | 35 tests covering all acceptance criteria |

---

## Technical Details

### Algorithm Overview

```
1. Split content on paragraph boundaries (\n\n or \r\n\r\n)
2. For each paragraph:
   - If > MaxSize: Use FixedSizeChunkingStrategy fallback
   - If buffer + paragraph > TargetSize: Flush buffer as chunk
   - Add paragraph to buffer
3. Flush remaining buffer as final chunk
4. Update all chunks with TotalChunks metadata
```

### Paragraph Merging

Short paragraphs are accumulated in a buffer and merged with `\n\n` separators until:

- Combined length would exceed `TargetSize`
- A paragraph exceeds `MaxSize` (triggers fallback)
- End of content reached

### Configuration Parameters

| Parameter    | Default | Usage                                   |
| :----------- | :------ | :-------------------------------------- |
| `TargetSize` | 1000    | Ideal chunk size, triggers buffer flush |
| `MinSize`    | 200     | Used for semantic coherence guidance    |
| `MaxSize`    | 2000    | Threshold for fallback splitting        |
| `Overlap`    | 100     | Passed to fallback strategy             |

---

## Dependencies

| Type      | Name                         | Version |
| :-------- | :--------------------------- | :------ |
| Interface | `IChunkingStrategy`          | v0.4.3a |
| Record    | `TextChunk`, `ChunkMetadata` | v0.4.3a |
| Record    | `ChunkingOptions`            | v0.4.3a |
| Class     | `FixedSizeChunkingStrategy`  | v0.4.3b |

---

## Verification

```bash
# Run paragraph chunker tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~ParagraphChunkingStrategy"

# Run all chunking tests (regression check)
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Chunking"
```

### Test Results

| Test Suite                       | Pass | Fail | Total |
| :------------------------------- | :--- | :--- | :---- |
| `ParagraphChunkingStrategyTests` | 35   | 0    | 35    |
| All Chunking Tests               | 116  | 0    | 116   |
