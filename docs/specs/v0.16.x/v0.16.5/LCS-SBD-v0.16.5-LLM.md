# LCS-SBD-v0.16.5-LLM: Scope Overview — Platform Integration

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-v0.16.5-LLM                                          |
| **Version**      | v0.16.5                                                      |
| **Codename**     | Platform Integration (Local LLM Phase 5)                     |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-31                                                   |
| **Owner**        | Local LLM Architecture Lead                                  |
| **Depends On**   | v0.16.1-LLM (Hardware & Backends), v0.16.2-LLM (Model Discovery), v0.16.3-LLM (Download & Storage), v0.16.4-LLM (Inference Runtime), v0.12.3-AGT (ILanguageModelProvider) |

---

## 1. Executive Summary

### 1.1 The Vision

**v0.16.5-LLM** delivers **Platform Integration** — the seamless integration of local LLM inference with Lexichord's existing AI platform ecosystem. This establishes:

- ILanguageModelProvider implementation enabling local LLMs as drop-in replacements for cloud providers
- Intelligent LLM Router for routing requests between local and cloud providers based on user preferences, task types, and cost/performance optimization
- Fallback Manager providing automatic failover with configurable retry strategies
- Cost & Performance Optimizer tracking cloud API costs and calculating savings from local usage
- Orchestration Integration enabling agents and workflows to leverage local LLMs with per-agent and per-task overrides
- Local LLM Dashboard UI for monitoring, configuration, and analytics

This is the capstone of the Local LLM initiative—without platform integration, local models remain isolated from the core Lexichord ecosystem.

### 1.2 Business Value

- **Seamless Integration:** Users leverage local LLMs within existing chat, agents, and workflows without code changes.
- **Cost Optimization:** Automatic routing reduces cloud API costs while maintaining quality through intelligent provider selection.
- **Resilience:** Fallback mechanisms ensure service continuity when local models are unavailable or cloud services are throttled.
- **Privacy + Flexibility:** Users choose local-first or cloud-first strategies based on sensitivity and latency requirements.
- **Visibility:** Dashboard provides usage analytics, cost tracking, and savings reports for informed decision-making.
- **Governance:** License-gated features ensure platform stability while enterprise users get advanced routing and cost controls.

### 1.3 Success Criteria

1. LocalLlmProvider passes 100% of ILanguageModelProvider interface compliance tests.
2. LLM Router makes routing decisions in <10ms P95 latency.
3. Fallback Manager recovers from provider failures in <100ms with zero data loss.
4. Cost Optimizer provides cost estimates accurate within ±15% of actual API charges.
5. Orchestration integration supports per-agent and per-task routing overrides.
6. Dashboard renders 6+ months of analytics data with <2s page load.
7. All features properly gated to WriterPro (basic), Teams (full), and Enterprise (advanced) tiers.
8. End-to-end integration testing covers 100% of routing strategies and fallback scenarios.

---

## 2. Key Deliverables

### 2.1 Sub-Parts

| Sub-Part | Title | Description | Est. Hours |
|:---------|:------|:------------|:-----------|
| v0.16.5e | ILanguageModelProvider Implementation | LocalLlmProvider class implementing ILanguageModelProvider interface for drop-in cloud provider replacement | 12 |
| v0.16.5f | LLM Router & Fallback | ILlmRouter routing service with multiple strategies (prefer local, prefer cloud, cost optimized, etc.) and IFallbackManager with configurable retry logic | 12 |
| v0.16.5g | Orchestration Integration | IOrchestrationLlmIntegration enabling agents and workflows to use local/cloud LLMs with per-agent and per-task overrides | 10 |
| v0.16.5h | Agent System Integration | Agent executor updates to support local LLM provider selection and runtime provider switching | 10 |
| v0.16.5i | Cost & Performance Optimizer | ICostPerformanceOptimizer for cost estimation, savings analysis, and provider recommendations | 8 |
| v0.16.5j | Local LLM Dashboard UI | Dashboard UI with routing configuration, provider status, analytics, cost tracking, and savings reports | 8 |
| **Total** | | | **60 hours** |

### 2.2 Core Interfaces

