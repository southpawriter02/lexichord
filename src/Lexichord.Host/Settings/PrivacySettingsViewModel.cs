using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Lexichord.Host.Settings;

/// <summary>
/// ViewModel for the Privacy Settings page.
/// </summary>
/// <remarks>
/// LOGIC: Provides bindable properties for telemetry preferences.
/// When the crash reporting toggle changes, it immediately calls
/// ITelemetryService.Enable() or Disable() to apply the change.
///
/// Version: v0.1.7d
/// </remarks>
public partial class PrivacySettingsViewModel : ObservableObject
{
    private readonly ITelemetryService _telemetryService;
    private readonly ILogger<PrivacySettingsViewModel> _logger;

    [ObservableProperty]
    private bool _crashReportingEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacySettingsViewModel"/> class.
    /// </summary>
    /// <param name="telemetryService">The telemetry service for crash reporting.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public PrivacySettingsViewModel(
        ITelemetryService telemetryService,
        ILogger<PrivacySettingsViewModel> logger)
    {
        _telemetryService = telemetryService ?? throw new ArgumentNullException(nameof(telemetryService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize from current state
        _crashReportingEnabled = _telemetryService.IsEnabled;

        _logger.LogDebug(
            "PrivacySettingsViewModel initialized. CrashReporting: {Enabled}",
            _crashReportingEnabled);
    }

    /// <summary>
    /// Called when CrashReportingEnabled changes.
    /// </summary>
    partial void OnCrashReportingEnabledChanged(bool value)
    {
        try
        {
            if (value)
            {
                _telemetryService.Enable();
                _logger.LogInformation("Crash reporting enabled by user");
            }
            else
            {
                _telemetryService.Disable();
                _logger.LogInformation("Crash reporting disabled by user");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update crash reporting preference");
            // Revert UI state on failure
            _crashReportingEnabled = _telemetryService.IsEnabled;
            OnPropertyChanged(nameof(CrashReportingEnabled));
        }
    }

    /// <summary>
    /// Opens the privacy policy in the default browser.
    /// </summary>
    [RelayCommand]
    private void LearnMore()
    {
        try
        {
            const string privacyPolicyUrl = "https://github.com/lexichord/lexichord/blob/main/PRIVACY.md";

            Process.Start(new ProcessStartInfo
            {
                FileName = privacyPolicyUrl,
                UseShellExecute = true
            });

            _logger.LogDebug("Opened privacy policy URL");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open privacy policy URL");
        }
    }
}
