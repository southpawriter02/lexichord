# LCS-01: Feature Design Composition

## 1. Metadata & Categorization

| Field                | Value                                     | Description                                          |
| :------------------- | :---------------------------------------- | :--------------------------------------------------- |
| **Feature ID**       | `INF-006d`                                | Infrastructure - Vault Integration Tests             |
| **Feature Name**     | Secure Vault Integration Testing          | End-to-end verification of vault functionality.      |
| **Target Version**   | `v0.0.6d`                                 | Infrastructure Foundation Layer.                     |
| **Module Scope**     | `Lexichord.Tests.Integration`             | Cross-process and persistence testing.               |
| **Swimlane**         | `Infrastructure`                          | The Vault (Security).                                |
| **License Tier**     | `Core`                                    | Foundation (Required for all tiers).                 |
| **Feature Gate Key** | N/A                                       | No runtime gating for tests.                         |
| **Author**           | System Architect                          |                                                      |
| **Status**           | **Draft**                                 | Pending approval.                                    |
| **Last Updated**     | 2026-01-26                                |                                                      |

---

## 2. Executive Summary

### 2.1 The Requirement

Unit tests verify individual components in isolation, but secure storage requires:

- **Persistence Testing:** Verify secrets survive application restart.
- **Cross-Platform Testing:** Ensure consistent behavior on Windows and Linux.
- **Factory Verification:** Confirm correct vault implementation is selected per platform.
- **Lifecycle Testing:** Validate complete CRUD operations in sequence.
- **Error Handling:** Test exception scenarios and edge cases.

### 2.2 The Proposed Solution

We **SHALL** implement comprehensive integration tests that:

1. **Store-Restart-Retrieve:** Store a secret, dispose the vault, recreate it, and retrieve.
2. **Platform Abstraction:** Verify `ISecureVaultFactory` returns correct implementation.
3. **Secret Lifecycle:** Test Store, Exists, Get, Delete, Exists sequence.
4. **Metadata Tracking:** Verify timestamps are accurate and update correctly.
5. **CI/CD Integration:** Tests run on GitHub Actions for both Windows and Linux.

---

## 3. Architecture & Modular Strategy

### 3.1 Test Architecture

```mermaid
graph TB
    subgraph "Test Suite"
        PERSIST[Persistence Tests<br/>Store-Restart-Retrieve]
        LIFECYCLE[Lifecycle Tests<br/>Full CRUD]
        FACTORY[Factory Tests<br/>Platform Selection]
        METADATA[Metadata Tests<br/>Timestamp Accuracy]
        EDGE[Edge Case Tests<br/>Error Handling]
    end

    subgraph "Test Infrastructure"
        FIXTURE[VaultTestFixture<br/>Isolated Vault Setup]
        CLEANUP[Test Cleanup<br/>Vault Directory Removal]
    end

    subgraph "Platform Matrix"
        WIN[Windows Runner<br/>windows-latest]
        LINUX[Ubuntu Runner<br/>ubuntu-latest]
    end

    PERSIST --> FIXTURE
    LIFECYCLE --> FIXTURE
    FACTORY --> FIXTURE
    METADATA --> FIXTURE
    EDGE --> FIXTURE
    FIXTURE --> CLEANUP
    WIN --> PERSIST
    WIN --> LIFECYCLE
    LINUX --> PERSIST
    LINUX --> LIFECYCLE
```

### 3.2 File Structure After v0.0.6d

```text
tests/Lexichord.Tests.Integration/
+-- Security/
    +-- SecureVaultIntegrationTests.cs    # NEW: Main integration tests
    +-- SecureVaultPersistenceTests.cs    # NEW: Restart survival tests
    +-- SecureVaultFactoryTests.cs        # NEW: Factory/DI tests
    +-- Fixtures/
        +-- VaultTestFixture.cs           # NEW: Test setup/teardown
```

---

## 4. Test Scenarios

### 4.1 Critical Test Matrix

