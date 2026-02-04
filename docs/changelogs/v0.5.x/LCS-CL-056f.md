# LCS-CL-056f: Sentence Parser Changelog

## Overview

| Field | Value |
| :--- | :--- |
| **Changelog ID** | LCS-CL-056f |
| **Feature ID** | KG-056f |
| **Feature Name** | Sentence Parser |
| **Version** | v0.5.6f |
| **Implementation Date** | 2026-02-03 |
| **Status** | Complete |

---

## Summary

Implemented the Sentence Parser feature for the Claim Extraction pipeline. This component parses text into structured linguistic representations including sentence segmentation, tokenization, dependency parsing, and semantic role labeling.

---

## New Files

### Lexichord.Abstractions

#### Contracts/Knowledge/Parsing/

| File | Description |
| :--- | :--- |
| `ISentenceParser.cs` | Interface for sentence parsing services |
| `ParseOptions.cs` | Configuration options for parsing behavior |
| `ParseResult.cs` | Result container with sentences and statistics |
| `ParsedSentence.cs` | Sentence with linguistic annotations |
| `Token.cs` | Token (word) with POS, lemma, offsets |
| `DependencyNode.cs` | Node in dependency tree |
| `DependencyRelation.cs` | Dependency relation between tokens |
| `DependencyRelations.cs` | Constants for dependency relation types |
| `SemanticFrame.cs` | Predicate-argument structure |
| `SemanticArgument.cs` | Argument in semantic frame |
| `SemanticRole.cs` | Enum of PropBank-style semantic roles |
| `NamedEntity.cs` | Named entity recognition result |

### Lexichord.Modules.Knowledge

#### Extraction/Parsing/

| File | Description |
| :--- | :--- |
| `SpacySentenceParser.cs` | SpaCy-based parser implementation |

### Lexichord.Tests.Unit

#### Modules/Knowledge/

| File | Description |
| :--- | :--- |
| `SentenceParserTests.cs` | 35 unit tests for sentence parser |

---

## API Additions

### ISentenceParser Interface

```csharp
public interface ISentenceParser
{
    Task<ParseResult> ParseAsync(
        string text,
        ParseOptions? options = null,
        CancellationToken ct = default);

    Task<ParsedSentence> ParseSentenceAsync(
        string sentence,
        ParseOptions? options = null,
        CancellationToken ct = default);

    IReadOnlyList<string> SupportedLanguages { get; }
}
```

### ParseOptions Record

```csharp
public record ParseOptions
{
    public string Language { get; init; } = "en";
    public bool IncludeDependencies { get; init; } = true;
    public bool IncludeSRL { get; init; } = true;
    public bool IncludePOS { get; init; } = true;
    public bool IncludeEntities { get; init; } = true;
    public int MaxSentenceLength { get; init; } = 500;
    public bool UseCache { get; init; } = true;
}
```

### ParsedSentence Helper Methods

```csharp
public record ParsedSentence
{
    // Properties: Text, Tokens, Dependencies, SemanticFrames, Entities...

    public Token? GetRootVerb();
    public Token? GetSubject();
    public Token? GetDirectObject();
    public IReadOnlyList<Token> GetVerbs();
    public IReadOnlyList<Token> GetNouns();
    public IReadOnlyList<Token> GetContentWords();
}
```

### SemanticRole Enum

```csharp
public enum SemanticRole
{
    ARG0,       // Agent
    ARG1,       // Patient/Theme
    ARG2, ARG3, ARG4,  // Verb-specific
    ARGM_DIR,   // Direction
    ARGM_LOC,   // Location
    ARGM_MNR,   // Manner
    ARGM_TMP,   // Temporal
    ARGM_EXT,   // Extent
    ARGM_PRP,   // Purpose
    ARGM_CAU,   // Cause
    ARGM_NEG,   // Negation
    ARGM_MOD    // Modal
}
```

---

## Features

### Sentence Segmentation

- Rule-based sentence boundary detection
- Handles common abbreviations (Dr., Corp., etc.)
- Supports multiple sentence-ending punctuation (., !, ?)

### Tokenization

- Word tokenization with punctuation separation
- Part-of-speech tagging (coarse and fine-grained)
- Lemmatization (base form extraction)
- Character offset tracking
- Stop word and punctuation identification

### Dependency Parsing

- Builds dependency tree from relations
- Identifies subjects (nsubj), objects (dobj), modifiers
- Provides tree traversal methods (GetDescendants, GetSubtreeText)
- Common relation constants in DependencyRelations class

### Semantic Role Labeling

- Rule-based SRL using dependency relations
- PropBank-style role assignment (ARG0, ARG1, ARGM-*)
- Frame extraction for verbal predicates
- Agent/Patient/Location/Temporal accessors

### Named Entity Recognition

- Proper noun sequence detection
- Basic entity type labeling (ORG, ENTITY)

### Caching

- SHA256-based cache key generation
- 30-minute cache expiration
- Per-sentence caching for efficiency
- Optional via ParseOptions.UseCache

### Multi-language Support

- English (en), German (de), French (fr), Spanish (es)
- Language-specific processing via ParseOptions.Language

---

## Test Summary

| Category | Count | Status |
| :--- | :--- | :--- |
| ParseAsync Tests | 7 | ✅ Pass |
| Helper Method Tests | 6 | ✅ Pass |
| Caching Tests | 2 | ✅ Pass |
| Long Sentence Tests | 2 | ✅ Pass |
| Token Tests | 4 | ✅ Pass |
| Dependency Tests | 3 | ✅ Pass |
| Semantic Frame Tests | 3 | ✅ Pass |
| Statistics Tests | 2 | ✅ Pass |
| Parser Metadata Tests | 2 | ✅ Pass |
| ParseSentenceAsync Tests | 2 | ✅ Pass |
| Edge Cases | 3 | ✅ Pass |
| **Total** | **35** | ✅ **All Pass** |

---

## Dependencies

### Upstream

| Module | Interface | Version |
| :--- | :--- | :--- |
| `Microsoft.Extensions.Logging` | `ILogger<T>` | v0.0.3b |
| `Microsoft.Extensions.Caching.Memory` | `IMemoryCache` | v0.4.6f |

### NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `Microsoft.Extensions.Caching.Memory` | 9.0.0 | Parse caching |

---

## License Gating

| Tier | Features |
| :--- | :--- |
| WriterPro | Basic parsing (tokens, POS) |
| Teams | Full functionality (SRL, multi-language) |
| Enterprise | Full functionality |

**Feature Gate Key:** `knowledge.claims.parser.enabled`

---

## Performance

| Metric | Target | Achieved |
| :--- | :--- | :--- |
| Parse 100 sentences | < 5 seconds | ✅ < 2 seconds |
| Cache hit | Identical result | ✅ Same ID |
| Memory footprint | < 50MB per 1000 sentences | ✅ |

---

## Breaking Changes

None. This is a new feature addition.

---

## Migration Notes

No migration required. New APIs only.

---

## Known Limitations

1. **Simple NLP Implementation**: The current implementation uses rule-based heuristics rather than full SpaCy. This is sufficient for testing but may be less accurate than a full ML-based parser.

2. **Abbreviation Handling**: Basic abbreviation detection may not catch all cases.

3. **SRL Accuracy**: Rule-based SRL identifies common patterns (nsubj→ARG0, dobj→ARG1) but may miss complex constructions.

---

## Next Steps

- v0.5.6g: Claim Extractor (consumes ParsedSentence to extract Claims)
- Future: Full SpaCy integration via Python interop
