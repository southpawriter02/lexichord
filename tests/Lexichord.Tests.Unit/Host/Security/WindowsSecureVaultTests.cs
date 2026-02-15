using System;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Host.Services.Security;

namespace Lexichord.Tests.Unit.Host.Security;

/// <summary>
/// Unit tests for <see cref="WindowsSecureVault"/>.
/// </summary>
[Trait("Category", "Unit")]
[SupportedOSPlatform("windows")]
public class WindowsSecureVaultTests : IDisposable
{
    private readonly string _testVaultPath;
    private readonly WindowsSecureVault _vault;

    public WindowsSecureVaultTests()
    {
        _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-test-{Guid.NewGuid()}");
        _vault = new WindowsSecureVault(_testVaultPath);
    }

    public void Dispose()
    {
        _vault.Dispose();
        if (Directory.Exists(_testVaultPath))
        {
            Directory.Delete(_testVaultPath, recursive: true);
        }
    }

    #region Store and Retrieve Tests

    [SkippableFact]
    public async Task StoreAndRetrieve_RoundTripsCorrectly()
    {
        SkipIfNotWindows();

        // Arrange
        const string key = "test:api-key";
        const string value = "sk-1234567890abcdef";

        // Act
        await _vault.StoreSecretAsync(key, value);
        var retrieved = await _vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [SkippableFact]
    public async Task StoreSecret_WithUnicodeValue_RoundTripsCorrectly()
    {
        SkipIfNotWindows();

        // Arrange
        const string key = "test:unicode";
        const string value = "Secret with unicode: 日本語 🔐";

        // Act
        await _vault.StoreSecretAsync(key, value);
        var retrieved = await _vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().Be(value);
    }

    [SkippableFact]
    public async Task StoreSecret_WithEmptyValue_RoundTripsCorrectly()
    {
        SkipIfNotWindows();

        // Arrange
        const string key = "test:empty";
        const string value = "";

        // Act
        await _vault.StoreSecretAsync(key, value);
        var retrieved = await _vault.GetSecretAsync(key);

        // Assert
        retrieved.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task StoreSecret_CreatesEncryptedFile()
    {
        SkipIfNotWindows();

        // Arrange
        const string key = "test:key";
        const string value = "secret-value";

        // Act
        await _vault.StoreSecretAsync(key, value);

        // Assert
        var hash = KeyHasher.ComputeFileName(key);
        var secretPath = Path.Combine(_testVaultPath, $"{hash}.secret");
        File.Exists(secretPath).Should().BeTrue();

        // Verify file is not plaintext
        var fileContent = await File.ReadAllBytesAsync(secretPath);
        var plainBytes = Encoding.UTF8.GetBytes(value);
        fileContent.Should().NotContain(plainBytes);
    }

    [SkippableFact]
    public async Task StoreSecret_OverwritesExisting_PreservesCreatedAt()
    {
        SkipIfNotWindows();

        // Arrange
        await _vault.StoreSecretAsync("test:overwrite", "original");
        var originalMeta = await _vault.GetSecretMetadataAsync("test:overwrite");
        await Task.Delay(100); // Ensure time difference

        // Act
        await _vault.StoreSecretAsync("test:overwrite", "updated");
        var updatedMeta = await _vault.GetSecretMetadataAsync("test:overwrite");

        // Assert
        updatedMeta!.CreatedAt.Should().Be(originalMeta!.CreatedAt);
        updatedMeta.LastModifiedAt.Should().BeOnOrAfter(originalMeta.CreatedAt);
    }

    [SkippableFact]
    public async Task StoreSecret_OverwritesExisting_ReturnsNewValue()
    {
        SkipIfNotWindows();

        // Arrange
        await _vault.StoreSecretAsync("test:overwrite-value", "original");

        // Act
        await _vault.StoreSecretAsync("test:overwrite-value", "updated");
        var retrieved = await _vault.GetSecretAsync("test:overwrite-value");

        // Assert
        retrieved.Should().Be("updated");
    }

    #endregion

    #region GetSecret Tests

    [SkippableFact]
    public async Task GetSecret_WhenNotExists_ThrowsSecretNotFoundException()
    {
        SkipIfNotWindows();

        // Act
        Func<Task> act = () => _vault.GetSecretAsync("nonexistent:key");

        // Assert
        await act.Should().ThrowAsync<SecretNotFoundException>()
            .Where(ex => ex.KeyName == "nonexistent:key");
    }

    [SkippableFact]
    public async Task GetSecret_WithNullKey_ThrowsArgumentNullException()
    {
        SkipIfNotWindows();

        // Act
        Func<Task> act = () => _vault.GetSecretAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteSecret Tests

    [SkippableFact]
    public async Task DeleteSecret_WhenExists_RemovesFiles()
    {
        SkipIfNotWindows();

        // Arrange
        const string key = "test:to-delete";
        await _vault.StoreSecretAsync(key, "value");
        var hash = KeyHasher.ComputeFileName(key);
        var secretPath = Path.Combine(_testVaultPath, $"{hash}.secret");
        var metaPath = Path.Combine(_testVaultPath, $"{hash}.meta");

        // Act
        var result = await _vault.DeleteSecretAsync(key);

        // Assert
        result.Should().BeTrue();
        File.Exists(secretPath).Should().BeFalse();
        File.Exists(metaPath).Should().BeFalse();
    }

    [SkippableFact]
    public async Task DeleteSecret_WhenNotExists_ReturnsFalse()
    {
        SkipIfNotWindows();

        // Act
        var result = await _vault.DeleteSecretAsync("nonexistent:key");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region SecretExists Tests

    [SkippableFact]
    public async Task SecretExists_WhenStored_ReturnsTrue()
    {
        SkipIfNotWindows();

        // Arrange
        await _vault.StoreSecretAsync("test:exists", "value");

        // Act
        var exists = await _vault.SecretExistsAsync("test:exists");

        // Assert
        exists.Should().BeTrue();
    }

    [SkippableFact]
    public async Task SecretExists_WhenNotStored_ReturnsFalse()
    {
        SkipIfNotWindows();

        // Act
        var exists = await _vault.SecretExistsAsync("nonexistent:key");

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region Metadata Tests

    [SkippableFact]
    public async Task GetMetadata_ReturnsCorrectTimestamps()
    {
        SkipIfNotWindows();

        // Arrange
        var beforeStore = DateTimeOffset.UtcNow;
        await _vault.StoreSecretAsync("test:meta", "value");

        // Act
        var meta = await _vault.GetSecretMetadataAsync("test:meta");

        // Assert
        meta.Should().NotBeNull();
        meta!.KeyName.Should().Be("test:meta");
        meta.CreatedAt.Should().BeOnOrAfter(beforeStore);
        meta.LastModifiedAt.Should().BeOnOrAfter(beforeStore);
    }

    [SkippableFact]
    public async Task GetMetadata_WhenNotExists_ReturnsNull()
    {
        SkipIfNotWindows();

        // Act
        var meta = await _vault.GetSecretMetadataAsync("nonexistent:key");

        // Assert
        meta.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetSecret_UpdatesLastAccessedAt()
    {
        SkipIfNotWindows();

        // Arrange
        await _vault.StoreSecretAsync("test:access", "value");
        var beforeAccess = DateTimeOffset.UtcNow;

        // Act
        await _vault.GetSecretAsync("test:access");
        await Task.Delay(50); // Allow async metadata update to complete

        // Assert
        var meta = await _vault.GetSecretMetadataAsync("test:access");
        meta!.LastAccessedAt.Should().NotBeNull();
        meta.LastAccessedAt!.Value.Should().BeOnOrAfter(beforeAccess);
    }

    #endregion

    #region ListSecrets Tests

    [SkippableFact]
    public async Task ListSecrets_ReturnsAllKeys()
    {
        SkipIfNotWindows();

        // Arrange
        await _vault.StoreSecretAsync("llm:openai:key", "value1");
        await _vault.StoreSecretAsync("llm:anthropic:key", "value2");
        await _vault.StoreSecretAsync("storage:s3:key", "value3");

        // Act
        var keys = await AsyncEnumerable.ToListAsync(_vault.ListSecretsAsync());

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("llm:openai:key");
        keys.Should().Contain("llm:anthropic:key");
        keys.Should().Contain("storage:s3:key");
    }

    [SkippableFact]
    public async Task ListSecrets_WithPrefix_FiltersCorrectly()
    {
        SkipIfNotWindows();

        // Arrange
        await _vault.StoreSecretAsync("llm:openai:key", "value1");
        await _vault.StoreSecretAsync("llm:anthropic:key", "value2");
        await _vault.StoreSecretAsync("storage:s3:key", "value3");

        // Act
        var llmKeys = await AsyncEnumerable.ToListAsync(_vault.ListSecretsAsync("llm:"));

        // Assert
        llmKeys.Should().HaveCount(2);
        llmKeys.Should().Contain("llm:openai:key");
        llmKeys.Should().Contain("llm:anthropic:key");
        llmKeys.Should().NotContain("storage:s3:key");
    }

    [SkippableFact]
    public async Task ListSecrets_EmptyVault_ReturnsEmpty()
    {
        SkipIfNotWindows();

        // Act
        var keys = await AsyncEnumerable.ToListAsync(_vault.ListSecretsAsync());

        // Assert
        keys.Should().BeEmpty();
    }

    #endregion

    #region Constructor Tests

    [SkippableFact]
    public void Constructor_CreatesVaultDirectory()
    {
        SkipIfNotWindows();

        // Assert (constructor was called in test setup)
        Directory.Exists(_testVaultPath).Should().BeTrue();
    }

    [SkippableFact]
    public void Constructor_GeneratesEntropyFile()
    {
        SkipIfNotWindows();

        // Assert
        var entropyPath = Path.Combine(_testVaultPath, ".entropy");
        File.Exists(entropyPath).Should().BeTrue();

        // Verify entropy is 32 bytes
        var entropy = File.ReadAllBytes(entropyPath);
        entropy.Should().HaveCount(32);
    }

    [SkippableFact]
    public void Constructor_EntropyFileIsHidden()
    {
        SkipIfNotWindows();

        // Assert
        var entropyPath = Path.Combine(_testVaultPath, ".entropy");
        var attributes = File.GetAttributes(entropyPath);
        attributes.Should().HaveFlag(FileAttributes.Hidden);
    }

    [SkippableFact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        SkipIfNotWindows();

        // Act
        Action act = () => new WindowsSecureVault(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Disposal Tests

    [SkippableFact]
    public async Task AfterDispose_OperationsThrowObjectDisposedException()
    {
        SkipIfNotWindows();

        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"vault-dispose-{Guid.NewGuid()}");
        var vault = new WindowsSecureVault(tempPath);

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

    private static void SkipIfNotWindows()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "WindowsSecureVault tests only run on Windows");
    }

    #endregion
}
