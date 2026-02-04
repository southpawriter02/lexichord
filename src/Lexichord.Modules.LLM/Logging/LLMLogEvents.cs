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
}
