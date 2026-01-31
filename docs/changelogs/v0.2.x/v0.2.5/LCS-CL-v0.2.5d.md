# v0.2.5d: Bulk Import/Export

**Version:** v0.2.5d  
**Codename:** The Librarian - Import/Export  
**Release Date:** 2026-01-30

---

## Summary

Implements CSV/Excel import and JSON/CSV export for style terminology, enabling team sharing and backup of custom rules.

---

## Abstraction Layer

### New Contracts

| File                                                                                                                                                  | Description                                 |
| ----------------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------- |
| [ITermImportService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/ITermImportService.cs)                     | Interface for CSV/Excel parsing and commit  |
| [ITermExportService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/ITermExportService.cs)                     | Interface for JSON/CSV export               |
| [TerminologyImportExportTypes.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Abstractions/Contracts/TerminologyImportExportTypes.cs) | Data contracts for import/export operations |

### New Types

| Type                        | Description                                              |
| --------------------------- | -------------------------------------------------------- |
| `ImportOptions`             | Parsing options (header row, sheet name, column mapping) |
| `ImportColumnMapping`       | Maps CSV columns to term properties                      |
| `ImportRow`                 | Parsed row with status, validation, and duplicate info   |
| `ImportRowStatus`           | Enum: Valid, Invalid, Duplicate                          |
| `ImportParseResult`         | Result of parsing with rows and statistics               |
| `DuplicateHandling`         | Enum: Skip, Overwrite                                    |
| `ImportResult`              | Commit result with imported/skipped/updated counts       |
| `ImportException`           | Thrown for file size/row limit violations                |
| `ExportOptions`             | Export settings (metadata, categories, inactive terms)   |
| `ExportResult`              | Export result with content and exported count            |
| `ExportedTerm`              | Serializable term representation                         |
| `TerminologyExportDocument` | JSON export wrapper with metadata                        |
| `ExportMetadata`            | Version, timestamp, count for traceability               |

---

## Implementation Layer

### New Components

| File                                                                                                                            | LOC  | Description                                   |
| ------------------------------------------------------------------------------------------------------------------------------- | ---- | --------------------------------------------- |
| [TermImportService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/TermImportService.cs) | ~435 | CSV (CsvHelper) and Excel (ClosedXML) parsing |
| [TermExportService.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Services/TermExportService.cs) | ~165 | JSON and CSV export with filtering            |

### Modified Components

| File                                                                                                                                       | Changes                                                  |
| ------------------------------------------------------------------------------------------------------------------------------------------ | -------------------------------------------------------- |
| [StyleModule.cs](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/StyleModule.cs)                                 | Registered `ITermImportService` and `ITermExportService` |
| [Lexichord.Modules.Style.csproj](file:///Users/ryan/Documents/GitHub/lexichord/src/Lexichord.Modules.Style/Lexichord.Modules.Style.csproj) | Added CsvHelper and ClosedXML packages                   |

### NuGet Packages Added

| Package   | Version | Purpose              |
| --------- | ------- | -------------------- |
| CsvHelper | 33.0.1  | CSV parsing          |
| ClosedXML | 0.102.3 | Excel parsing (safe) |

---

## Unit Tests

### New Test Files

| File                                                                                                                                          | Tests | Description                          |
| --------------------------------------------------------------------------------------------------------------------------------------------- | ----- | ------------------------------------ |
| [TermImportServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/TermImportServiceTests.cs) | 16    | CSV parsing, validation, commit      |
| [TermExportServiceTests.cs](file:///Users/ryan/Documents/GitHub/lexichord/tests/Lexichord.Tests.Unit/Modules/Style/TermExportServiceTests.cs) | 13    | JSON/CSV export, filtering, metadata |

---

## Security

- **File Size Limit:** 10MB max prevents memory exhaustion
- **Row Limit:** 10,000 rows max prevents excessive processing
- **ClosedXML:** Safe Excel parsing without macro execution
- **Pattern Validation:** Uses existing `TermPatternValidator` with ReDoS protection

---

## Verification

- **Build:** ✅ Succeeded
- **Import/Export Tests:** 29/29 passed
- **Style Module Tests:** 596/597 passed (1 pre-existing flaky test)
