// -----------------------------------------------------------------------
// <copyright file="MustachePromptRenderer.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Contracts.LLM;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stubble.Core;
using Stubble.Core.Builders;

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Implements <see cref="IPromptRenderer"/> using the Mustache templating engine via Stubble.Core.
/// </summary>
/// <remarks>
/// <para>
/// This renderer supports the full Mustache specification:
/// </para>
/// <list type="bullet">
///   <item><description>Variable substitution: <c>{{variable}}</c></description></item>
///   <item><description>Sections (conditional/iteration): <c>{{#section}}...{{/section}}</c></description></item>
///   <item><description>Inverted sections: <c>{{^inverted}}...{{/inverted}}</c></description></item>
///   <item><description>Raw/unescaped output: <c>{{{raw}}}</c> or <c>{{&amp;raw}}</c></description></item>
///   <item><description>Comments: <c>{{! comment }}</c></description></item>
///   <item><description>List iteration: <c>{{#items}}{{.}}{{/items}}</c></description></item>
/// </list>
/// <para>
/// The renderer is thread-safe and designed for singleton lifetime. The underlying
/// Stubble renderer is configured at construction and reused for all render calls.
/// </para>
/// <para>
/// <strong>HTML Escaping:</strong> HTML escaping is disabled by default for prompt content.
/// Use triple mustache <c>{{{variable}}}</c> or ampersand <c>{{&amp;variable}}</c> syntax
/// awareness is maintained for compatibility with standard Mustache templates.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Simple rendering
/// var renderer = new MustachePromptRenderer(logger);
/// var result = renderer.Render("Hello, {{name}}!", new Dictionary&lt;string, object&gt; { ["name"] = "World" });
/// // Result: "Hello, World!"
///
/// // Template rendering with validation
/// var template = PromptTemplate.Create(
///     templateId: "assistant",
///     name: "Assistant",
///     systemPrompt: "You are a helpful assistant.",
///     userPrompt: "{{user_input}}",
///     requiredVariables: ["user_input"]
/// );
///
/// var messages = renderer.RenderMessages(template, new Dictionary&lt;string, object&gt;
/// {
///     ["user_input"] = "What is the capital of France?"
/// });
/// </code>
/// </example>
/// <seealso cref="IPromptRenderer"/>
/// <seealso cref="MustacheRendererOptions"/>
public sealed class MustachePromptRenderer : IPromptRenderer
{
    private readonly StubbleVisitorRenderer _renderer;
    private readonly ILogger<MustachePromptRenderer> _logger;
    private readonly MustacheRendererOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="MustachePromptRenderer"/> class
    /// with the specified logger and options.
    /// </summary>
    /// <param name="logger">The logger instance for render diagnostics.</param>
    /// <param name="options">Optional renderer configuration. Uses <see cref="MustacheRendererOptions.Default"/> if not provided.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public MustachePromptRenderer(
        ILogger<MustachePromptRenderer> logger,
        IOptions<MustacheRendererOptions>? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? MustacheRendererOptions.Default;

        // LOGIC: Build the Stubble renderer with configured settings.
        // The renderer is thread-safe and can be reused across all render calls.
        _renderer = new StubbleBuilder()
            .Configure(settings =>
            {
                // LOGIC: Enable case-insensitive lookup if configured.
                settings.SetIgnoreCaseOnKeyLookup(_options.IgnoreCaseOnKeyLookup);

                // LOGIC: Disable HTML escaping for prompt content.
                // Prompts should contain raw text without entity encoding.
                settings.SetEncodingFunction(value => value);
            })
            .Build();

