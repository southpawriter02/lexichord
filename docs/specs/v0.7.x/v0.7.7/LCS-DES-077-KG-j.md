# LCS-DES-077-KG-j: CI/CD Integration

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-077-KG-j |
| **System Breakdown** | LCS-SBD-077-KG |
| **Version** | v0.7.7 |
| **Codename** | CI/CD Integration (CKVS Phase 4d) |
| **Estimated Hours** | 5 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **CI/CD Integration** module provides CLI and API interfaces for headless workflow execution in continuous integration/deployment pipelines. This enables validation workflows to run automatically during build processes, quality gates, and deployment workflows without user interaction.

### 1.2 Key Responsibilities

- Provide CLI command for workflow execution
- Support environment-based configuration
- Enable API endpoint for headless execution
- Handle exit codes for CI/CD integration
- Support workflow parameter injection
- Log results in CI/CD-friendly formats
- Support webhook triggers for pipeline events

### 1.3 Module Location

```
src/
  Lexichord.Cli/
    Commands/
      WorkflowCommand.cs
      ValidateCommand.cs
      WorkflowStatusCommand.cs
  Lexichord.Workflows/
    Validation/
      CI/
        ICIPipelineService.cs
        CIPipelineService.cs
        WorkflowExecutionRequest.cs
        CIExecutionResult.cs
```

---

## 2. Interface Definitions

### 2.1 CI Pipeline Service Interface

```csharp
namespace Lexichord.Workflows.Validation.CI;

/// <summary>
/// Service for executing workflows in CI/CD pipelines.
/// </summary>
public interface ICIPipelineService
{
    /// <summary>
    /// Executes workflow in headless mode.
    /// </summary>
    Task<CIExecutionResult> ExecuteWorkflowAsync(
        CIWorkflowRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets workflow execution status.
    /// </summary>
    Task<WorkflowExecutionStatus> GetExecutionStatusAsync(
        string executionId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancels running execution.
    /// </summary>
    Task CancelExecutionAsync(
        string executionId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets execution logs.
    /// </summary>
    Task<string> GetExecutionLogsAsync(
        string executionId,
        LogFormat format = LogFormat.Text,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 CI Workflow Request

```csharp
/// <summary>
/// Request to execute workflow in CI/CD context.
/// </summary>
public record CIWorkflowRequest
{
    /// <summary>Workspace ID or key.</summary>
    public required string WorkspaceId { get; init; }

    /// <summary>Workflow ID to execute.</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Document ID or path to validate.</summary>
    public string? DocumentId { get; init; }

    /// <summary>Path to document file (for file-based execution).</summary>
    public string? DocumentPath { get; init; }

    /// <summary>Workspace API key for authentication.</summary>
    public string? ApiKey { get; init; }

    /// <summary>Base URL of Lexichord instance.</summary>
    public string? BaseUrl { get; init; } = "http://localhost:5000";

    /// <summary>Timeout in seconds for workflow execution.</summary>
    public int TimeoutSeconds { get; init; } = 300;

    /// <summary>Whether to fail on warnings.</summary>
    public bool FailOnWarnings { get; init; } = false;

    /// <summary>Maximum allowed warnings before failure.</summary>
    public int? MaxWarnings { get; init; }

    /// <summary>Whether to halt on first error.</summary>
    public bool HaltOnFirstError { get; init; } = false;

    /// <summary>Output format for results.</summary>
    public OutputFormat OutputFormat { get; init; } = OutputFormat.Json;

    /// <summary>Parameters to pass to workflow.</summary>
    public IReadOnlyDictionary<string, string>? Parameters { get; init; }

    /// <summary>CI system metadata.</summary>
    public CISystemMetadata? CIMetadata { get; init; }

    /// <summary>Email for notifications on completion.</summary>
    public string? NotificationEmail { get; init; }

    /// <summary>Whether to wait for completion.</summary>
    public bool WaitForCompletion { get; init; } = true;
}

public enum OutputFormat
{
    Json,
    Xml,
    Junit,
    Markdown,
    Html,
    SarifJson
}

public record CISystemMetadata
{
    /// <summary>CI system name.</summary>
    public string? System { get; init; }

    /// <summary>Build/run ID.</summary>
    public string? BuildId { get; init; }

    /// <summary>Build/run URL.</summary>
    public string? BuildUrl { get; init; }

