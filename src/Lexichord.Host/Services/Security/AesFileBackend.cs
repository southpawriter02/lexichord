using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services.Security;

/// <summary>
/// AES-256-GCM file-based secret storage backend.
/// </summary>
/// <remarks>
/// LOGIC: This backend encrypts secrets using AES-256-GCM and stores them as files.
/// It serves as a fallback when <see cref="LibSecretBackend"/> is not available.
///
/// <para><b>Cryptographic Specifications:</b></para>
/// <list type="bullet">
///   <item>Algorithm: AES-256-GCM (authenticated encryption)</item>
///   <item>Key Size: 256-bit</item>
///   <item>Nonce: 96-bit (random per encryption)</item>
///   <item>Auth Tag: 128-bit</item>
///   <item>KDF: PBKDF2-SHA256 with 100,000 iterations</item>
///   <item>Salt: 256-bit per-installation</item>
/// </list>
///
/// <para><b>Key Derivation:</b></para>
/// The encryption key is derived from:
/// <list type="number">
///   <item>Machine ID (Linux: /etc/machine-id, macOS: hostname)</item>
///   <item>Username (Environment.UserName)</item>
///   <item>Per-installation salt (.salt file)</item>
/// </list>
///
/// <para><b>Security Properties:</b></para>
/// <list type="bullet">
///   <item>Secrets are machine and user bound</item>
///   <item>GCM provides integrity verification (detects tampering)</item>
///   <item>Secure deletion overwrites files with zeros</item>
///   <item>Memory cleared using CryptographicOperations.ZeroMemory</item>
/// </list>
/// </remarks>
internal sealed class AesFileBackend : ISecretStorageBackend, IDisposable
{
    private const int KeySizeBytes = 32;      // 256-bit key
    private const int NonceSizeBytes = 12;    // 96-bit nonce
    private const int TagSizeBytes = 16;      // 128-bit auth tag
    private const int SaltSizeBytes = 32;     // 256-bit salt
    private const int Pbkdf2Iterations = 100_000;
    private const int FileVersion = 1;

    private readonly string _vaultPath;
    private readonly ILogger? _logger;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly byte[] _encryptionKey;
    private bool _disposed;

    /// <inheritdoc/>
    public string Name => "AES-256-GCM (File)";

    /// <summary>
    /// Initializes a new instance of the AesFileBackend.
    /// </summary>
    /// <param name="vaultPath">Path to the vault directory.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="VaultAccessDeniedException">
    /// Cannot create or access the vault directory.
    /// </exception>
    public AesFileBackend(string vaultPath, ILogger? logger = null)
    {
        _vaultPath = vaultPath ?? throw new ArgumentNullException(nameof(vaultPath));
        _logger = logger;

        // LOGIC: Create vault directory with restrictive permissions
        EnsureVaultDirectoryExists();

        // LOGIC: Derive encryption key from machine identity
        _encryptionKey = DeriveEncryptionKey();

        _logger?.LogDebug(
            "AesFileBackend initialized: Path={VaultPath}",
            _vaultPath);
    }

    /// <inheritdoc/>
    public async Task StoreAsync(string key, string value, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var keyHash = KeyHasher.ComputeFileName(key);
        var secretPath = GetSecretPath(keyHash);
        var metaPath = GetMetadataPath(keyHash);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            // LOGIC: Check if updating existing secret (preserve CreatedAt)
            var existingMeta = await ReadMetadataAsync(metaPath, cancellationToken);
            var now = DateTimeOffset.UtcNow;

            // LOGIC: Encrypt the secret value
            var plainBytes = Encoding.UTF8.GetBytes(value);
            byte[] encryptedData;
            try
            {
                encryptedData = Encrypt(plainBytes);
            }
            finally
            {
                // LOGIC: Clear plaintext from memory
                CryptographicOperations.ZeroMemory(plainBytes);
            }

            // LOGIC: Write encrypted file with version header
            await WriteSecretFileAsync(secretPath, encryptedData, cancellationToken);

            // LOGIC: Apply restrictive file permissions (Unix only)
            SetRestrictivePermissions(secretPath);

            // LOGIC: Write metadata
            var metadata = new StoredMetadata
            {
                KeyName = key,
                CreatedAt = existingMeta?.CreatedAt ?? now,
                LastModifiedAt = now,
                LastAccessedAt = existingMeta?.LastAccessedAt,
                Version = FileVersion
            };
            await WriteMetadataAsync(metaPath, metadata, cancellationToken);

            _logger?.LogInformation(
                "Secret stored: KeyHash={KeyHash}",
                keyHash[..8]);
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
    public async Task<string> GetAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

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
            var encryptedData = await ReadSecretFileAsync(secretPath, cancellationToken);

            // LOGIC: Decrypt using AES-GCM
            byte[] plainBytes;
            try
            {
                plainBytes = Decrypt(encryptedData);
            }
            catch (CryptographicException ex)
            {
                _logger?.LogError(ex, "Decryption failed: KeyHash={KeyHash}", keyHash[..8]);
                throw new SecretDecryptionException(key, ex);
            }

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
        catch (SecretNotFoundException)
        {
            throw;
        }
        catch (SecretDecryptionException)
        {
            throw;
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultAccessDeniedException($"Access denied to vault path: {_vaultPath}", ex);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

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
    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        var keyHash = KeyHasher.ComputeFileName(key);
        var secretPath = GetSecretPath(keyHash);

        return Task.FromResult(File.Exists(secretPath));
    }

    /// <inheritdoc/>
    public async Task<SecretMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

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
    public async IAsyncEnumerable<string> ListAsync(
        string? prefix,
        [EnumeratorCancellation] CancellationToken cancellationToken)
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

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // LOGIC: Clear encryption key from memory
        CryptographicOperations.ZeroMemory(_encryptionKey);

        _writeLock.Dispose();
    }

