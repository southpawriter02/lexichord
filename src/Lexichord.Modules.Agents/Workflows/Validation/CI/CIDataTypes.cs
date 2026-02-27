// -----------------------------------------------------------------------
// <copyright file="CIDataTypes.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Workflows.Validation.CI;

// ═══════════════════════════════════════════════════════════════════════
// §3.1 — CI Workflow Request
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Request to execute a validation workflow in a CI/CD context.
/// </summary>
/// <remarks>
/// <para>
/// Encapsulates all parameters needed to run a workflow headlessly: target
/// workspace, workflow ID, document reference, timeout, failure thresholds,
/// output format, and optional CI system metadata for traceability.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §3.1
/// </para>
/// </remarks>
public record CIWorkflowRequest
{
    /// <summary>Workspace ID or API key identifying the target workspace.</summary>
    public required string WorkspaceId { get; init; }

    /// <summary>Workflow ID to execute (e.g., "on-save-validation", "pre-publish-gate").</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Document ID to validate. Mutually exclusive with <see cref="DocumentPath"/>.</summary>
    public string? DocumentId { get; init; }

    /// <summary>File system path to the document to validate. Mutually exclusive with <see cref="DocumentId"/>.</summary>
    public string? DocumentPath { get; init; }

    /// <summary>Workspace API key for authentication in remote scenarios.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Base URL of the Lexichord instance. Defaults to localhost.</summary>
    public string BaseUrl { get; init; } = "http://localhost:5000";

    /// <summary>
    /// Timeout in seconds for the entire workflow execution.
    /// Exceeding this triggers exit code 124 (Timeout).
    /// </summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>
    /// Whether to treat any warnings as failures. When <c>true</c>, exit code 1
    /// is returned if any warnings are detected, even if all validations pass.
    /// </summary>
    public bool FailOnWarnings { get; init; }

    /// <summary>
    /// Maximum number of warnings allowed before the execution is considered failed.
    /// <c>null</c> means no limit. Only evaluated when <see cref="FailOnWarnings"/> is <c>false</c>.
    /// </summary>
    public int? MaxWarnings { get; init; }

    /// <summary>
    /// Whether to stop execution immediately after the first error is encountered.
    /// When <c>false</c>, all steps execute regardless of individual failures.
    /// </summary>
    public bool HaltOnFirstError { get; init; }

    /// <summary>
    /// Desired output format for structured results.
    /// Determines how the <see cref="CIExecutionResult"/> is serialized for CI consumption.
    /// </summary>
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Json;

    /// <summary>
    /// Key-value parameters to inject into the workflow execution context.
    /// These are available to workflow steps as variables.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }

    /// <summary>
    /// Metadata about the CI/CD system triggering this execution.
    /// Used for traceability and reporting (build ID, commit SHA, branch, etc.).
    /// </summary>
    public CISystemMetadata? CIMetadata { get; init; }

    /// <summary>
    /// Optional email address for sending completion notifications.
    /// <c>null</c> disables notification.
    /// </summary>
    public string? NotificationEmail { get; init; }

    /// <summary>
    /// Whether to block until the workflow completes.
    /// When <c>false</c>, returns immediately with a pending execution ID.
    /// </summary>
    public bool WaitForCompletion { get; init; } = true;
}

// ═══════════════════════════════════════════════════════════════════════
// §3.1 — CI System Metadata
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Metadata describing the CI/CD system that triggered the workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Captures build context for linking validation results back to the originating
/// pipeline run. All fields are optional — the service gracefully handles missing
/// metadata without impacting workflow execution.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §3.1
/// </para>
/// </remarks>
public record CISystemMetadata
{
    /// <summary>CI system name (e.g., "github", "gitlab", "azure", "jenkins", "circleci").</summary>
    public string? System { get; init; }

    /// <summary>Build or pipeline run ID from the CI system.</summary>
    public string? BuildId { get; init; }

    /// <summary>URL to the build/run in the CI system's web UI.</summary>
    public string? BuildUrl { get; init; }

