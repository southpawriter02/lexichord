// -----------------------------------------------------------------------
// <copyright file="BatchSimplificationProgress.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Progress information reported during batch simplification operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="BatchSimplificationProgress"/> record is reported via
/// <see cref="IProgress{T}"/> during batch simplification operations. It provides
/// real-time feedback for UI progress displays, including current paragraph,
/// completion percentage, and time estimates.
/// </para>
/// <para>
/// <b>Reporting Frequency:</b>
/// Progress is reported at the following points:
/// <list type="bullet">
///   <item><description>Initial: Phase = Initializing (before processing begins)</description></item>
///   <item><description>Analysis: Phase = AnalyzingDocument (after document parsing)</description></item>
///   <item><description>Per-paragraph: Phase = ProcessingParagraphs (after each paragraph)</description></item>
///   <item><description>Final: Phase = Completed/Cancelled/Failed (when operation ends)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Time Estimation:</b>
/// The <see cref="EstimatedTimeRemaining"/> is calculated using a running average
/// of paragraph processing times, updated after each paragraph completes.
/// </para>
/// <para><b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.</para>
/// </remarks>
/// <example>
/// <code>
/// // Handling progress reports
/// var progress = new Progress&lt;BatchSimplificationProgress&gt;(p =>
/// {
///     progressBar.Value = p.PercentComplete;
///     statusText.Text = p.StatusMessage;
///     timeRemainingText.Text = FormatTimeSpan(p.EstimatedTimeRemaining);
///     paragraphPreview.Text = p.CurrentParagraphPreview;
///
///     UpdateStatistics(p.SimplifiedSoFar, p.SkippedSoFar);
/// });
///
/// var result = await batchService.SimplifyDocumentAsync(
///     documentPath, target, options, progress, cancellationToken);
/// </code>
/// </example>
/// <seealso cref="IBatchSimplificationService"/>
/// <seealso cref="BatchSimplificationPhase"/>
/// <seealso cref="BatchSimplificationResult"/>
public record BatchSimplificationProgress
{
    /// <summary>
    /// Default preview length for paragraph text.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> 50 characters provides enough context for identification
    /// without overwhelming the UI or logs.
    /// </remarks>
    public const int DefaultPreviewLength = 50;

    /// <summary>
    /// Gets the current paragraph being processed (1-based index).
    /// </summary>
    /// <value>
    /// The 1-based index of the paragraph currently being processed.
    /// Value is 0 during initialization, and equals <see cref="TotalParagraphs"/>
    /// when complete.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> 1-based indexing is used for user-friendly display
    /// (e.g., "Processing paragraph 5 of 20").
    /// </remarks>
    public required int CurrentParagraph { get; init; }

    /// <summary>
    /// Gets the total number of paragraphs in the document.
    /// </summary>
    /// <value>
    /// The total count of paragraphs identified for processing.
    /// Includes both paragraphs that will be processed and skipped.
    /// </value>
    public required int TotalParagraphs { get; init; }

    /// <summary>
    /// Gets a preview of the current paragraph text.
    /// </summary>
    /// <value>
    /// The first <see cref="DefaultPreviewLength"/> characters of the current
    /// paragraph, truncated with "..." if longer. Contains descriptive text
    /// during initialization and completion phases.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Preview text helps users identify which content is being
    /// processed without exposing the full paragraph.
    /// </remarks>
    public required string CurrentParagraphPreview { get; init; }

    /// <summary>
    /// Gets the completion percentage (0-100).
    /// </summary>
    /// <value>
    /// A value between 0 and 100 indicating overall progress.
    /// Calculated as <c>(CurrentParagraph / TotalParagraphs) * 100</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Percentage is based on paragraph count, not processing time,
    /// as paragraph complexity varies significantly.
    /// </remarks>
    public required double PercentComplete { get; init; }

    /// <summary>
    /// Gets the estimated time remaining.
    /// </summary>
    /// <value>
    /// An estimate of time until completion, based on the running average
    /// of paragraph processing times.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>averageTimePerParagraph × remainingParagraphs</c>.
    /// Updated after each paragraph completes with improved accuracy.
    /// </remarks>
    public required TimeSpan EstimatedTimeRemaining { get; init; }

    /// <summary>
    /// Gets the number of paragraphs simplified so far.
    /// </summary>
    /// <value>
    /// Count of paragraphs that were successfully simplified (not skipped).
    /// </value>
    public required int SimplifiedSoFar { get; init; }

    /// <summary>
    /// Gets the number of paragraphs skipped so far.
    /// </summary>
    /// <value>
    /// Count of paragraphs that were skipped due to skip conditions
    /// (already simple, too short, headings, code blocks, etc.).
    /// </value>
    public required int SkippedSoFar { get; init; }

    /// <summary>
    /// Gets a human-readable status message.
    /// </summary>
    /// <value>
    /// A brief description of the current operation state, suitable for
    /// display in a status bar or progress dialog.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Examples include "Initializing...", "Analyzing document...",
    /// "Processing paragraph 5 of 20...", "Completed: 15 paragraphs simplified".
    /// </remarks>
    public required string StatusMessage { get; init; }

    /// <summary>
    /// Gets the current processing phase.
    /// </summary>
    /// <value>
    /// The <see cref="BatchSimplificationPhase"/> indicating which stage
    /// of the batch operation is currently executing.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Phase transitions follow the sequence:
    /// Initializing → AnalyzingDocument → ProcessingParagraphs → Completed/Cancelled/Failed.
    /// </remarks>
    public required BatchSimplificationPhase Phase { get; init; }

