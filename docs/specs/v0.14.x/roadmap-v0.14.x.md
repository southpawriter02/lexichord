# Lexichord Agent Studio Roadmap (v0.14.1 - v0.14.5)

In v0.13.x, we delivered **The Orchestration Engine** — intelligent task decomposition, agent selection, coordinated execution, and result synthesis. In v0.14.x, we build **The Agent Studio** — a visual environment for designing, debugging, and monitoring agent systems, transforming users from orchestration consumers to orchestration composers.

**Architectural Note:** This version introduces the `Lexichord.Studio` module, implementing the "Agent IDE for Knowledge Workers" vision. The Studio provides visual programming capabilities while maintaining the power of the underlying orchestration engine.

**Total Sub-Parts:** 36 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 264 hours (~6.6 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.14.1-STU | Visual Workflow Canvas | Drag-drop graph editor, node library | 54 |
| v0.14.2-STU | Agent Debugger | Step execution, breakpoints, inspection | 56 |
| v0.14.3-STU | Live Monitoring Dashboard | Real-time visualization, metrics | 52 |
| v0.14.4-STU | Prompt Engineering Studio | Prompt editor, testing, versioning | 50 |
| v0.14.5-STU | Simulation & Replay | Dry runs, execution replay, what-if | 52 |

---

## Agent Studio Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Agent Studio                                       │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                    Visual Workflow Canvas (v0.14.1)                     │ │
│  │  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐             │ │
│  │  │Research │───▶│ Write   │───▶│ Review  │───▶│ Publish │             │ │
│  │  │  Node   │    │  Node   │    │  Node   │    │  Node   │             │ │
│  │  └─────────┘    └─────────┘    └─────────┘    └─────────┘             │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌──────────────────────┐  ┌──────────────────────┐  ┌─────────────────┐   │
│  │   Agent Debugger     │  │   Live Monitor       │  │ Prompt Studio   │   │
│  │     (v0.14.2)        │  │     (v0.14.3)        │  │   (v0.14.4)     │   │
│  │                      │  │                      │  │                 │   │
│  │ • Breakpoints        │  │ • Real-time graph    │  │ • Prompt editor │   │
│  │ • Step execution     │  │ • Metrics dashboard  │  │ • A/B testing   │   │
│  │ • State inspection   │  │ • Resource monitor   │  │ • Version ctrl  │   │
│  └──────────────────────┘  └──────────────────────┘  └─────────────────┘   │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                   Simulation & Replay (v0.14.5)                         │ │
│  │  • Dry run mode  • Execution replay  • What-if scenarios               │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.14.1-STU: Visual Workflow Canvas

**Goal:** Provide a drag-and-drop visual editor for designing agent workflows, with a comprehensive node library and real-time validation.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.14.1e | Canvas Infrastructure | 12 |
| v0.14.1f | Node System | 10 |
| v0.14.1g | Connection Manager | 10 |
| v0.14.1h | Node Library | 8 |
| v0.14.1i | Canvas Validation | 8 |
| v0.14.1j | Canvas Controls UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// The visual workflow canvas for designing agent orchestrations.
/// </summary>
public interface IWorkflowCanvas
{
    CanvasState State { get; }
    WorkflowDefinition CurrentWorkflow { get; }

    // Node operations
    Task<CanvasNode> AddNodeAsync(
        NodeDefinition definition,
        CanvasPosition position,
        CancellationToken ct = default);

    Task RemoveNodeAsync(CanvasNodeId nodeId, CancellationToken ct = default);
    Task MoveNodeAsync(CanvasNodeId nodeId, CanvasPosition position, CancellationToken ct = default);
    Task<CanvasNode> DuplicateNodeAsync(CanvasNodeId nodeId, CancellationToken ct = default);

    // Connection operations
    Task<CanvasConnection> ConnectAsync(
        ConnectionEndpoint source,
        ConnectionEndpoint target,
        CancellationToken ct = default);

    Task DisconnectAsync(CanvasConnectionId connectionId, CancellationToken ct = default);

    // Canvas operations
    Task<CanvasValidation> ValidateAsync(CancellationToken ct = default);
    Task<WorkflowDefinition> ExportAsync(CancellationToken ct = default);
    Task ImportAsync(WorkflowDefinition workflow, CancellationToken ct = default);
    Task ClearAsync(CancellationToken ct = default);

    // Selection
    Task SelectAsync(IReadOnlyList<CanvasNodeId> nodeIds, CancellationToken ct = default);
    Task SelectAllAsync(CancellationToken ct = default);
    Task ClearSelectionAsync(CancellationToken ct = default);

    // Viewport
    Task ZoomAsync(float level, CancellationToken ct = default);
    Task PanAsync(CanvasOffset offset, CancellationToken ct = default);
    Task FitToViewAsync(CancellationToken ct = default);

    // History
    Task UndoAsync(CancellationToken ct = default);
    Task RedoAsync(CancellationToken ct = default);

    // Events
    IObservable<CanvasEvent> Events { get; }
}

public record CanvasState
{
    public float Zoom { get; init; } = 1.0f;
    public CanvasOffset Pan { get; init; }
    public IReadOnlyList<CanvasNodeId> SelectedNodes { get; init; } = [];
    public bool IsDirty { get; init; }
    public bool IsValid { get; init; }
}

/// <summary>
/// A node on the canvas representing a task or control flow element.
/// </summary>
public record CanvasNode
{
    public CanvasNodeId Id { get; init; }
    public required NodeDefinition Definition { get; init; }
    public CanvasPosition Position { get; init; }
    public CanvasSize Size { get; init; }
    public NodeVisualState VisualState { get; init; }
    public IReadOnlyDictionary<string, object> Configuration { get; init; } =
        new Dictionary<string, object>();
    public IReadOnlyList<NodePort> Inputs { get; init; } = [];
    public IReadOnlyList<NodePort> Outputs { get; init; } = [];
}

public readonly record struct CanvasNodeId(Guid Value)
{
    public static CanvasNodeId New() => new(Guid.NewGuid());
}

public record CanvasPosition(double X, double Y);
public record CanvasSize(double Width, double Height);
public record CanvasOffset(double X, double Y);

public enum NodeVisualState
{
    Normal, Selected, Highlighted, Error, Running, Completed
}

/// <summary>
/// Definition of a node type available in the palette.
/// </summary>
public record NodeDefinition
{
    public required string TypeId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public NodeCategory Category { get; init; }
    public string? IconPath { get; init; }
    public string? Color { get; init; }
    public IReadOnlyList<NodePortDefinition> InputPorts { get; init; } = [];
    public IReadOnlyList<NodePortDefinition> OutputPorts { get; init; } = [];
    public IReadOnlyList<NodeConfigProperty> ConfigProperties { get; init; } = [];
    public Func<NodeConfiguration, TaskNode>? ToTaskNode { get; init; }
}

public enum NodeCategory
{
    Agent,          // Agent invocation nodes
    Control,        // Control flow (if/switch/loop)
    Data,           // Data transformation
    Integration,    // External integrations
    Utility,        // Helper nodes
    Custom          // User-defined
}

public record NodePortDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required PortDataType DataType { get; init; }
    public bool Required { get; init; } = true;
    public bool AllowMultiple { get; init; }
}