| Test ID | Category      | Description                                           | Platforms       |
| :------ | :------------ | :---------------------------------------------------- | :-------------- |
| T-001   | Persistence   | Store secret, dispose vault, recreate, retrieve       | Windows, Linux  |
| T-002   | Persistence   | Store multiple secrets, restart, list all             | Windows, Linux  |
| T-003   | Lifecycle     | Store -> Exists (true) -> Get -> Delete -> Exists (false) | Windows, Linux  |
| T-004   | Lifecycle     | Get non-existent key throws SecretNotFoundException   | Windows, Linux  |
| T-005   | Lifecycle     | Delete non-existent key returns false                 | Windows, Linux  |
| T-006   | Factory       | Factory returns WindowsSecureVault on Windows         | Windows         |
| T-007   | Factory       | Factory returns UnixSecureVault on Linux              | Linux           |
| T-008   | Metadata      | CreatedAt timestamp is accurate                       | Windows, Linux  |
| T-009   | Metadata      | LastAccessedAt updates on Get                         | Windows, Linux  |
| T-010   | Metadata      | LastModifiedAt updates on overwrite                   | Windows, Linux  |
| T-011   | Edge          | Store empty string value (allowed but unusual)        | Windows, Linux  |
| T-012   | Edge          | Store very long value (1MB)                           | Windows, Linux  |
| T-013   | Edge          | Key with special characters (valid ASCII)             | Windows, Linux  |
| T-014   | Edge          | Concurrent read access to same key                    | Windows, Linux  |
| T-015   | Isolation     | Different vault paths are independent                 | Windows, Linux  |

---

## 5. Data Contracts

### 5.1 VaultTestFixture

```csharp
using System;
using System.IO;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Host.Services.Security;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Lexichord.Tests.Integration.Security.Fixtures;

/// <summary>
/// Test fixture that provides an isolated vault for each test.
/// </summary>
/// <remarks>
/// LOGIC: Each test gets a unique vault directory to prevent test interference.
/// The fixture handles cleanup after each test to avoid disk space buildup.
/// </remarks>
public sealed class VaultTestFixture : IDisposable
{
    private readonly string _vaultPath;
    private readonly ILogger<VaultTestFixture>? _logger;

    /// <summary>
    /// Gets the path to the isolated vault directory.
    /// </summary>
    public string VaultPath => _vaultPath;

    /// <summary>
    /// Gets the platform-appropriate vault implementation.
    /// </summary>
    public ISecureVault Vault { get; }

    /// <summary>
    /// Gets the vault factory for platform testing.
    /// </summary>
    public ISecureVaultFactory Factory { get; }

    /// <summary>
    /// Initializes a new test fixture with an isolated vault.
    /// </summary>
    public VaultTestFixture()
    {
        // LOGIC: Create unique directory per test run
        _vaultPath = Path.Combine(
            Path.GetTempPath(),
            "lexichord-vault-test",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_vaultPath);

        // LOGIC: Create factory and vault
        Factory = new SecureVaultFactory(_vaultPath);
        Vault = Factory.CreateVault();
    }

    /// <summary>
    /// Creates a fresh vault instance (simulates restart).
    /// </summary>
    /// <returns>New vault instance pointing to same directory.</returns>
    /// <remarks>
    /// LOGIC: Disposes current vault and creates new instance.
    /// This simulates application restart for persistence testing.
    /// </remarks>
    public ISecureVault RecreateVault()
    {
        if (Vault is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return Factory.CreateVault();
    }

    /// <summary>
    /// Cleans up the test vault directory.
    /// </summary>
    public void Dispose()
    {
        if (Vault is IDisposable disposable)
        {
            disposable.Dispose();
        }

        try
        {
            if (Directory.Exists(_vaultPath))
            {
                Directory.Delete(_vaultPath, recursive: true);
            }
        }
        catch (Exception)
        {
            // LOGIC: Best-effort cleanup; don't fail test on cleanup errors
        }
    }
}
```

### 5.2 SecureVaultFactory Implementation

```csharp
using System;
using System.IO;
using System.Runtime.InteropServices;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services.Security;

/// <summary>
/// Factory for creating platform-appropriate secure vault instances.
/// </summary>
/// <remarks>
/// LOGIC: This factory encapsulates platform detection and vault instantiation.
/// It determines the correct implementation based on operating system and
/// available services (DPAPI on Windows, libsecret/AES on Unix).
/// </remarks>
public sealed class SecureVaultFactory : ISecureVaultFactory
{
    private readonly string _vaultPath;
    private readonly ILogger? _logger;
    private string? _implementationName;

    /// <summary>
    /// Initializes a new instance of the SecureVaultFactory.
    /// </summary>
    /// <param name="vaultPath">
    /// Optional vault path override. If null, uses platform-standard location:
    /// - Windows: %APPDATA%/Lexichord/vault/
    /// - Linux: ~/.config/Lexichord/vault/
    /// - macOS: ~/Library/Application Support/Lexichord/vault/
    /// </param>
    /// <param name="logger">Optional logger.</param>
    public SecureVaultFactory(string? vaultPath = null, ILogger? logger = null)
    {
        _vaultPath = vaultPath ?? GetDefaultVaultPath();
        _logger = logger;
    }

    /// <inheritdoc/>
    public string VaultImplementationName => _implementationName ?? "Not initialized";

    /// <inheritdoc/>
    public string VaultStoragePath => _vaultPath;

    /// <inheritdoc/>
    public ISecureVault CreateVault()
    {
        if (OperatingSystem.IsWindows())
        {
            _implementationName = "WindowsSecureVault (DPAPI)";
            _logger?.LogInformation("Creating WindowsSecureVault");
            return new WindowsSecureVault(_vaultPath, _logger as ILogger<WindowsSecureVault>);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var vault = new UnixSecureVault(_vaultPath, _logger as ILogger<UnixSecureVault>);
            _implementationName = $"UnixSecureVault ({vault.BackendName})";
            _logger?.LogInformation("Creating UnixSecureVault: Backend={Backend}", vault.BackendName);
            return vault;
        }

        throw new PlatformNotSupportedException(
            $"Secure vault is not supported on platform: {RuntimeInformation.OSDescription}");
    }

    private static string GetDefaultVaultPath()
    {
        string basePath;

        if (OperatingSystem.IsWindows())
        {
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            basePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Library",
                "Application Support");
        }
        else // Linux
        {
            // LOGIC: Respect XDG_CONFIG_HOME, fall back to ~/.config
            basePath = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME")
                ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
        }

        return Path.Combine(basePath, "Lexichord", "vault");
    }
}
```

