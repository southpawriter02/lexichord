# LDS-01: Feature Design Specification — Memory Hardening & Metrics

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-09` | Matches the Roadmap ID. |
| **Feature Name** | Memory Hardening & Metrics | The internal display name. |
| **Target Version** | `v0.8.9i` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Metrics` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The memory fabric (v0.8.9a-h) requires comprehensive testing, performance benchmarks, and production-ready observability to ensure reliable operation. Memory failures could result in lost learning and degraded agent performance.

### 2.2 The Proposed Solution
Implement a hardening layer with unit tests, integration tests, fidelity validation, performance benchmarks, and OpenTelemetry metrics. Includes memory quality evaluators for recall precision and consolidation accuracy.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a-h)
*   **NuGet Packages:**
    *   `OpenTelemetry.Api`
    *   `BenchmarkDotNet`
    *   `xUnit`
    *   `Moq`
    *   `FluentAssertions`

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Diagnostics;

/// <summary>
/// Evaluates memory recall quality and relevance.
/// </summary>
public interface IMemoryFidelityEvaluator
{
    /// <summary>
    /// Evaluate recall precision against expected results.
    /// </summary>
    /// <param name="query">The query used for recall.</param>
    /// <param name="expectedMemories">Expected memories in order of relevance.</param>
    /// <param name="actualMemories">Actual recalled memories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Fidelity score with breakdown.</returns>
    Task<FidelityScore> EvaluateRecallAsync(
        string query,
        IReadOnlyList<Memory> expectedMemories,
        IReadOnlyList<ScoredMemory> actualMemories,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluate consolidation pattern extraction quality.
    /// </summary>
    /// <param name="sourceEpisodic">Source episodic memories.</param>
    /// <param name="extractedSemantic">Extracted semantic memories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Quality score for pattern extraction.</returns>
    Task<float> EvaluateConsolidationQualityAsync(
        IReadOnlyList<Memory> sourceEpisodic,
        IReadOnlyList<Memory> extractedSemantic,
        CancellationToken ct = default);

    /// <summary>
    /// Evaluate salience ranking accuracy.
    /// </summary>
    /// <param name="memories">Memories with computed salience.</param>
    /// <param name="userRankings">User-provided relevance rankings.</param>
    /// <returns>Correlation between salience and user ranking.</returns>
    float EvaluateSalienceAccuracy(
        IReadOnlyList<ScoredMemory> memories,
        IReadOnlyList<int> userRankings);
}

/// <summary>
/// Comprehensive fidelity score for recall evaluation.
/// </summary>
/// <param name="Precision">Fraction of retrieved memories that are relevant.</param>
/// <param name="Recall">Fraction of relevant memories that are retrieved.</param>
/// <param name="F1Score">Harmonic mean of precision and recall.</param>
/// <param name="SalienceCorrelation">Correlation between salience and relevance.</param>
/// <param name="MeanReciprocalRank">Average reciprocal of first relevant result rank.</param>
/// <param name="NormalizedDCG">Normalized Discounted Cumulative Gain.</param>
public record FidelityScore(
    float Precision,
    float Recall,
    float F1Score,
    float SalienceCorrelation,
    float MeanReciprocalRank,
    float NormalizedDCG)
{
    /// <summary>
    /// Overall quality score (weighted combination).
    /// </summary>
    public float OverallScore =>
        0.25f * Precision +
        0.25f * Recall +
        0.20f * SalienceCorrelation +
        0.15f * MeanReciprocalRank +
        0.15f * NormalizedDCG;

    /// <summary>
    /// Whether the score meets quality thresholds.
    /// </summary>
    public bool MeetsThreshold => OverallScore >= 0.80f;
}

/// <summary>
/// Collects and exposes memory system metrics.
/// </summary>
public interface IMemoryMetricsCollector
{
    /// <summary>
    /// Record memory creation.
    /// </summary>
    void RecordMemoryCreated(MemoryType type, TimeSpan encodingDuration);

    /// <summary>
    /// Record memory recall operation.
    /// </summary>
    void RecordRecall(RecallMode mode, int resultCount, TimeSpan duration);

    /// <summary>
    /// Record salience calculation.
    /// </summary>
    void RecordSalienceCalculation(float salience, TimeSpan duration);

    /// <summary>
    /// Record consolidation cycle.
    /// </summary>
    void RecordConsolidation(ConsolidationReport report);

    /// <summary>
    /// Record memory capture.
    /// </summary>
    void RecordCapture(CaptureSource source, int memoriesCreated);

