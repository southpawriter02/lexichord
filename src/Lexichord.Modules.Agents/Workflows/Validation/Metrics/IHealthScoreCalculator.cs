// -----------------------------------------------------------------------
// <copyright file="IHealthScoreCalculator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.Metrics;

/// <summary>
/// Calculates health scores for workspaces and documents from metrics data.
/// </summary>
/// <remarks>
/// <para>
/// Implements the health score formulas defined in LCS-DES-077-KG-i §4.2:
/// <list type="bullet">
///   <item><description><b>Workspace:</b> PassRate × 80 + ErrorFreedom (0–20)</description></item>
///   <item><description><b>Document:</b> BaseScore (80/30) − ErrorPenalty − WarningPenalty</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-i §4.2
/// </para>
/// </remarks>
public interface IHealthScoreCalculator
{
    /// <summary>
    /// Calculates the workspace health score from recent execution metrics.
    /// </summary>
    /// <param name="recentMetrics">
    /// Execution metrics from the evaluation period (typically last 7 days).
    /// </param>
    /// <returns>The calculated workspace health score (0–100).</returns>
    WorkspaceHealthScore CalculateWorkspaceHealth(
        IReadOnlyList<WorkflowExecutionMetrics> recentMetrics);

    /// <summary>
    /// Calculates the document health score from its execution history.
    /// </summary>
    /// <param name="documentId">The document being scored.</param>
    /// <param name="documentMetrics">
    /// All execution metrics for this document, ordered by date.
    /// </param>
    /// <returns>The calculated document health score (0–100).</returns>
    DocumentHealthScore CalculateDocumentHealth(
        Guid documentId,
        IReadOnlyList<WorkflowExecutionMetrics> documentMetrics);
}
