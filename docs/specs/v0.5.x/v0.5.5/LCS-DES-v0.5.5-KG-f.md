# LCS-DES-055-KG-f: Candidate Generator

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-055-KG-f |
| **Feature ID** | KG-055f |
| **Feature Name** | Candidate Generator |
| **Target Version** | v0.5.5f |
| **Module Scope** | `Lexichord.Nlu.EntityLinking` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | WriterPro (basic), Teams (full) |
| **Feature Gate Key** | `knowledge.linking.candidates.enabled` |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

After entity recognition identifies mentions in text, we need to find potential matching entities in the Knowledge Graph. The **Candidate Generator** produces a set of candidate entities for each mention, which the Entity Linker will then score and rank.

### 2.2 The Proposed Solution

Implement a candidate generation system that:

- Performs exact and fuzzy name matching against the graph
- Uses type information to filter candidates
- Builds and maintains an inverted index for fast lookup
- Supports alias expansion and synonym matching
- Provides configurable candidate limits per mention

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.5e: `IEntityRecognizer` — Source mentions
- v0.4.5e: `IGraphRepository` — Entity queries
- v0.4.5f: `ISchemaRegistry` — Type metadata

**NuGet Packages:**
- `SimMetrics.Net` — String similarity algorithms
- `Microsoft.Extensions.Caching.Memory` — Index caching

### 3.2 Module Placement

```
Lexichord.Nlu/
├── EntityLinking/
│   ├── Candidates/
│   │   ├── ICandidateGenerator.cs
│   │   ├── LinkCandidate.cs
│   │   ├── CandidateGeneratorOptions.cs
│   │   ├── DefaultCandidateGenerator.cs
│   │   └── Index/
│   │       ├── IEntityIndex.cs
│   │       ├── InMemoryEntityIndex.cs
│   │       └── EntityIndexBuilder.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Load with linking service
- **Fallback Experience:** WriterPro: exact match only; Teams+: fuzzy + alias matching

---

## 4. Data Contract (The API)

### 4.1 Core Interfaces

```csharp
namespace Lexichord.Nlu.EntityLinking.Candidates;

