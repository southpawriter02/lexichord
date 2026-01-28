using System;

namespace Lexichord.Abstractions.Contracts.Security;

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
///   <item>Linux (desktop): UnixSecureVault (libsecret)</item>
///   <item>Linux (headless): UnixSecureVault (AES-256)</item>
///   <item>macOS: UnixSecureVault (AES-256)</item>
/// </list>
/// </remarks>
public interface ISecureVaultFactory
{
    /// <summary>
    /// Creates the appropriate secure vault for the current platform.
    /// </summary>
    /// <returns>A fully configured <see cref="ISecureVault"/> instance.</returns>
    /// <exception cref="PlatformNotSupportedException">
    /// The current platform is not supported.
    /// </exception>
    /// <remarks>
    /// LOGIC: The returned instance is thread-safe and can be registered
    /// as a singleton in the DI container.
    /// </remarks>
    ISecureVault CreateVault();

    /// <summary>
    /// Gets a human-readable name for the vault implementation.
    /// </summary>
    /// <remarks>
    /// Examples:
    /// <list type="bullet">
    ///   <item>"WindowsSecureVault (DPAPI)"</item>
    ///   <item>"UnixSecureVault (libsecret)"</item>
    ///   <item>"UnixSecureVault (AES-256 Fallback)"</item>
    /// </list>
    /// </remarks>
    string VaultImplementationName { get; }

    /// <summary>
    /// Gets the path to the vault storage directory.
    /// </summary>
    /// <remarks>
    /// Platform-specific paths:
    /// <list type="bullet">
    ///   <item>Windows: %APPDATA%/Lexichord/vault/</item>
    ///   <item>Linux: ~/.config/Lexichord/vault/</item>
    ///   <item>macOS: ~/Library/Application Support/Lexichord/vault/</item>
    /// </list>
    /// </remarks>
    string VaultStoragePath { get; }
}
