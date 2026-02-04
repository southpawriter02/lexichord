# LCS-DES-055-KG-h: LLM Fallback

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-055-KG-h |
| **Feature ID** | KG-055h |
| **Feature Name** | LLM Fallback |
| **Target Version** | v0.5.5h |
| **Module Scope** | `Lexichord.Nlu.EntityLinking.LLM` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | Teams (full), Enterprise (full) |
| **Feature Gate Key** | `knowledge.linking.llm.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

When the Entity Linker has multiple candidates with similar scores, traditional scoring may not be sufficient to determine the correct link. The **LLM Fallback** service uses large language models to perform context-aware disambiguation by analyzing the mention in its surrounding context.

### 2.2 The Proposed Solution

Implement an LLM-based disambiguation service that:

- Constructs prompts with mention, context, and candidate descriptions
- Calls the LLM gateway for disambiguation decisions
- Parses LLM responses to extract selected candidates
- Batches requests for cost efficiency
- Caches disambiguation decisions for similar contexts
- Tracks usage for cost monitoring

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- v0.5.5g: `IEntityLinkingService` — Calls fallback for ambiguous cases
- v0.6.1: `ILLMGateway` — LLM API access

**NuGet Packages:**
- `Microsoft.Extensions.Caching.Memory` — Response caching
- `Polly` — Retry policies

### 3.2 Module Placement

```
Lexichord.Nlu/
├── EntityLinking/
│   └── LLM/
│       ├── ILLMFallback.cs
│       ├── LLMFallbackService.cs
│       ├── DisambiguationResult.cs
│       ├── DisambiguationPromptBuilder.cs
│       ├── LLMResponseParser.cs
│       └── LLMFallbackOptions.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Only load for Teams+
- **Fallback Experience:** WriterPro: disabled, falls back to human review; Teams+: enabled

---

## 4. Data Contract (The API)

### 4.1 Core Interfaces

```csharp
namespace Lexichord.Nlu.EntityLinking.LLM;

/// <summary>
/// LLM-based disambiguation fallback for entity linking.
/// </summary>
public interface ILLMFallback
{
    /// <summary>
    /// Disambiguates between candidates using LLM.
    /// </summary>
    /// <param name="mention">The entity mention.</param>
    /// <param name="candidates">Scored candidates to choose from.</param>
    /// <param name="context">Linking context.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Disambiguation result with selected candidate.</returns>
    Task<DisambiguationResult> DisambiguateAsync(
        EntityMention mention,
        IReadOnlyList<ScoredCandidate> candidates,
        LinkingContext context,
        CancellationToken ct = default);

    /// <summary>
    /// Disambiguates multiple mentions in batch (for cost efficiency).
    /// </summary>
    Task<IReadOnlyList<DisambiguationResult>> DisambiguateBatchAsync(
        IReadOnlyList<DisambiguationRequest> requests,
        CancellationToken ct = default);

    /// <summary>
    /// Gets usage statistics.
    /// </summary>
    LLMFallbackStats GetStats();
}

/// <summary>
/// Request for batch disambiguation.
/// </summary>
public record DisambiguationRequest
{
    public required EntityMention Mention { get; init; }
    public required IReadOnlyList<ScoredCandidate> Candidates { get; init; }
    public required LinkingContext Context { get; init; }
}
```

### 4.2 DisambiguationResult

