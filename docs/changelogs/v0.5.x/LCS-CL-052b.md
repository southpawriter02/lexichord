# LCS-CL-052b: Citation Styles

## Document Control

| Field              | Value                           |
| :----------------- | :------------------------------ |
| **Document ID**    | LCS-CL-052b                     |
| **Version**        | v0.5.2b                         |
| **Feature Name**   | Citation Styles                 |
| **Module**         | Lexichord.Modules.RAG           |
| **Status**         | Complete                        |
| **Last Updated**   | 2026-02-02                      |
| **Related Spec**   | [LCS-DES-v0.5.2b](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2b.md) |

---

## Summary

Implemented the Citation Styles system — style-specific formatters and a registry with user preference support (v0.5.2b). This sub-part introduces the `ICitationFormatter` interface for extensible formatting strategies, three built-in formatter implementations (Inline, Footnote, Markdown), the `CitationFormatterRegistry` for style lookup and preference management, and `CitationSettingsKeys` constants for persisting user preferences.

---

## Changes

### New Files

#### Lexichord.Abstractions

| File | Description |
|:-----|:------------|
| `Contracts/ICitationFormatter.cs` | Interface defining the contract for style-specific citation formatters. Includes Style, DisplayName, Description, Example properties and Format/FormatForClipboard methods |
| `Constants/CitationSettingsKeys.cs` | Constants for citation-related user preference keys: DefaultStyle, IncludeLineNumbers, UseRelativePaths |

#### Lexichord.Modules.RAG

| File | Description |
|:-----|:------------|
| `Formatters/InlineCitationFormatter.cs` | Formats citations as `[filename.md, §Heading]`. Omits heading suffix when not available |
| `Formatters/FootnoteCitationFormatter.cs` | Formats citations as `[^id]: /path/to/doc.md:line`. Uses first 8 hex chars of ChunkId as footnote identifier |
| `Formatters/MarkdownCitationFormatter.cs` | Formats citations as `[Title](file:///path#L42)`. URL-encodes spaces in paths |
| `Services/CitationFormatterRegistry.cs` | Registry collecting all ICitationFormatter implementations. Provides style lookup, user preference persistence via ISystemSettingsRepository, and preferred formatter access |

#### Lexichord.Tests.Unit

| File | Description |
|:-----|:------------|
| `Modules/RAG/Formatters/InlineCitationFormatterTests.cs` | 8 tests: metadata properties, format with/without heading, filename extraction, clipboard parity, null validation |
| `Modules/RAG/Formatters/FootnoteCitationFormatterTests.cs` | 9 tests: metadata properties, format with/without line number, footnote ID format, full path usage, clipboard parity, null validation |
| `Modules/RAG/Formatters/MarkdownCitationFormatterTests.cs` | 10 tests: metadata properties, format with/without line number, space URL-encoding, title as link text, file scheme, clipboard parity, null validation |
| `Modules/RAG/Services/CitationFormatterRegistryTests.cs` | 26 tests: constructor validation, All property, GetFormatter for each style, GetFormatter for unregistered style, GetPreferredStyleAsync (default/persisted/invalid/case-insensitive), SetPreferredStyleAsync, GetPreferredFormatterAsync, duplicate registration |

### Modified Files

| File | Change |
|:-----|:-------|
| `RAGModule.cs` | Added DI registrations: three ICitationFormatter singletons (InlineCitationFormatter, FootnoteCitationFormatter, MarkdownCitationFormatter) and CitationFormatterRegistry singleton |

---

## Technical Details

### Formatter Architecture

Each formatter implements `ICitationFormatter` and handles a single `CitationStyle`. The formatters are stateless singletons registered in DI, collected by `CitationFormatterRegistry` via `IEnumerable<ICitationFormatter>`.

```
ICitationFormatter
├── InlineCitationFormatter    → CitationStyle.Inline
├── FootnoteCitationFormatter  → CitationStyle.Footnote
└── MarkdownCitationFormatter  → CitationStyle.Markdown
```

### Format Outputs

| Style | Format | Example |
|:------|:-------|:--------|
| Inline | `[filename.md, §Heading]` | `[auth-guide.md, §Authentication]` |
| Footnote | `[^XXXXXXXX]: /path:line` | `[^aabbccdd]: /docs/auth.md:42` |
| Markdown | `[Title](file:///path#Lline)` | `[OAuth Guide](file:///docs/auth.md#L42)` |

### CitationFormatterRegistry

