# LCS-SBD: Scope Breakdown - v0.0.6

**Target Version:** `v0.0.6`
**Codename:** The Vault (Security)
**Timeline:** Sprint 2 (Infrastructure Foundation)
**Owner:** Lead Architect
**Prerequisites:** v0.0.5d complete (Data Layer, Repository pattern).

## 1. Executive Summary

**v0.0.6** establishes the secure secrets management foundation for Lexichord. This release enables the application to store sensitive data (API keys, tokens, credentials) without leaving them in plain text. The success of this release is measured by:

1. A platform-agnostic `ISecureVault` interface exists in Abstractions.
2. Windows implementation uses DPAPI (`ProtectedData`) for user-scoped encryption.
3. Unix implementation uses `libsecret` where available, with AES-256 file-based fallback.
4. Integration test demonstrates secrets survive application restart.

If this foundation is flawed, modules requiring API keys (LLM integrations, cloud services) will be unable to securely store credentials.

---

## 2. Sub-Part Specifications

### v0.0.6a: Vault Interface (ISecureVault)

**Goal:** Define the contract for secure secret storage in `Lexichord.Abstractions`.

- **Task 1.1: Interface Definition**
    - Create `ISecureVault` interface in `Lexichord.Abstractions/Contracts/`.
    - Methods: `StoreSecretAsync`, `GetSecretAsync`, `DeleteSecretAsync`, `SecretExistsAsync`.
    - All methods use `string key` as identifier (namespace-qualified keys like `llm:openai:api-key`).
- **Task 1.2: Exception Types**
    - Create `SecureVaultException` base exception.
    - Create `SecretNotFoundException` for missing secrets.
    - Create `VaultAccessDeniedException` for permission errors.
- **Task 1.3: Secret Metadata**
    - Create `SecretMetadata` record with `CreatedAt`, `LastAccessedAt`, `KeyName` properties.
    - Add `GetSecretMetadataAsync(key)` method to interface.
- **Task 1.4: Vault Factory**
    - Define `ISecureVaultFactory` interface for platform-specific vault creation.
    - Method: `CreateVault()` returns appropriate `ISecureVault` for current platform.

**Definition of Done:**

- `ISecureVault` interface exists in Abstractions with all methods.
- Exception types are defined and documented.
- No platform-specific code in Abstractions assembly.

---

### v0.0.6b: Windows Implementation (DPAPI)

**Goal:** Implement `WindowsSecureVault` using Windows Data Protection API.

- **Task 1.1: DPAPI Wrapper**
    - Create `WindowsSecureVault` class in `Lexichord.Host/Services/Security/`.
    - Use `System.Security.Cryptography.ProtectedData` for encryption.
    - Scope: `DataProtectionScope.CurrentUser` (user-specific encryption).
- **Task 1.2: Storage Strategy**
    - Store encrypted secrets in `%APPDATA%/Lexichord/vault/` directory.
    - Each secret stored as individual `.secret` file (encrypted blob + metadata JSON).
    - File naming: SHA256 hash of key name (avoids special character issues).
- **Task 1.3: Entropy Management**
    - Generate per-installation entropy stored in `%APPDATA%/Lexichord/vault/.entropy`.
    - Entropy provides additional protection against cross-user attacks.
- **Task 1.4: Platform Detection**
    - Register `WindowsSecureVault` in DI only when `OperatingSystem.IsWindows()`.
    - Log warning if DPAPI unavailable (e.g., Windows Server Core without UI).

**Definition of Done:**

- `StoreSecretAsync` encrypts data using DPAPI with user scope.
- `GetSecretAsync` decrypts and returns original plaintext.
- Secrets persist across application restarts.
- Unit tests pass for encrypt/decrypt round-trip.

---

### v0.0.6c: Unix Implementation (libsecret/AES-256)

**Goal:** Implement `UnixSecureVault` with libsecret (preferred) or AES-256 fallback.

- **Task 1.1: libsecret Integration (Linux Desktop)**
    - Detect if D-Bus Secret Service is available (GNOME Keyring, KDE Wallet).
    - Use `Secret.Service` via P/Invoke or managed wrapper.
    - Store secrets with schema: `org.lexichord.vault` and attribute `key`.
