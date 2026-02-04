# LDS-01: Feature Design Specification — Memory Retriever

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-05` | Matches the Roadmap ID. |
| **Feature Name** | Memory Retriever | The internal display name. |
| **Target Version** | `v0.8.9e` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Retriever` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
The memory system needs flexible retrieval supporting multiple access patterns: semantic similarity search, temporal range queries, type filtering, and salience-weighted ranking. Users need to ask "what do I know about X?" and "what happened last week?"

### 2.2 The Proposed Solution
Implement `IMemoryRetriever` with multiple recall modes: Relevant (embedding similarity + salience), Recent (last accessed), Important (highest salience), Temporal (time range), and Frequent (most accessed). Combines vector search with SQL filtering for efficient hybrid retrieval.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a models)
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9b storage)
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9d salience)
    *   `Lexichord.Modules.Rag` (`IEmbeddingService`)
*   **NuGet Packages:**
    *   `Npgsql`
    *   `pgvector`
    *   `Dapper`

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Retrieval operations require Writer Pro license.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Retrieves memories using multiple access patterns.
/// </summary>
public interface IMemoryRetriever
{
    /// <summary>
    /// Recall memories relevant to a query.
    /// </summary>
    /// <param name="query">The natural language query.</param>
    /// <param name="options">Retrieval options including filters and limits.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Scored memories ordered by relevance.</returns>
    Task<MemoryRecallResult> RecallAsync(
        string query,
        RecallOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Recall memories from a specific time period.
    /// </summary>
    /// <param name="from">Start of time range.</param>
    /// <param name="to">End of time range.</param>
    /// <param name="topicFilter">Optional topic to filter by.</param>
    /// <param name="userId">User who owns the memories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memories created within the time range.</returns>
    Task<MemoryRecallResult> RecallTemporalAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? topicFilter = null,
        string? userId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Recall memories of a specific type.
    /// </summary>
    /// <param name="type">The memory type to retrieve.</param>
    /// <param name="userId">User who owns the memories.</param>
    /// <param name="limit">Maximum number to return.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memories of the specified type.</returns>
    Task<MemoryRecallResult> RecallByTypeAsync(
        MemoryType type,
        string userId,
        int limit = 10,
        CancellationToken ct = default);

    /// <summary>
    /// Recall linked memories from a source memory.
    /// </summary>
    /// <param name="memoryId">The source memory.</param>
    /// <param name="linkTypes">Optional filter by link types.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Memories linked to the source.</returns>
    Task<MemoryRecallResult> RecallLinkedAsync(
        string memoryId,
        IReadOnlyList<MemoryLinkType>? linkTypes = null,
        CancellationToken ct = default);

    /// <summary>
    /// Search for contradicting memories.
    /// </summary>
    /// <param name="content">Content to check for contradictions.</param>
    /// <param name="userId">User who owns the memories.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Potentially contradicting memories.</returns>
    Task<MemoryRecallResult> FindContradictionsAsync(
        string content,
        string userId,
        CancellationToken ct = default);
}

/// <summary>
/// Options for memory recall operations.
/// </summary>
/// <param name="UserId">User who owns the memories.</param>
/// <param name="MaxResults">Maximum number of results to return.</param>
/// <param name="TypeFilter">Optional filter by memory type.</param>
/// <param name="MinSalience">Minimum salience threshold.</param>
/// <param name="Mode">Retrieval strategy to use.</param>
/// <param name="ProjectId">Optional project scope filter.</param>
/// <param name="IncludeArchived">Whether to include archived memories.</param>
public record RecallOptions(
    string UserId,
    int MaxResults = 10,
    MemoryType? TypeFilter = null,
    float MinSalience = 0.0f,
    RecallMode Mode = RecallMode.Relevant,
    string? ProjectId = null,
    bool IncludeArchived = false);

/// <summary>
/// Retrieval strategy for memory recall.
/// </summary>
public enum RecallMode
{
    /// <summary>
    /// Most relevant to query (embedding similarity + salience).
    /// </summary>
    Relevant,

    /// <summary>
    /// Most recently accessed.
    /// </summary>
    Recent,

    /// <summary>
    /// Highest salience score.
    /// </summary>
    Important,

    /// <summary>
    /// Within time range (use RecallTemporalAsync).
    /// </summary>
    Temporal,

