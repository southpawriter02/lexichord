// =============================================================================
// File: DocumentFlagger.cs
// Project: Lexichord.Modules.Knowledge
// Description: Manages document flags for review workflows.
// =============================================================================
// LOGIC: DocumentFlagger creates and manages flags on documents that need
//   review due to graph changes. It handles flag creation, resolution,
//   and notification event publishing.
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: IDocumentFlagger, DocumentFlagStore, DocumentFlag, FlagReason,
//               FlagResolution, FlagStatus, DocumentFlagOptions,
//               DocumentFlaggedEvent, IMediator, ILogger<T>
// =============================================================================

using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Service for managing document flags in review workflows.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IDocumentFlagger"/> to create and manage document
/// flags. Used by <see cref="GraphToDocumentSyncProvider"/> to flag documents
/// affected by graph changes.
/// </para>
/// <para>
/// <b>Features:</b>
/// <list type="bullet">
///   <item>Single and batch flag creation.</item>
///   <item>Flag resolution with multiple resolution types.</item>
///   <item>Notification event publishing via MediatR.</item>
///   <item>Thread-safe flag storage via <see cref="DocumentFlagStore"/>.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public sealed class DocumentFlagger : IDocumentFlagger
{
    private readonly DocumentFlagStore _flagStore;
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentFlagger> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="DocumentFlagger"/>.
    /// </summary>
    /// <param name="flagStore">The flag storage service.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    public DocumentFlagger(
        DocumentFlagStore flagStore,
        IMediator mediator,
        ILogger<DocumentFlagger> logger)
    {
        _flagStore = flagStore;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<DocumentFlag> FlagDocumentAsync(
        Guid documentId,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Flagging document {DocumentId} with reason {Reason} and priority {Priority}",
            documentId, reason, options.Priority);

        // LOGIC: Create the flag with a new unique ID.
        var flag = new DocumentFlag
        {
            FlagId = Guid.NewGuid(),
            DocumentId = documentId,
            TriggeringEntityId = options.TriggeringEntityId,
            Reason = reason,
            Description = GetDescriptionForReason(reason),
            Priority = options.Priority,
            Status = FlagStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow,
            NotificationSent = options.SendNotification,
            NotificationSentAt = options.SendNotification ? DateTimeOffset.UtcNow : null
        };

        // LOGIC: Store the flag.
        await _flagStore.AddAsync(flag, ct);

        // LOGIC: Publish notification event if enabled.
        if (options.SendNotification)
        {
            _logger.LogDebug(
                "Publishing DocumentFlaggedEvent for flag {FlagId}",
                flag.FlagId);

            await _mediator.Publish(DocumentFlaggedEvent.Create(flag), ct);
        }

        _logger.LogInformation(
            "Flagged document {DocumentId} with flag {FlagId} ({Reason})",
            documentId, flag.FlagId, reason);

        return flag;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<DocumentFlag>> FlagDocumentsAsync(
        IReadOnlyList<Guid> documentIds,
        FlagReason reason,
        DocumentFlagOptions options,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Flagging {Count} documents with reason {Reason}",
            documentIds.Count, reason);

        var flags = new List<DocumentFlag>();

        foreach (var documentId in documentIds)
        {
            ct.ThrowIfCancellationRequested();

            var flag = await FlagDocumentAsync(documentId, reason, options, ct);
            flags.Add(flag);
        }

        _logger.LogInformation(
            "Created {Count} flags for {Reason}",
            flags.Count, reason);

        return flags;
    }

    /// <inheritdoc/>
    public async Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Resolving flag {FlagId} with resolution {Resolution}",
            flagId, resolution);

        // LOGIC: Get the existing flag.
        var flag = await _flagStore.GetAsync(flagId, ct);
        if (flag is null)
        {
            _logger.LogWarning(
                "Cannot resolve flag {FlagId} - not found",
                flagId);
            return false;
        }

        // LOGIC: Update the flag with resolution details.
        var resolvedFlag = flag with
        {
            Status = FlagStatus.Resolved,
            Resolution = resolution,
            ResolvedAt = DateTimeOffset.UtcNow
        };

        var updated = await _flagStore.UpdateAsync(resolvedFlag, ct);

        if (updated)
        {
            _logger.LogInformation(
                "Resolved flag {FlagId} for document {DocumentId} with {Resolution}",
                flagId, flag.DocumentId, resolution);
        }

        return updated;
    }

    /// <inheritdoc/>
    public async Task<int> ResolveFlagsAsync(
        IReadOnlyList<Guid> flagIds,
        FlagResolution resolution,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Resolving {Count} flags with resolution {Resolution}",
            flagIds.Count, resolution);

        var resolved = 0;

        foreach (var flagId in flagIds)
        {
            ct.ThrowIfCancellationRequested();

            if (await ResolveFlagAsync(flagId, resolution, ct))
            {
                resolved++;
            }
        }

        _logger.LogInformation(
            "Resolved {Count} of {Total} flags with {Resolution}",
            resolved, flagIds.Count, resolution);

        return resolved;
    }

    /// <inheritdoc/>
    public Task<DocumentFlag?> GetFlagAsync(
        Guid flagId,
        CancellationToken ct = default)
    {
        return _flagStore.GetAsync(flagId, ct);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DocumentFlag>> GetPendingFlagsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return _flagStore.GetPendingByDocumentAsync(documentId, ct);
    }

    /// <summary>
    /// Gets a human-readable description for a flag reason.
    /// </summary>
    /// <param name="reason">The flag reason.</param>
    /// <returns>A description string.</returns>
    private static string GetDescriptionForReason(FlagReason reason)
    {
        return reason switch
        {
            FlagReason.EntityValueChanged =>
                "A referenced entity's value has changed in the knowledge graph.",
            FlagReason.EntityPropertiesUpdated =>
                "A referenced entity's properties have been updated.",
            FlagReason.EntityDeleted =>
                "A referenced entity has been deleted from the knowledge graph.",
            FlagReason.NewRelationship =>
                "A new relationship involving a referenced entity has been created.",
            FlagReason.RelationshipRemoved =>
                "A relationship involving a referenced entity has been removed.",
            FlagReason.ManualSyncRequested =>
                "A manual synchronization review has been requested.",
            FlagReason.ConflictDetected =>
                "A conflict has been detected between document and graph state.",
            _ => "Document flagged for review."
        };
    }
}
