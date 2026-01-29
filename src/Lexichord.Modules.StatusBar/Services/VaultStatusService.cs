using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.Services;

/// <summary>
/// Placeholder vault status service implementation.
/// </summary>
/// <remarks>
/// LOGIC: This is a stub implementation for v0.0.8a.
/// Real vault integration comes in v0.0.8c.
/// </remarks>
public sealed class VaultStatusService : IVaultStatusService
{
    private readonly ILogger<VaultStatusService> _logger;
    private VaultStatus _currentStatus = VaultStatus.Unknown;

    public VaultStatusService(ILogger<VaultStatusService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public event EventHandler<VaultStatus>? StatusChanged;

    /// <inheritdoc/>
    public Task<VaultStatus> GetVaultStatusAsync()
    {
        _logger.LogDebug("VaultStatusService.GetVaultStatusAsync called (stub)");
        
        // Stub: Return Empty to show the "No Key" status
        _currentStatus = VaultStatus.Empty;
        return Task.FromResult(_currentStatus);
    }

    /// <inheritdoc/>
    public Task StoreKeyAsync(string keyName, string keyValue)
    {
        _logger.LogDebug("VaultStatusService.StoreKeyAsync called for {KeyName} (stub)", keyName);
        
        _currentStatus = VaultStatus.Ready;
        StatusChanged?.Invoke(this, _currentStatus);
        
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<bool> HasKeyAsync(string keyName)
    {
        _logger.LogDebug("VaultStatusService.HasKeyAsync called for {KeyName} (stub)", keyName);
        return Task.FromResult(false);
    }
}