    #region Private Methods - Cryptography

    private byte[] Encrypt(byte[] plaintext)
    {
        // LOGIC: Generate random nonce for each encryption
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aes = new AesGcm(_encryptionKey, TagSizeBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        // LOGIC: Output format: [nonce][tag][ciphertext]
        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        nonce.CopyTo(result, 0);
        tag.CopyTo(result, NonceSizeBytes);
        ciphertext.CopyTo(result, NonceSizeBytes + TagSizeBytes);

        return result;
    }

    private byte[] Decrypt(byte[] encryptedData)
    {
        if (encryptedData.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Encrypted data is too short.");
        }

        // LOGIC: Parse format: [nonce][tag][ciphertext]
        var nonce = encryptedData.AsSpan(0, NonceSizeBytes);
        var tag = encryptedData.AsSpan(NonceSizeBytes, TagSizeBytes);
        var ciphertext = encryptedData.AsSpan(NonceSizeBytes + TagSizeBytes);

        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(_encryptionKey, TagSizeBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    private byte[] DeriveEncryptionKey()
    {
        // LOGIC: Get machine-specific identity
        var machineId = GetMachineId();
        var userId = Environment.UserName;

        // LOGIC: Load or create per-installation salt
        var salt = LoadOrCreateSalt();

        // LOGIC: Combine machine ID and user ID for password material
        var keyMaterial = $"{machineId}:{userId}";
        var password = Encoding.UTF8.GetBytes(keyMaterial);

        try
        {
            // LOGIC: Derive key using PBKDF2-SHA256
            using var kdf = new Rfc2898DeriveBytes(
                password,
                salt,
                Pbkdf2Iterations,
                HashAlgorithmName.SHA256);

            return kdf.GetBytes(KeySizeBytes);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(password);
        }
    }

    private string GetMachineId()
    {
        if (OperatingSystem.IsLinux())
        {
            // LOGIC: Linux uses /etc/machine-id (systemd standard)
            const string machineIdPath = "/etc/machine-id";
            if (File.Exists(machineIdPath))
            {
                var id = File.ReadAllText(machineIdPath).Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }

            // Fallback to /var/lib/dbus/machine-id
            const string dbusIdPath = "/var/lib/dbus/machine-id";
            if (File.Exists(dbusIdPath))
            {
                var id = File.ReadAllText(dbusIdPath).Trim();
                if (!string.IsNullOrEmpty(id))
                {
                    return id;
                }
            }
        }

        // LOGIC: macOS and fallback: use hostname
        return Environment.MachineName;
    }

    private byte[] LoadOrCreateSalt()
    {
        var saltPath = Path.Combine(_vaultPath, ".salt");

        if (File.Exists(saltPath))
        {
            var existingSalt = File.ReadAllBytes(saltPath);
            if (existingSalt.Length == SaltSizeBytes)
            {
                return existingSalt;
            }
            // LOGIC: Invalid salt file, regenerate
            _logger?.LogWarning("Invalid salt file found, regenerating");
        }

        // LOGIC: Generate new salt using CSPRNG
        var salt = RandomNumberGenerator.GetBytes(SaltSizeBytes);
        File.WriteAllBytes(saltPath, salt);

        // LOGIC: Apply restrictive permissions to salt file
        SetRestrictivePermissions(saltPath);

        _logger?.LogInformation("Generated new vault salt");

        return salt;
    }

    #endregion

    #region Private Methods - File I/O

    private void EnsureVaultDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_vaultPath))
            {
                Directory.CreateDirectory(_vaultPath);

                // LOGIC: Apply restrictive directory permissions (Unix only)
                SetRestrictiveDirectoryPermissions(_vaultPath);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            throw new VaultAccessDeniedException($"Cannot create vault directory: {_vaultPath}", ex);
        }
    }

    private static void SetRestrictivePermissions(string filePath)
    {
        // LOGIC: On Unix, set file permissions to 0600 (owner read/write only)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            catch (Exception)
            {
                // Best effort - don't fail if permissions can't be set
            }
        }
    }

    private static void SetRestrictiveDirectoryPermissions(string directoryPath)
    {
        // LOGIC: On Unix, set directory permissions to 0700 (owner rwx only)
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            try
            {
                File.SetUnixFileMode(
                    directoryPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
            catch (Exception)
            {
                // Best effort - don't fail if permissions can't be set
            }
        }
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
        var version = BitConverter.GetBytes(FileVersion);
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

        if (version != FileVersion)
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
        StoredMetadata metadata,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(metadata, JsonOptions);
        await File.WriteAllTextAsync(path, json, cancellationToken);
    }

    private async Task<StoredMetadata?> ReadMetadataAsync(
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
            return JsonSerializer.Deserialize<StoredMetadata>(json, JsonOptions);
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

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// <summary>
    /// Internal representation of stored metadata.
    /// </summary>
    private sealed class StoredMetadata
    {
        public string KeyName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? LastAccessedAt { get; set; }
        public DateTimeOffset LastModifiedAt { get; set; }
        public int Version { get; set; }
    }

    #endregion
}
