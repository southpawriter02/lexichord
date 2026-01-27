# LCS-DES-043a: Chunking Abstractions

## Document Control

| Field            | Value                                    |
| :--------------- | :--------------------------------------- |
| **Document ID**  | LCS-DES-043a                             |
| **Version**      | v0.4.3a                                  |
| **Title**        | Chunking Abstractions                    |
| **Status**       | Draft                                    |
| **Last Updated** | 2026-01-27                               |
| **Owner**        | Lead Architect                           |
| **Module**       | `Lexichord.Abstractions`                 |
| **License Tier** | Core                                     |

---

## 1. Overview

### 1.1 Purpose

This specification defines the core interfaces, enums, and records that form the foundation of the chunking system. These abstractions enable pluggable chunking algorithms and provide a consistent contract for all chunking strategies.

### 1.2 Goals

- Define `IChunkingStrategy` interface for all chunking implementations
- Create `ChunkingMode` enum for strategy selection
- Design `TextChunk` and `ChunkMetadata` records for chunk output
- Establish `ChunkingOptions` for configurable behavior
- Enable extensibility for future chunking algorithms

### 1.3 Non-Goals

- Implementing specific chunking algorithms (v0.4.3b-d)
- UI for configuration (v0.4.7)
- Semantic/NLP-based chunking (v0.6.x)

---

## 2. Design

### 2.1 ChunkingMode Enum

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Available chunking strategies for document processing.
/// </summary>
public enum ChunkingMode
{
    /// <summary>
    /// Split by character count with overlap.
    /// Suitable for unstructured text or when consistent chunk sizes are needed.
    /// </summary>
    FixedSize = 0,

    /// <summary>
    /// Split on paragraph boundaries (double newlines).
    /// Preserves natural text structure, merges short paragraphs.
    /// </summary>
    Paragraph = 1,

    /// <summary>
    /// Split on Markdown headers for hierarchical chunking.
    /// Preserves document structure and section context.
    /// </summary>
    MarkdownHeader = 2,

    /// <summary>
    /// Split using semantic analysis (future).
    /// Uses NLP to find natural topic boundaries.
    /// </summary>
    Semantic = 3
}
```

### 2.2 IChunkingStrategy Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Strategy interface for splitting text into chunks.
/// Implementations provide different algorithms for identifying chunk boundaries.
/// </summary>
public interface IChunkingStrategy
{
    /// <summary>
    /// Gets the chunking mode this strategy implements.
    /// </summary>
    ChunkingMode Mode { get; }

    /// <summary>
    /// Splits the provided content into chunks.
    /// </summary>
    /// <param name="content">The text content to split. Must not be null.</param>
    /// <param name="options">Chunking configuration options.</param>
    /// <returns>
    /// An ordered list of text chunks with position information.
    /// Returns empty list for null or empty content.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is null.
    /// </exception>
    IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options);
}
```

### 2.3 TextChunk Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a chunk of text extracted from a document.
/// Includes the content, position information, and metadata.
/// </summary>
/// <param name="Content">
/// The text content of this chunk. May be trimmed based on options.
/// </param>
/// <param name="StartOffset">
/// Zero-based character offset from the start of the original document.
/// </param>
/// <param name="EndOffset">
/// Character offset marking the end of this chunk (exclusive).
/// </param>
/// <param name="Metadata">
/// Additional context about this chunk's position and structure.
/// </param>
public record TextChunk(
    string Content,
    int StartOffset,
    int EndOffset,
    ChunkMetadata Metadata)
{
    /// <summary>
    /// Gets the length of this chunk in characters.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Gets whether this chunk has meaningful content.
    /// </summary>
    public bool HasContent => !string.IsNullOrWhiteSpace(Content);

    /// <summary>
    /// Gets a preview of the content (first 100 chars).
    /// </summary>
    public string Preview => Content.Length <= 100
        ? Content
        : Content[..100] + "...";
}
```

### 2.4 ChunkMetadata Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Metadata about a chunk's context within its source document.
/// Provides information for navigation and context preservation.
/// </summary>
/// <param name="Index">
/// Zero-based index of this chunk within the document.
/// </param>
/// <param name="Heading">
/// Section heading this chunk belongs to, if applicable.
/// Null for chunks without heading context.
/// </param>
/// <param name="Level">
/// Heading level (1-6) if this chunk is under a header.
/// Zero if no heading applies.
/// </param>
public record ChunkMetadata(
    int Index,
    string? Heading = null,
    int Level = 0)
{
    /// <summary>
    /// Total number of chunks in the document.
    /// Set after all chunks are created.
    /// </summary>
    public int TotalChunks { get; init; }

    /// <summary>
    /// Gets whether this is the first chunk in the document.
    /// </summary>
    public bool IsFirst => Index == 0;

    /// <summary>
    /// Gets whether this is the last chunk in the document.
    /// </summary>
    public bool IsLast => TotalChunks > 0 && Index == TotalChunks - 1;

    /// <summary>
    /// Gets the relative position (0.0 to 1.0) within the document.
    /// </summary>
    public double RelativePosition => TotalChunks > 0
        ? (double)Index / TotalChunks
        : 0.0;

    /// <summary>
    /// Gets whether this chunk has heading context.
    /// </summary>
    public bool HasHeading => !string.IsNullOrEmpty(Heading);
}
```

