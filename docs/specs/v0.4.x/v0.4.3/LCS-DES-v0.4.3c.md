# LCS-DES-043c: Paragraph Chunker

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-043c                             |
| **Version**      | v0.4.3c                                  |
| **Title**        | Paragraph Chunker                        |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | Core                                     |

---

## 1. Overview

### 1.1 Purpose

This specification defines the `ParagraphChunkingStrategy`, which splits text on natural paragraph boundaries (double newlines) while intelligently merging short paragraphs and splitting oversized ones. This strategy preserves the natural flow of written content.

### 1.2 Goals

- Split content on double-newline (`\n\n`) paragraph boundaries
- Merge consecutive short paragraphs (<MinSize) to create meaningful chunks
- Split oversized paragraphs (>MaxSize) using fixed-size fallback
- Maintain target chunk sizes while respecting paragraph structure
- Preserve paragraph coherence for better embedding quality

### 1.3 Non-Goals

- Sentence-level splitting
- Semantic understanding of paragraph content
- Header/section awareness (use MarkdownHeader strategy)

---

## 2. Algorithm

### 2.1 Core Logic

```text
PARAGRAPH_SPLIT(content, options):
│
├── VALIDATE
│   ├── IF content is null or empty → RETURN []
│   └── options.Validate()
│
├── SPLIT INTO PARAGRAPHS
│   ├── Split on "\n\n" or "\r\n\r\n"
│   └── Remove empty entries
│
├── INITIALIZE
│   ├── chunks = []
│   ├── buffer = StringBuilder()
│   ├── bufferStart = 0
│   ├── position = 0
│   └── index = 0
│
├── PROCESS EACH PARAGRAPH
│   │
│   └── FOR EACH paragraph IN paragraphs:
│       │
│       ├── trimmed = paragraph.Trim()
│       │
│       ├── CASE: OVERSIZED PARAGRAPH (> MaxSize)
│       │   │
│       │   ├── IF buffer not empty:
│       │   │   └── FLUSH buffer as chunk
│       │   │
│       │   ├── Split paragraph using FixedSizeStrategy
│       │   └── Add all sub-chunks to chunks
│       │
│       ├── CASE: SHORT PARAGRAPH (buffer + paragraph < MinSize)
│       │   │
│       │   └── Append paragraph to buffer (merge)
│       │
│       ├── CASE: FITS IN BUFFER (buffer + paragraph <= TargetSize)
│       │   │
│       │   └── Append paragraph to buffer
│       │
│       └── CASE: BUFFER OVERFLOW (buffer + paragraph > TargetSize)
│           │
│           ├── IF buffer not empty:
│           │   └── FLUSH buffer as chunk
│           │
│           └── Start new buffer with paragraph
│       │
│       └── Update position += paragraph.Length + 2
│
├── FLUSH REMAINING
│   └── IF buffer not empty:
│       └── Create final chunk from buffer
│
├── FINALIZE
│   └── Set TotalChunks in all metadata
│
└── RETURN chunks
```

### 2.2 Decision Flowchart

```text
                    ┌─────────────────┐
                    │  New Paragraph  │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │ paragraph.Length │
                    │    > MaxSize?    │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              │ YES          │ NO           │
              ▼              │              ▼
    ┌─────────────────┐      │    ┌─────────────────┐
    │  Flush buffer   │      │    │ buffer + para   │
    │  Split with     │      │    │   < MinSize?    │
    │  FixedSize      │      │    └────────┬────────┘
    └─────────────────┘      │             │
                             │   ┌─────────┼─────────┐
                             │   │ YES     │ NO      │
                             │   ▼         │         ▼
                             │ ┌───────────┴┐  ┌─────────────┐
                             │ │  Merge to  │  │buffer + para│
                             │ │  buffer    │  │ <= Target?  │
                             │ └────────────┘  └──────┬──────┘
                             │                        │
                             │              ┌─────────┼─────────┐
                             │              │ YES     │ NO      │
                             │              ▼         ▼         │
                             │      ┌────────────┐ ┌────────────┐
                             │      │ Append to  │ │ Flush buf, │
                             │      │  buffer    │ │ new buffer │
                             │      └────────────┘ └────────────┘
                             │
                             └───────────────────────────────────
```

