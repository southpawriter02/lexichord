# LCS-DES-v0.10.4-KG-d: Design Specification — Semantic Search

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-104d` | Graph Visualization sub-part d |
| **Feature Name** | `Semantic Search` | Vector-based entity search |
| **Target Version** | `v0.10.4d` | Fourth sub-part of v0.10.4-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | CKVS knowledge graph module |
| **Swimlane** | `Graph Visualization` | Visualization vertical |
| **License Tier** | `Enterprise` | Available in Enterprise tier only |
| **Feature Gate Key** | `FeatureFlags.CKVS.GraphVisualization` | Graph visualization feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.4-KG](./LCS-SBD-v0.10.4-KG.md) | Graph Visualization & Search scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.4-KG S2.1](./LCS-SBD-v0.10.4-KG.md#21-sub-parts) | d = Semantic Search |

---

## 2. Executive Summary

### 2.1 The Requirement

CKVS v0.10.4-KG requires semantic search powered by vector embeddings. The Semantic Search must:

1. Search entities using natural language queries
2. Find similar entities by embedding distance
3. Return results ranked by relevance score
4. Include matched property and snippet information
5. Execute searches in <500ms
6. Use vector embeddings from IRagService (v0.4.3)

### 2.2 The Proposed Solution

Implement a comprehensive Semantic Search service with:

1. **ISemanticGraphSearch interface:** Main contract for semantic search
2. **SemanticSearchResult record:** Search results with ranked hits
3. **SemanticSearchHit record:** Individual search result with relevance score
4. **Vector similarity computation:** Cosine distance between embeddings
5. **Result ranking and filtering:** Score-based ranking with threshold

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IGraphRepository` | v0.4.5e | Entity and relationship data access |
| `IRagService` | v0.4.3 | Vector embedding generation |
| `IEntityBrowser` | v0.4.7-KG | Entity details and properties |
| `ILicenseContext` | v0.9.2 | License tier validation for Enterprise+ |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `System.Numerics` | Built-in | Vector math for similarity computation |

### 3.2 Licensing Behavior

- **Core Tier:** Not available
- **WriterPro Tier:** Not available
- **Teams Tier:** Not available
- **Enterprise Tier:** Full semantic search with vector embeddings

---

## 4. Data Contract (The API)

### 4.1 ISemanticGraphSearch Interface

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Performs semantic search across the knowledge graph using vector embeddings.
/// Uses natural language understanding to find relevant entities.
/// </summary>
public interface ISemanticGraphSearch
{
    /// <summary>
    /// Searches entities using natural language query.
    /// Returns entities ranked by semantic relevance.
    /// </summary>
    Task<SemanticSearchResult> SearchAsync(
        string query,
        SemanticSearchOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Finds entities semantically similar to a given entity.
    /// Uses entity's embedding as search vector.
    /// </summary>
    Task<IReadOnlyList<SimilarEntity>> FindSimilarAsync(
        Guid entityId,
        int limit = 10,
        CancellationToken ct = default);
}
```

### 4.2 SemanticSearchResult Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Result of semantic search operation.
/// Contains ranked list of relevant entities.
/// </summary>
public record SemanticSearchResult
{
    /// <summary>
    /// Ranked list of search hits.
    /// Sorted by relevance score (highest first).
    /// </summary>
    public IReadOnlyList<SemanticSearchHit> Hits { get; init; } = [];

    /// <summary>
    /// Total count of matching entities (before pagination).
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// Time to execute search.
    /// </summary>
    public TimeSpan SearchTime { get; init; }

    /// <summary>
    /// The query embedding used for search.
    /// Can be used for debugging or alternative searches.
    /// </summary>
    public float[]? QueryEmbedding { get; init; }

    /// <summary>
    /// Any warnings from search execution.
    /// </summary>
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
```

### 4.3 SemanticSearchHit Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Single search result hit from semantic search.
/// Includes entity info and relevance data.
/// </summary>
public record SemanticSearchHit
{
    /// <summary>
    /// The entity ID that matched.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Entity name/label.
    /// </summary>
    public string EntityName { get; init; } = "";

    /// <summary>
    /// Entity type (Service, Endpoint, Database, etc.).
    /// </summary>
    public string EntityType { get; init; } = "";

