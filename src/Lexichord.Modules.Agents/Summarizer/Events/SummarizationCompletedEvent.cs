// -----------------------------------------------------------------------
// <copyright file="SummarizationCompletedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a summarization operation
//   completes successfully (v0.7.6a). Published by SummarizerAgent after
//   the LLM response is parsed and metrics are calculated.
//
//   Consumers:
//     - UI components (completion state, metrics display)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Summarizer;
using MediatR;

namespace Lexichord.Modules.Agents.Summarizer.Events;

/// <summary>
/// Published when a summarization operation completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummarizerAgent after the LLM response is parsed and metrics
/// are calculated. Carries completion metrics including word counts, compression ratio,
/// chunking status, and total duration for observability.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6a</para>
/// </remarks>
/// <param name="Mode">The summarization mode that was executed.</param>
/// <param name="OriginalWordCount">Word count of the original document.</param>
/// <param name="SummaryWordCount">Word count of the generated summary.</param>
/// <param name="CompressionRatio">Compression ratio (original / summary words).</param>
/// <param name="WasChunked">Whether the document required chunking.</param>
/// <param name="Duration">Total time elapsed for the summarization operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummarizationCompletedEvent(
    SummarizationMode Mode,
    int OriginalWordCount,
    int SummaryWordCount,
    double CompressionRatio,
    bool WasChunked,
    TimeSpan Duration,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummarizationCompletedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="mode">The summarization mode that was executed.</param>
    /// <param name="originalWordCount">Word count of the original document.</param>
    /// <param name="summaryWordCount">Word count of the generated summary.</param>
    /// <param name="compressionRatio">Compression ratio (original / summary words).</param>
    /// <param name="wasChunked">Whether the document required chunking.</param>
    /// <param name="duration">Total time elapsed for the summarization operation.</param>
    /// <returns>A new <see cref="SummarizationCompletedEvent"/>.</returns>
    public static SummarizationCompletedEvent Create(
        SummarizationMode mode,
        int originalWordCount,
        int summaryWordCount,
        double compressionRatio,
        bool wasChunked,
        TimeSpan duration) =>
        new(mode, originalWordCount, summaryWordCount, compressionRatio, wasChunked, duration, DateTime.UtcNow);
}
