# LCS-DES-v0.10.2-KG-e: Design Specification — Provenance Tracker

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `KG-102-e` | Inference Engine sub-part e |
| **Feature Name** | `Provenance Tracker` | Track derivation chains for explanations |
| **Target Version** | `v0.10.2e` | Fifth sub-part of v0.10.2-KG |
| **Module Scope** | `Lexichord.Modules.CKVS.Inference` | Inference module |
| **Swimlane** | `Knowledge Graph` | Knowledge Graph vertical |
| **License Tier** | `Teams` / `Enterprise` | Custom rules require Teams+ |
| **Feature Gate Key** | `FeatureFlags.CKVS.InferenceEngine` | Inference engine feature flag |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-31` | |
| **Parent Document** | [LCS-SBD-v0.10.2-KG](./LCS-SBD-v0.10.2-KG.md) | Inference Engine scope |
| **Scope Breakdown** | [LCS-SBD-v0.10.2-KG S2.1](./LCS-SBD-v0.10.2-KG.md#21-sub-parts) | e = Provenance Tracker |

---

## 2. Executive Summary

### 2.1 The Requirement

Users must understand **why** a fact was derived. Provenance tracking must:

1. **Record** which rule derived each fact
2. **Track** which asserted facts were premises
3. **Chain** derivations recursively (fact A derived from B which was derived from C)
4. **Explain** in natural language how conclusions were reached
5. **Store** provenance persistently for audit trails
6. **Generate** complete derivation chains on demand

### 2.2 The Proposed Solution

Implement provenance tracking during forward chaining:

1. **Provenance Record:** Associate each derived fact with rule ID + premises
2. **Derivation Chain:** Recursively resolve explanations for premise facts
3. **Natural Language:** Convert structured derivations to readable text
4. **Persistence:** Store explanations in graph alongside derived facts
5. **Query API:** Retrieve explanations by fact ID

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Component | Source Version | Purpose |
| :--- | :--- | :--- |
| `IInferenceEngine` | v0.10.2c | Provide context during fact derivation |
| `IGraphRepository` | v0.4.5e | Store and retrieve explanations |
| `InferenceRule` | v0.10.2a | Rule metadata for explanations |
| `DerivedFact` | v0.10.2c | Derived fact structure |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| (None required) | | Uses only standard C# |

### 3.2 Licensing Behavior

- **WriterPro:** Explanations for built-in rules only
- **Teams:** Explanations for all rules (built-in + custom)
- **Enterprise:** Explanations + provenance API access

---

## 4. Data Contract (The API)

### 4.1 Provenance Record

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Records the provenance of a derived fact — which rule derived it and from what premises.
/// </summary>
public record ProvenanceRecord
{
    /// <summary>
    /// Unique identifier for this provenance entry.
    /// </summary>
    public required Guid ProvenanceId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The derived fact this provenance explains.
    /// </summary>
    public required Guid DerivedFactId { get; init; }

    /// <summary>
    /// The inference rule that produced this fact.
    /// </summary>
    public required Guid RuleId { get; init; }

    /// <summary>
    /// Human-readable rule name for explanations.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// The premises (asserted or derived facts) that matched the rule condition.
    /// </summary>
    public IReadOnlyList<ProvenancePremise> Premises { get; init; } = [];

    /// <summary>
    /// Timestamp when the fact was derived.
    /// </summary>
    public required DateTimeOffset DerivedAt { get; init; }

    /// <summary>
    /// How many derivation steps deep this is (0 = asserted, 1 = direct derivation, etc).
    /// </summary>
    public int DerivationDepth { get; init; }

    /// <summary>
    /// Confidence score for this derivation (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Whether this provenance is still valid (may be invalid if premises changed).
    /// </summary>
    public bool IsValid { get; init; } = true;
}

/// <summary>
/// A single premise in a rule that led to a derived fact.
/// </summary>
public record ProvenancePremise
{
    /// <summary>
    /// The premise fact ID (asserted or derived).
    /// </summary>
    public required Guid FactId { get; init; }

    /// <summary>
    /// Human-readable description of the premise.
    /// Example: "UserService -[CALLS]-> GET /auth/validate"
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Whether this premise is itself a derived fact.
    /// </summary>
    public bool IsDerived { get; init; }

    /// <summary>
    /// If IsDerived, the provenance of this premise (recursive).
    /// </summary>
    public ProvenanceRecord? NestedProvenance { get; init; }
}
```

### 4.2 Derivation Explanation (from SBD)

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Explanation of how a fact was derived.
/// </summary>
public record DerivationExplanation
{
    /// <summary>
    /// The derived fact being explained.
    /// </summary>
    public required Guid FactId { get; init; }

