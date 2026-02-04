// -----------------------------------------------------------------------
// <copyright file="LLMLogEvents.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Logging;

/// <summary>
/// Provides structured logging events for the LLM module using source generation.
/// </summary>
/// <remarks>
/// <para>
/// This class uses the <see cref="LoggerMessageAttribute"/> source generator pattern
/// for high-performance, zero-allocation logging in hot paths.
/// </para>
/// <para>
/// Event ID ranges:
/// </para>
/// <list type="bullet">
///   <item><description>1001-1099: Options resolution and validation</description></item>
///   <item><description>1100-1199: Model registry operations</description></item>
///   <item><description>1200-1299: Token estimation</description></item>
///   <item><description>1300-1399: Provider operations</description></item>
///   <item><description>1400-1499: Provider registry management (v0.6.1c)</description></item>
///   <item><description>1500-1599: LLM Settings Page (v0.6.1d)</description></item>
///   <item><description>1600-1699: OpenAI Provider Events (v0.6.2a)</description></item>
///   <item><description>1700-1799: Anthropic Provider Events (v0.6.2b)</description></item>
///   <item><description>1800-1899: Resilience Policy Events (v0.6.2c)</description></item>
///   <item><description>1900-1999: Token Counting Service Events (v0.6.2d)</description></item>
/// </list>
/// </remarks>
internal static partial class LLMLogEvents
{
    // =========================================================================
    // Options Resolution Events (1001-1099)
    // =========================================================================

    /// <summary>
    /// Logs the start of options resolution for a provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The target provider name.</param>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "Resolving ChatOptions for provider '{Provider}'")]
    public static partial void ResolvingOptions(ILogger logger, string provider);

