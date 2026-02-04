// =============================================================================
// File: ContradictionResolution.cs
// Project: Lexichord.Abstractions
// Description: Record representing a resolution decision for a contradiction.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Captures the full resolution details when an admin resolves a
//   contradiction, including the resolution type, rationale, and any
//   synthesized content if applicable.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Represents the resolution decision for a detected contradiction.
/// </summary>
/// <remarks>
/// <para>
/// <b>Introduced in:</b> v0.5.9e as part of the Semantic Memory Deduplication feature.
/// </para>
/// <para>
/// This record encapsulates the admin's decision when resolving a contradiction:
/// </para>
/// <list type="bullet">
///   <item><description>The chosen resolution strategy.</description></item>
///   <item><description>The rationale for the decision.</description></item>
///   <item><description>Any synthesized content (for <see cref="ContradictionResolutionType.CreateSynthesis"/>).</description></item>
///   <item><description>Resulting canonical chunk ID(s) after resolution.</description></item>
/// </list>
/// <para>
/// Resolution records are immutable and serve as an audit trail for contradiction handling.
/// </para>
/// </remarks>
/// <param name="Type">
/// The resolution strategy applied to the contradiction.
/// See <see cref="ContradictionResolutionType"/> for options.
/// </param>
/// <param name="Rationale">
/// The admin's explanation for choosing this resolution.
/// Required for audit purposes and team knowledge sharing.
/// </param>
/// <param name="ResolvedAt">
/// UTC timestamp when the resolution was applied.
/// </param>
/// <param name="ResolvedBy">
/// Identity of the admin who applied this resolution.
/// </param>
/// <param name="RetainedChunkId">
/// The ID of the chunk retained as canonical after resolution.
/// Populated for <see cref="ContradictionResolutionType.KeepOlder"/> and
/// <see cref="ContradictionResolutionType.KeepNewer"/>.
/// Null for <see cref="ContradictionResolutionType.DeleteBoth"/>.
/// </param>
/// <param name="ArchivedChunkId">
/// The ID of the chunk archived or superseded after resolution.
/// Populated for <see cref="ContradictionResolutionType.KeepOlder"/> and
/// <see cref="ContradictionResolutionType.KeepNewer"/>.
/// Null for <see cref="ContradictionResolutionType.KeepBoth"/> and
/// <see cref="ContradictionResolutionType.DeleteBoth"/>.
/// </param>
/// <param name="SynthesizedContent">
/// New content created to replace both conflicting chunks.
/// Only populated for <see cref="ContradictionResolutionType.CreateSynthesis"/>.
/// </param>
/// <param name="SynthesizedChunkId">
/// The ID of the newly created synthesis chunk.
/// Only populated for <see cref="ContradictionResolutionType.CreateSynthesis"/>
/// after the synthesis chunk has been stored.
/// </param>
public record ContradictionResolution(
    ContradictionResolutionType Type,
    string Rationale,
    DateTimeOffset ResolvedAt,
    string ResolvedBy,
    Guid? RetainedChunkId = null,
    Guid? ArchivedChunkId = null,
    string? SynthesizedContent = null,
    Guid? SynthesizedChunkId = null)
{
    /// <summary>
    /// Gets whether this resolution involved keeping one chunk over another.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Type"/> is <see cref="ContradictionResolutionType.KeepOlder"/>
    /// or <see cref="ContradictionResolutionType.KeepNewer"/>.
    /// </value>
    public bool IsKeepOneSide =>
        Type is ContradictionResolutionType.KeepOlder
            or ContradictionResolutionType.KeepNewer;

    /// <summary>
    /// Gets whether this resolution created synthesized content.
    /// </summary>
    /// <value><c>true</c> if <see cref="Type"/> is <see cref="ContradictionResolutionType.CreateSynthesis"/>.</value>
    public bool IsSynthesis => Type == ContradictionResolutionType.CreateSynthesis;

    /// <summary>
    /// Gets whether this resolution deleted content.
    /// </summary>
    /// <value><c>true</c> if <see cref="Type"/> is <see cref="ContradictionResolutionType.DeleteBoth"/>.</value>
    public bool IsDestructive => Type == ContradictionResolutionType.DeleteBoth;

    /// <summary>
    /// Creates a resolution that keeps the older chunk.
    /// </summary>
    /// <param name="rationale">Explanation for the decision.</param>
    /// <param name="resolvedBy">Admin identity.</param>
    /// <param name="retainedChunkId">The older chunk to keep.</param>
    /// <param name="archivedChunkId">The newer chunk to archive.</param>
    /// <returns>A new <see cref="ContradictionResolution"/> for keeping older content.</returns>
    public static ContradictionResolution KeepOlder(
        string rationale,
        string resolvedBy,
        Guid retainedChunkId,
        Guid archivedChunkId)
    {
        return new ContradictionResolution(
            Type: ContradictionResolutionType.KeepOlder,
            Rationale: rationale,
            ResolvedAt: DateTimeOffset.UtcNow,
            ResolvedBy: resolvedBy,
            RetainedChunkId: retainedChunkId,
            ArchivedChunkId: archivedChunkId);
    }

    /// <summary>
    /// Creates a resolution that keeps the newer chunk.
    /// </summary>
    /// <param name="rationale">Explanation for the decision.</param>
    /// <param name="resolvedBy">Admin identity.</param>
    /// <param name="retainedChunkId">The newer chunk to keep.</param>
    /// <param name="archivedChunkId">The older chunk to archive.</param>
    /// <returns>A new <see cref="ContradictionResolution"/> for keeping newer content.</returns>
    public static ContradictionResolution KeepNewer(
        string rationale,
        string resolvedBy,
        Guid retainedChunkId,
        Guid archivedChunkId)
    {
        return new ContradictionResolution(
            Type: ContradictionResolutionType.KeepNewer,
            Rationale: rationale,
            ResolvedAt: DateTimeOffset.UtcNow,
            ResolvedBy: resolvedBy,
            RetainedChunkId: retainedChunkId,
            ArchivedChunkId: archivedChunkId);
    }

    /// <summary>
    /// Creates a resolution that keeps both chunks.
    /// </summary>
    /// <param name="rationale">Explanation for the decision.</param>
    /// <param name="resolvedBy">Admin identity.</param>
    /// <returns>A new <see cref="ContradictionResolution"/> for keeping both.</returns>
    public static ContradictionResolution KeepBoth(string rationale, string resolvedBy)
    {
        return new ContradictionResolution(
            Type: ContradictionResolutionType.KeepBoth,
            Rationale: rationale,
            ResolvedAt: DateTimeOffset.UtcNow,
            ResolvedBy: resolvedBy);
    }

    /// <summary>
    /// Creates a resolution that synthesizes new content from both chunks.
    /// </summary>
    /// <param name="rationale">Explanation for the decision.</param>
    /// <param name="resolvedBy">Admin identity.</param>
    /// <param name="synthesizedContent">The new combined content.</param>
    /// <returns>A new <see cref="ContradictionResolution"/> with synthesis.</returns>
    public static ContradictionResolution Synthesize(
        string rationale,
        string resolvedBy,
        string synthesizedContent)
    {
        return new ContradictionResolution(
            Type: ContradictionResolutionType.CreateSynthesis,
            Rationale: rationale,
            ResolvedAt: DateTimeOffset.UtcNow,
            ResolvedBy: resolvedBy,
            SynthesizedContent: synthesizedContent);
    }

    /// <summary>
    /// Creates a resolution that deletes both chunks.
    /// </summary>
    /// <param name="rationale">Explanation for the decision.</param>
    /// <param name="resolvedBy">Admin identity.</param>
    /// <returns>A new <see cref="ContradictionResolution"/> for deletion.</returns>
    public static ContradictionResolution DeleteBoth(string rationale, string resolvedBy)
    {
        return new ContradictionResolution(
            Type: ContradictionResolutionType.DeleteBoth,
            Rationale: rationale,
            ResolvedAt: DateTimeOffset.UtcNow,
            ResolvedBy: resolvedBy);
    }
}