    /// <summary>
    /// The rule that derived this fact.
    /// </summary>
    public required Guid RuleId { get; init; }

    /// <summary>
    /// Human-readable rule name.
    /// </summary>
    public required string RuleName { get; init; }

    /// <summary>
    /// The premises that matched the rule condition.
    /// </summary>
    public IReadOnlyList<DerivationPremise> Premises { get; init; } = [];

    /// <summary>
    /// Natural language explanation of how the fact was derived.
    /// Example: "UserService depends on AuthService because UserService calls
    ///          the /auth/validate endpoint, which is defined in AuthService."
    /// </summary>
    public required string NaturalLanguageExplanation { get; init; }

    /// <summary>
    /// How many derivation steps deep (0 = root premises, 1+ = intermediate derivations).
    /// </summary>
    public int DerivationDepth { get; init; }
}

/// <summary>
/// A premise in a derivation explanation.
/// </summary>
public record DerivationPremise
{
    /// <summary>
    /// The fact ID that matched the rule condition.
    /// </summary>
    public required Guid FactId { get; init; }

    /// <summary>
    /// Whether this premise is derived (true) or asserted (false).
    /// </summary>
    public bool IsDerived { get; init; }

    /// <summary>
    /// Human-readable description of the fact.
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// Nested explanation if this premise is derived.
    /// </summary>
    public DerivationExplanation? NestedExplanation { get; init; }
}
```

### 4.3 Provenance Tracker Interface

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Tracks provenance (derivation history) of inferred facts.
/// </summary>
public interface IProvenanceTracker
{
    /// <summary>
    /// Records the derivation of a fact.
    /// Called during forward chaining whenever a new fact is derived.
    /// </summary>
    Task<ProvenanceRecord> RecordDerivationAsync(
        Guid derivedFactId,
        InferenceRule rule,
        IReadOnlyList<Guid> premiseFactIds,
        int derivationDepth,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves complete derivation explanation for a fact.
    /// </summary>
    Task<DerivationExplanation?> GetExplanationAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves raw provenance record without explanation generation.
    /// </summary>
    Task<ProvenanceRecord?> GetProvenanceAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all derivations that depend on a particular fact.
    /// </summary>
    Task<IReadOnlyList<ProvenanceRecord>> GetDependentDerivationsAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Invalidates provenance when source facts change.
    /// </summary>
    Task InvalidateDerivationsAsync(
        IReadOnlyList<Guid> changedFactIds,
        CancellationToken ct = default);

    /// <summary>
    /// Clears all provenance (used when inference is reset).
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);
}

/// <summary>
/// Generates human-readable explanations from provenance records.
/// </summary>
public interface IExplanationGenerator
{
    /// <summary>
    /// Generates natural language explanation for a derivation.
    /// </summary>
    Task<string> GenerateExplanationAsync(
        ProvenanceRecord provenance,
        CancellationToken ct = default);

    /// <summary>
    /// Generates multi-line explanation with full derivation chain.
    /// </summary>
    Task<string> GenerateDetailedExplanationAsync(
        DerivationExplanation explanation,
        CancellationToken ct = default);

    /// <summary>
    /// Generates short one-liner explanation.
    /// </summary>
    Task<string> GenerateSummaryAsync(
        ProvenanceRecord provenance,
        CancellationToken ct = default);
}
```

### 4.4 Provenance Storage

```csharp
namespace Lexichord.Abstractions.Contracts.CKVS.Inference;

/// <summary>
/// Persists and retrieves provenance records.
/// </summary>
public interface IProvenanceRepository
{
    /// <summary>
    /// Stores provenance for a derived fact.
    /// </summary>
    Task<ProvenanceRecord> SaveProvenanceAsync(
        ProvenanceRecord provenance,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves provenance by fact ID.
    /// </summary>
    Task<ProvenanceRecord?> GetByFactIdAsync(
        Guid factId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all provenances created within a time range.
    /// </summary>
    Task<IReadOnlyList<ProvenanceRecord>> GetByDateRangeAsync(
        DateTimeOffset startTime,
        DateTimeOffset endTime,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all provenances for a specific rule.
    /// </summary>
    Task<IReadOnlyList<ProvenanceRecord>> GetByRuleAsync(
        Guid ruleId,
        CancellationToken ct = default);

    /// <summary>
    /// Updates provenance validity when facts change.
    /// </summary>
    Task UpdateValidityAsync(
        IReadOnlyList<Guid> provenanceIds,
        bool isValid,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes provenance when derived fact is retracted.
    /// </summary>
    Task DeleteAsync(
        Guid provenanceId,
        CancellationToken ct = default);
}
```

