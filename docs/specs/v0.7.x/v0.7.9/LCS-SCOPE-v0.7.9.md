# Lexichord Scope Breakdown: v0.7.9 — Contextual Compression Engine

**Version:** v0.7.9
**Codename:** The Compressor
**Theme:** Intelligent context compression with selective expansion
**Prerequisites:** v0.7.8 (Specialists hardened), v0.7.2 (Context Assembler complete)

---

## Executive Summary

v0.7.9 introduces the **Contextual Compression Engine**, a system for intelligently compressing conversational context while preserving semantic fidelity. This enables longer agent conversations, reduced token costs, on-demand context expansion when deeper recall is needed, and **seamless session continuity** when context limits are reached.

The session continuity features (v0.7.9j-o) provide automatic conversation compaction and handoff when approaching context window limits—similar to how tools like Claude Code handle long-running tasks that exceed a single session's capacity.

---

## Problem Statement

Long-running AI conversations and complex agent workflows accumulate context that exceeds LLM context window limits:
- Naive truncation loses critical information
- Simple summarization flattens nuance
- Users must re-explain context when windows reset
- Token costs scale linearly with conversation length
- Long-running tasks are interrupted when context is exhausted

**Impact:**
- Agents lose track of earlier decisions and commitments
- Users experience "amnesia" mid-conversation
- High token costs for extended sessions
- Poor utilization of available context window
- Complex multi-step tasks cannot complete in a single session
- Work in progress is lost during session transitions

---

## Core Concepts

### Hierarchical Compression Levels

```
┌─────────────────────────────────────────────────────────────┐
│ Level 0: Full Transcript                                    │
│ Complete conversation history, all messages verbatim        │
├─────────────────────────────────────────────────────────────┤
│ Level 1: Detailed Summary                                   │
│ Condensed narrative preserving key exchanges and outcomes   │
├─────────────────────────────────────────────────────────────┤
│ Level 2: Brief Summary                                      │
│ High-level overview: goals, decisions made, current state   │
├─────────────────────────────────────────────────────────────┤
│ Level 3: Topic Tags                                         │
│ Keywords and entity mentions for retrieval                  │
└─────────────────────────────────────────────────────────────┘
```

### Anchor Points (Never Compressed)

Certain elements are preserved regardless of compression level:
- **Commitments**: "I will...", "You should...", action items
- **Decisions**: Choices made between alternatives
- **Unresolved Questions**: Open loops that need follow-up
- **Critical Facts**: User-provided data, configurations, preferences
- **Corrections**: "Actually...", "I was wrong about..."

---

## Feature Breakdown

### v0.7.9a: Compression Level Model
**Goal:** Define the data structures for multi-level compression.

**Deliverables:**
- `CompressionLevel` enum and associated types
- `CompressedSegment` record with level metadata
- `AnchorPoint` record for preserved elements
- `ExpansionMarker` for expandable regions

**Interface:**
```csharp
public enum CompressionLevel
{
    Full = 0,
    Detailed = 1,
    Brief = 2,
    Tags = 3
}

public record CompressedSegment(
    string SegmentId,
    string ConversationId,
    CompressionLevel Level,
    string Content,
    IReadOnlyList<AnchorPoint> Anchors,
    IReadOnlyList<ExpansionMarker> ExpansionMarkers,
    int TokenCount,
    DateTimeOffset CompressedAt);

public record AnchorPoint(
    AnchorType Type,
    string Content,
    int OriginalPosition,
    float Importance);

public enum AnchorType
{
    Commitment,
    Decision,
    UnresolvedQuestion,
    CriticalFact,
    Correction,
    UserPreference
}

public record ExpansionMarker(
    string MarkerId,
    string Label,
    CompressionLevel TargetLevel,
    int StartOffset,
    int EndOffset);
```

---

### v0.7.9b: Conversation Segmenter
**Goal:** Split conversations into logical segments for independent compression.

**Deliverables:**
- `IConversationSegmenter` for splitting conversation history
- Topic-based segmentation using embedding similarity
- Time-based segmentation fallback
- Configurable segment size limits

**Interface:**
```csharp
public interface IConversationSegmenter
{
    Task<IReadOnlyList<ConversationSegment>> SegmentAsync(
        IReadOnlyList<ChatMessage> messages,
        SegmentationOptions options,
        CancellationToken ct = default);
}

public record ConversationSegment(
    string SegmentId,
    int StartIndex,
    int EndIndex,
    IReadOnlyList<ChatMessage> Messages,
    string? TopicLabel,
    int TokenCount);

public record SegmentationOptions(
    int MaxMessagesPerSegment = 20,
    int MaxTokensPerSegment = 4000,
    SegmentationStrategy Strategy = SegmentationStrategy.Topic);

public enum SegmentationStrategy
{
    FixedSize,
    Topic,
    TimeGap,
    Hybrid
}
```

