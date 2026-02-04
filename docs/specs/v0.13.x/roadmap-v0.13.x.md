# Lexichord Agent Orchestration Roadmap (v0.13.1 - v0.13.7)

In v0.12.x, we established **Agent Infrastructure** — the foundational building blocks for agents including definitions, lifecycle management, communication, memory, and tools. In v0.13.x, we deliver **The Orchestration Engine** — the intelligent conductor that coordinates multiple agents to accomplish complex, multi-step goals, with **Interactive Planning & Approval Workflows** that keep users in control.

**Architectural Note:** This version introduces the `Lexichord.Orchestration` module, implementing the "Composer" vision from the original design proposal. The orchestrator elevates users from authors to conductors, delegating complex documentation tasks to coordinated agent ensembles. v0.13.6 and v0.13.7 add critical human-in-the-loop capabilities — users can review, comment on, and approve AI-generated plans before execution, and dynamically manage agent lifecycles during orchestration.

**Total Sub-Parts:** 53 distinct implementation steps across 7 versions.
**Total Estimated Hours:** 396 hours (~9.9 person-months)

---

## Version Overview

| Version | Codename | Focus | Est. Hours |
|:--------|:---------|:------|:-----------|
| v0.13.1-ORC | Task Decomposition Engine | Intent parsing, task breakdown, dependency graphs | 56 |
| v0.13.2-ORC | Agent Selection & Dispatch | Capability matching, load balancing, delegation | 52 |
| v0.13.3-ORC | Execution Coordinator | Parallel execution, sequencing, checkpoints | 58 |
| v0.13.4-ORC | Result Aggregation & Synthesis | Merge outputs, resolve conflicts, quality scoring | 54 |
| v0.13.5-ORC | Orchestration Patterns & Templates | Workflows, ensembles, conductor UI | 56 |
| v0.13.6-ORC | **Planning & Approval Workflows** | **Interactive plan review, commenting, approval gates** | **62** |
| v0.13.7-ORC | **Dynamic Agent Orchestration** | **Agent creation, status monitoring, lifecycle management** | **58** |

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
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │              ★ PLANNING & APPROVAL WORKFLOW (v0.13.6) ★               │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐    │  │
│  │  │  Plan Display   │─▶│ User Comments   │─▶│  Approval Decision  │    │  │
│  │  │  & Estimation   │  │ & Modifications │  │  Approve / Reject   │    │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
│                                  │                                           │
│                                  ▼                                           │
│         ┌────────────────────────────────────────────────────────┐          │
│         │            Agent Selection & Dispatch                   │          │
│         │                    (v0.13.2)                           │          │
│         └────────────────────────┬───────────────────────────────┘          │
│                                  │                                           │
│  ┌───────────────────────────────────────────────────────────────────────┐  │
│  │            ★ DYNAMIC AGENT ORCHESTRATION (v0.13.7) ★                  │  │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐    │  │
│  │  │  Agent Creation │  │ Status Monitor  │  │  Lifecycle Control  │    │  │
│  │  │  for Sub-tasks  │  │  & Health Check │  │  Pause/Resume/Stop  │    │  │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘    │  │
│  └───────────────────────────────────────────────────────────────────────┘  │
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

## v0.13.6-ORC: Planning & Approval Workflows

**Goal:** Implement interactive human-in-the-loop workflows where AI-generated plans are presented to users for review, commenting, modification, and approval before execution proceeds.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.6a | Plan Presentation Engine | 10 |
| v0.13.6b | Interactive Plan Editor | 12 |
| v0.13.6c | Comment & Annotation System | 10 |
| v0.13.6d | Approval Gate Manager | 10 |
| v0.13.6e | Plan Versioning & History | 8 |
| v0.13.6f | Approval Notification System | 6 |
| v0.13.6g | Plan Review Dashboard UI | 6 |

### Key Interfaces

