using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Settings;

/// <summary>
/// ViewModel for the Account settings page.
/// </summary>
/// <remarks>
/// LOGIC: Provides license status display and activation functionality.
/// Handles license key input validation, activation, and deactivation.
/// 
/// Version: v0.1.6c
/// </remarks>
public sealed partial class AccountSettingsViewModel : ObservableObject, IDisposable
{
    /// <summary>
    /// License key format: XXXX-XXXX-XXXX-XXXX where X is uppercase alphanumeric.
    /// </summary>
    [GeneratedRegex(@"^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex LicenseKeyFormat();

    private readonly ILicenseService _licenseService;
    private readonly ILogger<AccountSettingsViewModel> _logger;

    [ObservableProperty]
    private LicenseInfo _currentLicense = LicenseInfo.CoreDefault;

    [ObservableProperty]
    private string _licenseKeyInput = string.Empty;

    [ObservableProperty]
    private bool _isValidating;

    [ObservableProperty]
    private bool _isKeyFormatValid;

    [ObservableProperty]
    private string? _validationMessage;

    [ObservableProperty]
    private bool _isValidationSuccess;

    [ObservableProperty]
    private IReadOnlyList<FeatureAvailability> _features = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountSettingsViewModel"/> class.
    /// </summary>
    public AccountSettingsViewModel(
        ILicenseService licenseService,
        ILogger<AccountSettingsViewModel> logger)
    {
        _licenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        LoadCurrentLicense();

        // Subscribe to license changes
        _licenseService.LicenseChanged += OnLicenseChanged;

        _logger.LogDebug("AccountSettingsViewModel initialized");
    }

    /// <summary>
    /// Gets the display name for the current tier.
    /// </summary>
    public string TierDisplayName => CurrentLicense.Tier switch
    {
        LicenseTier.Core => "Core (Free)",
        LicenseTier.WriterPro => "WriterPro",
        LicenseTier.Teams => "Teams",
        LicenseTier.Enterprise => "Enterprise",
        _ => "Unknown"
    };

    /// <summary>
    /// Gets the tier badge color as a hex string.
    /// </summary>
    public string TierBadgeColor => CurrentLicense.Tier switch
    {
        LicenseTier.WriterPro => "#4CAF50",   // Green
        LicenseTier.Teams => "#FF9800",       // Orange
        LicenseTier.Enterprise => "#2196F3", // Blue
        _ => "#9E9E9E"                        // Gray
    };

    /// <summary>
    /// Gets whether the current license is about to expire (within 30 days).
    /// </summary>
    public bool IsExpirationWarning =>
        CurrentLicense.DaysUntilExpiration is > 0 and <= 30;

    /// <summary>
    /// Gets the expiration date formatted for display.
    /// </summary>
    public string? ExpirationDisplayText =>
        CurrentLicense.ExpirationDate?.ToString("'Expires:' MMMM d, yyyy");

    /// <summary>
    /// Gets whether the validation message is an error (not success).
    /// </summary>
    public bool IsValidationError =>
        !string.IsNullOrEmpty(ValidationMessage) && !IsValidationSuccess;

    /// <summary>
    /// Called when the license key input changes.
    /// </summary>
    partial void OnLicenseKeyInputChanged(string value)
    {
        // Clear validation message when input changes
        ValidationMessage = null;
        IsValidationSuccess = false;

        // Validate format
        IsKeyFormatValid = !string.IsNullOrWhiteSpace(value) &&
            LicenseKeyFormat().IsMatch(value.Trim());
    }

    /// <summary>
    /// Activates the entered license key.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanActivate))]
    private async Task ActivateAsync()
    {
        var key = LicenseKeyInput.Trim().ToUpperInvariant();

        _logger.LogInformation("Attempting to activate license");

        IsValidating = true;
        ValidationMessage = null;

        try
        {
            // First validate
            var validationResult = await _licenseService.ValidateLicenseKeyAsync(key);

            if (!validationResult.IsValid)
            {
                ValidationMessage = validationResult.ErrorMessage ?? "License key is invalid.";
                IsValidationSuccess = false;
                _logger.LogWarning("License validation failed: {Error}", ValidationMessage);
                return;
            }

            // Then activate
            var activated = await _licenseService.ActivateLicenseAsync(key);

            if (activated)
            {
                // Clear input first (this triggers OnLicenseKeyInputChanged which resets IsValidationSuccess)
                LicenseKeyInput = string.Empty;
                // Then set success state
                ValidationMessage = $"License activated! Welcome to {TierDisplayName}.";
                IsValidationSuccess = true;
                _logger.LogInformation("License activated successfully");
            }
            else
            {
                ValidationMessage = "Failed to activate license. Please try again.";
                IsValidationSuccess = false;
                _logger.LogWarning("License activation failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during license activation");
            ValidationMessage = "An error occurred. Please try again later.";
            IsValidationSuccess = false;
        }
        finally
        {
            IsValidating = false;
        }
    }

    private bool CanActivate() => IsKeyFormatValid && !IsValidating;

    /// <summary>
    /// Deactivates the current license.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeactivate))]
    private async Task DeactivateAsync()
    {
        _logger.LogInformation("Deactivating license");

        try
        {
            var result = await _licenseService.DeactivateLicenseAsync();

            if (result)
            {
                ValidationMessage = "License deactivated. You are now on the Core (Free) tier.";
                IsValidationSuccess = true;
                _logger.LogInformation("License deactivated successfully");
            }
            else
            {
                ValidationMessage = "Failed to deactivate license.";
                IsValidationSuccess = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during license deactivation");
            ValidationMessage = "An error occurred during deactivation.";
            IsValidationSuccess = false;
        }
    }

    private bool CanDeactivate() => CurrentLicense.IsActivated && !IsValidating;

    /// <summary>
    /// Loads the current license information.
    /// </summary>
    private void LoadCurrentLicense()
    {
        CurrentLicense = _licenseService.GetCurrentLicense();
        Features = _licenseService.GetFeatureAvailability(CurrentLicense.Tier);
        OnPropertyChanged(nameof(TierDisplayName));
        OnPropertyChanged(nameof(TierBadgeColor));
        OnPropertyChanged(nameof(IsExpirationWarning));
    }

    /// <summary>
    /// Handles license state changes from the service.
    /// </summary>
    private void OnLicenseChanged(object? sender, LicenseChangedEventArgs e)
    {
        LoadCurrentLicense();
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _licenseService.LicenseChanged -= OnLicenseChanged;
    }
}
