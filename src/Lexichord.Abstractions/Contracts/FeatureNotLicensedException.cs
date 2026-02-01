// =============================================================================
// File: FeatureNotLicensedException.cs
// Project: Lexichord.Abstractions
// Description: Exception thrown when attempting to use a feature not licensed
//              in the current license tier.
// =============================================================================
// LOGIC: Custom exception for feature access control and license gating.
//   - Thrown when current tier does not satisfy feature requirements.
//   - Carries RequiredTier for UI error messages and fallback logic.
//   - Enables fine-grained feature gating beyond simple tier checks.
// =============================================================================

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Exception thrown when attempting to use a feature not licensed in the current tier.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="FeatureNotLicensedException"/> is thrown when a feature requires
/// a higher license tier or additional licensing than the current user possesses.
/// This enables graceful degradation and clear user communication about licensing constraints.
/// </para>
/// <para>
/// <b>Usage Pattern:</b>
/// </para>
/// <code>
/// if (license.GetCurrentTier() &lt; LicenseTier.WriterPro)
/// {
///     throw new FeatureNotLicensedException(
///         "Document indexing and semantic search require WriterPro license.",
///         LicenseTier.WriterPro);
/// }
/// </code>
/// <para>
/// <b>Exception Hierarchy:</b>
/// <list type="bullet">
///   <item><see cref="FeatureNotLicensedException"/> - Feature not licensed</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.4d as part of the Embedding Pipeline feature.
/// </para>
/// </remarks>
public class FeatureNotLicensedException : Exception
{
    /// <summary>
    /// Initializes a new instance with a message and required tier.
    /// </summary>
    /// <param name="message">The error message explaining which feature is not licensed.</param>
    /// <param name="requiredTier">The minimum license tier required to use the feature.</param>
    /// <remarks>
    /// LOGIC: Standard constructor for license tier requirements. The <paramref name="requiredTier"/>
    /// is captured for UI error messages suggesting an upgrade path.
    /// </remarks>
    public FeatureNotLicensedException(string message, LicenseTier requiredTier)
        : base(message)
    {
        RequiredTier = requiredTier;
    }

    /// <summary>
    /// Initializes a new instance with a message, required tier, and inner exception.
    /// </summary>
    /// <param name="message">The error message explaining which feature is not licensed.</param>
    /// <param name="requiredTier">The minimum license tier required to use the feature.</param>
    /// <param name="innerException">The underlying exception.</param>
    /// <remarks>
    /// LOGIC: Constructor for wrapping license validation errors. Captures the complete
    /// context of why the feature is not available.
    /// </remarks>
    public FeatureNotLicensedException(string message, LicenseTier requiredTier, Exception innerException)
        : base(message, innerException)
    {
        RequiredTier = requiredTier;
    }

    /// <summary>
    /// Gets the minimum license tier required to use the feature.
    /// </summary>
    /// <remarks>
    /// LOGIC: Enables UI to display upgrade prompts suggesting the specific tier
    /// needed to access the feature. For example: "Upgrade to WriterPro to use this feature."
    /// </remarks>
    /// <value>
    /// The <see cref="LicenseTier"/> required for the feature.
    /// </value>
    public LicenseTier RequiredTier { get; }
}