public enum PortDataType
{
    Text, Document, Entity, List, Any, Custom
}

/// <summary>
/// Library of available node types.
/// </summary>
public interface INodeLibrary
{
    Task<IReadOnlyList<NodeDefinition>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<NodeDefinition>> GetByCategoryAsync(
        NodeCategory category,
        CancellationToken ct = default);
    Task<NodeDefinition?> GetByIdAsync(string typeId, CancellationToken ct = default);
    Task RegisterAsync(NodeDefinition definition, CancellationToken ct = default);
    Task UnregisterAsync(string typeId, CancellationToken ct = default);
}

/// <summary>
/// Connection between nodes.
/// </summary>
public record CanvasConnection
{
    public CanvasConnectionId Id { get; init; }
    public ConnectionEndpoint Source { get; init; }
    public ConnectionEndpoint Target { get; init; }
    public ConnectionVisualState VisualState { get; init; }
}

public record ConnectionEndpoint
{
    public CanvasNodeId NodeId { get; init; }
    public string PortId { get; init; } = "";
}

public enum ConnectionVisualState
{
    Normal, Highlighted, Error, DataFlowing
}

/// <summary>
/// Canvas validation result.
/// </summary>
public record CanvasValidation
{
    public bool IsValid { get; init; }
    public IReadOnlyList<ValidationError> Errors { get; init; } = [];
    public IReadOnlyList<ValidationWarning> Warnings { get; init; } = [];
}

