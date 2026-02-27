// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowLicenseRequirement.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Defines which license tiers are authorized to execute a validation workflow.
/// </summary>
/// <remarks>
/// <para>
/// Each boolean property indicates whether a specific license tier grants
/// access to the workflow. At least one tier must be <c>true</c> for the
/// workflow to be executable.
/// </para>
/// <para>
/// <b>License gating examples:</b>
/// <list type="bullet">
///   <item><description>On-Save Validation: WriterPro=true, Teams=true, Enterprise=true</description></item>
///   <item><description>Pre-Publish Gate: Teams=true, Enterprise=true</description></item>
///   <item><description>Nightly Health Check: Teams=true, Enterprise=true</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §3
/// </para>
/// </remarks>
public record ValidationWorkflowLicenseRequirement
{
    /// <summary>Whether the Core (free) tier can execute this workflow.</summary>
    public bool Core { get; init; } = false;

    /// <summary>Whether the WriterPro tier can execute this workflow.</summary>
    public bool WriterPro { get; init; } = false;

    /// <summary>Whether the Teams tier can execute this workflow.</summary>
    public bool Teams { get; init; } = true;

    /// <summary>Whether the Enterprise tier can execute this workflow.</summary>
    public bool Enterprise { get; init; } = true;
}
