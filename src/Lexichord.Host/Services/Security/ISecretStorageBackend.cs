using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lexichord.Abstractions.Contracts.Security;

namespace Lexichord.Host.Services.Security;

/// <summary>
/// Internal interface for secret storage backends.
/// </summary>
/// <remarks>
/// LOGIC: This abstraction allows <see cref="UnixSecureVault"/> to delegate to
/// different implementations based on platform capabilities:
/// <list type="bullet">
///   <item><see cref="LibSecretBackend"/> - D-Bus Secret Service (desktop Linux)</item>
///   <item><see cref="AesFileBackend"/> - AES-256-GCM file encryption (fallback)</item>
/// </list>
/// </remarks>
internal interface ISecretStorageBackend
{
    /// <summary>
    /// Gets the display name of this backend for logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Stores a secret value.
    /// </summary>
    Task StoreAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a secret value.
    /// </summary>
    Task<string> GetAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a secret.
    /// </summary>
    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Checks if a secret exists.
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Gets metadata for a secret.
    /// </summary>
    Task<SecretMetadata?> GetMetadataAsync(string key, CancellationToken cancellationToken);

    /// <summary>
    /// Lists all secrets, optionally filtered by prefix.
    /// </summary>
    IAsyncEnumerable<string> ListAsync(string? prefix, CancellationToken cancellationToken);
}
