// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowTrigger.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Defines when a validation workflow should be triggered.
/// </summary>
/// <remarks>
/// <para>
/// Each trigger type maps to a specific lifecycle event in the document
/// management pipeline. The workflow engine uses this enum to determine
/// which workflows to execute when an event occurs.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §2
/// </para>
/// </remarks>
public enum ValidationWorkflowTrigger
{
    /// <summary>
    /// Workflow is triggered manually by the user or API call.
    /// Available at all license tiers.
    /// </summary>
    Manual,

    /// <summary>
    /// Workflow is triggered automatically when a document is saved.
    /// Requires WriterPro+ license tier.
    /// </summary>
    OnSave,

    /// <summary>
    /// Workflow is triggered before a document is published.
    /// Requires Teams+ license tier.
    /// </summary>
    PrePublish,

    /// <summary>
    /// Workflow is triggered on a nightly schedule (default 2 AM UTC).
    /// Requires Teams+ license tier.
    /// </summary>
    ScheduledNightly,

    /// <summary>
    /// Custom trigger defined by the user or integration.
    /// Requires Enterprise license tier.
    /// </summary>
    Custom
}
