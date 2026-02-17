// -----------------------------------------------------------------------
// <copyright file="SummarizationStartedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when the summarization pipeline
//   begins execution (v0.7.6a). Published by SummarizerAgent after option
//   validation passes and before the LLM is invoked.
//
//   Consumers:
//     - UI components (progress state)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Summarizer;
using MediatR;

namespace Lexichord.Modules.Agents.Summarizer.Events;

/// <summary>
/// Published when a summarization pipeline begins execution.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummarizerAgent after option validation passes. Carries the
/// summarization mode, character count, and document path for observability
/// and UI state management.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6a</para>
/// </remarks>
/// <param name="Mode">The summarization mode being executed.</param>
/// <param name="CharacterCount">Number of characters in the content to summarize.</param>
/// <param name="DocumentPath">Path to the document being summarized, if available.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummarizationStartedEvent(
    SummarizationMode Mode,
    int CharacterCount,
    string? DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummarizationStartedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="mode">The summarization mode being executed.</param>
    /// <param name="characterCount">Number of characters in the content to summarize.</param>
    /// <param name="documentPath">Path to the document being summarized.</param>
    /// <returns>A new <see cref="SummarizationStartedEvent"/>.</returns>
    public static SummarizationStartedEvent Create(
        SummarizationMode mode,
        int characterCount,
        string? documentPath) =>
        new(mode, characterCount, documentPath, DateTime.UtcNow);
}
