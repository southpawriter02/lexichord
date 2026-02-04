# LCS-DES-055-KG-e: Entity Recognizer

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-055-KG-e |
| **Feature ID** | KG-055e |
| **Feature Name** | Entity Recognizer |
| **Target Version** | v0.5.5e |
| **Module Scope** | `Lexichord.Nlu.EntityRecognition` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | WriterPro (basic), Teams (full) |
| **Feature Gate Key** | `knowledge.linking.recognizer.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Entity Linking begins with **Entity Recognition** — identifying spans of text that refer to domain entities. While v0.4.5g extracts mentions during document processing, we need a dedicated recognizer that combines SpaCy NER models with custom domain rules for technical documentation.

### 2.2 The Proposed Solution

Implement a hybrid entity recognition pipeline that:

- Uses SpaCy for baseline NER (via Python interop)
- Applies custom rule-based patterns for domain-specific entities
- Outputs `EntityMention` records with type, confidence, and span information
- Supports both real-time (editor) and batch (document processing) modes

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Nlu.Core` — Base NLU pipeline infrastructure
- v0.4.5g: `IEntityExtractionPipeline` — Integration point
- SpaCy (via Python.NET) — Pre-trained NER models

**NuGet Packages:**
- `Pythonnet` (4.0+) — Python interop
- `Microsoft.Extensions.Caching.Memory` — Model caching

### 3.2 Module Placement

```
Lexichord.Nlu/
├── EntityRecognition/
│   ├── IEntityRecognizer.cs
│   ├── EntityMention.cs
│   ├── RecognitionContext.cs
│   ├── SpacyRecognizer.cs
│   ├── RuleBasedRecognizer.cs
│   ├── HybridEntityRecognizer.cs
│   └── Rules/
│       ├── IRecognitionRule.cs
│       ├── PatternRule.cs
│       ├── DictionaryRule.cs
│       └── BuiltInRules.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Lazy load when NLU pipeline activated
- **Fallback Experience:** WriterPro gets rule-based only; Teams+ gets SpaCy + rules

---

## 4. Data Contract (The API)

### 4.1 Core Interfaces

```csharp
namespace Lexichord.Nlu.EntityRecognition;

/// <summary>
/// Service for recognizing entity mentions in text.
/// </summary>
public interface IEntityRecognizer
{
    /// <summary>
    /// Recognizes entity mentions in the provided text.
    /// </summary>
    /// <param name="text">Text to analyze.</param>
    /// <param name="context">Recognition context with document info.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Recognized entity mentions.</returns>
    Task<RecognitionResult> RecognizeAsync(
        string text,
        RecognitionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Recognizes entities in a batch of text segments.
    /// </summary>
    Task<IReadOnlyList<RecognitionResult>> RecognizeBatchAsync(
        IReadOnlyList<TextSegment> segments,
        RecognitionContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Gets the supported entity types for this recognizer.
    /// </summary>
    IReadOnlyList<string> SupportedEntityTypes { get; }
}

/// <summary>
/// Text segment for batch processing.
/// </summary>
public record TextSegment
{
    /// <summary>Unique segment identifier.</summary>
    public required string Id { get; init; }

    /// <summary>Text content.</summary>
    public required string Text { get; init; }

    /// <summary>Offset in source document.</summary>
    public int SourceOffset { get; init; }

    /// <summary>Metadata about the segment.</summary>
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }
}
```

### 4.2 EntityMention Record

```csharp
namespace Lexichord.Nlu.EntityRecognition;

/// <summary>
/// A mention of an entity detected in text.
/// </summary>
public record EntityMention
{
    /// <summary>
    /// Unique mention identifier.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The text value of the mention as it appears.
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// Normalized form of the mention (lowercase, trimmed).
    /// </summary>
    public string NormalizedValue => Value.ToLowerInvariant().Trim();

    /// <summary>
    /// Predicted entity type (e.g., "Endpoint", "Parameter", "Product").
    /// </summary>
    public required string EntityType { get; init; }