---

### v0.7.9c: Anchor Extractor
**Goal:** Identify critical elements that must never be compressed away.

**Deliverables:**
- `IAnchorExtractor` for identifying anchor points
- Rule-based extraction for common patterns
- LLM-assisted extraction for complex cases
- Anchor importance scoring

**Interface:**
```csharp
public interface IAnchorExtractor
{
    Task<IReadOnlyList<AnchorPoint>> ExtractAsync(
        ConversationSegment segment,
        AnchorExtractionOptions options,
        CancellationToken ct = default);
}

public record AnchorExtractionOptions(
    bool UseRuleBasedExtraction = true,
    bool UseLLMExtraction = false,
    float MinImportanceThreshold = 0.5f);
```

**Rule-Based Patterns:**
```csharp
private static readonly Dictionary<AnchorType, Regex[]> AnchorPatterns = new()
{
    [AnchorType.Commitment] = new[]
    {
        new Regex(@"\b(I will|I'll|I am going to|Let me)\b", RegexOptions.IgnoreCase),
        new Regex(@"\b(You should|You need to|Make sure to)\b", RegexOptions.IgnoreCase)
    },
    [AnchorType.Decision] = new[]
    {
        new Regex(@"\b(decided to|chose|selected|going with)\b", RegexOptions.IgnoreCase),
        new Regex(@"\b(instead of|rather than|over)\b", RegexOptions.IgnoreCase)
    },
    [AnchorType.Correction] = new[]
    {
        new Regex(@"\b(actually|correction|I was wrong|that's not right)\b", RegexOptions.IgnoreCase)
    }
};
```

---

### v0.7.9d: Hierarchical Summarizer
**Goal:** Compress segments to each level with anchor preservation.

**Deliverables:**
- `IHierarchicalSummarizer` for multi-level compression
- Level-specific prompt templates
- Anchor injection into summaries
- Expansion marker generation

**Interface:**
```csharp
public interface IHierarchicalSummarizer
{
    Task<CompressedSegment> CompressAsync(
        ConversationSegment segment,
        IReadOnlyList<AnchorPoint> anchors,
        CompressionLevel targetLevel,
        CancellationToken ct = default);

    Task<IReadOnlyDictionary<CompressionLevel, CompressedSegment>> CompressAllLevelsAsync(
        ConversationSegment segment,
        IReadOnlyList<AnchorPoint> anchors,
        CancellationToken ct = default);
}
```

**Prompt Template (Level 2 - Brief):**
```yaml
template_id: "compression-brief"
system_prompt: |
  You are a conversation summarizer. Create a brief summary (2-3 sentences) that captures:
  - The main topic or goal
  - Key decisions made
  - Current state or next steps

  CRITICAL: You MUST include these anchor points verbatim:
  {{#anchors}}
  - [{{type}}]: "{{content}}"
  {{/anchors}}

  Format expansion markers as: [Topic →L1:segment_id]
user_prompt: |
  Summarize this conversation segment:
  {{messages}}
```

---

### v0.7.9e: Compression Storage
**Goal:** Persist compressed segments for retrieval and expansion.

**Deliverables:**
- Schema for storing multi-level compressions
- `ICompressionStore` for CRUD operations
- Efficient retrieval by conversation and level
- TTL-based cleanup for old compressions

**Schema:**
```sql
CREATE TABLE compressed_segments (
    id UUID PRIMARY KEY,
    conversation_id TEXT NOT NULL,
    segment_id TEXT NOT NULL,
    compression_level INTEGER NOT NULL,
    content TEXT NOT NULL,
    anchors JSONB NOT NULL,
    expansion_markers JSONB NOT NULL,
    token_count INTEGER NOT NULL,
    original_token_count INTEGER NOT NULL,
    compressed_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ,
    UNIQUE(conversation_id, segment_id, compression_level)
);

CREATE INDEX idx_compressed_segments_conversation
    ON compressed_segments(conversation_id, compression_level);

CREATE TABLE anchor_points (
    id UUID PRIMARY KEY,
    segment_id UUID NOT NULL REFERENCES compressed_segments(id),
    anchor_type TEXT NOT NULL,
    content TEXT NOT NULL,
    importance REAL NOT NULL,
    original_position INTEGER NOT NULL
);
```

**Interface:**
```csharp
public interface ICompressionStore
{
    Task StoreAsync(CompressedSegment segment, CancellationToken ct);
    Task<CompressedSegment?> GetAsync(string segmentId, CompressionLevel level, CancellationToken ct);
    Task<IReadOnlyList<CompressedSegment>> GetConversationSegmentsAsync(
        string conversationId,
        CompressionLevel level,
        CancellationToken ct);
    Task<IReadOnlyList<AnchorPoint>> GetAnchorsAsync(string conversationId, CancellationToken ct);
}
```

