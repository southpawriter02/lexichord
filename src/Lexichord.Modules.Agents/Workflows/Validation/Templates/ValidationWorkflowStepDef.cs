// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowStepDef.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Templates;

/// <summary>
/// Defines a single step within a validation workflow template.
/// </summary>
/// <remarks>
/// <para>
/// Each step corresponds to a specific validation operation (schema check,
/// consistency check, gating decision, sync, etc.) that is executed in order
/// by the workflow engine. Steps reference step types defined in v0.7.7e/f/g
/// (<see cref="ValidationStepType"/>, gating, sync).
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §3
/// </para>
/// </remarks>
public record ValidationWorkflowStepDef
{
    /// <summary>Unique step identifier within the workflow (e.g., "schema-validation").</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable display name for the step.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Step type identifier mapped to a registered step implementation.
    /// Corresponds to <see cref="ValidationStepType"/> values or special types
    /// like "gating" (<see cref="IGatingWorkflowStep"/>) or "sync"
    /// (<see cref="ISyncWorkflowStep"/>).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>Execution order (1-based). Steps execute in ascending order.</summary>
    public int Order { get; init; }

    /// <summary>Whether this step is enabled. Disabled steps are skipped at runtime.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Per-step timeout in milliseconds. Null uses the workflow-level default.
    /// Steps that exceed this timeout are cancelled and reported as failed.
    /// </summary>
    public int? TimeoutMs { get; init; }

    /// <summary>
    /// Step-specific configuration options passed to the step implementation.
    /// Keys and values are step-type-dependent (e.g., "severity_threshold" for gating).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Options { get; init; }
}