    /// <summary>
    /// Get current metrics snapshot.
    /// </summary>
    MemoryMetricsSnapshot GetSnapshot();
}

/// <summary>
/// Snapshot of memory system metrics.
/// </summary>
public record MemoryMetricsSnapshot(
    long TotalMemoriesCreated,
    Dictionary<MemoryType, long> MemoriesByType,
    long TotalRecallOperations,
    double AverageRecallLatencyMs,
    double P95RecallLatencyMs,
    long TotalConsolidationCycles,
    long TotalPatternsExtracted,
    long TotalContradictionsFound,
    long TotalDuplicatesMerged,
    double AverageSalienceScore,
    DateTimeOffset SnapshotTime);
```

---

## 5. Test Coverage

### 5.1 Unit Tests

```csharp
public class MemoryEncoderTests
{
    [Fact]
    public async Task ClassifyTypeAsync_SemanticContent_ReturnsSemanticType()
    {
        // Arrange
        var encoder = CreateEncoder();
        var content = "The project uses PostgreSQL for data storage";

        // Act
        var classification = await encoder.ClassifyTypeAsync(content);

        // Assert
        classification.Type.Should().Be(MemoryType.Semantic);
        classification.Confidence.Should().BeGreaterThan(0.7f);
    }

    [Fact]
    public async Task ClassifyTypeAsync_EpisodicContent_ReturnsEpisodicType()
    {
        // Arrange
        var encoder = CreateEncoder();
        var content = "Last week we fixed the authentication bug";

        // Act
        var classification = await encoder.ClassifyTypeAsync(content);

        // Assert
        classification.Type.Should().Be(MemoryType.Episodic);
    }

    [Fact]
    public async Task ClassifyTypeAsync_ProceduralContent_ReturnsProceduralType()
    {
        // Arrange
        var encoder = CreateEncoder();
        var content = "To deploy, first run npm build, then push to main";

        // Act
        var classification = await encoder.ClassifyTypeAsync(content);

        // Assert
        classification.Type.Should().Be(MemoryType.Procedural);
    }

    [Fact]
    public async Task EncodeAsync_GeneratesValidEmbedding()
    {
        // Arrange
        var encoder = CreateEncoder();
        var context = new MemoryContext("user-1", null, null, null, "test");

        // Act
        var memory = await encoder.EncodeAsync("Test content", context);

        // Assert
        memory.Embedding.Should().HaveCount(1536);
        memory.Embedding.Should().NotAllBe(0f);
    }
}

public class SalienceCalculatorTests
{
    [Fact]
    public void CalculateRecencyScore_AtHalfLife_ReturnsFiftyPercent()
    {
        // Arrange
        var calculator = CreateCalculator(halfLifeDays: 7);
        var memory = CreateMemory(lastAccessedDaysAgo: 7);
        var context = SalienceContext.WithoutQuery();

        // Act
        var breakdown = calculator.GetBreakdown(memory, context);

        // Assert
        breakdown.RecencyScore.Should().BeApproximately(0.5f, 0.05f);
    }

    [Fact]
    public void CalculateFrequencyScore_HighAccessCount_ReturnsHighScore()
    {
        // Arrange
        var calculator = CreateCalculator();
        var memory = CreateMemory(accessCount: 50);
        var context = SalienceContext.WithoutQuery();

        // Act
        var breakdown = calculator.GetBreakdown(memory, context);

        // Assert
        breakdown.FrequencyScore.Should().BeGreaterThan(0.7f);
    }

    [Fact]
    public void CalculateRelevanceScore_IdenticalEmbeddings_ReturnsOne()
    {
        // Arrange
        var calculator = CreateCalculator();
        var embedding = CreateRandomEmbedding();
        var memory = CreateMemory(embedding: embedding);
        var context = SalienceContext.WithQuery("test", embedding);

        // Act
        var breakdown = calculator.GetBreakdown(memory, context);

        // Assert
        breakdown.RelevanceScore.Should().BeApproximately(1.0f, 0.001f);
    }

