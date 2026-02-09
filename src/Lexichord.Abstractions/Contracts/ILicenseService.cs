namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides license management operations including validation, activation, and deactivation.
/// </summary>
/// <remarks>
/// LOGIC: This interface extends ILicenseContext with mutation operations for license management.
/// It provides the full license lifecycle: validate → activate → use → deactivate.
/// 
/// Thread Safety:
/// - Implementations must be thread-safe
/// - License state changes are synchronized and raise LicenseChanged event
/// 
/// Dependency:
/// - Registered in DI as Singleton (license state is application-wide)
/// - Injected into AccountSettingsViewModel for license management UI
/// 
/// Version: v0.1.6c
/// </remarks>
public interface ILicenseService : ILicenseContext
{
    /// <summary>
    /// Validates a license key without activating it.
    /// </summary>
    /// <param name="licenseKey">The license key to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Validation result with tier, licensee, and expiration if valid.</returns>
    /// <remarks>
    /// LOGIC: Performs format validation, checksum check, and server-side validation.
    /// Does not modify the current license state - use ActivateLicenseAsync to apply.
    /// </remarks>
    Task<LicenseValidationResult> ValidateLicenseKeyAsync(
        string licenseKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates and activates a license key.
    /// </summary>
    /// <param name="licenseKey">The license key to activate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if activation succeeded, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Validates the key, stores it securely, updates current tier,
    /// and raises LicenseChanged and LicenseActivatedEvent.
    /// </remarks>
    Task<bool> ActivateLicenseAsync(
        string licenseKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates the current license.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deactivation succeeded, false if no license was active.</returns>
    /// <remarks>
    /// LOGIC: Removes stored license key, reverts to Core tier,
    /// and raises LicenseChanged and LicenseDeactivatedEvent.
    /// </remarks>
    Task<bool> DeactivateLicenseAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current license information.
    /// </summary>
    /// <returns>Current license state including tier, licensee, and expiration.</returns>
    LicenseInfo GetCurrentLicense();

    /// <summary>
    /// Gets the availability of all features for a given tier.
    /// </summary>
    /// <param name="tier">The tier to check feature availability for.</param>
    /// <returns>List of features with their availability status.</returns>
    IReadOnlyList<FeatureAvailability> GetFeatureAvailability(LicenseTier tier);

    /// <summary>
    /// Raised when the license state changes (activation, deactivation, expiration).
    /// </summary>
    new event EventHandler<LicenseChangedEventArgs>? LicenseChanged;
}
