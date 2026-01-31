# LCS-DES-v0.10.2-KG-d: Design Specification — Incremental Inference

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-102-d` | Inference Engine sub-part d |
| **Feature Name** | `Incremental Inference` | Recompute only affected derivations |
| **Target Version** | `v0.10.2d` | Fourth sub-part of v0.10.2-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Composite Knowledge & Versioned Store |
| **Swimlane** | `Knowledge Graph` | Knowledge Graph vertical |
| **License Tier** | `WriterPro` / `Teams` / `Enterprise` | Built-in + Custom rules support |
| **Feature Gate Key** | `FeatureFlags.CKVS.InferenceEngine` | Inference engine feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.2-KG](./LCS-SBD-v0.10.2-KG.md) | Inference Engine scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.2-KG S2.1](./LCS-SBD-v0.10.2-KG.md#21-sub-parts) | d = Incremental Inference |

---

## 2. Executive Summary

### 2.1 The Requirement

When the knowledge graph changes, re-running full inference is wasteful. Incremental inference must:

1. **Detect** which facts changed in the graph
2. **Find** which derived facts depend on changed facts
3. **Retract** dependent facts that may no longer be valid
4. **Re-derive** only affected facts using changed rules
5. **Propagate** changes downstream to cascading dependencies

### 2.2 The Proposed Solution

Implement dependency tracking and selective re-inference:

1. **Change Detection:** Monitor graph modifications (entities, relationships, properties)
2. **Dependency Graph:** Track which rules depend on which facts
3. **Affected Finder:** Identify downstream impacts of changes
4. **Selective Re-inference:** Run only relevant rules
5. **Propagation:** Update all derived facts in the dependency chain

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IInferenceEngine` | v0.10.2c | Run inference on affected rules |
| `IGraphRepository` | v0.4.5e | Load facts and changes |
| `ISyncService` | v0.7.6-KG | Get change notifications |
| Compiled Rules | v0.10.2b | Track rule dependencies |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Uses only standard C# |

### 3.2 Licensing Behavior

Same as forward chainer (v0.10.2c):
- **WriterPro:** Built-in rules only
- **Teams:** Up to 50 custom rules
- **Enterprise:** Unlimited rules

---

## 4. Data Contract (The API)

### 4.1 Change Detection Types

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Represents a change in the knowledge graph.
/// </summary>
public abstract record GraphChange;

/// <summary>
/// An entity was created, modified, or deleted.
/// </summary>
public record EntityChange(
    Guid EntityId,
    string EntityType,
    GraphChangeKind Kind,
    Dictionary<string, object?>? OldProperties = null,
    Dictionary<string, object?>? NewProperties = null) : GraphChange;

/// <summary>
/// A relationship was created or deleted.
/// </summary>
public record RelationshipChange(
    Guid SourceEntityId,
    string RelationshipType,
    Guid TargetEntityId,
    GraphChangeKind Kind,
    string? TargetType = null) : GraphChange;

/// <summary>
/// A property value changed.
/// </summary>
public record PropertyChange(
    Guid EntityId,
    string PropertyName,
    object? OldValue,
    object? NewValue) : GraphChange;

/// <summary>
/// Type of change.
/// </summary>
public enum GraphChangeKind
{
    Created = 1,
    Modified = 2,
    Deleted = 3
}

/// <summary>
/// Batch of changes from a single graph update.
/// </summary>
public record GraphChangeBatch
{
    /// <summary>
    /// Changes in this batch.
    /// </summary>
    public required IReadOnlyList<GraphChange> Changes { get; init; }

