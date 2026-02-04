// -----------------------------------------------------------------------
// <copyright file="ChatOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Immutable;

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Configuration options for chat completion requests.
/// </summary>
/// <remarks>
/// <para>
/// This immutable record encapsulates all configurable parameters for LLM chat requests.
/// All properties are nullable to support provider-specific defaults.
/// </para>
/// <para>
/// Use the <see cref="Default"/> property for sensible defaults, or create custom
/// configurations using the <c>With*</c> methods for a fluent API.
/// </para>
/// </remarks>
/// <param name="Model">
/// The model identifier (e.g., "gpt-4", "claude-3-opus").
/// When null, the provider's default model is used.
/// </param>
/// <param name="Temperature">
/// Controls randomness in responses. Range: 0.0 (deterministic) to 2.0 (very random).
/// When null, the provider's default is used (typically 1.0).
/// </param>
/// <param name="MaxTokens">
/// Maximum number of tokens to generate in the response.
/// When null, the provider's default limit applies.
/// </param>
/// <param name="TopP">
/// Nucleus sampling threshold. Range: 0.0 to 1.0.
/// Alternative to temperature for controlling output diversity.
/// When null, the provider's default is used.
/// </param>
/// <param name="FrequencyPenalty">
/// Penalty for token frequency. Range: -2.0 to 2.0.
/// Positive values reduce repetition of frequent tokens.
/// When null, the provider's default is used (typically 0.0).
/// </param>
/// <param name="PresencePenalty">
/// Penalty for token presence. Range: -2.0 to 2.0.
/// Positive values encourage discussing new topics.
/// When null, the provider's default is used (typically 0.0).
/// </param>
/// <param name="StopSequences">
/// Sequences where the model will stop generating.
/// When null or empty, no custom stop sequences are applied.
/// </param>
/// <example>
/// <code>
/// // Use defaults
/// var options = ChatOptions.Default;
///
/// // Configure specific model
/// var gpt4Options = ChatOptions.Default.WithModel("gpt-4");
///
/// // Configure for creative writing
/// var creativeOptions = ChatOptions.Creative.WithMaxTokens(2000);
///
/// // Full customization
/// var customOptions = new ChatOptions(
///     Model: "gpt-4-turbo",
///     Temperature: 0.7,
///     MaxTokens: 1000,
///     TopP: 0.9,
///     FrequencyPenalty: 0.5,
///     PresencePenalty: 0.3,
///     StopSequences: ImmutableArray.Create("END", "STOP")
/// );
/// </code>
/// </example>
public record ChatOptions(
    string? Model = null,
    double? Temperature = null,
    int? MaxTokens = null,
    double? TopP = null,
    double? FrequencyPenalty = null,
    double? PresencePenalty = null,
    ImmutableArray<string>? StopSequences = null)
{
    /// <summary>
    /// Gets the default chat options with all values set to provider defaults.
    /// </summary>
    /// <value>A <see cref="ChatOptions"/> instance with all null values.</value>
    public static ChatOptions Default => new();

    /// <summary>
    /// Gets options configured for precise, deterministic responses.
    /// </summary>
    /// <value>A <see cref="ChatOptions"/> instance with Temperature = 0.</value>
    /// <remarks>
    /// Use this preset for tasks requiring consistent, reproducible outputs
    /// such as code generation, data extraction, or factual responses.
    /// </remarks>
    public static ChatOptions Precise => new(Temperature: 0.0);

    /// <summary>
    /// Gets options configured for creative, varied responses.
    /// </summary>
    /// <value>A <see cref="ChatOptions"/> instance with Temperature = 0.9.</value>
    /// <remarks>
    /// Use this preset for creative writing, brainstorming, or tasks
    /// that benefit from more diverse and imaginative outputs.
    /// </remarks>
    public static ChatOptions Creative => new(Temperature: 0.9);

    /// <summary>
    /// Gets options configured for balanced responses.
    /// </summary>
    /// <value>A <see cref="ChatOptions"/> instance with Temperature = 0.7.</value>
    /// <remarks>
    /// Use this preset for general-purpose conversations where a balance
    /// between consistency and creativity is desired.
    /// </remarks>
    public static ChatOptions Balanced => new(Temperature: 0.7);

    /// <summary>
    /// Gets options optimized for code generation with deterministic output.
    /// </summary>
    /// <value>
    /// A <see cref="ChatOptions"/> instance with Temperature = 0, TopP = 1.0,
    /// and stop sequences for code blocks.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this preset for generating code, scripts, configuration files,
    /// or any structured output requiring high consistency.
    /// </para>
    /// <para>
    /// The stop sequences ("```", "---") are designed to stop generation
    /// at natural code block boundaries.
    /// </para>
    /// </remarks>
    public static ChatOptions CodeGeneration => new(
        Temperature: 0.0,
        TopP: 1.0,
        FrequencyPenalty: 0.0,
        PresencePenalty: 0.0,
        StopSequences: ImmutableArray.Create("```", "---"));

    /// <summary>
    /// Gets options optimized for conversational interactions.
    /// </summary>
    /// <value>
    /// A <see cref="ChatOptions"/> instance with balanced temperature and light penalties
    /// to encourage natural conversation flow.
    /// </value>
    /// <remarks>
    /// Use this preset for chatbots, assistants, and interactive dialogue
    /// where natural, engaging responses are preferred.
    /// </remarks>
    public static ChatOptions Conversational => new(
        Temperature: 0.7,
        TopP: 0.9,
        FrequencyPenalty: 0.3,
        PresencePenalty: 0.3);

    /// <summary>
    /// Gets options optimized for summarization tasks.
    /// </summary>
    /// <value>
    /// A <see cref="ChatOptions"/> instance with moderate temperature, focused sampling,
    /// and limited token output for concise summaries.
    /// </value>
    /// <remarks>
    /// Use this preset for generating summaries, abstracts, TL;DRs,
    /// and other condensed versions of longer content.
    /// </remarks>
    public static ChatOptions Summarization => new(
        Temperature: 0.5,
        MaxTokens: 1024,
        TopP: 0.85,
        FrequencyPenalty: 0.0,
        PresencePenalty: 0.0);

    /// <summary>
    /// Gets options optimized for editing and revision tasks.
    /// </summary>
    /// <value>
    /// A <see cref="ChatOptions"/> instance with low temperature for consistency
    /// and light penalties to maintain original style while improving clarity.
    /// </value>
    /// <remarks>
    /// Use this preset for proofreading, grammar correction, style improvement,
    /// and refining existing text while preserving the author's voice.
    /// </remarks>
    public static ChatOptions Editing => new(
        Temperature: 0.4,
        TopP: 0.9,
        FrequencyPenalty: 0.2,
        PresencePenalty: 0.1);

    /// <summary>
    /// Gets options optimized for brainstorming with maximum creativity.
    /// </summary>
    /// <value>
    /// A <see cref="ChatOptions"/> instance with high temperature and strong penalties
    /// to encourage diverse, novel ideas.
    /// </value>
    /// <remarks>
    /// <para>
    /// Use this preset for idea generation, creative exploration, and tasks
    /// that benefit from unconventional thinking.
    /// </para>
    /// <para>
    /// <b>Warning:</b> The high temperature (1.5) may produce more varied
    /// and sometimes unexpected outputs. Review results carefully.
    /// </para>
    /// </remarks>
    public static ChatOptions Brainstorming => new(
        Temperature: 1.5,
        TopP: 0.98,
        FrequencyPenalty: 0.8,
        PresencePenalty: 0.8);

    /// <summary>
    /// Creates a new instance with the specified model.
    /// </summary>
    /// <param name="model">The model identifier to use.</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified model.</returns>
    public ChatOptions WithModel(string model) => this with { Model = model };

    /// <summary>
    /// Creates a new instance with the specified temperature.
    /// </summary>
    /// <param name="temperature">The temperature value (0.0 to 2.0).</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified temperature.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="temperature"/> is less than 0 or greater than 2.
    /// </exception>
    public ChatOptions WithTemperature(double temperature)
    {
        if (temperature < 0.0 || temperature > 2.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(temperature),
                temperature,
                "Temperature must be between 0.0 and 2.0.");
        }

        return this with { Temperature = temperature };
    }

    /// <summary>
    /// Creates a new instance with the specified maximum tokens.
    /// </summary>
    /// <param name="maxTokens">The maximum number of tokens to generate.</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified max tokens.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="maxTokens"/> is less than 1.
    /// </exception>
    public ChatOptions WithMaxTokens(int maxTokens)
    {
        if (maxTokens < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxTokens),
                maxTokens,
                "MaxTokens must be at least 1.");
        }

        return this with { MaxTokens = maxTokens };
    }

    /// <summary>
    /// Creates a new instance with the specified top-p value.
    /// </summary>
    /// <param name="topP">The nucleus sampling threshold (0.0 to 1.0).</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified top-p.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="topP"/> is less than 0 or greater than 1.
    /// </exception>
    public ChatOptions WithTopP(double topP)
    {
        if (topP < 0.0 || topP > 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(topP),
                topP,
                "TopP must be between 0.0 and 1.0.");
        }

        return this with { TopP = topP };
    }

    /// <summary>
    /// Creates a new instance with the specified frequency penalty.
    /// </summary>
    /// <param name="penalty">The frequency penalty (-2.0 to 2.0).</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified frequency penalty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="penalty"/> is less than -2 or greater than 2.
    /// </exception>
    public ChatOptions WithFrequencyPenalty(double penalty)
    {
        if (penalty < -2.0 || penalty > 2.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(penalty),
                penalty,
                "FrequencyPenalty must be between -2.0 and 2.0.");
        }

        return this with { FrequencyPenalty = penalty };
    }

    /// <summary>
    /// Creates a new instance with the specified presence penalty.
    /// </summary>
    /// <param name="penalty">The presence penalty (-2.0 to 2.0).</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified presence penalty.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="penalty"/> is less than -2 or greater than 2.
    /// </exception>
    public ChatOptions WithPresencePenalty(double penalty)
    {
        if (penalty < -2.0 || penalty > 2.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(penalty),
                penalty,
                "PresencePenalty must be between -2.0 and 2.0.");
        }

        return this with { PresencePenalty = penalty };
    }

    /// <summary>
    /// Creates a new instance with the specified stop sequences.
    /// </summary>
    /// <param name="sequences">The stop sequences to use.</param>
    /// <returns>A new <see cref="ChatOptions"/> with the specified stop sequences.</returns>
    public ChatOptions WithStopSequences(params string[] sequences)
        => this with { StopSequences = sequences.ToImmutableArray() };
}
