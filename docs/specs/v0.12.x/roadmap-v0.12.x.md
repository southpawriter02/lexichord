# Lexichord Agent Infrastructure Roadmap (v0.12.1 - v0.12.5)

In v0.11.x, we delivered comprehensive security infrastructure with RBAC/ABAC, audit logging, encryption, and API security. In v0.12.x, we establish **Agent Infrastructure & Primitives** — the foundational building blocks for multi-agent orchestration that transform Lexichord from a writing tool into an intelligent agent platform.

**Architectural Note:** This version introduces the `Lexichord.Agents.Core` module containing agent abstractions, lifecycle management, and inter-agent communication primitives. These components form the foundation for orchestration in v0.13.x.

**Total Sub-Parts:** 35 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 248 hours (~6.2 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.12.1-AGT | Agent Definition Model | Agent schemas, capabilities, contracts | 48 |
| v0.12.2-AGT | Agent Lifecycle Manager | Spawn, monitor, terminate, restart | 52 |
| v0.12.3-AGT | Agent Communication Bus | Message passing, events, request/response | 50 |
| v0.12.4-AGT | Agent Memory & Context | Working memory, long-term storage, context windows | 48 |
| v0.12.5-AGT | Agent Tool System | Tool definitions, execution sandbox, result handling | 50 |

---

## Agent Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                       Agent Infrastructure Layer                         │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌─────────────┐ │
│  │   Agent      │  │   Agent      │  │   Agent      │  │   Agent     │ │
│  │ Definition   │  │  Lifecycle   │  │ Communication│  │  Memory     │ │
│  │  (v0.12.1)   │  │  (v0.12.2)   │  │   (v0.12.3)  │  │  (v0.12.4)  │ │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘  └──────┬──────┘ │
│         │                 │                 │                 │         │
│         └─────────────────┴─────────────────┴─────────────────┘         │
│                                     │                                    │
│                          ┌──────────┴──────────┐                        │
│                          │     Agent Tool      │                        │
│                          │      System         │                        │
│                          │     (v0.12.5)       │                        │
│                          └─────────────────────┘                        │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## v0.12.1-AGT: Agent Definition Model

**Goal:** Define the schema and contract for what constitutes an agent in Lexichord, including capabilities, inputs/outputs, and behavioral specifications.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.12.1e | Agent Schema & Contracts | 8 |
| v0.12.1f | Capability Declaration | 8 |
| v0.12.1g | Agent Configuration | 6 |
| v0.12.1h | Agent Registry | 10 |
| v0.12.1i | Agent Validation | 8 |
| v0.12.1j | Agent Definition UI | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Defines the contract for an agent in Lexichord.
/// </summary>
public interface IAgent
{
    AgentId Id { get; }
    AgentManifest Manifest { get; }
    AgentState State { get; }

    Task<AgentResponse> ProcessAsync(AgentRequest request, CancellationToken ct = default);
    Task InitializeAsync(AgentContext context, CancellationToken ct = default);
    Task ShutdownAsync(ShutdownReason reason, CancellationToken ct = default);
}

/// <summary>
/// Immutable identifier for an agent instance.
/// </summary>
public readonly record struct AgentId(Guid Value)
{
    public static AgentId New() => new(Guid.NewGuid());
    public override string ToString() => $"agent:{Value:N}";
}

/// <summary>
/// Declarative manifest describing an agent's capabilities and requirements.
/// </summary>
public record AgentManifest
{
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public AgentType Type { get; init; } = AgentType.Task;

    // Capabilities this agent provides
    public IReadOnlyList<AgentCapability> Capabilities { get; init; } = [];

    // What this agent requires to function
    public AgentRequirements Requirements { get; init; } = new();

    // Input/output schema
    public AgentIOSchema? InputSchema { get; init; }
    public AgentIOSchema? OutputSchema { get; init; }

    // Behavioral constraints
    public AgentConstraints Constraints { get; init; } = new();

    // Metadata for discovery
    public IReadOnlyDictionary<string, string> Tags { get; init; } =
        new Dictionary<string, string>();
}

public enum AgentType
{
    Task,           // Single-shot task execution
    Conversational, // Multi-turn interaction
    Reactive,       // Event-driven
    Autonomous,     // Self-directed with goals
    Supervisor      // Manages other agents
}

/// <summary>
/// Declares a specific capability an agent provides.
/// </summary>
public record AgentCapability
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public CapabilityCategory Category { get; init; }
    public IReadOnlyList<string> InputTypes { get; init; } = [];
    public IReadOnlyList<string> OutputTypes { get; init; } = [];
    public float QualityScore { get; init; } = 0.8f;  // Self-reported quality
}