public record ValidationError
{
    public CanvasNodeId? NodeId { get; init; }
    public CanvasConnectionId? ConnectionId { get; init; }
    public required string Message { get; init; }
    public string? Suggestion { get; init; }
}
```

### Built-in Node Types

| Category | Nodes |
|:---------|:------|
| Agent | Researcher, Writer, Editor, Validator, Translator, Custom Agent |
| Control | Start, End, If/Else, Switch, Loop, Parallel, Join, Wait |
| Data | Transform, Filter, Merge, Split, Map, Aggregate |
| Integration | Git, CKVS Query, File Read/Write, HTTP, Webhook |
| Utility | Comment, Note, Log, Delay, Variable |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | View-only canvas |
| WriterPro | Basic editing, 10 node types |
| Teams | Full editing, all nodes |
| Enterprise | + Custom nodes + export |

---

## v0.14.2-STU: Agent Debugger

**Goal:** Enable step-by-step debugging of agent workflows with breakpoints, state inspection, and execution control.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.14.2e | Debug Session Manager | 12 |
| v0.14.2f | Breakpoint System | 10 |
| v0.14.2g | State Inspector | 10 |
| v0.14.2h | Step Controller | 10 |
| v0.14.2i | Variable Watch | 8 |
| v0.14.2j | Debug Console UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Manages debug sessions for workflow execution.
/// </summary>
public interface IDebugSessionManager
{
    Task<DebugSession> StartDebugAsync(
        WorkflowDefinition workflow,
        DebugOptions options,
        CancellationToken ct = default);

    Task<DebugSession> AttachAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    Task DetachAsync(DebugSessionId sessionId, CancellationToken ct = default);

    Task<DebugSession?> GetSessionAsync(
        DebugSessionId sessionId,
        CancellationToken ct = default);

    Task<IReadOnlyList<DebugSession>> GetActiveSessionsAsync(
        CancellationToken ct = default);
}

public readonly record struct DebugSessionId(Guid Value)
{
    public static DebugSessionId New() => new(Guid.NewGuid());
}

public record DebugSession
{
    public DebugSessionId Id { get; init; }
    public WorkflowDefinition Workflow { get; init; } = null!;
    public ExecutionId? ExecutionId { get; init; }
    public DebugSessionState State { get; init; }
    public CanvasNodeId? CurrentNodeId { get; init; }
    public IReadOnlyList<Breakpoint> Breakpoints { get; init; } = [];
    public DebugContext Context { get; init; } = new();
}

public enum DebugSessionState
{
    Initializing, Running, Paused, AtBreakpoint, Stepping, Completed, Error
}

/// <summary>
/// Controls execution flow during debugging.
/// </summary>
public interface IDebugController
{
    Task ContinueAsync(DebugSessionId sessionId, CancellationToken ct = default);
    Task PauseAsync(DebugSessionId sessionId, CancellationToken ct = default);
    Task StepOverAsync(DebugSessionId sessionId, CancellationToken ct = default);
    Task StepIntoAsync(DebugSessionId sessionId, CancellationToken ct = default);
    Task StepOutAsync(DebugSessionId sessionId, CancellationToken ct = default);
    Task RunToNodeAsync(DebugSessionId sessionId, CanvasNodeId targetNode, CancellationToken ct = default);
    Task StopAsync(DebugSessionId sessionId, CancellationToken ct = default);
    Task RestartAsync(DebugSessionId sessionId, CancellationToken ct = default);
}

/// <summary>
/// Manages breakpoints in workflows.
/// </summary>
public interface IBreakpointManager
{
    Task<Breakpoint> SetBreakpointAsync(
        DebugSessionId sessionId,
        BreakpointLocation location,
        BreakpointOptions? options = null,
        CancellationToken ct = default);

    Task RemoveBreakpointAsync(
        DebugSessionId sessionId,
        BreakpointId breakpointId,
        CancellationToken ct = default);

    Task<Breakpoint> ToggleBreakpointAsync(
        DebugSessionId sessionId,
        BreakpointId breakpointId,
        CancellationToken ct = default);

    Task ClearAllBreakpointsAsync(
        DebugSessionId sessionId,
        CancellationToken ct = default);

    Task<IReadOnlyList<Breakpoint>> GetBreakpointsAsync(
        DebugSessionId sessionId,
        CancellationToken ct = default);
}

public record Breakpoint
{
    public BreakpointId Id { get; init; }
    public BreakpointLocation Location { get; init; }
    public bool IsEnabled { get; init; } = true;
    public string? Condition { get; init; }  // Expression to evaluate
    public int? HitCount { get; init; }      // Break after N hits
    public string? LogMessage { get; init; }  // Log instead of break
}

public readonly record struct BreakpointId(Guid Value)
{
    public static BreakpointId New() => new(Guid.NewGuid());
}

public record BreakpointLocation
{
    public CanvasNodeId NodeId { get; init; }
    public BreakpointPosition Position { get; init; } = BreakpointPosition.Before;
}

public enum BreakpointPosition
{
    Before,     // Before node executes
    After,      // After node completes
    OnError     // When node fails
}

/// <summary>
/// Inspects state during debugging.
/// </summary>
public interface IStateInspector
{
    Task<NodeState> InspectNodeAsync(
        DebugSessionId sessionId,
        CanvasNodeId nodeId,
        CancellationToken ct = default);

    Task<AgentState> InspectAgentAsync(
        DebugSessionId sessionId,
        AgentId agentId,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<string, object>> GetVariablesAsync(
        DebugSessionId sessionId,
        VariableScope scope,
        CancellationToken ct = default);

    Task<object?> EvaluateExpressionAsync(
        DebugSessionId sessionId,
        string expression,
        CancellationToken ct = default);

    Task<CallStack> GetCallStackAsync(
        DebugSessionId sessionId,
        CancellationToken ct = default);
}

public record NodeState
{
    public CanvasNodeId NodeId { get; init; }
    public TaskExecutionState ExecutionState { get; init; }
    public IReadOnlyDictionary<string, object> Inputs { get; init; } =
        new Dictionary<string, object>();
    public IReadOnlyDictionary<string, object>? Outputs { get; init; }
    public IReadOnlyDictionary<string, object> LocalVariables { get; init; } =
        new Dictionary<string, object>();
    public DateTimeOffset? StartedAt { get; init; }
    public TimeSpan? Duration { get; init; }
    public string? ErrorMessage { get; init; }
}

public record CallStack
{
    public IReadOnlyList<StackFrame> Frames { get; init; } = [];
}

public record StackFrame
{
    public int Depth { get; init; }
    public CanvasNodeId NodeId { get; init; }
    public string NodeName { get; init; } = "";
    public AgentId? AgentId { get; init; }
    public DateTimeOffset EnteredAt { get; init; }
}

/// <summary>
/// Watch expressions and variables.
/// </summary>
public interface IVariableWatch
{
    Task<WatchId> AddWatchAsync(
        DebugSessionId sessionId,
        string expression,
        CancellationToken ct = default);

    Task RemoveWatchAsync(
        DebugSessionId sessionId,
        WatchId watchId,
        CancellationToken ct = default);

    Task<IReadOnlyList<WatchResult>> EvaluateWatchesAsync(
        DebugSessionId sessionId,
        CancellationToken ct = default);
}

public record WatchResult
{
    public WatchId Id { get; init; }
    public string Expression { get; init; } = "";
    public object? Value { get; init; }
    public string? Error { get; init; }
    public bool HasChanged { get; init; }
}
```

