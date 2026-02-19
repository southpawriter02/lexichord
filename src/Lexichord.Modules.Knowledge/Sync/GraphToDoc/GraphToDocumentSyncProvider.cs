// =============================================================================
// File: GraphToDocumentSyncProvider.cs
// Project: Lexichord.Modules.Knowledge
// Description: Main provider for graph-to-document synchronization.
// =============================================================================
// LOGIC: GraphToDocumentSyncProvider orchestrates the graph-to-doc sync pipeline:
//   - Validates license tier before sync operations
//   - Detects affected documents via IAffectedDocumentDetector
//   - Creates flags via IDocumentFlagger
//   - Tracks subscriptions for targeted notifications
//   - Publishes GraphToDocSyncCompletedEvent via MediatR
//
// v0.7.6g: Graph-to-Doc Sync (CKVS Phase 4c)
// Dependencies: IGraphToDocumentSyncProvider, IAffectedDocumentDetector,
//               IDocumentFlagger, ILicenseContext (v0.0.4c), IMediator, ILogger<T>
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.GraphToDoc.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.GraphToDoc;

/// <summary>
/// Main provider for graph-to-document synchronization operations.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="IGraphToDocumentSyncProvider"/> to orchestrate the
/// graph-to-doc sync pipeline. This is the primary entry point for handling
/// graph changes and flagging affected documents.
/// </para>
/// <para>
/// <b>License Requirements:</b>
/// <list type="bullet">
///   <item>Core: No access (throws <see cref="UnauthorizedAccessException"/>).</item>
///   <item>WriterPro: No access (throws <see cref="UnauthorizedAccessException"/>).</item>
///   <item>Teams+: Full graph-to-doc sync with flagging and notifications.</item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6g as part of the Graph-to-Doc Sync module.
/// </para>
/// </remarks>
public sealed class GraphToDocumentSyncProvider : IGraphToDocumentSyncProvider
{
    private readonly IAffectedDocumentDetector _detector;
    private readonly IDocumentFlagger _flagger;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<GraphToDocumentSyncProvider> _logger;

    // LOGIC: In-memory subscription storage. In production, this would be persisted.
    private readonly ConcurrentDictionary<Guid, List<GraphChangeSubscription>> _subscriptionsByDocument = new();
    private readonly object _subscriptionLock = new();

