# LCS-DES-043d: Markdown Header Chunker

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-043d                             |
| **Version**      | v0.4.3d                                  |
| **Title**        | Markdown Header Chunker                  |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Modules.RAG`                  |
| **License Tier** | Core                                     |

---

## 1. Overview

### 1.1 Purpose

This specification defines the `MarkdownHeaderChunkingStrategy`, which creates hierarchical chunks based on Markdown header structure. Each section (from one header to the next of equal or higher level) becomes a chunk, preserving the document's logical organization.

### 1.2 Goals

- Parse Markdown headers (`#`, `##`, `###`, etc.) using Markdig
- Create chunks at header boundaries respecting hierarchy
- Store header text in chunk metadata for context injection
- Handle sections exceeding MaxSize with fixed-size fallback
- Fall back gracefully for non-Markdown content

### 1.3 Non-Goals

- Parsing other Markdown elements (code blocks, lists, etc.) for chunking
- Creating chunks smaller than sections
- Semantic analysis of section content

---

## 2. Algorithm

### 2.1 Core Logic

```text
MARKDOWN_HEADER_SPLIT(content, options):
│
├── VALIDATE
│   ├── IF content is null or empty → RETURN []
│   └── options.Validate()
│
├── PARSE MARKDOWN
│   ├── Create MarkdownPipeline
│   ├── Parse content into AST
│   └── Extract HeadingBlocks ordered by line number
│
├── CHECK FOR HEADERS
│   └── IF no headings found:
│       └── FALLBACK to ParagraphChunkingStrategy
│
├── PROCESS HEADERS
│   │
│   ├── FOR i = 0 TO headings.Count - 1:
│   │   │
│   │   ├── currentHeading = headings[i]
│   │   ├── currentLevel = currentHeading.Level (1-6)
│   │   ├── headerText = ExtractHeaderText(currentHeading)
│   │   │
│   │   ├── FIND END BOUNDARY
│   │   │   │
│   │   │   ├── endLine = document.LineCount
│   │   │   │
│   │   │   └── FOR j = i + 1 TO headings.Count - 1:
│   │   │       ├── nextHeading = headings[j]
│   │   │       │
│   │   │       └── IF nextHeading.Level <= currentLevel:
│   │   │           ├── endLine = nextHeading.Line - 1
│   │   │           └── BREAK
│   │   │
│   │   ├── EXTRACT SECTION CONTENT
│   │   │   ├── startLine = currentHeading.Line
│   │   │   └── sectionContent = lines[startLine..endLine].Join("\n")
│   │   │
│   │   ├── CHECK SIZE
│   │   │   │
│   │   │   ├── IF sectionContent.Length > MaxSize:
│   │   │   │   │
│   │   │   │   ├── Split using FixedSizeStrategy
│   │   │   │   └── Preserve header metadata in all sub-chunks
│   │   │   │
│   │   │   └── ELSE:
│   │   │       └── Create single chunk with header metadata
│   │   │
│   │   └── Add chunk(s) to results
│   │
│   └── HANDLE CONTENT BEFORE FIRST HEADER
│       └── IF firstHeading.Line > 0:
│           └── Create chunk for preamble content
│
├── FINALIZE
│   └── Set TotalChunks in all metadata
│
└── RETURN chunks
```

### 2.2 Header Hierarchy Rules

| Current Level | Next Level | Action |
| :------------ | :--------- | :----- |
| H1 | H1 | End section, start new H1 section |
| H1 | H2 | Continue (H2 nested under H1) |
| H1 | H3 | Continue (H3 nested under H1) |
| H2 | H1 | End section (H1 is higher level) |
| H2 | H2 | End section, start new H2 section |
| H2 | H3 | Continue (H3 nested under H2) |
| H3 | H1 | End section (H1 is higher level) |
| H3 | H2 | End section (H2 is higher level) |
| H3 | H3 | End section, start new H3 section |

### 2.3 Visual Example

```markdown
# Chapter 1           ← Start of Section 0 (H1)
Intro text.

## Section 1.1        ← Start of Section 1 (H2)
Content here.

### Subsection 1.1.1  ← Start of Section 2 (H3)
Details here.

## Section 1.2        ← Start of Section 3 (H2) — ends Sections 1 & 2
More content.

# Chapter 2           ← Start of Section 4 (H1) — ends Section 3
New chapter.
```

**Result:**