    /// <summary>Git commit SHA.</summary>
    public string? CommitSha { get; init; }

    /// <summary>Git branch.</summary>
    public string? Branch { get; init; }

    /// <summary>Pull request number (if applicable).</summary>
    public int? PullRequestNumber { get; init; }

    /// <summary>Repository URL.</summary>
    public string? RepositoryUrl { get; init; }
}
```

### 3.2 CI Execution Result

```csharp
/// <summary>
/// Result of CI/CD workflow execution.
/// </summary>
public record CIExecutionResult
{
    /// <summary>Execution ID.</summary>
    public required string ExecutionId { get; init; }

    /// <summary>Workflow ID that executed.</summary>
    public required string WorkflowId { get; init; }

    /// <summary>Overall success status.</summary>
    public required bool Success { get; init; }

    /// <summary>Exit code for CI/CD system.</summary>
    public int ExitCode { get; init; }

    /// <summary>Total execution time in seconds.</summary>
    public int ExecutionTimeSeconds { get; init; }

    /// <summary>Validation summary.</summary>
    public ValidationSummary ValidationSummary { get; init; }

    /// <summary>Step results.</summary>
    public IReadOnlyList<StepResult> StepResults { get; init; } = [];

    /// <summary>Overall message.</summary>
    public string? Message { get; init; }

    /// <summary>Detailed error message (if failed).</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Raw output from workflow.</summary>
    public string? RawOutput { get; init; }

    /// <summary>Structured results.</summary>
    public IReadOnlyDictionary<string, object>? StructuredResults { get; init; }

    /// <summary>URL to view results in UI.</summary>
    public string? ResultsUrl { get; init; }

    /// <summary>Timestamp of execution.</summary>
    public DateTime ExecutedAt { get; init; }
}

public record ValidationSummary
{
    /// <summary>Total errors found.</summary>
    public int ErrorCount { get; init; }

    /// <summary>Total warnings found.</summary>
    public int WarningCount { get; init; }

    /// <summary>Documents validated.</summary>
    public int DocumentsValidated { get; init; }

    /// <summary>Documents passed.</summary>
    public int DocumentsPassed { get; init; }

    /// <summary>Documents failed.</summary>
    public int DocumentsFailed { get; init; }

    /// <summary>Pass rate percentage.</summary>
    public decimal PassRate { get; init; }
}

public record StepResult
{
    /// <summary>Step ID.</summary>
    public required string StepId { get; init; }

    /// <summary>Step name.</summary>
    public string? Name { get; init; }

    /// <summary>Step success.</summary>
    public bool Success { get; init; }

    /// <summary>Execution time in milliseconds.</summary>
    public long ExecutionTimeMs { get; init; }

    /// <summary>Errors found in step.</summary>
    public IReadOnlyList<string> Errors { get; init; } = [];

    /// <summary>Warnings found in step.</summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];

