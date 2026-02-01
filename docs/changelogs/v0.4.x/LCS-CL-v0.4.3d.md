# LCS-CL-v0.4.3d: Markdown Header Chunking Strategy

**Version**: 0.4.3d  
**Status**: Released  
**Specification**: [LCS-DES-v0.4.3d](../specs/v0.4.x/v0.4.3/LCS-DES-v0.4.3d.md)

---

## Summary

Implements the `MarkdownHeaderChunkingStrategy` for the RAG pipeline, providing semantic
chunking based on Markdown header structure. This strategy preserves document hierarchy
by creating chunks at header boundaries, enabling context-aware retrieval.

---

## Changes

### Added

- **MarkdownHeaderChunkingStrategy.cs** (`src/Lexichord.Modules.RAG/Chunking/`)
    - Parses Markdown using Markdig to extract `HeadingBlock` elements
    - Creates hierarchical chunks respecting header levels (H1 > H2 > H3, etc.)
    - Handles preamble content before the first header as a separate chunk
    - Uses `ParagraphChunkingStrategy` as fallback for content without headers
    - Uses `FixedSizeChunkingStrategy` to split oversized sections
    - Preserves header text and level in `ChunkMetadata.Heading` and `ChunkMetadata.Level`
    - Extracts plain text from formatted headers (bold, italic, code, links)
    - Supports both ATX-style (`#`) and Setext-style (`===`/`---`) headers

- **MarkdownHeaderChunkingStrategyTests.cs** (`tests/Lexichord.Tests.Unit/Modules/RAG/Chunking/`)
    - 38 unit tests covering all chunking scenarios
    - Empty/null content handling
    - No-header fallback to paragraph strategy
    - Single and multiple header chunking
    - Header hierarchy detection (H2 ends at H1/H2, not H3)
    - Preamble chunk creation
    - Oversized section splitting with fallback
    - Header text extraction with formatting
    - Setext header recognition
    - Metadata correctness (Index, TotalChunks, IsFirst, IsLast)
    - Unicode and emoji header support

### Modified

- **Lexichord.Modules.RAG.csproj**
    - Added `Markdig` v0.37.0 NuGet package for Markdown parsing

- **RAGModule.cs**
    - Registered `MarkdownHeaderChunkingStrategy` as singleton

---

## Dependencies

| Package | Version | Purpose                                |
| ------- | ------- | -------------------------------------- |
| Markdig | 0.37.0  | Markdown parsing and header extraction |

---

## Technical Notes

### Header Hierarchy Logic

The strategy ends a section when encountering a header of the same or higher level:

```
- H1 ends at: H1
- H2 ends at: H1, H2
- H3 ends at: H1, H2, H3
```

This preserves the logical grouping where subsections (H2, H3) remain within their
parent section (H1).

### Header Text Extraction

The `ExtractHeaderText` method recursively walks the inline content tree to extract
plain text from:

- `LiteralInline`: Regular text
- `CodeInline`: Inline code spans
- `ContainerInline`: Nested formatting (bold, italic, links)

### Chunk Metadata

Each chunk includes:

- `Level`: Header level (0 for preamble, 1-6 for headers)
- `Heading`: Plain text of the header (null for preamble)
- `Index`, `TotalChunks`: Position information
- `IsFirst`, `IsLast`: Computed boundary properties

---

## Testing

```bash
# Run MarkdownHeaderChunkingStrategy tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~MarkdownHeaderChunkingStrategy"
```

**Results**: 38/38 tests passing