/// <summary>
/// Generates candidate entities for linking.
/// </summary>
public interface ICandidateGenerator
{
    /// <summary>
    /// Generates candidates for a single mention.
    /// </summary>
    /// <param name="mention">The entity mention.</param>
    /// <param name="options">Generation options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Candidate entities ranked by initial similarity.</returns>
    Task<CandidateSet> GenerateCandidatesAsync(
        EntityMention mention,
        CandidateGeneratorOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates candidates for multiple mentions (batch).
    /// </summary>
    Task<IReadOnlyDictionary<Guid, CandidateSet>> GenerateCandidatesBatchAsync(
        IReadOnlyList<EntityMention> mentions,
        CandidateGeneratorOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Rebuilds the candidate index from the graph.
    /// </summary>
    Task RebuildIndexAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the index with changed entities.
    /// </summary>
    Task UpdateIndexAsync(
        IReadOnlyList<KnowledgeEntity> addedOrUpdated,
        IReadOnlyList<Guid> deleted,
        CancellationToken ct = default);
}
```

### 4.2 LinkCandidate Record

```csharp
namespace Lexichord.Nlu.EntityLinking.Candidates;

/// <summary>
/// A candidate entity for linking.
/// </summary>
public record LinkCandidate
{
    /// <summary>
    /// Graph entity ID.
    /// </summary>
    public required Guid EntityId { get; init; }

    /// <summary>
    /// Entity name/label.
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Entity type.
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// How the candidate was matched.
    /// </summary>
    public CandidateMatchType MatchType { get; init; }

    /// <summary>
    /// Which name/alias matched (if different from primary name).
    /// </summary>
    public string? MatchedName { get; init; }

    /// <summary>
    /// Initial similarity score (0.0-1.0) from candidate generation.
    /// </summary>
    public float SimilarityScore { get; init; }

    /// <summary>
    /// String distance metric used.
    /// </summary>
    public string? DistanceMetric { get; init; }

    /// <summary>
    /// Entity properties for context scoring.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Properties { get; init; }

    /// <summary>
    /// Related entity IDs (for co-occurrence scoring).
    /// </summary>
    public IReadOnlyList<Guid>? RelatedEntityIds { get; init; }

    /// <summary>
    /// Popularity/frequency score from usage statistics.
    /// </summary>
    public float PopularityScore { get; init; }
}

/// <summary>
/// How a candidate was matched.
/// </summary>
public enum CandidateMatchType
{
    /// <summary>Exact name match.</summary>
    ExactMatch,

    /// <summary>Exact alias match.</summary>
    AliasMatch,

    /// <summary>Fuzzy name match.</summary>
    FuzzyMatch,

    /// <summary>Fuzzy alias match.</summary>
    FuzzyAliasMatch,

    /// <summary>Partial/substring match.</summary>
    PartialMatch,

    /// <summary>Phonetic match (sounds like).</summary>
    PhoneticMatch
}

/// <summary>
/// Set of candidates for a mention.
/// </summary>
public record CandidateSet
{
    /// <summary>
    /// The source mention.
    /// </summary>
    public required EntityMention Mention { get; init; }

    /// <summary>
    /// Candidate entities, sorted by similarity.
    /// </summary>
    public required IReadOnlyList<LinkCandidate> Candidates { get; init; }

    /// <summary>
    /// Whether the candidate list was truncated.
    /// </summary>
    public bool WasTruncated { get; init; }

    /// <summary>
    /// Total candidates found before truncation.
    /// </summary>
    public int TotalCandidatesFound { get; init; }

    /// <summary>
    /// Generation duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether any exact match was found.
    /// </summary>
    public bool HasExactMatch => Candidates.Any(c =>
        c.MatchType == CandidateMatchType.ExactMatch);

    /// <summary>
    /// Best candidate (highest similarity).
    /// </summary>
    public LinkCandidate? BestCandidate => Candidates.FirstOrDefault();

    /// <summary>
    /// Creates an empty candidate set.
    /// </summary>
    public static CandidateSet Empty(EntityMention mention) => new()
    {
        Mention = mention,
        Candidates = Array.Empty<LinkCandidate>()
    };
}
```

### 4.3 Options and Configuration

```csharp
namespace Lexichord.Nlu.EntityLinking.Candidates;

/// <summary>
/// Options for candidate generation.
/// </summary>
public record CandidateGeneratorOptions
{
    /// <summary>
    /// Maximum candidates per mention.
    /// </summary>
    public int MaxCandidates { get; init; } = 10;

    /// <summary>
    /// Minimum similarity threshold for fuzzy matching.
    /// </summary>
    public float MinSimilarity { get; init; } = 0.6f;

    /// <summary>
    /// Maximum edit distance for fuzzy matching.
    /// </summary>
    public int MaxEditDistance { get; init; } = 3;

    /// <summary>
    /// Whether to include alias matches.
    /// </summary>
    public bool IncludeAliases { get; init; } = true;

    /// <summary>
    /// Whether to use fuzzy matching.
    /// </summary>
    public bool UseFuzzyMatching { get; init; } = true;

    /// <summary>
    /// Whether to filter by entity type.
    /// </summary>
    public bool FilterByType { get; init; } = true;

    /// <summary>
    /// Similarity algorithm to use.
    /// </summary>
    public SimilarityAlgorithm Algorithm { get; init; } = SimilarityAlgorithm.JaroWinkler;

    /// <summary>
    /// Project ID for scoped entities.
    /// </summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Default options.
    /// </summary>
    public static CandidateGeneratorOptions Default => new();

    /// <summary>
    /// Fast options for real-time use.
    /// </summary>
    public static CandidateGeneratorOptions Fast => new()
    {
        MaxCandidates = 5,
        UseFuzzyMatching = false,
        IncludeAliases = false
    };
}

/// <summary>
/// String similarity algorithms.
/// </summary>
public enum SimilarityAlgorithm
{
    /// <summary>Jaro-Winkler distance (good for names).</summary>
    JaroWinkler,

    /// <summary>Levenshtein edit distance.</summary>
    Levenshtein,

    /// <summary>Cosine similarity on character n-grams.</summary>
    Cosine,

    /// <summary>Jaccard index on character sets.</summary>
    Jaccard,

    /// <summary>Combination of multiple metrics.</summary>
    Combined
}
```

---

## 5. Implementation Logic

### 5.1 DefaultCandidateGenerator

```csharp
namespace Lexichord.Nlu.EntityLinking.Candidates;

/// <summary>
/// Default candidate generator using entity index.
/// </summary>
public class DefaultCandidateGenerator : ICandidateGenerator
{
    private readonly IEntityIndex _index;
    private readonly IGraphRepository _graphRepository;
    private readonly ISchemaRegistry _schemaRegistry;
    private readonly ILogger<DefaultCandidateGenerator> _logger;
    private readonly ISimilarityCalculator _similarity;

    public async Task<CandidateSet> GenerateCandidatesAsync(
        EntityMention mention,
        CandidateGeneratorOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= CandidateGeneratorOptions.Default;
        var sw = Stopwatch.StartNew();

        var candidates = new List<LinkCandidate>();
        var normalizedMention = NormalizeName(mention.Value);

        // 1. Exact match lookup
        var exactMatches = await _index.FindExactAsync(
            normalizedMention,
            options.FilterByType ? mention.EntityType : null,
            ct);

        foreach (var match in exactMatches)
        {
            candidates.Add(CreateCandidate(match, CandidateMatchType.ExactMatch, 1.0f));
        }

        // 2. Alias lookup (if enabled)
        if (options.IncludeAliases && candidates.Count < options.MaxCandidates)
        {
            var aliasMatches = await _index.FindByAliasAsync(
                normalizedMention,
                options.FilterByType ? mention.EntityType : null,
                ct);

            foreach (var match in aliasMatches.Where(m =>
                !candidates.Any(c => c.EntityId == m.EntityId)))
            {
                candidates.Add(CreateCandidate(match, CandidateMatchType.AliasMatch, 0.95f));
            }
        }

        // 3. Fuzzy matching (if enabled and needed)
        if (options.UseFuzzyMatching && candidates.Count < options.MaxCandidates)
        {
            var fuzzyMatches = await _index.FindFuzzyAsync(
                normalizedMention,
                options.FilterByType ? mention.EntityType : null,
                options.MinSimilarity,
                options.MaxEditDistance,
                options.MaxCandidates - candidates.Count,
                ct);

            foreach (var (match, similarity) in fuzzyMatches.Where(m =>
                !candidates.Any(c => c.EntityId == m.Match.EntityId)))
            {
                candidates.Add(CreateCandidate(
                    match.Match,
                    match.Match.Name.Equals(normalizedMention, StringComparison.OrdinalIgnoreCase)
                        ? CandidateMatchType.FuzzyMatch
                        : CandidateMatchType.FuzzyAliasMatch,
                    similarity));
            }
        }

        // 4. Sort by similarity and truncate
        candidates = candidates
            .OrderByDescending(c => c.SimilarityScore)
            .ThenByDescending(c => c.PopularityScore)
            .Take(options.MaxCandidates)
            .ToList();

        sw.Stop();

        return new CandidateSet
        {
            Mention = mention,
            Candidates = candidates,
            WasTruncated = candidates.Count >= options.MaxCandidates,
            TotalCandidatesFound = candidates.Count,
            Duration = sw.Elapsed
        };
    }

    public async Task<IReadOnlyDictionary<Guid, CandidateSet>> GenerateCandidatesBatchAsync(
        IReadOnlyList<EntityMention> mentions,
        CandidateGeneratorOptions? options = null,
        CancellationToken ct = default)
    {
        var results = new Dictionary<Guid, CandidateSet>();

        // Process in parallel with throttling
        var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 2);
        var tasks = mentions.Select(async mention =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var candidates = await GenerateCandidatesAsync(mention, options, ct);
                return (mention.Id, candidates);
            }
            finally
            {
                semaphore.Release();
            }
        });

