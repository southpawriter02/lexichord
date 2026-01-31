# LCS-DES-066-KG-e: Graph Context Provider

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-066-KG-e |
| **System Breakdown** | LCS-SBD-066-KG |
| **Version** | v0.6.6 |
| **Codename** | Graph Context Provider (CKVS Phase 3b) |
| **Estimated Hours** | 6 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Graph Context Provider** retrieves relevant entities, relationships, and axioms from the Knowledge Graph to inject into Co-pilot prompts. It ensures the LLM has accurate domain knowledge for generation.

### 1.2 Key Responsibilities

- Query Knowledge Graph for entities matching user query
- Retrieve relevant relationships between entities
- Fetch applicable axioms for domain rules
- Format context for prompt injection
- Manage token budget for context size
- Rank entities by relevance

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Copilot/
      Context/
        IKnowledgeContextProvider.cs
        KnowledgeContextProvider.cs
        EntityRelevanceRanker.cs
        ContextFormatter.cs
```

---

## 2. Interface Definitions

### 2.1 Knowledge Context Provider

```csharp
namespace Lexichord.KnowledgeGraph.Copilot.Context;

/// <summary>
/// Provides knowledge graph context for Co-pilot prompts.
/// </summary>
public interface IKnowledgeContextProvider
{
    /// <summary>
    /// Gets relevant knowledge context for a query.
    /// </summary>
    Task<KnowledgeContext> GetContextAsync(
        string query,
        KnowledgeContextOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets context for specific entities.
    /// </summary>
    Task<KnowledgeContext> GetContextForEntitiesAsync(
        IReadOnlyList<Guid> entityIds,
        KnowledgeContextOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Gets context from document content.
    /// </summary>
    Task<KnowledgeContext> GetContextFromDocumentAsync(
        Document document,
        KnowledgeContextOptions options,
        CancellationToken ct = default);
}
```

### 2.2 Entity Relevance Ranker

```csharp
/// <summary>
/// Ranks entities by relevance to a query.
/// </summary>
public interface IEntityRelevanceRanker
{
    /// <summary>
    /// Ranks entities by relevance to query.
    /// </summary>
    IReadOnlyList<RankedEntity> RankEntities(
        string query,
        IReadOnlyList<KnowledgeEntity> entities);

    /// <summary>
    /// Filters to top N most relevant entities within token budget.
    /// </summary>
    IReadOnlyList<KnowledgeEntity> SelectWithinBudget(
        IReadOnlyList<RankedEntity> rankedEntities,
        int tokenBudget);
}

/// <summary>
/// Entity with relevance score.
/// </summary>
public record RankedEntity
{
    public required KnowledgeEntity Entity { get; init; }
    public float RelevanceScore { get; init; }
    public int EstimatedTokens { get; init; }
    public IReadOnlyList<string> MatchedTerms { get; init; } = [];
}
```

### 2.3 Context Formatter

```csharp
/// <summary>
/// Formats knowledge context for prompt injection.
/// </summary>
public interface IContextFormatter
{
    /// <summary>
    /// Formats entities and relationships as text.
    /// </summary>
    string FormatContext(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options);

    /// <summary>
    /// Estimates token count for formatted context.
    /// </summary>
    int EstimateTokens(string formattedContext);
}
```

---

## 3. Data Types

### 3.1 Knowledge Context

```csharp
/// <summary>
/// Knowledge context for LLM prompts.
/// </summary>
public record KnowledgeContext
{
    /// <summary>Relevant entities from graph.</summary>
    public required IReadOnlyList<KnowledgeEntity> Entities { get; init; }

    /// <summary>Relevant relationships.</summary>
    public IReadOnlyList<KnowledgeRelationship>? Relationships { get; init; }

    /// <summary>Applicable axioms.</summary>
    public IReadOnlyList<Axiom>? Axioms { get; init; }

    /// <summary>Related claims from documents.</summary>
    public IReadOnlyList<Claim>? Claims { get; init; }

    /// <summary>Formatted context for prompt injection.</summary>
    public string FormattedContext { get; init; } = "";

    /// <summary>Token count of formatted context.</summary>
    public int TokenCount { get; init; }

    /// <summary>Query used to retrieve context.</summary>
    public string? OriginalQuery { get; init; }

    /// <summary>Whether context was truncated due to budget.</summary>
    public bool WasTruncated { get; init; }

    /// <summary>Empty context.</summary>
    public static KnowledgeContext Empty => new()
    {
        Entities = [],
        FormattedContext = "",
        TokenCount = 0
    };
}
```

### 3.2 Knowledge Context Options

```csharp
/// <summary>
/// Options for context retrieval.
/// </summary>
public record KnowledgeContextOptions
{
    /// <summary>Maximum tokens for context.</summary>
    public int MaxTokens { get; init; } = 2000;

    /// <summary>Maximum entities to include.</summary>
    public int MaxEntities { get; init; } = 20;

    /// <summary>Whether to include relationships.</summary>
    public bool IncludeRelationships { get; init; } = true;

    /// <summary>Maximum relationship depth.</summary>
    public int RelationshipDepth { get; init; } = 1;

    /// <summary>Whether to include axioms.</summary>
    public bool IncludeAxioms { get; init; } = true;

    /// <summary>Whether to include claims.</summary>
    public bool IncludeClaims { get; init; } = false;

    /// <summary>Entity types to include (null = all).</summary>
    public IReadOnlySet<string>? EntityTypes { get; init; }

    /// <summary>Minimum relevance score threshold.</summary>
    public float MinRelevanceScore { get; init; } = 0.1f;

    /// <summary>Format for context output.</summary>
    public ContextFormat Format { get; init; } = ContextFormat.Markdown;
}

public enum ContextFormat
{
    Markdown,
    Yaml,
    Json,
    Plain
}
```

### 3.3 Context Format Options

```csharp
/// <summary>
/// Options for context formatting.
/// </summary>
public record ContextFormatOptions
{
    public ContextFormat Format { get; init; } = ContextFormat.Markdown;
    public bool IncludePropertyDescriptions { get; init; } = false;
    public bool IncludeEntityIds { get; init; } = false;
    public int MaxPropertiesPerEntity { get; init; } = 10;
    public string EntitySeparator { get; init; } = "\n";
}
```

---

## 4. Implementation

### 4.1 Knowledge Context Provider

```csharp
public class KnowledgeContextProvider : IKnowledgeContextProvider
{
    private readonly IGraphRepository _graphRepository;
    private readonly IAxiomStore _axiomStore;
    private readonly IClaimRepository _claimRepository;
    private readonly IEntityRelevanceRanker _ranker;
    private readonly IContextFormatter _formatter;
    private readonly ILogger<KnowledgeContextProvider> _logger;

    public async Task<KnowledgeContext> GetContextAsync(
        string query,
        KnowledgeContextOptions options,
        CancellationToken ct = default)
    {
        // 1. Search for relevant entities
        var searchResults = await _graphRepository.SearchEntitiesAsync(
            new EntitySearchQuery
            {
                Query = query,
                MaxResults = options.MaxEntities * 2, // Over-fetch for ranking
                EntityTypes = options.EntityTypes
            }, ct);

        if (searchResults.Count == 0)
        {
            _logger.LogDebug("No entities found for query: {Query}", query);
            return KnowledgeContext.Empty;
        }

        // 2. Rank by relevance
        var rankedEntities = _ranker.RankEntities(query, searchResults);

        // 3. Select within token budget
        var selectedEntities = _ranker.SelectWithinBudget(
            rankedEntities, options.MaxTokens);

        // 4. Get relationships if requested
        IReadOnlyList<KnowledgeRelationship>? relationships = null;
        if (options.IncludeRelationships)
        {
            relationships = await GetRelationshipsAsync(
                selectedEntities, options.RelationshipDepth, ct);
        }

        // 5. Get applicable axioms if requested
        IReadOnlyList<Axiom>? axioms = null;
        if (options.IncludeAxioms)
        {
            axioms = await GetApplicableAxiomsAsync(selectedEntities, ct);
        }

        // 6. Get claims if requested
        IReadOnlyList<Claim>? claims = null;
        if (options.IncludeClaims)
        {
            claims = await GetRelatedClaimsAsync(selectedEntities, ct);
        }

        // 7. Format context
        var formatted = _formatter.FormatContext(
            selectedEntities, relationships, axioms,
            new ContextFormatOptions { Format = options.Format });

        var tokenCount = _formatter.EstimateTokens(formatted);

        return new KnowledgeContext
        {
            Entities = selectedEntities,
            Relationships = relationships,
            Axioms = axioms,
            Claims = claims,
            FormattedContext = formatted,
            TokenCount = tokenCount,
            OriginalQuery = query,
            WasTruncated = rankedEntities.Count > selectedEntities.Count
        };
    }

    public async Task<KnowledgeContext> GetContextForEntitiesAsync(
        IReadOnlyList<Guid> entityIds,
        KnowledgeContextOptions options,
        CancellationToken ct = default)
    {
        var entities = new List<KnowledgeEntity>();
        foreach (var id in entityIds.Take(options.MaxEntities))
        {
            var entity = await _graphRepository.GetEntityByIdAsync(id, ct);
            if (entity != null) entities.Add(entity);
        }

        IReadOnlyList<KnowledgeRelationship>? relationships = null;
        if (options.IncludeRelationships)
        {
            relationships = await GetRelationshipsAsync(
                entities, options.RelationshipDepth, ct);
        }

        IReadOnlyList<Axiom>? axioms = null;
        if (options.IncludeAxioms)
        {
            axioms = await GetApplicableAxiomsAsync(entities, ct);
        }

        var formatted = _formatter.FormatContext(
            entities, relationships, axioms,
            new ContextFormatOptions { Format = options.Format });

        return new KnowledgeContext
        {
            Entities = entities,
            Relationships = relationships,
            Axioms = axioms,
            FormattedContext = formatted,
            TokenCount = _formatter.EstimateTokens(formatted)
        };
    }

    public async Task<KnowledgeContext> GetContextFromDocumentAsync(
        Document document,
        KnowledgeContextOptions options,
        CancellationToken ct = default)
    {
        // Extract entity mentions from document
        if (document.LinkedEntities != null && document.LinkedEntities.Count > 0)
        {
            var entityIds = document.LinkedEntities
                .Where(le => le.EntityId.HasValue)
                .Select(le => le.EntityId!.Value)
                .Distinct()
                .ToList();

            return await GetContextForEntitiesAsync(entityIds, options, ct);
        }

        // Fall back to query-based retrieval using document content
        return await GetContextAsync(document.Content, options, ct);
    }

    private async Task<IReadOnlyList<KnowledgeRelationship>> GetRelationshipsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        int depth,
        CancellationToken ct)
    {
        var relationships = new List<KnowledgeRelationship>();
        var entityIds = entities.Select(e => e.Id).ToHashSet();

        foreach (var entity in entities)
        {
            var rels = await _graphRepository.GetRelationshipsAsync(entity.Id, ct);
            relationships.AddRange(rels.Where(r =>
                entityIds.Contains(r.FromEntityId) ||
                entityIds.Contains(r.ToEntityId)));
        }

        return relationships.Distinct().ToList();
    }

    private async Task<IReadOnlyList<Axiom>> GetApplicableAxiomsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct)
    {
        var entityTypes = entities.Select(e => e.Type).Distinct();
        var axioms = new List<Axiom>();

        foreach (var type in entityTypes)
        {
            var typeAxioms = await _axiomStore.GetAxiomsForTypeAsync(type, ct);
            axioms.AddRange(typeAxioms);
        }

        return axioms.Distinct().ToList();
    }