```csharp
/// <summary>
/// ILanguageModelProvider implementation for local LLMs.
/// Enables drop-in replacement for cloud providers.
/// </summary>
public class LocalLlmProvider : ILanguageModelProvider
{
    public string ProviderId => "local";
    public string ProviderName => "Local LLM";

    /// <summary>
    /// Check if local inference is available.
    /// </summary>
    public Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Get list of loaded local models.
    /// </summary>
    public Task<IReadOnlyList<LanguageModel>> GetModelsAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Generate response using local model (non-streaming).
    /// </summary>
    public Task<LanguageModelResponse> GenerateAsync(
        LanguageModelRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generate response using local model (streaming).
    /// </summary>
    public IAsyncEnumerable<LanguageModelChunk> GenerateStreamAsync(
        LanguageModelRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Generate embeddings using local model.
    /// </summary>
    public Task<EmbeddingResponse> EmbedAsync(
        EmbeddingRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get status of local LLM provider (local-specific extension).
    /// </summary>
    public Task<LocalProviderStatus> GetStatusAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Get currently active local model.
    /// </summary>
    public Task<LanguageModel?> GetActiveModelAsync(
        CancellationToken ct = default);

    /// <summary>
    /// Switch active local model.
    /// </summary>
    public Task SetActiveModelAsync(
        string modelId,
        CancellationToken ct = default);
}

/// <summary>
/// Status information for local LLM provider.
/// </summary>
public record LocalProviderStatus
{
    /// <summary>
    /// Whether local inference is ready to use.
    /// </summary>
    public bool IsReady { get; init; }

    /// <summary>
    /// ID of currently active model.
    /// </summary>
    public string? ActiveModelId { get; init; }

    /// <summary>
    /// Human-readable name of active model.
    /// </summary>
    public string? ActiveModelName { get; init; }

    /// <summary>
    /// Backend being used (Ollama, LM Studio, etc.).
    /// </summary>
    public InferenceBackendType? ActiveBackend { get; init; }

    /// <summary>
    /// Current resource usage (RAM, VRAM, GPU util).
    /// </summary>
    public InferenceResourceUsage? ResourceUsage { get; init; }

    /// <summary>
    /// List of model IDs currently loaded.
    /// </summary>
    public IReadOnlyList<string> LoadedModels { get; init; } = [];

    /// <summary>
    /// Error message if provider is not ready.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Routes LLM requests to appropriate providers based on configuration.
/// </summary>
public interface ILlmRouter
{
    /// <summary>
    /// Route a request to the best available provider.
    /// </summary>
    Task<RoutedRequest> RouteAsync(
        LanguageModelRequest request,
        RoutingOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Get routing configuration.
    /// </summary>
    Task<RoutingConfig> GetConfigAsync(CancellationToken ct = default);

    /// <summary>
    /// Update routing configuration.
    /// </summary>
    Task SetConfigAsync(
        RoutingConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Get routing statistics for a time period.
    /// </summary>
    Task<RoutingStats> GetStatsAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

/// <summary>
/// Options for routing a specific request.
/// </summary>
public record RoutingOptions
{
    /// <summary>
    /// Which provider to prefer (local, cloud, cost-optimized, etc.).
    /// </summary>
    public RoutingStrategy Strategy { get; init; } = RoutingStrategy.PreferLocal;

    /// <summary>
    /// Whether to fall back to other providers on failure.
    /// </summary>
    public bool AllowFallback { get; init; } = true;

    /// <summary>
    /// Maximum time to wait for local response before considering timeout.
    /// </summary>
    public TimeSpan? LocalTimeout { get; init; }

    /// <summary>
    /// Minimum token count to use local (skip local for very small requests).
    /// </summary>
    public int? MinTokensForLocal { get; init; }

    /// <summary>
    /// Maximum token count to use local (skip local for very large requests).
    /// </summary>
    public int? MaxTokensForLocal { get; init; }
}

/// <summary>
/// Strategies for choosing between local and cloud providers.
/// </summary>
public enum RoutingStrategy
{
    /// <summary>Use local if available, fallback to cloud.</summary>
    PreferLocal,

    /// <summary>Use cloud if available, fallback to local.</summary>
    PreferCloud,

    /// <summary>Only use local, fail if unavailable.</summary>
    LocalOnly,

    /// <summary>Only use cloud, fail if unavailable.</summary>
    CloudOnly,

    /// <summary>Choose based on estimated cost (minimize API spend).</summary>
    CostOptimized,

    /// <summary>Choose based on expected latency (minimize response time).</summary>
    PerformanceOptimized,

    /// <summary>Choose based on model quality for task type.</summary>
    QualityOptimized,

    /// <summary>Alternate between providers (round-robin).</summary>
    RoundRobin,

    /// <summary>Route based on current provider load.</summary>
    LoadBalanced
}

/// <summary>
/// Routing configuration for the LLM Router.
/// </summary>
public record RoutingConfig
{
    /// <summary>
    /// Default strategy when not specified per-request.
    /// </summary>
    public RoutingStrategy DefaultStrategy { get; init; } = RoutingStrategy.PreferLocal;

    /// <summary>
    /// Whether to enable fallback when primary provider fails.
    /// </summary>
    public bool EnableFallback { get; init; } = true;

    /// <summary>
    /// Preferred local model ID (null = use any available).
    /// </summary>
    public string? PreferredLocalModel { get; init; }

    /// <summary>
    /// Timeout (seconds) for local requests before considering unavailable.
    /// </summary>
    public int LocalTimeoutSeconds { get; init; } = 120;

    /// <summary>
    /// Maximum concurrent local requests (respect hardware limits).
    /// </summary>
    public int LocalMaxConcurrent { get; init; } = 2;

    /// <summary>
    /// Preferred cloud provider ID (OpenAI, Anthropic, etc.).
    /// </summary>
    public string? PreferredCloudProvider { get; init; }

    /// <summary>
    /// Preferred model within cloud provider.
    /// </summary>
    public string? PreferredCloudModel { get; init; }

    /// <summary>
    /// Task-specific routing overrides (e.g., code generation = PreferCloud).
    /// </summary>
    public IReadOnlyDictionary<TaskType, RoutingStrategy> TaskRouting { get; init; } =
        new Dictionary<TaskType, RoutingStrategy>();

    /// <summary>
    /// Daily cloud API budget (fail local if exceeded).
    /// </summary>
    public decimal? DailyCloudBudget { get; init; }

    /// <summary>
    /// Monthly cloud API budget (enforce cost limits).
    /// </summary>
    public decimal? MonthlyCloudBudget { get; init; }
}

/// <summary>
/// Task types for routing decisions.
/// </summary>
public enum TaskType
{
    Chat,
    Completion,
    Embedding,
    CodeGeneration,
    Summarization,
    Translation,
    Analysis,
    CreativeWriting
}

/// <summary>
/// A request that has been routed to a specific provider.
/// </summary>
public record RoutedRequest
{
    /// <summary>
    /// Provider instance for executing the request.
    /// </summary>
    public required ILanguageModelProvider Provider { get; init; }

    /// <summary>
    /// Model ID within the provider.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Decision details (why this provider was chosen).
    /// </summary>
    public required RoutingDecision Decision { get; init; }
}

/// <summary>
/// Details about how a request was routed.
/// </summary>
public record RoutingDecision
{
    /// <summary>
    /// ID of provider selected.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Human-readable reason (e.g., "Local model available").
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Strategy used for routing decision.
    /// </summary>
    public RoutingStrategy UsedStrategy { get; init; }

    /// <summary>
    /// Whether this was a fallback to secondary provider.
    /// </summary>
    public bool WasFallback { get; init; }

    /// <summary>
    /// List of providers considered during routing.
    /// </summary>
    public IReadOnlyList<string> ConsideredProviders { get; init; } = [];

    /// <summary>
    /// Estimated cost for this request (if cloud provider).
    /// </summary>
    public decimal? EstimatedCost { get; init; }

    /// <summary>
    /// Estimated latency for this provider.
    /// </summary>
    public TimeSpan? EstimatedLatency { get; init; }
}

/// <summary>
/// Statistics about routing decisions over a time period.
/// </summary>
public record RoutingStats
{
    /// <summary>
    /// Time period these statistics cover.
    /// </summary>
    public TimeSpan Period { get; init; }

    /// <summary>
    /// Total requests processed.
    /// </summary>
    public int TotalRequests { get; init; }

    /// <summary>
    /// Requests routed to local provider.
    /// </summary>
    public int LocalRequests { get; init; }

    /// <summary>
    /// Requests routed to cloud provider.
    /// </summary>
    public int CloudRequests { get; init; }

    /// <summary>
    /// Requests that triggered fallback mechanism.
    /// </summary>
    public int FallbackRequests { get; init; }

    /// <summary>
    /// Requests that failed despite fallback.
    /// </summary>
    public int FailedRequests { get; init; }

    /// <summary>
    /// Total cost of cloud API calls.
    /// </summary>
    public decimal TotalCloudCost { get; init; }

    /// <summary>
    /// Estimated cloud cost avoided by using local.
    /// </summary>
    public decimal EstimatedLocalSavings { get; init; }

    /// <summary>
    /// Average response time for local requests.
    /// </summary>
    public TimeSpan AverageLocalLatency { get; init; }

    /// <summary>
    /// Average response time for cloud requests.
    /// </summary>
    public TimeSpan AverageCloudLatency { get; init; }

    /// <summary>
    /// Request count by model ID.
    /// </summary>
    public IReadOnlyDictionary<string, int> RequestsByModel { get; init; } =
        new Dictionary<string, int>();
}

/// <summary>
/// Manages fallback behavior between providers.
/// </summary>
public interface IFallbackManager
{
    /// <summary>
    /// Execute an operation with automatic fallback on failure.
    /// </summary>
    Task<FallbackResult<T>> ExecuteWithFallbackAsync<T>(
        Func<ILanguageModelProvider, CancellationToken, Task<T>> operation,
        FallbackOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Get the fallback chain for a task type.
    /// </summary>
    Task<IReadOnlyList<ILanguageModelProvider>> GetFallbackChainAsync(
        TaskType taskType,
        CancellationToken ct = default);

    /// <summary>
    /// Configure fallback behavior system-wide.
    /// </summary>
    Task ConfigureAsync(
        FallbackConfig config,
        CancellationToken ct = default);
}

/// <summary>
/// Options for fallback behavior on a specific operation.
/// </summary>
public record FallbackOptions
{
    /// <summary>
    /// Maximum number of attempts (primary + fallbacks).
    /// </summary>
    public int MaxAttempts { get; init; } = 3;

    /// <summary>
    /// Initial timeout for first attempt.
    /// </summary>
    public TimeSpan InitialTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Multiplier for timeout on each retry (exponential backoff).
    /// </summary>
    public float TimeoutMultiplier { get; init; } = 1.5f;

    /// <summary>
    /// Retry if rate-limited (429 response).
    /// </summary>
    public bool RetryOnRateLimit { get; init; } = true;

    /// <summary>
    /// Retry if timeout occurs.
    /// </summary>
    public bool RetryOnTimeout { get; init; } = true;

    /// <summary>
    /// Retry on generic errors (not just timeout/rate limit).
    /// </summary>
    public bool RetryOnError { get; init; } = false;
}

/// <summary>
/// Result of executing with fallback.
/// </summary>
public record FallbackResult<T>
{
    /// <summary>
    /// Whether the operation ultimately succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Result value if successful.
    /// </summary>
    public T? Value { get; init; }

    /// <summary>
    /// ID of provider that ultimately provided result.
    /// </summary>
    public string? FinalProvider { get; init; }

    /// <summary>
    /// Number of attempts made.
    /// </summary>
    public int Attempts { get; init; }

    /// <summary>
    /// Details of each attempt.
    /// </summary>
    public IReadOnlyList<FallbackAttempt> AttemptHistory { get; init; } = [];
}

/// <summary>
/// Details about a single fallback attempt.
/// </summary>
public record FallbackAttempt
{
    /// <summary>
    /// Provider ID attempted.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Model ID used.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Whether this attempt succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// How long the attempt took.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Error message if failed.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Optimizes LLM usage for cost and performance.
/// </summary>
public interface ICostPerformanceOptimizer
{
    /// <summary>
    /// Get recommended provider/model for a request.
    /// </summary>
    Task<OptimizationRecommendation> GetRecommendationAsync(
        LanguageModelRequest request,
        OptimizationGoal goal,
        CancellationToken ct = default);

    /// <summary>
    /// Estimate cost for a request on all available providers.
    /// </summary>
    Task<IReadOnlyList<CostEstimate>> EstimateCostsAsync(
        LanguageModelRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Get detailed usage analytics over a time period.
    /// </summary>
    Task<UsageAnalytics> GetAnalyticsAsync(
        TimeSpan period,
        CancellationToken ct = default);

    /// <summary>
    /// Get savings report from using local LLMs.
    /// </summary>
    Task<SavingsReport> GetSavingsReportAsync(
        TimeSpan period,
        CancellationToken ct = default);
}

/// <summary>
/// Goals for optimization.
/// </summary>
public enum OptimizationGoal
{
    /// <summary>Minimize API costs.</summary>
    MinimizeCost,

    /// <summary>Minimize response latency.</summary>
    MinimizeLatency,

    /// <summary>Maximize output quality.</summary>
    MaximizeQuality,

    /// <summary>Balance cost and latency.</summary>
    BalanceCostLatency,

    /// <summary>Balance all three factors.</summary>
    BalanceAll
}

/// <summary>
/// Recommendation for provider/model choice.
/// </summary>
public record OptimizationRecommendation
{
    /// <summary>
    /// Provider ID recommended.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Model ID recommended.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Optimization goal this addresses.
    /// </summary>
    public required OptimizationGoal Goal { get; init; }

    /// <summary>
    /// Score for this recommendation (0-1, higher is better).
    /// </summary>
    public float Score { get; init; }

    /// <summary>
    /// Explanation of why this provider/model is recommended.
    /// </summary>
    public string Rationale { get; init; } = "";

    /// <summary>
    /// Estimated cost for using recommended option.
    /// </summary>
    public CostEstimate? CostEstimate { get; init; }

    /// <summary>
    /// Estimated performance for recommended option.
    /// </summary>
    public PerformanceEstimate? PerformanceEstimate { get; init; }

    /// <summary>
    /// Alternative options considered.
    /// </summary>
    public IReadOnlyList<AlternativeOption> Alternatives { get; init; } = [];
}

/// <summary>
/// Cost estimate for a provider/model combination.
/// </summary>
public record CostEstimate
{
    /// <summary>
    /// Provider ID.
    /// </summary>
    public required string ProviderId { get; init; }

    /// <summary>
    /// Model ID.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Estimated cost in specified currency.
    /// </summary>
    public decimal EstimatedCost { get; init; }

    /// <summary>
    /// Currency (USD, EUR, etc.).
    /// </summary>
    public string Currency { get; init; } = "USD";

    /// <summary>
    /// Estimated input tokens for this request.
    /// </summary>
    public int EstimatedInputTokens { get; init; }

    /// <summary>
    /// Estimated output tokens for this request.
    /// </summary>
    public int EstimatedOutputTokens { get; init; }

    /// <summary>
    /// Whether this is a local provider (cost = electricity).
    /// </summary>
    public bool IsLocal { get; init; }

    /// <summary>
    /// Estimated electricity cost for local execution (if local).
    /// </summary>
    public decimal? LocalElectricityCost { get; init; }
}

/// <summary>
/// Savings achieved by using local LLMs.
/// </summary>
public record SavingsReport
{
    /// <summary>
    /// Time period covered by this report.
    /// </summary>
    public TimeSpan Period { get; init; }

    /// <summary>
    /// Number of requests handled locally.
    /// </summary>
    public int LocalRequests { get; init; }

    /// <summary>
    /// Total tokens generated locally.
    /// </summary>
    public int LocalTokensGenerated { get; init; }

    /// <summary>
    /// Estimated cloud API cost if all requests used cloud.
    /// </summary>
    public decimal EstimatedCloudCostAvoided { get; init; }

    /// <summary>
    /// Actual cloud API cost paid (fallback + cloud-only requests).
    /// </summary>
    public decimal ActualCloudCost { get; init; }

    /// <summary>
    /// Estimated electricity cost for local inference.
    /// </summary>
    public decimal EstimatedLocalElectricityCost { get; init; }

    /// <summary>
    /// Net savings (avoided cost - electricity cost - actual cloud cost).
    /// </summary>
    public decimal NetSavings { get; init; }

    /// <summary>
    /// Percentage savings compared to cloud-only baseline.
    /// </summary>
    public float SavingsPercent { get; init; }

    /// <summary>
    /// Breakdown of savings by model.
    /// </summary>
    public IReadOnlyList<ModelSavings> ByModel { get; init; } = [];
}

/// <summary>
/// Savings for a specific model.
/// </summary>
public record ModelSavings
{
    /// <summary>
    /// Model ID.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Model human-readable name.
    /// </summary>
    public required string ModelName { get; init; }

    /// <summary>
    /// Number of requests using this model.
    /// </summary>
    public int Requests { get; init; }

    /// <summary>
    /// Tokens generated with this model.
    /// </summary>
    public int TokensGenerated { get; init; }

    /// <summary>
    /// Estimated savings if these requests used cloud.
    /// </summary>
    public decimal EstimatedSavings { get; init; }

    /// <summary>
    /// Which cloud model this was compared against.
    /// </summary>
    public string? ComparedToCloudModel { get; init; }
}

/// <summary>
/// Integrates local LLM with the orchestration system.
/// </summary>
public interface IOrchestrationLlmIntegration
{
    /// <summary>
    /// Configure LLM provider strategy for orchestration.
    /// </summary>
    Task ConfigureProviderAsync(
        OrchestrationLlmConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Get LLM provider for a specific agent.
    /// </summary>
    Task<ILanguageModelProvider> GetProviderForAgentAsync(
        Guid agentId,
        CancellationToken ct = default);

    /// <summary>
    /// Set provider override for a workflow execution.
    /// </summary>
    Task SetWorkflowOverrideAsync(
        Guid workflowId,
        LlmOverride @override,
        CancellationToken ct = default);
}

/// <summary>
/// Configuration for LLM integration in orchestration.
/// </summary>
public record OrchestrationLlmConfig
{
    /// <summary>
    /// Default routing strategy for all agents/workflows.
    /// </summary>
    public RoutingStrategy DefaultStrategy { get; init; } = RoutingStrategy.PreferLocal;

    /// <summary>
    /// Default local model to use (null = use any available).
    /// </summary>
    public string? DefaultLocalModel { get; init; }

    /// <summary>
    /// Default cloud provider (OpenAI, Anthropic, etc.).
    /// </summary>
    public string? DefaultCloudProvider { get; init; }

    /// <summary>
    /// Default cloud model within provider.
    /// </summary>
    public string? DefaultCloudModel { get; init; }

    /// <summary>
    /// Per-agent LLM overrides (e.g., agent "code-assistant" = prefer cloud).
    /// </summary>
    public IReadOnlyDictionary<string, LlmOverride> AgentOverrides { get; init; } =
        new Dictionary<string, LlmOverride>();

    /// <summary>
    /// Per-task-type defaults (e.g., code generation = prefer cloud).
    /// </summary>
    public IReadOnlyDictionary<TaskType, LlmOverride> TaskDefaults { get; init; } =
        new Dictionary<TaskType, LlmOverride>();
}

/// <summary>
/// Provider override for a specific context (agent, workflow, task).
/// </summary>
public record LlmOverride
{
    /// <summary>
    /// Provider ID to use (overrides default).
    /// </summary>
    public string? ProviderId { get; init; }

    /// <summary>
    /// Model ID to use.
    /// </summary>
    public string? ModelId { get; init; }

    /// <summary>
    /// Routing strategy to use.
    /// </summary>
    public RoutingStrategy? Strategy { get; init; }

    /// <summary>
    /// Additional parameters (temperature, max tokens, etc.).
    /// </summary>
    public IReadOnlyDictionary<string, object>? Parameters { get; init; }
}
```

