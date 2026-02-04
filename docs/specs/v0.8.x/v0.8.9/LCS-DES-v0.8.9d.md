# LDS-01: Feature Design Specification — Salience Calculator

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-04` | Matches the Roadmap ID. |
| **Feature Name** | Salience Calculator | The internal display name. |
| **Target Version** | `v0.8.9d` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Salience` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
Not all memories are equally important. The system needs to prioritize which memories to surface during retrieval based on multiple factors: how recently they were accessed, how often they're used, their inherent importance, and relevance to the current context.

### 2.2 The Proposed Solution
Implement `ISalienceCalculator` with a multi-factor scoring formula combining recency (exponential decay), frequency (logarithmic scaling), importance (explicit or inferred), and relevance (embedding similarity). Configurable weights allow tuning per use case.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a models)
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9b storage)
*   **NuGet Packages:**
    *   None additional

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Salience calculation is part of memory retrieval.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Calculates and manages memory salience scores.
/// </summary>
public interface ISalienceCalculator
{
    /// <summary>
    /// Calculate the current salience score for a memory.
    /// </summary>
    /// <param name="memory">The memory to score.</param>
    /// <param name="context">Context for relevance calculation.</param>
    /// <returns>Computed salience score (0.0 to 1.0).</returns>
    float CalculateSalience(Memory memory, SalienceContext context);

    /// <summary>
    /// Update the salience score for a memory in storage.
    /// </summary>
    /// <param name="memoryId">The memory to update.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateSalienceAsync(string memoryId, CancellationToken ct = default);

    /// <summary>
    /// Apply time-based decay to all active memories.
    /// </summary>
    /// <param name="timePeriod">Time since last decay cycle.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Number of memories updated.</returns>
    Task<int> DecayAllSalienceAsync(TimeSpan timePeriod, CancellationToken ct = default);

    /// <summary>
    /// Boost salience for a memory (reinforcement).
    /// </summary>
    /// <param name="memoryId">The memory to reinforce.</param>
    /// <param name="reason">Why the memory is being reinforced.</param>
    /// <param name="ct">Cancellation token.</param>
    Task ReinforceAsync(string memoryId, ReinforcementReason reason, CancellationToken ct = default);

    /// <summary>
    /// Get detailed breakdown of salience factors.
    /// </summary>
    /// <param name="memory">The memory to analyze.</param>
    /// <param name="context">Context for relevance calculation.</param>
    /// <returns>Detailed factor breakdown.</returns>
    SalienceBreakdown GetBreakdown(Memory memory, SalienceContext context);
}

/// <summary>
/// Context for calculating salience with relevance component.
/// </summary>
/// <param name="CurrentQuery">Optional query for relevance calculation.</param>
/// <param name="QueryEmbedding">Optional query embedding for similarity.</param>
/// <param name="CurrentTime">Reference time for recency calculation.</param>
public record SalienceContext(
    string? CurrentQuery,
    float[]? QueryEmbedding,
    DateTimeOffset CurrentTime)
{
    /// <summary>
    /// Create a context without query (no relevance component).
    /// </summary>
    public static SalienceContext WithoutQuery() =>
        new(null, null, DateTimeOffset.UtcNow);

    /// <summary>
    /// Create a context with query embedding.
    /// </summary>
    public static SalienceContext WithQuery(string query, float[] embedding) =>
        new(query, embedding, DateTimeOffset.UtcNow);
}

/// <summary>
/// Configurable weights for salience factors.
/// </summary>
public record SalienceWeights
{
    /// <summary>
    /// Weight for recency factor (default 0.25).
    /// </summary>
    public float RecencyWeight { get; init; } = 0.25f;

    /// <summary>
    /// Weight for access frequency factor (default 0.20).
    /// </summary>
    public float FrequencyWeight { get; init; } = 0.20f;

    /// <summary>
    /// Weight for importance factor (default 0.25).
    /// </summary>
    public float ImportanceWeight { get; init; } = 0.25f;

