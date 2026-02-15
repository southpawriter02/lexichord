// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the complete result of a batch simplification operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationResult"/> record aggregates results
/// from all paragraphs processed by <see cref="IBatchSimplificationService"/>. It includes
/// document-level metrics, individual paragraph results, token usage, and timing information.
/// </para>
/// <para>
/// <b>Result Categories:</b>
/// <list type="bullet">
///   <item><description><see cref="SimplifiedParagraphs"/>: Paragraphs that were processed and changed</description></item>
///   <item><description><see cref="SkippedParagraphs"/>: Paragraphs that were not processed (skip conditions met)</description></item>
///   <item><description><see cref="ProcessedParagraphs"/>: Total paragraphs evaluated (simplified + skipped)</description></item>
///   <item><description><see cref="TotalParagraphs"/>: All paragraphs in scope</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Success States:</b>
/// <list type="bullet">
///   <item><description><see cref="WasCancelled"/> = false, <see cref="ErrorMessage"/> = null: Successful completion</description></item>
///   <item><description><see cref="WasCancelled"/> = true: User cancelled; partial results available</description></item>
///   <item><description><see cref="ErrorMessage"/> != null: Error occurred; partial results may be available</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// var result = await batchService.SimplifyDocumentAsync(documentPath, target, options, progress, ct);
///
/// Console.WriteLine($"Document simplification complete:");
/// Console.WriteLine($"  Paragraphs: {result.SimplifiedParagraphs} simplified, {result.SkippedParagraphs} skipped");
/// Console.WriteLine($"  Grade level: {result.OriginalDocumentMetrics.FleschKincaidGradeLevel:F1} → " +
///                   $"{result.SimplifiedDocumentMetrics.FleschKincaidGradeLevel:F1}");
/// Console.WriteLine($"  Reduction: {result.GradeLevelReduction:F1} grade levels");
/// Console.WriteLine($"  Time: {result.TotalProcessingTime.TotalSeconds:F1}s");
/// Console.WriteLine($"  Tokens: {result.TotalTokenUsage.TotalTokens:N0} (~${result.TotalTokenUsage.EstimatedCost:F2})");
///
/// if (result.WasCancelled)
/// {
///     Console.WriteLine("  Note: Operation was cancelled before completion.");
/// }
/// </code>
/// </example>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="ParagraphSimplificationResult"/>
/// <seealso cref="BatchSimplificationOptions"/>
public record BatchSimplificationResult
{
    /// <summary>
    /// Gets the path to the document that was processed.
    /// </summary>
    /// <value>
    /// The document path provided to the batch operation.
    /// </value>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Gets the total number of paragraphs in the document/selection.
    /// </summary>
    /// <value>
    /// Total count of paragraphs identified within the processing scope.
    /// </value>
    public required int TotalParagraphs { get; init; }

    /// <summary>
    /// Gets the number of paragraphs that were evaluated.
    /// </summary>
    /// <value>
    /// Count of paragraphs that were analyzed (simplified + skipped).
    /// Less than <see cref="TotalParagraphs"/> if cancelled.
    /// </value>
    public required int ProcessedParagraphs { get; init; }

    /// <summary>
    /// Gets the number of paragraphs that were simplified.
    /// </summary>
    /// <value>
    /// Count of paragraphs that were processed through the simplification pipeline.
    /// </value>
    public required int SimplifiedParagraphs { get; init; }

    /// <summary>
    /// Gets the number of paragraphs that were skipped.
    /// </summary>
    /// <value>
    /// Count of paragraphs that were skipped due to skip conditions
    /// (already simple, too short, structural elements, etc.).
    /// </value>
    public required int SkippedParagraphs { get; init; }

    /// <summary>
    /// Gets the readability metrics for the original document.
    /// </summary>
    /// <value>
    /// Aggregate metrics calculated across all paragraphs before simplification.
    /// </value>
    public required ReadabilityMetrics OriginalDocumentMetrics { get; init; }

