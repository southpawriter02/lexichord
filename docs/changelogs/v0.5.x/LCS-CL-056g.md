# LCS-CL-056g: Claim Extractor Changelog

## Overview

| Field | Value |
| :--- | :--- |
| **Changelog ID** | LCS-CL-056g |
| **Feature ID** | KG-056g |
| **Feature Name** | Claim Extractor |
| **Version** | v0.5.6g |
| **Implementation Date** | 2026-02-03 |
| **Status** | Complete |

---

## Summary

Implemented the Claim Extractor service for the Knowledge Graph pipeline. This component transforms parsed sentences into structured claims (subject-predicate-object triples) using pattern-based and dependency-based extraction, with post-processing for deduplication and confidence scoring.

---

## New Files

### Lexichord.Abstractions

#### Contracts/Knowledge/ClaimExtraction/

| File | Description |
| :--- | :--- |
| `IClaimExtractionService.cs` | Main service interface for claim extraction |
| `IClaimExtractor.cs` | Interface for extraction strategies |
| `ClaimExtractionContext.cs` | Context record with extraction configuration |
| `ExtractedClaim.cs` | Intermediate claim representation |
| `ExtractionPattern.cs` | Pattern definition for extraction rules |
| `PatternType.cs` | Enum for pattern types (Regex, Template, Dependency) |
| `DependencyPattern.cs` | Dependency-based pattern definition |
| `TextSpan.cs` | Text region with character offsets |

### Lexichord.Modules.Knowledge

#### Extraction/ClaimExtraction/

| File | Description |
| :--- | :--- |
| `ClaimExtractionService.cs` | Main service orchestrating extraction |
| `PatternClaimExtractor.cs` | Regex/template-based extraction |
| `DependencyClaimExtractor.cs` | Dependency parse-based extraction |
| `ClaimDeduplicator.cs` | Removes semantically equivalent claims |
| `ConfidenceScorer.cs` | Adjusts claim confidence scores |
| `PatternLoader.cs` | Loads patterns from YAML configuration |
| `BuiltInPatterns.yaml` | Default extraction patterns |

### Lexichord.Tests.Unit

#### Modules/Knowledge/

| File | Description |
| :--- | :--- |
| `ClaimExtractionServiceTests.cs` | 22 unit tests for claim extraction |

---

## API Additions

### IClaimExtractionService Interface

```csharp
public interface IClaimExtractionService
{
    Task<ClaimExtractionResult> ExtractClaimsAsync(
        string text,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default);

    Task<IReadOnlyList<Claim>> ExtractFromSentenceAsync(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default);

    Task ReloadPatternsAsync(CancellationToken ct = default);
    ClaimExtractionStats GetStats();
}
```

### ClaimExtractionContext Record

```csharp
public record ClaimExtractionContext
{
    public Guid DocumentId { get; init; }
    public Guid ProjectId { get; init; }
    public float MinConfidence { get; init; } = 0.5f;
    public bool UsePatterns { get; init; } = true;
    public bool UseDependencyExtraction { get; init; } = true;
    public bool DeduplicateClaims { get; init; } = true;
    public IReadOnlyList<string>? PredicateFilter { get; init; }
    public int MaxClaimsPerSentence { get; init; } = 5;
    public IReadOnlyList<ExtractionPattern>? CustomPatterns { get; init; }
    public string Language { get; init; } = "en";
}
```

### IClaimExtractor Interface

```csharp
public interface IClaimExtractor
{
    string Name { get; }
    IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context);
}
```

---

## Features

### Pattern-Based Extraction

- Template patterns with {SUBJECT} and {OBJECT} placeholders
- Regex patterns with named capture groups
- Built-in patterns for common API documentation structures
- YAML configuration for custom patterns
- Priority-based pattern ordering

### Dependency-Based Extraction