### 5.3 Persistence Integration Tests

```csharp
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying secrets survive application restart.
/// </summary>
/// <remarks>
/// LOGIC: These tests simulate the critical scenario of storing a secret,
/// closing the application, and retrieving the secret after restart.
///
/// The fixture creates a unique vault directory for each test and
/// cleans it up afterward to prevent test interference.
/// </remarks>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultPersistenceTests : IDisposable
{
    private readonly VaultTestFixture _fixture;

    public SecureVaultPersistenceTests()
    {
        _fixture = new VaultTestFixture();
    }

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// T-001: Store secret, dispose vault, recreate, retrieve.
    /// </summary>
    /// <remarks>
    /// LOGIC: This is the canonical persistence test. It proves that:
    /// 1. Secrets are written to persistent storage.
    /// 2. Encryption keys are derived consistently.
    /// 3. The vault can be recreated from storage.
    /// </remarks>
    [Fact]
    public async Task StoreSecret_DisposeAndRecreate_CanRetrieve()
    {
        // Arrange
        const string key = "integration:test:api-key";
        const string expectedValue = "sk-12345abcdef67890";

        // Act - Store secret
        await _fixture.Vault.StoreSecretAsync(key, expectedValue);

        // Act - Simulate restart by recreating vault
        var recreatedVault = _fixture.RecreateVault();

        // Act - Retrieve from recreated vault
        var retrievedValue = await recreatedVault.GetSecretAsync(key);

        // Assert
        retrievedValue.Should().Be(expectedValue,
            because: "secrets must survive vault disposal and recreation");
    }

    /// <summary>
    /// T-002: Store multiple secrets, restart, verify all present.
    /// </summary>
    [Fact]
    public async Task StoreMultipleSecrets_AfterRestart_AllRetrievable()
    {
        // Arrange
        var secrets = new Dictionary<string, string>
        {
            ["llm:openai:api-key"] = "sk-openai-123",
            ["llm:anthropic:api-key"] = "sk-ant-456",
            ["storage:s3:access-key"] = "AKIAIOSFODNN7EXAMPLE",
            ["storage:s3:secret-key"] = "wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY"
        };

        // Act - Store all secrets
        foreach (var (key, value) in secrets)
        {
            await _fixture.Vault.StoreSecretAsync(key, value);
        }

        // Act - Simulate restart
        var recreatedVault = _fixture.RecreateVault();

        // Assert - All secrets retrievable
        foreach (var (key, expectedValue) in secrets)
        {
            var retrievedValue = await recreatedVault.GetSecretAsync(key);
            retrievedValue.Should().Be(expectedValue,
                because: $"secret '{key}' must survive restart");
        }

        // Assert - List returns all keys
        var keys = await recreatedVault.ListSecretsAsync().ToListAsync();
        keys.Should().HaveCount(secrets.Count);
    }

    /// <summary>
    /// Stress test: Multiple store-restart cycles.
    /// </summary>
    [Fact]
    public async Task StoreSecret_MultipleRestartCycles_ConsistentlyRetrievable()
    {
        // Arrange
        const string key = "stress:restart-test";
        const string value = "persistent-value-12345";

        // Act - Store once
        await _fixture.Vault.StoreSecretAsync(key, value);

        // Act - Multiple restart cycles
        for (var i = 0; i < 5; i++)
        {
            var vault = _fixture.RecreateVault();
            var retrieved = await vault.GetSecretAsync(key);
            retrieved.Should().Be(value, because: $"restart cycle {i + 1} must preserve secret");
        }
    }
}
```

