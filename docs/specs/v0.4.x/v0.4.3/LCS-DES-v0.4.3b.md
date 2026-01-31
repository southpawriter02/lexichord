# LCS-DES-043b: Fixed-Size Chunker

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-043b                             |
| **Version**      | v0.4.3b                                  |
| **Title**        | Fixed-Size Chunker                       |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | Core                                     |

---

## 1. Overview

### 1.1 Purpose

This specification defines the `FixedSizeChunkingStrategy`, which splits text into chunks of approximately equal character count. This is the fundamental chunking strategy and serves as a fallback for other strategies when handling oversized sections.

### 1.2 Goals

- Split text into chunks of configurable target size
- Apply overlap between consecutive chunks for context continuity
- Respect word boundaries to avoid mid-word splits
- Provide predictable, consistent chunk sizes
- Optimize for large document performance

### 1.3 Non-Goals

- Semantic understanding of content
- Paragraph or structure awareness
- Header hierarchy preservation

---

## 2. Algorithm

### 2.1 Core Logic

```text
FIXED_SIZE_SPLIT(content, options):
│
├── VALIDATE
│   ├── IF content is null or empty → RETURN []
│   └── options.Validate()
│
├── INITIALIZE
│   ├── chunks = []
│   ├── position = 0
│   ├── index = 0
│   └── contentLength = content.Length
│
├── PROCESS
│   │
│   └── WHILE position < contentLength:
│       │
│       ├── CALCULATE END POSITION
│       │   └── idealEnd = MIN(position + targetSize, contentLength)
│       │
│       ├── ADJUST FOR WORD BOUNDARIES (if enabled)
│       │   │
│       │   ├── IF idealEnd < contentLength:
│       │   │   │
│       │   │   ├── SEARCH BACKWARD for space
│       │   │   │   ├── searchStart = MAX(position, idealEnd - targetSize * 0.2)
│       │   │   │   └── FOR i = idealEnd - 1 DOWN TO searchStart:
│       │   │   │       └── IF content[i] is whitespace → end = i + 1, BREAK
│       │   │   │
│       │   │   ├── IF no space found backward:
│       │   │   │   └── SEARCH FORWARD for space
│       │   │   │       ├── searchEnd = MIN(contentLength, idealEnd + targetSize * 0.1)
│       │   │   │       └── FOR i = idealEnd TO searchEnd:
│       │   │   │           └── IF content[i] is whitespace → end = i + 1, BREAK
│       │   │   │
│       │   │   └── IF still no space found:
│       │   │       └── end = idealEnd (accept mid-word split)
│       │   │
│       │   └── ELSE: end = idealEnd
│       │
│       ├── EXTRACT CHUNK
│       │   ├── chunkContent = content[position..end]
│       │   ├── IF not preserveWhitespace:
│       │   │   └── chunkContent = chunkContent.Trim()
│       │   │
│       │   └── IF chunkContent is not empty OR includeEmptyChunks:
│       │       └── ADD TextChunk(chunkContent, position, end, metadata)
│       │
│       └── ADVANCE POSITION
│           ├── advance = end - position - overlap
│           └── position += MAX(advance, 1)  // Ensure progress
│
├── FINALIZE
│   ├── totalChunks = chunks.Count
│   └── UPDATE each chunk's metadata with TotalChunks
│
└── RETURN chunks
```

### 2.2 Word Boundary Detection

The algorithm searches for word boundaries in two phases:

1. **Backward Search (Primary)**: Look for whitespace within the last 20% of the target size, moving backward from the ideal end position.

2. **Forward Search (Fallback)**: If no space is found backward, search forward up to 10% beyond the target size.

3. **Accept Split (Last Resort)**: If no suitable boundary is found in either direction, accept the split at the exact target position.

```text
WORD_BOUNDARY_SEARCH:

         ┌─────────────────────────────────────────┐
         │              TARGET SIZE                 │
         └─────────────────────────────────────────┘
                   ┌────────┐     ┌───┐
                   │  80%   │     │20%│
                   │ SEARCH │     │ F │
position ─────────>│  BACK  │─────│ W │──> idealEnd
                   └────────┘     └───┘
                         ▲           │
                         │           ▼
                    found space    if not found,
                    here? use it   search forward 10%
```

---

## 3. Implementation

### 3.1 Class Definition