    /// <summary>Step output message.</summary>
    public string? Message { get; init; }
}
```

---

## 4. Implementation

### 4.1 CLI Command: lexichord-validate

```csharp
[Command("validate")]
public class ValidateCommand : AsyncCommand<ValidateCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandArgument(0, "[WORKFLOW]")]
        [Description("Workflow ID to execute (e.g., on-save-validation)")]
        public string WorkflowId { get; set; }

        [CommandOption("-w|--workspace <WORKSPACE>")]
        [Description("Workspace ID or API key")]
        public string Workspace { get; set; }

        [CommandOption("-d|--document <DOCUMENT>")]
        [Description("Document ID or file path to validate")]
        public string Document { get; set; }

        [CommandOption("-u|--url <URL>")]
        [Description("Lexichord instance URL")]
        public string Url { get; set; } = "http://localhost:5000";

        [CommandOption("-t|--timeout <SECONDS>")]
        [Description("Timeout in seconds")]
        public int Timeout { get; set; } = 300;

        [CommandOption("-o|--output <FORMAT>")]
        [Description("Output format: json, xml, junit, markdown, html, sarif")]
        public string OutputFormat { get; set; } = "json";

        [CommandOption("--fail-on-warnings")]
        [Description("Fail if any warnings detected")]
        public bool FailOnWarnings { get; set; }

        [CommandOption("--max-warnings <COUNT>")]
        [Description("Maximum warnings allowed before failure")]
        public int? MaxWarnings { get; set; }

        [CommandOption("--halt-on-error")]
        [Description("Stop after first error")]
        public bool HaltOnError { get; set; }

        [CommandOption("--ci-system <SYSTEM>")]
        [Description("CI system: github, gitlab, azure, jenkins, circleci")]
        public string CISystem { get; set; }

        [CommandOption("--build-id <ID>")]
        [Description("Build/run ID from CI system")]
        public string BuildId { get; set; }

        [CommandOption("--commit <SHA>")]
        [Description("Git commit SHA")]
        public string CommitSha { get; set; }

        [CommandOption("--branch <BRANCH>")]
        [Description("Git branch name")]
        public string Branch { get; set; }

        [CommandOption("--verbose")]
        [Description("Verbose output")]
        public bool Verbose { get; set; }

        [CommandOption("--json-output <FILE>")]
        [Description("Write structured results to file")]
        public string JsonOutputFile { get; set; }
    }

    private readonly ICIPipelineService _pipelineService;
    private readonly ILogger<ValidateCommand> _logger;

    public ValidateCommand(
        ICIPipelineService pipelineService,
        ILogger<ValidateCommand> logger)
    {
        _pipelineService = pipelineService;
        _logger = logger;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        try
        {
            // Validate inputs
            if (string.IsNullOrWhiteSpace(settings.WorkflowId))
            {
                AnsiConsole.MarkupLine("[red]Error: Workflow ID is required[/]");
                return ExitCode.InvalidInput;
            }

            if (string.IsNullOrWhiteSpace(settings.Workspace))
            {
                AnsiConsole.MarkupLine("[red]Error: Workspace ID or API key is required[/]");
                return ExitCode.InvalidInput;
            }

            // Build request
            var request = new CIWorkflowRequest
            {
                WorkspaceId = settings.Workspace,
                WorkflowId = settings.WorkflowId,
                DocumentId = settings.Document,
                DocumentPath = System.IO.File.Exists(settings.Document) ? settings.Document : null,
                BaseUrl = settings.Url,
                TimeoutSeconds = settings.Timeout,
                FailOnWarnings = settings.FailOnWarnings,
                MaxWarnings = settings.MaxWarnings,
                HaltOnFirstError = settings.HaltOnError,
                OutputFormat = Enum.Parse<OutputFormat>(settings.OutputFormat, ignoreCase: true),
                CIMetadata = new CISystemMetadata
                {
                    System = settings.CISystem,
                    BuildId = settings.BuildId,
                    CommitSha = settings.CommitSha,
                    Branch = settings.Branch
                }
            };

            // Show status
            var status = AnsiConsole.Status();
            status.Start("Executing workflow...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.Dots);

                try
                {
                    var result = await _pipelineService.ExecuteWorkflowAsync(request);

                    // Output results
                    OutputResults(settings, result);

                    // Write to file if requested
                    if (!string.IsNullOrWhiteSpace(settings.JsonOutputFile))
                    {
                        await System.IO.File.WriteAllTextAsync(
                            settings.JsonOutputFile,
                            JsonConvert.SerializeObject(result, Formatting.Indented));
                    }

                    return result.ExitCode;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                    if (settings.Verbose)
                    {
                        AnsiConsole.WriteException(ex);
                    }
                    return ExitCode.ExecutionFailed;
                }
            });

            return status.Status;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Validate command failed");
            AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message}[/]");
            return ExitCode.FatalError;
        }
    }

    private void OutputResults(Settings settings, CIExecutionResult result)
    {
        switch (settings.OutputFormat.ToLower())
        {
            case "json":
                OutputJson(result);
                break;
            case "junit":
                OutputJunit(result);
                break;
            case "markdown":
                OutputMarkdown(result);
                break;
            case "sarif":
                OutputSarif(result);
                break;
            default:
                OutputHuman(result);
                break;
        }
    }

    private void OutputHuman(CIExecutionResult result)
    {
        var table = new Table();
        table.AddColumn("Metric");
        table.AddColumn("Value");

        var status = result.Success ? "[green]PASS[/]" : "[red]FAIL[/]";
        table.AddRow("Status", status);
        table.AddRow("Errors", result.ValidationSummary.ErrorCount.ToString());
        table.AddRow("Warnings", result.ValidationSummary.WarningCount.ToString());
        table.AddRow("Documents Passed", result.ValidationSummary.DocumentsPassed.ToString());
        table.AddRow("Documents Failed", result.ValidationSummary.DocumentsFailed.ToString());
        table.AddRow("Pass Rate", $"{result.ValidationSummary.PassRate:P1}");
        table.AddRow("Time", $"{result.ExecutionTimeSeconds}s");

        AnsiConsole.Write(table);

        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            AnsiConsole.MarkupLine($"\n{result.Message}");
        }
    }

    private void OutputJson(CIExecutionResult result)
    {
        AnsiConsole.WriteLine(JsonConvert.SerializeObject(result, Formatting.Indented));
    }

    private void OutputJunit(CIExecutionResult result)
    {
        // Convert to JUnit XML format for CI systems
        var xml = new XDocument(
            new XElement("testsuites",
                new XElement("testsuite",
                    new XAttribute("name", result.WorkflowId),
                    new XAttribute("tests", result.ValidationSummary.DocumentsValidated),
                    new XAttribute("failures", result.ValidationSummary.DocumentsFailed),
                    new XAttribute("time", result.ExecutionTimeSeconds),
                    result.StepResults.Select(sr =>
                        new XElement("testcase",
                            new XAttribute("name", sr.StepId),
                            new XAttribute("time", sr.ExecutionTimeMs / 1000.0),
                            sr.Success ? null : new XElement("failure",
                                new XAttribute("message", sr.Message ?? "Unknown error"),
                                string.Join(Environment.NewLine, sr.Errors)))))));

        AnsiConsole.WriteLine(xml.ToString());
    }

    private void OutputMarkdown(CIExecutionResult result)
    {
        var md = new StringBuilder();
        md.AppendLine($"# Validation Results\n");
        md.AppendLine($"**Status:** {(result.Success ? "✅ PASS" : "❌ FAIL")}");
        md.AppendLine($"**Errors:** {result.ValidationSummary.ErrorCount}");
        md.AppendLine($"**Warnings:** {result.ValidationSummary.WarningCount}");
        md.AppendLine($"**Pass Rate:** {result.ValidationSummary.PassRate:P1}");
        md.AppendLine($"**Time:** {result.ExecutionTimeSeconds}s\n");

        if (result.StepResults.Any())
        {
            md.AppendLine("## Step Results\n");
            foreach (var step in result.StepResults)
            {
                md.AppendLine($"### {step.Name}");
                md.AppendLine($"Status: {(step.Success ? "✅" : "❌")}");
                if (step.Errors.Any())
                {
                    md.AppendLine("Errors:");
                    foreach (var error in step.Errors)
                    {
                        md.AppendLine($"- {error}");
                    }
                }
                md.AppendLine();
            }
        }

        AnsiConsole.WriteLine(md.ToString());
    }

    private void OutputSarif(CIExecutionResult result)
    {
        // Convert to SARIF format for GitHub/GitLab code scanning
        var sarif = new
        {
            version = "2.1.0",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Lexichord",
                            version = "0.7.7"
                        }
                    },
                    results = result.StepResults
                        .SelectMany(sr => sr.Errors.Select((e, i) => new
                        {
                            ruleId = sr.StepId,
                            message = new { text = e },
                            level = "error"
                        }))
                        .Cast<object>()
                        .ToList()
                }
            }
        };

        AnsiConsole.WriteLine(JsonConvert.SerializeObject(sarif, Formatting.Indented));
    }
}

