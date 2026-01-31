# LCS-SBD-v0.14.5-STU: Scope Overview — Simulation & Replay

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-v0.14.5-STU                                          |
| **Version**      | v0.14.5                                                      |
| **Codename**     | Simulation & Replay (Agent Studio Phase 5)                   |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Studio Architecture Lead                                     |
| **Depends On**   | v0.14.1-STU (Visual Workflow Canvas), v0.14.2-STU (Agent Debugger), v0.13.3-ORC (Execution Coordinator) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.14.5-STU** delivers **Simulation & Replay** — a comprehensive system for dry-run simulation and execution replay that enables testing and debugging workflows without side effects. This establishes:

- A simulation engine that executes workflows with mock LLM responses and no external effects
- A mock provider system for configuring deterministic responses for testing
- An execution recorder that captures all events for later analysis and replay
- A replay controller for playing back recorded executions with speed control and seeking
- A what-if analysis capability for testing alternative scenarios against recorded executions
- A simulation UI for configuring, running, and visualizing simulated executions

This is essential for workflow quality assurance—without simulation and replay, users cannot safely test complex workflows or diagnose production issues.

### 1.2 Business Value

- **Safety:** Test workflows without consuming tokens or affecting external systems.
- **Reproducibility:** Replay production issues for debugging without guessing.
- **Testing:** Validate workflow behavior with deterministic mock data.
- **Learning:** Replay successful executions for training and onboarding.
- **Analysis:** What-if scenarios reveal workflow sensitivity to input changes.
- **Cost:** Simulation avoids expensive LLM calls during development.

### 1.3 Success Criteria

1. Simulations run at least 2x faster than real-time execution.
2. Mock responses are configurable per node with latency simulation.
3. Execution recording captures all state transitions and data flow.
4. Replay accurately reproduces execution visualization at configurable speed.
5. What-if analysis correctly identifies differences from baseline execution.
6. Recordings can be exported and imported for sharing.
7. Simulation mode is visually distinct from live execution.

---

## 2. Key Deliverables

### 2.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.14.5e | Simulation Engine | Execute workflows with mock providers, no side effects | 12 |
| v0.14.5f | Mock Provider | Configure mock responses per node with latency simulation | 10 |
| v0.14.5g | Execution Recorder | Record execution events and state for replay | 10 |
| v0.14.5h | Replay Controller | Play back recordings with speed control and seeking | 10 |
| v0.14.5i | What-If Analysis | Compare scenarios against baseline recordings | 6 |
| v0.14.5j | Simulation UI | Interface for simulation configuration and visualization | 4 |
| **Total** | | | **52 hours** |

### 2.2 Core Interfaces

