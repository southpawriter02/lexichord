// -----------------------------------------------------------------------
// <copyright file="CIResultFormatter.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation.CI;

/// <summary>
/// Formats <see cref="CIExecutionResult"/> into various output formats for CI/CD consumption.
/// </summary>
/// <remarks>
/// <para>
/// Provides a unified API for converting workflow execution results into standard
/// CI/CD-compatible formats. Each format targets a specific integration scenario:
/// </para>
/// <list type="bullet">
///   <item><description><b>JSON</b> — Universal machine-readable format</description></item>
///   <item><description><b>JUnit XML</b> — CI test reporting (GitHub Actions, Jenkins, etc.)</description></item>
///   <item><description><b>Markdown</b> — PR comments and human-readable reports</description></item>
///   <item><description><b>SARIF 2.1.0</b> — GitHub/GitLab code scanning integration</description></item>
///   <item><description><b>HTML</b> — Rich report generation</description></item>
///   <item><description><b>XML</b> — Legacy CI system compatibility</description></item>
/// </list>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §4.1
/// </para>
/// </remarks>
/// <seealso cref="ICIResultFormatter"/>
/// <seealso cref="CIExecutionResult"/>
public interface ICIResultFormatter
{
    /// <summary>
    /// Formats a <see cref="CIExecutionResult"/> in the specified output format.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <param name="format">The desired output format.</param>
    /// <returns>A string containing the formatted result.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="result"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="format"/> is not a recognized <see cref="OutputFormat"/> value.
    /// </exception>
    string FormatResult(CIExecutionResult result, OutputFormat format);
}

/// <summary>
/// Default implementation of <see cref="ICIResultFormatter"/>.
/// </summary>
/// <remarks>
/// <para>
/// Stateless formatter that converts execution results into CI/CD-compatible
/// output strings. Each format method is deterministic and side-effect free.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-j §4.1
/// </para>
/// </remarks>
public class CIResultFormatter : ICIResultFormatter
{
    // ── Dependencies ────────────────────────────────────────────────────

    private readonly ILogger<CIResultFormatter> _logger;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes a new instance of <see cref="CIResultFormatter"/>.
    /// </summary>
    /// <param name="logger">Logger for recording formatting operations.</param>
    public CIResultFormatter(ILogger<CIResultFormatter> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── ICIResultFormatter Implementation ───────────────────────────────

    /// <inheritdoc />
    public string FormatResult(CIExecutionResult result, OutputFormat format)
    {
        ArgumentNullException.ThrowIfNull(result);

        _logger.LogDebug(
            "Formatting CI result for execution {ExecutionId} as {Format}",
            result.ExecutionId, format);

        return format switch
        {
            OutputFormat.Json => FormatJson(result),
            OutputFormat.Junit => FormatJunit(result),
            OutputFormat.Markdown => FormatMarkdown(result),
            OutputFormat.SarifJson => FormatSarif(result),
            OutputFormat.Html => FormatHtml(result),
            OutputFormat.Xml => FormatXml(result),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format,
                $"Unsupported output format: {format}")
        };
    }

