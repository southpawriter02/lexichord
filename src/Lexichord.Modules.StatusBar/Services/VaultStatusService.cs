using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts.Security;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Implementation of vault status service using ISecureVault.
/// </summary>
/// <remarks>
/// LOGIC: This service wraps the platform-agnostic ISecureVault to provide
/// simplified status-oriented operations for the StatusBar UI.
///
/// Error Handling Strategy:
/// - PlatformNotSupportedException → VaultStatus.Unavailable
/// - SecureVaultException → VaultStatus.Error
/// - Other exceptions → Logged and rethrown
///
/// Security Note: Key values are NEVER logged. Only key names appear in logs.
/// </remarks>
public sealed class VaultStatusService : IVaultStatusService
{
    private readonly ISecureVault _secureVault;
    private readonly ILogger<VaultStatusService> _logger;
    private VaultStatus _currentStatus = VaultStatus.Unknown;

    public VaultStatusService(
        ISecureVault secureVault,
        ILogger<VaultStatusService> logger)
    {
        _secureVault = secureVault;
        _logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<VaultStatus>? StatusChanged;

    /// <inheritdoc/>
    public async Task<VaultStatus> GetVaultStatusAsync()
    {
        _logger.LogDebug("Checking vault status");

        try
        {
            // First, test if the vault is accessible
            var isAccessible = await TestVaultAccessibilityAsync();
            if (!isAccessible)
            {
                return SetStatus(VaultStatus.Unavailable);
            }

            // Check for the test API key
            var hasKey = await _secureVault.SecretExistsAsync(VaultKeys.TestApiKey);

            var newStatus = hasKey ? VaultStatus.Ready : VaultStatus.Empty;
            _logger.LogInformation("Vault status: {Status}", newStatus);

            return SetStatus(newStatus);
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning(ex, "Secure vault not supported on this platform");
            return SetStatus(VaultStatus.Unavailable);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Vault status check failed");
            return SetStatus(VaultStatus.Error);
        }
    }

    /// <inheritdoc/>
    public async Task<bool> CheckApiKeyPresenceAsync()
    {
        _logger.LogDebug("Checking API key presence");

        try
        {
            return await _secureVault.SecretExistsAsync(VaultKeys.TestApiKey);
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning(ex, "Secure vault not supported on this platform");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "API key presence check failed");
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> StoreApiKeyAsync(string keyName, string keyValue)
    {
        // SECURITY: Never log keyValue
        _logger.LogInformation("Storing API key: {KeyName}", keyName);

        try
        {
            await _secureVault.StoreSecretAsync(keyName, keyValue);
            _logger.LogInformation("API key stored successfully: {KeyName}", keyName);

            // Update status if this was the test key
            if (keyName == VaultKeys.TestApiKey)
            {
                SetStatus(VaultStatus.Ready);
            }

            return true;
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning(ex, "Secure vault not supported on this platform");
            SetStatus(VaultStatus.Unavailable);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store API key: {KeyName}", keyName);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteApiKeyAsync(string keyName)
    {
        _logger.LogInformation("Deleting API key: {KeyName}", keyName);

        try
        {
            var deleted = await _secureVault.DeleteSecretAsync(keyName);

            if (deleted)
            {
                _logger.LogInformation("API key deleted: {KeyName}", keyName);

                // Update status if this was the test key
                if (keyName == VaultKeys.TestApiKey)
                {
                    SetStatus(VaultStatus.Empty);
                }
            }
            else
            {
                _logger.LogDebug("API key not found for deletion: {KeyName}", keyName);
            }

            return deleted;
        }
        catch (PlatformNotSupportedException ex)
        {
            _logger.LogWarning(ex, "Secure vault not supported on this platform");
            SetStatus(VaultStatus.Unavailable);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete API key: {KeyName}", keyName);
            return false;
        }
    }

    /// <summary>
    /// Tests if the vault is accessible on this platform.
    /// </summary>
    /// <remarks>
    /// LOGIC: Attempts to list secrets with the Lexichord prefix.
    /// If this succeeds, the vault is accessible. This is a lightweight
    /// operation that doesn't require reading/writing actual secrets.
    /// </remarks>
    private async Task<bool> TestVaultAccessibilityAsync()
    {
        try
        {
            // Attempt to list secrets - this verifies vault accessibility
            await foreach (var _ in _secureVault.ListSecretsAsync(VaultKeys.KeyPrefix))
            {
                // Just consume one item to verify the vault is accessible
                break;
            }

            return true;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vault accessibility test failed");
            return false;
        }
    }

    private VaultStatus SetStatus(VaultStatus newStatus)
    {
        if (_currentStatus != newStatus)
        {
            var oldStatus = _currentStatus;
            _currentStatus = newStatus;

            _logger.LogDebug("Vault status changed: {OldStatus} → {NewStatus}",
                oldStatus, newStatus);

            StatusChanged?.Invoke(this, newStatus);
        }

        return _currentStatus;
    }
}