---

## 5. Implementation Details

### 5.1 Provenance Recording Flow

```
Forward Chainer derives fact X from Rule R with premises [P1, P2]
    ↓
ProvenanceTracker.RecordDerivationAsync(X, R, [P1, P2])
    ↓
1. Create ProvenanceRecord with RuleId=R.Id, Premises=[P1, P2]
2. For each premise:
   - If derived: recursively fetch its ProvenanceRecord
   - If asserted: mark IsDerived=false
3. Store provenance in ProvenanceRepository
4. Return ProvenanceRecord
    ↓
IInferenceEngine stores ProvenanceId alongside DerivedFact
```

### 5.2 Explanation Generation Flow

```
User requests explanation for fact X
    ↓
IExplanationGenerator.GenerateExplanationAsync(provenance of X)
    ↓
1. Load provenance record for X
2. For each premise P:
   - If P.IsDerived: recursively get its explanation
   - If P.IsAsserted: format as "Entity description"
3. Build premise list with nested explanations
4. Generate natural language:
   "X was derived by rule R because:
    - Premise P1 (asserted fact)
    - Premise P2 was derived from premises Q1, Q2"
5. Return DerivationExplanation
```

### 5.3 Invalidation on Graph Change

```
Graph fact F changes
    ↓
IProvenanceTracker.InvalidateDerivationsAsync([F])
    ↓
1. Find all derivations depending on F
   (using IProvenanceRepository.GetDependentDerivationsAsync)
2. Mark those derivations as IsValid=false
3. Recursively mark downstream derivations as invalid
4. Return list of invalidated provenances
    ↓
IInferenceEngine marks corresponding DerivedFacts for retraction
```

### 5.4 Natural Language Template Examples

```csharp
// Rule: Transitivity (A is parent of B, B is parent of C → A is grandparent of C)
Provenance:
  - RuleName: "Grandparent Inference"
  - Premise 1: "Alice is parent of Bob" (asserted)
  - Premise 2: "Bob is parent of Charlie" (asserted)

Output: "Alice is grandparent of Charlie because Alice is parent of Bob,
         and Bob is parent of Charlie."

---

// Rule: Service Dependency (Service1 calls Endpoint, Endpoint defined in Service2)
Provenance:
  - RuleName: "Service Dependency Detection"
  - Premise 1: "UserService calls GET /auth/validate" (asserted)
  - Premise 2: "GET /auth/validate defined in AuthService" (asserted)

Output: "UserService depends on AuthService because UserService calls the
         /auth/validate endpoint, which is defined in AuthService."

---

// Rule: Transitivity with derived premise
Provenance:
  - RuleName: "Transitive Auth Requirement"
  - Premise 1: "Endpoint A requires auth" (asserted)
  - Premise 2: "Service B calls Endpoint A" (asserted)
  - Derived: "Service B requires auth" (from above two)
  - Premise 3: "Service C calls Service B" (asserted)

Output: "Service C requires auth because Service C calls Service B, which requires auth.
         Service B requires auth because it calls Endpoint A, which requires auth."
```

---

## 6. Error Handling

### 6.1 Missing Premise Fact

**Scenario:** Provenance references a fact ID that no longer exists.

**Handling:**
- Load provenance record
- For missing premise facts, return null for NestedProvenance
- Mark IsValid=false in explanation
- Log warning

**Code:**
```csharp
var premise = await _factRepository.GetAsync(premiseFactId);
if (premise == null)
{
    _logger.LogWarning("Premise fact {PremiseId} not found for provenance {ProvenanceId}",
        premiseFactId, provenanceId);
    premise = new DerivationPremise
    {
        FactId = premiseFactId,
        Description = "[Deleted fact]",
        IsDerived = false,
        NestedExplanation = null
    };
}
```

### 6.2 Circular Provenance (Cycle in Derivations)

**Scenario:** Fact A derived from B, B derived from A (indicates bug in forward chainer).

**Handling:**
- Detect cycle during explanation generation
- Stop recursion at cycle point
- Return explanation with IsValid=false
- Log error

**Code:**
```csharp
private async Task<DerivationExplanation> GenerateExplanationAsync(
    Guid factId,
    HashSet<Guid> visited,
    CancellationToken ct)
{
    if (visited.Contains(factId))
    {
        _logger.LogError("Circular provenance detected at fact {FactId}", factId);
        throw new InvalidOperationException($"Circular derivation chain at {factId}");
    }

    visited.Add(factId);
    // ... generate explanation ...
    visited.Remove(factId);
}
```