    /// <summary>
    /// Most frequently accessed.
    /// </summary>
    Frequent
}

/// <summary>
/// Configuration for the memory retriever.
/// </summary>
public record MemoryRetrieverOptions
{
    /// <summary>
    /// Weight for embedding similarity in combined scoring.
    /// </summary>
    public float SimilarityWeight { get; init; } = 0.6f;

    /// <summary>
    /// Weight for salience in combined scoring.
    /// </summary>
    public float SalienceWeight { get; init; } = 0.4f;

    /// <summary>
    /// Minimum similarity threshold for vector search.
    /// </summary>
    public float MinSimilarity { get; init; } = 0.3f;

    /// <summary>
    /// Number of candidates to retrieve before re-ranking.
    /// </summary>
    public int CandidateMultiplier { get; init; } = 3;

    /// <summary>
    /// Whether to update access time on retrieval.
    /// </summary>
    public bool TrackAccess { get; init; } = true;
}
```

---

## 5. Implementation Logic

**Memory Retriever Implementation:**
```csharp
public class MemoryRetriever : IMemoryRetriever
{
    private readonly NpgsqlDataSource _dataSource;
    private readonly IEmbeddingService _embeddingService;
    private readonly ISalienceCalculator _salienceCalculator;
    private readonly IMemoryStore _memoryStore;
    private readonly MemoryRetrieverOptions _options;
    private readonly ILogger<MemoryRetriever> _logger;

    public async Task<MemoryRecallResult> RecallAsync(
        string query,
        RecallOptions options,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        // Generate query embedding
        var queryEmbedding = await _embeddingService.EmbedAsync(query, ct);

        var memories = options.Mode switch
        {
            RecallMode.Relevant => await RecallRelevantAsync(queryEmbedding, options, ct),
            RecallMode.Recent => await RecallRecentAsync(options, ct),
            RecallMode.Important => await RecallImportantAsync(options, ct),
            RecallMode.Frequent => await RecallFrequentAsync(options, ct),
            _ => await RecallRelevantAsync(queryEmbedding, options, ct)
        };

        // Score and rank results
        var salienceContext = SalienceContext.WithQuery(query, queryEmbedding);
        var scoredMemories = memories
            .Select(m => ScoreMemory(m, salienceContext, queryEmbedding))
            .OrderByDescending(sm => sm.Score)
            .Take(options.MaxResults)
            .ToList();

        // Track access if enabled
        if (_options.TrackAccess)
        {
            foreach (var sm in scoredMemories)
            {
                await _memoryStore.RecordAccessAsync(sm.Memory.Id, ct);
            }
        }

        stopwatch.Stop();

        _logger.LogInformation(
            "[MEM:RETRIEVE] Recalled {Count} memories in {Mode} mode for query '{Query}' in {Duration}ms",
            scoredMemories.Count, options.Mode, query[..Math.Min(50, query.Length)], stopwatch.ElapsedMilliseconds);

        return new MemoryRecallResult(
            scoredMemories,
            scoredMemories.Count,
            stopwatch.Elapsed);
    }

    private async Task<IReadOnlyList<Memory>> RecallRelevantAsync(
        float[] queryEmbedding,
        RecallOptions options,
        CancellationToken ct)
    {
        // Hybrid query: vector similarity + salience
        const string sql = @"
            WITH vector_matches AS (
                SELECT
                    m.*,
                    1 - (m.embedding <=> @QueryEmbedding::vector) AS similarity
                FROM memories m
                WHERE m.user_id = @UserId
                  AND m.status = CASE WHEN @IncludeArchived THEN m.status ELSE 'active' END
                  AND (@TypeFilter IS NULL OR m.memory_type = @TypeFilter)
                  AND (@ProjectId IS NULL OR m.project_id = @ProjectId::uuid)
                  AND m.current_salience >= @MinSalience
                ORDER BY m.embedding <=> @QueryEmbedding::vector
                LIMIT @CandidateLimit
            )
            SELECT *,
                   similarity * @SimilarityWeight + current_salience * @SalienceWeight AS combined_score
            FROM vector_matches
            WHERE similarity >= @MinSimilarity
            ORDER BY combined_score DESC
            LIMIT @MaxResults;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<MemoryRow>(sql, new
        {
            QueryEmbedding = new Vector(queryEmbedding),
            options.UserId,
            options.IncludeArchived,
            TypeFilter = options.TypeFilter?.ToString().ToLowerInvariant(),
            ProjectId = options.ProjectId,
            options.MinSalience,
            CandidateLimit = options.MaxResults * _options.CandidateMultiplier,
            options.MaxResults,
            _options.MinSimilarity,
            _options.SimilarityWeight,
            _options.SalienceWeight
        });

        return results.Select(MapToMemory).ToList();
    }