    /// <summary>
    /// Relevance score (0.0 to 1.0).
    /// 1.0 = perfect match, 0.0 = no relevance.
    /// </summary>
    public float RelevanceScore { get; init; }

    /// <summary>
    /// Which property of the entity matched.
    /// Could be "name", "description", "tags", etc.
    /// </summary>
    public string? MatchedProperty { get; init; }

    /// <summary>
    /// Text snippet from matched property.
    /// Truncated to ~100 characters with ... if needed.
    /// </summary>
    public string? Snippet { get; init; }

    /// <summary>
    /// Distance metric between query and entity embedding.
    /// Lower = more similar.
    /// </summary>
    public float EmbeddingDistance { get; init; }
}
```

### 4.4 SimilarEntity Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Entity similar to a reference entity.
/// </summary>
public record SimilarEntity
{
    /// <summary>
    /// The similar entity ID.
    /// </summary>
    public Guid EntityId { get; init; }

    /// <summary>
    /// Entity name.
    /// </summary>
    public string EntityName { get; init; } = "";

    /// <summary>
    /// Entity type.
    /// </summary>
    public string EntityType { get; init; } = "";

    /// <summary>
    /// Similarity score (0.0 to 1.0).
    /// </summary>
    public float SimilarityScore { get; init; }

    /// <summary>
    /// Number of shared relationships with reference entity.
    /// </summary>
    public int SharedRelationships { get; init; }
}
```

### 4.5 SemanticSearchOptions Record

```csharp
namespace Lexichord.Modules.CKVS.Contracts.Visualization;

/// <summary>
/// Options for semantic search operation.
/// Controls filtering, scoring, and result ranking.
/// </summary>
public record SemanticSearchOptions
{
    /// <summary>
    /// Maximum number of results to return.
    /// 0 = all results.
    /// </summary>
    public int Limit { get; init; } = 20;

    /// <summary>
    /// Offset for pagination.
    /// </summary>
    public int Offset { get; init; } = 0;

    /// <summary>
    /// Minimum relevance score threshold (0.0 to 1.0).
    /// Results below this score are filtered out.
    /// </summary>
    public float MinRelevanceScore { get; init; } = 0.5f;

    /// <summary>
    /// Entity types to filter by.
    /// If empty, searches all types.
    /// </summary>
    public IReadOnlyList<string> EntityTypeFilter { get; init; } = [];

    /// <summary>
    /// Properties to search within.
    /// If empty, searches all properties.
    /// Examples: "name", "description", "tags"
    /// </summary>
    public IReadOnlyList<string> PropertyFilter { get; init; } = [];

    /// <summary>
    /// Whether to include snippet of matched text.
    /// </summary>
    public bool IncludeSnippets { get; init; } = true;

    /// <summary>
    /// Snippet length in characters.
    /// </summary>
    public int SnippetLength { get; init; } = 100;

    /// <summary>
    /// Timeout in milliseconds for search.
    /// </summary>
    public int TimeoutMs { get; init; } = 5000;

    /// <summary>
    /// Whether to boost results by relationship type.
    /// Can improve relevance for highly connected entities.
    /// </summary>
    public bool UseRelationshipBoost { get; init; } = true;
}
```

---

## 5. Implementation Details

### 5.1 Semantic Search Engine

```csharp
namespace Lexichord.Modules.CKVS.Services.Visualization;

internal class SemanticSearchEngine
{
    private readonly IRagService _ragService;
    private readonly IGraphRepository _repository;

    public async Task<SemanticSearchResult> SearchAsync(
        string query,
        SemanticSearchOptions options)
    {
        var sw = Stopwatch.StartNew();

        // Generate embedding for query
        var queryEmbedding = await _ragService.GenerateEmbeddingAsync(query);

        // Get all entities (or filtered by type)
        var entities = await _repository.GetEntitiesAsync();
        if (options.EntityTypeFilter.Count > 0)
        {
            entities = entities
                .Where(e => options.EntityTypeFilter.Contains(e.Type))
                .ToList();
        }

        var hits = new List<SemanticSearchHit>();

        // Compute similarity for each entity
        foreach (var entity in entities)
        {
            var score = await ComputeEntityRelevance(entity, queryEmbedding, options);

            if (score.RelevanceScore < options.MinRelevanceScore)
                continue;

            hits.Add(score);
        }

        sw.Stop();

        // Sort by relevance score
        hits = hits
            .OrderByDescending(h => h.RelevanceScore)
            .Skip(options.Offset)
            .Take(options.Limit == 0 ? hits.Count : options.Limit)
            .ToList();

        return new SemanticSearchResult
        {
            Hits = hits,
            TotalCount = hits.Count,
            SearchTime = sw.Elapsed,
            QueryEmbedding = queryEmbedding,
            Warnings = new List<string>()
        };
    }

    private async Task<SemanticSearchHit> ComputeEntityRelevance(
        Entity entity,
        float[] queryEmbedding,
        SemanticSearchOptions options)
    {
        // Get or generate entity embedding
        var entityEmbedding = await _ragService.GenerateEmbeddingAsync(entity.Description ?? "");

        // Compute cosine similarity
        float distance = ComputeCosineSimilarity(queryEmbedding, entityEmbedding);
        float score = 1.0f - distance; // Convert distance to similarity

        // Optionally boost by degree (number of relationships)
        if (options.UseRelationshipBoost)
        {
            var relationships = await _repository.GetRelationshipsAsync(entity.Id);
            float degreeBoost = Math.Min(relationships.Count * 0.02f, 0.2f);
            score = Math.Min(1.0f, score + degreeBoost);
        }

        var snippet = options.IncludeSnippets
            ? ExtractSnippet(entity.Description ?? "", options.SnippetLength)
            : null;

        return new SemanticSearchHit
        {
            EntityId = entity.Id,
            EntityName = entity.Name,
            EntityType = entity.Type,
            RelevanceScore = score,
            MatchedProperty = "description",
            Snippet = snippet,
            EmbeddingDistance = distance
        };
    }

    private float ComputeCosineSimilarity(float[] vec1, float[] vec2)
    {
        if (vec1.Length != vec2.Length)
            throw new ArgumentException("Vectors must have same dimension");

        float dotProduct = 0;
        float mag1 = 0;
        float mag2 = 0;

        for (int i = 0; i < vec1.Length; i++)
        {
            dotProduct += vec1[i] * vec2[i];
            mag1 += vec1[i] * vec1[i];
            mag2 += vec2[i] * vec2[i];
        }

        mag1 = (float)Math.Sqrt(mag1);
        mag2 = (float)Math.Sqrt(mag2);

        if (mag1 == 0 || mag2 == 0)
            return 1.0f; // Orthogonal

        return 1.0f - (dotProduct / (mag1 * mag2)); // Return distance (0 = identical)
    }

    private string ExtractSnippet(string text, int length)
    {
        if (text.Length <= length)
            return text;

        return text.Substring(0, length - 3) + "...";
    }
}
```

### 5.2 Similar Entity Finder

```csharp
internal class SimilarEntityFinder
{
    public async Task<IReadOnlyList<SimilarEntity>> FindSimilarAsync(
        Guid entityId,
        int limit,
        IGraphRepository repository,
        IRagService ragService)
    {
        // Get reference entity
        var entity = await repository.GetEntityAsync(entityId);
        if (entity == null)
            return [];

        // Get reference embedding
        var referenceEmbedding = await ragService.GenerateEmbeddingAsync(entity.Description ?? "");

        // Get candidate entities
        var candidates = await repository.GetEntitiesAsync();

        var similar = new List<SimilarEntity>();

        // Compute similarity for each candidate
        foreach (var candidate in candidates.Where(c => c.Id != entityId))
        {
            var candidateEmbedding = await ragService.GenerateEmbeddingAsync(candidate.Description ?? "");
            float distance = ComputeCosineSimilarity(referenceEmbedding, candidateEmbedding);
            float score = 1.0f - distance;

            // Count shared relationships
            var refRelationships = await repository.GetRelationshipsAsync(entityId);
            var candRelationships = await repository.GetRelationshipsAsync(candidate.Id);

            int shared = refRelationships.Count(r =>
                candRelationships.Any(c =>
                    (r.SourceId == c.SourceId && r.TargetId == c.TargetId) ||
                    (r.SourceId == c.TargetId && r.TargetId == c.SourceId)));

            similar.Add(new SimilarEntity
            {
                EntityId = candidate.Id,
                EntityName = candidate.Name,
                EntityType = candidate.Type,
                SimilarityScore = score,
                SharedRelationships = shared
            });
        }

        return similar
            .OrderByDescending(s => s.SimilarityScore)
            .Take(limit)
            .ToList();
    }
}
```

---

## 6. Testing Strategy

### 6.1 Unit Tests

```csharp
[TestClass]
public class SemanticSearchTests
{
    private ISemanticGraphSearch _search;
    private Mock<IRagService> _ragServiceMock;
    private Mock<IGraphRepository> _repositoryMock;

    [TestInitialize]
    public void Setup()
    {
        _ragServiceMock = new Mock<IRagService>();
        _repositoryMock = new Mock<IGraphRepository>();
        _search = new SemanticGraphSearch(_ragServiceMock.Object, _repositoryMock.Object);
    }

    [TestMethod]
    public async Task SearchAsync_ReturnsRankedResults()
    {
        var entities = new[]
        {
            new Entity { Id = Guid.NewGuid(), Name = "AuthService", Description = "Handles user authentication" },
            new Entity { Id = Guid.NewGuid(), Name = "PaymentService", Description = "Processes payments" }
        };

        _repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var authEmbedding = new float[] { 0.12f, 0.21f, 0.31f }; // Similar
        var paymentEmbedding = new float[] { 0.5f, 0.6f, 0.7f }; // Different

        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync("authenticate users", It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);
        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync("Handles user authentication", It.IsAny<CancellationToken>()))
            .ReturnsAsync(authEmbedding);
        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync("Processes payments", It.IsAny<CancellationToken>()))
            .ReturnsAsync(paymentEmbedding);

        var options = new SemanticSearchOptions { MinRelevanceScore = 0.5f };
        var result = await _search.SearchAsync("authenticate users", options);

        Assert.IsTrue(result.Hits.Count > 0);
        Assert.IsTrue(result.Hits[0].RelevanceScore > result.Hits[1].RelevanceScore);
    }

    [TestMethod]
    public async Task SearchAsync_FiltersLowRelevanceResults()
    {
        var entities = new[]
        {
            new Entity { Id = Guid.NewGuid(), Name = "Service1", Description = "Description1" }
        };

        _repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 1.0f, 0.0f }); // Orthogonal vectors

        var options = new SemanticSearchOptions { MinRelevanceScore = 0.8f };
        var result = await _search.SearchAsync("query", options);

        Assert.AreEqual(0, result.Hits.Count);
    }

    [TestMethod]
    public async Task FindSimilarAsync_ReturnsSimilarEntities()
    {
        var referenceEntity = new Entity { Id = Guid.NewGuid(), Name = "AuthService", Description = "Auth" };
        var similarEntity = new Entity { Id = Guid.NewGuid(), Name = "UserService", Description = "User management" };

        _repositoryMock.Setup(r => r.GetEntityAsync(referenceEntity.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(referenceEntity);
        _repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { similarEntity });

        var refEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var simEmbedding = new float[] { 0.15f, 0.25f, 0.35f };

        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync("Auth", It.IsAny<CancellationToken>()))
            .ReturnsAsync(refEmbedding);
        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync("User management", It.IsAny<CancellationToken>()))
            .ReturnsAsync(simEmbedding);

        var result = await _search.FindSimilarAsync(referenceEntity.Id);

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(similarEntity.Id, result[0].EntityId);
    }

    [TestMethod]
    public async Task SearchAsync_IncludesSnippets()
    {
        var entity = new Entity
        {
            Id = Guid.NewGuid(),
            Name = "Service",
            Description = "This is a very long description that should be truncated when creating snippets"
        };

        _repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { entity });

        _ragServiceMock.Setup(r => r.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f });

        var options = new SemanticSearchOptions { IncludeSnippets = true, SnippetLength = 50 };
        var result = await _search.SearchAsync("query", options);

        Assert.IsNotNull(result.Hits[0].Snippet);
        Assert.IsTrue(result.Hits[0].Snippet.Length <= 50 + 3); // +3 for "..."
    }
}
```

### 6.2 Performance Tests

```csharp
[TestClass]
public class SemanticSearchPerformanceTests
{
    [TestMethod]
    public async Task SearchAsync_LargeDataset_UnderTimeTarget()
    {
        // Create 1000 entities
        var entities = Enumerable.Range(0, 1000)
            .Select(i => new Entity
            {
                Id = Guid.NewGuid(),
                Name = $"Service{i}",
                Description = $"Service description {i}"
            })
            .ToList();

        var repositoryMock = new Mock<IGraphRepository>();
        var ragServiceMock = new Mock<IRagService>();

        repositoryMock.Setup(r => r.GetEntitiesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(entities);

        ragServiceMock.Setup(r => r.GenerateEmbeddingAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => GenerateRandomEmbedding());

        var search = new SemanticGraphSearch(ragServiceMock.Object, repositoryMock.Object);

        var sw = Stopwatch.StartNew();
        var result = await search.SearchAsync("query", new SemanticSearchOptions { Limit = 10 });
        sw.Stop();

        Assert.IsTrue(sw.ElapsedMilliseconds < 500, $"Search took {sw.ElapsedMilliseconds}ms");
    }
}
```

---

## 7. Error Handling

### 7.1 Missing Entity

**Scenario:** FindSimilarAsync called with non-existent entity ID.

**Handling:**
- Repository returns null
- Method returns empty list gracefully

### 7.2 Embedding Generation Failure

**Scenario:** RAG service fails to generate embedding.

**Handling:**
- Exception caught in Search method
- Warning added to SemanticSearchResult
- Search continues with fallback method (keyword matching)

### 7.3 Dimension Mismatch

**Scenario:** Query embedding has different dimension than entity embeddings.

**Handling:**
- ComputeCosineSimilarity throws ArgumentException
- Caught and logged as error
- Search returns empty results with warning

---

## 8. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Generate query embedding | <100ms | Delegated to IRagService |
| Similarity computation (1K entities) | <400ms | Vectorized cosine similarity |
| Result ranking | <50ms | Single sort pass |
| Total semantic search | <500ms | Parallel embedding generation |

---

## 9. Security & Validation

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Large embedding vectors | Low | Dimension validation (typically 384-1536) |
| Memory exhaustion (1M+ entities) | Medium | Pagination (Limit/Offset), timeout |
| NaN/Inf in embeddings | Low | Vector validation, fallback to 0.0 score |
| Information leakage | Low | Results filtered by entity permissions (future) |

---

## 10. License Gating

```csharp
public class SemanticSearchLicenseCheck : ISemanticGraphSearch
{
    private readonly ILicenseContext _licenseContext;
    private readonly ISemanticGraphSearch _inner;

    public async Task<SemanticSearchResult> SearchAsync(string query, SemanticSearchOptions options, CancellationToken ct)
    {
        if (!_licenseContext.IsTier(LicenseTier.Enterprise))
            throw new LicenseException("Semantic search requires Enterprise tier");

        return await _inner.SearchAsync(query, options, ct);
    }

    public async Task<IReadOnlyList<SimilarEntity>> FindSimilarAsync(Guid entityId, int limit, CancellationToken ct)
    {
        if (!_licenseContext.IsTier(LicenseTier.Enterprise))
            throw new LicenseException("Semantic search requires Enterprise tier");

        return await _inner.FindSimilarAsync(entityId, limit, ct);
    }
}
```

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Query "auth" | SearchAsync called | Returns entities related to authentication |
| 2 | MinRelevanceScore=0.8 | SearchAsync called | Filters results below threshold |
| 3 | EntityTypeFilter=["Service"] | SearchAsync called | Returns only Service entities |
| 4 | IncludeSnippets=true | SearchAsync called | Snippets included in results |
| 5 | Similar entity request | FindSimilarAsync called | Returns ranked similar entities |
| 6 | Large entity set (1000) | SearchAsync called | Completes in <500ms |
| 7 | Non-existent entity | FindSimilarAsync called | Returns empty list |
| 8 | License check | Enterprise required | Throws LicenseException if not Enterprise |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - Semantic search, cosine similarity, similar entity finder |
