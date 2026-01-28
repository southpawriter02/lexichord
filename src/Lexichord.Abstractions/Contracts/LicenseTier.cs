namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the license tiers for Lexichord features.
/// </summary>
/// <remarks>
/// LOGIC: License tiers are hierarchical. Higher tiers include all features of lower tiers.
/// The numeric values enable comparison: if RequiredTier &lt;= CurrentTier, access is granted.
///
/// Tier Hierarchy:
/// - Core (0): Free/Community edition with basic features
/// - WriterPro (1): Individual professional writer subscription
/// - Teams (2): Business/Agency team license
/// - Enterprise (3): Corporate with compliance features
///
/// Design Decisions:
/// - Enum values are explicitly numbered for stable serialization
/// - Values are sequential to allow simple greater-than/less-than checks
/// - Core = 0 ensures default(LicenseTier) is the lowest tier
/// </remarks>
/// <example>
/// <code>
/// // Tier comparison for access check
/// var required = LicenseTier.Teams;
/// var current = LicenseTier.WriterPro;
/// var hasAccess = required &lt;= current; // false, Teams > WriterPro
///
/// // Using with attribute
/// [RequiresLicense(LicenseTier.Teams)]
/// public class CollaborationModule : IModule { }
/// </code>
/// </example>
public enum LicenseTier
{
    /// <summary>
    /// Free/Community edition with basic features.
    /// </summary>
    /// <remarks>
    /// Features: Basic editor, file operations, minimal styling.
    /// All users start at this tier. Modules without [RequiresLicense] default to Core.
    /// </remarks>
    Core = 0,

    /// <summary>
    /// Individual professional writer license.
    /// </summary>
    /// <remarks>
    /// Features: Full style engine, readability metrics, basic AI assistance,
    /// voice analysis, personal style guides.
    /// </remarks>
    WriterPro = 1,

    /// <summary>
    /// Business/Agency team license.
    /// </summary>
    /// <remarks>
    /// Features: All WriterPro features plus collaboration, shared style guides,
    /// team management, release notes agent, multi-user editing.
    /// </remarks>
    Teams = 2,

    /// <summary>
    /// Corporate license with compliance features.
    /// </summary>
    /// <remarks>
    /// Features: All Teams features plus SSO/SAML, audit logs, data residency options,
    /// compliance reporting, priority support, custom deployment.
    /// </remarks>
    Enterprise = 3
}