        var completed = await Task.WhenAll(tasks);
        foreach (var (id, candidates) in completed)
        {
            results[id] = candidates;
        }

        return results;
    }

    private static string NormalizeName(string name)
    {
        return name
            .ToLowerInvariant()
            .Trim()
            .Replace("_", " ")
            .Replace("-", " ");
    }

    private LinkCandidate CreateCandidate(
        IndexedEntity match,
        CandidateMatchType matchType,
        float similarity)
    {
        return new LinkCandidate
        {
            EntityId = match.EntityId,
            EntityName = match.Name,
            EntityType = match.EntityType,
            MatchType = matchType,
            MatchedName = match.MatchedAlias,
            SimilarityScore = similarity,
            Properties = match.Properties,
            PopularityScore = match.PopularityScore
        };
    }
}
```

### 5.2 Entity Index

```csharp
namespace Lexichord.Nlu.EntityLinking.Candidates.Index;

/// <summary>
/// Fast lookup index for entity names and aliases.
/// </summary>
public interface IEntityIndex
{
    /// <summary>Find entities with exact name match.</summary>
    Task<IReadOnlyList<IndexedEntity>> FindExactAsync(
        string normalizedName,
        string? entityType = null,
        CancellationToken ct = default);

    /// <summary>Find entities by alias.</summary>
    Task<IReadOnlyList<IndexedEntity>> FindByAliasAsync(
        string normalizedAlias,
        string? entityType = null,
        CancellationToken ct = default);

