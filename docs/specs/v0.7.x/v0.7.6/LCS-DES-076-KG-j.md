# LCS-DES-076-KG-j: Sync Event Publisher

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-076-KG-j |
| **System Breakdown** | LCS-SBD-076-KG |
| **Version** | v0.7.6 |
| **Codename** | Sync Event Publisher (CKVS Phase 4c) |
| **Estimated Hours** | 3 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Sync Event Publisher** publishes synchronization events via MediatR, enabling loosely-coupled notification and handling of sync operations across the system. It provides event definitions for all major sync lifecycle events and supports filtering and batching of events.

### 1.2 Key Responsibilities

- Define sync event types for all sync operations
- Publish events via MediatR for handlers
- Support event filtering and routing
- Enable event batching and deduplication
- Maintain event audit trail
- Support event subscriptions
- Provide event metadata and context

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Sync/
      Events/
        ISyncEventPublisher.cs
        SyncEventPublisher.cs
        SyncEvents.cs
        SyncEventHandlers.cs
```

---

## 2. Interface Definitions

### 2.1 Sync Event Publisher

```csharp
namespace Lexichord.KnowledgeGraph.Sync.Events;

/// <summary>
/// Publishes sync events via MediatR.
/// </summary>
public interface ISyncEventPublisher
{
    /// <summary>
    /// Publishes a sync event.
    /// </summary>
    Task PublishAsync<TEvent>(
        TEvent eventData,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Publishes multiple sync events.
    /// </summary>
    Task PublishBatchAsync<TEvent>(
        IReadOnlyList<TEvent> events,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Subscribes to sync events.
    /// </summary>
    Task<Guid> SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        SyncEventSubscriptionOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Unsubscribes from sync events.
    /// </summary>
    Task<bool> UnsubscribeAsync<TEvent>(
        Guid subscriptionId,
        CancellationToken ct = default)
        where TEvent : ISyncEvent;

    /// <summary>
    /// Gets published events.
    /// </summary>
    Task<IReadOnlyList<SyncEventRecord>> GetEventsAsync(
        SyncEventQuery query,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Sync Event Interface

```csharp
/// <summary>
/// Base interface for all sync events.
/// </summary>
public interface ISyncEvent : INotification
{
    /// <summary>Unique event ID.</summary>
    Guid EventId { get; }

    /// <summary>Event timestamp.</summary>
    DateTimeOffset PublishedAt { get; }

    /// <summary>Document involved in event.</summary>
    Guid DocumentId { get; }

    /// <summary>Event metadata.</summary>
    IReadOnlyDictionary<string, object> Metadata { get; }
}
```

### 3.2 Sync Event Types

```csharp
/// <summary>
/// Published when document-to-graph sync completes.
/// </summary>
public record SyncCompletedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Result of the sync operation.</summary>
    public required SyncResult Result { get; init; }

    /// <summary>Direction of sync.</summary>
    public SyncDirection SyncDirection { get; init; } = SyncDirection.DocumentToGraph;

    /// <summary>Duration of the sync.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Entities affected by sync.</summary>
    public IReadOnlyList<Guid> EntityIds { get; init; } = [];
}

/// <summary>
/// Published when sync conflicts are detected.
/// </summary>
public record SyncConflictDetectedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Conflicts detected.</summary>
    public required IReadOnlyList<SyncConflict> Conflicts { get; init; }

    /// <summary>Number of conflicts.</summary>
    public int ConflictCount { get; init; }

    /// <summary>Suggested resolution strategy.</summary>
    public ConflictResolutionStrategy? SuggestedStrategy { get; init; }
}

/// <summary>
/// Published when sync conflicts are resolved.
/// </summary>
public record SyncConflictResolvedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Conflict that was resolved.</summary>
    public required SyncConflict Conflict { get; init; }

    /// <summary>Resolution strategy applied.</summary>
    public required ConflictResolutionStrategy Strategy { get; init; }

    /// <summary>Resolved value.</summary>
    public object? ResolvedValue { get; init; }

    /// <summary>User who resolved the conflict.</summary>
    public Guid? ResolvedBy { get; init; }
}

/// <summary>
/// Published when graph change is detected and documents are affected.
/// </summary>
public record GraphToDocumentSyncedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Graph change that triggered the sync.</summary>
    public required GraphChange TriggeringChange { get; init; }

    /// <summary>Documents affected by the change.</summary>
    public IReadOnlyList<AffectedDocument> AffectedDocuments { get; init; } = [];

    /// <summary>Flags created for documents.</summary>
    public IReadOnlyList<DocumentFlag> FlagsCreated { get; init; } = [];

    /// <summary>Total documents affected.</summary>
    public int TotalAffectedDocuments { get; init; }
}

/// <summary>
/// Published when a document is flagged for review.
/// </summary>
public record DocumentFlaggedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Flag that was created.</summary>
    public required DocumentFlag Flag { get; init; }

    /// <summary>Reason for the flag.</summary>
    public FlagReason FlagReason { get; init; }

    /// <summary>Priority of the flag.</summary>
    public FlagPriority FlagPriority { get; init; }

    /// <summary>Entity that triggered the flag.</summary>
    public Guid TriggeringEntityId { get; init; }
}

/// <summary>
/// Published when sync status changes.
/// </summary>
public record SyncStatusChangedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Previous sync state.</summary>
    public required SyncState PreviousState { get; init; }

    /// <summary>New sync state.</summary>
    public required SyncState NewState { get; init; }

    /// <summary>Reason for state change.</summary>
    public string? Reason { get; init; }

    /// <summary>Timestamp of state change.</summary>
    public DateTimeOffset ChangedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>User who initiated the change.</summary>
    public Guid? ChangedBy { get; init; }
}

/// <summary>
/// Published when sync operation fails.
/// </summary>
public record SyncFailedEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Error message.</summary>
    public required string ErrorMessage { get; init; }

    /// <summary>Error code.</summary>
    public string? ErrorCode { get; init; }

    /// <summary>Exception details.</summary>
    public string? ExceptionDetails { get; init; }

    /// <summary>Sync direction that failed.</summary>
    public SyncDirection FailedDirection { get; init; }

    /// <summary>Whether retry is recommended.</summary>
    public bool RetryRecommended { get; init; }
}

/// <summary>
/// Published when sync is retried after failure.
/// </summary>
public record SyncRetryEvent : ISyncEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset PublishedAt { get; init; } = DateTimeOffset.UtcNow;
    public required Guid DocumentId { get; init; }
    public IReadOnlyDictionary<string, object> Metadata { get; init; } =
        new Dictionary<string, object>();

    /// <summary>Attempt number.</summary>
    public int AttemptNumber { get; init; }

    /// <summary>Maximum retry attempts.</summary>
    public int MaxAttempts { get; init; }

    /// <summary>Delay before retry.</summary>
    public TimeSpan RetryDelay { get; init; }

    /// <summary>Original failure reason.</summary>
    public string? FailureReason { get; init; }
}
```

### 3.3 Sync Event Record

```csharp
/// <summary>
/// Historical record of a published event.
/// </summary>
public record SyncEventRecord
{
    /// <summary>Event ID.</summary>
    public required Guid EventId { get; init; }

    /// <summary>Event type name.</summary>
    public required string EventType { get; init; }

    /// <summary>Document ID.</summary>
    public required Guid DocumentId { get; init; }

    /// <summary>When event was published.</summary>
    public required DateTimeOffset PublishedAt { get; init; }

    /// <summary>Event payload (serialized).</summary>
    public required string Payload { get; init; }

    /// <summary>Number of handlers executed.</summary>
    public int HandlersExecuted { get; init; }

    /// <summary>Number of handlers failed.</summary>
    public int HandlersFailed { get; init; }

    /// <summary>Total time to handle event.</summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>Whether all handlers succeeded.</summary>
    public bool AllHandlersSucceeded { get; init; }

    /// <summary>Error details if any failed.</summary>
    public IReadOnlyList<string> HandlerErrors { get; init; } = [];
}
```

### 3.4 Event Options

```csharp
/// <summary>
/// Options for publishing sync events.
/// </summary>
public record SyncEventOptions
{
    /// <summary>Whether to store event in history.</summary>
    public bool StoreInHistory { get; init; } = true;

    /// <summary>Whether to await all handlers.</summary>
    public bool AwaitAll { get; init; } = true;

    /// <summary>Whether to catch handler exceptions.</summary>
    public bool CatchHandlerExceptions { get; init; } = true;

    /// <summary>Timeout for handler execution.</summary>
    public TimeSpan? HandlerTimeout { get; init; }

    /// <summary>Priority for event processing.</summary>
    public EventPriority Priority { get; init; } = EventPriority.Normal;

    /// <summary>Whether to batch with other events.</summary>
    public bool AllowBatching { get; init; } = false;

    /// <summary>Tags for categorizing events.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>Additional context data.</summary>
    public Dictionary<string, object> ContextData { get; init; } = new();
}

public record SyncEventSubscriptionOptions
{
    /// <summary>Event type to subscribe to.</summary>
    public required Type EventType { get; init; }

    /// <summary>Filter for events.</summary>
    public Func<ISyncEvent, bool>? Filter { get; init; }

    /// <summary>Whether subscription is active.</summary>
    public bool IsActive { get; init; } = true;

    /// <summary>Subscription created at.</summary>
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>Human-readable name.</summary>
    public string? Name { get; init; }
}

public record SyncEventQuery
{
    /// <summary>Filter by event type.</summary>
    public string? EventType { get; init; }

    /// <summary>Filter by document.</summary>
    public Guid? DocumentId { get; init; }

    /// <summary>Filter by published after date.</summary>
    public DateTimeOffset? PublishedAfter { get; init; }

    /// <summary>Filter by published before date.</summary>
    public DateTimeOffset? PublishedBefore { get; init; }

    /// <summary>Filter by successful events only.</summary>
    public bool? SuccessfulOnly { get; init; }

    /// <summary>Sort order.</summary>
    public EventSortOrder SortOrder { get; init; } = EventSortOrder.ByPublishedDescending;

    /// <summary>Maximum results.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Offset for pagination.</summary>
    public int PageOffset { get; init; } = 0;
}

public enum EventPriority
{
    Low,
    Normal,
    High,
    Critical
}

public enum EventSortOrder
{
    ByPublishedAscending,
    ByPublishedDescending,
    ByDocumentAscending,
    ByDocumentDescending,
    ByEventTypeAscending
}
```

---

## 4. Implementation

### 4.1 Sync Event Publisher

```csharp
public class SyncEventPublisher : ISyncEventPublisher
{
    private readonly IMediator _mediator;
    private readonly IEventStore _eventStore;
    private readonly ILogger<SyncEventPublisher> _logger;
    private readonly Dictionary<Type, List<Guid>> _subscriptions;

    public SyncEventPublisher(
        IMediator mediator,
        IEventStore eventStore,
        ILogger<SyncEventPublisher> logger)
    {
        _mediator = mediator;
        _eventStore = eventStore;
        _logger = logger;
        _subscriptions = new Dictionary<Type, List<Guid>>();
    }

    public async Task PublishAsync<TEvent>(
        TEvent eventData,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        options ??= new SyncEventOptions();

        _logger.LogInformation(
            "Publishing sync event {EventType} for document {DocumentId}",
            typeof(TEvent).Name, eventData.DocumentId);

        try
        {
            var stopwatch = Stopwatch.StartNew();

            // Publish via MediatR
            await _mediator.Publish(eventData, ct);

            stopwatch.Stop();

            // Store in history if enabled
            if (options.StoreInHistory)
            {
                var record = new SyncEventRecord
                {
                    EventId = eventData.EventId,
                    EventType = typeof(TEvent).Name,
                    DocumentId = eventData.DocumentId,
                    PublishedAt = eventData.PublishedAt,
                    Payload = System.Text.Json.JsonSerializer.Serialize(eventData),
                    HandlersExecuted = 1, // Simplified, would be actual count
                    HandlersFailed = 0,
                    TotalDuration = stopwatch.Elapsed,
                    AllHandlersSucceeded = true
                };

                await _eventStore.RecordEventAsync(record, ct);
            }

            _logger.LogDebug(
                "Published sync event {EventId} in {Duration}ms",
                eventData.EventId, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish sync event {EventType} for document {DocumentId}",
                typeof(TEvent).Name, eventData.DocumentId);

            throw;
        }
    }

    public async Task PublishBatchAsync<TEvent>(
        IReadOnlyList<TEvent> events,
        SyncEventOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        options ??= new SyncEventOptions();

        _logger.LogInformation(
            "Publishing batch of {Count} sync events",
            events.Count);

        foreach (var @event in events)
        {
            await PublishAsync(@event, options, ct);
        }
    }

    public async Task<Guid> SubscribeAsync<TEvent>(
        Func<TEvent, CancellationToken, Task> handler,
        SyncEventSubscriptionOptions? options = null,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        var subscriptionId = Guid.NewGuid();
        var eventType = typeof(TEvent);

        options ??= new SyncEventSubscriptionOptions { EventType = eventType };

        if (!_subscriptions.ContainsKey(eventType))
        {
            _subscriptions[eventType] = new List<Guid>();
        }

        _subscriptions[eventType].Add(subscriptionId);

        _logger.LogInformation(
            "Created subscription {SubscriptionId} for event type {EventType}",
            subscriptionId, eventType.Name);

        return subscriptionId;
    }

    public async Task<bool> UnsubscribeAsync<TEvent>(
        Guid subscriptionId,
        CancellationToken ct = default)
        where TEvent : ISyncEvent
    {
        var eventType = typeof(TEvent);

        if (_subscriptions.TryGetValue(eventType, out var subscriptions))
        {
            var removed = subscriptions.Remove(subscriptionId);

            if (removed)
            {
                _logger.LogInformation(
                    "Removed subscription {SubscriptionId} from event type {EventType}",
                    subscriptionId, eventType.Name);
            }

            return removed;
        }

        return false;
    }

    public async Task<IReadOnlyList<SyncEventRecord>> GetEventsAsync(
        SyncEventQuery query,
        CancellationToken ct = default)
    {
        return await _eventStore.QueryEventsAsync(query, ct);
    }
}

public interface IEventStore
{
    Task RecordEventAsync(SyncEventRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<SyncEventRecord>> QueryEventsAsync(SyncEventQuery query, CancellationToken ct = default);
}
```

### 4.2 Event Handler Examples

```csharp
/// <summary>
/// Handler for sync completed events.
/// </summary>
public class SyncCompletedEventHandler : INotificationHandler<SyncCompletedEvent>
{
    private readonly ISyncStatusTracker _statusTracker;
    private readonly ILogger<SyncCompletedEventHandler> _logger;

    public SyncCompletedEventHandler(
        ISyncStatusTracker statusTracker,
        ILogger<SyncCompletedEventHandler> logger)
    {
        _statusTracker = statusTracker;
        _logger = logger;
    }

    public async Task Handle(SyncCompletedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Handling sync completed event for document {DocumentId}",
            notification.DocumentId);

        // Update status
        var status = await _statusTracker.GetStatusAsync(notification.DocumentId, cancellationToken);

        var newState = notification.Result.Status == SyncOperationStatus.Success
            ? SyncState.InSync
            : SyncState.NeedsReview;

        await _statusTracker.UpdateStatusAsync(
            notification.DocumentId,
            status with { State = newState },
            cancellationToken);
    }
}

/// <summary>
/// Handler for sync failed events.
/// </summary>
public class SyncFailedEventHandler : INotificationHandler<SyncFailedEvent>
{
    private readonly ILogger<SyncFailedEventHandler> _logger;
    private readonly INotificationService _notificationService;

    public SyncFailedEventHandler(
        ILogger<SyncFailedEventHandler> logger,
        INotificationService notificationService)
    {
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task Handle(SyncFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogError(
            "Handling sync failed event for document {DocumentId}: {ErrorMessage}",
            notification.DocumentId, notification.ErrorMessage);

        // Notify user
        await _notificationService.NotifyUserAsync(
            notification.DocumentId,
            $"Sync failed: {notification.ErrorMessage}",
            NotificationSeverity.Error,
            cancellationToken);

        // Log for diagnostics
        _logger.LogError(
            "Sync failure details - DocumentId: {DocumentId}, Code: {Code}, Details: {Details}",
            notification.DocumentId, notification.ErrorCode, notification.ExceptionDetails);
    }
}

/// <summary>
/// Handler for conflict detected events.
/// </summary>
public class SyncConflictDetectedEventHandler : INotificationHandler<SyncConflictDetectedEvent>
{
    private readonly ILogger<SyncConflictDetectedEventHandler> _logger;
    private readonly IConflictNotificationService _conflictNotifier;

    public SyncConflictDetectedEventHandler(
        ILogger<SyncConflictDetectedEventHandler> logger,
        IConflictNotificationService conflictNotifier)
    {
        _logger = logger;
        _conflictNotifier = conflictNotifier;
    }

    public async Task Handle(SyncConflictDetectedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Handling {ConflictCount} sync conflicts for document {DocumentId}",
            notification.ConflictCount, notification.DocumentId);

        // Notify user about conflicts
        await _conflictNotifier.NotifyConflictsAsync(
            notification.DocumentId,
            notification.Conflicts,
            notification.SuggestedStrategy,
            cancellationToken);
    }
}
```

---

## 5. Algorithm / Flow

### Event Publishing Flow

```
1. Create event instance with all required data
2. Call PublishAsync with event and options
3. Publish via MediatR to registered handlers
4. If StoreInHistory enabled:
   - Create SyncEventRecord
   - Serialize event payload
   - Store in event store
5. Log event publication
6. Return to caller
```

### Event Handler Execution Flow

```
1. Event published via MediatR
2. MediatR invokes all registered handlers
3. Each handler processes event asynchronously
4. If handler throws:
   - If CatchHandlerExceptions: log and continue
   - Otherwise: propagate exception
5. Record handler execution status
6. Return completion to publisher
```

---

## 6. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Handler throws exception | Log if CatchHandlerExceptions enabled |
| Event store unavailable | Log warning, continue publishing |
| Handler timeout | Cancel handler, log timeout |
| Serialization fails | Log error, skip history recording |

---

## 7. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `PublishAsync_InvokesHandlers` | Handlers invoked correctly |
| `PublishAsync_StoresInHistory` | Event stored in history |
| `PublishBatchAsync_PublishesBatch` | Batch publishing works |
| `Subscribe_CreatesSubscription` | Subscription created |
| `Unsubscribe_RemovesSubscription` | Subscription removed |
| `GetEvents_ReturnsRecords` | History query works |
| `ConflictDetected_NotifiesUser` | Conflict notification sent |

---

## 8. Performance

| Aspect | Target |
| :----- | :----- |
| Event publication | < 50ms |
| Handler invocation | < 200ms per handler |
| Batch publication | < 1s per 100 events |
| Event store write | < 100ms |
| History query | < 500ms |

---

## 9. License Gating

| Tier | Event Publication | History | Handlers | Subscriptions |
| :--- | :--- | :--- | :--- | :--- |
| Core | No | N/A | N/A | N/A |
| WriterPro | Yes | 7 days | Manual | Manual |
| Teams | Yes | 30 days | All | All |
| Enterprise | Yes | Unlimited | All + custom | Unlimited |

---

## 10. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