    /// <summary>
    /// Timestamp when batch was recorded.
    /// </summary>
    public DateTimeOffset RecordedAt { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// User who made the changes.
    /// </summary>
    public Guid? ChangedBy { get; init; }
}
```

### 4.2 Dependency Graph

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Tracks dependencies between rules and facts.
/// </summary>
public record RuleDependency
{
    /// <summary>
    /// The rule ID.
    /// </summary>
    public required Guid RuleId { get; init; }

    /// <summary>
    /// Entity types this rule pattern-matches on.
    /// </summary>
    public IReadOnlyList<string> SourceEntityTypes { get; init; } = [];

    /// <summary>
    /// Relationship types this rule uses in conditions.
    /// </summary>
    public IReadOnlyList<string> SourceRelationshipTypes { get; init; } = [];

    /// <summary>
    /// Property names this rule checks.
    /// </summary>
    public IReadOnlyList<string> SourcePropertyNames { get; init; } = [];

    /// <summary>
    /// Relationship types this rule derives.
    /// </summary>
    public IReadOnlyList<string> DerivedRelationshipTypes { get; init; } = [];

    /// <summary>
    /// Property names this rule derives.
    /// </summary>
    public IReadOnlyList<string> DerivedPropertyNames { get; init; } = [];
}

/// <summary>
/// Manages the dependency graph between rules and facts.
/// </summary>
public interface IDependencyGraph
{
    /// <summary>
    /// Adds rule dependencies.
    /// </summary>
    void RegisterDependencies(IReadOnlyList<RuleDependency> dependencies);

    /// <summary>
    /// Finds rules that depend on changed facts.
    /// </summary>
    IReadOnlySet<Guid> FindAffectedRules(
        IReadOnlyList<GraphChange> changes);

    /// <summary>
    /// Finds derived facts that depend on changed facts.
    /// </summary>
    IReadOnlySet<Guid> FindAffectedDerivedFacts(
        IReadOnlyList<GraphChange> changes);

    /// <summary>
    /// Finds rules whose output might affect other rules.
    /// </summary>
    IReadOnlySet<Guid> FindDownstreamRules(Guid ruleId);

    /// <summary>
    /// Clears all dependency information.
    /// </summary>
    void Clear();
}
```

### 4.3 Incremental Inference Interface

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Extension to IInferenceEngine for incremental updates.
/// </summary>
public interface IIncrementalInferenceEngine : IInferenceEngine
{
    /// <summary>
    /// Runs incremental inference after graph changes.
    /// Retracts affected derived facts and re-derives them.
    /// </summary>
    Task<IncrementalInferenceResult> InferIncrementalAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default);
}

/// <summary>
/// Result of incremental inference execution.
/// </summary>
public record IncrementalInferenceResult
{
    /// <summary>
    /// Overall status.
    /// </summary>
    public required InferenceStatus Status { get; init; }

    /// <summary>
    /// Changes that triggered inference.
    /// </summary>
    public required IReadOnlyList<GraphChange> TriggeredBy { get; init; }

    /// <summary>
    /// Rules that were affected by changes.
    /// </summary>
    public IReadOnlyList<Guid> AffectedRuleIds { get; init; } = [];

    /// <summary>
    /// Derived facts that were retracted.
    /// </summary>
    public IReadOnlyList<Guid> RetractedFactIds { get; init; } = [];

    /// <summary>
    /// New facts derived after the change.
    /// </summary>
    public IReadOnlyList<DerivedFact> NewFacts { get; init; } = [];

    /// <summary>
    /// Derived facts that were updated (retracted then re-derived).
    /// </summary>
    public IReadOnlyList<Guid> UpdatedFactIds { get; init; } = [];

    /// <summary>
    /// Total number of affected entities.
    /// </summary>
    public int AffectedEntityCount { get; init; }

    /// <summary>
    /// Execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Warnings during inference.
    /// </summary>
    public IReadOnlyList<InferenceWarning> Warnings { get; init; } = [];
}
```

---

## 5. Incremental Inference Algorithm

### 5.1 Change Detection Flow

```
ALGORITHM IncrementalInference:
  INPUT: GraphChanges, DependencyGraph, IInferenceEngine
  OUTPUT: IncrementalInferenceResult

  affected_rules ← DependencyGraph.FindAffectedRules(GraphChanges)
  affected_facts ← DependencyGraph.FindAffectedDerivedFacts(GraphChanges)

  retracted_facts ← Empty
  FOR EACH affected_fact:
    Retract affected_fact from knowledge graph
    retracted_facts.Add(affected_fact)

  new_facts ← Empty
  FOR EACH affected_rule:
    results ← InferenceEngine.Infer({RuleIds: [affected_rule]})
    new_facts.AddAll(results.NewFacts)

  // Propagate changes to downstream rules
  downstream_rules ← DependencyGraph.FindDownstream(affected_rules)
  FOR EACH downstream_rule:
    results ← InferenceEngine.Infer({RuleIds: [downstream_rule]})
    new_facts.AddAll(results.NewFacts)

  RETURN Result(affected_rules, retracted_facts, new_facts)
