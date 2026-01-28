using System;
using System.IO;
using FluentAssertions;
using Lexichord.Host.Services.Security;
using Xunit;
using Xunit.Abstractions;

namespace Lexichord.Tests.Integration.Security;

/// <summary>
/// Integration tests verifying factory creates correct implementation.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Component", "SecureVault")]
public class SecureVaultFactoryTests
{
    private readonly ITestOutputHelper _output;

    public SecureVaultFactoryTests(ITestOutputHelper output)
    {
        _output = output;
    }

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

            _output.WriteLine($"Platform: {Environment.OSVersion.Platform}");
            _output.WriteLine($"Implementation: {factory.VaultImplementationName}");

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

            // Cleanup
            if (vault is IDisposable disposable)
            {
                disposable.Dispose();
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
        var expectedPath = Path.Combine(Path.GetTempPath(), "custom-vault-path-test");

        // Act
        var factory = new SecureVaultFactory(expectedPath);

        // Assert
        factory.VaultStoragePath.Should().Be(expectedPath);
    }

    /// <summary>
    /// Factory with default path uses platform-standard location.
    /// </summary>
    [Fact]
    public void Constructor_Default_UsesDefaultLocation()
    {
        // Arrange & Act
        var factory = new SecureVaultFactory();

        // Assert
        factory.VaultStoragePath.Should().NotBeNullOrEmpty();
        factory.VaultStoragePath.Should().Contain("Lexichord");
        factory.VaultStoragePath.Should().Contain("vault");

        _output.WriteLine($"Default vault path: {factory.VaultStoragePath}");
    }
}
