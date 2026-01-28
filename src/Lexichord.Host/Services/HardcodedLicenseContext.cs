using Lexichord.Abstractions.Contracts;

namespace Lexichord.Host.Services;

/// <summary>
/// Hardcoded license context for development and Core users.
/// </summary>
/// <remarks>
/// LOGIC: This stub implementation always returns Core tier.
/// In v0.0.4c, this will be replaced with:
/// - File-based license validation
/// - Server-based license validation
/// - Feature-specific overrides
///
/// For now, all modules marked as Core tier will load.
/// Higher tier modules will be skipped.
/// </remarks>
public sealed class HardcodedLicenseContext : ILicenseContext
{
    /// <inheritdoc/>
    public LicenseTier GetCurrentTier() => LicenseTier.Core;

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: All features enabled in development mode.
    /// v0.0.4c will implement proper feature gating.
    /// </remarks>
    public bool IsFeatureEnabled(string featureCode) => true;
}