    /// <summary>
    /// Weight for relevance factor (default 0.30).
    /// </summary>
    public float RelevanceWeight { get; init; } = 0.30f;

    /// <summary>
    /// Validate weights sum to 1.0.
    /// </summary>
    public bool IsValid =>
        Math.Abs(RecencyWeight + FrequencyWeight + ImportanceWeight + RelevanceWeight - 1.0f) < 0.001f;

    /// <summary>
    /// Default weights configuration.
    /// </summary>
    public static SalienceWeights Default => new();

    /// <summary>
    /// Weights emphasizing recency (for time-sensitive contexts).
    /// </summary>
    public static SalienceWeights RecencyFocused => new()
    {
        RecencyWeight = 0.40f,
        FrequencyWeight = 0.15f,
        ImportanceWeight = 0.20f,
        RelevanceWeight = 0.25f
    };

    /// <summary>
    /// Weights emphasizing relevance (for search-focused contexts).
    /// </summary>
    public static SalienceWeights RelevanceFocused => new()
    {
        RecencyWeight = 0.15f,
        FrequencyWeight = 0.15f,
        ImportanceWeight = 0.20f,
        RelevanceWeight = 0.50f
    };
}

/// <summary>
/// Detailed breakdown of salience score components.
/// </summary>
/// <param name="RecencyScore">Score from time decay (0.0 to 1.0).</param>
/// <param name="FrequencyScore">Score from access frequency (0.0 to 1.0).</param>
/// <param name="ImportanceScore">Score from explicit importance (0.0 to 1.0).</param>
/// <param name="RelevanceScore">Score from embedding similarity (0.0 to 1.0).</param>
/// <param name="FinalScore">Weighted combination of all factors.</param>
/// <param name="Weights">Weights used for calculation.</param>
public record SalienceBreakdown(
    float RecencyScore,
    float FrequencyScore,
    float ImportanceScore,
    float RelevanceScore,
    float FinalScore,
    SalienceWeights Weights);

/// <summary>
/// Reasons for reinforcing a memory.
/// </summary>
public enum ReinforcementReason
{
    /// <summary>
    /// User explicitly confirmed the memory is correct.
    /// </summary>
    ExplicitUserConfirmation,

    /// <summary>
    /// Memory was successfully applied in context.
    /// </summary>
    SuccessfulApplication,

    /// <summary>
    /// Memory was accessed multiple times.
    /// </summary>
    RepeatedAccess,

    /// <summary>
    /// Memory was corrected by user (new version is more salient).
    /// </summary>
    UserCorrection
}

/// <summary>
/// Configuration for salience calculation behavior.
/// </summary>
public record SalienceOptions
{
    /// <summary>
    /// Half-life for recency decay in days.
    /// </summary>
    public double RecencyHalfLifeDays { get; init; } = 7.0;

    /// <summary>
    /// Maximum access count for frequency scaling.
    /// </summary>
    public int FrequencyMaxCount { get; init; } = 100;

    /// <summary>
    /// Default relevance score when no query is provided.
    /// </summary>
    public float DefaultRelevance { get; init; } = 0.5f;

    /// <summary>
    /// Minimum salience before archival consideration.
    /// </summary>
    public float ArchivalThreshold { get; init; } = 0.1f;

    /// <summary>
    /// Reinforcement boost amounts by reason.
    /// </summary>
    public Dictionary<ReinforcementReason, float> ReinforcementBoosts { get; init; } = new()
    {
        [ReinforcementReason.ExplicitUserConfirmation] = 0.3f,
        [ReinforcementReason.SuccessfulApplication] = 0.2f,
        [ReinforcementReason.RepeatedAccess] = 0.1f,
        [ReinforcementReason.UserCorrection] = 0.25f
    };
}
```

---

## 5. Implementation Logic

**Salience Calculator Implementation:**
```csharp
public class SalienceCalculator : ISalienceCalculator
{
    private readonly IMemoryStore _memoryStore;
    private readonly SalienceWeights _weights;
    private readonly SalienceOptions _options;
    private readonly ILogger<SalienceCalculator> _logger;

