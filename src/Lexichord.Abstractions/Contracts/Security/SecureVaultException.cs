using System;

namespace Lexichord.Abstractions.Contracts.Security;

/// <summary>
/// Base exception for all secure vault operations.
/// </summary>
/// <remarks>
/// LOGIC: Provides a common catch-all for vault errors while allowing
/// more specific exception types for targeted error handling.
///
/// <para><b>Exception Hierarchy:</b></para>
/// <list type="bullet">
///   <item><see cref="SecureVaultException"/> - Base type</item>
///   <item><see cref="SecretNotFoundException"/> - Key doesn't exist</item>
///   <item><see cref="SecretDecryptionException"/> - Decryption failed</item>
///   <item><see cref="VaultAccessDeniedException"/> - Permission denied</item>
/// </list>
/// </remarks>
public class SecureVaultException : Exception
{
    /// <summary>
    /// Initializes a new instance with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public SecureVaultException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The underlying exception.</param>
    public SecureVaultException(string message, Exception innerException)
        : base(message, innerException) { }
}

/// <summary>
/// Thrown when a requested secret does not exist in the vault.
/// </summary>
/// <remarks>
/// LOGIC: This is the expected exception when calling <see cref="ISecureVault.GetSecretAsync"/>
/// for a key that has never been stored or has been deleted.
///
/// <para><b>Handling Pattern:</b></para>
/// <code>
/// try
/// {
///     var key = await vault.GetSecretAsync("llm:openai:api-key");
/// }
/// catch (SecretNotFoundException ex)
/// {
///     // Prompt user to configure API key
///     Logger.LogInformation("API key not configured: {Key}", ex.KeyName);
/// }
/// </code>
/// </remarks>
public class SecretNotFoundException : SecureVaultException
{
    /// <summary>
    /// The key that was not found.
    /// </summary>
    public string KeyName { get; }

    /// <summary>
    /// Initializes a new instance for the specified key.
    /// </summary>
    /// <param name="keyName">The key that was not found.</param>
    public SecretNotFoundException(string keyName)
        : base($"Secret '{keyName}' was not found in the vault.")
    {
        KeyName = keyName;
    }
}

/// <summary>
/// Thrown when the vault cannot decrypt a stored secret.
/// </summary>
/// <remarks>
/// LOGIC: This exception indicates a serious problem:
/// <list type="bullet">
///   <item>The encrypted file is corrupted.</item>
///   <item>The encryption key has changed (machine reinstall, user change).</item>
///   <item>The entropy file is missing or modified.</item>
/// </list>
///
/// <para><b>Recovery:</b></para>
/// The secret must be re-stored. The corrupted entry should be deleted
/// and the user prompted to re-enter the credential.
/// </remarks>
public class SecretDecryptionException : SecureVaultException
{
    /// <summary>
    /// The key that failed to decrypt.
    /// </summary>
    public string KeyName { get; }

    /// <summary>
    /// Initializes a new instance for the specified key.
    /// </summary>
    /// <param name="keyName">The key that failed to decrypt.</param>
    /// <param name="innerException">The underlying cryptographic exception.</param>
    public SecretDecryptionException(string keyName, Exception innerException)
        : base($"Failed to decrypt secret '{keyName}'. The vault may be corrupted or the encryption key has changed.", innerException)
    {
        KeyName = keyName;
    }
}

/// <summary>
/// Thrown when access to the vault storage location is denied.
/// </summary>
/// <remarks>
/// LOGIC: This exception indicates a permission or access control issue:
/// <list type="bullet">
///   <item>Vault directory has incorrect permissions.</item>
///   <item>Another process has exclusive lock on vault files.</item>
///   <item>libsecret service denied the request (Linux).</item>
///   <item>User account lacks necessary privileges.</item>
/// </list>
///
/// <para><b>Recovery:</b></para>
/// Check file permissions, ensure exclusive access, or run with elevated privileges.
/// </remarks>
public class VaultAccessDeniedException : SecureVaultException
{
    /// <summary>
    /// Initializes a new instance with a message.
    /// </summary>
    /// <param name="message">Detailed description of the access denial.</param>
    public VaultAccessDeniedException(string message)
        : base(message) { }

    /// <summary>
    /// Initializes a new instance with a message and inner exception.
    /// </summary>
    /// <param name="message">Detailed description of the access denial.</param>
    /// <param name="innerException">The underlying I/O or security exception.</param>
    public VaultAccessDeniedException(string message, Exception innerException)
        : base(message, innerException) { }
}