    private async Task<IReadOnlyList<Memory>> RecallRecentAsync(
        RecallOptions options,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT *
            FROM memories
            WHERE user_id = @UserId
              AND status = CASE WHEN @IncludeArchived THEN status ELSE 'active' END
              AND (@TypeFilter IS NULL OR memory_type = @TypeFilter)
              AND (@ProjectId IS NULL OR project_id = @ProjectId::uuid)
              AND current_salience >= @MinSalience
            ORDER BY last_accessed_at DESC
            LIMIT @MaxResults;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<MemoryRow>(sql, new
        {
            options.UserId,
            options.IncludeArchived,
            TypeFilter = options.TypeFilter?.ToString().ToLowerInvariant(),
            ProjectId = options.ProjectId,
            options.MinSalience,
            options.MaxResults
        });

        return results.Select(MapToMemory).ToList();
    }

    private async Task<IReadOnlyList<Memory>> RecallImportantAsync(
        RecallOptions options,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT *
            FROM memories
            WHERE user_id = @UserId
              AND status = 'active'
              AND (@TypeFilter IS NULL OR memory_type = @TypeFilter)
              AND (@ProjectId IS NULL OR project_id = @ProjectId::uuid)
            ORDER BY current_salience DESC
            LIMIT @MaxResults;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<MemoryRow>(sql, new
        {
            options.UserId,
            TypeFilter = options.TypeFilter?.ToString().ToLowerInvariant(),
            ProjectId = options.ProjectId,
            options.MaxResults
        });

        return results.Select(MapToMemory).ToList();
    }

    private async Task<IReadOnlyList<Memory>> RecallFrequentAsync(
        RecallOptions options,
        CancellationToken ct)
    {
        const string sql = @"
            SELECT *
            FROM memories
            WHERE user_id = @UserId
              AND status = 'active'
              AND (@TypeFilter IS NULL OR memory_type = @TypeFilter)
              AND (@ProjectId IS NULL OR project_id = @ProjectId::uuid)
              AND current_salience >= @MinSalience
            ORDER BY access_count DESC
            LIMIT @MaxResults;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<MemoryRow>(sql, new
        {
            options.UserId,
            TypeFilter = options.TypeFilter?.ToString().ToLowerInvariant(),
            ProjectId = options.ProjectId,
            options.MinSalience,
            options.MaxResults
        });

        return results.Select(MapToMemory).ToList();
    }

    public async Task<MemoryRecallResult> RecallTemporalAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        string? topicFilter,
        string? userId,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        const string sql = @"
            SELECT *
            FROM memories
            WHERE (@UserId IS NULL OR user_id = @UserId)
              AND status = 'active'
              AND created_at >= @From
              AND created_at <= @To
            ORDER BY created_at DESC;";

        await using var conn = await _dataSource.OpenConnectionAsync(ct);
        var results = await conn.QueryAsync<MemoryRow>(sql, new
        {
            UserId = userId,
            From = from,
            To = to
        });

        var memories = results.Select(MapToMemory).ToList();

        // Filter by topic if provided (semantic filtering)
        if (!string.IsNullOrEmpty(topicFilter))
        {
            var topicEmbedding = await _embeddingService.EmbedAsync(topicFilter, ct);
            memories = memories
                .Where(m => CosineSimilarity(m.Embedding, topicEmbedding) >= _options.MinSimilarity)
                .ToList();
        }

        var scoredMemories = memories
            .Select(m => new ScoredMemory(m, m.CurrentSalience, "Temporal match"))
            .ToList();

        stopwatch.Stop();

        _logger.LogInformation(
            "[MEM:RETRIEVE] Temporal recall found {Count} memories from {From} to {To}",
            scoredMemories.Count, from, to);

        return new MemoryRecallResult(scoredMemories, scoredMemories.Count, stopwatch.Elapsed);
    }

