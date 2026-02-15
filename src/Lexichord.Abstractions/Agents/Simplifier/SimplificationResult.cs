// -----------------------------------------------------------------------
// <copyright file="SimplificationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the result of a text simplification operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="SimplificationResult"/> contains the complete output
/// of the <see cref="ISimplificationPipeline.SimplifyAsync"/> method, including:
/// </para>
/// <list type="bullet">
///   <item><description>The simplified text</description></item>
///   <item><description>Before/after readability metrics</description></item>
///   <item><description>List of individual changes made</description></item>
///   <item><description>Optional glossary of term replacements</description></item>
///   <item><description>Token usage and cost metrics</description></item>
///   <item><description>Processing time and success status</description></item>
/// </list>
/// <para>
/// <b>Success vs. Failure:</b>
/// Check <see cref="Success"/> before using the result. Failed results have
/// <see cref="ErrorMessage"/> populated and may have empty or partial data
/// in other fields.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4b as part of the Simplifier Agent Simplification Pipeline.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var result = await pipeline.SimplifyAsync(request, ct);
///
/// if (result.Success)
/// {
///     Console.WriteLine($"Simplified: {result.SimplifiedText}");
///     Console.WriteLine($"Grade level: {result.OriginalMetrics.FleschKincaidGradeLevel:F1} → " +
///                       $"{result.SimplifiedMetrics.FleschKincaidGradeLevel:F1}");
///     Console.WriteLine($"Reduction: {result.GradeLevelReduction:F1} grades");
///     Console.WriteLine($"Changes: {result.Changes.Count}");
///
///     if (result.TargetAchieved)
///     {
///         Console.WriteLine("Target readability achieved!");
///     }
/// }
/// else
/// {
///     Console.WriteLine($"Simplification failed: {result.ErrorMessage}");
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationRequest"/>
/// <seealso cref="ISimplificationPipeline"/>
/// <seealso cref="SimplificationChange"/>
public record SimplificationResult
{
    /// <summary>
    /// Gets the simplified text.
    /// </summary>
    /// <value>
    /// The transformed text with improved readability. Empty string if the
    /// operation failed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> This is the primary output of the simplification process.
    /// The text maintains the original structure unless <see cref="SimplificationRequest.PreserveFormatting"/>
    /// was set to false.
    /// </remarks>
    public required string SimplifiedText { get; init; }

    /// <summary>
    /// Gets the readability metrics of the original text.
    /// </summary>
    /// <value>
    /// Metrics calculated before simplification, including Flesch-Kincaid
    /// grade level, word count, sentence count, and complex word ratio.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used to calculate <see cref="GradeLevelReduction"/> and
    /// display before/after comparison in the UI.
    /// </remarks>
    public required ReadabilityMetrics OriginalMetrics { get; init; }

    /// <summary>
    /// Gets the readability metrics of the simplified text.
    /// </summary>
    /// <value>
    /// Metrics calculated after simplification. Should show improvement
    /// over <see cref="OriginalMetrics"/> in grade level and complexity.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Compared against <see cref="SimplificationRequest.Target"/>
    /// to determine if the target was achieved.
    /// </remarks>
    public required ReadabilityMetrics SimplifiedMetrics { get; init; }

    /// <summary>
    /// Gets the list of individual changes made during simplification.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="SimplificationChange"/> records
    /// detailing each transformation. May be empty if changes could not be parsed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Changes are extracted from the LLM response by the
    /// <see cref="Lexichord.Modules.Agents.Simplifier.SimplificationResponseParser"/>.
    /// Each change includes the original text, simplified text, change type, and explanation.
    /// </remarks>
    public required IReadOnlyList<SimplificationChange> Changes { get; init; }

    /// <summary>
    /// Gets the glossary of term replacements.
    /// </summary>
    /// <value>
    /// A dictionary mapping technical terms to their simplified equivalents.
    /// <c>null</c> if <see cref="SimplificationRequest.GenerateGlossary"/> was false
    /// or if the glossary could not be parsed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The glossary helps users understand terminology changes
    /// and can be exported for reference documentation.
    /// </remarks>
    public IReadOnlyDictionary<string, string>? Glossary { get; init; }

