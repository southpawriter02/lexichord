# LCS-DES-072-KG-e: Knowledge Context Strategy

## Document Control

| Field | Value |
| :--- | :--- |
| **Spec ID** | LCS-DES-072-KG-e |
| **System Breakdown** | LCS-SBD-072-KG |
| **Version** | v0.7.2 |
| **Codename** | Knowledge Context Strategy (CKVS Phase 4a) |
| **Estimated Hours** | 5 |
| **Status** | Draft |
| **Last Updated** | 2026-01-31 |

---

## 1. Overview

### 1.1 Purpose

The **Knowledge Context Strategy** implements `IContextStrategy` to provide Knowledge Graph data to the Context Assembler. This enables all specialist agents to leverage graph entities, relationships, and axioms in their context windows.

### 1.2 Key Responsibilities

- Implement IContextStrategy for knowledge graph data
- Query graph for entities relevant to context request
- Apply relevance scoring to rank entities
- Format knowledge data for prompt injection
- Respect token budget constraints
- Support agent-specific configurations

### 1.3 Module Location

```
src/
  Lexichord.KnowledgeGraph/
    Context/
      Strategy/
        IKnowledgeContextStrategy.cs
        KnowledgeContextStrategy.cs
        KnowledgeContextConfig.cs
```

---

## 2. Interface Definitions

### 2.1 Knowledge Context Strategy Interface

```csharp
namespace Lexichord.KnowledgeGraph.Context.Strategy;

/// <summary>
/// Context strategy providing knowledge graph data to agents.
/// </summary>
public interface IKnowledgeContextStrategy : IContextStrategy
{
    /// <summary>
    /// Gets knowledge context for a request.
    /// </summary>
    Task<ContextFragment> GetContextAsync(
        ContextRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Gets knowledge context with specific configuration.
    /// </summary>
    Task<KnowledgeContextFragment> GetKnowledgeContextAsync(
        ContextRequest request,
        KnowledgeContextConfig config,
        CancellationToken ct = default);

    /// <summary>
    /// Gets configuration for an agent type.
    /// </summary>
    KnowledgeContextConfig GetConfigForAgent(AgentType agentType);
}
```

### 2.2 Base Context Strategy Interface

```csharp
/// <summary>
/// Base interface for context strategies (from v0.7.2).
/// </summary>
public interface IContextStrategy
{
    /// <summary>Strategy name.</summary>
    string Name { get; }

    /// <summary>Strategy priority (lower = earlier in assembly).</summary>
    int Priority { get; }

    /// <summary>Whether strategy is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Gets context fragment for a request.</summary>
    Task<ContextFragment> GetContextAsync(
        ContextRequest request,
        CancellationToken ct = default);
}
```

---

## 3. Data Types

### 3.1 Knowledge Context Configuration

```csharp
/// <summary>
/// Configuration for knowledge context retrieval.
/// </summary>
public record KnowledgeContextConfig
{
    /// <summary>Maximum tokens for knowledge context.</summary>
    public int MaxTokens { get; init; } = 4000;

    /// <summary>Entity types to include (null = all).</summary>
    public IReadOnlyList<string>? IncludeEntityTypes { get; init; }

    /// <summary>Entity types to exclude.</summary>
    public IReadOnlyList<string>? ExcludeEntityTypes { get; init; }

    /// <summary>Minimum relevance score (0-1).</summary>
    public float MinRelevanceScore { get; init; } = 0.5f;

    /// <summary>Include relationships.</summary>
    public bool IncludeRelationships { get; init; } = true;

    /// <summary>Maximum relationship depth.</summary>
    public int RelationshipDepth { get; init; } = 1;

    /// <summary>Include applicable axioms.</summary>
    public bool IncludeAxioms { get; init; } = true;

    /// <summary>Maximum entities to include.</summary>
    public int MaxEntities { get; init; } = 20;

    /// <summary>Context format.</summary>
    public ContextFormat Format { get; init; } = ContextFormat.Yaml;

    /// <summary>Whether to include entity properties.</summary>
    public bool IncludeProperties { get; init; } = true;

    /// <summary>Maximum properties per entity.</summary>
    public int MaxPropertiesPerEntity { get; init; } = 10;
}
```

### 3.2 Knowledge Context Fragment

```csharp
/// <summary>
/// Context fragment containing knowledge graph data.
/// </summary>
public record KnowledgeContextFragment : ContextFragment
{
    /// <summary>Entities in context.</summary>
    public required IReadOnlyList<KnowledgeEntity> Entities { get; init; }

    /// <summary>Relationships in context.</summary>
    public IReadOnlyList<KnowledgeRelationship> Relationships { get; init; } = [];

    /// <summary>Applicable axioms.</summary>
    public IReadOnlyList<Axiom> Axioms { get; init; } = [];

    /// <summary>Relevance scores per entity.</summary>
    public IReadOnlyDictionary<Guid, float> RelevanceScores { get; init; } =
        new Dictionary<Guid, float>();

    /// <summary>Total entities available (before filtering).</summary>
    public int TotalEntitiesAvailable { get; init; }

    /// <summary>Whether context was truncated.</summary>
    public bool WasTruncated { get; init; }
}

/// <summary>
/// Base context fragment (from v0.7.2).
/// </summary>
public record ContextFragment
{
    /// <summary>Strategy that produced this fragment.</summary>
    public required string StrategyName { get; init; }

    /// <summary>Formatted content for prompt.</summary>
    public required string FormattedContent { get; init; }

    /// <summary>Token count.</summary>
    public int TokenCount { get; init; }

    /// <summary>Metadata.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 3.3 Context Request

```csharp
/// <summary>
/// Request for context (from v0.7.2).
/// </summary>
public record ContextRequest
{
    /// <summary>Agent type requesting context.</summary>
    public required AgentType AgentType { get; init; }

