# LCS-CL-052d: Citation Copy Actions

**Version**: 0.5.2d  
**Category**: Feature Enhancement  
**Module**: Lexichord.Modules.RAG  
**Scope**: Citation Engine · Clipboard Operations  
**Status**: ✅ Complete

---

## Summary

This release implements clipboard copy functionality for citations, enabling users to copy formatted citations, raw chunk text, and document paths directly from search results. The feature integrates with the existing Citation Engine (v0.5.2a-c) and provides a context menu with multiple copy options.

---

## Key Changes

### Lexichord.Abstractions

| File | Change | Description |
|------|--------|-------------|
| `Contracts/ICitationClipboardService.cs` | [NEW] | Interface for clipboard operations on citations |
| `Contracts/CitationCopyFormat.cs` | [NEW] | Enum: FormattedCitation, ChunkText, DocumentPath, FileUri |
| `Events/CitationCopiedEvent.cs` | [NEW] | MediatR notification for telemetry and toast notifications |

### Lexichord.Modules.RAG

| File | Change | Description |
|------|--------|-------------|
| `Services/CitationClipboardService.cs` | [NEW] | Implementation with Avalonia clipboard integration |
| `ViewModels/SearchResultItemViewModel.cs` | [MODIFY] | Added copy commands: CopyCitation, CopyAsCitationStyle, CopyChunkText, CopyDocumentPath |
| `Views/SearchResultItemView.axaml` | [MODIFY] | Added context menu with copy actions and keyboard shortcuts |
| `RAGModule.cs` | [MODIFY] | DI registration for ICitationClipboardService |

### Lexichord.Tests.Unit

| File | Change | Description |
|------|--------|-------------|
| `Modules/RAG/Services/CitationClipboardServiceTests.cs` | [NEW] | 10 unit tests covering constructor and argument validation |

---

## Feature Details

### Copy Operations

| Operation | Description | License |
|-----------|-------------|---------|
| Copy Citation | Copies formatted citation using preferred style | Gated via ICitationService |
| Copy as Inline | [filename.md, §Heading] format | WriterPro+ |
| Copy as Footnote | [^ref]: /path:line format | WriterPro+ |
| Copy as Markdown | [Title](file:///path#L42) format | WriterPro+ |
| Copy Chunk Text | Raw chunk content | All users |
| Copy Path | Absolute file path | All users |

### Context Menu

```
┌─────────────────────────────┐
│ 📋 Copy Citation     Ctrl+C │
│ ▶ Copy as...                │
│   ├─ Inline [doc.md, §Sec]  │
│   ├─ Footnote [^ref]: path  │
│   └─ Markdown [Title](url)  │
├─────────────────────────────┤
│ 📄 Copy Chunk Text          │
│ 📁 Copy Path                │
├─────────────────────────────┤
│ ↗️ Open in Editor           │
└─────────────────────────────┘
```

### Telemetry

All copy operations publish `CitationCopiedEvent` with:
- Citation reference
- Copy format (FormattedCitation/ChunkText/DocumentPath/FileUri)
- Citation style (when applicable)
- UTC timestamp

---

## Dependencies

| Dependency | Version | Usage |
|------------|---------|-------|
| `ICitationService` | v0.5.2a | Citation creation and formatting |
| `CitationFormatterRegistry` | v0.5.2b | User style preferences |
| `Avalonia.IClipboard` | - | System clipboard access |
| `MediatR.INotification` | - | Event publishing |

---

## Testing

- **Unit Tests**: 10 tests covering constructor validation and argument validation
- **Build Verification**: ✅ 0 errors, 0 warnings
- **Integration**: Requires Avalonia application context for clipboard operations

---

## Migration Notes

No breaking changes. New functionality is additive.

---

## Related Specifications

- [LCS-DES-v0.5.2d](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2d.md) — Design specification
- [LCS-DES-v0.5.2-INDEX](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2-INDEX.md) — Version index