    /// <summary>
    /// Gets the token usage and cost metrics.
    /// </summary>
    /// <value>
    /// Usage information including prompt tokens, completion tokens, and
    /// estimated cost in USD.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated using <see cref="UsageMetrics.Calculate"/>
    /// with default pricing constants.
    /// </remarks>
    public required UsageMetrics TokenUsage { get; init; }

    /// <summary>
    /// Gets the total processing time.
    /// </summary>
    /// <value>
    /// Elapsed time from request start to result completion, including
    /// context gathering, LLM invocation, and response parsing.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Measured using <see cref="System.Diagnostics.Stopwatch"/>
    /// for accurate timing.
    /// </remarks>
    public required TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the strategy that was used for simplification.
    /// </summary>
    /// <value>
    /// The <see cref="SimplificationStrategy"/> from the request, used for
    /// logging and analytics.
    /// </value>
    public required SimplificationStrategy StrategyUsed { get; init; }

    /// <summary>
    /// Gets the readability target that was used.
    /// </summary>
    /// <value>
    /// The <see cref="ReadabilityTarget"/> from the request, used to
    /// determine <see cref="TargetAchieved"/>.
    /// </value>
    public required ReadabilityTarget TargetUsed { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation succeeded.
    /// </summary>
    /// <value>
    /// <c>true</c> if simplification completed without errors;
    /// otherwise, <c>false</c>. Check <see cref="ErrorMessage"/> for failure details.
    /// </value>
    public bool Success { get; init; } = true;

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    /// <value>
    /// A user-facing error message describing what went wrong.
    /// <c>null</c> if <see cref="Success"/> is <c>true</c>.
    /// </value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the grade level reduction achieved.
    /// </summary>
    /// <value>
    /// The difference in Flesch-Kincaid grade level between original and
    /// simplified text. Positive values indicate improvement (lower grade = easier).
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>OriginalMetrics.FleschKincaidGradeLevel -
    /// SimplifiedMetrics.FleschKincaidGradeLevel</c>.
    /// </remarks>
    public double GradeLevelReduction =>
        OriginalMetrics.FleschKincaidGradeLevel - SimplifiedMetrics.FleschKincaidGradeLevel;

    /// <summary>
    /// Gets a value indicating whether the target readability was achieved.
    /// </summary>
    /// <value>
    /// <c>true</c> if the simplified text's grade level is within the
    /// target's acceptable range; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Uses <see cref="ReadabilityTarget.IsGradeLevelAcceptable"/>
    /// to check if the result is within tolerance.
    /// </remarks>
    public bool TargetAchieved =>
        Success && TargetUsed.IsGradeLevelAcceptable(SimplifiedMetrics.FleschKincaidGradeLevel);

    /// <summary>
    /// Gets the word count difference between original and simplified text.
    /// </summary>
    /// <value>
    /// Positive values indicate word reduction; negative values indicate
    /// the text grew (e.g., adding explanations).
    /// </value>
    public int WordCountDifference =>
        OriginalMetrics.WordCount - SimplifiedMetrics.WordCount;

    /// <summary>
    /// Creates a failed result with the specified error message.
    /// </summary>
    /// <param name="originalText">The original text from the request.</param>
    /// <param name="strategy">The strategy that was requested.</param>
    /// <param name="target">The readability target that was requested.</param>
    /// <param name="errorMessage">A user-facing error message.</param>
    /// <param name="processingTime">The elapsed processing time.</param>
    /// <returns>A <see cref="SimplificationResult"/> with <see cref="Success"/> set to false.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="originalText"/>, <paramref name="target"/>,
    /// or <paramref name="errorMessage"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating failed results with consistent
    /// structure. Uses <see cref="ReadabilityMetrics.Empty"/> for metrics and
    /// <see cref="UsageMetrics.Zero"/> for token usage.
    /// </remarks>
    public static SimplificationResult Failed(
        string originalText,
        SimplificationStrategy strategy,
        ReadabilityTarget target,
        string errorMessage,
        TimeSpan processingTime)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(errorMessage);

        return new SimplificationResult
        {
            SimplifiedText = string.Empty,
            OriginalMetrics = ReadabilityMetrics.Empty,
            SimplifiedMetrics = ReadabilityMetrics.Empty,
            Changes = Array.Empty<SimplificationChange>(),
            Glossary = null,
            TokenUsage = UsageMetrics.Zero,
            ProcessingTime = processingTime,
            StrategyUsed = strategy,
            TargetUsed = target,
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