public enum CapabilityCategory
{
    TextGeneration,
    TextAnalysis,
    CodeGeneration,
    CodeAnalysis,
    DataExtraction,
    DataTransformation,
    Research,
    Validation,
    Translation,
    Summarization,
    Planning,
    Execution
}

/// <summary>
/// Resource and permission requirements for an agent.
/// </summary>
public record AgentRequirements
{
    public LLMRequirements? LLM { get; init; }
    public MemoryRequirements? Memory { get; init; }
    public IReadOnlyList<string> RequiredTools { get; init; } = [];
    public IReadOnlyList<Permission> RequiredPermissions { get; init; } = [];
    public TimeSpan? MaxExecutionTime { get; init; }
    public int? MaxTokenBudget { get; init; }
}

public record LLMRequirements
{
    public IReadOnlyList<string> SupportedProviders { get; init; } = [];
    public int? MinContextWindow { get; init; }
    public bool RequiresVision { get; init; }
    public bool RequiresToolUse { get; init; }
    public bool RequiresStreaming { get; init; }
}

/// <summary>
/// Registry for discovering and managing agent definitions.
/// </summary>
public interface IAgentRegistry
{
    Task<AgentRegistration> RegisterAsync(
        AgentManifest manifest,
        AgentFactory factory,
        CancellationToken ct = default);

    Task UnregisterAsync(string agentName, CancellationToken ct = default);

