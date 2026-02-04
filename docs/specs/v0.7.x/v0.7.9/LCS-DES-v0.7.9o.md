# LDS-01: Feature Design Specification — Session Continuity Hardening

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `CONT-06` | Matches the Roadmap ID. |
| **Feature Name** | Session Continuity Hardening | The internal display name. |
| **Target Version** | `v0.7.9o` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Continuity.Metrics` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The session continuity system (v0.7.9j-n) requires comprehensive testing, performance benchmarks, and production-ready metrics to ensure reliable operation. Handoff failures could result in lost context and frustrated users.

### 2.2 The Proposed Solution
Implement a hardening layer with unit tests, integration tests, fidelity validation, performance benchmarks, and OpenTelemetry metrics. This ensures the continuity system meets quality gates before production deployment.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Continuity` (v0.7.9j-n)
    *   `Lexichord.Modules.Agents.Compression` (v0.7.9a-i)
*   **NuGet Packages:**
    *   `OpenTelemetry.Api`
    *   `BenchmarkDotNet`
    *   `xUnit`
    *   `Moq`
    *   `FluentAssertions`

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Continuity.Diagnostics;

/// <summary>
/// Validates that handoff packages preserve essential information.
/// </summary>
public interface IHandoffFidelityValidator
{
    /// <summary>
    /// Validate that a handoff preserves required context.
    /// </summary>
    Task<FidelityReport> ValidateAsync(
        SessionHandoff handoff,
        string originalConversationId,
        CancellationToken ct = default);

    /// <summary>
    /// Compare resumed session context against original.
    /// </summary>
    Task<ResumptionReport> ValidateResumptionAsync(
        ResumedSession resumed,
        SessionHandoff handoff,
        CancellationToken ct = default);
}

public record FidelityReport(
    string HandoffId,
    float AnchorPreservationScore,
    float TaskPreservationScore,
    float StatePreservationScore,
    float OverallFidelityScore,
    IReadOnlyList<FidelityIssue> Issues,
    bool PassesThreshold)
{
    public bool IsValid => OverallFidelityScore >= 0.85f && !Issues.Any(i => i.Severity == IssueSeverity.Critical);
}

public record FidelityIssue(
    string Component,
    IssueSeverity Severity,
    string Description,
    string? Recommendation);

public enum IssueSeverity
{
    Info,
    Warning,
    Critical
}

public record ResumptionReport(
    string NewConversationId,
    string PreviousConversationId,
    float DirectiveCompleteness,
    float AnchorRestorationScore,
    float StateRestorationScore,
    IReadOnlyList<string> MissingElements,
    bool IsSeamless);

/// <summary>
/// Metrics collector for session continuity operations.
/// </summary>
public interface IContinuityMetricsCollector
{
    /// <summary>
    /// Record a handoff preparation operation.
    /// </summary>
    void RecordHandoffPreparation(string conversationId, TimeSpan duration, bool success, int tokensBefore, int tokensAfter);

    /// <summary>
    /// Record a session resumption operation.
    /// </summary>
    void RecordSessionResumption(string handoffId, TimeSpan duration, bool success, int anchorsRestored);

    /// <summary>
    /// Record token tracking update.
    /// </summary>
    void RecordTokenUsage(string conversationId, int totalTokens, float utilization);

    /// <summary>
    /// Record pending task extraction.
    /// </summary>
    void RecordTaskExtraction(string conversationId, int tasksFound, TimeSpan duration);

    /// <summary>
    /// Record handoff chain extension.
    /// </summary>
    void RecordChainExtension(string chainId, int chainLength);

    /// <summary>
    /// Get current metrics snapshot.
    /// </summary>
    ContinuityMetricsSnapshot GetSnapshot();
}