- **Style Lookup:** O(1) dictionary lookup from `CitationStyle` → `ICitationFormatter`.
- **User Preference:** Reads/writes `Citation.DefaultStyle` key via `ISystemSettingsRepository`.
- **Default Style:** `Inline` when preference is not set or contains an invalid value.
- **Case-Insensitive:** Style name parsing uses `Enum.TryParse` with `ignoreCase: true`.
- **Duplicate Handling:** If multiple formatters claim the same style, the last one wins (with a warning log).

### Settings Keys

| Key | Type | Default | Description |
|:----|:-----|:--------|:------------|
| `Citation.DefaultStyle` | string | `"Inline"` | User's preferred citation format |
| `Citation.IncludeLineNumbers` | bool | `true` | Whether to include line numbers |
| `Citation.UseRelativePaths` | bool | `false` | Whether to use workspace-relative paths |

### Dependencies

| Dependency | Version | Purpose |
|:-----------|:--------|:--------|
| Citation record | v0.5.2a | Citation data to format |
| CitationStyle enum | v0.5.2a | Style identification |
| ISystemSettingsRepository | v0.0.5d | User preference persistence |
| ILogger&lt;T&gt; | v0.0.3b | Structured logging |

### Design Adaptations

The specification referenced `ISettingsService` as the settings persistence mechanism, but the actual codebase uses `ISystemSettingsRepository` (v0.0.5d). The registry was adapted to use the async `GetValueAsync`/`SetValueAsync` methods from `ISystemSettingsRepository`, making `GetPreferredStyleAsync` and `SetPreferredStyleAsync` async (rather than the synchronous `Get`/`SetAsync` pattern in the spec).

The specification placed `CitationSettingsKeys` in `Lexichord.Abstractions.Settings` namespace, but the codebase convention keeps constants in `Lexichord.Abstractions.Constants` (alongside `FeatureCodes` and `VaultKeys`).

---

## Verification

### Unit Tests

All 53 tests passed:

- InlineCitationFormatter tests (8 tests)
  - Metadata properties: Style, DisplayName, Description, Example
  - Format with heading, without heading, with empty heading
  - Filename extraction from full path
  - FormatForClipboard parity
  - Null citation validation (Format and FormatForClipboard)

- FootnoteCitationFormatter tests (9 tests)
  - Metadata properties: Style, DisplayName, Description, Example
  - Format with line number, without line number
  - Footnote ID from ChunkId hex
  - Full document path usage
  - FormatForClipboard parity
  - Null citation validation (Format and FormatForClipboard)

- MarkdownCitationFormatter tests (10 tests)
  - Metadata properties: Style, DisplayName, Description, Example
  - Format with line number, without line number
  - Space URL-encoding in paths
  - DocumentTitle as link text
  - file:// scheme usage
  - FormatForClipboard parity
  - Null citation validation (Format and FormatForClipboard)

- CitationFormatterRegistry tests (26 tests)
  - Constructor null validation (3 tests)
  - All property (2 tests)
  - GetFormatter for each style (3 tests)
  - GetFormatter for unregistered style (1 test)
  - GetPreferredStyleAsync: default, persisted, invalid, case-insensitive (5 tests)
  - SetPreferredStyleAsync persistence (2 tests)
  - GetPreferredFormatterAsync (2 tests)
  - Duplicate formatter registration (1 test)

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
Passed: 5207, Skipped: 33, Failed: 0
```

---

## Deliverable Checklist

| # | Deliverable | Status |
|:--|:------------|:-------|
| 1 | `ICitationFormatter` interface | [x] |
| 2 | `InlineCitationFormatter` implementation | [x] |
| 3 | `FootnoteCitationFormatter` implementation | [x] |
| 4 | `MarkdownCitationFormatter` implementation | [x] |
| 5 | `CitationFormatterRegistry` service | [x] |
| 6 | `CitationSettingsKeys` constants | [x] |
| 7 | Settings UI for citation style selection | [ ] (deferred — UI component, not part of service layer) |
| 8 | Context menu "Copy as..." submenu | [ ] (deferred — v0.5.2d copy actions) |
| 9 | Unit tests for each formatter | [x] |
| 10 | Unit tests for registry | [x] |
| 11 | DI registration | [x] |

---

## Related Documents

- [LCS-DES-v0.5.2b](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2b.md) — Design specification
- [LCS-DES-v0.5.2-INDEX](../../specs/v0.5.x/v0.5.2/LCS-DES-v0.5.2-INDEX.md) — Feature index
- [LCS-SBD-v0.5.2](../../specs/v0.5.x/v0.5.2/LCS-SBD-v0.5.2.md) — Scope breakdown
- [LCS-CL-052a](./LCS-CL-052a.md) — Citation Model (prerequisite: v0.5.2a complete)