    /// <summary>
    /// Logs the resolved model and its source.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The resolved model identifier.</param>
    /// <param name="source">The source of the resolution (e.g., "request", "user-default", "provider-default").</param>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Debug,
        Message = "Resolved model: '{Model}' (source: {Source})")]
    public static partial void ResolvedModel(ILogger logger, string model, string source);

    /// <summary>
    /// Logs a validation failure during options resolution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The target provider name.</param>
    /// <param name="errors">The concatenated validation error messages.</param>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "ChatOptions validation failed for provider '{Provider}': {Errors}")]
    public static partial void ValidationFailed(ILogger logger, string provider, string errors);

    /// <summary>
    /// Logs the final resolved chat options.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="temperature">The temperature value.</param>
    /// <param name="maxTokens">The maximum tokens value.</param>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Final ChatOptions: Model='{Model}', Temperature={Temperature}, MaxTokens={MaxTokens}")]
    public static partial void FinalOptions(ILogger logger, string model, double temperature, int maxTokens);

    /// <summary>
    /// Logs application of user preferences to options.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="temperature">The user's preferred temperature, if set.</param>
    /// <param name="maxTokens">The user's preferred max tokens, if set.</param>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Debug,
        Message = "Applied user preferences: Temperature={Temperature}, MaxTokens={MaxTokens}")]
    public static partial void AppliedUserPreferences(ILogger logger, double? temperature, int? maxTokens);

    /// <summary>
    /// Logs when model defaults are loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="maxTokens">The default max tokens for the model.</param>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Debug,
        Message = "Loaded model defaults for '{Model}': MaxTokens={MaxTokens}")]
    public static partial void LoadedModelDefaults(ILogger logger, string model, int maxTokens);

    // =========================================================================
    // Model Registry Events (1100-1199)
    // =========================================================================

    /// <summary>
    /// Logs fetching models from a provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The provider name.</param>
    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Information,
        Message = "Fetching models for provider '{Provider}'")]
    public static partial void FetchingModels(ILogger logger, string provider);

    /// <summary>
    /// Logs successful model caching.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="count">The number of models cached.</param>
    /// <param name="provider">The provider name.</param>
    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Information,
        Message = "Cached {Count} models for provider '{Provider}'")]
    public static partial void CachedModels(ILogger logger, int count, string provider);

    /// <summary>
    /// Logs a cache hit for model retrieval.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The provider name.</param>
    [LoggerMessage(
        EventId = 1102,
        Level = LogLevel.Debug,
        Message = "Model cache hit for provider '{Provider}'")]
    public static partial void ModelCacheHit(ILogger logger, string provider);

    /// <summary>
    /// Logs falling back to static model list.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The provider name.</param>
    /// <param name="count">The number of static models returned.</param>
    [LoggerMessage(
        EventId = 1103,
        Level = LogLevel.Debug,
        Message = "Falling back to static model list for provider '{Provider}' ({Count} models)")]
    public static partial void FallingBackToStaticModels(ILogger logger, string provider, int count);

    /// <summary>
    /// Logs model info lookup result.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="modelId">The model identifier.</param>
    /// <param name="found">Whether the model was found.</param>
    [LoggerMessage(
        EventId = 1104,
        Level = LogLevel.Debug,
        Message = "Model info lookup for '{ModelId}': Found={Found}")]
    public static partial void ModelInfoLookup(ILogger logger, string modelId, bool found);

    // =========================================================================
    // Token Estimation Events (1200-1299)
    // =========================================================================

    /// <summary>
    /// Logs token estimation results.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="promptTokens">The estimated prompt tokens.</param>
    /// <param name="availableTokens">The tokens available for response.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Debug,
        Message = "Token estimate: Prompt={PromptTokens}, Available={AvailableTokens}, Context={ContextWindow}")]
    public static partial void TokenEstimation(ILogger logger, int promptTokens, int availableTokens, int contextWindow);

    /// <summary>
    /// Logs context window exceeded warning.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestedTokens">The tokens requested.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Warning,
        Message = "Request would exceed context window: Requested={RequestedTokens}, Window={ContextWindow}")]
    public static partial void ContextWindowExceeded(ILogger logger, int requestedTokens, int contextWindow);

    /// <summary>
    /// Logs MaxTokens adjustment due to context limits.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="originalMaxTokens">The originally requested max tokens.</param>
    /// <param name="adjustedMaxTokens">The adjusted max tokens after clamping.</param>
    [LoggerMessage(
        EventId = 1202,
        Level = LogLevel.Debug,
        Message = "Adjusted MaxTokens from {OriginalMaxTokens} to {AdjustedMaxTokens} to fit context window")]
    public static partial void MaxTokensAdjusted(ILogger logger, int originalMaxTokens, int adjustedMaxTokens);

    /// <summary>
    /// Logs message token counting details.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageIndex">The index of the message in the request.</param>
    /// <param name="role">The message role.</param>
    /// <param name="contentTokens">The number of content tokens.</param>
    [LoggerMessage(
        EventId = 1203,
        Level = LogLevel.Trace,
        Message = "Message {MessageIndex} ({Role}): {ContentTokens} tokens")]
    public static partial void MessageTokenCount(ILogger logger, int messageIndex, string role, int contentTokens);

    // =========================================================================
    // Provider Operations Events (1300-1399)
    // =========================================================================

    /// <summary>
    /// Logs parameter mapping for a provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The provider name.</param>
    /// <param name="model">The model identifier.</param>
    [LoggerMessage(
        EventId = 1300,
        Level = LogLevel.Debug,
        Message = "Mapping parameters for provider '{Provider}', model '{Model}'")]
    public static partial void MappingParameters(ILogger logger, string provider, string model);

    /// <summary>
    /// Logs provider-specific parameter adjustment.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="parameter">The parameter name.</param>
    /// <param name="originalValue">The original value.</param>
    /// <param name="adjustedValue">The adjusted value.</param>
    /// <param name="reason">The reason for adjustment.</param>
    [LoggerMessage(
        EventId = 1301,
        Level = LogLevel.Debug,
        Message = "Adjusted parameter '{Parameter}': {OriginalValue} -> {AdjustedValue} ({Reason})")]
    public static partial void ParameterAdjusted(ILogger logger, string parameter, string originalValue, string adjustedValue, string reason);

    /// <summary>
    /// Logs provider-specific validation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="provider">The provider name.</param>
    /// <param name="isValid">Whether validation passed.</param>
    [LoggerMessage(
        EventId = 1302,
        Level = LogLevel.Debug,
        Message = "Provider-aware validation for '{Provider}': IsValid={IsValid}")]
    public static partial void ProviderValidation(ILogger logger, string provider, bool isValid);

    // =========================================================================
    // Provider Registry Events (1400-1499) - v0.6.1c
    // =========================================================================

    /// <summary>
    /// Logs the start of provider resolution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name being resolved.</param>
    [LoggerMessage(
        EventId = 1400,
        Level = LogLevel.Debug,
        Message = "Resolving provider '{ProviderName}'")]
    public static partial void ResolvingProvider(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a requested provider is not found in the registry.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name that was not found.</param>
    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Warning,
        Message = "Provider '{ProviderName}' is not registered in the provider registry")]
    public static partial void ProviderNotFound(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a provider is registered but not configured (missing API key).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    [LoggerMessage(
        EventId = 1402,
        Level = LogLevel.Warning,
        Message = "Provider '{ProviderName}' is registered but not configured (missing API key)")]
    public static partial void ProviderNotConfigured(ILogger logger, string providerName);

    /// <summary>
    /// Logs successful provider resolution.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The resolved provider name.</param>
    [LoggerMessage(
        EventId = 1403,
        Level = LogLevel.Debug,
        Message = "Provider '{ProviderName}' resolved successfully")]
    public static partial void ProviderResolved(ILogger logger, string providerName);

    /// <summary>
    /// Logs when using a persisted default provider from settings.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The default provider name from settings.</param>
    [LoggerMessage(
        EventId = 1404,
        Level = LogLevel.Debug,
        Message = "Using persisted default provider '{ProviderName}' from settings")]
    public static partial void UsingPersistedDefault(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a persisted default provider is not found in the registry.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The default provider name that was not found.</param>
    [LoggerMessage(
        EventId = 1405,
        Level = LogLevel.Warning,
        Message = "Persisted default provider '{ProviderName}' is not registered; will fall back to first configured")]
    public static partial void DefaultProviderNotRegistered(ILogger logger, string providerName);

    /// <summary>
    /// Logs when no providers are configured.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1406,
        Level = LogLevel.Error,
        Message = "No configured providers available; cannot resolve default provider")]
    public static partial void NoConfiguredProviders(ILogger logger);

    /// <summary>
    /// Logs falling back to the first configured provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider being used as fallback.</param>
    [LoggerMessage(
        EventId = 1407,
        Level = LogLevel.Information,
        Message = "Falling back to first configured provider '{ProviderName}'")]
    public static partial void FallingBackToFirstConfigured(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a default provider is set.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name being set as default.</param>
    [LoggerMessage(
        EventId = 1408,
        Level = LogLevel.Information,
        Message = "Setting default provider to '{ProviderName}'")]
    public static partial void DefaultProviderSet(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a new provider is registered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The registered provider name.</param>
    /// <param name="displayName">The provider's display name.</param>
    /// <param name="modelCount">The number of supported models.</param>
    [LoggerMessage(
        EventId = 1409,
        Level = LogLevel.Information,
        Message = "Registered provider '{ProviderName}' ({DisplayName}) with {ModelCount} models")]
    public static partial void ProviderRegistered(ILogger logger, string providerName, string displayName, int modelCount);

    /// <summary>
    /// Logs the start of configuration status refresh.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerCount">The number of providers being refreshed.</param>
    [LoggerMessage(
        EventId = 1410,
        Level = LogLevel.Debug,
        Message = "Refreshing configuration status for {ProviderCount} providers")]
    public static partial void RefreshingConfigurationStatus(ILogger logger, int providerCount);

    /// <summary>
    /// Logs a provider's configuration status change.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="isConfigured">Whether the provider is now configured.</param>
    [LoggerMessage(
        EventId = 1411,
        Level = LogLevel.Debug,
        Message = "Provider '{ProviderName}' configuration status: IsConfigured={IsConfigured}")]
    public static partial void ProviderConfigurationStatusChanged(ILogger logger, string providerName, bool isConfigured);

    /// <summary>
    /// Logs completion of configuration status refresh.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="configuredCount">The number of providers that are configured.</param>
    /// <param name="totalCount">The total number of providers.</param>
    [LoggerMessage(
        EventId = 1412,
        Level = LogLevel.Information,
        Message = "Configuration status refresh completed: {ConfiguredCount}/{TotalCount} providers configured")]
    public static partial void ConfigurationStatusRefreshCompleted(ILogger logger, int configuredCount, int totalCount);

    /// <summary>
    /// Logs when a provider is already registered (duplicate registration attempt).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    [LoggerMessage(
        EventId = 1413,
        Level = LogLevel.Debug,
        Message = "Provider '{ProviderName}' is already registered; updating registration")]
    public static partial void ProviderAlreadyRegistered(ILogger logger, string providerName);

    /// <summary>
    /// Logs provider registry initialization.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="registrationCount">The number of pending registrations.</param>
    [LoggerMessage(
        EventId = 1414,
        Level = LogLevel.Information,
        Message = "Initializing provider registry with {RegistrationCount} pending registrations")]
    public static partial void InitializingProviderRegistry(ILogger logger, int registrationCount);

    /// <summary>
    /// Logs when checking provider configuration status.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="vaultKey">The vault key being checked.</param>
    [LoggerMessage(
        EventId = 1415,
        Level = LogLevel.Trace,
        Message = "Checking configuration for provider '{ProviderName}' using vault key '{VaultKey}'")]
    public static partial void CheckingProviderConfiguration(ILogger logger, string providerName, string vaultKey);

    // =========================================================================
    // LLM Settings Page Events (1500-1599) - v0.6.1d
    // =========================================================================

    /// <summary>
    /// Logs when the LLM settings page is loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerCount">The number of providers loaded.</param>
    [LoggerMessage(
        EventId = 1500,
        Level = LogLevel.Information,
        Message = "LLM settings page loaded with {ProviderCount} providers")]
    public static partial void SettingsPageLoaded(ILogger logger, int providerCount);

    /// <summary>
    /// Logs when an API key is saved for a provider.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Security:</b> The API key value is never logged; only the provider name is recorded.
    /// </para>
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    [LoggerMessage(
        EventId = 1501,
        Level = LogLevel.Information,
        Message = "API key saved for provider '{ProviderName}'")]
    public static partial void ApiKeySaved(ILogger logger, string providerName);

    /// <summary>
    /// Logs when an API key is deleted for a provider.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    [LoggerMessage(
        EventId = 1502,
        Level = LogLevel.Warning,
        Message = "API key deleted for provider '{ProviderName}'")]
    public static partial void ApiKeyDeleted(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a connection test is started.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name being tested.</param>
    [LoggerMessage(
        EventId = 1503,
        Level = LogLevel.Debug,
        Message = "Connection test started for provider '{ProviderName}'")]
    public static partial void ConnectionTestStarted(ILogger logger, string providerName);

    /// <summary>
    /// Logs when a connection test succeeds.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="latencyMs">The response latency in milliseconds.</param>
    [LoggerMessage(
        EventId = 1504,
        Level = LogLevel.Information,
        Message = "Connection test succeeded for provider '{ProviderName}' (latency: {LatencyMs}ms)")]
    public static partial void ConnectionTestSucceeded(ILogger logger, string providerName, long latencyMs);

    /// <summary>
    /// Logs when a connection test fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="reason">The reason for failure.</param>
    [LoggerMessage(
        EventId = 1505,
        Level = LogLevel.Warning,
        Message = "Connection test failed for provider '{ProviderName}': {Reason}")]
    public static partial void ConnectionTestFailed(ILogger logger, string providerName, string reason);

    /// <summary>
    /// Logs when the default provider is changed via settings UI.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The new default provider name.</param>
    [LoggerMessage(
        EventId = 1506,
        Level = LogLevel.Information,
        Message = "Default provider changed to '{ProviderName}' via settings UI")]
    public static partial void DefaultProviderChangedViaSettings(ILogger logger, string providerName);

    /// <summary>
    /// Logs when provider configuration is loaded in the settings page.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="isConfigured">Whether the provider has an API key configured.</param>
    /// <param name="isDefault">Whether this is the default provider.</param>
    [LoggerMessage(
        EventId = 1507,
        Level = LogLevel.Debug,
        Message = "Loaded provider configuration: {ProviderName} (IsConfigured={IsConfigured}, IsDefault={IsDefault})")]
    public static partial void ProviderConfigurationLoaded(ILogger logger, string providerName, bool isConfigured, bool isDefault);

    /// <summary>
    /// Logs when a provider is selected in the settings UI.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The selected provider name.</param>
    [LoggerMessage(
        EventId = 1508,
        Level = LogLevel.Debug,
        Message = "Provider '{ProviderName}' selected in settings UI")]
    public static partial void ProviderSelected(ILogger logger, string providerName);

    /// <summary>
    /// Logs when API key validation fails before saving.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="reason">The reason for validation failure.</param>
    [LoggerMessage(
        EventId = 1509,
        Level = LogLevel.Warning,
        Message = "API key validation failed for provider '{ProviderName}': {Reason}")]
    public static partial void ApiKeyValidationFailed(ILogger logger, string providerName, string reason);

    /// <summary>
    /// Logs when saving API key fails due to vault error.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="providerName">The provider name.</param>
    /// <param name="error">The error message.</param>
    [LoggerMessage(
        EventId = 1510,
        Level = LogLevel.Error,
        Message = "Failed to save API key for provider '{ProviderName}': {Error}")]
    public static partial void ApiKeySaveFailed(ILogger logger, string providerName, string error);

    // =========================================================================
    // OpenAI Provider Events (1600-1699) - v0.6.2a
    // =========================================================================

    /// <summary>
    /// Logs the start of an OpenAI completion request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier being used.</param>
    [LoggerMessage(
        EventId = 1600,
        Level = LogLevel.Debug,
        Message = "Starting OpenAI completion request for model '{Model}'")]
    public static partial void OpenAICompletionStarting(ILogger logger, string model);

    /// <summary>
    /// Logs the estimated prompt tokens for an OpenAI request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="promptTokens">The estimated number of prompt tokens.</param>
    [LoggerMessage(
        EventId = 1601,
        Level = LogLevel.Debug,
        Message = "Estimated prompt tokens: {PromptTokens}")]
    public static partial void OpenAIPromptTokensEstimated(ILogger logger, int promptTokens);

    /// <summary>
    /// Logs successful completion of an OpenAI request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <param name="promptTokens">The number of prompt tokens used.</param>
    /// <param name="completionTokens">The number of completion tokens generated.</param>
    [LoggerMessage(
        EventId = 1602,
        Level = LogLevel.Information,
        Message = "OpenAI completion succeeded in {DurationMs}ms. Tokens: {PromptTokens}/{CompletionTokens}")]
    public static partial void OpenAICompletionSucceeded(ILogger logger, long durationMs, int promptTokens, int completionTokens);

    /// <summary>
    /// Logs the start of an OpenAI streaming request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier being used.</param>
    [LoggerMessage(
        EventId = 1603,
        Level = LogLevel.Debug,
        Message = "Starting OpenAI streaming request for model '{Model}'")]
    public static partial void OpenAIStreamingStarting(ILogger logger, string model);

    /// <summary>
    /// Logs when an OpenAI stream has started receiving data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1604,
        Level = LogLevel.Debug,
        Message = "OpenAI stream started")]
    public static partial void OpenAIStreamStarted(ILogger logger);

    /// <summary>
    /// Logs receipt of an OpenAI streaming chunk.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="contentLength">The length of the content in the chunk.</param>
    [LoggerMessage(
        EventId = 1605,
        Level = LogLevel.Trace,
        Message = "OpenAI stream chunk received: {ContentLength} chars")]
    public static partial void OpenAIStreamChunkReceived(ILogger logger, int contentLength);

    /// <summary>
    /// Logs completion of an OpenAI streaming request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="finishReason">The reason the stream finished.</param>
    [LoggerMessage(
        EventId = 1606,
        Level = LogLevel.Information,
        Message = "OpenAI stream completed. Finish reason: {FinishReason}")]
    public static partial void OpenAIStreamCompleted(ILogger logger, string? finishReason);

    /// <summary>
    /// Logs an error response from the OpenAI API.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="errorType">The type of error from OpenAI.</param>
    [LoggerMessage(
        EventId = 1607,
        Level = LogLevel.Warning,
        Message = "OpenAI API returned error: {StatusCode} - {ErrorType}")]
    public static partial void OpenAIApiError(ILogger logger, int statusCode, string? errorType);

    /// <summary>
    /// Logs an HTTP request failure to OpenAI.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="message">The error message.</param>
    [LoggerMessage(
        EventId = 1608,
        Level = LogLevel.Error,
        Message = "OpenAI HTTP request failed: {Message}")]
    public static partial void OpenAIHttpRequestFailed(ILogger logger, Exception exception, string message);

    /// <summary>
    /// Logs a failure to parse an OpenAI streaming chunk.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="error">The parsing error message.</param>
    [LoggerMessage(
        EventId = 1609,
        Level = LogLevel.Warning,
        Message = "Failed to parse OpenAI streaming chunk: {Error}")]
    public static partial void OpenAIStreamChunkParseFailed(ILogger logger, string error);

    /// <summary>
    /// Logs the building of an OpenAI HTTP request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The API endpoint being called.</param>
    [LoggerMessage(
        EventId = 1610,
        Level = LogLevel.Debug,
        Message = "Building OpenAI HTTP request for endpoint {Endpoint}")]
    public static partial void OpenAIBuildingRequest(ILogger logger, string endpoint);

    /// <summary>
    /// Logs retrieval of the API key from the vault.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Security:</b> The API key value is never logged; only the retrieval action is recorded.
    /// </para>
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1611,
        Level = LogLevel.Debug,
        Message = "Retrieving OpenAI API key from vault")]
    public static partial void OpenAIRetrievingApiKey(ILogger logger);

    /// <summary>
    /// Logs when the API key is not found in the vault.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1612,
        Level = LogLevel.Warning,
        Message = "OpenAI API key not found in vault")]
    public static partial void OpenAIApiKeyNotFound(ILogger logger);

    /// <summary>
    /// Logs parsing of a successful OpenAI response.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1613,
        Level = LogLevel.Debug,
        Message = "Parsing OpenAI success response")]
    public static partial void OpenAIParsingSuccessResponse(ILogger logger);

    /// <summary>
    /// Logs parsing of an OpenAI error response.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="statusCode">The HTTP status code of the error response.</param>
    [LoggerMessage(
        EventId = 1614,
        Level = LogLevel.Debug,
        Message = "Parsing OpenAI error response: {StatusCode}")]
    public static partial void OpenAIParsingErrorResponse(ILogger logger, int statusCode);

    /// <summary>
    /// Logs the raw OpenAI response size for debugging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="responseLength">The length of the response in bytes.</param>
    [LoggerMessage(
        EventId = 1615,
        Level = LogLevel.Trace,
        Message = "Raw OpenAI response: {ResponseLength} bytes")]
    public static partial void OpenAIRawResponse(ILogger logger, int responseLength);

    // =========================================================================
    // Anthropic Provider Events (1700-1799) - v0.6.2b
    // =========================================================================

    /// <summary>
    /// Logs the start of an Anthropic completion request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier being used.</param>
    [LoggerMessage(
        EventId = 1700,
        Level = LogLevel.Debug,
        Message = "Starting Anthropic completion request for model '{Model}'")]
    public static partial void AnthropicCompletionStarting(ILogger logger, string model);

    /// <summary>
    /// Logs the estimated prompt tokens for an Anthropic request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="promptTokens">The estimated number of prompt tokens.</param>
    [LoggerMessage(
        EventId = 1701,
        Level = LogLevel.Debug,
        Message = "Estimated Anthropic prompt tokens: {PromptTokens}")]
    public static partial void AnthropicPromptTokensEstimated(ILogger logger, int promptTokens);

    /// <summary>
    /// Logs successful completion of an Anthropic request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="durationMs">The request duration in milliseconds.</param>
    /// <param name="inputTokens">The number of input tokens used.</param>
    /// <param name="outputTokens">The number of output tokens generated.</param>
    [LoggerMessage(
        EventId = 1702,
        Level = LogLevel.Information,
        Message = "Anthropic completion succeeded in {DurationMs}ms. Tokens: {InputTokens}/{OutputTokens}")]
    public static partial void AnthropicCompletionSucceeded(ILogger logger, long durationMs, int inputTokens, int outputTokens);

    /// <summary>
    /// Logs the start of an Anthropic streaming request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier being used.</param>
    [LoggerMessage(
        EventId = 1703,
        Level = LogLevel.Debug,
        Message = "Starting Anthropic streaming request for model '{Model}'")]
    public static partial void AnthropicStreamingStarting(ILogger logger, string model);

    /// <summary>
    /// Logs when an Anthropic stream has started receiving data.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1704,
        Level = LogLevel.Debug,
        Message = "Anthropic stream started")]
    public static partial void AnthropicStreamStarted(ILogger logger);

    /// <summary>
    /// Logs receipt of an Anthropic streaming chunk.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="contentLength">The length of the content in the chunk.</param>
    [LoggerMessage(
        EventId = 1705,
        Level = LogLevel.Trace,
        Message = "Anthropic stream chunk received: {ContentLength} chars")]
    public static partial void AnthropicStreamChunkReceived(ILogger logger, int contentLength);

    /// <summary>
    /// Logs completion of an Anthropic streaming request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="finishReason">The reason the stream finished.</param>
    [LoggerMessage(
        EventId = 1706,
        Level = LogLevel.Information,
        Message = "Anthropic stream completed. Finish reason: {FinishReason}")]
    public static partial void AnthropicStreamCompleted(ILogger logger, string? finishReason);

    /// <summary>
    /// Logs an error response from the Anthropic API.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="statusCode">The HTTP status code returned.</param>
    /// <param name="errorType">The type of error from Anthropic.</param>
    [LoggerMessage(
        EventId = 1707,
        Level = LogLevel.Warning,
        Message = "Anthropic API returned error: {StatusCode} - {ErrorType}")]
    public static partial void AnthropicApiError(ILogger logger, int statusCode, string? errorType);

    /// <summary>
    /// Logs an HTTP request failure to Anthropic.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception that occurred.</param>
    /// <param name="message">The error message.</param>
    [LoggerMessage(
        EventId = 1708,
        Level = LogLevel.Error,
        Message = "Anthropic HTTP request failed: {Message}")]
    public static partial void AnthropicHttpRequestFailed(ILogger logger, Exception exception, string message);

    /// <summary>
    /// Logs a failure to parse an Anthropic streaming chunk.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="error">The parsing error message.</param>
    [LoggerMessage(
        EventId = 1709,
        Level = LogLevel.Warning,
        Message = "Failed to parse Anthropic streaming chunk: {Error}")]
    public static partial void AnthropicStreamChunkParseFailed(ILogger logger, string error);

    /// <summary>
    /// Logs the building of an Anthropic HTTP request.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="endpoint">The API endpoint being called.</param>
    [LoggerMessage(
        EventId = 1710,
        Level = LogLevel.Debug,
        Message = "Building Anthropic HTTP request for endpoint {Endpoint}")]
    public static partial void AnthropicBuildingRequest(ILogger logger, string endpoint);

    /// <summary>
    /// Logs retrieval of the Anthropic API key from the vault.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Security:</b> The API key value is never logged; only the retrieval action is recorded.
    /// </para>
    /// </remarks>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1711,
        Level = LogLevel.Debug,
        Message = "Retrieving Anthropic API key from vault")]
    public static partial void AnthropicRetrievingApiKey(ILogger logger);

    /// <summary>
    /// Logs when the Anthropic API key is not found in the vault.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1712,
        Level = LogLevel.Warning,
        Message = "Anthropic API key not found in vault")]
    public static partial void AnthropicApiKeyNotFound(ILogger logger);

    /// <summary>
    /// Logs parsing of a successful Anthropic response.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1713,
        Level = LogLevel.Debug,
        Message = "Parsing Anthropic success response")]
    public static partial void AnthropicParsingSuccessResponse(ILogger logger);

    /// <summary>
    /// Logs parsing of an Anthropic error response.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="statusCode">The HTTP status code of the error response.</param>
    [LoggerMessage(
        EventId = 1714,
        Level = LogLevel.Debug,
        Message = "Parsing Anthropic error response: {StatusCode}")]
    public static partial void AnthropicParsingErrorResponse(ILogger logger, int statusCode);

    /// <summary>
    /// Logs the raw Anthropic response size for debugging.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="responseLength">The length of the response in bytes.</param>
    [LoggerMessage(
        EventId = 1715,
        Level = LogLevel.Trace,
        Message = "Raw Anthropic response: {ResponseLength} bytes")]
    public static partial void AnthropicRawResponse(ILogger logger, int responseLength);

    // =========================================================================
    // Resilience Policy Events (1800-1899) - v0.6.2c
    // =========================================================================

    /// <summary>
    /// Logs when a resilience pipeline is created.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1800,
        Level = LogLevel.Debug,
        Message = "Resilience pipeline created with policies: Bulkhead, Timeout, CircuitBreaker, Retry")]
    public static partial void ResiliencePipelineCreated(ILogger logger);

    /// <summary>
    /// Logs when executing a request through the resilience pipeline.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1801,
        Level = LogLevel.Debug,
        Message = "Executing request through resilience pipeline")]
    public static partial void ResiliencePipelineExecuting(ILogger logger);

    /// <summary>
    /// Logs when a retry attempt is triggered.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="attempt">The retry attempt number.</param>
    /// <param name="maxRetries">The maximum number of retries configured.</param>
    /// <param name="delayMs">The delay before the retry in milliseconds.</param>
    /// <param name="reason">The reason for the retry.</param>
    [LoggerMessage(
        EventId = 1802,
        Level = LogLevel.Warning,
        Message = "Retry attempt {Attempt}/{MaxRetries} after {DelayMs}ms. Reason: {Reason}")]
    public static partial void ResilienceRetryAttempt(ILogger logger, int attempt, int maxRetries, double delayMs, string reason);

    /// <summary>
    /// Logs when the circuit breaker opens (trips).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="breakDurationSeconds">The duration the circuit will remain open.</param>
    /// <param name="reason">The reason the circuit opened.</param>
    [LoggerMessage(
        EventId = 1803,
        Level = LogLevel.Warning,
        Message = "Circuit breaker opened for {BreakDurationSeconds}s. Reason: {Reason}")]
    public static partial void ResilienceCircuitBreakerOpened(ILogger logger, double breakDurationSeconds, string reason);

    /// <summary>
    /// Logs when the circuit breaker resets to closed state.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1804,
        Level = LogLevel.Information,
        Message = "Circuit breaker reset - resuming normal operations")]
    public static partial void ResilienceCircuitBreakerReset(ILogger logger);

    /// <summary>
    /// Logs when the circuit breaker transitions to half-open state.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1805,
        Level = LogLevel.Information,
        Message = "Circuit breaker half-open - testing with next request")]
    public static partial void ResilienceCircuitBreakerHalfOpen(ILogger logger);

    /// <summary>
    /// Logs when a request times out.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="timeoutSeconds">The timeout duration that was exceeded.</param>
    [LoggerMessage(
        EventId = 1806,
        Level = LogLevel.Warning,
        Message = "Request timed out after {TimeoutSeconds}s")]
    public static partial void ResilienceRequestTimeout(ILogger logger, double timeoutSeconds);

    /// <summary>
    /// Logs when the bulkhead rejects a request due to capacity.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    [LoggerMessage(
        EventId = 1807,
        Level = LogLevel.Warning,
        Message = "Bulkhead rejected request - system at capacity")]
    public static partial void ResilienceBulkheadRejected(ILogger logger);

    /// <summary>
    /// Logs when the policy wrap is constructed.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="retryCount">The configured retry count.</param>
    /// <param name="circuitBreakerThreshold">The circuit breaker failure threshold.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <param name="bulkheadConcurrency">The bulkhead max concurrency.</param>
    [LoggerMessage(
        EventId = 1808,
        Level = LogLevel.Debug,
        Message = "Policy wrap constructed: Retry={RetryCount}, CircuitBreaker={CircuitBreakerThreshold} failures, Timeout={TimeoutSeconds}s, Bulkhead={BulkheadConcurrency} concurrent")]
    public static partial void ResiliencePolicyWrapConstructed(ILogger logger, int retryCount, int circuitBreakerThreshold, int timeoutSeconds, int bulkheadConcurrency);

    /// <summary>
    /// Logs when using the Retry-After header for delay calculation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="retryAfterSeconds">The Retry-After header value in seconds.</param>
    [LoggerMessage(
        EventId = 1809,
        Level = LogLevel.Debug,
        Message = "Using Retry-After header: {RetryAfterSeconds}s")]
    public static partial void ResilienceUsingRetryAfterHeader(ILogger logger, double retryAfterSeconds);

    /// <summary>
    /// Logs the calculated exponential backoff delay.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="attempt">The retry attempt number.</param>
    /// <param name="baseDelayMs">The base delay in milliseconds.</param>
    /// <param name="jitterMs">The jitter added in milliseconds.</param>
    /// <param name="totalDelayMs">The total delay in milliseconds.</param>
    [LoggerMessage(
        EventId = 1810,
        Level = LogLevel.Debug,
        Message = "Exponential backoff: attempt={Attempt}, base={BaseDelayMs}ms, jitter={JitterMs}ms, total={TotalDelayMs}ms")]
    public static partial void ResilienceExponentialBackoff(ILogger logger, int attempt, double baseDelayMs, double jitterMs, double totalDelayMs);

    /// <summary>
    /// Logs when a resilience event is raised.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="policyName">The policy that raised the event.</param>
    /// <param name="eventType">The type of event.</param>
    [LoggerMessage(
        EventId = 1811,
        Level = LogLevel.Trace,
        Message = "Resilience event raised: Policy={PolicyName}, Type={EventType}")]
    public static partial void ResilienceEventRaised(ILogger logger, string policyName, string eventType);

    /// <summary>
    /// Logs when resilience configuration is loaded.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="retryCount">The configured retry count.</param>
    /// <param name="timeoutSeconds">The configured timeout in seconds.</param>
    /// <param name="circuitBreakerThreshold">The circuit breaker threshold.</param>
    /// <param name="bulkheadConcurrency">The bulkhead max concurrency.</param>
    [LoggerMessage(
        EventId = 1812,
        Level = LogLevel.Information,
        Message = "Resilience configuration loaded: Retry={RetryCount}, Timeout={TimeoutSeconds}s, CircuitBreaker={CircuitBreakerThreshold} failures, Bulkhead={BulkheadConcurrency} concurrent")]
    public static partial void ResilienceConfigurationLoaded(ILogger logger, int retryCount, int timeoutSeconds, int circuitBreakerThreshold, int bulkheadConcurrency);

    /// <summary>
    /// Logs a warning when resilience options validation fails.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="errors">The validation error messages.</param>
    [LoggerMessage(
        EventId = 1813,
        Level = LogLevel.Warning,
        Message = "Resilience options validation warning: {Errors}")]
    public static partial void ResilienceOptionsValidationWarning(ILogger logger, string errors);

    /// <summary>
    /// Logs when resilience pipeline execution fails after all retries.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The final exception.</param>
    /// <param name="message">The error message.</param>
    [LoggerMessage(
        EventId = 1814,
        Level = LogLevel.Error,
        Message = "Resilience pipeline execution failed after all retries: {Message}")]
    public static partial void ResiliencePipelineExecutionFailed(ILogger logger, Exception exception, string message);

    /// <summary>
    /// Logs when the circuit breaker health check is queried.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="circuitState">The current circuit state.</param>
    [LoggerMessage(
        EventId = 1815,
        Level = LogLevel.Debug,
        Message = "Circuit breaker health check queried: State={CircuitState}")]
    public static partial void ResilienceHealthCheckQueried(ILogger logger, string circuitState);

    /// <summary>
    /// Logs when resilience pipeline execution completes successfully.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="durationMs">The total execution duration in milliseconds.</param>
    [LoggerMessage(
        EventId = 1816,
        Level = LogLevel.Debug,
        Message = "Resilience pipeline execution completed in {DurationMs}ms")]
    public static partial void ResiliencePipelineExecutionCompleted(ILogger logger, long durationMs);

    /// <summary>
    /// Logs when a transient HTTP error is detected.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="statusCode">The HTTP status code.</param>
    [LoggerMessage(
        EventId = 1817,
        Level = LogLevel.Debug,
        Message = "Transient HTTP error detected: StatusCode={StatusCode}")]
    public static partial void ResilienceTransientErrorDetected(ILogger logger, int statusCode);

    /// <summary>
    /// Logs when the delay is capped at maximum.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="calculatedDelayMs">The calculated delay before capping.</param>
    /// <param name="maxDelayMs">The maximum delay cap.</param>
    [LoggerMessage(
        EventId = 1818,
        Level = LogLevel.Debug,
        Message = "Delay capped at maximum: calculated={CalculatedDelayMs}ms, max={MaxDelayMs}ms")]
    public static partial void ResilienceDelayCapped(ILogger logger, double calculatedDelayMs, double maxDelayMs);

    // =========================================================================
    // Token Counting Service Events (1900-1909) - v0.6.2d
    // =========================================================================

    /// <summary>
    /// Logs when tokens are counted for plain text.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="textLength">The length of the input text in characters.</param>
    /// <param name="tokenCount">The counted token count.</param>
    [LoggerMessage(
        EventId = 1900,
        Level = LogLevel.Trace,
        Message = "Tokens counted for text: Model={Model}, TextLength={TextLength} chars, TokenCount={TokenCount}")]
    public static partial void TokenCounterTextCounted(ILogger logger, string model, int textLength, int tokenCount);

    /// <summary>
    /// Logs when tokens are counted for chat messages.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="messageCount">The number of messages.</param>
    /// <param name="tokenCount">The total token count including overhead.</param>
    [LoggerMessage(
        EventId = 1901,
        Level = LogLevel.Debug,
        Message = "Tokens counted for messages: Model={Model}, Messages={MessageCount}, TokenCount={TokenCount}")]
    public static partial void TokenCounterMessagesCounted(ILogger logger, string model, int messageCount, int tokenCount);

    /// <summary>
    /// Logs response token estimation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="promptTokens">The number of prompt tokens.</param>
    /// <param name="maxTokens">The maximum tokens requested.</param>
    /// <param name="estimatedTokens">The estimated response tokens.</param>
    [LoggerMessage(
        EventId = 1902,
        Level = LogLevel.Debug,
        Message = "Response tokens estimated: PromptTokens={PromptTokens}, MaxTokens={MaxTokens}, Estimated={EstimatedTokens}")]
    public static partial void TokenCounterResponseEstimated(ILogger logger, int promptTokens, int maxTokens, int estimatedTokens);

    /// <summary>
    /// Logs cost calculation.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="inputTokens">The number of input tokens.</param>
    /// <param name="outputTokens">The number of output tokens.</param>
    /// <param name="cost">The calculated cost in USD.</param>
    [LoggerMessage(
        EventId = 1903,
        Level = LogLevel.Debug,
        Message = "Cost calculated: Model={Model}, InputTokens={InputTokens}, OutputTokens={OutputTokens}, Cost=${Cost}")]
    public static partial void TokenCounterCostCalculated(ILogger logger, string model, int inputTokens, int outputTokens, decimal cost);

    /// <summary>
    /// Logs when a tokenizer is created for a known model.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="encoding">The tokenizer encoding (e.g., "o200k_base", "cl100k_base", "approximation").</param>
    /// <param name="isExact">Whether the tokenizer provides exact counts.</param>
    [LoggerMessage(
        EventId = 1904,
        Level = LogLevel.Debug,
        Message = "Tokenizer created: Model={Model}, Encoding={Encoding}, IsExact={IsExact}")]
    public static partial void TokenizerCreated(ILogger logger, string model, string encoding, bool isExact);

    /// <summary>
    /// Logs when a tokenizer is created for an unknown model (uses approximation).
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The unknown model identifier.</param>
    [LoggerMessage(
        EventId = 1905,
        Level = LogLevel.Warning,
        Message = "Tokenizer created for unknown model '{Model}' - using approximation")]
    public static partial void TokenizerCreatedUnknownModel(ILogger logger, string model);

    /// <summary>
    /// Logs a tokenizer cache hit.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cacheKey">The cache key that was hit.</param>
    [LoggerMessage(
        EventId = 1906,
        Level = LogLevel.Trace,
        Message = "Tokenizer cache hit: Key={CacheKey}")]
    public static partial void TokenizerCacheHit(ILogger logger, string cacheKey);

    /// <summary>
    /// Logs when a tokenizer is being created and cached.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="cacheKey">The cache key for the new tokenizer.</param>
    /// <param name="model">The original model identifier.</param>
    [LoggerMessage(
        EventId = 1907,
        Level = LogLevel.Debug,
        Message = "Tokenizer cache creating: Key={CacheKey}, Model={Model}")]
    public static partial void TokenizerCacheCreating(ILogger logger, string cacheKey, string model);

    /// <summary>
    /// Logs when the tokenizer cache is cleared.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="entriesCleared">The number of entries that were cleared.</param>
    [LoggerMessage(
        EventId = 1908,
        Level = LogLevel.Information,
        Message = "Tokenizer cache cleared: {EntriesCleared} entries removed")]
    public static partial void TokenizerCacheCleared(ILogger logger, int entriesCleared);

    /// <summary>
    /// Logs when pricing data is not found for a model.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier without pricing data.</param>
    [LoggerMessage(
        EventId = 1909,
        Level = LogLevel.Debug,
        Message = "Model pricing not found: Model={Model}")]
    public static partial void TokenCounterPricingNotFound(ILogger logger, string model);

    /// <summary>
    /// Logs details about individual message token counting.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="messageIndex">The message index (1-based).</param>
    /// <param name="role">The message role.</param>
    /// <param name="contentTokens">The number of content tokens.</param>
    /// <param name="overheadTokens">The overhead tokens added.</param>
    [LoggerMessage(
        EventId = 1910,
        Level = LogLevel.Trace,
        Message = "Message {MessageIndex} ({Role}): ContentTokens={ContentTokens}, Overhead={OverheadTokens}")]
    public static partial void TokenCounterMessageDetail(ILogger logger, int messageIndex, string role, int contentTokens, int overheadTokens);

    /// <summary>
    /// Logs when a model limit is queried.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="limit">The context window limit.</param>
    [LoggerMessage(
        EventId = 1911,
        Level = LogLevel.Trace,
        Message = "Model limit queried: Model={Model}, Limit={Limit}")]
    public static partial void TokenCounterModelLimitQueried(ILogger logger, string model, int limit);

    /// <summary>
    /// Logs when max output tokens is queried.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="model">The model identifier.</param>
    /// <param name="maxOutput">The maximum output tokens.</param>
    [LoggerMessage(
        EventId = 1912,
        Level = LogLevel.Trace,
        Message = "Max output tokens queried: Model={Model}, MaxOutput={MaxOutput}")]
    public static partial void TokenCounterMaxOutputQueried(ILogger logger, string model, int maxOutput);
}
