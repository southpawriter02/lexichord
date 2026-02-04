// -----------------------------------------------------------------------
// <copyright file="ProviderAwareChatOptionsValidator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.LLM.Configuration;
using Lexichord.Modules.LLM.Logging;
using Lexichord.Modules.LLM.Services;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.LLM.Validation;

/// <summary>
/// Provider-specific validation rules for <see cref="ChatOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// This validator extends the base <see cref="ChatOptionsValidator"/> with additional
/// rules specific to individual LLM providers.
/// </para>
/// <para>
/// <b>Provider-Specific Rules:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Anthropic: Temperature must be 0.0-1.0 (mapped from 0.0-2.0)</description></item>
///   <item><description>Anthropic: FrequencyPenalty and PresencePenalty not supported</description></item>
///   <item><description>All providers: Model must be available for the target provider</description></item>
/// </list>
/// <para>
/// <b>Async Validation:</b>
/// </para>
/// <para>
/// Model availability checking is asynchronous because it may require fetching
/// from the provider's API. Use <see cref="AbstractValidator{T}.ValidateAsync"/>
/// for full validation.
/// </para>
/// </remarks>
public class ProviderAwareChatOptionsValidator : AbstractValidator<ChatOptions>
{
    private readonly string _providerName;
    private readonly ModelRegistry _modelRegistry;
    private readonly ILogger<ProviderAwareChatOptionsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderAwareChatOptionsValidator"/> class.
    /// </summary>
    /// <param name="providerName">The target provider name (e.g., "openai", "anthropic").</param>
    /// <param name="modelRegistry">The model registry for model availability checks.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/> is null or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="modelRegistry"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ProviderAwareChatOptionsValidator(
        string providerName,
        ModelRegistry modelRegistry,
        ILogger<ProviderAwareChatOptionsValidator> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName, nameof(providerName));
        _providerName = providerName;
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // RULE: Include all base validation rules.
        Include(new ChatOptionsValidator());

        // RULE: Provider-specific rules.
        ConfigureProviderSpecificRules();

        // RULE: Model availability check (async).
        ConfigureModelAvailabilityRule();
    }

    /// <summary>
    /// Configures validation rules specific to the target provider.
    /// </summary>
    private void ConfigureProviderSpecificRules()
    {
        if (_providerName.Equals("anthropic", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureAnthropicRules();
        }
        else if (_providerName.Equals("openai", StringComparison.OrdinalIgnoreCase))
        {
            ConfigureOpenAIRules();
        }
    }

    /// <summary>
    /// Configures Anthropic-specific validation rules.
    /// </summary>
    private void ConfigureAnthropicRules()
    {
        // RULE: Anthropic temperature is 0.0-1.0 (we map from 0.0-2.0).
        // Warn if temperature > 2.0 because it will be clamped to 1.0.
        When(x => x.Temperature.HasValue, () =>
        {
            RuleFor(x => x.Temperature!.Value)
                .LessThanOrEqualTo(2.0)
                .WithMessage("Temperature values above 2.0 will be clamped to 1.0 for Anthropic.")
                .WithSeverity(Severity.Warning)
                .WithErrorCode("ANTHROPIC_TEMPERATURE_CLAMPED");
        });

        // RULE: Anthropic does not support frequency or presence penalties.
        When(x => x.FrequencyPenalty.HasValue && x.FrequencyPenalty.Value != 0.0, () =>
        {
            RuleFor(x => x.FrequencyPenalty!.Value)
                .Must(_ => false)
                .WithMessage("Anthropic does not support FrequencyPenalty. This parameter will be ignored.")
                .WithSeverity(Severity.Warning)
                .WithErrorCode("ANTHROPIC_NO_FREQUENCY_PENALTY");
        });

        When(x => x.PresencePenalty.HasValue && x.PresencePenalty.Value != 0.0, () =>
        {
            RuleFor(x => x.PresencePenalty!.Value)
                .Must(_ => false)
                .WithMessage("Anthropic does not support PresencePenalty. This parameter will be ignored.")
                .WithSeverity(Severity.Warning)
                .WithErrorCode("ANTHROPIC_NO_PRESENCE_PENALTY");
        });
    }

    /// <summary>
    /// Configures OpenAI-specific validation rules.
    /// </summary>
    private void ConfigureOpenAIRules()
    {
        // OpenAI supports all parameters, no additional restrictions needed.
        // Rules could be added here for specific model restrictions.
    }

    /// <summary>
    /// Configures the async model availability validation rule.
    /// </summary>
    private void ConfigureModelAvailabilityRule()
    {
        When(x => !string.IsNullOrWhiteSpace(x.Model), () =>
        {
            RuleFor(x => x.Model!)
                .MustAsync(async (model, ct) => await IsModelAvailableAsync(model, ct))
                .WithMessage(x => $"Model '{x.Model}' is not available for provider '{_providerName}'.")
                .WithErrorCode("MODEL_NOT_AVAILABLE");
        });
    }

    /// <summary>
    /// Checks whether a model is available for the current provider.
    /// </summary>
    /// <param name="model">The model identifier to check.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>True if the model is available; otherwise, false.</returns>
    private async Task<bool> IsModelAvailableAsync(string model, CancellationToken ct)
    {
        try
        {
            var models = await _modelRegistry.GetModelsAsync(_providerName, ct: ct);
            var isAvailable = models.Any(m =>
                m.Id.Equals(model, StringComparison.OrdinalIgnoreCase));

            LLMLogEvents.ProviderValidation(_logger, _providerName, isAvailable);
            return isAvailable;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // LOGIC: If we can't verify model availability, don't fail validation.
            // This prevents validation failures due to network issues.
            _logger.LogWarning(
                ex,
                "Could not verify model availability for '{Model}' on provider '{Provider}'",
                model,
                _providerName);

            return true;
        }
    }
}

/// <summary>
/// Factory for creating provider-aware validators.
/// </summary>
/// <remarks>
/// <para>
/// Use this factory to create <see cref="ProviderAwareChatOptionsValidator"/> instances
/// for specific providers. This is useful in DI scenarios where the provider name
/// is not known until runtime.
/// </para>
/// </remarks>
public class ProviderAwareChatOptionsValidatorFactory
{
    private readonly ModelRegistry _modelRegistry;
    private readonly ILogger<ProviderAwareChatOptionsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProviderAwareChatOptionsValidatorFactory"/> class.
    /// </summary>
    /// <param name="modelRegistry">The model registry.</param>
    /// <param name="logger">The logger instance.</param>
    public ProviderAwareChatOptionsValidatorFactory(
        ModelRegistry modelRegistry,
        ILogger<ProviderAwareChatOptionsValidator> logger)
    {
        _modelRegistry = modelRegistry ?? throw new ArgumentNullException(nameof(modelRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Creates a validator for the specified provider.
    /// </summary>
    /// <param name="providerName">The provider name.</param>
    /// <returns>A new <see cref="ProviderAwareChatOptionsValidator"/> instance.</returns>
    public ProviderAwareChatOptionsValidator Create(string providerName)
    {
        return new ProviderAwareChatOptionsValidator(providerName, _modelRegistry, _logger);
    }
}
