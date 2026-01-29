using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Settings;

/// <summary>
/// ViewModel for the Updates Settings page.
/// </summary>
/// <remarks>
/// LOGIC: Provides bindable properties for update channel selection,
/// version information display, and manual update checking.
///
/// Version: v0.1.6d
/// </remarks>
public partial class UpdatesSettingsViewModel : ObservableObject
{
    private readonly IUpdateService _updateService;
    private readonly ILogger<UpdatesSettingsViewModel> _logger;

    [ObservableProperty]
    private bool _isStable;

    [ObservableProperty]
    private bool _isInsider;

    [ObservableProperty]
    private VersionInfo _versionInfo = null!;

    [ObservableProperty]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    private string? _updateStatus;

    [ObservableProperty]
    private DateTime? _lastCheckTime;

    /// <summary>
    /// Initializes a new instance of <see cref="UpdatesSettingsViewModel"/>.
    /// </summary>
    /// <param name="updateService">The update service for channel management.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public UpdatesSettingsViewModel(
        IUpdateService updateService,
        ILogger<UpdatesSettingsViewModel> logger)
    {
        _updateService = updateService ?? throw new ArgumentNullException(nameof(updateService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize state from service
        VersionInfo = _updateService.GetVersionInfo();
        LastCheckTime = _updateService.LastCheckTime;
        InitializeChannelSelection(_updateService.CurrentChannel);

        _logger.LogDebug(
            "UpdatesSettingsViewModel initialized: Version={Version}, Channel={Channel}",
            VersionInfo.Version, _updateService.CurrentChannel);
    }

    /// <summary>
    /// Gets the currently selected update channel.
    /// </summary>
    public UpdateChannel SelectedChannel => IsInsider
        ? UpdateChannel.Insider
        : UpdateChannel.Stable;

    /// <summary>
    /// Gets a description of the currently selected channel.
    /// </summary>
    public string ChannelDescription => SelectedChannel switch
    {
        UpdateChannel.Insider => "Get early access to new features. Insider builds may contain bugs and are not recommended for production use.",
        _ => "Receive stable, well-tested updates. Recommended for most users."
    };

    /// <summary>
    /// Gets a formatted display of the last update check time.
    /// </summary>
    public string LastCheckDisplay => LastCheckTime switch
    {
        null => "Never checked",
        DateTime time when DateTime.UtcNow - time < TimeSpan.FromMinutes(1) => "Just now",
        DateTime time when DateTime.UtcNow - time < TimeSpan.FromHours(1) =>
            $"{(int)(DateTime.UtcNow - time).TotalMinutes} minutes ago",
        DateTime time when DateTime.UtcNow - time < TimeSpan.FromDays(1) =>
            $"{(int)(DateTime.UtcNow - time).TotalHours} hours ago",
        DateTime time => time.ToLocalTime().ToString("MMMM d, yyyy")
    };

    /// <summary>
    /// Command to select the Stable channel.
    /// </summary>
    [RelayCommand]
    private async Task SelectStableAsync()
    {
        if (IsStable) return;

        IsStable = true;
        IsInsider = false;

        await SetChannelAsync(UpdateChannel.Stable);
        OnPropertyChanged(nameof(ChannelDescription));
    }

    /// <summary>
    /// Command to select the Insider channel.
    /// </summary>
    [RelayCommand]
    private async Task SelectInsiderAsync()
    {
        if (IsInsider) return;

        IsInsider = true;
        IsStable = false;

        await SetChannelAsync(UpdateChannel.Insider);
        OnPropertyChanged(nameof(ChannelDescription));
    }

    /// <summary>
    /// Command to check for updates manually.
    /// </summary>
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
    {
        if (IsCheckingForUpdates) return;

        try
        {
            IsCheckingForUpdates = true;
            UpdateStatus = "Checking for updates...";
            _logger.LogDebug("Manual update check initiated");

            var update = await _updateService.CheckForUpdatesAsync(cancellationToken);

            LastCheckTime = _updateService.LastCheckTime;
            OnPropertyChanged(nameof(LastCheckDisplay));

            UpdateStatus = update is not null
                ? $"Update available: v{update.Version}"
                : "You're up to date!";
        }
        catch (OperationCanceledException)
        {
            UpdateStatus = "Check cancelled";
            _logger.LogDebug("Update check cancelled by user");
        }
        catch (Exception ex)
        {
            UpdateStatus = "Check failed. Please try again.";
            _logger.LogError(ex, "Update check failed");
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    private void InitializeChannelSelection(UpdateChannel channel)
    {
        IsStable = channel == UpdateChannel.Stable;
        IsInsider = channel == UpdateChannel.Insider;
    }

    private async Task SetChannelAsync(UpdateChannel channel)
    {
        try
        {
            await _updateService.SetChannelAsync(channel);
            _logger.LogDebug("Channel selection changed to: {Channel}", channel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set channel: {Channel}", channel);
            // Revert selection on failure
            InitializeChannelSelection(_updateService.CurrentChannel);
        }
    }
}