```csharp
/// <summary>
/// Manages the planning and approval workflow for orchestration tasks.
/// Presents AI-generated plans to users and collects feedback before execution.
/// </summary>
public interface IPlanApprovalService
{
    /// <summary>
    /// Submit a decomposition result for user approval.
    /// </summary>
    Task<PlanReviewSession> SubmitForApprovalAsync(
        DecompositionResult decomposition,
        ApprovalRequestOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get the current state of a plan review session.
    /// </summary>
    Task<PlanReviewSession> GetSessionAsync(
        PlanReviewSessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Wait for approval decision (blocks until user acts).
    /// </summary>
    Task<ApprovalDecision> WaitForDecisionAsync(
        PlanReviewSessionId sessionId,
        TimeSpan? timeout = null,
        CancellationToken ct = default);

    /// <summary>
    /// Submit user approval for a plan.
    /// </summary>
    Task<ApprovalResult> ApproveAsync(
        PlanReviewSessionId sessionId,
        ApprovalSubmission submission,
        CancellationToken ct = default);

    /// <summary>
    /// Submit user rejection for a plan.
    /// </summary>
    Task<RejectionResult> RejectAsync(
        PlanReviewSessionId sessionId,
        RejectionSubmission submission,
        CancellationToken ct = default);

    /// <summary>
    /// Request plan modification based on user feedback.
    /// </summary>
    Task<DecompositionResult> RequestModificationAsync(
        PlanReviewSessionId sessionId,
        PlanModificationRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of approval events.
    /// </summary>
    IObservable<PlanApprovalEvent> ApprovalEvents { get; }
}

public readonly record struct PlanReviewSessionId(Guid Value)
{
    public static PlanReviewSessionId New() => new(Guid.NewGuid());
}

/// <summary>
/// Options for submitting a plan for approval.
/// </summary>
public record ApprovalRequestOptions
{
    /// <summary>
    /// Whether approval is required before execution.
    /// </summary>
    public bool RequireExplicitApproval { get; init; } = true;

    /// <summary>
    /// How long to wait for approval before timing out.
    /// </summary>
    public TimeSpan? ApprovalTimeout { get; init; }

    /// <summary>
    /// Action to take if approval times out.
    /// </summary>
    public TimeoutAction TimeoutAction { get; init; } = TimeoutAction.Cancel;

    /// <summary>
    /// Users who can approve this plan.
    /// </summary>
    public IReadOnlyList<UserId>? AuthorizedApprovers { get; init; }

    /// <summary>
    /// Whether to allow partial approval (approve some tasks, reject others).
    /// </summary>
    public bool AllowPartialApproval { get; init; } = true;

    /// <summary>
    /// Whether to show cost/resource estimates to user.
    /// </summary>
    public bool ShowEstimates { get; init; } = true;

    /// <summary>
    /// Whether to allow user comments on individual tasks.
    /// </summary>
    public bool AllowTaskComments { get; init; } = true;

    /// <summary>
    /// Notification channels for approval request.
    /// </summary>
    public IReadOnlyList<NotificationChannel>? NotifyOn { get; init; }
}

public enum TimeoutAction
{
    Cancel,     // Cancel the plan entirely
    Proceed,    // Proceed with default approval
    Escalate,   // Escalate to another approver
    Remind      // Send reminder and extend timeout
}

/// <summary>
/// Represents an active plan review session.
/// </summary>
public record PlanReviewSession
{
    public PlanReviewSessionId Id { get; init; }
    public DecompositionResult Plan { get; init; } = null!;
    public PlanReviewState State { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public UserId? AssignedReviewer { get; init; }
    public IReadOnlyList<PlanComment> Comments { get; init; } = [];
    public IReadOnlyList<TaskApprovalStatus> TaskStatuses { get; init; } = [];
    public IReadOnlyList<PlanVersion> Versions { get; init; } = [];
    public ApprovalDecision? Decision { get; init; }
    public PlanEstimate Estimate { get; init; } = new();
}

public enum PlanReviewState
{
    Pending,            // Waiting for review
    InReview,           // User is actively reviewing
    ModificationRequested, // User requested changes
    Approved,           // Plan approved
    PartiallyApproved,  // Some tasks approved
    Rejected,           // Plan rejected
    TimedOut,           // Approval window expired
    Cancelled           // Session cancelled
}

/// <summary>
/// Estimate information displayed to users during review.
/// </summary>
public record PlanEstimate
{
    public int TotalTasks { get; init; }
    public int EstimatedTokens { get; init; }
    public TimeSpan EstimatedDuration { get; init; }
    public int RequiredAgents { get; init; }
    public decimal? EstimatedCost { get; init; }
    public IReadOnlyList<string> ResourceRequirements { get; init; } = [];
    public IReadOnlyList<string> RiskFactors { get; init; } = [];
    public float ConfidenceScore { get; init; }
}

/// <summary>
/// A comment or annotation on a plan or task.
/// </summary>
public record PlanComment
{
    public Guid CommentId { get; init; } = Guid.NewGuid();
    public UserId Author { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ModifiedAt { get; init; }
    public string Content { get; init; } = "";
    public CommentScope Scope { get; init; }
    public TaskNodeId? TargetTaskId { get; init; }
    public CommentType Type { get; init; }
    public IReadOnlyList<Guid>? ReplyToIds { get; init; }
    public bool IsResolved { get; init; }
}

public enum CommentScope
{
    Plan,       // Comment on the entire plan
    Task,       // Comment on a specific task
    Edge,       // Comment on a dependency relationship
    Estimate    // Comment on resource estimates
}

public enum CommentType
{
    General,        // General feedback
    Question,       // Asking for clarification
    Concern,        // Expressing concern
    Suggestion,     // Suggesting improvement
    Approval,       // Noting approval
    Rejection,      // Noting rejection reason
    Modification    // Requesting specific change
}

/// <summary>
/// Approval status for an individual task.
/// </summary>
public record TaskApprovalStatus
{
    public TaskNodeId TaskId { get; init; }
    public TaskApprovalState State { get; init; }
    public UserId? ApprovedBy { get; init; }
    public DateTimeOffset? DecidedAt { get; init; }
    public string? Notes { get; init; }
    public IReadOnlyList<PlanComment> Comments { get; init; } = [];
}

public enum TaskApprovalState
{
    Pending,
    Approved,
    Rejected,
    Modified,
    Skipped
}

/// <summary>
/// A version of the plan (for tracking modifications).
/// </summary>
public record PlanVersion
{
    public int VersionNumber { get; init; }
    public DecompositionResult Plan { get; init; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
    public string ChangeDescription { get; init; } = "";
    public UserId? ModifiedBy { get; init; }
    public IReadOnlyList<PlanChange> Changes { get; init; } = [];
}

public record PlanChange
{
    public PlanChangeType Type { get; init; }
    public TaskNodeId? AffectedTaskId { get; init; }
    public string Description { get; init; } = "";
    public object? OldValue { get; init; }
    public object? NewValue { get; init; }
}

public enum PlanChangeType
{
    TaskAdded,
    TaskRemoved,
    TaskModified,
    DependencyAdded,
    DependencyRemoved,
    StrategyChanged,
    EstimateUpdated
}

/// <summary>
/// User's decision on a plan.
/// </summary>
public record ApprovalDecision
{
    public ApprovalOutcome Outcome { get; init; }
    public UserId DecidedBy { get; init; }
    public DateTimeOffset DecidedAt { get; init; }
    public string? Reason { get; init; }
    public IReadOnlyList<TaskNodeId>? ApprovedTasks { get; init; }
    public IReadOnlyList<TaskNodeId>? RejectedTasks { get; init; }
    public IReadOnlyList<string>? Conditions { get; init; }
}

public enum ApprovalOutcome
{
    Approved,
    PartiallyApproved,
    Rejected,
    ModificationRequested,
    Deferred,
    TimedOut
}

/// <summary>
/// Submission for approving a plan.
/// </summary>
public record ApprovalSubmission
{
    public UserId ApproverId { get; init; }
    public string? ApprovalNotes { get; init; }
    public IReadOnlyList<TaskNodeId>? TasksToSkip { get; init; }
    public IReadOnlyList<string>? Conditions { get; init; }
    public bool AcknowledgeRisks { get; init; }
}

/// <summary>
/// Submission for rejecting a plan.
/// </summary>
public record RejectionSubmission
{
    public UserId RejecterId { get; init; }
    public required string Reason { get; init; }
    public RejectionAction NextAction { get; init; } = RejectionAction.Cancel;
    public string? AlternativeSuggestion { get; init; }
}

public enum RejectionAction
{
    Cancel,             // Cancel entirely
    RequestNewPlan,     // Ask for a new decomposition
    ModifyAndResubmit   // User will modify and resubmit
}

/// <summary>
/// Request to modify the plan based on user feedback.
/// </summary>
public record PlanModificationRequest
{
    public UserId RequestedBy { get; init; }
    public string? FreeformFeedback { get; init; }
    public IReadOnlyList<TaskModification>? TaskModifications { get; init; }
    public IReadOnlyList<DependencyModification>? DependencyModifications { get; init; }
    public DecompositionStrategy? PreferredStrategy { get; init; }
    public ComplexityBudget? RevisedBudget { get; init; }
}

public record TaskModification
{
    public TaskNodeId? TaskId { get; init; }  // null for new task
    public ModificationType Type { get; init; }
    public string? NewName { get; init; }
    public string? NewDescription { get; init; }
    public TaskObjective? NewObjective { get; init; }
    public string? UserNote { get; init; }
}

public enum ModificationType
{
    Add,
    Remove,
    UpdateObjective,
    UpdateConstraints,
    ChangePriority,
    Split,      // Split into multiple tasks
    Merge       // Merge with another task
}

public record DependencyModification
{
    public TaskNodeId FromTaskId { get; init; }
    public TaskNodeId ToTaskId { get; init; }
    public DependencyModificationType Type { get; init; }
}

public enum DependencyModificationType
{
    Add,
    Remove,
    ChangeType
}

/// <summary>
/// Events published during the approval workflow.
/// </summary>
public abstract record PlanApprovalEvent
{
    public PlanReviewSessionId SessionId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record PlanSubmittedForApprovalEvent : PlanApprovalEvent
{
    public DecompositionResult Plan { get; init; } = null!;
}

public record PlanReviewStartedEvent : PlanApprovalEvent
{
    public UserId ReviewerId { get; init; }
}

public record PlanCommentAddedEvent : PlanApprovalEvent
{
    public PlanComment Comment { get; init; } = null!;
}

public record PlanModificationRequestedEvent : PlanApprovalEvent
{
    public PlanModificationRequest Request { get; init; } = null!;
}

public record PlanVersionCreatedEvent : PlanApprovalEvent
{
    public PlanVersion Version { get; init; } = null!;
}

public record PlanApprovedEvent : PlanApprovalEvent
{
    public ApprovalDecision Decision { get; init; } = null!;
}

public record PlanRejectedEvent : PlanApprovalEvent
{
    public RejectionSubmission Rejection { get; init; } = null!;
}

public record ApprovalTimeoutEvent : PlanApprovalEvent
{
    public TimeoutAction ActionTaken { get; init; }
}
```

