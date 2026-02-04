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
}