### 5.4 Secret Lifecycle Tests

```csharp
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying complete secret lifecycle (CRUD).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultLifecycleTests : IDisposable
{
    private readonly VaultTestFixture _fixture;

    public SecureVaultLifecycleTests()
    {
        _fixture = new VaultTestFixture();
    }

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// T-003: Full CRUD lifecycle.
    /// </summary>
    [Fact]
    public async Task SecretLifecycle_StoreExistsGetDeleteExists()
    {
        // Arrange
        const string key = "lifecycle:test-key";
        const string value = "test-secret-value";
        var vault = _fixture.Vault;

        // Act & Assert - Store
        await vault.StoreSecretAsync(key, value);

        // Act & Assert - Exists (should be true)
        var existsAfterStore = await vault.SecretExistsAsync(key);
        existsAfterStore.Should().BeTrue("secret was just stored");

        // Act & Assert - Get
        var retrieved = await vault.GetSecretAsync(key);
        retrieved.Should().Be(value);

        // Act & Assert - Delete
        var deleteResult = await vault.DeleteSecretAsync(key);
        deleteResult.Should().BeTrue("secret existed before delete");

        // Act & Assert - Exists (should be false)
        var existsAfterDelete = await vault.SecretExistsAsync(key);
        existsAfterDelete.Should().BeFalse("secret was deleted");
    }

    /// <summary>
    /// T-004: Get non-existent key throws SecretNotFoundException.
    /// </summary>
    [Fact]
    public async Task GetSecret_WhenNotExists_ThrowsSecretNotFoundException()
    {
        // Arrange
        const string nonExistentKey = "lifecycle:nonexistent:key";

        // Act
        Func<Task> act = () => _fixture.Vault.GetSecretAsync(nonExistentKey);

        // Assert
        await act.Should().ThrowAsync<SecretNotFoundException>()
            .Where(ex => ex.KeyName == nonExistentKey,
                because: "exception should identify the missing key");
    }

    /// <summary>
    /// T-005: Delete non-existent key returns false (idempotent).
    /// </summary>
    [Fact]
    public async Task DeleteSecret_WhenNotExists_ReturnsFalse()
    {
        // Arrange
        const string nonExistentKey = "lifecycle:delete:nonexistent";

        // Act
        var result = await _fixture.Vault.DeleteSecretAsync(nonExistentKey);

        // Assert
        result.Should().BeFalse(
            because: "deleting non-existent key should return false, not throw");
    }

    /// <summary>
    /// Store with overwrite preserves CreatedAt, updates LastModifiedAt.
    /// </summary>
    [Fact]
    public async Task StoreSecret_Overwrite_PreservesCreatedAtUpdatesModified()
    {
        // Arrange
        const string key = "lifecycle:overwrite-test";
        var vault = _fixture.Vault;

        // Act - Initial store
        await vault.StoreSecretAsync(key, "original-value");
        var originalMeta = await vault.GetSecretMetadataAsync(key);

        // Wait a bit to ensure timestamp difference
        await Task.Delay(50);

        // Act - Overwrite
        await vault.StoreSecretAsync(key, "updated-value");
        var updatedMeta = await vault.GetSecretMetadataAsync(key);

        // Assert
        originalMeta.Should().NotBeNull();
        updatedMeta.Should().NotBeNull();

        updatedMeta!.CreatedAt.Should().Be(originalMeta!.CreatedAt,
            because: "CreatedAt must be preserved on overwrite");

        updatedMeta.LastModifiedAt.Should().BeAfter(originalMeta.CreatedAt,
            because: "LastModifiedAt must be updated on overwrite");

        // Verify value is actually updated
        var retrievedValue = await vault.GetSecretAsync(key);
        retrievedValue.Should().Be("updated-value");
    }
}
```

### 5.5 Metadata Tests

