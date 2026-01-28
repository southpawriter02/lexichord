using System;
using System.IO;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services.Security;

/// <summary>
/// Factory for creating platform-specific secure vault instances.
/// </summary>
/// <remarks>
/// LOGIC: The factory encapsulates platform detection and vault instantiation.
/// Modules inject <see cref="ISecureVault"/> directly; the Host uses
/// <see cref="ISecureVaultFactory"/> during DI container setup.
///
/// <para><b>Registration Pattern:</b></para>
/// <code>
/// // In Host startup
/// services.AddSingleton&lt;ISecureVaultFactory, SecureVaultFactory&gt;();
/// services.AddSingleton(sp =&gt; sp.GetRequiredService&lt;ISecureVaultFactory&gt;().CreateVault());
/// </code>
///
/// <para><b>Platform Selection:</b></para>
/// <list type="bullet">
///   <item>Windows: WindowsSecureVault (DPAPI)</item>
///   <item>Linux/macOS: PlatformNotSupportedException (until v0.0.6c)</item>
/// </list>
/// </remarks>
public sealed class SecureVaultFactory : ISecureVaultFactory
{
    private readonly ILoggerFactory? _loggerFactory;
    private readonly string _vaultPath;

    /// <summary>
    /// Initializes a new instance of the SecureVaultFactory.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory for creating vault loggers.</param>
    public SecureVaultFactory(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
        _vaultPath = GetDefaultVaultPath();
    }

    /// <inheritdoc/>
    public ISecureVault CreateVault()
    {
        if (OperatingSystem.IsWindows())
        {
            var logger = _loggerFactory?.CreateLogger<WindowsSecureVault>();
            return new WindowsSecureVault(_vaultPath, logger);
        }

        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            var logger = _loggerFactory?.CreateLogger<UnixSecureVault>();
            return new UnixSecureVault(_vaultPath, logger);
        }

        throw new PlatformNotSupportedException(
            $"SecureVault is not supported on {Environment.OSVersion.Platform}. " +
            "Supported platforms: Windows, Linux, macOS.");
    }

    /// <inheritdoc/>
    public string VaultImplementationName
    {
        get
        {
            if (OperatingSystem.IsWindows())
            {
                return "WindowsSecureVault (DPAPI)";
            }

            if (OperatingSystem.IsLinux())
            {
                return "UnixSecureVault (libsecret/AES-256)";
            }

            if (OperatingSystem.IsMacOS())
            {
                return "UnixSecureVault (AES-256)";
            }

            return "Unsupported Platform";
        }
    }

    /// <inheritdoc/>
    public string VaultStoragePath => _vaultPath;

    /// <summary>
    /// Gets the default vault storage path for the current platform.
    /// </summary>
    /// <returns>The platform-specific vault storage path.</returns>
    /// <remarks>
    /// LOGIC: Platform-specific paths:
    /// <list type="bullet">
    ///   <item>Windows: %APPDATA%/Lexichord/vault/</item>
    ///   <item>Linux: ~/.config/Lexichord/vault/</item>
    ///   <item>macOS: ~/Library/Application Support/Lexichord/vault/</item>
    /// </list>
    /// </remarks>
    private static string GetDefaultVaultPath()
    {
        string basePath;

        if (OperatingSystem.IsWindows())
        {
            // %APPDATA% = C:\Users\{user}\AppData\Roaming
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else if (OperatingSystem.IsMacOS())
        {
            // ~/Library/Application Support
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }
        else
        {
            // ~/.config
            basePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        }

        return Path.Combine(basePath, "Lexichord", "vault");
    }
}