    /// <summary>Find entities with fuzzy matching.</summary>
    Task<IReadOnlyList<(IndexedEntity Match, float Similarity)>> FindFuzzyAsync(
        string normalizedName,
        string? entityType,
        float minSimilarity,
        int maxEditDistance,
        int limit,
        CancellationToken ct = default);

    /// <summary>Index statistics.</summary>
    IndexStats Stats { get; }
}

/// <summary>
/// Entity stored in the index.
/// </summary>
public record IndexedEntity
{
    public required Guid EntityId { get; init; }
    public required string Name { get; init; }
    public required string NormalizedName { get; init; }
    public required string EntityType { get; init; }
    public IReadOnlyList<string>? Aliases { get; init; }
    public string? MatchedAlias { get; init; }
    public IReadOnlyDictionary<string, object>? Properties { get; init; }
    public float PopularityScore { get; init; }
}

/// <summary>
/// Index statistics.
/// </summary>
public record IndexStats
{
    public int TotalEntities { get; init; }
    public int TotalAliases { get; init; }
    public IReadOnlyDictionary<string, int>? EntitiesByType { get; init; }
    public DateTimeOffset LastRebuilt { get; init; }
    public TimeSpan RebuildDuration { get; init; }
}

/// <summary>
/// In-memory entity index with trie-based lookup.
/// </summary>
public class InMemoryEntityIndex : IEntityIndex
{
    private readonly Dictionary<string, List<IndexedEntity>> _exactIndex = new();
    private readonly Dictionary<string, List<IndexedEntity>> _aliasIndex = new();
    private readonly Dictionary<string, List<IndexedEntity>> _typeIndex = new();
    private readonly List<IndexedEntity> _allEntities = new();
    private readonly ReaderWriterLockSlim _lock = new();

    public IndexStats Stats { get; private set; } = new();

    public Task<IReadOnlyList<IndexedEntity>> FindExactAsync(
        string normalizedName,
        string? entityType = null,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_exactIndex.TryGetValue(normalizedName, out var matches))
            {
                return Task.FromResult<IReadOnlyList<IndexedEntity>>(Array.Empty<IndexedEntity>());
            }

            if (entityType != null)
            {
                matches = matches.Where(m => m.EntityType == entityType).ToList();
            }

            return Task.FromResult<IReadOnlyList<IndexedEntity>>(matches);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<IndexedEntity>> FindByAliasAsync(
        string normalizedAlias,
        string? entityType = null,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_aliasIndex.TryGetValue(normalizedAlias, out var matches))
            {
                return Task.FromResult<IReadOnlyList<IndexedEntity>>(Array.Empty<IndexedEntity>());
            }

