// =============================================================================
// File: ContradictionDetectedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a contradiction is detected.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Published by IContradictionService.FlagAsync() when a new contradiction
//   is recorded. Enables downstream consumers to:
//   - Display notifications in admin dashboard
//   - Trigger email/push notifications for high-confidence contradictions
//   - Update metrics and monitoring systems
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a new contradiction between chunks is detected and recorded.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This event is published by <see cref="IContradictionService.FlagAsync"/> after
/// successfully storing a new contradiction record.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Admin dashboard: Shows real-time contradiction count.</description></item>
///   <item><description>Notification service: Alerts admins of high-confidence contradictions.</description></item>
///   <item><description>Metrics/telemetry: Tracks contradiction detection rates.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="ContradictionId">
/// The unique identifier of the newly created contradiction record.
/// </param>
/// <param name="ChunkAId">
/// The identifier of the first conflicting chunk.
/// </param>
/// <param name="ChunkBId">
/// The identifier of the second conflicting chunk.
/// </param>
/// <param name="SimilarityScore">
/// The cosine similarity score between the conflicting chunks (0.0 to 1.0).
/// </param>
/// <param name="Confidence">
/// The confidence level that this is a genuine contradiction (0.0 to 1.0).
/// High confidence (>= 0.8) may trigger priority notifications.
/// </param>
/// <param name="Reason">
/// Optional explanation of why the contradiction was detected.
/// May contain LLM-generated rationale or rule-based reasoning.
/// </param>
/// <param name="ProjectId">
/// Optional project scope for multi-tenant filtering.
/// </param>
/// <param name="DetectedAt">
/// UTC timestamp when the contradiction was detected.
/// </param>
public record ContradictionDetectedEvent(
    Guid ContradictionId,
    Guid ChunkAId,
    Guid ChunkBId,
    float SimilarityScore,
    float Confidence,
    string? Reason,
    Guid? ProjectId,
    DateTimeOffset DetectedAt) : INotification
{
    /// <summary>
    /// Gets whether this contradiction has high confidence.
    /// </summary>
    /// <value><c>true</c> if <see cref="Confidence"/> is >= 0.8.</value>
    public bool IsHighConfidence => Confidence >= 0.8f;

    /// <summary>
    /// Creates an event from a <see cref="Contradiction"/> record.
    /// </summary>
    /// <param name="contradiction">The contradiction record.</param>
    /// <returns>A new <see cref="ContradictionDetectedEvent"/>.</returns>
    public static ContradictionDetectedEvent FromContradiction(Contradiction contradiction)
    {
        return new ContradictionDetectedEvent(
            ContradictionId: contradiction.Id,
            ChunkAId: contradiction.ChunkAId,
            ChunkBId: contradiction.ChunkBId,
            SimilarityScore: contradiction.SimilarityScore,
            Confidence: contradiction.ClassificationConfidence,
            Reason: contradiction.ContradictionReason,
            ProjectId: contradiction.ProjectId,
            DetectedAt: contradiction.DetectedAt);
    }
}