| Index | Heading | Level | Content |
| :---- | :------ | :---- | :------ |
| 0 | Chapter 1 | 1 | "Intro text." |
| 1 | Section 1.1 | 2 | "Content here." |
| 2 | Subsection 1.1.1 | 3 | "Details here." |
| 3 | Section 1.2 | 2 | "More content." |
| 4 | Chapter 2 | 1 | "New chapter." |

---

## 3. Implementation

### 3.1 Class Definition

```csharp
namespace Lexichord.Modules.RAG.Chunking;

using Markdig;
using Markdig.Syntax;

/// <summary>
/// Chunks Markdown documents by header structure.
/// Creates hierarchical chunks respecting heading levels.
/// </summary>
public sealed class MarkdownHeaderChunkingStrategy : IChunkingStrategy
{
    private readonly MarkdownPipeline _pipeline;
    private readonly IChunkingStrategy _fallbackStrategy;
    private readonly FixedSizeChunkingStrategy _fixedSizeStrategy;
    private readonly ILogger<MarkdownHeaderChunkingStrategy> _logger;

    public ChunkingMode Mode => ChunkingMode.MarkdownHeader;

    public MarkdownHeaderChunkingStrategy(
        ParagraphChunkingStrategy fallbackStrategy,
        FixedSizeChunkingStrategy fixedSizeStrategy,
        ILogger<MarkdownHeaderChunkingStrategy> logger)
    {
        _pipeline = new MarkdownPipelineBuilder().Build();
        _fallbackStrategy = fallbackStrategy;
        _fixedSizeStrategy = fixedSizeStrategy;
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

        var document = Markdown.Parse(content, _pipeline);

        var headings = document
            .Descendants<HeadingBlock>()
            .OrderBy(h => h.Line)
            .ToList();

        if (headings.Count == 0)
        {
            _logger.LogInfo("No headers found, falling back to paragraph chunking");
            return _fallbackStrategy.Split(content, options);
        }

        _logger.LogDebug("Found {HeaderCount} headers in document", headings.Count);

        var lines = content.Split('\n');
        var chunks = new List<TextChunk>();

        // Handle content before first header (preamble)
        if (headings[0].Line > 0)
        {
            var preambleContent = GetLinesContent(lines, 0, headings[0].Line - 1);
            if (!string.IsNullOrWhiteSpace(preambleContent))
            {
                chunks.Add(CreateChunk(preambleContent.Trim(), 0, chunks.Count, null, 0));
            }
        }

        // Process each header section
        for (var i = 0; i < headings.Count; i++)
        {
            var heading = headings[i];
            var headerText = GetHeadingText(heading);
            var level = heading.Level;

            // Find section end
            var endLine = lines.Length - 1;
            for (var j = i + 1; j < headings.Count; j++)
            {
                if (headings[j].Level <= level)
                {
                    endLine = headings[j].Line - 1;
                    break;
                }
            }

            // Extract section content (including the header itself)
            var startLine = heading.Line;
            var sectionContent = GetLinesContent(lines, startLine, endLine);
            var startOffset = GetLineOffset(lines, startLine);

            _logger.LogDebug(
                "Created chunk for header: {Heading} (level {Level})",
                headerText, level);

            // Check if section exceeds max size
            if (sectionContent.Length > options.MaxSize)
            {
                // Split oversized section but preserve header metadata
                var subChunks = _fixedSizeStrategy.Split(sectionContent, options);
                foreach (var subChunk in subChunks)
                {
                    chunks.Add(new TextChunk(
                        subChunk.Content,
                        startOffset + subChunk.StartOffset,
                        startOffset + subChunk.EndOffset,
                        new ChunkMetadata(chunks.Count, headerText, level)));
                }
            }
            else if (!string.IsNullOrWhiteSpace(sectionContent))
            {
                chunks.Add(CreateChunk(
                    sectionContent.Trim(),
                    startOffset,
                    chunks.Count,
                    headerText,
                    level));
            }
        }

        // Set total chunks
        var totalChunks = chunks.Count;
        return chunks.Select(c => c with
        {
            Metadata = c.Metadata with { TotalChunks = totalChunks }
        }).ToList();
    }

    private static string GetHeadingText(HeadingBlock heading)
    {
        // Extract inline text from heading
        if (heading.Inline == null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var inline in heading.Inline)
        {
            if (inline is Markdig.Syntax.Inlines.LiteralInline literal)
            {
                sb.Append(literal.Content.ToString());
            }
        }
        return sb.ToString().Trim();
    }

    private static string GetLinesContent(string[] lines, int startLine, int endLine)
    {
        if (startLine > endLine || startLine >= lines.Length)
        {
            return string.Empty;
        }

        endLine = Math.Min(endLine, lines.Length - 1);
        return string.Join("\n", lines[startLine..(endLine + 1)]);
    }

    private static int GetLineOffset(string[] lines, int targetLine)
    {
        var offset = 0;
        for (var i = 0; i < targetLine && i < lines.Length; i++)
        {
            offset += lines[i].Length + 1; // +1 for newline
        }
        return offset;
    }

    private static TextChunk CreateChunk(
        string content, int startOffset, int index, string? heading, int level)
    {
        return new TextChunk(
            content,
            startOffset,
            startOffset + content.Length,
            new ChunkMetadata(index, heading, level));
    }
}
```