```csharp
namespace Lexichord.Nlu.EntityLinking.LLM;

/// <summary>
/// Result of LLM disambiguation.
/// </summary>
public record DisambiguationResult
{
    /// <summary>Original mention.</summary>
    public required EntityMention Mention { get; init; }

    /// <summary>Selected candidate (null if LLM couldn't decide).</summary>
    public ScoredCandidate? SelectedCandidate { get; init; }

    /// <summary>Whether disambiguation succeeded.</summary>
    public bool IsResolved => SelectedCandidate != null;

    /// <summary>Confidence in the selection (from LLM).</summary>
    public float Confidence { get; init; }

    /// <summary>LLM's reasoning for the selection.</summary>
    public string? Reasoning { get; init; }

    /// <summary>Whether result came from cache.</summary>
    public bool FromCache { get; init; }

    /// <summary>Token usage for this request.</summary>
    public TokenUsage? TokenUsage { get; init; }

    /// <summary>Processing duration.</summary>
    public TimeSpan Duration { get; init; }

    /// <summary>Error message if disambiguation failed.</summary>
    public string? Error { get; init; }

    /// <summary>Creates an unresolved result.</summary>
    public static DisambiguationResult Unresolved(EntityMention mention, string reason) =>
        new()
        {
            Mention = mention,
            SelectedCandidate = null,
            Error = reason
        };
}

/// <summary>
/// Token usage tracking.
/// </summary>
public record TokenUsage
{
    public int PromptTokens { get; init; }
    public int CompletionTokens { get; init; }
    public int TotalTokens => PromptTokens + CompletionTokens;
    public decimal EstimatedCost { get; init; }
}

/// <summary>
/// LLM fallback usage statistics.
/// </summary>
public record LLMFallbackStats
{
    public int TotalRequests { get; init; }
    public int ResolvedCount { get; init; }
    public int UnresolvedCount { get; init; }
    public int CacheHits { get; init; }
    public int TotalTokensUsed { get; init; }
    public decimal TotalCost { get; init; }
    public float AverageConfidence { get; init; }
    public TimeSpan AverageLatency { get; init; }
}
```

### 4.3 Options

```csharp
namespace Lexichord.Nlu.EntityLinking.LLM;

/// <summary>
/// Configuration options for LLM fallback.
/// </summary>
public record LLMFallbackOptions
{
    /// <summary>LLM model to use.</summary>
    public string Model { get; init; } = "claude-3-haiku-20240307";

    /// <summary>Maximum tokens for response.</summary>
    public int MaxTokens { get; init; } = 256;

    /// <summary>Temperature for generation (lower = more deterministic).</summary>
    public float Temperature { get; init; } = 0.1f;

    /// <summary>Maximum candidates to include in prompt.</summary>
    public int MaxCandidatesInPrompt { get; init; } = 5;

    /// <summary>Context window size around mention.</summary>
    public int ContextWindowChars { get; init; } = 500;

    /// <summary>Whether to cache responses.</summary>
    public bool EnableCaching { get; init; } = true;

    /// <summary>Cache duration.</summary>
    public TimeSpan CacheDuration { get; init; } = TimeSpan.FromHours(24);

    /// <summary>Maximum batch size.</summary>
    public int MaxBatchSize { get; init; } = 10;

    /// <summary>Timeout per request.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>Cost per 1K input tokens.</summary>
    public decimal InputTokenCost { get; init; } = 0.00025m;

    /// <summary>Cost per 1K output tokens.</summary>
    public decimal OutputTokenCost { get; init; } = 0.00125m;
}
```

---

## 5. Implementation Logic

### 5.1 LLMFallbackService

