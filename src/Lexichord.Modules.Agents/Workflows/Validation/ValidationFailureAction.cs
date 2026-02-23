// -----------------------------------------------------------------------
// <copyright file="ValidationFailureAction.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Defines how the workflow engine should respond when a validation
//   step fails. This enables flexible error handling strategies ranging
//   from hard stops to silent logging.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Actions to take when a validation step fails.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="IValidationWorkflowStep"/> declares its failure action,
/// which the workflow engine uses to determine execution flow after a
/// validation failure. This enables both strict (halt-on-error) and lenient
/// (continue-with-warnings) validation strategies.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public enum ValidationFailureAction
{
    /// <summary>
    /// Stop workflow execution immediately.
    /// </summary>
    /// <remarks>
    /// The workflow engine will abort all remaining steps and return
    /// a failed result. This is the default action for critical validations.
    /// </remarks>
    Halt = 0,

    /// <summary>
    /// Continue with the next step despite failure.
    /// </summary>
    /// <remarks>
    /// The failure is recorded in the step result but execution continues.
    /// Useful for non-blocking validations where issues should be reported
    /// but not prevent further processing.
    /// </remarks>
    Continue = 1,

    /// <summary>
    /// Branch to an alternate workflow path.
    /// </summary>
    /// <remarks>
    /// The workflow engine will redirect execution to an alternate path
    /// defined in the workflow definition. Enables conditional error recovery.
    /// </remarks>
    Branch = 2,

    /// <summary>
    /// Log and notify but continue execution.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="Continue"/> but additionally triggers a
    /// notification event for UI display or external alerting.
    /// </remarks>
    Notify = 3
}