    [Theory]
    [InlineData(ReinforcementReason.ExplicitUserConfirmation, 0.3f)]
    [InlineData(ReinforcementReason.SuccessfulApplication, 0.2f)]
    [InlineData(ReinforcementReason.RepeatedAccess, 0.1f)]
    public async Task ReinforceAsync_AppliesCorrectBoost(
        ReinforcementReason reason,
        float expectedBoost)
    {
        // Arrange
        var calculator = CreateCalculator();
        var memoryId = "memory-1";
        var initialSalience = 0.5f;

        // Act
        await calculator.ReinforceAsync(memoryId, reason);
        var newSalience = await GetMemorySalience(memoryId);

        // Assert
        newSalience.Should().BeApproximately(initialSalience + expectedBoost, 0.01f);
    }
}

public class MemoryRetrieverTests
{
    [Fact]
    public async Task RecallAsync_RelevantMode_ReturnsOrderedByScore()
    {
        // Arrange
        var retriever = CreateRetriever();
        await SeedMemories(10);
        var options = new RecallOptions("user-1", MaxResults: 5, Mode: RecallMode.Relevant);

        // Act
        var result = await retriever.RecallAsync("database performance", options);

        // Assert
        result.Memories.Should().HaveCount(5);
        result.Memories.Should().BeInDescendingOrder(m => m.Score);
    }

    [Fact]
    public async Task RecallAsync_RecentMode_ReturnsOrderedByLastAccessed()
    {
        // Arrange
        var retriever = CreateRetriever();
        await SeedMemoriesWithDifferentTimes(10);
        var options = new RecallOptions("user-1", MaxResults: 5, Mode: RecallMode.Recent);

        // Act
        var result = await retriever.RecallAsync("", options);

        // Assert
        result.Memories.Should().BeInDescendingOrder(
            m => m.Memory.Temporal.LastAccessed);
    }

    [Fact]
    public async Task RecallTemporalAsync_ReturnsOnlyInRange()
    {
        // Arrange
        var retriever = CreateRetriever();
        await SeedMemoriesAcrossTimeRange();
        var from = DateTimeOffset.UtcNow.AddDays(-7);
        var to = DateTimeOffset.UtcNow;

        // Act
        var result = await retriever.RecallTemporalAsync(from, to, null, "user-1");

        // Assert
        result.Memories.Should().OnlyContain(
            m => m.Memory.Temporal.CreatedAt >= from &&
                 m.Memory.Temporal.CreatedAt <= to);
    }
}

public class MemoryConsolidatorTests
{
    [Fact]
    public async Task ExtractPatternsAsync_ReturnsSemanticMemories()
    {
        // Arrange
        var consolidator = CreateConsolidator();
        await SeedEpisodicMemoriesWithPattern("user-1", 10);

        // Act
        var patterns = await consolidator.ExtractPatternsAsync(
            "user-1",
            DateTimeOffset.UtcNow.AddDays(-7));

        // Assert
        patterns.Should().NotBeEmpty();
        patterns.Should().OnlyContain(m => m.Type == MemoryType.Semantic);
    }

    [Fact]
    public async Task DetectContradictionsAsync_FindsConflictingMemories()
    {
        // Arrange
        var consolidator = CreateConsolidator();
        await SeedContradictingMemories("user-1");

        // Act
        var contradictions = await consolidator.DetectContradictionsAsync("user-1");

        // Assert
        contradictions.Should().NotBeEmpty();
        contradictions.Should().OnlyContain(c => c.ContradictionConfidence > 0.7f);
    }
}
```

### 5.2 Integration Tests

```csharp
public class MemoryFabricIntegrationTests : IClassFixture<MemoryTestFixture>
{
    private readonly MemoryTestFixture _fixture;

    [Fact]
    public async Task EndToEnd_RememberAndRecall()
    {
        // Arrange
        var content = "The API uses OAuth 2.0 for authentication";
        var context = new MemoryContext(
            "user-1", "conv-1", "project-1", null, "integration test");

        // Act: Remember
        var memory = await _fixture.Fabric.RememberAsync(content, context);

        // Assert: Memory created
        memory.Should().NotBeNull();
        memory.Type.Should().Be(MemoryType.Semantic);

        // Act: Recall
        var options = new RecallOptions("user-1", MaxResults: 10);
        var result = await _fixture.Fabric.RecallAsync("OAuth authentication", options);

        // Assert: Memory recalled
        result.Memories.Should().Contain(m => m.Memory.Id == memory.Id);
    }

    [Fact]
    public async Task EndToEnd_CorrectionCreatesSupersedingMemory()
    {
        // Arrange
        var original = await CreateMemory("API uses REST");
        var correction = "API uses gRPC";

        // Act
        var corrected = await _fixture.Fabric.UpdateMemoryAsync(
            original.Id,
            correction,
            "Changed to gRPC");

        // Assert
        corrected.Content.Should().Contain("gRPC");
        var originalAfter = await _fixture.Fabric.GetMemoryAsync(original.Id);
        originalAfter!.Status.Should().Be(MemoryStatus.Superseded);
    }