    public async Task<MemoryRecallResult> RecallLinkedAsync(
        string memoryId,
        IReadOnlyList<MemoryLinkType>? linkTypes,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var links = await _memoryStore.GetLinksFromAsync(memoryId, ct);

        if (linkTypes != null && linkTypes.Any())
        {
            links = links.Where(l => linkTypes.Contains(l.LinkType)).ToList();
        }

        var memories = new List<ScoredMemory>();
        foreach (var link in links)
        {
            var memory = await _memoryStore.GetByIdAsync(link.ToMemoryId, ct);
            if (memory != null)
            {
                memories.Add(new ScoredMemory(
                    memory,
                    link.Strength,
                    $"Linked via {link.LinkType}"));
            }
        }

        stopwatch.Stop();

        return new MemoryRecallResult(memories, memories.Count, stopwatch.Elapsed);
    }

    public async Task<MemoryRecallResult> FindContradictionsAsync(
        string content,
        string userId,
        CancellationToken ct)
    {
        // Find semantically similar memories that might contradict
        var embedding = await _embeddingService.EmbedAsync(content, ct);
        var options = new RecallOptions(userId, MaxResults: 20, MinSalience: 0.3f);
        var similar = await RecallRelevantAsync(embedding, options, ct);

        // Filter to those with existing contradiction links
        var contradictions = new List<ScoredMemory>();
        foreach (var memory in similar)
        {
            var links = await _memoryStore.GetLinksFromAsync(memory.Id, ct);
            if (links.Any(l => l.LinkType == MemoryLinkType.Contradicts))
            {
                contradictions.Add(new ScoredMemory(
                    memory,
                    memory.CurrentSalience,
                    "Potential contradiction"));
            }
        }

        return new MemoryRecallResult(contradictions, contradictions.Count, TimeSpan.Zero);
    }

    private ScoredMemory ScoreMemory(Memory memory, SalienceContext context, float[] queryEmbedding)
    {
        var similarity = CosineSimilarity(memory.Embedding, queryEmbedding);
        var salience = _salienceCalculator.CalculateSalience(memory, context);
        var combinedScore = similarity * _options.SimilarityWeight + salience * _options.SalienceWeight;

        return new ScoredMemory(memory, combinedScore, $"Similarity: {similarity:F2}, Salience: {salience:F2}");
    }

    private static float CosineSimilarity(float[] a, float[] b)
    {
        float dot = 0, normA = 0, normB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            normA += a[i] * a[i];
            normB += b[i] * b[i];
        }
        var denom = Math.Sqrt(normA) * Math.Sqrt(normB);
        return denom < 1e-10 ? 0 : (float)(dot / denom);
    }
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Memory.Retriever.Queries` (Counter by Mode)
*   **Metric:** `Agents.Memory.Retriever.Latency` (Histogram)
*   **Metric:** `Agents.Memory.Retriever.ResultCount` (Histogram)
*   **Log (Info):** `[MEM:RETRIEVE] Recalled {Count} memories in {Mode} mode for query '{Query}' in {Duration}ms`
*   **Log (Info):** `[MEM:RETRIEVE] Temporal recall found {Count} memories from {From} to {To}`

---

## 7. Acceptance Criteria (QA)

1.  **[Relevant]** Relevant mode SHALL combine vector similarity with salience.
2.  **[Recent]** Recent mode SHALL order by LastAccessed descending.
3.  **[Important]** Important mode SHALL order by salience descending.
4.  **[Frequent]** Frequent mode SHALL order by AccessCount descending.
5.  **[Temporal]** Temporal recall SHALL filter by time range.
6.  **[Linked]** Linked recall SHALL traverse memory graph.
7.  **[Access]** Retrieval SHALL update access tracking when enabled.
8.  **[Filters]** Type and project filters SHALL apply correctly.

---

## 8. Test Scenarios

```gherkin
Scenario: Recall relevant memories
    Given 10 memories about various topics
    When RecallAsync is called with query "database performance"
    Then memories about databases SHALL rank highest
    And results SHALL be ordered by combined score

Scenario: Recall by time range
    Given memories from the last month
    When RecallTemporalAsync is called for "last week"
    Then only memories from last week SHALL be returned
    And results SHALL be ordered by creation time

Scenario: Recall by type
    Given 5 semantic and 5 episodic memories
    When RecallByTypeAsync is called with Semantic filter
    Then only semantic memories SHALL be returned

Scenario: Access tracking on retrieval
    Given a memory with AccessCount 5
    When RecallAsync returns this memory
    And TrackAccess is enabled
    Then AccessCount SHALL be 6

Scenario: Linked memory traversal
    Given memory A linked to memories B and C
    When RecallLinkedAsync is called for memory A
    Then memories B and C SHALL be returned
```