    // ── Format Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Formats the result as indented JSON.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <returns>A JSON string representation of the result.</returns>
    /// <remarks>
    /// LOGIC: Uses System.Text.Json with camelCase naming for consistency
    /// with CI/CD tooling conventions.
    /// </remarks>
    internal static string FormatJson(CIExecutionResult result)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(result, options);
    }

    /// <summary>
    /// Formats the result as JUnit XML for CI test reporting systems.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <returns>A JUnit XML string.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Maps workflow steps to JUnit test cases within a test suite.
    /// Failed steps produce <c>&lt;failure&gt;</c> elements with the step's
    /// error messages concatenated.
    /// </para>
    /// <para>
    /// <b>Specification:</b> LCS-DES-077-KG-j §4.1
    /// </para>
    /// </remarks>
    internal static string FormatJunit(CIExecutionResult result)
    {
        var documentsValidated = result.ValidationSummary?.DocumentsValidated ?? 0;
        var documentsFailed = result.ValidationSummary?.DocumentsFailed ?? 0;

        var testCases = result.StepResults.Select(sr =>
        {
            var testCase = new XElement("testcase",
                new XAttribute("name", sr.StepId),
                new XAttribute("time", sr.ExecutionTimeMs / 1000.0));

            // LOGIC: Only add a <failure> element for steps that did not pass.
            if (!sr.Success)
            {
                var failureMessage = sr.Message ?? "Validation failed";
                var failureDetail = sr.Errors.Any()
                    ? string.Join(Environment.NewLine, sr.Errors)
                    : failureMessage;

                testCase.Add(new XElement("failure",
                    new XAttribute("message", failureMessage),
                    failureDetail));
            }

            return testCase;
        });

        var xml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("testsuites",
                new XElement("testsuite",
                    new XAttribute("name", result.WorkflowId),
                    new XAttribute("tests", documentsValidated),
                    new XAttribute("failures", documentsFailed),
                    new XAttribute("time", result.ExecutionTimeSeconds),
                    testCases)));

        return xml.ToString();
    }

    /// <summary>
    /// Formats the result as Markdown for PR comments and reports.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <returns>A Markdown string.</returns>
    /// <remarks>
    /// LOGIC: Produces a structured Markdown document with a header, summary
    /// table, and per-step results with error/warning lists.
    /// </remarks>
    internal static string FormatMarkdown(CIExecutionResult result)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("# Validation Results");
        sb.AppendLine();

        // Summary
        var statusEmoji = result.Success ? "✅ PASS" : "❌ FAIL";
        sb.AppendLine($"**Status:** {statusEmoji}");
        sb.AppendLine($"**Workflow:** {result.WorkflowId}");
        sb.AppendLine($"**Execution ID:** {result.ExecutionId}");

        if (result.ValidationSummary != null)
        {
            sb.AppendLine($"**Errors:** {result.ValidationSummary.ErrorCount}");
            sb.AppendLine($"**Warnings:** {result.ValidationSummary.WarningCount}");
            sb.AppendLine($"**Pass Rate:** {result.ValidationSummary.PassRate:F1}%");
        }

        sb.AppendLine($"**Time:** {result.ExecutionTimeSeconds}s");
        sb.AppendLine();

        // Step results
        if (result.StepResults.Any())
        {
            sb.AppendLine("## Step Results");
            sb.AppendLine();

            foreach (var step in result.StepResults)
            {
                var stepEmoji = step.Success ? "✅" : "❌";
                sb.AppendLine($"### {stepEmoji} {step.Name ?? step.StepId}");

                if (step.Errors.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("**Errors:**");
                    foreach (var error in step.Errors)
                    {
                        sb.AppendLine($"- {error}");
                    }
                }

                if (step.Warnings.Any())
                {
                    sb.AppendLine();
                    sb.AppendLine("**Warnings:**");
                    foreach (var warning in step.Warnings)
                    {
                        sb.AppendLine($"- {warning}");
                    }
                }

                sb.AppendLine();
            }
        }

        // Message
        if (!string.IsNullOrWhiteSpace(result.Message))
        {
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine(result.Message);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats the result as SARIF 2.1.0 JSON for code scanning integration.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <returns>A SARIF 2.1.0 JSON string.</returns>
    /// <remarks>
    /// <para>
    /// LOGIC: Creates a minimal SARIF document with a single run containing
    /// the Lexichord tool driver and results mapped from step errors. Each
    /// error becomes a SARIF result with ruleId set to the step ID.
    /// </para>
    /// <para>
    /// <b>Specification:</b> LCS-DES-077-KG-j §4.1
    /// </para>
    /// </remarks>
    internal static string FormatSarif(CIExecutionResult result)
    {
        // LOGIC: Build SARIF results from step errors. Warnings are mapped
        // as "warning" level, errors as "error" level.
        var sarifResults = new List<object>();

        foreach (var step in result.StepResults)
        {
            foreach (var error in step.Errors)
            {
                sarifResults.Add(new
                {
                    ruleId = step.StepId,
                    message = new { text = error },
                    level = "error"
                });
            }

            foreach (var warning in step.Warnings)
            {
                sarifResults.Add(new
                {
                    ruleId = step.StepId,
                    message = new { text = warning },
                    level = "warning"
                });
            }
        }

        var sarif = new
        {
            version = "2.1.0",
            @schema = "https://raw.githubusercontent.com/oasis-tcs/sarif-spec/master/Schemata/sarif-schema-2.1.0.json",
            runs = new[]
            {
                new
                {
                    tool = new
                    {
                        driver = new
                        {
                            name = "Lexichord",
                            version = "0.7.7",
                            informationUri = "https://lexichord.io"
                        }
                    },
                    results = sarifResults
                }
            }
        };

        return JsonSerializer.Serialize(sarif, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Formats the result as a simple HTML report.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <returns>An HTML string.</returns>
    internal static string FormatHtml(CIExecutionResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><title>Validation Results</title></head><body>");
        sb.AppendLine($"<h1>Validation Results — {(result.Success ? "PASS" : "FAIL")}</h1>");
        sb.AppendLine($"<p><b>Workflow:</b> {result.WorkflowId}</p>");
        sb.AppendLine($"<p><b>Execution ID:</b> {result.ExecutionId}</p>");
        sb.AppendLine($"<p><b>Time:</b> {result.ExecutionTimeSeconds}s</p>");

        if (result.ValidationSummary != null)
        {
            sb.AppendLine("<table border='1'><tr><th>Metric</th><th>Value</th></tr>");
            sb.AppendLine($"<tr><td>Errors</td><td>{result.ValidationSummary.ErrorCount}</td></tr>");
            sb.AppendLine($"<tr><td>Warnings</td><td>{result.ValidationSummary.WarningCount}</td></tr>");
            sb.AppendLine($"<tr><td>Pass Rate</td><td>{result.ValidationSummary.PassRate:F1}%</td></tr>");
            sb.AppendLine("</table>");
        }

        if (result.StepResults.Any())
        {
            sb.AppendLine("<h2>Steps</h2><ul>");
            foreach (var step in result.StepResults)
            {
                var status = step.Success ? "✅" : "❌";
                sb.AppendLine($"<li>{status} {step.Name ?? step.StepId}</li>");
            }
            sb.AppendLine("</ul>");
        }

        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Formats the result as a simple XML document.
    /// </summary>
    /// <param name="result">The execution result to format.</param>
    /// <returns>An XML string.</returns>
    internal static string FormatXml(CIExecutionResult result)
    {
        var xml = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("validationResult",
                new XElement("executionId", result.ExecutionId),
                new XElement("workflowId", result.WorkflowId),
                new XElement("success", result.Success),
                new XElement("exitCode", result.ExitCode),
                new XElement("executionTimeSeconds", result.ExecutionTimeSeconds),
                result.ValidationSummary != null
                    ? new XElement("summary",
                        new XElement("errors", result.ValidationSummary.ErrorCount),
                        new XElement("warnings", result.ValidationSummary.WarningCount),
                        new XElement("passRate", result.ValidationSummary.PassRate))
                    : null,
                new XElement("steps",
                    result.StepResults.Select(sr =>
                        new XElement("step",
                            new XAttribute("id", sr.StepId),
                            new XAttribute("success", sr.Success),
                            sr.Errors.Select(e => new XElement("error", e)),
                            sr.Warnings.Select(w => new XElement("warning", w))))),
                result.Message != null
                    ? new XElement("message", result.Message)
                    : null));

        return xml.ToString();
    }
}