```csharp
namespace Lexichord.Nlu.EntityLinking.LLM;

/// <summary>
/// LLM-based disambiguation service.
/// </summary>
public class LLMFallbackService : ILLMFallback
{
    private readonly ILLMGateway _llmGateway;
    private readonly IMemoryCache _cache;
    private readonly DisambiguationPromptBuilder _promptBuilder;
    private readonly LLMResponseParser _responseParser;
    private readonly LLMFallbackOptions _options;
    private readonly ILogger<LLMFallbackService> _logger;
    private readonly LLMFallbackStatsTracker _stats = new();

    public async Task<DisambiguationResult> DisambiguateAsync(
        EntityMention mention,
        IReadOnlyList<ScoredCandidate> candidates,
        LinkingContext context,
        CancellationToken ct = default)
    {
        var sw = Stopwatch.StartNew();

        // Check cache first
        var cacheKey = ComputeCacheKey(mention, candidates);
        if (_options.EnableCaching && _cache.TryGetValue(cacheKey, out DisambiguationResult? cached))
        {
            _stats.RecordCacheHit();
            return cached! with { FromCache = true };
        }

        try
        {
            // Build prompt
            var prompt = _promptBuilder.Build(mention, candidates, context);

            // Call LLM
            var llmResponse = await _llmGateway.GenerateAsync(new LLMRequest
            {
                Model = _options.Model,
                Messages = new[]
                {
                    new LLMMessage { Role = "user", Content = prompt }
                },
                MaxTokens = _options.MaxTokens,
                Temperature = _options.Temperature
            }, ct);

            // Parse response
            var parsed = _responseParser.Parse(llmResponse.Content, candidates);

            sw.Stop();

            var result = new DisambiguationResult
            {
                Mention = mention,
                SelectedCandidate = parsed.SelectedCandidate,
                Confidence = parsed.Confidence,
                Reasoning = parsed.Reasoning,
                TokenUsage = new TokenUsage
                {
                    PromptTokens = llmResponse.Usage?.PromptTokens ?? 0,
                    CompletionTokens = llmResponse.Usage?.CompletionTokens ?? 0,
                    EstimatedCost = CalculateCost(llmResponse.Usage)
                },
                Duration = sw.Elapsed
            };

            // Cache successful results
            if (_options.EnableCaching && result.IsResolved)
            {
                _cache.Set(cacheKey, result, _options.CacheDuration);
            }

            _stats.RecordRequest(result);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM disambiguation failed for mention '{Mention}'", mention.Value);
            _stats.RecordError();

            return DisambiguationResult.Unresolved(mention, ex.Message);
        }
    }

    public async Task<IReadOnlyList<DisambiguationResult>> DisambiguateBatchAsync(
        IReadOnlyList<DisambiguationRequest> requests,
        CancellationToken ct = default)
    {
        var results = new List<DisambiguationResult>();

        // Process in batches
        foreach (var batch in requests.Chunk(_options.MaxBatchSize))
        {
            var tasks = batch.Select(r =>
                DisambiguateAsync(r.Mention, r.Candidates, r.Context, ct));

            var batchResults = await Task.WhenAll(tasks);
            results.AddRange(batchResults);
        }

        return results;
    }

    private string ComputeCacheKey(EntityMention mention, IReadOnlyList<ScoredCandidate> candidates)
    {
        var candidateIds = string.Join(",", candidates
            .OrderBy(c => c.Candidate.EntityId)
            .Select(c => c.Candidate.EntityId));

        var contextHash = mention.SurroundingContext?.GetHashCode() ?? 0;

        return $"llm:{mention.NormalizedValue}:{mention.EntityType}:{candidateIds}:{contextHash}";
    }

    private decimal CalculateCost(LLMUsage? usage)
    {
        if (usage == null) return 0;

        return (usage.PromptTokens / 1000m * _options.InputTokenCost)
             + (usage.CompletionTokens / 1000m * _options.OutputTokenCost);
    }

    public LLMFallbackStats GetStats() => _stats.GetSnapshot();
}
```

### 5.2 Prompt Builder

