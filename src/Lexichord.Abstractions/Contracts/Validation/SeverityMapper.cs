// =============================================================================
// File: SeverityMapper.cs
// Project: Lexichord.Abstractions
// Description: Maps source-specific severity levels to UnifiedSeverity.
// =============================================================================
// LOGIC: Different validation sources use different severity enums with different
//   numeric orderings. This mapper normalizes them all to UnifiedSeverity.
//
//   Source Severity Mappings:
//   - ViolationSeverity (Style Linter): Error=0, Warning=1, Info=2, Hint=3
//     → Direct mapping to UnifiedSeverity (same values)
//
//   - ValidationSeverity (CKVS): Info=0, Warning=1, Error=2
//     → Inverted mapping: Info→Info(2), Warning→Warning(1), Error→Error(0)
//
//   - DeviationPriority (Tuning Agent): Low=0, Normal=1, High=2, Critical=3
//     → Reverse mapping: Critical→Error, High→Warning, Normal→Info, Low→Hint
//
// v0.7.5e: Unified Issue Model (Unified Validation Feature)
// =============================================================================

using Lexichord.Abstractions.Contracts.Agents;
using Lexichord.Abstractions.Contracts.Knowledge.Validation;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

namespace Lexichord.Abstractions.Contracts.Validation;

/// <summary>
/// Static helper class for mapping source-specific severity levels to <see cref="UnifiedSeverity"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Different validation sources use different severity enums with different
/// numeric orderings. This class provides conversion methods for each source type:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="ViolationSeverity"/> (Style Linter): Error=0, Warning=1, Info=2, Hint=3
///       — Direct mapping (same values as UnifiedSeverity)
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="ValidationSeverity"/> (CKVS): Info=0, Warning=1, Error=2
///       — Inverted mapping required
///     </description>
///   </item>
///   <item>
///     <description>
///       <see cref="DeviationPriority"/> (Tuning Agent): Low=0, Normal=1, High=2, Critical=3
///       — Reverse mapping (priority inversely related to severity number)
///     </description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5e as part of the Unified Issue Model feature.
/// </para>
/// </remarks>
public static class SeverityMapper
{
    /// <summary>
    /// Maps a <see cref="ViolationSeverity"/> from the Style Linter to <see cref="UnifiedSeverity"/>.
    /// </summary>
    /// <param name="severity">The style linter violation severity.</param>
    /// <returns>The corresponding <see cref="UnifiedSeverity"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Direct 1:1 mapping since both enums share the same value ordering:
    /// <list type="bullet">
    ///   <item><description>Error (0) → Error (0)</description></item>
    ///   <item><description>Warning (1) → Warning (1)</description></item>
    ///   <item><description>Info (2) → Info (2)</description></item>
    ///   <item><description>Hint (3) → Hint (3)</description></item>
    /// </list>
    /// </remarks>
    public static UnifiedSeverity FromViolationSeverity(ViolationSeverity severity)
    {
        // LOGIC: ViolationSeverity and UnifiedSeverity share the same numeric values
        // Error=0, Warning=1, Info=2, Hint=3
        return severity switch
        {
            ViolationSeverity.Error => UnifiedSeverity.Error,
            ViolationSeverity.Warning => UnifiedSeverity.Warning,
            ViolationSeverity.Info => UnifiedSeverity.Info,
            ViolationSeverity.Hint => UnifiedSeverity.Hint,
            _ => UnifiedSeverity.Info // Default fallback for unknown values
        };
    }

    /// <summary>
    /// Maps a <see cref="ValidationSeverity"/> from CKVS Validation to <see cref="UnifiedSeverity"/>.
    /// </summary>
    /// <param name="severity">The CKVS validation severity.</param>
    /// <returns>The corresponding <see cref="UnifiedSeverity"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Inverted mapping since ValidationSeverity uses different ordering:
    /// <list type="bullet">
    ///   <item><description>Info (0) → Info (2)</description></item>
    ///   <item><description>Warning (1) → Warning (1)</description></item>
    ///   <item><description>Error (2) → Error (0)</description></item>
    /// </list>
    /// Note: ValidationSeverity does not have a Hint level.
    /// </remarks>
    public static UnifiedSeverity FromValidationSeverity(ValidationSeverity severity)
    {
        // LOGIC: ValidationSeverity uses inverted ordering: Info=0, Warning=1, Error=2
        // UnifiedSeverity uses: Error=0, Warning=1, Info=2, Hint=3
        return severity switch
        {
            ValidationSeverity.Error => UnifiedSeverity.Error,
            ValidationSeverity.Warning => UnifiedSeverity.Warning,
            ValidationSeverity.Info => UnifiedSeverity.Info,
            _ => UnifiedSeverity.Info // Default fallback for unknown values
        };
    }

