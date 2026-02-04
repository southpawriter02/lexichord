// =============================================================================
// File: ContextExpansionCacheService.cs
// Project: Lexichord.Modules.RAG
// Description: Session-scoped cache for context expansion results.
// Version: v0.5.8c
// =============================================================================
// LOGIC: Provides per-session caching of ExpandedChunk objects keyed by chunk ID.
//   - Session isolation prevents cross-user data leakage.
//   - No TTL (lives for session duration).
//   - ConcurrentDictionary for thread-safe access.
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Session-scoped cache for context expansion results.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ContextExpansionCacheService"/> stores <see cref="ExpandedChunk"/> objects
/// per user session, keyed by chunk ID. Each session maintains isolated cache space
/// to prevent data leakage between users.
/// </para>
/// <para>
/// <b>Session Isolation:</b> Sessions are identified by a string ID. Each session
/// has its own <see cref="ConcurrentDictionary{TKey,TValue}"/> for cache storage.
/// </para>
/// <para>
/// <b>Thread Safety:</b> Uses nested <see cref="ConcurrentDictionary{TKey,TValue}"/>
/// structures for lock-free concurrent access.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.8c as part of the Multi-Layer Caching System.
/// </para>
/// </remarks>
public sealed class ContextExpansionCacheService : IContextExpansionCache
{
    private readonly ContextCacheOptions _options;
    private readonly ILogger<ContextExpansionCacheService> _logger;

    // LOGIC: Outer dictionary keyed by session ID, inner by chunk ID
    private readonly ConcurrentDictionary<string, SessionCache> _sessions = new();

    /// <summary>
    /// Cache entry with expanded chunk and timestamp.
    /// </summary>
    private sealed record CacheEntry(ExpandedChunk Expanded, DateTimeOffset CachedAt);

    /// <summary>
    /// Per-session cache container.
    /// </summary>
    private sealed class SessionCache
    {
        public ConcurrentDictionary<Guid, CacheEntry> Entries { get; } = new();
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ContextExpansionCacheService"/>.
    /// </summary>
    /// <param name="options">Configuration options for the cache.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public ContextExpansionCacheService(
        IOptions<ContextCacheOptions> options,
        ILogger<ContextExpansionCacheService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        _logger.LogInformation(
            "ContextExpansionCacheService initialized: MaxEntriesPerSession={MaxEntries}, Enabled={Enabled}",
            _options.MaxEntriesPerSession, _options.Enabled);
    }

    /// <inheritdoc/>
    public int ActiveSessionCount => _sessions.Count;

    /// <inheritdoc/>
    public int TotalEntryCount => _sessions.Values.Sum(s => s.Entries.Count);

    /// <inheritdoc/>
    public bool TryGet(string sessionId, Guid chunkId, out ExpandedChunk? result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_options.Enabled)
        {
            result = null;
            return false;
        }

        if (!_sessions.TryGetValue(sessionId, out var sessionCache))
        {
            _logger.LogDebug("No session cache found for session {SessionId}", TruncateId(sessionId));
            result = null;
            return false;
        }

        if (!sessionCache.Entries.TryGetValue(chunkId, out var entry))
        {
            _logger.LogDebug("Cache miss for chunk {ChunkId} in session {SessionId}", chunkId, TruncateId(sessionId));
            result = null;
            return false;
        }

        _logger.LogDebug("Cache hit for chunk {ChunkId} in session {SessionId}", chunkId, TruncateId(sessionId));
        result = entry.Expanded;
        return true;
    }

    /// <inheritdoc/>
    public void Set(string sessionId, Guid chunkId, ExpandedChunk result)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(result);

        if (!_options.Enabled)
        {
            return;
        }

        var sessionCache = _sessions.GetOrAdd(sessionId, _ => new SessionCache());

        // LOGIC: Evict oldest if at capacity for this session
        while (sessionCache.Entries.Count >= _options.MaxEntriesPerSession)
        {
            var oldest = sessionCache.Entries
                .OrderBy(kvp => kvp.Value.CachedAt)
                .FirstOrDefault();

            if (oldest.Key != Guid.Empty)
            {
                sessionCache.Entries.TryRemove(oldest.Key, out _);
                _logger.LogDebug(
                    "Evicted oldest entry {ChunkId} from session {SessionId}",
                    oldest.Key, TruncateId(sessionId));
            }
            else
            {
                break;
            }
        }

        var entry = new CacheEntry(result, DateTimeOffset.UtcNow);
        sessionCache.Entries[chunkId] = entry;

        _logger.LogDebug(
            "Cached expanded chunk {ChunkId} for session {SessionId} (session has {Count} entries)",
            chunkId, TruncateId(sessionId), sessionCache.Entries.Count);
    }

    /// <inheritdoc/>
    public void InvalidateSession(string sessionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (_sessions.TryRemove(sessionId, out var sessionCache))
        {
            _logger.LogInformation(
                "Invalidated session {SessionId} ({Count} entries removed)",
                TruncateId(sessionId), sessionCache.Entries.Count);
        }
        else
        {
            _logger.LogDebug("No session cache to invalidate for session {SessionId}", TruncateId(sessionId));
        }
    }

    /// <inheritdoc/>
    public void InvalidateForDocument(Guid documentId)
    {
        var totalRemoved = 0;

        foreach (var kvp in _sessions)
        {
            var sessionId = kvp.Key;
            var sessionCache = kvp.Value;

            // LOGIC: Find and remove entries belonging to the document
            var keysToRemove = sessionCache.Entries
                .Where(e => e.Value.Expanded.Core.DocumentId == documentId)
                .Select(e => e.Key)
                .ToList();

            foreach (var chunkId in keysToRemove)
            {
                if (sessionCache.Entries.TryRemove(chunkId, out _))
                {
                    totalRemoved++;
                }
            }

            // LOGIC: Clean up empty sessions
            if (sessionCache.Entries.IsEmpty)
            {
                _sessions.TryRemove(sessionId, out _);
            }
        }

        if (totalRemoved > 0)
        {
            _logger.LogInformation(
                "Invalidated {Count} context expansion entries for document {DocumentId}",
                totalRemoved, documentId);
        }
        else
        {
            _logger.LogDebug("No context expansion entries to invalidate for document {DocumentId}", documentId);
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        var sessionCount = _sessions.Count;
        var entryCount = TotalEntryCount;

        _sessions.Clear();

        _logger.LogInformation(
            "Cleared context expansion cache ({SessionCount} sessions, {EntryCount} entries)",
            sessionCount, entryCount);
    }

    /// <summary>
    /// Truncates a session ID for logging.
    /// </summary>
    private static string TruncateId(string id) =>
        id.Length <= 8 ? id : id[..8] + "...";
}
