// -----------------------------------------------------------------------
// <copyright file="IGatingConditionEvaluator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Interface for evaluating individual gating conditions. Each
//   condition is evaluated independently against the expression evaluation
//   context, and results are combined by the GatingWorkflowStep using
//   AND/OR logic.
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: GatingCondition (v0.7.7f), GatingEvaluationContext (v0.7.7f)
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Evaluates individual gating conditions against an evaluation context.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="GatingCondition"/> is evaluated independently by this service.
/// The <see cref="GatingWorkflowStep"/> combines individual results using
/// AND/OR logic based on its <see cref="IGatingWorkflowStep.RequireAll"/> setting.
/// </para>
/// <para>
/// <b>Supported Expression Types:</b>
/// <list type="bullet">
///   <item><description><c>validation_count(severity) op value</c></description></item>
///   <item><description><c>metadata('key') op value</c></description></item>
///   <item><description><c>content_length op value</c></description></item>
///   <item><description><c>has_property == true/false</c></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe for concurrent
/// condition evaluation.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public interface IGatingConditionEvaluator
{
    /// <summary>
    /// Evaluates a single gating condition against the provided context.
    /// </summary>
    /// <param name="condition">
    /// The condition to evaluate, containing the expression string and expected result.
    /// </param>
    /// <param name="context">
    /// The evaluation context containing document content, validation results,
    /// and user-defined variables.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for aborting evaluation.
    /// </param>
    /// <returns>
    /// <c>true</c> if the condition passes (expression result matches
    /// <see cref="GatingCondition.ExpectedResult"/>); <c>false</c> otherwise.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the expression syntax is invalid or references an unknown function.
    /// </exception>
    Task<bool> EvaluateAsync(
        GatingCondition condition,
        GatingEvaluationContext context,
        CancellationToken ct = default);
}