### 2.3 Merging Strategy

| Current Buffer | Incoming Paragraph | Combined Size | Action |
| :------------- | :----------------- | :------------ | :----- |
| Empty | Short (<200) | <200 | Start buffer |
| Short (<200) | Short (<200) | <200 | Merge (keep buffering) |
| Short (<200) | Short (<200) | ≥200, ≤1000 | Merge (keep buffering) |
| Any | Any | ≤1000 | Merge (keep buffering) |
| Any | Any | >1000 | Flush buffer, start new |
| Empty | Long (>2000) | >2000 | Split with FixedSize |
| Any | Long (>2000) | — | Flush, then split |

---

## 3. Implementation

### 3.1 Class Definition

```csharp
namespace Lexichord.Modules.RAG.Chunking;

/// <summary>
/// Splits text on paragraph boundaries with intelligent merging and splitting.
/// </summary>
public sealed class ParagraphChunkingStrategy : IChunkingStrategy
{
    private readonly FixedSizeChunkingStrategy _fixedSizeFallback;
    private readonly ILogger<ParagraphChunkingStrategy> _logger;

    private static readonly string[] ParagraphSeparators = { "\n\n", "\r\n\r\n" };

    public ChunkingMode Mode => ChunkingMode.Paragraph;

    public ParagraphChunkingStrategy(
        FixedSizeChunkingStrategy fixedSizeFallback,
        ILogger<ParagraphChunkingStrategy> logger)
    {
        _fixedSizeFallback = fixedSizeFallback;
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

        var paragraphs = content.Split(
            ParagraphSeparators,
            StringSplitOptions.RemoveEmptyEntries);

        _logger.LogDebug("Found {ParagraphCount} paragraphs", paragraphs.Length);

        if (paragraphs.Length == 0)
        {
            return Array.Empty<TextChunk>();
        }

        var chunks = new List<TextChunk>();
        var buffer = new StringBuilder();
        var bufferStart = 0;
        var position = 0;
        var mergeCount = 0;
        var splitCount = 0;

        foreach (var paragraph in paragraphs)
        {
            var trimmed = paragraph.Trim();

            if (string.IsNullOrEmpty(trimmed))
            {
                position += paragraph.Length + 2;
                continue;
            }

            if (trimmed.Length > options.MaxSize)
            {
                // Flush buffer first
                FlushBuffer(chunks, buffer, bufferStart, ref position);

                // Split oversized paragraph
                var subChunks = _fixedSizeFallback.Split(trimmed, options);
                foreach (var subChunk in subChunks)
                {
                    chunks.Add(new TextChunk(
                        subChunk.Content,
                        position + subChunk.StartOffset,
                        position + subChunk.EndOffset,
                        new ChunkMetadata(chunks.Count)));
                }
                splitCount++;
                position += trimmed.Length + 2;
            }
            else if (buffer.Length + trimmed.Length < options.MinSize)
            {
                // Merge short paragraph
                AppendToBuffer(buffer, trimmed, ref bufferStart, position);
                mergeCount++;
                position += paragraph.Length + 2;
            }
            else if (buffer.Length + trimmed.Length + 2 <= options.TargetSize)
            {
                // Continue building buffer
                AppendToBuffer(buffer, trimmed, ref bufferStart, position);
                position += paragraph.Length + 2;
            }
            else
            {
                // Flush and start new
                FlushBuffer(chunks, buffer, bufferStart, ref position);
                buffer.Append(trimmed);
                bufferStart = position;
                position += paragraph.Length + 2;
            }
        }

        // Flush remaining buffer
        if (buffer.Length > 0)
        {
            chunks.Add(CreateChunk(buffer.ToString(), bufferStart, chunks.Count));
        }

        _logger.LogDebug("Merged {MergeCount} short paragraphs", mergeCount);
        _logger.LogDebug("Split {SplitCount} long paragraphs", splitCount);

        // Set total chunks
        var totalChunks = chunks.Count;
        return chunks.Select(c => c with
        {
            Metadata = c.Metadata with { TotalChunks = totalChunks }
        }).ToList();
    }

    private static void AppendToBuffer(
        StringBuilder buffer, string content, ref int bufferStart, int position)
    {
        if (buffer.Length == 0)
        {
            bufferStart = position;
        }
        else
        {
            buffer.Append("\n\n");
        }
        buffer.Append(content);
    }

    private void FlushBuffer(
        List<TextChunk> chunks, StringBuilder buffer, int bufferStart, ref int position)
    {
        if (buffer.Length > 0)
        {
            chunks.Add(CreateChunk(buffer.ToString(), bufferStart, chunks.Count));
            buffer.Clear();
        }
    }

    private static TextChunk CreateChunk(string content, int start, int index)
    {
        return new TextChunk(
            content,
            start,
            start + content.Length,
            new ChunkMetadata(index));
    }
}
```