public record ContinuityMetricsSnapshot(
    long TotalHandoffs,
    long SuccessfulHandoffs,
    long FailedHandoffs,
    double AverageHandoffLatencyMs,
    double P95HandoffLatencyMs,
    long TotalResumptions,
    long SuccessfulResumptions,
    double AverageResumptionLatencyMs,
    double AverageAnchorPreservationRate,
    double AverageTaskPreservationRate,
    int MaxChainLength,
    DateTimeOffset SnapshotTime);
```

---

## 5. Implementation Logic

**Fidelity Validation:**
```csharp
public class HandoffFidelityValidator : IHandoffFidelityValidator
{
    private readonly IHandoffStore _handoffStore;
    private readonly IAnchorExtractor _anchorExtractor;
    private readonly IPendingTaskExtractor _taskExtractor;

    public async Task<FidelityReport> ValidateAsync(
        SessionHandoff handoff,
        string originalConversationId,
        CancellationToken ct)
    {
        var issues = new List<FidelityIssue>();

        // 1. Validate anchor preservation
        var originalAnchors = await _anchorExtractor.ExtractAnchorsAsync(originalConversationId, ct);
        var preservedAnchors = handoff.Anchors;
        var anchorScore = CalculatePreservationScore(originalAnchors, preservedAnchors, issues, "Anchor");

        // 2. Validate task preservation
        var originalTasks = await _taskExtractor.ExtractPendingTasksAsync(originalConversationId, ct);
        var preservedTasks = handoff.PendingTasks;
        var taskScore = CalculateTaskPreservationScore(originalTasks, preservedTasks, issues);

        // 3. Validate state preservation
        var stateScore = ValidateStatePreservation(handoff.SessionState, issues);

        // 4. Calculate overall fidelity
        var overall = (anchorScore * 0.4f) + (taskScore * 0.4f) + (stateScore * 0.2f);

        return new FidelityReport(
            handoff.Id,
            anchorScore,
            taskScore,
            stateScore,
            overall,
            issues,
            overall >= 0.85f);
    }

    private float CalculatePreservationScore(
        IReadOnlyList<AnchorPoint> original,
        IReadOnlyList<AnchorPoint> preserved,
        List<FidelityIssue> issues,
        string component)
    {
        if (!original.Any()) return 1.0f;

        var criticalOriginal = original.Where(a => a.Type is AnchorType.Decision or AnchorType.Commitment).ToList();
        var criticalPreserved = preserved.Where(a => a.Type is AnchorType.Decision or AnchorType.Commitment).ToList();

        // Critical anchors must ALL be preserved
        foreach (var anchor in criticalOriginal)
        {
            if (!criticalPreserved.Any(p => p.OriginalContent == anchor.OriginalContent))
            {
                issues.Add(new FidelityIssue(
                    component,
                    IssueSeverity.Critical,
                    $"Critical {anchor.Type} anchor not preserved: {anchor.OriginalContent[..Math.Min(50, anchor.OriginalContent.Length)]}...",
                    "Ensure all Decision and Commitment anchors are preserved during handoff"));
            }
        }

        var preservedCount = preserved.Count(p => original.Any(o => o.OriginalContent == p.OriginalContent));
        return (float)preservedCount / original.Count;
    }
}
```

**OpenTelemetry Metrics:**
```csharp
public class ContinuityMetricsCollector : IContinuityMetricsCollector
{
    private static readonly Meter Meter = new("Lexichord.Agents.Continuity", "1.0.0");

    private readonly Counter<long> _handoffCounter;
    private readonly Counter<long> _handoffSuccessCounter;
    private readonly Counter<long> _handoffFailureCounter;
    private readonly Histogram<double> _handoffLatency;
    private readonly Counter<long> _resumptionCounter;
    private readonly Histogram<double> _resumptionLatency;
    private readonly Gauge<int> _maxChainLength;
    private readonly Histogram<double> _utilizationHistogram;

    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentBag<double> _handoffLatencies = new();
    private readonly ConcurrentBag<double> _resumptionLatencies = new();

