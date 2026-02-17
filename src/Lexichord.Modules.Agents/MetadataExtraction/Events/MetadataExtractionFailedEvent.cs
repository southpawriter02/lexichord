// -----------------------------------------------------------------------
// <copyright file="MetadataExtractionFailedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a metadata extraction operation
//   fails (v0.7.6b). Published by MetadataExtractor from catch blocks
//   when an exception prevents successful completion.
//
//   Failure sources:
//     - User cancellation (OperationCanceledException from user CT)
//     - Timeout (OperationCanceledException from timeout CTS)
//     - LLM errors (generic Exception)
//     - Validation errors (ArgumentException)
//     - JSON parsing errors (when LLM response is malformed)
//
//   Consumers:
//     - UI components (error display)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.MetadataExtraction.Events;

/// <summary>
/// Published when a metadata extraction operation fails.
/// </summary>
/// <remarks>
/// <para>
/// Published by the MetadataExtractor from catch blocks when an exception prevents
/// successful completion. Carries the error message and document path for
/// diagnostics and UI error display.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
/// <param name="ErrorMessage">A user-facing error message describing the failure.</param>
/// <param name="DocumentPath">Path to the document being analyzed, if available.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record MetadataExtractionFailedEvent(
    string ErrorMessage,
    string? DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="MetadataExtractionFailedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="errorMessage">A user-facing error message describing the failure.</param>
    /// <param name="documentPath">Path to the document being analyzed.</param>
    /// <returns>A new <see cref="MetadataExtractionFailedEvent"/>.</returns>
    public static MetadataExtractionFailedEvent Create(
        string errorMessage,
        string? documentPath) =>
        new(errorMessage, documentPath, DateTime.UtcNow);
}