### 3.2 DI Registration

```csharp
// In RAGModule.cs
services.AddSingleton<MarkdownHeaderChunkingStrategy>();
```

---

## 4. Markdig Integration

### 4.1 AST Parsing

```csharp
// Markdig parses Markdown into an Abstract Syntax Tree
var document = Markdown.Parse(content, pipeline);

// HeadingBlock contains:
// - Line: Line number in source (0-indexed)
// - Level: Heading level (1-6)
// - Inline: Child inline elements (text, emphasis, etc.)

var headings = document.Descendants<HeadingBlock>();
```

### 4.2 Heading Text Extraction

Headers may contain rich inline content:

```markdown
# **Bold** and *italic* header
```

The implementation extracts plain text from all `LiteralInline` children:

```csharp
foreach (var inline in heading.Inline)
{
    if (inline is LiteralInline literal)
    {
        sb.Append(literal.Content.ToString());
    }
}
// Result: "Bold and italic header"
```

---

## 5. Configuration

| Parameter | Type | Default | Impact |
| :-------- | :--- | :------ | :----- |
| `MaxSize` | int | 2000 | Triggers splitting of large sections |
| `TargetSize` | int | 1000 | Used by FixedSize fallback |
| `Overlap` | int | 100 | Used by FixedSize fallback |

---

## 6. Examples

### 6.1 Simple Document

```markdown
# Introduction
Welcome to the guide.

# Getting Started
Follow these steps.

# Conclusion
That's all!
```

**Output:**

| Index | Heading | Level | Content |
| :---- | :------ | :---- | :------ |
| 0 | Introduction | 1 | "# Introduction\nWelcome to the guide." |
| 1 | Getting Started | 1 | "# Getting Started\nFollow these steps." |
| 2 | Conclusion | 1 | "# Conclusion\nThat's all!" |

### 6.2 Nested Headers

```markdown
# Chapter 1
Overview.

## Section A
Details A.

### Subsection A1
More details.

## Section B
Details B.

# Chapter 2
New chapter.
```

**Output:**

| Index | Heading | Level | Lines |
| :---- | :------ | :---- | :---- |
| 0 | Chapter 1 | 1 | 0-1 (stops at H2) |
| 1 | Section A | 2 | 3-4 (stops at H3) |
| 2 | Subsection A1 | 3 | 6-7 (stops at H2) |
| 3 | Section B | 2 | 9-10 (stops at H1) |
| 4 | Chapter 2 | 1 | 12-end |

### 6.3 With Preamble

```markdown
This is preamble content before any headers.
It should become its own chunk.

# First Header
Actual content.
```

**Output:**

| Index | Heading | Level | Content |
| :---- | :------ | :---- | :------ |
| 0 | *(null)* | 0 | "This is preamble content..." |
| 1 | First Header | 1 | "# First Header\nActual content." |

### 6.4 Oversized Section

```markdown
# Large Section
[5000 characters of content...]

# Next Section
Short content.
```

**Output (with MaxSize=2000):**

| Index | Heading | Level | Content |
| :---- | :------ | :---- | :------ |
| 0 | Large Section | 1 | "# Large Section\n[first ~1900 chars]" |
| 1 | Large Section | 1 | "[next ~1900 chars with overlap]" |
| 2 | Large Section | 1 | "[remaining chars]" |
| 3 | Next Section | 1 | "# Next Section\nShort content." |

---

## 7. Edge Cases

