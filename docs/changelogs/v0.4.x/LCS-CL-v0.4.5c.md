# Changelog: v0.4.5c - Query Preprocessing

**Release Date:** 2026-02-01
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.5c](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5c.md)

---

## Summary

Implements the full `QueryPreprocessor` class, replacing the temporary `PassthroughQueryPreprocessor` stub from v0.4.5b. The preprocessor normalizes raw search queries through a four-stage pipeline (whitespace trimming, whitespace collapsing, Unicode NFC normalization, optional abbreviation expansion) and provides SHA256-based query embedding caching with a 5-minute sliding expiration via `IMemoryCache`.

---

## Changes

### Added

#### Lexichord.Modules.RAG/Search/

| File                    | Type   | Description                                                                        |
| :---------------------- | :----- | :--------------------------------------------------------------------------------- |
| `QueryPreprocessor.cs`  | Class  | Full `IQueryPreprocessor` implementation with normalization, expansion, and caching |

#### Lexichord.Tests.Unit/Modules/RAG/Search/

| File                         | Tests | Coverage                                                                  |
| :--------------------------- | :---- | :------------------------------------------------------------------------ |
| `QueryPreprocessorTests.cs`  | 28    | Constructor, null/empty, whitespace, Unicode NFC, abbreviations, caching  |

### Modified

#### Lexichord.Modules.RAG/

| File                              | Change                                                                     |
| :-------------------------------- | :------------------------------------------------------------------------- |
| `Lexichord.Modules.RAG.csproj`    | Added `Microsoft.Extensions.Caching.Memory` 9.0.0 NuGet package           |
| `RAGModule.cs`                    | Added `AddMemoryCache()`; swapped `PassthroughQueryPreprocessor` → `QueryPreprocessor` |

#### Lexichord.Modules.RAG/Search/

| File                               | Change                                                           |
| :--------------------------------- | :--------------------------------------------------------------- |
| `IQueryPreprocessor.cs`            | Added `ClearCache()` method to interface (v0.4.5c)               |
| `PassthroughQueryPreprocessor.cs`  | Added no-op `ClearCache()` to satisfy updated interface           |

---

## Technical Details

### Processing Pipeline

| Step | Operation              | Description                                               |
| :--- | :--------------------- | :-------------------------------------------------------- |
| 1    | Null/whitespace guard  | Returns `string.Empty` for null, empty, or whitespace     |
| 2    | Trim                   | Removes leading and trailing whitespace                   |
| 3    | Collapse whitespace    | `Regex.Replace(text, @"\s+", " ")` — tabs, newlines → space |
| 4    | Unicode NFC            | `string.Normalize(NormalizationForm.FormC)` — canonical form |
| 5    | Abbreviation expansion | Optional: 35+ technical abbreviations via word boundary regex |

### Abbreviation Dictionary (35 entries)

| Category                 | Abbreviations                                          |
| :----------------------- | :----------------------------------------------------- |
| Programming & Dev        | API, SDK, IDE, CLI, GUI, UI, UX                        |
| Data & Databases         | DB, SQL, NoSQL, JSON, XML, CSV, ORM, CRUD              |
| Web & Networking         | HTML, CSS, HTTP, HTTPS, REST, URL, DNS                  |
| Architecture & Patterns  | DI, IoC, MVC, MVVM, SOLID, DRY                         |
| Process & Methodology    | TDD, BDD, CI, CD, MVP, POC, QA                         |
| AI & ML                  | AI, ML, NLP, LLM, RAG                                  |

### Abbreviation Expansion Format

| Input                    | Output (with expansion)                                               |
| :----------------------- | :-------------------------------------------------------------------- |
| "How does the API work?" | "How does the API (Application Programming Interface) work?"          |
| "UI and UX design"       | "UI (User Interface) and UX (User Experience) design"                 |
| "CI/CD pipeline"         | "CI (Continuous Integration)/CD (Continuous Deployment) pipeline"     |

### Embedding Cache Strategy

| Property           | Value                                    |
| :----------------- | :--------------------------------------- |
| Key format         | `query_embedding:{sha256_hex16}`         |
| Hash algorithm     | SHA256, first 16 hex characters          |
| Case sensitivity   | Case-insensitive (query lowered before hash) |
| Expiration         | 5-minute sliding window                  |
| Eviction           | Automatic via IMemoryCache TTL           |

### DI Registrations (RAGModule.cs)

| Service              | Lifetime  | Implementation     | Change      |
| :------------------- | :-------- | :----------------- | :---------- |
| `IMemoryCache`       | Singleton | MemoryCache        | New (v0.4.5c) |
| `IQueryPreprocessor` | Singleton | `QueryPreprocessor` | Changed from `PassthroughQueryPreprocessor` |

---

## Verification

```bash
# Build solution
dotnet build
# Result: Build succeeded, 0 warnings, 0 errors

# Run v0.4.5c tests
dotnet test tests/Lexichord.Tests.Unit --filter "Feature=v0.4.5c"
# Result: 28 tests passed

# Run full regression
dotnet test tests/Lexichord.Tests.Unit
# Result: 4044 passed, 0 failed, 33 skipped (4077 total)
```

---

## Test Coverage

| Category                              | Tests |
| :------------------------------------ | ----: |
| Constructor null validation           |     2 |
| Constructor valid dependencies        |     1 |
| Process null/empty/whitespace         |     4 |
| Process whitespace normalization      |     4 |
| Process Unicode NFC normalization     |     2 |
| Process abbreviation expansion        |     6 |
| Cache store and retrieve              |     5 |
| ClearCache safety                     |     2 |
| IQueryPreprocessor interface          |     1 |
| Interface addition (ClearCache)       |     1 |
| **Total**                             | **28** |

---

## Dependencies

- v0.4.5a: `SearchOptions` record (ExpandAbbreviations, UseCache properties)
- v0.4.5b: `IQueryPreprocessor` interface (Process, GetCachedEmbedding, CacheEmbedding)
- NuGet: `Microsoft.Extensions.Caching.Memory` 9.0.0 (IMemoryCache)
- NuGet: `Microsoft.Extensions.Logging.Abstractions` 9.0.0 (ILogger<T>)

## Dependents

- v0.4.5b: `PgVectorSearchService` (consumes IQueryPreprocessor for query normalization and caching)
- v0.4.6: Reference Panel (inherits improved search quality via normalized queries)

---

## Related Documents

- [LCS-DES-v0.4.5c](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5c.md) - Design specification
- [LCS-SBD-v0.4.5 &sect;3.3](../../specs/v0.4.x/v0.4.5/LCS-SBD-v0.4.5.md#33-v045c-query-preprocessing) - Scope breakdown
- [LCS-DES-v0.4.5-INDEX](../../specs/v0.4.x/v0.4.5/LCS-DES-v0.4.5-INDEX.md) - Version index
- [LCS-CL-v0.4.5b](./LCS-CL-v0.4.5b.md) - Previous version (Vector Search Query)