### Debug Features

| Feature | Description |
|:--------|:------------|
| Step Over | Execute current node, pause at next |
| Step Into | Step into agent's internal execution |
| Step Out | Complete current agent, return to caller |
| Run to Cursor | Execute until reaching selected node |
| Conditional Breakpoints | Break only when condition is true |
| Logpoints | Log message without stopping |
| Watch Expressions | Monitor variable values |
| Call Stack | View execution hierarchy |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Not available |
| WriterPro | Basic debugging, 5 breakpoints |
| Teams | Full debugging |
| Enterprise | + Remote debugging + API |

---

## v0.14.3-STU: Live Monitoring Dashboard

**Goal:** Real-time visualization of running orchestrations with metrics, resource monitoring, and alerting.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.14.3e | Live Graph Renderer | 12 |
| v0.14.3f | Metrics Collector | 10 |
| v0.14.3g | Resource Monitor | 8 |
| v0.14.3h | Alert System | 10 |
| v0.14.3i | Dashboard Widgets | 8 |
| v0.14.3j | Dashboard Layout UI | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Live monitoring dashboard for running orchestrations.
/// </summary>
public interface IMonitoringDashboard
{
    Task<Dashboard> CreateDashboardAsync(
        DashboardConfiguration config,
        CancellationToken ct = default);

    Task<Dashboard> GetDashboardAsync(
        DashboardId dashboardId,
        CancellationToken ct = default);

    Task UpdateLayoutAsync(
        DashboardId dashboardId,
        DashboardLayout layout,
        CancellationToken ct = default);

    Task<IReadOnlyList<Dashboard>> GetDashboardsAsync(
        CancellationToken ct = default);
}

public record Dashboard
{
    public DashboardId Id { get; init; }
    public required string Name { get; init; }
    public DashboardLayout Layout { get; init; } = new();
    public IReadOnlyList<DashboardWidget> Widgets { get; init; } = [];
    public TimeRange DefaultTimeRange { get; init; }
    public TimeSpan RefreshInterval { get; init; } = TimeSpan.FromSeconds(5);
}

public record DashboardWidget
{
    public Guid WidgetId { get; init; }
    public required WidgetType Type { get; init; }
    public required string Title { get; init; }
    public WidgetPosition Position { get; init; }
    public WidgetSize Size { get; init; }
    public IReadOnlyDictionary<string, object> Configuration { get; init; } =
        new Dictionary<string, object>();
}

public enum WidgetType
{
    LiveGraph,          // Real-time workflow visualization
    MetricsChart,       // Time-series metrics
    AgentList,          // Active agents
    ExecutionList,      // Running executions
    ResourceGauge,      // CPU/Memory/Tokens
    AlertFeed,          // Recent alerts
    LogStream,          // Live logs
    StatCard            // Single metric
}

/// <summary>
/// Real-time workflow graph visualization.
/// </summary>
public interface ILiveGraphRenderer
{
    IObservable<LiveGraphState> ObserveExecution(
        ExecutionId executionId,
        LiveGraphOptions options);

    Task<LiveGraphSnapshot> GetSnapshotAsync(
        ExecutionId executionId,
        CancellationToken ct = default);
}

public record LiveGraphState
{
    public ExecutionId ExecutionId { get; init; }
    public IReadOnlyList<LiveNode> Nodes { get; init; } = [];
    public IReadOnlyList<LiveConnection> Connections { get; init; } = [];
    public ExecutionProgress Progress { get; init; } = new();
    public DateTimeOffset Timestamp { get; init; }
}

public record LiveNode
{
    public CanvasNodeId NodeId { get; init; }
    public string Name { get; init; } = "";
    public LiveNodeState State { get; init; }
    public AgentId? AssignedAgentId { get; init; }
    public float? Progress { get; init; }
    public TimeSpan? Duration { get; init; }
    public int? TokensUsed { get; init; }
}

