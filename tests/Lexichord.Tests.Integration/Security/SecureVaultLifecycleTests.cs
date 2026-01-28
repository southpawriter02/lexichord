using System;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying complete secret lifecycle (CRUD).
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultLifecycleTests : IDisposable
{
    private readonly VaultTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SecureVaultLifecycleTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new VaultTestFixture();
        _output.WriteLine($"Test vault path: {_fixture.VaultPath}");
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