    /// <summary>
    /// Start character offset in source text.
    /// </summary>
    public required int StartOffset { get; init; }

    /// <summary>
    /// End character offset in source text.
    /// </summary>
    public required int EndOffset { get; init; }

    /// <summary>
    /// Length of the mention.
    /// </summary>
    public int Length => EndOffset - StartOffset;

    /// <summary>
    /// Recognition confidence score (0.0-1.0).
    /// </summary>
    public float Confidence { get; init; } = 1.0f;

    /// <summary>
    /// Source of the recognition (SpaCy, Rule, etc.).
    /// </summary>
    public RecognitionSource Source { get; init; }

    /// <summary>
    /// Rule ID if recognized by a rule.
    /// </summary>
    public string? RuleId { get; init; }

    /// <summary>
    /// Surrounding context (for disambiguation).
    /// </summary>
    public string? SurroundingContext { get; init; }

    /// <summary>
    /// Document ID where mention was found.
    /// </summary>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Additional properties extracted during recognition.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Properties { get; init; }
}

/// <summary>
/// Source of entity recognition.
/// </summary>
public enum RecognitionSource
{
    /// <summary>Recognized by SpaCy NER model.</summary>
    SpaCy,

    /// <summary>Recognized by pattern matching rule.</summary>
    PatternRule,

    /// <summary>Recognized by dictionary lookup.</summary>
    DictionaryLookup,

    /// <summary>Recognized by schema type matcher.</summary>
    SchemaMatcher,

    /// <summary>Recognized by multiple sources (merged).</summary>
    Merged
}
```

### 4.3 RecognitionContext and Result

```csharp
namespace Lexichord.Nlu.EntityRecognition;

/// <summary>
/// Context for entity recognition.
/// </summary>
public record RecognitionContext
{
    /// <summary>
    /// Document ID being processed.
    /// </summary>
    public Guid? DocumentId { get; init; }

    /// <summary>
    /// Project ID for project-specific rules.
    /// </summary>
    public Guid? ProjectId { get; init; }

    /// <summary>
    /// Entity types to recognize (null = all).
    /// </summary>
    public IReadOnlyList<string>? EntityTypeFilter { get; init; }

    /// <summary>
    /// Minimum confidence threshold.
    /// </summary>
    public float MinConfidence { get; init; } = 0.5f;

    /// <summary>
    /// Include surrounding context in mentions.
    /// </summary>
    public bool IncludeContext { get; init; } = true;

    /// <summary>
    /// Context window size (characters before/after).
    /// </summary>
    public int ContextWindowSize { get; init; } = 100;

    /// <summary>
    /// Processing mode.
    /// </summary>
    public RecognitionMode Mode { get; init; } = RecognitionMode.Standard;

    /// <summary>
    /// Known entities to prioritize (from graph).
    /// </summary>
    public IReadOnlyList<KnownEntityHint>? KnownEntities { get; init; }
}

/// <summary>
/// Recognition processing mode.
/// </summary>
public enum RecognitionMode
{
    /// <summary>Standard processing with all features.</summary>
    Standard,

    /// <summary>Fast mode for real-time editing.</summary>
    RealTime,

    /// <summary>High-accuracy mode for final processing.</summary>
    HighAccuracy
}

/// <summary>
/// Hint about a known entity to help recognition.
/// </summary>
public record KnownEntityHint
{
    /// <summary>Entity name/label.</summary>
    public required string Name { get; init; }

    /// <summary>Entity type.</summary>
    public required string EntityType { get; init; }

    /// <summary>Alternative names/aliases.</summary>
    public IReadOnlyList<string>? Aliases { get; init; }

    /// <summary>Graph entity ID.</summary>
    public Guid? GraphEntityId { get; init; }
}

/// <summary>
/// Result of entity recognition.
/// </summary>
public record RecognitionResult
{
    /// <summary>
    /// Recognized entity mentions.
    /// </summary>
    public required IReadOnlyList<EntityMention> Mentions { get; init; }