    public ContinuityMetricsCollector()
    {
        _handoffCounter = Meter.CreateCounter<long>(
            "lexichord.continuity.handoffs.total",
            description: "Total handoff operations attempted");

        _handoffSuccessCounter = Meter.CreateCounter<long>(
            "lexichord.continuity.handoffs.success",
            description: "Successful handoff operations");

        _handoffFailureCounter = Meter.CreateCounter<long>(
            "lexichord.continuity.handoffs.failed",
            description: "Failed handoff operations");

        _handoffLatency = Meter.CreateHistogram<double>(
            "lexichord.continuity.handoff.latency",
            unit: "ms",
            description: "Handoff preparation latency");

        _resumptionCounter = Meter.CreateCounter<long>(
            "lexichord.continuity.resumptions.total",
            description: "Total session resumptions");

        _resumptionLatency = Meter.CreateHistogram<double>(
            "lexichord.continuity.resumption.latency",
            unit: "ms",
            description: "Session resumption latency");

        _utilizationHistogram = Meter.CreateHistogram<double>(
            "lexichord.continuity.context.utilization",
            unit: "%",
            description: "Context window utilization at handoff trigger");
    }

    public void RecordHandoffPreparation(
        string conversationId,
        TimeSpan duration,
        bool success,
        int tokensBefore,
        int tokensAfter)
    {
        var tags = new TagList
        {
            { "conversation_id", conversationId },
            { "success", success.ToString() }
        };

        _handoffCounter.Add(1, tags);
        _handoffLatency.Record(duration.TotalMilliseconds, tags);
        _handoffLatencies.Add(duration.TotalMilliseconds);

        if (success)
            _handoffSuccessCounter.Add(1, tags);
        else
            _handoffFailureCounter.Add(1, tags);

        _counters.AddOrUpdate("total_handoffs", 1, (_, v) => v + 1);
        if (success)
            _counters.AddOrUpdate("successful_handoffs", 1, (_, v) => v + 1);
        else
            _counters.AddOrUpdate("failed_handoffs", 1, (_, v) => v + 1);
    }

    public void RecordSessionResumption(
        string handoffId,
        TimeSpan duration,
        bool success,
        int anchorsRestored)
    {
        var tags = new TagList
        {
            { "handoff_id", handoffId },
            { "success", success.ToString() },
            { "anchors_restored", anchorsRestored.ToString() }
        };

        _resumptionCounter.Add(1, tags);
        _resumptionLatency.Record(duration.TotalMilliseconds, tags);
        _resumptionLatencies.Add(duration.TotalMilliseconds);

        _counters.AddOrUpdate("total_resumptions", 1, (_, v) => v + 1);
        if (success)
            _counters.AddOrUpdate("successful_resumptions", 1, (_, v) => v + 1);
    }

    public void RecordTokenUsage(string conversationId, int totalTokens, float utilization)
    {
        _utilizationHistogram.Record(utilization * 100);
    }

    public ContinuityMetricsSnapshot GetSnapshot()
    {
        var handoffLatencyList = _handoffLatencies.ToList();
        var resumptionLatencyList = _resumptionLatencies.ToList();

        return new ContinuityMetricsSnapshot(
            TotalHandoffs: _counters.GetValueOrDefault("total_handoffs", 0),
            SuccessfulHandoffs: _counters.GetValueOrDefault("successful_handoffs", 0),
            FailedHandoffs: _counters.GetValueOrDefault("failed_handoffs", 0),
            AverageHandoffLatencyMs: handoffLatencyList.Any() ? handoffLatencyList.Average() : 0,
            P95HandoffLatencyMs: CalculatePercentile(handoffLatencyList, 95),
            TotalResumptions: _counters.GetValueOrDefault("total_resumptions", 0),
            SuccessfulResumptions: _counters.GetValueOrDefault("successful_resumptions", 0),
            AverageResumptionLatencyMs: resumptionLatencyList.Any() ? resumptionLatencyList.Average() : 0,
            AverageAnchorPreservationRate: 0, // Calculated from fidelity reports
            AverageTaskPreservationRate: 0,   // Calculated from fidelity reports
            MaxChainLength: (int)_counters.GetValueOrDefault("max_chain_length", 0),
            SnapshotTime: DateTimeOffset.UtcNow);
    }