### 2.5 ChunkingOptions Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for chunking behavior.
/// All sizes are in characters unless otherwise noted.
/// </summary>
public record ChunkingOptions
{
    /// <summary>
    /// Default options with sensible defaults for general use.
    /// </summary>
    public static ChunkingOptions Default { get; } = new();

    /// <summary>
    /// Target chunk size in characters.
    /// Chunks will be approximately this size, adjusted for boundaries.
    /// Default: 1000 characters.
    /// </summary>
    public int TargetSize { get; init; } = 1000;

    /// <summary>
    /// Number of characters to overlap between consecutive chunks.
    /// Provides context continuity across chunk boundaries.
    /// Default: 100 characters.
    /// </summary>
    public int Overlap { get; init; } = 100;

    /// <summary>
    /// Minimum chunk size before merging with adjacent chunks.
    /// Prevents creation of very small, low-context chunks.
    /// Default: 200 characters.
    /// </summary>
    public int MinSize { get; init; } = 200;

    /// <summary>
    /// Maximum chunk size before forced splitting.
    /// Prevents chunks too large for embedding models.
    /// Default: 2000 characters.
    /// </summary>
    public int MaxSize { get; init; } = 2000;

    /// <summary>
    /// Whether to avoid splitting in the middle of words.
    /// When true, chunk boundaries are adjusted to word edges.
    /// Default: true.
    /// </summary>
    public bool RespectWordBoundaries { get; init; } = true;

    /// <summary>
    /// Whether to preserve leading/trailing whitespace in chunks.
    /// When false, chunks are trimmed.
    /// Default: false.
    /// </summary>
    public bool PreserveWhitespace { get; init; } = false;

    /// <summary>
    /// Whether to include empty or whitespace-only chunks.
    /// When false, empty chunks are filtered out.
    /// Default: false.
    /// </summary>
    public bool IncludeEmptyChunks { get; init; } = false;

    /// <summary>
    /// Validates that the options are internally consistent.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when options are invalid.
    /// </exception>
    public void Validate()
    {
        if (TargetSize <= 0)
            throw new ArgumentException("TargetSize must be positive", nameof(TargetSize));

        if (Overlap < 0)
            throw new ArgumentException("Overlap cannot be negative", nameof(Overlap));

        if (Overlap >= TargetSize)
            throw new ArgumentException("Overlap must be less than TargetSize", nameof(Overlap));

        if (MinSize < 0)
            throw new ArgumentException("MinSize cannot be negative", nameof(MinSize));

        if (MaxSize <= MinSize)
            throw new ArgumentException("MaxSize must be greater than MinSize", nameof(MaxSize));

        if (TargetSize > MaxSize)
            throw new ArgumentException("TargetSize cannot exceed MaxSize", nameof(TargetSize));
    }
}
```

---

## 3. Configuration Presets

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Predefined chunking configurations for common use cases.
/// </summary>
public static class ChunkingPresets
{
    /// <summary>
    /// Small chunks for high-precision retrieval.
    /// Best for: FAQ-style content, short documents.
    /// </summary>
    public static ChunkingOptions HighPrecision { get; } = new()
    {
        TargetSize = 500,
        Overlap = 50,
        MinSize = 100,
        MaxSize = 1000
    };

    /// <summary>
    /// Default balanced configuration.
    /// Best for: General documents, articles, blog posts.
    /// </summary>
    public static ChunkingOptions Balanced { get; } = ChunkingOptions.Default;

    /// <summary>
    /// Large chunks for context-heavy retrieval.
    /// Best for: Technical documentation, long-form content.
    /// </summary>
    public static ChunkingOptions HighContext { get; } = new()
    {
        TargetSize = 2000,
        Overlap = 200,
        MinSize = 500,
        MaxSize = 4000
    };

    /// <summary>
    /// Configuration optimized for code or structured content.
    /// Uses smaller overlap and allows larger chunks.
    /// </summary>
    public static ChunkingOptions Code { get; } = new()
    {
        TargetSize = 1500,
        Overlap = 50,
        MinSize = 200,
        MaxSize = 3000,
        RespectWordBoundaries = false,
        PreserveWhitespace = true
    };
}
```

