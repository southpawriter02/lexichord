// -----------------------------------------------------------------------
// <copyright file="IModelProvider.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Provides model discovery capabilities for an LLM provider.
/// </summary>
/// <remarks>
/// <para>
/// Implement this interface alongside <see cref="IChatCompletionService"/> to enable
/// dynamic model discovery. This allows the application to query available models
/// from the provider at runtime, supporting model selection UIs and context-aware
/// token estimation.
/// </para>
/// <para>
/// Providers that do not support model listing can return a static list of known models.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// public class OpenAIModelProvider : IModelProvider
/// {
///     public async Task&lt;IReadOnlyList&lt;ModelInfo&gt;&gt; GetAvailableModelsAsync(CancellationToken ct)
///     {
///         // Query OpenAI API for models, or return static list
///         return new[]
///         {
///             new ModelInfo("gpt-4o", "GPT-4o", 128000, 4096, true, true),
///             new ModelInfo("gpt-4o-mini", "GPT-4o Mini", 128000, 4096, true, true),
///             new ModelInfo("gpt-4-turbo", "GPT-4 Turbo", 128000, 4096, true, true),
///         };
///     }
/// }
/// </code>
/// </example>
public interface IModelProvider
{
    /// <summary>
    /// Gets the list of available models from the provider.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>
    /// A read-only list of <see cref="ModelInfo"/> describing available models.
    /// Returns an empty list if the provider cannot be queried.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Implementations should cache the results appropriately to avoid excessive API calls.
    /// A typical cache duration is 1 hour for production use.
    /// </para>
    /// <para>
    /// If the provider API is unavailable, implementations should return a fallback list
    /// of commonly used models rather than throwing an exception.
    /// </para>
    /// </remarks>
    /// <exception cref="OperationCanceledException">
    /// Thrown when <paramref name="ct"/> is cancelled.
    /// </exception>
    Task<IReadOnlyList<ModelInfo>> GetAvailableModelsAsync(CancellationToken ct = default);
}