            if (entityType != null)
            {
                matches = matches.Where(m => m.EntityType == entityType).ToList();
            }

            // Mark which alias matched
            var results = matches.Select(m => m with { MatchedAlias = normalizedAlias }).ToList();
            return Task.FromResult<IReadOnlyList<IndexedEntity>>(results);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public Task<IReadOnlyList<(IndexedEntity Match, float Similarity)>> FindFuzzyAsync(
        string normalizedName,
        string? entityType,
        float minSimilarity,
        int maxEditDistance,
        int limit,
        CancellationToken ct = default)
    {
        _lock.EnterReadLock();
        try
        {
            var candidates = entityType != null && _typeIndex.TryGetValue(entityType, out var typed)
                ? typed
                : _allEntities;

            var results = candidates
                .AsParallel()
                .WithCancellation(ct)
                .Select(entity =>
                {
                    var similarity = CalculateJaroWinkler(normalizedName, entity.NormalizedName);
                    return (Entity: entity, Similarity: similarity);
                })
                .Where(x => x.Similarity >= minSimilarity)
                .OrderByDescending(x => x.Similarity)
                .Take(limit)
                .Select(x => (x.Entity, x.Similarity))
                .ToList();

            return Task.FromResult<IReadOnlyList<(IndexedEntity, float)>>(results);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Rebuild(IEnumerable<KnowledgeEntity> entities)
    {
        var sw = Stopwatch.StartNew();

        _lock.EnterWriteLock();
        try
        {
            _exactIndex.Clear();
            _aliasIndex.Clear();
            _typeIndex.Clear();
            _allEntities.Clear();

            int totalAliases = 0;

            foreach (var entity in entities)
            {
                var indexed = new IndexedEntity
                {
                    EntityId = entity.Id,
                    Name = entity.Name,
                    NormalizedName = NormalizeName(entity.Name),
                    EntityType = entity.EntityType,
                    Aliases = GetAliases(entity),
                    Properties = entity.Properties,
                    PopularityScore = GetPopularityScore(entity)
                };

                _allEntities.Add(indexed);

                // Index by normalized name
                AddToIndex(_exactIndex, indexed.NormalizedName, indexed);

                // Index by type
                AddToIndex(_typeIndex, indexed.EntityType, indexed);

                // Index aliases
                if (indexed.Aliases != null)
                {
                    foreach (var alias in indexed.Aliases)
                    {
                        AddToIndex(_aliasIndex, NormalizeName(alias), indexed);
                        totalAliases++;
                    }
                }
            }

            sw.Stop();

            Stats = new IndexStats
            {
                TotalEntities = _allEntities.Count,
                TotalAliases = totalAliases,
                EntitiesByType = _typeIndex.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.Count),
                LastRebuilt = DateTimeOffset.UtcNow,
                RebuildDuration = sw.Elapsed
            };
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private static void AddToIndex(
        Dictionary<string, List<IndexedEntity>> index,
        string key,
        IndexedEntity entity)
    {
        if (!index.TryGetValue(key, out var list))
        {
            list = new List<IndexedEntity>();
            index[key] = list;
        }
        list.Add(entity);
    }

    private static string NormalizeName(string name) =>
        name.ToLowerInvariant().Trim();

    private static float CalculateJaroWinkler(string s1, string s2)
    {
        // Jaro-Winkler implementation
        // ... (using SimMetrics.Net)
        return new JaroWinkler().GetSimilarity(s1, s2);
    }

    private static IReadOnlyList<string>? GetAliases(KnowledgeEntity entity)
    {
        if (entity.Properties?.TryGetValue("aliases", out var aliases) == true)
        {
            return aliases switch
            {
                IEnumerable<string> list => list.ToList(),
                string s => new[] { s },
                _ => null
            };
        }
        return null;
    }

    private static float GetPopularityScore(KnowledgeEntity entity)
    {
        // Could be based on: reference count, view count, etc.
        if (entity.Properties?.TryGetValue("reference_count", out var count) == true)
        {
            return Math.Min(1.0f, (int)count / 100.0f);
        }
        return 0.5f;
    }
}
```

---

## 6. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   Candidate Generation Flow                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│    ┌──────────────────┐                                         │
│    │  EntityMention   │                                         │
│    │  value: "limit"  │                                         │
│    │  type: Parameter │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │   Normalize      │  "limit" → "limit"                      │
│    │     Name         │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│    ┌────────┴────────────────────────────────────┐              │
│    │                                             │              │
│    ▼                                             ▼              │
│  ┌──────────────┐                         ┌──────────────┐      │
│  │ Exact Match  │                         │ Alias Match  │      │
│  │    Index     │                         │    Index     │      │
│  └──────┬───────┘                         └──────┬───────┘      │
│         │                                        │              │
│         │ [entities with name="limit"]           │ [entities    │
│         │                                        │  with alias] │
│         └─────────────────┬──────────────────────┘              │
│                           │                                      │
│                           ▼                                      │
│                  ┌──────────────────┐                           │
│                  │  Fuzzy Matching  │  (if needed)              │
│                  │  Jaro-Winkler    │                           │
│                  └────────┬─────────┘                           │
│                           │                                      │
│                           ▼                                      │
│                  ┌──────────────────┐                           │
│                  │  Type Filter     │  (Parameter only)         │
│                  └────────┬─────────┘                           │
│                           │                                      │
│                           ▼                                      │
│                  ┌──────────────────┐                           │
│                  │ Sort by Score    │                           │
│                  │ Truncate to N    │                           │
│                  └────────┬─────────┘                           │
│                           │                                      │
│                           ▼                                      │
│                  ┌──────────────────┐                           │
│                  │  CandidateSet    │                           │
│                  │  [Candidate 1]   │  score: 1.0 (exact)       │
│                  │  [Candidate 2]   │  score: 0.9 (alias)       │
│                  │  [Candidate 3]   │  score: 0.75 (fuzzy)      │
│                  └──────────────────┘                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5f")]
public class CandidateGeneratorTests
{
    private readonly ICandidateGenerator _generator;
    private readonly IEntityIndex _index;

    [Fact]
    public async Task GenerateCandidatesAsync_ExactMatch_ReturnsHighConfidence()
    {
        // Arrange
        var mention = new EntityMention
        {
            Value = "limit",
            EntityType = "Parameter",
            StartOffset = 0,
            EndOffset = 5
        };

        // Act
        var result = await _generator.GenerateCandidatesAsync(mention);

        // Assert
        result.HasExactMatch.Should().BeTrue();
        result.BestCandidate!.SimilarityScore.Should().Be(1.0f);
        result.BestCandidate.MatchType.Should().Be(CandidateMatchType.ExactMatch);
    }

    [Fact]
    public async Task GenerateCandidatesAsync_AliasMatch_ReturnsCorrectEntity()
    {
        // Arrange - "users endpoint" is alias for "GET /users"
        var mention = new EntityMention
        {
            Value = "users endpoint",
            EntityType = "Endpoint",
            StartOffset = 0,
            EndOffset = 14
        };

        // Act
        var result = await _generator.GenerateCandidatesAsync(mention);

        // Assert
        result.Candidates.Should().Contain(c =>
            c.MatchType == CandidateMatchType.AliasMatch &&
            c.EntityName == "GET /users");
    }

    [Fact]
    public async Task GenerateCandidatesAsync_FuzzyMatch_FindsSimilarNames()
    {
        // Arrange - "limti" is typo of "limit"
        var mention = new EntityMention
        {
            Value = "limti",
            EntityType = "Parameter",
            StartOffset = 0,
            EndOffset = 5
        };
        var options = new CandidateGeneratorOptions
        {
            UseFuzzyMatching = true,
            MinSimilarity = 0.7f
        };

        // Act
        var result = await _generator.GenerateCandidatesAsync(mention, options);

        // Assert
        result.Candidates.Should().Contain(c =>
            c.EntityName == "limit" &&
            c.MatchType == CandidateMatchType.FuzzyMatch);
    }

    [Fact]
    public async Task GenerateCandidatesAsync_TypeFilter_OnlyReturnsMatchingType()
    {
        // Arrange
        var mention = new EntityMention
        {
            Value = "user",
            EntityType = "Schema",
            StartOffset = 0,
            EndOffset = 4
        };
        var options = new CandidateGeneratorOptions { FilterByType = true };

        // Act
        var result = await _generator.GenerateCandidatesAsync(mention, options);

        // Assert
        result.Candidates.Should().OnlyContain(c => c.EntityType == "Schema");
    }

    [Fact]
    public async Task GenerateCandidatesAsync_MaxCandidates_TruncatesResults()
    {
        // Arrange
        var mention = new EntityMention
        {
            Value = "get",
            EntityType = "Endpoint",
            StartOffset = 0,
            EndOffset = 3
        };
        var options = new CandidateGeneratorOptions { MaxCandidates = 3 };

        // Act
        var result = await _generator.GenerateCandidatesAsync(mention, options);

        // Assert
        result.Candidates.Should().HaveCountLessOrEqualTo(3);
        result.WasTruncated.Should().BeTrue();
    }

    [Fact]
    public async Task GenerateCandidatesBatchAsync_ProcessesInParallel()
    {
        // Arrange
        var mentions = Enumerable.Range(1, 20)
            .Select(i => new EntityMention
            {
                Value = $"entity{i}",
                EntityType = "Schema",
                StartOffset = 0,
                EndOffset = 7
            })
            .ToList();

        // Act
        var sw = Stopwatch.StartNew();
        var results = await _generator.GenerateCandidatesBatchAsync(mentions);
        sw.Stop();

        // Assert
        results.Should().HaveCount(20);
        sw.ElapsedMilliseconds.Should().BeLessThan(1000, "batch should process in parallel");
    }

    [Fact]
    public async Task UpdateIndexAsync_AddedEntity_BecomesSearchable()
    {
        // Arrange
        var newEntity = new KnowledgeEntity
        {
            Id = Guid.NewGuid(),
            Name = "NewParameter",
            EntityType = "Parameter"
        };

        // Act
        await _generator.UpdateIndexAsync(new[] { newEntity }, Array.Empty<Guid>());

        var mention = new EntityMention
        {
            Value = "NewParameter",
            EntityType = "Parameter",
            StartOffset = 0,
            EndOffset = 12
        };
        var result = await _generator.GenerateCandidatesAsync(mention);

        // Assert
        result.Candidates.Should().Contain(c => c.EntityId == newEntity.Id);
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Exact name matches return with similarity = 1.0. |
| 2 | Alias matches return with similarity > 0.9. |
| 3 | Fuzzy matches with 1-2 edit distance found. |
| 4 | Type filtering excludes mismatched entity types. |
| 5 | MaxCandidates limit enforced correctly. |
| 6 | Batch processing handles 100 mentions in <2 seconds. |
| 7 | Index rebuild completes for 10K entities in <10 seconds. |
| 8 | Incremental index updates work without full rebuild. |
| 9 | Popularity score influences ranking when similarity ties. |
| 10 | Memory usage stays under 500MB for 100K entities. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `ICandidateGenerator` interface | [ ] |
| 2 | `LinkCandidate` record | [ ] |
| 3 | `CandidateSet` record | [ ] |
| 4 | `CandidateGeneratorOptions` record | [ ] |
| 5 | `DefaultCandidateGenerator` implementation | [ ] |
| 6 | `IEntityIndex` interface | [ ] |
| 7 | `InMemoryEntityIndex` implementation | [ ] |
| 8 | `EntityIndexBuilder` for initial load | [ ] |
| 9 | Jaro-Winkler similarity calculator | [ ] |
| 10 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.5f)

- `ICandidateGenerator` interface for entity candidate generation
- `LinkCandidate` record with match type and similarity
- `CandidateSet` for grouped candidates per mention
- `InMemoryEntityIndex` with trie-based exact lookup
- Fuzzy matching using Jaro-Winkler algorithm
- Alias-based candidate matching
- Batch candidate generation with parallel processing
- Incremental index updates for changed entities
```

---
