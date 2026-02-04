// -----------------------------------------------------------------------
// <copyright file="ModelRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Configuration;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Services;

/// <summary>
/// Caches and provides access to available models across all configured providers.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ModelRegistry"/> acts as a centralized cache for model information,
/// reducing the need for repeated API calls to retrieve model lists from providers.
/// </para>
/// <para>
/// <b>Caching Strategy:</b>
/// </para>
/// <list type="bullet">
///   <item><description>First access populates the cache from the provider or static defaults.</description></item>
///   <item><description>Subsequent accesses return cached data unless <c>forceRefresh</c> is true.</description></item>
///   <item><description>Thread-safe using <see cref="SemaphoreSlim"/> for concurrent access.</description></item>
/// </list>
/// <para>
/// <b>Fallback Behavior:</b>
/// </para>
/// <list type="bullet">
///   <item><description>If a provider implements <see cref="IModelProvider"/>, dynamic model discovery is used.</description></item>
///   <item><description>Otherwise, falls back to static model lists from <see cref="ModelDefaults"/>.</description></item>
/// </list>
/// </remarks>
public class ModelRegistry : IDisposable
{
    private readonly IEnumerable<IModelProvider> _modelProviders;
    private readonly ILogger<ModelRegistry> _logger;
    private readonly ConcurrentDictionary<string, IReadOnlyList<ModelInfo>> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelRegistry"/> class.
    /// </summary>
    /// <param name="modelProviders">The collection of model providers for dynamic discovery.</param>
    /// <param name="logger">The logger instance.</param>
    public ModelRegistry(
        IEnumerable<IModelProvider> modelProviders,
        ILogger<ModelRegistry> logger)
    {
        _modelProviders = modelProviders ?? Enumerable.Empty<IModelProvider>();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the available models for a provider, with caching support.
    /// </summary>
    /// <param name="providerName">The provider name (e.g., "openai", "anthropic").</param>
    /// <param name="forceRefresh">If true, bypasses the cache and fetches fresh data.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>A read-only list of available models for the provider.</returns>
    /// <remarks>
    /// <para>
    /// This method uses a double-check locking pattern to ensure thread safety
    /// while minimizing lock contention.
    /// </para>
    /// <para>
    /// If no <see cref="IModelProvider"/> is registered for the provider,
    /// the method falls back to <see cref="ModelDefaults.GetModelList(string)"/>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Get cached models
    /// var models = await registry.GetModelsAsync("openai");
    ///
    /// // Force refresh from provider
    /// var freshModels = await registry.GetModelsAsync("openai", forceRefresh: true);
    /// </code>
    /// </example>
    public async Task<IReadOnlyList<ModelInfo>> GetModelsAsync(
        string providerName,
        bool forceRefresh = false,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        // Fast path: return cached data if available and not forcing refresh
        if (!forceRefresh && _cache.TryGetValue(providerName, out var cached))
        {
            LLMLogEvents.ModelCacheHit(_logger, providerName);
            return cached;
        }

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (!forceRefresh && _cache.TryGetValue(providerName, out cached))
            {
                LLMLogEvents.ModelCacheHit(_logger, providerName);
                return cached;
            }

            LLMLogEvents.FetchingModels(_logger, providerName);

            // LOGIC: Try to find a model provider that can provide models for this provider name.
            // In the future, this could be extended to match providers by their registered name.
            var provider = _modelProviders.FirstOrDefault();

            if (provider is not null)
            {
                try
                {
                    var models = await provider.GetAvailableModelsAsync(ct);
                    _cache[providerName] = models;
                    LLMLogEvents.CachedModels(_logger, models.Count, providerName);
                    return models;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // LOGIC: If dynamic discovery fails, fall back to static list.
                    // This ensures the application can still function with known models.
                    _logger.LogWarning(
                        ex,
                        "Failed to fetch models from provider '{Provider}', falling back to static list",
                        providerName);
                }
            }

            // Fall back to static model list from ModelDefaults
            var staticModelIds = ModelDefaults.GetModelList(providerName);
            var staticModels = staticModelIds
                .Select(id =>
                {
                    var info = ModelDefaults.GetModelInfo(id);
                    return info ?? new ModelInfo(
                        Id: id,
                        DisplayName: id,
                        ContextWindow: ModelDefaults.DefaultContextWindow,
                        MaxOutputTokens: ModelDefaults.DefaultMaxOutputTokens);
                })
                .ToList()
                .AsReadOnly();

            _cache[providerName] = staticModels;
            LLMLogEvents.FallingBackToStaticModels(_logger, providerName, staticModels.Count);
            return staticModels;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Gets information for a specific model by ID.
    /// </summary>
    /// <param name="modelId">The model identifier (e.g., "gpt-4o", "claude-3-opus-20240229").</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// The <see cref="ModelInfo"/> for the model, or null if not found.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method first checks the static <see cref="ModelDefaults"/> for the model,
    /// then searches through all cached provider model lists.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var info = await registry.GetModelInfoAsync("gpt-4o");
    /// if (info is not null)
    /// {
    ///     Console.WriteLine($"Context window: {info.ContextWindow}");
    /// }
    /// </code>
    /// </example>
    public async Task<ModelInfo?> GetModelInfoAsync(
        string modelId,
        CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(modelId))
        {
            return null;
        }

        // LOGIC: First check static defaults for quick lookup
        var staticInfo = ModelDefaults.GetModelInfo(modelId);
        if (staticInfo is not null)
        {
            LLMLogEvents.ModelInfoLookup(_logger, modelId, true);
            return staticInfo;
        }

        // Search through cached provider models
        foreach (var kvp in _cache)
        {
            var model = kvp.Value.FirstOrDefault(m =>
                m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

            if (model is not null)
            {
                LLMLogEvents.ModelInfoLookup(_logger, modelId, true);
                return model;
            }
        }

        // LOGIC: If not in cache, try fetching from known providers
        foreach (var providerName in ModelDefaults.GetAllKnownProviders())
        {
            ct.ThrowIfCancellationRequested();

            var models = await GetModelsAsync(providerName, ct: ct);
            var model = models.FirstOrDefault(m =>
                m.Id.Equals(modelId, StringComparison.OrdinalIgnoreCase));

            if (model is not null)
            {
                LLMLogEvents.ModelInfoLookup(_logger, modelId, true);
                return model;
            }
        }

        LLMLogEvents.ModelInfoLookup(_logger, modelId, false);
        return null;
    }

    /// <summary>
    /// Gets the context window size for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// The context window size in tokens. Returns <see cref="ModelDefaults.DefaultContextWindow"/>
    /// if the model is not found.
    /// </returns>
    public async Task<int> GetContextWindowAsync(string modelId, CancellationToken ct = default)
    {
        var info = await GetModelInfoAsync(modelId, ct);
        return info?.ContextWindow ?? ModelDefaults.DefaultContextWindow;
    }

    /// <summary>
    /// Gets the maximum output tokens for a specific model.
    /// </summary>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>
    /// The maximum output tokens. Returns <see cref="ModelDefaults.DefaultMaxOutputTokens"/>
    /// if the model is not found.
    /// </returns>
    public async Task<int> GetMaxOutputTokensAsync(string modelId, CancellationToken ct = default)
    {
        var info = await GetModelInfoAsync(modelId, ct);
        return info?.MaxOutputTokens ?? ModelDefaults.DefaultMaxOutputTokens;
    }

    /// <summary>
    /// Clears the model cache, forcing fresh data on next access.
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    /// <summary>
    /// Releases the resources used by this instance.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _lock.Dispose();
        GC.SuppressFinalize(this);
    }
}