    /// <summary>
    /// Processing duration.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Source text length processed.
    /// </summary>
    public int TextLength { get; init; }

    /// <summary>
    /// Whether processing was truncated.
    /// </summary>
    public bool WasTruncated { get; init; }

    /// <summary>
    /// Recognition statistics.
    /// </summary>
    public RecognitionStats Stats { get; init; } = new();

    /// <summary>
    /// Creates an empty result.
    /// </summary>
    public static RecognitionResult Empty => new()
    {
        Mentions = Array.Empty<EntityMention>()
    };
}

/// <summary>
/// Statistics about recognition.
/// </summary>
public record RecognitionStats
{
    public int TotalMentions { get; init; }
    public int SpaCyMentions { get; init; }
    public int RuleMentions { get; init; }
    public int MergedMentions { get; init; }
    public int FilteredByConfidence { get; init; }
    public IReadOnlyDictionary<string, int>? MentionsByType { get; init; }
}
```

### 4.4 Recognition Rules

```csharp
namespace Lexichord.Nlu.EntityRecognition.Rules;

/// <summary>
/// Interface for custom recognition rules.
/// </summary>
public interface IRecognitionRule
{
    /// <summary>Rule identifier.</summary>
    string Id { get; }

    /// <summary>Entity type this rule recognizes.</summary>
    string EntityType { get; }

    /// <summary>Rule priority (higher = evaluated first).</summary>
    int Priority { get; }

    /// <summary>Whether the rule is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Applies the rule to find mentions in text.
    /// </summary>
    IReadOnlyList<EntityMention> Apply(string text, RecognitionContext context);
}

/// <summary>
/// Pattern-based recognition rule using regex.
/// </summary>
public record PatternRule : IRecognitionRule
{
    public required string Id { get; init; }
    public required string EntityType { get; init; }
    public int Priority { get; init; } = 0;
    public bool IsEnabled { get; init; } = true;

    /// <summary>Regex pattern to match.</summary>
    public required string Pattern { get; init; }

    /// <summary>Named capture group for the entity value (default: entire match).</summary>
    public string? CaptureGroup { get; init; }

    /// <summary>Base confidence for matches.</summary>
    public float BaseConfidence { get; init; } = 0.8f;

    /// <summary>Property extractors from capture groups.</summary>
    public IReadOnlyDictionary<string, string>? PropertyExtractors { get; init; }

    public IReadOnlyList<EntityMention> Apply(string text, RecognitionContext context)
    {
        var mentions = new List<EntityMention>();
        var regex = new Regex(Pattern, RegexOptions.IgnoreCase);

        foreach (Match match in regex.Matches(text))
        {
            var value = CaptureGroup != null
                ? match.Groups[CaptureGroup].Value
                : match.Value;

            var properties = PropertyExtractors?.ToDictionary(
                kvp => kvp.Key,
                kvp => (object)match.Groups[kvp.Value].Value);

            mentions.Add(new EntityMention
            {
                Value = value,
                EntityType = EntityType,
                StartOffset = match.Index,
                EndOffset = match.Index + match.Length,
                Confidence = BaseConfidence,
                Source = RecognitionSource.PatternRule,
                RuleId = Id,
                Properties = properties
            });
        }

        return mentions;
    }
}

/// <summary>
/// Dictionary-based recognition rule.
/// </summary>
public record DictionaryRule : IRecognitionRule
{
    public required string Id { get; init; }
    public required string EntityType { get; init; }
    public int Priority { get; init; } = 10;
    public bool IsEnabled { get; init; } = true;

    /// <summary>Dictionary of terms to match (normalized form → original).</summary>
    public required IReadOnlyDictionary<string, string> Terms { get; init; }

    /// <summary>Whether to match case-insensitively.</summary>
    public bool CaseInsensitive { get; init; } = true;

    /// <summary>Whether to match whole words only.</summary>
    public bool WholeWordsOnly { get; init; } = true;

    /// <summary>Base confidence for matches.</summary>
    public float BaseConfidence { get; init; } = 0.9f;