        _logger.LogDebug(
            "MustachePromptRenderer initialized with options: " +
            "IgnoreCaseOnKeyLookup={IgnoreCaseOnKeyLookup}, " +
            "ThrowOnMissingVariables={ThrowOnMissingVariables}, " +
            "FastRenderThresholdMs={FastRenderThresholdMs}",
            _options.IgnoreCaseOnKeyLookup,
            _options.ThrowOnMissingVariables,
            _options.FastRenderThresholdMs);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MustachePromptRenderer"/> class
    /// with default options.
    /// </summary>
    /// <param name="logger">The logger instance for render diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logger"/> is null.</exception>
    public MustachePromptRenderer(ILogger<MustachePromptRenderer> logger)
        : this(logger, null)
    {
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method performs simple variable substitution without validation.
    /// Missing variables are rendered as empty strings.
    /// </para>
    /// <para>
    /// Supported Mustache syntax:
    /// </para>
    /// <list type="bullet">
    ///   <item><description><c>{{variable}}</c> - Simple substitution</description></item>
    ///   <item><description><c>{{#section}}...{{/section}}</c> - Conditional/iteration</description></item>
    ///   <item><description><c>{{^inverted}}...{{/inverted}}</c> - Render when falsy</description></item>
    ///   <item><description><c>{{{raw}}}</c> - Unescaped output (same as escaped since escaping is disabled)</description></item>
    /// </list>
    /// </remarks>
    public string Render(string template, IDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);

        var sw = Stopwatch.StartNew();

        try
        {
            // LOGIC: Render the template using Stubble.
            var result = _renderer.Render(template, variables);
            sw.Stop();

            _logger.LogDebug(
                "Rendered template ({TemplateLength} chars) with {VariableCount} variables in {ElapsedMs}ms. " +
                "Output length: {OutputLength} chars",
                template.Length,
                variables.Count,
                sw.ElapsedMilliseconds,
                result.Length);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(
                ex,
                "Template rendering failed after {ElapsedMs}ms. " +
                "Template length: {TemplateLength} chars, Variable count: {VariableCount}. Error: {ErrorMessage}",
                sw.ElapsedMilliseconds,
                template.Length,
                variables.Count,
                ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method validates required variables before rendering when
    /// <see cref="MustacheRendererOptions.ThrowOnMissingVariables"/> is <c>true</c>.
    /// </para>
    /// <para>
    /// The returned array contains:
    /// </para>
    /// <list type="number">
    ///   <item><description>System message (if <see cref="IPromptTemplate.SystemPromptTemplate"/> is not empty)</description></item>
    ///   <item><description>User message (if <see cref="IPromptTemplate.UserPromptTemplate"/> is not empty)</description></item>
    /// </list>
    /// <para>
    /// Empty or whitespace-only rendered prompts are excluded from the result.
    /// </para>
    /// </remarks>
    public ChatMessage[] RenderMessages(
        IPromptTemplate template,
        IDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);

        var sw = Stopwatch.StartNew();

        // LOGIC: Validate required variables before rendering if configured to throw.
        if (_options.ThrowOnMissingVariables)
        {
            var validation = ValidateVariables(template, variables);
            if (!validation.IsValid)
            {
                _logger.LogWarning(
                    "Template '{TemplateId}' validation failed. Missing required variables: {MissingVariables}",
                    template.TemplateId,
                    string.Join(", ", validation.MissingVariables));

                validation.ThrowIfInvalid(template.TemplateId);
            }
        }

        var messages = new List<ChatMessage>();

        // LOGIC: Render system prompt if present and non-empty.
        if (!string.IsNullOrEmpty(template.SystemPromptTemplate))
        {
            var systemContent = _renderer.Render(template.SystemPromptTemplate, variables);
            if (!string.IsNullOrWhiteSpace(systemContent))
            {
                messages.Add(ChatMessage.System(systemContent.Trim()));
            }
        }

        // LOGIC: Render user prompt if present and non-empty.
        if (!string.IsNullOrEmpty(template.UserPromptTemplate))
        {
            var userContent = _renderer.Render(template.UserPromptTemplate, variables);
            if (!string.IsNullOrWhiteSpace(userContent))
            {
                messages.Add(ChatMessage.User(userContent.Trim()));
            }
        }

        sw.Stop();

        var wasFast = sw.ElapsedMilliseconds < _options.FastRenderThresholdMs;

        _logger.LogDebug(
            "Rendered template '{TemplateId}' to {MessageCount} messages in {ElapsedMs}ms (fast: {WasFast}). " +
            "Variable count: {VariableCount}",
            template.TemplateId,
            messages.Count,
            sw.ElapsedMilliseconds,
            wasFast,
            variables.Count);

        return messages.ToArray();
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// This method checks for missing required variables and generates warnings
    /// for unexpected variables that are provided but not defined in the template.
    /// </para>
    /// <para>
    /// A variable is considered "missing" if:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>It is not present in the variables dictionary</description></item>
    ///   <item><description>Its value is <c>null</c></description></item>
    ///   <item><description>Its value is an empty or whitespace-only string</description></item>
    /// </list>
    /// <para>
    /// Variable name matching respects the <see cref="MustacheRendererOptions.IgnoreCaseOnKeyLookup"/> setting.
    /// </para>
    /// </remarks>
    public ValidationResult ValidateVariables(
        IPromptTemplate template,
        IDictionary<string, object> variables)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(variables);

        var missingVariables = new List<string>();
        var warnings = new List<string>();

        // LOGIC: Determine the string comparer based on case-sensitivity configuration.
        var comparer = _options.IgnoreCaseOnKeyLookup
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;

        // LOGIC: Check for missing required variables.
        foreach (var requiredVar in template.RequiredVariables)
        {
            var found = _options.IgnoreCaseOnKeyLookup
                ? variables.Keys.Any(k => k.Equals(requiredVar, StringComparison.OrdinalIgnoreCase))
                : variables.ContainsKey(requiredVar);

            if (!found)
            {
                missingVariables.Add(requiredVar);
                continue;
            }

            // LOGIC: Get the actual key from the dictionary (handling case-insensitivity).
            var actualKey = _options.IgnoreCaseOnKeyLookup
                ? variables.Keys.First(k => k.Equals(requiredVar, StringComparison.OrdinalIgnoreCase))
                : requiredVar;

            var value = variables[actualKey];

            // LOGIC: Check for null or empty string values in required variables.
            if (value is null)
            {
                missingVariables.Add(requiredVar);
            }
            else if (value is string stringValue && string.IsNullOrWhiteSpace(stringValue))
            {
                missingVariables.Add(requiredVar);
            }
        }

        // LOGIC: Generate warnings for unexpected variables (provided but not defined).
        var allTemplateVars = template.RequiredVariables
            .Concat(template.OptionalVariables)
            .ToHashSet(comparer);

        foreach (var providedVar in variables.Keys)
        {
            if (!allTemplateVars.Contains(providedVar))
            {
                warnings.Add($"Variable '{providedVar}' is not defined in template '{template.TemplateId}'");
            }
        }

        // LOGIC: Return appropriate validation result based on findings.
        if (missingVariables.Count > 0)
        {
            _logger.LogDebug(
                "Validation failed for template '{TemplateId}'. " +
                "Missing: [{MissingVariables}], Warnings: {WarningCount}",
                template.TemplateId,
                string.Join(", ", missingVariables),
                warnings.Count);

            return warnings.Count > 0
                ? ValidationResult.Failure(missingVariables, warnings)
                : ValidationResult.Failure(missingVariables);
        }

        if (warnings.Count > 0)
        {
            _logger.LogDebug(
                "Validation passed for template '{TemplateId}' with {WarningCount} warnings: [{Warnings}]",
                template.TemplateId,
                warnings.Count,
                string.Join("; ", warnings));

            return ValidationResult.WithWarnings(warnings);
        }

        _logger.LogDebug(
            "Validation passed for template '{TemplateId}'. " +
            "Required: {RequiredCount}, Optional: {OptionalCount}, Provided: {ProvidedCount}",
            template.TemplateId,
            template.RequiredVariables.Count,
            template.OptionalVariables.Count,
            variables.Count);

        return ValidationResult.Success();
    }
}
