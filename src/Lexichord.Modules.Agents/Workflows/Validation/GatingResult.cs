// -----------------------------------------------------------------------
// <copyright file="GatingResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Result of a gating step evaluation. Contains the pass/fail
//   status, the condition that was evaluated, failure details, branch path
//   for alternate workflow routing, and an audit trail of evaluation logs.
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Result of a gating step evaluation.
/// </summary>
/// <remarks>
/// <para>
/// Returned by <see cref="IGatingWorkflowStep.EvaluateAsync"/> after evaluating
/// all gate conditions. Contains the overall pass/fail status, the original
/// condition expression, failure details for user display, an optional branch
/// path for alternate workflow routing, and a timestamped audit trail.
/// </para>
/// <para>
/// The workflow engine inspects <see cref="Passed"/> to determine whether to
/// continue execution. If <see cref="BranchPath"/> is set and the gate fails,
/// the engine may redirect to an alternate workflow path.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
public record GatingResult
{
    /// <summary>
    /// Gets whether the gate passed (all/any conditions met, depending on <c>RequireAll</c>).
    /// </summary>
    public required bool Passed { get; init; }

    /// <summary>
    /// Gets the identifier of the gate step that produced this result.
    /// </summary>
    public required string GateId { get; init; }

    /// <summary>
    /// Gets the condition expression that was evaluated.
    /// </summary>
    public required string Condition { get; init; }

    /// <summary>
    /// Gets the failure message displayed to the user when the gate does not pass.
    /// </summary>
    /// <remarks>
    /// Null when <see cref="Passed"/> is <c>true</c>.
    /// </remarks>
    public string? FailureMessage { get; init; }

    /// <summary>
    /// Gets the alternate workflow path to branch to on gate failure.
    /// </summary>
    /// <remarks>
    /// Null when <see cref="Passed"/> is <c>true</c> or when no branch is configured.
    /// The workflow engine uses this to redirect execution.
    /// </remarks>
    public string? BranchPath { get; init; }

    /// <summary>
    /// Gets the timestamped audit trail of evaluation decisions.
    /// </summary>
    /// <remarks>
    /// Each entry records a step in the evaluation process: condition parsing,
    /// individual condition results, and the overall outcome. Useful for
    /// debugging gate logic and compliance auditing.
    /// </remarks>
    public IReadOnlyList<string> EvaluationLogs { get; init; } = [];

    /// <summary>
    /// Gets the execution duration in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>
    /// Gets optional metadata about the evaluation.
    /// </summary>
    /// <remarks>
    /// May contain condition count, require-all flag, and other diagnostic data.
    /// </remarks>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
