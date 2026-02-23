// -----------------------------------------------------------------------
// <copyright file="GatingEvaluationContext.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Context record passed to IGatingConditionEvaluator during gate
//   condition evaluation. Contains document content, validation results
//   from prior steps, and user-defined variables for expression resolution.
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: ValidationStepResult (v0.7.7e)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Context for evaluating gate condition expressions.
/// </summary>
/// <remarks>
/// <para>
/// Provides the data environment for <see cref="IGatingConditionEvaluator"/>
/// when evaluating individual <see cref="GatingCondition"/> expressions.
/// Contains the document content, metadata from previous validation steps,
/// and user-defined variables for custom expressions.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record GatingEvaluationContext
{
    /// <summary>
    /// Gets the content of the document being evaluated.
    /// </summary>
    /// <remarks>
    /// Used by <c>content_length</c> expressions to check document size.
    /// </remarks>
    public string? DocumentContent { get; init; }

    /// <summary>
    /// Gets the document type (e.g., "markdown", "yaml", "json").
    /// </summary>
    public string? DocumentType { get; init; }

    /// <summary>
    /// Gets the document metadata dictionary.
    /// </summary>
    /// <remarks>
    /// Used by <c>metadata('key')</c> expressions to check document properties.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? DocumentMetadata { get; init; }

    /// <summary>
    /// Gets the validation results from previously executed validation steps.
    /// </summary>
    /// <remarks>
    /// Used by <c>validation_count(severity)</c> expressions to count
    /// issues by severity level. These are <see cref="ValidationStepResult"/>
    /// instances from v0.7.7e validation steps.
    /// </remarks>
    public IReadOnlyList<ValidationStepResult>? ValidationResults { get; init; }

    /// <summary>
    /// Gets optional user-defined variables for custom expression resolution.
    /// </summary>
    /// <remarks>
    /// Allows callers to pass arbitrary variables (e.g., <c>has_schema</c>)
    /// that can be referenced in gate expressions.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Variables { get; init; }
}