### 6.3 Invalidation Cascade Too Deep

**Scenario:** Single fact change invalidates thousands of downstream derivations.

**Handling:**
- Batch invalidation operations
- Limit recursion depth to MaxInvalidationDepth (default 100)
- Log warnings if cascade exceeds threshold

**Code:**
```csharp
public async Task InvalidateDerivationsAsync(
    IReadOnlyList<Guid> changedFactIds,
    CancellationToken ct = default)
{
    const int MaxInvalidationDepth = 100;
    var toInvalidate = new Queue<Guid>(changedFactIds);
    var invalidatedCount = 0;
    var currentDepth = 0;

    while (toInvalidate.Count > 0 && currentDepth < MaxInvalidationDepth)
    {
        var batch = toInvalidate.Dequeue();
        var dependents = await _provenanceRepository.GetDependentDerivationsAsync(batch, ct);
        invalidatedCount += dependents.Count;

        foreach (var provenance in dependents)
        {
            toInvalidate.Enqueue(provenance.DerivedFactId);
        }
        currentDepth++;
    }

    if (currentDepth >= MaxInvalidationDepth)
    {
        _logger.LogWarning("Invalidation cascade exceeded {MaxDepth} levels", MaxInvalidationDepth);
    }
}
```

---

## 7. Testing

### 7.1 Unit Tests

```csharp
[TestClass]
public class ProvenanceTrackerTests
{
    private IProvenanceTracker _tracker;
    private IProvenanceRepository _repository;

    [TestInitialize]
    public void Setup()
    {
        _repository = new InMemoryProvenanceRepository();
        _tracker = new ProvenanceTracker(_repository);
    }

    [TestMethod]
    public async Task RecordDerivation_WithSinglePremise_RecordsCorrectly()
    {
        var derivedFactId = Guid.NewGuid();
        var premiseId = Guid.NewGuid();
        var rule = new InferenceRule
        {
            RuleId = Guid.NewGuid(),
            Name = "Test Rule"
        };

        var provenance = await _tracker.RecordDerivationAsync(
            derivedFactId, rule, new[] { premiseId }, 1);

        Assert.AreEqual(derivedFactId, provenance.DerivedFactId);
        Assert.AreEqual(rule.RuleId, provenance.RuleId);
        Assert.AreEqual(1, provenance.Premises.Count);
    }

    [TestMethod]
    public async Task GetExplanation_WithValidProvenance_GeneratesExplanation()
    {
        var derivedFactId = Guid.NewGuid();
        var premiseId = Guid.NewGuid();
        var rule = new InferenceRule
        {
            RuleId = Guid.NewGuid(),
            Name = "Grandparent Inference"
        };

        await _tracker.RecordDerivationAsync(derivedFactId, rule, new[] { premiseId }, 1);
        var explanation = await _tracker.GetExplanationAsync(derivedFactId);

        Assert.IsNotNull(explanation);
        Assert.AreEqual(rule.RuleId, explanation.RuleId);
        Assert.IsTrue(explanation.NaturalLanguageExplanation.Length > 0);
    }

    [TestMethod]
    public async Task InvalidateDerivations_WithChangedFact_MarksAsInvalid()
    {
        var factId = Guid.NewGuid();
        var derivedId = Guid.NewGuid();
        var rule = new InferenceRule { RuleId = Guid.NewGuid(), Name = "Test" };

        var provenance = await _tracker.RecordDerivationAsync(derivedId, rule, new[] { factId }, 1);
        Assert.IsTrue(provenance.IsValid);

        await _tracker.InvalidateDerivationsAsync(new[] { factId });
        var invalidated = await _tracker.GetProvenanceAsync(derivedId);

        Assert.IsFalse(invalidated.IsValid);
    }

    [TestMethod]
    public async Task GetDependentDerivations_WithChainedDerivations_ReturnsAll()
    {
        // Fact1 → Fact2 (derived) → Fact3 (derived)
        var fact1 = Guid.NewGuid();
        var fact2 = Guid.NewGuid();
        var fact3 = Guid.NewGuid();

        var rule = new InferenceRule { RuleId = Guid.NewGuid(), Name = "Test" };

        await _tracker.RecordDerivationAsync(fact2, rule, new[] { fact1 }, 1);
        await _tracker.RecordDerivationAsync(fact3, rule, new[] { fact2 }, 2);

        var dependents = await _tracker.GetDependentDerivationsAsync(fact1);

        Assert.AreEqual(1, dependents.Count);
        Assert.AreEqual(fact2, dependents[0].DerivedFactId);
    }
}
```

