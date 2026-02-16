using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.StatusBar.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for VaultStatusService.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the vault status service correctly:
/// - Returns Ready when test key exists
/// - Returns Empty when no key exists
/// - Returns Error on vault exceptions
/// - Returns Unavailable when platform doesn't support secure storage
/// - Stores and deletes keys correctly
/// </remarks>
public class VaultStatusServiceTests
{
    private readonly ISecureVault _secureVault;
    private readonly ILogger<VaultStatusService> _logger;
    private readonly VaultStatusService _sut;

    public VaultStatusServiceTests()
    {
        _secureVault = Substitute.For<ISecureVault>();
        _logger = Substitute.For<ILogger<VaultStatusService>>();

        // Default: vault is accessible (ListSecretsAsync returns empty)
        _secureVault.ListSecretsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(System.Linq.AsyncEnumerable.Empty<string>());

        _sut = new VaultStatusService(_secureVault, _logger);
    }

    #region GetVaultStatusAsync Tests

    [Fact]
    public async Task GetVaultStatusAsync_ReturnsReady_WhenKeyExists()
    {
        // Arrange
        _secureVault.SecretExistsAsync(VaultKeys.TestApiKey, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var status = await _sut.GetVaultStatusAsync();

        // Assert
        status.Should().Be(VaultStatus.Ready);
    }

    [Fact]
    public async Task GetVaultStatusAsync_ReturnsEmpty_WhenNoKey()
    {
        // Arrange
        _secureVault.SecretExistsAsync(VaultKeys.TestApiKey, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var status = await _sut.GetVaultStatusAsync();

        // Assert
        status.Should().Be(VaultStatus.Empty);
    }

    [Fact]
    public async Task GetVaultStatusAsync_ReturnsError_OnVaultException()
    {
        // Arrange
        _secureVault.SecretExistsAsync(VaultKeys.TestApiKey, Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Vault error"));

        // Act
        var status = await _sut.GetVaultStatusAsync();

        // Assert
        status.Should().Be(VaultStatus.Error);
    }

    [Fact]
    public async Task GetVaultStatusAsync_ReturnsUnavailable_WhenPlatformNotSupported()
    {
        // Arrange
        _secureVault.ListSecretsAsync(Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(ThrowPlatformNotSupported());

        // Act
        var status = await _sut.GetVaultStatusAsync();

        // Assert
        status.Should().Be(VaultStatus.Unavailable);
    }

    #endregion

    #region StoreApiKeyAsync Tests

    [Fact]
    public async Task StoreApiKeyAsync_StoresKey_AndReturnsTrue()
    {
        // Arrange
        _secureVault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.StoreApiKeyAsync("test-key", "secret-value");

        // Assert
        result.Should().BeTrue();
        await _secureVault.Received(1).StoreSecretAsync("test-key", "secret-value", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StoreApiKeyAsync_ReturnsFalse_OnException()
    {
        // Arrange
        _secureVault.StoreSecretAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Store failed"));

        // Act
        var result = await _sut.StoreApiKeyAsync("test-key", "secret-value");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region DeleteApiKeyAsync Tests

    [Fact]
    public async Task DeleteApiKeyAsync_DeletesKey_AndReturnsTrue()
    {
        // Arrange
        _secureVault.DeleteSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.DeleteApiKeyAsync("test-key");

        // Assert
        result.Should().BeTrue();
        await _secureVault.Received(1).DeleteSecretAsync("test-key", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteApiKeyAsync_ReturnsFalse_WhenKeyNotFound()
    {
        // Arrange
        _secureVault.DeleteSecretAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.DeleteApiKeyAsync("nonexistent-key");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region CheckApiKeyPresenceAsync Tests

    [Fact]
    public async Task CheckApiKeyPresenceAsync_ReturnsTrue_WhenKeyExists()
    {
        // Arrange
        _secureVault.SecretExistsAsync(VaultKeys.TestApiKey, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        var result = await _sut.CheckApiKeyPresenceAsync();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CheckApiKeyPresenceAsync_ReturnsFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        _secureVault.SecretExistsAsync(VaultKeys.TestApiKey, Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var result = await _sut.CheckApiKeyPresenceAsync();

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region StatusChanged Event Tests

    [Fact]
    public async Task StatusChanged_IsRaised_WhenStatusChanges()
    {
        // Arrange
        VaultStatus? raisedStatus = null;
        _sut.StatusChanged += (_, status) => raisedStatus = status;

        _secureVault.SecretExistsAsync(VaultKeys.TestApiKey, Arg.Any<CancellationToken>())
            .Returns(true);

        // Act
        await _sut.GetVaultStatusAsync();

        // Assert
        raisedStatus.Should().Be(VaultStatus.Ready);
    }

    #endregion

    #region Helpers

    private static async IAsyncEnumerable<string> ThrowPlatformNotSupported()
    {
        await Task.CompletedTask;
        throw new PlatformNotSupportedException("Vault not supported");
#pragma warning disable CS0162 // Unreachable code detected
        yield break; // Required to make this an async enumerable
#pragma warning restore CS0162
    }

    #endregion
}
