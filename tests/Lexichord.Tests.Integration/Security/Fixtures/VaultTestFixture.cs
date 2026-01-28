using System;
using System.IO;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Host.Services.Security;
using Xunit.Abstractions;

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
    private ISecureVault _currentVault;

    /// <summary>
    /// Gets the path to the isolated vault directory.
    /// </summary>
    public string VaultPath => _vaultPath;

    /// <summary>
    /// Gets the platform-appropriate vault implementation.
    /// </summary>
    public ISecureVault Vault => _currentVault;

    /// <summary>
    /// Gets the vault factory for platform testing.
    /// </summary>
    public SecureVaultFactory Factory { get; }

    /// <summary>
    /// Initializes a new test fixture with an isolated vault.
    /// </summary>
    /// <param name="vaultPath">Optional custom vault path. If null, creates unique temp directory.</param>
    public VaultTestFixture(string? vaultPath = null)
    {
        // LOGIC: Create unique directory per test run
        _vaultPath = vaultPath ?? Path.Combine(
            Path.GetTempPath(),
            "lexichord-vault-test",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_vaultPath);

        // LOGIC: Create factory and vault
        Factory = new SecureVaultFactory(_vaultPath);
        _currentVault = Factory.CreateVault();
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
        if (_currentVault is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _currentVault = Factory.CreateVault();
        return _currentVault;
    }

    /// <summary>
    /// Cleans up the test vault directory.
    /// </summary>
    public void Dispose()
    {
        if (_currentVault is IDisposable disposable)
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