    public IReadOnlyList<EntityMention> Apply(string text, RecognitionContext context)
    {
        // Implementation uses Aho-Corasick for efficient multi-pattern matching
        throw new NotImplementedException("Implemented in DictionaryMatcher class");
    }
}
```

---

## 5. Implementation Logic

### 5.1 HybridEntityRecognizer

```csharp
namespace Lexichord.Nlu.EntityRecognition;

/// <summary>
/// Hybrid recognizer combining SpaCy and rules.
/// </summary>
public class HybridEntityRecognizer : IEntityRecognizer
{
    private readonly SpacyRecognizer _spacyRecognizer;
    private readonly RuleBasedRecognizer _ruleRecognizer;
    private readonly ILogger<HybridEntityRecognizer> _logger;
    private readonly EntityRecognizerOptions _options;

    public IReadOnlyList<string> SupportedEntityTypes =>
        _spacyRecognizer.SupportedEntityTypes
            .Concat(_ruleRecognizer.SupportedEntityTypes)
            .Distinct()
            .ToList();

    public async Task<RecognitionResult> RecognizeAsync(
        string text,
        RecognitionContext context,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Run SpaCy and rules in parallel
        var spacyTask = _options.UseSpaCy && context.Mode != RecognitionMode.RealTime
            ? _spacyRecognizer.RecognizeAsync(text, context, ct)
            : Task.FromResult(RecognitionResult.Empty);

        var ruleTask = _ruleRecognizer.RecognizeAsync(text, context, ct);

        await Task.WhenAll(spacyTask, ruleTask);

        var spacyResult = await spacyTask;
        var ruleResult = await ruleTask;

        // Merge and deduplicate mentions
        var merged = MergeMentions(spacyResult.Mentions, ruleResult.Mentions);

        // Apply confidence filter
        var filtered = merged
            .Where(m => m.Confidence >= context.MinConfidence)
            .ToList();

        // Add surrounding context if requested
        if (context.IncludeContext)
        {
            filtered = AddSurroundingContext(filtered, text, context.ContextWindowSize);
        }

        sw.Stop();

        return new RecognitionResult
        {
            Mentions = filtered,
            Duration = sw.Elapsed,
            TextLength = text.Length,
            Stats = new RecognitionStats
            {
                TotalMentions = filtered.Count,
                SpaCyMentions = spacyResult.Mentions.Count,
                RuleMentions = ruleResult.Mentions.Count,
                MergedMentions = merged.Count(m => m.Source == RecognitionSource.Merged),
                FilteredByConfidence = merged.Count - filtered.Count,
                MentionsByType = filtered.GroupBy(m => m.EntityType)
                    .ToDictionary(g => g.Key, g => g.Count())
            }
        };
    }

    private List<EntityMention> MergeMentions(
        IReadOnlyList<EntityMention> spacyMentions,
        IReadOnlyList<EntityMention> ruleMentions)
    {
        var allMentions = spacyMentions.Concat(ruleMentions).ToList();
        var merged = new List<EntityMention>();

        // Sort by start offset
        allMentions.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));

        foreach (var mention in allMentions)
        {
            var overlapping = merged.FirstOrDefault(m =>
                OverlapsSignificantly(m, mention));

            if (overlapping != null)
            {
                // Merge: keep higher confidence, combine sources
                if (mention.Confidence > overlapping.Confidence)
                {
                    merged.Remove(overlapping);
                    merged.Add(mention with
                    {
                        Source = RecognitionSource.Merged,
                        Confidence = Math.Max(mention.Confidence, overlapping.Confidence)
                    });
                }
            }
            else
            {
                merged.Add(mention);
            }
        }

