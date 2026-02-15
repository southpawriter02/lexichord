// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationEstimate.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Provides cost and time estimates for a batch simplification operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationEstimate"/> record is returned by
/// <see cref="IBatchSimplificationService.EstimateCostAsync"/> to help users understand
/// the scope and cost of a batch operation before committing to it. Estimates are
/// based on document analysis without actually performing simplification.
/// </para>
/// <para>
/// <b>Estimation Methodology:</b>
/// <list type="bullet">
///   <item><description><b>Paragraphs:</b> Document parsed and skip conditions evaluated</description></item>
///   <item><description><b>Tokens:</b> Word count × token factor (typically 1.3 tokens/word) × 2 (input + output)</description></item>
///   <item><description><b>Time:</b> Paragraph count × average time per paragraph (typically 2.5 seconds)</description></item>
///   <item><description><b>Cost:</b> Token count × token price (model-dependent)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Accuracy Disclaimer:</b>
/// Estimates are approximations based on typical document characteristics. Actual
/// results may vary based on paragraph complexity, API latency, and model behavior.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Get estimate before processing
/// var estimate = await batchService.EstimateCostAsync(documentPath, target, ct);
///
/// // Display confirmation dialog
/// var message = $"Estimated processing:\n" +
///               $"• {estimate.EstimatedParagraphs} paragraphs to simplify\n" +
///               $"• {estimate.EstimatedSkipped} paragraphs will be skipped\n" +
///               $"• {estimate.EstimatedTokens:N0} tokens (~${estimate.EstimatedCostUsd:F2})\n" +
///               $"• {estimate.EstimatedTime.TotalMinutes:F1} minutes";
///
/// if (await ConfirmAsync(message))
/// {
///     var result = await batchService.SimplifyDocumentAsync(documentPath, target);
/// }
/// </code>
/// </example>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="BatchSimplificationResult"/>
public record BatchSimplificationEstimate
{
    /// <summary>
    /// Default token factor for word-to-token conversion.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Average English text uses approximately 1.3 tokens per word.
    /// This factor accounts for subword tokenization in typical LLMs.
    /// </remarks>
    public const double DefaultTokenFactor = 1.3;

    /// <summary>
    /// Default seconds per paragraph for time estimation.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Based on typical API latency and processing time for
    /// moderate-length paragraphs.
    /// </remarks>
    public const double DefaultSecondsPerParagraph = 2.5;

    /// <summary>
    /// Gets the estimated number of paragraphs that will be processed.
    /// </summary>
    /// <value>
    /// Count of paragraphs that will be sent to the simplification pipeline.
    /// Does not include skipped paragraphs.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated by evaluating skip conditions against each
    /// paragraph without actually processing them.
    /// </remarks>
    public required int EstimatedParagraphs { get; init; }

    /// <summary>
    /// Gets the estimated number of paragraphs that will be skipped.
    /// </summary>
    /// <value>
    /// Count of paragraphs that meet skip conditions and will not be processed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Includes paragraphs that are already simple, too short,
    /// headings, code blocks, or otherwise excluded.
    /// </remarks>
    public required int EstimatedSkipped { get; init; }

    /// <summary>
    /// Gets the estimated total token usage.
    /// </summary>
    /// <value>
    /// Approximate token count for the entire batch operation,
    /// including both input and output tokens.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as: wordCount × <see cref="DefaultTokenFactor"/> × 2
    /// (doubling accounts for both input and output).
    /// </remarks>
    public required int EstimatedTokens { get; init; }

    /// <summary>
    /// Gets the estimated processing time.
    /// </summary>
    /// <value>
    /// Approximate duration for the batch operation to complete.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as: paragraphCount × <see cref="DefaultSecondsPerParagraph"/>.
    /// Does not account for API rate limiting or concurrent processing.
    /// </remarks>
    public required TimeSpan EstimatedTime { get; init; }

