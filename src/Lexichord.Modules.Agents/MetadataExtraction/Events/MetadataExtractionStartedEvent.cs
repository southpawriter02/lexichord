// -----------------------------------------------------------------------
// <copyright file="MetadataExtractionStartedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when the metadata extraction pipeline
//   begins execution (v0.7.6b). Published by MetadataExtractor after option
//   validation passes and before the LLM is invoked.
//
//   Consumers:
//     - UI components (progress state)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.MetadataExtraction.Events;

/// <summary>
/// Published when a metadata extraction pipeline begins execution.
/// </summary>
/// <remarks>
/// <para>
/// Published by the MetadataExtractor after option validation passes. Carries the
/// character count and document path for observability and UI state management.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
/// <param name="CharacterCount">Number of characters in the content to analyze.</param>
/// <param name="DocumentPath">Path to the document being analyzed, if available.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record MetadataExtractionStartedEvent(
    int CharacterCount,
    string? DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="MetadataExtractionStartedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="characterCount">Number of characters in the content to analyze.</param>
    /// <param name="documentPath">Path to the document being analyzed.</param>
    /// <returns>A new <see cref="MetadataExtractionStartedEvent"/>.</returns>
    public static MetadataExtractionStartedEvent Create(
        int characterCount,
        string? documentPath) =>
        new(characterCount, documentPath, DateTime.UtcNow);
}