```csharp
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying metadata timestamp accuracy.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultMetadataTests : IDisposable
{
    private readonly VaultTestFixture _fixture;

    public SecureVaultMetadataTests()
    {
        _fixture = new VaultTestFixture();
    }

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// T-008: CreatedAt timestamp is accurate.
    /// </summary>
    [Fact]
    public async Task StoreSecret_CreatedAtIsAccurate()
    {
        // Arrange
        const string key = "metadata:created-at-test";
        var beforeStore = DateTimeOffset.UtcNow;

        // Act
        await _fixture.Vault.StoreSecretAsync(key, "test-value");
        var afterStore = DateTimeOffset.UtcNow;

        var metadata = await _fixture.Vault.GetSecretMetadataAsync(key);

        // Assert
        metadata.Should().NotBeNull();
        metadata!.CreatedAt.Should().BeOnOrAfter(beforeStore);
        metadata.CreatedAt.Should().BeOnOrBefore(afterStore);
        metadata.KeyName.Should().Be(key);
    }

    /// <summary>
    /// T-009: LastAccessedAt updates on Get.
    /// </summary>
    [Fact]
    public async Task GetSecret_UpdatesLastAccessedAt()
    {
        // Arrange
        const string key = "metadata:last-accessed-test";
        await _fixture.Vault.StoreSecretAsync(key, "test-value");

        var metaBeforeAccess = await _fixture.Vault.GetSecretMetadataAsync(key);
        metaBeforeAccess!.LastAccessedAt.Should().BeNull(
            because: "secret has not been retrieved yet");

        // Wait a bit
        await Task.Delay(50);
        var beforeGet = DateTimeOffset.UtcNow;

        // Act - Access the secret
        _ = await _fixture.Vault.GetSecretAsync(key);

        var afterGet = DateTimeOffset.UtcNow;
        var metaAfterAccess = await _fixture.Vault.GetSecretMetadataAsync(key);

        // Assert
        metaAfterAccess!.LastAccessedAt.Should().NotBeNull(
            because: "secret was accessed");
        metaAfterAccess.LastAccessedAt!.Value.Should().BeOnOrAfter(beforeGet);
        metaAfterAccess.LastAccessedAt.Value.Should().BeOnOrBefore(afterGet);
    }

    /// <summary>
    /// T-010: LastModifiedAt updates on overwrite.
    /// </summary>
    [Fact]
    public async Task StoreSecret_Overwrite_UpdatesLastModifiedAt()
    {
        // Arrange
        const string key = "metadata:last-modified-test";
        await _fixture.Vault.StoreSecretAsync(key, "original");

        var originalMeta = await _fixture.Vault.GetSecretMetadataAsync(key);
        await Task.Delay(50);
        var beforeOverwrite = DateTimeOffset.UtcNow;

        // Act
        await _fixture.Vault.StoreSecretAsync(key, "updated");
        var afterOverwrite = DateTimeOffset.UtcNow;

        var updatedMeta = await _fixture.Vault.GetSecretMetadataAsync(key);

        // Assert
        updatedMeta!.LastModifiedAt.Should().BeOnOrAfter(beforeOverwrite);
        updatedMeta.LastModifiedAt.Should().BeOnOrBefore(afterOverwrite);
        updatedMeta.LastModifiedAt.Should().BeAfter(originalMeta!.LastModifiedAt);
    }

    /// <summary>
    /// GetSecretMetadataAsync does NOT update LastAccessedAt.
    /// </summary>
    [Fact]
    public async Task GetMetadata_DoesNotUpdateLastAccessedAt()
    {
        // Arrange
        const string key = "metadata:no-update-test";
        await _fixture.Vault.StoreSecretAsync(key, "test-value");

        // Act - Multiple metadata reads
        var meta1 = await _fixture.Vault.GetSecretMetadataAsync(key);
        await Task.Delay(50);
        var meta2 = await _fixture.Vault.GetSecretMetadataAsync(key);
        await Task.Delay(50);
        var meta3 = await _fixture.Vault.GetSecretMetadataAsync(key);

        // Assert - LastAccessedAt remains null (never accessed via Get)
        meta1!.LastAccessedAt.Should().BeNull();
        meta2!.LastAccessedAt.Should().BeNull();
        meta3!.LastAccessedAt.Should().BeNull();
    }
}
```

### 5.6 Factory Tests

```csharp
using System;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Host.Services.Security;
using Xunit;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying factory creates correct implementation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultFactoryTests
{
    /// <summary>
    /// T-006/T-007: Factory returns correct implementation for current platform.
    /// </summary>
    [Fact]
    public void CreateVault_ReturnsCorrectImplementationForPlatform()
    {
        // Arrange
        var vaultPath = Path.Combine(Path.GetTempPath(), $"vault-factory-test-{Guid.NewGuid()}");

        try
        {
            var factory = new SecureVaultFactory(vaultPath);

            // Act
            var vault = factory.CreateVault();

            // Assert
            vault.Should().NotBeNull();

            if (OperatingSystem.IsWindows())
            {
                vault.Should().BeOfType<WindowsSecureVault>();
                factory.VaultImplementationName.Should().Contain("Windows");
                factory.VaultImplementationName.Should().Contain("DPAPI");
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                vault.Should().BeOfType<UnixSecureVault>();
                factory.VaultImplementationName.Should().Contain("Unix");
            }
        }
        finally
        {
            if (Directory.Exists(vaultPath))
            {
                Directory.Delete(vaultPath, true);
            }
        }
    }

    /// <summary>
    /// Factory.VaultStoragePath returns configured path.
    /// </summary>
    [Fact]
    public void VaultStoragePath_ReturnsConfiguredPath()
    {
        // Arrange
        var expectedPath = "/custom/vault/path";
        var factory = new SecureVaultFactory(expectedPath);

        // Assert
        factory.VaultStoragePath.Should().Be(expectedPath);
    }

    /// <summary>
    /// Factory with null path uses platform-default location.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPath_UsesDefaultLocation()
    {
        // Arrange & Act
        var factory = new SecureVaultFactory(null);

        // Assert
        factory.VaultStoragePath.Should().NotBeNullOrEmpty();
        factory.VaultStoragePath.Should().Contain("Lexichord");
        factory.VaultStoragePath.Should().Contain("vault");
    }
}
```

