// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowDefinition.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Complete definition of a validation workflow template.
/// </summary>
/// <remarks>
/// <para>
/// A validation workflow defines a sequence of validation, gating, and sync
/// steps that are executed by the <c>IWorkflowEngine</c> (v0.7.7b). Pre-built
/// workflows are loaded from embedded YAML resources; custom workflows are
/// persisted via <see cref="IValidationWorkflowStorage"/>.
/// </para>
/// <para>
/// Three pre-built validation workflows are provided:
/// <list type="bullet">
///   <item><description><b>On-Save Validation</b> — schema, consistency, and reference checks on save</description></item>
///   <item><description><b>Pre-Publish Gate</b> — comprehensive validation with gating before publication</description></item>
///   <item><description><b>Nightly Health Check</b> — scheduled workspace-wide batch validation</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §2–§3
/// </para>
/// </remarks>
public record ValidationWorkflowDefinition
{
    /// <summary>Unique workflow identifier (e.g., "on-save-validation").</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable workflow name.</summary>
    public required string Name { get; init; }

    /// <summary>Optional description of the workflow's purpose and behavior.</summary>
    public string? Description { get; init; }

    /// <summary>Semantic version of the workflow definition (e.g., "1.0.0").</summary>
    public required string Version { get; init; }

    /// <summary>
    /// Event that triggers this workflow.
    /// See <see cref="ValidationWorkflowTrigger"/> for available trigger types.
    /// </summary>
    public ValidationWorkflowTrigger Trigger { get; init; }

    /// <summary>
    /// Indicates whether this is a system-provided pre-built workflow.
    /// Pre-built workflows cannot be modified or deleted via the registry.
    /// </summary>
    public bool IsPrebuilt { get; init; }

    /// <summary>
    /// Whether this workflow is enabled by default when first registered.
    /// Users can toggle individual workflows on/off at runtime.
    /// </summary>
    public bool EnabledByDefault { get; init; } = true;

    /// <summary>
    /// Overall workflow timeout in minutes. The workflow engine cancels
    /// execution if total elapsed time exceeds this value.
    /// </summary>
    public int TimeoutMinutes { get; init; } = 10;

    /// <summary>Ordered list of validation steps to execute.</summary>
    public required IReadOnlyList<ValidationWorkflowStepDef> Steps { get; init; }

    /// <summary>
    /// License tiers authorized to execute this workflow.
    /// See <see cref="ValidationWorkflowLicenseRequirement"/>.
    /// </summary>
    public required ValidationWorkflowLicenseRequirement LicenseRequirement { get; init; }

    /// <summary>
    /// Expected duration in minutes under normal conditions.
    /// Used for progress estimation and scheduling.
    /// </summary>
    public int? ExpectedDurationMinutes { get; init; }

    /// <summary>
    /// Performance targets as key-value pairs (e.g., "max_duration_ms" → 120000).
    /// Used for monitoring and alerting.
    /// </summary>
    public IReadOnlyDictionary<string, int>? PerformanceTargets { get; init; }

    /// <summary>UTC timestamp when this workflow definition was created.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>UTC timestamp of the last modification. Null for never-modified workflows.</summary>
    public DateTime? ModifiedAt { get; init; }

    /// <summary>User ID of the creator. Null for system pre-built workflows.</summary>
    public Guid? CreatedBy { get; init; }
}
