namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Describes the availability of a feature based on license tier.
/// </summary>
/// <remarks>
/// LOGIC: Used to build the feature availability matrix in the Account settings page.
/// Each feature has a required tier; features are available if current tier >= required tier.
/// 
/// Version: v0.1.6c
/// </remarks>
/// <param name="FeatureId">Unique identifier for the feature (e.g., "ai-suggestions").</param>
/// <param name="FeatureName">Display name for the feature.</param>
/// <param name="Description">Brief description of what the feature provides.</param>
/// <param name="IsAvailable">Whether the feature is available at the current tier.</param>
/// <param name="RequiredTier">The minimum tier required to access the feature.</param>
public record FeatureAvailability(
    string FeatureId,
    string FeatureName,
    string Description,
    bool IsAvailable,
    LicenseTier RequiredTier
)
{
    /// <summary>
    /// Gets the icon to display based on availability status.
    /// </summary>
    public string AvailabilityIcon => IsAvailable ? "✅" : "🔒";
}