```

### 5.2 Affected Fact Finder

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Identifies which derived facts are affected by graph changes.
/// </summary>
public interface IAffectedFactFinder
{
    /// <summary>
    /// Finds all derived facts that depend on changed facts.
    /// </summary>
    Task<IReadOnlySet<Guid>> FindAffectedAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default);

    /// <summary>
    /// Finds facts that depend on a specific entity or relationship.
    /// </summary>
    Task<IReadOnlySet<Guid>> FindDependentAsync(
        Guid entityId,
        string? relationshipType = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the dependency chain for a fact.
    /// </summary>
    Task<IReadOnlyList<DerivedFact>> GetDependencyChainAsync(
        Guid factId,
        CancellationToken ct = default);
}
```

---

## 6. Implementation Strategy

### 6.1 Dependency Tracking

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Extracts dependencies from compiled rules.
/// </summary>
public class RuleDependencyExtractor
{
    /// <summary>
    /// Analyzes a compiled rule to extract dependencies.
    /// </summary>
    public RuleDependency Extract(CompiledRule rule)
    {
        var sourceEntityTypes = new HashSet<string>();
        var sourceRelTypes = new HashSet<string>();
        var sourceProperties = new HashSet<string>();
        var derivedRelTypes = new HashSet<string>();
        var derivedProperties = new HashSet<string>();

        // Analyze condition instructions
        foreach (var instr in rule.ConditionInstructions)
        {
            if (instr is MatchPatternInstruction pattern)
            {
                sourceRelTypes.Add(pattern.RelationshipType);
            }
            else if (instr is MatchTypeInstruction type)
            {
                sourceEntityTypes.Add(type.EntityType);
            }
            else if (instr is MatchPropertyInstruction prop)
            {
                sourceProperties.Add(prop.PropertyName);
            }
        }

        // Analyze conclusion instructions
        foreach (var instr in rule.ConclusionInstructions)
        {
            if (instr is DeriveRelationshipInstruction rel)
            {
                derivedRelTypes.Add(rel.RelationshipType);
            }
            else if (instr is DerivePropertyInstruction propInstr)
            {
                derivedProperties.Add(propInstr.PropertyName);
            }
        }

        return new RuleDependency
        {
            RuleId = rule.Original.RuleId,
            SourceEntityTypes = sourceEntityTypes.ToList(),
            SourceRelationshipTypes = sourceRelTypes.ToList(),
            SourcePropertyNames = sourceProperties.ToList(),
            DerivedRelationshipTypes = derivedRelTypes.ToList(),
            DerivedPropertyNames = derivedProperties.ToList()
        };
    }
}
```

### 6.2 Dependency Graph Implementation

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// In-memory dependency graph.
/// </summary>
public class DependencyGraph : IDependencyGraph
{
    private readonly Dictionary<Guid, RuleDependency> _dependencies = [];

    public void RegisterDependencies(IReadOnlyList<RuleDependency> dependencies)
    {
        foreach (var dep in dependencies)
        {
            _dependencies[dep.RuleId] = dep;
        }
    }

    public IReadOnlySet<Guid> FindAffectedRules(
        IReadOnlyList<GraphChange> changes)
    {
        var affected = new HashSet<Guid>();

        foreach (var change in changes)
        {
            foreach (var (ruleId, dep) in _dependencies)
            {
                if (IsAffected(change, dep))
                {
                    affected.Add(ruleId);
                }
            }
        }

        return affected;
    }

    public IReadOnlySet<Guid> FindAffectedDerivedFacts(
        IReadOnlyList<GraphChange> changes)
    {
        // Query fact provenance to find which derived facts depend on changed facts
        // (Handled by fact store, covered in v0.10.2e)
        throw new NotImplementedException();
    }

    public IReadOnlySet<Guid> FindDownstreamRules(Guid ruleId)
    {
        var downstream = new HashSet<Guid>();

        if (!_dependencies.TryGetValue(ruleId, out var sourceDep))
            return downstream;

        // Find rules that depend on output of this rule
        foreach (var (otherId, otherDep) in _dependencies)
        {
            if (otherId == ruleId)
                continue;

            // Check if otherDep's inputs overlap with sourceDep's outputs
            if (otherDep.SourceRelationshipTypes
                .Intersect(sourceDep.DerivedRelationshipTypes).Any() ||
                otherDep.SourcePropertyNames
                .Intersect(sourceDep.DerivedPropertyNames).Any())
            {
                downstream.Add(otherId);
            }
        }

        return downstream;
    }

    public void Clear()
    {
        _dependencies.Clear();
    }

    private bool IsAffected(GraphChange change, RuleDependency dep)
    {
        return change switch
        {
            EntityChange ec =>
                dep.SourceEntityTypes.Contains(ec.EntityType),

            RelationshipChange rc =>
                dep.SourceRelationshipTypes.Contains(rc.RelationshipType),

            PropertyChange pc =>
                dep.SourcePropertyNames.Contains(pc.PropertyName),

            _ => false
        };
    }
}
```

