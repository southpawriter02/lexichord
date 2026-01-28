using System;
using System.IO;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Host.Services.Security;
using Moq;

namespace Lexichord.Tests.Unit.Host.Security;

/// <summary>
/// Unit tests for <see cref="UnixSecureVault"/>.
/// </summary>
[Trait("Category", "Unit")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public class UnixSecureVaultTests : IDisposable
{
    private readonly string _testVaultPath;
    private readonly UnixSecureVault _vault;

    public UnixSecureVaultTests()
    {
        _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-unix-test-{Guid.NewGuid()}");
        _vault = new UnixSecureVault(_testVaultPath);
    }

    public void Dispose()
    {
        _vault.Dispose();
        if (Directory.Exists(_testVaultPath))
        {
            Directory.Delete(_testVaultPath, recursive: true);
        }
    }

    #region Backend Selection Tests

    [SkippableFact]
    public void Constructor_SelectsAesBackendOnMacOS()
    {
        SkipIfNotMacOS();

        // Assert - macOS always uses AES backend
        _vault.BackendName.Should().Be("AES-256-GCM (File)");
    }

    [SkippableFact]
    public void Constructor_SelectsAesBackendWhenLibsecretUnavailable()
    {
        SkipIfNotUnix();

        // Assert - Without libsecret, falls back to AES
        _vault.BackendName.Should().Be("AES-256-GCM (File)");
    }

    #endregion

    #region Delegation Tests

    [SkippableFact]
    public async Task StoreAndRetrieve_DelegatesToBackend()
    {
        SkipIfNotUnix();

        // Arrange
        const string key = "test:delegation";
        const string value = "delegated-value";

        // Act
        await _vault.StoreSecretAsync(key, value);
        var retrieved = await _vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [SkippableFact]
    public async Task SecretExists_DelegatesToBackend()
    {
        SkipIfNotUnix();

        // Arrange
        await _vault.StoreSecretAsync("test:exists", "value");

        // Act
        var exists = await _vault.SecretExistsAsync("test:exists");
        var notExists = await _vault.SecretExistsAsync("test:not-exists");

        // Assert
        exists.Should().BeTrue();
        notExists.Should().BeFalse();
    }

    [SkippableFact]
    public async Task DeleteSecret_DelegatesToBackend()
    {
        SkipIfNotUnix();

        // Arrange
        await _vault.StoreSecretAsync("test:delete", "value");

        // Act
        var deleted = await _vault.DeleteSecretAsync("test:delete");
        var deletedAgain = await _vault.DeleteSecretAsync("test:delete");

        // Assert
        deleted.Should().BeTrue();
        deletedAgain.Should().BeFalse();
    }

    [SkippableFact]
    public async Task GetMetadata_DelegatesToBackend()
    {
        SkipIfNotUnix();

        // Arrange
        await _vault.StoreSecretAsync("test:meta", "value");

        // Act
        var meta = await _vault.GetSecretMetadataAsync("test:meta");

        // Assert
        meta.Should().NotBeNull();
        meta!.KeyName.Should().Be("test:meta");
    }

    #endregion

    #region Key Validation Tests

    [SkippableFact]
    public async Task StoreSecret_WithNullKey_ThrowsArgumentNullException()
    {
        SkipIfNotUnix();

        // Act
        Func<Task> act = () => _vault.StoreSecretAsync(null!, "value");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task StoreSecret_WithNullValue_ThrowsArgumentNullException()
    {
        SkipIfNotUnix();

        // Act
        Func<Task> act = () => _vault.StoreSecretAsync("key", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task GetSecret_WithNullKey_ThrowsArgumentNullException()
    {
        SkipIfNotUnix();

        // Act
        Func<Task> act = () => _vault.GetSecretAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [SkippableFact]
    public async Task GetSecret_WhenNotExists_ThrowsSecretNotFoundException()
    {
        SkipIfNotUnix();

        // Act
        Func<Task> act = () => _vault.GetSecretAsync("nonexistent:key");

        // Assert
        await act.Should().ThrowAsync<SecretNotFoundException>()
            .Where(ex => ex.KeyName == "nonexistent:key");
    }

    #endregion

    #region Disposal Tests

    [SkippableFact]
    public async Task AfterDispose_OperationsThrowObjectDisposedException()
    {
        SkipIfNotUnix();

        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"vault-dispose-{Guid.NewGuid()}");
        var vault = new UnixSecureVault(tempPath);

        // Act
        vault.Dispose();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => vault.StoreSecretAsync("key", "value"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => vault.GetSecretAsync("key"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => vault.DeleteSecretAsync("key"));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => vault.SecretExistsAsync("key"));

        // Cleanup
        if (Directory.Exists(tempPath))
        {
            Directory.Delete(tempPath, recursive: true);
        }
    }

    #endregion

    #region Helpers

    private static void SkipIfNotUnix()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "UnixSecureVault tests only run on Linux/macOS");
    }

    private static void SkipIfNotMacOS()
    {
        Skip.IfNot(OperatingSystem.IsMacOS(),
            "This test only runs on macOS");
    }

    #endregion
}
