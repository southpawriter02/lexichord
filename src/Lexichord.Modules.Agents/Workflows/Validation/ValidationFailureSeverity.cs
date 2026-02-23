// -----------------------------------------------------------------------
// <copyright file="ValidationFailureSeverity.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Severity levels for validation failures. Used in conjunction with
//   ValidationFailureAction to determine how the workflow engine responds
//   to validation issues. Higher severity levels indicate more urgent issues.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Severity levels for validation failures.
/// </summary>
/// <remarks>
/// <para>
/// Each validation step declares a <see cref="ValidationFailureSeverity"/>
/// which categorizes the importance of any failures found. Combined with
/// <see cref="ValidationFailureAction"/>, this determines the overall
/// workflow behavior on failure.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public enum ValidationFailureSeverity
{
    /// <summary>
    /// Informational message — no action required.
    /// </summary>
    /// <remarks>
    /// Used for minor observations that do not affect document quality.
    /// </remarks>
    Info = 0,

    /// <summary>
    /// Warning — proceed with caution.
    /// </summary>
    /// <remarks>
    /// Indicates potential issues that should be reviewed but do not
    /// necessarily indicate incorrect content.
    /// </remarks>
    Warning = 1,

    /// <summary>
    /// Error — should be addressed before publishing.
    /// </summary>
    /// <remarks>
    /// Indicates definite issues that affect document quality or correctness
    /// and should be resolved.
    /// </remarks>
    Error = 2,

    /// <summary>
    /// Critical — requires immediate action.
    /// </summary>
    /// <remarks>
    /// Indicates severe issues such as broken references, schema violations,
    /// or data integrity problems that must be fixed immediately.
    /// </remarks>
    Critical = 3
}
