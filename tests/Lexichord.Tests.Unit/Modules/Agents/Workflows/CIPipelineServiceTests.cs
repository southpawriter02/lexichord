// -----------------------------------------------------------------------
// <copyright file="CIPipelineServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.CI;
using Lexichord.Modules.Agents.Workflows.Validation.Templates;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="CIPipelineService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests workflow execution, exit code determination, timeout handling,
/// cancellation, status queries, and log retrieval.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §7
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7j")]
public class CIPipelineServiceTests
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly Mock<IValidationWorkflowRegistry> _registryMock;
    private readonly CIPipelineService _sut;

    // ── Constructor ─────────────────────────────────────────────────────

    public CIPipelineServiceTests()
    {
        _registryMock = new Mock<IValidationWorkflowRegistry>();
        var loggerMock = new Mock<ILogger<CIPipelineService>>();

        _sut = new CIPipelineService(
            _registryMock.Object,
            loggerMock.Object);
    }

    // ── ExecuteWorkflowAsync Tests ──────────────────────────────────────

    /// <summary>
    /// Verifies that a successful workflow execution returns exit code 0.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_Success_ReturnsExitCodeZero()
    {
        // Arrange
        var workflow = CreateWorkflow("test-workflow", "Test Workflow", 2);
        _registryMock
            .Setup(r => r.GetWorkflowAsync("test-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var request = CreateRequest("test-workflow");

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(ExitCode.Success);
        result.WorkflowId.Should().Be("test-workflow");
        result.ExecutionId.Should().NotBeNullOrWhiteSpace();
        result.StepResults.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that an empty workflow ID produces exit code 2 (InvalidInput).
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_EmptyWorkflowId_ReturnsInvalidInput()
    {
        // Arrange
        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = ""
        };

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(ExitCode.InvalidInput);
    }

    /// <summary>
    /// Verifies that an empty workspace ID produces exit code 2 (InvalidInput).
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_EmptyWorkspaceId_ReturnsInvalidInput()
    {
        // Arrange
        var request = new CIWorkflowRequest
        {
            WorkspaceId = "",
            WorkflowId = "test-workflow"
        };

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(ExitCode.InvalidInput);
    }

    /// <summary>
    /// Verifies that a missing workflow produces exit code 2 (InvalidInput).
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_WorkflowNotFound_ReturnsInvalidInput()
    {
        // Arrange
        _registryMock
            .Setup(r => r.GetWorkflowAsync("missing", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Workflow not found"));

        var request = CreateRequest("missing");

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(ExitCode.InvalidInput);
        result.ErrorMessage.Should().Contain("not found");
    }

    /// <summary>
    /// Verifies that a timeout produces exit code 124.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_Timeout_ReturnsTimeoutExitCode()
    {
        // Arrange — workflow that takes longer than the 1-second timeout
        _registryMock
            .Setup(r => r.GetWorkflowAsync("slow-workflow", It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken ct) =>
            {
                await Task.Delay(5000, ct); // Will be cancelled by timeout
                return CreateWorkflow("slow-workflow", "Slow", 1);
            });

        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = "slow-workflow",
            TimeoutSeconds = 1 // Very short timeout
        };

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(ExitCode.Timeout);
    }

    /// <summary>
    /// Verifies that an unhandled exception produces exit code 3 (ExecutionFailed).
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_UnhandledException_ReturnsExecutionFailed()
    {
        // Arrange
        _registryMock
            .Setup(r => r.GetWorkflowAsync("bad-workflow", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var request = CreateRequest("bad-workflow");

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(ExitCode.ExecutionFailed);
        result.ErrorMessage.Should().Contain("Unexpected error");
    }

    /// <summary>
    /// Verifies that the validation summary is populated after execution.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_Success_PopulatesValidationSummary()
    {
        // Arrange
        var workflow = CreateWorkflow("summary-test", "Summary Test", 3);
        _registryMock
            .Setup(r => r.GetWorkflowAsync("summary-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var request = CreateRequest("summary-test");

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.ValidationSummary.Should().NotBeNull();
        result.ValidationSummary!.DocumentsValidated.Should().Be(1);
        result.ValidationSummary.DocumentsPassed.Should().Be(1);
        result.ValidationSummary.DocumentsFailed.Should().Be(0);
        result.ValidationSummary.PassRate.Should().Be(100m);
    }

    /// <summary>
    /// Verifies that CI metadata is accepted and does not affect execution.
    /// </summary>
    [Fact]
    public async Task ExecuteWorkflow_WithCIMetadata_ExecutesSuccessfully()
    {
        // Arrange
        var workflow = CreateWorkflow("ci-meta-test", "CI Meta Test", 1);
        _registryMock
            .Setup(r => r.GetWorkflowAsync("ci-meta-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = "ci-meta-test",
            CIMetadata = new CISystemMetadata
            {
                System = "github",
                BuildId = "12345",
                CommitSha = "abc123",
                Branch = "main",
                PullRequestNumber = 42
            }
        };

        // Act
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Assert
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(ExitCode.Success);
    }

    // ── DetermineExitCode Tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that FailOnWarnings produces exit code 1 when warnings exist.
    /// </summary>
    [Fact]
    public void DetermineExitCode_FailOnWarnings_WithWarnings_ReturnsValidationFailed()
    {
        // Arrange
        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = "test",
            FailOnWarnings = true
        };

        // Act
        var exitCode = CIPipelineService.DetermineExitCode(
            success: true, errorCount: 0, warningCount: 3, request);

        // Assert
        exitCode.Should().Be(ExitCode.ValidationFailed);
    }

    /// <summary>
    /// Verifies that MaxWarnings exceeded produces exit code 1.
    /// </summary>
    [Fact]
    public void DetermineExitCode_MaxWarningsExceeded_ReturnsValidationFailed()
    {
        // Arrange
        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = "test",
            MaxWarnings = 5
        };

        // Act
        var exitCode = CIPipelineService.DetermineExitCode(
            success: true, errorCount: 0, warningCount: 10, request);

        // Assert
        exitCode.Should().Be(ExitCode.ValidationFailed);
    }

    /// <summary>
    /// Verifies that MaxWarnings within limit returns success.
    /// </summary>
    [Fact]
    public void DetermineExitCode_MaxWarningsWithinLimit_ReturnsSuccess()
    {
        // Arrange
        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = "test",
            MaxWarnings = 10
        };

        // Act
        var exitCode = CIPipelineService.DetermineExitCode(
            success: true, errorCount: 0, warningCount: 5, request);

        // Assert
        exitCode.Should().Be(ExitCode.Success);
    }

    // ── GetExecutionStatusAsync Tests ───────────────────────────────────

    /// <summary>
    /// Verifies that status query for unknown execution throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task GetExecutionStatus_UnknownId_ThrowsKeyNotFound()
    {
        // Act & Assert
        var act = () => _sut.GetExecutionStatusAsync("unknown-id");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── CancelExecutionAsync Tests ─────────────────────────────────────

    /// <summary>
    /// Verifies that cancel for unknown execution throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task CancelExecution_UnknownId_ThrowsKeyNotFound()
    {
        // Act & Assert
        var act = () => _sut.CancelExecutionAsync("unknown-id");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    // ── GetExecutionLogsAsync Tests ─────────────────────────────────────

    /// <summary>
    /// Verifies that log retrieval for unknown execution throws KeyNotFoundException.
    /// </summary>
    [Fact]
    public async Task GetExecutionLogs_UnknownId_ThrowsKeyNotFound()
    {
        // Act & Assert
        var act = () => _sut.GetExecutionLogsAsync("unknown-id");
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    /// <summary>
    /// Verifies that logs are populated after a successful execution.
    /// </summary>
    [Fact]
    public async Task GetExecutionLogs_AfterExecution_ReturnsLogs()
    {
        // Arrange
        var workflow = CreateWorkflow("log-test", "Log Test", 2);
        _registryMock
            .Setup(r => r.GetWorkflowAsync("log-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var request = CreateRequest("log-test");
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Act
        var logs = await _sut.GetExecutionLogsAsync(result.ExecutionId, LogFormat.Text);

        // Assert
        logs.Should().NotBeNullOrWhiteSpace();
        logs.Should().Contain("Executing step:");
    }

    /// <summary>
    /// Verifies that JSON log format returns valid JSON.
    /// </summary>
    [Fact]
    public async Task GetExecutionLogs_JsonFormat_ReturnsValidJson()
    {
        // Arrange
        var workflow = CreateWorkflow("json-log-test", "JSON Log Test", 1);
        _registryMock
            .Setup(r => r.GetWorkflowAsync("json-log-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var request = CreateRequest("json-log-test");
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Act
        var logs = await _sut.GetExecutionLogsAsync(result.ExecutionId, LogFormat.Json);

        // Assert
        logs.Should().StartWith("[");
        logs.Should().EndWith("]");
    }

    /// <summary>
    /// Verifies that Markdown log format wraps in a code block.
    /// </summary>
    [Fact]
    public async Task GetExecutionLogs_MarkdownFormat_ContainsCodeBlock()
    {
        // Arrange
        var workflow = CreateWorkflow("md-log-test", "MD Log Test", 1);
        _registryMock
            .Setup(r => r.GetWorkflowAsync("md-log-test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(workflow);

        var request = CreateRequest("md-log-test");
        var result = await _sut.ExecuteWorkflowAsync(request);

        // Act
        var logs = await _sut.GetExecutionLogsAsync(result.ExecutionId, LogFormat.Markdown);

        // Assert
        logs.Should().StartWith("```");
        logs.TrimEnd().Should().EndWith("```");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static CIWorkflowRequest CreateRequest(string workflowId)
        => new()
        {
            WorkspaceId = "workspace-1",
            WorkflowId = workflowId
        };

    private static ValidationWorkflowDefinition CreateWorkflow(
        string id, string name, int stepCount)
    {
        var steps = Enumerable.Range(1, stepCount)
            .Select(i => new ValidationWorkflowStepDef
            {
                Id = $"step-{i}",
                Name = $"Step {i}",
                Type = "schema",
                Order = i
            })
            .ToList();

        return new ValidationWorkflowDefinition
        {
            Id = id,
            Name = name,
            Version = "1.0.0",
            Steps = steps,
            LicenseRequirement = new ValidationWorkflowLicenseRequirement
            {
                WriterPro = true,
                Teams = true,
                Enterprise = true
            }
        };
    }
}