        return merged;
    }

    private bool OverlapsSignificantly(EntityMention a, EntityMention b)
    {
        var overlapStart = Math.Max(a.StartOffset, b.StartOffset);
        var overlapEnd = Math.Min(a.EndOffset, b.EndOffset);
        var overlapLength = Math.Max(0, overlapEnd - overlapStart);

        var minLength = Math.Min(a.Length, b.Length);
        return overlapLength > minLength * 0.5;
    }

    private List<EntityMention> AddSurroundingContext(
        List<EntityMention> mentions,
        string text,
        int windowSize)
    {
        return mentions.Select(m =>
        {
            var contextStart = Math.Max(0, m.StartOffset - windowSize);
            var contextEnd = Math.Min(text.Length, m.EndOffset + windowSize);
            var context = text.Substring(contextStart, contextEnd - contextStart);

            return m with { SurroundingContext = context };
        }).ToList();
    }
}
```

### 5.2 SpaCy Interop

```csharp
namespace Lexichord.Nlu.EntityRecognition;

/// <summary>
/// SpaCy-based entity recognizer via Python interop.
/// </summary>
public class SpacyRecognizer : IEntityRecognizer, IDisposable
{
    private readonly PythonEngine _python;
    private readonly dynamic _nlp;
    private readonly ILogger<SpacyRecognizer> _logger;

    private static readonly Dictionary<string, string> SpacyToLexichordTypes = new()
    {
        ["ORG"] = "Organization",
        ["PRODUCT"] = "Product",
        ["PERSON"] = "Person",
        ["GPE"] = "Location",
        ["DATE"] = "Date",
        ["CARDINAL"] = "Number",
        ["MONEY"] = "Currency",
        ["PERCENT"] = "Percentage",
        // Custom labels from trained model
        ["ENDPOINT"] = "Endpoint",
        ["PARAMETER"] = "Parameter",
        ["SCHEMA"] = "Schema",
        ["ERROR_CODE"] = "ErrorCode"
    };

    public IReadOnlyList<string> SupportedEntityTypes =>
        SpacyToLexichordTypes.Values.Distinct().ToList();

    public SpacyRecognizer(SpacyOptions options, ILogger<SpacyRecognizer> logger)
    {
        _logger = logger;

        // Initialize Python runtime
        _python = new PythonEngine();
        _python.Initialize();

        // Load SpaCy model
        using (Py.GIL())
        {
            dynamic spacy = Py.Import("spacy");
            _nlp = spacy.load(options.ModelName ?? "en_core_web_sm");
        }
    }

    public async Task<RecognitionResult> RecognizeAsync(
        string text,
        RecognitionContext context,
        CancellationToken ct = default)
    {
        return await Task.Run(() =>
        {
            var mentions = new List<EntityMention>();

            using (Py.GIL())
            {
                dynamic doc = _nlp(text);

                foreach (var ent in doc.ents)
                {
                    string label = ent.label_.ToString();

                    if (!SpacyToLexichordTypes.TryGetValue(label, out var entityType))
                    {
                        entityType = label; // Use raw label if no mapping
                    }

                    // Apply type filter if specified
                    if (context.EntityTypeFilter != null &&
                        !context.EntityTypeFilter.Contains(entityType))
                    {
                        continue;
                    }

                    mentions.Add(new EntityMention
                    {
                        Value = ent.text.ToString(),
                        EntityType = entityType,
                        StartOffset = (int)ent.start_char,
                        EndOffset = (int)ent.end_char,
                        Confidence = 0.75f, // SpaCy doesn't provide confidence by default
                        Source = RecognitionSource.SpaCy,
                        DocumentId = context.DocumentId
                    });
                }
            }

            return new RecognitionResult
            {
                Mentions = mentions,
                TextLength = text.Length
            };
        }, ct);
    }

