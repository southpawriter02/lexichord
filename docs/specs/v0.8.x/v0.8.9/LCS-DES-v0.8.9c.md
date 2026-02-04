# LDS-01: Feature Design Specification — Memory Encoder

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `MEM-03` | Matches the Roadmap ID. |
| **Feature Name** | Memory Encoder | The internal display name. |
| **Target Version** | `v0.8.9c` | The semantic version target. |
| **Module Scope** | `Lexichord.Modules.Agents` | The specific DLL/Project this code lives in. |
| **Swimlane** | Memory | The functional vertical. |
| **License Tier** | Writer Pro | The minimum license required to load this. |
| **Feature Gate Key** | `Agents.Memory.Encoder` | The string key used in `ILicenseService`. |
| **Author** | Lexichord Architecture | Primary Architect. |
| **Reviewer** | — | Lead Architect / Peer. |
| **Status** | Draft | Current lifecycle state. |
| **Last Updated** | 2026-02-03 | Date of last modification. |

---

## 2. Executive Summary

### 2.1 The Requirement
Raw content from conversations, corrections, and observations must be transformed into structured Memory objects with appropriate type classification, embeddings, and provenance. The system needs automated classification to determine whether content represents facts (semantic), events (episodic), or procedures (procedural).

### 2.2 The Proposed Solution
Implement `IMemoryEncoder` that processes content into Memory objects using hybrid classification (rule-based patterns + LLM fallback), embedding generation via existing `IEmbeddingService`, and provenance extraction from context.

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies
*   **Upstream Modules:**
    *   `Lexichord.Modules.Agents.Memory` (v0.8.9a models)
    *   `Lexichord.Modules.Rag` (`IEmbeddingService`)
    *   `Lexichord.Modules.Agents` (`IChatCompletionService`)
*   **NuGet Packages:**
    *   None additional

### 3.2 Licensing Behavior
*   **Load Behavior:**
    *   [x] **Soft Gate:** Encoding operations require Writer Pro license.
*   **Fallback Experience:**
    *   Core users cannot create memories; upgrade prompt displayed.

---

## 4. Data Contract (The API)

```csharp
namespace Lexichord.Modules.Agents.Memory.Abstractions;

/// <summary>
/// Encodes raw content into structured Memory objects.
/// </summary>
public interface IMemoryEncoder
{
    /// <summary>
    /// Encode content into a Memory with automatic type classification.
    /// </summary>
    /// <param name="content">The raw content to encode.</param>
    /// <param name="context">Context about where/how this content was learned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A fully constructed Memory object.</returns>
    Task<Memory> EncodeAsync(
        string content,
        MemoryContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Encode content with explicit type specification.
    /// </summary>
    /// <param name="content">The raw content to encode.</param>
    /// <param name="type">The memory type to use.</param>
    /// <param name="context">Context about where/how this content was learned.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A fully constructed Memory object.</returns>
    Task<Memory> EncodeWithTypeAsync(
        string content,
        MemoryType type,
        MemoryContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Classify content into a memory type.
    /// </summary>
    /// <param name="content">The content to classify.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The classified memory type with confidence.</returns>
    Task<TypeClassification> ClassifyTypeAsync(
        string content,
        CancellationToken ct = default);

    /// <summary>
    /// Batch encode multiple pieces of content.
    /// </summary>
    /// <param name="items">Content items with contexts.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Encoded memories.</returns>
    Task<IReadOnlyList<Memory>> EncodeBatchAsync(
        IReadOnlyList<EncodingRequest> items,
        CancellationToken ct = default);
}

/// <summary>
/// Result of memory type classification.
/// </summary>
/// <param name="Type">The classified memory type.</param>
/// <param name="Confidence">Confidence score (0.0 to 1.0).</param>
/// <param name="Method">How the classification was determined.</param>
/// <param name="Rationale">Explanation for the classification.</param>
public record TypeClassification(
    MemoryType Type,
    float Confidence,
    ClassificationMethod Method,
    string? Rationale);

/// <summary>
/// How the memory type was classified.
/// </summary>
public enum ClassificationMethod
{
    /// <summary>
    /// Classified using rule-based pattern matching.
    /// </summary>
    RuleBased,

    /// <summary>
    /// Classified using LLM analysis.
    /// </summary>
    LlmBased,

    /// <summary>
    /// Explicitly specified by caller.
    /// </summary>
    Explicit
}

/// <summary>
/// Request for batch encoding.
/// </summary>
/// <param name="Content">The content to encode.</param>
/// <param name="Context">The memory context.</param>
/// <param name="ExplicitType">Optional explicit type override.</param>
public record EncodingRequest(
    string Content,
    MemoryContext Context,
    MemoryType? ExplicitType = null);

/// <summary>
/// Configuration for the memory encoder.
/// </summary>
public record MemoryEncoderOptions
{
    /// <summary>
    /// Minimum confidence threshold for rule-based classification.
    /// Below this, LLM classification is used.
    /// </summary>
    public float RuleConfidenceThreshold { get; init; } = 0.7f;

    /// <summary>
    /// Maximum content length before summarization.
    /// </summary>
    public int MaxContentLength { get; init; } = 2000;

    /// <summary>
    /// Whether to use LLM for classification when rules are uncertain.
    /// </summary>
    public bool UseLlmFallback { get; init; } = true;

    /// <summary>
    /// Default salience for new memories.
    /// </summary>
    public float DefaultSalience { get; init; } = 0.5f;
}
```

