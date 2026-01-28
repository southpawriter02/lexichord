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
    public void CreateVault_OnNonWindows_ThrowsPlatformNotSupportedException()
    {
        Skip.If(OperatingSystem.IsWindows(), "Test only runs on non-Windows platforms");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        Action act = () => factory.CreateVault();

        // Assert
        act.Should().Throw<PlatformNotSupportedException>()
            .WithMessage("*not yet supported*");
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
    public void VaultImplementationName_OnNonWindows_ReturnsUnsupported()
    {
        Skip.If(OperatingSystem.IsWindows(), "Test only runs on non-Windows platforms");

        // Arrange
        var factory = new SecureVaultFactory();

        // Act
        var name = factory.VaultImplementationName;

        // Assert
        name.Should().Be("Unsupported Platform");
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