    [Fact]
    public async Task EndToEnd_ConsolidationExtractsPatterns()
    {
        // Arrange: Create episodic memories with pattern
        for (int i = 0; i < 10; i++)
        {
            await CreateEpisodicMemory($"Ran tests before commit #{i}");
        }

        // Act
        var options = new ConsolidationOptions { ExtractPatterns = true };
        var report = await _fixture.Consolidator.RunCycleAsync("user-1", options);

        // Assert
        report.PatternsExtracted.Should().BeGreaterThan(0);
    }
}
```

---

## 6. Benchmark Scenarios

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class MemoryBenchmarks
{
    private IMemoryFabric _fabric;
    private IMemoryRetriever _retriever;
    private string _userId;

    [GlobalSetup]
    public async Task Setup()
    {
        _fabric = CreateFabric();
        _retriever = CreateRetriever();
        _userId = "bench-user";
        await SeedMemories(1000);
    }

    [Benchmark(Description = "Encode single memory")]
    public async Task<Memory> EncodeSingleMemory()
    {
        var context = new MemoryContext(_userId, null, null, null, "benchmark");
        return await _fabric.RememberAsync("Test memory content", context);
    }

    [Benchmark(Description = "Recall relevant (10 results)")]
    public async Task<MemoryRecallResult> RecallRelevant()
    {
        var options = new RecallOptions(_userId, MaxResults: 10, Mode: RecallMode.Relevant);
        return await _retriever.RecallAsync("test query", options);
    }

    [Benchmark(Description = "Recall by type")]
    public async Task<MemoryRecallResult> RecallByType()
    {
        return await _retriever.RecallByTypeAsync(MemoryType.Semantic, _userId, 10);
    }

    [Benchmark(Description = "Calculate salience")]
    public float CalculateSalience()
    {
        return _salienceCalculator.CalculateSalience(_testMemory, _testContext);
    }

    [Benchmark(Description = "Consolidation cycle (100 memories)")]
    public async Task<ConsolidationReport> ConsolidationCycle()
    {
        var options = new ConsolidationOptions { MaxMemoriesPerCycle = 100 };
        return await _consolidator.RunCycleAsync(_userId, options);
    }
}
```

**Expected Benchmark Results:**

| Benchmark | Target | Measured |
|-----------|--------|----------|
| Encode single memory | <100ms | TBD |
| Recall relevant (10 results) | <50ms | TBD |
| Recall by type | <30ms | TBD |
| Calculate salience | <1ms | TBD |
| Consolidation cycle (100 memories) | <30s | TBD |

---

## 7. OpenTelemetry Metrics

```csharp
public class MemoryMetricsCollector : IMemoryMetricsCollector
{
    private static readonly Meter Meter = new("Lexichord.Agents.Memory", "1.0.0");

    private readonly Counter<long> _memoriesCreated;
    private readonly Histogram<double> _encodingLatency;
    private readonly Counter<long> _recallOperations;
    private readonly Histogram<double> _recallLatency;
    private readonly Histogram<int> _recallResultCount;
    private readonly Histogram<double> _salienceDistribution;
    private readonly Counter<long> _consolidationCycles;
    private readonly Counter<long> _patternsExtracted;
    private readonly Counter<long> _contradictionsFound;
    private readonly Counter<long> _captureEvents;

    public MemoryMetricsCollector()
    {
        _memoriesCreated = Meter.CreateCounter<long>(
            "lexichord.memory.created",
            description: "Total memories created");

        _encodingLatency = Meter.CreateHistogram<double>(
            "lexichord.memory.encoding.latency",
            unit: "ms",
            description: "Memory encoding latency");

        _recallOperations = Meter.CreateCounter<long>(
            "lexichord.memory.recall.operations",
            description: "Total recall operations");

        _recallLatency = Meter.CreateHistogram<double>(
            "lexichord.memory.recall.latency",
            unit: "ms",
            description: "Memory recall latency");

        _recallResultCount = Meter.CreateHistogram<int>(
            "lexichord.memory.recall.results",
            description: "Number of results per recall");

        _salienceDistribution = Meter.CreateHistogram<double>(
            "lexichord.memory.salience",
            description: "Distribution of salience scores");

        _consolidationCycles = Meter.CreateCounter<long>(
            "lexichord.memory.consolidation.cycles",
            description: "Total consolidation cycles");

        _patternsExtracted = Meter.CreateCounter<long>(
            "lexichord.memory.consolidation.patterns",
            description: "Patterns extracted during consolidation");

        _contradictionsFound = Meter.CreateCounter<long>(
            "lexichord.memory.consolidation.contradictions",
            description: "Contradictions detected");

        _captureEvents = Meter.CreateCounter<long>(
            "lexichord.memory.capture.events",
            description: "Memory capture events");
    }

    public void RecordMemoryCreated(MemoryType type, TimeSpan encodingDuration)
    {
        var tags = new TagList { { "type", type.ToString() } };
        _memoriesCreated.Add(1, tags);
        _encodingLatency.Record(encodingDuration.TotalMilliseconds, tags);
    }

    public void RecordRecall(RecallMode mode, int resultCount, TimeSpan duration)
    {
        var tags = new TagList { { "mode", mode.ToString() } };
        _recallOperations.Add(1, tags);
        _recallLatency.Record(duration.TotalMilliseconds, tags);
        _recallResultCount.Record(resultCount, tags);
    }

    public void RecordConsolidation(ConsolidationReport report)
    {
        _consolidationCycles.Add(1);
        _patternsExtracted.Add(report.PatternsExtracted);
        _contradictionsFound.Add(report.ContradictionsFound);
    }
}
```

---

## 8. Metrics Summary

| Metric Name | Type | Labels | Description |
|-------------|------|--------|-------------|
| `lexichord.memory.created` | Counter | type | Memories created by type |
| `lexichord.memory.encoding.latency` | Histogram | type | Encoding latency in ms |
| `lexichord.memory.recall.operations` | Counter | mode | Recall operations by mode |
| `lexichord.memory.recall.latency` | Histogram | mode | Recall latency in ms |
| `lexichord.memory.recall.results` | Histogram | mode | Results per recall |
| `lexichord.memory.salience` | Histogram | — | Salience score distribution |
| `lexichord.memory.consolidation.cycles` | Counter | — | Consolidation cycles |
| `lexichord.memory.consolidation.patterns` | Counter | — | Patterns extracted |
| `lexichord.memory.consolidation.contradictions` | Counter | — | Contradictions found |
| `lexichord.memory.capture.events` | Counter | source | Capture events by source |

---

## 9. Acceptance Criteria (QA)

1.  **[Fidelity]** Recall precision SHALL be ≥0.85 on benchmark queries.
2.  **[Fidelity]** Salience ranking accuracy SHALL be ≥0.80.
3.  **[Consolidation]** Pattern extraction accuracy SHALL be ≥0.75.
4.  **[Latency]** Memory read latency SHALL be <50ms (P95).
5.  **[Latency]** Memory write latency SHALL be <100ms (P95).
6.  **[Latency]** Consolidation cycle SHALL complete in <30s per 1000 memories.
7.  **[Storage]** Average memory storage SHALL be <2KB.
8.  **[Coverage]** Unit test coverage SHALL be ≥80%.

---

## 10. Test Scenarios

```gherkin
Scenario: Recall precision meets threshold
    Given 100 memories with known relevance to test queries
    When RecallAsync is called with 10 test queries
    Then average precision SHALL be >= 0.85

Scenario: Salience ranking correlates with user preference
    Given memories ranked by salience
    And user-provided relevance rankings
    When EvaluateSalienceAccuracy is called
    Then correlation SHALL be >= 0.80

Scenario: Read latency under load
    Given 10000 memories in storage
    When 100 concurrent recalls are executed
    Then P95 latency SHALL be < 50ms

Scenario: Consolidation completes in time
    Given 1000 memories for a user
    When RunCycleAsync is called
    Then Duration SHALL be < 30 seconds

Scenario: Metrics are recorded correctly
    Given a fresh metrics collector
    When 10 memories are created
    Then lexichord.memory.created counter SHALL be 10
```

---

## 11. Success Metrics Summary

| Metric | Target | Measurement Method |
|--------|--------|-------------------|
| Memory Recall Precision | >0.85 | Fidelity evaluator |
| Salience Ranking Accuracy | >0.80 | User ranking correlation |
| Consolidation Pattern Accuracy | >0.75 | Manual review |
| Memory Store Latency (read) | <50ms | P95 from histogram |
| Memory Store Latency (write) | <100ms | P95 from histogram |
| Consolidation Cycle Duration | <30s per 1000 | Timer metric |
| Storage per Memory | <2KB | Database analysis |
| Unit Test Coverage | ≥80% | Coverage tooling |

