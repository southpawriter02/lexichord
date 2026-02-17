// -----------------------------------------------------------------------
// <copyright file="IFixSuggestionGenerator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Generates AI-powered fix suggestions for style deviations detected by the
/// <see cref="IStyleDeviationScanner"/>.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="IFixSuggestionGenerator"/> is the second component
/// in the Tuning Agent pipeline (v0.7.5b). It takes style deviations with context
/// and generates intelligent fix suggestions using LLM:
/// <list type="bullet">
///   <item><description>Constructs prompts enhanced with rule details and learning context</description></item>
///   <item><description>Invokes LLM to generate contextually appropriate rewrites</description></item>
///   <item><description>Validates generated fixes against the linter</description></item>
///   <item><description>Computes confidence and quality scores</description></item>
///   <item><description>Generates visual diffs for review</description></item>
/// </list>
/// </para>
/// <para>
/// <b>License Requirement:</b> Requires WriterPro tier or higher. Returns
/// <see cref="FixSuggestion.LicenseRequired"/> for unlicensed users.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Implementations must be thread-safe. All methods may be
/// called concurrently from multiple threads.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5b as part of the Automatic Fix Suggestions feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Single fix generation
/// var scanner = serviceProvider.GetRequiredService&lt;IStyleDeviationScanner&gt;();
/// var generator = serviceProvider.GetRequiredService&lt;IFixSuggestionGenerator&gt;();
///
/// var scanResult = await scanner.ScanDocumentAsync(documentPath);
/// foreach (var deviation in scanResult.Deviations.Where(d => d.IsAutoFixable))
/// {
///     var suggestion = await generator.GenerateFixAsync(deviation);
///     if (suggestion.Success &amp;&amp; suggestion.IsHighConfidence)
///     {
///         Console.WriteLine($"High-confidence fix: {suggestion.Explanation}");
///     }
/// }
///
/// // Batch generation with custom options
/// var options = new FixGenerationOptions
/// {
///     MaxAlternatives = 3,
///     Tone = TonePreference.Formal,
///     MaxParallelism = 3
/// };
/// var suggestions = await generator.GenerateFixesAsync(
///     scanResult.Deviations.ToList(),
///     options);
/// </code>
/// </example>
/// <seealso cref="IStyleDeviationScanner"/>
/// <seealso cref="StyleDeviation"/>
/// <seealso cref="FixSuggestion"/>
/// <seealso cref="FixGenerationOptions"/>
public interface IFixSuggestionGenerator
{
    /// <summary>
    /// Generates a fix suggestion for a single deviation.
    /// </summary>
    /// <param name="deviation">The style deviation to fix.</param>
    /// <param name="options">Options controlling fix generation. Uses defaults if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="FixSuggestion"/> containing the suggested fix with confidence
    /// and quality scores. Check <see cref="FixSuggestion.Success"/> to determine
    /// if generation succeeded.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="deviation"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> The generation process:
    /// <list type="number">
    ///   <item><description>Build prompt context from deviation and options</description></item>
    ///   <item><description>Fetch learning context if enabled and available</description></item>
    ///   <item><description>Render prompt using template repository</description></item>
    ///   <item><description>Call LLM to generate fix</description></item>
    ///   <item><description>Parse response and generate diff</description></item>
    ///   <item><description>Validate fix if enabled</description></item>
    ///   <item><description>Calculate confidence and quality scores</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Error Handling:</b> Does not throw on generation failure. Instead,
    /// returns a <see cref="FixSuggestion"/> with <see cref="FixSuggestion.Success"/>
    /// set to <c>false</c> and <see cref="FixSuggestion.ErrorMessage"/> containing
    /// the error details.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var suggestion = await generator.GenerateFixAsync(deviation);
    /// if (suggestion.Success)
    /// {
    ///     Console.WriteLine($"Suggested: '{suggestion.SuggestedText}'");
    ///     Console.WriteLine($"Confidence: {suggestion.Confidence:P0}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Failed: {suggestion.ErrorMessage}");
    /// }
    /// </code>
    /// </example>
    Task<FixSuggestion> GenerateFixAsync(
        StyleDeviation deviation,
        FixGenerationOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates fix suggestions for multiple deviations in batch.
    /// </summary>
    /// <param name="deviations">The style deviations to fix.</param>
    /// <param name="options">Options controlling fix generation. Uses defaults if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A list of <see cref="FixSuggestion"/> instances, one for each input deviation
    /// in the same order. Failed generations are included with
    /// <see cref="FixSuggestion.Success"/> set to <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="deviations"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> More efficient than calling <see cref="GenerateFixAsync"/>
    /// individually due to:
    /// <list type="bullet">
    ///   <item><description>Parallel processing controlled by <see cref="FixGenerationOptions.MaxParallelism"/></description></item>
    ///   <item><description>Shared context and caching</description></item>
    ///   <item><description>Batched logging and metrics</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Parallelism:</b> Uses <see cref="SemaphoreSlim"/> to limit concurrent
    /// LLM requests to <see cref="FixGenerationOptions.MaxParallelism"/> (default: 5).
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var suggestions = await generator.GenerateFixesAsync(
    ///     scanResult.Deviations.ToList(),
    ///     new FixGenerationOptions { MaxParallelism = 3 });
    ///
    /// var successCount = suggestions.Count(s => s.Success);
    /// Console.WriteLine($"Generated {successCount}/{suggestions.Count} fixes");
    /// </code>
    /// </example>
    Task<IReadOnlyList<FixSuggestion>> GenerateFixesAsync(
        IReadOnlyList<StyleDeviation> deviations,
        FixGenerationOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Regenerates a fix suggestion with additional user guidance.
    /// </summary>
    /// <param name="deviation">The style deviation to fix.</param>
    /// <param name="userGuidance">User's feedback or preferred direction for the fix.</param>
    /// <param name="options">Options controlling fix generation. Uses defaults if null.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A new <see cref="FixSuggestion"/> incorporating the user's guidance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="deviation"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="userGuidance"/> is null, empty, or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Used when the user rejects the initial suggestion and provides
    /// feedback about their preferred approach. The user guidance is injected into
    /// the prompt to steer the LLM toward the user's preference.
    /// </para>
    /// <para>
    /// <b>Examples of user guidance:</b>
    /// <list type="bullet">
    ///   <item><description>"Keep the technical term but add an explanation"</description></item>
    ///   <item><description>"Make it more conversational"</description></item>
    ///   <item><description>"Preserve the original sentence structure"</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // User rejects initial fix
    /// var betterFix = await generator.RegenerateFixAsync(
    ///     deviation,
    ///     "Keep the word 'utilize' but add a simpler explanation after it");
    /// </code>
    /// </example>
    Task<FixSuggestion> RegenerateFixAsync(
        StyleDeviation deviation,
        string userGuidance,
        FixGenerationOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Validates that a fix suggestion correctly addresses the deviation
    /// without introducing new violations.
    /// </summary>
    /// <param name="deviation">The original deviation.</param>
    /// <param name="suggestion">The suggested fix to validate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="FixValidationResult"/> with detailed validation information.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="deviation"/> or <paramref name="suggestion"/> is null.
    /// </exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Validation process:
    /// <list type="number">
    ///   <item><description>Apply the suggested fix to the document text</description></item>
    ///   <item><description>Re-run linting on the fixed text</description></item>
    ///   <item><description>Check if the original violation is resolved</description></item>
    ///   <item><description>Check if any new violations were introduced</description></item>
    ///   <item><description>Calculate semantic similarity</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Note:</b> This method is also called internally when
    /// <see cref="FixGenerationOptions.ValidateFixes"/> is <c>true</c>.
    /// Call it explicitly to validate fixes that were generated with validation disabled.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var validation = await generator.ValidateFixAsync(deviation, suggestion);
    /// if (validation.Status == ValidationStatus.Valid)
    /// {
    ///     Console.WriteLine("Fix is safe to apply");
    /// }
    /// else if (validation.IntroducesNewViolations)
    /// {
    ///     Console.WriteLine($"Fix introduces {validation.NewViolations?.Count} new violations");
    /// }
    /// </code>
    /// </example>
    Task<FixValidationResult> ValidateFixAsync(
        StyleDeviation deviation,
        FixSuggestion suggestion,
        CancellationToken ct = default);
}
