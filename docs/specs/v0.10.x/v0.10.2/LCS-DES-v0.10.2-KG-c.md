# LCS-DES-v0.10.2-KG-c: Design Specification — Forward Chainer

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-102-c` | Inference Engine sub-part c |
| **Feature Name** | `Forward Chainer` | Execute rules to derive new facts |
| **Target Version** | `v0.10.2c` | Third sub-part of v0.10.2-KG |
| **Module Scope** | `Lexichord.Modules.CKVS` | Composite Knowledge & Versioned Store |
| **Swimlane** | `Knowledge Graph` | Knowledge Graph vertical |
| **License Tier** | `WriterPro` / `Teams` / `Enterprise` | Built-in + Custom rules support |
| **Feature Gate Key** | `FeatureFlags.CKVS.InferenceEngine` | Inference engine feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.2-KG](./LCS-SBD-v0.10.2-KG.md) | Inference Engine scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.2-KG S2.1](./LCS-SBD-v0.10.2-KG.md#21-sub-parts) | c = Forward Chainer |

---

## 2. Executive Summary

### 2.1 The Requirement

The Forward Chainer is the core inference execution engine. It must:

1. **Match** compiled rules against working memory (in-memory facts)
2. **Apply** rule conclusions to derive new facts
3. **Iterate** until no new facts are derived (fixpoint)
4. **Detect** infinite loops and apply safeguards
5. **Return** results with performance metrics and warnings

### 2.2 The Proposed Solution

Implement a forward-chaining algorithm with:

1. **Working Memory:** In-memory fact store loaded from knowledge graph
2. **Agenda:** Priority queue of rule firings
3. **Conflict Resolution:** Priority-based rule selection
4. **Fact Derivation:** Create and persist derived facts
5. **Loop Detection:** Cycle detection, iteration limits, timeout guards

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IRuleCompiler` | v0.10.2b | Compile rules to executable form |
| `IGraphRepository` | v0.4.5e | Read facts, write derived facts |
| `IValidationEngine` | v0.6.5-KG | Validate derived facts |
| `ISyncService` | v0.7.6-KG | Notification on completion |
| `IMediator` | v0.0.7a | Publish inference events |
| Entity Models | v0.4.5e | Entity/relationship structures |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Forward chaining uses only standard C# |

### 3.2 Licensing Behavior

- **WriterPro Tier:** Run built-in rules only, max 50 iterations
- **Teams Tier:** Run custom rules (up to 50), max 1000 iterations
- **Enterprise Tier:** Unlimited rules and iterations

---

## 4. Data Contract (The API)

### 4.1 IInferenceEngine Interface

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Executes inference rules to derive new facts.
/// </summary>
public interface IInferenceEngine
{
    /// <summary>
    /// Runs inference and returns derived facts.
    /// </summary>
    Task<InferenceResult> InferAsync(
        InferenceOptions options,
        CancellationToken ct = default);

    /// <summary>
    /// Runs incremental inference after graph changes.
    /// </summary>
    Task<InferenceResult> InferIncrementalAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default);

    /// <summary>
    /// Explains how a fact was derived.
    /// </summary>
    Task<DerivationExplanation?> ExplainAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Validates inference rules without executing.
    /// </summary>
    Task<RuleValidationResult> ValidateRulesAsync(
        IReadOnlyList<InferenceRule> rules,
        CancellationToken ct = default);
}

/// <summary>
/// Options for inference execution.
/// </summary>
public record InferenceOptions
{
    /// <summary>
    /// Maximum derivation depth to prevent infinite recursion.
    /// Default: 10
    /// </summary>
    public int MaxDepth { get; init; } = 10;

    /// <summary>
    /// Maximum number of rule firings to prevent infinite loops.
    /// Default: 1000
    /// </summary>
    public int MaxIterations { get; init; } = 1000;

    /// <summary>
    /// Total timeout for inference execution.
    /// Default: 30 seconds
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// If specified, only execute these rule IDs.
    /// </summary>
    public IReadOnlyList<Guid>? RuleIds { get; init; }

    /// <summary>
    /// If specified, only process these entity types.
    /// </summary>
    public IReadOnlyList<string>? EntityTypes { get; init; }

