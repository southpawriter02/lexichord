// -----------------------------------------------------------------------
// <copyright file="ChatOptionsResolver.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Configuration;
using Lexichord.Modules.LLM.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.LLM.Services;

/// <summary>
/// Resolves <see cref="ChatOptions"/> by merging defaults, user preferences, and request overrides.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="ChatOptionsResolver"/> implements a layered resolution pipeline
/// that applies configuration from multiple sources in order of precedence:
/// </para>
/// <list type="number">
///   <item><description>Model-specific defaults from <see cref="ModelDefaults"/>.</description></item>
///   <item><description>Configuration defaults from <see cref="LLMOptions.Defaults"/>.</description></item>
///   <item><description>User preferences from <see cref="ISystemSettingsRepository"/>.</description></item>
///   <item><description>Request-specific overrides from the caller.</description></item>
/// </list>
/// <para>
/// The final resolved options are validated before being returned. If validation fails,
/// a <see cref="ChatOptionsValidationException"/> is thrown.
/// </para>
/// </remarks>
public class ChatOptionsResolver
{
    private readonly IOptions<LLMOptions> _options;
    private readonly ISystemSettingsRepository _settings;
    private readonly IValidator<ChatOptions> _validator;
    private readonly ILogger<ChatOptionsResolver> _logger;

    /// <summary>
    /// Settings key prefix for user LLM preferences.
    /// </summary>
    private const string UserSettingsPrefix = "LLM.User";

    /// <summary>
    /// Settings key for user's preferred temperature.
    /// </summary>
    private const string UserTemperatureKey = "LLM.User.Temperature";

    /// <summary>
    /// Settings key for user's preferred max tokens.
    /// </summary>
    private const string UserMaxTokensKey = "LLM.User.MaxTokens";

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatOptionsResolver"/> class.
    /// </summary>
    /// <param name="options">The LLM configuration options.</param>
    /// <param name="settings">The system settings repository for user preferences.</param>
    /// <param name="validator">The chat options validator.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public ChatOptionsResolver(
        IOptions<LLMOptions> options,
        ISystemSettingsRepository settings,
        IValidator<ChatOptions> validator,
        ILogger<ChatOptionsResolver> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Resolves the final <see cref="ChatOptions"/> by applying the resolution pipeline.
    /// </summary>
    /// <param name="requestOptions">Options provided with the request (highest precedence).</param>
    /// <param name="providerName">Target provider for model-specific defaults.</param>
    /// <param name="ct">A cancellation token to cancel the operation.</param>
    /// <returns>Fully resolved and validated <see cref="ChatOptions"/>.</returns>
    /// <exception cref="ChatOptionsValidationException">
    /// Thrown when the resolved options fail validation.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Resolution order:
    /// </para>
    /// <list type="number">
    ///   <item><description>Determine the base model (request → user default → provider default).</description></item>
    ///   <item><description>Load model-specific defaults.</description></item>
    ///   <item><description>Apply configuration defaults from <see cref="LLMOptions.Defaults"/>.</description></item>
    ///   <item><description>Apply user preferences from settings.</description></item>
    ///   <item><description>Apply request overrides.</description></item>
    ///   <item><description>Validate and return final options.</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Resolve with request overrides
    /// var requestOptions = new ChatOptions(Temperature: 0.9);
    /// var resolved = await resolver.ResolveAsync(requestOptions, "openai");
    ///
    /// // Resolve with defaults only
    /// var defaults = await resolver.ResolveAsync(null, "anthropic");
    /// </code>
    /// </example>
    public async Task<ChatOptions> ResolveAsync(
        ChatOptions? requestOptions,
        string providerName,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        LLMLogEvents.ResolvingOptions(_logger, providerName);

        // Step 1: Determine the base model
        var (model, modelSource) = await DetermineModelAsync(requestOptions, providerName, ct);
        LLMLogEvents.ResolvedModel(_logger, model, modelSource);

        // Step 2: Load model-specific defaults
        var modelDefaults = ModelDefaults.GetDefaults(model);
        LLMLogEvents.LoadedModelDefaults(_logger, model, modelDefaults.MaxTokens ?? 0);

        // Step 3: Apply configuration defaults
        var withConfigDefaults = ApplyConfigurationDefaults(modelDefaults);

        // Step 4: Apply user preferences
        var withUserPrefs = await ApplyUserPreferencesAsync(withConfigDefaults, ct);

        // Step 5: Apply request overrides
        var finalOptions = MergeOptions(withUserPrefs, requestOptions);

        // Step 6: Validate
        var result = await _validator.ValidateAsync(finalOptions, ct);
        if (!result.IsValid)
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
            LLMLogEvents.ValidationFailed(_logger, providerName, errorMessages);

            var errors = result.Errors
                .Select(e => new ValidationError(
                    Property: e.PropertyName,
                    Message: e.ErrorMessage,
                    ErrorCode: e.ErrorCode,
                    AttemptedValue: e.AttemptedValue))
                .ToList();

            throw new ChatOptionsValidationException(errors);
        }

        LLMLogEvents.FinalOptions(
            _logger,
            finalOptions.Model ?? model,
            finalOptions.Temperature ?? 0.7,
            finalOptions.MaxTokens ?? 2048);

