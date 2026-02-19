// =============================================================================
// File: ConflictStore.cs
// Project: Lexichord.Modules.Knowledge
// Description: Thread-safe in-memory storage for conflict details.
// =============================================================================
// LOGIC: ConflictStore provides centralized storage for detected conflicts.
//   It uses ConcurrentDictionary for thread-safe access and supports
//   operations for adding, retrieving, updating, and removing conflicts.
//
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// Dependencies: ConflictDetail, ConflictResolutionResult (v0.7.6h),
//               SyncConflict (v0.7.6e)
// =============================================================================

using System.Collections.Concurrent;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Thread-safe in-memory storage for conflict details.
/// </summary>
/// <remarks>
/// <para>
/// Provides centralized conflict storage with:
/// </para>
/// <list type="bullet">
///   <item>Thread-safe access via ConcurrentDictionary.</item>
///   <item>Document-based conflict grouping.</item>
///   <item>Resolution history tracking.</item>
///   <item>Query operations for pending and resolved conflicts.</item>
/// </list>
/// <para>
/// <b>Storage Structure:</b>
/// <list type="bullet">
///   <item>Primary key: ConflictId (Guid)</item>
///   <item>Secondary index: DocumentId for grouped queries</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6h as part of the Conflict Resolver module.
/// </para>
/// </remarks>
public sealed class ConflictStore
{
    private readonly ILogger<ConflictStore> _logger;

    // LOGIC: Primary storage keyed by ConflictId.
    private readonly ConcurrentDictionary<Guid, StoredConflict> _conflicts = new();

    // LOGIC: Index for document-based lookups.
    private readonly ConcurrentDictionary<Guid, HashSet<Guid>> _conflictsByDocument = new();