    Task<AgentManifest?> GetManifestAsync(
        string agentName,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentManifest>> FindByCapabilityAsync(
        CapabilityCategory category,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentManifest>> SearchAsync(
        AgentSearchQuery query,
        CancellationToken ct = default);

    IAsyncEnumerable<AgentManifest> GetAllAsync(CancellationToken ct = default);
}

public delegate IAgent AgentFactory(AgentContext context);

public record AgentSearchQuery
{
    public string? NamePattern { get; init; }
    public IReadOnlyList<CapabilityCategory>? Categories { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public AgentType? Type { get; init; }
}
```

### Built-in Agent Types

| Agent Type | Description | Example Use Cases |
|:-----------|:------------|:------------------|
| Task | Executes a single, well-defined task | Generate release notes, format code |
| Conversational | Maintains dialogue state | Co-pilot chat, Q&A |
| Reactive | Responds to events | File watcher, CI/CD triggers |
| Autonomous | Pursues goals independently | Research agent, doc crawler |
| Supervisor | Coordinates child agents | Orchestrator, task decomposer |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | 3 built-in agents |
| WriterPro | + 2 custom agents |
| Teams | + 10 custom agents + sharing |
| Enterprise | Unlimited + custom types |

---

## v0.12.2-AGT: Agent Lifecycle Manager

**Goal:** Manage the complete lifecycle of agent instances including spawning, monitoring, health checks, and graceful termination.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.12.2e | Agent Spawner | 10 |
| v0.12.2f | Agent Monitor | 10 |
| v0.12.2g | Health Check System | 8 |
| v0.12.2h | Graceful Shutdown | 8 |
| v0.12.2i | Agent Restart Policies | 8 |
| v0.12.2j | Lifecycle Dashboard UI | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Manages the spawning and lifecycle of agent instances.
/// </summary>
public interface IAgentLifecycleManager
{
    Task<AgentInstance> SpawnAsync(
        SpawnRequest request,
        CancellationToken ct = default);

    Task<AgentInstance?> GetInstanceAsync(
        AgentId agentId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentInstance>> GetActiveInstancesAsync(
        CancellationToken ct = default);

    Task TerminateAsync(
        AgentId agentId,
        TerminationReason reason,
        CancellationToken ct = default);

    Task<TerminateAllResult> TerminateAllAsync(
        TerminationReason reason,
        CancellationToken ct = default);

    IAsyncEnumerable<AgentLifecycleEvent> ObserveAsync(
        AgentId? agentId = null,
        CancellationToken ct = default);
}

public record SpawnRequest
{
    public required string AgentName { get; init; }
    public string? Version { get; init; }  // null = latest
    public AgentSpawnOptions Options { get; init; } = new();
    public IReadOnlyDictionary<string, object>? InitialContext { get; init; }
    public AgentId? ParentId { get; init; }  // For hierarchical agents
}

public record AgentSpawnOptions
{
    public RestartPolicy RestartPolicy { get; init; } = RestartPolicy.OnFailure;
    public int MaxRestarts { get; init; } = 3;
    public TimeSpan RestartBackoff { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan? Timeout { get; init; }
    public ResourceLimits? Limits { get; init; }
    public IsolationLevel Isolation { get; init; } = IsolationLevel.Shared;
}

public enum RestartPolicy
{
    Never,      // Never restart
    OnFailure,  // Restart only on unhandled exceptions
    Always,     // Always restart (for long-running agents)
    OnCrash     // Restart only on process crash
}

public enum IsolationLevel
{
    Shared,     // Shares resources with other agents
    Isolated,   // Dedicated resources
    Sandboxed   // Maximum isolation (for untrusted agents)
}

public record ResourceLimits
{
    public int? MaxConcurrentRequests { get; init; }
    public int? MaxTokensPerMinute { get; init; }
    public long? MaxMemoryBytes { get; init; }
    public TimeSpan? MaxExecutionTime { get; init; }
}

/// <summary>
/// Represents a running agent instance.
/// </summary>
public record AgentInstance
{
    public AgentId Id { get; init; }
    public string AgentName { get; init; } = "";
    public string Version { get; init; } = "";
    public AgentState State { get; init; }
    public DateTimeOffset SpawnedAt { get; init; }
    public DateTimeOffset? LastActivityAt { get; init; }
    public AgentId? ParentId { get; init; }
    public IReadOnlyList<AgentId> ChildIds { get; init; } = [];
    public AgentHealthStatus Health { get; init; }
    public AgentMetrics Metrics { get; init; } = new();
}

public enum AgentState
{
    Initializing,
    Ready,
    Processing,
    Waiting,
    Suspended,
    Terminating,
    Terminated,
    Failed
}

public record AgentHealthStatus
{
    public HealthState State { get; init; }
    public DateTimeOffset LastCheckAt { get; init; }
    public string? Message { get; init; }
    public IReadOnlyList<HealthIssue> Issues { get; init; } = [];
}

public enum HealthState { Healthy, Degraded, Unhealthy, Unknown }

/// <summary>
/// Monitors agent health and performance.
/// </summary>
public interface IAgentMonitor
{
    Task<AgentHealthStatus> CheckHealthAsync(
        AgentId agentId,
        CancellationToken ct = default);

    Task<AgentMetrics> GetMetricsAsync(
        AgentId agentId,
        MetricsTimeRange range,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentAlert>> GetAlertsAsync(
        AgentId? agentId = null,
        CancellationToken ct = default);

    Task SetAlertRuleAsync(
        AgentAlertRule rule,
        CancellationToken ct = default);
}

public record AgentMetrics
{
    public long RequestsProcessed { get; init; }
    public long RequestsFailed { get; init; }
    public TimeSpan AverageLatency { get; init; }
    public TimeSpan P95Latency { get; init; }
    public long TokensConsumed { get; init; }
    public long MemoryUsedBytes { get; init; }
    public int RestartCount { get; init; }
    public double UptimePercentage { get; init; }
}
```

### Lifecycle State Machine

```
                    ┌─────────────┐
                    │ Initializing│
                    └──────┬──────┘
                           │ init complete
                           ▼
    ┌───────────────► Ready ◄──────────────┐
    │                   │                   │
    │ idle              │ request           │ complete
    │                   ▼                   │
    │              Processing ─────────────┘
    │                   │
    │ resume            │ suspend
    │                   ▼
    │              Suspended
    │                   │
    │                   │ terminate
    ▼                   ▼
┌────────┐        Terminating
│ Failed │              │
└────────┘              ▼
                   Terminated
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | 2 concurrent agents |
| WriterPro | 5 concurrent agents |
| Teams | 20 concurrent + hierarchies |
| Enterprise | Unlimited + custom isolation |

---

## v0.12.3-AGT: Agent Communication Bus

**Goal:** Enable rich inter-agent communication through message passing, events, and request/response patterns.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.12.3e | Message Bus Core | 10 |
| v0.12.3f | Event System | 8 |
| v0.12.3g | Request/Response | 10 |
| v0.12.3h | Broadcast & Multicast | 8 |
| v0.12.3i | Message Routing | 8 |
| v0.12.3j | Communication Monitor UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Central message bus for inter-agent communication.
/// </summary>
public interface IAgentMessageBus
{
    // Direct messaging
    Task SendAsync(
        AgentId target,
        AgentMessage message,
        CancellationToken ct = default);

    // Request/Response pattern
    Task<AgentMessage> RequestAsync(
        AgentId target,
        AgentMessage request,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    // Pub/Sub
    Task PublishAsync(
        AgentEvent @event,
        CancellationToken ct = default);

    Task<IAsyncDisposable> SubscribeAsync(
        AgentEventFilter filter,
        Func<AgentEvent, CancellationToken, Task> handler,
        CancellationToken ct = default);

    // Broadcast to all agents matching criteria
    Task BroadcastAsync(
        AgentMessage message,
        AgentSelector selector,
        CancellationToken ct = default);

    // Observe all messages (for debugging/monitoring)
    IAsyncEnumerable<AgentMessage> ObserveAsync(
        MessageObservationOptions options,
        CancellationToken ct = default);
}

/// <summary>
/// A message sent between agents.
/// </summary>
public record AgentMessage
{
    public MessageId Id { get; init; } = MessageId.New();
    public AgentId SenderId { get; init; }
    public AgentId? TargetId { get; init; }
    public required string Type { get; init; }
    public required object Payload { get; init; }
    public MessagePriority Priority { get; init; } = MessagePriority.Normal;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public MessageId? CorrelationId { get; init; }  // For request/response
    public MessageId? CausationId { get; init; }    // Chain of causation
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();
    public TimeSpan? TimeToLive { get; init; }
}

public readonly record struct MessageId(Guid Value)
{
    public static MessageId New() => new(Guid.NewGuid());
}

public enum MessagePriority
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// An event published to the agent ecosystem.
/// </summary>
public record AgentEvent
{
    public required string EventType { get; init; }
    public required AgentId SourceId { get; init; }
    public required object Data { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Filter for subscribing to specific events.
/// </summary>
public record AgentEventFilter
{
    public IReadOnlyList<string>? EventTypes { get; init; }
    public IReadOnlyList<AgentId>? SourceIds { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public Func<AgentEvent, bool>? Predicate { get; init; }
}

/// <summary>
/// Selects agents for broadcast messages.
/// </summary>
public record AgentSelector
{
    public IReadOnlyList<AgentId>? Ids { get; init; }
    public IReadOnlyList<string>? Names { get; init; }
    public IReadOnlyList<AgentType>? Types { get; init; }
    public IReadOnlyList<CapabilityCategory>? Capabilities { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public Func<AgentInstance, bool>? Predicate { get; init; }
}

/// <summary>
/// Routes messages to appropriate handlers.
/// </summary>
public interface IMessageRouter
{
    Task<RouteResult> RouteAsync(
        AgentMessage message,
        RoutingContext context,
        CancellationToken ct = default);

    Task RegisterRouteAsync(
        RouteDefinition route,
        CancellationToken ct = default);
}

public record RouteDefinition
{
    public required string Name { get; init; }
    public required MessageMatcher Matcher { get; init; }
    public required AgentSelector Target { get; init; }
    public RoutingStrategy Strategy { get; init; } = RoutingStrategy.First;
    public int Priority { get; init; } = 0;
}

public enum RoutingStrategy
{
    First,       // First matching agent
    RoundRobin,  // Distribute evenly
    LeastBusy,   // Agent with lowest load
    Broadcast,   // All matching agents
    Random       // Random selection
}
```

### Communication Patterns

| Pattern | Use Case | Example |
|:--------|:---------|:--------|
| Fire-and-Forget | Notifications | "Document saved" |
| Request/Response | Queries, commands | "Analyze this text" |
| Pub/Sub | Events, state changes | "Entity updated" |
| Broadcast | System-wide announcements | "Shutdown initiated" |
| Saga | Multi-step transactions | "Publish workflow" |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Direct messaging only |
| WriterPro | + Pub/Sub (5 topics) |
| Teams | + Broadcast + routing |
| Enterprise | + Sagas + unlimited topics |

---

## v0.12.4-AGT: Agent Memory & Context

**Goal:** Provide agents with working memory, long-term storage, and intelligent context window management.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.12.4e | Working Memory | 10 |
| v0.12.4f | Long-Term Memory | 10 |
| v0.12.4g | Context Window Manager | 10 |
| v0.12.4h | Memory Retrieval | 8 |
| v0.12.4i | Memory Persistence | 6 |
| v0.12.4j | Memory Inspector UI | 4 |

### Key Interfaces

```csharp
/// <summary>
/// Manages all memory types for an agent.
/// </summary>
public interface IAgentMemory
{
    IWorkingMemory Working { get; }
    ILongTermMemory LongTerm { get; }
    IContextWindow Context { get; }

    Task<MemorySnapshot> SnapshotAsync(CancellationToken ct = default);
    Task RestoreAsync(MemorySnapshot snapshot, CancellationToken ct = default);
    Task ClearAsync(MemoryClearOptions options, CancellationToken ct = default);
}

/// <summary>
/// Short-term, session-scoped memory.
/// </summary>
public interface IWorkingMemory
{
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task<bool> ContainsAsync(string key, CancellationToken ct = default);
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, object>> GetAllAsync(CancellationToken ct = default);

    // Scoped variables (auto-cleared after task)
    IWorkingMemoryScope CreateScope();
}

public interface IWorkingMemoryScope : IAsyncDisposable
{
    Task SetAsync<T>(string key, T value, CancellationToken ct = default);
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
}

/// <summary>
/// Persistent memory across sessions with semantic retrieval.
/// </summary>
public interface ILongTermMemory
{
    Task StoreAsync(
        MemoryEntry entry,
        CancellationToken ct = default);

    Task<IReadOnlyList<MemoryEntry>> RetrieveAsync(
        MemoryQuery query,
        CancellationToken ct = default);

    Task<IReadOnlyList<MemoryEntry>> SearchSemanticAsync(
        string query,
        int limit = 10,
        CancellationToken ct = default);

    Task ForgetAsync(
        MemoryForgetCriteria criteria,
        CancellationToken ct = default);

    Task<MemoryStats> GetStatsAsync(CancellationToken ct = default);
}

public record MemoryEntry
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Content { get; init; }
    public required MemoryType Type { get; init; }
    public float Importance { get; init; } = 0.5f;  // 0-1 scale
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
    public float[]? Embedding { get; init; }  // For semantic search
}

public enum MemoryType
{
    Fact,        // Factual information
    Event,       // Something that happened
    Insight,     // Derived knowledge
    Preference,  // User preference
    Correction,  // Error correction
    Conversation // Chat history
}

/// <summary>
/// Manages the LLM context window intelligently.
/// </summary>
public interface IContextWindow
{
    int MaxTokens { get; }
    int CurrentTokens { get; }
    int AvailableTokens { get; }

    Task<ContextBuildResult> BuildAsync(
        ContextBuildRequest request,
        CancellationToken ct = default);

    Task AddAsync(
        ContextItem item,
        CancellationToken ct = default);

    Task<CompactionResult> CompactAsync(
        CompactionStrategy strategy,
        int targetTokens,
        CancellationToken ct = default);

    Task ClearAsync(CancellationToken ct = default);

    IReadOnlyList<ContextItem> GetItems();
}

public record ContextItem
{
    public required string Content { get; init; }
    public required ContextItemType Type { get; init; }
    public float Priority { get; init; } = 0.5f;  // For compaction decisions
    public int TokenCount { get; init; }
    public bool Pinned { get; init; }  // Never remove during compaction
    public DateTimeOffset AddedAt { get; init; } = DateTimeOffset.UtcNow;
}

public enum ContextItemType
{
    SystemPrompt,
    UserMessage,
    AssistantMessage,
    ToolResult,
    RetrievedDocument,
    WorkingMemory,
    Instruction
}

public record ContextBuildRequest
{
    public IReadOnlyList<ContextItem> RequiredItems { get; init; } = [];
    public int ReserveTokens { get; init; } = 1000;  // For response
    public bool IncludeRelevantMemory { get; init; } = true;
    public string? QueryForRetrieval { get; init; }
}

public enum CompactionStrategy
{
    RemoveOldest,      // FIFO
    RemoveLowPriority, // By priority score
    Summarize,         // Compress older messages
    Selective          // Keep only high-value items
}
```

### Memory Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    Agent Memory                          │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │  Working Memory │  │      Long-Term Memory        │  │
│  │   (Session)     │  │       (Persistent)           │  │
│  │                 │  │                              │  │
│  │ • Variables     │  │ • Facts        ┌──────────┐ │  │
│  │ • Scratch pad   │  │ • Events       │ pgvector │ │  │
│  │ • Task state    │  │ • Insights ───►│  Index   │ │  │
│  │                 │  │ • Corrections  └──────────┘ │  │
│  └────────┬────────┘  └──────────────┬──────────────┘  │
│           │                          │                  │
│           ▼                          ▼                  │
│  ┌───────────────────────────────────────────────────┐ │
│  │              Context Window Manager                │ │
│  │  • Token counting    • Compaction strategies      │ │
│  │  • Priority ranking  • Semantic retrieval         │ │
│  └───────────────────────────────────────────────────┘ │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Working memory only |
| WriterPro | + Long-term (100MB) |
| Teams | + Long-term (1GB) + semantic |
| Enterprise | Unlimited + custom retention |

---

## v0.12.5-AGT: Agent Tool System

**Goal:** Define, register, and execute tools that agents can use, with proper sandboxing and result handling.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.12.5e | Tool Definition Schema | 8 |
| v0.12.5f | Tool Registry | 8 |
| v0.12.5g | Tool Executor | 12 |
| v0.12.5h | Execution Sandbox | 10 |
| v0.12.5i | Result Handling | 6 |
| v0.12.5j | Tool Management UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Defines a tool that can be used by agents.
/// </summary>
public interface ITool
{
    ToolDefinition Definition { get; }
    Task<ToolResult> ExecuteAsync(
        ToolInput input,
        ToolExecutionContext context,
        CancellationToken ct = default);
}

/// <summary>
/// Schema definition for a tool.
/// </summary>
public record ToolDefinition
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public ToolCategory Category { get; init; }
    public IReadOnlyList<ToolParameter> Parameters { get; init; } = [];
    public ToolOutputSchema? OutputSchema { get; init; }
    public ToolConstraints Constraints { get; init; } = new();
    public IReadOnlyList<string> RequiredPermissions { get; init; } = [];
    public bool RequiresConfirmation { get; init; }
}

public enum ToolCategory
{
    FileSystem,
    Network,
    Database,
    CodeExecution,
    ExternalApi,
    Knowledge,
    Communication,
    System
}

public record ToolParameter
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required ToolParameterType Type { get; init; }
    public bool Required { get; init; } = true;
    public object? Default { get; init; }
    public IReadOnlyList<object>? Enum { get; init; }
    public JsonSchema? Schema { get; init; }  // For complex types
}

public enum ToolParameterType
{
    String, Integer, Number, Boolean, Array, Object
}

public record ToolConstraints
{
    public TimeSpan? MaxExecutionTime { get; init; }
    public int? MaxOutputSize { get; init; }
    public bool AllowSideEffects { get; init; } = true;
    public IsolationLevel RequiredIsolation { get; init; } = IsolationLevel.Shared;
}

/// <summary>
/// Registry for tool discovery and management.
/// </summary>
public interface IToolRegistry
{
    Task<ToolRegistration> RegisterAsync(
        ITool tool,
        CancellationToken ct = default);

