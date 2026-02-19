// =============================================================================
// File: SyncEventPublisher.cs
// Project: Lexichord.Modules.Knowledge
// Description: Implementation of ISyncEventPublisher for sync event publishing.
// =============================================================================
// LOGIC: SyncEventPublisher orchestrates sync event publishing via MediatR with
//   additional features: history storage, batching, deduplication, subscriptions,
//   and license gating. Thread-safe for concurrent event publishing.
//
// v0.7.6j: Sync Event Publisher (CKVS Phase 4c)
// Dependencies: IMediator, IEventStore (v0.7.6j), ILicenseContext (v0.0.4c)
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Knowledge.Sync.Events;

/// <summary>
/// Implementation of <see cref="ISyncEventPublisher"/> for sync event publishing.
/// </summary>
/// <remarks>
/// <para>
/// Orchestrates sync event publishing via MediatR with additional features:
/// </para>
/// <list type="bullet">
///   <item>Event history storage via <see cref="IEventStore"/>.</item>
///   <item>Batch publishing with deduplication.</item>
///   <item>Dynamic subscriptions with filtering.</item>
///   <item>License gating by tier.</item>
/// </list>
/// <para>
/// <b>License Requirements:</b>
/// </para>
/// <list type="bullet">
///   <item>Core: No access to event publishing.</item>
///   <item>WriterPro: Publish events, 7-day history.</item>
///   <item>Teams: Full access, 30-day history, subscriptions, batching.</item>
///   <item>Enterprise: Unlimited history, advanced features.</item>
/// </list>
/// <para>
/// <b>Thread Safety:</b> Uses ConcurrentDictionary for subscription storage.
/// Event publishing is thread-safe via MediatR.
/// </para>
/// <para>
/// <b>Error Handling:</b> Uses three-catch pattern (OperationCanceled, Timeout, Exception)
/// for robust error handling with configurable exception propagation.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6j as part of the Sync Event Publisher.
/// </para>
/// </remarks>
public sealed class SyncEventPublisher : ISyncEventPublisher
{
    private readonly IMediator _mediator;
    private readonly IEventStore _eventStore;
    private readonly ILicenseContext _licenseContext;
    private readonly ILogger<SyncEventPublisher> _logger;

    // LOGIC: Subscription storage keyed by event type, then by subscription ID.
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, SubscriptionInfo>> _subscriptions = new();

    // LOGIC: Track published event IDs for batch deduplication.
    private readonly ConcurrentDictionary<Guid, byte> _publishedEventIds = new();

    /// <summary>
    /// Initializes a new instance of <see cref="SyncEventPublisher"/>.
    /// </summary>
    /// <param name="mediator">The MediatR mediator for event publishing.</param>
    /// <param name="eventStore">The event store for history persistence.</param>
    /// <param name="licenseContext">The license context for tier checking.</param>
    /// <param name="logger">The logger instance.</param>
    public SyncEventPublisher(
        IMediator mediator,
        IEventStore eventStore,
        ILicenseContext licenseContext,
        ILogger<SyncEventPublisher> logger)
    {
        _mediator = mediator;
        _eventStore = eventStore;
        _licenseContext = licenseContext;
        _logger = logger;

        _logger.LogDebug("SyncEventPublisher initialized");
    }

    #region ISyncEventPublisher Implementation

    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(
        TEvent eventData,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        ct.ThrowIfCancellationRequested();

        options ??= SyncEventOptions.Default;

        // LOGIC: Check license tier for event publishing.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
        {
            _logger.LogWarning(
                "Event publishing blocked: {Feature} requires WriterPro or higher",
                FeatureCodes.SyncEventPublisher);
            throw new UnauthorizedAccessException(
                "Event publishing requires WriterPro or higher license tier.");
        }

        _logger.LogInformation(
            "Publishing sync event {EventType} (ID: {EventId}) for document {DocumentId}",
            typeof(TEvent).Name, eventData.EventId, eventData.DocumentId);

        var stopwatch = Stopwatch.StartNew();
        var handlersExecuted = 0;
        var handlersFailed = 0;
        var handlerErrors = new List<string>();

        try
        {
            // LOGIC: Publish via MediatR to registered handlers.
            await _mediator.Publish(eventData, ct);
            handlersExecuted = 1; // MediatR doesn't expose handler count
            stopwatch.Stop();

            _logger.LogDebug(
                "Published event {EventId} in {Duration}ms",
                eventData.EventId, stopwatch.ElapsedMilliseconds);

            // LOGIC: Invoke dynamic subscriptions.
            await InvokeSubscriptionsAsync(eventData, options, ct);
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Always rethrow cancellation.
            _logger.LogDebug(
                "Event publication cancelled for {EventId}",
                eventData.EventId);
            throw;
        }
        catch (TimeoutException ex)
        {
            _logger.LogWarning(ex,
                "Handler timeout for event {EventId}: {Message}",
                eventData.EventId, ex.Message);
            handlersFailed++;
            handlerErrors.Add($"Timeout: {ex.Message}");

            if (!options.CatchHandlerExceptions)
            {
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Handler failed for event {EventId}: {Message}",
                eventData.EventId, ex.Message);
            handlersFailed++;
            handlerErrors.Add($"{ex.GetType().Name}: {ex.Message}");

            if (!options.CatchHandlerExceptions)
            {
                throw;
            }
        }
        finally
        {
            stopwatch.Stop();

            // LOGIC: Store event in history if enabled.
            if (options.StoreInHistory)
            {
                await StoreEventRecordAsync(
                    eventData,
                    handlersExecuted,
                    handlersFailed,
                    stopwatch.Elapsed,
                    handlerErrors,
                    ct);
            }

            // LOGIC: Track event ID for deduplication.
            _publishedEventIds.TryAdd(eventData.EventId, 0);
        }
    }

