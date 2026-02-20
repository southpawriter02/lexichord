// -----------------------------------------------------------------------
// <copyright file="WorkflowDefinition.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows;

/// <summary>
/// Complete workflow definition that can be persisted and executed.
/// </summary>
/// <remarks>
/// <para>
/// A workflow defines a sequence of agent steps that process content in order.
/// Each step invokes a specific agent with optional persona selection, prompt
/// overrides, and conditional execution logic.
/// </para>
/// <para>
/// Workflows are created and edited through the Workflow Designer UI (v0.7.7a)
/// and can be exported/imported as YAML for sharing and version control.
/// </para>
/// </remarks>
/// <param name="WorkflowId">
/// Unique identifier for the workflow, prefixed with "wf-".
/// Generated automatically by <see cref="IWorkflowDesignerService.CreateNew"/>.
/// </param>
/// <param name="Name">
/// Human-readable name for the workflow. Required for validation.
/// </param>
/// <param name="Description">
/// Optional description explaining the workflow's purpose and behavior.
/// </param>
/// <param name="IconName">
/// Optional Lucide icon name for UI display (e.g., "file-code", "sparkles").
/// </param>
/// <param name="Steps">
/// Ordered list of workflow steps. Each step invokes a registered agent.
/// </param>
/// <param name="Metadata">
/// Metadata about the workflow including author, timestamps, and license tier.
/// </param>
/// <example>
/// <code>
/// var workflow = new WorkflowDefinition(
///     WorkflowId: "wf-abc123",
///     Name: "Technical Review",
///     Description: "Reviews technical documentation",
///     IconName: "file-code",
///     Steps: new[]
///     {
///         new WorkflowStepDefinition("s1", "editor", "strict", null, 1, null, null, null),
///         new WorkflowStepDefinition("s2", "simplifier", null, null, 2, null, null, null),
///     },
///     Metadata: new WorkflowMetadata("user", DateTime.UtcNow, DateTime.UtcNow,
///         "1.0", Array.Empty&lt;string&gt;(), WorkflowCategory.Technical, false, LicenseTier.Teams));
/// </code>
/// </example>
public record WorkflowDefinition(
    string WorkflowId,
    string Name,
    string Description,
    string? IconName,
    IReadOnlyList<WorkflowStepDefinition> Steps,
    WorkflowMetadata Metadata
);

/// <summary>
/// Represents a single step in a workflow definition.
/// </summary>
/// <remarks>
/// <para>
/// Each step maps to a registered agent in the <see cref="Lexichord.Abstractions.Agents.IAgentRegistry"/>.
/// Steps execute sequentially by <see cref="Order"/> and can optionally specify:
/// </para>
/// <list type="bullet">
///   <item><description>A persona variant via <see cref="PersonaId"/></description></item>
///   <item><description>A custom prompt via <see cref="PromptOverride"/></description></item>
///   <item><description>Conditional execution via <see cref="Condition"/></description></item>
///   <item><description>Data flow via <see cref="InputMappings"/> and <see cref="OutputMappings"/></description></item>
/// </list>
/// </remarks>
/// <param name="StepId">Unique identifier for this step within the workflow.</param>
/// <param name="AgentId">ID of the agent to invoke (must exist in the registry).</param>
/// <param name="PersonaId">Optional persona ID for the agent. Null uses the agent's default persona.</param>
/// <param name="PromptOverride">Optional custom prompt to use instead of the agent's default prompt template.</param>
/// <param name="Order">Execution order (1-based). Steps execute in ascending order.</param>
/// <param name="Condition">Optional condition that must be met for this step to execute.</param>
/// <param name="InputMappings">Optional mappings from previous step outputs to this step's inputs.</param>
/// <param name="OutputMappings">Optional mappings from this step's outputs to named variables.</param>
public record WorkflowStepDefinition(
    string StepId,
    string AgentId,
    string? PersonaId,
    string? PromptOverride,
    int Order,
    WorkflowStepCondition? Condition,
    IReadOnlyDictionary<string, string>? InputMappings,
    IReadOnlyDictionary<string, string>? OutputMappings
);

/// <summary>
/// Condition that determines whether a workflow step should execute.
/// </summary>
/// <remarks>
/// Conditions enable branching logic in workflows. The <see cref="Type"/> property
/// determines how the condition is evaluated:
/// <list type="bullet">
///   <item><description><see cref="ConditionType.Always"/> — Step always executes (default)</description></item>
///   <item><description><see cref="ConditionType.PreviousSuccess"/> — Only if previous step succeeded</description></item>
///   <item><description><see cref="ConditionType.PreviousFailed"/> — Only if previous step failed</description></item>
///   <item><description><see cref="ConditionType.Expression"/> — Evaluates the <see cref="Expression"/> string</description></item>
/// </list>
/// </remarks>
/// <param name="Expression">
/// Expression string for <see cref="ConditionType.Expression"/> conditions.
/// Empty for non-expression condition types.
/// </param>
/// <param name="Type">The type of condition. Defaults to <see cref="ConditionType.Expression"/>.</param>
public record WorkflowStepCondition(
    string Expression,
    ConditionType Type = ConditionType.Expression
);

/// <summary>
/// Types of conditions supported in workflow steps.
/// </summary>
/// <remarks>
/// Condition types control the execution flow between workflow steps.
/// </remarks>
public enum ConditionType
{
    /// <summary>
    /// Evaluates a custom expression string (e.g., "violations.Count > 0").
    /// </summary>
    Expression,

    /// <summary>
    /// Execute only if the previous step completed successfully.
    /// </summary>
    PreviousSuccess,

    /// <summary>
    /// Execute only if the previous step failed.
    /// </summary>
    PreviousFailed,

    /// <summary>
    /// Always execute this step regardless of previous step outcome.
    /// </summary>
    Always
}