    /// <summary>
    /// Initializes a new instance of <see cref="GraphToDocumentSyncProvider"/>.
    /// </summary>
    /// <param name="detector">The affected document detector.</param>
    /// <param name="flagger">The document flagger.</param>
    /// <param name="licenseContext">The license context for tier validation.</param>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="logger">The logger instance.</param>
    public GraphToDocumentSyncProvider(
        IAffectedDocumentDetector detector,
        IDocumentFlagger flagger,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<GraphToDocumentSyncProvider> logger)
    {
        _detector = detector;
        _flagger = flagger;
        _licenseContext = licenseContext;
        _mediator = mediator;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GraphToDocSyncResult> OnGraphChangeAsync(
        GraphChange change,
        GraphToDocSyncOptions? options = null,
        CancellationToken ct = default)
    {
        // LOGIC: Use default options if none provided.
        options ??= new GraphToDocSyncOptions();

        // LOGIC: Measure total operation duration for logging and result.
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation(
                "Starting graph-to-doc sync for entity {EntityId} ({ChangeType})",
                change.EntityId, change.ChangeType);

            // LOGIC: Validate license tier supports graph-to-doc sync.
            // Only Teams+ can use graph-to-doc sync.
            if (!CanPerformSync())
            {
                _logger.LogWarning(
                    "License tier {Tier} does not support graph-to-doc sync",
                    _licenseContext.Tier);
                throw new UnauthorizedAccessException(
                    $"License tier {_licenseContext.Tier} does not support graph-to-doc sync. " +
                    "Upgrade to Teams or higher to access this feature.");
            }

            // LOGIC: Step 1 - Detect affected documents.
            _logger.LogDebug(
                "Step 1: Detecting documents affected by entity {EntityId}",
                change.EntityId);

            var affectedDocuments = await _detector.DetectAsync(change, ct);

            // LOGIC: Limit affected documents if exceeding max.
            if (affectedDocuments.Count > options.MaxDocumentsPerChange)
            {
                _logger.LogWarning(
                    "Affected documents exceed maximum for entity {EntityId}: {Count} > {Max}",
                    change.EntityId, affectedDocuments.Count, options.MaxDocumentsPerChange);

                affectedDocuments = affectedDocuments
                    .Take(options.MaxDocumentsPerChange)
                    .ToList();
            }

            _logger.LogDebug(
                "Found {Count} affected documents for entity {EntityId}",
                affectedDocuments.Count, change.EntityId);

            // LOGIC: If no affected documents, return early with NoChanges.
            if (affectedDocuments.Count == 0)
            {
                stopwatch.Stop();
                return new GraphToDocSyncResult
                {
                    Status = SyncOperationStatus.NoChanges,
                    TriggeringChange = change,
                    Duration = stopwatch.Elapsed
                };
            }

            // LOGIC: Step 2 - Determine flag priority based on change type.
            var flagReason = GetFlagReasonFromChangeType(change.ChangeType);
            var priority = options.ReasonPriorities.TryGetValue(flagReason, out var p)
                ? p
                : FlagPriority.Medium;

            // LOGIC: Step 3 - Flag documents if enabled.
            var flagsCreated = new List<DocumentFlag>();
            var documentsNotified = 0;

            if (options.AutoFlagDocuments)
            {
                _logger.LogDebug(
                    "Step 3: Flagging {Count} documents",
                    affectedDocuments.Count);

                var flagOptions = new DocumentFlagOptions
                {
                    Priority = priority,
                    TriggeringEntityId = change.EntityId,
                    IncludeSuggestedActions = options.IncludeSuggestedActions,
                    SendNotification = options.SendNotifications
                };

                var documentIds = affectedDocuments.Select(d => d.DocumentId).ToList();
                flagsCreated = (await _flagger.FlagDocumentsAsync(
                    documentIds, flagReason, flagOptions, ct)).ToList();

                documentsNotified = options.SendNotifications ? flagsCreated.Count : 0;

                _logger.LogInformation(
                    "Created {Count} flags for entity {EntityId}",
                    flagsCreated.Count, change.EntityId);
            }

            stopwatch.Stop();

            // LOGIC: Build successful result.
            var result = new GraphToDocSyncResult
            {
                Status = flagsCreated.Count > 0
                    ? SyncOperationStatus.Success
                    : SyncOperationStatus.NoChanges,
                AffectedDocuments = affectedDocuments,
                FlagsCreated = flagsCreated,
                TotalDocumentsNotified = documentsNotified,
                TriggeringChange = change,
                Duration = stopwatch.Elapsed
            };

            // LOGIC: Publish completion event.
            await _mediator.Publish(
                GraphToDocSyncCompletedEvent.Create(change, result),
                ct);

            _logger.LogInformation(
                "Graph-to-doc sync completed for entity {EntityId} in {Duration}ms. " +
                "Affected: {AffectedCount}, Flags: {FlagCount}, Notified: {NotifiedCount}",
                change.EntityId,
                stopwatch.ElapsedMilliseconds,
                affectedDocuments.Count,
                flagsCreated.Count,
                documentsNotified);

            return result;
        }
        catch (OperationCanceledException)
        {
            // LOGIC: User cancelled the operation.
            _logger.LogInformation(
                "Graph-to-doc sync cancelled for entity {EntityId}",
                change.EntityId);
            throw;
        }
        catch (TimeoutException ex)
        {
            // LOGIC: Operation timed out.
            _logger.LogError(ex,
                "Graph-to-doc sync timed out for entity {EntityId} after {Timeout}",
                change.EntityId, options.Timeout);
            throw;
        }
        catch (UnauthorizedAccessException)
        {
            // LOGIC: License check failed. Re-throw without additional handling.
            throw;
        }
        catch (Exception ex)
        {
            // LOGIC: Unexpected error.
            _logger.LogError(ex,
                "Graph-to-doc sync failed for entity {EntityId}",
                change.EntityId);

            stopwatch.Stop();
            return new GraphToDocSyncResult
            {
                Status = SyncOperationStatus.Failed,
                TriggeringChange = change,
                Duration = stopwatch.Elapsed,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<AffectedDocument>> GetAffectedDocumentsAsync(
        Guid entityId,
        CancellationToken ct = default)
    {
        _logger.LogDebug(
            "Getting affected documents for entity {EntityId}",
            entityId);

        // LOGIC: Create a synthetic change to use the detector.
        var change = new GraphChange
        {
            EntityId = entityId,
            ChangeType = ChangeType.EntityUpdated,
            NewValue = string.Empty,
            ChangedAt = DateTimeOffset.UtcNow
        };

        return await _detector.DetectAsync(change, ct);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<DocumentFlag>> GetPendingFlagsAsync(
        Guid documentId,
        CancellationToken ct = default)
    {
        return _flagger.GetPendingFlagsAsync(documentId, ct);
    }

    /// <inheritdoc/>
    public Task<bool> ResolveFlagAsync(
        Guid flagId,
        FlagResolution resolution,
        CancellationToken ct = default)
    {
        return _flagger.ResolveFlagAsync(flagId, resolution, ct);
    }

    /// <inheritdoc/>
    public Task SubscribeToGraphChangesAsync(
        Guid documentId,
        GraphChangeSubscription subscription,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Subscribing document {DocumentId} to {EntityCount} entities",
            documentId, subscription.EntityIds.Count);

        // LOGIC: Get or create the subscription list for this document.
        var subscriptions = _subscriptionsByDocument.GetOrAdd(
            documentId,
            _ => new List<GraphChangeSubscription>());

        lock (_subscriptionLock)
        {
            subscriptions.Add(subscription);
        }

        _logger.LogDebug(
            "Document {DocumentId} now has {Count} subscriptions",
            documentId, subscriptions.Count);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks if the current license tier can perform graph-to-doc sync.
    /// </summary>
    /// <returns>True if sync is allowed, false otherwise.</returns>
    private bool CanPerformSync()
    {
        // LOGIC: License gating based on specification:
        // - Core, WriterPro: No graph-to-doc sync
        // - Teams+: Full access
        return _licenseContext.Tier switch
        {
            LicenseTier.Core => false,
            LicenseTier.WriterPro => false,
            LicenseTier.Teams => true,
            LicenseTier.Enterprise => true,
            _ => false
        };
    }

    /// <summary>
    /// Maps a change type to a flag reason.
    /// </summary>
    /// <param name="changeType">The graph change type.</param>
    /// <returns>The corresponding flag reason.</returns>
    private static FlagReason GetFlagReasonFromChangeType(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.EntityCreated => FlagReason.NewRelationship,
            ChangeType.EntityUpdated => FlagReason.EntityValueChanged,
            ChangeType.EntityDeleted => FlagReason.EntityDeleted,
            ChangeType.RelationshipCreated => FlagReason.NewRelationship,
            ChangeType.RelationshipDeleted => FlagReason.RelationshipRemoved,
            ChangeType.PropertyChanged => FlagReason.EntityPropertiesUpdated,
            _ => FlagReason.EntityValueChanged
        };
    }
}
