# Changelog: v0.4.3a - Chunking Abstractions

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.3a](../../specs/v0.4.x/v0.4.3/LCS-DES-v0.4.3a.md)

---

## Summary

Defines the core abstractions for the document chunking system in `Lexichord.Abstractions.Contracts`. This establishes the contracts for pluggable chunking algorithms, including the strategy interface, configuration options, output records, and predefined configuration presets. These abstractions form the foundation for the three concrete chunking strategies (Fixed-Size, Paragraph, Markdown Header) implemented in v0.4.3b-d.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/

| File                   | Type         | Description                                                     |
| :--------------------- | :----------- | :-------------------------------------------------------------- |
| `ChunkingMode.cs`      | Enum         | 4 strategy modes: FixedSize, Paragraph, MarkdownHeader, Semantic |
| `ChunkMetadata.cs`     | Record       | Chunk context with index, heading, level, and navigation helpers |
| `TextChunk.cs`         | Record       | Chunk output with content, offsets, and computed properties      |
| `ChunkingOptions.cs`   | Record       | Configuration with 7 properties, defaults, and validation       |
| `IChunkingStrategy.cs` | Interface    | Strategy contract with Mode property and Split method            |
| `ChunkingPresets.cs`   | Static class | 4 predefined configurations: HighPrecision, Balanced, HighContext, Code |

#### Lexichord.Tests.Unit/Abstractions/Chunking/

| File                           | Tests | Coverage                                                         |
| :----------------------------- | :---- | :--------------------------------------------------------------- |
| `ChunkingAbstractionsTests.cs` | 55    | Records, enums, presets, validation, equality, contract mocking  |

---

## Technical Details

### Type Summary

| Type              | Properties                                     | Key Features                         |
| :---------------- | :--------------------------------------------- | :----------------------------------- |
| `ChunkingMode`    | FixedSize=0, Paragraph=1, MarkdownHeader=2, Semantic=3 | Strategy selection enum     |
| `TextChunk`       | Content, StartOffset, EndOffset, Metadata      | Length, HasContent, Preview (100ch)  |
| `ChunkMetadata`   | Index, Heading, Level, TotalChunks             | IsFirst, IsLast, RelativePosition    |
| `ChunkingOptions` | TargetSize, Overlap, MinSize, MaxSize, ...     | Validate() with 6 constraint rules  |
| `ChunkingPresets` | HighPrecision, Balanced, HighContext, Code      | Ready-to-use configurations          |

### ChunkingOptions Defaults

| Property              | Default | Description                     |
| :-------------------- | :------ | :------------------------------ |
| `TargetSize`          | 1000    | Target chunk size in characters |
| `Overlap`             | 100     | Overlap between chunks          |
| `MinSize`             | 200     | Minimum before merging          |
| `MaxSize`             | 2000    | Maximum before splitting        |
| `RespectWordBoundaries` | true | Don't split mid-word            |
| `PreserveWhitespace`  | false   | Trim chunk content              |
| `IncludeEmptyChunks`  | false   | Filter whitespace-only chunks   |

### Validation Rules

| # | Constraint                                 | Error Message                              |
| :- | :---------------------------------------- | :----------------------------------------- |
| 1 | TargetSize > 0                             | "TargetSize must be positive"              |
| 2 | Overlap >= 0                               | "Overlap cannot be negative"               |
| 3 | Overlap < TargetSize                       | "Overlap must be less than TargetSize"     |
| 4 | MinSize >= 0                               | "MinSize cannot be negative"               |
| 5 | MaxSize > MinSize                          | "MaxSize must be greater than MinSize"     |
| 6 | TargetSize <= MaxSize                      | "TargetSize cannot exceed MaxSize"         |

### ChunkingPresets Summary

| Preset         | TargetSize | Overlap | MinSize | MaxSize | WordBounds | Whitespace |
| :------------- | :--------- | :------ | :------ | :------ | :--------- | :--------- |
| HighPrecision  | 500        | 50      | 100     | 1000    | true       | false      |
| Balanced       | 1000       | 100     | 200     | 2000    | true       | false      |
| HighContext     | 2000       | 200     | 500     | 4000    | true       | false      |
| Code           | 1500       | 50      | 200     | 3000    | false      | true       |

---

## Verification

```bash
# Build abstractions
dotnet build src/Lexichord.Abstractions

# Run v0.4.3a tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.3a"
# Result: 55 tests passed
```

---

## Dependencies

- None (pure abstractions, no external packages required)

## Dependents

- v0.4.3b: Fixed-Size Chunker (implements IChunkingStrategy with FixedSize mode)
- v0.4.3c: Paragraph Chunker (implements IChunkingStrategy with Paragraph mode)
- v0.4.3d: Markdown Header Chunker (implements IChunkingStrategy with MarkdownHeader mode)
- v0.4.4d: Document Indexing Pipeline (consumes TextChunk output)

---

## Related Documents

- [LCS-DES-v0.4.3a](../../specs/v0.4.x/v0.4.3/LCS-DES-v0.4.3a.md) - Design specification
- [LCS-SBD-v0.4.3 §3.1](../../specs/v0.4.x/v0.4.3/LCS-SBD-v0.4.3.md#31-v043a-chunking-abstractions) - Scope breakdown
- [LCS-DES-v0.4.3-INDEX](../../specs/v0.4.x/v0.4.3/LCS-DES-v0.4.3-INDEX.md) - Version index
- [LCS-CL-v0.4.2d](./LCS-CL-v0.4.2d.md) - Previous sub-part (Ingestion Queue)
