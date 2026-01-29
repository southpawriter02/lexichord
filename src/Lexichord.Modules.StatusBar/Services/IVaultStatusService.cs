namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Vault accessibility status.
/// </summary>
/// <remarks>
/// LOGIC: Status transitions:
/// - Unknown → Ready/Empty/Error/Unavailable (after first check)
/// - Empty → Ready (after key stored)
/// - Ready → Empty (after all keys deleted)
/// - Any → Error (on vault access failure)
/// - Any → Unavailable (platform doesn't support secure storage)
/// </remarks>
public enum VaultStatus
{
    /// <summary>Status has not been checked yet.</summary>
    Unknown,

    /// <summary>Vault is accessible and contains at least one key.</summary>
    Ready,

    /// <summary>Vault is accessible but contains no keys.</summary>
    Empty,

    /// <summary>Vault encountered an error during access.</summary>
    Error,

    /// <summary>Vault is not available on this platform.</summary>
    Unavailable
}

/// <summary>
/// Service for checking and managing vault status.
/// </summary>
/// <remarks>
/// LOGIC: This service provides a high-level abstraction over ISecureVault
/// specifically for the StatusBar module's vault status indicator.
///
/// The service manages:
/// - Vault accessibility verification
/// - Test API key presence checking
/// - Key storage and deletion
///
/// Implementation Note: This service wraps ISecureVault to provide
/// simplified status-oriented operations for the UI.
/// </remarks>
public interface IVaultStatusService
{
    /// <summary>
    /// Gets the current vault status.
    /// </summary>
    /// <returns>The vault status indicating accessibility and key presence.</returns>
    /// <remarks>
    /// LOGIC: This method performs a vault accessibility test and checks
    /// for the presence of the test API key. Returns:
    /// - Ready: Vault accessible and test key exists
    /// - Empty: Vault accessible but no test key
    /// - Error: Vault access failed
    /// - Unavailable: Platform doesn't support secure storage
    /// </remarks>
    Task<VaultStatus> GetVaultStatusAsync();

    /// <summary>
    /// Checks if the test API key exists in the vault.
    /// </summary>
    /// <returns>True if the test key exists; false otherwise.</returns>
    Task<bool> CheckApiKeyPresenceAsync();

    /// <summary>
    /// Stores an API key in the vault.
    /// </summary>
    /// <param name="keyName">The name/identifier for the key.</param>
    /// <param name="keyValue">The secret key value to store.</param>
    /// <returns>True if storage succeeded; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: The keyValue is never logged. Only the keyName is logged
    /// for diagnostic purposes.
    /// </remarks>
    Task<bool> StoreApiKeyAsync(string keyName, string keyValue);

    /// <summary>
    /// Deletes an API key from the vault.
    /// </summary>
    /// <param name="keyName">The name/identifier of the key to delete.</param>
    /// <returns>True if the key was deleted; false if it didn't exist.</returns>
    Task<bool> DeleteApiKeyAsync(string keyName);

    /// <summary>
    /// Raised when the vault status changes.
    /// </summary>
    event EventHandler<VaultStatus>? StatusChanged;
}
