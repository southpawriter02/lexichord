# LCS-CL-006c: UnixSecureVault (libsecret/AES-256)

**Version:** v0.0.6c  
**Category:** Infrastructure  
**Feature Name:** UnixSecureVault (libsecret/AES-256)  
**Date:** 2026-01-28

---

## Summary

Implements the Unix-specific secure vault for Linux and macOS platforms. Provides a two-tier storage strategy: libsecret integration via D-Bus Secret Service for desktop Linux environments, with AES-256-GCM file-based encryption as a robust fallback for headless Linux, macOS, or when libsecret is unavailable.

---

## New Features

### UnixSecureVault Implementation

- **UnixSecureVault** — Unix-specific `ISecureVault` implementation:
    - Automatic backend selection based on platform and environment
    - Delegates to appropriate backend (libsecret or AES file)
    - Full key validation via `KeyValidator`
    - Thread-safe, disposable resource management

### AesFileBackend

- **AES-256-GCM Encryption** — File-based cryptographic backend:
    - AES-256-GCM with 256-bit key, 96-bit nonce, 128-bit authentication tag
    - PBKDF2-SHA256 key derivation with 100,000 iterations
    - Machine-bound key material from machine ID + user ID + installation salt
    - Random 12-byte nonces for each encryption operation
    - Versioned storage format for future compatibility

### LibSecretBackend (Stub)

- **D-Bus Integration** — Desktop Linux keyring integration:
    - Availability detection for D-Bus session and libsecret library
    - Stub implementation for future P/Invoke integration
    - Runtime library detection on Linux

### Backend Interface

- **ISecretStorageBackend** — Internal abstraction:
    - Defines contract for pluggable storage implementations
    - Methods: `StoreAsync`, `GetAsync`, `DeleteAsync`, `ExistsAsync`, `GetMetadataAsync`, `ListAsync`
    - Enables testing and future backend additions

### Security Features

- **Memory Safety** — `CryptographicOperations.ZeroMemory` clears all sensitive data
- **Secure Deletion** — Files overwritten with zeros before deletion
- **Thread Safety** — `SemaphoreSlim` serializes write operations
- **Machine Binding** — Keys derived from platform-specific identifiers
- **Unix Permissions** — Restrictive permissions (0600/0700) on vault files

---

## Files Added

| File                                                               | Description                              |
| :----------------------------------------------------------------- | :--------------------------------------- |
| `src/Lexichord.Host/Services/Security/ISecretStorageBackend.cs`    | Internal backend interface               |
| `src/Lexichord.Host/Services/Security/LibSecretBackend.cs`         | libsecret stub with availability checks  |
| `src/Lexichord.Host/Services/Security/AesFileBackend.cs`           | AES-256-GCM file encryption backend      |
| `src/Lexichord.Host/Services/Security/UnixSecureVault.cs`          | Unix vault facade with backend selection |
| `tests/Lexichord.Tests.Unit/Host/Security/AesFileBackendTests.cs`  | AES backend unit tests (21 tests)        |
| `tests/Lexichord.Tests.Unit/Host/Security/UnixSecureVaultTests.cs` | Unix vault unit tests (11 tests)         |

---

## Files Modified

| File                                                         | Changes                                      |
| :----------------------------------------------------------- | :------------------------------------------- |
| `src/Lexichord.Host/Services/Security/SecureVaultFactory.cs` | Added Linux/macOS support in `CreateVault()` |

---

## Design Decisions

### Backend Selection Strategy

The vault selects backends in priority order:

1. **Linux + D-Bus + libsecret available**: Would use `LibSecretBackend` (currently stub)
2. **Fallback**: Uses `AesFileBackend` with machine-derived encryption

> [!NOTE]
> Full libsecret P/Invoke is deferred. `LibSecretBackend` currently serves only as an availability checker, with `AesFileBackend` as the functional default.

### Key Derivation Material

The AES encryption key is derived from:

- **Machine ID**: `/etc/machine-id` on Linux, hostname on macOS
- **User ID**: `Environment.UserName`
- **Installation Salt**: 256-bit random salt stored in `.salt` file

### Storage Paths

- **Linux**: `~/.config/Lexichord/vault/` (XDG compliant)
- **macOS**: `~/Library/Application Support/Lexichord/vault/`

### Cryptographic Specifications

| Parameter      | Value/Algorithm     |
| :------------- | :------------------ |
| Cipher         | AES-256-GCM         |
| Key Size       | 256 bits            |
| Nonce Size     | 96 bits (12 bytes)  |
| Tag Size       | 128 bits (16 bytes) |
| KDF            | PBKDF2-SHA256       |
| KDF Iterations | 100,000             |
| Salt Size      | 256 bits (32 bytes) |

---

## Usage

```csharp
using Lexichord.Host.Services.Security;

// Factory usage (recommended)
var factory = new SecureVaultFactory("/path/to/vault", null);
Console.WriteLine($"Platform: {factory.VaultImplementationName}");
// "UnixSecureVault (libsecret/AES-256)" on Linux
// "UnixSecureVault (AES-256)" on macOS

using var vault = factory.CreateVault();
await vault.StoreSecretAsync("api:openai", "sk-1234567890");
var secret = await vault.GetSecretAsync("api:openai");
```

---

## Verification

```bash
# Build and test
dotnet build src/Lexichord.Host

# Run Unix vault tests (on Linux/macOS)
dotnet test tests/Lexichord.Tests.Unit \
  --filter "FullyQualifiedName~AesFileBackend|FullyQualifiedName~UnixSecureVault"
```

---

## Test Summary

| Test Class           | Tests | Status  |
| :------------------- | :---- | :------ |
| AesFileBackendTests  | 21    | ✅ Pass |
| UnixSecureVaultTests | 11    | ✅ Pass |
| **Total**            | 32    | ✅ Pass |

---

## Dependencies

- **Upstream**: `ISecureVault`, `ISecureVaultFactory`, `SecretMetadata`, `SecureVaultException` (v0.0.6a)
- **NuGet**: `System.Text.Json` (built-in), `System.Security.Cryptography` (built-in)

---

## Related Specifications

- **Design**: [`LCS-DES-006c.md`](../specs/v0.0.x/v0.0.6/LCS-DES-006c.md)
- **Scope Breakdown**: [`LCS-SBD-006.md`](../specs/v0.0.x/v0.0.6/LCS-SBD-006.md)
- **Index**: [`LCS-DES-006-INDEX.md`](../specs/v0.0.x/v0.0.6/LCS-DES-006-INDEX.md)
