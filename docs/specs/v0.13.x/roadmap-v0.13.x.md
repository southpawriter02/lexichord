# Lexichord Agent Orchestration Roadmap (v0.13.1 - v0.13.5)

In v0.12.x, we established **Agent Infrastructure** — the foundational building blocks for agents including definitions, lifecycle management, communication, memory, and tools. In v0.13.x, we deliver **The Orchestration Engine** — the intelligent conductor that coordinates multiple agents to accomplish complex, multi-step goals.

**Architectural Note:** This version introduces the `Lexichord.Orchestration` module, implementing the "Composer" vision from the original design proposal. The orchestrator elevates users from authors to conductors, delegating complex documentation tasks to coordinated agent ensembles.

**Total Sub-Parts:** 37 distinct implementation steps across 5 versions.
**Total Estimated Hours:** 276 hours (~6.9 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.13.1-ORC | Task Decomposition Engine | Intent parsing, task breakdown, dependency graphs | 56 |
| v0.13.2-ORC | Agent Selection & Dispatch | Capability matching, load balancing, delegation | 52 |
| v0.13.3-ORC | Execution Coordinator | Parallel execution, sequencing, checkpoints | 58 |
| v0.13.4-ORC | Result Aggregation & Synthesis | Merge outputs, resolve conflicts, quality scoring | 54 |
| v0.13.5-ORC | Orchestration Patterns & Templates | Workflows, ensembles, conductor UI | 56 |

---

## Orchestration Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        Orchestration Engine                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│         ┌──────────────────────────────────────────────────────┐            │
│         │                User Intent                            │            │
│         │    "Document the new Auth v2 flow based on commits"   │            │
│         └────────────────────────┬─────────────────────────────┘            │
│                                  │                                           │
│                                  ▼                                           │
│         ┌────────────────────────────────────────────────────────┐          │
│         │              Task Decomposition Engine                  │          │
│         │                    (v0.13.1)                           │          │
│         └────────────────────────┬───────────────────────────────┘          │
│                                  │                                           │
│                                  ▼                                           │
│         ┌────────────────────────────────────────────────────────┐          │
│         │            Agent Selection & Dispatch                   │          │
│         │                    (v0.13.2)                           │          │
│         └────────────────────────┬───────────────────────────────┘          │
│                                  │                                           │
│              ┌───────────────────┼───────────────────┐                      │
│              ▼                   ▼                   ▼                      │
│    ┌──────────────┐    ┌──────────────┐    ┌──────────────┐                │
│    │  Chronicler  │    │    Scribe    │    │   Validator  │                │
│    │    Agent     │    │    Agent     │    │    Agent     │                │
│    └──────┬───────┘    └──────┬───────┘    └──────┬───────┘                │
│           │                   │                   │                         │
│           └───────────────────┴───────────────────┘                         │
│                                  │                                           │
│                                  ▼                                           │
│         ┌────────────────────────────────────────────────────────┐          │
│         │           Execution Coordinator (v0.13.3)               │          │
│         │    Parallel execution, sequencing, checkpoints          │          │
│         └────────────────────────┬───────────────────────────────┘          │
│                                  │                                           │
│                                  ▼                                           │
│         ┌────────────────────────────────────────────────────────┐          │
│         │         Result Aggregation & Synthesis (v0.13.4)        │          │
│         │      Merge outputs, resolve conflicts, score quality    │          │
│         └────────────────────────┬───────────────────────────────┘          │
│                                  │                                           │
│                                  ▼                                           │
│         ┌────────────────────────────────────────────────────────┐          │
│         │                   Final Output                          │          │
│         │      Unified documentation ready for human review       │          │
│         └────────────────────────────────────────────────────────┘          │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## v0.13.1-ORC: Task Decomposition Engine

**Goal:** Parse user intent and decompose complex requests into a structured task graph with dependencies, enabling intelligent work distribution.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.1e | Intent Parser | 10 |
| v0.13.1f | Task Graph Builder | 12 |
| v0.13.1g | Dependency Analyzer | 10 |
| v0.13.1h | Complexity Estimator | 8 |
| v0.13.1i | Decomposition Strategies | 8 |
| v0.13.1j | Decomposition Preview UI | 8 |

### Key Interfaces

```csharp
/// <summary>
/// Parses user intent and decomposes into executable tasks.
/// </summary>
public interface ITaskDecomposer
{
    Task<DecompositionResult> DecomposeAsync(
        DecompositionRequest request,
        CancellationToken ct = default);

    Task<DecompositionResult> RefineAsync(
        DecompositionResult previous,
        RefinementFeedback feedback,
        CancellationToken ct = default);

    Task<IReadOnlyList<DecompositionStrategy>> GetStrategiesAsync(
        string intent,
        CancellationToken ct = default);
}

public record DecompositionRequest
{
    public required string Intent { get; init; }
    public DecompositionOptions Options { get; init; } = new();
    public IReadOnlyDictionary<string, object>? Context { get; init; }
    public IReadOnlyList<Guid>? RelevantEntityIds { get; init; }
    public IReadOnlyList<string>? RelevantDocuments { get; init; }
}

public record DecompositionOptions
{
    public int MaxDepth { get; init; } = 5;
    public int MaxTasks { get; init; } = 50;
    public DecompositionStrategy? PreferredStrategy { get; init; }
    public bool IncludeValidationTasks { get; init; } = true;
    public bool IncludeReviewTasks { get; init; } = true;
    public ComplexityBudget? Budget { get; init; }
}

public record ComplexityBudget
{
    public int? MaxAgents { get; init; }
    public int? MaxTokens { get; init; }
    public TimeSpan? MaxDuration { get; init; }
}

/// <summary>
/// Result of decomposing an intent into tasks.
/// </summary>
public record DecompositionResult
{
    public DecompositionId Id { get; init; } = DecompositionId.New();
    public required string OriginalIntent { get; init; }
    public required TaskGraph Graph { get; init; }
    public DecompositionStrategy StrategyUsed { get; init; }
    public ComplexityEstimate Estimate { get; init; } = new();
    public IReadOnlyList<DecompositionWarning> Warnings { get; init; } = [];
    public float Confidence { get; init; }
}

/// <summary>
/// Directed acyclic graph of tasks with dependencies.
/// </summary>
public record TaskGraph
{
    public IReadOnlyList<TaskNode> Nodes { get; init; } = [];
    public IReadOnlyList<TaskEdge> Edges { get; init; } = [];
    public IReadOnlyList<TaskNodeId> RootNodes { get; init; } = [];
    public IReadOnlyList<TaskNodeId> LeafNodes { get; init; } = [];

    public IReadOnlyList<TaskNode> GetDependencies(TaskNodeId nodeId);
    public IReadOnlyList<TaskNode> GetDependents(TaskNodeId nodeId);
    public IReadOnlyList<IReadOnlyList<TaskNode>> GetExecutionLayers();
    public bool HasCycle();
}

/// <summary>
/// A single task in the decomposition graph.
/// </summary>
public record TaskNode
{
    public TaskNodeId Id { get; init; } = TaskNodeId.New();
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required TaskType Type { get; init; }
    public TaskPriority Priority { get; init; } = TaskPriority.Normal;

    // What this task needs to accomplish
    public required TaskObjective Objective { get; init; }

    // Requirements for execution
    public IReadOnlyList<CapabilityCategory> RequiredCapabilities { get; init; } = [];
    public IReadOnlyList<string> RequiredInputs { get; init; } = [];
    public IReadOnlyList<string> ProducedOutputs { get; init; } = [];

    // Constraints
    public TaskConstraints Constraints { get; init; } = new();

    // Metadata
    public ComplexityEstimate Complexity { get; init; } = new();
    public IReadOnlyDictionary<string, object>? Context { get; init; }
}

public readonly record struct TaskNodeId(Guid Value)
{
    public static TaskNodeId New() => new(Guid.NewGuid());
}

public enum TaskType
{
    Research,       // Gather information
    Generation,     // Create content
    Analysis,       // Analyze existing content
    Transformation, // Convert/transform content
    Validation,     // Check quality/correctness
    Integration,    // Combine multiple outputs
    Review,         // Human review checkpoint
    Decision        // Branching decision point
}

public enum TaskPriority { Low, Normal, High, Critical }

public record TaskObjective
{
    public required string Goal { get; init; }
    public IReadOnlyList<string> SuccessCriteria { get; init; } = [];
    public string? OutputFormat { get; init; }
    public IReadOnlyList<string>? Examples { get; init; }
}

public record TaskEdge
{
    public TaskNodeId From { get; init; }
    public TaskNodeId To { get; init; }
    public EdgeType Type { get; init; } = EdgeType.Dependency;
    public string? OutputMapping { get; init; }  // Which output feeds which input
}

public enum EdgeType
{
    Dependency,     // Must complete before
    DataFlow,       // Output feeds input
    Conditional,    // Execute if condition met
    Parallel        // Can run simultaneously
}

public record ComplexityEstimate
{
    public int EstimatedTokens { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public int SuggestedAgentCount { get; init; }
    public float DifficultyScore { get; init; }  // 0-1
    public IReadOnlyList<string> RiskFactors { get; init; } = [];
}

/// <summary>
/// Strategies for decomposing different types of intents.
/// </summary>
public enum DecompositionStrategy
{
    Sequential,      // Linear step-by-step
    Parallel,        // Independent concurrent tasks
    Hierarchical,    // Tree structure with sub-decomposition
    Pipeline,        // Assembly line pattern
    MapReduce,       // Scatter-gather pattern
    Iterative,       // Refine through multiple passes
    Adaptive         // Choose dynamically based on feedback
}
```

### Intent Parsing Examples

| User Intent | Decomposition |
|:------------|:--------------|
| "Document the Auth v2 flow" | Research (commits) → Research (specs) → Generate (overview) → Generate (details) → Validate → Review |
| "Update all API endpoints" | Discover (endpoints) → [Generate × N in parallel] → Integrate → Validate |
| "Fix terminology in Chapter 3" | Analyze (current) → Identify (violations) → [Fix × N] → Review |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Sequential only, max 5 tasks |
| WriterPro | + Parallel, max 15 tasks |
| Teams | + All strategies, max 50 tasks |
| Enterprise | Unlimited + custom strategies |

---

## v0.13.2-ORC: Agent Selection & Dispatch

**Goal:** Match tasks to the most suitable agents based on capabilities, current load, and historical performance, then dispatch work efficiently.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.2e | Capability Matcher | 10 |
| v0.13.2f | Load Balancer | 10 |
| v0.13.2g | Performance Tracker | 8 |
| v0.13.2h | Dispatch Scheduler | 10 |
| v0.13.2i | Fallback Strategies | 8 |
| v0.13.2j | Selection Dashboard UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Selects and dispatches agents for task execution.
/// </summary>
public interface IAgentDispatcher
{
    Task<DispatchPlan> PlanDispatchAsync(
        TaskGraph graph,
        DispatchOptions options,
        CancellationToken ct = default);

    Task<DispatchResult> DispatchAsync(
        DispatchPlan plan,
        CancellationToken ct = default);

    Task<ReassignmentResult> ReassignAsync(
        TaskNodeId taskId,
        ReassignmentReason reason,
        CancellationToken ct = default);
}

public record DispatchOptions
{
    public SelectionStrategy Strategy { get; init; } = SelectionStrategy.BestMatch;
    public bool AllowAgentSpawning { get; init; } = true;
    public int MaxConcurrentAgents { get; init; } = 10;
    public bool PreferSpecialization { get; init; } = true;
    public IReadOnlyList<AgentId>? PreferredAgents { get; init; }
    public IReadOnlyList<AgentId>? ExcludedAgents { get; init; }
}

public enum SelectionStrategy
{
    BestMatch,      // Highest capability match
    LeastBusy,      // Lowest current load
    BestPerformer,  // Highest historical success
    RoundRobin,     // Distribute evenly
    Cheapest,       // Minimize token cost
    Fastest,        // Minimize latency
    Balanced        // Multi-factor optimization
}

/// <summary>
/// Plan for dispatching tasks to agents.
/// </summary>
public record DispatchPlan
{
    public Guid PlanId { get; init; } = Guid.NewGuid();
    public IReadOnlyList<TaskAssignment> Assignments { get; init; } = [];
    public IReadOnlyList<AgentId> RequiredAgents { get; init; } = [];
    public IReadOnlyList<string> AgentsToSpawn { get; init; } = [];
    public DispatchEstimate Estimate { get; init; } = new();
    public IReadOnlyList<DispatchWarning> Warnings { get; init; } = [];
}

public record TaskAssignment
{
    public TaskNodeId TaskId { get; init; }
    public AgentId? AssignedAgentId { get; init; }
    public string? AgentToSpawn { get; init; }  // If no existing agent suitable
    public float MatchScore { get; init; }
    public IReadOnlyList<AgentCandidate> Alternatives { get; init; } = [];
}

public record AgentCandidate
{
    public AgentId AgentId { get; init; }
    public string AgentName { get; init; } = "";
    public float CapabilityScore { get; init; }
    public float AvailabilityScore { get; init; }
    public float PerformanceScore { get; init; }
    public float OverallScore { get; init; }
    public string? DisqualificationReason { get; init; }
}

/// <summary>
/// Matches task requirements to agent capabilities.
/// </summary>
public interface ICapabilityMatcher
{
    Task<MatchResult> MatchAsync(
        TaskNode task,
        IReadOnlyList<AgentInstance> candidates,
        CancellationToken ct = default);

    Task<float> ScoreMatchAsync(
        TaskNode task,
        AgentManifest agent,
        CancellationToken ct = default);
}

public record MatchResult
{
    public IReadOnlyList<AgentCandidate> Candidates { get; init; } = [];
    public bool HasSuitableAgent => Candidates.Any(c => c.OverallScore >= 0.6f);
    public AgentCandidate? BestMatch => Candidates.MaxBy(c => c.OverallScore);
}

/// <summary>
/// Tracks agent performance for selection decisions.
/// </summary>
public interface IAgentPerformanceTracker
{
    Task RecordExecutionAsync(
        AgentId agentId,
        TaskType taskType,
        ExecutionOutcome outcome,
        CancellationToken ct = default);

    Task<AgentPerformanceProfile> GetProfileAsync(
        AgentId agentId,
        CancellationToken ct = default);

    Task<IReadOnlyList<AgentPerformanceProfile>> GetTopPerformersAsync(
        TaskType taskType,
        int limit = 5,
        CancellationToken ct = default);
}

public record ExecutionOutcome
{
    public bool Success { get; init; }
    public TimeSpan Duration { get; init; }
    public int TokensUsed { get; init; }
    public float? QualityScore { get; init; }
    public string? FailureReason { get; init; }
}

public record AgentPerformanceProfile
{
    public AgentId AgentId { get; init; }
    public string AgentName { get; init; } = "";
    public int TotalExecutions { get; init; }
    public float SuccessRate { get; init; }
    public TimeSpan AverageDuration { get; init; }
    public float AverageQualityScore { get; init; }
    public IReadOnlyDictionary<TaskType, TaskTypePerformance> ByTaskType { get; init; } =
        new Dictionary<TaskType, TaskTypePerformance>();
}

/// <summary>
/// Manages agent load and availability.
/// </summary>
public interface ILoadBalancer
{
    Task<float> GetLoadAsync(AgentId agentId, CancellationToken ct = default);
    Task<AgentId?> GetLeastBusyAsync(
        IReadOnlyList<AgentId> candidates,
        CancellationToken ct = default);
    Task UpdateLoadAsync(AgentId agentId, LoadChange change, CancellationToken ct = default);
}
```

### Selection Algorithm

```
For each task in graph:
    1. Filter agents by required capabilities
    2. Score remaining agents:
       - Capability match (40%)
       - Current availability (30%)
       - Historical performance (30%)
    3. Apply selection strategy
    4. If no suitable agent:
       a. Check if spawning is allowed
       b. Identify best agent type to spawn
       c. Queue for spawning
    5. Record assignment
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Manual selection only |
| WriterPro | Auto-selection, 3 strategies |
| Teams | All strategies + performance tracking |
| Enterprise | + Custom strategies + API |

---

## v0.13.3-ORC: Execution Coordinator

**Goal:** Coordinate the execution of task graphs, managing parallel execution, sequencing, checkpoints, and failure recovery.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.3e | Execution Engine | 14 |
| v0.13.3f | Checkpoint Manager | 10 |
| v0.13.3g | Parallel Executor | 10 |
| v0.13.3h | Failure Handler | 10 |
| v0.13.3i | Progress Tracker | 8 |
| v0.13.3j | Execution Monitor UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Coordinates execution of task graphs across agents.
/// </summary>
public interface IExecutionCoordinator
{
    Task<Execution> StartAsync(
        DispatchPlan plan,
        ExecutionOptions options,
        CancellationToken ct = default);

    Task<Execution> ResumeAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    Task PauseAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    Task CancelAsync(
        ExecutionId executionId,
        CancellationReason reason,
        CancellationToken ct = default);

    Task<Execution> GetExecutionAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    IAsyncEnumerable<ExecutionEvent> ObserveAsync(
        ExecutionId executionId,
        CancellationToken ct = default);
}

public readonly record struct ExecutionId(Guid Value)
{
    public static ExecutionId New() => new(Guid.NewGuid());
}

public record ExecutionOptions
{
    public int MaxConcurrency { get; init; } = 5;
    public bool EnableCheckpoints { get; init; } = true;
    public TimeSpan CheckpointInterval { get; init; } = TimeSpan.FromMinutes(5);
    public FailurePolicy FailurePolicy { get; init; } = new();
    public TimeSpan? GlobalTimeout { get; init; }
    public bool PauseOnHumanReview { get; init; } = true;
}

public record FailurePolicy
{
    public FailureAction DefaultAction { get; init; } = FailureAction.Retry;
    public int MaxRetries { get; init; } = 3;
    public TimeSpan RetryBackoff { get; init; } = TimeSpan.FromSeconds(10);
    public bool ContinueOnPartialFailure { get; init; } = true;
    public IReadOnlyDictionary<TaskType, FailureAction>? TaskTypeOverrides { get; init; }
}

public enum FailureAction
{
    Retry,          // Retry the failed task
    Skip,           // Skip and continue
    Reassign,       // Assign to different agent
    Pause,          // Pause for human intervention
    Abort,          // Abort entire execution
    Fallback        // Use fallback strategy
}

/// <summary>
/// Represents an executing or completed orchestration.
/// </summary>
public record Execution
{
    public ExecutionId Id { get; init; }
    public string OriginalIntent { get; init; } = "";
    public ExecutionState State { get; init; }
    public DispatchPlan Plan { get; init; } = null!;
    public DateTimeOffset StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public ExecutionProgress Progress { get; init; } = new();
    public IReadOnlyList<TaskExecution> TaskExecutions { get; init; } = [];
    public IReadOnlyList<ExecutionCheckpoint> Checkpoints { get; init; } = [];
    public ExecutionResult? Result { get; init; }
}

public enum ExecutionState
{
    Pending,
    Running,
    Paused,
    WaitingForHuman,
    Completed,
    Failed,
    Cancelled
}

public record ExecutionProgress
{
    public int TotalTasks { get; init; }
    public int CompletedTasks { get; init; }
    public int FailedTasks { get; init; }
    public int RunningTasks { get; init; }
    public int PendingTasks { get; init; }
    public float PercentComplete => TotalTasks > 0 ? (float)CompletedTasks / TotalTasks * 100 : 0;
    public TimeSpan Elapsed { get; init; }
    public TimeSpan? EstimatedRemaining { get; init; }
}

/// <summary>
/// Execution state for a single task.
/// </summary>
public record TaskExecution
{
    public TaskNodeId TaskId { get; init; }
    public AgentId? ExecutingAgentId { get; init; }
    public TaskExecutionState State { get; init; }
    public DateTimeOffset? StartedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public int AttemptCount { get; init; }
    public TaskOutput? Output { get; init; }
    public string? ErrorMessage { get; init; }
}

public enum TaskExecutionState
{
    Pending,
    Queued,
    Running,
    Completed,
    Failed,
    Skipped,
    Cancelled
}

public record TaskOutput
{
    public required object Value { get; init; }
    public string? ContentType { get; init; }
    public int TokensUsed { get; init; }
    public float? QualityScore { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}

/// <summary>
/// Manages execution checkpoints for recovery.
/// </summary>
public interface ICheckpointManager
{
    Task<ExecutionCheckpoint> CreateCheckpointAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    Task<Execution> RestoreFromCheckpointAsync(
        Guid checkpointId,
        CancellationToken ct = default);

    Task<IReadOnlyList<ExecutionCheckpoint>> GetCheckpointsAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    Task PruneCheckpointsAsync(
        ExecutionId executionId,
        int keepCount = 3,
        CancellationToken ct = default);
}

public record ExecutionCheckpoint
{
    public Guid CheckpointId { get; init; }
    public ExecutionId ExecutionId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public ExecutionState StateAtCheckpoint { get; init; }
    public IReadOnlyDictionary<TaskNodeId, TaskExecution> TaskStates { get; init; } =
        new Dictionary<TaskNodeId, TaskExecution>();
    public IReadOnlyDictionary<TaskNodeId, TaskOutput> CompletedOutputs { get; init; } =
        new Dictionary<TaskNodeId, TaskOutput>();
    public long SizeBytes { get; init; }
}

/// <summary>
/// Events emitted during execution.
/// </summary>
public abstract record ExecutionEvent
{
    public ExecutionId ExecutionId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record TaskStartedEvent : ExecutionEvent
{
    public TaskNodeId TaskId { get; init; }
    public AgentId AgentId { get; init; }
}

public record TaskCompletedEvent : ExecutionEvent
{
    public TaskNodeId TaskId { get; init; }
    public TaskOutput Output { get; init; } = null!;
}

public record TaskFailedEvent : ExecutionEvent
{
    public TaskNodeId TaskId { get; init; }
    public string Error { get; init; } = "";
    public FailureAction ActionTaken { get; init; }
}

public record CheckpointCreatedEvent : ExecutionEvent
{
    public Guid CheckpointId { get; init; }
}

public record HumanReviewRequestedEvent : ExecutionEvent
{
    public TaskNodeId TaskId { get; init; }
    public string ReviewPrompt { get; init; } = "";
}
```

### Execution Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                     Execution Coordinator                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. Initialize execution from dispatch plan                      │
│  2. Identify ready tasks (no pending dependencies)               │
│  3. For each ready task (up to max concurrency):                │
│     a. Dispatch to assigned agent                                │
│     b. Monitor progress                                          │
│     c. Handle completion/failure                                 │
│  4. Update dependency graph                                      │
│  5. Create checkpoint if interval elapsed                        │
│  6. Repeat until all tasks complete or failure                   │
│  7. Aggregate results                                            │
│                                                                  │
│  ┌─────────┐    ┌─────────┐    ┌─────────┐    ┌─────────┐      │
│  │ Task A  │───▶│ Task B  │───▶│ Task D  │───▶│ Task E  │      │
│  └─────────┘    └─────────┘    └─────────┘    └─────────┘      │
│                      │                                           │
│                      ▼                                           │
│                 ┌─────────┐                                      │
│                 │ Task C  │ (parallel with B→D)                  │
│                 └─────────┘                                      │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Sequential only, no checkpoints |
| WriterPro | + Parallel (3), checkpoints |
| Teams | + Full concurrency, recovery |
| Enterprise | Unlimited + distributed execution |

---

## v0.13.4-ORC: Result Aggregation & Synthesis

**Goal:** Merge outputs from multiple agents into coherent results, resolve conflicts, and assess quality.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.4e | Output Merger | 12 |
| v0.13.4f | Conflict Resolver | 10 |
| v0.13.4g | Quality Assessor | 10 |
| v0.13.4h | Synthesis Engine | 10 |
| v0.13.4i | Human Handoff | 6 |
| v0.13.4j | Results Dashboard UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Aggregates and synthesizes results from multiple agents.
/// </summary>
public interface IResultAggregator
{
    Task<AggregationResult> AggregateAsync(
        Execution execution,
        AggregationOptions options,
        CancellationToken ct = default);

    Task<SynthesisResult> SynthesizeAsync(
        AggregationResult aggregation,
        SynthesisOptions options,
        CancellationToken ct = default);
}

public record AggregationOptions
{
    public ConflictResolutionStrategy ConflictStrategy { get; init; } =
        ConflictResolutionStrategy.BestQuality;
    public bool IncludeProvenance { get; init; } = true;
    public float MinQualityThreshold { get; init; } = 0.7f;
    public bool RequireHumanReview { get; init; } = false;
}

public enum ConflictResolutionStrategy
{
    BestQuality,    // Use highest quality output
    MostRecent,     // Use latest output
    Consensus,      // Find common ground
    Manual,         // Flag for human
    Merge           // Attempt to merge
}

public record AggregationResult
{
    public ExecutionId ExecutionId { get; init; }
    public IReadOnlyList<OutputChunk> Chunks { get; init; } = [];
    public IReadOnlyList<Conflict> Conflicts { get; init; } = [];
    public IReadOnlyList<Conflict> ResolvedConflicts { get; init; } = [];
    public IReadOnlyList<Conflict> UnresolvedConflicts { get; init; } = [];
    public QualityAssessment Quality { get; init; } = new();
}

public record OutputChunk
{
    public Guid Id { get; init; }
    public TaskNodeId SourceTaskId { get; init; }
    public AgentId ProducingAgentId { get; init; }
    public required object Content { get; init; }
    public string ContentType { get; init; } = "text/markdown";
    public float QualityScore { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
    public ChunkProvenance Provenance { get; init; } = new();
}

public record ChunkProvenance
{
    public TaskNodeId TaskId { get; init; }
    public AgentId AgentId { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
    public IReadOnlyList<Guid>? SourceDocumentIds { get; init; }
    public IReadOnlyList<Guid>? SourceEntityIds { get; init; }
}

/// <summary>
/// Represents a conflict between agent outputs.
/// </summary>
public record Conflict
{
    public Guid ConflictId { get; init; } = Guid.NewGuid();
    public ConflictType Type { get; init; }
    public IReadOnlyList<OutputChunk> ConflictingChunks { get; init; } = [];
    public string Description { get; init; } = "";
    public ConflictSeverity Severity { get; init; }
    public ConflictResolution? Resolution { get; init; }
}

public enum ConflictType
{
    Factual,        // Different facts claimed
    Stylistic,      // Different writing styles
    Structural,     // Different organizations
    Terminology,    // Different terms used
    Overlap,        // Duplicate content
    Gap             // Missing expected content
}

public enum ConflictSeverity { Low, Medium, High, Critical }

public record ConflictResolution
{
    public ConflictResolutionMethod Method { get; init; }
    public OutputChunk? SelectedChunk { get; init; }
    public string? MergedContent { get; init; }
    public string Justification { get; init; } = "";
    public bool RequiresHumanReview { get; init; }
}

public enum ConflictResolutionMethod
{
    Selected,       // One chunk selected
    Merged,         // Chunks merged
    Deferred,       // Deferred to human
    Discarded       // All conflicting chunks discarded
}

/// <summary>
/// Assesses quality of aggregated outputs.
/// </summary>
public interface IQualityAssessor
{
    Task<QualityAssessment> AssessAsync(
        AggregationResult aggregation,
        QualityOptions options,
        CancellationToken ct = default);
}

public record QualityAssessment
{
    public float OverallScore { get; init; }
    public float CompletenessScore { get; init; }
    public float ConsistencyScore { get; init; }
    public float StyleComplianceScore { get; init; }
    public float AccuracyScore { get; init; }
    public IReadOnlyList<QualityIssue> Issues { get; init; } = [];
    public IReadOnlyList<string> Suggestions { get; init; } = [];
}

public record QualityIssue
{
    public QualityDimension Dimension { get; init; }
    public IssueSeverity Severity { get; init; }
    public string Description { get; init; } = "";
    public OutputChunk? AffectedChunk { get; init; }
    public string? SuggestedFix { get; init; }
}

public enum QualityDimension
{
    Completeness, Accuracy, Consistency, Style, Clarity, Structure
}

/// <summary>
/// Synthesizes final output from aggregated chunks.
/// </summary>
public interface ISynthesisEngine
{
    Task<SynthesisResult> SynthesizeAsync(
        AggregationResult aggregation,
        SynthesisOptions options,
        CancellationToken ct = default);
}

public record SynthesisOptions
{
    public SynthesisStrategy Strategy { get; init; } = SynthesisStrategy.Intelligent;
    public string? OutputFormat { get; init; }
    public bool ApplyStyleGuide { get; init; } = true;
    public bool IncludeSourceCitations { get; init; } = true;
}

public enum SynthesisStrategy
{
    Concatenate,    // Simple concatenation
    Interleave,     // Interleave by section
    Intelligent,    // AI-assisted synthesis
    Template        // Fill template
}

public record SynthesisResult
{
    public required object FinalOutput { get; init; }
    public string ContentType { get; init; } = "text/markdown";
    public QualityAssessment Quality { get; init; } = new();
    public IReadOnlyList<ChunkProvenance> Sources { get; init; } = [];
    public SynthesisMetadata Metadata { get; init; } = new();
}

public record SynthesisMetadata
{
    public int ChunksUsed { get; init; }
    public int ChunksDiscarded { get; init; }
    public int ConflictsResolved { get; init; }
    public TimeSpan SynthesisTime { get; init; }
}
```

### Quality Dimensions

| Dimension | What It Measures | Assessment Method |
|:----------|:-----------------|:------------------|
| Completeness | All required sections present | Checklist validation |
| Accuracy | Factual correctness | CKVS validation |
| Consistency | Internal consistency | Cross-reference check |
| Style | Adherence to style guide | Style engine scoring |
| Clarity | Readability and clarity | Readability metrics |
| Structure | Proper organization | Structural analysis |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic concatenation |
| WriterPro | + Conflict detection |
| Teams | + Auto resolution + quality |
| Enterprise | + AI synthesis + full provenance |

---

## v0.13.5-ORC: Orchestration Patterns & Templates

**Goal:** Provide reusable orchestration patterns and a visual interface for designing and monitoring agent ensembles.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.5e | Pattern Library | 10 |
| v0.13.5f | Ensemble Templates | 10 |
| v0.13.5g | Pattern Designer | 12 |
| v0.13.5h | Workflow Persistence | 8 |
| v0.13.5i | Conductor Dashboard | 10 |
| v0.13.5j | Ensemble Marketplace | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Manages reusable orchestration patterns.
/// </summary>
public interface IPatternLibrary
{
    Task<IReadOnlyList<OrchestrationPattern>> GetPatternsAsync(
        PatternCategory? category = null,
        CancellationToken ct = default);

    Task<OrchestrationPattern?> GetPatternAsync(
        string patternId,
        CancellationToken ct = default);

    Task<Execution> ApplyPatternAsync(
        string patternId,
        PatternParameters parameters,
        CancellationToken ct = default);

    Task<string> SaveAsPatternAsync(
        Execution execution,
        PatternMetadata metadata,
        CancellationToken ct = default);
}

/// <summary>
/// A reusable orchestration pattern.
/// </summary>
public record OrchestrationPattern
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public PatternCategory Category { get; init; }
    public required TaskGraphTemplate GraphTemplate { get; init; }
    public IReadOnlyList<PatternParameter> Parameters { get; init; } = [];
    public IReadOnlyList<string> RequiredAgentTypes { get; init; } = [];
    public PatternMetrics? Metrics { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

public enum PatternCategory
{
    Documentation,      // Doc generation patterns
    Research,          // Information gathering
    Analysis,          // Content analysis
    Transformation,    // Content transformation
    Review,            // Review workflows
    Publishing,        // Publishing workflows
    Custom             // User-defined
}

public record TaskGraphTemplate
{
    public IReadOnlyList<TaskNodeTemplate> Nodes { get; init; } = [];
    public IReadOnlyList<TaskEdge> Edges { get; init; } = [];
}

public record TaskNodeTemplate
{
    public required string TemplateId { get; init; }
    public required string Name { get; init; }
    public required TaskType Type { get; init; }
    public string ObjectiveTemplate { get; init; } = "";  // With {parameter} placeholders
    public IReadOnlyList<CapabilityCategory> RequiredCapabilities { get; init; } = [];
    public string? PreferredAgentType { get; init; }
}

/// <summary>
/// Pre-built ensemble configurations.
/// </summary>
public interface IEnsembleTemplates
{
    Task<Ensemble> CreateEnsembleAsync(
        string templateId,
        EnsembleParameters parameters,
        CancellationToken ct = default);

    Task<IReadOnlyList<EnsembleTemplate>> GetTemplatesAsync(
        CancellationToken ct = default);
}

public record EnsembleTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<AgentRole> Roles { get; init; } = [];
    public OrchestrationPattern DefaultPattern { get; init; } = null!;
    public EnsembleCapabilities Capabilities { get; init; } = new();
}

public record AgentRole
{
    public required string RoleId { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string AgentType { get; init; }
    public int MinInstances { get; init; } = 1;
    public int MaxInstances { get; init; } = 1;
    public IReadOnlyList<string> Responsibilities { get; init; } = [];
}

/// <summary>
/// A configured ensemble of agents.
/// </summary>
public record Ensemble
{
    public Guid EnsembleId { get; init; } = Guid.NewGuid();
    public required string Name { get; init; }
    public string TemplateId { get; init; } = "";
    public IReadOnlyList<EnsembleMember> Members { get; init; } = [];
    public EnsembleState State { get; init; }
    public OrchestrationPattern? ActivePattern { get; init; }
}

public record EnsembleMember
{
    public string RoleId { get; init; } = "";
    public AgentId AgentId { get; init; }
    public AgentState State { get; init; }
}

public enum EnsembleState { Inactive, Activating, Ready, Working, Paused }

/// <summary>
/// Visual workflow designer.
/// </summary>
public interface IWorkflowDesigner
{
    Task<WorkflowDefinition> LoadAsync(
        Guid workflowId,
        CancellationToken ct = default);

    Task<Guid> SaveAsync(
        WorkflowDefinition workflow,
        CancellationToken ct = default);

    Task<ValidationResult> ValidateAsync(
        WorkflowDefinition workflow,
        CancellationToken ct = default);

    Task<Execution> ExecuteAsync(
        Guid workflowId,
        IReadOnlyDictionary<string, object>? inputs = null,
        CancellationToken ct = default);
}

public record WorkflowDefinition
{
    public Guid Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required TaskGraph Graph { get; init; }
    public IReadOnlyList<WorkflowParameter> Parameters { get; init; } = [];
    public IReadOnlyList<WorkflowTrigger>? Triggers { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ModifiedAt { get; init; }
}

public record WorkflowTrigger
{
    public TriggerType Type { get; init; }
    public string? Schedule { get; init; }  // Cron for scheduled
    public string? EventType { get; init; }  // For event-based
    public IReadOnlyDictionary<string, object>? Conditions { get; init; }
}

public enum TriggerType { Manual, Scheduled, Event, Webhook }
```

### Built-in Patterns

| Pattern | Category | Description |
|:--------|:---------|:------------|
| `api-docs-generator` | Documentation | Generate API docs from OpenAPI spec |
| `release-notes` | Documentation | Generate release notes from Git history |
| `doc-updater` | Transformation | Update docs based on code changes |
| `terminology-audit` | Analysis | Audit terminology compliance |
| `translation-pipeline` | Transformation | Translate and validate |
| `research-and-write` | Documentation | Research topic and write article |
| `review-and-publish` | Publishing | Multi-stage review workflow |

### Built-in Ensembles

| Ensemble | Roles | Use Case |
|:---------|:------|:---------|
| Technical Writing Team | Researcher, Writer, Editor, Validator | Full documentation workflow |
| API Documentation | Scribe, Validator | API reference generation |
| Release Management | Chronicler, Formatter, Publisher | Release notes pipeline |
| Content Review | Analyzer, Reviewer, Editor | Quality assurance |

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | 3 built-in patterns |
| WriterPro | + 5 custom patterns |
| Teams | + Ensembles + designer |
| Enterprise | Unlimited + marketplace |

---

## Dependencies on Prior Versions

| Component | Source | Usage in v0.13.x |
|:----------|:-------|:-----------------|
| `IAgentRegistry` | v0.12.1-AGT | Agent discovery |
| `IAgentLifecycleManager` | v0.12.2-AGT | Agent spawning |
| `IAgentMessageBus` | v0.12.3-AGT | Agent communication |
| `IAgentMemory` | v0.12.4-AGT | Execution state |
| `IToolExecutor` | v0.12.5-AGT | Tool invocation |
| `IChatCompletionService` | v0.6.1a | Decomposition LLM |
| `IValidationEngine` | v0.6.5-KG | Quality validation |
| `IStyleChecker` | v0.2.3 | Style compliance |

---

## MediatR Events Introduced

| Event | Version | Description |
|:------|:--------|:------------|
| `IntentDecomposedEvent` | v0.13.1 | Intent parsed into task graph |
| `TaskGraphCreatedEvent` | v0.13.1 | Task graph ready |
| `DispatchPlanCreatedEvent` | v0.13.2 | Agents assigned to tasks |
| `AgentDispatchedEvent` | v0.13.2 | Agent started on task |
| `ExecutionStartedEvent` | v0.13.3 | Orchestration began |
| `ExecutionCompletedEvent` | v0.13.3 | Orchestration finished |
| `CheckpointSavedEvent` | v0.13.3 | Recovery checkpoint created |
| `ConflictDetectedEvent` | v0.13.4 | Output conflict found |
| `ResultSynthesizedEvent` | v0.13.4 | Final output generated |
| `PatternAppliedEvent` | v0.13.5 | Pattern used |
| `WorkflowSavedEvent` | v0.13.5 | Custom workflow saved |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `DotNetGraph` | 3.x | Graph visualization |
| `Cronos` | 0.8.x | Workflow scheduling |
| `BlazorDiagram` | 3.x | Visual workflow designer |

---

## Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Intent decomposition | <2s |
| Dispatch planning | <500ms |
| Checkpoint save | <1s |
| Quality assessment | <3s |
| Result synthesis | <5s |

---

## What This Enables

With v0.13.x complete, Lexichord becomes a true agentic orchestration platform:

- **Intent Understanding:** Natural language commands decomposed intelligently
- **Smart Delegation:** Tasks matched to best-suited agents automatically
- **Reliable Execution:** Parallel execution with failure recovery
- **Quality Assurance:** Conflicts detected and resolved, quality assessed
- **Reusability:** Patterns and ensembles for common workflows
- **Visibility:** Full observability into orchestration progress

This enables the visual Agent Studio in v0.14.x.

---

## Total Orchestration Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 7: Orchestration | v0.13.1 - v0.13.5 | ~276 |

**Combined with prior phases:** ~1,220 hours (~30 person-months)

---