```csharp
namespace Lexichord.Modules.RAG.Chunking;

/// <summary>
/// Splits text into fixed-size chunks with configurable overlap.
/// Respects word boundaries to avoid mid-word splits when possible.
/// </summary>
public sealed class FixedSizeChunkingStrategy : IChunkingStrategy
{
    private readonly ILogger<FixedSizeChunkingStrategy> _logger;

    public ChunkingMode Mode => ChunkingMode.FixedSize;

    public FixedSizeChunkingStrategy(ILogger<FixedSizeChunkingStrategy> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        if (string.IsNullOrEmpty(content))
        {
            return Array.Empty<TextChunk>();
        }

        _logger.LogDebug(
            "Splitting {ContentLength} chars with target {TargetSize}, overlap {Overlap}",
            content.Length, options.TargetSize, options.Overlap);

        var chunks = new List<TextChunk>();
        var position = 0;
        var index = 0;

        while (position < content.Length)
        {
            var end = CalculateEndPosition(content, position, options);
            var chunkContent = ExtractChunk(content, position, end, options);

            if (!string.IsNullOrWhiteSpace(chunkContent) || options.IncludeEmptyChunks)
            {
                chunks.Add(new TextChunk(
                    chunkContent,
                    position,
                    end,
                    new ChunkMetadata(index++)));
            }

            // Advance with overlap
            var advance = end - position - options.Overlap;
            position += Math.Max(advance, 1);
        }

        // Set total chunks
        var totalChunks = chunks.Count;
        for (var i = 0; i < chunks.Count; i++)
        {
            chunks[i] = chunks[i] with
            {
                Metadata = chunks[i].Metadata with { TotalChunks = totalChunks }
            };
        }

        _logger.LogDebug(
            "Created {ChunkCount} chunks with {Overlap} char overlap",
            chunks.Count, options.Overlap);

        return chunks;
    }

    private int CalculateEndPosition(string content, int position, ChunkingOptions options)
    {
        var idealEnd = Math.Min(position + options.TargetSize, content.Length);

        // No adjustment needed at document end
        if (idealEnd >= content.Length)
        {
            return content.Length;
        }

        // Skip word boundary if disabled
        if (!options.RespectWordBoundaries)
        {
            return idealEnd;
        }

        return FindWordBoundary(content, position, idealEnd, options.TargetSize);
    }

    private static int FindWordBoundary(string content, int start, int idealEnd, int targetSize)
    {
        // Phase 1: Search backward (last 20% of target)
        var backwardSearchStart = Math.Max(start, idealEnd - (int)(targetSize * 0.2));

        for (var i = idealEnd - 1; i >= backwardSearchStart; i--)
        {
            if (char.IsWhiteSpace(content[i]))
            {
                return i + 1; // Position after whitespace
            }
        }

        // Phase 2: Search forward (up to 10% overage)
        var forwardSearchEnd = Math.Min(content.Length, idealEnd + (int)(targetSize * 0.1));

        for (var i = idealEnd; i < forwardSearchEnd; i++)
        {
            if (char.IsWhiteSpace(content[i]))
            {
                return i + 1;
            }
        }

        // Phase 3: No good boundary found
        return idealEnd;
    }

    private static string ExtractChunk(
        string content, int start, int end, ChunkingOptions options)
    {
        var chunk = content[start..end];
        return options.PreserveWhitespace ? chunk : chunk.Trim();
    }
}
```

### 3.2 DI Registration

```csharp
// In RAGModule.cs
services.AddSingleton<FixedSizeChunkingStrategy>();
services.AddSingleton<IChunkingStrategy>(sp =>
    sp.GetRequiredService<FixedSizeChunkingStrategy>());
```

---

## 4. Configuration

| Parameter | Type | Default | Description |
| :-------- | :--- | :------ | :---------- |
| `TargetSize` | int | 1000 | Target chunk size in characters |
| `Overlap` | int | 100 | Characters to overlap between chunks |
| `RespectWordBoundaries` | bool | true | Adjust boundaries to word edges |
| `PreserveWhitespace` | bool | false | Keep leading/trailing whitespace |
| `IncludeEmptyChunks` | bool | false | Include whitespace-only chunks |

---

## 5. Examples

### 5.1 Basic Splitting

```text
INPUT (250 chars):
"The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs. How vexingly quick daft zebras jump! The five boxing wizards jump quickly. Sphinx of black quartz, judge my vow."

OPTIONS:
- TargetSize: 100
- Overlap: 20
- RespectWordBoundaries: true

OUTPUT:
Chunk 0: "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs. How" (89 chars)
Chunk 1: "jugs. How vexingly quick daft zebras jump! The five boxing wizards jump quickly." (80 chars)
Chunk 2: "quickly. Sphinx of black quartz, judge my vow." (46 chars)
```

### 5.2 Overlap Visualization