public enum LiveNodeState
{
    Pending, Queued, Running, Completed, Failed, Skipped
}

public record LiveConnection
{
    public CanvasConnectionId ConnectionId { get; init; }
    public LiveConnectionState State { get; init; }
    public bool DataFlowing { get; init; }
}

public enum LiveConnectionState
{
    Inactive, Active, Completed, Error
}

/// <summary>
/// Collects and aggregates metrics.
/// </summary>
public interface IMetricsCollector
{
    Task RecordMetricAsync(
        MetricPoint point,
        CancellationToken ct = default);

    Task<MetricSeries> QueryAsync(
        MetricQuery query,
        CancellationToken ct = default);

    IObservable<MetricPoint> ObserveMetric(string metricName);
}

public record MetricPoint
{
    public required string Name { get; init; }
    public required double Value { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>();
}

public record MetricQuery
{
    public required string MetricName { get; init; }
    public TimeRange TimeRange { get; init; }
    public TimeSpan? Aggregation { get; init; }
    public AggregationType AggregationType { get; init; } = AggregationType.Average;
    public IReadOnlyDictionary<string, string>? TagFilters { get; init; }
}

public enum AggregationType
{
    Sum, Average, Min, Max, Count, P50, P95, P99
}

/// <summary>
/// Monitors system resources.
/// </summary>
public interface IResourceMonitor
{
    Task<ResourceSnapshot> GetSnapshotAsync(CancellationToken ct = default);
    IObservable<ResourceSnapshot> Observe(TimeSpan interval);
}

public record ResourceSnapshot
{
    public DateTimeOffset Timestamp { get; init; }
    public double CpuPercent { get; init; }
    public long MemoryUsedBytes { get; init; }
    public long MemoryTotalBytes { get; init; }
    public int ActiveAgents { get; init; }
    public int ActiveExecutions { get; init; }
    public int TokensPerMinute { get; init; }
    public int QueuedTasks { get; init; }
}

/// <summary>
/// Alert management.
/// </summary>
public interface IAlertManager
{
    Task<AlertRule> CreateRuleAsync(
        AlertRuleDefinition definition,
        CancellationToken ct = default);

    Task<IReadOnlyList<Alert>> GetActiveAlertsAsync(
        AlertSeverity? minSeverity = null,
        CancellationToken ct = default);

    Task AcknowledgeAlertAsync(
        AlertId alertId,
        string? comment = null,
        CancellationToken ct = default);

