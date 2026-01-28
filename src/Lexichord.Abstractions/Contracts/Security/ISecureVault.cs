using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Provides secure storage for sensitive secrets (API keys, tokens, credentials).
/// </summary>
/// <remarks>
/// LOGIC: The vault encrypts secrets at rest using platform-specific mechanisms:
/// - Windows: DPAPI (Data Protection API) with CurrentUser scope
/// - Linux: libsecret (D-Bus Secret Service) or AES-256 with machine-derived key
/// - macOS: AES-256 with machine-derived key (future: Keychain Services)
///
/// <para><b>Key Naming Convention:</b></para>
/// Keys are namespace-qualified strings to avoid collisions between modules:
/// <list type="bullet">
///   <item><c>llm:openai:api-key</c> - LLM module, OpenAI provider</item>
///   <item><c>storage:s3:access-key</c> - Storage module, S3 provider</item>
///   <item><c>auth:oauth:{provider}:token</c> - OAuth tokens</item>
/// </list>
///
/// <para><b>Thread Safety:</b></para>
/// Implementations MUST be thread-safe. Multiple concurrent operations on
/// different keys are allowed. Operations on the same key are serialized.
///
/// <para><b>Memory Safety:</b></para>
/// Callers SHOULD clear returned secret values from memory after use.
/// Implementations SHOULD use <see cref="System.Security.Cryptography.CryptographicOperations.ZeroMemory"/>
/// to clear internal buffers.
/// </remarks>
public interface ISecureVault
{
    /// <summary>
    /// Stores a secret value securely.
    /// </summary>
    /// <param name="key">
    /// The unique identifier for the secret (namespace-qualified).
    /// Must be non-null, non-empty, and contain only printable ASCII characters.
    /// Maximum length: 256 characters.
    /// </param>
    /// <param name="value">
    /// The secret value to encrypt and store.
    /// Must be non-null. Empty string is allowed (though unusual).
    /// Maximum length: 1MB (1,048,576 bytes when UTF-8 encoded).
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> or <paramref name="value"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="key"/> is empty, exceeds 256 characters, or contains invalid characters.
    /// </exception>
    /// <exception cref="VaultAccessDeniedException">
    /// Permission denied to vault storage location.
    /// </exception>
    /// <exception cref="SecureVaultException">
    /// General vault operation failure (I/O error, encryption failure).
    /// </exception>
    /// <remarks>
    /// LOGIC: If a secret with the same key exists, it will be overwritten.
    /// The operation is atomic—either fully succeeds or fails without partial state.
    ///
    /// <para><b>Metadata Updates:</b></para>
    /// <list type="bullet">
    ///   <item>New secret: Sets CreatedAt, LastModifiedAt to current UTC time.</item>
    ///   <item>Overwrite: Preserves CreatedAt, updates LastModifiedAt.</item>
    /// </list>
    /// </remarks>
    Task StoreSecretAsync(string key, string value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a secret value by decrypting it from storage.
    /// </summary>
    /// <param name="key">The unique identifier for the secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The decrypted secret value (original plaintext).</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="SecretNotFoundException">
    /// The specified key does not exist in the vault.
    /// </exception>
    /// <exception cref="SecretDecryptionException">
    /// The secret exists but cannot be decrypted (corrupted or key mismatch).
    /// </exception>
    /// <exception cref="VaultAccessDeniedException">
    /// Permission denied to vault storage location.
    /// </exception>
    /// <exception cref="SecureVaultException">
    /// General vault operation failure.
    /// </exception>
    /// <remarks>
    /// LOGIC: This method updates the LastAccessedAt metadata timestamp.
    ///
    /// <para><b>Security Warning:</b></para>
    /// The returned string contains the plaintext secret. Callers SHOULD:
    /// <list type="bullet">
    ///   <item>Avoid logging or persisting the returned value.</item>
    ///   <item>Clear the string from memory after use (where possible).</item>
    ///   <item>Minimize the lifetime of the returned value.</item>
    /// </list>
    /// </remarks>
    Task<string> GetSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a secret from the vault.
    /// </summary>
    /// <param name="key">The unique identifier for the secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// <c>true</c> if the secret existed and was deleted;
    /// <c>false</c> if the secret did not exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="VaultAccessDeniedException">
    /// Permission denied to vault storage location.
    /// </exception>
    /// <exception cref="SecureVaultException">
    /// General vault operation failure (I/O error).
    /// </exception>
    /// <remarks>
    /// LOGIC: This operation is idempotent—deleting a non-existent key returns
    /// <c>false</c> without throwing an exception.
    ///
    /// <para><b>Secure Deletion:</b></para>
    /// Implementations SHOULD overwrite the file with zeros before deletion
    /// to prevent recovery from disk forensics (where supported by filesystem).
    /// </remarks>
    Task<bool> DeleteSecretAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a secret exists in the vault without decrypting it.
    /// </summary>
    /// <param name="key">The unique identifier for the secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><c>true</c> if the secret exists; <c>false</c> otherwise.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="VaultAccessDeniedException">
    /// Permission denied to vault storage location.
    /// </exception>
    /// <remarks>
    /// LOGIC: This method does NOT:
    /// <list type="bullet">
    ///   <item>Decrypt the secret (no cryptographic operations).</item>
    ///   <item>Update the LastAccessedAt timestamp.</item>
    ///   <item>Verify the secret is still decryptable.</item>
    /// </list>
    ///
    /// <para><b>Use Case:</b></para>
    /// Check credential availability before prompting user for input:
    /// <code>
    /// if (!await vault.SecretExistsAsync("llm:openai:api-key"))
    /// {
    ///     // Prompt user to enter API key
    /// }
    /// </code>
    /// </remarks>
    Task<bool> SecretExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves metadata about a stored secret without decrypting it.
    /// </summary>
    /// <param name="key">The unique identifier for the secret.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// Metadata about the secret, or <c>null</c> if the secret doesn't exist.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key"/> is null.
    /// </exception>
    /// <exception cref="VaultAccessDeniedException">
    /// Permission denied to vault storage location.
    /// </exception>
    /// <remarks>
    /// LOGIC: This method does NOT update LastAccessedAt.
    ///
    /// <para><b>Use Case:</b></para>
    /// Display credential information in settings UI:
    /// <code>
    /// var meta = await vault.GetSecretMetadataAsync("llm:openai:api-key");
    /// if (meta != null)
    /// {
    ///     Console.WriteLine($"Stored on: {meta.CreatedAt}");
    ///     Console.WriteLine($"Last used: {meta.LastAccessedAt}");
    /// }
    /// </code>
    /// </remarks>
    Task<SecretMetadata?> GetSecretMetadataAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists all secret keys stored in the vault.
    /// </summary>
    /// <param name="prefix">
    /// Optional prefix filter. If provided, only keys starting with this
    /// prefix are returned. Example: <c>"llm:"</c> returns all LLM keys.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An async enumerable of secret keys matching the prefix filter.
    /// Keys are returned in no guaranteed order.
    /// </returns>
    /// <exception cref="VaultAccessDeniedException">
    /// Permission denied to vault storage location.
    /// </exception>
    /// <remarks>
    /// LOGIC: Returns only key names, not secret values. No decryption occurs.
    ///
    /// <para><b>Streaming:</b></para>
    /// Results are streamed via <see cref="IAsyncEnumerable{T}"/> to support
    /// vaults with many secrets without loading all into memory:
    /// <code>
    /// await foreach (var key in vault.ListSecretsAsync("llm:"))
    /// {
    ///     Console.WriteLine(key);
    /// }
    /// </code>
    /// </remarks>
    IAsyncEnumerable<string> ListSecretsAsync(string? prefix = null, CancellationToken cancellationToken = default);
}
