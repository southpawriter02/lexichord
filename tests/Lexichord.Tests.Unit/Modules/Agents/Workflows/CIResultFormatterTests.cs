// -----------------------------------------------------------------------
// <copyright file="CIResultFormatterTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text.Json;
using System.Xml.Linq;
using Lexichord.Modules.Agents.Workflows.Validation.CI;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="CIResultFormatter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests JSON, JUnit XML, Markdown, SARIF 2.1.0, HTML, and XML output
/// formatters with both passing and failing result scenarios.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §4.1, §7
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7j")]
public class CIResultFormatterTests
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly CIResultFormatter _sut;

    // ── Constructor ─────────────────────────────────────────────────────

    public CIResultFormatterTests()
    {
        var loggerMock = new Mock<ILogger<CIResultFormatter>>();
        _sut = new CIResultFormatter(loggerMock.Object);
    }

    // ── JSON Format Tests ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that JSON output is valid JSON containing expected fields.
    /// </summary>
    [Fact]
    public void FormatResult_Json_ProducesValidJson()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var json = _sut.FormatResult(result, OutputFormat.Json);

        // Assert
        json.Should().NotBeNullOrWhiteSpace();
        var doc = JsonDocument.Parse(json);
        doc.RootElement.GetProperty("executionId").GetString().Should().Be("exec-1");
        doc.RootElement.GetProperty("success").GetBoolean().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that JSON output uses camelCase naming.
    /// </summary>
    [Fact]
    public void FormatResult_Json_UsesCamelCase()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var json = _sut.FormatResult(result, OutputFormat.Json);

        // Assert
        json.Should().Contain("\"executionId\"");
        json.Should().Contain("\"workflowId\"");
        json.Should().NotContain("\"ExecutionId\"");
    }

    // ── JUnit XML Format Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that JUnit output is valid XML with testsuites structure.
    /// </summary>
    [Fact]
    public void FormatResult_Junit_ProducesValidXml()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var xml = _sut.FormatResult(result, OutputFormat.Junit);

        // Assert
        xml.Should().NotBeNullOrWhiteSpace();
        var doc = XDocument.Parse(xml);
        doc.Root!.Name.LocalName.Should().Be("testsuites");
        doc.Root.Element("testsuite").Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that failed steps produce failure elements in JUnit.
    /// </summary>
    [Fact]
    public void FormatResult_Junit_FailedStepsHaveFailureElements()
    {
        // Arrange
        var result = CreateFailedResult();

        // Act
        var xml = _sut.FormatResult(result, OutputFormat.Junit);

        // Assert
        var doc = XDocument.Parse(xml);
        var testCases = doc.Descendants("testcase").ToList();
        testCases.Should().HaveCount(2);

        // The second step (failed) should have a failure element
        var failedCase = testCases.First(tc =>
            tc.Attribute("name")?.Value == "step-2");
        failedCase.Element("failure").Should().NotBeNull();
    }

    // ── Markdown Format Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that Markdown output contains the header and pass status.
    /// </summary>
    [Fact]
    public void FormatResult_Markdown_ContainsHeaderAndStatus()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var md = _sut.FormatResult(result, OutputFormat.Markdown);

        // Assert
        md.Should().Contain("# Validation Results");
        md.Should().Contain("✅ PASS");
        md.Should().Contain("**Workflow:** test-workflow");
    }

    /// <summary>
    /// Verifies that failed Markdown output contains FAIL status and errors.
    /// </summary>
    [Fact]
    public void FormatResult_Markdown_FailedShowsErrors()
    {
        // Arrange
        var result = CreateFailedResult();

        // Act
        var md = _sut.FormatResult(result, OutputFormat.Markdown);

        // Assert
        md.Should().Contain("❌ FAIL");
        md.Should().Contain("**Errors:**");
        md.Should().Contain("Schema validation error");
    }

    // ── SARIF Format Tests ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that SARIF output follows 2.1.0 schema structure.
    /// </summary>
    [Fact]
    public void FormatResult_Sarif_HasCorrectVersion()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var sarif = _sut.FormatResult(result, OutputFormat.SarifJson);

        // Assert
        var doc = JsonDocument.Parse(sarif);
        doc.RootElement.GetProperty("version").GetString().Should().Be("2.1.0");
        doc.RootElement.GetProperty("runs").GetArrayLength().Should().Be(1);

        var run = doc.RootElement.GetProperty("runs")[0];
        run.GetProperty("tool").GetProperty("driver")
            .GetProperty("name").GetString().Should().Be("Lexichord");
    }

    /// <summary>
    /// Verifies that SARIF output includes error results from failed steps.
    /// </summary>
    [Fact]
    public void FormatResult_Sarif_ErrorsAppearAsResults()
    {
        // Arrange
        var result = CreateFailedResult();

        // Act
        var sarif = _sut.FormatResult(result, OutputFormat.SarifJson);

        // Assert
        var doc = JsonDocument.Parse(sarif);
        var results = doc.RootElement.GetProperty("runs")[0]
            .GetProperty("results");
        results.GetArrayLength().Should().BeGreaterThan(0);

        // At least one result should be an error
        var firstResult = results[0];
        firstResult.GetProperty("level").GetString().Should().Be("error");
    }

    // ── HTML Format Tests ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that HTML output contains DOCTYPE and basic structure.
    /// </summary>
    [Fact]
    public void FormatResult_Html_ContainsBasicStructure()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var html = _sut.FormatResult(result, OutputFormat.Html);

        // Assert
        html.Should().Contain("<!DOCTYPE html>");
        html.Should().Contain("<title>Validation Results</title>");
        html.Should().Contain("PASS");
    }

    // ── XML Format Tests ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that XML output is well-formed with expected root element.
    /// </summary>
    [Fact]
    public void FormatResult_Xml_ProducesValidXml()
    {
        // Arrange
        var result = CreateSuccessResult();

        // Act
        var xml = _sut.FormatResult(result, OutputFormat.Xml);

        // Assert
        var doc = XDocument.Parse(xml);
        doc.Root!.Name.LocalName.Should().Be("validationResult");
        doc.Root.Element("executionId")!.Value.Should().Be("exec-1");
        doc.Root.Element("success")!.Value.Should().Be("true");
    }

    // ── Edge Case Tests ─────────────────────────────────────────────────

    /// <summary>
    /// Verifies that null result throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void FormatResult_NullResult_ThrowsArgumentNull()
    {
        // Act & Assert
        var act = () => _sut.FormatResult(null!, OutputFormat.Json);
        act.Should().Throw<ArgumentNullException>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static CIExecutionResult CreateSuccessResult()
    {
        return new CIExecutionResult
        {
            ExecutionId = "exec-1",
            WorkflowId = "test-workflow",
            Success = true,
            ExitCode = ExitCode.Success,
            ExecutionTimeSeconds = 5,
            ValidationSummary = new CIValidationSummary
            {
                ErrorCount = 0,
                WarningCount = 0,
                DocumentsValidated = 1,
                DocumentsPassed = 1,
                DocumentsFailed = 0,
                PassRate = 100m
            },
            StepResults = new List<CIStepResult>
            {
                new() { StepId = "step-1", Name = "Schema Check", Success = true, ExecutionTimeMs = 200 },
                new() { StepId = "step-2", Name = "Consistency Check", Success = true, ExecutionTimeMs = 300 }
            },
            Message = "All validations passed",
            ExecutedAt = DateTime.UtcNow
        };
    }

    private static CIExecutionResult CreateFailedResult()
    {
        return new CIExecutionResult
        {
            ExecutionId = "exec-2",
            WorkflowId = "test-workflow",
            Success = false,
            ExitCode = ExitCode.ValidationFailed,
            ExecutionTimeSeconds = 8,
            ValidationSummary = new CIValidationSummary
            {
                ErrorCount = 2,
                WarningCount = 1,
                DocumentsValidated = 1,
                DocumentsPassed = 0,
                DocumentsFailed = 1,
                PassRate = 0m
            },
            StepResults = new List<CIStepResult>
            {
                new() { StepId = "step-1", Name = "Schema Check", Success = true, ExecutionTimeMs = 200 },
                new()
                {
                    StepId = "step-2",
                    Name = "Consistency Check",
                    Success = false,
                    ExecutionTimeMs = 500,
                    Errors = new List<string> { "Schema validation error", "Missing required field" },
                    Warnings = new List<string> { "Deprecated field usage" },
                    Message = "Consistency check failed"
                }
            },
            Message = "Validation failed",
            ErrorMessage = "2 errors detected",
            ExecutedAt = DateTime.UtcNow
        };
    }
}
