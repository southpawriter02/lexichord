# LCS-DES-056-KG-g: Claim Extractor

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-056-KG-g |
| **Feature ID** | KG-056g |
| **Feature Name** | Claim Extractor |
| **Target Version** | v0.5.6g |
| **Module Scope** | `Lexichord.Nlu.ClaimExtraction` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | Teams (full), Enterprise (custom patterns) |
| **Feature Gate Key** | `knowledge.claims.extractor.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

The **Claim Extractor** is the core component that transforms parsed sentences into formal claims (subject-predicate-object triples). It combines rule-based pattern matching with dependency-based extraction to identify assertions that can be validated against the Knowledge Graph.

### 2.2 The Proposed Solution

Implement a hybrid extraction system that:

- Uses configurable patterns to match claim structures in text
- Extracts claims from dependency parse trees (SVO extraction)
- Links claim subjects/objects to graph entities via the Entity Linking service
- Scores extraction confidence based on pattern match quality
- Deduplicates semantically equivalent claims
- Supports custom extraction patterns for domain-specific claims

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.6e: Claim data model
- v0.5.6f: `ISentenceParser` — Parsed sentences
- v0.5.5g: `IEntityLinkingService` — Entity resolution

**NuGet Packages:**
- `YamlDotNet` — Pattern configuration
- `Microsoft.Extensions.Caching.Memory` — Extraction caching

### 3.2 Module Placement

```
Lexichord.Nlu/
├── ClaimExtraction/
│   ├── IClaimExtractionService.cs
│   ├── ClaimExtractionService.cs
│   ├── ClaimExtractionContext.cs
│   ├── Extractors/
│   │   ├── IClaimExtractor.cs
│   │   ├── PatternClaimExtractor.cs
│   │   ├── DependencyClaimExtractor.cs
│   │   └── HybridClaimExtractor.cs
│   ├── Patterns/
│   │   ├── ExtractionPattern.cs
│   │   ├── PatternLoader.cs
│   │   └── BuiltInPatterns.yaml
│   └── PostProcessing/
│       ├── ClaimDeduplicator.cs
│       └── ConfidenceScorer.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Load with NLU pipeline
- **Fallback Experience:** WriterPro: view only; Teams: full extraction; Enterprise: custom patterns

---

## 4. Data Contract (The API)

### 4.1 Core Interfaces

```csharp
namespace Lexichord.Nlu.ClaimExtraction;

/// <summary>
/// Service for extracting claims from text.
/// </summary>
public interface IClaimExtractionService
{
    /// <summary>
    /// Extracts claims from document text.
    /// </summary>
    /// <param name="text">Document text.</param>
    /// <param name="linkedEntities">Pre-linked entities in text.</param>
    /// <param name="context">Extraction context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Extracted claims.</returns>
    Task<ClaimExtractionResult> ExtractClaimsAsync(
        string text,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Extracts claims from a single sentence.
    /// </summary>
    Task<IReadOnlyList<Claim>> ExtractFromSentenceAsync(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Reloads extraction patterns.
    /// </summary>
    Task ReloadPatternsAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets extraction statistics.
    /// </summary>
    ClaimExtractionStats GetStats();
}

/// <summary>
/// Context for claim extraction.
/// </summary>
public record ClaimExtractionContext
{
    /// <summary>Document ID being processed.</summary>
    public Guid DocumentId { get; init; }

    /// <summary>Project ID for project-specific patterns.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>Minimum confidence threshold.</summary>
    public float MinConfidence { get; init; } = 0.5f;

    /// <summary>Whether to use pattern-based extraction.</summary>
    public bool UsePatterns { get; init; } = true;

    /// <summary>Whether to use dependency-based extraction.</summary>
    public bool UseDependencyExtraction { get; init; } = true;

    /// <summary>Whether to deduplicate claims.</summary>
    public bool DeduplicateClaims { get; init; } = true;

    /// <summary>Predicates to extract (null = all).</summary>
    public IReadOnlyList<string>? PredicateFilter { get; init; }

    /// <summary>Maximum claims per sentence.</summary>
    public int MaxClaimsPerSentence { get; init; } = 5;

    /// <summary>Custom patterns to use.</summary>
    public IReadOnlyList<ExtractionPattern>? CustomPatterns { get; init; }
}
```

### 4.2 Extraction Pattern

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Patterns;