    /// <summary>
    /// Gets a value indicating whether the operation is in a terminal phase.
    /// </summary>
    /// <value>
    /// <c>true</c> if the phase is Completed, Cancelled, or Failed; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Terminal phases indicate no further progress updates will occur.
    /// </remarks>
    public bool IsTerminal =>
        Phase is BatchSimplificationPhase.Completed
            or BatchSimplificationPhase.Cancelled
            or BatchSimplificationPhase.Failed;

    /// <summary>
    /// Gets a value indicating whether the operation is actively processing.
    /// </summary>
    /// <value>
    /// <c>true</c> if the phase is ProcessingParagraphs; otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> During active processing, paragraph-level updates are expected.
    /// </remarks>
    public bool IsProcessing =>
        Phase == BatchSimplificationPhase.ProcessingParagraphs;

    /// <summary>
    /// Gets the number of paragraphs remaining to process.
    /// </summary>
    /// <value>
    /// Count of paragraphs not yet processed.
    /// Calculated as <c>TotalParagraphs - CurrentParagraph</c>.
    /// </value>
    public int RemainingParagraphs =>
        Math.Max(0, TotalParagraphs - CurrentParagraph);

    /// <summary>
    /// Creates a progress report for the initialization phase.
    /// </summary>
    /// <param name="totalParagraphs">Total paragraphs in the document.</param>
    /// <returns>A progress report for initialization.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating the initial progress report
    /// before processing begins.
    /// </remarks>
    public static BatchSimplificationProgress Initializing(int totalParagraphs) => new()
    {
        CurrentParagraph = 0,
        TotalParagraphs = totalParagraphs,
        CurrentParagraphPreview = "Initializing...",
        PercentComplete = 0,
        EstimatedTimeRemaining = TimeSpan.Zero,
        SimplifiedSoFar = 0,
        SkippedSoFar = 0,
        StatusMessage = "Initializing batch simplification...",
        Phase = BatchSimplificationPhase.Initializing
    };

    /// <summary>
    /// Creates a progress report for the document analysis phase.
    /// </summary>
    /// <param name="totalParagraphs">Total paragraphs identified.</param>
    /// <param name="estimatedTime">Estimated total processing time.</param>
    /// <returns>A progress report for document analysis.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating a progress report after
    /// document parsing completes.
    /// </remarks>
    public static BatchSimplificationProgress Analyzing(
        int totalParagraphs,
        TimeSpan estimatedTime) => new()
    {
        CurrentParagraph = 0,
        TotalParagraphs = totalParagraphs,
        CurrentParagraphPreview = "Analyzing document structure...",
        PercentComplete = 0,
        EstimatedTimeRemaining = estimatedTime,
        SimplifiedSoFar = 0,
        SkippedSoFar = 0,
        StatusMessage = $"Analyzing document ({totalParagraphs} paragraphs found)...",
        Phase = BatchSimplificationPhase.AnalyzingDocument
    };

    /// <summary>
    /// Creates a progress report for successful completion.
    /// </summary>
    /// <param name="totalParagraphs">Total paragraphs processed.</param>
    /// <param name="simplified">Number of paragraphs simplified.</param>
    /// <param name="skipped">Number of paragraphs skipped.</param>
    /// <returns>A completion progress report.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating the final progress report
    /// when processing completes successfully.
    /// </remarks>
    public static BatchSimplificationProgress Completed(
        int totalParagraphs,
        int simplified,
        int skipped) => new()
    {
        CurrentParagraph = totalParagraphs,
        TotalParagraphs = totalParagraphs,
        CurrentParagraphPreview = "Complete",
        PercentComplete = 100,
        EstimatedTimeRemaining = TimeSpan.Zero,
        SimplifiedSoFar = simplified,
        SkippedSoFar = skipped,
        StatusMessage = $"Completed: {simplified} paragraphs simplified, {skipped} skipped",
        Phase = BatchSimplificationPhase.Completed
    };

    /// <summary>
    /// Creates a progress report for cancellation.
    /// </summary>
    /// <param name="currentParagraph">Paragraph index when cancelled.</param>
    /// <param name="totalParagraphs">Total paragraphs in document.</param>
    /// <param name="simplified">Number of paragraphs simplified before cancellation.</param>
    /// <param name="skipped">Number of paragraphs skipped before cancellation.</param>
    /// <returns>A cancellation progress report.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for creating the final progress report
    /// when processing is cancelled by the user.
    /// </remarks>
    public static BatchSimplificationProgress Cancelled(
        int currentParagraph,
        int totalParagraphs,
        int simplified,
        int skipped) => new()
    {
        CurrentParagraph = currentParagraph,
        TotalParagraphs = totalParagraphs,
        CurrentParagraphPreview = "Cancelled",
        PercentComplete = (double)currentParagraph / totalParagraphs * 100,
        EstimatedTimeRemaining = TimeSpan.Zero,
        SimplifiedSoFar = simplified,
        SkippedSoFar = skipped,
        StatusMessage = $"Cancelled at paragraph {currentParagraph} of {totalParagraphs}",
        Phase = BatchSimplificationPhase.Cancelled
    };
}