```text
CHUNK 1: ████████████████████████████████████████░░░░
CHUNK 2:                                 ░░░░████████████████████████████████████████░░░░
CHUNK 3:                                                                  ░░░░████████████████████████████████████████

Legend: ████ = Unique content, ░░░░ = Overlap
```

### 5.3 Word Boundary Adjustment

```text
INPUT: "Internationalization and localization..."

WITHOUT word boundaries (target 20):
  "Internationalizatio" | "ion and localizat" | "tion..."

WITH word boundaries (target 20):
  "Internationalization" | "and localization..."
```

---

## 6. Edge Cases

| Case | Behavior |
| :--- | :------- |
| Empty string | Returns empty list |
| Null string | Returns empty list |
| Content < TargetSize | Single chunk with full content |
| Single long word > TargetSize | Splits mid-word (no boundary available) |
| All whitespace | Returns empty list (unless IncludeEmptyChunks) |
| Overlap >= Content | Single chunk (no overlap possible) |
| Unicode characters | Handled correctly (char-based, not byte-based) |
| Mixed whitespace (tabs, newlines) | Treated as word boundaries |

---

## 7. Performance

### 7.1 Complexity

| Operation | Time | Space |
| :-------- | :--- | :---- |
| Split | O(n) | O(n) |
| Word boundary search | O(k) where k = 30% of TargetSize | O(1) |

### 7.2 Benchmarks

| Document Size | Target Size | Chunks | Time |
| :------------ | :---------- | :----- | :--- |
| 10 KB | 1000 | ~12 | < 1ms |
| 100 KB | 1000 | ~120 | < 5ms |
| 1 MB | 1000 | ~1200 | < 50ms |
| 10 MB | 1000 | ~12000 | < 500ms |

### 7.3 Memory Optimization

For very large documents (>10MB), consider streaming:

```csharp
// Future enhancement: streaming chunker
public async IAsyncEnumerable<TextChunk> SplitStreamingAsync(
    TextReader reader,
    ChunkingOptions options,
    [EnumeratorCancellation] CancellationToken ct = default)
{
    // Read and yield chunks without loading entire document
}
```

---

## 8. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3b")]
public class FixedSizeChunkingStrategyTests
{
    private readonly FixedSizeChunkingStrategy _sut;

    public FixedSizeChunkingStrategyTests()
    {
        _sut = new FixedSizeChunkingStrategy(NullLogger<FixedSizeChunkingStrategy>.Instance);
    }