```csharp
/// <summary>
/// Simulates workflow execution without side effects.
/// Provides dry-run capability for testing and validation.
/// </summary>
public interface ISimulationEngine
{
    /// <summary>
    /// Create a new simulation for a workflow.
    /// </summary>
    Task<Simulation> CreateSimulationAsync(
        WorkflowDefinition workflow,
        SimulationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Run a simulation to completion.
    /// </summary>
    Task<SimulationResult> RunAsync(
        SimulationId simulationId,
        CancellationToken ct = default);

    /// <summary>
    /// Run a simulation with specific mock responses.
    /// </summary>
    Task<SimulationResult> RunWithMocksAsync(
        SimulationId simulationId,
        IReadOnlyDictionary<CanvasNodeId, MockResponse> mocks,
        CancellationToken ct = default);

    /// <summary>
    /// Get current state of a simulation.
    /// </summary>
    Task<Simulation?> GetSimulationAsync(
        SimulationId simulationId,
        CancellationToken ct = default);

    /// <summary>
    /// Cancel a running simulation.
    /// </summary>
    Task CancelSimulationAsync(
        SimulationId simulationId,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribe to simulation events.
    /// </summary>
    IObservable<SimulationEvent> Events { get; }
}

/// <summary>
/// Strongly-typed identifier for a simulation.
/// </summary>
public readonly record struct SimulationId(Guid Value)
{
    public static SimulationId New() => new(Guid.NewGuid());
    public static SimulationId Parse(string s) => new(Guid.Parse(s));
    public override string ToString() => $"sim:{Value:N}";
}

/// <summary>
/// Options for simulation execution.
/// </summary>
public record SimulationOptions
{
    /// <summary>
    /// Simulation mode.
    /// </summary>
    public SimulationMode Mode { get; init; } = SimulationMode.Full;

    /// <summary>
    /// Whether to record the simulation for replay.
    /// </summary>
    public bool RecordExecution { get; init; } = true;

    /// <summary>
    /// Input value overrides.
    /// </summary>
    public IReadOnlyDictionary<string, object>? InputOverrides { get; init; }

    /// <summary>
    /// Pre-configured mock responses per node.
    /// </summary>
    public IReadOnlyDictionary<CanvasNodeId, MockResponse>? NodeMocks { get; init; }

    /// <summary>
    /// Speed multiplier for time-based operations.
    /// </summary>
    public float SpeedMultiplier { get; init; } = 1.0f;

    /// <summary>
    /// Whether to pause at breakpoints.
    /// </summary>
    public bool RespectBreakpoints { get; init; } = false;

    /// <summary>
    /// Maximum execution time.
    /// </summary>
    public TimeSpan? Timeout { get; init; }

    /// <summary>
    /// Seed for random operations (for reproducibility).
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// Mock configuration for LLM calls.
    /// </summary>
    public LLMMockConfiguration? LLMMockConfig { get; init; }
}

/// <summary>
/// Simulation modes.
/// </summary>
public enum SimulationMode
{
    /// <summary>Run all nodes with mock LLM responses.</summary>
    Full,

    /// <summary>Validate workflow structure only (no execution).</summary>
    Structural,

    /// <summary>Trace data flow without executing nodes.</summary>
    DataFlow,

    /// <summary>Run only a subset of nodes.</summary>
    Partial
}

/// <summary>
/// Configuration for mocking LLM calls.
/// </summary>
public record LLMMockConfiguration
{
    /// <summary>
    /// Default mock response for LLM calls.
    /// </summary>
    public string? DefaultResponse { get; init; }

    /// <summary>
    /// Whether to simulate realistic latency.
    /// </summary>
    public bool SimulateLatency { get; init; } = true;

    /// <summary>
    /// Latency range in milliseconds.
    /// </summary>
    public (int Min, int Max) LatencyRange { get; init; } = (100, 500);

    /// <summary>
    /// Whether to simulate token counts.
    /// </summary>
    public bool SimulateTokens { get; init; } = true;

    /// <summary>
    /// Tokens per response estimate.
    /// </summary>
    public int EstimatedTokensPerResponse { get; init; } = 500;
}

/// <summary>
/// A simulation instance.
/// </summary>
public record Simulation
{
    /// <summary>
    /// Unique identifier.
    /// </summary>
    public SimulationId Id { get; init; }

    /// <summary>
    /// Workflow being simulated.
    /// </summary>
    public WorkflowDefinition Workflow { get; init; } = null!;

    /// <summary>
    /// Current state.
    /// </summary>
    public SimulationState State { get; init; }

    /// <summary>
    /// Simulation options.
    /// </summary>
    public SimulationOptions Options { get; init; } = new();

    /// <summary>
    /// Current progress (0.0-1.0).
    /// </summary>
    public float Progress { get; init; }

    /// <summary>
    /// Current node being simulated.
    /// </summary>
    public CanvasNodeId? CurrentNodeId { get; init; }

    /// <summary>
    /// When the simulation was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// When the simulation started.
    /// </summary>
    public DateTimeOffset? StartedAt { get; init; }

    /// <summary>
    /// Elapsed simulation time.
    /// </summary>
    public TimeSpan ElapsedTime { get; init; }
}

/// <summary>
/// Simulation execution states.
/// </summary>
public enum SimulationState
{
    Created, Running, Paused, Completed, Failed, Cancelled
}

/// <summary>
/// Result of a simulation run.
/// </summary>
public record SimulationResult
{
    /// <summary>
    /// Simulation identifier.
    /// </summary>
    public SimulationId SimulationId { get; init; }

    /// <summary>
    /// Whether the simulation completed successfully.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Total simulation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Results for each node.
    /// </summary>
    public IReadOnlyList<SimulatedNodeResult> NodeResults { get; init; } = [];

    /// <summary>
    /// Final output values.
    /// </summary>
    public IReadOnlyDictionary<string, object> FinalOutputs { get; init; } =
        new Dictionary<string, object>();

    /// <summary>
    /// Warnings generated during simulation.
    /// </summary>
    public IReadOnlyList<SimulationWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Recording of the execution (if recorded).
    /// </summary>
    public RecordingId? RecordingId { get; init; }

    /// <summary>
    /// Simulated token usage.
    /// </summary>
    public int SimulatedTokens { get; init; }

    /// <summary>
    /// Simulated cost estimate.
    /// </summary>
    public decimal? SimulatedCost { get; init; }
}

/// <summary>
/// Result for a single simulated node.
/// </summary>
public record SimulatedNodeResult
{
    /// <summary>
    /// Node identifier.
    /// </summary>
    public CanvasNodeId NodeId { get; init; }

    /// <summary>
    /// Node name.
    /// </summary>
    public string NodeName { get; init; } = "";

    /// <summary>
    /// Whether the node executed.
    /// </summary>
    public bool Executed { get; init; }

    /// <summary>
    /// Whether execution was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Node output (if successful).
    /// </summary>
    public object? Output { get; init; }

    /// <summary>
    /// Whether mock data was used.
    /// </summary>
    public bool WasMocked { get; init; }

    /// <summary>
    /// Simulated execution duration.
    /// </summary>
    public TimeSpan SimulatedDuration { get; init; }

    /// <summary>
    /// Simulated tokens used.
    /// </summary>
    public int SimulatedTokens { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? ErrorMessage { get; init; }
}

/// <summary>
/// A warning from simulation.
/// </summary>
public record SimulationWarning
{
    /// <summary>
    /// Warning code.
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Related node (if applicable).
    /// </summary>
    public CanvasNodeId? NodeId { get; init; }
}

/// <summary>
/// Mock response configuration for a node.
/// </summary>
public record MockResponse
{
    /// <summary>
    /// Output value to return.
    /// </summary>
    public required object Output { get; init; }

    /// <summary>
    /// Simulated latency.
    /// </summary>
    public TimeSpan? SimulatedLatency { get; init; }

    /// <summary>
    /// Whether the mock should fail.
    /// </summary>
    public bool ShouldFail { get; init; }

    /// <summary>
    /// Failure message (if ShouldFail is true).
    /// </summary>
    public string? FailureMessage { get; init; }

    /// <summary>
    /// Simulated token count.
    /// </summary>
    public int? SimulatedTokens { get; init; }

    /// <summary>
    /// Whether to use this mock (false = skip node).
    /// </summary>
    public bool Enabled { get; init; } = true;
}

/// <summary>
/// Provides and manages mock responses.
/// </summary>
public interface IMockProvider
{
    /// <summary>
    /// Create a mock configuration for a workflow.
    /// </summary>
    Task<MockConfiguration> CreateConfigurationAsync(
        WorkflowDefinition workflow,
        CancellationToken ct = default);

    /// <summary>
    /// Save a mock configuration.
    /// </summary>
    Task SaveConfigurationAsync(
        MockConfigurationId configId,
        MockConfiguration config,
        CancellationToken ct = default);

    /// <summary>
    /// Load a mock configuration.
    /// </summary>
    Task<MockConfiguration?> LoadConfigurationAsync(
        MockConfigurationId configId,
        CancellationToken ct = default);

    /// <summary>
    /// Get mock response for a node.
    /// </summary>
    Task<MockResponse?> GetMockResponseAsync(
        MockConfigurationId configId,
        CanvasNodeId nodeId,
        IReadOnlyDictionary<string, object> inputs,
        CancellationToken ct = default);

    /// <summary>
    /// List saved mock configurations.
    /// </summary>
    Task<IReadOnlyList<MockConfigurationInfo>> ListConfigurationsAsync(
        Guid workflowId,
        CancellationToken ct = default);
}

/// <summary>
/// Strongly-typed identifier for a mock configuration.
/// </summary>
public readonly record struct MockConfigurationId(Guid Value)
{
    public static MockConfigurationId New() => new(Guid.NewGuid());
}

/// <summary>
/// A saved mock configuration.
/// </summary>
public record MockConfiguration
{
    /// <summary>
    /// Configuration identifier.
    /// </summary>
    public MockConfigurationId Id { get; init; }

    /// <summary>
    /// Configuration name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Workflow this configuration is for.
    /// </summary>
    public Guid WorkflowId { get; init; }

    /// <summary>
    /// Mock responses per node.
    /// </summary>
    public IReadOnlyDictionary<CanvasNodeId, MockResponse> NodeMocks { get; init; } =
        new Dictionary<CanvasNodeId, MockResponse>();

    /// <summary>
    /// Global input overrides.
    /// </summary>
    public IReadOnlyDictionary<string, object>? InputOverrides { get; init; }

    /// <summary>
    /// When created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Info about a mock configuration.
/// </summary>
public record MockConfigurationInfo
{
    public MockConfigurationId Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Records executions for replay.
/// </summary>
public interface IExecutionRecorder
{
    /// <summary>
    /// Start recording an execution.
    /// </summary>
    Task<RecordingId> StartRecordingAsync(
        ExecutionId executionId,
        RecordingOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Stop recording an execution.
    /// </summary>
    Task StopRecordingAsync(
        RecordingId recordingId,
        CancellationToken ct = default);

    /// <summary>
    /// Get a recorded execution.
    /// </summary>
    Task<RecordedExecution?> GetRecordingAsync(
        RecordingId recordingId,
        CancellationToken ct = default);

    /// <summary>
    /// List recordings.
    /// </summary>
    Task<IReadOnlyList<RecordingInfo>> ListRecordingsAsync(
        RecordingQuery query,
        CancellationToken ct = default);

    /// <summary>
    /// Delete a recording.
    /// </summary>
    Task DeleteRecordingAsync(
        RecordingId recordingId,
        CancellationToken ct = default);

    /// <summary>
    /// Export a recording.
    /// </summary>
    Task<byte[]> ExportRecordingAsync(
        RecordingId recordingId,
        ExportFormat format,
        CancellationToken ct = default);

    /// <summary>
    /// Import a recording.
    /// </summary>
    Task<RecordingId> ImportRecordingAsync(
        byte[] data,
        ExportFormat format,
        CancellationToken ct = default);
}

/// <summary>
/// Strongly-typed identifier for a recording.
/// </summary>
public readonly record struct RecordingId(Guid Value)
{
    public static RecordingId New() => new(Guid.NewGuid());
    public static RecordingId Parse(string s) => new(Guid.Parse(s));
    public override string ToString() => $"rec:{Value:N}";
}

/// <summary>
/// Options for recording.
/// </summary>
public record RecordingOptions
{
    /// <summary>
    /// Recording name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Recording description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Whether to include full data payloads.
    /// </summary>
    public bool IncludePayloads { get; init; } = true;

    /// <summary>
    /// Maximum payload size to record.
    /// </summary>
    public long MaxPayloadSizeBytes { get; init; } = 1024 * 1024; // 1MB

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>
/// A recorded execution.
/// </summary>
public record RecordedExecution
{
    /// <summary>
    /// Recording identifier.
    /// </summary>
    public RecordingId Id { get; init; }

    /// <summary>
    /// Original execution ID.
    /// </summary>
    public ExecutionId OriginalExecutionId { get; init; }

    /// <summary>
    /// Recording name.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Recording description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Workflow that was executed.
    /// </summary>
    public WorkflowDefinition Workflow { get; init; } = null!;

    /// <summary>
    /// When the execution was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; }

    /// <summary>
    /// Total execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// All recorded events.
    /// </summary>
    public IReadOnlyList<RecordedEvent> Events { get; init; } = [];

    /// <summary>
    /// Initial inputs.
    /// </summary>
    public IReadOnlyDictionary<string, object>? InitialInputs { get; init; }

    /// <summary>
    /// Final outputs.
    /// </summary>
    public IReadOnlyDictionary<string, object>? FinalOutputs { get; init; }

    /// <summary>
    /// Size of the recording in bytes.
    /// </summary>
    public long SizeBytes { get; init; }

    /// <summary>
    /// Whether the execution was successful.
    /// </summary>
    public bool WasSuccessful { get; init; }

    /// <summary>
    /// Total tokens consumed.
    /// </summary>
    public int TotalTokens { get; init; }

    /// <summary>
    /// Tags for categorization.
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// A recorded event in an execution.
/// </summary>
public record RecordedEvent
{
    /// <summary>
    /// Event timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Offset from execution start.
    /// </summary>
    public TimeSpan Offset { get; init; }

    /// <summary>
    /// Event type name.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Event data.
    /// </summary>
    public required object EventData { get; init; }

    /// <summary>
    /// Related node.
    /// </summary>
    public CanvasNodeId? NodeId { get; init; }

    /// <summary>
    /// Related agent.
    /// </summary>
    public AgentId? AgentId { get; init; }
}

/// <summary>
/// Query for recordings.
/// </summary>
public record RecordingQuery
{
    public Guid? WorkflowId { get; init; }
    public ExecutionId? ExecutionId { get; init; }
    public TimeRange? TimeRange { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public bool? WasSuccessful { get; init; }
    public int Limit { get; init; } = 50;
    public int Offset { get; init; }
}

/// <summary>
/// Info about a recording.
/// </summary>
public record RecordingInfo
{
    public RecordingId Id { get; init; }
    public string? Name { get; init; }
    public ExecutionId OriginalExecutionId { get; init; }
    public DateTimeOffset RecordedAt { get; init; }
    public TimeSpan Duration { get; init; }
    public long SizeBytes { get; init; }
    public bool WasSuccessful { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}

/// <summary>
/// Export formats for recordings.
/// </summary>
public enum ExportFormat
{
    Json, MessagePack, Protobuf
}

/// <summary>
/// Replays recorded executions.
/// </summary>
public interface IReplayController
{
    /// <summary>
    /// Start a replay session.
    /// </summary>
    Task<ReplaySession> StartReplayAsync(
        RecordingId recordingId,
        ReplayOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Pause replay.
    /// </summary>
    Task PauseReplayAsync(
        ReplaySessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Resume replay.
    /// </summary>
    Task ResumeReplayAsync(
        ReplaySessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Seek to a specific position.
    /// </summary>
    Task SeekAsync(
        ReplaySessionId sessionId,
        TimeSpan position,
        CancellationToken ct = default);

    /// <summary>
    /// Set playback speed.
    /// </summary>
    Task SetSpeedAsync(
        ReplaySessionId sessionId,
        float speed,
        CancellationToken ct = default);

    /// <summary>
    /// Step forward one event.
    /// </summary>
    Task StepForwardAsync(
        ReplaySessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Step backward one event.
    /// </summary>
    Task StepBackwardAsync(
        ReplaySessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Stop replay.
    /// </summary>
    Task StopReplayAsync(
        ReplaySessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Get current replay state.
    /// </summary>
    Task<ReplaySession?> GetReplaySessionAsync(
        ReplaySessionId sessionId,
        CancellationToken ct = default);

    /// <summary>
    /// Subscribe to replay events.
    /// </summary>
    IObservable<ReplayEvent> ObserveReplay(ReplaySessionId sessionId);
}

/// <summary>
/// Strongly-typed identifier for a replay session.
/// </summary>
public readonly record struct ReplaySessionId(Guid Value)
{
    public static ReplaySessionId New() => new(Guid.NewGuid());
}

/// <summary>
/// Options for replay.
/// </summary>
public record ReplayOptions
{
    /// <summary>
    /// Playback speed (0.5x to 10x).
    /// </summary>
    public float Speed { get; init; } = 1.0f;

    /// <summary>
    /// Start position.
    /// </summary>
    public TimeSpan? StartAt { get; init; }

    /// <summary>
    /// End position.
    /// </summary>
    public TimeSpan? EndAt { get; init; }

    /// <summary>
    /// Pause on errors.
    /// </summary>
    public bool PauseOnErrors { get; init; }

    /// <summary>
    /// Pause on node completion.
    /// </summary>
    public bool PauseOnNodeCompletion { get; init; }
}

/// <summary>
/// A replay session.
/// </summary>
public record ReplaySession
{
    /// <summary>
    /// Session identifier.
    /// </summary>
    public ReplaySessionId Id { get; init; }

    /// <summary>
    /// Recording being replayed.
    /// </summary>
    public RecordingId RecordingId { get; init; }

    /// <summary>
    /// Current state.
    /// </summary>
    public ReplayState State { get; init; }

    /// <summary>
    /// Current position in the recording.
    /// </summary>
    public TimeSpan CurrentPosition { get; init; }

    /// <summary>
    /// Total duration.
    /// </summary>
    public TimeSpan TotalDuration { get; init; }

    /// <summary>
    /// Current playback speed.
    /// </summary>
    public float Speed { get; init; }

    /// <summary>
    /// Current event index.
    /// </summary>
    public int CurrentEventIndex { get; init; }

    /// <summary>
    /// Total event count.
    /// </summary>
    public int TotalEvents { get; init; }

    /// <summary>
    /// Current node (at this position).
    /// </summary>
    public CanvasNodeId? CurrentNodeId { get; init; }
}

/// <summary>
/// Replay states.
/// </summary>
public enum ReplayState
{
    Playing, Paused, Stopped, Completed
}

/// <summary>
/// Event during replay.
/// </summary>
public record ReplayEvent
{
    /// <summary>
    /// Session identifier.
    /// </summary>
    public ReplaySessionId SessionId { get; init; }

    /// <summary>
    /// Event type.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>
    /// Event data.
    /// </summary>
    public required object EventData { get; init; }

    /// <summary>
    /// Position in the recording.
    /// </summary>
    public TimeSpan Position { get; init; }
}

/// <summary>
/// What-if analysis for scenarios.
/// </summary>
public interface IWhatIfAnalyzer
{
    /// <summary>
    /// Analyze a what-if scenario.
    /// </summary>
    Task<WhatIfResult> AnalyzeAsync(
        RecordingId baseRecordingId,
        WhatIfScenario scenario,
        CancellationToken ct = default);

    /// <summary>
    /// Compare two recordings.
    /// </summary>
    Task<WhatIfComparison> CompareAsync(
        RecordingId recordingA,
        RecordingId recordingB,
        CancellationToken ct = default);
}

/// <summary>
/// A what-if scenario.
/// </summary>
public record WhatIfScenario
{
    /// <summary>
    /// Scenario name.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Node output overrides.
    /// </summary>
    public IReadOnlyDictionary<CanvasNodeId, MockResponse>? NodeOverrides { get; init; }

    /// <summary>
    /// Input value overrides.
    /// </summary>
    public IReadOnlyDictionary<string, object>? InputOverrides { get; init; }

    /// <summary>
    /// Nodes to skip.
    /// </summary>
    public IReadOnlyList<CanvasNodeId>? SkipNodes { get; init; }

    /// <summary>
    /// Nodes to inject failures into.
    /// </summary>
    public IReadOnlyList<CanvasNodeId>? InjectFailures { get; init; }
}

/// <summary>
/// Result of what-if analysis.
/// </summary>
public record WhatIfResult
{
    /// <summary>
    /// Base recording used.
    /// </summary>
    public RecordingId BaseRecordingId { get; init; }

    /// <summary>
    /// Scenario that was tested.
    /// </summary>
    public WhatIfScenario Scenario { get; init; } = new();

    /// <summary>
    /// Simulated result with the scenario applied.
    /// </summary>
    public SimulationResult SimulatedResult { get; init; } = null!;

    /// <summary>
    /// Differences from the baseline.
    /// </summary>
    public IReadOnlyList<WhatIfDifference> Differences { get; init; } = [];

    /// <summary>
    /// Overall impact summary.
    /// </summary>
    public WhatIfImpactSummary ImpactSummary { get; init; } = new();
}

/// <summary>
/// A difference in what-if analysis.
/// </summary>
public record WhatIfDifference
{
    /// <summary>
    /// Node where difference occurred.
    /// </summary>
    public CanvasNodeId NodeId { get; init; }

    /// <summary>
    /// Node name.
    /// </summary>
    public string NodeName { get; init; } = "";

    /// <summary>
    /// Type of difference.
    /// </summary>
    public DifferenceType Type { get; init; }

    /// <summary>
    /// Original value.
    /// </summary>
    public object? OriginalValue { get; init; }

    /// <summary>
    /// New value.
    /// </summary>
    public object? NewValue { get; init; }

    /// <summary>
    /// Description of the difference.
    /// </summary>
    public string Description { get; init; } = "";
}

/// <summary>
/// Types of differences.
/// </summary>
public enum DifferenceType
{
    OutputChanged, ExecutionSkipped, ExecutionAdded, FailureAdded, FailureRemoved, DurationChanged
}

/// <summary>
/// Summary of what-if impact.
/// </summary>
public record WhatIfImpactSummary
{
    /// <summary>
    /// Number of nodes affected.
    /// </summary>
    public int NodesAffected { get; init; }

    /// <summary>
    /// Output changed?
    /// </summary>
    public bool FinalOutputChanged { get; init; }

    /// <summary>
    /// Success status changed?
    /// </summary>
    public bool SuccessStatusChanged { get; init; }

    /// <summary>
    /// Token difference.
    /// </summary>
    public int TokenDifference { get; init; }

    /// <summary>
    /// Duration difference.
    /// </summary>
    public TimeSpan DurationDifference { get; init; }
}

/// <summary>
/// Comparison of two recordings.
/// </summary>
public record WhatIfComparison
{
    /// <summary>
    /// Recording A.
    /// </summary>
    public RecordingId RecordingA { get; init; }

    /// <summary>
    /// Recording B.
    /// </summary>
    public RecordingId RecordingB { get; init; }

    /// <summary>
    /// Differences between recordings.
    /// </summary>
    public IReadOnlyList<WhatIfDifference> Differences { get; init; } = [];

    /// <summary>
    /// Similarity score (0.0-1.0).
    /// </summary>
    public float SimilarityScore { get; init; }
}

/// <summary>
/// Events from simulation.
/// </summary>
public abstract record SimulationEvent
{
    public SimulationId SimulationId { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record SimulationStartedEvent(SimulationId SimulationId) : SimulationEvent;
public record SimulationNodeStartedEvent(SimulationId SimulationId, CanvasNodeId NodeId) : SimulationEvent;
public record SimulationNodeCompletedEvent(SimulationId SimulationId, CanvasNodeId NodeId, bool Success) : SimulationEvent;
public record SimulationCompletedEvent(SimulationId SimulationId, bool Success) : SimulationEvent;
public record SimulationProgressEvent(SimulationId SimulationId, float Progress) : SimulationEvent;
```