/// <summary>
/// A pattern for extracting claims from text.
/// </summary>
public record ExtractionPattern
{
    /// <summary>Unique pattern ID.</summary>
    public required string Id { get; init; }

    /// <summary>Pattern name for display.</summary>
    public required string Name { get; init; }

    /// <summary>Description of what this pattern extracts.</summary>
    public string? Description { get; init; }

    /// <summary>Pattern type.</summary>
    public PatternType Type { get; init; }

    /// <summary>Regex pattern (for Type=Regex).</summary>
    public string? RegexPattern { get; init; }

    /// <summary>Template pattern with placeholders (for Type=Template).</summary>
    public string? Template { get; init; }

    /// <summary>Dependency pattern (for Type=Dependency).</summary>
    public DependencyPattern? DependencyPattern { get; init; }

    /// <summary>Predicate to assign to extracted claims.</summary>
    public required string Predicate { get; init; }

    /// <summary>Subject entity type.</summary>
    public required string SubjectType { get; init; }

    /// <summary>Object entity type (or "literal" for literals).</summary>
    public required string ObjectType { get; init; }

    /// <summary>Whether object is literal value.</summary>
    public bool ObjectIsLiteral { get; init; }

    /// <summary>Literal type if ObjectIsLiteral (e.g., "int", "bool").</summary>
    public string? LiteralType { get; init; }

    /// <summary>Base confidence for matches.</summary>
    public float BaseConfidence { get; init; } = 0.8f;

    /// <summary>Priority (higher = matched first).</summary>
    public int Priority { get; init; }

    /// <summary>Whether pattern is enabled.</summary>
    public bool IsEnabled { get; init; } = true;

    /// <summary>Tags for filtering.</summary>
    public IReadOnlyList<string>? Tags { get; init; }
}

/// <summary>
/// Pattern type.
/// </summary>
public enum PatternType
{
    /// <summary>Regular expression pattern.</summary>
    Regex,

    /// <summary>Template with placeholders ({SUBJECT}, {OBJECT}).</summary>
    Template,

    /// <summary>Dependency tree pattern.</summary>
    Dependency
}

/// <summary>
/// Pattern based on dependency relations.
/// </summary>
public record DependencyPattern
{
    /// <summary>Required relations from verb to subject.</summary>
    public IReadOnlyList<string>? SubjectRelations { get; init; }

    /// <summary>Required relations from verb to object.</summary>
    public IReadOnlyList<string>? ObjectRelations { get; init; }

    /// <summary>Required verb lemmas.</summary>
    public IReadOnlyList<string>? VerbLemmas { get; init; }

    /// <summary>Required prepositions.</summary>
    public IReadOnlyList<string>? Prepositions { get; init; }
}
```

### 4.3 Extractor Interface

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Extractors;

/// <summary>
/// Extracts claims using a specific method.
/// </summary>
public interface IClaimExtractor
{
    /// <summary>Extractor name.</summary>
    string Name { get; }

    /// <summary>
    /// Extracts claims from a parsed sentence.
    /// </summary>
    IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context);
}

/// <summary>
/// A claim extracted before post-processing.
/// </summary>
public record ExtractedClaim
{
    /// <summary>Subject text span.</summary>
    public required TextSpan SubjectSpan { get; init; }

    /// <summary>Object text span.</summary>
    public required TextSpan ObjectSpan { get; init; }

    /// <summary>Predicate.</summary>
    public required string Predicate { get; init; }

    /// <summary>Extraction method.</summary>
    public required ClaimExtractionMethod Method { get; init; }

    /// <summary>Pattern ID (if pattern-based).</summary>
    public string? PatternId { get; init; }

    /// <summary>Raw confidence before adjustments.</summary>
    public float RawConfidence { get; init; }

    /// <summary>Source sentence.</summary>
    public required ParsedSentence Sentence { get; init; }

    /// <summary>Matched linked entity for subject.</summary>
    public LinkedEntity? SubjectEntity { get; init; }

    /// <summary>Matched linked entity for object.</summary>
    public LinkedEntity? ObjectEntity { get; init; }

    /// <summary>Literal value if object is literal.</summary>
    public object? LiteralValue { get; init; }

    /// <summary>Literal type.</summary>
    public string? LiteralType { get; init; }
}

/// <summary>
/// Text span with positions.
/// </summary>
public record TextSpan
{
    public required string Text { get; init; }
    public int StartOffset { get; init; }
    public int EndOffset { get; init; }
}
```

