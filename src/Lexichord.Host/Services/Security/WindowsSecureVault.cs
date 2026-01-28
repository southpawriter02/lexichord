using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services.Security;

/// <summary>
/// Windows implementation of <see cref="ISecureVault"/> using DPAPI.
/// </summary>
/// <remarks>
/// LOGIC: This implementation uses Windows Data Protection API (DPAPI) via
/// <see cref="ProtectedData"/>. DPAPI provides:
///
/// <list type="bullet">
///   <item>Encryption tied to the current Windows user account</item>
///   <item>OS-managed key derivation (no key management required)</item>
///   <item>Hardware-backed protection via TPM when available</item>
/// </list>
///
/// <para><b>Encryption Process:</b></para>
/// <code>
/// encryptedData = ProtectedData.Protect(
///     userData: secretBytes,
///     optionalEntropy: installationEntropy,
///     scope: DataProtectionScope.CurrentUser
/// );
/// </code>
///
/// <para><b>Storage:</b></para>
/// Encrypted secrets are stored as individual files in %APPDATA%/Lexichord/vault/
/// with filenames derived from SHA256 hash of the key name.
///
/// <para><b>Thread Safety:</b></para>
/// This implementation is thread-safe. File operations use locking to prevent
/// corruption from concurrent writes to the same key.
/// </remarks>
[SupportedOSPlatform("windows")]
public sealed class WindowsSecureVault : ISecureVault, IDisposable
{
    private readonly string _vaultPath;
    private readonly byte[] _entropy;
    private readonly ILogger<WindowsSecureVault>? _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private bool _disposed;

    private const int EntropyLength = 32;
    private const int SecretFileVersion = 1;

    /// <summary>
    /// Initializes a new instance of the WindowsSecureVault.
    /// </summary>
    /// <param name="vaultPath">Path to the vault storage directory.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="VaultAccessDeniedException">
    /// Cannot create or access the vault directory.
    /// </exception>
    public WindowsSecureVault(string vaultPath, ILogger<WindowsSecureVault>? logger = null)
    {
        _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
        _logger = logger;

        // LOGIC: Create vault directory with restrictive permissions
        EnsureVaultDirectoryExists();

        // LOGIC: Load or generate installation-specific entropy
        _entropy = LoadOrCreateEntropy();

        _logger?.LogDebug(
            "WindowsSecureVault initialized: Path={VaultPath}",
            _vaultPath);
    }

