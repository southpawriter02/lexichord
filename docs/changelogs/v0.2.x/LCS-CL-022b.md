# LCS-CL-022b — Terminology Repository Layer

| Metadata Field | Value                                               |
| -------------- | --------------------------------------------------- |
| Changelog ID   | `LCS-CL-022b`                                       |
| Version        | v0.2.2b                                             |
| Title          | Terminology Repository Layer                        |
| Short Title    | Repository Layer                                    |
| Date           | 2026-01-29                                          |
| Status         | Complete                                            |
| Scope          | `Lexichord.Abstractions`, `Lexichord.Modules.Style` |
| Related        | LCS-DES-022b, LCS-SBD-022                           |

---

## Summary

Implements the data access layer for the Lexicon terminology database, providing high-performance style term lookups with IMemoryCache integration. The repository follows the established module patterns, implementing IGenericRepository<StyleTerm, Guid> with terminology-specific extensions.

---

## New Files

| File                                                                        | Purpose                                                            |
| --------------------------------------------------------------------------- | ------------------------------------------------------------------ |
| `Lexichord.Abstractions/Entities/StyleTerm.cs`                              | Entity record for style terms with Dapper.Contrib attributes       |
| `Lexichord.Abstractions/Contracts/ITerminologyRepository.cs`                | Repository interface with cached active terms and filtered queries |
| `Lexichord.Abstractions/Contracts/TerminologyCacheOptions.cs`               | Configuration for cache sliding/absolute expiration                |
| `Lexichord.Modules.Style/Data/TerminologyRepository.cs`                     | Full repository implementation with Dapper + IMemoryCache          |
| `Lexichord.Tests.Unit/Modules/Style/TerminologyRepositoryTests.cs`          | Unit tests for constructor and cache behavior                      |
| `Lexichord.Tests.Integration/Data/TerminologyRepositoryIntegrationTests.cs` | Integration tests with PostgreSQL                                  |

---

## Modified Files

| File                                                     | Change                                                     |
| -------------------------------------------------------- | ---------------------------------------------------------- |
| `Lexichord.Modules.Style/StyleModule.cs`                 | Added ITerminologyRepository and IMemoryCache registration |
| `Lexichord.Modules.Style/Lexichord.Modules.Style.csproj` | Added Dapper, Microsoft.Extensions.Caching.Memory packages |

---

## Technical Notes

### Caching Strategy

- **Active terms** cached as `HashSet<StyleTerm>` for O(1) Contains() lookups
- **Sliding expiration**: 60 minutes (configurable via TerminologyCacheOptions)
- **Absolute expiration**: 240 minutes maximum
- **Automatic invalidation** on Insert/Update/Delete operations

### Architecture Decision

The TerminologyRepository is self-contained within the Style module, implementing all IGenericRepository methods directly rather than inheriting from GenericRepository. This preserves the module constraint (only reference Abstractions) while providing full Dapper functionality.

### Performance Targets

| Operation                           | Target | Notes                       |
| ----------------------------------- | ------ | --------------------------- |
| GetAllActiveTermsAsync (cache hit)  | <1ms   | HashSet returned directly   |
| GetAllActiveTermsAsync (cache miss) | <50ms  | Single database round-trip  |
| GetByCategoryAsync                  | <30ms  | Indexed query               |
| SearchAsync                         | <100ms | Trigram similarity matching |
