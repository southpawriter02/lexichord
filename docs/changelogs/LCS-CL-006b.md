# LCS-CL-006b: WindowsSecureVault (DPAPI)

**Version:** v0.0.6b  
**Category:** Infrastructure  
**Feature Name:** WindowsSecureVault (DPAPI)  
**Date:** 2026-01-28

---

## Summary

Implements the Windows-specific secure vault using Windows Data Protection API (DPAPI). This enables secure, user-scoped encryption of sensitive credentials on Windows systems.

---

## New Features

### WindowsSecureVault Implementation

- **WindowsSecureVault** — Windows-specific `ISecureVault` implementation using DPAPI:
    - `ProtectedData.Protect/Unprotect` with `DataProtectionScope.CurrentUser`
    - Encryption tied to Windows user account credentials
    - Hardware-backed protection via TPM when available
    - OS-managed key derivation (no key management burden)

### Per-Installation Entropy

- **Entropy File** — `.entropy` file in vault directory:
    - 32 bytes of cryptographically random data
    - Generated on first use, reused thereafter
    - Prevents cross-application decryption attacks
    - Hidden file attribute set automatically

### File-Based Storage

- **Storage Location** — `%APPDATA%/Lexichord/vault/`:
    - `.secret` files — Encrypted secret data with version header
    - `.meta` files — JSON metadata with timestamps
    - Filenames derived from SHA256 hash of key name

### SecureVaultFactory

- **SecureVaultFactory** — Platform detection factory:
    - Returns `WindowsSecureVault` on Windows
    - `PlatformNotSupportedException` on other platforms (until v0.0.6c)
    - Properties for `VaultImplementationName` and `VaultStoragePath`

### Security Features

- **Memory Safety** — `CryptographicOperations.ZeroMemory` clears plaintext
- **Secure Deletion** — Files overwritten with zeros before deletion
- **Thread Safety** — `SemaphoreSlim` prevents concurrent write corruption

---

## Files Added

| File                                                                  | Description                      |
| :-------------------------------------------------------------------- | :------------------------------- |
| `src/Lexichord.Host/Services/Security/WindowsSecureVault.cs`          | DPAPI-based vault implementation |
| `src/Lexichord.Host/Services/Security/SecureVaultFactory.cs`          | Platform detection factory       |
| `tests/Lexichord.Tests.Unit/Host/Security/WindowsSecureVaultTests.cs` | Comprehensive vault tests        |
| `tests/Lexichord.Tests.Unit/Host/Security/SecureVaultFactoryTests.cs` | Factory tests                    |

## Files Modified

| File                                                     | Description                           |
| :------------------------------------------------------- | :------------------------------------ |
| `src/Lexichord.Host/Lexichord.Host.csproj`               | Added ProtectedData package reference |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj` | Added SkippableFact package reference |

---

## Design Decisions

### DPAPI Choice Rationale

Windows DPAPI was chosen because:

1. **OS-Native Security** — Keys derived from Windows credentials
2. **TPM Integration** — Hardware-backed when available
3. **Zero Key Management** — No master key to store or protect
4. **Roaming Profile Support** — Works with domain-joined machines
5. **Password Change Resilience** — DPAPI handles credential rotation

### Additional Entropy

Per-installation entropy provides defense-in-depth:

```csharp
// Even if another app uses DPAPI with same user,
// it cannot decrypt Lexichord secrets without the entropy file
var encryptedBytes = ProtectedData.Protect(
    userData: plainBytes,
    optionalEntropy: _entropy,  // Installation-specific
    scope: DataProtectionScope.CurrentUser
);
```

### File Format

Secret files use a versioned binary format:

```
[4 bytes: version (int32)]
[4 bytes: length (int32)]
[N bytes: encrypted data]
```

This enables future format evolution without breaking existing vaults.

---

## Usage

### DI Registration Pattern

```csharp
// In Host startup
services.AddSingleton<ISecureVaultFactory, SecureVaultFactory>();
services.AddSingleton(sp => sp.GetRequiredService<ISecureVaultFactory>().CreateVault());
```

### Store and Retrieve

```csharp
public class LlmService(ISecureVault vault)
{
    public async Task ConfigureApiKeyAsync(string apiKey)
    {
        await vault.StoreSecretAsync("llm:openai:api-key", apiKey);
    }

    public async Task<string> GetApiKeyAsync()
    {
        return await vault.GetSecretAsync("llm:openai:api-key");
    }
}
```

---

## Verification Commands

```powershell
# 1. Verify implementation exists
Get-ChildItem src\Lexichord.Host\Services\Security\*.cs

# 2. Build for Windows
dotnet build --configuration Release

# 3. Run unit tests
dotnet test tests\Lexichord.Tests.Unit --filter "FullyQualifiedName~WindowsSecureVault"

# 4. Manual verification:
# a) Check vault directory exists after first use:
$vaultPath = "$env:APPDATA\Lexichord\vault"
Get-ChildItem $vaultPath

# b) Verify .entropy file exists and is hidden
Get-Item "$vaultPath\.entropy" -Force

# c) Verify .secret files are encrypted (not plaintext):
$secretFile = Get-ChildItem "$vaultPath\*.secret" | Select-Object -First 1
[System.Text.Encoding]::UTF8.GetString([System.IO.File]::ReadAllBytes($secretFile.FullName))
# Expected: Binary garbage, not readable
```

---

## Test Summary

| Test Class              | Tests  | Status |
| :---------------------- | :----- | :----- |
| WindowsSecureVaultTests | 21     | ✅     |
| SecureVaultFactoryTests | 5      | ✅     |
| **Total**               | **26** | **✅** |

---

## Dependencies

- **From v0.0.6a:** `ISecureVault`, `ISecureVaultFactory`, `SecretMetadata`, exception types
- **From v0.0.3a:** DI Container for factory registration
- **From v0.0.3b:** `ILogger<T>` for structured logging
- **NuGet:** `System.Security.Cryptography.ProtectedData` v9.0.0

## Enables

- **v0.0.6c:** UnixSecureVault (libsecret/AES-256 implementation)
- **v0.0.6d:** Integration testing with real vault
- **v0.1.x+:** Secure settings storage
- **v0.3.x+:** LLM API key storage