---

## 3. Architecture

### 3.1 Platform Integration Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                    Lexichord Platform Integration                            │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                       User-Facing Features                              │ │
│  │                                                                         │ │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐ ┌─────────────┐      │ │
│  │  │    Chat     │ │   Agents    │ │  Document   │ │  Workflows  │      │ │
│  │  │  Interface  │ │   System    │ │ Interaction │ │  & Canvas   │      │ │
│  │  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘ └──────┬──────┘      │ │
│  │         │               │               │               │              │ │
│  └─────────┼───────────────┼───────────────┼───────────────┼──────────────┘ │
│            │               │               │               │                 │
│            └───────────────┴───────────────┴───────────────┘                 │
│                                    │                                         │
│                                    ▼                                         │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                          ILlmRouter                                     │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │ Route requests based on:                                          │  │ │
│  │  │ • User preference (local vs cloud)                                │  │ │
│  │  │ • Task type (code, creative, analysis)                            │  │ │
│  │  │ • Cost/performance optimization                                   │  │ │
│  │  │ • Model availability                                              │  │ │
│  │  │ • Context length requirements                                     │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                    │                                         │
│            ┌───────────────────────┴───────────────────────┐                │
│            ▼                                               ▼                │
│  ┌─────────────────────────┐               ┌─────────────────────────────┐ │
│  │    LocalLlmProvider     │               │     Cloud Providers         │ │
│  │                         │               │                             │ │
│  │  ILanguageModelProvider │               │  ┌─────────────────────┐   │ │
│  │                         │               │  │ OpenAI Provider     │   │ │
│  │  ┌───────────────────┐  │               │  ├─────────────────────┤   │ │
│  │  │ LocalInference    │  │               │  │ Anthropic Provider  │   │ │
│  │  │ Manager           │  │               │  ├─────────────────────┤   │ │
│  │  └───────────────────┘  │               │  │ Azure Provider      │   │ │
│  │           │             │               │  └─────────────────────┘   │ │
│  │           ▼             │               │                             │ │
│  │  ┌───────────────────┐  │               │                             │ │
│  │  │ Ollama/LM Studio  │  │               │                             │ │
│  │  │ Backend           │  │               │                             │ │
│  │  └───────────────────┘  │               │                             │ │
│  └─────────────────────────┘               └─────────────────────────────┘ │
│            │                                               │                │
│            ▼                                               ▼                │
│  ┌─────────────────────────┐               ┌─────────────────────────────┐ │
│  │   Local GPU/CPU         │               │   Cloud APIs                │ │
│  │   (User's Hardware)     │               │   (Internet)                │ │
│  └─────────────────────────┘               └─────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                     IFallbackManager                                    │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │ Automatic fallback on:                                            │  │ │
│  │  │ • Local model unavailable → Try cloud                             │  │ │
│  │  │ • Local timeout → Retry or fallback                               │  │ │
│  │  │ • Cloud rate limit → Try local                                    │  │ │
│  │  │ • Cloud quota exceeded → Force local                              │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │                  ICostPerformanceOptimizer                              │ │
│  │  ┌──────────────────────────────────────────────────────────────────┐  │ │
│  │  │ • Track cloud API costs                                           │  │ │
│  │  │ • Estimate local electricity costs                                │  │ │
│  │  │ • Calculate savings from local usage                              │  │ │
│  │  │ • Suggest optimal model for task                                  │  │ │
│  │  └──────────────────────────────────────────────────────────────────┘  │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 3.2 Routing Decision Flow

```
User Request (Chat/Agent/Workflow)
    │
    ▼
ILlmRouter.RouteAsync(request, options)
    │
    ├─ Check routing strategy
    │  • Prefer Local: Is local available?
    │  • Prefer Cloud: Is cloud available?
    │  • Cost Optimized: What's cheapest?
    │  • Performance Optimized: What's fastest?
    │  • Task-Specific: What's best for this task?
    │
    ├─ Check constraints
    │  • Token count range (min/max for local)
    │  • Model availability
    │  • Context length requirements
    │  • Cost budget remaining
    │
    ├─ Select primary provider
    │  → LocalLlmProvider or Cloud provider
    │
    └─ Return RoutedRequest with decision
       │
       ├─ Provider instance
       ├─ Model ID
       ├─ Reasoning
       └─ EstimatedCost/Latency
           │
           ▼
        Execute request via provider
           │
           ├─ Success → Return response
           │
           └─ Failure:
              ├─ Fallback enabled?
              │  Yes → IFallbackManager.ExecuteWithFallbackAsync()
              │        Try next provider in chain
              │        Retry up to MaxAttempts
              │
              └─ No → Propagate error
```

### 3.3 Orchestration Integration Flow

```
Agent/Workflow Execution
    │
    ▼
IOrchestrationLlmIntegration.GetProviderForAgentAsync(agentId)
    │
    ├─ Check agent-specific overrides
    │  → AgentOverrides["agent-name"] = { ProviderId, ModelId, Strategy }
    │
    ├─ Check task-type defaults
    │  → TaskDefaults[TaskType.CodeGeneration] = { Strategy: PreferCloud }
    │
    ├─ Check global defaults
    │  → DefaultStrategy, DefaultLocalModel, DefaultCloudProvider
    │
    └─ Return ILanguageModelProvider
       │
       ▼
    Agent invokes provider.GenerateAsync()
       │
       ├─ LocalLlmProvider.GenerateAsync()
       │  → Use Ollama/LM Studio backend
       │  → Monitor resource usage
       │  → Return local response
       │
       └─ CloudProvider.GenerateAsync()
          → Use cloud API
          → Track API cost
          → Return cloud response
```

---

## 4. Data Model

### 4.1 Database Schema

```sql
-- Routing configurations per user/workspace
CREATE TABLE llm_routing_configs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    owner_id UUID NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    default_strategy VARCHAR(50) NOT NULL DEFAULT 'PreferLocal',
    enable_fallback BOOLEAN NOT NULL DEFAULT TRUE,
    preferred_local_model VARCHAR(200),
    local_timeout_seconds INT NOT NULL DEFAULT 120,
    local_max_concurrent INT NOT NULL DEFAULT 2,
    preferred_cloud_provider VARCHAR(100),
    preferred_cloud_model VARCHAR(200),
    daily_cloud_budget DECIMAL(10, 2),
    monthly_cloud_budget DECIMAL(10, 2),
    config_json JSONB,
    is_active BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(workspace_id, name),
    INDEX idx_routing_workspace ON llm_routing_configs(workspace_id),
    INDEX idx_routing_active ON llm_routing_configs(workspace_id, is_active)
);

-- Task-specific routing overrides
CREATE TABLE llm_task_routing_overrides (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    routing_config_id UUID NOT NULL REFERENCES llm_routing_configs(id) ON DELETE CASCADE,
    task_type VARCHAR(50) NOT NULL,
    strategy VARCHAR(50) NOT NULL,
    model_id VARCHAR(200),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(routing_config_id, task_type),
    INDEX idx_task_override_config ON llm_task_routing_overrides(routing_config_id)
);

-- Per-agent LLM overrides for orchestration
CREATE TABLE orchestration_llm_overrides (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    agent_id UUID NOT NULL,
    provider_id VARCHAR(100),
    model_id VARCHAR(200),
    strategy VARCHAR(50),
    parameters_json JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    UNIQUE(agent_id),
    INDEX idx_agent_override_agent ON orchestration_llm_overrides(agent_id)
);

-- Routing decision history for analytics
CREATE TABLE routing_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    request_id UUID NOT NULL,
    provider_id VARCHAR(100) NOT NULL,
    model_id VARCHAR(200) NOT NULL,
    strategy VARCHAR(50) NOT NULL,
    was_fallback BOOLEAN NOT NULL DEFAULT FALSE,
    considered_providers TEXT,
    estimated_cost DECIMAL(10, 6),
    actual_cost DECIMAL(10, 6),
    input_tokens INT,
    output_tokens INT,
    latency_ms INT,
    success BOOLEAN NOT NULL,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_routing_history_workspace ON routing_history(workspace_id, created_at DESC),
    INDEX idx_routing_history_provider ON routing_history(provider_id),
    INDEX idx_routing_history_model ON routing_history(model_id)
);

-- Cost tracking for cloud API usage
CREATE TABLE cloud_api_costs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    workspace_id UUID NOT NULL,
    provider_id VARCHAR(100) NOT NULL,
    model_id VARCHAR(200) NOT NULL,
    request_id UUID NOT NULL,
    input_tokens INT NOT NULL,
    output_tokens INT NOT NULL,
    unit_cost DECIMAL(10, 8) NOT NULL,
    total_cost DECIMAL(10, 6) NOT NULL,
    currency VARCHAR(3) NOT NULL DEFAULT 'USD',
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_cost_workspace ON cloud_api_costs(workspace_id, created_at DESC),
    INDEX idx_cost_provider ON cloud_api_costs(workspace_id, provider_id, created_at DESC)
);

-- Fallback attempt history
CREATE TABLE fallback_attempts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id UUID NOT NULL,
    primary_provider VARCHAR(100) NOT NULL,
    attempt_number INT NOT NULL,
    provider_id VARCHAR(100) NOT NULL,
    model_id VARCHAR(200) NOT NULL,
    success BOOLEAN NOT NULL,
    duration_ms INT,
    error_message TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    INDEX idx_fallback_request ON fallback_attempts(request_id),
    INDEX idx_fallback_created ON fallback_attempts(created_at DESC)
);
```

### 4.2 Routing Configuration JSON

```json
{
  "defaultStrategy": "PreferLocal",
  "enableFallback": true,
  "preferredLocalModel": "llama2:13b",
  "localTimeoutSeconds": 120,
  "localMaxConcurrent": 2,
  "preferredCloudProvider": "openai",
  "preferredCloudModel": "gpt-4",
  "taskRouting": {
    "CodeGeneration": "PreferCloud",
    "Summarization": "PreferLocal",
    "Chat": "PreferLocal",
    "Analysis": "CostOptimized"
  },
  "dailyCloudBudget": 10.00,
  "monthlyCloudBudget": 200.00
}
```

---

## 5. UI Mockups

### 5.1 Routing Configuration Panel

```
┌─ Routing Configuration ──────────────────────────────────────────────────┐
│                                                                           │
│ Default Strategy:  [PreferLocal ▼]     Enable Fallback: [✓]            │
│                                                                           │
│ Local Settings:                                                           │
│   Preferred Model: [llama2:13b ▼]                                        │
│   Timeout (s):     [120______]                                           │
│   Max Concurrent:  [2______]                                             │
│                                                                           │
│ Cloud Settings:                                                           │
│   Preferred Provider: [OpenAI ▼]                                         │
│   Preferred Model:    [gpt-4 ▼]                                          │
│                                                                           │
│ Cost Controls:                                                            │
│   Daily Budget:   [$10.00 ___]                                           │
│   Monthly Budget: [$200.00 ___]                                          │
│                                                                           │
│ Task-Specific Overrides:                                                  │
│   ┌─────────────────────────────────────────────────────────────────────┐│
│   │ Task Type         │ Strategy          │ Model                       ││
│   ├─────────────────────────────────────────────────────────────────────┤│
│   │ CodeGeneration    │ PreferCloud ▼     │ gpt-4 ▼                     ││
│   │ Summarization     │ PreferLocal ▼     │ (auto) ▼                    ││
│   │ Translation       │ CostOptimized ▼   │ (auto) ▼                    ││
│   │ + Add Task Override                                                 ││
│   └─────────────────────────────────────────────────────────────────────┘│
│                                                                           │
│  [Save Config]  [Reset to Defaults]  [Test Routing]                      │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.2 Provider Status Dashboard

```
┌─ LLM Provider Status ───────────────────────────────────────────────────┐
│                                                                          │
│ Local LLM Provider                     ⬤ Ready                          │
│ ├─ Active Model: llama2:13b            RAM: 12.5 GB / 32 GB             │
│ ├─ Backend: Ollama                     VRAM: 8.2 GB / 24 GB             │
│ ├─ Loaded Models: [llama2:13b, neural-chat:7b]                          │
│ └─ Last Response: 1.2s (45 tok/s)                                       │
│                                                                          │
│ OpenAI Provider                        ⬤ Connected                      │
│ ├─ Status: Available                   Requests This Hour: 12 of 3500   │
│ ├─ Primary Model: gpt-4                Est. Cost Today: $2.45 / $10.00  │
│ └─ Last Response: 0.8s                                                  │
│                                                                          │
│ Anthropic Provider                     ⬤ Connected                      │
│ ├─ Status: Available                   Requests This Month: 234 of 1M   │
│ ├─ Primary Model: claude-3-opus        Est. Cost Today: $1.20 / $10.00  │
│ └─ Last Response: 1.5s                                                  │
│                                                                          │
│ Azure OpenAI                           ● Offline (credential expired)   │
│                                                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

### 5.3 Routing Analytics Dashboard

```
┌─ Routing Analytics ─ Last 30 Days ────────────────────────────────────┐
│                                                                        │
│ Summary Metrics:                                                       │
│  • Total Requests: 1,847                                              │
│  • Local: 1,205 (65.2%)  | Cloud: 642 (34.8%)  | Fallback: 87 (4.7%)│
│  • Total Cloud Cost: $145.32                                          │
│  • Estimated Savings: $267.50 (from local usage)                      │
│  • Net Savings: $122.18                                               │
│                                                                        │
│ Request Distribution by Provider:                                     │
│  ▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬▬ Local (65.2%)                                  │
│  ▬▬▬▬▬▬▬▬▬ OpenAI (22.1%)                                             │
│  ▬▬▬▬ Anthropic (10.5%)                                               │
│  ▬ Other (2.2%)                                                       │
│                                                                        │
│ Cost Trend:                                                            │
│   $6 │                                   ╱─╮                         │
│   $5 │       ╱─╮       ╱─╮       ╱─╮   ╱   ╲                         │
│   $4 │     ╱   ╲     ╱   ╲     ╱   ╲ ╱     ╲                         │
│   $3 │   ╱     ╲   ╱     ╲   ╱     ╲       ╲                         │
│   $2 │ ╱       ╲ ╱       ╲ ╱       ╲       ╲                         │
│   $0 └─────────────────────────────────────────                       │
│      Week1 Week2 Week3 Week4                                          │
│                                                                        │
│ Top Models:                                                            │
│  Model              │ Requests │ Avg Latency │ Avg Cost │ Savings    │
│  ───────────────────┼──────────┼─────────────┼──────────┼────────    │
│  llama2:13b (local) │    652   │ 1.2s        │ $0.00    │ $98.40     │
│  gpt-4              │    284   │ 0.8s        │ $0.52    │ -          │
│  claude-3-opus      │    195   │ 1.5s        │ $0.34    │ -          │
│  neural-chat:7b     │    553   │ 0.9s        │ $0.00    │ $84.10     │
│                                                                        │
└────────────────────────────────────────────────────────────────────────┘
```

### 5.4 Routing History Timeline

```
┌─ Routing History ───────────────────────────────────────────────────┐
│                                                              [Today▼]│
│                                                                      │
│ Time     │ Request       │ Provider      │ Model      │ Status      │
│ ─────────┼───────────────┼───────────────┼────────────┼─────────── │
│ 14:35:22 │ Chat Message  │ Local         │ llama2:13b │ ✓ 1.2s    │
│ 14:34:58 │ Code Gen      │ Cloud (OpenAI)│ gpt-4      │ ✓ 0.8s    │
│ 14:34:12 │ Summarize     │ Local         │ llama2:13b │ ✓ 0.9s    │
│ 14:33:45 │ Chat Message  │ Local (F)     │ neural-7b  │ ✓ 1.1s    │
│          │               │ (tried OpenAI timeout)                   │
│ 14:32:18 │ Translate     │ Cloud (Anth.) │ claude-3   │ ✓ 1.5s    │
│ 14:31:05 │ Analysis      │ Cloud (OpenAI)│ gpt-4      │ ✗ Rate L. │
│ 14:30:22 │ Chat Message  │ Local         │ llama2:13b │ ✓ 1.0s    │
│                                                                      │
│ Legend: (F) = Fallback used, Rate L. = Rate Limited                │
└──────────────────────────────────────────────────────────────────┘
```

---

## 6. Dependencies

### 6.1 Internal Dependencies

| Component | Version | Usage |
|:----------|:--------|:------|
| LocalInferenceManager | v0.16.4-LLM | Execute local model inference |
| ModelManager | v0.16.2-LLM | Access downloaded models |
| HardwareDetector | v0.16.1-LLM | Check hardware constraints |
| ILanguageModelProvider | v0.12.3-AGT | Interface to implement |
| IAgentExecutor | v0.12.3-AGT | Agent LLM selection |
| IWorkflowEngine | v0.13.2-ORC | Workflow LLM selection |
| ISettingsService | v0.1.6a | Store user preferences |
| ILicenseService | v0.2.1a | Feature gating by tier |
| IAuditLogger | v0.11.2-SEC | Log routing decisions |
| IDatabaseService | Core | PostgreSQL schema access |

### 6.2 External Dependencies

| Package | Version | Purpose |
|:--------|:--------|:---------|
| MediatR | 12.x | Event publishing (RequestRoutedEvent, FallbackTriggeredEvent) |
| AutoMapper | 13.x | DTO mapping |
| Serilog | 3.x | Structured logging |
| Polly | 8.x | Retry policies for fallback |
| Prometheus.Client | 4.x | Metrics export (request counts, costs, latency) |

---

## 7. License Gating

### 7.1 Feature Matrix

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| Local inference provider | ❌ | ✅ | ✅ | ✅ |
| Basic routing (prefer local/cloud) | ❌ | ✅ | ✅ | ✅ |
| Manual fallback management | ❌ | ✅ | ✅ | ✅ |
| Full routing strategies | ❌ | ❌ | ✅ | ✅ |
| Automatic fallback | ❌ | ❌ | ✅ | ✅ |
| Basic analytics (request count) | ❌ | ❌ | ✅ | ✅ |
| Cost optimization | ❌ | ❌ | ❌ | ✅ |
| Detailed analytics | ❌ | ❌ | ❌ | ✅ |
| Task-type routing | ❌ | ❌ | ❌ | ✅ |
| Cost budgets | ❌ | ❌ | ❌ | ✅ |
| Per-agent overrides | ❌ | ❌ | ❌ | ✅ |
| Provider fallback chains | ❌ | ❌ | ❌ | ✅ |

---

## 8. Performance Targets

| Operation | Target (P95) |
|:----------|:-------------|
| Routing decision | <10ms |
| LocalLlmProvider availability check | <100ms |
| IFallbackManager fallback switch | <100ms |
| Cost estimate calculation | <50ms |
| Savings report generation (30 days) | <2s |
| Orchestration provider lookup | <5ms |
| Router statistics aggregation | <1s |
| Dashboard page load | <2s |
| Routing history query (30 days) | <1s |

---

## 9. Testing Strategy

### 9.1 Unit Tests

- **LocalLlmProvider**
  - GenerateAsync with various model states
  - EmbedAsync with different backends
  - GetStatusAsync reflecting accurate state
  - SetActiveModelAsync model switching
  - Error handling for unavailable models

- **ILlmRouter**
  - All routing strategies (PreferLocal, PreferCloud, CostOptimized, etc.)
  - Token count constraints
  - Cost budget enforcement
  - Task-type overrides
  - Statistics aggregation

- **IFallbackManager**
  - Primary provider failure → fallback
  - Timeout handling with exponential backoff
  - Rate limit handling
  - Max attempts enforcement
  - Attempt history tracking

- **ICostPerformanceOptimizer**
  - Cost estimation accuracy (±15%)
  - Savings calculation
  - Provider recommendation ranking
  - Per-model breakdowns

- **IOrchestrationLlmIntegration**
  - Agent override lookup
  - Task-type default application
  - Provider resolution

### 9.2 Integration Tests

- End-to-end routing: Request → Router → Provider → Response
- Fallback chain: Primary fails → Fallback succeeds → Track as fallback
- Cost tracking: Request executed → Cost recorded → Reported in analytics
- Orchestration: Agent invocation → Router selection → Execution
- Database: Routing configs persist, costs recorded, history queryable

### 9.3 Performance Tests

- Routing decision latency <10ms with 100 concurrent requests
- Dashboard analytics load <2s with 6+ months history
- Cost calculation <50ms for 10,000 request aggregation
- Fallback switch <100ms for provider timeout

### 9.4 E2E Scenarios

1. **Prefer Local Workflow**: Chat request → Routes to local → Returns local response
2. **Local Timeout Fallback**: Code gen (local preferred, timeout 5s) → Timeout → Fallback to cloud → Success
3. **Cost Budget Enforcement**: Cloud budget $100/month → 80% spent → Subsequent requests force local
4. **Task-Specific Routing**: CodeGen task → Configured to PreferCloud → Routes to gpt-4
5. **Savings Report**: 100 requests local + 50 cloud → Report shows $X.XX saved
6. **Agent Override**: Researcher agent override to PreferCloud → Researcher uses cloud despite global PreferLocal
7. **Fallback Chain Exhaustion**: All providers fail → Error with attempt history

---

## 10. Risks & Mitigations

| Risk | Impact | Mitigation |
|:-----|:-------|:-----------|
| Local model timeout during routing | User perceives slow response | Set aggressive timeout (30s), implement fallback |
| Cost estimation inaccuracy | User budget expectations mismatch | Historical calibration, conservative estimates, weekly reconciliation |
| Fallback loops (cycling between providers) | Repeated failures, poor UX | Max attempts limit (default 3), exponential backoff, clear error messaging |
| Cloud provider rate limiting | Requests rejected, service degradation | Detect 429 responses, fallback to local, log events, alert user |
| Incorrect license tier gating | Feature usage by unpaid tier | Validate tier on every routing decision, automated tests |
| Cost tracking gaps | Missed billing, disputes | Reconcile cloud invoices monthly, audit logs, transparent reporting |
| Orchestration integration complexity | Agents/workflows LLM selection confusion | Clear override hierarchy docs, override precedence unit tests |
| Large analytics queries | Dashboard slow for old data | Archiving strategy, indexed queries, caching layer |

---

## 11. MediatR Events Introduced

| Event | Handler | Purpose |
|:------|:--------|:---------|
| `RequestRoutedEvent` | Analytics, Dashboard | Track routing decision (provider, strategy, cost, latency) |
| `FallbackTriggeredEvent` | Logging, Alerts | Alert on fallback (primary provider, reason, fallback provider) |
| `CostThresholdReachedEvent` | Alerts, Notifications | Warn when approaching daily/monthly cloud budget |
| `LocalProviderStatusChangedEvent` | Dashboard, Notifications | Update UI when local provider becomes ready/unavailable |
| `ProviderConfigurationChangedEvent` | Logging, Cache Invalidation | Re-initialize router when routing config updated |
| `FallbackAttemptFailedEvent` | Logging, Diagnostics | Log when single fallback attempt fails |

---

## 12. Success Metrics

- **Platform Integration Completeness**: 100% of orchestration system (agents, workflows) can leverage local LLMs
- **Cost Savings**: Users report average 40-60% reduction in cloud API spend (via analytics dashboards)
- **Provider Availability**: Local provider available 95%+ of the time when model loaded
- **Routing Latency**: Router decisions complete in <10ms P95
- **Fallback Success Rate**: 95%+ of fallback attempts succeed within max attempts
- **Feature Adoption**: 60%+ of WriterPro users enable local routing, 80%+ of Teams adopt routing overrides
- **Analytics Reliability**: Cost estimates accurate within ±15% of actual charges
- **Governance**: 100% of Users paying below license tier report zero access to restricted routing features

---

## 13. Glossary

- **Local LLM Provider**: Drop-in ILanguageModelProvider implementation for local inference
- **Routing Strategy**: Algorithm for selecting primary provider (Prefer Local, Cost Optimized, etc.)
- **Fallback Manager**: Service managing automatic retry to alternate providers on failure
- **Cost Optimizer**: Service tracking cloud costs and calculating savings from local usage
- **Orchestration Integration**: Enabling agents/workflows to select providers with overrides
- **Provider**: LLM execution endpoint (LocalLlmProvider or CloudProvider like OpenAI)
- **Task Type**: Classification of request (chat, code, summarize, etc.) for routing decisions
- **Fallback Chain**: Ordered list of providers to attempt (primary → secondary → tertiary)
- **Routing Config**: User-settable preferences (default strategy, budget, per-task overrides)
- **LLM Override**: Per-agent or per-task provider specification superseding defaults

---

## 14. Document History

| Date | Version | Author | Changes |
|:-----|:--------|:-------|:--------|
| 2026-01-31 | 1.0 | Local LLM Lead | Initial scope breakdown for v0.16.5-LLM |

---

## Appendix A: Implementation Sequence

1. **v0.16.5e** (Week 1-2): Implement LocalLlmProvider class with all ILanguageModelProvider methods
2. **v0.16.5f** (Week 2-3): Implement ILlmRouter with all strategies, IFallbackManager with retry logic
3. **v0.16.5g** (Week 3): Implement IOrchestrationLlmIntegration for agent/workflow selection
4. **v0.16.5h** (Week 3-4): Update agent executor to use orchestration integration
5. **v0.16.5i** (Week 4): Implement ICostPerformanceOptimizer with analytics
6. **v0.16.5j** (Week 4-5): Build dashboard UI with configuration, status, and analytics
7. **Testing & Polish** (Week 5): Integration tests, performance tuning, documentation

---

## Appendix B: Configuration Examples

### Example 1: Prefer Local with Cloud Fallback

```csharp
var config = new RoutingConfig
{
    DefaultStrategy = RoutingStrategy.PreferLocal,
    EnableFallback = true,
    PreferredLocalModel = "llama2:13b",
    LocalTimeoutSeconds = 120,
    PreferredCloudProvider = "openai",
    PreferredCloudModel = "gpt-4"
};
```

### Example 2: Cost Optimization

```csharp
var config = new RoutingConfig
{
    DefaultStrategy = RoutingStrategy.CostOptimized,
    DailyCloudBudget = 10.00m,
    TaskRouting = new Dictionary<TaskType, RoutingStrategy>
    {
        { TaskType.CodeGeneration, RoutingStrategy.PreferCloud },
        { TaskType.Summarization, RoutingStrategy.PreferLocal },
        { TaskType.Chat, RoutingStrategy.CostOptimized }
    }
};
```

### Example 3: Orchestration Overrides

```csharp
var orchConfig = new OrchestrationLlmConfig
{
    DefaultStrategy = RoutingStrategy.PreferLocal,
    AgentOverrides = new Dictionary<string, LlmOverride>
    {
        {
            "code-assistant",
            new LlmOverride { ProviderId = "openai", ModelId = "gpt-4" }
        },
        {
            "summarizer",
            new LlmOverride { Strategy = RoutingStrategy.PreferLocal }
        }
    }
};
```

