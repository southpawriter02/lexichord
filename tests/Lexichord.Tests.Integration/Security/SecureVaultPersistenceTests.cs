using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Tests.Integration.Security.Fixtures;
using Xunit;
using Xunit.Abstractions;

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
    private readonly ITestOutputHelper _output;

    public SecureVaultPersistenceTests(ITestOutputHelper output)
    {
        _output = output;
        _fixture = new VaultTestFixture();
        _output.WriteLine($"Test vault path: {_fixture.VaultPath}");
        _output.WriteLine($"Vault implementation: {_fixture.Factory.VaultImplementationName}");
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
        var keys = new List<string>();
        await foreach (var k in recreatedVault.ListSecretsAsync())
        {
            keys.Add(k);
        }
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
