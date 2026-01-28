using System;

namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Metadata about a stored secret, accessible without decryption.
/// </summary>
/// <remarks>
/// LOGIC: Metadata is stored in a separate unencrypted file alongside each
/// secret. This enables audit logging, stale credential detection, and UI
/// display without exposing the secret value.
///
/// <para><b>Privacy Note:</b></para>
/// The <see cref="KeyName"/> is stored in plaintext. Avoid including
/// sensitive information in key names (e.g., user IDs, email addresses).
/// </remarks>
/// <param name="KeyName">
/// The full key name of the secret (e.g., "llm:openai:api-key").
/// </param>
/// <param name="CreatedAt">
/// UTC timestamp when the secret was first stored.
/// Immutable—does not change on updates.
/// </param>
/// <param name="LastAccessedAt">
/// UTC timestamp when the secret was last decrypted via <see cref="ISecureVault.GetSecretAsync"/>.
/// Null if the secret has never been retrieved since creation.
/// </param>
/// <param name="LastModifiedAt">
/// UTC timestamp when the secret value was last updated via <see cref="ISecureVault.StoreSecretAsync"/>.
/// Equals <see cref="CreatedAt"/> if never updated.
/// </param>
public record SecretMetadata(
    string KeyName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastAccessedAt,
    DateTimeOffset LastModifiedAt
)
{
    /// <summary>
    /// Calculates the age of the secret since creation.
    /// </summary>
    /// <remarks>
    /// Useful for identifying stale credentials that may need rotation.
    /// </remarks>
    public TimeSpan Age => DateTimeOffset.UtcNow - CreatedAt;

    /// <summary>
    /// Calculates time since the secret was last accessed.
    /// </summary>
    /// <returns>
    /// Time since last access, or null if never accessed.
    /// </returns>
    public TimeSpan? TimeSinceLastAccess => LastAccessedAt.HasValue
        ? DateTimeOffset.UtcNow - LastAccessedAt.Value
        : null;

    /// <summary>
    /// Indicates whether the secret appears unused (never accessed after creation).
    /// </summary>
    public bool IsUnused => LastAccessedAt is null;
}
