// -----------------------------------------------------------------------
// <copyright file="ModelInfo.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Information about an available LLM model.
/// </summary>
/// <remarks>
/// <para>
/// This record provides metadata about models available from LLM providers,
/// enabling dynamic model discovery and context window management.
/// </para>
/// <para>
/// Use <see cref="IModelProvider.GetAvailableModelsAsync"/> to retrieve
/// available models from a provider, or use <see cref="ModelDefaults"/>
/// for static model configurations.
/// </para>
/// </remarks>
/// <param name="Id">
/// The unique model identifier used in API requests (e.g., "gpt-4o", "claude-3-opus-20240229").
/// </param>
/// <param name="DisplayName">
/// Human-readable name for UI display (e.g., "GPT-4o", "Claude 3 Opus").
/// </param>
/// <param name="ContextWindow">
/// Maximum total tokens (prompt + response) the model can handle.
/// </param>
/// <param name="MaxOutputTokens">
/// Maximum tokens the model can generate in a single response.
/// </param>
/// <param name="SupportsVision">
/// Whether the model accepts image inputs. Defaults to false.
/// </param>
/// <param name="SupportsTools">
/// Whether the model supports function/tool calling. Defaults to false.
/// </param>
/// <example>
/// <code>
/// // Create model info for GPT-4o
/// var gpt4o = new ModelInfo(
///     Id: "gpt-4o",
///     DisplayName: "GPT-4o",
///     ContextWindow: 128000,
///     MaxOutputTokens: 4096,
///     SupportsVision: true,
///     SupportsTools: true
/// );
///
/// // Check context availability
/// if (estimatedTokens > gpt4o.ContextWindow)
/// {
///     Console.WriteLine("Request too large for model");
/// }
/// </code>
/// </example>
public record ModelInfo(
    string Id,
    string DisplayName,
    int ContextWindow,
    int MaxOutputTokens,
    bool SupportsVision = false,
    bool SupportsTools = false)
{
    /// <summary>
    /// Gets the available response tokens given a prompt size.
    /// </summary>
    /// <param name="promptTokens">The number of tokens in the prompt.</param>
    /// <returns>
    /// The maximum tokens available for the response, clamped to <see cref="MaxOutputTokens"/>.
    /// Returns 0 if the prompt exceeds the context window.
    /// </returns>
    /// <remarks>
    /// This is a convenience method for calculating available response space.
    /// For more comprehensive token estimation, use <see cref="TokenEstimate"/>.
    /// </remarks>
    public int GetAvailableResponseTokens(int promptTokens)
    {
        var available = ContextWindow - promptTokens;
        if (available <= 0)
        {
            return 0;
        }

        return Math.Min(available, MaxOutputTokens);
    }

    /// <summary>
    /// Determines whether a request of the given size would fit in the context window.
    /// </summary>
    /// <param name="totalTokens">Total tokens (prompt + expected response).</param>
    /// <returns>True if the request fits; otherwise, false.</returns>
    public bool CanFitRequest(int totalTokens) => totalTokens <= ContextWindow;
}
