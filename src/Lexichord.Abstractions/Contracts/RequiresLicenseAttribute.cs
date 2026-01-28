namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Specifies the license tier required to load a module.
/// </summary>
/// <remarks>
/// LOGIC: Apply this attribute to IModule implementations to restrict loading.
/// ModuleLoader checks this attribute BEFORE instantiating the module.
///
/// Example:
/// [RequiresLicense(LicenseTier.WriterPro)]
/// public class TuningModule : IModule { }
///
/// v0.0.4b: Stub attribute for module loader.
/// v0.0.4c: Full implementation with licensing logic.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequiresLicenseAttribute : Attribute
{
    /// <summary>
    /// Gets the minimum license tier required.
    /// </summary>
    public LicenseTier Tier { get; }

    /// <summary>
    /// Gets the optional feature code required (for granular gating).
    /// </summary>
    public string? FeatureCode { get; }

    /// <summary>
    /// Creates a new RequiresLicenseAttribute with the specified tier.
    /// </summary>
    /// <param name="tier">The minimum license tier required to load the module.</param>
    /// <param name="featureCode">Optional feature code for granular gating.</param>
    public RequiresLicenseAttribute(LicenseTier tier, string? featureCode = null)
    {
        Tier = tier;
        FeatureCode = featureCode;
    }
}
