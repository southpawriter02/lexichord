// =============================================================================
// File: DocumentFlagStore.cs
// Project: Lexichord.Modules.Knowledge
// Description: In-memory storage for document flags.
// =============================================================================
// LOGIC: DocumentFlagStore maintains document flags in memory with thread-safe
//   access. It enables flag creation, retrieval, and resolution tracking.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: DocumentFlag, FlagStatus (v0.7.6g)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Service for storing and retrieving document flags.
/// </summary>
/// <remarks>
/// <para>
/// Maintains document flags in memory to enable:
/// </para>
/// <list type="bullet">
///   <item>Flag creation and storage.</item>
///   <item>Retrieval by flag ID or document ID.</item>
///   <item>Pending flag queries for review workflows.</item>
///   <item>Flag updates and resolution tracking.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> This service is thread-safe and can be used from
/// multiple concurrent sync operations.
/// </para>
/// <para>
/// <b>Persistence:</b> Currently in-memory only. Flags are lost on application
/// restart. Future versions may add persistence via PostgreSQL.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public sealed class DocumentFlagStore
{
    // LOGIC: ConcurrentDictionary for thread-safe flag storage.
    // Primary index: FlagId -> DocumentFlag
    private readonly ConcurrentDictionary<Guid, DocumentFlag> _flagsByFlagId = new();

    // LOGIC: Secondary index: DocumentId -> List of FlagIds
    // Enables efficient lookup of flags by document.
    private readonly ConcurrentDictionary<Guid, List<Guid>> _flagIdsByDocumentId = new();

    private readonly ILogger<DocumentFlagStore> _logger;

    // LOGIC: Lock object for thread-safe list modifications.
    private readonly object _listLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentFlagStore"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public DocumentFlagStore(ILogger<DocumentFlagStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a flag to the store.
    /// </summary>
    /// <param name="flag">The flag to store.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: Adds the flag to both the primary (by FlagId) and secondary
    /// (by DocumentId) indices.
    /// </remarks>
    public Task AddAsync(DocumentFlag flag, CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Adding flag {FlagId} for document {DocumentId}",
            flag.FlagId, flag.DocumentId);

        // LOGIC: Add to primary index.
        _flagsByFlagId[flag.FlagId] = flag;

        // LOGIC: Add to secondary index (document -> flags).
        var documentFlags = _flagIdsByDocumentId.GetOrAdd(
            flag.DocumentId,
            _ => new List<Guid>());

        lock (_listLock)
        {
            if (!documentFlags.Contains(flag.FlagId))
            {
                documentFlags.Add(flag.FlagId);
            }
        }

        _logger.LogDebug(
            "Added flag {FlagId}. Document {DocumentId} now has {Count} flags",
            flag.FlagId, flag.DocumentId, documentFlags.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a flag by its ID.
    /// </summary>
    /// <param name="flagId">The flag ID to retrieve.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The flag, or null if not found.</returns>
    public Task<DocumentFlag?> GetAsync(Guid flagId, CancellationToken ct = default)
    {
        _flagsByFlagId.TryGetValue(flagId, out var flag);
        return Task.FromResult(flag);
    }

    /// <summary>
    /// Gets all flags for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All flags for the document, in creation order.</returns>
    public Task<IReadOnlyList<DocumentFlag>> GetByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        if (!_flagIdsByDocumentId.TryGetValue(documentId, out var flagIds))
        {
            return Task.FromResult<IReadOnlyList<DocumentFlag>>([]);
        }

        lock (_listLock)
        {
            var flags = flagIds
                .Select(id => _flagsByFlagId.TryGetValue(id, out var f) ? f : null)
                .Where(f => f is not null)
                .Cast<DocumentFlag>()
                .OrderBy(f => f.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<DocumentFlag>>(flags);
        }
    }

    /// <summary>
    /// Gets pending flags for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Pending and acknowledged flags, ordered by priority (highest first)
    /// then creation date.
    /// </returns>
    public Task<IReadOnlyList<DocumentFlag>> GetPendingByDocumentAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        if (!_flagIdsByDocumentId.TryGetValue(documentId, out var flagIds))
        {
            return Task.FromResult<IReadOnlyList<DocumentFlag>>([]);
        }

        lock (_listLock)
        {
            var flags = flagIds
                .Select(id => _flagsByFlagId.TryGetValue(id, out var f) ? f : null)
                .Where(f => f is not null)
                .Cast<DocumentFlag>()
                .Where(f => f.Status == FlagStatus.Pending || f.Status == FlagStatus.Acknowledged)
                .OrderByDescending(f => f.Priority)
                .ThenBy(f => f.CreatedAt)
                .ToList();

            return Task.FromResult<IReadOnlyList<DocumentFlag>>(flags);
        }
    }

    /// <summary>
    /// Updates a flag in the store.
    /// </summary>
    /// <param name="flag">The updated flag.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the flag was updated, false if not found.</returns>
    public Task<bool> UpdateAsync(DocumentFlag flag, CancellationToken ct = default)
    {
        if (!_flagsByFlagId.ContainsKey(flag.FlagId))
        {
            _logger.LogWarning(
                "Cannot update flag {FlagId} - not found",
                flag.FlagId);
            return Task.FromResult(false);
        }

        _flagsByFlagId[flag.FlagId] = flag;

        _logger.LogDebug(
            "Updated flag {FlagId} to status {Status}",
            flag.FlagId, flag.Status);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Removes a flag from the store.
    /// </summary>
    /// <param name="flagId">The flag ID to remove.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the flag was removed, false if not found.</returns>
    public Task<bool> RemoveAsync(Guid flagId, CancellationToken ct = default)
    {
        if (!_flagsByFlagId.TryRemove(flagId, out var flag))
        {
            return Task.FromResult(false);
        }

        // LOGIC: Remove from secondary index.
        if (_flagIdsByDocumentId.TryGetValue(flag.DocumentId, out var flagIds))
        {
            lock (_listLock)
            {
                flagIds.Remove(flagId);
            }
        }

        _logger.LogDebug(
            "Removed flag {FlagId} from document {DocumentId}",
            flagId, flag.DocumentId);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets the count of pending flags for a document.
    /// </summary>
    /// <param name="documentId">The document ID to query.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The count of pending flags.</returns>
    public Task<int> GetPendingCountAsync(Guid documentId, CancellationToken ct = default)
    {
        if (!_flagIdsByDocumentId.TryGetValue(documentId, out var flagIds))
        {
            return Task.FromResult(0);
        }

        lock (_listLock)
        {
            var count = flagIds
                .Select(id => _flagsByFlagId.TryGetValue(id, out var f) ? f : null)
                .Count(f => f is not null &&
                           (f.Status == FlagStatus.Pending || f.Status == FlagStatus.Acknowledged));

            return Task.FromResult(count);
        }
    }
}
