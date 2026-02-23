// -----------------------------------------------------------------------
// <copyright file="GatingConditionEvaluator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Regex-based expression evaluator for gate conditions. Supports
//   four expression types:
//     1. validation_count(severity) op value — counts issues by severity
//     2. metadata('key') op value — checks document metadata
//     3. content_length op value — checks document content length
//     4. has_property == true/false — checks property presence
//
//   Comparison operators: ==, !=, <, >, <=, >=
//
//   The evaluator compares the expression result against the condition's
//   ExpectedResult to produce the final pass/fail.
//
// v0.7.7f: Gating Step Type (CKVS Phase 4d)
// Dependencies: GatingCondition (v0.7.7f), GatingEvaluationContext (v0.7.7f),
//               ValidationStepResult (v0.7.7e), ILogger<T>
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Workflows.Validation;

/// <summary>
/// Regex-based evaluator for individual gate conditions.
/// </summary>
/// <remarks>
/// <para>
/// Parses condition expressions using regular expressions and evaluates them
/// against the provided <see cref="GatingEvaluationContext"/>. Supports four
/// expression types and six comparison operators.
/// </para>
/// <para>
/// <b>Performance Targets:</b>
/// <list type="bullet">
///   <item><description>Expression parsing: &lt; 50ms</description></item>
///   <item><description>Condition evaluation: &lt; 100ms per condition</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe. No mutable state is held.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.7f as part of Gating Step Type (CKVS Phase 4d).
/// </para>
/// </remarks>
internal sealed class GatingConditionEvaluator : IGatingConditionEvaluator
{
    // ── Compiled Regex Patterns ──────────────────────────────────────────

    // LOGIC: Pre-compiled regexes for each expression type to avoid
    //   repeated compilation overhead on hot paths.

    /// <summary>Pattern for <c>validation_count(severity) op value</c>.</summary>
    private static readonly Regex ValidationCountPattern = new(
        @"validation_count\((\w+)\)\s*(==|!=|<|>|<=|>=)\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Pattern for <c>metadata('key') op value</c>.</summary>
    private static readonly Regex MetadataPattern = new(
        @"metadata\('([^']+)'\)\s*(==|!=)\s*(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Pattern for <c>content_length op value</c>.</summary>
    private static readonly Regex ContentLengthPattern = new(
        @"content_length\s*(==|!=|<|>|<=|>=)\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>Pattern for <c>has_property == true/false</c>.</summary>
    private static readonly Regex HasPropertyPattern = new(
        @"has_(\w+)\s*==\s*(true|false)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly ILogger<GatingConditionEvaluator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GatingConditionEvaluator"/> class.
    /// </summary>
    /// <param name="logger">Logger for evaluation diagnostics.</param>
    public GatingConditionEvaluator(ILogger<GatingConditionEvaluator> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogDebug("GatingConditionEvaluator initialized");
    }

    // ── IGatingConditionEvaluator.EvaluateAsync ──────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Evaluates the condition expression by:
    /// </para>
    /// <list type="number">
    ///   <item>Matching the expression against known patterns</item>
    ///   <item>Extracting operands and operator</item>
    ///   <item>Computing the expression result</item>
    ///   <item>Comparing against <see cref="GatingCondition.ExpectedResult"/></item>
    /// </list>
    /// </remarks>
    public Task<bool> EvaluateAsync(
        GatingCondition condition,
        GatingEvaluationContext context,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _logger.LogDebug(
            "Evaluating condition '{ConditionId}': {Expression} (expected: {Expected})",
            condition.Id, condition.Expression, condition.ExpectedResult);

        try
        {
            var result = EvaluateExpression(condition.Expression.Trim(), context);

            _logger.LogDebug(
                "Condition '{ConditionId}' expression result: {Result}, " +
                "expected: {Expected}, passes: {Passes}",
                condition.Id, result, condition.ExpectedResult,
                result == condition.ExpectedResult);

            return Task.FromResult(result == condition.ExpectedResult);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex,
                "Condition '{ConditionId}' evaluation failed: {Expression}",
                condition.Id, condition.Expression);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error evaluating condition '{ConditionId}': {Expression}",
                condition.Id, condition.Expression);
            return Task.FromResult(false);
        }
    }

    // ── Expression Dispatch ──────────────────────────────────────────────

    /// <summary>
    /// Dispatches expression evaluation to the appropriate handler based on pattern matching.
    /// </summary>
    /// <param name="expression">The expression string to evaluate.</param>
    /// <param name="context">The evaluation context.</param>
    /// <returns>The boolean result of the expression.</returns>
    /// <exception cref="InvalidOperationException">Thrown for unknown expressions.</exception>
    private bool EvaluateExpression(string expression, GatingEvaluationContext context)
    {
        // LOGIC: Match against known patterns in priority order.
        // validation_count is checked first as it's the most common gate expression.

        if (expression.Contains("validation_count", StringComparison.OrdinalIgnoreCase))
        {
            return EvaluateValidationCount(expression, context);
        }

        if (expression.Contains("metadata", StringComparison.OrdinalIgnoreCase))
        {
            return EvaluateMetadata(expression, context);
        }

        if (expression.Contains("content_length", StringComparison.OrdinalIgnoreCase))
        {
            return EvaluateContentLength(expression, context);
        }

        if (expression.Contains("has_", StringComparison.OrdinalIgnoreCase))
        {
            return EvaluateHasProperty(expression, context);
        }

        _logger.LogWarning("Unknown expression type: {Expression}", expression);
        throw new InvalidOperationException($"Unknown expression: {expression}");
    }

    // ── Expression Handlers ──────────────────────────────────────────────

    /// <summary>
    /// Evaluates <c>validation_count(severity) op value</c> expressions.
    /// </summary>
    /// <remarks>
    /// Counts validation issues matching the specified severity from prior
    /// step results. Severity is matched case-insensitively.
    /// </remarks>
    private bool EvaluateValidationCount(string expression, GatingEvaluationContext context)
    {
        var match = ValidationCountPattern.Match(expression);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Invalid validation_count expression: {expression}");
        }

        var severity = match.Groups[1].Value;
        var op = match.Groups[2].Value;
        var value = int.Parse(match.Groups[3].Value);

        // LOGIC: Count errors/warnings/items from validation results matching severity.
        // We match against FailureSeverity name for severity-level counting,
        // and also sum errors/warnings lists for error/warning counting.
        int count;
        if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            count = context.ValidationResults?
                .Sum(r => r.Errors.Count) ?? 0;
        }
        else if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            count = context.ValidationResults?
                .Sum(r => r.Warnings.Count) ?? 0;
        }
        else
        {
            // LOGIC: For other severities, match by FailureSeverity enum name.
            count = context.ValidationResults?
                .Where(r => r.FailureSeverity.ToString()
                    .Equals(severity, StringComparison.OrdinalIgnoreCase))
                .Sum(r => r.ItemsWithIssues) ?? 0;
        }

