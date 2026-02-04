// -----------------------------------------------------------------------
// <copyright file="ChatOptionsValidator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentValidation;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// FluentValidation validator for <see cref="ChatOptions"/>.
/// </summary>
/// <remarks>
/// <para>
/// This validator enforces the parameter constraints defined by LLM providers.
/// All rules are applied only when the corresponding property has a value,
/// since null values indicate "use provider default".
/// </para>
/// <para>
/// <b>Validation Rules:</b>
/// </para>
/// <list type="bullet">
///   <item><description>Temperature: 0.0 to 2.0</description></item>
///   <item><description>MaxTokens: Greater than 0</description></item>
///   <item><description>TopP: 0.0 to 1.0</description></item>
///   <item><description>FrequencyPenalty: -2.0 to 2.0</description></item>
///   <item><description>PresencePenalty: -2.0 to 2.0</description></item>
///   <item><description>StopSequences: Maximum 4 items</description></item>
/// </list>
/// </remarks>
/// <example>
/// <code>
/// var validator = new ChatOptionsValidator();
/// var options = new ChatOptions(Temperature: 3.0); // Invalid
///
/// var result = validator.Validate(options);
/// if (!result.IsValid)
/// {
///     foreach (var error in result.Errors)
///     {
///         Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
///     }
/// }
/// </code>
/// </example>
public sealed class ChatOptionsValidator : AbstractValidator<ChatOptions>
{
    /// <summary>
    /// Minimum valid temperature value.
    /// </summary>
    public const double MinTemperature = 0.0;

    /// <summary>
    /// Maximum valid temperature value.
    /// </summary>
    public const double MaxTemperature = 2.0;

    /// <summary>
    /// Minimum valid MaxTokens value.
    /// </summary>
    public const int MinMaxTokens = 1;

    /// <summary>
    /// Minimum valid TopP value.
    /// </summary>
    public const double MinTopP = 0.0;

    /// <summary>
    /// Maximum valid TopP value.
    /// </summary>
    public const double MaxTopP = 1.0;

    /// <summary>
    /// Minimum valid penalty value (frequency and presence).
    /// </summary>
    public const double MinPenalty = -2.0;

    /// <summary>
    /// Maximum valid penalty value (frequency and presence).
    /// </summary>
    public const double MaxPenalty = 2.0;

    /// <summary>
    /// Maximum number of stop sequences allowed.
    /// </summary>
    public const int MaxStopSequences = 4;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatOptionsValidator"/> class.
    /// </summary>
    public ChatOptionsValidator()
    {
        // RULE: Temperature must be between 0.0 and 2.0 (when specified)
        When(x => x.Temperature.HasValue, () =>
        {
            RuleFor(x => x.Temperature!.Value)
                .InclusiveBetween(MinTemperature, MaxTemperature)
                    .WithMessage($"Temperature must be between {MinTemperature} and {MaxTemperature}.")
                    .WithErrorCode("TEMPERATURE_OUT_OF_RANGE");
        });

        // RULE: MaxTokens must be greater than 0 (when specified)
        When(x => x.MaxTokens.HasValue, () =>
        {
            RuleFor(x => x.MaxTokens!.Value)
                .GreaterThan(0)
                    .WithMessage("MaxTokens must be greater than 0.")
                    .WithErrorCode("MAX_TOKENS_INVALID");
        });

        // RULE: TopP must be between 0.0 and 1.0 (when specified)
        When(x => x.TopP.HasValue, () =>
        {
            RuleFor(x => x.TopP!.Value)
                .InclusiveBetween(MinTopP, MaxTopP)
                    .WithMessage($"TopP must be between {MinTopP} and {MaxTopP}.")
                    .WithErrorCode("TOP_P_OUT_OF_RANGE");
        });

        // RULE: FrequencyPenalty must be between -2.0 and 2.0 (when specified)
        When(x => x.FrequencyPenalty.HasValue, () =>
        {
            RuleFor(x => x.FrequencyPenalty!.Value)
                .InclusiveBetween(MinPenalty, MaxPenalty)
                    .WithMessage($"FrequencyPenalty must be between {MinPenalty} and {MaxPenalty}.")
                    .WithErrorCode("FREQUENCY_PENALTY_OUT_OF_RANGE");
        });

        // RULE: PresencePenalty must be between -2.0 and 2.0 (when specified)
        When(x => x.PresencePenalty.HasValue, () =>
        {
            RuleFor(x => x.PresencePenalty!.Value)
                .InclusiveBetween(MinPenalty, MaxPenalty)
                    .WithMessage($"PresencePenalty must be between {MinPenalty} and {MaxPenalty}.")
                    .WithErrorCode("PRESENCE_PENALTY_OUT_OF_RANGE");
        });

        // RULE: StopSequences cannot exceed 4 items (when specified)
        When(x => x.StopSequences.HasValue && !x.StopSequences.Value.IsDefaultOrEmpty, () =>
        {
            RuleFor(x => x.StopSequences!.Value)
                .Must(s => s.Length <= MaxStopSequences)
                    .WithMessage($"Maximum {MaxStopSequences} stop sequences are allowed.")
                    .WithErrorCode("TOO_MANY_STOP_SEQUENCES");
        });
    }
}
