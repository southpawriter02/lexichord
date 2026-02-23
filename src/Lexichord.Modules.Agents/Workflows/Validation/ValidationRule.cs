// -----------------------------------------------------------------------
// <copyright file="ValidationRule.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Represents a single validation rule that can be executed within
//   a validation workflow step. Rules are self-describing with metadata
//   for filtering, categorization, and severity configuration.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: ValidationStepType (v0.7.7e), ValidationFailureSeverity (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Represents a single validation rule within a validation step.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="ValidationRule"/> is a self-describing unit of validation logic
/// that can be enabled/disabled, categorized, and configured. Rules are retrieved
/// by <see cref="IValidationWorkflowStep.GetValidationRulesAsync"/> and executed
/// as part of the step's validation pass.
/// </para>
/// <para>
/// Rules declare a <see cref="MinimumSeverity"/> which determines the threshold
/// at which a finding from this rule is treated as a failure. For example, a rule
/// with <see cref="ValidationFailureSeverity.Warning"/> will only fail if the
/// finding severity is Warning or higher.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public record ValidationRule
{
    /// <summary>
    /// Gets the unique identifier for this validation rule.
    /// </summary>
    /// <remarks>
    /// Must be stable across application restarts. Used for rule filtering,
    /// result attribution, and configuration-based enable/disable.
    /// Convention: lowercase kebab-case (e.g., "schema-frontmatter", "ref-internal-links").
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the human-readable display name for this rule.
    /// </summary>
    /// <remarks>
    /// Shown in the validation results panel and logs.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Gets the optional description of what this rule validates.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Gets the optional category for grouping related rules.
    /// </summary>
    /// <remarks>
    /// Used for UI grouping and bulk enable/disable operations.
    /// Examples: "Structure", "References", "Content", "Metadata".
    /// </remarks>
    public string? Category { get; init; }

    /// <summary>
    /// Gets the validation step type this rule belongs to.
    /// </summary>
    /// <remarks>
    /// Links this rule to the appropriate <see cref="ValidationStepType"/>
    /// for routing and filtering.
    /// </remarks>
    public ValidationStepType Type { get; init; }

    /// <summary>
    /// Gets whether this rule is enabled for execution.
    /// </summary>
    /// <remarks>
    /// Disabled rules are skipped during validation but remain registered
    /// for configuration purposes.
    /// </remarks>
    public bool IsEnabled { get; init; } = true;

    /// <summary>
    /// Gets the optional configuration parameters for this rule.
    /// </summary>
    /// <remarks>
    /// Rule-specific settings such as thresholds, patterns, or target values.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Config { get; init; }

    /// <summary>
    /// Gets the minimum severity level at which this rule causes a validation failure.
    /// </summary>
    /// <remarks>
    /// Findings below this severity are treated as informational and do not
    /// contribute to the step's failure state. Defaults to <see cref="ValidationFailureSeverity.Error"/>.
    /// </remarks>
    public ValidationFailureSeverity MinimumSeverity { get; init; } = ValidationFailureSeverity.Error;
}
