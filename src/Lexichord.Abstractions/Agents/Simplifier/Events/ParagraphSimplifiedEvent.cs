// -----------------------------------------------------------------------
// <copyright file="ParagraphSimplifiedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Simplifier.Events;

/// <summary>
/// Published when a single paragraph is processed during batch simplification.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by <see cref="IBatchSimplificationService"/>
/// after each paragraph is either simplified or skipped. Subscribers can use this
/// event for:
/// </para>
/// <list type="bullet">
///   <item><description>Fine-grained progress tracking</description></item>
///   <item><description>Real-time UI updates showing current paragraph</description></item>
///   <item><description>Logging individual paragraph processing</description></item>
///   <item><description>Analytics on skip reasons and grade level improvements</description></item>
///   <item><description>Streaming updates to external systems</description></item>
/// </list>
/// <para>
/// <b>Event Frequency:</b>
/// This event is published once for every paragraph in the batch scope, regardless
/// of whether it was simplified or skipped. For large documents, this can result
/// in many events—handlers should be lightweight.
/// </para>
/// <para>
/// <b>Skip vs. Simplify:</b>
/// Check <see cref="WasSimplified"/> to determine if the paragraph was processed
/// through the pipeline or skipped. When skipped, <see cref="SkipReason"/> contains
/// the reason.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// The file path of the document being processed.
/// </param>
/// <param name="ParagraphIndex">
/// The zero-based index of the paragraph in the document.
/// </param>
/// <param name="TotalParagraphs">
/// The total number of paragraphs in the batch scope.
/// </param>
/// <param name="OriginalGradeLevel">
/// The Flesch-Kincaid grade level of the original paragraph.
/// </param>
/// <param name="SimplifiedGradeLevel">
/// The Flesch-Kincaid grade level after simplification.
/// Equals <paramref name="OriginalGradeLevel"/> if the paragraph was skipped.
/// </param>
/// <param name="WasSimplified">
/// <c>true</c> if the paragraph was processed through the simplification pipeline;
/// <c>false</c> if it was skipped.
/// </param>
/// <param name="SkipReason">
/// The reason the paragraph was skipped, or <see cref="ParagraphSkipReason.None"/>
/// if it was simplified.
/// </param>
/// <example>
/// <code>
/// // Publishing the event after processing a paragraph
/// await _mediator.Publish(new ParagraphSimplifiedEvent(
///     DocumentPath: "/path/to/document.md",
///     ParagraphIndex: 5,
///     TotalParagraphs: 63,
///     OriginalGradeLevel: 14.2,
///     SimplifiedGradeLevel: 8.1,
///     WasSimplified: true,
///     SkipReason: ParagraphSkipReason.None));
///
/// // Publishing for a skipped paragraph
/// await _mediator.Publish(new ParagraphSimplifiedEvent(
///     DocumentPath: "/path/to/document.md",
///     ParagraphIndex: 6,
///     TotalParagraphs: 63,
///     OriginalGradeLevel: 7.5,
///     SimplifiedGradeLevel: 7.5,
///     WasSimplified: false,
///     SkipReason: ParagraphSkipReason.AlreadySimple));
///
/// // Handling the event for real-time UI updates
/// public class ProgressHandler : INotificationHandler&lt;ParagraphSimplifiedEvent&gt;
/// {
///     public Task Handle(ParagraphSimplifiedEvent e, CancellationToken ct)
///     {
///         _progressService.UpdateParagraph(
///             index: e.ParagraphIndex,
///             total: e.TotalParagraphs,
///             simplified: e.WasSimplified,
///             gradeReduction: e.GradeLevelReduction);
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationCompletedEvent"/>
/// <seealso cref="BatchSimplificationCancelledEvent"/>
/// <seealso cref="IBatchSimplificationService"/>
public record ParagraphSimplifiedEvent(
    string DocumentPath,
    int ParagraphIndex,
    int TotalParagraphs,
    double OriginalGradeLevel,
    double SimplifiedGradeLevel,
    bool WasSimplified,
    ParagraphSkipReason SkipReason) : INotification
{
    /// <summary>
    /// Gets the 1-based paragraph number for display.
    /// </summary>
    /// <value>
    /// The paragraph index plus one, for user-friendly display
    /// (e.g., "Paragraph 5 of 63").
    /// </value>
    public int ParagraphNumber => ParagraphIndex + 1;

    /// <summary>
    /// Gets the grade level reduction achieved.
    /// </summary>
    /// <value>
    /// The difference in Flesch-Kincaid grade level.
    /// Positive values indicate improvement (lower grade = easier).
    /// Zero for skipped paragraphs.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>OriginalGradeLevel - SimplifiedGradeLevel</c>.
    /// </remarks>
    public double GradeLevelReduction =>
        OriginalGradeLevel - SimplifiedGradeLevel;

    /// <summary>
    /// Gets the completion percentage at this point.
    /// </summary>
    /// <value>
    /// Progress as a percentage (0-100) based on paragraphs processed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Calculated as <c>(ParagraphNumber / TotalParagraphs) * 100</c>.
    /// </remarks>
    public double PercentComplete =>
        TotalParagraphs > 0
            ? (double)ParagraphNumber / TotalParagraphs * 100
            : 0;

    /// <summary>
    /// Gets a value indicating whether this is the last paragraph.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ParagraphNumber"/> equals <see cref="TotalParagraphs"/>;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool IsLastParagraph =>
        ParagraphNumber == TotalParagraphs;

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
        ParagraphSkipReason.AlreadySimple => $"Already at target level (Grade {OriginalGradeLevel:F1})",
        ParagraphSkipReason.TooShort => "Too short",
        ParagraphSkipReason.IsHeading => "Heading",
        ParagraphSkipReason.IsCodeBlock => "Code block",
        ParagraphSkipReason.IsBlockquote => "Blockquote",
        ParagraphSkipReason.IsListItem => "List item",
        ParagraphSkipReason.MaxParagraphsReached => "Maximum paragraphs limit reached",
        ParagraphSkipReason.UserCancelled => "User cancelled",
        ParagraphSkipReason.ProcessingFailed => "Processing failed",
        _ => SkipReason.ToString()
    };
}
