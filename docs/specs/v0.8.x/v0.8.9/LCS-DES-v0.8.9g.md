# LDS-01: Feature Design Specification — Memory Consolidator

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-07` | Matches the Roadmap ID. |
| **Feature Name** | Memory Consolidator | The internal display name. |
| **Target Version** | `v0.8.9g` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Teams | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Consolidator` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
Over time, the memory fabric accumulates redundant, outdated, and contradictory memories. The system needs background processing to extract patterns from episodic memories, merge duplicates, apply salience decay, detect contradictions, and archive stale memories.

### 2.2 The Proposed Solution
Implement `IMemoryConsolidator` for background memory organization inspired by human memory consolidation during sleep. The consolidator runs periodic cycles that replay recent memories, extract semantic patterns, strengthen frequently accessed memories, link related memories, and prune low-salience entries. Integrates with v0.5.9 deduplication for merging similar memories.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a-f)
    *   `Lexichord.Modules.Rag` (v0.5.9 `IDeduplicationService`)
    *   `Lexichord.Modules.Agents` (`IChatCompletionService`)
*   **NuGet Packages:**
    *   `MediatR`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Automatic consolidation requires Teams license.
*   **Fallback Experience:**
    *   Writer Pro users can trigger manual consolidation but not automatic scheduling.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Background process for memory organization and optimization.