### 6.3 Incremental Inference Implementation

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Incremental inference engine implementation.
/// </summary>
public class IncrementalInferenceEngine : IIncrementalInferenceEngine
{
    private readonly IInferenceEngine _baseEngine;
    private readonly IDependencyGraph _dependencyGraph;
    private readonly IAffectedFactFinder _factFinder;
    private readonly IGraphRepository _graph;
    private readonly ILogger<IncrementalInferenceEngine> _logger;

    public async Task<InferenceResult> InferIncrementalAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // 1. Find affected rules
            var affectedRuleIds = _dependencyGraph.FindAffectedRules(changes);
            _logger.LogInformation("Incremental inference: {RuleCount} affected rules",
                affectedRuleIds.Count);

            if (affectedRuleIds.Count == 0)
            {
                return new InferenceResult
                {
                    Status = InferenceStatus.Success,
                    Duration = sw.Elapsed,
                    RulesEvaluated = 0
                };
            }

            // 2. Find and retract affected derived facts
            var affectedFactIds = await _factFinder.FindAffectedAsync(changes, ct);
            _logger.LogInformation("Retracting {FactCount} affected facts",
                affectedFactIds.Count);

            foreach (var factId in affectedFactIds)
            {
                await _graph.RetractFactAsync(factId, ct);
            }

            // 3. Re-run affected rules
            var options = new InferenceOptions
            {
                RuleIds = affectedRuleIds.ToList()
            };

            var result = await _baseEngine.InferAsync(options, ct);

            // 4. Find and run downstream rules
            var downstreamRuleIds = new HashSet<Guid>();
            foreach (var ruleId in affectedRuleIds)
            {
                var downstream = _dependencyGraph.FindDownstreamRules(ruleId);
                downstreamRuleIds.UnionWith(downstream);
            }

            if (downstreamRuleIds.Count > 0)
            {
                _logger.LogInformation("Running {DownstreamCount} downstream rules",
                    downstreamRuleIds.Count);

                var downstreamOptions = new InferenceOptions
                {
                    RuleIds = downstreamRuleIds.ToList()
                };

                var downstreamResult = await _baseEngine.InferAsync(
                    downstreamOptions, ct);

                // Combine results
                var allFacts = new List<DerivedFact>(result.NewFacts);
                allFacts.AddRange(downstreamResult.NewFacts);

                result = result with
                {
                    NewFacts = allFacts.AsReadOnly(),
                    FactsDerived = allFacts.Count
                };
            }

            sw.Stop();