- **Task 1.2: AES-256 Fallback (Headless/macOS)**
    - If libsecret unavailable, use file-based encryption.
    - Key derivation: PBKDF2-SHA256 from machine ID + user ID.
    - Store in `~/.config/Lexichord/vault/` (Linux) or `~/Library/Application Support/Lexichord/vault/` (macOS).
- **Task 1.3: macOS Keychain (Future)**
    - Stub out `MacOSSecureVault` for future Keychain Services integration.
    - Currently falls back to AES-256 implementation.
- **Task 1.4: Machine Key Generation**
    - Generate installation-specific key on first vault access.
    - Store key material derivation salt securely.
    - Use `/etc/machine-id` or `IOPlatformUUID` as one factor.

**Definition of Done:**

- libsecret integration works on desktop Linux with GNOME/KDE.
- AES-256 fallback works on headless Linux and macOS.
- Secrets persist across application restarts.
- Unit tests pass for both paths.

---

### v0.0.6d: Integration Test

**Goal:** Verify secrets survive application restart and are platform-consistent.

- **Task 1.1: Store-Restart-Retrieve Test**
    - Store secret `"test:api-key"` with value `"sk-12345abcdef"`.
    - Simulate application restart (dispose vault, recreate).
    - Retrieve secret and verify exact match.
- **Task 1.2: Cross-Platform Abstraction Test**
    - Mock `ISecureVault` in test harness.
    - Verify `ISecureVaultFactory` returns correct implementation per platform.
- **Task 1.3: Secret Lifecycle Test**
    - Test: Store, Exists (true), Delete, Exists (false), Get (throws).
    - Verify `SecretNotFoundException` on missing key.
- **Task 1.4: Metadata Tracking Test**
    - Store secret, retrieve metadata.
    - Verify `CreatedAt` timestamp is accurate.
    - Access secret, verify `LastAccessedAt` updates.

**Definition of Done:**

- Integration tests run on CI for Windows and Linux runners.
- Tests demonstrate complete secret lifecycle (CRUD).
- Documentation includes manual verification steps.

---

## 3. Implementation Checklist (for Developer)

| Step     | Description                                                        | Status |
| :------- | :----------------------------------------------------------------- | :----- |
| **0.6a** | `ISecureVault` interface defined in Abstractions.                  | [ ]    |
| **0.6a** | `SecureVaultException` and derived exceptions created.             | [ ]    |
| **0.6a** | `SecretMetadata` record defined.                                   | [ ]    |
| **0.6a** | `ISecureVaultFactory` interface defined.                           | [ ]    |
| **0.6b** | `WindowsSecureVault` implementation using DPAPI.                   | [ ]    |
| **0.6b** | Entropy file generated on first use.                               | [ ]    |
| **0.6b** | Secrets stored as encrypted files in `%APPDATA%/Lexichord/vault/`. | [ ]    |
| **0.6c** | `UnixSecureVault` with libsecret detection.                        | [ ]    |
| **0.6c** | AES-256 fallback implementation for headless environments.         | [ ]    |
| **0.6c** | Machine key derivation implemented securely.                       | [ ]    |
| **0.6d** | Integration test: store-restart-retrieve passes.                   | [x]    |
| **0.6d** | Integration test: full CRUD lifecycle passes.                      | [x]    |
| **0.6d** | CI pipeline runs vault tests on Windows and Linux.                 | [x]    |

## 4. Risks & Mitigations

- **Risk:** DPAPI not available on Windows Server Core without desktop experience.
    - _Mitigation:_ Detect availability and fall back to AES-256 if necessary.
- **Risk:** libsecret requires D-Bus which may not run in containers/CI.
    - _Mitigation:_ AES-256 fallback is always available; CI uses fallback mode.
- **Risk:** User migrates secrets between machines (different machine keys).
    - _Mitigation:_ Document that vault is machine-specific; provide export/import commands in future version.
- **Risk:** File permissions allow other users to read encrypted vault files.
    - _Mitigation:_ Set restrictive permissions (0600 on Unix, ACLs on Windows).
- **Risk:** Memory scraping attacks can read decrypted secrets.
    - _Mitigation:_ Use `SecureString` where possible; clear buffers after use.
