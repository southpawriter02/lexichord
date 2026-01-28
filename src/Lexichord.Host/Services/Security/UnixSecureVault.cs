using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services.Security;

/// <summary>
/// Unix implementation of <see cref="ISecureVault"/> for Linux and macOS.
/// </summary>
/// <remarks>
/// LOGIC: This implementation uses a two-tier strategy:
///
/// <list type="number">
///   <item>
///     <b>Primary (Desktop Linux):</b> <see cref="LibSecretBackend"/> uses D-Bus
///     Secret Service (GNOME Keyring, KDE Wallet) for system-integrated storage.
///   </item>
///   <item>
///     <b>Fallback:</b> <see cref="AesFileBackend"/> provides AES-256-GCM file-based
///     encryption for headless Linux, macOS, or when libsecret is unavailable.
///   </item>
/// </list>
///
/// <para><b>Backend Selection:</b></para>
/// The appropriate backend is selected at construction time based on:
/// <list type="bullet">
///   <item>Operating system (Linux vs macOS)</item>
///   <item>D-Bus session availability</item>
///   <item>libsecret library presence</item>
/// </list>
///
/// <para><b>Thread Safety:</b></para>
/// Delegates to thread-safe backend implementations.
/// </remarks>
[SupportedOSPlatform("linux")]
[SupportedOSPlatform("macos")]
public sealed class UnixSecureVault : ISecureVault, IDisposable
{
    private readonly ISecretStorageBackend _backend;
    private readonly ILogger<UnixSecureVault>? _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the name of the active backend for diagnostics.
    /// </summary>
    public string BackendName => _backend.Name;

    /// <summary>
    /// Initializes a new instance of UnixSecureVault with automatic backend selection.
    /// </summary>
    /// <param name="vaultPath">Path to the vault storage directory.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public UnixSecureVault(string vaultPath, ILogger<UnixSecureVault>? logger = null)
    {
        _logger = logger;
        _backend = SelectBackend(vaultPath);

        _logger?.LogInformation(
            "UnixSecureVault initialized: Backend={BackendName}",
            _backend.Name);
    }

    /// <summary>
    /// Initializes a new instance with a specific backend (for testing).
    /// </summary>
    /// <param name="backend">The backend to use.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    internal UnixSecureVault(ISecretStorageBackend backend, ILogger<UnixSecureVault>? logger = null)
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _logger = logger;

        _logger?.LogInformation(
            "UnixSecureVault initialized with injected backend: {BackendName}",
            _backend.Name);
    }

    private ISecretStorageBackend SelectBackend(string vaultPath)
    {
        // LOGIC: Try libsecret first on Linux desktop environments
        if (OperatingSystem.IsLinux())
        {
            var libSecretLogger = _logger is not null
                ? LoggerFactoryExtensions.CreateLogger(_logger.GetType().Assembly.GetName().Name!, typeof(LibSecretBackend))
                : null;

            var libSecretBackend = new LibSecretBackend(libSecretLogger);
            if (libSecretBackend.IsAvailable())
            {
                _logger?.LogDebug("libsecret available, using Secret Service backend");
                // Note: libsecret is a stub, so we fall through to AES for now
                // When P/Invoke is implemented, remove this disposal and return libSecretBackend
                libSecretBackend.Dispose();
            }
            else
            {
                libSecretBackend.Dispose();
            }
        }

        // LOGIC: Fall back to AES-256-GCM file-based encryption
        _logger?.LogDebug("Using AES-256-GCM file backend");

        var aesLogger = _logger is not null
            ? LoggerFactoryExtensions.CreateLogger(_logger.GetType().Assembly.GetName().Name!, typeof(AesFileBackend))
            : null;

        return new AesFileBackend(vaultPath, aesLogger);
    }

    /// <inheritdoc/>
    public Task StoreSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        return _backend.StoreAsync(key, value, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<string> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        return _backend.GetAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        return _backend.DeleteAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        return _backend.ExistsAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public Task<SecretMetadata?> GetSecretMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        return _backend.GetMetadataAsync(key, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<string> ListSecretsAsync(string? prefix = null, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        return _backend.ListAsync(prefix, cancellationToken);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_backend is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}

/// <summary>
/// Logger factory helper for creating typed loggers.
/// </summary>
internal static class LoggerFactoryExtensions
{
    public static ILogger? CreateLogger(string categoryName, Type type)
    {
        // In a real scenario, we'd use ILoggerFactory. For now, return null.
        return null;
    }
}