### 5.7 Edge Case Tests

```csharp
using System;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests for edge cases and unusual inputs.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultEdgeCaseTests : IDisposable
{
    private readonly VaultTestFixture _fixture;

    public SecureVaultEdgeCaseTests()
    {
        _fixture = new VaultTestFixture();
    }

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// T-011: Store empty string value (unusual but allowed).
    /// </summary>
    [Fact]
    public async Task StoreSecret_EmptyValue_IsAllowed()
    {
        // Arrange
        const string key = "edge:empty-value";
        const string emptyValue = "";

        // Act
        await _fixture.Vault.StoreSecretAsync(key, emptyValue);
        var retrieved = await _fixture.Vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().BeEmpty();
    }

    /// <summary>
    /// T-012: Store very long value (1MB).
    /// </summary>
    [Fact]
    public async Task StoreSecret_LargeValue_HandledCorrectly()
    {
        // Arrange
        const string key = "edge:large-value";
        var largeValue = new string('x', 1024 * 1024); // 1MB

        // Act
        await _fixture.Vault.StoreSecretAsync(key, largeValue);
        var retrieved = await _fixture.Vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(largeValue);
        retrieved.Length.Should().Be(1024 * 1024);
    }

    /// <summary>
    /// T-013: Key with special characters (valid ASCII).
    /// </summary>
    [Theory]
    [InlineData("key:with:colons")]
    [InlineData("key-with-dashes")]
    [InlineData("key_with_underscores")]
    [InlineData("key.with.dots")]
    [InlineData("key/with/slashes")]
    [InlineData("key with spaces")]
    [InlineData("key!@#$%^&*()")]
    public async Task StoreSecret_SpecialCharactersInKey_WorksCorrectly(string key)
    {
        // Arrange
        const string value = "test-value";

        // Act
        await _fixture.Vault.StoreSecretAsync(key, value);
        var retrieved = await _fixture.Vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(value);
    }

    /// <summary>
    /// T-014: Concurrent read access to same key.
    /// </summary>
    [Fact]
    public async Task GetSecret_ConcurrentReads_AllSucceed()
    {
        // Arrange
        const string key = "edge:concurrent-read";
        const string value = "concurrent-test-value";
        await _fixture.Vault.StoreSecretAsync(key, value);

        // Act - Concurrent reads
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => _fixture.Vault.GetSecretAsync(key))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert - All reads succeed with correct value
        results.Should().OnlyContain(r => r == value);
    }

    /// <summary>
    /// T-015: Different vault paths are independent.
    /// </summary>
    [Fact]
    public async Task DifferentVaultPaths_AreIndependent()
    {
        // Arrange
        var vault1Path = Path.Combine(Path.GetTempPath(), $"vault-1-{Guid.NewGuid()}");
        var vault2Path = Path.Combine(Path.GetTempPath(), $"vault-2-{Guid.NewGuid()}");

        try
        {
            using var factory1 = new SecureVaultFactory(vault1Path);
            using var factory2 = new SecureVaultFactory(vault2Path);

            using var vault1 = (IDisposable)factory1.CreateVault();
            using var vault2 = (IDisposable)factory2.CreateVault();

            var v1 = (ISecureVault)vault1;
            var v2 = (ISecureVault)vault2;

            // Act - Store in vault1 only
            await v1.StoreSecretAsync("test:key", "vault1-value");

            // Assert - Not visible in vault2
            var exists1 = await v1.SecretExistsAsync("test:key");
            var exists2 = await v2.SecretExistsAsync("test:key");

            exists1.Should().BeTrue();
            exists2.Should().BeFalse("different vault paths should be independent");
        }
        finally
        {
            if (Directory.Exists(vault1Path)) Directory.Delete(vault1Path, true);
            if (Directory.Exists(vault2Path)) Directory.Delete(vault2Path, true);
        }
    }

    /// <summary>
    /// Unicode value round-trips correctly.
    /// </summary>
    [Fact]
    public async Task StoreSecret_UnicodeValue_RoundTripsCorrectly()
    {
        // Arrange
        const string key = "edge:unicode";
        const string unicodeValue = "Hello \u4e16\u754c \ud83c\udf0d \u00e9\u00e8\u00ea"; // "Hello 世界 🌍 éèê"

        // Act
        await _fixture.Vault.StoreSecretAsync(key, unicodeValue);
        var retrieved = await _fixture.Vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(unicodeValue);
    }

    /// <summary>
    /// JSON value round-trips correctly (important for API responses).
    /// </summary>
    [Fact]
    public async Task StoreSecret_JsonValue_RoundTripsCorrectly()
    {
        // Arrange
        const string key = "edge:json";
        const string jsonValue = @"{""apiKey"":""sk-123"",""orgId"":""org-456"",""config"":{""enabled"":true}}";

        // Act
        await _fixture.Vault.StoreSecretAsync(key, jsonValue);
        var retrieved = await _fixture.Vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(jsonValue);
    }
}
```