    /// <summary>
    /// Maps a <see cref="DeviationPriority"/> from the Tuning Agent to <see cref="UnifiedSeverity"/>.
    /// </summary>
    /// <param name="priority">The deviation priority.</param>
    /// <returns>The corresponding <see cref="UnifiedSeverity"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Reverse mapping since priority is inversely related to severity number:
    /// <list type="bullet">
    ///   <item><description>Critical (3) → Error (0)</description></item>
    ///   <item><description>High (2) → Warning (1)</description></item>
    ///   <item><description>Normal (1) → Info (2)</description></item>
    ///   <item><description>Low (0) → Hint (3)</description></item>
    /// </list>
    /// This matches the conceptual equivalence: Critical priority = Error severity.
    /// </remarks>
    public static UnifiedSeverity FromDeviationPriority(DeviationPriority priority)
    {
        // LOGIC: DeviationPriority is reverse-ordered: Low=0, Normal=1, High=2, Critical=3
        // Maps conceptually: Critical → Error, High → Warning, Normal → Info, Low → Hint
        return priority switch
        {
            DeviationPriority.Critical => UnifiedSeverity.Error,
            DeviationPriority.High => UnifiedSeverity.Warning,
            DeviationPriority.Normal => UnifiedSeverity.Info,
            DeviationPriority.Low => UnifiedSeverity.Hint,
            _ => UnifiedSeverity.Info // Default fallback for unknown values
        };
    }

    /// <summary>
    /// Maps a <see cref="UnifiedSeverity"/> to <see cref="DeviationPriority"/>.
    /// </summary>
    /// <param name="severity">The unified severity.</param>
    /// <returns>The corresponding <see cref="DeviationPriority"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Reverse of <see cref="FromDeviationPriority"/>:
    /// <list type="bullet">
    ///   <item><description>Error (0) → Critical (3)</description></item>
    ///   <item><description>Warning (1) → High (2)</description></item>
    ///   <item><description>Info (2) → Normal (1)</description></item>
    ///   <item><description>Hint (3) → Low (0)</description></item>
    /// </list>
    /// </remarks>
    public static DeviationPriority ToDeviationPriority(UnifiedSeverity severity)
    {
        return severity switch
        {
            UnifiedSeverity.Error => DeviationPriority.Critical,
            UnifiedSeverity.Warning => DeviationPriority.High,
            UnifiedSeverity.Info => DeviationPriority.Normal,
            UnifiedSeverity.Hint => DeviationPriority.Low,
            _ => DeviationPriority.Normal // Default fallback for unknown values
        };
    }

    /// <summary>
    /// Maps a <see cref="UnifiedSeverity"/> to <see cref="ViolationSeverity"/>.
    /// </summary>
    /// <param name="severity">The unified severity.</param>
    /// <returns>The corresponding <see cref="ViolationSeverity"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Direct 1:1 mapping (reverse of <see cref="FromViolationSeverity"/>).
    /// </remarks>
    public static ViolationSeverity ToViolationSeverity(UnifiedSeverity severity)
    {
        return severity switch
        {
            UnifiedSeverity.Error => ViolationSeverity.Error,
            UnifiedSeverity.Warning => ViolationSeverity.Warning,
            UnifiedSeverity.Info => ViolationSeverity.Info,
            UnifiedSeverity.Hint => ViolationSeverity.Hint,
            _ => ViolationSeverity.Info // Default fallback for unknown values
        };
    }

    /// <summary>
    /// Maps a <see cref="UnifiedSeverity"/> to <see cref="ValidationSeverity"/>.
    /// </summary>
    /// <param name="severity">The unified severity.</param>
    /// <returns>The corresponding <see cref="ValidationSeverity"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Hint maps to Info since ValidationSeverity does not have Hint:
    /// <list type="bullet">
    ///   <item><description>Error (0) → Error (2)</description></item>
    ///   <item><description>Warning (1) → Warning (1)</description></item>
    ///   <item><description>Info (2) → Info (0)</description></item>
    ///   <item><description>Hint (3) → Info (0)</description></item>
    /// </list>
    /// </remarks>
    public static ValidationSeverity ToValidationSeverity(UnifiedSeverity severity)
    {
        return severity switch
        {
            UnifiedSeverity.Error => ValidationSeverity.Error,
            UnifiedSeverity.Warning => ValidationSeverity.Warning,
            UnifiedSeverity.Info => ValidationSeverity.Info,
            UnifiedSeverity.Hint => ValidationSeverity.Info, // Hint not available, downgrade to Info
            _ => ValidationSeverity.Info // Default fallback for unknown values
        };
    }
}