    private static double CalculatePercentile(List<double> values, int percentile)
    {
        if (!values.Any()) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Math.Max(0, index)];
    }
}
```

---

## 6. Test Coverage

### 6.1 Unit Tests

```csharp
public class ConversationTokenTrackerTests
{
    [Fact]
    public async Task RecordMessageAsync_AccumulatesTokensCorrectly()
    {
        // Arrange
        var tokenCounter = new Mock<ITokenCounter>();
        tokenCounter.Setup(t => t.CountTokens(It.IsAny<string>())).Returns(100);
        var tracker = new ConversationTokenTracker(tokenCounter.Object);

        // Act
        for (int i = 0; i < 10; i++)
        {
            await tracker.RecordMessageAsync("conv-1", new ChatMessage("user", $"Message {i}"));
        }
        var usage = await tracker.GetUsageAsync("conv-1");

        // Assert
        usage.TotalTokens.Should().Be(1000);
        usage.MessageCount.Should().Be(10);
        usage.AverageTokensPerTurn.Should().Be(100);
    }

    [Theory]
    [InlineData(85000, 100000, true, "85%")]
    [InlineData(70000, 100000, false, "Sufficient")]
    [InlineData(90000, 100000, true, "85%")]
    public async Task ShouldHandoffAsync_TriggersAtCorrectThreshold(
        int currentTokens,
        int maxTokens,
        bool expectedHandoff,
        string expectedReasonContains)
    {
        // Arrange
        var service = CreateServiceWithTokens(currentTokens);

        // Act
        var decision = await service.ShouldHandoffAsync("conv-1", maxTokens);

        // Assert
        decision.ShouldHandoff.Should().Be(expectedHandoff);
        decision.Reason.Should().Contain(expectedReasonContains);
    }
}

public class PendingTaskExtractorTests
{
    [Fact]
    public async Task ExtractPendingTasksAsync_IdentifiesIncompleteTasks()
    {
        // Arrange
        var conversationWithTasks = CreateConversationWithTodoList();
        var extractor = new PendingTaskExtractor(_llmClient.Object);

        // Act
        var tasks = await extractor.ExtractPendingTasksAsync("conv-1");

        // Assert
        tasks.Should().HaveCount(3);
        tasks.Should().AllSatisfy(t => t.Status.Should().NotBe(TaskCompletionStatus.Completed));
    }

    [Fact]
    public async Task ExtractPendingTasksAsync_CalculatesProgressPercentage()
    {
        // Arrange
        var conversation = CreateConversationWithPartialProgress();
        var extractor = new PendingTaskExtractor(_llmClient.Object);

        // Act
        var tasks = await extractor.ExtractPendingTasksAsync("conv-1");

        // Assert
        var mainTask = tasks.First(t => t.Description == "Implement feature X");
        mainTask.ProgressPercentage.Should().Be(60); // 3 of 5 steps complete
        mainTask.RemainingSteps.Should().HaveCount(2);
    }
}

public class HandoffFidelityValidatorTests
{
    [Fact]
    public async Task ValidateAsync_FailsWhenCriticalAnchorMissing()
    {
        // Arrange
        var originalAnchors = new[]
        {
            new AnchorPoint(AnchorType.Decision, "Use PostgreSQL", "conv-1", 5),
            new AnchorPoint(AnchorType.Commitment, "Deliver by Friday", "conv-1", 10)
        };
        var handoff = CreateHandoffWithAnchors(new[] { originalAnchors[0] }); // Missing commitment

        var validator = new HandoffFidelityValidator(_store.Object, _anchorExtractor.Object, _taskExtractor.Object);

        // Act
        var report = await validator.ValidateAsync(handoff, "conv-1");

        // Assert
        report.PassesThreshold.Should().BeFalse();
        report.Issues.Should().Contain(i => i.Severity == IssueSeverity.Critical);
    }