---

## 6. CI/CD Integration

### 6.1 GitHub Actions Workflow

```yaml
# .github/workflows/vault-integration-tests.yml
name: Vault Integration Tests

on:
  push:
    paths:
      - 'src/Lexichord.Host/Services/Security/**'
      - 'src/Lexichord.Abstractions/Contracts/Security/**'
      - 'tests/Lexichord.Tests.Integration/Security/**'
  pull_request:
    paths:
      - 'src/Lexichord.Host/Services/Security/**'
      - 'src/Lexichord.Abstractions/Contracts/Security/**'
      - 'tests/Lexichord.Tests.Integration/Security/**'

jobs:
  test-windows:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run Vault Integration Tests
        run: |
          dotnet test tests/Lexichord.Tests.Integration `
            --filter "Category=Integration&Component=SecureVault" `
            --configuration Release `
            --no-build `
            --logger "trx;LogFileName=vault-windows.trx" `
            --results-directory TestResults

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: vault-test-results-windows
          path: TestResults/*.trx

  test-linux:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build --configuration Release --no-restore

      - name: Run Vault Integration Tests
        run: |
          dotnet test tests/Lexichord.Tests.Integration \
            --filter "Category=Integration&Component=SecureVault" \
            --configuration Release \
            --no-build \
            --logger "trx;LogFileName=vault-linux.trx" \
            --results-directory TestResults

      - name: Upload Test Results
        uses: actions/upload-artifact@v4
        if: always()
        with:
          name: vault-test-results-linux
          path: TestResults/*.trx

  # Verify libsecret is NOT available in CI (expected fallback to AES)
  test-linux-no-dbus:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Verify no D-Bus session
        run: |
          if [ -n "$DBUS_SESSION_BUS_ADDRESS" ]; then
            echo "Warning: D-Bus session found in CI"
          fi
          echo "D-Bus session: ${DBUS_SESSION_BUS_ADDRESS:-not set}"

      - name: Run tests (should use AES fallback)
        run: |
          unset DBUS_SESSION_BUS_ADDRESS
          dotnet test tests/Lexichord.Tests.Integration \
            --filter "Category=Integration&Component=SecureVault" \
            --configuration Release
```

---

## 7. Use Cases & User Stories

### 7.1 User Stories

| ID    | Role      | Story                                                                        | Acceptance Criteria                                 |
| :---- | :-------- | :--------------------------------------------------------------------------- | :-------------------------------------------------- |
| US-01 | Developer | As a developer, I want integration tests that verify persistence.            | Store-restart-retrieve test passes.                 |
| US-02 | Developer | As a developer, I want tests to run on multiple platforms.                   | CI runs on Windows and Linux.                       |
| US-03 | Developer | As a developer, I want meaningful test output on failures.                   | FluentAssertions provides clear error messages.     |
| US-04 | DevOps    | As a DevOps engineer, I want vault tests in the CI pipeline.                 | GitHub Actions workflow exists and runs.            |
| US-05 | DevOps    | As a DevOps engineer, I want test artifacts for investigation.               | Test results uploaded as artifacts.                 |

---

## 8. Observability & Logging

### 8.1 Test Output

Tests use xUnit's built-in output mechanism:

```csharp
public class SecureVaultPersistenceTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly VaultTestFixture _fixture;

    public SecureVaultPersistenceTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new VaultTestFixture();
        _output.WriteLine($"Test vault path: {_fixture.VaultPath}");
        _output.WriteLine($"Vault implementation: {_fixture.Factory.VaultImplementationName}");
    }
    // ...
}
```

---

## 9. Risks & Mitigations

| Risk                                   | Impact | Mitigation                                                  |
| :------------------------------------- | :----- | :---------------------------------------------------------- |
| Tests interfere with each other        | High   | Each test uses unique vault directory via fixture.          |
| CI environment differs from local      | Medium | Test on same platforms used in CI locally.                  |
| Flaky tests due to timing              | Medium | Use adequate delays for timestamp tests.                    |
| Temp directory permissions             | Low    | Verify temp directory is writable in CI environment.        |
| Tests leave orphan files               | Low    | Fixture implements IDisposable for cleanup.                 |

---

## 10. Acceptance Criteria (QA)

| #   | Category        | Criterion                                                               |
| :-- | :-------------- | :---------------------------------------------------------------------- |
| 1   | **[Persist]**   | T-001: Store-restart-retrieve test passes on Windows and Linux.         |
| 2   | **[Persist]**   | T-002: Multiple secrets survive restart.                                |
| 3   | **[Lifecycle]** | T-003: Full CRUD lifecycle test passes.                                 |
| 4   | **[Lifecycle]** | T-004: SecretNotFoundException thrown for missing key.                  |
| 5   | **[Lifecycle]** | T-005: Delete of non-existent key returns false.                        |
| 6   | **[Factory]**   | T-006/T-007: Correct implementation selected per platform.              |
| 7   | **[Metadata]**  | T-008/T-009/T-010: Timestamps are accurate and update correctly.        |
| 8   | **[Edge]**      | T-011/T-012/T-013: Edge cases handled correctly.                        |
| 9   | **[Edge]**      | T-014: Concurrent reads succeed.                                        |
| 10  | **[Edge]**      | T-015: Different vault paths are isolated.                              |
| 11  | **[CI]**        | GitHub Actions workflow runs on push/PR to vault code.                  |
| 12  | **[CI]**        | Test results uploaded as artifacts.                                     |

---

## 11. Verification Commands

```bash
# ===========================================================================
# v0.0.6d Verification
# ===========================================================================

# 1. Run all vault integration tests
dotnet test tests/Lexichord.Tests.Integration \
    --filter "Category=Integration&Component=SecureVault" \
    --logger "console;verbosity=detailed"

# 2. Run specific test categories
dotnet test --filter "FullyQualifiedName~PersistenceTests"
dotnet test --filter "FullyQualifiedName~LifecycleTests"
dotnet test --filter "FullyQualifiedName~MetadataTests"
dotnet test --filter "FullyQualifiedName~FactoryTests"
dotnet test --filter "FullyQualifiedName~EdgeCaseTests"

# 3. Run with code coverage
dotnet test \
    --filter "Category=Integration&Component=SecureVault" \
    --collect:"XPlat Code Coverage" \
    --results-directory TestResults

# 4. Generate coverage report
reportgenerator \
    -reports:TestResults/**/coverage.cobertura.xml \
    -targetdir:TestResults/CoverageReport \
    -reporttypes:Html