---

## 4. Usage Examples

### 4.1 Basic Chunking

```csharp
// Get strategy from factory
var strategy = chunkingFactory.GetStrategy(ChunkingMode.Paragraph);

// Chunk with default options
var chunks = strategy.Split(documentContent, ChunkingOptions.Default);

foreach (var chunk in chunks)
{
    Console.WriteLine($"Chunk {chunk.Metadata.Index + 1}/{chunk.Metadata.TotalChunks}");
    Console.WriteLine($"  Offset: {chunk.StartOffset}-{chunk.EndOffset}");
    Console.WriteLine($"  Preview: {chunk.Preview}");
    if (chunk.Metadata.HasHeading)
    {
        Console.WriteLine($"  Heading: {chunk.Metadata.Heading} (H{chunk.Metadata.Level})");
    }
}
```

### 4.2 Custom Options

```csharp
// Configure for small, precise chunks
var options = new ChunkingOptions
{
    TargetSize = 500,
    Overlap = 100,
    MinSize = 100,
    MaxSize = 800
};

options.Validate(); // Throws if invalid

var strategy = chunkingFactory.GetStrategy(ChunkingMode.FixedSize);
var chunks = strategy.Split(content, options);
```

### 4.3 Auto-Detection

```csharp
// Let factory choose based on content
var strategy = chunkingFactory.GetStrategy(content, fileExtension: ".md");

// Will choose MarkdownHeader for .md files
var chunks = strategy.Split(content, ChunkingOptions.Default);
```

---

## 5. Validation Rules

| Rule | Constraint | Error |
| :--- | :--------- | :---- |
| TargetSize | > 0 | "TargetSize must be positive" |
| Overlap | >= 0 | "Overlap cannot be negative" |
| Overlap | < TargetSize | "Overlap must be less than TargetSize" |
| MinSize | >= 0 | "MinSize cannot be negative" |
| MaxSize | > MinSize | "MaxSize must be greater than MinSize" |
| TargetSize | <= MaxSize | "TargetSize cannot exceed MaxSize" |

---

## 6. Extension Points

### 6.1 Adding New Strategies

```csharp
// 1. Create implementation
public class SentenceChunkingStrategy : IChunkingStrategy
{
    public ChunkingMode Mode => ChunkingMode.Sentence; // Would need enum extension

    public IReadOnlyList<TextChunk> Split(string content, ChunkingOptions options)
    {
        // Implementation using sentence tokenizer
    }
}

// 2. Register in DI
services.AddSingleton<SentenceChunkingStrategy>();

// 3. Update factory to include new strategy
```

### 6.2 Custom Metadata

```csharp
// For domain-specific needs, extend ChunkMetadata
public record ExtendedChunkMetadata(
    int Index,
    string? Heading = null,
    int Level = 0) : ChunkMetadata(Index, Heading, Level)
{
    public string? Author { get; init; }
    public DateOnly? Date { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
}
```

---

## 7. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3a")]
public class ChunkingAbstractionsTests
{
    [Fact]
    public void TextChunk_Length_CalculatedCorrectly()
    {
        // Arrange
        var chunk = new TextChunk("Hello", 10, 15, new ChunkMetadata(0));

        // Assert
        chunk.Length.Should().Be(5);
    }

    [Fact]
    public void TextChunk_Preview_TruncatesLongContent()
    {
        // Arrange
        var longContent = new string('a', 200);
        var chunk = new TextChunk(longContent, 0, 200, new ChunkMetadata(0));

        // Assert
        chunk.Preview.Should().HaveLength(103); // 100 + "..."
        chunk.Preview.Should().EndWith("...");
    }

