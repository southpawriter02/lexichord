// -----------------------------------------------------------------------
// <copyright file="TokenEstimate.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Token usage estimates for a chat completion request.
/// </summary>
/// <remarks>
/// <para>
/// This record provides token estimation data used for context window management
/// and request planning. Use it with <see cref="ChatOptionsContextExtensions.AdjustForContext"/>
/// to automatically adjust <see cref="ChatOptions.MaxTokens"/> based on available space.
/// </para>
/// <para>
/// Token counts are estimates and may differ slightly from the actual usage
/// reported by providers due to tokenizer implementation differences.
/// </para>
/// </remarks>
/// <param name="EstimatedPromptTokens">
/// The estimated number of tokens in the prompt (all messages combined).
/// Includes per-message overhead tokens.
/// </param>
/// <param name="AvailableResponseTokens">
/// The maximum tokens available for the response, accounting for context limits.
/// This is the minimum of the requested <see cref="ChatOptions.MaxTokens"/>,
/// the model's <see cref="ModelInfo.MaxOutputTokens"/>, and the remaining context space.
/// </param>
/// <param name="ContextWindow">
/// The total context window size for the target model.
/// </param>
/// <param name="WouldExceedContext">
/// Whether the prompt alone would exceed the model's context window.
/// If true, the request cannot be sent without reducing the prompt size.
/// </param>
/// <example>
/// <code>
/// // Estimate tokens before sending request
/// var estimate = await tokenEstimator.EstimateAsync(request);
///
/// if (estimate.WouldExceedContext)
/// {
///     Console.WriteLine($"Request too large: {estimate.EstimatedPromptTokens} tokens");
///     return;
/// }
///
/// // Adjust options based on available space
/// var adjustedOptions = request.Options.AdjustForContext(estimate);
/// </code>
/// </example>
public record TokenEstimate(
    int EstimatedPromptTokens,
    int AvailableResponseTokens,
    int ContextWindow,
    bool WouldExceedContext)
{
    /// <summary>
    /// Gets the percentage of the context window used by the prompt.
    /// </summary>
    /// <value>A value between 0.0 and 1.0+ representing context utilization.</value>
    /// <remarks>
    /// Values over 1.0 indicate the prompt exceeds the context window.
    /// </remarks>
    public double ContextUtilization => ContextWindow > 0
        ? (double)EstimatedPromptTokens / ContextWindow
        : 0.0;

    /// <summary>
    /// Gets the remaining context space after the prompt.
    /// </summary>
    /// <value>
    /// The number of tokens available for response generation.
    /// May be negative if the prompt exceeds the context window.
    /// </value>
    public int RemainingContext => ContextWindow - EstimatedPromptTokens;

    /// <summary>
    /// Gets whether there is sufficient space for a meaningful response.
    /// </summary>
    /// <value>
    /// True if at least 100 tokens are available for the response; otherwise, false.
    /// </value>
    /// <remarks>
    /// A minimum of 100 tokens is considered necessary for a useful response.
    /// Adjust this threshold based on your application's needs.
    /// </remarks>
    public bool HasSufficientResponseSpace => AvailableResponseTokens >= 100;

    /// <summary>
    /// Creates a token estimate indicating the context would be exceeded.
    /// </summary>
    /// <param name="promptTokens">The estimated prompt tokens.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    /// <returns>A <see cref="TokenEstimate"/> with <see cref="WouldExceedContext"/> set to true.</returns>
    public static TokenEstimate Exceeded(int promptTokens, int contextWindow) =>
        new(promptTokens, 0, contextWindow, WouldExceedContext: true);

    /// <summary>
    /// Creates a token estimate for a request that fits within context limits.
    /// </summary>
    /// <param name="promptTokens">The estimated prompt tokens.</param>
    /// <param name="availableResponse">The available response tokens.</param>
    /// <param name="contextWindow">The model's context window size.</param>
    /// <returns>A <see cref="TokenEstimate"/> with calculated values.</returns>
    public static TokenEstimate WithinLimits(int promptTokens, int availableResponse, int contextWindow) =>
        new(promptTokens, availableResponse, contextWindow, WouldExceedContext: false);
}