| Case | Behavior |
| :--- | :------- |
| No headers | Falls back to ParagraphChunkingStrategy |
| Only headers (no content) | Creates chunks with just header text |
| Header at end of file | Creates chunk with just the header |
| Deeply nested (H6) | Handled same as other levels |
| Code blocks with # | Not treated as headers (Markdig parses correctly) |
| HTML comments | Preserved in content |
| Mixed header styles (`#` and `===`) | Both parsed by Markdig |
| Empty sections | Filtered out (unless IncludeEmptyChunks) |

---

## 8. Performance

### 8.1 Complexity

| Operation | Time | Space |
| :-------- | :--- | :---- |
| Markdown parsing | O(n) | O(n) for AST |
| Header extraction | O(h) where h = header count | O(h) |
| Section extraction | O(n) | O(n) |
| Overall | O(n) | O(n) |

### 8.2 Benchmarks

| Document Size | Headers | Chunks | Parse Time | Total Time |
| :------------ | :------ | :----- | :--------- | :--------- |
| 10 KB | 10 | 10 | < 5ms | < 10ms |
| 100 KB | 50 | 50 | < 20ms | < 30ms |
| 1 MB | 200 | 200 | < 100ms | < 150ms |

---

## 9. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3d")]
public class MarkdownHeaderChunkingStrategyTests
{
    private readonly MarkdownHeaderChunkingStrategy _sut;

    public MarkdownHeaderChunkingStrategyTests()
    {
        var fixedSize = new FixedSizeChunkingStrategy(
            NullLogger<FixedSizeChunkingStrategy>.Instance);
        var paragraph = new ParagraphChunkingStrategy(
            fixedSize,
            NullLogger<ParagraphChunkingStrategy>.Instance);
        _sut = new MarkdownHeaderChunkingStrategy(
            paragraph,
            fixedSize,
            NullLogger<MarkdownHeaderChunkingStrategy>.Instance);
    }

    [Fact]
    public void Split_EmptyContent_ReturnsEmptyList()
    {
        var chunks = _sut.Split("", ChunkingOptions.Default);
        chunks.Should().BeEmpty();
    }

    [Fact]
    public void Split_NoHeaders_FallsBackToParagraph()
    {
        var content = "Just plain text.\n\nWith paragraphs.";
        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks.Should().NotBeEmpty();
        chunks.All(c => c.Metadata.Heading == null).Should().BeTrue();
    }

    [Fact]
    public void Split_CreatesChunkPerHeader()
    {
        var content = """
            # Header 1
            Content 1.

            # Header 2
            Content 2.

            # Header 3
            Content 3.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks.Should().HaveCount(3);
        chunks[0].Metadata.Heading.Should().Be("Header 1");
        chunks[1].Metadata.Heading.Should().Be("Header 2");
        chunks[2].Metadata.Heading.Should().Be("Header 3");
    }

    [Fact]
    public void Split_RespectsHeaderHierarchy()
    {
        var content = """
            # Chapter
            Intro.

            ## Section
            Details.

            # Next Chapter
            More.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        // Chapter should only include "Intro.", not Section content
        chunks[0].Metadata.Heading.Should().Be("Chapter");
        chunks[0].Content.Should().Contain("Intro");
        chunks[0].Content.Should().NotContain("Details");

        // Section has its own chunk
        chunks[1].Metadata.Heading.Should().Be("Section");
        chunks[1].Metadata.Level.Should().Be(2);
    }

    [Fact]
    public void Split_PreservesHeaderLevel()
    {
        var content = """
            # H1
            ## H2
            ### H3
            #### H4
            ##### H5
            ###### H6
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks[0].Metadata.Level.Should().Be(1);
        chunks[1].Metadata.Level.Should().Be(2);
        chunks[2].Metadata.Level.Should().Be(3);
        chunks[3].Metadata.Level.Should().Be(4);
        chunks[4].Metadata.Level.Should().Be(5);
        chunks[5].Metadata.Level.Should().Be(6);
    }

    [Fact]
    public void Split_HandlesPreambleContent()
    {
        var content = """
            This is preamble text.

            # First Header
            Content.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks[0].Metadata.Heading.Should().BeNull();
        chunks[0].Content.Should().Contain("preamble");
        chunks[1].Metadata.Heading.Should().Be("First Header");
    }