    public float CalculateSalience(Memory memory, SalienceContext context)
    {
        var breakdown = GetBreakdown(memory, context);
        return breakdown.FinalScore;
    }

    public SalienceBreakdown GetBreakdown(Memory memory, SalienceContext context)
    {
        var recency = CalculateRecencyScore(memory.Temporal.LastAccessed, context.CurrentTime);
        var frequency = CalculateFrequencyScore(memory.Temporal.AccessCount);
        var importance = GetImportanceScore(memory);
        var relevance = CalculateRelevanceScore(memory.Embedding, context.QueryEmbedding);

        var final = _weights.RecencyWeight * recency
                  + _weights.FrequencyWeight * frequency
                  + _weights.ImportanceWeight * importance
                  + _weights.RelevanceWeight * relevance;

        return new SalienceBreakdown(
            RecencyScore: recency,
            FrequencyScore: frequency,
            ImportanceScore: importance,
            RelevanceScore: relevance,
            FinalScore: Math.Clamp(final, 0f, 1f),
            Weights: _weights);
    }

    /// <summary>
    /// Exponential decay based on time since last access.
    /// </summary>
    private float CalculateRecencyScore(DateTimeOffset lastAccess, DateTimeOffset now)
    {
        var daysSinceAccess = (now - lastAccess).TotalDays;

        // Exponential decay: score = e^(-t/halflife * ln(2))
        // This gives exactly 0.5 at half-life
        var decayConstant = Math.Log(2) / _options.RecencyHalfLifeDays;
        var score = Math.Exp(-daysSinceAccess * decayConstant);

        return (float)Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>
    /// Logarithmic scaling of access frequency.
    /// </summary>
    private float CalculateFrequencyScore(int accessCount)
    {
        if (accessCount <= 0) return 0f;

        // Logarithmic scaling: log(count+1) / log(max+1)
        // Prevents runaway scores for very frequently accessed memories
        var logCount = Math.Log(accessCount + 1);
        var logMax = Math.Log(_options.FrequencyMaxCount + 1);
        var score = logCount / logMax;

        return (float)Math.Clamp(score, 0.0, 1.0);
    }

    /// <summary>
    /// Get importance from confidence trajectory or current salience.
    /// </summary>
    private float GetImportanceScore(Memory memory)
    {
        // Use the latest confidence from trajectory if available
        if (memory.Temporal.ConfidenceTrajectory.Any())
        {
            return memory.Temporal.ConfidenceTrajectory.Last();
        }

        // Fall back to current salience
        return memory.CurrentSalience;
    }

    /// <summary>
    /// Cosine similarity between memory and query embeddings.
    /// </summary>
    private float CalculateRelevanceScore(float[] memoryEmbedding, float[]? queryEmbedding)
    {
        if (queryEmbedding == null)
        {
            return _options.DefaultRelevance;
        }

        return CosineSimilarity(memoryEmbedding, queryEmbedding);
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length)
            throw new ArgumentException("Vectors must have same length");

        float dotProduct = 0f;
        float normA = 0f;
        float normB = 0f;

        for (int i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }

        var denominator = Math.Sqrt(normA) * Math.Sqrt(normB);
        if (denominator < 1e-10) return 0f;

        return (float)(dotProduct / denominator);
    }

    public async Task UpdateSalienceAsync(string memoryId, CancellationToken ct)
    {
        var memory = await _memoryStore.GetByIdAsync(memoryId, ct);
        if (memory == null) return;

        var context = SalienceContext.WithoutQuery();
        var newSalience = CalculateSalience(memory, context);

        await _memoryStore.UpdateSalienceAsync(memoryId, newSalience, ct);

        _logger.LogDebug(
            "[MEM:SALIENCE] Updated salience for {MemoryId}: {OldSalience} -> {NewSalience}",
            memoryId, memory.CurrentSalience, newSalience);
    }