    IObservable<Alert> ObserveAlerts();
}

public record AlertRuleDefinition
{
    public required string Name { get; init; }
    public required string Condition { get; init; }  // Expression
    public required AlertSeverity Severity { get; init; }
    public TimeSpan EvaluationInterval { get; init; } = TimeSpan.FromMinutes(1);
    public IReadOnlyList<AlertAction>? Actions { get; init; }
}

public record Alert
{
    public AlertId Id { get; init; }
    public required string RuleName { get; init; }
    public required string Message { get; init; }
    public required AlertSeverity Severity { get; init; }
    public DateTimeOffset FiredAt { get; init; }
    public DateTimeOffset? AcknowledgedAt { get; init; }
    public string? AcknowledgedBy { get; init; }
}

public enum AlertSeverity { Info, Warning, Error, Critical }
```

### Default Metrics

| Metric | Description |
|:-------|:------------|
| `executions.active` | Currently running orchestrations |
| `executions.completed` | Completed in time window |
| `agents.active` | Running agent instances |
| `agents.spawned` | Agents spawned in time window |
| `tokens.used` | LLM tokens consumed |
| `tasks.completed` | Tasks completed |
| `tasks.failed` | Tasks failed |
| `latency.p95` | 95th percentile task latency |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic execution list |
| WriterPro | + Live graph + 3 widgets |
| Teams | Full dashboard + alerts |
| Enterprise | + Custom dashboards + export |

---

## v0.14.4-STU: Prompt Engineering Studio

**Goal:** Dedicated environment for crafting, testing, and versioning prompts used by agents.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.14.4e | Prompt Editor | 10 |
| v0.14.4f | Prompt Testing | 12 |
| v0.14.4g | Prompt Versioning | 8 |
| v0.14.4h | A/B Testing | 10 |
| v0.14.4i | Prompt Analytics | 6 |
| v0.14.4j | Prompt Library UI | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Studio for engineering and managing prompts.
/// </summary>
public interface IPromptStudio
{
    // Prompt management
    Task<Prompt> CreatePromptAsync(
        CreatePromptRequest request,
        CancellationToken ct = default);

    Task<Prompt> UpdatePromptAsync(
        PromptId promptId,
        UpdatePromptRequest request,
        CancellationToken ct = default);

    Task<Prompt?> GetPromptAsync(
        PromptId promptId,
        PromptVersion? version = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<Prompt>> SearchPromptsAsync(
        PromptSearchQuery query,
        CancellationToken ct = default);

    // Testing
    Task<PromptTestResult> TestAsync(
        PromptId promptId,
        PromptTestRequest request,
        CancellationToken ct = default);

    // Versioning
    Task<PromptVersion> PublishVersionAsync(
        PromptId promptId,
        string versionName,
        CancellationToken ct = default);

    Task<IReadOnlyList<PromptVersion>> GetVersionsAsync(
        PromptId promptId,
        CancellationToken ct = default);
}

public readonly record struct PromptId(Guid Value)
{
    public static PromptId New() => new(Guid.NewGuid());
}

/// <summary>
/// A managed prompt with templates and variables.
/// </summary>
public record Prompt
{
    public PromptId Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Template { get; init; }
    public IReadOnlyList<PromptVariable> Variables { get; init; } = [];
    public PromptMetadata Metadata { get; init; } = new();
    public PromptVersion CurrentVersion { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public record PromptVariable
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public PromptVariableType Type { get; init; } = PromptVariableType.String;
    public bool Required { get; init; } = true;
    public object? DefaultValue { get; init; }
    public string? ValidationPattern { get; init; }
}

public enum PromptVariableType
{
    String, Number, Boolean, List, Object, Document, Entity
}

public record PromptMetadata
{
    public string? Category { get; init; }
    public string? Author { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
    public int UsageCount { get; init; }
    public float? AverageQualityScore { get; init; }
}

/// <summary>
/// Prompt test execution.
/// </summary>
public record PromptTestRequest
{
    public IReadOnlyDictionary<string, object> Variables { get; init; } =
        new Dictionary<string, object>();
    public string? Model { get; init; }
    public int Runs { get; init; } = 1;
    public PromptTestOptions Options { get; init; } = new();
}

public record PromptTestOptions
{
    public bool IncludeTokenCounts { get; init; } = true;
    public bool IncludeLatency { get; init; } = true;
    public bool IncludeRawResponse { get; init; }
    public float? Temperature { get; init; }
    public int? MaxTokens { get; init; }
}

public record PromptTestResult
{
    public PromptId PromptId { get; init; }
    public IReadOnlyList<PromptTestRun> Runs { get; init; } = [];
    public PromptTestSummary Summary { get; init; } = new();
}

public record PromptTestRun
{
    public int RunNumber { get; init; }
    public string RenderedPrompt { get; init; } = "";
    public string Response { get; init; } = "";
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public TimeSpan Latency { get; init; }
    public string? Error { get; init; }
}

public record PromptTestSummary
{
    public int TotalRuns { get; init; }
    public int SuccessfulRuns { get; init; }
    public int AverageInputTokens { get; init; }
    public int AverageOutputTokens { get; init; }
    public TimeSpan AverageLatency { get; init; }
    public float SuccessRate { get; init; }
}

/// <summary>
/// A/B testing for prompts.
/// </summary>
public interface IPromptABTesting
{
    Task<ABTest> CreateTestAsync(
        ABTestDefinition definition,
        CancellationToken ct = default);

    Task<ABTest> StartTestAsync(
        ABTestId testId,
        CancellationToken ct = default);

    Task<ABTest> StopTestAsync(
        ABTestId testId,
        CancellationToken ct = default);

    Task<ABTestResults> GetResultsAsync(
        ABTestId testId,
        CancellationToken ct = default);

    Task<PromptId> SelectWinnerAsync(
        ABTestId testId,
        CancellationToken ct = default);
}

public record ABTestDefinition
{
    public required string Name { get; init; }
    public required PromptId ControlPromptId { get; init; }
    public required IReadOnlyList<PromptId> VariantPromptIds { get; init; }
    public required ABTestMetric PrimaryMetric { get; init; }
    public int TargetSampleSize { get; init; } = 100;
    public float TrafficSplit { get; init; } = 0.5f;  // % to variants
}

public enum ABTestMetric
{
    QualityScore,
    TokenEfficiency,
    Latency,
    UserSatisfaction
}

public record ABTestResults
{
    public ABTestId TestId { get; init; }
    public IReadOnlyList<VariantResult> Results { get; init; } = [];
    public PromptId? StatisticalWinner { get; init; }
    public float Confidence { get; init; }
}

public record VariantResult
{
    public PromptId PromptId { get; init; }
    public int SampleSize { get; init; }
    public float AverageMetricValue { get; init; }
    public float StandardDeviation { get; init; }
    public bool IsWinner { get; init; }
}
```

### Prompt Editor Features

| Feature | Description |
|:--------|:------------|
| Syntax Highlighting | Template variable highlighting |
| Variable Autocomplete | Suggest available variables |
| Live Preview | Real-time rendered preview |
| Token Counter | Estimate token usage |
| Diff View | Compare versions |
| Import/Export | YAML/JSON formats |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic editing, 5 prompts |
| WriterPro | + Testing + versioning |
| Teams | + A/B testing + analytics |
| Enterprise | + Unlimited + API |

---

## v0.14.5-STU: Simulation & Replay

**Goal:** Enable dry-run simulation and execution replay for testing and debugging workflows without side effects.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.14.5e | Simulation Engine | 12 |
| v0.14.5f | Mock Provider | 10 |
| v0.14.5g | Execution Recorder | 10 |
| v0.14.5h | Replay Controller | 10 |
| v0.14.5i | What-If Analysis | 6 |
| v0.14.5j | Simulation UI | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Simulates workflow execution without side effects.
/// </summary>
public interface ISimulationEngine
{
    Task<Simulation> CreateSimulationAsync(
        WorkflowDefinition workflow,
        SimulationOptions options,
        CancellationToken ct = default);

