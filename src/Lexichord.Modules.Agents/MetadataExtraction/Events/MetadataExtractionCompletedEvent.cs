// -----------------------------------------------------------------------
// <copyright file="MetadataExtractionCompletedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a metadata extraction operation
//   completes successfully (v0.7.6b). Published by MetadataExtractor after
//   the LLM response is parsed and metadata is assembled.
//
//   Consumers:
//     - UI components (completion state, metrics display)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.MetadataExtraction;
using MediatR;

namespace Lexichord.Modules.Agents.MetadataExtraction.Events;

/// <summary>
/// Published when a metadata extraction operation completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// Published by the MetadataExtractor after the LLM response is parsed and metadata
/// is assembled. Carries completion metrics including document type, key term count,
/// complexity score, and total duration for observability.
/// </para>
/// <para><b>Introduced in:</b> v0.7.6b</para>
/// </remarks>
/// <param name="DocumentType">The detected document type.</param>
/// <param name="KeyTermCount">Number of key terms extracted.</param>
/// <param name="ConceptCount">Number of concepts identified.</param>
/// <param name="TagCount">Number of tags suggested.</param>
/// <param name="ComplexityScore">Document complexity score (1-10).</param>
/// <param name="ReadingTimeMinutes">Estimated reading time in minutes.</param>
/// <param name="WordCount">Word count of the analyzed document.</param>
/// <param name="Duration">Total time elapsed for the extraction operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record MetadataExtractionCompletedEvent(
    DocumentType DocumentType,
    int KeyTermCount,
    int ConceptCount,
    int TagCount,
    int ComplexityScore,
    int ReadingTimeMinutes,
    int WordCount,
    TimeSpan Duration,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="MetadataExtractionCompletedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="documentType">The detected document type.</param>
    /// <param name="keyTermCount">Number of key terms extracted.</param>
    /// <param name="conceptCount">Number of concepts identified.</param>
    /// <param name="tagCount">Number of tags suggested.</param>
    /// <param name="complexityScore">Document complexity score (1-10).</param>
    /// <param name="readingTimeMinutes">Estimated reading time in minutes.</param>
    /// <param name="wordCount">Word count of the analyzed document.</param>
    /// <param name="duration">Total time elapsed for the extraction operation.</param>
    /// <returns>A new <see cref="MetadataExtractionCompletedEvent"/>.</returns>
    public static MetadataExtractionCompletedEvent Create(
        DocumentType documentType,
        int keyTermCount,
        int conceptCount,
        int tagCount,
        int complexityScore,
        int readingTimeMinutes,
        int wordCount,
        TimeSpan duration) =>
        new(documentType, keyTermCount, conceptCount, tagCount, complexityScore, readingTimeMinutes, wordCount, duration, DateTime.UtcNow);
}