    /// <summary>Git commit SHA that triggered this build.</summary>
    public string? CommitSha { get; init; }

    /// <summary>Git branch name (e.g., "main", "feature/my-branch").</summary>
    public string? Branch { get; init; }

    /// <summary>Pull request number, if this build was triggered by a PR. <c>null</c> otherwise.</summary>
    public int? PullRequestNumber { get; init; }

    /// <summary>Repository URL for source code traceability.</summary>
    public string? RepositoryUrl { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════
// §3.2 — CI Execution Result
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Result of a CI/CD workflow execution, including outcome, exit code, and details.
/// </summary>
/// <remarks>
/// <para>
/// Contains everything a CI/CD pipeline needs to evaluate the workflow outcome:
/// a boolean success flag, a numeric exit code, a validation summary with
/// error/warning counts, and per-step results with individual errors and warnings.
/// </para>
/// <para>
/// <b>Exit code mapping:</b>
/// </para>
/// <list type="table">
///   <listheader><term>Code</term><description>Meaning</description></listheader>
///   <item><term>0</term><description>All validations passed</description></item>
///   <item><term>1</term><description>Validation failures detected</description></item>
///   <item><term>2</term><description>Invalid input</description></item>
///   <item><term>3</term><description>Execution error</description></item>
///   <item><term>124</term><description>Timeout exceeded</description></item>
///   <item><term>127</term><description>Fatal error</description></item>
/// </list>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §3.2
/// </para>
/// </remarks>
public record CIExecutionResult
{
    /// <summary>Unique execution identifier, usable for status queries and log retrieval.</summary>
    public required string ExecutionId { get; init; }

    /// <summary>Workflow definition ID that was executed.</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Overall success status. <c>true</c> if all validations passed within thresholds.</summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Numeric exit code for CI/CD system integration.
    /// See <see cref="ExitCode"/> for standard values.
    /// </summary>
    public int ExitCode { get; init; }

    /// <summary>Total wall-clock execution time in seconds.</summary>
    public int ExecutionTimeSeconds { get; init; }

    /// <summary>Aggregated validation summary with error/warning counts and pass rates.</summary>
    public CIValidationSummary? ValidationSummary { get; init; }

    /// <summary>Per-step execution results with individual errors and warnings.</summary>
    public IReadOnlyList<CIStepResult> StepResults { get; init; } = [];

    /// <summary>Human-readable message summarizing the execution outcome.</summary>
    public string? Message { get; init; }

    /// <summary>Detailed error message if the execution failed. <c>null</c> on success.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Raw output from the workflow engine, if available.</summary>
    public string? RawOutput { get; init; }

    /// <summary>
    /// Structured key-value results for programmatic consumption.
    /// Content varies by workflow type.
    /// </summary>
    public IReadOnlyDictionary<string, object>? StructuredResults { get; init; }

    /// <summary>URL to view detailed results in the Lexichord UI, if available.</summary>
    public string? ResultsUrl { get; init; }

    /// <summary>UTC timestamp of when the execution started.</summary>
    public DateTime ExecutedAt { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════
// §3.2 — Validation Summary
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Aggregated validation summary for a CI/CD workflow execution.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-j §3.2
/// </remarks>
public record CIValidationSummary
{
    /// <summary>Total validation errors found across all steps.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Total validation warnings found across all steps.</summary>
    public int WarningCount { get; init; }

    /// <summary>Number of documents that were validated.</summary>
    public int DocumentsValidated { get; init; }

    /// <summary>Number of documents that passed all validations.</summary>
    public int DocumentsPassed { get; init; }

    /// <summary>Number of documents that failed one or more validations.</summary>
    public int DocumentsFailed { get; init; }

    /// <summary>Pass rate as a percentage (0–100).</summary>
    public decimal PassRate { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════
// §3.2 — Step Result
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Result of a single workflow step execution within a CI/CD run.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-j §3.2
/// </remarks>
public record CIStepResult
{
    /// <summary>Unique step identifier within the workflow.</summary>
    public required string StepId { get; init; }

    /// <summary>Human-readable step name.</summary>
    public string? Name { get; init; }

    /// <summary>Whether this step passed its validation criteria.</summary>
    public bool Success { get; init; }

    /// <summary>Step execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Error messages produced by this step.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Warning messages produced by this step.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Human-readable summary message from the step.</summary>
    public string? Message { get; init; }
}

// ═══════════════════════════════════════════════════════════════════════
// §3.1 — Output Format Enum
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Output formats for CI/CD workflow results.
/// </summary>
/// <remarks>
/// <para>
/// Determines how the <see cref="CIExecutionResult"/> is serialized for
/// consumption by various CI/CD tools and reporting systems.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §3.1
/// </para>
/// </remarks>
public enum OutputFormat
{
    /// <summary>JSON format — universal, machine-readable.</summary>
    Json,

    /// <summary>XML format — legacy CI system compatibility.</summary>
    Xml,

    /// <summary>JUnit XML format — standard CI test reporting (GitHub Actions, Jenkins, etc.).</summary>
    Junit,

    /// <summary>Markdown format — PR comments and human-readable reports.</summary>
    Markdown,

    /// <summary>HTML format — rich report generation.</summary>
    Html,

    /// <summary>SARIF 2.1.0 JSON — GitHub/GitLab code scanning integration.</summary>
    SarifJson
}

// ═══════════════════════════════════════════════════════════════════════
// §4.2 — Log Format Enum
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Formats for execution log output.
/// </summary>
/// <remarks>
/// <b>Specification:</b> LCS-DES-077-KG-j §4.2
/// </remarks>
public enum LogFormat
{
    /// <summary>Plain text — one log entry per line.</summary>
    Text,

    /// <summary>JSON array — structured log entries.</summary>
    Json,

    /// <summary>Markdown — fenced code block containing log entries.</summary>
    Markdown
}

// ═══════════════════════════════════════════════════════════════════════
// §4.1 — Exit Code Constants
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Standard exit codes for CI/CD workflow execution results.
/// </summary>
/// <remarks>
/// <para>
/// These codes follow Unix conventions and are compatible with all major
/// CI/CD systems (GitHub Actions, GitLab CI, Jenkins, Azure Pipelines, CircleCI).
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §4.1, §6
/// </para>
/// </remarks>
public static class ExitCode
{
    /// <summary>All validations passed successfully. Exit code: 0.</summary>
    public const int Success = 0;

    /// <summary>One or more validation failures detected. Exit code: 1.</summary>
    public const int ValidationFailed = 1;

    /// <summary>Invalid input parameters (bad workspace, workflow, or document). Exit code: 2.</summary>
    public const int InvalidInput = 2;

    /// <summary>Unexpected execution error. Exit code: 3.</summary>
    public const int ExecutionFailed = 3;

    /// <summary>Workflow execution exceeded the configured timeout. Exit code: 124.</summary>
    public const int Timeout = 124;

    /// <summary>Fatal, unrecoverable error. Exit code: 127.</summary>
    public const int FatalError = 127;
}

// ═══════════════════════════════════════════════════════════════════════
// §2.1 — CI Execution Status
// ═══════════════════════════════════════════════════════════════════════

/// <summary>
/// Status of a CI/CD workflow execution.
/// </summary>
/// <remarks>
/// <para>
/// Tracks the lifecycle of an execution from submission through completion.
/// Used by <see cref="ICIPipelineService.GetExecutionStatusAsync"/> to report
/// current state to polling CI/CD clients.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §2.1
/// </para>
/// </remarks>
public enum CIExecutionStatus
{
    /// <summary>Execution has been submitted but not yet started.</summary>
    Pending,

    /// <summary>Execution is currently in progress.</summary>
    Running,

    /// <summary>Execution completed successfully (exit code 0).</summary>
    Completed,

    /// <summary>Execution completed with validation failures (exit code 1).</summary>
    Failed,

    /// <summary>Execution was cancelled by the user or system.</summary>
    Cancelled,

    /// <summary>Execution exceeded the configured timeout (exit code 124).</summary>
    TimedOut
}