---

## 5. Implementation Logic

**Rule-Based Classification:**
```csharp
public class RuleBasedClassifier
{
    private static readonly Dictionary<MemoryType, (string[] Patterns, float BaseWeight)> TypePatterns = new()
    {
        [MemoryType.Semantic] = (new[]
        {
            @"\b(is|are|uses|prefers|requires|always|never|has|means|contains)\b",
            @"^the\s+\w+\s+(is|uses|requires)",
            @"\b(fact|truth|reality|definition)\b"
        }, 0.8f),

        [MemoryType.Episodic] = (new[]
        {
            @"\b(yesterday|last\s+week|on\s+\w+\s+\d+|when\s+we|that\s+time)\b",
            @"\b(remember\s+when|earlier|ago|back\s+when|that\s+day)\b",
            @"\b(happened|occurred|took\s+place)\b"
        }, 0.85f),

        [MemoryType.Procedural] = (new[]
        {
            @"\b(to\s+do|how\s+to|steps\s+to|in\s+order\s+to)\b",
            @"^\s*(first|then|next|finally|step\s+\d+)",
            @"\b(run|execute|install|configure|deploy)\b.*\b(command|script|tool)\b"
        }, 0.85f)
    };

    public TypeClassification Classify(string content)
    {
        var scores = new Dictionary<MemoryType, float>();
        var contentLower = content.ToLowerInvariant();

        foreach (var (type, (patterns, baseWeight)) in TypePatterns)
        {
            var matchCount = patterns.Count(p => Regex.IsMatch(contentLower, p));
            var matchRatio = (float)matchCount / patterns.Length;
            scores[type] = matchRatio * baseWeight;
        }

        var bestMatch = scores.OrderByDescending(kv => kv.Value).First();

        if (bestMatch.Value >= 0.5f)
        {
            return new TypeClassification(
                bestMatch.Key,
                bestMatch.Value,
                ClassificationMethod.RuleBased,
                $"Matched {(int)(bestMatch.Value * 100)}% of {bestMatch.Key} patterns");
        }

        // Low confidence, return Semantic as default with low confidence
        return new TypeClassification(
            MemoryType.Semantic,
            0.3f,
            ClassificationMethod.RuleBased,
            "No strong pattern matches; defaulting to Semantic");
    }
}
```

**LLM-Based Classification:**
```csharp
public class LlmClassifier
{
    private readonly IChatCompletionService _llm;

    private const string ClassificationPrompt = @"
Classify the following content into one of these memory types:

1. SEMANTIC: Facts, preferences, concepts, definitions
   Examples: ""The project uses PostgreSQL"", ""User prefers dark mode""

2. EPISODIC: Specific events, conversations, incidents with temporal context
   Examples: ""On January 15th, we fixed the auth bug"", ""Last week's deployment failed""

3. PROCEDURAL: How-to knowledge, workflows, step-by-step processes
   Examples: ""To deploy, run 'make prod'"", ""First compile, then test, then deploy""

Content to classify:
{content}

Respond with JSON: {""type"": ""semantic|episodic|procedural"", ""confidence"": 0.0-1.0, ""rationale"": ""brief explanation""}";

    public async Task<TypeClassification> ClassifyAsync(string content, CancellationToken ct)
    {
        var prompt = ClassificationPrompt.Replace("{content}", content);
        var response = await _llm.CompleteAsync(prompt, ct);

        var result = JsonSerializer.Deserialize<LlmClassificationResult>(response);

        var type = result.Type.ToLowerInvariant() switch
        {
            "semantic" => MemoryType.Semantic,
            "episodic" => MemoryType.Episodic,
            "procedural" => MemoryType.Procedural,
            _ => MemoryType.Semantic
        };

        return new TypeClassification(
            type,
            result.Confidence,
            ClassificationMethod.LlmBased,
            result.Rationale);
    }
}
```

