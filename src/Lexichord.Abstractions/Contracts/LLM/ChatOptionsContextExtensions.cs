// -----------------------------------------------------------------------
// <copyright file="ChatOptionsContextExtensions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.LLM;

/// <summary>
/// Extension methods for <see cref="ChatOptions"/> context window management.
/// </summary>
/// <remarks>
/// <para>
/// These extensions help adjust <see cref="ChatOptions"/> based on token estimates
/// to ensure requests fit within model context windows.
/// </para>
/// </remarks>
public static class ChatOptionsContextExtensions
{
    /// <summary>
    /// Adjusts <see cref="ChatOptions.MaxTokens"/> to fit within the available context space.
    /// </summary>
    /// <param name="options">The chat options to adjust.</param>
    /// <param name="estimate">The token estimate for the request.</param>
    /// <returns>
    /// A new <see cref="ChatOptions"/> with <see cref="ChatOptions.MaxTokens"/> clamped
    /// to the available response tokens if necessary.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="estimate"/> is null.
    /// </exception>
    /// <exception cref="ContextWindowExceededException">
    /// Thrown when the prompt alone exceeds the model's context window.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method performs the following adjustments:
    /// </para>
    /// <list type="number">
    ///   <item>
    ///     <description>
    ///       If <see cref="TokenEstimate.WouldExceedContext"/> is true, throws
    ///       <see cref="ContextWindowExceededException"/> since the request cannot proceed.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       If <see cref="ChatOptions.MaxTokens"/> exceeds <see cref="TokenEstimate.AvailableResponseTokens"/>,
    ///       creates a new instance with clamped MaxTokens.
    ///     </description>
    ///   </item>
    ///   <item>
    ///     <description>
    ///       Otherwise, returns the original options unchanged.
    ///     </description>
    ///   </item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// var estimate = await tokenEstimator.EstimateAsync(request);
    ///
    /// try
    /// {
    ///     var adjustedOptions = request.Options.AdjustForContext(estimate);
    ///     var adjustedRequest = request with { Options = adjustedOptions };
    ///     var response = await service.CompleteAsync(adjustedRequest);
    /// }
    /// catch (ContextWindowExceededException ex)
    /// {
    ///     Console.WriteLine($"Reduce prompt by {ex.Overage} tokens");
    /// }
    /// </code>
    /// </example>
    public static ChatOptions AdjustForContext(this ChatOptions options, TokenEstimate estimate)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(estimate, nameof(estimate));

        // LOGIC: If the prompt alone exceeds the context window, we cannot proceed.
        // Throw an exception to signal the caller must reduce the prompt size.
        if (estimate.WouldExceedContext)
        {
            throw new ContextWindowExceededException(
                estimate.EstimatedPromptTokens,
                estimate.ContextWindow);
        }

        // LOGIC: If MaxTokens is not set or fits within available space, return unchanged.
        if (!options.MaxTokens.HasValue)
        {
            return options;
        }

        // LOGIC: If requested MaxTokens exceeds available space, clamp it.
        if (options.MaxTokens.Value > estimate.AvailableResponseTokens)
        {
            return options with { MaxTokens = estimate.AvailableResponseTokens };
        }

        // LOGIC: Options fit within context limits, return unchanged.
        return options;
    }

    /// <summary>
    /// Checks whether the options would fit within the given token estimate.
    /// </summary>
    /// <param name="options">The chat options to check.</param>
    /// <param name="estimate">The token estimate for the request.</param>
    /// <returns>True if the options fit; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="estimate"/> is null.
    /// </exception>
    /// <remarks>
    /// This is a non-throwing check that can be used before attempting to adjust options.
    /// </remarks>
    public static bool WouldFitInContext(this ChatOptions options, TokenEstimate estimate)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(estimate, nameof(estimate));

        if (estimate.WouldExceedContext)
        {
            return false;
        }

        if (!options.MaxTokens.HasValue)
        {
            return true;
        }

        return options.MaxTokens.Value <= estimate.AvailableResponseTokens;
    }

    /// <summary>
    /// Gets the effective MaxTokens value, considering the available context space.
    /// </summary>
    /// <param name="options">The chat options.</param>
    /// <param name="estimate">The token estimate for the request.</param>
    /// <returns>
    /// The effective MaxTokens, which is the minimum of the requested value
    /// and the available response tokens.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="estimate"/> is null.
    /// </exception>
    /// <remarks>
    /// This method does not throw for context overflow; it returns 0 if the context
    /// would be exceeded.
    /// </remarks>
    public static int GetEffectiveMaxTokens(this ChatOptions options, TokenEstimate estimate)
    {
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(estimate, nameof(estimate));

        if (estimate.WouldExceedContext)
        {
            return 0;
        }

        if (!options.MaxTokens.HasValue)
        {
            return estimate.AvailableResponseTokens;
        }

        return Math.Min(options.MaxTokens.Value, estimate.AvailableResponseTokens);
    }
}
