# v0.5.6a: Snippet Extraction

**Version:** v0.5.6a  
**Parent Feature:** v0.5.6 "The Answer Preview (Snippet Generation)"  
**Spec Reference:** LCS-DES-v0.5.6a  
**Date:** 2026-02-03

## Summary

Implemented the core snippet extraction service for generating query-highlighted previews of search results. This is the foundational sub-part of the Answer Preview feature.

## New Files

### Abstractions

| File | Description |
|------|-------------|
| `Contracts/HighlightType.cs` | Enum defining match types (QueryMatch, FuzzyMatch, KeyPhrase, Entity) |
| `Contracts/HighlightSpan.cs` | Record for highlighting spans with `Overlaps()` and `Merge()` methods |
| `Contracts/Snippet.cs` | Record for extracted snippets with `Empty` and `FromPlainText()` factories |
| `Contracts/SnippetOptions.cs` | Configuration record with `Default`, `Compact`, `Extended` presets |
| `Contracts/ISnippetService.cs` | Interface with `ExtractSnippet`, `ExtractMultipleSnippets`, `ExtractBatch` |
| `Contracts/ISentenceBoundaryDetector.cs` | Interface for sentence boundary detection (implementation in v0.5.6c) |

### RAG Module

| File | Description |
|------|-------------|
| `Services/SnippetService.cs` | Core implementation with match finding, density calculation, boundary expansion |
| `Services/PassthroughSentenceBoundaryDetector.cs` | Placeholder that returns positions unchanged |

## Modified Files

| File | Changes |
|------|---------|
| `RAGModule.cs` | Added DI registrations for `ISentenceBoundaryDetector` and `ISnippetService` |

## Unit Tests

| File | Test Count |
|------|------------|
| `Services/SnippetServiceTests.cs` | 17 tests |
| `Abstractions/SnippetTests.cs` | 13 tests |
| `Abstractions/HighlightSpanTests.cs` | 16 tests |

**Total:** 46 unit tests (all passing)

## API Additions

### ISnippetService Interface

```csharp
public interface ISnippetService
{
    Snippet ExtractSnippet(TextChunk chunk, string query, SnippetOptions options);
    IReadOnlyList<Snippet> ExtractMultipleSnippets(TextChunk chunk, string query, SnippetOptions options, int maxSnippets = 3);
    IDictionary<Guid, Snippet> ExtractBatch(IEnumerable<TextChunk> chunks, string query, SnippetOptions options);
}
```

### Data Contracts

- **Snippet**: `(Text, Highlights, StartOffset, IsTruncatedStart, IsTruncatedEnd)` with `Length`, `HasHighlights`, `IsTruncated` computed properties
- **HighlightSpan**: `(Start, Length, Type)` with `End` property and `Overlaps()`, `Merge()` methods
- **SnippetOptions**: `(MaxLength=200, ContextPadding=50, RespectSentenceBoundaries=true, IncludeFuzzyMatches=true, MinMatchLength=3)`
- **HighlightType**: `QueryMatch=0, FuzzyMatch=1, KeyPhrase=2, Entity=3`

## Dependencies

- `IQueryAnalyzer` (v0.5.4a) - For keyword extraction
- `ISentenceBoundaryDetector` (v0.5.6c) - Placeholder until real implementation

## Notes

- `PassthroughSentenceBoundaryDetector` is a placeholder that returns positions unchanged. Real sentence detection will be implemented in v0.5.6c.
- `ExtractBatch` uses deterministic GUID generation from chunk content/position since `ChunkMetadata` doesn't have an inherent `ChunkId` field.
