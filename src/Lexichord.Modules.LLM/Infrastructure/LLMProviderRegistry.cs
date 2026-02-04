// -----------------------------------------------------------------------
// <copyright file="LLMProviderRegistry.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Abstractions.Contracts.Security;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Infrastructure;

/// <summary>
/// Default implementation of the LLM provider registry.
/// </summary>
/// <remarks>
/// <para>
/// This registry manages LLM provider instances using .NET 8+ keyed services
/// for DI resolution. It provides:
/// </para>
/// <list type="bullet">
///   <item><description>Thread-safe provider registration and lookup</description></item>
///   <item><description>Default provider persistence via settings repository</description></item>
///   <item><description>API key configuration checking via secure vault</description></item>
///   <item><description>Case-insensitive provider name matching</description></item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This class uses <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// for provider storage, making it safe for concurrent access from multiple threads.
/// </para>
/// </remarks>
public sealed class LLMProviderRegistry : ILLMProviderRegistry
{
    /// <summary>
    /// The settings key used to persist the default provider preference.
    /// </summary>
    internal const string DefaultProviderSettingsKey = "LLM.DefaultProvider";

    /// <summary>
    /// The format string for provider API key names in the secure vault.
    /// </summary>
    private const string ApiKeyFormat = "{0}:api-key";

    /// <summary>
    /// Thread-safe dictionary storing provider information by normalized (lowercase) name.
    /// </summary>
    private readonly ConcurrentDictionary<string, LLMProviderInfo> _providers;

    /// <summary>
    /// The service provider for resolving keyed chat completion services.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// The settings repository for persisting the default provider preference.
    /// </summary>
    private readonly ISystemSettingsRepository _settings;

    /// <summary>
    /// The secure vault for checking API key configuration status.
    /// </summary>
    private readonly ISecureVault _vault;

    /// <summary>
    /// The logger for registry operations.
    /// </summary>
    private readonly ILogger<LLMProviderRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMProviderRegistry"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider for resolving keyed services.</param>
    /// <param name="settings">The settings repository for default provider persistence.</param>
    /// <param name="vault">The secure vault for API key status checking.</param>
    /// <param name="logger">The logger for registry operations.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public LLMProviderRegistry(
        IServiceProvider serviceProvider,
        ISystemSettingsRepository settings,
        ISecureVault vault,
        ILogger<LLMProviderRegistry> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _vault = vault ?? throw new ArgumentNullException(nameof(vault));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Use OrdinalIgnoreCase comparer for case-insensitive provider names.
        _providers = new ConcurrentDictionary<string, LLMProviderInfo>(StringComparer.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public IReadOnlyList<LLMProviderInfo> AvailableProviders => _providers.Values.ToList().AsReadOnly();

    /// <inheritdoc />
    public IChatCompletionService GetProvider(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        var normalizedName = NormalizeName(providerName);

        LLMLogEvents.ResolvingProvider(_logger, normalizedName);

        // LOGIC: Check if provider is registered.
        if (!_providers.TryGetValue(normalizedName, out var info))
        {
            LLMLogEvents.ProviderNotFound(_logger, normalizedName);
            throw new ProviderNotFoundException(providerName);
        }

        // LOGIC: Check if provider is configured (has API key).
        if (!info.IsConfigured)
        {
            LLMLogEvents.ProviderNotConfigured(_logger, normalizedName);
            throw new ProviderNotConfiguredException(providerName);
        }

        // LOGIC: Resolve the keyed service from DI container.
        var service = _serviceProvider.GetKeyedService<IChatCompletionService>(normalizedName);

        if (service is null)
        {
            // LOGIC: Provider was registered but service is not in DI container.
            // This indicates a configuration issue - the provider info was registered
            // but the actual service implementation was not.
            LLMLogEvents.ProviderNotFound(_logger, normalizedName);
            throw new ProviderNotFoundException(
                providerName,
                $"Provider '{providerName}' is registered but its service implementation was not found in the DI container. " +
                "Ensure AddChatCompletionProvider<T> was called with the correct service type.");
        }

        LLMLogEvents.ProviderResolved(_logger, normalizedName);
        return service;
    }

    /// <inheritdoc />
    public IChatCompletionService GetDefaultProvider()
    {
        // LOGIC: First try to get the persisted default provider preference.
        var defaultProviderName = GetDefaultProviderNameSync();

        if (!string.IsNullOrEmpty(defaultProviderName))
        {
            LLMLogEvents.UsingPersistedDefault(_logger, defaultProviderName);

            // LOGIC: Verify the default provider is still registered.
            if (_providers.ContainsKey(NormalizeName(defaultProviderName)))
            {
                return GetProvider(defaultProviderName);
            }

            // LOGIC: Default provider is no longer registered, fall through to fallback.
            LLMLogEvents.DefaultProviderNotRegistered(_logger, defaultProviderName);
        }

        // LOGIC: Fall back to the first configured provider (by registration order).
        var firstConfigured = _providers.Values.FirstOrDefault(p => p.IsConfigured);

        if (firstConfigured is null)
        {
            LLMLogEvents.NoConfiguredProviders(_logger);
            throw new ProviderNotConfiguredException(
                "No default provider is configured. Please add an API key for at least one provider in Settings.");
        }

        LLMLogEvents.FallingBackToFirstConfigured(_logger, firstConfigured.Name);
        return GetProvider(firstConfigured.Name);
    }

    /// <inheritdoc />
    public void SetDefaultProvider(string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        var normalizedName = NormalizeName(providerName);

        // LOGIC: Verify provider is registered (doesn't need to be configured).
        if (!_providers.ContainsKey(normalizedName))
        {
            LLMLogEvents.ProviderNotFound(_logger, normalizedName);
            throw new ProviderNotFoundException(providerName);
        }

        // LOGIC: Persist the preference synchronously using blocking wait.
        // This ensures the preference is saved before returning.
        SetDefaultProviderSync(normalizedName);

        LLMLogEvents.DefaultProviderSet(_logger, normalizedName);
    }

    /// <inheritdoc />
    public bool IsProviderConfigured(string providerName)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            return false;
        }