### 3.2 DI Registration

```csharp
// In RAGModule.cs
services.AddSingleton<ParagraphChunkingStrategy>();
```

---

## 4. Configuration

| Parameter | Type | Default | Impact |
| :-------- | :--- | :------ | :----- |
| `TargetSize` | int | 1000 | Target merged paragraph size |
| `MinSize` | int | 200 | Threshold for merging short paragraphs |
| `MaxSize` | int | 2000 | Threshold for splitting long paragraphs |
| `Overlap` | int | 100 | Used by FixedSize fallback only |

---

## 5. Examples

### 5.1 Normal Paragraphs

```text
INPUT:
"First paragraph with some content.

Second paragraph with more content.

Third paragraph wraps things up."

OPTIONS: TargetSize=500, MinSize=200

OUTPUT:
Chunk 0: "First paragraph with some content.\n\nSecond paragraph with more content.\n\nThird paragraph wraps things up."
(All merged into one chunk since combined < TargetSize)
```

### 5.2 Short Paragraph Merging

```text
INPUT:
"Hi.

Hello.

Hey.

How are you doing today? This is a longer paragraph that provides more context."

OPTIONS: TargetSize=500, MinSize=200

OUTPUT:
Chunk 0: "Hi.\n\nHello.\n\nHey.\n\nHow are you doing today? This is a longer paragraph that provides more context."
(Short paragraphs merged until MinSize reached)
```

### 5.3 Long Paragraph Splitting

```text
INPUT:
"Short intro.

[3000 characters of continuous text without paragraph breaks]

Short conclusion."

OPTIONS: TargetSize=1000, MaxSize=2000

OUTPUT:
Chunk 0: "Short intro."
Chunk 1: "[First 1000 chars of long paragraph...]"
Chunk 2: "[...next ~900 chars with overlap...]"
Chunk 3: "[...remaining chars...]"
Chunk 4: "Short conclusion."
```

### 5.4 Mixed Content

```text
INPUT:
"Introduction paragraph.

Brief point 1.

Brief point 2.

Brief point 3.

This is a medium-length paragraph that provides some context about the topic at hand and elaborates on the brief points.

Another brief point.

Conclusion."

OPTIONS: TargetSize=300, MinSize=100

OUTPUT:
Chunk 0: "Introduction paragraph.\n\nBrief point 1.\n\nBrief point 2.\n\nBrief point 3."
Chunk 1: "This is a medium-length paragraph that provides some context about the topic at hand and elaborates on the brief points."
Chunk 2: "Another brief point.\n\nConclusion."
```

---

## 6. Edge Cases

| Case | Behavior |
| :--- | :------- |
| Empty string | Returns empty list |
| Single paragraph | Single chunk (even if short) |
| All short paragraphs | Merged until TargetSize |
| Single giant paragraph | Split using FixedSize |
| Multiple blank lines | Treated as single separator |
| Windows line endings (CRLF) | Handled correctly |
| Mixed line endings | Both `\n\n` and `\r\n\r\n` recognized |
| Trailing newlines | Trimmed, no empty chunks |
| Leading newlines | Trimmed, no empty chunks |