    public void Dispose()
    {
        _python?.Shutdown();
    }
}
```

---

## 6. Built-in Recognition Rules

### 6.1 Technical Documentation Rules

```yaml
# Built-in rules for technical documentation
rules:
  # HTTP Endpoint patterns
  - id: http-endpoint
    entity_type: Endpoint
    type: pattern
    pattern: '(?<method>GET|POST|PUT|PATCH|DELETE|HEAD|OPTIONS)\s+(?<path>/[^\s]+)'
    confidence: 0.85
    property_extractors:
      method: method
      path: path

  # API path references
  - id: api-path-reference
    entity_type: Endpoint
    type: pattern
    pattern: '`(?<path>/[a-z0-9/_\-\{\}]+)`'
    confidence: 0.8
    capture_group: path

  # Query/path parameter references
  - id: parameter-reference
    entity_type: Parameter
    type: pattern
    pattern: '`(?<name>[a-z_][a-z0-9_]*)`\s+parameter'
    confidence: 0.85
    capture_group: name

  # Status code references
  - id: status-code
    entity_type: StatusCode
    type: pattern
    pattern: '(?<code>\d{3})\s+(?<name>[A-Z][a-z]+(?:\s+[A-Z][a-z]+)*)'
    confidence: 0.9
    property_extractors:
      code: code
      name: name

  # Schema/type references
  - id: schema-reference
    entity_type: Schema
    type: pattern
    pattern: '`(?<name>[A-Z][a-zA-Z0-9]+)`\s+(?:object|schema|type|model)'
    confidence: 0.85
    capture_group: name

  # Error code patterns
  - id: error-code
    entity_type: ErrorCode
    type: pattern
    pattern: '(?:error|code)\s*[:\s]+`?(?<code>[A-Z_]+\d*)`?'
    confidence: 0.8
    capture_group: code
```

---

## 7. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Entity Recognition Flow                       │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│    ┌──────────────┐                                             │
│    │  Input Text  │                                             │
│    └──────┬───────┘                                             │
│           │                                                      │
│           ▼                                                      │
│    ┌──────────────┐                                             │
│    │   Context    │ (DocumentId, EntityTypeFilter, Mode)        │
│    └──────┬───────┘                                             │
│           │                                                      │
│           ├─────────────────┬─────────────────┐                 │
│           │                 │                 │                  │
│           ▼                 ▼                 ▼                  │
│    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐        │
│    │    SpaCy     │  │   Pattern    │  │  Dictionary  │        │
│    │     NER      │  │    Rules     │  │   Lookup     │        │
│    │  (Teams+)    │  │              │  │              │        │
│    └──────┬───────┘  └──────┬───────┘  └──────┬───────┘        │
│           │                 │                 │                  │
│           └─────────────────┼─────────────────┘                 │
│                             │                                    │
│                             ▼                                    │
│                      ┌──────────────┐                           │
│                      │    Merge     │                           │
│                      │  & Dedup     │                           │
│                      └──────┬───────┘                           │
│                             │                                    │
│                             ▼                                    │
│                      ┌──────────────┐                           │
│                      │  Confidence  │                           │
│                      │   Filter     │                           │
│                      └──────┬───────┘                           │
│                             │                                    │
│                             ▼                                    │
│                      ┌──────────────┐                           │
│                      │ Add Context  │                           │
│                      │   Window     │                           │
│                      └──────┬───────┘                           │
│                             │                                    │
│                             ▼                                    │
│                      ┌──────────────┐                           │
│                      │  Recognition │                           │
│                      │    Result    │                           │
│                      └──────────────┘                           │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 8. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5e")]
public class EntityRecognizerTests
{
    private readonly HybridEntityRecognizer _recognizer;

    [Fact]
    public async Task RecognizeAsync_HttpEndpoint_DetectsCorrectly()
    {
        // Arrange
        var text = "Call the GET /users endpoint to retrieve all users.";
        var context = new RecognitionContext();

        // Act
        var result = await _recognizer.RecognizeAsync(text, context);

        // Assert
        result.Mentions.Should().ContainSingle(m =>
            m.EntityType == "Endpoint" &&
            m.Value.Contains("/users"));
    }

    [Fact]
    public async Task RecognizeAsync_Parameter_DetectsWithConfidence()
    {
        // Arrange
        var text = "The `limit` parameter controls pagination.";
        var context = new RecognitionContext { MinConfidence = 0.7f };

        // Act
        var result = await _recognizer.RecognizeAsync(text, context);

        // Assert
        result.Mentions.Should().ContainSingle(m =>
            m.EntityType == "Parameter" &&
            m.Value == "limit" &&
            m.Confidence >= 0.7f);
    }

    [Fact]
    public async Task RecognizeAsync_OverlappingMentions_MergesCorrectly()
    {
        // Arrange - Both SpaCy and rules might find same entity
        var text = "Use the POST /users endpoint";
        var context = new RecognitionContext();

        // Act
        var result = await _recognizer.RecognizeAsync(text, context);

        // Assert
        result.Mentions.Where(m => m.EntityType == "Endpoint")
            .Should().HaveCount(1, "overlapping mentions should be merged");
    }

    [Fact]
    public async Task RecognizeAsync_WithTypeFilter_FiltersCorrectly()
    {
        // Arrange
        var text = "GET /users returns the User schema with status 200 OK.";
        var context = new RecognitionContext
        {
            EntityTypeFilter = new[] { "Endpoint" }
        };

        // Act
        var result = await _recognizer.RecognizeAsync(text, context);

        // Assert
        result.Mentions.Should().OnlyContain(m => m.EntityType == "Endpoint");
    }

    [Fact]
    public async Task RecognizeAsync_IncludesContext_WhenRequested()
    {
        // Arrange
        var text = "To create a user, call POST /users with a JSON body.";
        var context = new RecognitionContext
        {
            IncludeContext = true,
            ContextWindowSize = 20
        };

        // Act
        var result = await _recognizer.RecognizeAsync(text, context);

        // Assert
        var mention = result.Mentions.First(m => m.EntityType == "Endpoint");
        mention.SurroundingContext.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task RecognizeBatchAsync_ProcessesSegmentsInParallel()
    {
        // Arrange
        var segments = Enumerable.Range(1, 10)
            .Select(i => new TextSegment
            {
                Id = i.ToString(),
                Text = $"GET /resource{i} endpoint"
            })
            .ToList();
        var context = new RecognitionContext();

        // Act
        var results = await _recognizer.RecognizeBatchAsync(segments, context);

        // Assert
        results.Should().HaveCount(10);
        results.All(r => r.Mentions.Count > 0).Should().BeTrue();
    }
}
```