    [Fact]
    public void ChunkMetadata_IsFirst_TrueForIndexZero()
    {
        // Arrange
        var metadata = new ChunkMetadata(0) { TotalChunks = 5 };

        // Assert
        metadata.IsFirst.Should().BeTrue();
        metadata.IsLast.Should().BeFalse();
    }

    [Fact]
    public void ChunkMetadata_IsLast_TrueForLastIndex()
    {
        // Arrange
        var metadata = new ChunkMetadata(4) { TotalChunks = 5 };

        // Assert
        metadata.IsFirst.Should().BeFalse();
        metadata.IsLast.Should().BeTrue();
    }

    [Fact]
    public void ChunkMetadata_RelativePosition_CalculatedCorrectly()
    {
        // Arrange
        var metadata = new ChunkMetadata(2) { TotalChunks = 4 };

        // Assert
        metadata.RelativePosition.Should().Be(0.5);
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForInvalidTargetSize()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = 0 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*TargetSize*");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForOverlapExceedingTarget()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = 100, Overlap = 150 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*Overlap*");
    }

    [Fact]
    public void ChunkingOptions_Default_IsValid()
    {
        // Act & Assert
        ChunkingOptions.Default.Invoking(o => o.Validate())
            .Should().NotThrow();
    }

    [Theory]
    [InlineData(ChunkingMode.FixedSize)]
    [InlineData(ChunkingMode.Paragraph)]
    [InlineData(ChunkingMode.MarkdownHeader)]
    public void ChunkingMode_AllValuesAreDefined(ChunkingMode mode)
    {
        // Assert
        Enum.IsDefined(typeof(ChunkingMode), mode).Should().BeTrue();
    }

    [Fact]
    public void ChunkingPresets_AllPresetsAreValid()
    {
        // Act & Assert
        ChunkingPresets.HighPrecision.Invoking(o => o.Validate()).Should().NotThrow();
        ChunkingPresets.Balanced.Invoking(o => o.Validate()).Should().NotThrow();
        ChunkingPresets.HighContext.Invoking(o => o.Validate()).Should().NotThrow();
        ChunkingPresets.Code.Invoking(o => o.Validate()).Should().NotThrow();
    }
}
```

---

## 8. File Locations

| File | Path |
| :--- | :--- |
| ChunkingMode enum | `src/Lexichord.Abstractions/Contracts/ChunkingMode.cs` |
| IChunkingStrategy | `src/Lexichord.Abstractions/Contracts/IChunkingStrategy.cs` |
| TextChunk record | `src/Lexichord.Abstractions/Contracts/TextChunk.cs` |
| ChunkMetadata record | `src/Lexichord.Abstractions/Contracts/ChunkMetadata.cs` |
| ChunkingOptions record | `src/Lexichord.Abstractions/Contracts/ChunkingOptions.cs` |
| ChunkingPresets class | `src/Lexichord.Abstractions/Contracts/ChunkingPresets.cs` |
| Unit tests | `tests/Lexichord.Abstractions.Tests/ChunkingAbstractionsTests.cs` |

---

## 9. Dependencies

| Dependency | Version | Purpose |
| :--------- | :------ | :------ |
| None | — | Pure abstractions with no external dependencies |

---

## 10. Acceptance Criteria

| # | Criterion | Status |
| :- | :-------- | :----- |
| 1 | ChunkingMode enum has FixedSize, Paragraph, MarkdownHeader, Semantic | [ ] |
| 2 | IChunkingStrategy defines Mode property and Split method | [ ] |
| 3 | TextChunk includes Content, StartOffset, EndOffset, Metadata | [ ] |
| 4 | TextChunk.Length computed correctly | [ ] |
| 5 | ChunkMetadata includes Index, Heading, Level, TotalChunks | [ ] |
| 6 | ChunkMetadata provides IsFirst, IsLast, RelativePosition | [ ] |
| 7 | ChunkingOptions has all configurable properties | [ ] |
| 8 | ChunkingOptions.Validate() enforces all constraints | [ ] |
| 9 | ChunkingPresets provides HighPrecision, Balanced, HighContext, Code | [ ] |
| 10 | All unit tests pass | [ ] |

---

## 11. Revision History

| Version | Date       | Author         | Changes                    |
| :------ | :--------- | :------------- | :------------------------- |
| 0.1     | 2026-01-27 | Lead Architect | Initial draft              |

---