    /// <summary>
    /// Gets the readability metrics for the simplified document.
    /// </summary>
    /// <value>
    /// Aggregate metrics calculated across all paragraphs after simplification.
    /// </value>
    public required ReadabilityMetrics SimplifiedDocumentMetrics { get; init; }

    /// <summary>
    /// Gets the results for each processed paragraph.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="ParagraphSimplificationResult"/> records,
    /// one for each paragraph that was evaluated.
    /// </value>
    public required IReadOnlyList<ParagraphSimplificationResult> ParagraphResults { get; init; }

    /// <summary>
    /// Gets the total token usage across all paragraphs.
    /// </summary>
    /// <value>
    /// Aggregated usage metrics including prompt tokens, completion tokens, and cost.
    /// </value>
    public required UsageMetrics TotalTokenUsage { get; init; }

    /// <summary>
    /// Gets the total processing time.
    /// </summary>
    /// <value>
    /// Elapsed time from batch start to completion (or cancellation).
    /// </value>
    public required TimeSpan TotalProcessingTime { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation was cancelled.
    /// </summary>
    /// <value>
    /// <c>true</c> if the user cancelled before all paragraphs were processed;
    /// otherwise, <c>false</c>.
    /// </value>
    public required bool WasCancelled { get; init; }

    /// <summary>
    /// Gets the error message if the operation failed.
    /// </summary>
    /// <value>
    /// A user-facing error message if an error occurred; otherwise, <c>null</c>.
    /// </value>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets the aggregate glossary from all paragraphs.
    /// </summary>
    /// <value>
    /// A dictionary mapping technical terms to their simplified equivalents,
    /// merged from all paragraph glossaries. <c>null</c> if glossary generation
    /// was not requested.
    /// </value>
    public IReadOnlyDictionary<string, string>? AggregateGlossary { get; init; }

    /// <summary>
    /// Gets the document-wide grade level reduction.
    /// </summary>
    /// <value>
    /// The difference in Flesch-Kincaid grade level between original and
    /// simplified document. Positive values indicate improvement.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>OriginalDocumentMetrics.FleschKincaidGradeLevel -
    /// SimplifiedDocumentMetrics.FleschKincaidGradeLevel</c>.
    /// </remarks>
    public double GradeLevelReduction =>
        OriginalDocumentMetrics.FleschKincaidGradeLevel -
        SimplifiedDocumentMetrics.FleschKincaidGradeLevel;

    /// <summary>
    /// Gets the average time per paragraph.
    /// </summary>
    /// <value>
    /// Mean processing time calculated as <c>TotalProcessingTime / ProcessedParagraphs</c>.
    /// Returns <see cref="TimeSpan.Zero"/> if no paragraphs were processed.
    /// </value>
    public TimeSpan AverageTimePerParagraph =>
        ProcessedParagraphs > 0
            ? TotalProcessingTime / ProcessedParagraphs
            : TimeSpan.Zero;

    /// <summary>
    /// Gets the simplification rate as a percentage.
    /// </summary>
    /// <value>
    /// Percentage of paragraphs that were simplified (vs. skipped).
    /// Returns 0 if no paragraphs were processed.
    /// </value>
    public double SimplificationRate =>
        ProcessedParagraphs > 0
            ? (double)SimplifiedParagraphs / ProcessedParagraphs * 100
            : 0;

    /// <summary>
    /// Gets the skip rate as a percentage.
    /// </summary>
    /// <value>
    /// Percentage of paragraphs that were skipped.
    /// Returns 0 if no paragraphs were processed.
    /// </value>
    public double SkipRate =>
        ProcessedParagraphs > 0
            ? (double)SkippedParagraphs / ProcessedParagraphs * 100
            : 0;

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    /// <value>
    /// <c>true</c> if not cancelled and no error occurred; otherwise, <c>false</c>.
    /// </value>
    public bool Success =>
        !WasCancelled && ErrorMessage is null;

    /// <summary>
    /// Gets the number of paragraphs that were actually changed.
    /// </summary>
    /// <value>
    /// Count of paragraphs where <see cref="ParagraphSimplificationResult.TextChanged"/>
    /// is true.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Some paragraphs may be "simplified" (processed) but produce
    /// identical output. This property counts only actual text changes.
    /// </remarks>
    public int ChangedParagraphs =>
        ParagraphResults.Count(r => r.TextChanged);

    /// <summary>
    /// Gets the average grade level reduction across simplified paragraphs.
    /// </summary>
    /// <value>
    /// Mean grade level reduction for paragraphs that were simplified.
    /// Returns 0 if no paragraphs were simplified.
    /// </value>
    public double AverageGradeLevelReduction
    {
        get
        {
            var simplified = ParagraphResults.Where(r => r.WasSimplified).ToList();
            return simplified.Count > 0
                ? simplified.Average(r => r.GradeLevelReduction)
                : 0;
        }
    }

    /// <summary>
    /// Creates a result for a failed batch operation.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="errorMessage">User-facing error message.</param>
    /// <param name="processingTime">Elapsed time before failure.</param>
    /// <returns>A failed result with zero counts and empty metrics.</returns>
    public static BatchSimplificationResult Failed(
        string documentPath,
        string errorMessage,
        TimeSpan processingTime)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(errorMessage);

        return new BatchSimplificationResult
        {
            DocumentPath = documentPath,
            TotalParagraphs = 0,
            ProcessedParagraphs = 0,
            SimplifiedParagraphs = 0,
            SkippedParagraphs = 0,
            OriginalDocumentMetrics = ReadabilityMetrics.Empty,
            SimplifiedDocumentMetrics = ReadabilityMetrics.Empty,
            ParagraphResults = Array.Empty<ParagraphSimplificationResult>(),
            TotalTokenUsage = UsageMetrics.Zero,
            TotalProcessingTime = processingTime,
            WasCancelled = false,
            ErrorMessage = errorMessage,
            AggregateGlossary = null
        };
    }