**Memory Encoder Implementation:**
```csharp
public class MemoryEncoder : IMemoryEncoder
{
    private readonly IEmbeddingService _embeddingService;
    private readonly RuleBasedClassifier _ruleClassifier;
    private readonly LlmClassifier _llmClassifier;
    private readonly MemoryEncoderOptions _options;
    private readonly ILogger<MemoryEncoder> _logger;

    public async Task<Memory> EncodeAsync(
        string content,
        MemoryContext context,
        CancellationToken ct)
    {
        // 1. Classify the content type
        var classification = await ClassifyTypeAsync(content, ct);

        _logger.LogInformation(
            "[MEM:ENCODE] Classified content as {Type} with {Confidence}% confidence via {Method}",
            classification.Type,
            (int)(classification.Confidence * 100),
            classification.Method);

        // 2. Encode with the classified type
        return await EncodeWithTypeAsync(content, classification.Type, context, ct);
    }

    public async Task<Memory> EncodeWithTypeAsync(
        string content,
        MemoryType type,
        MemoryContext context,
        CancellationToken ct)
    {
        // 1. Truncate or summarize if needed
        var processedContent = content.Length > _options.MaxContentLength
            ? await SummarizeContentAsync(content, ct)
            : content;

        // 2. Generate embedding
        var embedding = await _embeddingService.EmbedAsync(processedContent, ct);

        // 3. Build memory
        var memory = new MemoryBuilder()
            .OfType(type)
            .WithContent(processedContent)
            .WithEmbedding(embedding)
            .FromContext(context)
            .WithSalience(_options.DefaultSalience)
            .Build();

        _logger.LogInformation(
            "[MEM:ENCODE] Encoded {Type} memory with {ContentLength} chars",
            type, processedContent.Length);

        return memory;
    }

    public async Task<TypeClassification> ClassifyTypeAsync(
        string content,
        CancellationToken ct)
    {
        // Try rule-based first
        var ruleResult = _ruleClassifier.Classify(content);

        if (ruleResult.Confidence >= _options.RuleConfidenceThreshold)
        {
            return ruleResult;
        }

        // Fall back to LLM if enabled and confidence is low
        if (_options.UseLlmFallback)
        {
            return await _llmClassifier.ClassifyAsync(content, ct);
        }

        return ruleResult;
    }

    public async Task<IReadOnlyList<Memory>> EncodeBatchAsync(
        IReadOnlyList<EncodingRequest> items,
        CancellationToken ct)
    {
        // Generate all embeddings in batch
        var contents = items.Select(i => i.Content).ToList();
        var embeddings = await _embeddingService.EmbedBatchAsync(contents, ct);

        var memories = new List<Memory>();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var type = item.ExplicitType ?? (await ClassifyTypeAsync(item.Content, ct)).Type;

            var memory = new MemoryBuilder()
                .OfType(type)
                .WithContent(item.Content)
                .WithEmbedding(embeddings[i])
                .FromContext(item.Context)
                .WithSalience(_options.DefaultSalience)
                .Build();

            memories.Add(memory);
        }

        _logger.LogInformation(
            "[MEM:ENCODE] Batch encoded {Count} memories",
            memories.Count);

        return memories;
    }

    private async Task<string> SummarizeContentAsync(string content, CancellationToken ct)
    {
        // Use compression summarizer to reduce content
        var summary = await _llmClassifier._llm.CompleteAsync(
            $"Summarize this in under 500 words, preserving key facts:\n\n{content}",
            ct);

        return summary;
    }
}
```

---

## 6. Observability & Logging

*   **Metric:** `Agents.Memory.Encoder.Encoded` (Counter by Type)
*   **Metric:** `Agents.Memory.Encoder.Classifications` (Counter by Method)
*   **Metric:** `Agents.Memory.Encoder.Latency` (Histogram)
*   **Log (Info):** `[MEM:ENCODE] Classified content as {Type} with {Confidence}% confidence via {Method}`
*   **Log (Info):** `[MEM:ENCODE] Encoded {Type} memory with {ContentLength} chars`
*   **Log (Info):** `[MEM:ENCODE] Batch encoded {Count} memories`

---

## 7. Acceptance Criteria (QA)

1.  **[Classification]** Rule-based classifier SHALL correctly identify semantic patterns.
2.  **[Classification]** Episodic content with temporal markers SHALL classify as Episodic.
3.  **[Classification]** Procedural content with how-to patterns SHALL classify as Procedural.
4.  **[Fallback]** Low-confidence rule results SHALL trigger LLM classification.
5.  **[Encoding]** `EncodeAsync` SHALL generate valid embeddings.
6.  **[Batch]** `EncodeBatchAsync` SHALL process all items efficiently.
7.  **[Truncation]** Content exceeding max length SHALL be summarized.

---

## 8. Test Scenarios

```gherkin
Scenario: Classify semantic content
    Given content "The project uses PostgreSQL for data storage"
    When ClassifyTypeAsync is called
    Then Type SHALL be Semantic
    And Confidence SHALL be >= 0.7

Scenario: Classify episodic content
    Given content "Last week we debugged the authentication module"
    When ClassifyTypeAsync is called
    Then Type SHALL be Episodic
    And Method SHALL be RuleBased

Scenario: Classify procedural content
    Given content "To deploy, first run 'npm build', then 'npm deploy'"
    When ClassifyTypeAsync is called
    Then Type SHALL be Procedural

Scenario: LLM fallback for ambiguous content
    Given content "Important information about the system"
    And RuleConfidenceThreshold is 0.7
    When ClassifyTypeAsync is called
    And rule confidence is below threshold
    Then LLM classification SHALL be invoked

Scenario: Batch encoding
    Given 10 content items with contexts
    When EncodeBatchAsync is called
    Then 10 memories SHALL be returned
    And each SHALL have valid embedding
```