    private async Task<IReadOnlyList<Claim>> GetRelatedClaimsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        CancellationToken ct)
    {
        var claims = new List<Claim>();

        foreach (var entity in entities.Take(5)) // Limit for performance
        {
            var entityClaims = await _claimRepository.GetClaimsForEntityAsync(
                entity.Id, ct);
            claims.AddRange(entityClaims.Take(3)); // Top 3 per entity
        }

        return claims;
    }
}
```

### 4.2 Entity Relevance Ranker

```csharp
public class EntityRelevanceRanker : IEntityRelevanceRanker
{
    private readonly ITokenizer _tokenizer;

    public IReadOnlyList<RankedEntity> RankEntities(
        string query,
        IReadOnlyList<KnowledgeEntity> entities)
    {
        var queryTerms = ExtractTerms(query);
        var ranked = new List<RankedEntity>();

        foreach (var entity in entities)
        {
            var score = CalculateRelevance(entity, queryTerms, out var matchedTerms);
            var tokens = EstimateEntityTokens(entity);

            ranked.Add(new RankedEntity
            {
                Entity = entity,
                RelevanceScore = score,
                EstimatedTokens = tokens,
                MatchedTerms = matchedTerms
            });
        }

        return ranked.OrderByDescending(r => r.RelevanceScore).ToList();
    }

