# LDS-01: Communication Monitor UI

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Feature ID** | `CMB-UI-01` |
| **Feature Name** | Communication Monitor UI |
| **Target Version** | `v0.12.3f` |
| **Module Scope** | `Lexichord.Modules.Agents` |
| **Swimlane** | Ensemble |
| **License Tier** | WriterPro |
| **Feature Gate Key** | `FeatureFlags.Agents.Communication.Monitor` |
| **Author** | Agent Architecture Lead |
| **Reviewer** | Lead Architect |
| **Status** | Draft |
| **Last Updated** | 2026-02-03 |
| **Parent Spec** | [LCS-SBD-v0.12.3-AGT](./LCS-SBD-v0.12.3-AGT.md) |
| **Depends On** | [v0.12.3a (Message Bus Core)](./LCS-SBD-v0.12.3a-BUS.md), [v0.12.3b (Event System)](./LCS-SBD-v0.12.3b-EVT.md), [v0.12.3c (Request/Response)](./LCS-SBD-v0.12.3c-RRQ.md) |
| **Estimated Hours** | 6 |

---

## 2. Executive Summary

### 2.1 The Requirement

Debugging inter-agent communication is difficult without visibility into message flow. Developers need to trace messages, identify bottlenecks, and understand communication patterns. The system needs a real-time monitoring UI that visualizes message flow, displays statistics, and supports debugging.

### 2.2 The Proposed Solution

Implement a communication monitoring dashboard providing:
- Real-time message flow visualization
- Message trace history with filtering
- Correlation chain visualization
- Performance metrics (latency, throughput)
- Agent communication graph
- Live message inspection

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Modules.Agents` — `IAgentMessageBus`, message observation (v0.12.3a)
- `Lexichord.Modules.Agents` — Event subscriptions (v0.12.3b)
- `Lexichord.Modules.Agents` — `PendingRequest` tracking (v0.12.3c)
- `Lexichord.Host` — `ILicenseService`

**NuGet Packages:**
- `Avalonia.ReactiveUI` (MVVM)
- `ReactiveUI` (reactive bindings)
- `LiveChartsCore.SkiaSharpView.Avalonia` (charts)
- `DynamicData` (reactive collections)

### 3.2 Licensing Behavior

- **Load Behavior:** [x] **UI Gate** — Monitor tab visible but locked for Core users.
- **Fallback Experience:** Core users see "Upgrade to WriterPro" overlay. WriterPro gets basic monitoring. Teams+ get full features.

---

## 4. Data Contract (The API)

### 4.1 Message Trace Service

```csharp
namespace Lexichord.Modules.Agents;

/// <summary>
/// Service for collecting and querying message traces.
/// </summary>
public interface IMessageTraceService
{
    /// <summary>
    /// Gets recent message traces.
    /// </summary>
    /// <param name="filter">Optional filter criteria.</param>
    /// <param name="limit">Maximum traces to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of message traces.</returns>
    Task<IReadOnlyList<MessageTrace>> GetTracesAsync(
        TraceFilter? filter = null,
        int limit = 100,
        CancellationToken ct = default);

    /// <summary>
    /// Gets a correlation chain for a message.
    /// </summary>
    /// <param name="correlationId">The correlation ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>All messages in the correlation chain.</returns>
    Task<CorrelationChain> GetCorrelationChainAsync(
        MessageId correlationId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets real-time statistics.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Current statistics.</returns>
    Task<CommunicationStatistics> GetStatisticsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Gets agent communication graph data.
    /// </summary>
    /// <param name="since">Time window start.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Communication graph.</returns>
    Task<CommunicationGraph> GetCommunicationGraphAsync(
        DateTimeOffset? since = null,
        CancellationToken ct = default);

    /// <summary>
    /// Observes traces in real-time.
    /// </summary>
    /// <returns>Observable stream of traces.</returns>
    IObservable<MessageTrace> ObserveTraces();

    /// <summary>
    /// Clears trace history.
    /// </summary>
    /// <param name="before">Clear traces before this time.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ClearTracesAsync(
        DateTimeOffset? before = null,
        CancellationToken ct = default);
}
```

### 4.2 Message Trace Record

```csharp
namespace Lexichord.Modules.Agents.Abstractions;

/// <summary>
/// Trace record for a message.
/// </summary>
public sealed record MessageTrace
{
    /// <summary>
    /// Message ID.
    /// </summary>
    public required MessageId MessageId { get; init; }

    /// <summary>
    /// Correlation ID for chain tracking.
    /// </summary>
    public MessageId? CorrelationId { get; init; }

    /// <summary>
    /// Causation ID (parent message).
    /// </summary>
    public MessageId? CausationId { get; init; }

    /// <summary>
    /// Message type.
    /// </summary>
    public required MessageType Type { get; init; }

