// -----------------------------------------------------------------------
// <copyright file="StyleRulesContextProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Templates.Formatters;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Templates.Providers;

/// <summary>
/// Context provider that supplies formatted style rules from the active style sheet.
/// </summary>
/// <remarks>
/// <para>
/// This provider retrieves enabled style rules from <see cref="IStyleEngine"/> and
/// formats them for injection into prompts. Style rules help the LLM understand
/// writing guidelines and produce content that conforms to the user's style preferences.
/// </para>
/// <para>
/// <strong>Variables Produced:</strong>
/// </para>
/// <list type="table">
///   <listheader>
///     <term>Variable</term>
///     <description>Description</description>
///   </listheader>
///   <item>
///     <term><c>style_rules</c></term>
///     <description>Formatted bullet list of enabled style rules.</description>
///   </item>
///   <item>
///     <term><c>style_rule_count</c></term>
///     <description>Number of enabled rules in the active style sheet.</description>
///   </item>
/// </list>
/// <para>
/// <strong>License Requirements:</strong> None. Style rules context is available to all tiers.
/// </para>
/// <para>
/// <strong>Priority:</strong> 100 (medium - enhances prompts with writing guidelines).
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var provider = new StyleRulesContextProvider(styleEngine, formatter, logger);
/// var request = ContextRequest.StyleOnly("/path/to/document.md");
///
/// if (provider.IsEnabled(request))
/// {
///     var result = await provider.GetContextAsync(request, CancellationToken.None);
///     if (result.Success &amp;&amp; result.HasData)
///     {
///         // result.Data["style_rules"] = "- Avoid Jargon: Technical jargon reduces...\n- ..."
///         // result.Data["style_rule_count"] = 5
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IContextProvider"/>
/// <seealso cref="IStyleEngine"/>
/// <seealso cref="IContextFormatter"/>
public sealed class StyleRulesContextProvider : IContextProvider
{
    private readonly IStyleEngine _styleEngine;
    private readonly IContextFormatter _formatter;
    private readonly ILogger<StyleRulesContextProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleRulesContextProvider"/> class.
    /// </summary>
    /// <param name="styleEngine">The style engine to retrieve active rules from.</param>
    /// <param name="formatter">The formatter for converting rules to strings.</param>
    /// <param name="logger">The logger for provider diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public StyleRulesContextProvider(
        IStyleEngine styleEngine,
        IContextFormatter formatter,
        ILogger<StyleRulesContextProvider> logger)
    {
        _styleEngine = styleEngine ?? throw new ArgumentNullException(nameof(styleEngine));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("StyleRulesContextProvider initialized");
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: This provider is identified as "StyleRules" in logs and result tracking.
    /// </remarks>
    public string ProviderName => "StyleRules";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Priority 100 is medium, meaning style rules are processed after document
    /// context but before RAG context.
    /// </remarks>
    public int Priority => 100;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Style rules context is available to all license tiers and does not require
    /// a specific feature code.
    /// </remarks>
    public string? RequiredLicenseFeature => null;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: This provider is enabled when the request explicitly includes style rules
    /// via the <see cref="ContextRequest.IncludeStyleRules"/> flag.
    /// </remarks>
    public bool IsEnabled(ContextRequest request)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        _logger.LogDebug(
            "StyleRulesContextProvider.IsEnabled: {IsEnabled}",
            request.IncludeStyleRules);

        return request.IncludeStyleRules;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: Retrieves enabled style rules from the active style sheet and formats
    /// them using the injected <see cref="IContextFormatter"/>. The operation is
    /// synchronous and fast.
    /// </para>
    /// <para>
    /// Returns an empty result if the active style sheet has no enabled rules.
    /// </para>
    /// </remarks>
    public Task<ContextResult> GetContextAsync(ContextRequest request, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        ct.ThrowIfCancellationRequested();

        var sw = Stopwatch.StartNew();

        _logger.LogDebug("StyleRulesContextProvider.GetContextAsync starting");

        try
        {
            // LOGIC: Retrieve the active style sheet from the engine
            var styleSheet = _styleEngine.GetActiveStyleSheet();

            if (styleSheet is null || styleSheet == StyleSheet.Empty)
            {
                _logger.LogDebug("No active style sheet configured");
                sw.Stop();
                return Task.FromResult(ContextResult.Empty(ProviderName, sw.Elapsed));
            }

            _logger.LogDebug(
                "Retrieved active style sheet '{StyleSheetName}' with {RuleCount} rules",
                styleSheet.Name,
                styleSheet.Rules.Count);

            // LOGIC: Get only enabled rules
            var enabledRules = styleSheet.GetEnabledRules();

            if (enabledRules.Count == 0)
            {
                _logger.LogDebug("Active style sheet has no enabled rules");
                sw.Stop();
                return Task.FromResult(ContextResult.Empty(ProviderName, sw.Elapsed));
            }

            _logger.LogDebug(
                "Found {EnabledRuleCount} enabled rules out of {TotalRuleCount} total",
                enabledRules.Count,
                styleSheet.Rules.Count);

            // LOGIC: Format the rules using the injected formatter
            var formattedRules = _formatter.FormatStyleRules(enabledRules);

            if (string.IsNullOrWhiteSpace(formattedRules))
            {
                _logger.LogDebug("Formatter produced empty output");
                sw.Stop();
                return Task.FromResult(ContextResult.Empty(ProviderName, sw.Elapsed));
            }

            // LOGIC: Assemble the context data
            var data = new Dictionary<string, object>
            {
                ["style_rules"] = formattedRules,
                ["style_rule_count"] = enabledRules.Count
            };

            sw.Stop();

            _logger.LogInformation(
                "StyleRulesContextProvider produced {VariableCount} variables ({RuleCount} rules) in {Duration}ms",
                data.Count,
                enabledRules.Count,
                sw.ElapsedMilliseconds);

            return Task.FromResult(ContextResult.Ok(ProviderName, data, sw.Elapsed));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();

            _logger.LogWarning(
                ex,
                "StyleRulesContextProvider encountered error: {ErrorMessage}",
                ex.Message);

            return Task.FromResult(ContextResult.Failure(ProviderName, ex.Message));
        }
    }
}
