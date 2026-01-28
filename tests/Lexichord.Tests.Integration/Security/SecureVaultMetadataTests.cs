using System;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying metadata timestamp accuracy.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultMetadataTests : IDisposable
{
    private readonly VaultTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SecureVaultMetadataTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new VaultTestFixture();
        _output.WriteLine($"Test vault path: {_fixture.VaultPath}");
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

        // Wait for fire-and-forget LastAccessedAt update to complete
        await Task.Delay(100);

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