    /// <summary>
    /// Sender agent ID.
    /// </summary>
    public required AgentId SenderId { get; init; }

    /// <summary>
    /// Sender agent name (denormalized for display).
    /// </summary>
    public string? SenderName { get; init; }

    /// <summary>
    /// Target agent ID.
    /// </summary>
    public required AgentId TargetId { get; init; }

    /// <summary>
    /// Target agent name (denormalized for display).
    /// </summary>
    public string? TargetName { get; init; }

    /// <summary>
    /// Message priority.
    /// </summary>
    public MessagePriority Priority { get; init; }

    /// <summary>
    /// When message was sent.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Delivery latency (null if not yet delivered).
    /// </summary>
    public TimeSpan? Latency { get; init; }

    /// <summary>
    /// Delivery status.
    /// </summary>
    public TraceStatus Status { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Payload summary (truncated for display).
    /// </summary>
    public string? PayloadSummary { get; init; }

    /// <summary>
    /// Full payload (for detail view).
    /// </summary>
    public object? Payload { get; init; }

    /// <summary>
    /// Message headers.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}

/// <summary>
/// Trace delivery status.
/// </summary>
public enum TraceStatus
{
    Pending,
    Delivered,
    Failed,
    Expired,
    Timeout
}
```

### 4.3 Supporting Records

```csharp
namespace Lexichord.Modules.Agents.Abstractions;

/// <summary>
/// Filter for querying traces.
/// </summary>
public sealed record TraceFilter
{
    public MessageType? Type { get; init; }
    public AgentId? SenderId { get; init; }
    public AgentId? TargetId { get; init; }
    public TraceStatus? Status { get; init; }
    public DateTimeOffset? Since { get; init; }
    public DateTimeOffset? Until { get; init; }
    public string? SearchText { get; init; }
}

/// <summary>
/// Correlation chain of related messages.
/// </summary>
public sealed record CorrelationChain
{
    public required MessageId CorrelationId { get; init; }
    public required IReadOnlyList<MessageTrace> Messages { get; init; }
    public required DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public TimeSpan Duration => (CompletedAt ?? DateTimeOffset.UtcNow) - StartedAt;
}

/// <summary>
/// Real-time communication statistics.
/// </summary>
public sealed record CommunicationStatistics
{
    public long TotalMessages { get; init; }
    public long MessagesLastMinute { get; init; }
    public long MessagesLastHour { get; init; }
    public double AverageLatencyMs { get; init; }
    public double P95LatencyMs { get; init; }
    public int PendingRequests { get; init; }
    public int ActiveSubscriptions { get; init; }
    public int ActiveRoutes { get; init; }
    public IReadOnlyDictionary<MessageType, long> MessagesByType { get; init; } =
        new Dictionary<MessageType, long>();
    public IReadOnlyDictionary<TraceStatus, long> MessagesByStatus { get; init; } =
        new Dictionary<TraceStatus, long>();
}

/// <summary>
/// Agent communication graph for visualization.
/// </summary>
public sealed record CommunicationGraph
{
    public required IReadOnlyList<GraphNode> Nodes { get; init; }
    public required IReadOnlyList<GraphEdge> Edges { get; init; }
}

public sealed record GraphNode
{
    public required AgentId AgentId { get; init; }
    public required string AgentName { get; init; }
    public long MessagesSent { get; init; }
    public long MessagesReceived { get; init; }
}

public sealed record GraphEdge
{
    public required AgentId SourceId { get; init; }
    public required AgentId TargetId { get; init; }
    public long MessageCount { get; init; }
    public double AverageLatencyMs { get; init; }
}
```

---

## 5. Implementation Logic

### 5.1 ViewModel Architecture

```csharp
namespace Lexichord.Modules.Agents.ViewModels;

/// <summary>
/// Main ViewModel for the Communication Monitor dashboard.
/// </summary>
public sealed class CommunicationMonitorViewModel : ViewModelBase, IActivatableViewModel
{
    private readonly IMessageTraceService _traceService;
    private readonly ILicenseService _licenseService;
    private readonly SourceCache<MessageTrace, MessageId> _traces;

    [Reactive] public bool IsLicensed { get; private set; }
    [Reactive] public CommunicationStatistics? Statistics { get; private set; }
    [Reactive] public TraceFilter CurrentFilter { get; set; } = new();
    [Reactive] public MessageTrace? SelectedTrace { get; set; }
    [Reactive] public CorrelationChain? SelectedChain { get; set; }
    [Reactive] public bool IsLive { get; set; } = true;

    public ReadOnlyObservableCollection<MessageTrace> Traces { get; }

    public ReactiveCommand<Unit, Unit> RefreshCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearCommand { get; }
    public ReactiveCommand<MessageTrace, CorrelationChain> ViewChainCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleLiveCommand { get; }

    public ViewModelActivator Activator { get; } = new();

    public CommunicationMonitorViewModel(
        IMessageTraceService traceService,
        ILicenseService licenseService)
    {
        _traceService = traceService;
        _licenseService = licenseService;
        _traces = new SourceCache<MessageTrace, MessageId>(t => t.MessageId);

        // Bind traces to observable collection
        _traces.Connect()
            .Sort(SortExpressionComparer<MessageTrace>.Descending(t => t.Timestamp))
            .Bind(out var traces)
            .Subscribe();
        Traces = traces;

        // Setup commands
        RefreshCommand = ReactiveCommand.CreateFromTask(RefreshAsync);
        ClearCommand = ReactiveCommand.CreateFromTask(ClearAsync);
        ViewChainCommand = ReactiveCommand.CreateFromTask<MessageTrace, CorrelationChain>(
            trace => _traceService.GetCorrelationChainAsync(
                trace.CorrelationId ?? trace.MessageId));
        ToggleLiveCommand = ReactiveCommand.Create(() => IsLive = !IsLive);

        // Activation
        this.WhenActivated(disposables =>
        {
            // Check license
            IsLicensed = _licenseService.HasFeature(
                "FeatureFlags.Agents.Communication.Monitor");

            if (IsLicensed)
            {
                // Start live updates
                _traceService.ObserveTraces()
                    .Where(_ => IsLive)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(trace => _traces.AddOrUpdate(trace))
                    .DisposeWith(disposables);

                // Refresh statistics every 5 seconds
                Observable.Interval(TimeSpan.FromSeconds(5))
                    .SelectMany(_ => Observable.FromAsync(RefreshStatisticsAsync))
                    .Subscribe()
                    .DisposeWith(disposables);

                // Initial load
                Observable.FromAsync(RefreshAsync)
                    .Subscribe()
                    .DisposeWith(disposables);
            }
        });
    }

    private async Task RefreshAsync()
    {
        var traces = await _traceService.GetTracesAsync(CurrentFilter);
        _traces.Edit(cache =>
        {
            cache.Clear();
            cache.AddOrUpdate(traces);
        });
        await RefreshStatisticsAsync();
    }

    private async Task RefreshStatisticsAsync()
    {
        Statistics = await _traceService.GetStatisticsAsync();
    }

    private async Task ClearAsync()
    {
        await _traceService.ClearTracesAsync();
        _traces.Clear();
    }
}
```

### 5.2 UI Wireframe

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Communication Monitor                                           [WriterPro] │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Statistics                                                              │ │
│ │ ┌──────────────┐ ┌──────────────┐ ┌──────────────┐ ┌──────────────────┐ │ │
│ │ │ Messages/min │ │ Avg Latency  │ │   Pending    │ │  Subscriptions   │ │ │
│ │ │    1,234     │ │   12.5 ms    │ │      5       │ │       23         │ │ │
│ │ └──────────────┘ └──────────────┘ └──────────────┘ └──────────────────┘ │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Message Traces                    [🔴 Live] [Filter ▼] [Clear] [Export] │ │
│ ├─────────────────────────────────────────────────────────────────────────┤ │
│ │ Time       │ Type    │ From          │ To            │ Status │ Latency │ │
│ │──────────────────────────────────────────────────────────────────────── │ │
│ │ 10:23:45.1 │ Request │ Orchestrator  │ Worker-1      │ ✓      │ 15ms    │ │
│ │ 10:23:44.8 │ Event   │ Worker-2      │ (broadcast)   │ ✓      │ 8ms     │ │
│ │ 10:23:44.5 │ Message │ Supervisor    │ Worker-1      │ ✓      │ 5ms     │ │
│ │ 10:23:44.0 │ Request │ Worker-1      │ DataAgent     │ ⏱      │ -       │ │
│ │ 10:23:43.2 │ Event   │ System        │ (all)         │ ✗      │ -       │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐ │
│ │ Message Detail                                          [View Chain ▶]  │ │
│ ├─────────────────────────────────────────────────────────────────────────┤ │
│ │ ID: msg:a1b2c3d4e5f6...                                                 │ │
│ │ Correlation: msg:f6e5d4c3...                                            │ │
│ │ Type: Request                Priority: High                             │ │
│ │ From: Orchestrator (agent:1234...)                                      │ │
│ │ To: Worker-1 (agent:5678...)                                            │ │
│ │                                                                         │ │
│ │ Payload:                                                                │ │
│ │ ┌─────────────────────────────────────────────────────────────────────┐ │ │
│ │ │ {                                                                   │ │ │
│ │ │   "action": "process",                                              │ │ │
│ │ │   "data": { "id": 123, "type": "document" }                         │ │ │
│ │ │ }                                                                   │ │ │
│ │ └─────────────────────────────────────────────────────────────────────┘ │ │
│ └─────────────────────────────────────────────────────────────────────────┘ │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Correlation Chain View

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Correlation Chain: msg:f6e5d4c3...                              Duration: 45ms
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│   ┌─────────────┐     ┌─────────────┐     ┌─────────────┐                  │
│   │ Orchestrator│     │  Worker-1   │     │  DataAgent  │                  │
│   └──────┬──────┘     └──────┬──────┘     └──────┬──────┘                  │
│          │                   │                   │                          │
│          │──── Request ─────>│                   │     t=0ms               │
│          │     (15ms)        │                   │                          │
│          │                   │──── Request ─────>│     t=15ms              │
│          │                   │     (20ms)        │                          │
│          │                   │<─── Response ────│     t=35ms              │
│          │<─── Response ────│                   │     t=45ms              │
│          │                   │                   │                          │
│   ✓ Completed                                                              │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 6. Data Persistence (Database)

### 6.1 Migration (Enterprise Tier)

**Migration ID:** `Migration_20260203_003_CreateMessageTraces`

```csharp
[Migration(20260203003)]
public class CreateMessageTraces : Migration
{
    public override void Up()
    {
        Create.Table("message_traces")
            .InSchema("agents")
            .WithColumn("id").AsGuid().PrimaryKey()
            .WithColumn("correlation_id").AsGuid().Nullable().Indexed()
            .WithColumn("causation_id").AsGuid().Nullable()
            .WithColumn("message_type").AsInt32().NotNullable()
            .WithColumn("sender_id").AsGuid().NotNullable().Indexed()
            .WithColumn("sender_name").AsString(128).Nullable()
            .WithColumn("target_id").AsGuid().NotNullable().Indexed()
            .WithColumn("target_name").AsString(128).Nullable()
            .WithColumn("priority").AsInt32().NotNullable()
            .WithColumn("timestamp").AsDateTimeOffset().NotNullable().Indexed()
            .WithColumn("latency_ms").AsInt32().Nullable()
            .WithColumn("status").AsInt32().NotNullable()
            .WithColumn("error_message").AsString(1024).Nullable()
            .WithColumn("payload_summary").AsString(512).Nullable()
            .WithColumn("payload_json").AsString(int.MaxValue).Nullable()
            .WithColumn("headers_json").AsString(4096).Nullable();

        Create.Index("ix_traces_timestamp_status")
            .OnTable("message_traces").InSchema("agents")
            .OnColumn("timestamp").Descending()
            .OnColumn("status").Ascending();
    }

    public override void Down()
    {
        Delete.Table("message_traces").InSchema("agents");
    }
}
```

**Note:** Database persistence is Enterprise tier only. WriterPro/Teams use in-memory ring buffer (last 1000 traces).

---

## 7. Observability & Logging

### 7.1 Log Messages

| Level | Template |
|:------|:---------|
| Debug | `"Trace recorded for message {MessageId}"` |
| Info | `"Communication monitor opened by user {UserId}"` |
| Warning | `"Trace buffer full, oldest traces discarded"` |

### 7.2 Metrics

| Metric | Type | Description |
|:-------|:-----|:------------|
| `agents.monitor.traces_recorded` | Counter | Traces recorded |
| `agents.monitor.buffer_size` | Gauge | Current buffer size |
| `agents.monitor.ui_active` | Gauge | Monitor UI open |

---

## 8. Security & Safety

### 8.1 Payload Sanitization

- Sensitive headers (Authorization, API keys) are redacted
- Payload displayed only to users with appropriate permissions
- Enterprise tier allows payload persistence; others are in-memory only

---

## 9. Acceptance Criteria (QA)

1. **[Real-time]** New messages appear in trace list within 500ms.
2. **[Filtering]** Filters correctly narrow displayed traces.
3. **[Correlation]** Chain view shows complete message chain.
4. **[Statistics]** Statistics update every 5 seconds.
5. **[License]** Core users see upgrade prompt.

---

## 10. Test Scenarios

```gherkin
Scenario: Live trace updates
  Given monitor UI is open with Live mode enabled
  When a message is sent between agents
  Then the message appears in trace list within 500ms

Scenario: View correlation chain
  Given a request/response pair exists
  When "View Chain" is clicked on the request trace
  Then chain view shows both request and response
  And timeline shows correct ordering and latencies

Scenario: Filter by status
  Given traces with various statuses exist
  When filter is set to Status=Failed
  Then only failed traces are displayed
```

---

## 11. Changelog

| Version | Date | Author | Changes |
|:--------|:-----|:-------|:--------|
| 1.0.0 | 2026-02-03 | Agent Architecture Lead | Initial specification |

---

**End of Specification**
