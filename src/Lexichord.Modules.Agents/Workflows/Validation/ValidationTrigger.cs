// -----------------------------------------------------------------------
// <copyright file="ValidationTrigger.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Enumerates the events that can trigger a validation workflow.
//   The trigger type is recorded in the ValidationContext to enable
//   conditional rule execution based on the originating event.
//
// v0.7.7e: Validation Step Types (CKVS Phase 4d)
// Dependencies: None
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Events that can trigger a validation workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// The trigger type is stored in the <see cref="ValidationWorkflowContext"/> and
/// can be used by validation rules to adjust their behavior. For example, an
/// <see cref="OnSave"/> trigger might run fewer checks than <see cref="PrePublish"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7e as part of Validation Step Types (CKVS Phase 4d).
/// </para>
/// </remarks>
public enum ValidationTrigger
{
    /// <summary>
    /// Manually triggered by the user.
    /// </summary>
    Manual,

    /// <summary>
    /// Triggered automatically when a document is saved.
    /// </summary>
    OnSave,

    /// <summary>
    /// Triggered before publishing a document.
    /// </summary>
    PrePublish,

    /// <summary>
    /// Triggered by a scheduled nightly validation job.
    /// </summary>
    ScheduledNightly,

    /// <summary>
    /// Triggered as a pre-step within a workflow execution.
    /// </summary>
    PreWorkflow,

    /// <summary>
    /// Custom trigger from an external source or plugin.
    /// </summary>
    Custom
}