    /// <inheritdoc/>
    public async Task StoreSecretAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);
        ArgumentNullException.ThrowIfNull(value);

        var keyHash = KeyHasher.ComputeFileName(key);
        var secretPath = GetSecretPath(keyHash);
        var metaPath = GetMetadataPath(keyHash);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            // LOGIC: Check if updating existing secret (preserve CreatedAt)
            var existingMeta = await ReadMetadataAsync(metaPath, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            // LOGIC: Encrypt the secret value using DPAPI
            var plainBytes = Encoding.UTF8.GetBytes(value);
            byte[] encryptedBytes;
            try
            {
                encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    _entropy,
                    DataProtectionScope.CurrentUser);
            }
            finally
            {
                // LOGIC: Clear plaintext from memory
                CryptographicOperations.ZeroMemory(plainBytes);
            }

            // LOGIC: Write encrypted file with version header
            await WriteSecretFileAsync(secretPath, encryptedBytes, cancellationToken);

            // LOGIC: Write metadata
            var metadata = new StoredSecretMetadata
            {
                KeyName = key,
                CreatedAt = existingMeta?.CreatedAt ?? now,
                LastModifiedAt = now,
                LastAccessedAt = existingMeta?.LastAccessedAt,
                Version = SecretFileVersion
            };
            await WriteMetadataAsync(metaPath, metadata, cancellationToken);

            _logger?.LogInformation(
                "Secret stored: KeyHash={KeyHash}",
                keyHash[..8]);
        }
        catch (CryptographicException ex)
        {
            _logger?.LogError(ex, "DPAPI encryption failed: KeyHash={KeyHash}", keyHash[..8]);
            throw new SecureVaultException("Failed to encrypt secret using DPAPI.", ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultAccessDeniedException($"Access denied to vault path: {_vaultPath}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        var keyHash = KeyHasher.ComputeFileName(key);
        var secretPath = GetSecretPath(keyHash);
        var metaPath = GetMetadataPath(keyHash);

        if (!File.Exists(secretPath))
        {
            _logger?.LogWarning("Secret not found: KeyHash={KeyHash}", keyHash[..8]);
            throw new SecretNotFoundException(key);
        }

        try
        {
            // LOGIC: Read encrypted file
            var encryptedBytes = await ReadSecretFileAsync(secretPath, cancellationToken);

            // LOGIC: Decrypt using DPAPI
            var plainBytes = ProtectedData.Unprotect(
                encryptedBytes,
                _entropy,
                DataProtectionScope.CurrentUser);

            var value = Encoding.UTF8.GetString(plainBytes);

            // LOGIC: Clear decrypted bytes from memory
            CryptographicOperations.ZeroMemory(plainBytes);

            // LOGIC: Update LastAccessedAt (fire and forget, don't fail on error)
            _ = UpdateLastAccessedAsync(metaPath, cancellationToken);

            _logger?.LogInformation(
                "Secret retrieved: KeyHash={KeyHash}",
                keyHash[..8]);

            return value;
        }
        catch (CryptographicException ex)
        {
            _logger?.LogError(ex, "DPAPI decryption failed: KeyHash={KeyHash}", keyHash[..8]);
            throw new SecretDecryptionException(key, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultAccessDeniedException($"Access denied to vault path: {_vaultPath}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteSecretAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        var keyHash = KeyHasher.ComputeFileName(key);
        var secretPath = GetSecretPath(keyHash);
        var metaPath = GetMetadataPath(keyHash);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(secretPath))
            {
                return false;
            }

            // LOGIC: Securely delete by overwriting with zeros first
            await SecureDeleteFileAsync(secretPath, cancellationToken);

            // LOGIC: Delete metadata file
            if (File.Exists(metaPath))
            {
                File.Delete(metaPath);
            }

            _logger?.LogInformation(
                "Secret deleted: KeyHash={KeyHash}",
                keyHash[..8]);

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultAccessDeniedException($"Access denied to delete secret: {key}", ex);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    /// <inheritdoc/>
    public Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        var keyHash = KeyHasher.ComputeFileName(key);
        var secretPath = GetSecretPath(keyHash);

        return Task.FromResult(File.Exists(secretPath));
    }

    /// <inheritdoc/>
    public async Task<SecretMetadata?> GetSecretMetadataAsync(string key, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        KeyValidator.ValidateKey(key);

        var keyHash = KeyHasher.ComputeFileName(key);
        var metaPath = GetMetadataPath(keyHash);

        var stored = await ReadMetadataAsync(metaPath, cancellationToken);
        if (stored is null)
        {
            return null;
        }

        return new SecretMetadata(
            stored.KeyName,
            stored.CreatedAt,
            stored.LastAccessedAt,
            stored.LastModifiedAt);
    }

    /// <inheritdoc/>
    public async IAsyncEnumerable<string> ListSecretsAsync(
        string? prefix = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        if (!Directory.Exists(_vaultPath))
        {
            yield break;
        }

        var metaFiles = Directory.GetFiles(_vaultPath, "*.meta");

        foreach (var metaPath in metaFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var stored = await ReadMetadataAsync(metaPath, cancellationToken);
            if (stored is null)
            {
                continue;
            }

            // LOGIC: Apply prefix filter if specified
            if (prefix is null || stored.KeyName.StartsWith(prefix, StringComparison.Ordinal))
            {
                yield return stored.KeyName;
            }
        }
    }

    /// <summary>
    /// Disposes resources used by the vault.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // LOGIC: Clear entropy from memory
        CryptographicOperations.ZeroMemory(_entropy);

        _writeLock.Dispose();
    }

    #region Private Methods

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void EnsureVaultDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_vaultPath))
            {
                Directory.CreateDirectory(_vaultPath);

                // LOGIC: On Windows, default user folder permissions provide
                // adequate security (owner-only access)
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultAccessDeniedException($"Cannot create vault directory: {_vaultPath}", ex);
        }
    }

    private byte[] LoadOrCreateEntropy()
    {
        var entropyPath = Path.Combine(_vaultPath, ".entropy");

        if (File.Exists(entropyPath))
        {
            var existingEntropy = File.ReadAllBytes(entropyPath);
            if (existingEntropy.Length == EntropyLength)
            {
                return existingEntropy;
            }
            // LOGIC: Invalid entropy file, regenerate
            _logger?.LogWarning("Invalid entropy file found, regenerating");
        }

        // LOGIC: Generate new entropy using CSPRNG
        var entropy = RandomNumberGenerator.GetBytes(EntropyLength);
        File.WriteAllBytes(entropyPath, entropy);

        // LOGIC: Make entropy file hidden
        File.SetAttributes(entropyPath, FileAttributes.Hidden);

        _logger?.LogInformation("Generated new vault entropy");

        return entropy;
    }

    private string GetSecretPath(string keyHash)
        => Path.Combine(_vaultPath, $"{keyHash}.secret");

    private string GetMetadataPath(string keyHash)
        => Path.Combine(_vaultPath, $"{keyHash}.meta");

    private static async Task WriteSecretFileAsync(
        string path,
        byte[] encryptedData,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true);

        // LOGIC: Write version header
        var version = BitConverter.GetBytes(SecretFileVersion);
        await stream.WriteAsync(version, cancellationToken);

        // LOGIC: Write encrypted data length
        var length = BitConverter.GetBytes(encryptedData.Length);
        await stream.WriteAsync(length, cancellationToken);

        // LOGIC: Write encrypted data
        await stream.WriteAsync(encryptedData, cancellationToken);
    }

    private static async Task<byte[]> ReadSecretFileAsync(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 4096,
            useAsync: true);

        // LOGIC: Read and validate version
        var versionBytes = new byte[4];
        await stream.ReadExactlyAsync(versionBytes, cancellationToken);
        var version = BitConverter.ToInt32(versionBytes);

        if (version != SecretFileVersion)
        {
            throw new SecureVaultException($"Unsupported secret file version: {version}");
        }

        // LOGIC: Read data length
        var lengthBytes = new byte[4];
        await stream.ReadExactlyAsync(lengthBytes, cancellationToken);
        var length = BitConverter.ToInt32(lengthBytes);

        // LOGIC: Read encrypted data
        var encryptedData = new byte[length];
        await stream.ReadExactlyAsync(encryptedData, cancellationToken);

        return encryptedData;
    }

    private async Task WriteMetadataAsync(
        string path,
        StoredSecretMetadata metadata,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private async Task<StoredSecretMetadata?> ReadMetadataAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken);
            return JsonSerializer.Deserialize<StoredSecretMetadata>(json, JsonOptions);
        }
        catch (JsonException)
        {
            _logger?.LogWarning("Corrupted metadata file: {Path}", path);
            return null;
        }
    }

    private async Task UpdateLastAccessedAsync(string metaPath, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await ReadMetadataAsync(metaPath, cancellationToken);
            if (metadata is not null)
            {
                metadata.LastAccessedAt = DateTimeOffset.UtcNow;
                await WriteMetadataAsync(metaPath, metadata, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            // LOGIC: Non-critical operation, log but don't throw
            _logger?.LogDebug(ex, "Failed to update LastAccessedAt");
        }
    }

    private static async Task SecureDeleteFileAsync(string path, CancellationToken cancellationToken)
    {
        // LOGIC: Overwrite file with zeros before deletion
        var fileInfo = new FileInfo(path);
        var length = fileInfo.Length;

        await using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write))
        {
            var zeros = new byte[Math.Min(length, 8192)];
            var remaining = length;
            while (remaining > 0)
            {
                var toWrite = (int)Math.Min(remaining, zeros.Length);
                await stream.WriteAsync(zeros.AsMemory(0, toWrite), cancellationToken);
                remaining -= toWrite;
            }
            await stream.FlushAsync(cancellationToken);
        }

        File.Delete(path);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #endregion

    #region Internal Types

    /// <summary>
    /// Internal representation of stored metadata.
    /// </summary>
    private sealed class StoredSecretMetadata
    {
        public string KeyName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastAccessedAt { get; set; }
        public DateTimeOffset LastModifiedAt { get; set; }
        public int Version { get; set; }
    }

    #endregion
}
