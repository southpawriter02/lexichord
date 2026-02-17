// -----------------------------------------------------------------------
// <copyright file="SummarizationFailedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a summarization operation
//   fails (v0.7.6a). Published by SummarizerAgent from catch blocks
//   when an exception prevents successful completion.
//
//   Failure sources:
//     - User cancellation (OperationCanceledException from user CT)
//     - Timeout (OperationCanceledException from timeout CTS)
//     - LLM errors (generic Exception)
//     - Validation errors (ArgumentException)
//
//   Consumers:
//     - UI components (error display)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Summarizer;
using MediatR;

namespace Lexichord.Modules.Agents.Summarizer.Events;

/// <summary>
/// Published when a summarization operation fails.
/// </summary>
/// <remarks>
/// <para>
/// Published by the SummarizerAgent from catch blocks when an exception prevents
/// successful completion. Carries the error message and document path for
/// diagnostics and UI error display.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6a</para>
/// </remarks>
/// <param name="Mode">The summarization mode that was attempted.</param>
/// <param name="ErrorMessage">A user-facing error message describing the failure.</param>
/// <param name="DocumentPath">Path to the document being summarized, if available.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record SummarizationFailedEvent(
    SummarizationMode Mode,
    string ErrorMessage,
    string? DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="SummarizationFailedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="mode">The summarization mode that was attempted.</param>
    /// <param name="errorMessage">A user-facing error message describing the failure.</param>
    /// <param name="documentPath">Path to the document being summarized.</param>
    /// <returns>A new <see cref="SummarizationFailedEvent"/>.</returns>
    public static SummarizationFailedEvent Create(
        SummarizationMode mode,
        string errorMessage,
        string? documentPath) =>
        new(mode, errorMessage, documentPath, DateTime.UtcNow);
}