    /// <summary>
    /// Initializes a new instance of <see cref="ConflictStore"/>.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    public ConflictStore(ILogger<ConflictStore> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds a conflict to the store.
    /// </summary>
    /// <param name="conflict">The conflict to store.</param>
    /// <param name="documentId">The document ID this conflict belongs to.</param>
    /// <returns>True if added successfully; false if already exists.</returns>
    public bool Add(SyncConflict conflict, Guid documentId)
    {
        var conflictId = Guid.NewGuid();
        var stored = new StoredConflict
        {
            ConflictId = conflictId,
            DocumentId = documentId,
            Conflict = conflict,
            StoredAt = DateTimeOffset.UtcNow,
            IsResolved = false
        };

        if (_conflicts.TryAdd(conflictId, stored))
        {
            // LOGIC: Update the document index.
            _conflictsByDocument.AddOrUpdate(
                documentId,
                _ => new HashSet<Guid> { conflictId },
                (_, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(conflictId);
                    }
                    return existing;
                });

            _logger.LogDebug(
                "Conflict added: {ConflictId} for document {DocumentId}",
                conflictId, documentId);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Adds a conflict detail to the store.
    /// </summary>
    /// <param name="detail">The conflict detail to store.</param>
    /// <param name="documentId">The document ID this conflict belongs to.</param>
    /// <returns>True if added successfully; false if already exists.</returns>
    public bool Add(ConflictDetail detail, Guid documentId)
    {
        var conflict = new SyncConflict
        {
            ConflictTarget = $"{detail.Entity.Type}:{detail.Entity.Name}.{detail.ConflictField}",
            DocumentValue = detail.DocumentValue,
            GraphValue = detail.GraphValue,
            DetectedAt = detail.DetectedAt,
            Type = detail.Type,
            Severity = detail.Severity,
            Description = $"Conflict in {detail.ConflictField}"
        };

        var stored = new StoredConflict
        {
            ConflictId = detail.ConflictId,
            DocumentId = documentId,
            Conflict = conflict,
            Detail = detail,
            StoredAt = DateTimeOffset.UtcNow,
            IsResolved = false
        };

        if (_conflicts.TryAdd(detail.ConflictId, stored))
        {
            // LOGIC: Update the document index.
            _conflictsByDocument.AddOrUpdate(
                documentId,
                _ => new HashSet<Guid> { detail.ConflictId },
                (_, existing) =>
                {
                    lock (existing)
                    {
                        existing.Add(detail.ConflictId);
                    }
                    return existing;
                });

            _logger.LogDebug(
                "ConflictDetail added: {ConflictId} for document {DocumentId}",
                detail.ConflictId, documentId);

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets a conflict by ID.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <returns>The stored conflict, or null if not found.</returns>
    public StoredConflict? Get(Guid conflictId)
    {
        return _conflicts.TryGetValue(conflictId, out var stored) ? stored : null;
    }

    /// <summary>
    /// Gets all unresolved conflicts for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>List of unresolved conflicts.</returns>
    public IReadOnlyList<SyncConflict> GetUnresolvedForDocument(Guid documentId)
    {
        if (!_conflictsByDocument.TryGetValue(documentId, out var conflictIds))
        {
            return Array.Empty<SyncConflict>();
        }

        var result = new List<SyncConflict>();

        lock (conflictIds)
        {
            foreach (var id in conflictIds)
            {
                if (_conflicts.TryGetValue(id, out var stored) && !stored.IsResolved)
                {
                    result.Add(stored.Conflict);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Gets all conflict details for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>List of conflict details.</returns>
    public IReadOnlyList<ConflictDetail> GetDetailsForDocument(Guid documentId)
    {
        if (!_conflictsByDocument.TryGetValue(documentId, out var conflictIds))
        {
            return Array.Empty<ConflictDetail>();
        }

        var result = new List<ConflictDetail>();

        lock (conflictIds)
        {
            foreach (var id in conflictIds)
            {
                if (_conflicts.TryGetValue(id, out var stored) && stored.Detail is not null)
                {
                    result.Add(stored.Detail);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Marks a conflict as resolved.
    /// </summary>
    /// <param name="conflictId">The conflict ID.</param>
    /// <param name="result">The resolution result.</param>
    /// <returns>True if conflict was found and updated; otherwise, false.</returns>
    public bool MarkResolved(Guid conflictId, ConflictResolutionResult result)
    {
        if (!_conflicts.TryGetValue(conflictId, out var stored))
        {
            return false;
        }

        var updated = stored with
        {
            IsResolved = true,
            Resolution = result,
            ResolvedAt = DateTimeOffset.UtcNow
        };

        if (_conflicts.TryUpdate(conflictId, updated, stored))
        {
            _logger.LogDebug(
                "Conflict resolved: {ConflictId}, Strategy: {Strategy}",
                conflictId, result.Strategy);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes all conflicts for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>Number of conflicts removed.</returns>
    public int RemoveForDocument(Guid documentId)
    {
        if (!_conflictsByDocument.TryRemove(documentId, out var conflictIds))
        {
            return 0;
        }

        var count = 0;
        lock (conflictIds)
        {
            foreach (var id in conflictIds)
            {
                if (_conflicts.TryRemove(id, out _))
                {
                    count++;
                }
            }
        }

        _logger.LogDebug(
            "Removed {Count} conflicts for document {DocumentId}",
            count, documentId);

        return count;
    }

    /// <summary>
    /// Gets the count of unresolved conflicts for a document.
    /// </summary>
    /// <param name="documentId">The document ID.</param>
    /// <returns>Count of unresolved conflicts.</returns>
    public int GetUnresolvedCount(Guid documentId)
    {
        if (!_conflictsByDocument.TryGetValue(documentId, out var conflictIds))
        {
            return 0;
        }

        var count = 0;
        lock (conflictIds)
        {
            foreach (var id in conflictIds)
            {
                if (_conflicts.TryGetValue(id, out var stored) && !stored.IsResolved)
                {
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// Gets the total count of stored conflicts.
    /// </summary>
    public int TotalCount => _conflicts.Count;

    /// <summary>
    /// Clears all conflicts from the store.
    /// </summary>
    public void Clear()
    {
        _conflicts.Clear();
        _conflictsByDocument.Clear();
        _logger.LogDebug("Conflict store cleared");
    }
}

/// <summary>
/// Internal record for storing conflicts with metadata.
/// </summary>
public record StoredConflict
{
    /// <summary>
    /// Unique identifier for this stored conflict.
    /// </summary>
    public required Guid ConflictId { get; init; }

    /// <summary>
    /// The document this conflict belongs to.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// The sync conflict data.
    /// </summary>
    public required SyncConflict Conflict { get; init; }

    /// <summary>
    /// Optional detailed conflict information.
    /// </summary>
    public ConflictDetail? Detail { get; init; }

    /// <summary>
    /// When the conflict was stored.
    /// </summary>
    public DateTimeOffset StoredAt { get; init; }

    /// <summary>
    /// Whether the conflict has been resolved.
    /// </summary>
    public bool IsResolved { get; init; }

    /// <summary>
    /// The resolution result, if resolved.
    /// </summary>
    public ConflictResolutionResult? Resolution { get; init; }

    /// <summary>
    /// When the conflict was resolved.
    /// </summary>
    public DateTimeOffset? ResolvedAt { get; init; }
}
