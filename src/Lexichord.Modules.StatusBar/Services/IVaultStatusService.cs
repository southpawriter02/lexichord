namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Vault status values.
/// </summary>
public enum VaultStatus
{
    /// <summary>Unknown status (not yet checked).</summary>
    Unknown,
    
    /// <summary>Vault is ready with keys loaded.</summary>
    Ready,
    
    /// <summary>Vault is empty (no keys configured).</summary>
    Empty,
    
    /// <summary>Vault encountered an error.</summary>
    Error
}

/// <summary>
/// Service for checking secure vault status.
/// </summary>
/// <remarks>
/// LOGIC: The vault status service checks whether the secure vault
/// has API keys configured and is accessible.
/// Full implementation comes in v0.0.8c.
/// </remarks>
public interface IVaultStatusService
{
    /// <summary>
    /// Gets the current vault status.
    /// </summary>
    Task<VaultStatus> GetVaultStatusAsync();

    /// <summary>
    /// Stores an API key in the vault.
    /// </summary>
    /// <param name="keyName">The key identifier.</param>
    /// <param name="keyValue">The key value (will be encrypted).</param>
    Task StoreKeyAsync(string keyName, string keyValue);

    /// <summary>
    /// Checks if a specific key exists in the vault.
    /// </summary>
    /// <param name="keyName">The key identifier.</param>
    Task<bool> HasKeyAsync(string keyName);

    /// <summary>
    /// Event raised when vault status changes.
    /// </summary>
    event EventHandler<VaultStatus>? StatusChanged;
}
