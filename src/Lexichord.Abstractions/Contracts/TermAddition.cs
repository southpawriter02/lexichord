namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a project-specific term addition for the style checker.
/// </summary>
/// <remarks>
/// LOGIC: Allows users to add custom terminology rules at the project level
/// that supplement or override the global terminology database.
///
/// Use Cases:
/// - Add project-specific jargon that should be flagged
/// - Add domain-specific terms with custom recommendations
/// - Enforce project terminology standards beyond global rules
///
/// Design Decisions:
/// - Immutable record for thread safety
/// - Optional recommendation allows "flag only" patterns
/// - Default severity is Warning for discoverability without disruption
///
/// Version: v0.3.6a
/// </remarks>
/// <param name="Pattern">
/// The term pattern to match. Supports exact match or regex patterns.
/// </param>
/// <param name="Recommendation">
/// Optional suggested replacement text. If null, the term is flagged without
/// a specific recommendation.
/// </param>
/// <param name="Severity">
/// The severity level for violations of this term. Defaults to Warning.
/// </param>
public record TermAddition(
    string Pattern,
    string? Recommendation,
    ViolationSeverity Severity = ViolationSeverity.Warning);
