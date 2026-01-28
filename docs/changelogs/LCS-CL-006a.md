# LCS-CL-006a: ISecureVault Interface

**Version:** v0.0.6a  
**Category:** Infrastructure  
**Feature Name:** ISecureVault Interface  
**Date:** 2026-01-28

---

## Summary

Defines the platform-agnostic secure storage contract for Lexichord's vault infrastructure. This sub-part establishes the interface, exception hierarchy, metadata record, and factory pattern that platform-specific implementations will use.

---

## New Features

### ISecureVault Interface

- **ISecureVault** — Platform-agnostic secure storage contract with:
    - `StoreSecretAsync` — Encrypt and store a secret by key
    - `GetSecretAsync` — Decrypt and retrieve a secret by key
    - `DeleteSecretAsync` — Remove a secret (idempotent)
    - `SecretExistsAsync` — Check existence without decryption
    - `GetSecretMetadataAsync` — Access timestamps without decryption
    - `ListSecretsAsync` — Stream all keys with optional prefix filter

### ISecureVaultFactory Interface

- **ISecureVaultFactory** — Factory for platform-specific vault creation:
    - `CreateVault()` — Returns platform-appropriate ISecureVault
    - `VaultImplementationName` — Human-readable implementation name
    - `VaultStoragePath` — Platform-specific vault storage directory

### SecretMetadata Record

- **SecretMetadata** — Non-sensitive secret information:
    - `KeyName` — Full key identifier
    - `CreatedAt` — Original creation timestamp (immutable)
    - `LastAccessedAt` — Last decryption timestamp (nullable)
    - `LastModifiedAt` — Last update timestamp
    - Computed: `Age`, `TimeSinceLastAccess`, `IsUnused`

### Exception Hierarchy

- **SecureVaultException** — Base exception for all vault operations
- **SecretNotFoundException** — Key does not exist in vault
- **SecretDecryptionException** — Decryption failed (corrupted/key changed)
- **VaultAccessDeniedException** — Permission or access control error

### Internal Helpers

- **KeyValidator** — Validates key naming rules:
    - Non-null, non-empty
    - Max 256 characters
    - Printable ASCII only (0x20-0x7E)
    - Colon allowed for namespacing

- **KeyHasher** — Computes filesystem-safe filenames:
    - SHA256 hash → 32-char hex string
    - Avoids special character and path length issues

---

## Files Added

| File                                                                            | Description                                 |
| :------------------------------------------------------------------------------ | :------------------------------------------ |
| `src/Lexichord.Abstractions/Contracts/Security/ISecureVault.cs`                 | Main vault interface with CRUD operations   |
| `src/Lexichord.Abstractions/Contracts/Security/ISecureVaultFactory.cs`          | Factory interface for vault creation        |
| `src/Lexichord.Abstractions/Contracts/Security/SecretMetadata.cs`               | Metadata record with computed properties    |
| `src/Lexichord.Abstractions/Contracts/Security/SecureVaultException.cs`         | Exception hierarchy (4 exception types)     |
| `src/Lexichord.Abstractions/Contracts/Security/KeyValidator.cs`                 | Internal key validation helper              |
| `src/Lexichord.Abstractions/Contracts/Security/KeyHasher.cs`                    | Internal key hashing helper                 |
| `tests/Lexichord.Tests.Unit/Abstractions/Security/SecretMetadataTests.cs`       | Unit tests for metadata computed properties |
| `tests/Lexichord.Tests.Unit/Abstractions/Security/SecureVaultExceptionTests.cs` | Unit tests for exception types              |
| `tests/Lexichord.Tests.Unit/Abstractions/Security/KeyValidatorTests.cs`         | Unit tests for key validation               |
| `tests/Lexichord.Tests.Unit/Abstractions/Security/KeyHasherTests.cs`            | Unit tests for key hashing                  |

## Files Modified

| File                                                       | Description                             |
| :--------------------------------------------------------- | :-------------------------------------- |
| `src/Lexichord.Abstractions/Lexichord.Abstractions.csproj` | Added InternalsVisibleTo for unit tests |

---

## Design Decisions

### Key Naming Convention

Keys use namespace-qualified strings to avoid collisions:

- `llm:openai:api-key` — LLM module, OpenAI provider
- `storage:s3:access-key` — Storage module, S3 provider
- `auth:oauth:{provider}:token` — OAuth tokens

### Metadata Without Decryption

Metadata is stored separately from encrypted secrets, enabling:

- Audit logging without security exposure
- Stale credential detection
- Settings UI display without decryption

### Streaming Results

`ListSecretsAsync` uses `IAsyncEnumerable<string>` for:

- Memory efficiency with large vaults
- Cancellation support
- Progressive result rendering

---

## Usage

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

### Check Existence

```csharp
public async Task EnsureConfiguredAsync()
{
    if (!await vault.SecretExistsAsync("llm:openai:api-key"))
    {
        // Prompt user for API key
    }
}
```

### Handle Missing Secret

```csharp
try
{
    var key = await vault.GetSecretAsync("llm:openai:api-key");
}
catch (SecretNotFoundException ex)
{
    _logger.LogWarning("API key not configured: {Key}", ex.KeyName);
    // Show configuration dialog
}
```

### Display Metadata

```csharp
var meta = await vault.GetSecretMetadataAsync("llm:openai:api-key");
if (meta != null)
{
    Console.WriteLine($"Stored: {meta.CreatedAt}");
    Console.WriteLine($"Age: {meta.Age.TotalDays:F0} days");
    Console.WriteLine($"Unused: {meta.IsUnused}");
}
```

---

## Verification Commands

```bash
# 1. Build Abstractions
dotnet build src/Lexichord.Abstractions

# 2. Verify no external dependencies added (only existing packages)
grep -E "<PackageReference" src/Lexichord.Abstractions/*.csproj

# 3. Run Security unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "Category=Unit&FullyQualifiedName~Security"

# 4. Verify files exist
ls -la src/Lexichord.Abstractions/Contracts/Security/
```

---

## Test Summary

| Test Class                | Tests  | Status |
| :------------------------ | :----- | :----- |
| SecretMetadataTests       | 8      | ✅     |
| SecureVaultExceptionTests | 10     | ✅     |
| KeyValidatorTests         | 11     | ✅     |
| KeyHasherTests            | 10     | ✅     |
| **Total**                 | **39** | **✅** |

---

## Dependencies

- **From v0.0.3a:** DI Container (for factory registration pattern)
- **From v0.0.3b:** ILogger<T> (for implementation logging guidance)

## Enables

- **v0.0.6b:** WindowsSecureVault (DPAPI implementation)
- **v0.0.6c:** UnixSecureVault (libsecret/AES-256 implementation)
- **v0.0.6d:** Integration testing
- **v0.1.x+:** Secure settings storage
- **v0.3.x+:** LLM API key storage
