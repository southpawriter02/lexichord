using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Host.Services.Security;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;
using Xunit.Abstractions;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests for edge cases and unusual inputs.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultEdgeCaseTests : IDisposable
{
    private readonly VaultTestFixture _fixture;
    private readonly ITestOutputHelper _output;

    public SecureVaultEdgeCaseTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new VaultTestFixture();
        _output.WriteLine($"Test vault path: {_fixture.VaultPath}");
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
            var factory1 = new SecureVaultFactory(vault1Path);
            var factory2 = new SecureVaultFactory(vault2Path);

            var vault1 = factory1.CreateVault();
            var vault2 = factory2.CreateVault();

            // Act - Store in vault1 only
            await vault1.StoreSecretAsync("test:key", "vault1-value");

            // Assert - Not visible in vault2
            var exists1 = await vault1.SecretExistsAsync("test:key");
            var exists2 = await vault2.SecretExistsAsync("test:key");

            exists1.Should().BeTrue();
            exists2.Should().BeFalse("different vault paths should be independent");

            // Cleanup
            if (vault1 is IDisposable d1) d1.Dispose();
            if (vault2 is IDisposable d2) d2.Dispose();
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
        const string unicodeValue = "Hello 世界 🌍 éèê";

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