- Extracts claims from dependency parse trees
- Maps verb lemmas to ClaimPredicate constants
- Identifies subjects via nsubj relations
- Identifies objects via dobj relations
- Fallback to HAS_PROPERTY for unknown verbs

### Confidence Scoring

- Base confidence from pattern definitions
- Bonuses for resolved entities (+0.05 each)
- Penalty for dependency extraction (-0.05)
- Penalty for long sentences (>20 tokens: -0.05, >30 tokens: -0.1)
- Final score clamped to [0, 1]

### Deduplication

- Groups claims by normalized subject-predicate-object key
- Keeps highest-confidence version of each unique claim
- Case-insensitive surface form comparison
- Removes articles (the, a, an) before comparison

### Built-in Patterns

| Pattern | Predicate | Example |
| :--- | :--- | :--- |
| Endpoint Accepts | ACCEPTS | "The endpoint accepts a parameter" |
| Endpoint Returns | RETURNS | "The endpoint returns a response" |
| Parameter Defaults | HAS_DEFAULT | "The parameter defaults to 30" |
| Entity Requires | REQUIRES | "The API requires authentication" |
| Entity Contains | CONTAINS | "The response contains data" |
| Deprecated Endpoint | IS_DEPRECATED | "GET /api/v1 is deprecated" |

---

## Test Summary

| Category | Count | Status |
| :--- | :--- | :--- |
| Pattern Extraction Tests | 4 | ✅ Pass |
| Dependency Extraction Tests | 1 | ✅ Pass |
| Deduplication Tests | 1 | ✅ Pass |
| Filtering Tests | 2 | ✅ Pass |
| Empty Input Tests | 2 | ✅ Pass |
| Sentence Extraction Tests | 2 | ✅ Pass |
| Statistics Tests | 4 | ✅ Pass |
| Claim Structure Tests | 5 | ✅ Pass |
| Cancellation Tests | 1 | ✅ Pass |
| **Total** | **22** | ✅ **All Pass** |

---

## Dependencies

### Upstream

| Module | Interface | Version |
| :--- | :--- | :--- |
| `ISentenceParser` | v0.5.6f | Sentence Parser |
| `Claim` | v0.5.6e | Claim Data Model |
| `ClaimEntity` | v0.5.6e | Claim subject/object entity |
| `ClaimObject` | v0.5.6e | Claim object (entity/literal) |
| `ClaimPredicate` | v0.5.6e | Standard predicates |
| `ClaimExtractionResult` | v0.5.6e | Result container |
| `ClaimExtractionStats` | v0.5.6e | Extraction statistics |
| `ILogger<T>` | v0.0.3b | Logging |

### NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `YamlDotNet` | 16.1.3 | Pattern YAML parsing |

---

## License Gating

| Tier | Features |
| :--- | :--- |
| WriterPro | Basic pattern extraction |
| Teams | Full functionality (dependency extraction) |
| Enterprise | Full functionality + custom patterns |

**Feature Gate Key:** `knowledge.claims.extraction.enabled`

---

## Performance

| Metric | Target | Achieved |
| :--- | :--- | :--- |
| Extract from 100 sentences | < 2 seconds | ✅ < 1 second |
| Memory per 1000 claims | < 20MB | ✅ |
| Pattern match overhead | < 10ms/sentence | ✅ |

---

## Breaking Changes

None. This is a new feature addition.

---

## Migration Notes

No migration required. New APIs only.

---

## Known Limitations

1. **Entity Linking Placeholder**: Claims use `ClaimEntity.Unresolved` for all entities. Full entity linking will be implemented in a future version.

2. **Pattern Coverage**: Built-in patterns cover common API documentation structures. Additional patterns can be added via YAML configuration.

3. **Dependency Extraction Accuracy**: Rule-based dependency extraction may miss complex grammatical constructions. Confidence scores are lower for dependency-based claims.

---

## Next Steps

- v0.5.6h: Entity Linking integration (connect claims to Knowledge Graph entities)
- Future: Machine learning-based relation extraction