    public IReadOnlyList<KnowledgeEntity> SelectWithinBudget(
        IReadOnlyList<RankedEntity> rankedEntities,
        int tokenBudget)
    {
        var selected = new List<KnowledgeEntity>();
        var usedTokens = 0;

        foreach (var ranked in rankedEntities)
        {
            if (usedTokens + ranked.EstimatedTokens > tokenBudget)
                break;

            selected.Add(ranked.Entity);
            usedTokens += ranked.EstimatedTokens;
        }

        return selected;
    }

    private float CalculateRelevance(
        KnowledgeEntity entity,
        IReadOnlyList<string> queryTerms,
        out List<string> matchedTerms)
    {
        matchedTerms = new List<string>();
        var score = 0f;

        // Name match (highest weight)
        foreach (var term in queryTerms)
        {
            if (entity.Name.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 1.0f;
                matchedTerms.Add(term);
            }
        }

        // Type match
        foreach (var term in queryTerms)
        {
            if (entity.Type.Contains(term, StringComparison.OrdinalIgnoreCase))
            {
                score += 0.5f;
            }
        }

        // Property value match
        foreach (var prop in entity.Properties)
        {
            foreach (var term in queryTerms)
            {
                if (prop.Value?.ToString()?.Contains(term, StringComparison.OrdinalIgnoreCase) == true)
                {
                    score += 0.3f;
                    if (!matchedTerms.Contains(term)) matchedTerms.Add(term);
                }
            }
        }

        // Normalize by number of query terms
        return queryTerms.Count > 0 ? score / queryTerms.Count : 0;
    }

