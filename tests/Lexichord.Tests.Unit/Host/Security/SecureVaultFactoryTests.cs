using System;
using System.IO;
using System.Runtime.Versioning;
using Lexichord.Host.Services.Security;

namespace Lexichord.Tests.Unit.Host.Security;

/// <summary>
/// Unit tests for <see cref="SecureVaultFactory"/>.
/// </summary>
[Trait("Category", "Unit")]
public class SecureVaultFactoryTests
{
    #region CreateVault Tests

    [SkippableFact]
    [SupportedOSPlatform("windows")]
    public void CreateVault_OnWindows_ReturnsWindowsSecureVault()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Test only runs on Windows");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var vault = factory.CreateVault();

        // Assert
        vault.Should().NotBeNull();
        vault.Should().BeOfType<WindowsSecureVault>();

        // Cleanup
        (vault as IDisposable)?.Dispose();
    }

    [SkippableFact]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    public void CreateVault_OnUnix_ReturnsUnixSecureVault()
    {
        Skip.IfNot(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "Test only runs on Linux/macOS");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var vault = factory.CreateVault();

        // Assert
        vault.Should().NotBeNull();
        vault.Should().BeOfType<UnixSecureVault>();

        // Cleanup
        (vault as IDisposable)?.Dispose();

        // Clean up vault directory created by test
        var testPath = factory.VaultStoragePath;
        if (Directory.Exists(testPath))
        {
            Directory.Delete(testPath, recursive: true);
        }
    }

    #endregion

    #region VaultImplementationName Tests

    [SkippableFact]
    public void VaultImplementationName_OnWindows_ReturnsWindowsDPAPI()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Test only runs on Windows");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var name = factory.VaultImplementationName;

        // Assert
        name.Should().Be("WindowsSecureVault (DPAPI)");
    }

    [SkippableFact]
    public void VaultImplementationName_OnLinux_ReturnsUnixLibsecretAes()
    {
        Skip.IfNot(OperatingSystem.IsLinux(), "Test only runs on Linux");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var name = factory.VaultImplementationName;

        // Assert
        name.Should().Be("UnixSecureVault (libsecret/AES-256)");
    }

    [SkippableFact]
    public void VaultImplementationName_OnMacOS_ReturnsUnixAes()
    {
        Skip.IfNot(OperatingSystem.IsMacOS(), "Test only runs on macOS");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var name = factory.VaultImplementationName;

        // Assert
        name.Should().Be("UnixSecureVault (AES-256)");
    }

    #endregion

    #region VaultStoragePath Tests

    [SkippableFact]
    public void VaultStoragePath_OnWindows_ReturnsAppDataPath()
    {
        Skip.IfNot(OperatingSystem.IsWindows(), "Test only runs on Windows");

        // Arrange
        var factory = new SecureVaultFactory();
        var expectedBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

        // Act
        var path = factory.VaultStoragePath;

        // Assert
        path.Should().StartWith(expectedBasePath);
        path.Should().Contain("Lexichord");
        path.Should().EndWith("vault");
    }

    [Fact]
    public void VaultStoragePath_ContainsLexichordVault()
    {
        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var path = factory.VaultStoragePath;

        // Assert
        path.Should().Contain("Lexichord");
        path.Should().EndWith("vault");
    }

    #endregion
}
