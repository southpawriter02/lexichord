// -----------------------------------------------------------------------
// <copyright file="CIDataTypeTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.CI;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for CI/CD data types: enums, records, and constants.
/// </summary>
/// <remarks>
/// <para>
/// Validates enum value counts, constant values, record defaults,
/// and nullable property behavior for all CI/CD data types.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §3, §6
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7j")]
public class CIDataTypeTests
{
    // ── OutputFormat Enum Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that OutputFormat has exactly 6 values per spec §3.1.
    /// </summary>
    [Fact]
    public void OutputFormat_HasSixValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<OutputFormat>();

        // Assert
        values.Should().HaveCount(6);
    }

    /// <summary>
    /// Verifies that OutputFormat contains all expected values.
    /// </summary>
    [Theory]
    [InlineData(OutputFormat.Json)]
    [InlineData(OutputFormat.Xml)]
    [InlineData(OutputFormat.Junit)]
    [InlineData(OutputFormat.Markdown)]
    [InlineData(OutputFormat.Html)]
    [InlineData(OutputFormat.SarifJson)]
    public void OutputFormat_ContainsExpectedValue(OutputFormat format)
    {
        // Assert
        Enum.IsDefined(format).Should().BeTrue();
    }

    // ── LogFormat Enum Tests ────────────────────────────────────────────

    /// <summary>
    /// Verifies that LogFormat has exactly 3 values per spec §4.2.
    /// </summary>
    [Fact]
    public void LogFormat_HasThreeValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<LogFormat>();

        // Assert
        values.Should().HaveCount(3);
    }

    // ── CIExecutionStatus Enum Tests ────────────────────────────────────

    /// <summary>
    /// Verifies that CIExecutionStatus has exactly 6 values per spec §2.1.
    /// </summary>
    [Fact]
    public void CIExecutionStatus_HasSixValues()
    {
        // Arrange & Act
        var values = Enum.GetValues<CIExecutionStatus>();

        // Assert
        values.Should().HaveCount(6);
    }

    // ── ExitCode Constants Tests ────────────────────────────────────────

    /// <summary>
    /// Verifies that ExitCode.Success is 0 per spec §6.
    /// </summary>
    [Fact]
    public void ExitCode_Success_IsZero()
    {
        ExitCode.Success.Should().Be(0);
    }

    /// <summary>
    /// Verifies that ExitCode.ValidationFailed is 1 per spec §6.
    /// </summary>
    [Fact]
    public void ExitCode_ValidationFailed_IsOne()
    {
        ExitCode.ValidationFailed.Should().Be(1);
    }

    /// <summary>
    /// Verifies that ExitCode.InvalidInput is 2 per spec §6.
    /// </summary>
    [Fact]
    public void ExitCode_InvalidInput_IsTwo()
    {
        ExitCode.InvalidInput.Should().Be(2);
    }

    /// <summary>
    /// Verifies that ExitCode.ExecutionFailed is 3 per spec §6.
    /// </summary>
    [Fact]
    public void ExitCode_ExecutionFailed_IsThree()
    {
        ExitCode.ExecutionFailed.Should().Be(3);
    }

    /// <summary>
    /// Verifies that ExitCode.Timeout is 124 per spec §6.
    /// </summary>
    [Fact]
    public void ExitCode_Timeout_Is124()
    {
        ExitCode.Timeout.Should().Be(124);
    }

    /// <summary>
    /// Verifies that ExitCode.FatalError is 127 per spec §6.
    /// </summary>
    [Fact]
    public void ExitCode_FatalError_Is127()
    {
        ExitCode.FatalError.Should().Be(127);
    }

    // ── CIWorkflowRequest Defaults Tests ────────────────────────────────

    /// <summary>
    /// Verifies that CIWorkflowRequest has correct default values.
    /// </summary>
    [Fact]
    public void CIWorkflowRequest_HasCorrectDefaults()
    {
        // Arrange & Act
        var request = new CIWorkflowRequest
        {
            WorkspaceId = "ws-1",
            WorkflowId = "wf-1"
        };

        // Assert
        request.BaseUrl.Should().Be("http://localhost:5000");
        request.TimeoutSeconds.Should().Be(300);
        request.FailOnWarnings.Should().BeFalse();
        request.MaxWarnings.Should().BeNull();
        request.HaltOnFirstError.Should().BeFalse();
        request.OutputFormat.Should().Be(OutputFormat.Json);
        request.WaitForCompletion.Should().BeTrue();
        request.Parameters.Should().BeNull();
        request.CIMetadata.Should().BeNull();
        request.NotificationEmail.Should().BeNull();
        request.DocumentId.Should().BeNull();
        request.DocumentPath.Should().BeNull();
        request.ApiKey.Should().BeNull();
    }

    // ── CISystemMetadata Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that CISystemMetadata allows all nullable properties.
    /// </summary>
    [Fact]
    public void CISystemMetadata_AllPropertiesNullable()
    {
        // Arrange & Act
        var metadata = new CISystemMetadata();

        // Assert
        metadata.System.Should().BeNull();
        metadata.BuildId.Should().BeNull();
        metadata.BuildUrl.Should().BeNull();
        metadata.CommitSha.Should().BeNull();
        metadata.Branch.Should().BeNull();
        metadata.PullRequestNumber.Should().BeNull();
        metadata.RepositoryUrl.Should().BeNull();
    }

    // ── CIExecutionResult Tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that CIExecutionResult has correct default collection values.
    /// </summary>
    [Fact]
    public void CIExecutionResult_HasEmptyDefaultCollections()
    {
        // Arrange & Act
        var result = new CIExecutionResult
        {
            ExecutionId = "exec-1",
            WorkflowId = "wf-1",
            Success = true
        };

        // Assert
        result.StepResults.Should().BeEmpty();
        result.ValidationSummary.Should().BeNull();
        result.StructuredResults.Should().BeNull();
        result.Message.Should().BeNull();
        result.ErrorMessage.Should().BeNull();
        result.RawOutput.Should().BeNull();
        result.ResultsUrl.Should().BeNull();
    }

    // ── CIStepResult Tests ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that CIStepResult has correct default collection values.
    /// </summary>
    [Fact]
    public void CIStepResult_HasEmptyDefaultCollections()
    {
        // Arrange & Act
        var step = new CIStepResult { StepId = "step-1" };

        // Assert
        step.Errors.Should().BeEmpty();
        step.Warnings.Should().BeEmpty();
        step.Name.Should().BeNull();
        step.Message.Should().BeNull();
        step.Success.Should().BeFalse();
        step.ExecutionTimeMs.Should().Be(0);
    }
}