public static class ExitCode
{
    public const int Success = 0;
    public const int ValidationFailed = 1;
    public const int InvalidInput = 2;
    public const int ExecutionFailed = 3;
    public const int Timeout = 124;
    public const int FatalError = 127;
}
```

### 4.2 CI Pipeline Service Implementation

```csharp
public class CIPipelineService : ICIPipelineService
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IWorkflowRegistry _registry;
    private readonly IDocumentService _documentService;
    private readonly ILogger<CIPipelineService> _logger;

    public CIPipelineService(
        IWorkflowEngine workflowEngine,
        IWorkflowRegistry registry,
        IDocumentService documentService,
        ILogger<CIPipelineService> logger)
    {
        _workflowEngine = workflowEngine;
        _registry = registry;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<CIExecutionResult> ExecuteWorkflowAsync(
        CIWorkflowRequest request,
        CancellationToken ct = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;

        try
        {
            _logger.LogInformation(
                "CI: Starting workflow execution {ExecutionId}",
                executionId);

            // Load document
            Document document = null;
            if (!string.IsNullOrWhiteSpace(request.DocumentId))
            {
                document = await _documentService.GetDocumentAsync(request.DocumentId, ct);
            }
            else if (!string.IsNullOrWhiteSpace(request.DocumentPath))
            {
                var content = await System.IO.File.ReadAllTextAsync(request.DocumentPath, ct);
                document = new Document
                {
                    Id = Guid.NewGuid(),
                    Title = System.IO.Path.GetFileName(request.DocumentPath),
                    Content = content
                };
            }

            if (document == null)
            {
                return new CIExecutionResult
                {
                    ExecutionId = executionId,
                    WorkflowId = request.WorkflowId,
                    Success = false,
                    ExitCode = ExitCode.InvalidInput,
                    Message = "No document specified or found",
                    ExecutedAt = startTime
                };
            }

            // Load workflow
            var workflow = await _registry.GetWorkflowAsync(request.WorkflowId, ct);

            // Execute workflow
            using var cts = new CancellationTokenSource(
                TimeSpan.FromSeconds(request.TimeoutSeconds));

            var workflowContext = new WorkflowContext
            {
                Id = Guid.Parse(executionId),
                WorkflowId = request.WorkflowId,
                WorkspaceId = Guid.Parse(request.WorkspaceId),
                CurrentDocument = document,
                Metadata = new Dictionary<string, object>
                {
                    ["ciSystem"] = request.CIMetadata?.System,
                    ["buildId"] = request.CIMetadata?.BuildId,
                    ["commitSha"] = request.CIMetadata?.CommitSha
                }
            };

            var result = await _workflowEngine.ExecuteAsync(workflowContext, cts.Token);

            // Build result
            var duration = (DateTime.UtcNow - startTime).TotalSeconds;

            return new CIExecutionResult
            {
                ExecutionId = executionId,
                WorkflowId = request.WorkflowId,
                Success = result.Success,
                ExitCode = DetermineExitCode(result, request),
                ExecutionTimeSeconds = (int)duration,
                ValidationSummary = new ValidationSummary
                {
                    ErrorCount = result.ErrorCount,
                    WarningCount = result.WarningCount,
                    DocumentsValidated = 1,
                    DocumentsPassed = result.Success ? 1 : 0,
                    DocumentsFailed = result.Success ? 0 : 1,
                    PassRate = result.Success ? 100 : 0
                },
                StepResults = BuildStepResults(result),
                Message = result.Message,
                ExecutedAt = startTime
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("CI: Workflow execution timed out {ExecutionId}", executionId);

            return new CIExecutionResult
            {
                ExecutionId = executionId,
                WorkflowId = request.WorkflowId,
                Success = false,
                ExitCode = ExitCode.Timeout,
                Message = $"Workflow timed out after {request.TimeoutSeconds} seconds",
                ExecutedAt = startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CI: Workflow execution failed {ExecutionId}", executionId);

            return new CIExecutionResult
            {
                ExecutionId = executionId,
                WorkflowId = request.WorkflowId,
                Success = false,
                ExitCode = ExitCode.ExecutionFailed,
                ErrorMessage = ex.Message,
                ExecutedAt = startTime
            };
        }
    }

    public async Task<WorkflowExecutionStatus> GetExecutionStatusAsync(
        string executionId,
        CancellationToken ct = default)
    {
        // Query execution status from storage
        return await _workflowEngine.GetExecutionStatusAsync(executionId, ct);
    }

    public async Task CancelExecutionAsync(
        string executionId,
        CancellationToken ct = default)
    {
        await _workflowEngine.CancelExecutionAsync(executionId, ct);
    }

    public async Task<string> GetExecutionLogsAsync(
        string executionId,
        LogFormat format = LogFormat.Text,
        CancellationToken ct = default)
    {
        var logs = await _workflowEngine.GetExecutionLogsAsync(executionId, ct);

        return format switch
        {
            LogFormat.Json => JsonConvert.SerializeObject(logs, Formatting.Indented),
            LogFormat.Markdown => FormatAsMarkdown(logs),
            _ => string.Join(Environment.NewLine, logs)
        };
    }

    private int DetermineExitCode(
        WorkflowResult result,
        CIWorkflowRequest request)
    {
        if (result.Success)
            return ExitCode.Success;

        if (request.FailOnWarnings && result.WarningCount > 0)
            return ExitCode.ValidationFailed;

        if (request.MaxWarnings.HasValue && result.WarningCount > request.MaxWarnings)
            return ExitCode.ValidationFailed;

        if (result.ErrorCount > 0)
            return ExitCode.ValidationFailed;

        return ExitCode.Success;
    }

    private List<StepResult> BuildStepResults(WorkflowResult workflowResult)
    {
        return workflowResult.StepResults
            .Select(sr => new StepResult
            {
                StepId = sr.StepId,
                Name = sr.Message,
                Success = sr.Success,
                ExecutionTimeMs = sr.ExecutionTimeMs,
                Message = sr.Message
            })
            .ToList();
    }

    private string FormatAsMarkdown(IReadOnlyList<string> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("```");
        foreach (var log in logs)
        {
            sb.AppendLine(log);
        }
        sb.AppendLine("```");
        return sb.ToString();
    }
}

public enum LogFormat
{
    Text,
    Json,
    Markdown
}
```

---

## 5. CI/CD Integration Flow

```
[CI/CD Pipeline Trigger]
        |
        v
[lexichord validate <workflow>]
        |
        +---> [Parse CLI Arguments]
        |
        +---> [Build CIWorkflowRequest]
        |
        +---> [Call ICIPipelineService]
        |
        +---> [Load Document]
        |
        +---> [Load Workflow]
        |
        +---> [Execute via IWorkflowEngine]
        |
        +---> [Collect Results]
        |
        +---> [Output in CI Format]
        |
        +---> [Return Exit Code]
        |
        v
[CI/CD System Evaluates Exit Code]
```

---

## 6. Error Handling

| Error | Exit Code | Handling |
| :---- | :--------: | :---------- |
| Validation failed | 1 | Continue in CI, mark step failed |
| Invalid input | 2 | Fail immediately |
| Execution error | 3 | Fail immediately |
| Timeout | 124 | Fail, may retry |
| Fatal error | 127 | Fail immediately |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `CLI_ValidateCommand_Success` | CLI executes successfully |
| `CLI_ValidateCommand_FailOnError` | CLI fails on validation error |
| `CLI_ValidateCommand_Timeout` | CLI handles timeout |
| `CLI_OutputJson` | JSON output works |
| `CLI_OutputJunit` | JUnit output works |
| `CLI_OutputSarif` | SARIF output works |
| `CIPipelineService_Execute` | Service executes workflow |
| `CIPipelineService_ExitCode` | Exit codes correct |
| `CIPipelineService_Integration` | Integrates with GitHub/GitLab |

---

## 8. Performance Considerations

| Aspect | Target |
| :------ | :------ |
| CLI startup | < 1s |
| Request parsing | < 100ms |
| Workflow execution | Per workflow |
| Output generation | < 500ms |
| Total CLI time | < TimeoutSeconds |

---

## 9. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | On-save workflows only |
| Teams | All workflows via CLI |
| Enterprise | Full + custom + webhooks |

---

## 10. GitHub Actions Integration Example

```yaml
name: Document Validation

on: [push, pull_request]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2

      - name: Setup Lexichord CLI
        run: |
          wget https://releases.lexichord.io/lexichord-cli-latest-linux-x64.tar.gz
          tar -xzf lexichord-cli-latest-linux-x64.tar.gz
          chmod +x lexichord

      - name: Run Pre-Publish Validation
        run: |
          ./lexichord validate pre-publish-gate \
            --workspace ${{ secrets.LEXICHORD_WORKSPACE }} \
            --document ./docs/api.md \
            --output sarif \
            --json-output validation-results.json

      - name: Upload SARIF to GitHub
        uses: github/codeql-action/upload-sarif@v1
        with:
          sarif_file: validation-results.json

      - name: Comment PR with Results
        if: github.event_name == 'pull_request'
        uses: actions/github-script@v6
        with:
          script: |
            const fs = require('fs');
            const results = JSON.parse(fs.readFileSync('validation-results.json', 'utf8'));
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## Validation Results\n\n**Status:** ${results.success ? '✅ Pass' : '❌ Fail'}\n**Errors:** ${results.validationSummary.errorCount}`
            });
```

---

## 11. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
