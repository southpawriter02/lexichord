using Lexichord.Abstractions.Contracts;

using Microsoft.Extensions.Logging;

namespace Lexichord.Host.Services;

/// <summary>
/// Stub implementation of ILicenseContext that returns Core tier.
/// </summary>
/// <remarks>
/// LOGIC: This is a placeholder implementation for v0.0.4.
/// It provides the API surface without real license validation.
///
/// Purpose:
/// - Allows development and testing without license files
/// - Establishes the API contract for v1.x implementation
/// - Demonstrates the license check flow in ModuleLoader
///
/// Future v1.x Implementation Will:
/// - Read encrypted license files
/// - Validate signatures
/// - Check expiration dates
/// - Contact licensing server for activation
/// - Support BYOK (Bring Your Own Key) with token counting
///
/// Security Note:
/// - This stub provides NO actual security
/// - Users can trivially bypass by modifying code or removing attributes
/// - Real security requires server-side validation and obfuscation
/// </remarks>
public sealed class HardcodedLicenseContext : ILicenseContext
{
    private readonly ILogger<HardcodedLicenseContext>? _logger;

    /// <summary>
    /// Creates a new HardcodedLicenseContext.
    /// </summary>
    /// <param name="logger">Optional logger for license check events.</param>
    public HardcodedLicenseContext(ILogger<HardcodedLicenseContext>? logger = null)
    {
        _logger = logger;
        _logger?.LogInformation(
            "Using hardcoded license context (development mode). Tier: {Tier}",
            LicenseTier.Core);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub always returns Core tier.
    /// To test higher tiers during development, modify this return value.
    /// </remarks>
    public LicenseTier GetCurrentTier()
    {
        _logger?.LogDebug("License tier check: returning {Tier}", LicenseTier.Core);
        return LicenseTier.Core;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub returns true for all features.
    /// This allows testing any feature without feature gating.
    /// </remarks>
    public bool IsFeatureEnabled(string featureCode)
    {
        _logger?.LogDebug(
            "Feature check for {FeatureCode}: returning enabled (stub)",
            featureCode);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub returns null (perpetual license).
    /// No expiration warnings in development mode.
    /// </remarks>
    public DateTime? GetExpirationDate() => null;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Returns development indicator for UI display.
    /// </remarks>
    public string? GetLicenseeName() => "Development License";

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Stub never fires this event since the hardcoded license tier
    /// never changes. Required by the ILicenseContext interface (v0.6.6c).
    /// </remarks>
#pragma warning disable CS0067 // Event is never used (stub implementation)
    public event EventHandler<LicenseChangedEventArgs>? LicenseChanged;
#pragma warning restore CS0067
}
