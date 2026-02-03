# LCS-CL-052a: Citation Model

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-052a                     |
| **Version**        | v0.5.2a                         |
| **Feature Name**   | Citation Model                  |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |
| **Related Spec**   | [LCS-DES-v0.5.2a](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2a.md) |

---

## Summary

Implemented the Citation Model — the foundation of the Citation Engine (v0.5.2). This sub-part introduces the `Citation` record with complete provenance information, the `ICitationService` interface for creating, formatting, and validating citations, and the `CitationService` implementation that transforms search hits into traceable source attributions with line number calculation.

---

## Changes

### New Files

#### Lexichord.Abstractions

| File | Description |
|:-----|:------------|
| `Contracts/Citation.cs` | Immutable record with provenance fields: ChunkId, DocumentPath, DocumentTitle, StartOffset, EndOffset, Heading, LineNumber, IndexedAt. Includes computed properties FileName, RelativePath, HasHeading, HasLineNumber |
| `Contracts/CitationStyle.cs` | Enum defining three formatting styles: Inline (0), Footnote (1), Markdown (2) |
| `Contracts/ICitationService.cs` | Interface with CreateCitation, CreateCitations, FormatCitation, and ValidateCitationAsync methods |
| `Events/CitationCreatedEvent.cs` | MediatR INotification published when a citation is created, carrying Citation and Timestamp |

#### Lexichord.Modules.RAG

| File | Description |
|:-----|:------------|
| `Services/CitationService.cs` | Singleton implementation of ICitationService with line number calculation, three format methods, license-gated formatting, file-based validation, workspace-relative path resolution, and CitationCreatedEvent publishing |

#### Lexichord.Tests.Unit

| File | Description |
|:-----|:------------|
| `Modules/RAG/Services/CitationServiceTests.cs` | 56 unit tests covering constructor validation, citation creation, batch creation, inline/footnote/markdown formatting, license gating, validation, line number calculation, and record property tests |

### Modified Files

| File | Change |
|:-----|:-------|
| `Constants/FeatureCodes.cs` | Added `Citation` feature code constant (`Feature.Citation`) in new Citation Engine region (v0.5.2) |
| `RAGModule.cs` | Updated module version from 0.5.1 to 0.5.2. Added singleton registration of `ICitationService` → `CitationService` |

---

## Technical Details

### Citation Creation Flow

1. Validate SearchHit is not null
2. Extract Document and TextChunk from the hit
3. Determine title (frontmatter title or filename fallback)
4. Extract heading from ChunkMetadata when present
5. Calculate line number by reading source file and counting newlines to offset
6. Compute workspace-relative path when workspace is open
7. Build Citation record with all provenance fields
8. Publish CitationCreatedEvent via MediatR (fire-and-forget)

### Line Number Calculation

- Reads the entire source file into memory
- Counts `\n` characters from position 0 to the chunk's StartOffset
- Returns 1-indexed line number (first line = 1)
- Returns null on any failure (file not found, access denied, offset out of bounds)
- Method visibility is `internal` for direct unit testing

### Citation Formatting

| Style | Format | Example |
|:------|:-------|:--------|
| Inline | `[filename.md, §Heading]` | `[auth-guide.md, §Authentication]` |
| Footnote | `[^XXXXXXXX]: /path:line` | `[^aabbccdd]: /docs/auth.md:42` |
| Markdown | `[Title](file:///path#Lline)` | `[Test Document](file:///docs/test.md#L42)` |

### License Gating Strategy

| Tier | Behavior |
|:-----|:---------|
| Core | `FormatCitation` returns DocumentPath only |
| WriterPro+ | Full formatted citation in requested style |

License gating uses `ILicenseContext.IsFeatureEnabled(FeatureCodes.Citation)` at the formatting layer. Citation creation itself is always permitted.

### Validation Logic

Basic timestamp-based validation for v0.5.2a:
1. Check if file exists at `Citation.DocumentPath`
2. Compare `FileInfo.LastWriteTimeUtc` against `Citation.IndexedAt`
3. Return `true` if unchanged, `false` if stale or missing

Full hash-based validation is deferred to `ICitationValidator` (v0.5.2c).

### Dependencies

| Dependency | Version | Purpose |
|:-----------|:--------|:--------|
| IWorkspaceService | v0.1.2a | Workspace root for relative path resolution |
| ILicenseContext | v0.0.4c | License-gated formatting |
| IMediator | v0.0.7a | CitationCreatedEvent publishing |
| ILogger&lt;T&gt; | v0.0.3b | Structured logging |
| SearchHit | v0.4.5a | Source data for citation creation |
| Document | v0.4.1c | Document metadata (path, title, IndexedAt) |
| TextChunk | v0.4.3a | Chunk content and positional offsets |
| ChunkMetadata | v0.4.3a | Heading context from chunking |

---

## Verification

### Unit Tests

All 56 tests passed:

- Constructor null-parameter validation (4 tests)
- CreateCitation from SearchHit (10 tests)
- CreateCitations batch (3 tests)
- FormatCitation Inline style (2 tests)
- FormatCitation Footnote style (3 tests)
- FormatCitation Markdown style (3 tests)
- FormatCitation license gating (2 tests)
- FormatCitation error cases (2 tests)
- FormatCitation style theory (3 tests)
- ValidateCitationAsync scenarios (4 tests)
- CalculateLineNumber (4 tests)
- Citation record property tests (6 tests)
- CitationStyle enum tests (2 tests)
- CitationCreatedEvent test (1 test)

### Build Verification

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### Regression Check

```
dotnet test tests/Lexichord.Tests.Unit
Passed: 5154, Skipped: 33, Failed: 0
```

---

## Deliverable Checklist

| # | Deliverable | Status |
|:--|:------------|:-------|
| 1 | `Citation` record in Abstractions/Contracts | [x] |
| 2 | `CitationStyle` enum in Abstractions/Contracts | [x] |
| 3 | `ICitationService` interface | [x] |
| 4 | `CitationService` implementation | [x] |
| 5 | Line number calculation from offset | [x] |
| 6 | `CitationCreatedEvent` MediatR notification | [x] |
| 7 | All three format methods (Inline/Footnote/MD) | [x] |
| 8 | Unit tests for CreateCitation | [x] |
| 9 | Unit tests for FormatCitation | [x] |
| 10 | Unit tests for line number calculation | [x] |
| 11 | DI registration in RAGModule.cs | [x] |

---

## Design Adaptations

The specification referenced `IDocumentRepository` as a constructor dependency, but the actual implementation does not require it because:
- Document metadata is available directly from `SearchHit.Document`
- File system access for line number calculation uses `System.IO.File` directly
- Workspace-relative paths use `IWorkspaceService` instead

The specification's `FeatureFlags.RAG.Citation` was adapted to the codebase's actual pattern: `FeatureCodes.Citation` (matching the existing `FeatureCodes` static class convention established in v0.3.1d).

---

## Related Documents

- [LCS-DES-v0.5.2a](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2a.md) — Design specification
- [LCS-DES-v0.5.2-INDEX](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2-INDEX.md) — Feature index
- [LCS-SBD-v0.5.2](../../specs/v0.5.x/v0.5.2/LCS-SBD-v0.5.2.md) — Scope breakdown
- [LCS-CL-051d](./LCS-CL-051d.md) — Search Mode Toggle (prerequisite: v0.5.1 complete)