---

### v0.7.9f: Context Expander
**Goal:** Retrieve more detailed content when compressed info is insufficient.

**Deliverables:**
- `IContextExpander` for on-demand expansion
- Expansion trigger detection
- Cascading expansion (L3 → L2 → L1 → L0)
- Expansion caching to avoid repeated fetches

**Interface:**
```csharp
public interface IContextExpander
{
    Task<ExpandedContext> ExpandAsync(
        string segmentId,
        CompressionLevel fromLevel,
        CompressionLevel toLevel,
        CancellationToken ct = default);

    Task<ExpandedContext> ExpandMarkerAsync(
        ExpansionMarker marker,
        CancellationToken ct = default);
}

public record ExpandedContext(
    string SegmentId,
    CompressionLevel Level,
    string Content,
    int TokenCount,
    bool IsFullTranscript);
```

**Expansion Trigger Detection:**
```csharp
public interface IExpansionTriggerDetector
{
    Task<ExpansionDecision> ShouldExpandAsync(
        string agentResponse,
        AssembledContext currentContext,
        CancellationToken ct = default);
}

public record ExpansionDecision(
    bool ShouldExpand,
    IReadOnlyList<string> SegmentIds,
    CompressionLevel TargetLevel,
    string Reason);
```

---

### v0.7.9g: Token Budget Manager
**Goal:** Dynamically allocate context window across compression levels.

**Deliverables:**
- `ITokenBudgetManager` for budget allocation
- Priority-based allocation (recent > anchors > old)
- Dynamic rebalancing as conversation grows
- Budget visualization for debugging

**Interface:**
```csharp
public interface ITokenBudgetManager
{
    Task<BudgetAllocation> AllocateAsync(
        string conversationId,
        int totalBudget,
        BudgetAllocationOptions options,
        CancellationToken ct = default);
}

public record BudgetAllocation(
    IReadOnlyList<SegmentAllocation> Segments,
    int UsedTokens,
    int RemainingTokens,
    IReadOnlyDictionary<CompressionLevel, int> TokensByLevel);

public record SegmentAllocation(
    string SegmentId,
    CompressionLevel Level,
    int AllocatedTokens,
    AllocationReason Reason);

public enum AllocationReason
{
    Recent,
    ContainsAnchors,
    TopicRelevant,
    Expanded,
    Baseline
}

public record BudgetAllocationOptions(
    int RecentMessagesFullBudget = 2000,
    int AnchorBudget = 1000,
    float CompressionAggressiveness = 0.5f);
```

---

### v0.7.9h: Context Assembler Integration
**Goal:** Integrate compression into the existing Context Assembler (v0.7.2).

**Deliverables:**
- New `CompressionContextStrategy` for Context Assembler
- Seamless fallback to uncompressed when storage unavailable
- Configuration for compression aggressiveness
- Metrics for compression effectiveness

**Integration:**
```csharp
public class CompressionContextStrategy : IContextStrategy
{
    public string StrategyId => "compression";
    public int Priority => 10; // High priority

    private readonly IContextCompressor _compressor;
    private readonly ICompressionStore _store;
    private readonly ITokenBudgetManager _budgetManager;

    public async Task<ContextFragment?> GatherAsync(
        ContextGatheringRequest request,
        CancellationToken ct)
    {
        var conversationId = request.Hints?.GetValueOrDefault("conversation_id") as string;
        if (string.IsNullOrEmpty(conversationId)) return null;

        // Allocate budget
        var remainingBudget = request.Hints?.GetValueOrDefault("remaining_budget") as int? ?? 4000;
        var allocation = await _budgetManager.AllocateAsync(conversationId, remainingBudget, ct);

        // Assemble from compressed segments
        var content = await AssembleFromAllocation(allocation, ct);

        return new ContextFragment(
            "compression",
            "Conversation History",
            content,
            allocation.UsedTokens,
            0.9f); // High relevance
    }
}
```

---

### v0.7.9i: Hardening & Metrics
**Goal:** Ensure production readiness with testing and observability.

**Deliverables:**
- Unit tests for all compression components
- Integration tests with realistic conversations
- Compression quality benchmarks (fidelity tests)
- Performance benchmarks

**Success Metrics:**
| Metric | Target |
|--------|--------|
| Compression Ratio (L0 → L2) | 10:1 |
| Compression Ratio (L0 → L3) | 50:1 |
| Anchor Preservation Rate | 100% |
| Fidelity Score (LLM evaluation) | >0.85 |
| Compression Latency | <500ms per segment |
| Expansion Latency | <200ms |