    Task<SimulationResult> RunAsync(
        SimulationId simulationId,
        CancellationToken ct = default);

    Task<SimulationResult> RunWithMocksAsync(
        SimulationId simulationId,
        IReadOnlyDictionary<string, MockResponse> mocks,
        CancellationToken ct = default);
}

public readonly record struct SimulationId(Guid Value)
{
    public static SimulationId New() => new(Guid.NewGuid());
}

public record SimulationOptions
{
    public SimulationMode Mode { get; init; } = SimulationMode.Full;
    public bool RecordExecution { get; init; } = true;
    public IReadOnlyDictionary<string, object>? InputOverrides { get; init; }
    public IReadOnlyDictionary<CanvasNodeId, MockResponse>? NodeMocks { get; init; }
    public TimeSpan? SpeedMultiplier { get; init; }  // For time-based tests
}

public enum SimulationMode
{
    Full,           // Run all agents with mock LLM
    Structural,     // Validate structure only
    DataFlow,       // Trace data flow
    Partial         // Run subset of nodes
}

public record Simulation
{
    public SimulationId Id { get; init; }
    public WorkflowDefinition Workflow { get; init; } = null!;
    public SimulationState State { get; init; }
    public SimulationOptions Options { get; init; } = new();
    public DateTimeOffset CreatedAt { get; init; }
}

public enum SimulationState
{
    Created, Running, Completed, Failed
}

public record SimulationResult
{
    public SimulationId SimulationId { get; init; }
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<SimulatedNodeResult> NodeResults { get; init; } = [];
    public IReadOnlyDictionary<string, object> FinalOutputs { get; init; } =
        new Dictionary<string, object>();
    public IReadOnlyList<SimulationWarning> Warnings { get; init; } = [];
    public RecordedExecution? Recording { get; init; }
}

public record SimulatedNodeResult
{
    public CanvasNodeId NodeId { get; init; }
    public bool Executed { get; init; }
    public bool Success { get; init; }
    public object? Output { get; init; }
    public bool WasMocked { get; init; }
    public TimeSpan SimulatedDuration { get; init; }
}

/// <summary>
/// Mock responses for simulation.
/// </summary>
public record MockResponse
{
    public required object Output { get; init; }
    public TimeSpan? SimulatedLatency { get; init; }
    public bool ShouldFail { get; init; }
    public string? FailureMessage { get; init; }
}

/// <summary>
/// Records execution for replay.
/// </summary>
public interface IExecutionRecorder
{
    Task<RecordingId> StartRecordingAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    Task StopRecordingAsync(
        RecordingId recordingId,
        CancellationToken ct = default);

    Task<RecordedExecution> GetRecordingAsync(
        RecordingId recordingId,
        CancellationToken ct = default);

    Task<IReadOnlyList<RecordedExecution>> ListRecordingsAsync(
        RecordingQuery query,
        CancellationToken ct = default);
}

public readonly record struct RecordingId(Guid Value)
{
    public static RecordingId New() => new(Guid.NewGuid());
}

public record RecordedExecution
{
    public RecordingId Id { get; init; }
    public ExecutionId OriginalExecutionId { get; init; }
    public WorkflowDefinition Workflow { get; init; } = null!;
    public DateTimeOffset RecordedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public IReadOnlyList<RecordedEvent> Events { get; init; } = [];
    public long SizeBytes { get; init; }
}

public record RecordedEvent
{
    public DateTimeOffset Timestamp { get; init; }
    public TimeSpan Offset { get; init; }  // From start
    public required string EventType { get; init; }
    public required object EventData { get; init; }
    public CanvasNodeId? NodeId { get; init; }
    public AgentId? AgentId { get; init; }
}

/// <summary>
/// Replays recorded executions.
/// </summary>
public interface IReplayController
{
    Task<ReplaySession> StartReplayAsync(
        RecordingId recordingId,
        ReplayOptions options,
        CancellationToken ct = default);

    Task PauseReplayAsync(ReplaySessionId sessionId, CancellationToken ct = default);
    Task ResumeReplayAsync(ReplaySessionId sessionId, CancellationToken ct = default);
    Task SeekAsync(ReplaySessionId sessionId, TimeSpan position, CancellationToken ct = default);
    Task SetSpeedAsync(ReplaySessionId sessionId, float speed, CancellationToken ct = default);
    Task StopReplayAsync(ReplaySessionId sessionId, CancellationToken ct = default);