    /// <summary>
    /// Creates a result for a cancelled batch operation.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="totalParagraphs">Total paragraphs in document.</param>
    /// <param name="processedParagraphs">Paragraphs processed before cancellation.</param>
    /// <param name="simplifiedParagraphs">Paragraphs simplified before cancellation.</param>
    /// <param name="skippedParagraphs">Paragraphs skipped before cancellation.</param>
    /// <param name="paragraphResults">Results for processed paragraphs.</param>
    /// <param name="originalMetrics">Original document metrics.</param>
    /// <param name="totalTokenUsage">Accumulated token usage.</param>
    /// <param name="processingTime">Elapsed time before cancellation.</param>
    /// <returns>A cancelled result with partial data.</returns>
    public static BatchSimplificationResult Cancelled(
        string documentPath,
        int totalParagraphs,
        int processedParagraphs,
        int simplifiedParagraphs,
        int skippedParagraphs,
        IReadOnlyList<ParagraphSimplificationResult> paragraphResults,
        ReadabilityMetrics originalMetrics,
        UsageMetrics totalTokenUsage,
        TimeSpan processingTime)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(paragraphResults);
        ArgumentNullException.ThrowIfNull(originalMetrics);
        ArgumentNullException.ThrowIfNull(totalTokenUsage);

        return new BatchSimplificationResult
        {
            DocumentPath = documentPath,
            TotalParagraphs = totalParagraphs,
            ProcessedParagraphs = processedParagraphs,
            SimplifiedParagraphs = simplifiedParagraphs,
            SkippedParagraphs = skippedParagraphs,
            OriginalDocumentMetrics = originalMetrics,
            SimplifiedDocumentMetrics = originalMetrics, // Partial - use original
            ParagraphResults = paragraphResults,
            TotalTokenUsage = totalTokenUsage,
            TotalProcessingTime = processingTime,
            WasCancelled = true,
            ErrorMessage = null,
            AggregateGlossary = null
        };
    }
}