    /// <summary>
    /// Gets the estimated cost in USD.
    /// </summary>
    /// <value>
    /// Approximate monetary cost based on token usage and current pricing.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Based on model pricing at time of estimation. Actual costs
    /// may vary based on model selection and pricing changes.
    /// </remarks>
    public required decimal EstimatedCostUsd { get; init; }

    /// <summary>
    /// Gets the total paragraph count (to be processed + skipped).
    /// </summary>
    /// <value>
    /// Total number of paragraphs identified in the document.
    /// </value>
    public int TotalParagraphs =>
        EstimatedParagraphs + EstimatedSkipped;

    /// <summary>
    /// Gets the skip rate as a percentage (0-100).
    /// </summary>
    /// <value>
    /// Percentage of paragraphs that will be skipped.
    /// Returns 0 if total paragraphs is 0.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> High skip rates indicate the document is already well-simplified
    /// or contains primarily structural content (headings, code).
    /// </remarks>
    public double SkipRate =>
        TotalParagraphs > 0 ? (double)EstimatedSkipped / TotalParagraphs * 100 : 0;

    /// <summary>
    /// Gets a value indicating whether the estimate suggests low impact.
    /// </summary>
    /// <value>
    /// <c>true</c> if more than 80% of paragraphs will be skipped; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> When most paragraphs will be skipped, the batch operation may
    /// not provide significant value. Users may want to consider alternate approaches.
    /// </remarks>
    public bool IsLowImpact => SkipRate > 80;

    /// <summary>
    /// Gets a value indicating whether the estimate suggests high cost.
    /// </summary>
    /// <value>
    /// <c>true</c> if estimated cost exceeds $1.00 USD; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> High-cost operations warrant user confirmation before proceeding.
    /// </remarks>
    public bool IsHighCost => EstimatedCostUsd > 1.00m;

    /// <summary>
    /// Gets a value indicating whether the estimate suggests long duration.
    /// </summary>
    /// <value>
    /// <c>true</c> if estimated time exceeds 2 minutes; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Long operations warrant user confirmation and may benefit
    /// from progress display.
    /// </remarks>
    public bool IsLongRunning => EstimatedTime > TimeSpan.FromMinutes(2);

    /// <summary>
    /// Creates an empty estimate for documents with no content.
    /// </summary>
    /// <returns>An estimate with all values set to zero.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory for edge cases where document has no
    /// paragraphs or all paragraphs are skipped.
    /// </remarks>
    public static BatchSimplificationEstimate Empty => new()
    {
        EstimatedParagraphs = 0,
        EstimatedSkipped = 0,
        EstimatedTokens = 0,
        EstimatedTime = TimeSpan.Zero,
        EstimatedCostUsd = 0m
    };

    /// <summary>
    /// Creates an estimate from document analysis results.
    /// </summary>
    /// <param name="paragraphsToProcess">Number of paragraphs that will be processed.</param>
    /// <param name="paragraphsToSkip">Number of paragraphs that will be skipped.</param>
    /// <param name="totalWords">Total word count of paragraphs to process.</param>
    /// <param name="costPerToken">Cost per token in USD.</param>
    /// <returns>A calculated estimate.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method that applies standard formulas to calculate
    /// token usage, time, and cost from document metrics.
    /// </remarks>
    public static BatchSimplificationEstimate FromAnalysis(
        int paragraphsToProcess,
        int paragraphsToSkip,
        int totalWords,
        decimal costPerToken = 0.00001m)
    {
        // LOGIC: Calculate estimated tokens (input + output)
        var estimatedTokens = (int)(totalWords * DefaultTokenFactor * 2);

        // LOGIC: Calculate estimated time
        var estimatedTime = TimeSpan.FromSeconds(paragraphsToProcess * DefaultSecondsPerParagraph);

        // LOGIC: Calculate estimated cost
        var estimatedCost = estimatedTokens * costPerToken;

        return new BatchSimplificationEstimate
        {
            EstimatedParagraphs = paragraphsToProcess,
            EstimatedSkipped = paragraphsToSkip,
            EstimatedTokens = estimatedTokens,
            EstimatedTime = estimatedTime,
            EstimatedCostUsd = estimatedCost
        };
    }
}