    [Fact]
    public async Task ValidateAsync_PassesWithAllCriticalAnchorsPreserved()
    {
        // Arrange
        var anchors = CreateSampleAnchors();
        var handoff = CreateHandoffWithAnchors(anchors);
        var validator = new HandoffFidelityValidator(_store.Object, _anchorExtractor.Object, _taskExtractor.Object);

        // Act
        var report = await validator.ValidateAsync(handoff, "conv-1");

        // Assert
        report.PassesThreshold.Should().BeTrue();
        report.AnchorPreservationScore.Should().Be(1.0f);
    }
}
```

### 6.2 Integration Tests

```csharp
public class SessionContinuityIntegrationTests : IClassFixture<ContinuityTestFixture>
{
    private readonly ContinuityTestFixture _fixture;

    [Fact]
    public async Task EndToEnd_HandoffAndResumption_PreservesContext()
    {
        // Arrange: Create a conversation approaching context limits
        var conversationId = await _fixture.CreateConversationAsync(tokenCount: 85000, maxTokens: 100000);
        await _fixture.AddDecisionAnchorAsync(conversationId, "Use microservices architecture");
        await _fixture.AddPendingTaskAsync(conversationId, "Implement authentication", progress: 50);

        // Act: Check if handoff should occur
        var decision = await _fixture.ContinuityService.ShouldHandoffAsync(conversationId, 100000);
        decision.ShouldHandoff.Should().BeTrue();

        // Act: Prepare handoff
        var handoff = await _fixture.ContinuityService.PrepareHandoffAsync(
            conversationId,
            targetTokenBudget: 10000,
            new HandoffOptions());

        // Assert: Handoff package is complete
        handoff.Anchors.Should().Contain(a => a.OriginalContent.Contains("microservices"));
        handoff.PendingTasks.Should().Contain(t => t.Description == "Implement authentication");

        // Act: Resume in new session
        var resumed = await _fixture.ContinuityService.ResumeFromHandoffAsync(handoff.Id);

        // Assert: Context is restored
        resumed.RestoredAnchors.Should().HaveCount(handoff.Anchors.Count);
        resumed.Directive.ImmediateActions.Should().Contain(a => a.Contains("authentication"));
    }

    [Fact]
    public async Task ChainedHandoffs_MaintainContinuityAcrossMultipleHops()
    {
        // Arrange
        var chainId = Guid.NewGuid().ToString();
        SessionHandoff currentHandoff = null;

        // Act: Create chain of 5 handoffs
        for (int i = 0; i < 5; i++)
        {
            var conversationId = await _fixture.CreateConversationAsync(tokenCount: 85000, maxTokens: 100000);

            if (currentHandoff != null)
            {
                await _fixture.ContinuityService.ResumeFromHandoffAsync(currentHandoff.Id);
            }

            currentHandoff = await _fixture.ContinuityService.PrepareHandoffAsync(
                conversationId,
                targetTokenBudget: 10000,
                new HandoffOptions());
        }

        // Assert: Chain is tracked
        var chain = await _fixture.HandoffStore.GetChainAsync(currentHandoff.ChainId);
        chain.Should().HaveCount(5);
        chain.All(h => h.ChainId == currentHandoff.ChainId).Should().BeTrue();
    }

