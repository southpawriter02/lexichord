# Changelog: v0.4.2b - Hash-Based Change Detection

**Release Date:** 2026-01-31
**Status:** Implemented
**Specification:** [LCS-DES-v0.4.2b](../../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2b.md)

---

## Summary

Implements SHA-256 hash-based file change detection for the ingestion pipeline. Uses a tiered detection strategy (size → timestamp → hash) to minimize expensive hash computation for obvious cases while ensuring accurate change detection.

---

## Changes

### Added

#### Lexichord.Abstractions/Contracts/

| File                   | Description                                                                 |
| :--------------------- | :-------------------------------------------------------------------------- |
| `FileMetadata.cs`      | Record for file metadata (exists, size, timestamp) with `NotFound` sentinel |
| `FileMetadataWithHash` | Extended record including SHA-256 hash                                      |
| `IFileHashService.cs`  | Interface for hash operations and change detection                          |

#### Lexichord.Modules.RAG/Services/

| File                 | Description                                                |
| :------------------- | :--------------------------------------------------------- |
| `FileHashService.cs` | Implementation with streaming SHA-256 and tiered detection |

### Modified

#### Lexichord.Modules.RAG/

| File           | Description                                               |
| :------------- | :-------------------------------------------------------- |
| `RAGModule.cs` | Added DI registration for `IFileHashService` as singleton |

### Unit Tests

#### Lexichord.Tests.Unit/Abstractions/Contracts/

| File                               | Tests                                                |
| :--------------------------------- | :--------------------------------------------------- |
| `IFileHashServiceContractTests.cs` | Interface mockability and contract verification      |
| `FileMetadataRecordTests.cs`       | Record equality, with-expressions, NotFound sentinel |

#### Lexichord.Tests.Unit/Modules/RAG/

| File                      | Tests                                          |
| :------------------------ | :--------------------------------------------- |
| `FileHashServiceTests.cs` | Hash computation, tiered detection, edge cases |

---

## Technical Details

### Hash Computation

- Uses `SHA256.HashDataAsync` for streaming computation
- 80KB buffer size for efficient I/O
- `FileShare.Read` for concurrent access
- Returns lowercase hex string (64 characters)

### Tiered Detection Strategy

1. **File deleted → changed** (immediate return)
2. **Size differs → changed** (skip hash computation)
3. **Timestamp same (±1s) → unchanged** (skip hash computation)
4. **Compute hash → compare** (definitive answer)

### Performance

- Unchanged files (same timestamp): < 1ms
- 1MB file hash: < 50ms
- 10MB file hash: < 200ms

---

## Verification

```bash
# Build
dotnet build src/Lexichord.Modules.RAG

# Run tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~FileHash"
# Result: 42 tests passed
```

---

## Dependencies

| Dependency                     | Version  | Purpose                            |
| :----------------------------- | :------- | :--------------------------------- |
| `IDocumentRepository`          | v0.4.1c  | Will use for stored hash retrieval |
| `System.Security.Cryptography` | Built-in | SHA-256 implementation             |

---

## Related Documents

- [LCS-DES-v0.4.2b](../../specs/v0.4.x/v0.4.2/LCS-DES-v0.4.2b.md) - Design specification
- [LCS-SBD-v0.4.2 §3.2](../../specs/v0.4.x/v0.4.2/LCS-SBD-v0.4.2.md#32-v042b-hash-based-change-detection) - Scope breakdown
- [LCS-CL-v0.4.2a](./LCS-CL-v0.4.2a.md) - Previous sub-part (Ingestion Service Interface)