    /// <summary>
    /// If true, perform dry-run without persisting facts.
    /// </summary>
    public bool DryRun { get; init; } = false;
}

/// <summary>
/// Result of an inference run.
/// </summary>
public record InferenceResult
{
    /// <summary>
    /// Overall status of the inference execution.
    /// </summary>
    public required InferenceStatus Status { get; init; }

    /// <summary>
    /// Number of facts derived.
    /// </summary>
    public int FactsDerived { get; init; }

    /// <summary>
    /// Number of facts retracted.
    /// </summary>
    public int FactsRetracted { get; init; }

    /// <summary>
    /// Number of rule evaluations performed.
    /// </summary>
    public int RulesEvaluated { get; init; }

    /// <summary>
    /// Total execution duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Derived facts created.
    /// </summary>
    public IReadOnlyList<DerivedFact> NewFacts { get; init; } = [];

    /// <summary>
    /// Warnings during inference (e.g., unused rules).
    /// </summary>
    public IReadOnlyList<InferenceWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Timestamp when inference completed.
    /// </summary>
    public DateTimeOffset CompletedAt { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Status of inference execution.
/// </summary>
public enum InferenceStatus
{
    /// <summary>Completed successfully, all facts derived.</summary>
    Success = 1,

    /// <summary>Some facts derived but not all rules completed.</summary>
    PartialSuccess = 2,

    /// <summary>Cycle detected in rule dependencies.</summary>
    CycleDetected = 3,

    /// <summary>Error occurred during rule execution.</summary>
    RuleError = 4,

    /// <summary>Execution timed out.</summary>
    Timeout = 5,

    /// <summary>Max iterations reached.</summary>
    MaxIterationsExceeded = 6
}

/// <summary>
/// A fact derived through inference.
/// </summary>
public record DerivedFact
{
    /// <summary>
    /// Unique identifier for the derived fact.
    /// </summary>
    public required Guid FactId { get; init; }

    /// <summary>
    /// Type of fact derived.
    /// </summary>
    public required DerivedFactType FactType { get; init; }

    /// <summary>
    /// Source entity ID (for relationships and properties).
    /// </summary>
    public Guid? SourceEntityId { get; init; }

    /// <summary>
    /// Target entity ID (for relationships).
    /// </summary>
    public Guid? TargetEntityId { get; init; }

    /// <summary>
    /// Relationship type (for relationships).
    /// </summary>
    public string? RelationshipType { get; init; }

    /// <summary>
    /// Property name (for properties).
    /// </summary>
    public string? PropertyName { get; init; }

    /// <summary>
    /// Property value (for properties).
    /// </summary>
    public object? PropertyValue { get; init; }

    /// <summary>
    /// ID of the rule that derived this fact.
    /// </summary>
    public required Guid RuleId { get; init; }

    /// <summary>
    /// Confidence level (0-1, default 1.0 for certain facts).
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Timestamp when fact was derived.
    /// </summary>
    public required DateTimeOffset DerivedAt { get; init; }
}

/// <summary>
/// Type of fact derived.
/// </summary>
public enum DerivedFactType
{
    /// <summary>A relationship between entities.</summary>
    Relationship = 1,

    /// <summary>A property value on an entity.</summary>
    Property = 2,

    /// <summary>A claim assertion.</summary>
    Claim = 3
}

/// <summary>
/// A warning during inference execution.
/// </summary>
public record InferenceWarning
{
    /// <summary>
    /// Warning type.
    /// </summary>
    public required InferenceWarningKind Kind { get; init; }

    /// <summary>
    /// Warning message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Related rule ID if applicable.
    /// </summary>
    public Guid? RuleId { get; init; }

    /// <summary>
    /// Related entity if applicable.
    /// </summary>
    public Guid? EntityId { get; init; }
}

/// <summary>
/// Types of warnings during inference.
/// </summary>
public enum InferenceWarningKind
{
    RuleNeverFired = 1,
    RuleTimeout = 2,
    ConflictingFacts = 3,
    HighIterationCount = 4
}
```

### 4.2 Working Memory & Agenda

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// In-memory fact store for inference matching.
/// </summary>
public interface IWorkingMemory
{
    /// <summary>
    /// Loads initial facts from the knowledge graph.
    /// </summary>
    Task LoadAsync(IGraphRepository graph, CancellationToken ct);

    /// <summary>
    /// Finds relationships matching a pattern.
    /// </summary>
    IEnumerable<(Guid source, Guid target)> FindRelationships(
        string? sourceType,
        string relationshipType,
        string? targetType);

    /// <summary>
    /// Finds entities with a property value.
    /// </summary>
    IEnumerable<Guid> FindEntitiesWithProperty(
        string propertyName,
        object? value,
        PropertyOperator op);

    /// <summary>
    /// Checks if a relationship exists.
    /// </summary>
    bool HasRelationship(Guid source, string relationshipType, Guid target);

    /// <summary>
    /// Checks if an entity has a property value.
    /// </summary>
    bool HasPropertyValue(Guid entity, string propertyName, object? value);

    /// <summary>
    /// Gets entity type.
    /// </summary>
    string? GetEntityType(Guid entity);

    /// <summary>
    /// Adds a derived fact to working memory.
    /// </summary>
    void AddDerivedFact(DerivedFact fact);

    /// <summary>
    /// Removes a fact from working memory.
    /// </summary>
    void RemoveFact(Guid factId);

    /// <summary>
    /// Gets all derived facts.
    /// </summary>
    IReadOnlyList<DerivedFact> GetDerivedFacts();

    /// <summary>
    /// Clears all derived facts.
    /// </summary>
    void ClearDerivedFacts();
}

/// <summary>
/// Priority queue of rule firings.
/// </summary>
public interface IInferenceAgenda
{
    /// <summary>
    /// Adds a rule firing to the agenda.
    /// </summary>
    void Enqueue(CompiledRule rule, VariableBindings bindings);

    /// <summary>
    /// Gets the next highest-priority rule firing.
    /// </summary>
    (CompiledRule rule, VariableBindings bindings)? Dequeue();

    /// <summary>
    /// Gets current queue depth.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Clears the agenda.
    /// </summary>
    void Clear();
}

/// <summary>
/// Variable bindings from condition matching.
/// Maps variable names to entity IDs.
/// </summary>
public record VariableBindings
{
    /// <summary>
    /// Map of variable name to bound entity ID.
    /// </summary>
    public required IReadOnlyDictionary<string, Guid> Bindings { get; init; }
}
```

---

## 5. Forward Chaining Algorithm

### 5.1 Core Algorithm

```
ALGORITHM ForwardChaining:
  INPUT: CompiledRules, GraphRepository, InferenceOptions
  OUTPUT: InferenceResult

  WorkingMemory ← Load facts from GraphRepository
  Agenda ← Empty
  DerivedFacts ← Empty
  Iteration ← 0

  FOR EACH compiled rule:
    initial_matches ← MatchConditions(rule, WorkingMemory)
    FOR EACH match:
      Agenda.Enqueue(rule, match)

  WHILE Agenda.HasMore() AND Iteration < MaxIterations:
    IF elapsed_time > Timeout:
      RETURN Result(Timeout, DerivedFacts, Iteration)

    (rule, bindings) ← Agenda.Dequeue()

    IF NOT MatchConditions(rule, WorkingMemory, bindings):
      CONTINUE

    new_facts ← ExecuteConclusions(rule, bindings)

    FOR EACH new_fact:
      IF new_fact NOT in WorkingMemory:
        WorkingMemory.Add(new_fact)
        DerivedFacts.Add(new_fact)

        FOR EACH affected_rule:
          Agenda.Enqueue(affected_rule, new_match)

    Iteration ← Iteration + 1

  RETURN Result(Success, DerivedFacts, Iteration)
```

### 5.2 Condition Matching

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Matches rule conditions against working memory.
/// </summary>
public interface IRuleMatchEngine
{
    /// <summary>
    /// Finds all bindings that satisfy a rule's conditions.
    /// </summary>
    IEnumerable<VariableBindings> Match(
        CompiledRule rule,
        IWorkingMemory workingMemory);

    /// <summary>
    /// Checks if a set of bindings still satisfies conditions.
    /// </summary>
    bool VerifyBindings(
        CompiledRule rule,
        VariableBindings bindings,
        IWorkingMemory workingMemory);
}
```

**Matching strategy:**

1. Process conditions in order
2. For each pattern condition, generate candidate bindings
3. Filter by subsequent conditions
4. Return all satisfying variable binding sets

### 5.3 Conclusion Execution

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Executes rule conclusions to derive new facts.
/// </summary>
public interface IRuleConclusionExecutor
{
    /// <summary>
    /// Executes conclusions, returning derived facts.
    /// </summary>
    IReadOnlyList<DerivedFact> Execute(
        CompiledRule rule,
        VariableBindings bindings,
        IWorkingMemory workingMemory);
}
```

**Execution strategy:**

1. For each conclusion instruction:
   - DERIVE_REL: Create relationship fact
   - DERIVE_PROP: Create property fact
2. Apply variable substitutions from bindings
3. Assign RuleId and timestamp
4. Return list of new facts

---

## 6. Cycle Detection

### 6.1 Cycle Detection Strategy

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Detects cycles in rule application.
/// </summary>
public interface ICycleDetector
{
    /// <summary>
    /// Checks if applying a new fact would create a cycle.
    /// </summary>
    bool WouldCreateCycle(
        DerivedFact fact,
        IReadOnlyList<DerivedFact> previousFacts);

    /// <summary>
    /// Gets the cycle path if one exists.
    /// </summary>
    IReadOnlyList<DerivedFact>? FindCyclePath(
        DerivedFact fact,
        IReadOnlyList<DerivedFact> previousFacts);
}
```

**Detection strategies:**

1. **Iteration limit:** Stop after MaxIterations firings
2. **Duplicate detection:** Don't re-derive the same fact
3. **Cycle detection:** Track derivation chains, detect self-referential loops
4. **Timeout:** Abort if execution exceeds time limit

---

## 7. Implementation Strategy

### 7.1 Core InferenceEngine Implementation

```csharp
namespace Lexichord.Modules.CKVS.Inference;

/// <summary>
/// Main inference engine implementation.
/// </summary>
public class InferenceEngine : IInferenceEngine
{
    private readonly IRuleCompiler _compiler;
    private readonly IGraphRepository _graph;
    private readonly IValidationEngine _validator;
    private readonly IMediator _mediator;
    private readonly ILogger<InferenceEngine> _logger;
    private readonly IFeatureFlags _featureFlags;

    public InferenceEngine(
        IRuleCompiler compiler,
        IGraphRepository graph,
        IValidationEngine validator,
        IMediator mediator,
        ILogger<InferenceEngine> logger,
        IFeatureFlags featureFlags)
    {
        _compiler = compiler;
        _graph = graph;
        _validator = validator;
        _mediator = mediator;
        _logger = logger;
        _featureFlags = featureFlags;
    }

    public async Task<InferenceResult> InferAsync(
        InferenceOptions options,
        CancellationToken ct = default)
    {
        // Check feature gate
        if (!_featureFlags.IsEnabled(FeatureFlags.CKVS.InferenceEngine))
            return new InferenceResult { Status = InferenceStatus.RuleError };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Load rules
            var rules = await LoadRulesAsync(options, ct);

            // Compile rules
            var compiled = new List<CompiledRule>();
            foreach (var rule in rules)
            {
                var result = await _compiler.CompileAsync(rule, ct);
                if (result.Success && result.Compiled != null)
                    compiled.Add(result.Compiled);
            }

            // Load working memory
            var workingMemory = new WorkingMemory();
            await workingMemory.LoadAsync(_graph, ct);

            // Create agenda and execute
            var agenda = new InferenceAgenda(compiled);
            var derivedFacts = new List<DerivedFact>();

            return await ExecuteForwardChainingAsync(
                compiled, workingMemory, agenda, derivedFacts, options, sw, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inference error");
            sw.Stop();
            return new InferenceResult
            {
                Status = InferenceStatus.RuleError,
                Duration = sw.Elapsed
            };
        }
    }

    private async Task<InferenceResult> ExecuteForwardChainingAsync(
        IReadOnlyList<CompiledRule> compiled,
        IWorkingMemory workingMemory,
        IInferenceAgenda agenda,
        List<DerivedFact> derivedFacts,
        InferenceOptions options,
        System.Diagnostics.Stopwatch sw,
        CancellationToken ct)
    {
        var iteration = 0;
        var rulesEvaluated = 0;
        var warnings = new List<InferenceWarning>();

        // Initial matches
        var matchEngine = new RuleMatchEngine();
        foreach (var rule in compiled)
        {
            var matches = matchEngine.Match(rule, workingMemory);
            foreach (var bindings in matches)
            {
                agenda.Enqueue(rule, bindings);
            }
        }

        // Forward chaining loop
        while (agenda.Count > 0 && iteration < options.MaxIterations)
        {
            ct.ThrowIfCancellationRequested();

            if (sw.Elapsed > options.Timeout)
            {
                return new InferenceResult
                {
                    Status = InferenceStatus.Timeout,
                    FactsDerived = derivedFacts.Count,
                    Duration = sw.Elapsed,
                    NewFacts = derivedFacts.AsReadOnly(),
                    Warnings = warnings.AsReadOnly()
                };
            }

            var firing = agenda.Dequeue();
            if (firing == null)
                break;

            var (rule, bindings) = firing.Value;
            rulesEvaluated++;

            // Verify bindings still valid
            if (!matchEngine.VerifyBindings(rule, bindings, workingMemory))
                continue;

            // Execute conclusions
            var executor = new RuleConclusionExecutor();
            var newFacts = executor.Execute(rule, bindings, workingMemory);

            foreach (var fact in newFacts)
            {
                if (!options.DryRun)
                {
                    await _graph.UpsertFactAsync(fact, ct);
                }

                workingMemory.AddDerivedFact(fact);
                derivedFacts.Add(fact);

                // Re-trigger rules affected by this new fact
                foreach (var affectedRule in compiled)
                {
                    var matches = matchEngine.Match(affectedRule, workingMemory);
                    foreach (var m in matches)
                    {
                        agenda.Enqueue(affectedRule, m);
                    }
                }
            }

            iteration++;
        }

        sw.Stop();

        var status = iteration >= options.MaxIterations
            ? InferenceStatus.MaxIterationsExceeded
            : InferenceStatus.Success;

        return new InferenceResult
        {
            Status = status,
            FactsDerived = derivedFacts.Count,
            RulesEvaluated = rulesEvaluated,
            Duration = sw.Elapsed,
            NewFacts = derivedFacts.AsReadOnly(),
            Warnings = warnings.AsReadOnly()
        };
    }

    private async Task<IReadOnlyList<InferenceRule>> LoadRulesAsync(
        InferenceOptions options,
        CancellationToken ct)
    {
        var allRules = await _graph.GetInferenceRulesAsync(ct);

        if (options.RuleIds != null)
            allRules = allRules.Where(r => options.RuleIds.Contains(r.RuleId)).ToList();

        return allRules
            .Where(r => r.IsEnabled)
            .OrderByDescending(r => r.Priority)
            .ToList();
    }

    public Task<InferenceResult> InferIncrementalAsync(
        IReadOnlyList<GraphChange> changes,
        CancellationToken ct = default)
    {
        // Covered in v0.10.2d
        throw new NotImplementedException();
    }

    public Task<DerivationExplanation?> ExplainAsync(
        Guid factId,
        CancellationToken ct = default)
    {
        // Covered in v0.10.2e
        throw new NotImplementedException();
    }

    public Task<RuleValidationResult> ValidateRulesAsync(
        IReadOnlyList<InferenceRule> rules,
        CancellationToken ct = default)
    {
        // Delegate to compiler
        throw new NotImplementedException();
    }
}
```

---

## 8. Testing

### 8.1 Unit Tests

```csharp
[TestClass]
public class ForwardChainingTests
{
    private InferenceEngine _engine;

    [TestInitialize]
    public void Setup()
    {
        _engine = new InferenceEngine(
            _compiler,
            _graph,
            _validator,
            _mediator,
            _logger,
            _featureFlags);
    }

    [TestMethod]
    public async Task InferAsync_SimpleTransitivity_DerivasFacts()
    {
        // Setup: Add parent relationships
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        await _graph.UpsertRelationshipAsync(a, "PARENT_OF", b);
        await _graph.UpsertRelationshipAsync(b, "PARENT_OF", c);

        // Execute: Run inference
        var result = await _engine.InferAsync(new InferenceOptions());

        // Verify: Grandparent relationship derived
        Assert.IsTrue(result.Status == InferenceStatus.Success);
        Assert.IsTrue(result.FactsDerived > 0);
        Assert.IsTrue(result.NewFacts.Any(f =>
            f.SourceEntityId == a &&
            f.RelationshipType == "GRANDPARENT_OF" &&
            f.TargetEntityId == c));
    }

    [TestMethod]
    public async Task InferAsync_MaxIterations_Stops()
    {
        // Setup: Add rules that would loop infinitely
        // ... setup code ...

        // Execute with low iteration limit
        var result = await _engine.InferAsync(
            new InferenceOptions { MaxIterations = 10 });

        // Verify: Stops at limit
        Assert.AreEqual(InferenceStatus.MaxIterationsExceeded, result.Status);
    }

    [TestMethod]
    public async Task InferAsync_Timeout_Returns()
    {
        // Setup: Add expensive rules
        // ...

        // Execute with short timeout
        var result = await _engine.InferAsync(
            new InferenceOptions { Timeout = TimeSpan.FromMilliseconds(1) });

        // Verify: Returns with timeout status
        Assert.AreEqual(InferenceStatus.Timeout, result.Status);
    }
}
```

### 8.2 Integration Tests

```csharp
[TestClass]
public class InferenceIntegrationTests
{
    [TestMethod]
    public async Task FullInference_WithBuiltInRules_Succeeds()
    {
        // Setup complex scenario with multiple rules
        // Execute inference
        // Verify all expected facts derived
    }

    [TestMethod]
    public async Task Inference_DryRun_DoesNotPersist()
    {
        var result = await _engine.InferAsync(
            new InferenceOptions { DryRun = true });

        // Verify facts derived but not persisted
        Assert.IsTrue(result.FactsDerived > 0);
        // ... verify they're not in graph ...
    }
}
```

---

## 9. Error Handling

### 9.1 Graceful Degradation

- Compilation errors: Skip invalid rules, continue with others
- Execution errors: Log error, mark fact derivation as failed
- Timeouts: Return partial results
- Iteration limits: Return PartialSuccess status

### 9.2 Logging

```csharp
_logger.LogInformation("Inference started with {RuleCount} rules", compiled.Count);
_logger.LogInformation("Derived {FactCount} facts in {Duration}ms",
    derivedFacts.Count, sw.ElapsedMilliseconds);
_logger.LogWarning("Inference exceeded max iterations: {MaxIterations}",
    options.MaxIterations);
```

---

## 10. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Infinite loops | Critical | Iteration limit, timeout |
| Memory explosion | High | Fact deduplication, limits |
| Rule errors | Medium | Try-catch, error logging |
| Unauthorized execution | High | Feature gate, license tier check |

---

## 11. Performance Considerations

| Operation | Target | Implementation |
| :--- | :--- | :--- |
| Load working memory | <1s | Batch load from graph |
| Match conditions | <10ms per rule | Indexed lookups |
| Execute conclusions | <5ms per match | Direct fact creation |
| Full inference | <30s | Iterative application |

---

## 12. License Gating

| Tier | Support |
| :--- | :--- |
| **Core** | Not available |
| **WriterPro** | Built-in rules only, max 50 iterations |
| **Teams** | Custom rules (50), max 1000 iterations |
| **Enterprise** | Unlimited rules and iterations |

---

## 13. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Knowledge graph + rules | InferAsync called | Derived facts returned |
| 2 | Transitive rule | Executed | Chained derivations created |
| 3 | MaxIterations exceeded | Executing | PartialSuccess status, facts so far |
| 4 | Timeout reached | Executing | Timeout status, partial results |
| 5 | DryRun enabled | Inference runs | No facts persisted, but counted |
| 6 | Feature gate disabled | InferAsync called | RuleError status |
| 7 | Rule compilation error | Inference runs | Warning logged, rule skipped |
| 8 | Built-in rules | Loaded | All 5 rules compiled and executed |

---

## 14. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial specification - forward chaining algorithm, working memory, agenda |
