// =============================================================================
// File: IContextExpansionCache.cs
// Project: Lexichord.Abstractions
// Description: Interface for session-scoped context expansion caching.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Abstracts session-isolated caching for expanded chunk context. Each
//   session maintains its own cache to prevent data leakage between users.
//   Entries are invalidated when documents change or sessions end.
// =============================================================================

namespace Lexichord.Abstractions.Contracts.RAG;

/// <summary>
/// Provides session-scoped in-memory caching for context expansion results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IContextExpansionCache"/> caches <see cref="ExpandedChunk"/> objects
/// per session, keyed by chunk ID. This prevents repeated database queries for the
/// same chunk's surrounding context within a user session.
/// </para>
/// <para>
/// <b>Scope:</b> Each session has its own isolated cache space. Sessions do not
/// share cached data to prevent cross-session data leakage.
/// </para>
/// <para>
/// <b>Lifetime:</b> Entries persist for the duration of the session. There is no
/// TTL expiration; entries are only removed via explicit invalidation or session end.
/// </para>
/// <para>
/// <b>Thread Safety:</b> All operations are thread-safe via <c>ConcurrentDictionary</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public interface IContextExpansionCache
{
    /// <summary>
    /// Attempts to retrieve a cached expanded chunk for the specified session.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the user session.</param>
    /// <param name="chunkId">The ID of the chunk to retrieve context for.</param>
    /// <param name="result">
    /// When this method returns <c>true</c>, contains the cached expanded chunk.
    /// When this method returns <c>false</c>, contains <c>null</c>.
    /// </param>
    /// <returns>
    /// <c>true</c> if the expanded chunk was found in the session cache;
    /// otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// LOGIC: Performs a two-level lookup: first finds the session's cache, then
    /// looks up the chunk within that session. Returns false if either doesn't exist.
    /// </remarks>
    bool TryGet(string sessionId, Guid chunkId, out ExpandedChunk? result);

    /// <summary>
    /// Stores an expanded chunk in the session's cache.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the user session.</param>
    /// <param name="chunkId">The ID of the chunk.</param>
    /// <param name="result">The expanded chunk to cache.</param>
    /// <remarks>
    /// LOGIC: Creates the session's cache if it doesn't exist, then stores the entry.
    /// If an entry for the same chunk already exists, it is replaced.
    /// </remarks>
    void Set(string sessionId, Guid chunkId, ExpandedChunk result);

    /// <summary>
    /// Removes all cached entries for the specified session.
    /// </summary>
    /// <param name="sessionId">The unique identifier for the session to invalidate.</param>
    /// <remarks>
    /// LOGIC: Should be called when a session ends to free memory and ensure no
    /// stale data persists for future sessions with the same ID.
    /// </remarks>
    void InvalidateSession(string sessionId);

    /// <summary>
    /// Invalidates all cached entries across all sessions that reference the specified document.
    /// </summary>
    /// <param name="documentId">The ID of the document that was modified.</param>
    /// <remarks>
    /// LOGIC: Scans all sessions and removes entries where the core chunk belongs
    /// to the specified document. This ensures context data stays fresh.
    /// </remarks>
    void InvalidateForDocument(Guid documentId);

    /// <summary>
    /// Removes all entries from all sessions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Clears the entire cache, removing all session data.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Gets the count of active sessions with cached data.
    /// </summary>
    int ActiveSessionCount { get; }

    /// <summary>
    /// Gets the total number of cached entries across all sessions.
    /// </summary>
    int TotalEntryCount { get; }
}