    /// <summary>User query/request.</summary>
    public required string Query { get; init; }

    /// <summary>Current document (if any).</summary>
    public Document? Document { get; init; }

    /// <summary>Selection in document (if any).</summary>
    public TextSpan? Selection { get; init; }

    /// <summary>Token budget for all context.</summary>
    public int TokenBudget { get; init; } = 8000;

    /// <summary>Workspace ID.</summary>
    public Guid? WorkspaceId { get; init; }
}

public enum AgentType
{
    Editor,
    Simplifier,
    Tuning,
    Summarizer,
    Copilot
}
```

---

## 4. Implementation

### 4.1 Knowledge Context Strategy

```csharp
public class KnowledgeContextStrategy : IKnowledgeContextStrategy
{
    private readonly IGraphRepository _graphRepository;
    private readonly IAxiomStore _axiomStore;
    private readonly IEntityRelevanceScorer _relevanceScorer;
    private readonly IKnowledgeContextFormatter _formatter;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<KnowledgeContextStrategy> _logger;

    private readonly Dictionary<AgentType, KnowledgeContextConfig> _agentConfigs;

    public string Name => "KnowledgeContext";
    public int Priority => 30; // After Document and RAG
    public bool IsEnabled => true;

    public KnowledgeContextStrategy(
        IGraphRepository graphRepository,
        IAxiomStore axiomStore,
        IEntityRelevanceScorer relevanceScorer,
        IKnowledgeContextFormatter formatter,
        ILicenseService licenseService,
        ILogger<KnowledgeContextStrategy> logger)
    {
        _graphRepository = graphRepository;
        _axiomStore = axiomStore;
        _relevanceScorer = relevanceScorer;
        _formatter = formatter;
        _licenseService = licenseService;
        _logger = logger;

        _agentConfigs = InitializeAgentConfigs();
    }

    public async Task<ContextFragment> GetContextAsync(
        ContextRequest request,
        CancellationToken ct = default)
    {
        var config = GetConfigForAgent(request.AgentType);
        return await GetKnowledgeContextAsync(request, config, ct);
    }

    public async Task<KnowledgeContextFragment> GetKnowledgeContextAsync(
        ContextRequest request,
        KnowledgeContextConfig config,
        CancellationToken ct = default)
    {
        // Check license
        var license = await _licenseService.GetCurrentLicenseAsync(ct);
        config = ApplyLicenseRestrictions(config, license.Tier);

        // 1. Search for relevant entities
        var searchQuery = BuildSearchQuery(request, config);
        var entities = await _graphRepository.SearchEntitiesAsync(searchQuery, ct);

        _logger.LogDebug(
            "Found {Count} entities for query: {Query}",
            entities.Count, request.Query);

        // 2. Score entities by relevance
        var scoredEntities = await _relevanceScorer.ScoreEntitiesAsync(
            request, entities, ct);

        // 3. Filter by minimum relevance
        var filteredEntities = scoredEntities
            .Where(se => se.Score >= config.MinRelevanceScore)
            .OrderByDescending(se => se.Score)
            .Take(config.MaxEntities)
            .ToList();

        var selectedEntities = filteredEntities.Select(se => se.Entity).ToList();
        var relevanceScores = filteredEntities.ToDictionary(se => se.Entity.Id, se => se.Score);

        // 4. Get relationships if requested
        IReadOnlyList<KnowledgeRelationship> relationships = [];
        if (config.IncludeRelationships)
        {
            relationships = await GetRelationshipsAsync(selectedEntities, config, ct);
        }

        // 5. Get axioms if requested
        IReadOnlyList<Axiom> axioms = [];
        if (config.IncludeAxioms)
        {
            axioms = await GetAxiomsAsync(selectedEntities, ct);
        }

        // 6. Format for prompt
        var formatted = _formatter.Format(
            selectedEntities, relationships, axioms, config);

        // 7. Check token budget
        var wasTruncated = false;
        if (formatted.TokenCount > config.MaxTokens)
        {
            formatted = _formatter.TruncateToTokenBudget(formatted, config.MaxTokens);
            wasTruncated = true;
        }

        return new KnowledgeContextFragment
        {
            StrategyName = Name,
            FormattedContent = formatted.Content,
            TokenCount = formatted.TokenCount,
            Entities = selectedEntities,
            Relationships = relationships,
            Axioms = axioms,
            RelevanceScores = relevanceScores,
            TotalEntitiesAvailable = entities.Count,
            WasTruncated = wasTruncated,
            Metadata = new Dictionary<string, object>
            {
                ["agentType"] = request.AgentType.ToString(),
                ["entityCount"] = selectedEntities.Count,
                ["relationshipCount"] = relationships.Count,
                ["axiomCount"] = axioms.Count
            }
        };
    }