    /// <inheritdoc/>
    public async Task PublishBatchAsync<TEvent>(
        IReadOnlyList<TEvent> events,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        ct.ThrowIfCancellationRequested();

        options ??= SyncEventOptions.Default;

        // LOGIC: Batch operations require Teams tier or higher.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
        {
            _logger.LogWarning(
                "Batch publishing blocked: requires WriterPro or higher");
            throw new UnauthorizedAccessException(
                "Batch event publishing requires WriterPro or higher license tier.");
        }

        // LOGIC: Check for Teams tier for batch operations.
        if (_licenseContext.Tier < LicenseTier.Teams)
        {
            _logger.LogWarning(
                "Batch publishing blocked: requires Teams tier, current tier: {Tier}",
                _licenseContext.Tier);
            throw new UnauthorizedAccessException(
                "Batch event publishing requires Teams or higher license tier.");
        }

        _logger.LogInformation(
            "Publishing batch of {Count} {EventType} events",
            events.Count, typeof(TEvent).Name);

        var publishedCount = 0;
        var skippedCount = 0;

        foreach (var eventData in events)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Skip duplicates based on EventId.
            if (_publishedEventIds.ContainsKey(eventData.EventId))
            {
                _logger.LogTrace(
                    "Skipping duplicate event {EventId}",
                    eventData.EventId);
                skippedCount++;
                continue;
            }

            await PublishAsync(eventData, options, ct);
            publishedCount++;
        }