**Fidelity Testing:**
```csharp
public interface ICompressionFidelityEvaluator
{
    Task<FidelityScore> EvaluateAsync(
        ConversationSegment original,
        CompressedSegment compressed,
        IReadOnlyList<string> testQuestions,
        CancellationToken ct = default);
}

public record FidelityScore(
    float OverallScore,
    float AnchorPreservation,
    float FactualAccuracy,
    float ContextRetention);
```

---

### v0.7.9j: Session Continuity Model
**Goal:** Define data structures for seamless session handoffs when approaching context limits.

**Deliverables:**
- `SessionHandoff` record for cross-session state transfer
- `PendingTask` record for unfinished work tracking
- `SessionState` for preserving working context
- `ContinuationDirective` for guiding resumed sessions

**Interface:**
```csharp
public record SessionHandoff(
    string HandoffId,
    string ConversationId,
    string CompactedSummary,
    IReadOnlyList<AnchorPoint> PreservedAnchors,
    IReadOnlyList<PendingTask> UnfinishedWork,
    SessionState State,
    HandoffMetadata Metadata);

public record PendingTask(
    string TaskId,
    string Description,
    TaskStatus Status,
    string? BlockingReason,
    IReadOnlyList<string> CompletedSteps,
    IReadOnlyList<string> RemainingSteps,
    float ProgressPercentage);

public enum TaskStatus
{
    NotStarted,
    InProgress,
    Blocked,
    AwaitingInput,
    NearCompletion
}

public record SessionState(
    IReadOnlyDictionary<string, object> Variables,
    IReadOnlyList<string> ActiveFiles,
    IReadOnlyList<string> RecentCommands,
    string? CurrentWorkingDirectory,
    string? ActiveBranch);

public record HandoffMetadata(
    DateTimeOffset CreatedAt,
    int OriginalTokenCount,
    int CompactedTokenCount,
    float CompressionRatio,
    string TriggerReason);

public record ContinuationDirective(
    string Summary,
    IReadOnlyList<string> ImmediateActions,
    IReadOnlyList<string> ContextReminders,
    string? UserIntent);
```

---

### v0.7.9k: Session Continuity Service
**Goal:** Orchestrate session handoffs and resumptions, including conversation-level token tracking.

**Deliverables:**
- `IConversationTokenTracker` for accumulating token usage across messages
- `ISessionContinuityService` for handoff preparation and resumption
- Automatic context limit detection based on real-time token tracking
- Handoff storage and retrieval
- Continuation directive generation

**Token Tracking Interface:**
```csharp
public interface IConversationTokenTracker
{
    /// <summary>
    /// Get current token usage for a conversation.
    /// </summary>
    Task<TokenUsage> GetUsageAsync(
        string conversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Record token usage for a new message.
    /// </summary>
    Task RecordMessageAsync(
        string conversationId,
        ChatMessage message,
        CancellationToken ct = default);

    /// <summary>
    /// Get utilization percentage against model's context window.
    /// </summary>
    float GetUtilization(TokenUsage usage, int maxContextTokens);

    /// <summary>
    /// Estimate remaining turns before context exhaustion.
    /// </summary>
    int EstimateRemainingTurns(TokenUsage usage, int maxContextTokens);

    /// <summary>
    /// Reset tracking for a conversation (e.g., after handoff).
    /// </summary>
    Task ResetAsync(string conversationId, CancellationToken ct = default);
}

public record TokenUsage(
    string ConversationId,
    int TotalTokens,
    int SystemPromptTokens,
    int ConversationHistoryTokens,
    int LastMessageTokens,
    int MessageCount,
    int AverageTokensPerTurn,
    DateTimeOffset LastUpdated)
{
    /// <summary>
    /// Calculate utilization against a context window limit.
    /// </summary>
    public float GetUtilization(int maxContextTokens) =>
        maxContextTokens > 0 ? (float)TotalTokens / maxContextTokens : 0f;

    /// <summary>
    /// Estimate how many more turns fit in the remaining context.
    /// </summary>
    public int EstimateRemainingTurns(int maxContextTokens) =>
        AverageTokensPerTurn > 0
            ? Math.Max(0, (maxContextTokens - TotalTokens) / AverageTokensPerTurn)
            : 0;
}
```

