// -----------------------------------------------------------------------
// <copyright file="ChatOptionsDefaults.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.LLM.Configuration;

/// <summary>
/// Default values for <see cref="Abstractions.Contracts.LLM.ChatOptions"/> parameters.
/// </summary>
/// <remarks>
/// <para>
/// This class is designed for binding to the "Defaults" section under "LLM" in <c>appsettings.json</c>.
/// These defaults are applied when creating resolved <see cref="Abstractions.Contracts.LLM.ChatOptions"/>
/// instances via <see cref="ChatOptionsResolver"/>.
/// </para>
/// <para>
/// Example configuration:
/// </para>
/// <code>
/// {
///   "LLM": {
///     "Defaults": {
///       "Temperature": 0.7,
///       "MaxTokens": 2048,
///       "TopP": 1.0
///     }
///   }
/// }
/// </code>
/// </remarks>
public class ChatOptionsDefaults
{
    /// <summary>
    /// Gets or sets the default temperature for chat completion requests.
    /// </summary>
    /// <value>The default temperature. Defaults to 0.7 (balanced).</value>
    /// <remarks>
    /// Values range from 0.0 (deterministic) to 2.0 (very random).
    /// </remarks>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Gets or sets the default maximum tokens for chat completion requests.
    /// </summary>
    /// <value>The default max tokens. Defaults to 2048.</value>
    /// <remarks>
    /// This is the maximum number of tokens to generate in the response.
    /// </remarks>
    public int MaxTokens { get; set; } = 2048;

    /// <summary>
    /// Gets or sets the default top-p (nucleus sampling) value.
    /// </summary>
    /// <value>The default top-p. Defaults to 1.0 (no filtering).</value>
    /// <remarks>
    /// Values range from 0.0 to 1.0. Lower values restrict token sampling to
    /// the most likely tokens.
    /// </remarks>
    public double TopP { get; set; } = 1.0;

    /// <summary>
    /// Gets or sets the default frequency penalty.
    /// </summary>
    /// <value>The default frequency penalty. Defaults to 0.0 (no penalty).</value>
    /// <remarks>
    /// Values range from -2.0 to 2.0. Positive values reduce repetition.
    /// </remarks>
    public double FrequencyPenalty { get; set; } = 0.0;

    /// <summary>
    /// Gets or sets the default presence penalty.
    /// </summary>
    /// <value>The default presence penalty. Defaults to 0.0 (no penalty).</value>
    /// <remarks>
    /// Values range from -2.0 to 2.0. Positive values encourage discussing new topics.
    /// </remarks>
    public double PresencePenalty { get; set; } = 0.0;
}