    [Fact]
    public async Task HighLoad_HandoffPreparation_MeetsLatencyTarget()
    {
        // Arrange
        var conversationIds = await Task.WhenAll(
            Enumerable.Range(0, 10)
                .Select(_ => _fixture.CreateConversationAsync(tokenCount: 85000, maxTokens: 100000)));

        var stopwatch = Stopwatch.StartNew();

        // Act: Prepare handoffs concurrently
        var handoffs = await Task.WhenAll(
            conversationIds.Select(id =>
                _fixture.ContinuityService.PrepareHandoffAsync(id, 10000, new HandoffOptions())));

        stopwatch.Stop();

        // Assert: All complete and meet latency target
        handoffs.Should().AllSatisfy(h => h.Should().NotBeNull());
        var averageLatency = stopwatch.ElapsedMilliseconds / 10.0;
        averageLatency.Should().BeLessThan(2000); // <2s target
    }
}
```

---

## 7. Benchmark Scenarios

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class ContinuityBenchmarks
{
    private ISessionContinuityService _service;
    private IConversationTokenTracker _tracker;
    private string _conversationId;

    [GlobalSetup]
    public async Task Setup()
    {
        _service = CreateService();
        _tracker = CreateTracker();
        _conversationId = await CreateLargeConversation(messageCount: 100);
    }

    [Benchmark(Description = "Token tracking for 100 messages")]
    public async Task TokenTracking_100Messages()
    {
        for (int i = 0; i < 100; i++)
        {
            await _tracker.RecordMessageAsync(_conversationId, new ChatMessage("user", GenerateMessage(100)));
        }
    }

    [Benchmark(Description = "Handoff decision check")]
    public async Task<HandoffDecision> ShouldHandoff_Check()
    {
        return await _service.ShouldHandoffAsync(_conversationId, 100000);
    }

    [Benchmark(Description = "Full handoff preparation")]
    public async Task<SessionHandoff> PrepareHandoff_Full()
    {
        return await _service.PrepareHandoffAsync(_conversationId, 10000, new HandoffOptions());
    }

    [Benchmark(Description = "Session resumption")]
    public async Task<ResumedSession> ResumeSession()
    {
        var handoff = await _service.PrepareHandoffAsync(_conversationId, 10000, new HandoffOptions());
        return await _service.ResumeFromHandoffAsync(handoff.Id);
    }

    [Benchmark(Description = "Directive generation")]
    public async Task<ContinuationDirective> GenerateDirective()
    {
        var handoff = await _service.PrepareHandoffAsync(_conversationId, 10000, new HandoffOptions());
        return await _service.GenerateContinuationDirectiveAsync(handoff);
    }
}
```

**Expected Benchmark Results:**

| Benchmark | Target | Measured |
|-----------|--------|----------|
| Token tracking (100 messages) | <50ms | TBD |
| Handoff decision check | <10ms | TBD |
| Full handoff preparation | <2000ms | TBD |
| Session resumption | <500ms | TBD |
| Directive generation | <300ms | TBD |

---

## 8. Observability & Logging

### 8.1 Metrics

| Metric Name | Type | Labels | Description |
|-------------|------|--------|-------------|
| `lexichord.continuity.handoffs.total` | Counter | conversation_id, success | Total handoff attempts |
| `lexichord.continuity.handoffs.success` | Counter | conversation_id | Successful handoffs |
| `lexichord.continuity.handoffs.failed` | Counter | conversation_id, reason | Failed handoffs |
| `lexichord.continuity.handoff.latency` | Histogram | conversation_id | Handoff preparation time (ms) |
| `lexichord.continuity.resumptions.total` | Counter | handoff_id, success | Total session resumptions |
| `lexichord.continuity.resumption.latency` | Histogram | handoff_id | Resumption time (ms) |
| `lexichord.continuity.context.utilization` | Histogram | conversation_id | Context utilization at trigger (%) |
| `lexichord.continuity.chain.length` | Gauge | chain_id | Current handoff chain length |
| `lexichord.continuity.anchors.preserved` | Counter | conversation_id, type | Anchors preserved per handoff |
| `lexichord.continuity.tasks.extracted` | Counter | conversation_id | Pending tasks extracted |
| `lexichord.continuity.fidelity.score` | Histogram | conversation_id | Fidelity validation score |

