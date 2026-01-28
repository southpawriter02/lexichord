namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the license tiers for Lexichord.
/// </summary>
/// <remarks>
/// LOGIC: License tiers control which modules are available.
/// Lower tiers are subsets of higher tiers:
/// - Core: Free features (always available)
/// - WriterPro: Individual professional features
/// - Teams: Collaboration features
/// - Enterprise: Advanced administration and integrations
///
/// v0.0.4b: Stub implementation for module loader.
/// v0.0.4c: Full implementation with licensing logic.
/// </remarks>
public enum LicenseTier
{
    /// <summary>
    /// Core tier. Always available. Free features.
    /// </summary>
    Core = 0,

    /// <summary>
    /// Writer Pro tier. Individual professional features.
    /// </summary>
    WriterPro = 1,

    /// <summary>
    /// Teams tier. Collaboration features.
    /// </summary>
    Teams = 2,

    /// <summary>
    /// Enterprise tier. Advanced administration.
    /// </summary>
    Enterprise = 3
}