    [Fact]
    public void Split_EmptyContent_ReturnsEmptyList()
    {
        var chunks = _sut.Split("", ChunkingOptions.Default);
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void Split_ContentSmallerThanTarget_ReturnsSingleChunk()
    {
        var content = "Short text.";
        var options = new ChunkingOptions { TargetSize = 100 };

        var chunks = _sut.Split(content, options);

        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(content);
    }

    [Fact]
    public void Split_CreateOverlappingChunks()
    {
        var content = new string('a', 250);
        var options = new ChunkingOptions { TargetSize = 100, Overlap = 20 };

        var chunks = _sut.Split(content, options);

        chunks.Should().HaveCountGreaterThan(2);

        // Verify overlap exists
        for (var i = 1; i < chunks.Count; i++)
        {
            var prevEnd = chunks[i - 1].EndOffset;
            var currStart = chunks[i].StartOffset;
            (prevEnd - currStart).Should().BeGreaterOrEqualTo(20);
        }
    }

    [Fact]
    public void Split_RespectsWordBoundaries()
    {
        var content = "word1 word2 word3 word4 word5 word6 word7 word8 word9 word10";
        var options = new ChunkingOptions
        {
            TargetSize = 25,
            Overlap = 5,
            RespectWordBoundaries = true
        };

        var chunks = _sut.Split(content, options);

        foreach (var chunk in chunks)
        {
            // No chunk should start or end with partial word (except at boundaries)
            if (!chunk.Metadata.IsFirst && chunk.StartOffset > 0)
            {
                var charBefore = content[chunk.StartOffset - 1];
                charBefore.Should().Match(c => char.IsWhiteSpace(c) || chunk.StartOffset == 0);
            }
        }
    }

    [Fact]
    public void Split_DisabledWordBoundaries_SplitsExactly()
    {
        var content = "abcdefghijklmnopqrstuvwxyz";
        var options = new ChunkingOptions
        {
            TargetSize = 10,
            Overlap = 0,
            RespectWordBoundaries = false
        };

        var chunks = _sut.Split(content, options);

        chunks[0].Content.Should().Be("abcdefghij");
        chunks[1].Content.Should().Be("klmnopqrst");
        chunks[2].Content.Should().Be("uvwxyz");
    }

    [Fact]
    public void Split_TrimsWhitespace_ByDefault()
    {
        var content = "  hello world  \n\n  goodbye  ";
        var options = new ChunkingOptions { TargetSize = 20 };

        var chunks = _sut.Split(content, options);

        foreach (var chunk in chunks)
        {
            chunk.Content.Should().NotStartWith(" ");
            chunk.Content.Should().NotEndWith(" ");
        }
    }

    [Fact]
    public void Split_PreservesWhitespace_WhenEnabled()
    {
        var content = "  hello  ";
        var options = new ChunkingOptions { TargetSize = 100, PreserveWhitespace = true };

        var chunks = _sut.Split(content, options);

        chunks[0].Content.Should().Be("  hello  ");
    }

    [Fact]
    public void Split_SetsCorrectMetadata()
    {
        var content = new string('x', 500);
        var options = new ChunkingOptions { TargetSize = 100, Overlap = 10 };

        var chunks = _sut.Split(content, options);

        chunks[0].Metadata.Index.Should().Be(0);
        chunks[0].Metadata.IsFirst.Should().BeTrue();
        chunks[0].Metadata.TotalChunks.Should().Be(chunks.Count);

        chunks[^1].Metadata.IsLast.Should().BeTrue();
    }

    [Fact]
    public void Split_SetsCorrectOffsets()
    {
        var content = "Hello, World!";
        var options = new ChunkingOptions { TargetSize = 100 };

        var chunks = _sut.Split(content, options);

        chunks[0].StartOffset.Should().Be(0);
        chunks[0].EndOffset.Should().Be(content.Length);
    }

    [Theory]
    [InlineData(100, 10)]
    [InlineData(500, 50)]
    [InlineData(1000, 100)]
    [InlineData(2000, 200)]
    public void Split_VariousConfigurations_ProducesValidChunks(int targetSize, int overlap)
    {
        var content = new string('a', 10000);
        var options = new ChunkingOptions { TargetSize = targetSize, Overlap = overlap };

        var chunks = _sut.Split(content, options);

        chunks.Should().NotBeEmpty();
        chunks.Should().AllSatisfy(c =>
        {
            c.Length.Should().BeLessOrEqualTo(targetSize + (int)(targetSize * 0.1));
            c.StartOffset.Should().BeGreaterOrEqualTo(0);
            c.EndOffset.Should().BeLessOrEqualTo(content.Length);
        });
    }

    [Fact]
    public void Split_LongWordExceedingTarget_SplitsMidWord()
    {
        var longWord = new string('a', 200);
        var options = new ChunkingOptions { TargetSize = 50, Overlap = 10 };

        var chunks = _sut.Split(longWord, options);

        // Should produce multiple chunks even without word boundaries
        chunks.Should().HaveCountGreaterThan(1);
    }

    [Fact]
    public void Split_UnicodeCharacters_HandledCorrectly()
    {
        var content = "Hello 世界! Привет мир! 🌍🌎🌏";
        var options = new ChunkingOptions { TargetSize = 15, Overlap = 5 };

        var chunks = _sut.Split(content, options);

        // Should not throw and should produce valid chunks
        chunks.Should().NotBeEmpty();
        var reconstructed = string.Concat(chunks.Select(c => c.Content));
        // Note: Won't be exact due to overlap, but should contain all unique content
    }
}
```

---

## 9. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Debug | "Splitting {ContentLength} chars with target {TargetSize}, overlap {Overlap}" | Start of operation |
| Debug | "Created {ChunkCount} chunks with {Overlap} char overlap" | End of operation |
| Warning | "Chunk exceeds target size after boundary adjustment: {ActualSize}" | When boundary search extends chunk |
| Trace | "Word boundary found at position {Position}" | Boundary detection (verbose) |

---

## 10. File Locations

| File | Path |
| :--- | :--- |
| Strategy implementation | `src/Lexichord.Modules.RAG/Chunking/FixedSizeChunkingStrategy.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Chunking/FixedSizeChunkingStrategyTests.cs` |

---

## 11. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Chunks are approximately TargetSize (±10%) | [ ] |
| 2 | Overlap applied between consecutive chunks | [ ] |
| 3 | Word boundaries respected when enabled | [ ] |
| 4 | Empty content returns empty list | [ ] |
| 5 | Single chunk for content < TargetSize | [ ] |
| 6 | Whitespace trimmed by default | [ ] |
| 7 | Correct StartOffset/EndOffset values | [ ] |
| 8 | Metadata includes Index and TotalChunks | [ ] |
| 9 | 1MB document chunked in < 50ms | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 12. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