---

## 3. Architecture

### 3.1 Component Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         Simulation & Replay                                  │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                     ISimulationEngine                                │   │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │   │
│  │  │   Create     │  │     Run      │  │   Cancel     │              │   │
│  │  │  Simulation  │  │  Simulation  │  │  Simulation  │              │   │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │   │
│  │         │                 │                 │                       │   │
│  │         ▼                 ▼                 ▼                       │   │
│  │  ┌─────────────────────────────────────────────────────────────┐   │   │
│  │  │                 Simulation Executor                          │   │   │
│  │  │  ┌───────────┐  ┌───────────┐  ┌───────────┐                │   │   │
│  │  │  │   Mock    │  │  Virtual  │  │   Time    │                │   │   │
│  │  │  │ Provider  │  │ Execution │  │ Controller│                │   │   │
│  │  │  └───────────┘  └───────────┘  └───────────┘                │   │   │
│  │  └─────────────────────────────────────────────────────────────┘   │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                     │                                       │
│              ┌──────────────────────┼──────────────────────┐               │
│              ▼                      ▼                      ▼               │
│  ┌───────────────────┐  ┌───────────────────┐  ┌───────────────────────┐  │
│  │IExecutionRecorder │  │ IReplayController │  │  IWhatIfAnalyzer      │  │
│  │                   │  │                   │  │                       │  │
│  │ • Record events   │  │ • Play/Pause      │  │ • Scenario analysis   │  │
│  │ • Export/Import   │  │ • Seek/Speed      │  │ • Comparison          │  │
│  │ • Query history   │  │ • Step forward    │  │ • Impact assessment   │  │
│  └───────────────────┘  └───────────────────┘  └───────────────────────┘  │
│              │                      │                      │               │
│              └──────────────────────┼──────────────────────┘               │
│                                     ▼                                       │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Recording Store                                 │   │
│  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────┐  │   │
│  │  │   Recording 1   │  │   Recording 2   │  │   Recording N       │  │   │
│  │  │   Events []     │  │   Events []     │  │   Events []         │  │   │
│  │  │   Metadata      │  │   Metadata      │  │   Metadata          │  │   │
│  │  └─────────────────┘  └─────────────────┘  └─────────────────────┘  │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Simulation UI                                   │   │
│  │  ┌──────────┐  ┌─────────┐  ┌──────────┐  ┌──────────┐  ┌────────┐ │   │
│  │  │ Config   │  │ Mock    │  │ Timeline │  │ What-If  │  │Results │ │   │
│  │  │  Panel   │  │ Editor  │  │  Player  │  │  Panel   │  │  View  │ │   │
│  │  └──────────┘  └─────────┘  └──────────┘  └──────────┘  └────────┘ │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 4. Data Model