---

## 5. Implementation Logic

### 5.1 ClaimExtractionService

```csharp
namespace Lexichord.Nlu.ClaimExtraction;

/// <summary>
/// Main claim extraction service.
/// </summary>
public class ClaimExtractionService : IClaimExtractionService
{
    private readonly ISentenceParser _parser;
    private readonly IEnumerable<IClaimExtractor> _extractors;
    private readonly IEntityLinkingService _linkingService;
    private readonly ClaimDeduplicator _deduplicator;
    private readonly ConfidenceScorer _confidenceScorer;
    private readonly ILogger<ClaimExtractionService> _logger;
    private readonly ClaimExtractionStats _stats = new();

    public async Task<ClaimExtractionResult> ExtractClaimsAsync(
        string text,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Parse text into sentences
        var parseResult = await _parser.ParseAsync(text, new ParseOptions
        {
            IncludeDependencies = context.UseDependencyExtraction,
            IncludeSRL = true
        }, ct);

        var allClaims = new List<Claim>();
        var failedSentences = new List<string>();

        foreach (var sentence in parseResult.Sentences)
        {
            try
            {
                var claims = await ExtractFromSentenceAsync(
                    sentence, linkedEntities, context, ct);

                allClaims.AddRange(claims);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract claims from sentence: {Sentence}",
                    sentence.Text.Truncate(100));
                failedSentences.Add(sentence.Text);
            }
        }

        // Deduplicate
        if (context.DeduplicateClaims)
        {
            var beforeCount = allClaims.Count;
            allClaims = _deduplicator.Deduplicate(allClaims);
            _stats.DuplicatesRemoved += beforeCount - allClaims.Count;
        }

        sw.Stop();

        return new ClaimExtractionResult
        {
            Claims = allClaims,
            DocumentId = context.DocumentId,
            Duration = sw.Elapsed,
            Stats = ComputeStats(allClaims, parseResult.Sentences.Count),
            FailedSentences = failedSentences
        };
    }

    public async Task<IReadOnlyList<Claim>> ExtractFromSentenceAsync(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> linkedEntities,
        ClaimExtractionContext context,
        CancellationToken ct = default)
    {
        var extractedClaims = new List<ExtractedClaim>();

        // Get entities in this sentence
        var sentenceEntities = linkedEntities
            .Where(e => e.Mention.StartOffset >= sentence.StartOffset &&
                        e.Mention.EndOffset <= sentence.EndOffset)
            .ToList();

        // Run all extractors
        foreach (var extractor in _extractors)
        {
            var claims = extractor.Extract(sentence, sentenceEntities, context);
            extractedClaims.AddRange(claims);
        }

        // Limit claims per sentence
        extractedClaims = extractedClaims
            .OrderByDescending(c => c.RawConfidence)
            .Take(context.MaxClaimsPerSentence)
            .ToList();

        // Convert to Claim records
        var claims = new List<Claim>();

        foreach (var extracted in extractedClaims)
        {
            var claim = await ConvertToClaimAsync(extracted, context, ct);
            if (claim.Confidence >= context.MinConfidence)
            {
                claims.Add(claim);
            }
        }

        return claims;
    }

    private async Task<Claim> ConvertToClaimAsync(
        ExtractedClaim extracted,
        ClaimExtractionContext context,
        CancellationToken ct)
    {
        // Resolve subject entity
        var subject = extracted.SubjectEntity != null
            ? ClaimEntity.Resolved(
                extracted.SubjectEntity.ResolvedEntity!,
                extracted.SubjectSpan.Text,
                extracted.SubjectEntity.Confidence)
            : ClaimEntity.Unresolved(
                extracted.SubjectSpan.Text,
                GuessEntityType(extracted.SubjectSpan.Text));

        // Resolve object
        ClaimObject claimObject;
        if (extracted.LiteralValue != null)
        {
            claimObject = new ClaimObject
            {
                Type = ClaimObjectType.Literal,
                LiteralValue = extracted.LiteralValue,
                LiteralType = extracted.LiteralType
            };
        }
        else if (extracted.ObjectEntity != null)
        {
            claimObject = ClaimObject.FromEntity(ClaimEntity.Resolved(
                extracted.ObjectEntity.ResolvedEntity!,
                extracted.ObjectSpan.Text,
                extracted.ObjectEntity.Confidence));
        }
        else
        {
            claimObject = ClaimObject.FromEntity(ClaimEntity.Unresolved(
                extracted.ObjectSpan.Text,
                GuessEntityType(extracted.ObjectSpan.Text)));
        }

        // Score confidence
        var confidence = _confidenceScorer.Score(extracted);

        return new Claim
        {
            Subject = subject,
            Predicate = extracted.Predicate,
            Object = claimObject,
            Confidence = confidence,
            DocumentId = context.DocumentId,
            ProjectId = context.ProjectId,
            Evidence = new ClaimEvidence
            {
                Sentence = extracted.Sentence.Text,
                StartOffset = extracted.Sentence.StartOffset,
                EndOffset = extracted.Sentence.EndOffset,
                ExtractionMethod = extracted.Method,
                PatternId = extracted.PatternId
            }
        };
    }

    private string GuessEntityType(string text)
    {
        // Simple heuristics for unknown entities
        if (text.StartsWith("/") || text.Contains(" /"))
            return "Endpoint";
        if (text.All(c => char.IsLower(c) || c == '_'))
            return "Parameter";
        if (text.StartsWith("HTTP") || int.TryParse(text, out _))
            return "StatusCode";

        return "Concept";
    }
}
```

