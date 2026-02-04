# v0.5.9b Changelog: Relationship Classification

**Version:** v0.5.9b  
**Released:** 2026-02-04  
**Module:** Lexichord.Modules.Rag  
**License Tier:** Writer Pro

---

## Overview

Implements the `IRelationshipClassifier` interface for classifying semantic relationships between similar chunks identified by v0.5.9a's similarity detection. Uses a hybrid approach with rule-based fast-path classification for high-confidence matches and caching to minimize redundant processing.

## Components Added

### Data Contracts (Abstractions)

| File | Description |
|------|-------------|
| `RelationshipType.cs` | Enum: Unknown, Equivalent, Complementary, Contradictory, Superseding, Subset, Distinct |
| `ClassificationMethod.cs` | Enum: RuleBased, LlmBased, Cached |
| `ChunkPair.cs` | Record bundling two chunks with similarity score |
| `RelationshipClassification.cs` | Result record with type, confidence, explanation, method |
| `ClassificationOptions.cs` | Configuration: thresholds, caching, LLM enablement |
| `IRelationshipClassifier.cs` | Interface with `ClassifyAsync` and `ClassifyBatchAsync` |

### Service Implementation

| File | Description |
|------|-------------|
| `RelationshipClassifier.cs` | Hybrid classifier with rule-based fast-path and caching |

### Feature Code

| Constant | Description |
|----------|-------------|
| `FeatureCodes.SemanticDeduplication` | License gate for v0.5.9 deduplication features |

## Configuration Defaults

```csharp
ClassificationOptions.Default = new()
{
    RuleBasedThreshold = 0.95f,
    EnableLlmClassification = true,
    EnableCaching = true,
    CacheDuration = TimeSpan.FromHours(1),
    IncludeExplanation = true,
    MinimumSimilarityThreshold = 0.80f
};
```

## Rule-Based Classification Logic

| Condition | Result | Confidence |
|-----------|--------|------------|
| Similarity >= 0.999 | Equivalent | 1.0 |
| Similarity >= 0.95, same doc, adjacent | Complementary | 0.85 |
| Similarity >= 0.95, length ratio < 0.5 | Subset | 0.80 |
| Similarity >= 0.95, default | Equivalent | 0.95 |
| Similarity < 0.80 | Distinct | 0.95 |
| Similarity 0.80-0.95 | Complementary (fallback) | 0.60-0.70 |

## Dependencies

- **Upstream:** v0.5.9a (ISimilarityDetector)
- **NuGet:** Microsoft.Extensions.Caching.Memory (existing)
- **Optional:** IChatCompletionService (LLM integration stubbed for future)

## Files Changed

### Added
- `src/Lexichord.Abstractions/Contracts/RAG/RelationshipType.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ClassificationMethod.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ChunkPair.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/RelationshipClassification.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/ClassificationOptions.cs`
- `src/Lexichord.Abstractions/Contracts/RAG/IRelationshipClassifier.cs`
- `src/Lexichord.Modules.RAG/Services/RelationshipClassifier.cs`
- `tests/Lexichord.Tests.Unit/Modules/RAG/Services/RelationshipClassifierTests.cs`

### Modified
- `src/Lexichord.Abstractions/Constants/FeatureCodes.cs` — Added SemanticDeduplication
- `src/Lexichord.Modules.RAG/RAGModule.cs` — Registered v0.5.9b services

## Test Coverage

- **20 unit tests** covering:
  - Constructor null argument validation
  - Rule-based classification (perfect/high/low similarity)
  - Same-document adjacent chunks → Complementary
  - Significant length difference → Subset
  - Cache hit/miss behavior
  - License gating (returns Unknown without feature)
  - Batch processing
  - Cancellation token respect
  - Options configuration

## Verification Steps

```bash
# Build RAG module
dotnet build src/Lexichord.Modules.RAG/Lexichord.Modules.RAG.csproj

# Run classification tests
dotnet test --filter "FullyQualifiedName~RelationshipClassifier"

# Full test suite
dotnet test
```

## Next Steps

- **v0.5.9c:** Deduplication Policies — Define merge, link, and flag actions
- **LLM Integration:** Wire up IChatCompletionService for ambiguous classification