**Session Continuity Interface:**
```csharp
public interface ISessionContinuityService
{
    /// <summary>
    /// Prepare a handoff package when approaching context limits.
    /// </summary>
    Task<SessionHandoff> PrepareHandoffAsync(
        string conversationId,
        int targetTokenBudget,
        HandoffOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Resume a conversation from a handoff.
    /// </summary>
    Task<ResumedSession> ResumeFromHandoffAsync(
        string handoffId,
        CancellationToken ct = default);

    /// <summary>
    /// Check if a session should trigger a handoff.
    /// Uses IConversationTokenTracker internally for token awareness.
    /// </summary>
    Task<HandoffDecision> ShouldHandoffAsync(
        string conversationId,
        int maxTokenCount,
        CancellationToken ct = default);

    /// <summary>
    /// Generate the continuation directive for a new session.
    /// </summary>
    Task<ContinuationDirective> GenerateContinuationDirectiveAsync(
        SessionHandoff handoff,
        CancellationToken ct = default);
}

public record HandoffOptions(
    int TargetSummaryTokens = 2000,
    bool IncludeSessionState = true,
    bool ExtractPendingTasks = true,
    CompressionLevel SummaryLevel = CompressionLevel.Detailed);

public record ResumedSession(
    string NewConversationId,
    string PreviousConversationId,
    ContinuationDirective Directive,
    IReadOnlyList<AnchorPoint> RestoredAnchors,
    SessionState? RestoredState);

public record HandoffDecision(
    bool ShouldHandoff,
    float ContextUtilization,
    int EstimatedRemainingTurns,
    TokenUsage CurrentUsage,
    string Reason);
```

**Token Tracker Implementation:**
```csharp
public class ConversationTokenTracker(
    ITokenCounter tokenCounter,
    ILogger<ConversationTokenTracker> logger) : IConversationTokenTracker
{
    private readonly ConcurrentDictionary<string, TokenUsageAccumulator> _usage = new();

    public Task<TokenUsage> GetUsageAsync(string conversationId, CancellationToken ct)
    {
        if (_usage.TryGetValue(conversationId, out var accumulator))
        {
            return Task.FromResult(accumulator.ToTokenUsage());
        }

        return Task.FromResult(new TokenUsage(
            conversationId, 0, 0, 0, 0, 0, 0, DateTimeOffset.UtcNow));
    }

    public Task RecordMessageAsync(
        string conversationId,
        ChatMessage message,
        CancellationToken ct)
    {
        var tokens = tokenCounter.CountTokens(message.Content ?? string.Empty);

        var accumulator = _usage.GetOrAdd(conversationId, _ => new TokenUsageAccumulator(conversationId));
        accumulator.AddMessage(tokens, message.Role);

        logger.LogDebug(
            "Recorded {Tokens} tokens for conversation {ConversationId}. Total: {Total}",
            tokens, conversationId, accumulator.TotalTokens);

        return Task.CompletedTask;
    }

    public float GetUtilization(TokenUsage usage, int maxContextTokens) =>
        usage.GetUtilization(maxContextTokens);

    public int EstimateRemainingTurns(TokenUsage usage, int maxContextTokens) =>
        usage.EstimateRemainingTurns(maxContextTokens);

    public Task ResetAsync(string conversationId, CancellationToken ct)
    {
        _usage.TryRemove(conversationId, out _);
        return Task.CompletedTask;
    }

    private class TokenUsageAccumulator(string conversationId)
    {
        private int _systemPromptTokens;
        private int _historyTokens;
        private int _lastMessageTokens;
        private int _messageCount;
        private readonly object _lock = new();

        public int TotalTokens => _systemPromptTokens + _historyTokens;

        public void AddMessage(int tokens, string role)
        {
            lock (_lock)
            {
                if (role == "system" && _messageCount == 0)
                {
                    _systemPromptTokens = tokens;
                }
                else
                {
                    _historyTokens += _lastMessageTokens; // Previous "last" becomes history
                    _lastMessageTokens = tokens;
                }
                _messageCount++;
            }
        }

        public TokenUsage ToTokenUsage()
        {
            lock (_lock)
            {
                var avgPerTurn = _messageCount > 1
                    ? _historyTokens / (_messageCount - 1)
                    : _lastMessageTokens;

                return new TokenUsage(
                    conversationId,
                    TotalTokens + _lastMessageTokens,
                    _systemPromptTokens,
                    _historyTokens,
                    _lastMessageTokens,
                    _messageCount,
                    avgPerTurn,
                    DateTimeOffset.UtcNow);
            }
        }
    }
}
```