# 5. Run in Docker (Linux, no D-Bus)
docker run --rm \
    -v $(pwd):/app \
    -w /app \
    mcr.microsoft.com/dotnet/sdk:9.0 \
    dotnet test tests/Lexichord.Tests.Integration \
    --filter "Category=Integration&Component=SecureVault"

# 6. Verify CI workflow syntax
gh workflow view vault-integration-tests.yml
```

---

## 12. Deliverable Checklist

| Step | Description                                                        | Status |
| :--- | :----------------------------------------------------------------- | :----- |
| 1    | `VaultTestFixture.cs` with isolated vault setup/teardown.          | [ ]    |
| 2    | `SecureVaultPersistenceTests.cs` with restart survival tests.      | [ ]    |
| 3    | `SecureVaultLifecycleTests.cs` with full CRUD tests.               | [ ]    |
| 4    | `SecureVaultMetadataTests.cs` with timestamp accuracy tests.       | [ ]    |
| 5    | `SecureVaultFactoryTests.cs` with platform detection tests.        | [ ]    |
| 6    | `SecureVaultEdgeCaseTests.cs` with edge case coverage.             | [ ]    |
| 7    | All tests pass on Windows.                                         | [ ]    |
| 8    | All tests pass on Linux.                                           | [ ]    |
| 9    | GitHub Actions workflow created (`vault-integration-tests.yml`).   | [ ]    |
| 10   | Test artifacts uploaded on failure.                                | [ ]    |
| 11   | Tests use `[Trait("Category", "Integration")]` for filtering.      | [ ]    |
| 12   | Tests use `[Trait("Component", "SecureVault")]` for filtering.     | [ ]    |
| 13   | FluentAssertions used for readable assertions.                     | [ ]    |
| 14   | Documentation includes manual verification steps.                  | [ ]    |
