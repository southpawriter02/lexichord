// -----------------------------------------------------------------------
// <copyright file="GatingCondition.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Represents a single parsed condition within a gate expression.
//   Conditions are extracted from the ConditionExpression string by
//   splitting on AND/OR operators. Each condition has an expected result
//   (true/false) and an optional error message for failure reporting.
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Represents a single parsed condition within a gate expression.
/// </summary>
/// <remarks>
/// <para>
/// Conditions are extracted from the <see cref="IGatingWorkflowStep.ConditionExpression"/>
/// by splitting on <c>AND</c> / <c>OR</c> operators. Each condition is independently
/// evaluated by the <see cref="IGatingConditionEvaluator"/> and the results are
/// combined using the gate's <see cref="IGatingWorkflowStep.RequireAll"/> logic.
/// </para>
/// <para>
/// <b>Supported Expressions:</b>
/// <list type="bullet">
///   <item><description><c>validation_count(severity) op value</c> — Count validation issues by severity</description></item>
///   <item><description><c>metadata('key') op value</c> — Check document metadata</description></item>
///   <item><description><c>content_length op value</c> — Check document content length</description></item>
///   <item><description><c>has_property == true/false</c> — Check property presence</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record GatingCondition
{
    /// <summary>
    /// Gets the unique identifier for this condition within the gate.
    /// </summary>
    /// <remarks>
    /// Auto-generated during expression parsing (e.g., "condition_0", "condition_1").
    /// Used for result attribution in evaluation logs.
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Gets the condition expression to evaluate.
    /// </summary>
    /// <remarks>
    /// A single evaluable expression such as <c>validation_count(error) == 0</c>
    /// or <c>content_length > 100</c>.
    /// </remarks>
    public required string Expression { get; init; }

    /// <summary>
    /// Gets the expected result of the condition evaluation.
    /// </summary>
    /// <remarks>
    /// Default is <c>true</c>. When set to <c>false</c>, the condition passes
    /// when the expression evaluates to <c>false</c>.
    /// </remarks>
    public bool ExpectedResult { get; init; } = true;

    /// <summary>
    /// Gets the optional error message displayed when this specific condition fails.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets optional metadata for evaluation context.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