        _logger.LogInformation(
            "Batch complete: published {Published}, skipped {Skipped} duplicates",
            publishedCount, skippedCount);
    }

    /// <inheritdoc/>
    public Task<Guid> SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        SyncEventSubscriptionOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        ct.ThrowIfCancellationRequested();

        // LOGIC: Subscriptions require Teams tier or higher.
        if (_licenseContext.Tier < LicenseTier.Teams)
        {
            _logger.LogWarning(
                "Subscription blocked: requires Teams tier, current tier: {Tier}",
                _licenseContext.Tier);
            throw new UnauthorizedAccessException(
                "Event subscriptions require Teams or higher license tier.");
        }

        var subscriptionId = Guid.NewGuid();
        var eventType = typeof(TEvent);

        options ??= new SyncEventSubscriptionOptions { EventType = eventType };

        var subscriptions = _subscriptions.GetOrAdd(
            eventType,
            _ => new ConcurrentDictionary<Guid, SubscriptionInfo>());

        var info = new SubscriptionInfo
        {
            SubscriptionId = subscriptionId,
            Handler = async (e, c) => await handler((TEvent)e, c),
            Options = options
        };

        subscriptions.TryAdd(subscriptionId, info);

        _logger.LogInformation(
            "Created subscription {SubscriptionId} for event type {EventType}",
            subscriptionId, eventType.Name);

        return Task.FromResult(subscriptionId);
    }

    /// <inheritdoc/>
    public Task<bool> UnsubscribeAsync<TEvent>(
        Guid subscriptionId,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        ct.ThrowIfCancellationRequested();

        var eventType = typeof(TEvent);

        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            var removed = subscriptions.TryRemove(subscriptionId, out _);

            if (removed)
            {
                _logger.LogInformation(
                    "Removed subscription {SubscriptionId} from event type {EventType}",
                    subscriptionId, eventType.Name);
            }
            else
            {
                _logger.LogDebug(
                    "Subscription {SubscriptionId} not found for event type {EventType}",
                    subscriptionId, eventType.Name);
            }

            return Task.FromResult(removed);
        }

        _logger.LogDebug(
            "No subscriptions found for event type {EventType}",
            eventType.Name);

        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<SyncEventRecord>> GetEventsAsync(
        SyncEventQuery query,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        // LOGIC: Event history queries require WriterPro or higher.
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.SyncEventPublisher))
        {
            _logger.LogWarning(
                "Event history query blocked: requires WriterPro or higher");
            throw new UnauthorizedAccessException(
                "Event history queries require WriterPro or higher license tier.");
        }

        // LOGIC: Apply retention limits based on license tier.
        var adjustedQuery = ApplyRetentionLimits(query);

        _logger.LogDebug(
            "Querying events with filters: EventType={EventType}, DocumentId={DocumentId}",
            query.EventType, query.DocumentId);

        return await _eventStore.QueryAsync(adjustedQuery, ct);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Stores an event record in the event store.
    /// </summary>
    private async Task StoreEventRecordAsync<TEvent>(
        TEvent eventData,
        int handlersExecuted,
        int handlersFailed,
        TimeSpan duration,
        List<string> handlerErrors,
        CancellationToken ct)
        where TEvent : ISyncEvent
    {
        try
        {
            var record = new SyncEventRecord
            {
                EventId = eventData.EventId,
                EventType = typeof(TEvent).Name,
                DocumentId = eventData.DocumentId,
                PublishedAt = eventData.PublishedAt,
                Payload = JsonSerializer.Serialize(eventData),
                HandlersExecuted = handlersExecuted,
                HandlersFailed = handlersFailed,
                TotalDuration = duration,
                AllHandlersSucceeded = handlersFailed == 0,
                HandlerErrors = handlerErrors
            };

            await _eventStore.StoreAsync(record, ct);

            _logger.LogTrace(
                "Stored event record {EventId} in history",
                eventData.EventId);
        }
        catch (Exception ex)
        {
            // LOGIC: Don't fail event publishing if history storage fails.
            _logger.LogWarning(ex,
                "Failed to store event {EventId} in history: {Message}",
                eventData.EventId, ex.Message);
        }
    }

    /// <summary>
    /// Invokes dynamic subscriptions for an event.
    /// </summary>
    private async Task InvokeSubscriptionsAsync<TEvent>(
        TEvent eventData,
        SyncEventOptions options,
        CancellationToken ct)
        where TEvent : ISyncEvent
    {
        var eventType = typeof(TEvent);

        if (!_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            return;
        }

        _logger.LogTrace(
            "Invoking {Count} subscriptions for event {EventId}",
            subscriptions.Count, eventData.EventId);

        foreach (var (subscriptionId, info) in subscriptions)
        {
            ct.ThrowIfCancellationRequested();

            // LOGIC: Skip inactive subscriptions.
            if (!info.Options.IsActive)
            {
                _logger.LogTrace(
                    "Skipping inactive subscription {SubscriptionId}",
                    subscriptionId);
                continue;
            }

            // LOGIC: Apply filter if present.
            if (info.Options.Filter is not null && !info.Options.Filter(eventData))
            {
                _logger.LogTrace(
                    "Event {EventId} filtered out by subscription {SubscriptionId}",
                    eventData.EventId, subscriptionId);
                continue;
            }

            try
            {
                await info.Handler(eventData, ct);

                _logger.LogTrace(
                    "Subscription {SubscriptionId} handled event {EventId}",
                    subscriptionId, eventData.EventId);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Subscription {SubscriptionId} failed for event {EventId}: {Message}",
                    subscriptionId, eventData.EventId, ex.Message);

                if (!options.CatchHandlerExceptions)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Applies retention limits to a query based on license tier.
    /// </summary>
    private SyncEventQuery ApplyRetentionLimits(SyncEventQuery query)
    {
        var retentionDays = _licenseContext.Tier switch
        {
            LicenseTier.WriterPro => 7,
            LicenseTier.Teams => 30,
            LicenseTier.Enterprise => int.MaxValue,
            _ => 0
        };

        if (retentionDays == int.MaxValue || retentionDays == 0)
        {
            return query;
        }

        var oldestAllowed = DateTimeOffset.UtcNow.AddDays(-retentionDays);

        // LOGIC: Ensure query doesn't request data older than retention allows.
        var effectiveAfter = query.PublishedAfter.HasValue
            ? (query.PublishedAfter.Value > oldestAllowed ? query.PublishedAfter.Value : oldestAllowed)
            : oldestAllowed;

        return query with { PublishedAfter = effectiveAfter };
    }

    #endregion

    #region Internal Types

    /// <summary>
    /// Internal record for storing subscription information.
    /// </summary>
    private sealed record SubscriptionInfo
    {
        public required Guid SubscriptionId { get; init; }
        public required Func<ISyncEvent, CancellationToken, Task> Handler { get; init; }
        public required SyncEventSubscriptionOptions Options { get; init; }
    }

    #endregion
}