        _logger.LogDebug(
            "validation_count({Severity}) = {Count}, comparing {Op} {Value}",
            severity, count, op, value);

        return EvaluateComparison(count, op, value);
    }

    /// <summary>
    /// Evaluates <c>metadata('key') op value</c> expressions.
    /// </summary>
    /// <remarks>
    /// Checks document metadata for the specified key and compares its
    /// string representation against the expected value.
    /// </remarks>
    private bool EvaluateMetadata(string expression, GatingEvaluationContext context)
    {
        var match = MetadataPattern.Match(expression);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Invalid metadata expression: {expression}");
        }

        var key = match.Groups[1].Value;
        var op = match.Groups[2].Value;
        var expectedValue = match.Groups[3].Value.Trim().Trim('\'', '"');

        // LOGIC: Look up the key in document metadata.
        string? actualValue = null;
        if (context.DocumentMetadata?.TryGetValue(key, out var metaValue) == true)
        {
            actualValue = metaValue?.ToString();
        }

        _logger.LogDebug(
            "metadata('{Key}') = '{Actual}', comparing {Op} '{Expected}'",
            key, actualValue ?? "(null)", op, expectedValue);

        return op == "=="
            ? string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase)
            : !string.Equals(actualValue, expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Evaluates <c>content_length op value</c> expressions.
    /// </summary>
    /// <remarks>
    /// Measures the character length of document content and compares
    /// against the specified threshold.
    /// </remarks>
    private bool EvaluateContentLength(string expression, GatingEvaluationContext context)
    {
        var match = ContentLengthPattern.Match(expression);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Invalid content_length expression: {expression}");
        }

        var op = match.Groups[1].Value;
        var value = int.Parse(match.Groups[2].Value);
        var length = context.DocumentContent?.Length ?? 0;

        _logger.LogDebug(
            "content_length = {Length}, comparing {Op} {Value}",
            length, op, value);

        return EvaluateComparison(length, op, value);
    }

    /// <summary>
    /// Evaluates <c>has_property == true/false</c> expressions.
    /// </summary>
    /// <remarks>
    /// Checks for the presence of a named property in the evaluation context's
    /// <see cref="GatingEvaluationContext.Variables"/> dictionary.
    /// </remarks>
    private bool EvaluateHasProperty(string expression, GatingEvaluationContext context)
    {
        var match = HasPropertyPattern.Match(expression);
        if (!match.Success)
        {
            throw new InvalidOperationException(
                $"Invalid has_property expression: {expression}");
        }

        var property = match.Groups[1].Value;
        var expectedStr = match.Groups[2].Value;
        var expected = bool.Parse(expectedStr);

        // LOGIC: Check if the property exists in the variables dictionary
        // and has a truthy value.
        bool hasProperty;
        if (context.Variables?.TryGetValue($"has_{property}", out var propValue) == true)
        {
            hasProperty = propValue is true or "true";
        }
        else
        {
            hasProperty = false;
        }

        _logger.LogDebug(
            "has_{Property} = {HasProperty}, expected: {Expected}",
            property, hasProperty, expected);

        return hasProperty == expected;
    }

    // ── Comparison Helper ────────────────────────────────────────────────

    /// <summary>
    /// Evaluates a numeric comparison using the specified operator.
    /// </summary>
    /// <param name="actual">The actual integer value.</param>
    /// <param name="op">The comparison operator string.</param>
    /// <param name="expected">The expected integer value.</param>
    /// <returns><c>true</c> if the comparison holds.</returns>
    /// <exception cref="InvalidOperationException">Thrown for unknown operators.</exception>
    private static bool EvaluateComparison(int actual, string op, int expected)
    {
        return op switch
        {
            "==" => actual == expected,
            "!=" => actual != expected,
            "<" => actual < expected,
            ">" => actual > expected,
            "<=" => actual <= expected,
            ">=" => actual >= expected,
            _ => throw new InvalidOperationException($"Unknown operator: {op}")
        };
    }
}