    public async Task<int> DecayAllSalienceAsync(TimeSpan timePeriod, CancellationToken ct)
    {
        // Calculate decay factor based on time period
        var periodDays = timePeriod.TotalDays;
        var decayConstant = Math.Log(2) / _options.RecencyHalfLifeDays;
        var decayFactor = (float)Math.Exp(-periodDays * decayConstant);

        var count = await _memoryStore.DecayAllSalienceAsync(decayFactor, ct);

        _logger.LogInformation(
            "[MEM:SALIENCE] Decayed salience for {Count} memories by factor {Factor}",
            count, decayFactor);

        return count;
    }

    public async Task ReinforceAsync(string memoryId, ReinforcementReason reason, CancellationToken ct)
    {
        var memory = await _memoryStore.GetByIdAsync(memoryId, ct);
        if (memory == null) return;

        var boost = _options.ReinforcementBoosts.GetValueOrDefault(reason, 0.1f);
        var newSalience = Math.Min(1.0f, memory.CurrentSalience + boost);

        await _memoryStore.UpdateSalienceAsync(memoryId, newSalience, ct);
        await _memoryStore.RecordConfidenceAsync(memoryId, newSalience, ct);

        _logger.LogInformation(
            "[MEM:SALIENCE] Reinforced {MemoryId} ({Reason}): {OldSalience} -> {NewSalience}",
            memoryId, reason, memory.CurrentSalience, newSalience);
    }
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Memory.Salience.Calculated` (Counter)
*   **Metric:** `Agents.Memory.Salience.Distribution` (Histogram)
*   **Metric:** `Agents.Memory.Salience.Reinforcements` (Counter by Reason)
*   **Metric:** `Agents.Memory.Salience.DecayedCount` (Counter)
*   **Log (Debug):** `[MEM:SALIENCE] Updated salience for {MemoryId}: {OldSalience} -> {NewSalience}`
*   **Log (Info):** `[MEM:SALIENCE] Decayed salience for {Count} memories by factor {Factor}`
*   **Log (Info):** `[MEM:SALIENCE] Reinforced {MemoryId} ({Reason}): {OldSalience} -> {NewSalience}`

---

## 7. Acceptance Criteria (QA)

1.  **[Recency]** Score SHALL be 1.0 for just-accessed memory.
2.  **[Recency]** Score SHALL be 0.5 at half-life (7 days default).
3.  **[Frequency]** Score SHALL increase logarithmically with access count.
4.  **[Relevance]** Score SHALL be cosine similarity when query provided.
5.  **[Relevance]** Score SHALL be default (0.5) when no query provided.
6.  **[Weights]** Final score SHALL be weighted sum of factors.
7.  **[Reinforcement]** Reinforcement SHALL boost salience by configured amount.
8.  **[Decay]** Bulk decay SHALL apply to all active memories.

---

## 8. Test Scenarios

```gherkin
Scenario: Recency decay at half-life
    Given a memory last accessed 7 days ago
    And RecencyHalfLifeDays is 7
    When CalculateSalience is called
    Then RecencyScore SHALL be approximately 0.5

Scenario: Frequency scaling is logarithmic
    Given a memory with AccessCount 10
    And FrequencyMaxCount is 100
    When GetBreakdown is called
    Then FrequencyScore SHALL be approximately log(11)/log(101)

Scenario: Relevance with query embedding
    Given a memory with embedding [0.5, 0.5, 0.5]
    And query embedding [0.5, 0.5, 0.5]
    When CalculateSalience is called
    Then RelevanceScore SHALL be 1.0

Scenario: Reinforcement boosts salience
    Given a memory with CurrentSalience 0.5
    When ReinforceAsync is called with ExplicitUserConfirmation
    Then CurrentSalience SHALL be 0.8 (0.5 + 0.3 boost)

Scenario: Bulk decay applies to all memories
    Given 100 active memories
    When DecayAllSalienceAsync is called with 7 day period
    Then all memories SHALL have salience multiplied by ~0.5
```