---

## 9. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | HTTP endpoints (GET /path) recognized with >85% precision. |
| 2 | Parameter references in backticks recognized with >80% precision. |
| 3 | Schema/type references recognized with >80% precision. |
| 4 | Status code patterns recognized with >90% precision. |
| 5 | Overlapping mentions from different sources merged correctly. |
| 6 | Confidence filtering excludes low-confidence mentions. |
| 7 | Context window correctly captures surrounding text. |
| 8 | Batch processing handles 100 segments in <5 seconds. |
| 9 | Real-time mode processes <1000 chars in <100ms. |
| 10 | Custom rules can be added without code changes. |

---

## 10. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `IEntityRecognizer` interface | [ ] |
| 2 | `EntityMention` record | [ ] |
| 3 | `RecognitionContext` record | [ ] |
| 4 | `RecognitionResult` record | [ ] |
| 5 | `HybridEntityRecognizer` implementation | [ ] |
| 6 | `SpacyRecognizer` implementation | [ ] |
| 7 | `RuleBasedRecognizer` implementation | [ ] |
| 8 | `IRecognitionRule` interface | [ ] |
| 9 | `PatternRule` implementation | [ ] |
| 10 | `DictionaryRule` implementation | [ ] |
| 11 | Built-in technical documentation rules | [ ] |
| 12 | Unit tests | [ ] |

---

## 11. Changelog Entry

```markdown
### Added (v0.5.5e)

- `IEntityRecognizer` interface for entity mention detection
- `EntityMention` record with confidence and source tracking
- `HybridEntityRecognizer` combining SpaCy + rules
- `SpacyRecognizer` for ML-based NER via Python interop
- `RuleBasedRecognizer` for pattern and dictionary matching
- Built-in rules for technical documentation patterns
- Batch processing support for large documents
- Real-time mode for editor integration
```

---
