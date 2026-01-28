namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Provides access to the current license context for tier and feature checks.
/// </summary>
/// <remarks>
/// LOGIC: This interface abstracts the license validation mechanism.
/// The v0.0.4 implementation is a stub returning Core tier.
/// Future v1.x implementations will provide:
/// - Encrypted license file validation
/// - Online license activation
/// - Feature-level gating
/// - Usage metering (for BYOK token counting)
/// - License expiration handling
///
/// Thread Safety:
/// - Implementations must be thread-safe
/// - License state may change during runtime (upgrade, expiration)
///
/// Dependency:
/// - Registered in DI as Singleton (license context is application-wide)
/// - Injected into ModuleLoader and services that need tier checks
/// </remarks>
/// <example>
/// <code>
/// public class MyService(ILicenseContext license)
/// {
///     public void DoSomethingPremium()
///     {
///         if (license.GetCurrentTier() &lt; LicenseTier.WriterPro)
///             throw new LicenseRequiredException(LicenseTier.WriterPro);
///
///         if (!license.IsFeatureEnabled("MY-FEATURE"))
///             throw new FeatureNotEnabledException("MY-FEATURE");
///
///         // Proceed with premium functionality
///     }
/// }
/// </code>
/// </example>
public interface ILicenseContext
{
    /// <summary>
    /// Gets the current license tier.
    /// </summary>
    /// <returns>The active license tier.</returns>
    /// <remarks>
    /// LOGIC: Returns the highest tier the current user is authorized for.
    /// In the stub implementation, always returns Core.
    /// </remarks>
    LicenseTier GetCurrentTier();

    /// <summary>
    /// Checks if a specific feature is enabled.
    /// </summary>
    /// <param name="featureCode">The feature code (e.g., "RAG-01", "AGT-05").</param>
    /// <returns>True if the feature is enabled, false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Feature codes allow granular gating beyond tiers.
    /// A user at WriterPro tier might not have all WriterPro features enabled if:
    /// - Feature is in beta (opt-in)
    /// - Feature is an add-on (separate purchase)
    /// - Feature has usage limits (quota exceeded)
    ///
    /// In the stub implementation, always returns true (all features enabled).
    /// </remarks>
    bool IsFeatureEnabled(string featureCode);

    /// <summary>
    /// Gets the license expiration date, if applicable.
    /// </summary>
    /// <returns>The expiration date, or null for perpetual licenses.</returns>
    /// <remarks>
    /// LOGIC: Subscription licenses have expiration dates.
    /// When expired, tier typically reverts to Core.
    /// UI should warn users before expiration.
    ///
    /// In the stub implementation, returns null (perpetual).
    /// </remarks>
    DateTime? GetExpirationDate();

    /// <summary>
    /// Gets the licensed user or organization name.
    /// </summary>
    /// <returns>The licensee name, or null if not licensed.</returns>
    /// <remarks>
    /// LOGIC: Used for display in UI ("Licensed to: Company Name").
    /// Also useful for support/diagnostics.
    ///
    /// In the stub implementation, returns "Development License".
    /// </remarks>
    string? GetLicenseeName();
}