**Handoff Trigger Logic:**
```csharp
public async Task<HandoffDecision> ShouldHandoffAsync(
    string conversationId,
    int maxTokenCount,
    CancellationToken ct)
{
    // Get real-time token usage from tracker
    var usage = await _tokenTracker.GetUsageAsync(conversationId, ct);
    var utilization = usage.GetUtilization(maxTokenCount);
    var remainingTurns = usage.EstimateRemainingTurns(maxTokenCount);

    // Trigger handoff at 85% utilization to leave room for response
    if (utilization >= 0.85f)
    {
        return new HandoffDecision(
            ShouldHandoff: true,
            ContextUtilization: utilization,
            EstimatedRemainingTurns: remainingTurns,
            CurrentUsage: usage,
            Reason: "Context window utilization exceeded 85% threshold");
    }

    // Also trigger if we detect a long-running task pattern
    var pendingTasks = await _taskExtractor.ExtractPendingTasksAsync(conversationId, ct);
    var hasLongRunningTask = pendingTasks.Any(t =>
        t.Status == TaskStatus.InProgress &&
        t.RemainingSteps.Count > 5);

    if (utilization >= 0.70f && hasLongRunningTask)
    {
        return new HandoffDecision(
            ShouldHandoff: true,
            ContextUtilization: utilization,
            EstimatedRemainingTurns: remainingTurns,
            CurrentUsage: usage,
            Reason: "Long-running task detected with limited remaining context");
    }

    // Warn if approaching limit (optional early notification)
    if (utilization >= 0.75f && remainingTurns <= 5)
    {
        _mediator.Publish(new ContextLimitApproachingEvent(
            conversationId, utilization, remainingTurns, usage));
    }

    return new HandoffDecision(
        ShouldHandoff: false,
        ContextUtilization: utilization,
        EstimatedRemainingTurns: remainingTurns,
        CurrentUsage: usage,
        Reason: "Sufficient context remaining");
}
```

---

### v0.7.9l: Pending Task Extractor
**Goal:** Identify and track unfinished work for session continuity.

**Deliverables:**
- `IPendingTaskExtractor` for work-in-progress detection
- Integration with TodoWrite tool state
- LLM-assisted task status inference
- Progress estimation

**Interface:**
```csharp
public interface IPendingTaskExtractor
{
    Task<IReadOnlyList<PendingTask>> ExtractPendingTasksAsync(
        string conversationId,
        CancellationToken ct = default);

    Task<IReadOnlyList<PendingTask>> ExtractFromMessagesAsync(
        IReadOnlyList<ChatMessage> messages,
        CancellationToken ct = default);
}
```

**Extraction Prompt Template:**
```yaml
template_id: "pending-task-extraction"
system_prompt: |
  Analyze this conversation and identify any unfinished tasks or work in progress.

  For each task, determine:
  1. A clear description of what needs to be done
  2. Current status (not_started, in_progress, blocked, awaiting_input, near_completion)
  3. Steps already completed
  4. Steps remaining
  5. Estimated progress percentage
  6. Any blocking issues

  Focus on:
  - Code changes mentioned but not yet made
  - Tests that need to be run
  - Files that need to be created or modified
  - Questions that were asked but not fully answered
  - Explicit "TODO" or "next step" mentions

  Return as JSON array.
user_prompt: |
  Conversation to analyze:
  {{messages}}

  Current todo list state (if available):
  {{todo_state}}
```

**Task Status Detection Patterns:**
```csharp
private static readonly Dictionary<TaskStatus, string[]> StatusIndicators = new()
{
    [TaskStatus.InProgress] = new[]
    {
        "working on", "currently", "in the middle of", "let me", "I'll now"
    },
    [TaskStatus.Blocked] = new[]
    {
        "waiting for", "blocked by", "need", "requires", "can't proceed"
    },
    [TaskStatus.AwaitingInput] = new[]
    {
        "which would you prefer", "should I", "do you want", "please confirm"
    },
    [TaskStatus.NearCompletion] = new[]
    {
        "almost done", "just need to", "final step", "one more thing"
    }
};
```

---

### v0.7.9m: Handoff Storage
**Goal:** Persist handoff packages for reliable session resumption.

**Deliverables:**
- Schema for storing session handoffs
- `IHandoffStore` for CRUD operations
- TTL-based cleanup for old handoffs
- Handoff chain tracking for multi-hop continuations

**Schema:**
```sql
CREATE TABLE session_handoffs (
    id UUID PRIMARY KEY,
    conversation_id TEXT NOT NULL,
    previous_handoff_id UUID REFERENCES session_handoffs(id),
    compacted_summary TEXT NOT NULL,
    anchors JSONB NOT NULL,
    pending_tasks JSONB NOT NULL,
    session_state JSONB,
    metadata JSONB NOT NULL,
    continuation_directive JSONB,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resumed_at TIMESTAMPTZ,
    resumed_conversation_id TEXT,
    expires_at TIMESTAMPTZ NOT NULL DEFAULT NOW() + INTERVAL '30 days'
);

CREATE INDEX idx_handoffs_conversation ON session_handoffs(conversation_id);
CREATE INDEX idx_handoffs_expires ON session_handoffs(expires_at) WHERE resumed_at IS NULL;

-- Track handoff chains for long-running work
CREATE TABLE handoff_chains (
    id UUID PRIMARY KEY,
    root_conversation_id TEXT NOT NULL,
    current_handoff_id UUID REFERENCES session_handoffs(id),
    total_handoffs INTEGER NOT NULL DEFAULT 1,
    total_tokens_processed INTEGER NOT NULL,
    started_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_activity_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_chains_root ON handoff_chains(root_conversation_id);
```