/// Inspired by memory consolidation during sleep.
/// </summary>
public interface IMemoryConsolidator
{
    /// <summary>
    /// Run a full consolidation cycle for a user.
    /// </summary>
    /// <param name="userId">The user whose memories to consolidate.</param>
    /// <param name="options">Consolidation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Report of consolidation actions taken.</returns>
    Task<ConsolidationReport> RunCycleAsync(
        string userId,
        ConsolidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Run consolidation for all users (scheduled job).
    /// </summary>
    /// <param name="options">Consolidation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Aggregate report across all users.</returns>
    Task<AggregateConsolidationReport> ConsolidateAllAsync(
        ConsolidationOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Extract semantic patterns from recent episodic memories.
    /// </summary>
    /// <param name="userId">The user context.</param>
    /// <param name="sinceDate">Look at episodic memories since this date.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Extracted patterns as new semantic memories.</returns>
    Task<IReadOnlyList<Memory>> ExtractPatternsAsync(
        string userId,
        DateTimeOffset sinceDate,
        CancellationToken ct = default);

    /// <summary>
    /// Detect contradictions within a user's memory.
    /// </summary>
    /// <param name="userId">The user context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Pairs of contradicting memories.</returns>
    Task<IReadOnlyList<ContradictionPair>> DetectContradictionsAsync(
        string userId,
        CancellationToken ct = default);
}

/// <summary>
/// Options for memory consolidation.
/// </summary>
public record ConsolidationOptions
{
    /// <summary>
    /// Whether to extract patterns from episodic memories.
    /// </summary>
    public bool ExtractPatterns { get; init; } = true;

    /// <summary>
    /// Whether to apply salience decay.
    /// </summary>
    public bool DecaySalience { get; init; } = true;

    /// <summary>
    /// Whether to prune archived/low-salience memories.
    /// </summary>
    public bool PruneArchived { get; init; } = true;

    /// <summary>
    /// Whether to detect contradictions.
    /// </summary>
    public bool DetectContradictions { get; init; } = true;

    /// <summary>
    /// Whether to merge duplicate memories.
    /// </summary>
    public bool MergeDuplicates { get; init; } = true;

    /// <summary>
    /// Maximum age for memories to keep (null = no limit).
    /// </summary>
    public TimeSpan? MaxAge { get; init; } = null;

    /// <summary>
    /// Minimum salience before archival.
    /// </summary>
    public float ArchivalThreshold { get; init; } = 0.1f;

    /// <summary>
    /// Similarity threshold for duplicate detection.
    /// </summary>
    public float DuplicateThreshold { get; init; } = 0.95f;

    /// <summary>
    /// Maximum memories to process per cycle.
    /// </summary>
    public int MaxMemoriesPerCycle { get; init; } = 1000;
}

/// <summary>
/// Report of a consolidation cycle.
/// </summary>
public record ConsolidationReport(
    string UserId,
    int MemoriesProcessed,
    int PatternsExtracted,
    int MemoriesArchived,
    int ContradictionsFound,
    int DuplicatesMerged,
    int LinksCreated,
    TimeSpan Duration,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Aggregate report across all users.
/// </summary>
public record AggregateConsolidationReport(
    int UsersProcessed,
    int TotalMemoriesProcessed,
    int TotalPatternsExtracted,
    int TotalArchived,
    int TotalContradictions,
    int TotalDuplicatesMerged,
    TimeSpan TotalDuration,
    IReadOnlyList<ConsolidationReport> UserReports);

/// <summary>
/// A pair of contradicting memories.
/// </summary>
public record ContradictionPair(
    Memory First,
    Memory Second,
    float ContradictionConfidence,
    string ExplanationOfContradiction);
```

---

## 5. Implementation Logic

**Consolidation Cycle:**
```csharp
public class MemoryConsolidator : IMemoryConsolidator
{
    private readonly IMemoryStore _store;
    private readonly IMemoryFabric _fabric;
    private readonly IMemoryRetriever _retriever;
    private readonly ISalienceCalculator _salienceCalculator;
    private readonly IDeduplicationService _deduplicationService;
    private readonly IChatCompletionService _llm;
    private readonly IMediator _mediator;
    private readonly ILogger<MemoryConsolidator> _logger;

    public async Task<ConsolidationReport> RunCycleAsync(
        string userId,
        ConsolidationOptions options,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var warnings = new List<string>();

        int memoriesProcessed = 0;
        int patternsExtracted = 0;
        int memoriesArchived = 0;
        int contradictionsFound = 0;
        int duplicatesMerged = 0;
        int linksCreated = 0;

        _logger.LogInformation(
            "[MEM:CONSOLIDATE] Starting consolidation cycle for user {UserId}",
            userId);

        try
        {
            // 1. REPLAY: Review recent episodic memories
            if (options.ExtractPatterns)
            {
                var patterns = await ExtractPatternsAsync(userId, DateTimeOffset.UtcNow.AddDays(-7), ct);
                patternsExtracted = patterns.Count;
                memoriesProcessed += patterns.Count;

                _logger.LogInformation(
                    "[MEM:CONSOLIDATE] Extracted {Count} patterns from episodic memories",
                    patternsExtracted);
            }

            // 2. DECAY: Apply salience decay
            if (options.DecaySalience)
            {
                var decayed = await _salienceCalculator.DecayAllSalienceAsync(TimeSpan.FromDays(1), ct);
                memoriesProcessed += decayed;
            }

            // 3. DEDUPLICATE: Merge similar memories
            if (options.MergeDuplicates)
            {
                duplicatesMerged = await MergeDuplicatesAsync(userId, options.DuplicateThreshold, ct);
            }

            // 4. DETECT CONTRADICTIONS
            if (options.DetectContradictions)
            {
                var contradictions = await DetectContradictionsAsync(userId, ct);
                contradictionsFound = contradictions.Count;

                foreach (var pair in contradictions)
                {
                    // Link contradicting memories
                    await _fabric.LinkMemoriesAsync(
                        pair.First.Id,
                        pair.Second.Id,
                        MemoryLinkType.Contradicts,
                        pair.ContradictionConfidence,
                        ct);
                    linksCreated++;
                }
            }

            // 5. PRUNE: Archive low-salience memories
            if (options.PruneArchived)
            {
                memoriesArchived = await ArchiveLowSalienceAsync(
                    userId,
                    options.ArchivalThreshold,
                    options.MaxAge,
                    ct);
            }
        }
        catch (Exception ex)
        {
            warnings.Add($"Error during consolidation: {ex.Message}");
            _logger.LogError(ex, "[MEM:CONSOLIDATE] Error during consolidation for {UserId}", userId);
        }

        stopwatch.Stop();

        var report = new ConsolidationReport(
            userId,
            memoriesProcessed,
            patternsExtracted,
            memoriesArchived,
            contradictionsFound,
            duplicatesMerged,
            linksCreated,
            stopwatch.Elapsed,
            warnings);

        await _mediator.Publish(new ConsolidationCompletedEvent(report), ct);

        _logger.LogInformation(
            "[MEM:CONSOLIDATE] Completed cycle for {UserId}: {Processed} processed, {Patterns} patterns, {Archived} archived in {Duration}ms",
            userId, memoriesProcessed, patternsExtracted, memoriesArchived, stopwatch.ElapsedMilliseconds);

        return report;
    }

    public async Task<IReadOnlyList<Memory>> ExtractPatternsAsync(
        string userId,
        DateTimeOffset sinceDate,
        CancellationToken ct)
    {
        // Get recent episodic memories
        var episodic = await _retriever.RecallTemporalAsync(
            sinceDate,
            DateTimeOffset.UtcNow,
            null,
            userId,
            ct);

        var episodicMemories = episodic.Memories
            .Where(sm => sm.Memory.Type == MemoryType.Episodic)
            .Select(sm => sm.Memory)
            .ToList();

        if (episodicMemories.Count < 3)
        {
            return Array.Empty<Memory>();
        }

        // Use LLM to extract patterns
        var prompt = BuildPatternExtractionPrompt(episodicMemories);
        var response = await _llm.CompleteAsync(prompt, ct);
        var extractedPatterns = ParsePatterns(response);

        var newMemories = new List<Memory>();
        foreach (var pattern in extractedPatterns)
        {
            var context = new MemoryContext(
                userId,
                null,
                null,
                null,
                "Extracted from episodic patterns");

            var memory = await _fabric.RememberAsAsync(
                pattern,
                MemoryType.Semantic,
                context,
                ct);

            newMemories.Add(memory);

            await _mediator.Publish(new PatternExtractedEvent(memory, episodicMemories.Count), ct);
        }

        return newMemories;
    }

    private string BuildPatternExtractionPrompt(IReadOnlyList<Memory> episodicMemories)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Analyze these episodic memories and extract recurring patterns or facts:");
        builder.AppendLine();

        foreach (var memory in episodicMemories.Take(20))
        {
            builder.AppendLine($"- [{memory.Temporal.CreatedAt:yyyy-MM-dd}] {memory.Content}");
        }

        builder.AppendLine();
        builder.AppendLine("Extract generalizable facts or patterns. Output as JSON array of strings:");
        builder.AppendLine("Example: [\"User prefers to run tests before committing\", \"Project uses PostgreSQL\"]");

        return builder.ToString();
    }

    public async Task<IReadOnlyList<ContradictionPair>> DetectContradictionsAsync(
        string userId,
        CancellationToken ct)
    {
        // Get high-salience semantic memories
        var options = new RecallOptions(userId, MaxResults: 100, Mode: RecallMode.Important);
        var important = await _retriever.RecallAsync("", options with { TypeFilter = MemoryType.Semantic }, ct);

        var contradictions = new List<ContradictionPair>();
        var memories = important.Memories.Select(sm => sm.Memory).ToList();

        // Pairwise comparison for potential contradictions
        for (int i = 0; i < memories.Count; i++)
        {
            for (int j = i + 1; j < memories.Count; j++)
            {
                var similarity = CosineSimilarity(memories[i].Embedding, memories[j].Embedding);

                // High similarity but potentially contradictory
                if (similarity > 0.7f && similarity < 0.95f)
                {
                    var contradiction = await CheckContradictionAsync(memories[i], memories[j], ct);
                    if (contradiction != null)
                    {
                        contradictions.Add(contradiction);
                    }
                }
            }
        }

        if (contradictions.Any())
        {
            await _mediator.Publish(
                new MemoryContradictionDetectedEvent(contradictions),
                ct);
        }

        return contradictions;
    }

    private async Task<ContradictionPair?> CheckContradictionAsync(
        Memory first,
        Memory second,
        CancellationToken ct)
    {
        var prompt = $"""
            Do these two statements contradict each other?

            Statement 1: {first.Content}
            Statement 2: {second.Content}

            Respond with JSON: {{"contradicts": true/false, "confidence": 0.0-1.0, "explanation": "..."}}
            """;

        var response = await _llm.CompleteAsync(prompt, ct);
        var result = JsonSerializer.Deserialize<ContradictionResult>(response);

        if (result.Contradicts && result.Confidence > 0.7f)
        {
            return new ContradictionPair(first, second, result.Confidence, result.Explanation);
        }

        return null;
    }

    private async Task<int> MergeDuplicatesAsync(
        string userId,
        float threshold,
        CancellationToken ct)
    {
        // Leverage v0.5.9 deduplication service
        var result = await _deduplicationService.FindDuplicatesAsync(
            new DeduplicationRequest { UserId = userId, Threshold = threshold },
            ct);

        int merged = 0;
        foreach (var group in result.DuplicateGroups)
        {
            // Keep the one with highest salience
            var primary = group.OrderByDescending(m => m.CurrentSalience).First();

            foreach (var duplicate in group.Where(m => m.Id != primary.Id))
            {
                await _fabric.LinkMemoriesAsync(
                    primary.Id,
                    duplicate.Id,
                    MemoryLinkType.Supersedes,
                    1.0f,
                    ct);

                await _store.SupersedeAsync(duplicate.Id, primary.Id, ct);
                merged++;
            }
        }

        return merged;
    }

    private async Task<int> ArchiveLowSalienceAsync(
        string userId,
        float threshold,
        TimeSpan? maxAge,
        CancellationToken ct)
    {
        var options = new RecallOptions(userId, MaxResults: 1000, MinSalience: 0f);
        var all = await _retriever.RecallAsync("", options, ct);

        int archived = 0;
        var cutoff = maxAge.HasValue ? DateTimeOffset.UtcNow - maxAge.Value : DateTimeOffset.MinValue;

        foreach (var sm in all.Memories)
        {
            var memory = sm.Memory;

            // Archive if below threshold and old enough
            if (memory.CurrentSalience < threshold &&
                memory.Temporal.LastAccessed < cutoff)
            {
                await _fabric.ArchiveMemoryAsync(memory.Id, ct);
                archived++;
            }
        }

        return archived;
    }
}
```

---

## 6. MediatR Events

```csharp
/// <summary>
/// Published when consolidation cycle completes.
/// </summary>
public record ConsolidationCompletedEvent(ConsolidationReport Report) : INotification;

/// <summary>
/// Published when a pattern is extracted from episodic memories.
/// </summary>
public record PatternExtractedEvent(
    Memory ExtractedMemory,
    int SourceEpisodicCount) : INotification;

/// <summary>
/// Published when contradictions are detected.
/// </summary>
public record MemoryContradictionDetectedEvent(
    IReadOnlyList<ContradictionPair> Contradictions) : INotification;
```

---

## 7. Observability & Logging

*   **Metric:** `Agents.Memory.Consolidator.CyclesDuration` (Histogram)
*   **Metric:** `Agents.Memory.Consolidator.PatternsExtracted` (Counter)
*   **Metric:** `Agents.Memory.Consolidator.ContradictionsFound` (Counter)
*   **Metric:** `Agents.Memory.Consolidator.DuplicatesMerged` (Counter)
*   **Metric:** `Agents.Memory.Consolidator.MemoriesArchived` (Counter)
*   **Log (Info):** `[MEM:CONSOLIDATE] Starting consolidation cycle for user {UserId}`
*   **Log (Info):** `[MEM:CONSOLIDATE] Extracted {Count} patterns from episodic memories`
*   **Log (Info):** `[MEM:CONSOLIDATE] Completed cycle for {UserId}: {Processed} processed`

---

## 8. Acceptance Criteria (QA)

1.  **[Patterns]** `ExtractPatternsAsync` SHALL create semantic memories from episodic patterns.
2.  **[Contradictions]** `DetectContradictionsAsync` SHALL identify conflicting memories.
3.  **[Contradictions]** Contradicting memories SHALL be linked with Contradicts type.
4.  **[Duplicates]** Similar memories SHALL be merged with deduplication service.
5.  **[Archival]** Low-salience, old memories SHALL be archived.
6.  **[Decay]** Salience decay SHALL be applied to all memories.
7.  **[Events]** Consolidation SHALL publish completion event.

---

## 9. Test Scenarios

```gherkin
Scenario: Extract patterns from episodic memories
    Given 10 episodic memories about testing before commits
    When ExtractPatternsAsync is called
    Then a semantic memory "User runs tests before committing" SHALL be created

Scenario: Detect contradictions
    Given memory "Project uses PostgreSQL"
    And memory "Project uses MySQL"
    When DetectContradictionsAsync is called
    Then a contradiction SHALL be detected
    And memories SHALL be linked with Contradicts type

Scenario: Merge duplicates
    Given memory A "API uses REST"
    And memory B "The API is RESTful"
    And similarity > 0.95
    When consolidation runs with MergeDuplicates
    Then one memory SHALL supersede the other

Scenario: Archive stale memories
    Given a memory with salience 0.05
    And last accessed 30 days ago
    When consolidation runs with ArchivalThreshold 0.1
    Then memory SHALL be archived
```

