// -----------------------------------------------------------------------
// <copyright file="ParagraphSimplificationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the result of simplifying a single paragraph within a batch operation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="ParagraphSimplificationResult"/> record is produced for
/// each paragraph processed by <see cref="IBatchSimplificationService"/>, whether the
/// paragraph was simplified or skipped. Results are collected in
/// <see cref="BatchSimplificationResult.ParagraphResults"/>.
/// </para>
/// <para>
/// <b>Simplified vs. Skipped:</b>
/// <list type="bullet">
///   <item><description><see cref="WasSimplified"/> = true: Paragraph was processed through the pipeline</description></item>
///   <item><description><see cref="WasSimplified"/> = false: Paragraph was skipped; check <see cref="SkipReason"/></description></item>
/// </list>
/// </para>
/// <para>
/// <b>Skipped Paragraphs:</b>
/// When <see cref="WasSimplified"/> is false, the <see cref="SimplifiedText"/> equals
/// <see cref="OriginalText"/>, and <see cref="SimplifiedMetrics"/> equals <see cref="OriginalMetrics"/>.
/// The <see cref="SkipReason"/> indicates why the paragraph was not processed.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Processing results from a batch operation
/// foreach (var result in batchResult.ParagraphResults)
/// {
///     if (result.WasSimplified)
///     {
///         Console.WriteLine($"Para {result.ParagraphIndex}: " +
///             $"Grade {result.OriginalMetrics.FleschKincaidGradeLevel:F1} → " +
///             $"{result.SimplifiedMetrics.FleschKincaidGradeLevel:F1} " +
///             $"({result.GradeLevelReduction:+0.0;-0.0;0} grades)");
///     }
///     else
///     {
///         Console.WriteLine($"Para {result.ParagraphIndex}: Skipped - {result.SkipReason}");
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="BatchSimplificationResult"/>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="ParagraphSkipReason"/>
public record ParagraphSimplificationResult
{
    /// <summary>
    /// Gets the zero-based index of the paragraph in the document.
    /// </summary>
    /// <value>
    /// Sequential index starting from 0 for the first paragraph.
    /// Corresponds to the paragraph's position in the parsed document.
    /// </value>
    public required int ParagraphIndex { get; init; }

    /// <summary>
    /// Gets the original paragraph text.
    /// </summary>
    /// <value>
    /// The text content before simplification. For skipped paragraphs,
    /// this equals <see cref="SimplifiedText"/>.
    /// </value>
    public required string OriginalText { get; init; }

    /// <summary>
    /// Gets the simplified paragraph text.
    /// </summary>
    /// <value>
    /// The text content after simplification. For skipped paragraphs,
    /// this equals <see cref="OriginalText"/>.
    /// </value>
    public required string SimplifiedText { get; init; }

    /// <summary>
    /// Gets the character offset where this paragraph begins in the document.
    /// </summary>
    /// <value>
    /// Zero-based character index from the start of the document.
    /// Used for document reconstruction.
    /// </value>
    public required int StartOffset { get; init; }

    /// <summary>
    /// Gets the exclusive end offset of the paragraph in the document.
    /// </summary>
    /// <value>
    /// The character offset immediately after the last character of the paragraph.
    /// </value>
    public required int EndOffset { get; init; }

    /// <summary>
    /// Gets the readability metrics for the original paragraph.
    /// </summary>
    /// <value>
    /// Metrics calculated before simplification, including Flesch-Kincaid
    /// grade level, word count, and complexity indicators.
    /// </value>
    public required ReadabilityMetrics OriginalMetrics { get; init; }

    /// <summary>
    /// Gets the readability metrics for the simplified paragraph.
    /// </summary>
    /// <value>
    /// Metrics calculated after simplification. For skipped paragraphs,
    /// this equals <see cref="OriginalMetrics"/>.
    /// </value>
    public required ReadabilityMetrics SimplifiedMetrics { get; init; }

    /// <summary>
    /// Gets a value indicating whether the paragraph was simplified.
    /// </summary>
    /// <value>
    /// <c>true</c> if the paragraph was processed through the simplification pipeline;
    /// <c>false</c> if it was skipped.
    /// </value>
    public required bool WasSimplified { get; init; }

    /// <summary>
    /// Gets the reason the paragraph was skipped.
    /// </summary>
    /// <value>
    /// The <see cref="ParagraphSkipReason"/> indicating why processing was skipped.
    /// <see cref="ParagraphSkipReason.None"/> if the paragraph was simplified.
    /// </value>
    public ParagraphSkipReason SkipReason { get; init; } = ParagraphSkipReason.None;

    /// <summary>
    /// Gets the list of changes made to this paragraph.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="SimplificationChange"/> records.
    /// <c>null</c> or empty for skipped paragraphs.
    /// </value>
    public IReadOnlyList<SimplificationChange>? Changes { get; init; }

    /// <summary>
    /// Gets the token usage for this paragraph.
    /// </summary>
    /// <value>
    /// Usage metrics including prompt tokens, completion tokens, and cost.
    /// <c>null</c> for skipped paragraphs.
    /// </value>
    public UsageMetrics? TokenUsage { get; init; }

    /// <summary>
    /// Gets the processing time for this paragraph.
    /// </summary>
    /// <value>
    /// Elapsed time for processing this paragraph.
    /// Near-zero for skipped paragraphs.
    /// </value>
    public TimeSpan ProcessingTime { get; init; }