    Task UnregisterAsync(
        string toolName,
        CancellationToken ct = default);

    Task<ITool?> GetToolAsync(
        string toolName,
        CancellationToken ct = default);

    Task<IReadOnlyList<ToolDefinition>> GetAvailableToolsAsync(
        ToolAvailabilityContext context,
        CancellationToken ct = default);

    Task<IReadOnlyList<ToolDefinition>> FindByCategoryAsync(
        ToolCategory category,
        CancellationToken ct = default);
}

/// <summary>
/// Executes tools with proper sandboxing and monitoring.
/// </summary>
public interface IToolExecutor
{
    Task<ToolResult> ExecuteAsync(
        string toolName,
        ToolInput input,
        ToolExecutionOptions options,
        CancellationToken ct = default);

    Task<ToolResult> ExecuteBatchAsync(
        IReadOnlyList<ToolInvocation> invocations,
        BatchExecutionOptions options,
        CancellationToken ct = default);

    IAsyncEnumerable<ToolExecutionEvent> ObserveAsync(
        CancellationToken ct = default);
}

public record ToolInput
{
    public IReadOnlyDictionary<string, object> Parameters { get; init; } =
        new Dictionary<string, object>();
}

public record ToolExecutionOptions
{
    public TimeSpan? Timeout { get; init; }
    public bool DryRun { get; init; }
    public bool RequireConfirmation { get; init; }
    public IsolationLevel Isolation { get; init; } = IsolationLevel.Shared;
    public AgentId? CallingAgentId { get; init; }
}

public record ToolResult
{
    public bool Success { get; init; }
    public object? Output { get; init; }
    public string? Error { get; init; }
    public TimeSpan ExecutionTime { get; init; }
    public ToolResultMetadata Metadata { get; init; } = new();
}

public record ToolResultMetadata
{
    public int? OutputTokens { get; init; }
    public bool HadSideEffects { get; init; }
    public IReadOnlyList<string> AffectedResources { get; init; } = [];
}

/// <summary>
/// Sandboxed execution environment for untrusted tools.
/// </summary>
public interface IToolSandbox
{
    Task<SandboxedToolResult> ExecuteAsync(
        ITool tool,
        ToolInput input,
        SandboxOptions options,
        CancellationToken ct = default);
}

public record SandboxOptions
{
    public IReadOnlyList<string> AllowedPaths { get; init; } = [];
    public IReadOnlyList<string> AllowedHosts { get; init; } = [];
    public long MaxMemoryBytes { get; init; } = 100 * 1024 * 1024;  // 100MB
    public TimeSpan MaxCpuTime { get; init; } = TimeSpan.FromSeconds(30);
    public bool AllowNetworkAccess { get; init; }
    public bool AllowFileSystemAccess { get; init; }
}
```

### Built-in Tools

| Tool | Category | Description |
|:-----|:---------|:------------|
| `read_file` | FileSystem | Read file contents |
| `write_file` | FileSystem | Write to file |
| `search_knowledge` | Knowledge | Query CKVS |
| `web_fetch` | Network | Fetch URL content |
| `execute_ckvs_query` | Database | Run CKVS-QL |
| `send_message` | Communication | Message another agent |
| `create_entity` | Knowledge | Add to knowledge graph |
| `validate_content` | Knowledge | Check against style rules |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | 5 built-in tools |
| WriterPro | + 10 custom tools |
| Teams | + 50 custom + sandbox |
| Enterprise | Unlimited + custom sandboxes |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.12.x |
|:----------|:-------|:-----------------|
| `IAuthorizationService` | v0.11.1-SEC | Tool permissions |
| `IAuditLogger` | v0.11.2-SEC | Agent activity logging |
| `IEncryptionService` | v0.11.3-SEC | Memory encryption |
| `IRateLimiter` | v0.11.4-SEC | Agent rate limiting |
| `IApiKeyService` | v0.11.5-SEC | External tool auth |
| `IGraphRepository` | v0.4.5e | Knowledge access |
| `IRagService` | v0.4.3 | Memory embeddings |
| `IChatCompletionService` | v0.6.1a | LLM integration |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `AgentRegisteredEvent` | v0.12.1 | New agent type registered |
| `AgentSpawnedEvent` | v0.12.2 | Agent instance created |
| `AgentTerminatedEvent` | v0.12.2 | Agent instance stopped |
| `AgentHealthChangedEvent` | v0.12.2 | Health status changed |
| `AgentMessageSentEvent` | v0.12.3 | Message sent |
| `AgentEventPublishedEvent` | v0.12.3 | Event published |
| `MemoryStoredEvent` | v0.12.4 | Memory entry added |
| `ContextCompactedEvent` | v0.12.4 | Context window compacted |
| `ToolExecutedEvent` | v0.12.5 | Tool execution completed |
| `ToolRegisteredEvent` | v0.12.5 | New tool registered |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `System.Threading.Channels` | 8.x | Message bus implementation |
| `Cronos` | 0.8.x | Agent scheduling |
| `Jint` | 3.x | JavaScript sandbox |
| `Docker.DotNet` | 3.x | Container-based isolation |

---

## Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Agent spawn | <100ms |
| Message delivery | <5ms |
| Tool execution | <timeout |
| Memory retrieval | <50ms |
| Context build | <100ms |
| Health check | <10ms |

---

## What This Enables

With v0.12.x complete, Lexichord has the foundational infrastructure for multi-agent systems:

- **Modularity:** Agents are well-defined, discoverable components
- **Reliability:** Lifecycle management ensures resilience
- **Communication:** Rich inter-agent interaction patterns
- **Intelligence:** Agents remember and learn across sessions
- **Capability:** Extensible tool system for real-world actions

This foundation enables the orchestration capabilities in v0.13.x.

---

## Total Agent Infrastructure Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 6: Agent Infrastructure | v0.12.1 - v0.12.5 | ~248 |

**Combined with prior phases:** ~944 hours (~24 person-months)

---