        return finalOptions;
    }

    /// <summary>
    /// Resolves options synchronously using default values only (no user preferences).
    /// </summary>
    /// <param name="requestOptions">Options provided with the request.</param>
    /// <param name="providerName">Target provider name.</param>
    /// <returns>Resolved <see cref="ChatOptions"/>.</returns>
    /// <remarks>
    /// This synchronous overload skips user preference lookup from the settings repository.
    /// Use this for scenarios where async access is not available or user preferences
    /// are not needed.
    /// </remarks>
    public ChatOptions ResolveSync(ChatOptions? requestOptions, string providerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));

        LLMLogEvents.ResolvingOptions(_logger, providerName);

        // Determine model from request or provider default (no async user lookup)
        var model = requestOptions?.Model ?? GetProviderDefaultModel(providerName);
        LLMLogEvents.ResolvedModel(_logger, model, requestOptions?.Model is not null ? "request" : "provider-default");

        var modelDefaults = ModelDefaults.GetDefaults(model);
        var withConfigDefaults = ApplyConfigurationDefaults(modelDefaults);
        var finalOptions = MergeOptions(withConfigDefaults, requestOptions);

        var result = _validator.Validate(finalOptions);
        if (!result.IsValid)
        {
            var errorMessages = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
            LLMLogEvents.ValidationFailed(_logger, providerName, errorMessages);

            var errors = result.Errors
                .Select(e => new ValidationError(
                    Property: e.PropertyName,
                    Message: e.ErrorMessage,
                    ErrorCode: e.ErrorCode,
                    AttemptedValue: e.AttemptedValue))
                .ToList();

            throw new ChatOptionsValidationException(errors);
        }

        LLMLogEvents.FinalOptions(
            _logger,
            finalOptions.Model ?? model,
            finalOptions.Temperature ?? 0.7,
            finalOptions.MaxTokens ?? 2048);

        return finalOptions;
    }

    /// <summary>
    /// Determines the model to use based on request, user settings, and provider defaults.
    /// </summary>
    private async Task<(string Model, string Source)> DetermineModelAsync(
        ChatOptions? requestOptions,
        string providerName,
        CancellationToken ct)
    {
        // Priority 1: Request-specified model
        if (!string.IsNullOrWhiteSpace(requestOptions?.Model))
        {
            return (requestOptions.Model, "request");
        }

        // Priority 2: User's default model for this provider
        var userModelKey = $"{UserSettingsPrefix}.{providerName}.DefaultModel";
        var userModel = await _settings.GetValueAsync(userModelKey, string.Empty, ct);
        if (!string.IsNullOrWhiteSpace(userModel))
        {
            return (userModel, "user-default");
        }

        // Priority 3: Provider's configured default model
        var providerDefault = GetProviderDefaultModel(providerName);
        return (providerDefault, "provider-default");
    }

    /// <summary>
    /// Gets the default model for a provider from configuration.
    /// </summary>
    private string GetProviderDefaultModel(string providerName)
    {
        var providerOptions = _options.Value.GetProviderOptions(providerName);

        if (!string.IsNullOrWhiteSpace(providerOptions?.DefaultModel))
        {
            return providerOptions.DefaultModel;
        }

        // Fall back to a sensible default
        return providerName.Equals("anthropic", StringComparison.OrdinalIgnoreCase)
            ? "claude-3-haiku-20240307"
            : "gpt-4o-mini";
    }

    /// <summary>
    /// Applies configuration defaults from <see cref="LLMOptions.Defaults"/>.
    /// </summary>
    private ChatOptions ApplyConfigurationDefaults(ChatOptions baseOptions)
    {
        var defaults = _options.Value.Defaults;

        // LOGIC: Only apply defaults for properties not already set in base options.
        return baseOptions with
        {
            Temperature = baseOptions.Temperature ?? defaults.Temperature,
            MaxTokens = baseOptions.MaxTokens ?? defaults.MaxTokens,
            TopP = baseOptions.TopP ?? defaults.TopP,
            FrequencyPenalty = baseOptions.FrequencyPenalty ?? defaults.FrequencyPenalty,
            PresencePenalty = baseOptions.PresencePenalty ?? defaults.PresencePenalty
        };
    }

    /// <summary>
    /// Applies user preferences from the settings repository.
    /// </summary>
    private async Task<ChatOptions> ApplyUserPreferencesAsync(
        ChatOptions baseOptions,
        CancellationToken ct)
    {
        // LOGIC: User preferences override configuration defaults but not request overrides.
        // This allows users to set their preferred settings while still allowing
        // per-request customization.

        var userTemp = await _settings.GetValueAsync<double?>(UserTemperatureKey, null, ct);
        var userMaxTokens = await _settings.GetValueAsync<int?>(UserMaxTokensKey, null, ct);

        if (userTemp.HasValue || userMaxTokens.HasValue)
        {
            LLMLogEvents.AppliedUserPreferences(_logger, userTemp, userMaxTokens);
        }

        return baseOptions with
        {
            Temperature = userTemp ?? baseOptions.Temperature,
            MaxTokens = userMaxTokens ?? baseOptions.MaxTokens
        };
    }

    /// <summary>
    /// Merges request overrides onto the base options.
    /// </summary>
    /// <param name="baseOptions">The resolved base options.</param>
    /// <param name="overrides">Request-specific overrides.</param>
    /// <returns>Merged options with request overrides applied.</returns>
    private static ChatOptions MergeOptions(ChatOptions baseOptions, ChatOptions? overrides)
    {
        if (overrides is null)
        {
            return baseOptions;
        }

        // LOGIC: Only apply override values that are explicitly set (non-null).
        // This allows partial overrides where the caller only specifies
        // the options they want to change.
        return baseOptions with
        {
            Model = overrides.Model ?? baseOptions.Model,
            Temperature = overrides.Temperature ?? baseOptions.Temperature,
            MaxTokens = overrides.MaxTokens ?? baseOptions.MaxTokens,
            TopP = overrides.TopP ?? baseOptions.TopP,
            FrequencyPenalty = overrides.FrequencyPenalty ?? baseOptions.FrequencyPenalty,
            PresencePenalty = overrides.PresencePenalty ?? baseOptions.PresencePenalty,
            StopSequences = overrides.StopSequences ?? baseOptions.StopSequences
        };
    }
}
