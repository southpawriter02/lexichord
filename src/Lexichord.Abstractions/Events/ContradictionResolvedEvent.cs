// =============================================================================
// File: ContradictionResolvedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when a contradiction is resolved.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Published by IContradictionService.ResolveAsync() and related methods
//   when a contradiction is resolved, dismissed, or auto-resolved. Enables:
//   - Dashboard updates showing resolution status
//   - Audit logging of resolution decisions
//   - Integration with downstream canonical management workflows
// =============================================================================

using Lexichord.Abstractions.Contracts.RAG;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a contradiction is resolved, dismissed, or auto-resolved.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This event is published by:
/// <list type="bullet">
///   <item><description><see cref="IContradictionService.ResolveAsync"/>: Admin resolution.</description></item>
///   <item><description><see cref="IContradictionService.DismissAsync"/>: False positive dismissal.</description></item>
///   <item><description><see cref="IContradictionService.AutoResolveAsync"/>: System-triggered resolution.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Audit logging: Records resolution decisions with rationale.</description></item>
///   <item><description>Metrics: Tracks resolution rates and types.</description></item>
///   <item><description>Canonical manager: May trigger archival or deletion workflows.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <param name="ContradictionId">
/// The unique identifier of the resolved contradiction.
/// </param>
/// <param name="ChunkAId">
/// The identifier of the first chunk that was in conflict.
/// </param>
/// <param name="ChunkBId">
/// The identifier of the second chunk that was in conflict.
/// </param>
/// <param name="FinalStatus">
/// The terminal status after resolution.
/// One of: <see cref="ContradictionStatus.Resolved"/>,
/// <see cref="ContradictionStatus.Dismissed"/>, or
/// <see cref="ContradictionStatus.AutoResolved"/>.
/// </param>
/// <param name="ResolutionType">
/// The resolution strategy applied, if admin-resolved.
/// Null for dismissed or auto-resolved contradictions.
/// </param>
/// <param name="Rationale">
/// The explanation for the resolution decision.
/// For dismissals, this is the false positive reason.
/// For auto-resolve, this is the system-generated reason.
/// </param>
/// <param name="ResolvedBy">
/// The identity of the resolver.
/// "System" for auto-resolved contradictions.
/// </param>
/// <param name="ResolvedAt">
/// UTC timestamp when the resolution occurred.
/// </param>
/// <param name="RetainedChunkId">
/// The chunk ID that was retained, if applicable.
/// Populated for KeepOlder/KeepNewer resolutions.
/// </param>
/// <param name="ArchivedChunkId">
/// The chunk ID that was archived, if applicable.
/// Populated for KeepOlder/KeepNewer resolutions.
/// </param>
/// <param name="SynthesizedChunkId">
/// The newly created synthesis chunk ID, if applicable.
/// Populated for CreateSynthesis resolutions.
/// </param>
public record ContradictionResolvedEvent(
    Guid ContradictionId,
    Guid ChunkAId,
    Guid ChunkBId,
    ContradictionStatus FinalStatus,
    ContradictionResolutionType? ResolutionType,
    string Rationale,
    string ResolvedBy,
    DateTimeOffset ResolvedAt,
    Guid? RetainedChunkId = null,
    Guid? ArchivedChunkId = null,
    Guid? SynthesizedChunkId = null) : INotification
{
    /// <summary>
    /// Gets whether this was an admin resolution (vs. auto/dismissed).
    /// </summary>
    /// <value><c>true</c> if <see cref="FinalStatus"/> is <see cref="ContradictionStatus.Resolved"/>.</value>
    public bool IsAdminResolution => FinalStatus == ContradictionStatus.Resolved;

    /// <summary>
    /// Gets whether this was a dismissal.
    /// </summary>
    /// <value><c>true</c> if <see cref="FinalStatus"/> is <see cref="ContradictionStatus.Dismissed"/>.</value>
    public bool IsDismissal => FinalStatus == ContradictionStatus.Dismissed;

    /// <summary>
    /// Gets whether this was system-auto-resolved.
    /// </summary>
    /// <value><c>true</c> if <see cref="FinalStatus"/> is <see cref="ContradictionStatus.AutoResolved"/>.</value>
    public bool IsAutoResolved => FinalStatus == ContradictionStatus.AutoResolved;

    /// <summary>
    /// Gets whether content was deleted by this resolution.
    /// </summary>
    /// <value><c>true</c> if <see cref="ResolutionType"/> is <see cref="ContradictionResolutionType.DeleteBoth"/>.</value>
    public bool IsDestructive => ResolutionType == ContradictionResolutionType.DeleteBoth;

    /// <summary>
    /// Creates an event for an admin resolution.
    /// </summary>
    /// <param name="contradiction">The resolved contradiction.</param>
    /// <returns>A new <see cref="ContradictionResolvedEvent"/>.</returns>
    public static ContradictionResolvedEvent FromContradiction(Contradiction contradiction)
    {
        ArgumentNullException.ThrowIfNull(contradiction);
        if (contradiction.Resolution is null)
            throw new ArgumentException("Contradiction must have a resolution.", nameof(contradiction));

        return new ContradictionResolvedEvent(
            ContradictionId: contradiction.Id,
            ChunkAId: contradiction.ChunkAId,
            ChunkBId: contradiction.ChunkBId,
            FinalStatus: contradiction.Status,
            ResolutionType: contradiction.Resolution.Type,
            Rationale: contradiction.Resolution.Rationale,
            ResolvedBy: contradiction.Resolution.ResolvedBy,
            ResolvedAt: contradiction.Resolution.ResolvedAt,
            RetainedChunkId: contradiction.Resolution.RetainedChunkId,
            ArchivedChunkId: contradiction.Resolution.ArchivedChunkId,
            SynthesizedChunkId: contradiction.Resolution.SynthesizedChunkId);
    }

    /// <summary>
    /// Creates an event for a dismissal.
    /// </summary>
    /// <param name="contradiction">The dismissed contradiction.</param>
    /// <param name="reason">The dismissal reason.</param>
    /// <param name="dismissedBy">The admin who dismissed.</param>
    /// <returns>A new <see cref="ContradictionResolvedEvent"/>.</returns>
    public static ContradictionResolvedEvent ForDismissal(
        Contradiction contradiction,
        string reason,
        string dismissedBy)
    {
        return new ContradictionResolvedEvent(
            ContradictionId: contradiction.Id,
            ChunkAId: contradiction.ChunkAId,
            ChunkBId: contradiction.ChunkBId,
            FinalStatus: ContradictionStatus.Dismissed,
            ResolutionType: null,
            Rationale: reason,
            ResolvedBy: dismissedBy,
            ResolvedAt: DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Creates an event for auto-resolution.
    /// </summary>
    /// <param name="contradiction">The auto-resolved contradiction.</param>
    /// <param name="reason">The system reason for auto-resolution.</param>
    /// <returns>A new <see cref="ContradictionResolvedEvent"/>.</returns>
    public static ContradictionResolvedEvent ForAutoResolve(
        Contradiction contradiction,
        string reason)
    {
        return new ContradictionResolvedEvent(
            ContradictionId: contradiction.Id,
            ChunkAId: contradiction.ChunkAId,
            ChunkBId: contradiction.ChunkBId,
            FinalStatus: ContradictionStatus.AutoResolved,
            ResolutionType: null,
            Rationale: reason,
            ResolvedBy: "System",
            ResolvedAt: DateTimeOffset.UtcNow);
    }
}
