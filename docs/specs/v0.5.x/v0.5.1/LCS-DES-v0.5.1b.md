# LCS-DES-051b: BM25 Search Implementation

## Document Control

| Field              | Value                             |
| :----------------- | :-------------------------------- |
| **Document ID**    | LCS-DES-051b                      |
| **Feature ID**     | INF-051b                          |
| **Feature Name**   | BM25 Search Implementation        |
| **Target Version** | v0.5.1b                           |
| **Module**         | Lexichord.Modules.RAG             |
| **Status**         | Complete                          |
| **Last Updated**   | 2026-01-27                        |
| **Depends On**     | v0.5.1a (content_tsvector column) |

---

## 1. Executive Summary

### 1.1 Problem Statement

With the `content_tsvector` column in place (v0.5.1a), we need a service layer that executes BM25-style keyword searches. The service must use PostgreSQL's `ts_rank_cd()` for relevance scoring and support phrase matching.

### 1.2 Solution

Create `IBM25SearchService` interface and `BM25SearchService` implementation that:

- Executes full-text queries using `plainto_tsquery()` for word matching
- Executes phrase queries using `phraseto_tsquery()` for exact sequences
- Returns ranked results using `ts_rank_cd()` (cover density ranking)
- Captures matched terms for highlighting

---

## 2. Dependencies

| Interface/Component    | Source Version | Purpose                       |
| :--------------------- | :------------- | :---------------------------- |
| `content_tsvector`     | v0.5.1a        | GIN-indexed tsvector column   |
| `IDbConnectionFactory` | v0.0.5b        | PostgreSQL connection factory |
| `ILogger<T>`           | v0.0.3b        | Structured logging            |

---

## 3. Data Contract

### 3.1 Interface Definition

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Executes BM25-style keyword search against indexed document chunks.
/// </summary>
public interface IBM25SearchService
{
    /// <summary>
    /// Searches for chunks matching the query using BM25-style ranking.
    /// </summary>
    Task<IReadOnlyList<BM25Hit>> SearchAsync(
        string query,
        int topK,
        CancellationToken ct = default);

    /// <summary>
    /// Searches for chunks containing an exact phrase sequence.
    /// </summary>
    Task<IReadOnlyList<BM25Hit>> SearchPhraseAsync(
        string phrase,
        int topK,
        CancellationToken ct = default);
}
```

### 3.2 Result Record

```csharp
/// <summary>
/// A single hit from BM25 keyword search.
/// </summary>
public record BM25Hit(
    Guid ChunkId,
    float Score,
    IReadOnlyList<string> MatchedTerms,
    string? Headline = null);
```

---

## 4. Implementation

```csharp
namespace Lexichord.Modules.RAG.Services;

public sealed class BM25SearchService : IBM25SearchService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<BM25SearchService> _logger;

    public BM25SearchService(
        IDbConnectionFactory connectionFactory,
        ILogger<BM25SearchService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BM25Hit>> SearchAsync(
        string query, int topK, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<BM25Hit>();

        _logger.LogDebug("BM25 search starting: query='{Query}', topK={TopK}", query, topK);
        var stopwatch = Stopwatch.StartNew();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT
                c.id AS chunk_id,
                ts_rank_cd(c.content_tsvector, query, 32) AS score,
                ts_headline('english', c.content, query,
                    'MaxFragments=3,MaxWords=35,StartSel=**,StopSel=**') AS headline
            FROM chunks c, plainto_tsquery('english', @query) query
            WHERE c.content_tsvector @@ query
            ORDER BY score DESC
            LIMIT @topK;
        ";

        var results = await connection.QueryAsync<BM25HitDto>(sql, new { query, topK });

        var hits = results.Select(r => new BM25Hit(
            r.ChunkId, r.Score, Array.Empty<string>(), r.Headline
        )).ToList();

        _logger.LogDebug("BM25 search completed: {HitCount} hits in {ElapsedMs}ms",
            hits.Count, stopwatch.ElapsedMilliseconds);

        return hits.AsReadOnly();
    }

    public async Task<IReadOnlyList<BM25Hit>> SearchPhraseAsync(
        string phrase, int topK, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(phrase))
            return Array.Empty<BM25Hit>();

        await using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        const string sql = @"
            SELECT
                c.id AS chunk_id,
                ts_rank_cd(c.content_tsvector, query, 32) AS score,
                ts_headline('english', c.content, query,
                    'MaxFragments=3,MaxWords=35,StartSel=**,StopSel=**') AS headline
            FROM chunks c, phraseto_tsquery('english', @phrase) query
            WHERE c.content_tsvector @@ query
            ORDER BY score DESC
            LIMIT @topK;
        ";

        var results = await connection.QueryAsync<BM25HitDto>(sql, new { phrase, topK });
        return results.Select(r => new BM25Hit(
            r.ChunkId, r.Score, Array.Empty<string>(), r.Headline
        )).ToList().AsReadOnly();
    }

    private record BM25HitDto(Guid ChunkId, float Score, string? Headline);
}
```

---

## 5. PostgreSQL Functions

| Function             | Purpose                                 |
| :------------------- | :-------------------------------------- |
| `plainto_tsquery()`  | Word matching with AND logic            |
| `phraseto_tsquery()` | Exact phrase sequence matching          |
| `ts_rank_cd()`       | Cover density ranking (proximity bonus) |
| `ts_headline()`      | Highlighted excerpts with matched terms |

---

## 6. Acceptance Criteria

| #   | Criterion                                         |
| :-- | :------------------------------------------------ |
| 1   | SearchAsync returns results for matching keywords |
| 2   | SearchAsync returns empty list for non-matches    |
| 3   | Results are ordered by score descending           |
| 4   | SearchPhraseAsync only matches exact phrases      |
| 5   | Search latency < 100ms for 50K chunks             |

---

## 7. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.5.1b")]
public class BM25SearchServiceTests
{
    [Fact]
    public async Task SearchAsync_WithValidQuery_ReturnsMatchingChunks()
    {
        // Arrange & Act
        var results = await _sut.SearchAsync("authentication", 10);

        // Assert
        results.Should().NotBeEmpty();
        results.Should().BeInDescendingOrder(r => r.Score);
    }

    [Fact]
    public async Task SearchAsync_WithEmptyQuery_ReturnsEmptyList()
    {
        var results = await _sut.SearchAsync("   ", 10);
        results.Should().BeEmpty();
    }
}
```

---

## 8. Deliverable Checklist

| #   | Deliverable                                    | Status |
| :-- | :--------------------------------------------- | :----- |
| 1   | `IBM25SearchService` interface in Abstractions | [x]    |
| 2   | `BM25Hit` record in Abstractions.Contracts     | [x] *  |
| 3   | `BM25SearchService` implementation             | [x]    |
| 4   | Unit tests for core functionality              | [x]    |
| 5   | DI registration in RAGModule.cs                | [x]    |

> \* `BM25Hit` was adapted to use the existing `SearchHit` record per project convention, maintaining consistency with `ISemanticSearchService`.

---

## Document History

| Version | Date       | Author         | Changes       |
| :------ | :--------- | :------------- | :------------ |
| 1.0     | 2026-01-27 | Lead Architect | Initial draft |