    private IReadOnlyList<string> ExtractTerms(string query)
    {
        return query.ToLowerInvariant()
            .Split([' ', ',', '.', '/', '-', '_'], StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2)
            .Distinct()
            .ToList();
    }

    private int EstimateEntityTokens(KnowledgeEntity entity)
    {
        // Rough estimation: ~4 chars per token
        var totalChars = entity.Name.Length + entity.Type.Length;
        foreach (var prop in entity.Properties)
        {
            totalChars += prop.Key.Length + (prop.Value?.ToString()?.Length ?? 0);
        }
        return totalChars / 4 + 10; // Add overhead for formatting
    }
}
```

### 4.3 Context Formatter

```csharp
public class ContextFormatter : IContextFormatter
{
    public string FormatContext(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        return options.Format switch
        {
            ContextFormat.Markdown => FormatAsMarkdown(entities, relationships, axioms, options),
            ContextFormat.Yaml => FormatAsYaml(entities, relationships, axioms, options),
            ContextFormat.Json => FormatAsJson(entities, relationships, axioms, options),
            _ => FormatAsPlain(entities, relationships, axioms, options)
        };
    }

    public int EstimateTokens(string formattedContext)
    {
        // GPT-style tokenization: ~4 chars per token
        return formattedContext.Length / 4;
    }

    private string FormatAsMarkdown(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine("## Knowledge Entities");
        foreach (var entity in entities)
        {
            sb.AppendLine($"### {entity.Type}: {entity.Name}");
            var props = entity.Properties.Take(options.MaxPropertiesPerEntity);
            foreach (var prop in props)
            {
                sb.AppendLine($"- **{prop.Key}**: {prop.Value}");
            }
            sb.AppendLine();
        }

        if (relationships?.Count > 0)
        {
            sb.AppendLine("## Relationships");
            foreach (var rel in relationships)
            {
                sb.AppendLine($"- {rel.FromEntityName} --[{rel.Type}]--> {rel.ToEntityName}");
            }
            sb.AppendLine();
        }

        if (axioms?.Count > 0)
        {
            sb.AppendLine("## Domain Rules");
            foreach (var axiom in axioms)
            {
                sb.AppendLine($"- **{axiom.Name}**: {axiom.Description}");
            }
        }

        return sb.ToString();
    }

    private string FormatAsYaml(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var sb = new StringBuilder();
        sb.AppendLine("entities:");
        foreach (var entity in entities)
        {
            sb.AppendLine($"  - type: {entity.Type}");
            sb.AppendLine($"    name: {entity.Name}");
            sb.AppendLine("    properties:");
            foreach (var prop in entity.Properties.Take(options.MaxPropertiesPerEntity))
            {
                sb.AppendLine($"      {prop.Key}: {prop.Value}");
            }
        }
        return sb.ToString();
    }

    private string FormatAsJson(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var context = new
        {
            entities = entities.Select(e => new
            {
                type = e.Type,
                name = e.Name,
                properties = e.Properties.Take(options.MaxPropertiesPerEntity)
            }),
            relationships = relationships?.Select(r => new
            {
                from = r.FromEntityName,
                type = r.Type,
                to = r.ToEntityName
            }),
            axioms = axioms?.Select(a => new { name = a.Name, rule = a.Description })
        };
        return JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true });
    }

    private string FormatAsPlain(
        IReadOnlyList<KnowledgeEntity> entities,
        IReadOnlyList<KnowledgeRelationship>? relationships,
        IReadOnlyList<Axiom>? axioms,
        ContextFormatOptions options)
    {
        var sb = new StringBuilder();
        foreach (var entity in entities)
        {
            sb.AppendLine($"{entity.Type}: {entity.Name}");
            foreach (var prop in entity.Properties.Take(options.MaxPropertiesPerEntity))
            {
                sb.AppendLine($"  {prop.Key}: {prop.Value}");
            }
        }
        return sb.ToString();
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Graph unavailable | Return empty context |
| Query returns no results | Return empty context, log info |
| Token budget exceeded | Truncate entities, set flag |
| Invalid entity ID | Skip entity, continue |

---

## 6. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `GetContext_ReturnsRelevantEntities` | Query finds correct entities |
| `GetContext_RespectsTokenBudget` | Context within limits |
| `GetContext_IncludesRelationships` | Relationships retrieved |
| `RankEntities_HighestScoreFirst` | Ranking works correctly |
| `FormatContext_Markdown` | Markdown format correct |

---

## 7. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Entity context only |
| Teams | Full context (entities + axioms) |
| Enterprise | Full + custom formats |

---

## 8. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
