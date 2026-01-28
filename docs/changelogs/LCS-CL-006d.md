# LCS-CL-006d: Secure Vault Integration Testing

**Version:** v0.0.6d  
**Category:** Infrastructure  
**Feature Name:** Secure Vault Integration Testing  
**Date:** 2026-01-28

---

## Summary

Implements comprehensive integration tests verifying the Secure Vault system across platforms. Tests cover persistence (secrets survive restart), CRUD lifecycle, metadata accuracy, platform factory selection, and edge cases.

---

## New Features

### VaultTestFixture

- **Isolated Test Infrastructure** — Each test gets a unique vault directory:
    - Prevents test interference
    - `RecreateVault()` method simulates application restart
    - Automatic cleanup via `IDisposable`

### Persistence Tests (T-001, T-002)

- **Store-Restart-Retrieve** — Canonical persistence verification:
    - Store secret, dispose vault, recreate, retrieve
    - Multiple secrets survive restart
    - Multiple restart cycles stress test

### Lifecycle Tests (T-003, T-004, T-005)

- **Full CRUD Verification**:
    - Store → Exists (true) → Get → Delete → Exists (false)
    - `SecretNotFoundException` for missing keys
    - Idempotent delete (returns false, not exception)
    - Overwrite preserves CreatedAt, updates LastModifiedAt

### Metadata Tests (T-008, T-009, T-010)

- **Timestamp Accuracy**:
    - CreatedAt within store operation bounds
    - LastAccessedAt updates on `GetSecretAsync`
    - LastModifiedAt updates on overwrite
    - `GetSecretMetadataAsync` does NOT update LastAccessedAt

### Factory Tests (T-006, T-007)

- **Platform Selection**:
    - Windows → WindowsSecureVault (DPAPI)
    - Linux/macOS → UnixSecureVault
    - VaultStoragePath returns configured path
    - Default path uses platform-standard location

### Edge Case Tests (T-011 through T-015)

- **Robustness Verification**:
    - Empty string values
    - Large values (1MB)
    - Special characters in keys
    - Concurrent read access
    - Independent vault paths
    - Unicode and JSON round-trips

---

## Files Added

| File                                                                        | Description                        |
| :-------------------------------------------------------------------------- | :--------------------------------- |
| `tests/Lexichord.Tests.Integration/Security/Fixtures/VaultTestFixture.cs`   | Test fixture with isolated vaults  |
| `tests/Lexichord.Tests.Integration/Security/SecureVaultPersistenceTests.cs` | Restart survival tests (3 tests)   |
| `tests/Lexichord.Tests.Integration/Security/SecureVaultLifecycleTests.cs`   | CRUD lifecycle tests (4 tests)     |
| `tests/Lexichord.Tests.Integration/Security/SecureVaultMetadataTests.cs`    | Timestamp accuracy tests (4 tests) |
| `tests/Lexichord.Tests.Integration/Security/SecureVaultFactoryTests.cs`     | Platform detection tests (3 tests) |
| `tests/Lexichord.Tests.Integration/Security/SecureVaultEdgeCaseTests.cs`    | Edge case tests (8 tests)          |
| `.github/workflows/vault-integration-tests.yml`                             | CI workflow for Windows/Linux      |

---

## Test Summary

| Test Class                  | Tests | Traits                                        |
| :-------------------------- | :---- | :-------------------------------------------- |
| SecureVaultPersistenceTests | 3     | `Category=Integration, Component=SecureVault` |
| SecureVaultLifecycleTests   | 4     | `Category=Integration, Component=SecureVault` |
| SecureVaultMetadataTests    | 4     | `Category=Integration, Component=SecureVault` |
| SecureVaultFactoryTests     | 3     | `Category=Integration, Component=SecureVault` |
| SecureVaultEdgeCaseTests    | 8     | `Category=Integration, Component=SecureVault` |
| **Total**                   | 22    |                                               |

---

## Verification Commands

```bash
# Run all vault integration tests
dotnet test tests/Lexichord.Tests.Integration \
    --filter "Category=Integration&Component=SecureVault" \
    --logger "console;verbosity=detailed"

# Run specific test class
dotnet test tests/Lexichord.Tests.Integration \
    --filter "FullyQualifiedName~SecureVaultPersistenceTests"
```

---

## Dependencies

- **Upstream**: `ISecureVault`, `WindowsSecureVault`, `UnixSecureVault`, `SecureVaultFactory` (v0.0.6a-c)
- **NuGet**: FluentAssertions (existing), xUnit (existing)

---

## Related Specifications

- **Design**: [`LCS-DES-006d.md`](../specs/v0.0.x/v0.0.6/LCS-DES-006d.md)
- **Scope Breakdown**: [`LCS-SBD-006.md`](../specs/v0.0.x/v0.0.6/LCS-SBD-006.md)
- **Index**: [`LCS-DES-006-INDEX.md`](../specs/v0.0.x/v0.0.6/LCS-DES-006-INDEX.md)