```csharp
namespace Lexichord.Nlu.EntityLinking.LLM;

/// <summary>
/// Builds disambiguation prompts for the LLM.
/// </summary>
public class DisambiguationPromptBuilder
{
    private readonly LLMFallbackOptions _options;

    private const string PromptTemplate = """
        You are an entity disambiguation system for technical documentation. Given a mention in context and candidate entities, determine which entity the mention refers to.

        ## Mention
        Text: "{mention}"
        Type: {entity_type}

        ## Context
        {context}

        ## Candidates
        {candidates}

        ## Instructions
        1. Analyze the mention in its surrounding context
        2. Compare with each candidate's name and properties
        3. Select the most likely match based on semantic meaning
        4. If no candidate clearly matches, respond with "NONE"

        ## Response Format
        Respond in this exact format:
        SELECTION: [candidate number or NONE]
        CONFIDENCE: [high/medium/low]
        REASONING: [brief explanation]
        """;

    public string Build(
        EntityMention mention,
        IReadOnlyList<ScoredCandidate> candidates,
        LinkingContext context)
    {
        var candidateText = BuildCandidateList(candidates);
        var contextText = ExtractContext(mention, context);

        return PromptTemplate
            .Replace("{mention}", mention.Value)
            .Replace("{entity_type}", mention.EntityType)
            .Replace("{context}", contextText)
            .Replace("{candidates}", candidateText);
    }

    private string BuildCandidateList(IReadOnlyList<ScoredCandidate> candidates)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < Math.Min(candidates.Count, _options.MaxCandidatesInPrompt); i++)
        {
            var candidate = candidates[i];
            sb.AppendLine($"{i + 1}. **{candidate.Candidate.EntityName}** (Type: {candidate.Candidate.EntityType})");

            if (candidate.Candidate.Properties != null)
            {
                foreach (var (key, value) in candidate.Candidate.Properties.Take(5))
                {
                    sb.AppendLine($"   - {key}: {value}");
                }
            }

            sb.AppendLine($"   - Score: {candidate.FinalScore:F2}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private string ExtractContext(EntityMention mention, LinkingContext context)
    {
        if (!string.IsNullOrEmpty(mention.SurroundingContext))
        {
            return mention.SurroundingContext;
        }

        if (string.IsNullOrEmpty(context.DocumentText))
        {
            return "[No context available]";
        }

        // Extract context window from document
        var start = Math.Max(0, mention.StartOffset - _options.ContextWindowChars / 2);
        var end = Math.Min(context.DocumentText.Length, mention.EndOffset + _options.ContextWindowChars / 2);
        var window = context.DocumentText[start..end];

        // Mark the mention
        var mentionStart = mention.StartOffset - start;
        var mentionEnd = mention.EndOffset - start;

        return window.Insert(mentionEnd, "]]").Insert(mentionStart, "[[");
    }
}
```

### 5.3 Response Parser

```csharp
namespace Lexichord.Nlu.EntityLinking.LLM;

/// <summary>
/// Parses LLM responses for disambiguation.
/// </summary>
public class LLMResponseParser
{
    private static readonly Regex SelectionRegex = new(
        @"SELECTION:\s*(\d+|NONE)",
        RegexOptions.IgnoreCase);

    private static readonly Regex ConfidenceRegex = new(
        @"CONFIDENCE:\s*(high|medium|low)",
        RegexOptions.IgnoreCase);

    private static readonly Regex ReasoningRegex = new(
        @"REASONING:\s*(.+?)(?=\n\n|\z)",
        RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public ParsedResponse Parse(string response, IReadOnlyList<ScoredCandidate> candidates)
    {
        var result = new ParsedResponse();

        // Extract selection
        var selectionMatch = SelectionRegex.Match(response);
        if (selectionMatch.Success)
        {
            var selection = selectionMatch.Groups[1].Value.ToUpperInvariant();

            if (selection != "NONE" && int.TryParse(selection, out var index))
            {
                // Convert 1-based to 0-based index
                var adjustedIndex = index - 1;
                if (adjustedIndex >= 0 && adjustedIndex < candidates.Count)
                {
                    result.SelectedCandidate = candidates[adjustedIndex];
                }
            }
        }

        // Extract confidence
        var confidenceMatch = ConfidenceRegex.Match(response);
        if (confidenceMatch.Success)
        {
            result.Confidence = confidenceMatch.Groups[1].Value.ToLowerInvariant() switch
            {
                "high" => 0.9f,
                "medium" => 0.7f,
                "low" => 0.5f,
                _ => 0.6f
            };
        }

        // Extract reasoning
        var reasoningMatch = ReasoningRegex.Match(response);
        if (reasoningMatch.Success)
        {
            result.Reasoning = reasoningMatch.Groups[1].Value.Trim();
        }

        return result;
    }

    public record ParsedResponse
    {
        public ScoredCandidate? SelectedCandidate { get; set; }
        public float Confidence { get; set; } = 0.6f;
        public string? Reasoning { get; set; }
    }
}
```

---