### 5.2 Pattern-Based Extractor

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Extractors;

/// <summary>
/// Extracts claims using configurable patterns.
/// </summary>
public class PatternClaimExtractor : IClaimExtractor
{
    private readonly List<CompiledPattern> _patterns = new();

    public string Name => "PatternExtractor";

    public PatternClaimExtractor(IEnumerable<ExtractionPattern> patterns)
    {
        foreach (var pattern in patterns.Where(p => p.IsEnabled).OrderByDescending(p => p.Priority))
        {
            _patterns.Add(new CompiledPattern(pattern));
        }
    }

    public IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context)
    {
        var claims = new List<ExtractedClaim>();

        foreach (var pattern in _patterns)
        {
            if (context.PredicateFilter != null &&
                !context.PredicateFilter.Contains(pattern.Pattern.Predicate))
            {
                continue;
            }

            var matches = pattern.Match(sentence.Text);

            foreach (var match in matches)
            {
                // Find matching entities
                var subjectEntity = FindEntityInSpan(entities, match.SubjectSpan, sentence);
                var objectEntity = pattern.Pattern.ObjectIsLiteral
                    ? null
                    : FindEntityInSpan(entities, match.ObjectSpan, sentence);

                claims.Add(new ExtractedClaim
                {
                    SubjectSpan = match.SubjectSpan,
                    ObjectSpan = match.ObjectSpan,
                    Predicate = pattern.Pattern.Predicate,
                    Method = ClaimExtractionMethod.PatternRule,
                    PatternId = pattern.Pattern.Id,
                    RawConfidence = pattern.Pattern.BaseConfidence,
                    Sentence = sentence,
                    SubjectEntity = subjectEntity,
                    ObjectEntity = objectEntity,
                    LiteralValue = pattern.Pattern.ObjectIsLiteral ? match.ObjectSpan.Text : null,
                    LiteralType = pattern.Pattern.LiteralType
                });
            }
        }

        return claims;
    }

    private LinkedEntity? FindEntityInSpan(
        IReadOnlyList<LinkedEntity> entities,
        TextSpan span,
        ParsedSentence sentence)
    {
        var absoluteStart = sentence.StartOffset + span.StartOffset;
        var absoluteEnd = sentence.StartOffset + span.EndOffset;

        return entities.FirstOrDefault(e =>
            e.Mention.StartOffset >= absoluteStart &&
            e.Mention.EndOffset <= absoluteEnd);
    }

    private class CompiledPattern
    {
        public ExtractionPattern Pattern { get; }
        private readonly Regex? _regex;

        public CompiledPattern(ExtractionPattern pattern)
        {
            Pattern = pattern;

            if (pattern.Type == PatternType.Regex && pattern.RegexPattern != null)
            {
                _regex = new Regex(pattern.RegexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
            else if (pattern.Type == PatternType.Template && pattern.Template != null)
            {
                // Convert template to regex
                var regexPattern = Regex.Escape(pattern.Template)
                    .Replace("\\{SUBJECT\\}", "(?<subject>.+?)")
                    .Replace("\\{OBJECT\\}", "(?<object>.+?)");
                _regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }
        }

        public IEnumerable<PatternMatch> Match(string text)
        {
            if (_regex == null) yield break;

            foreach (Match match in _regex.Matches(text))
            {
                var subjectGroup = match.Groups["subject"];
                var objectGroup = match.Groups["object"];

                if (subjectGroup.Success && objectGroup.Success)
                {
                    yield return new PatternMatch
                    {
                        SubjectSpan = new TextSpan
                        {
                            Text = subjectGroup.Value,
                            StartOffset = subjectGroup.Index,
                            EndOffset = subjectGroup.Index + subjectGroup.Length
                        },
                        ObjectSpan = new TextSpan
                        {
                            Text = objectGroup.Value,
                            StartOffset = objectGroup.Index,
                            EndOffset = objectGroup.Index + objectGroup.Length
                        }
                    };
                }
            }
        }
    }

    private record PatternMatch
    {
        public required TextSpan SubjectSpan { get; init; }
        public required TextSpan ObjectSpan { get; init; }
    }
}
```

### 5.3 Dependency-Based Extractor

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Extractors;

/// <summary>
/// Extracts claims from dependency parse trees.
/// </summary>
public class DependencyClaimExtractor : IClaimExtractor
{
    private readonly Dictionary<string, string> _verbToPredicateMap = new()
    {
        ["accept"] = ClaimPredicate.ACCEPTS,
        ["return"] = ClaimPredicate.RETURNS,
        ["require"] = ClaimPredicate.REQUIRES,
        ["contain"] = ClaimPredicate.CONTAINS,
        ["produce"] = ClaimPredicate.PRODUCES,
        ["consume"] = ClaimPredicate.CONSUMES,
        ["have"] = ClaimPredicate.HAS_PROPERTY,
        ["implement"] = ClaimPredicate.IMPLEMENTS,
        ["extend"] = ClaimPredicate.EXTENDS,
        ["depend"] = ClaimPredicate.DEPENDS_ON,
        ["deprecate"] = ClaimPredicate.IS_DEPRECATED
    };

    public string Name => "DependencyExtractor";

    public IReadOnlyList<ExtractedClaim> Extract(
        ParsedSentence sentence,
        IReadOnlyList<LinkedEntity> entities,
        ClaimExtractionContext context)
    {
        var claims = new List<ExtractedClaim>();

        if (sentence.Dependencies == null) return claims;

        // Find root verb
        var rootVerb = sentence.GetRootVerb();
        if (rootVerb == null) return claims;

        // Map verb to predicate
        var lemma = rootVerb.Lemma?.ToLowerInvariant() ?? rootVerb.Text.ToLowerInvariant();
        if (!_verbToPredicateMap.TryGetValue(lemma, out var predicate))
        {
            predicate = ClaimPredicate.RELATED_TO; // Default
        }

        // Check predicate filter
        if (context.PredicateFilter != null && !context.PredicateFilter.Contains(predicate))
        {
            return claims;
        }

        // Find subject
        var subject = sentence.GetSubject();
        if (subject == null) return claims;

        // Find object
        var directObject = sentence.GetDirectObject();
        if (directObject == null) return claims;

        // Get full noun phrases
        var subjectPhrase = GetNounPhrase(subject, sentence);
        var objectPhrase = GetNounPhrase(directObject, sentence);

        // Find matching entities
        var subjectEntity = FindMatchingEntity(entities, subjectPhrase, sentence);
        var objectEntity = FindMatchingEntity(entities, objectPhrase, sentence);

        claims.Add(new ExtractedClaim
        {
            SubjectSpan = new TextSpan
            {
                Text = subjectPhrase,
                StartOffset = subject.StartChar - sentence.StartOffset,
                EndOffset = subject.EndChar - sentence.StartOffset
            },
            ObjectSpan = new TextSpan
            {
                Text = objectPhrase,
                StartOffset = directObject.StartChar - sentence.StartOffset,
                EndOffset = directObject.EndChar - sentence.StartOffset
            },
            Predicate = predicate,
            Method = ClaimExtractionMethod.DependencyParsing,
            RawConfidence = 0.7f, // Lower base confidence for dependency extraction
            Sentence = sentence,
            SubjectEntity = subjectEntity,
            ObjectEntity = objectEntity
        });

        return claims;
    }

    private string GetNounPhrase(Token head, ParsedSentence sentence)
    {
        var tokens = new List<Token> { head };

        // Add compound modifiers
        var compounds = sentence.Dependencies?
            .Where(d => d.Head == head && d.Relation == DependencyRelations.COMPOUND)
            .Select(d => d.Dependent)
            .ToList() ?? new List<Token>();

        tokens.AddRange(compounds);

        // Sort by position and join
        return string.Join(" ", tokens.OrderBy(t => t.Index).Select(t => t.Text));
    }

    private LinkedEntity? FindMatchingEntity(
        IReadOnlyList<LinkedEntity> entities,
        string phrase,
        ParsedSentence sentence)
    {
        return entities.FirstOrDefault(e =>
            phrase.Contains(e.Mention.Value, StringComparison.OrdinalIgnoreCase) ||
            e.Mention.Value.Contains(phrase, StringComparison.OrdinalIgnoreCase));
    }
}
```

---

## 6. Built-in Extraction Patterns

```yaml
# BuiltInPatterns.yaml
patterns:
  # API/Endpoint patterns
  - id: endpoint-accepts-parameter
    name: "Endpoint Accepts Parameter"
    type: Template
    template: "{SUBJECT} accepts {OBJECT}"
    predicate: ACCEPTS
    subject_type: Endpoint
    object_type: Parameter
    base_confidence: 0.85
    priority: 10
    tags: [api, endpoint, parameter]

  - id: endpoint-returns-response
    name: "Endpoint Returns Response"
    type: Template
    template: "{SUBJECT} returns {OBJECT}"
    predicate: RETURNS
    subject_type: Endpoint
    object_type: Response
    base_confidence: 0.85
    priority: 10

  - id: endpoint-requires-auth
    name: "Endpoint Requires Authentication"
    type: Regex
    regex_pattern: "(?<subject>[A-Z]+\\s*/[\\w/]+|the\\s+\\w+\\s+endpoint)\\s+requires\\s+(?<object>authentication|authorization|an?\\s+API\\s+key)"
    predicate: REQUIRES
    subject_type: Endpoint
    object_type: Concept
    base_confidence: 0.9
    priority: 15

  # Property patterns
  - id: parameter-defaults-to
    name: "Parameter Has Default"
    type: Template
    template: "{SUBJECT} defaults to {OBJECT}"
    predicate: HAS_DEFAULT
    subject_type: Parameter
    object_type: literal
    object_is_literal: true
    base_confidence: 0.9
    priority: 10

  - id: rate-limit-value
    name: "Rate Limit Value"
    type: Regex
    regex_pattern: "(?<subject>rate\\s+limit(?:ing)?)\\s+(?:is\\s+)?(?:set\\s+to\\s+)?(?<object>\\d+\\s*(?:requests?)?(?:\\s*(?:per|/)\\s*(?:minute|hour|second|day))?)"
    predicate: HAS_VALUE
    subject_type: Concept
    object_type: literal
    object_is_literal: true
    literal_type: string
    base_confidence: 0.85
    priority: 12

  # Deprecation patterns
  - id: deprecated-endpoint
    name: "Deprecated Endpoint"
    type: Regex
    regex_pattern: "(?<subject>[A-Z]+\\s*/[\\w/]+|this\\s+endpoint)\\s+(?:is|has\\s+been)\\s+(?<object>deprecated)"
    predicate: IS_DEPRECATED
    subject_type: Endpoint
    object_type: literal
    object_is_literal: true
    literal_type: bool
    base_confidence: 0.95
    priority: 20

  # Relationship patterns
  - id: entity-contains
    name: "Entity Contains"
    type: Template
    template: "{SUBJECT} contains {OBJECT}"
    predicate: CONTAINS
    subject_type: "*"
    object_type: "*"
    base_confidence: 0.8
    priority: 5

  - id: entity-implements
    name: "Entity Implements"
    type: Template
    template: "{SUBJECT} implements {OBJECT}"
    predicate: IMPLEMENTS
    subject_type: "*"
    object_type: "*"
    base_confidence: 0.85
    priority: 8
```

---

## 7. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                   Claim Extraction Flow                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────┐    ┌────────────────────────┐       │
│  │   Document Text        │    │   Linked Entities      │       │
│  └───────────┬────────────┘    └───────────┬────────────┘       │
│              │                             │                     │
│              ▼                             │                     │
│  ┌────────────────────────┐               │                     │
│  │   Sentence Parser      │               │                     │
│  │   (v0.5.6f)            │               │                     │
│  └───────────┬────────────┘               │                     │
│              │                             │                     │
│              ▼                             ▼                     │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                    For Each Sentence                     │   │
│  ├─────────────────────────────────────────────────────────┤   │
│  │                                                          │   │
│  │  ┌─────────────────┐        ┌─────────────────┐         │   │
│  │  │ Pattern-Based   │        │ Dependency-Based│         │   │
│  │  │ Extractor       │        │ Extractor       │         │   │
│  │  │                 │        │                 │         │   │
│  │  │ Template Match  │        │ SVO Extraction  │         │   │
│  │  │ Regex Match     │        │ Verb→Predicate  │         │   │
│  │  └────────┬────────┘        └────────┬────────┘         │   │
│  │           │                          │                   │   │
│  │           └──────────┬───────────────┘                   │   │
│  │                      │                                    │   │
│  │                      ▼                                    │   │
│  │           ┌─────────────────┐                            │   │
│  │           │ Merge Extracts  │                            │   │
│  │           │ (Limit per sent)│                            │   │
│  │           └────────┬────────┘                            │   │
│  │                    │                                      │   │
│  │                    ▼                                      │   │
│  │           ┌─────────────────┐                            │   │
│  │           │ Entity Linking  │                            │   │
│  │           │ (Subject/Object)│                            │   │
│  │           └────────┬────────┘                            │   │
│  │                    │                                      │   │
│  │                    ▼                                      │   │
│  │           ┌─────────────────┐                            │   │
│  │           │ Score Confidence│                            │   │
│  │           └────────┬────────┘                            │   │
│  │                    │                                      │   │
│  └────────────────────┼─────────────────────────────────────┘   │
│                       │                                          │
│                       ▼                                          │
│           ┌─────────────────────┐                               │
│           │   Deduplicate       │                               │
│           │   Claims            │                               │
│           └───────────┬─────────┘                               │
│                       │                                          │
│                       ▼                                          │
│           ┌─────────────────────┐                               │
│           │ ClaimExtractionResult│                               │
│           │ - Claims            │                               │
│           │ - Stats             │                               │
│           └─────────────────────┘                               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6g")]
public class ClaimExtractorTests
{
    private readonly IClaimExtractionService _service;

    [Fact]
    public async Task ExtractClaimsAsync_AcceptsPattern_ExtractsClaim()
    {
        // Arrange
        var text = "The GET /users endpoint accepts a limit parameter.";
        var entities = CreateLinkedEntities("GET /users", "Endpoint", "limit", "Parameter");
        var context = new ClaimExtractionContext { DocumentId = Guid.NewGuid() };

        // Act
        var result = await _service.ExtractClaimsAsync(text, entities, context);

        // Assert
        result.Claims.Should().ContainSingle(c =>
            c.Predicate == ClaimPredicate.ACCEPTS &&
            c.Subject.SurfaceForm.Contains("/users") &&
            c.Object.Entity!.SurfaceForm == "limit");
    }

    [Fact]
    public async Task ExtractClaimsAsync_DefaultsPattern_ExtractsLiteralObject()
    {
        // Arrange
        var text = "The limit parameter defaults to 10.";
        var entities = CreateLinkedEntities("limit", "Parameter");
        var context = new ClaimExtractionContext { DocumentId = Guid.NewGuid() };

        // Act
        var result = await _service.ExtractClaimsAsync(text, entities, context);

        // Assert
        result.Claims.Should().ContainSingle(c =>
            c.Predicate == ClaimPredicate.HAS_DEFAULT &&
            c.Object.Type == ClaimObjectType.Literal &&
            c.Object.LiteralValue!.ToString() == "10");
    }

    [Fact]
    public async Task ExtractClaimsAsync_DeprecationPattern_ExtractsBoolLiteral()
    {
        // Arrange
        var text = "The GET /v1/users endpoint has been deprecated.";
        var entities = CreateLinkedEntities("GET /v1/users", "Endpoint");
        var context = new ClaimExtractionContext { DocumentId = Guid.NewGuid() };

        // Act
        var result = await _service.ExtractClaimsAsync(text, entities, context);

        // Assert
        result.Claims.Should().Contain(c =>
            c.Predicate == ClaimPredicate.IS_DEPRECATED);
    }

    [Fact]
    public async Task ExtractClaimsAsync_DependencyExtraction_ExtractsFromParse()
    {
        // Arrange
        var text = "The authentication service requires valid credentials.";
        var entities = new List<LinkedEntity>();
        var context = new ClaimExtractionContext
        {
            DocumentId = Guid.NewGuid(),
            UseDependencyExtraction = true
        };

        // Act
        var result = await _service.ExtractClaimsAsync(text, entities, context);

        // Assert
        result.Claims.Should().Contain(c => c.Predicate == ClaimPredicate.REQUIRES);
    }

    [Fact]
    public async Task ExtractClaimsAsync_Deduplication_RemovesDuplicates()
    {
        // Arrange
        var text = "GET /users accepts limit. The GET /users endpoint accepts a limit parameter.";
        var entities = CreateLinkedEntities("GET /users", "Endpoint", "limit", "Parameter");
        var context = new ClaimExtractionContext
        {
            DocumentId = Guid.NewGuid(),
            DeduplicateClaims = true
        };

        // Act
        var result = await _service.ExtractClaimsAsync(text, entities, context);

        // Assert
        var acceptsClaims = result.Claims.Where(c => c.Predicate == ClaimPredicate.ACCEPTS);
        acceptsClaims.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExtractClaimsAsync_MinConfidenceFilter_ExcludesLowConfidence()
    {
        // Arrange
        var text = "Something vague about things.";
        var context = new ClaimExtractionContext
        {
            DocumentId = Guid.NewGuid(),
            MinConfidence = 0.8f
        };

        // Act
        var result = await _service.ExtractClaimsAsync(text, new List<LinkedEntity>(), context);

        // Assert
        result.Claims.Should().OnlyContain(c => c.Confidence >= 0.8f);
    }

    [Fact]
    public async Task ExtractClaimsAsync_PredicateFilter_OnlyExtractsFiltered()
    {
        // Arrange
        var text = "The endpoint accepts parameters and returns JSON.";
        var entities = CreateLinkedEntities("endpoint", "Endpoint", "parameters", "Parameter");
        var context = new ClaimExtractionContext
        {
            DocumentId = Guid.NewGuid(),
            PredicateFilter = new[] { ClaimPredicate.ACCEPTS }
        };

        // Act
        var result = await _service.ExtractClaimsAsync(text, entities, context);

        // Assert
        result.Claims.Should().OnlyContain(c => c.Predicate == ClaimPredicate.ACCEPTS);
    }
}
```

---

## 9. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Pattern-based extraction matches configured templates. |
| 2 | Dependency-based extraction extracts SVO triples. |
| 3 | Claims link to graph entities when available. |
| 4 | Literal objects correctly typed (int, bool, string). |
| 5 | Deduplication removes semantically equivalent claims. |
| 6 | MinConfidence filter excludes low-confidence claims. |
| 7 | PredicateFilter limits extraction to specified predicates. |
| 8 | MaxClaimsPerSentence limits output per sentence. |
| 9 | Custom patterns can be added without code changes. |
| 10 | 100-page document processes in <30 seconds. |

---

## 10. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IClaimExtractionService` interface | [ ] |
| 2 | `ClaimExtractionService` implementation | [ ] |
| 3 | `ClaimExtractionContext` record | [ ] |
| 4 | `IClaimExtractor` interface | [ ] |
| 5 | `PatternClaimExtractor` implementation | [ ] |
| 6 | `DependencyClaimExtractor` implementation | [ ] |
| 7 | `ExtractionPattern` record | [ ] |
| 8 | `PatternLoader` for YAML patterns | [ ] |
| 9 | `BuiltInPatterns.yaml` | [ ] |
| 10 | `ClaimDeduplicator` | [ ] |
| 11 | `ConfidenceScorer` | [ ] |
| 12 | Unit tests | [ ] |

---

## 11. Changelog Entry

```markdown
### Added (v0.5.6g)

- `IClaimExtractionService` interface for claim extraction
- `PatternClaimExtractor` with configurable patterns
- `DependencyClaimExtractor` using parse trees
- `ExtractionPattern` record with multiple pattern types
- Built-in patterns for API documentation claims
- Claim deduplication based on semantic equivalence
- Confidence scoring for extracted claims
- Support for literal and entity objects
- Custom pattern loading from YAML
```

---