### 4.1 Database Schema

```sql
-- Simulations
CREATE TABLE simulations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL,
    owner_id UUID NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'Created',
    options_json JSONB NOT NULL,
    progress REAL NOT NULL DEFAULT 0,
    current_node_id UUID,
    result_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    started_at TIMESTAMPTZ,
    completed_at TIMESTAMPTZ,

    INDEX idx_simulations_workflow ON simulations(workflow_id),
    INDEX idx_simulations_owner ON simulations(owner_id),
    INDEX idx_simulations_state ON simulations(state) WHERE state IN ('Created', 'Running')
);

-- Mock configurations
CREATE TABLE mock_configurations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workflow_id UUID NOT NULL,
    owner_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    mocks_json JSONB NOT NULL,
    input_overrides_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    modified_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_mock_configs_workflow ON mock_configurations(workflow_id),
    INDEX idx_mock_configs_owner ON mock_configurations(owner_id)
);

-- Execution recordings
CREATE TABLE execution_recordings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    execution_id UUID NOT NULL,
    workflow_id UUID NOT NULL,
    owner_id UUID NOT NULL,
    name VARCHAR(200),
    description TEXT,
    workflow_snapshot_json JSONB NOT NULL,
    initial_inputs_json JSONB,
    final_outputs_json JSONB,
    recorded_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    duration_ms BIGINT NOT NULL,
    size_bytes BIGINT NOT NULL,
    was_successful BOOLEAN NOT NULL,
    total_tokens INT NOT NULL DEFAULT 0,
    tags JSONB,

    INDEX idx_recordings_execution ON execution_recordings(execution_id),
    INDEX idx_recordings_workflow ON execution_recordings(workflow_id),
    INDEX idx_recordings_owner ON execution_recordings(owner_id),
    INDEX idx_recordings_time ON execution_recordings(recorded_at DESC),
    INDEX idx_recordings_tags ON execution_recordings USING gin(tags)
);

-- Recording events (stored separately for efficiency)
CREATE TABLE recording_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    recording_id UUID NOT NULL REFERENCES execution_recordings(id) ON DELETE CASCADE,
    sequence_number INT NOT NULL,
    timestamp TIMESTAMPTZ NOT NULL,
    offset_ms BIGINT NOT NULL,
    event_type VARCHAR(100) NOT NULL,
    event_data_json JSONB NOT NULL,
    node_id UUID,
    agent_id UUID,

    UNIQUE(recording_id, sequence_number),
    INDEX idx_recording_events_recording ON recording_events(recording_id, sequence_number)
);

-- Replay sessions
CREATE TABLE replay_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    recording_id UUID NOT NULL REFERENCES execution_recordings(id),
    owner_id UUID NOT NULL,
    state VARCHAR(20) NOT NULL DEFAULT 'Paused',
    current_position_ms BIGINT NOT NULL DEFAULT 0,
    speed REAL NOT NULL DEFAULT 1.0,
    current_event_index INT NOT NULL DEFAULT 0,
    options_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_replay_sessions_owner ON replay_sessions(owner_id),
    INDEX idx_replay_sessions_state ON replay_sessions(state) WHERE state IN ('Playing', 'Paused')
);

-- What-if scenarios
CREATE TABLE whatif_scenarios (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    base_recording_id UUID NOT NULL REFERENCES execution_recordings(id),
    owner_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    scenario_json JSONB NOT NULL,
    result_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_whatif_recording ON whatif_scenarios(base_recording_id),
    INDEX idx_whatif_owner ON whatif_scenarios(owner_id)
);
```