## 6. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    LLM Fallback Flow                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌────────────────────────────────────────┐                     │
│  │ Input: Mention + Candidates (similar)  │                     │
│  │ "users" → [GET /users, POST /users]    │                     │
│  └───────────────────┬────────────────────┘                     │
│                      │                                           │
│                      ▼                                           │
│            ┌──────────────────┐                                 │
│            │   Check Cache    │                                 │
│            └────────┬─────────┘                                 │
│                     │                                            │
│         ┌──────────┴──────────┐                                 │
│         │                     │                                  │
│    Cache Hit             Cache Miss                              │
│         │                     │                                  │
│         ▼                     ▼                                  │
│  ┌─────────────┐    ┌──────────────────┐                       │
│  │Return Cached│    │  Build Prompt    │                       │
│  │   Result    │    │                  │                       │
│  └─────────────┘    │  - Mention text  │                       │
│                     │  - Context window│                       │
│                     │  - Candidate list│                       │
│                     └────────┬─────────┘                       │
│                              │                                   │
│                              ▼                                   │
│                     ┌──────────────────┐                        │
│                     │   Call LLM API   │                        │
│                     │  (Claude Haiku)  │                        │
│                     └────────┬─────────┘                        │
│                              │                                   │
│                              ▼                                   │
│                     ┌──────────────────┐                        │
│                     │  Parse Response  │                        │
│                     │                  │                        │
│                     │ SELECTION: 1     │                        │
│                     │ CONFIDENCE: high │                        │
│                     │ REASONING: ...   │                        │
│                     └────────┬─────────┘                        │
│                              │                                   │
│                    ┌─────────┴─────────┐                        │
│                    │                   │                         │
│              Valid Selection      "NONE"                        │
│                    │                   │                         │
│                    ▼                   ▼                         │
│         ┌─────────────────┐  ┌─────────────────┐               │
│         │ Cache + Return  │  │ Return Unresolved│               │
│         │ Selected Entity │  │ (needs review)   │               │
│         └─────────────────┘  └─────────────────┘               │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.5h")]
public class LLMFallbackServiceTests
{
    private readonly Mock<ILLMGateway> _llmGatewayMock;
    private readonly LLMFallbackService _service;

    [Fact]
    public async Task DisambiguateAsync_ValidResponse_ReturnsSelectedCandidate()
    {
        // Arrange
        var mention = CreateMention("users", "Endpoint");
        var candidates = new[]
        {
            CreateScoredCandidate("GET /users", 0.75f),
            CreateScoredCandidate("POST /users", 0.72f)
        };
        var context = new LinkingContext();

        _llmGatewayMock.Setup(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                Content = "SELECTION: 1\nCONFIDENCE: high\nREASONING: GET is typically used for listing."
            });

        // Act
        var result = await _service.DisambiguateAsync(mention, candidates, context);