            return result with
            {
                Duration = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Incremental inference error");
            sw.Stop();

            return new InferenceResult
            {
                Status = InferenceStatus.RuleError,
                Duration = sw.Elapsed
            };
        }
    }

    // ... other IInferenceEngine methods delegated to _baseEngine ...
}
```

---

## 7. Optimization Strategies

### 7.1 Rete Algorithm (Optional Enhancement)

The Rete (Recognize-Execute) algorithm optimizes pattern matching:

```
- Beta network: Stores partial matches
- Alpha network: Caches entity/property lookups
- Result nodes: Cache final rule matches

Benefits:
- Avoid re-matching unchanged patterns
- Share computation across similar rules
- Incremental updates to match network
```

### 7.2 Indexing Strategy

```csharp
// Index derived facts by:
// - Source entity ID
// - Relationship type
// - Rule ID
// - Property name

// This enables O(1) lookup of affected facts on changes
```

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class IncrementalInferenceTests
{
    [TestMethod]
    public async Task InferIncremental_EntityCreated_AffectsRules()
    {
        // Setup: Entity A exists and has rule
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        await _graph.UpsertEntityAsync(a, "Person");

        // Execute: Add parent relationship A->B
        var changes = new[] {
            new RelationshipChange(a, "PARENT_OF", b, GraphChangeKind.Created)
        };

        var result = await _engine.InferIncrementalAsync(changes);

        // Verify: Grandparent rules triggered
        Assert.IsTrue(result.AffectedRuleIds.Count > 0);
    }

    [TestMethod]
    public async Task InferIncremental_NoAffectedRules_ReturnsFast()
    {
        // Setup: Create entity not used by any rule
        var changes = new[] {
            new EntityChange(Guid.NewGuid(), "UnusedType", GraphChangeKind.Created)
        };

        var result = await _engine.InferIncrementalAsync(changes);

        // Verify: No affected rules
        Assert.AreEqual(0, result.AffectedRuleIds.Count);
    }

    [TestMethod]
    public async Task InferIncremental_PropagatesDownstream()
    {
        // Setup: Rules A produces facts used by rule B
        // Change something affecting rule A
        // Verify: Rule B is also re-executed

        var changes = new[] { /* ... */ };
        var result = await _engine.InferIncrementalAsync(changes);

        // Verify both rule A and downstream rule B in affected list
        Assert.IsTrue(result.AffectedRuleIds.Count >= 2);
    }

    [TestMethod]
    public async Task DependencyGraph_FindDownstreamRules_Correct()
    {
        var ruleA = Guid.NewGuid();
        var ruleB = Guid.NewGuid();

        _dependencyGraph.RegisterDependencies(new[]
        {
            new RuleDependency {
                RuleId = ruleA,
                DerivedRelationshipTypes = new[] { "DEPENDS_ON" }
            },
            new RuleDependency {
                RuleId = ruleB,
                SourceRelationshipTypes = new[] { "DEPENDS_ON" }
            }
        });

        var downstream = _dependencyGraph.FindDownstreamRules(ruleA);

        Assert.IsTrue(downstream.Contains(ruleB));
    }
}
```

---

## 9. Error Handling

### 9.1 Partial Failure Handling

- If retraction fails: Log error, continue with re-inference
- If re-inference fails: Return status with partial results
- If downstream fails: Continue and report warnings

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Cascading retractions | High | Careful fact dependency tracking |
| Missed updates | Medium | Complete affect analysis |
| Performance regression | Medium | Incremental > full when < 20% affected |

---

## 11. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Change detection | <100ms | Dependency graph lookup |
| Affected finder | <200ms | Index-based fact lookup |
| Incremental inference | <2s | Only re-run necessary rules |

---

## 12. License Gating

Same as forward chainer (v0.10.2c).

---

## 13. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Graph change + dependency graph | InferIncremental | Affected rules identified |
| 2 | Affected derived facts | Retracting | Facts removed from graph |
| 3 | Affected rules | Re-running | New facts re-derived |
| 4 | Downstream rules | Re-executing | Cascading updates applied |
| 5 | Unaffected rules | Running | Not re-executed (optimization) |
| 6 | No affected rules | Change detected | Fast return with zero work |
| 7 | Retraction error | Handling | Continue with re-inference, warn |
| 8 | Result metric | Returned | AffectedRuleIds populated |

---

## 14. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial specification - incremental algorithm, dependency tracking |