**Interface:**
```csharp
public interface IHandoffStore
{
    Task<SessionHandoff> StoreAsync(SessionHandoff handoff, CancellationToken ct);
    Task<SessionHandoff?> GetAsync(string handoffId, CancellationToken ct);
    Task<SessionHandoff?> GetLatestForConversationAsync(string conversationId, CancellationToken ct);
    Task MarkResumedAsync(string handoffId, string newConversationId, CancellationToken ct);
    Task<HandoffChain?> GetChainAsync(string rootConversationId, CancellationToken ct);
    Task CleanupExpiredAsync(CancellationToken ct);
}

public record HandoffChain(
    string ChainId,
    string RootConversationId,
    IReadOnlyList<SessionHandoff> Handoffs,
    int TotalTokensProcessed,
    DateTimeOffset StartedAt,
    DateTimeOffset LastActivityAt);
```

---

### v0.7.9n: Continuation Directive Generator
**Goal:** Create clear instructions for resuming work in a new session.

**Deliverables:**
- `IContinuationDirectiveGenerator` for generating session preambles
- Context-aware directive customization
- Integration with system prompts

**Interface:**
```csharp
public interface IContinuationDirectiveGenerator
{
    Task<ContinuationDirective> GenerateAsync(
        SessionHandoff handoff,
        DirectiveOptions options,
        CancellationToken ct = default);

    string FormatAsSystemPrompt(ContinuationDirective directive);
}

public record DirectiveOptions(
    bool IncludeImmediateActions = true,
    bool IncludeContextReminders = true,
    int MaxReminderCount = 5,
    DirectiveStyle Style = DirectiveStyle.Concise);

public enum DirectiveStyle
{
    Concise,    // Brief bullet points
    Narrative,  // Flowing prose
    Structured  // Formal sections
}
```

**Directive Template:**
```yaml
template_id: "continuation-directive"
system_prompt: |
  You are resuming a conversation that was compacted due to context limits.

  ## Previous Session Summary
  {{compacted_summary}}

  ## Preserved Decisions & Commitments
  {{#anchors}}
  - [{{type}}]: {{content}}
  {{/anchors}}

  ## Unfinished Work
  {{#pending_tasks}}
  ### {{description}}
  - Status: {{status}}
  - Progress: {{progress_percentage}}%
  {{#remaining_steps}}
  - [ ] {{.}}
  {{/remaining_steps}}
  {{#blocking_reason}}
  - ⚠️ Blocked: {{blocking_reason}}
  {{/blocking_reason}}
  {{/pending_tasks}}

  ## Immediate Actions
  {{#immediate_actions}}
  1. {{.}}
  {{/immediate_actions}}

  ## Context Reminders
  {{#context_reminders}}
  - {{.}}
  {{/context_reminders}}

  Continue the work seamlessly. The user should not notice a session break.
```

---

### v0.7.9o: Session Continuity Hardening
**Goal:** Ensure reliable session handoffs with comprehensive testing.

**Deliverables:**
- Unit tests for all continuity components
- Integration tests simulating context exhaustion
- Handoff fidelity tests (information preservation)
- Chain continuity tests (multi-handoff scenarios)

**Additional Success Metrics:**
| Metric | Target |
|--------|--------|
| Handoff Preparation Latency | <2s |
| Task Extraction Accuracy | >0.90 |
| Anchor Preservation (cross-session) | 100% |
| Successful Resumption Rate | >99% |
| User-Perceived Continuity Score | >0.85 |
| Max Handoff Chain Length | 10+ |

**Continuity Fidelity Testing:**
```csharp
public interface ISessionContinuityEvaluator
{
    /// <summary>
    /// Evaluate if a resumed session maintains fidelity to the original.
    /// </summary>
    Task<ContinuityFidelityScore> EvaluateAsync(
        SessionHandoff handoff,
        ResumedSession resumed,
        IReadOnlyList<string> testQueries,
        CancellationToken ct = default);
}

public record ContinuityFidelityScore(
    float OverallScore,
    float AnchorRetention,
    float TaskContinuity,
    float ContextCoherence,
    IReadOnlyList<string> LostInformation);
```

---

## Dependencies

| Component | Source Version | Usage |
|-----------|----------------|-------|
| `IContextOrchestrator` | v0.7.2c | Context assembly integration |
| `IContextStrategy` | v0.7.2a | Strategy interface |
| `IChatCompletionService` | v0.6.1a | LLM summarization, task extraction |
| `IPromptRenderer` | v0.6.3b | Template rendering |
| `ITokenCounter` | v0.4.4c | Per-message token counting (existing) |
| `TiktokenTokenCounter` | v0.4.4c | OpenAI tiktoken implementation (existing) |
| `IMediator` | v0.0.7a | Event publishing |
| `ITodoService` | v0.x.x | Pending task state integration |

