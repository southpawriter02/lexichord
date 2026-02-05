// -----------------------------------------------------------------------
// <copyright file="ContextInjector.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Templates.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.Agents.Templates;

/// <summary>
/// Implements <see cref="IContextInjector"/> to assemble context from multiple providers.
/// </summary>
/// <remarks>
/// <para>
/// The ContextInjector orchestrates context assembly from multiple <see cref="IContextProvider"/>
/// implementations. Providers execute in parallel with individual timeouts, and their results
/// are merged in priority order.
/// </para>
/// <para>
/// <strong>Execution Model:</strong>
/// </para>
/// <list type="number">
///   <item><description>Filter providers based on <see cref="IContextProvider.IsEnabled"/>.</description></item>
///   <item><description>Filter providers based on license requirements.</description></item>
///   <item><description>Execute remaining providers in parallel with <c>Task.WhenAll</c>.</description></item>
///   <item><description>Apply individual timeouts to each provider.</description></item>
///   <item><description>Merge results in <see cref="IContextProvider.Priority"/> order.</description></item>
///   <item><description>Return combined context dictionary.</description></item>
/// </list>
/// <para>
/// <strong>Error Handling:</strong>
/// Provider failures (exceptions, timeouts) are logged but do not fail the entire operation.
/// The injector continues with partial context from successful providers.
/// </para>
/// <para>
/// <strong>Thread Safety:</strong>
/// This class is thread-safe. Multiple concurrent calls to <see cref="AssembleContextAsync"/>
/// are supported.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.6.3d as part of the Context Injection Service.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var injector = serviceProvider.GetRequiredService&lt;IContextInjector&gt;();
///
/// var request = ContextRequest.Full(
///     documentPath: "/path/to/document.md",
///     selectedText: "What is dependency injection?"
/// );
///
/// var context = await injector.AssembleContextAsync(request);
///
/// // context contains:
/// // - document_path, document_name, document_extension (from DocumentContextProvider)
/// // - selected_text, selection_length, selection_word_count (from DocumentContextProvider)
/// // - style_rules, style_rule_count (from StyleRulesContextProvider)
/// // - context, context_source_count, context_sources (from RAGContextProvider, if licensed)
/// </code>
/// </example>
/// <seealso cref="IContextInjector"/>
/// <seealso cref="IContextProvider"/>
/// <seealso cref="ContextInjectorOptions"/>
public sealed class ContextInjector : IContextInjector
{
    private readonly IEnumerable<IContextProvider> _providers;
    private readonly ILicenseContext _licenseContext;
    private readonly ContextInjectorOptions _options;
    private readonly ILogger<ContextInjector> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContextInjector"/> class.
    /// </summary>
    /// <param name="providers">The collection of context providers to orchestrate.</param>
    /// <param name="licenseContext">The license context for feature checking.</param>
    /// <param name="options">Configuration options for context injection.</param>
    /// <param name="logger">The logger for injection diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public ContextInjector(
        IEnumerable<IContextProvider> providers,
        ILicenseContext licenseContext,
        IOptions<ContextInjectorOptions> options,
        ILogger<ContextInjector> logger)
    {
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var providerList = _providers.ToList();
        _logger.LogDebug(
            "ContextInjector initialized with {ProviderCount} providers: {ProviderNames}",
            providerList.Count,
            string.Join(", ", providerList.Select(p => p.ProviderName)));
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// LOGIC: This method orchestrates parallel provider execution with the following steps:
    /// </para>
    /// <list type="number">
    ///   <item><description>Enumerate all registered providers.</description></item>
    ///   <item><description>Filter by <see cref="IContextProvider.IsEnabled"/> check.</description></item>
    ///   <item><description>Filter by license feature requirements.</description></item>
    ///   <item><description>Execute enabled providers in parallel.</description></item>
    ///   <item><description>Collect results and handle failures gracefully.</description></item>
    ///   <item><description>Merge results in priority order (lower priority first).</description></item>
    ///   <item><description>Return the combined context dictionary.</description></item>
    /// </list>
    /// </remarks>
    public async Task<IDictionary<string, object>> AssembleContextAsync(
        ContextRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request, nameof(request));

        var sw = Stopwatch.StartNew();

        _logger.LogDebug(
            "AssembleContextAsync starting (IncludeStyleRules={IncludeStyle}, IncludeRAGContext={IncludeRAG}, HasDocumentContext={HasDoc})",
            request.IncludeStyleRules,
            request.IncludeRAGContext,
            request.HasDocumentContext);

        // LOGIC: Get all registered providers
        var allProviders = _providers.ToList();

        if (allProviders.Count == 0)
        {
            _logger.LogDebug("No providers registered, returning empty context");
            sw.Stop();
            return new Dictionary<string, object>();
        }

        // LOGIC: Filter providers by enabled state and license requirements
        var enabledProviders = FilterEnabledProviders(allProviders, request);

        if (enabledProviders.Count == 0)
        {
            _logger.LogDebug("No providers enabled for this request, returning empty context");
            sw.Stop();
            return new Dictionary<string, object>();
        }

        _logger.LogDebug(
            "Executing {EnabledCount} of {TotalCount} providers: {ProviderNames}",
            enabledProviders.Count,
            allProviders.Count,
            string.Join(", ", enabledProviders.Select(p => p.ProviderName)));

        // LOGIC: Execute all enabled providers in parallel
        var providerTasks = enabledProviders
            .Select(provider => ExecuteProviderWithTimeoutAsync(provider, request, ct))
            .ToList();

        var results = await Task.WhenAll(providerTasks);

        // LOGIC: Merge results in priority order (lower priority first, so higher can overwrite)
        var mergedContext = MergeResults(results);

        sw.Stop();

        _logger.LogInformation(
            "AssembleContextAsync completed in {Duration}ms with {VariableCount} variables from {SuccessCount} providers",
            sw.ElapsedMilliseconds,
            mergedContext.Count,
            results.Count(r => r.Success));

        return mergedContext;
    }

    /// <summary>
    /// Filters providers based on enabled state and license requirements.
    /// </summary>
    /// <param name="providers">All registered providers.</param>
    /// <param name="request">The current context request.</param>
    /// <returns>List of providers that should execute.</returns>
    private List<IContextProvider> FilterEnabledProviders(
        List<IContextProvider> providers,
        ContextRequest request)
    {
        var enabled = new List<IContextProvider>();

        foreach (var provider in providers)
        {
            // LOGIC: Check if provider is enabled for this request
            if (!provider.IsEnabled(request))
            {
                _logger.LogDebug(
                    "Provider '{ProviderName}' is disabled for this request",
                    provider.ProviderName);
                continue;
            }

            // LOGIC: Check license requirements
            if (!string.IsNullOrEmpty(provider.RequiredLicenseFeature))
            {
                if (!_licenseContext.IsFeatureEnabled(provider.RequiredLicenseFeature))
                {
                    _logger.LogDebug(
                        "Provider '{ProviderName}' skipped: requires feature '{FeatureCode}' which is not licensed",
                        provider.ProviderName,
                        provider.RequiredLicenseFeature);
                    continue;
                }
            }

            enabled.Add(provider);
        }

        return enabled;
    }

    /// <summary>
    /// Executes a single provider with timeout handling.
    /// </summary>
    /// <param name="provider">The provider to execute.</param>
    /// <param name="request">The context request.</param>
    /// <param name="ct">Parent cancellation token.</param>
    /// <returns>The provider result, or a timeout/error result on failure.</returns>
    private async Task<ContextResult> ExecuteProviderWithTimeoutAsync(
        IContextProvider provider,
        ContextRequest request,
        CancellationToken ct)
    {
        // LOGIC: Determine timeout based on provider type
        var timeoutMs = provider.ProviderName == "RAG"
            ? _options.EffectiveRAGTimeoutMs
            : _options.EffectiveProviderTimeoutMs;

        _logger.LogDebug(
            "Executing provider '{ProviderName}' with {TimeoutMs}ms timeout",
            provider.ProviderName,
            timeoutMs);

        // LOGIC: Create a linked cancellation token source with timeout
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeoutMs);

        try
        {
            var result = await provider.GetContextAsync(request, timeoutCts.Token);

            _logger.LogDebug(
                "Provider '{ProviderName}' completed (Success={Success}, VariableCount={VarCount}, Duration={Duration}ms)",
                provider.ProviderName,
                result.Success,
                result.VariableCount,
                result.Duration.TotalMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            // LOGIC: Timeout occurred (not parent cancellation)
            _logger.LogWarning(
                "Provider '{ProviderName}' timed out after {TimeoutMs}ms",
                provider.ProviderName,
                timeoutMs);

            return ContextResult.Timeout(provider.ProviderName);
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Parent cancellation - rethrow to propagate
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Provider error - log and return error result
            _logger.LogWarning(
                ex,
                "Provider '{ProviderName}' failed with error: {ErrorMessage}",
                provider.ProviderName,
                ex.Message);

            return ContextResult.Failure(provider.ProviderName, ex.Message);
        }
    }

    /// <summary>
    /// Merges provider results into a single context dictionary.
    /// </summary>
    /// <param name="results">The results from all providers.</param>
    /// <returns>A merged dictionary with all context variables.</returns>
    /// <remarks>
    /// LOGIC: Results are sorted by provider priority (ascending) before merging.
    /// This means lower-priority providers are processed first, and higher-priority
    /// providers can overwrite their values.
    /// </remarks>
    private Dictionary<string, object> MergeResults(ContextResult[] results)
    {
        var merged = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        // LOGIC: Get successful results with data, ordered by priority
        var successfulResults = results
            .Where(r => r.Success && r.HasData)
            .OrderBy(r => GetProviderPriority(r.ProviderName))
            .ToList();

        _logger.LogDebug(
            "Merging {SuccessCount} successful results (of {TotalCount} total)",
            successfulResults.Count,
            results.Length);

        foreach (var result in successfulResults)
        {
            if (result.Data is null)
            {
                continue;
            }

            foreach (var kvp in result.Data)
            {
                if (merged.ContainsKey(kvp.Key))
                {
                    _logger.LogDebug(
                        "Variable '{Key}' overwritten by provider '{ProviderName}'",
                        kvp.Key,
                        result.ProviderName);
                }

                merged[kvp.Key] = kvp.Value;
            }

            _logger.LogDebug(
                "Merged {VarCount} variables from provider '{ProviderName}'",
                result.Data.Count,
                result.ProviderName);
        }

        // LOGIC: Log any failures for diagnostics
        var failures = results.Where(r => !r.Success).ToList();
        if (failures.Count > 0)
        {
            _logger.LogDebug(
                "{FailureCount} providers failed: {Failures}",
                failures.Count,
                string.Join(", ", failures.Select(f => $"{f.ProviderName}: {f.Error}")));
        }

        return merged;
    }

    /// <summary>
    /// Gets the priority for a provider by name.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>The priority, or int.MaxValue if not found.</returns>
    private int GetProviderPriority(string providerName)
    {
        var provider = _providers.FirstOrDefault(p =>
            string.Equals(p.ProviderName, providerName, StringComparison.OrdinalIgnoreCase));

        return provider?.Priority ?? int.MaxValue;
    }
}