---

## 7. Performance

### 7.1 Complexity

| Operation | Time | Space |
| :-------- | :--- | :---- |
| Paragraph split | O(n) | O(n) |
| Merge processing | O(p) where p = paragraph count | O(n) for buffer |
| Overall | O(n) | O(n) |

### 7.2 Benchmarks

| Document Size | Paragraphs | Chunks | Time |
| :------------ | :--------- | :----- | :--- |
| 10 KB | ~50 | ~15 | < 1ms |
| 100 KB | ~500 | ~150 | < 10ms |
| 1 MB | ~5000 | ~1500 | < 100ms |

---

## 8. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3c")]
public class ParagraphChunkingStrategyTests
{
    private readonly ParagraphChunkingStrategy _sut;

    public ParagraphChunkingStrategyTests()
    {
        var fixedSize = new FixedSizeChunkingStrategy(
            NullLogger<FixedSizeChunkingStrategy>.Instance);
        _sut = new ParagraphChunkingStrategy(
            fixedSize,
            NullLogger<ParagraphChunkingStrategy>.Instance);
    }

    [Fact]
    public void Split_EmptyContent_ReturnsEmptyList()
    {
        var chunks = _sut.Split("", ChunkingOptions.Default);
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void Split_SingleParagraph_ReturnsSingleChunk()
    {
        var content = "This is a single paragraph with no breaks.";
        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Be(content);
    }

    [Fact]
    public void Split_SeparatesParagraphsOnDoubleNewline()
    {
        var content = "Paragraph one.\n\nParagraph two.\n\nParagraph three.";
        var options = new ChunkingOptions { MinSize = 10, TargetSize = 50 };

        var chunks = _sut.Split(content, options);

        chunks.Should().HaveCount(3);
        chunks[0].Content.Should().Be("Paragraph one.");
        chunks[1].Content.Should().Be("Paragraph two.");
        chunks[2].Content.Should().Be("Paragraph three.");
    }

    [Fact]
    public void Split_MergesShortParagraphs()
    {
        var content = "Hi.\n\nHello.\n\nHey.";
        var options = new ChunkingOptions { MinSize = 200, TargetSize = 1000 };

        var chunks = _sut.Split(content, options);

        chunks.Should().HaveCount(1);
        chunks[0].Content.Should().Contain("Hi.");
        chunks[0].Content.Should().Contain("Hello.");
        chunks[0].Content.Should().Contain("Hey.");
    }

    [Fact]
    public void Split_SplitsLongParagraphs()
    {
        var longParagraph = new string('a', 3000);
        var content = $"Short.\n\n{longParagraph}\n\nEnd.";
        var options = new ChunkingOptions { MaxSize = 2000, TargetSize = 1000 };

        var chunks = _sut.Split(content, options);

        // Should have more than 3 chunks due to split
        chunks.Should().HaveCountGreaterThan(3);
    }

    [Fact]
    public void Split_HandlesWindowsLineEndings()
    {
        var content = "Para one.\r\n\r\nPara two.\r\n\r\nPara three.";
        var options = new ChunkingOptions { MinSize = 10, TargetSize = 50 };

        var chunks = _sut.Split(content, options);

        chunks.Should().HaveCount(3);
    }

    [Fact]
    public void Split_PreservesParagraphSeparatorInMerged()
    {
        var content = "First.\n\nSecond.\n\nThird.";
        var options = new ChunkingOptions { MinSize = 200, TargetSize = 1000 };

        var chunks = _sut.Split(content, options);

        // Merged content should preserve \n\n between paragraphs
        chunks[0].Content.Should().Contain("\n\n");
    }

    [Fact]
    public void Split_FlushesBufferBeforeSplittingLong()
    {
        var content = "Short intro.\n\n" + new string('x', 3000);
        var options = new ChunkingOptions { MaxSize = 2000, TargetSize = 1000 };

        var chunks = _sut.Split(content, options);

        // First chunk should be the short intro, not merged with long
        chunks[0].Content.Should().Be("Short intro.");
    }

    [Fact]
    public void Split_SetsCorrectMetadata()
    {
        var content = "Para one.\n\nPara two.\n\nPara three.";
        var options = new ChunkingOptions { MinSize = 10, TargetSize = 50 };

        var chunks = _sut.Split(content, options);

        chunks[0].Metadata.Index.Should().Be(0);
        chunks[0].Metadata.IsFirst.Should().BeTrue();
        chunks[^1].Metadata.IsLast.Should().BeTrue();
        chunks.All(c => c.Metadata.TotalChunks == chunks.Count).Should().BeTrue();
    }

    [Fact]
    public void Split_TrimsWhitespace()
    {
        var content = "  Paragraph one.  \n\n  Paragraph two.  ";
        var options = new ChunkingOptions { MinSize = 10, TargetSize = 50 };

        var chunks = _sut.Split(content, options);

        foreach (var chunk in chunks)
        {
            chunk.Content.Should().NotStartWith(" ");
            chunk.Content.Should().NotEndWith(" ");
        }
    }

    [Fact]
    public void Split_IgnoresEmptyParagraphs()
    {
        var content = "Para one.\n\n\n\n\n\nPara two.";
        var options = new ChunkingOptions { MinSize = 10, TargetSize = 50 };

        var chunks = _sut.Split(content, options);

        // Should treat multiple newlines as single separator
        chunks.Should().HaveCount(2);
    }

    [Theory]
    [InlineData(100, 50)]
    [InlineData(200, 100)]
    [InlineData(500, 200)]
    public void Split_RespectsMinSizeForMerging(int minSize, int expectedMergedCount)
    {
        // Create content with many short paragraphs
        var paragraphs = Enumerable.Range(1, 20).Select(i => $"P{i}.").ToArray();
        var content = string.Join("\n\n", paragraphs);
        var options = new ChunkingOptions { MinSize = minSize, TargetSize = 1000, MaxSize = 2000 };

        var chunks = _sut.Split(content, options);

        // Should merge paragraphs, reducing total chunk count
        chunks.Should().HaveCountLessThan(paragraphs.Length);
    }
}
```

---

## 9. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Debug | "Found {ParagraphCount} paragraphs" | After initial split |
| Debug | "Merged {MergeCount} short paragraphs" | After processing |
| Debug | "Split {SplitCount} long paragraphs" | After processing |
| Debug | "Flushing buffer with {BufferLength} chars" | On buffer flush |
| Trace | "Processing paragraph {Index}: {Length} chars" | Per-paragraph |

---

## 10. File Locations

| File | Path |
| :--- | :--- |
| Strategy implementation | `src/Lexichord.Modules.RAG/Chunking/ParagraphChunkingStrategy.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Chunking/ParagraphChunkingStrategyTests.cs` |

---

## 11. Dependencies

| Dependency | Source | Purpose |
| :--------- | :----- | :------ |
| `FixedSizeChunkingStrategy` | v0.4.3b | Fallback for oversized paragraphs |
| `IChunkingStrategy` | v0.4.3a | Interface implementation |
| `TextChunk`, `ChunkMetadata` | v0.4.3a | Return types |
| `ChunkingOptions` | v0.4.3a | Configuration |

---

## 12. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Splits on `\n\n` paragraph boundaries | [ ] |
| 2 | Handles `\r\n\r\n` (Windows) line endings | [ ] |
| 3 | Merges paragraphs below MinSize | [ ] |
| 4 | Splits paragraphs exceeding MaxSize | [ ] |
| 5 | Preserves `\n\n` in merged content | [ ] |
| 6 | Flushes buffer before splitting long paragraph | [ ] |
| 7 | Trims whitespace from paragraphs | [ ] |
| 8 | Sets correct metadata (Index, TotalChunks) | [ ] |
| 9 | Returns empty list for empty content | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 13. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