### 7.2 Integration Tests

```csharp
[TestClass]
public class ProvenanceIntegrationTests
{
    [TestMethod]
    public async Task CompleteWorkflow_DeriveFact_GenerateExplanation()
    {
        // Setup inference engine and provenance tracker
        var inferenceEngine = new ForwardChainer(/* dependencies */);
        var tracker = new ProvenanceTracker(/* repository */);

        // Record a derivation
        var rule = new InferenceRule
        {
            RuleId = Guid.NewGuid(),
            Name = "Grandparent Inference"
        };
        var derivedFactId = Guid.NewGuid();
        var premise1 = Guid.NewGuid();
        var premise2 = Guid.NewGuid();

        await tracker.RecordDerivationAsync(derivedFactId, rule, new[] { premise1, premise2 }, 1);

        // Request explanation
        var explanation = await tracker.GetExplanationAsync(derivedFactId);

        // Verify explanation structure
        Assert.IsNotNull(explanation);
        Assert.AreEqual(2, explanation.Premises.Count);
        Assert.IsTrue(explanation.NaturalLanguageExplanation.Contains("because"));
    }

    [TestMethod]
    public async Task InvalidationCascade_ChangeSourceFact_InvalidatesDownstream()
    {
        var tracker = new ProvenanceTracker(/* repository */);

        // Create chain: Fact1 → Fact2 (derived) → Fact3 (derived)
        var fact1 = Guid.NewGuid();
        var fact2 = Guid.NewGuid();
        var fact3 = Guid.NewGuid();
        var rule = new InferenceRule { RuleId = Guid.NewGuid(), Name = "Test" };

        await tracker.RecordDerivationAsync(fact2, rule, new[] { fact1 }, 1);
        await tracker.RecordDerivationAsync(fact3, rule, new[] { fact2 }, 2);

        // Change fact1
        await tracker.InvalidateDerivationsAsync(new[] { fact1 });

        // Verify fact2 and fact3 are invalidated
        var prov2 = await tracker.GetProvenanceAsync(fact2);
        var prov3 = await tracker.GetProvenanceAsync(fact3);

        Assert.IsFalse(prov2.IsValid);
        Assert.IsFalse(prov3.IsValid);
    }
}
```

---

## 8. Performance Considerations

### 8.1 Provenance Recording

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| Record derivation | <10ms | Batch inserts, async I/O |
| Store provenance | <20ms | Indexed repository, fast path |
| Invalidate derivations | <100ms (for 100 dependents) | Breadth-first search, batching |

### 8.2 Explanation Generation

| Operation | Target | Strategy |
| :--- | :--- | :--- |
| Generate explanation | <500ms | Lazy load premises, memoization |
| Recursive explanation chain | <1s (for depth 10) | Stop recursion at depth limit |
| Batch explanations | <5s (for 100 facts) | Parallel fetching with limits |

### 8.3 Storage Optimization

- **Index on DerivedFactId:** O(1) provenance lookups
- **Index on RuleId:** O(log n) rule-based queries
- **Index on DerivedAt:** O(log n) time-range queries
- **Batch invalidation:** Update multiple records in single DB operation

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Provenance tampering | Medium | Append-only storage, audit logging |
| Explanation disclosure | Medium | License tier checks, permission validation |
| Circular references | Low | Cycle detection during generation |
| Memory explosion | Low | Depth limits, result set limits |

---

## 10. License Gating

| Tier | Support |
| :--- | :--- |
| Core | Not available |
| WriterPro | Explanations for built-in rules only |
| Teams | Explanations for all rules |
| Enterprise | Provenance API + advanced analytics |

**Feature Gate:** `FeatureFlags.CKVS.InferenceEngine`

---

## 11. Acceptance Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Derived fact | Inference completes | Provenance recorded with rule ID |
| 2 | Provenance record | Explanation requested | Natural language explanation generated |
| 3 | Explanation | Generated | Premises and rule name included |
| 4 | Source fact | Changed | Dependent derivations marked invalid |
| 5 | Circular provenance | Detected | Error raised, cycle prevented |
| 6 | Explanation depth | Reaches limit | Recursion stops gracefully |
| 7 | Batch invalidation | 1000 dependent facts | Completes in <2s |
| 8 | Storage | Provenance persisted | Retrieved correctly via API |

---

## 12. Changelog

| Version | Date | Changes |
| :--- | :--- | :--- |
| 1.0 | 2026-01-31 | Initial design - Provenance tracking, explanation generation, invalidation |