        var normalizedName = NormalizeName(providerName);

        // LOGIC: Return false for unregistered providers rather than throwing.
        return _providers.TryGetValue(normalizedName, out var info) && info.IsConfigured;
    }

    /// <summary>
    /// Registers a provider with the registry.
    /// </summary>
    /// <param name="info">The provider information to register.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="info"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method is called during service registration to add provider metadata
    /// to the registry. The <see cref="LLMProviderInfo.IsConfigured"/> property
    /// will be updated during <see cref="RefreshConfigurationStatusAsync"/>.
    /// </para>
    /// <para>
    /// If a provider with the same name is already registered, it will be replaced.
    /// </para>
    /// </remarks>
    internal void RegisterProvider(LLMProviderInfo info)
    {
        ArgumentNullException.ThrowIfNull(info, nameof(info));

        var normalizedName = NormalizeName(info.Name);

        // LOGIC: Store with normalized name as key, preserving original info.
        _providers[normalizedName] = info;

        LLMLogEvents.ProviderRegistered(_logger, normalizedName, info.DisplayName, info.ModelCount);
    }

    /// <summary>
    /// Refreshes the configuration status of all registered providers.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// <para>
    /// This method checks the secure vault for API key presence for each
    /// registered provider and updates the <see cref="LLMProviderInfo.IsConfigured"/>
    /// property accordingly.
    /// </para>
    /// <para>
    /// Call this method during application startup and whenever API keys
    /// may have been added or removed.
    /// </para>
    /// </remarks>
    internal async Task RefreshConfigurationStatusAsync(CancellationToken cancellationToken = default)
    {
        LLMLogEvents.RefreshingConfigurationStatus(_logger, _providers.Count);

        foreach (var (key, info) in _providers)
        {
            try
            {
                var apiKeyName = string.Format(ApiKeyFormat, key);
                var hasKey = await _vault.SecretExistsAsync(apiKeyName, cancellationToken);

                // LOGIC: Update configuration status if changed.
                if (hasKey != info.IsConfigured)
                {
                    var updatedInfo = info with { IsConfigured = hasKey };
                    _providers[key] = updatedInfo;

                    LLMLogEvents.ProviderConfigurationStatusChanged(_logger, key, hasKey);
                }
            }
            catch (Exception ex)
            {
                // LOGIC: Log and continue; don't let one provider failure block others.
                _logger.LogWarning(
                    ex,
                    "Failed to check configuration status for provider '{Provider}'",
                    key);
            }
        }

        var configuredCount = _providers.Values.Count(p => p.IsConfigured);
        LLMLogEvents.ConfigurationStatusRefreshCompleted(_logger, configuredCount, _providers.Count);
    }

    /// <summary>
    /// Gets the current default provider name synchronously.
    /// </summary>
    /// <returns>The default provider name, or null if not set.</returns>
    private string? GetDefaultProviderNameSync()
    {
        // LOGIC: Use synchronous blocking for settings access.
        // This is acceptable because settings are typically fast and cached.
        try
        {
            return _settings
                .GetValueAsync(DefaultProviderSettingsKey, string.Empty)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read default provider setting");
            return null;
        }
    }

    /// <summary>
    /// Sets the default provider name synchronously.
    /// </summary>
    /// <param name="providerName">The provider name to set as default.</param>
    private void SetDefaultProviderSync(string providerName)
    {
        try
        {
            _settings
                .SetValueAsync(
                    DefaultProviderSettingsKey,
                    providerName,
                    "User's preferred default LLM provider")
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist default provider setting");
            throw;
        }
    }

    /// <summary>
    /// Normalizes a provider name to lowercase for consistent lookup.
    /// </summary>
    /// <param name="providerName">The provider name to normalize.</param>
    /// <returns>The lowercase provider name.</returns>
    private static string NormalizeName(string providerName)
    {
        return providerName.ToLowerInvariant();
    }
}
