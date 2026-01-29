using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Modules.StatusBar.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.ViewModels;

/// <summary>
/// ViewModel for the StatusBar view.
/// </summary>
/// <remarks>
/// LOGIC: The StatusBarViewModel manages the state for all status indicators:
/// - Database health (green/yellow/red/gray)
/// - Vault status (ready/empty/error/unknown)
/// - Application uptime
///
/// It subscribes to events from HeartbeatService and VaultStatusService
/// to update indicators in real-time.
/// </remarks>
public partial class StatusBarViewModel : ObservableObject
{
    private readonly IHealthRepository _healthRepository;
    private readonly IHeartbeatService _heartbeatService;
    private readonly IVaultStatusService _vaultStatusService;
    private readonly ILogger<StatusBarViewModel> _logger;
    private readonly DateTime _startTime = DateTime.UtcNow;

    public StatusBarViewModel(
        IHealthRepository healthRepository,
        IHeartbeatService heartbeatService,
        IVaultStatusService vaultStatusService,
        ILogger<StatusBarViewModel> logger)
    {
        _healthRepository = healthRepository;
        _heartbeatService = heartbeatService;
        _vaultStatusService = vaultStatusService;
        _logger = logger;

        // Subscribe to status change events
        _heartbeatService.HealthChanged += OnHealthChanged;
        _vaultStatusService.StatusChanged += OnVaultStatusChanged;

        // Initialize status
        _ = InitializeAsync();
    }

    #region Database Status Properties

    [ObservableProperty]
    private bool _isDatabaseHealthy;

    [ObservableProperty]
    private bool _isDatabaseWarning;

    [ObservableProperty]
    private bool _isDatabaseError;

    [ObservableProperty]
    private bool _isDatabaseUnknown = true;

    [ObservableProperty]
    private string _databaseStatusText = "Checking...";

    [ObservableProperty]
    private string _databaseTooltip = "Checking database connection...";

    #endregion

    #region Vault Status Properties

    [ObservableProperty]
    private bool _isVaultReady;

    [ObservableProperty]
    private bool _isVaultEmpty;

    [ObservableProperty]
    private bool _isVaultError;

    [ObservableProperty]
    private bool _isVaultUnknown = true;

    [ObservableProperty]
    private string _vaultStatusText = "Checking...";

    [ObservableProperty]
    private string _vaultTooltip = "Checking vault status...";

    #endregion

    #region Uptime Properties

    [ObservableProperty]
    private string _uptimeText = "Uptime: 0:00:00";

    #endregion

    #region Service Access (for code-behind)

    /// <summary>
    /// Gets the vault status service for dialog interaction.
    /// </summary>
    public IVaultStatusService VaultStatusService => _vaultStatusService;

    /// <summary>
    /// Gets the logger for dialog ViewModels.
    /// </summary>
    public ILogger<StatusBarViewModel> Logger => _logger;

    #endregion

    #region Commands

    [RelayCommand]
    private async Task RefreshVaultStatusAsync()
    {
        await UpdateVaultStatusAsync();
    }

    #endregion

    #region Private Methods

    private async Task InitializeAsync()
    {
        try
        {
            await UpdateDatabaseStatusAsync();
            await UpdateVaultStatusAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize StatusBarViewModel");
        }
    }

    private async Task UpdateDatabaseStatusAsync()
    {
        try
        {
            var isHealthy = await _healthRepository.IsHealthyAsync();
            UpdateDatabaseIndicator(isHealthy);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Database health check failed");
            SetDatabaseStatus(healthy: false, warning: false, error: true, unknown: false,
                "Error", $"Database error: {ex.Message}");
        }
    }

    private async Task UpdateVaultStatusAsync()
    {
        try
        {
            var status = await _vaultStatusService.GetVaultStatusAsync();
            UpdateVaultIndicator(status);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vault status check failed");
            SetVaultStatus(ready: false, empty: false, error: true, unknown: false,
                "Error", $"Vault error: {ex.Message}");
        }
    }

    private void UpdateDatabaseIndicator(bool isHealthy)
    {
        if (isHealthy)
        {
            SetDatabaseStatus(healthy: true, warning: false, error: false, unknown: false,
                "DB OK", "Database connection is healthy");
        }
        else
        {
            SetDatabaseStatus(healthy: false, warning: true, error: false, unknown: false,
                "DB Warning", "Database connection may be degraded");
        }
    }

    private void UpdateVaultIndicator(VaultStatus status)
    {
        switch (status)
        {
            case VaultStatus.Ready:
                SetVaultStatus(ready: true, empty: false, error: false, unknown: false,
                    "Vault OK", "Secure vault is ready with keys loaded");
                break;

            case VaultStatus.Empty:
                SetVaultStatus(ready: false, empty: true, error: false, unknown: false,
                    "No Key", "Click to configure API key");
                break;

            case VaultStatus.Error:
                SetVaultStatus(ready: false, empty: false, error: true, unknown: false,
                    "Vault Error", "Secure vault encountered an error");
                break;

            default:
                SetVaultStatus(ready: false, empty: false, error: false, unknown: true,
                    "Checking...", "Checking vault status...");
                break;
        }
    }

    private void SetDatabaseStatus(
        bool healthy, bool warning, bool error, bool unknown,
        string text, string tooltip)
    {
        IsDatabaseHealthy = healthy;
        IsDatabaseWarning = warning;
        IsDatabaseError = error;
        IsDatabaseUnknown = unknown;
        DatabaseStatusText = text;
        DatabaseTooltip = tooltip;
    }

    private void SetVaultStatus(
        bool ready, bool empty, bool error, bool unknown,
        string text, string tooltip)
    {
        IsVaultReady = ready;
        IsVaultEmpty = empty;
        IsVaultError = error;
        IsVaultUnknown = unknown;
        VaultStatusText = text;
        VaultTooltip = tooltip;
    }

    private void OnHealthChanged(object? sender, bool isHealthy)
    {
        UpdateDatabaseIndicator(isHealthy);
    }

    private void OnVaultStatusChanged(object? sender, VaultStatus status)
    {
        UpdateVaultIndicator(status);
    }

    #endregion
}