### 8.2 Logging

```
[INFO]  [CONT:TRACK] Token usage updated: {ConversationId} at {TotalTokens} tokens ({Utilization}%)
[WARN]  [CONT:LIMIT] Context limit approaching: {ConversationId} - {RemainingTurns} turns remaining
[INFO]  [CONT:HANDOFF] Preparing handoff: {ConversationId} targeting {TargetTokenBudget} tokens
[INFO]  [CONT:HANDOFF] Handoff prepared: {HandoffId} with {AnchorCount} anchors, {TaskCount} tasks
[ERROR] [CONT:HANDOFF] Handoff failed: {ConversationId} - {ErrorMessage}
[INFO]  [CONT:RESUME] Resuming session: {HandoffId} -> {NewConversationId}
[INFO]  [CONT:RESUME] Session resumed: {NewConversationId} with {RestoredAnchorCount} anchors
[WARN]  [CONT:FIDELITY] Low fidelity score: {HandoffId} at {FidelityScore} (threshold: 0.85)
[INFO]  [CONT:CHAIN] Chain extended: {ChainId} now has {ChainLength} handoffs
```

---

## 9. Acceptance Criteria (QA)

1.  **[Fidelity]** `ValidateAsync` SHALL detect missing critical anchors.
2.  **[Fidelity]** Fidelity score SHALL be ≥0.85 for all valid handoffs.
3.  **[Metrics]** All OpenTelemetry metrics SHALL be recorded during operations.
4.  **[Latency]** Handoff preparation SHALL complete in <2 seconds.
5.  **[Latency]** Session resumption SHALL complete in <500ms.
6.  **[Chain]** Handoff chains SHALL support ≥10 consecutive handoffs.
7.  **[Preservation]** 100% of critical anchors SHALL be preserved across handoffs.
8.  **[Coverage]** Unit test coverage SHALL be ≥80% for continuity module.

---

## 10. Test Scenarios

```gherkin
Scenario: Fidelity validation detects missing anchors
    Given a handoff package missing a Decision anchor
    When ValidateAsync is called
    Then FidelityReport.Issues SHALL contain a Critical issue
    And PassesThreshold SHALL be false

Scenario: Metrics are recorded during handoff
    Given a conversation requiring handoff
    When PrepareHandoffAsync completes
    Then lexichord.continuity.handoffs.total SHALL increment by 1
    And lexichord.continuity.handoff.latency SHALL record the duration

Scenario: Chain length is tracked across handoffs
    Given an existing chain with 5 handoffs
    When a new handoff is prepared continuing the chain
    Then lexichord.continuity.chain.length SHALL show 6
    And HandoffChainExtendedEvent SHALL be published

Scenario: Performance meets latency targets
    Given a conversation with 1000 messages
    When PrepareHandoffAsync is called
    Then completion time SHALL be less than 2000ms
    And all anchors SHALL be preserved

Scenario: Resumption restores full context
    Given a handoff with 5 anchors and 3 pending tasks
    When ResumeFromHandoffAsync is called
    Then RestoredAnchors SHALL have count 5
    And Directive.ImmediateActions SHALL reference pending tasks
```

---

## 11. Success Metrics Summary

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Handoff Preparation Latency | <2s | P95 from histogram |
| Session Resumption Latency | <500ms | P95 from histogram |
| Anchor Preservation Rate | 100% | Fidelity validation |
| Task Extraction Accuracy | >90% | Manual review of sample |
| Successful Resumption Rate | >99% | Success counter / total |
| Max Handoff Chain Length | ≥10 | Integration test |
| Fidelity Score (average) | >0.85 | Histogram average |
| Unit Test Coverage | ≥80% | Coverage tooling |