        // Assert
        result.IsResolved.Should().BeTrue();
        result.SelectedCandidate!.Candidate.EntityName.Should().Be("GET /users");
        result.Confidence.Should().BeGreaterOrEqualTo(0.9f);
    }

    [Fact]
    public async Task DisambiguateAsync_NoneResponse_ReturnsUnresolved()
    {
        // Arrange
        var mention = CreateMention("something", "Unknown");
        var candidates = new[] { CreateScoredCandidate("Entity1", 0.5f) };
        var context = new LinkingContext();

        _llmGatewayMock.Setup(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                Content = "SELECTION: NONE\nCONFIDENCE: low\nREASONING: No clear match."
            });

        // Act
        var result = await _service.DisambiguateAsync(mention, candidates, context);

        // Assert
        result.IsResolved.Should().BeFalse();
        result.SelectedCandidate.Should().BeNull();
    }

    [Fact]
    public async Task DisambiguateAsync_CachedResult_ReturnsCached()
    {
        // Arrange
        var mention = CreateMention("users", "Endpoint");
        var candidates = new[] { CreateScoredCandidate("GET /users", 0.75f) };
        var context = new LinkingContext();

        _llmGatewayMock.Setup(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                Content = "SELECTION: 1\nCONFIDENCE: high\nREASONING: Match."
            });

        // Act - First call
        var result1 = await _service.DisambiguateAsync(mention, candidates, context);
        // Act - Second call (should hit cache)
        var result2 = await _service.DisambiguateAsync(mention, candidates, context);

        // Assert
        result2.FromCache.Should().BeTrue();
        _llmGatewayMock.Verify(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()),
            Times.Once()); // Only called once
    }

    [Fact]
    public async Task DisambiguateAsync_TracksTokenUsage()
    {
        // Arrange
        var mention = CreateMention("test", "Schema");
        var candidates = new[] { CreateScoredCandidate("Test", 0.6f) };
        var context = new LinkingContext();

        _llmGatewayMock.Setup(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                Content = "SELECTION: 1\nCONFIDENCE: medium\nREASONING: Match.",
                Usage = new LLMUsage { PromptTokens = 200, CompletionTokens = 50 }
            });

        // Act
        var result = await _service.DisambiguateAsync(mention, candidates, context);

        // Assert
        result.TokenUsage.Should().NotBeNull();
        result.TokenUsage!.TotalTokens.Should().Be(250);
        result.TokenUsage.EstimatedCost.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task DisambiguateBatchAsync_ProcessesInBatches()
    {
        // Arrange
        var requests = Enumerable.Range(1, 15)
            .Select(i => new DisambiguationRequest
            {
                Mention = CreateMention($"entity{i}", "Schema"),
                Candidates = new[] { CreateScoredCandidate($"Entity{i}", 0.6f) },
                Context = new LinkingContext()
            })
            .ToList();

        _llmGatewayMock.Setup(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LLMResponse
            {
                Content = "SELECTION: 1\nCONFIDENCE: medium\nREASONING: Match."
            });

        // Act
        var results = await _service.DisambiguateBatchAsync(requests);

        // Assert
        results.Should().HaveCount(15);
    }

    [Fact]
    public async Task DisambiguateAsync_LLMError_ReturnsUnresolved()
    {
        // Arrange
        var mention = CreateMention("test", "Schema");
        var candidates = new[] { CreateScoredCandidate("Test", 0.6f) };
        var context = new LinkingContext();

        _llmGatewayMock.Setup(g => g.GenerateAsync(It.IsAny<LLMRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("LLM service unavailable"));

        // Act
        var result = await _service.DisambiguateAsync(mention, candidates, context);

        // Assert
        result.IsResolved.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | LLM correctly disambiguates between similar candidates. |
| 2 | Response parsing extracts selection, confidence, reasoning. |
| 3 | Cache prevents duplicate LLM calls for same input. |
| 4 | Batch processing handles multiple requests efficiently. |
| 5 | Token usage is tracked and cost calculated. |
| 6 | LLM errors are handled gracefully (returns unresolved). |
| 7 | "NONE" responses correctly return unresolved. |
| 8 | Prompt includes relevant context and candidate info. |
| 9 | Statistics tracking is accurate. |
| 10 | Timeout enforcement prevents hanging requests. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `ILLMFallback` interface | [ ] |
| 2 | `DisambiguationResult` record | [ ] |
| 3 | `DisambiguationRequest` record | [ ] |
| 4 | `TokenUsage` record | [ ] |
| 5 | `LLMFallbackStats` record | [ ] |
| 6 | `LLMFallbackOptions` record | [ ] |
| 7 | `LLMFallbackService` implementation | [ ] |
| 8 | `DisambiguationPromptBuilder` implementation | [ ] |
| 9 | `LLMResponseParser` implementation | [ ] |
| 10 | Response caching | [ ] |
| 11 | Batch processing | [ ] |
| 12 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.5h)

- `ILLMFallback` interface for LLM-based disambiguation
- `DisambiguationResult` with confidence and reasoning
- `DisambiguationPromptBuilder` for structured prompts
- `LLMResponseParser` for extracting selections
- Response caching to reduce LLM costs
- Batch disambiguation for multiple mentions
- Token usage tracking and cost estimation
- Configurable model, temperature, and limits
```

---