---

## MediatR Events

| Event | Description |
|-------|-------------|
| `SegmentCompressedEvent` | Conversation segment compressed |
| `AnchorExtractedEvent` | Anchor points identified |
| `ContextExpandedEvent` | User/agent triggered expansion |
| `BudgetAllocatedEvent` | Token budget distributed |
| `CompressionFidelityWarningEvent` | Low fidelity score detected |
| `SessionHandoffPreparedEvent` | Handoff package created for context limit |
| `SessionResumedEvent` | New session started from handoff |
| `HandoffChainExtendedEvent` | Multi-handoff chain continued |
| `PendingTasksExtractedEvent` | Unfinished work identified |
| `ContextLimitApproachingEvent` | Warning before forced handoff |

---

## License Gating

| Feature | Core | WriterPro | Teams | Enterprise |
|---------|------|-----------|-------|------------|
| Basic compression (L0→L2) | — | ✓ | ✓ | ✓ |
| Full hierarchy (L0→L3) | — | — | ✓ | ✓ |
| Anchor extraction (LLM) | — | — | ✓ | ✓ |
| Dynamic budget allocation | — | — | ✓ | ✓ |
| Compression analytics | — | — | — | ✓ |
| Session continuity (basic) | — | ✓ | ✓ | ✓ |
| Automatic handoff triggers | — | — | ✓ | ✓ |
| Pending task extraction | — | — | ✓ | ✓ |
| Handoff chain tracking | — | — | — | ✓ |
| Cross-session analytics | — | — | — | ✓ |

---

## Open Questions

1. **Segmentation Strategy**: Fixed turn count vs. topic detection vs. time-based?
2. **Anchor Classification**: Rule-based patterns vs. LLM classification?
3. **Expansion Triggering**: Explicit markers vs. embedding similarity vs. LLM self-assessment?
4. **Storage Location**: Alongside conversation in app DB vs. separate cache?
5. **Multi-conversation**: Should compression be per-conversation or cross-conversation?
6. **Handoff Threshold**: At what context utilization should automatic handoff trigger (80%? 85%? 90%)?
7. **User Notification**: Should users be notified before a handoff occurs, or should it be seamless?
8. **Task Granularity**: How fine-grained should pending task extraction be?
9. **Chain Limits**: Maximum handoff chain length before forcing a fresh start?

---

## Session Continuity Flow

```
┌─────────────────────────────────────────────────────────────┐
│                   Session Continuity Flow                    │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│  Session A (approaching context limit)                       │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 1. Detect context utilization > 85%                 │    │
│  │ 2. Extract pending tasks from conversation          │    │
│  │ 3. Identify anchor points (decisions, commitments)  │    │
│  │ 4. Compress conversation to target budget           │    │
│  │ 5. Capture session state (files, variables)         │    │
│  │ 6. Generate continuation directive                  │    │
│  │ 7. Store handoff package                            │    │
│  └─────────────────────────────────────────────────────┘    │
│                           │                                  │
│                           ▼                                  │
│  ┌─────────────────────────────────────────────────────┐    │
│  │              SessionHandoff Package                  │    │
│  │  • Compacted summary (2000 tokens)                  │    │
│  │  • Preserved anchors (decisions, commitments)       │    │
│  │  • Pending tasks with progress                      │    │
│  │  • Session state snapshot                           │    │
│  │  • Continuation directive                           │    │
│  └─────────────────────────────────────────────────────┘    │
│                           │                                  │
│                           ▼                                  │
│  Session B (new session)                                     │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ 1. Load handoff package                             │    │
│  │ 2. Inject continuation directive as system context  │    │
│  │ 3. Restore session state                            │    │
│  │ 4. Resume work on pending tasks                     │    │
│  │ 5. User experiences seamless continuation           │    │
│  └─────────────────────────────────────────────────────┘    │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

---

## Timeline Estimate

| Phase | Sub-versions | Relative Effort |
|-------|--------------|-----------------|
| Models & Segmentation | v0.7.9a-b | 15% |
| Compression Core | v0.7.9c-d | 20% |
| Storage & Expansion | v0.7.9e-f | 15% |
| Budget & Integration | v0.7.9g-h | 10% |
| Compression Hardening | v0.7.9i | 5% |
| Session Continuity Model | v0.7.9j | 10% |
| Continuity Service | v0.7.9k | 10% |
| Task Extraction | v0.7.9l | 5% |
| Handoff Storage | v0.7.9m | 5% |
| Directive Generation | v0.7.9n | 3% |
| Continuity Hardening | v0.7.9o | 2% |