---

## 5. Simulation Capabilities

| Capability | Description |
|:-----------|:------------|
| Dry Run | Execute workflow with mock LLM responses |
| Data Flow Trace | Visualize how data moves through the workflow |
| Failure Injection | Test error handling by injecting failures |
| Speed Control | Run simulation faster or slower than real-time |
| Recording | Capture execution for later replay |
| What-If | Test alternative scenarios against baseline |
| Deterministic Mode | Use random seed for reproducible results |
| Partial Execution | Run only specific nodes |

---

## 6. Simulation UI

```
┌────────────────────────────────────────────────────────────────────────────┐
│ Simulation Mode: "Customer Analysis Pipeline"              [Exit Simulation]│
├────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│ ┌─ Simulation Config ───────────────────────────────────────────────────┐  │
│ │                                                                        │  │
│ │ Mode: [Full Simulation ▼]  Speed: [1x ▼]  Record: [✓]                │  │
│ │                                                                        │  │
│ │ Mock Configuration: [default-mocks ▼] [Edit Mocks]                    │  │
│ │                                                                        │  │
│ │ [▶ Run Simulation] [⏸ Pause] [⏹ Stop] [⟳ Reset]                      │  │
│ └────────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─ Workflow (Simulation View) ─────────────────────────────────────────┐   │
│ │                                                                       │   │
│ │    ┌─────────────┐                      SIMULATION MODE              │   │
│ │    │   Start     │ ✓ Mocked                                          │   │
│ │    └──────┬──────┘                                                   │   │
│ │           │                                                           │   │
│ │           ▼                                                           │   │
│ │    ┌─────────────┐                                                   │   │
│ │    │ Researcher  │ 🔄 Simulating... (mock data)                      │   │
│ │    │ [MOCKED]    │                                                   │   │
│ │    └──────┬──────┘                                                   │   │
│ │           │                                                           │   │
│ │           ▼                                                           │   │
│ │    ┌─────────────┐                                                   │   │
│ │    │   Writer    │ ○ Pending (mock ready)                            │   │
│ │    │ [MOCKED]    │                                                   │   │
│ │    └──────┬──────┘                                                   │   │
│ │           │                                                           │   │
│ │           ▼                                                           │   │
│ │    ┌─────────────┐                                                   │   │
│ │    │    End      │ ○ Pending                                         │   │
│ │    └─────────────┘                                                   │   │
│ │                                                                       │   │
│ └───────────────────────────────────────────────────────────────────────┘   │
│                                                                             │
│ ┌─ Timeline ────────────────────────────────────────────────────────────┐  │
│ │ ◀ │ ▶ │ ⏸ │     0:00 ━━━━●━━━━━━━━━━━━━━━━━━━━━━ 0:45         [1x ▼] │  │
│ │                                                                        │  │
│ │ Events: ▪▪▪▪▪●▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪▪    │  │
│ │ [Start] [Research:start] [Research:mock] [Research:end] ...           │  │
│ └────────────────────────────────────────────────────────────────────────┘  │
│                                                                             │
│ ┌─ Mock Data Editor ────────────┐ ┌─ Simulation Results ─────────────────┐ │
│ │                                │ │                                      │ │
│ │ Node: Researcher               │ │ Status: Running                      │ │
│ │                                │ │ Progress: 35%                        │ │
│ │ Response:                      │ │ Simulated Tokens: 1,245              │ │
│ │ ┌────────────────────────────┐ │ │ Simulated Duration: 0:15             │ │
│ │ │ {                          │ │ │                                      │ │
│ │ │   "competitors": [         │ │ │ Nodes:                               │ │
│ │ │     {"name": "Acme"...}    │ │ │ ├─ Start: ✓ (mocked)                │ │
│ │ │   ]                        │ │ │ ├─ Researcher: 🔄 (mocking)         │ │
│ │ │ }                          │ │ │ ├─ Writer: ○ (pending)              │ │
│ │ └────────────────────────────┘ │ │ └─ End: ○ (pending)                 │ │
│ │                                │ │                                      │ │
│ │ Latency: [200ms]               │ │ Warnings: 0                          │ │
│ │ Should Fail: [ ]               │ │ Errors: 0                            │ │
│ │                                │ │                                      │ │
│ │ [Apply Mock]                   │ │ [View Full Results]                  │ │
│ └────────────────────────────────┘ └──────────────────────────────────────┘ │
│                                                                             │
│ Status: 🎭 SIMULATION MODE │ No tokens consumed │ No external effects       │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## 7. Dependencies

| Component | Source | Usage |
|:----------|:-------|:------|
| `IWorkflowCanvas` | v0.14.1-STU | Workflow visualization |
| `IDebugController` | v0.14.2-STU | Breakpoint integration |
| `IExecutionCoordinator` | v0.13.3-ORC | Execution hooks |
| `IChatCompletionService` | v0.6.1a | Mock LLM interface |
| `IMediator` | v0.0.7a | Simulation events |
| `Moq` | 4.x | Mock framework |

---

## 8. License Gating

| Tier | Features |
|:-----|:---------|
| **Core** | Basic dry run only |
| **WriterPro** | + Recording; replay; 10 saved recordings |
| **Teams** | + What-if analysis; mock library; unlimited recordings |
| **Enterprise** | + Export/import; API access; custom mock providers |

---

## 9. Performance Targets

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| Simulation speed | >2x real-time | Ratio to actual execution |
| Recording start | <100ms | P95 timing |
| Replay seek | <200ms | P95 for any position |
| What-if analysis | <5s | P95 for 50-node workflow |
| Export recording | <500ms | P95 for 1MB recording |
| Import recording | <500ms | P95 for 1MB recording |

---

## 10. Testing Strategy

### 10.1 Unit Tests

- Mock provider response matching
- Recording event serialization
- Replay position calculations
- What-if diff generation

### 10.2 Integration Tests

- Full simulation lifecycle
- Recording and replay accuracy
- What-if scenario execution
- Export/import round-trip

### 10.3 Performance Tests

- 100-node workflow simulation
- 1-hour recording replay
- Concurrent simulations (10+)

---

## 11. Risks & Mitigations

| Risk | Impact | Mitigation |
|:-----|:-------|:-----------|
| Mock data accuracy | Misleading results | Validation against schema; warnings |
| Recording size | Storage costs | Compression; payload limits; retention |
| Replay timing drift | Incorrect visualization | Periodic sync; event-based replay |
| Simulation divergence | False confidence | Clear simulation indicators; warnings |

---

## 12. MediatR Events

| Event | Description |
|:------|:------------|
| `SimulationStartedEvent` | Simulation began |
| `SimulationCompletedEvent` | Simulation finished |
| `SimulationNodeExecutedEvent` | Node simulated |
| `RecordingCreatedEvent` | New recording saved |
| `RecordingDeletedEvent` | Recording removed |
| `ReplayStartedEvent` | Replay began |
| `ReplayCompletedEvent` | Replay finished |
| `WhatIfAnalysisCompletedEvent` | What-if analysis done |

---

**Document End**
