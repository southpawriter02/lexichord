namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Specifies the minimum license tier required to load a module or use a service.
/// </summary>
/// <remarks>
/// LOGIC: This attribute is checked by ModuleLoader during assembly discovery.
/// If the current license tier is lower than the required tier, the module is skipped.
///
/// Check Timing:
/// - Module Loading: Checked BEFORE module instantiation (prevents any code execution)
/// - Runtime Services: Checked by ILicenseService.Guard() at feature boundaries
///
/// Inheritance:
/// - Inherited = false: Each class must explicitly declare its requirement
/// - This prevents accidentally inheriting a lower tier from base class
///
/// AllowMultiple:
/// - AllowMultiple = false: A class has exactly one license requirement
/// - Use FeatureCode for additional granularity within a tier
///
/// Design Decisions:
/// - Class-level only (not method-level) to keep gating coarse-grained
/// - FeatureCode allows feature-level gating within a tier (future use)
/// </remarks>
/// <example>
/// <code>
/// // Module requiring Teams tier
/// [RequiresLicense(LicenseTier.Teams)]
/// public class ReleaseNotesAgentModule : IModule { }
///
/// // Service with feature code for granular gating
/// [RequiresLicense(LicenseTier.WriterPro, FeatureCode = "RAG-01")]
/// public class VectorMemoryService : IMemoryService { }
///
/// // Core tier (explicit, same as no attribute)
/// [RequiresLicense(LicenseTier.Core)]
/// public class BasicEditorModule : IModule { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequiresLicenseAttribute : Attribute
{
    /// <summary>
    /// Gets the minimum required license tier.
    /// </summary>
    /// <remarks>
    /// LOGIC: Comparison uses Tier &lt;= CurrentTier for access check.
    /// If this value is greater than the user's current tier, access is denied.
    /// </remarks>
    public LicenseTier Tier { get; }

    /// <summary>
    /// Gets or sets the feature code for granular license checks.
    /// </summary>
    /// <remarks>
    /// LOGIC: Feature codes allow fine-grained gating within a tier.
    /// A user might have WriterPro tier but not all WriterPro features enabled.
    ///
    /// Use Cases:
    /// - Beta features: Only enabled for beta testers within a tier
    /// - Add-on features: Purchased separately within a tier
    /// - Usage limits: Feature disabled after quota exceeded
    ///
    /// Format: "{Category}-{Number}" (e.g., "RAG-01", "AGT-05", "STY-03")
    /// </remarks>
    /// <example>
    /// <code>
    /// [RequiresLicense(LicenseTier.WriterPro, FeatureCode = "RAG-01")]
    /// public class VectorMemoryService { }
    ///
    /// // Check at runtime:
    /// if (!licenseContext.IsFeatureEnabled("RAG-01"))
    ///     throw new FeatureNotEnabledException("RAG-01");
    /// </code>
    /// </example>
    public string? FeatureCode { get; init; }

    /// <summary>
    /// Creates a new RequiresLicenseAttribute.
    /// </summary>
    /// <param name="tier">The minimum required license tier.</param>
    public RequiresLicenseAttribute(LicenseTier tier)
    {
        Tier = tier;
    }
}