    /// <summary>
    /// Gets the grade level reduction achieved for this paragraph.
    /// </summary>
    /// <value>
    /// The difference in Flesch-Kincaid grade level. Positive values indicate
    /// improvement (lower grade = easier). Zero for skipped paragraphs.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>OriginalMetrics.FleschKincaidGradeLevel -
    /// SimplifiedMetrics.FleschKincaidGradeLevel</c>.
    /// </remarks>
    public double GradeLevelReduction =>
        OriginalMetrics.FleschKincaidGradeLevel - SimplifiedMetrics.FleschKincaidGradeLevel;

    /// <summary>
    /// Gets a value indicating whether the text was actually changed.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="SimplifiedText"/> differs from <see cref="OriginalText"/>;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> A paragraph may be "simplified" (processed) but produce identical
    /// output if it was already at the target level.
    /// </remarks>
    public bool TextChanged =>
        !string.Equals(OriginalText, SimplifiedText, StringComparison.Ordinal);

    /// <summary>
    /// Gets a preview of the original text for display.
    /// </summary>
    /// <value>
    /// The first 50 characters of the original text, truncated with "..."
    /// if longer.
    /// </value>
    public string OriginalTextPreview =>
        OriginalText.Length <= 50
            ? OriginalText.Replace('\n', ' ')
            : OriginalText[..47].Replace('\n', ' ') + "...";

    /// <summary>
    /// Gets a human-readable description of the skip reason.
    /// </summary>
    /// <value>
    /// A user-friendly string describing why the paragraph was skipped,
    /// or <c>null</c> if it was simplified.
    /// </value>
    public string? SkipReasonDescription => SkipReason switch
    {
        ParagraphSkipReason.None => null,
        ParagraphSkipReason.AlreadySimple => $"Already at target level (Grade {OriginalMetrics.FleschKincaidGradeLevel:F1})",
        ParagraphSkipReason.TooShort => $"Too short ({OriginalMetrics.WordCount} words)",
        ParagraphSkipReason.IsHeading => "Heading",
        ParagraphSkipReason.IsCodeBlock => "Code block",
        ParagraphSkipReason.IsBlockquote => "Blockquote",
        ParagraphSkipReason.IsListItem => "List item",
        ParagraphSkipReason.MaxParagraphsReached => "Maximum paragraphs limit reached",
        ParagraphSkipReason.UserCancelled => "User cancelled",
        ParagraphSkipReason.ProcessingFailed => "Processing failed",
        _ => SkipReason.ToString()
    };

    /// <summary>
    /// Creates a result for a skipped paragraph.
    /// </summary>
    /// <param name="paragraphIndex">Zero-based paragraph index.</param>
    /// <param name="originalText">The original paragraph text.</param>
    /// <param name="startOffset">Character offset where paragraph begins.</param>
    /// <param name="endOffset">Exclusive end offset.</param>
    /// <param name="metrics">Readability metrics for the paragraph.</param>
    /// <param name="skipReason">Reason for skipping.</param>
    /// <returns>A result indicating the paragraph was skipped.</returns>
    public static ParagraphSimplificationResult Skipped(
        int paragraphIndex,
        string originalText,
        int startOffset,
        int endOffset,
        ReadabilityMetrics metrics,
        ParagraphSkipReason skipReason)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(metrics);

        return new ParagraphSimplificationResult
        {
            ParagraphIndex = paragraphIndex,
            OriginalText = originalText,
            SimplifiedText = originalText,
            StartOffset = startOffset,
            EndOffset = endOffset,
            OriginalMetrics = metrics,
            SimplifiedMetrics = metrics,
            WasSimplified = false,
            SkipReason = skipReason,
            Changes = null,
            TokenUsage = null,
            ProcessingTime = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Creates a result for a successfully simplified paragraph.
    /// </summary>
    /// <param name="paragraphIndex">Zero-based paragraph index.</param>
    /// <param name="originalText">The original paragraph text.</param>
    /// <param name="simplifiedText">The simplified paragraph text.</param>
    /// <param name="startOffset">Character offset where paragraph begins.</param>
    /// <param name="endOffset">Exclusive end offset.</param>
    /// <param name="originalMetrics">Readability metrics before simplification.</param>
    /// <param name="simplifiedMetrics">Readability metrics after simplification.</param>
    /// <param name="changes">List of changes made.</param>
    /// <param name="tokenUsage">Token usage for this paragraph.</param>
    /// <param name="processingTime">Time taken to process.</param>
    /// <returns>A result indicating successful simplification.</returns>
    public static ParagraphSimplificationResult Simplified(
        int paragraphIndex,
        string originalText,
        string simplifiedText,
        int startOffset,
        int endOffset,
        ReadabilityMetrics originalMetrics,
        ReadabilityMetrics simplifiedMetrics,
        IReadOnlyList<SimplificationChange>? changes,
        UsageMetrics tokenUsage,
        TimeSpan processingTime)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(simplifiedText);
        ArgumentNullException.ThrowIfNull(originalMetrics);
        ArgumentNullException.ThrowIfNull(simplifiedMetrics);
        ArgumentNullException.ThrowIfNull(tokenUsage);

        return new ParagraphSimplificationResult
        {
            ParagraphIndex = paragraphIndex,
            OriginalText = originalText,
            SimplifiedText = simplifiedText,
            StartOffset = startOffset,
            EndOffset = endOffset,
            OriginalMetrics = originalMetrics,
            SimplifiedMetrics = simplifiedMetrics,
            WasSimplified = true,
            SkipReason = ParagraphSkipReason.None,
            Changes = changes,
            TokenUsage = tokenUsage,
            ProcessingTime = processingTime
        };
    }
}
