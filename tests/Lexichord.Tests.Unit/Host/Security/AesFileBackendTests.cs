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
/// Unit tests for <see cref="AesFileBackend"/>.
/// </summary>
[Trait("Category", "Unit")]
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public class AesFileBackendTests : IDisposable
{
    private readonly string _testVaultPath;
    private readonly AesFileBackend _backend;

    public AesFileBackendTests()
    {
        _testVaultPath = Path.Combine(Path.GetTempPath(), $"vault-aes-test-{Guid.NewGuid()}");
        _backend = new AesFileBackend(_testVaultPath);
    }

    public void Dispose()
    {
        _backend.Dispose();
        if (Directory.Exists(_testVaultPath))
        {
            Directory.Delete(_testVaultPath, recursive: true);
        }
    }

    #region Store and Retrieve Tests

    [SkippableFact]
    public async Task StoreAndRetrieve_RoundTripsCorrectly()
    {
        SkipIfNotUnix();

        // Arrange
        const string key = "test:api-key";
        const string value = "sk-1234567890abcdef";

        // Act
        await _backend.StoreAsync(key, value, default);
        var retrieved = await _backend.GetAsync(key, default);

        // Assert
        retrieved.Should().Be(value);
    }

    [SkippableFact]
    public async Task StoreSecret_WithUnicodeValue_RoundTripsCorrectly()
    {
        SkipIfNotUnix();

        // Arrange
        const string key = "test:unicode";
        const string value = "Secret with unicode: 日本語 🔐";

        // Act
        await _backend.StoreAsync(key, value, default);
        var retrieved = await _backend.GetAsync(key, default);

        // Assert
        retrieved.Should().Be(value);
    }

    [SkippableFact]
    public async Task StoreSecret_WithEmptyValue_RoundTripsCorrectly()
    {
        SkipIfNotUnix();

        // Arrange
        const string key = "test:empty";
        const string value = "";

        // Act
        await _backend.StoreAsync(key, value, default);
        var retrieved = await _backend.GetAsync(key, default);

        // Assert
        retrieved.Should().BeEmpty();
    }

    [SkippableFact]
    public async Task StoreSecret_CreatesEncryptedFile()
    {
        SkipIfNotUnix();

        // Arrange
        const string key = "test:key";
        const string value = "secret-value-that-should-be-encrypted";

        // Act
        await _backend.StoreAsync(key, value, default);

        // Assert
        var hash = KeyHasher.ComputeFileName(key);
        var secretPath = Path.Combine(_testVaultPath, $"{hash}.secret");
        File.Exists(secretPath).Should().BeTrue();

        // Verify file doesn't contain plaintext value as a substring
        var fileContent = File.ReadAllText(secretPath);
        fileContent.Should().NotContain(value, "The secret value should not appear as plaintext in the encrypted file");
    }

    [SkippableFact]
    public async Task StoreSecret_OverwritesExisting_PreservesCreatedAt()
    {
        SkipIfNotUnix();

        // Arrange
        await _backend.StoreAsync("test:overwrite", "original", default);
        var originalMeta = await _backend.GetMetadataAsync("test:overwrite", default);
        await Task.Delay(100); // Ensure time difference

        // Act
        await _backend.StoreAsync("test:overwrite", "updated", default);
        var updatedMeta = await _backend.GetMetadataAsync("test:overwrite", default);

        // Assert
        updatedMeta!.CreatedAt.Should().Be(originalMeta!.CreatedAt);
        updatedMeta.LastModifiedAt.Should().BeOnOrAfter(originalMeta.CreatedAt);
    }

    [SkippableFact]
    public async Task StoreSecret_OverwritesExisting_ReturnsNewValue()
    {
        SkipIfNotUnix();

        // Arrange
        await _backend.StoreAsync("test:overwrite-value", "original", default);

        // Act
        await _backend.StoreAsync("test:overwrite-value", "updated", default);
        var retrieved = await _backend.GetAsync("test:overwrite-value", default);

        // Assert
        retrieved.Should().Be("updated");
    }

    #endregion

    #region GetSecret Tests

    [SkippableFact]
    public async Task GetSecret_WhenNotExists_ThrowsSecretNotFoundException()
    {
        SkipIfNotUnix();

        // Act
        Func<Task> act = () => _backend.GetAsync("nonexistent:key", default);

        // Assert
        await act.Should().ThrowAsync<SecretNotFoundException>()
            .Where(ex => ex.KeyName == "nonexistent:key");
    }

    #endregion

    #region DeleteSecret Tests

    [SkippableFact]
    public async Task DeleteSecret_WhenExists_RemovesFiles()
    {
        SkipIfNotUnix();

        // Arrange
        const string key = "test:to-delete";
        await _backend.StoreAsync(key, "value", default);
        var hash = KeyHasher.ComputeFileName(key);
        var secretPath = Path.Combine(_testVaultPath, $"{hash}.secret");
        var metaPath = Path.Combine(_testVaultPath, $"{hash}.meta");

        // Act
        var result = await _backend.DeleteAsync(key, default);

        // Assert
        result.Should().BeTrue();
        File.Exists(secretPath).Should().BeFalse();
        File.Exists(metaPath).Should().BeFalse();
    }

    [SkippableFact]
    public async Task DeleteSecret_WhenNotExists_ReturnsFalse()
    {
        SkipIfNotUnix();

        // Act
        var result = await _backend.DeleteAsync("nonexistent:key", default);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region ExistsSecret Tests

    [SkippableFact]
    public async Task ExistsSecret_WhenStored_ReturnsTrue()
    {
        SkipIfNotUnix();

        // Arrange
        await _backend.StoreAsync("test:exists", "value", default);

        // Act
        var exists = await _backend.ExistsAsync("test:exists", default);

        // Assert
        exists.Should().BeTrue();
    }

    [SkippableFact]
    public async Task ExistsSecret_WhenNotStored_ReturnsFalse()
    {
        SkipIfNotUnix();

        // Act
        var exists = await _backend.ExistsAsync("nonexistent:key", default);

        // Assert
        exists.Should().BeFalse();
    }

    #endregion

    #region Metadata Tests

    [SkippableFact]
    public async Task GetMetadata_ReturnsCorrectTimestamps()
    {
        SkipIfNotUnix();

        // Arrange
        var beforeStore = DateTimeOffset.UtcNow;
        await _backend.StoreAsync("test:meta", "value", default);

        // Act
        var meta = await _backend.GetMetadataAsync("test:meta", default);

        // Assert
        meta.Should().NotBeNull();
        meta!.KeyName.Should().Be("test:meta");
        meta.CreatedAt.Should().BeOnOrAfter(beforeStore);
        meta.LastModifiedAt.Should().BeOnOrAfter(beforeStore);
    }

    [SkippableFact]
    public async Task GetMetadata_WhenNotExists_ReturnsNull()
    {
        SkipIfNotUnix();

        // Act
        var meta = await _backend.GetMetadataAsync("nonexistent:key", default);

        // Assert
        meta.Should().BeNull();
    }

    [SkippableFact]
    public async Task GetSecret_UpdatesLastAccessedAt()
    {
        SkipIfNotUnix();

        // Arrange
        await _backend.StoreAsync("test:access", "value", default);
        var beforeAccess = DateTimeOffset.UtcNow;

        // Act
        await _backend.GetAsync("test:access", default);
        await Task.Delay(50); // Allow async metadata update to complete

        // Assert
        var meta = await _backend.GetMetadataAsync("test:access", default);
        meta!.LastAccessedAt.Should().NotBeNull();
        meta.LastAccessedAt!.Value.Should().BeOnOrAfter(beforeAccess);
    }

    #endregion

    #region ListSecrets Tests

    [SkippableFact]
    public async Task ListSecrets_ReturnsAllKeys()
    {
        SkipIfNotUnix();

        // Arrange
        await _backend.StoreAsync("llm:openai:key", "value1", default);
        await _backend.StoreAsync("llm:anthropic:key", "value2", default);
        await _backend.StoreAsync("storage:s3:key", "value3", default);

        // Act
        var keys = await _backend.ListAsync(null, default).ToListAsync();

        // Assert
        keys.Should().HaveCount(3);
        keys.Should().Contain("llm:openai:key");
        keys.Should().Contain("llm:anthropic:key");
        keys.Should().Contain("storage:s3:key");
    }

    [SkippableFact]
    public async Task ListSecrets_WithPrefix_FiltersCorrectly()
    {
        SkipIfNotUnix();

        // Arrange
        await _backend.StoreAsync("llm:openai:key", "value1", default);
        await _backend.StoreAsync("llm:anthropic:key", "value2", default);
        await _backend.StoreAsync("storage:s3:key", "value3", default);

        // Act
        var llmKeys = await _backend.ListAsync("llm:", default).ToListAsync();

        // Assert
        llmKeys.Should().HaveCount(2);
        llmKeys.Should().Contain("llm:openai:key");
        llmKeys.Should().Contain("llm:anthropic:key");
        llmKeys.Should().NotContain("storage:s3:key");
    }

    [SkippableFact]
    public async Task ListSecrets_EmptyVault_ReturnsEmpty()
    {
        SkipIfNotUnix();

        // Act
        var keys = await _backend.ListAsync(null, default).ToListAsync();

        // Assert
        keys.Should().BeEmpty();
    }

    #endregion

    #region Constructor Tests

    [SkippableFact]
    public void Constructor_CreatesVaultDirectory()
    {
        SkipIfNotUnix();

        // Assert (constructor was called in test setup)
        Directory.Exists(_testVaultPath).Should().BeTrue();
    }

    [SkippableFact]
    public void Constructor_GeneratesSaltFile()
    {
        SkipIfNotUnix();

        // Assert
        var saltPath = Path.Combine(_testVaultPath, ".salt");
        File.Exists(saltPath).Should().BeTrue();

        // Verify salt is 32 bytes
        var salt = File.ReadAllBytes(saltPath);
        salt.Should().HaveCount(32);
    }

    [SkippableFact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        SkipIfNotUnix();

        // Act
        Action act = () => new AesFileBackend(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Disposal Tests

    [SkippableFact]
    public async Task AfterDispose_OperationsThrowObjectDisposedException()
    {
        SkipIfNotUnix();

        // Arrange
        var tempPath = Path.Combine(Path.GetTempPath(), $"vault-dispose-{Guid.NewGuid()}");
        var backend = new AesFileBackend(tempPath);

        // Act
        backend.Dispose();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(() => backend.StoreAsync("key", "value", default));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => backend.GetAsync("key", default));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => backend.DeleteAsync("key", default));
        await Assert.ThrowsAsync<ObjectDisposedException>(() => backend.ExistsAsync("key", default));

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
            "AesFileBackend tests only run on Linux/macOS");
    }

    #endregion
}