    public KnowledgeContextConfig GetConfigForAgent(AgentType agentType)
    {
        return _agentConfigs.TryGetValue(agentType, out var config)
            ? config
            : new KnowledgeContextConfig();
    }

    private Dictionary<AgentType, KnowledgeContextConfig> InitializeAgentConfigs()
    {
        return new Dictionary<AgentType, KnowledgeContextConfig>
        {
            [AgentType.Editor] = new()
            {
                MaxEntities = 30,
                MaxTokens = 5000,
                IncludeAxioms = true,
                IncludeRelationships = true,
                MinRelevanceScore = 0.4f
            },
            [AgentType.Simplifier] = new()
            {
                MaxEntities = 10,
                MaxTokens = 2000,
                IncludeEntityTypes = ["Concept", "Term", "Definition"],
                IncludeAxioms = false,
                IncludeRelationships = false,
                MinRelevanceScore = 0.6f
            },
            [AgentType.Tuning] = new()
            {
                MaxEntities = 20,
                MaxTokens = 4000,
                IncludeEntityTypes = ["Endpoint", "Parameter", "Response", "Schema"],
                IncludeAxioms = true,
                IncludeRelationships = true,
                MinRelevanceScore = 0.5f
            },
            [AgentType.Summarizer] = new()
            {
                MaxEntities = 15,
                MaxTokens = 3000,
                IncludeEntityTypes = ["Product", "Component", "Feature", "Service"],
                IncludeAxioms = false,
                IncludeRelationships = true,
                MinRelevanceScore = 0.5f
            },
            [AgentType.Copilot] = new()
            {
                MaxEntities = 25,
                MaxTokens = 4000,
                IncludeAxioms = true,
                IncludeRelationships = true,
                MinRelevanceScore = 0.4f
            }
        };
    }

    private EntitySearchQuery BuildSearchQuery(
        ContextRequest request,
        KnowledgeContextConfig config)
    {
        return new EntitySearchQuery
        {
            Query = request.Query,
            EntityTypes = config.IncludeEntityTypes,
            ExcludeTypes = config.ExcludeEntityTypes,
            WorkspaceId = request.WorkspaceId,
            MaxResults = config.MaxEntities * 3 // Over-fetch for scoring
        };
    }

    private async Task<IReadOnlyList<KnowledgeRelationship>> GetRelationshipsAsync(
        IReadOnlyList<KnowledgeEntity> entities,
        KnowledgeContextConfig config,
        CancellationToken ct)
    {
        var entityIds = entities.Select(e => e.Id).ToHashSet();
        var relationships = new List<KnowledgeRelationship>();

        foreach (var entity in entities.Take(10)) // Limit for performance
        {
            var rels = await _graphRepository.GetRelationshipsAsync(
                entity.Id, config.RelationshipDepth, ct);

            relationships.AddRange(rels.Where(r =>
                entityIds.Contains(r.FromEntityId) &&
                entityIds.Contains(r.ToEntityId)));
        }

        return relationships.Distinct().ToList();
    }

    private async Task<IReadOnlyList<Axiom>> GetAxiomsAsync(
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

    private KnowledgeContextConfig ApplyLicenseRestrictions(
        KnowledgeContextConfig config,
        LicenseTier tier)
    {
        return tier switch
        {
            LicenseTier.Core => config with
            {
                MaxEntities = 0, // Not available
                IncludeAxioms = false,
                IncludeRelationships = false
            },
            LicenseTier.WriterPro => config with
            {
                IncludeAxioms = false,
                MaxEntities = Math.Min(config.MaxEntities, 10)
            },
            _ => config
        };
    }
}
```

---

## 5. Error Handling

| Error | Handling Strategy |
| :---- | :---------------- |
| Graph unavailable | Return empty fragment |
| No entities found | Return empty fragment with message |
| Token budget exceeded | Truncate and mark as truncated |
| License check fails | Apply restrictions |

---

## 6. Testing Requirements

| Test Case | Description |
| :-------- | :---------- |
| `GetContext_ReturnsEntities` | Entities returned |
| `GetContext_RespectsConfig` | Config honored |
| `GetContext_RespectsTokenBudget` | Budget enforced |
| `GetConfigForAgent_ReturnsCorrect` | Agent configs |
| `ApplyLicenseRestrictions_Works` | License gating |

---

## 7. License Gating

| Tier | Access |
| :--- | :----- |
| Core | Not available |
| WriterPro | Entities only (max 10) |
| Teams | Full (entities + rels + axioms) |
| Enterprise | Full + custom configs |

---

## 8. Changelog

| Version | Date | Author | Changes |
| :------ | :--- | :----- | :------ |
| 1.0 | 2026-01-31 | Lead Architect | Initial creation |
