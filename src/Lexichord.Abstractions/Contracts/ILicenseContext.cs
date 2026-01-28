namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides access to the current license context.
/// </summary>
/// <remarks>
/// LOGIC: The license context determines which modules are authorized to load.
/// It provides:
/// - Current tier: What license level the user has
/// - Feature checks: Granular feature flag checks
///
/// v0.0.4b: Stub interface for module loader.
/// v0.0.4c: Full implementation with licensing logic.
/// </remarks>
public interface ILicenseContext
{
    /// <summary>
    /// Gets the current license tier.
    /// </summary>
    /// <returns>The active license tier.</returns>
    /// <remarks>
    /// LOGIC: Returns the highest tier the user is licensed for.
    /// Used by ModuleLoader to filter modules at startup.
    /// </remarks>
    LicenseTier GetCurrentTier();

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="featureCode">The feature code to check.</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Enables granular feature gating beyond tier checks.
    /// A module might require both a tier AND a specific feature code.
    /// </remarks>
    bool IsFeatureEnabled(string featureCode);
}