    IObservable<ReplayEvent> ObserveReplay(ReplaySessionId sessionId);
}

public record ReplayOptions
{
    public float Speed { get; init; } = 1.0f;  // 0.5x to 10x
    public TimeSpan? StartAt { get; init; }
    public TimeSpan? EndAt { get; init; }
    public bool PauseOnErrors { get; init; }
}

public record ReplaySession
{
    public ReplaySessionId Id { get; init; }
    public RecordingId RecordingId { get; init; }
    public ReplayState State { get; init; }
    public TimeSpan CurrentPosition { get; init; }
    public TimeSpan TotalDuration { get; init; }
    public float Speed { get; init; }
}

public enum ReplayState
{
    Playing, Paused, Stopped, Completed
}

/// <summary>
/// What-if analysis for workflows.
/// </summary>
public interface IWhatIfAnalyzer
{
    Task<WhatIfResult> AnalyzeAsync(
        RecordingId baseRecordingId,
        WhatIfScenario scenario,
        CancellationToken ct = default);

    Task<WhatIfComparison> CompareAsync(
        RecordingId recordingA,
        RecordingId recordingB,
        CancellationToken ct = default);
}

public record WhatIfScenario
{
    public string Name { get; init; } = "";
    public IReadOnlyDictionary<CanvasNodeId, MockResponse>? NodeOverrides { get; init; }
    public IReadOnlyDictionary<string, object>? InputOverrides { get; init; }
    public IReadOnlyList<CanvasNodeId>? SkipNodes { get; init; }
}

public record WhatIfResult
{
    public RecordingId BaseRecordingId { get; init; }
    public WhatIfScenario Scenario { get; init; } = new();
    public SimulationResult SimulatedResult { get; init; } = null!;
    public IReadOnlyList<WhatIfDifference> Differences { get; init; } = [];
}

public record WhatIfDifference
{
    public CanvasNodeId NodeId { get; init; }
    public DifferenceType Type { get; init; }
    public object? OriginalValue { get; init; }
    public object? NewValue { get; init; }
    public string Description { get; init; } = "";
}

public enum DifferenceType
{
    OutputChanged, ExecutionSkipped, ExecutionAdded, FailureAdded, FailureRemoved
}
```

### Simulation Capabilities

| Capability | Description |
|:-----------|:------------|
| Dry Run | Execute with mock LLM responses |
| Data Flow Trace | Visualize data movement |
| Failure Injection | Test error handling |
| Speed Control | Fast-forward or slow-motion |
| Recording | Capture for later replay |
| What-If | Test alternative scenarios |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic dry run |
| WriterPro | + Recording + replay |
| Teams | + What-if + mock library |
| Enterprise | Unlimited recordings + API |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.14.x |
|:----------|:-------|:-----------------|
| `IWorkflowDesigner` | v0.13.5-ORC | Workflow definitions |
| `IExecutionCoordinator` | v0.13.3-ORC | Execution control |
| `IAgentLifecycleManager` | v0.12.2-AGT | Agent inspection |
| `IAgentMemory` | v0.12.4-AGT | State inspection |
| `IChatCompletionService` | v0.6.1a | Prompt testing |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `CanvasNodeAddedEvent` | v0.14.1 | Node added to canvas |
| `WorkflowExportedEvent` | v0.14.1 | Workflow exported |
| `BreakpointHitEvent` | v0.14.2 | Debugger hit breakpoint |
| `DebugSessionStartedEvent` | v0.14.2 | Debug session began |
| `AlertFiredEvent` | v0.14.3 | Monitoring alert triggered |
| `PromptPublishedEvent` | v0.14.4 | Prompt version published |
| `ABTestCompletedEvent` | v0.14.4 | A/B test finished |
| `SimulationCompletedEvent` | v0.14.5 | Simulation finished |
| `RecordingCreatedEvent` | v0.14.5 | Execution recorded |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `Blazor.Diagrams` | 3.x | Canvas rendering |
| `OxyPlot.Avalonia` | 2.x | Metrics charts |
| `Moq` | 4.x | Mock framework |
| `SignalR` | 8.x | Real-time updates |

---

## Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Canvas render | <16ms (60fps) |
| Node add/remove | <50ms |
| Debug step | <100ms |
| Dashboard refresh | <500ms |
| Prompt test | <3s |
| Simulation run | <2x real time |

---

## What This Enables

With v0.14.x complete, Lexichord provides a complete visual development environment:

- **Visual Design:** Drag-and-drop workflow creation
- **Debugging:** Step through orchestrations like code
- **Monitoring:** Real-time visibility into running systems
- **Prompt Engineering:** Professional prompt management
- **Testing:** Safe simulation and what-if analysis

This foundation enables the marketplace and extensibility in v0.15.x.

---

## Total Agent Studio Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 8: Agent Studio | v0.14.1 - v0.14.5 | ~264 |

**Combined with prior phases:** ~1,484 hours (~37 person-months)

---