### Plan Review UI Mockup

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Plan Review: "Document Auth v2 Flow"                    [Session #a1b2c3] │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ Status: ⏳ Awaiting Your Approval                                          │
│ Submitted: 2 minutes ago | Expires in: 28 minutes                          │
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ ESTIMATES                                                                ││
│ │ ├── Tasks: 7 total (3 research, 2 generation, 1 validation, 1 review)  ││
│ │ ├── Tokens: ~18,500 estimated                                          ││
│ │ ├── Duration: ~8 minutes                                               ││
│ │ ├── Agents: 3 required (Chronicler, Scribe, Validator)                 ││
│ │ ├── Confidence: 87%                                                    ││
│ │ └── Risk Factors: Large dataset, multiple visualization types          ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ TASK GRAPH:                                                                │
│ ═══════════════════════════════════════════════════════════════════════════│
│                                                                             │
│  [✓] Task 1: Fetch Q4 Sales Data              Research    [Approve][Skip] │
│      └── Retrieve all sales transactions for Oct-Dec 2025                  │
│          💬 2 comments                                                     │
│              │                                                              │
│              ▼                                                              │
│  [?] Task 2: Aggregate and Validate Data      Analysis    [Approve][Skip] │
│      └── Clean, validate, and aggregate Q4 sales data                      │
│          💬 Add comment...                                                 │
│              │                                                              │
│       ┌──────┴──────┐                                                      │
│       ▼             ▼                                                      │
│  [?] Task 3     [?] Task 4                                                 │
│  Analyze        Create                                                     │
│  Trends         Charts                                                     │
│       │             │                                                      │
│       └──────┬──────┘                                                      │
│              ▼                                                              │
│  [?] Task 5: Generate Report                  Generation  [Approve][Skip] │
│      └── Create comprehensive Q4 sales analysis report                     │
│              │                                                              │
│              ▼                                                              │
│  [?] Task 6: Email Team                       Integration [Approve][Skip] │
│                                                                             │
│ COMMENTS (2):                                                              │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ 👤 You (on Task 1):                                                     ││
│ │ "Make sure to include the APAC region data which was added in November" ││
│ │ [Reply] [Resolve]                                                       ││
│ │                                                                          ││
│ │ 🤖 AI Response:                                                         ││
│ │ "Understood. I'll ensure APAC region data is included in the query."    ││
│ │ [✓ Resolved]                                                            ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ Add Overall Comment:                                                     ││
│ │ ┌───────────────────────────────────────────────────────────────────┐   ││
│ │ │                                                                    │   ││
│ │ └───────────────────────────────────────────────────────────────────┘   ││
│ │ [Post Comment]                                                          ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ [✓ Approve Plan]  [✗ Reject]  [✎ Request Changes]  [⏸ Save for Later] ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

### Approval Workflow State Machine

```
                    ┌─────────────┐
                    │   Pending   │
                    └──────┬──────┘
                           │ User opens review
                           ▼
                    ┌─────────────┐
            ┌───────│  In Review  │───────┐
            │       └──────┬──────┘       │
            │              │              │
     Request│              │Approve       │Reject
   Modification            │              │
            │              ▼              ▼
            │       ┌─────────────┐ ┌───────────┐
            └──────▶│  Modified   │ │  Rejected │
                    └──────┬──────┘ └───────────┘
                           │
                    Resubmit│
                           ▼
                    ┌─────────────┐
                    │  Approved   │───────▶ Begin Execution
                    └─────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Basic approve/reject only |
| WriterPro | + Comments, partial approval |
| Teams | + Plan versioning, multiple approvers |
| Enterprise | + Custom approval workflows, SLAs |

---

## v0.13.7-ORC: Dynamic Agent Orchestration

**Goal:** Enable dynamic creation, monitoring, and lifecycle management of agents during orchestration, allowing users to spawn agents for sub-tasks, review their status, and terminate them when necessary.

### Sub-Parts

| Sub-Part | Title | Est. Hours |
|:---------|:------|:-----------|
| v0.13.7a | Dynamic Agent Spawner | 12 |
| v0.13.7b | Agent Status Monitor | 10 |
| v0.13.7c | Agent Health Checker | 8 |
| v0.13.7d | Lifecycle Controller | 10 |
| v0.13.7e | Resource Quota Manager | 8 |
| v0.13.7f | Agent Control Dashboard UI | 10 |

### Key Interfaces

```csharp
/// <summary>
/// Manages dynamic agent creation and orchestration during execution.
/// </summary>
public interface IDynamicAgentOrchestrator
{
    /// <summary>
    /// Spawn a new agent for a specific task or sub-task.
    /// </summary>
    Task<SpawnedAgent> SpawnAgentAsync(
        AgentSpawnRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Spawn multiple agents in parallel.
    /// </summary>
    Task<IReadOnlyList<SpawnedAgent>> SpawnAgentBatchAsync(
        IReadOnlyList<AgentSpawnRequest> requests,
        CancellationToken ct = default);

    /// <summary>
    /// Get status of all active agents in an execution.
    /// </summary>
    Task<IReadOnlyList<AgentStatus>> GetActiveAgentsAsync(
        ExecutionId executionId,
        CancellationToken ct = default);

    /// <summary>
    /// Get detailed status of a specific agent.
    /// </summary>
    Task<AgentStatus> GetAgentStatusAsync(
        AgentId agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Pause an agent's execution.
    /// </summary>
    Task PauseAgentAsync(
        AgentId agentId,
        string? reason = null,
        CancellationToken ct = default);

    /// <summary>
    /// Resume a paused agent.
    /// </summary>
    Task ResumeAgentAsync(
        AgentId agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Terminate an agent.
    /// </summary>
    Task TerminateAgentAsync(
        AgentId agentId,
        TerminationReason reason,
        CancellationToken ct = default);

    /// <summary>
    /// Reassign a task from one agent to another.
    /// </summary>
    Task<AgentId> ReassignTaskAsync(
        TaskNodeId taskId,
        AgentId? preferredAgentId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of agent lifecycle events.
    /// </summary>
    IObservable<AgentLifecycleEvent> LifecycleEvents { get; }
}

/// <summary>
/// Request to spawn a new agent.
/// </summary>
public record AgentSpawnRequest
{
    /// <summary>
    /// Type of agent to spawn.
    /// </summary>
    public required string AgentType { get; init; }

    /// <summary>
    /// Custom name for this agent instance.
    /// </summary>
    public string? CustomName { get; init; }

    /// <summary>
    /// Task this agent is being spawned for.
    /// </summary>
    public TaskNodeId? AssignedTaskId { get; init; }

    /// <summary>
    /// Execution context for the agent.
    /// </summary>
    public ExecutionId? ExecutionId { get; init; }

    /// <summary>
    /// Configuration overrides for this instance.
    /// </summary>
    public AgentConfiguration? Configuration { get; init; }

    /// <summary>
    /// Resource limits for this agent.
    /// </summary>
    public AgentResourceLimits? ResourceLimits { get; init; }

    /// <summary>
    /// Priority level for resource allocation.
    /// </summary>
    public SpawnPriority Priority { get; init; } = SpawnPriority.Normal;

    /// <summary>
    /// Whether to wait for agent to be ready before returning.
    /// </summary>
    public bool WaitForReady { get; init; } = true;

    /// <summary>
    /// Timeout for agent initialization.
    /// </summary>
    public TimeSpan? InitTimeout { get; init; }
}

public enum SpawnPriority
{
    Low,
    Normal,
    High,
    Critical
}

/// <summary>
/// A spawned agent instance.
/// </summary>
public record SpawnedAgent
{
    public AgentId Id { get; init; }
    public string AgentType { get; init; } = "";
    public string Name { get; init; } = "";
    public AgentState State { get; init; }
    public DateTimeOffset SpawnedAt { get; init; }
    public TaskNodeId? AssignedTaskId { get; init; }
    public ExecutionId? ExecutionId { get; init; }
    public AgentResourceLimits ResourceLimits { get; init; } = new();
    public AgentCapabilities Capabilities { get; init; } = new();
}

/// <summary>
/// Resource limits for an agent.
/// </summary>
public record AgentResourceLimits
{
    public int? MaxTokensPerRequest { get; init; }
    public int? MaxTotalTokens { get; init; }
    public TimeSpan? MaxExecutionTime { get; init; }
    public long? MaxMemoryBytes { get; init; }
    public int? MaxConcurrentTools { get; init; }
    public int? MaxRetries { get; init; }
}

/// <summary>
/// Current status of an agent.
/// </summary>
public record AgentStatus
{
    public AgentId Id { get; init; }
    public string Name { get; init; } = "";
    public string AgentType { get; init; } = "";
    public AgentState State { get; init; }
    public AgentHealth Health { get; init; }
    public TaskNodeId? CurrentTaskId { get; init; }
    public string? CurrentActivity { get; init; }
    public AgentMetrics Metrics { get; init; } = new();
    public DateTimeOffset LastActivityAt { get; init; }
    public IReadOnlyList<AgentIssue> Issues { get; init; } = [];
    public ExecutionId? ExecutionId { get; init; }
}

public enum AgentState
{
    Initializing,
    Ready,
    Working,
    Paused,
    Waiting,        // Waiting for input/dependency
    Error,
    Terminating,
    Terminated
}

public enum AgentHealth
{
    Healthy,
    Degraded,       // Working but with issues
    Unhealthy,      // Significant problems
    Unresponsive,   // Not responding
    Unknown
}

/// <summary>
/// Runtime metrics for an agent.
/// </summary>
public record AgentMetrics
{
    public int TasksCompleted { get; init; }
    public int TasksFailed { get; init; }
    public int TotalTokensUsed { get; init; }
    public TimeSpan TotalRunTime { get; init; }
    public TimeSpan CurrentTaskDuration { get; init; }
    public float SuccessRate => TasksCompleted + TasksFailed > 0
        ? (float)TasksCompleted / (TasksCompleted + TasksFailed)
        : 0;
    public float AverageTaskDuration { get; init; }
    public int RetryCount { get; init; }
    public int ToolInvocations { get; init; }
}

/// <summary>
/// An issue affecting an agent.
/// </summary>
public record AgentIssue
{
    public Guid IssueId { get; init; } = Guid.NewGuid();
    public IssueSeverity Severity { get; init; }
    public string Code { get; init; } = "";
    public string Description { get; init; } = "";
    public DateTimeOffset DetectedAt { get; init; }
    public bool IsResolved { get; init; }
    public string? Resolution { get; init; }
}

public enum IssueSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Reason for terminating an agent.
/// </summary>
public record TerminationReason
{
    public TerminationType Type { get; init; }
    public string? Description { get; init; }
    public bool SaveState { get; init; } = true;
    public bool GracefulShutdown { get; init; } = true;
    public TimeSpan? GracePeriod { get; init; }
}

public enum TerminationType
{
    TaskComplete,       // Task finished successfully
    TaskFailed,         // Task failed permanently
    UserRequested,      // User requested termination
    ResourceExhausted,  // Hit resource limits
    Timeout,            // Execution timeout
    Error,              // Unrecoverable error
    Reassigned,         // Task reassigned to another agent
    ExecutionCancelled  // Parent execution cancelled
}

/// <summary>
/// Monitors agent health and performance.
/// </summary>
public interface IAgentHealthMonitor
{
    /// <summary>
    /// Perform health check on an agent.
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(
        AgentId agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Get health history for an agent.
    /// </summary>
    Task<IReadOnlyList<HealthCheckResult>> GetHealthHistoryAsync(
        AgentId agentId,
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Configure health check settings.
    /// </summary>
    Task ConfigureHealthChecksAsync(
        HealthCheckConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Observable stream of health alerts.
    /// </summary>
    IObservable<HealthAlert> HealthAlerts { get; }
}

public record HealthCheckResult
{
    public AgentId AgentId { get; init; }
    public AgentHealth Health { get; init; }
    public DateTimeOffset CheckedAt { get; init; }
    public TimeSpan ResponseTime { get; init; }
    public IReadOnlyList<HealthCheckItem> Items { get; init; } = [];
    public string? Summary { get; init; }
}

public record HealthCheckItem
{
    public string Name { get; init; } = "";
    public bool Passed { get; init; }
    public string? Details { get; init; }
}

public record HealthAlert
{
    public AgentId AgentId { get; init; }
    public AgentHealth PreviousHealth { get; init; }
    public AgentHealth CurrentHealth { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Description { get; init; } = "";
    public SuggestedAction? Suggestion { get; init; }
}

public record SuggestedAction
{
    public string Description { get; init; } = "";
    public ActionType Type { get; init; }
    public bool AutomatedAvailable { get; init; }
}

public enum ActionType
{
    Restart,
    Reassign,
    Terminate,
    Investigate,
    Ignore
}

/// <summary>
/// Manages resource quotas for agent orchestration.
/// </summary>
public interface IAgentQuotaManager
{
    /// <summary>
    /// Check if resources are available for spawning.
    /// </summary>
    Task<QuotaCheckResult> CheckQuotaAsync(
        AgentSpawnRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Reserve resources for an agent.
    /// </summary>
    Task<QuotaReservation> ReserveAsync(
        AgentSpawnRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Release reserved resources.
    /// </summary>
    Task ReleaseAsync(
        QuotaReservationId reservationId,
        CancellationToken ct = default);

    /// <summary>
    /// Get current quota usage.
    /// </summary>
    Task<QuotaUsage> GetUsageAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Update quota limits.
    /// </summary>
    Task UpdateLimitsAsync(
        QuotaLimits limits,
        CancellationToken ct = default);
}

public record QuotaCheckResult
{
    public bool CanSpawn { get; init; }
    public string? BlockingReason { get; init; }
    public QuotaUsage CurrentUsage { get; init; } = new();
    public IReadOnlyList<QuotaWarning> Warnings { get; init; } = [];
}

public record QuotaUsage
{
    public int ActiveAgents { get; init; }
    public int MaxAgents { get; init; }
    public int TokensUsedToday { get; init; }
    public int DailyTokenLimit { get; init; }
    public int ConcurrentTasks { get; init; }
    public int MaxConcurrentTasks { get; init; }
    public long MemoryUsedBytes { get; init; }
    public long MaxMemoryBytes { get; init; }

    public float AgentUtilization => MaxAgents > 0 ? (float)ActiveAgents / MaxAgents : 0;
    public float TokenUtilization => DailyTokenLimit > 0 ? (float)TokensUsedToday / DailyTokenLimit : 0;
}

/// <summary>
/// Events for agent lifecycle changes.
/// </summary>
public abstract record AgentLifecycleEvent
{
    public AgentId AgentId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record AgentSpawnedEvent : AgentLifecycleEvent
{
    public SpawnedAgent Agent { get; init; } = null!;
    public AgentSpawnRequest Request { get; init; } = null!;
}

public record AgentStateChangedEvent : AgentLifecycleEvent
{
    public AgentState PreviousState { get; init; }
    public AgentState NewState { get; init; }
    public string? Reason { get; init; }
}

public record AgentTaskAssignedEvent : AgentLifecycleEvent
{
    public TaskNodeId TaskId { get; init; }
    public TaskNode Task { get; init; } = null!;
}

public record AgentTaskCompletedEvent : AgentLifecycleEvent
{
    public TaskNodeId TaskId { get; init; }
    public TaskOutput Output { get; init; } = null!;
}

public record AgentErrorEvent : AgentLifecycleEvent
{
    public string ErrorCode { get; init; } = "";
    public string ErrorMessage { get; init; } = "";
    public bool Recoverable { get; init; }
}

public record AgentTerminatedEvent : AgentLifecycleEvent
{
    public TerminationReason Reason { get; init; } = null!;
    public AgentMetrics FinalMetrics { get; init; } = null!;
}

public record AgentReassignedEvent : AgentLifecycleEvent
{
    public TaskNodeId TaskId { get; init; }
    public AgentId NewAgentId { get; init; }
    public string? ReassignmentReason { get; init; }
}
```

### Agent Control Dashboard UI Mockup

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Agent Orchestration Control Panel          Execution: #exec-7890-abcd     │
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ RESOURCE USAGE:                                                            │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ Agents: [████████░░] 4/5     Tokens: [██████░░░░] 12.5k/25k today      ││
│ │ Memory: [███░░░░░░░] 256MB/1GB    Tasks: [███████░░░] 7/10 concurrent  ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ ACTIVE AGENTS (4):                                                         │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ 🟢 Chronicler-1    │ WORKING │ Task: Research Git commits              ││
│ │    ├── Tokens: 2,340 | Runtime: 2m 15s | Health: Healthy               ││
│ │    └── Progress: Analyzing 47 commits...                               ││
│ │    [Pause] [View Details] [Terminate]                                  ││
│ ├─────────────────────────────────────────────────────────────────────────┤│
│ │ 🟢 Scribe-1        │ WORKING │ Task: Generate API overview             ││
│ │    ├── Tokens: 4,120 | Runtime: 3m 42s | Health: Healthy               ││
│ │    └── Progress: Writing section 3 of 5...                             ││
│ │    [Pause] [View Details] [Terminate]                                  ││
│ ├─────────────────────────────────────────────────────────────────────────┤│
│ │ 🟡 Validator-1     │ WAITING │ Task: Validate output                   ││
│ │    ├── Tokens: 0 | Runtime: 0s | Health: Healthy                       ││
│ │    └── Status: Waiting for Scribe-1 to complete...                     ││
│ │    [Skip Wait] [View Details] [Terminate]                              ││
│ ├─────────────────────────────────────────────────────────────────────────┤│
│ │ 🟠 Researcher-2    │ DEGRADED │ Task: Fetch external docs              ││
│ │    ├── Tokens: 1,890 | Runtime: 5m 10s | Health: Degraded              ││
│ │    └── Issue: Rate limited by external API, retrying in 30s...         ││
│ │    [Pause] [View Details] [Reassign] [Terminate]                       ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ TERMINATED AGENTS (2):                                                     │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ ⚫ Chronicler-0    │ Completed: 10 min ago │ Tokens: 3,200 │ 2 tasks   ││
│ │ ⚫ Helper-1        │ Terminated: 5 min ago │ Reason: Task reassigned   ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ QUICK ACTIONS:                                                             │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ [+ Spawn New Agent] [⏸ Pause All] [⏹ Terminate All] [📊 View Metrics] ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
│ EVENT LOG:                                                                 │
│ ┌─────────────────────────────────────────────────────────────────────────┐│
│ │ 14:23:45 │ INFO  │ Chronicler-1 assigned to task "Research commits"    ││
│ │ 14:23:47 │ INFO  │ Scribe-1 spawned for task "Generate overview"       ││
│ │ 14:25:12 │ WARN  │ Researcher-2 health degraded: API rate limit        ││
│ │ 14:26:30 │ INFO  │ Chronicler-0 terminated: task complete              ││
│ └─────────────────────────────────────────────────────────────────────────┘│
│                                                                             │
└────────────────────────────────────────────────────────────────────────────┘
```

### Agent Lifecycle State Machine

```
                              SpawnRequested
                                    │
                                    ▼
                            ┌───────────────┐
                            │ Initializing  │
                            └───────┬───────┘
                                    │ Ready
                                    ▼
                ┌───────────────────────────────────────┐
                │                                       │
          Pause │            ┌─────────┐              │ TaskAssigned
                │       ┌───▶│  Ready  │◀───┐         │
                ▼       │    └────┬────┘    │         ▼
          ┌─────────┐   │         │         │   ┌─────────┐
          │ Paused  │───┘   Assign│Task  Complete│   │ Working │
          └─────────┘             ▼         │   └────┬────┘
                Resume     ┌──────────┐     │        │
                           │ Waiting  │─────┘        │
                           └──────────┘              │
                                                     │ Error/Timeout
                                                     ▼
                                               ┌─────────┐
                                               │  Error  │
                                               └────┬────┘
                                                    │
                        Terminate (any state)       │ Recover or
                               │                    │ Terminate
                               ▼                    ▼
                         ┌─────────────┐    ┌─────────────┐
                         │ Terminating │───▶│ Terminated  │
                         └─────────────┘    └─────────────┘
```

### License Gating

| Tier | Features |
|:-----|:---------|
| Core | Max 2 concurrent agents, basic status |
| WriterPro | Max 5 agents, health monitoring |
| Teams | Max 10 agents, full lifecycle control |
| Enterprise | Unlimited agents, custom quotas, API |

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
| `PlanSubmittedForApprovalEvent` | v0.13.6 | Plan submitted for user review |
| `PlanApprovedEvent` | v0.13.6 | User approved plan |
| `PlanRejectedEvent` | v0.13.6 | User rejected plan |
| `PlanModificationRequestedEvent` | v0.13.6 | User requested plan changes |
| `PlanCommentAddedEvent` | v0.13.6 | Comment added to plan |
| `AgentSpawnedEvent` | v0.13.7 | New agent dynamically created |
| `AgentStateChangedEvent` | v0.13.7 | Agent state transition |
| `AgentHealthChangedEvent` | v0.13.7 | Agent health status changed |
| `AgentTerminatedEvent` | v0.13.7 | Agent terminated |
| `AgentReassignedEvent` | v0.13.7 | Task reassigned to different agent |

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

With v0.13.x complete, Lexichord becomes a true agentic orchestration platform with human-in-the-loop capabilities:

- **Intent Understanding:** Natural language commands decomposed intelligently
- **Smart Delegation:** Tasks matched to best-suited agents automatically
- **Reliable Execution:** Parallel execution with failure recovery
- **Quality Assurance:** Conflicts detected and resolved, quality assessed
- **Reusability:** Patterns and ensembles for common workflows
- **Visibility:** Full observability into orchestration progress
- **Human Control:** Interactive plan review, commenting, and approval before execution
- **Dynamic Orchestration:** Spawn agents on-demand, monitor status, manage lifecycles

The planning and approval workflows (v0.13.6) ensure users maintain control over AI actions, while dynamic agent orchestration (v0.13.7) provides the flexibility to scale agent resources during complex tasks.

This enables the visual Agent Studio in v0.14.x.

---

## Total Orchestration Investment

| Phase | Versions | Hours |
|:------|:---------|:------|
| Phase 7: Core Orchestration | v0.13.1 - v0.13.5 | ~276 |
| Phase 7b: Human-in-the-Loop | v0.13.6 - v0.13.7 | ~120 |
| **Total Phase 7** | **v0.13.1 - v0.13.7** | **~396** |

**Combined with prior phases:** ~1,340 hours (~33.5 person-months)

---