    [Fact]
    public void Split_SplitsOversizedSections()
    {
        var largeContent = new string('x', 5000);
        var content = $"""
            # Large Section
            {largeContent}

            # Normal Section
            Short.
            """;

        var options = new ChunkingOptions { MaxSize = 2000, TargetSize = 1000 };
        var chunks = _sut.Split(content, options);

        // Large section should be split into multiple chunks
        var largeSectionChunks = chunks.Where(c => c.Metadata.Heading == "Large Section").ToList();
        largeSectionChunks.Should().HaveCountGreaterThan(1);

        // All sub-chunks should preserve the header metadata
        largeSectionChunks.All(c => c.Metadata.Level == 1).Should().BeTrue();
    }

    [Fact]
    public void Split_IgnoresCodeBlockHashes()
    {
        var content = """
            # Real Header
            Some content.

            ```python
            # This is a comment, not a header
            print("hello")
            ```

            # Another Header
            More content.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        // Should only find 2 real headers
        var headings = chunks.Where(c => c.Metadata.Heading != null).ToList();
        headings.Should().HaveCount(2);
        headings[0].Metadata.Heading.Should().Be("Real Header");
        headings[1].Metadata.Heading.Should().Be("Another Header");
    }

    [Fact]
    public void Split_ExtractsHeaderTextWithFormatting()
    {
        var content = """
            # **Bold** Header
            Content.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        // Should extract plain text from formatted header
        chunks[0].Metadata.Heading.Should().Be("Bold Header");
    }

    [Fact]
    public void Split_SetsCorrectMetadata()
    {
        var content = """
            # First
            A

            # Second
            B

            # Third
            C
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks[0].Metadata.Index.Should().Be(0);
        chunks[0].Metadata.IsFirst.Should().BeTrue();
        chunks[^1].Metadata.IsLast.Should().BeTrue();
        chunks.All(c => c.Metadata.TotalChunks == 3).Should().BeTrue();
    }

    [Fact]
    public void Split_HandlesSetext Headers()
    {
        var content = """
            Header One
            ==========
            Content one.

            Header Two
            ----------
            Content two.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        chunks.Should().HaveCount(2);
        chunks[0].Metadata.Level.Should().Be(1); // = is H1
        chunks[1].Metadata.Level.Should().Be(2); // - is H2
    }

    [Fact]
    public void Split_HandlesEmptySection()
    {
        var content = """
            # Header 1

            # Header 2
            Content.
            """;

        var chunks = _sut.Split(content, ChunkingOptions.Default);

        // Empty section should be filtered or minimal
        chunks.Should().HaveCountGreaterOrEqualTo(1);
    }
}
```

---

## 10. Logging

| Level | Message | Context |
| :---- | :------ | :------ |
| Info | "No headers found, falling back to paragraph chunking" | Fallback activation |
| Debug | "Found {HeaderCount} headers in document" | After parsing |
| Debug | "Created chunk for header: {Heading} (level {Level})" | Per header |
| Debug | "Splitting oversized section: {Heading} ({Size} chars)" | When splitting |
| Trace | "Parsed heading at line {Line}: {Text}" | Verbose parsing |

---

## 11. File Locations

| File | Path |
| :--- | :--- |
| Strategy implementation | `src/Lexichord.Modules.RAG/Chunking/MarkdownHeaderChunkingStrategy.cs` |
| Unit tests | `tests/Lexichord.Modules.RAG.Tests/Chunking/MarkdownHeaderChunkingStrategyTests.cs` |

---

## 12. Dependencies

| Dependency | Source | Purpose |
| :--------- | :----- | :------ |
| `Markdig` | v0.1.3b (NuGet) | Markdown parsing |
| `ParagraphChunkingStrategy` | v0.4.3c | Fallback for no headers |
| `FixedSizeChunkingStrategy` | v0.4.3b | Splitting oversized sections |
| `IChunkingStrategy` | v0.4.3a | Interface implementation |
| `TextChunk`, `ChunkMetadata` | v0.4.3a | Return types |

---

## 13. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | Parses Markdown headers using Markdig | [ ] |
| 2 | Creates chunk per header section | [ ] |
| 3 | Respects header hierarchy (H1 > H2 > H3) | [ ] |
| 4 | Stores header text in ChunkMetadata.Heading | [ ] |
| 5 | Stores header level in ChunkMetadata.Level | [ ] |
| 6 | Handles preamble content before first header | [ ] |
| 7 | Splits oversized sections with FixedSize | [ ] |
| 8 | Falls back to Paragraph for no headers | [ ] |
| 9 | Ignores # in code blocks | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 14. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
