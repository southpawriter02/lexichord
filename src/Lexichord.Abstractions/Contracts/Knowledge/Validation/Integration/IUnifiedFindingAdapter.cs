// =============================================================================
// File: IUnifiedFindingAdapter.cs
// Project: Lexichord.Abstractions
// Description: Adapts findings from different sources to unified format.
// =============================================================================
// LOGIC: Maps ValidationFinding → UnifiedFinding and StyleViolation →
//   UnifiedFinding. Also normalizes the different severity models to
//   UnifiedSeverity.
//
// SPEC ADAPTATION:
//   - FromLinterFinding(LinterFinding) → FromStyleViolation(StyleViolation)
//   - NormalizeSeverity split into two typed overloads instead of
//     accepting (object, FindingSource)
//
// v0.6.5j: Linter Integration (CKVS Phase 3a)
// =============================================================================

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;

/// <summary>
/// Adapts findings from the validation engine and style linter to the
/// <see cref="UnifiedFinding"/> format.
/// </summary>
/// <remarks>
/// <para>
/// This adapter is the single point of conversion logic. It encapsulates
/// the mapping between source-specific data contracts and the unified model.
/// </para>
/// <para>
/// <b>Severity Mapping:</b>
/// <list type="table">
///   <listheader>
///     <term>Source</term>
///     <description>Mapping</description>
///   </listheader>
///   <item>
///     <term><see cref="ValidationSeverity"/></term>
///     <description>Error→Error, Warning→Warning, Info→Info</description>
///   </item>
///   <item>
///     <term><see cref="ViolationSeverity"/></term>
///     <description>Error→Error, Warning→Warning, Info→Info, Hint→Hint</description>
///   </item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.5j as part of the Linter Integration.
/// </para>
/// </remarks>
public interface IUnifiedFindingAdapter
{
    /// <summary>
    /// Converts a <see cref="ValidationFinding"/> to a <see cref="UnifiedFinding"/>.
    /// </summary>
    /// <param name="finding">The validation finding to convert.</param>
    /// <returns>A normalized <see cref="UnifiedFinding"/>.</returns>
    /// <remarks>
    /// LOGIC: Maps ValidatorId to FindingCategory (schema→Schema, axiom→Axiom,
    /// consistency→Consistency, default→Schema). Uses PropertyPath for location.
    /// </remarks>
    UnifiedFinding FromValidationFinding(ValidationFinding finding);

    /// <summary>
    /// Converts a <see cref="StyleViolation"/> to a <see cref="UnifiedFinding"/>.
    /// </summary>
    /// <param name="violation">The style violation to convert.</param>
    /// <returns>A normalized <see cref="UnifiedFinding"/>.</returns>
    /// <remarks>
    /// LOGIC: Maps Rule.Category to FindingCategory. Uses StartLine:StartColumn
    /// as the PropertyPath string. Sets Source to StyleLinter.
    /// </remarks>
    UnifiedFinding FromStyleViolation(StyleViolation violation);

    /// <summary>
    /// Normalizes a <see cref="ValidationSeverity"/> to <see cref="UnifiedSeverity"/>.
    /// </summary>
    /// <param name="severity">The validation severity to normalize.</param>
    /// <returns>The equivalent <see cref="UnifiedSeverity"/>.</returns>
    UnifiedSeverity NormalizeSeverity(ValidationSeverity severity);

    /// <summary>
    /// Normalizes a <see cref="ViolationSeverity"/> to <see cref="UnifiedSeverity"/>.
    /// </summary>
    /// <param name="severity">The violation severity to normalize.</param>
    /// <returns>The equivalent <see cref="UnifiedSeverity"/>.</returns>
    UnifiedSeverity NormalizeSeverity(ViolationSeverity severity);
}
