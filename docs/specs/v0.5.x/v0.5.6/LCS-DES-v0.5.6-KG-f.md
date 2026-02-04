# LCS-DES-056-KG-f: Sentence Parser

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-056-KG-f |
| **Feature ID** | KG-056f |
| **Feature Name** | Sentence Parser |
| **Target Version** | v0.5.6f |
| **Module Scope** | `Lexichord.Nlu.ClaimExtraction` |
| **Swimlane** | NLU Pipeline |
| **License Tier** | Teams (full), Enterprise (full) |
| **Feature Gate Key** | `knowledge.claims.parser.enabled` |
| **Status** | Implemented |
| **Last Updated** | 2026-01-31 |

---

## 2. Executive Summary

### 2.1 The Requirement

Before claims can be extracted, text must be parsed into structured linguistic representations. The **Sentence Parser** segments text into sentences, performs dependency parsing to identify grammatical relationships, and applies semantic role labeling (SRL) to identify who did what to whom.

### 2.2 The Proposed Solution

Implement a parsing pipeline that:

- Segments documents into sentences using rule-based and ML approaches
- Performs dependency parsing via SpaCy (Python interop)
- Applies semantic role labeling for claim structure identification
- Caches parse results for efficient re-use
- Provides both batch and streaming parsing modes

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

**Upstream Modules:**
- `Lexichord.Nlu.Core` — Base NLU pipeline infrastructure
- v0.5.5e: `IEntityRecognizer` — Entity annotations

**NuGet Packages:**
- `Pythonnet` (4.0+) — Python interop for SpaCy
- `Microsoft.Extensions.Caching.Memory` — Parse caching

### 3.2 Module Placement

```
Lexichord.Nlu/
├── ClaimExtraction/
│   └── Parsing/
│       ├── ISentenceParser.cs
│       ├── ParsedSentence.cs
│       ├── DependencyTree.cs
│       ├── SemanticRole.cs
│       ├── SpacySentenceParser.cs
│       └── SentenceSegmenter.cs
```

### 3.3 Licensing Behavior

- **Load Behavior:** [x] On Feature Toggle — Load with claim extraction
- **Fallback Experience:** WriterPro: no parsing; Teams+: full parsing

---

## 4. Data Contract (The API)

### 4.1 Core Interfaces

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Parsing;

/// <summary>
/// Service for parsing text into linguistic structures.
/// </summary>
public interface ISentenceParser
{
    /// <summary>
    /// Parses text into structured sentences with dependencies.
    /// </summary>
    /// <param name="text">Text to parse.</param>
    /// <param name="options">Parsing options.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Parsed sentences.</returns>
    Task<ParseResult> ParseAsync(
        string text,
        ParseOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Parses a single sentence.
    /// </summary>
    Task<ParsedSentence> ParseSentenceAsync(
        string sentence,
        ParseOptions? options = null,
        CancellationToken ct = default);

    /// <summary>
    /// Gets supported languages.
    /// </summary>
    IReadOnlyList<string> SupportedLanguages { get; }
}

/// <summary>
/// Options for parsing.
/// </summary>
public record ParseOptions
{
    /// <summary>Language code (default: "en").</summary>
    public string Language { get; init; } = "en";

    /// <summary>Whether to perform dependency parsing.</summary>
    public bool IncludeDependencies { get; init; } = true;

    /// <summary>Whether to perform semantic role labeling.</summary>
    public bool IncludeSRL { get; init; } = true;

    /// <summary>Whether to include POS tags.</summary>
    public bool IncludePOS { get; init; } = true;

    /// <summary>Whether to include named entities.</summary>
    public bool IncludeEntities { get; init; } = true;

    /// <summary>Maximum sentence length to process.</summary>
    public int MaxSentenceLength { get; init; } = 500;

    /// <summary>Whether to cache results.</summary>
    public bool UseCache { get; init; } = true;
}
```

### 4.2 ParsedSentence Record

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Parsing;

/// <summary>
/// A sentence with linguistic annotations.
/// </summary>
public record ParsedSentence
{
    /// <summary>
    /// Unique sentence ID.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Original sentence text.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Tokens in the sentence.
    /// </summary>
    public required IReadOnlyList<Token> Tokens { get; init; }

    /// <summary>
    /// Dependency tree root.
    /// </summary>
    public DependencyNode? DependencyRoot { get; init; }

    /// <summary>
    /// All dependency relations.
    /// </summary>
    public IReadOnlyList<DependencyRelation>? Dependencies { get; init; }

    /// <summary>
    /// Semantic roles (for SRL).
    /// </summary>
    public IReadOnlyList<SemanticFrame>? SemanticFrames { get; init; }

    /// <summary>
    /// Named entities found.
    /// </summary>
    public IReadOnlyList<NamedEntity>? Entities { get; init; }

    /// <summary>
    /// Start offset in source document.
    /// </summary>
    public int StartOffset { get; init; }

    /// <summary>
    /// End offset in source document.
    /// </summary>
    public int EndOffset { get; init; }

    /// <summary>
    /// Sentence index in document.
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Gets the root verb of the sentence.
    /// </summary>
    public Token? GetRootVerb()
    {
        return DependencyRoot?.Token.POS == "VERB"
            ? DependencyRoot.Token
            : Dependencies?.FirstOrDefault(d => d.Relation == "ROOT" && d.Dependent.POS == "VERB")?.Dependent;
    }

    /// <summary>
    /// Gets the subject of the root verb.
    /// </summary>
    public Token? GetSubject()
    {
        var root = GetRootVerb();
        if (root == null) return null;

        return Dependencies?.FirstOrDefault(d =>
            d.Head == root &&
            (d.Relation == "nsubj" || d.Relation == "nsubjpass"))?.Dependent;
    }

    /// <summary>
    /// Gets the direct object of the root verb.
    /// </summary>
    public Token? GetDirectObject()
    {
        var root = GetRootVerb();
        if (root == null) return null;

        return Dependencies?.FirstOrDefault(d =>
            d.Head == root && d.Relation == "dobj")?.Dependent;
    }
}

/// <summary>
/// A token (word) in a sentence.
/// </summary>
public record Token
{
    /// <summary>Token text.</summary>
    public required string Text { get; init; }

    /// <summary>Lemma (base form).</summary>
    public string? Lemma { get; init; }

    /// <summary>Part-of-speech tag (e.g., "NOUN", "VERB").</summary>
    public string? POS { get; init; }

    /// <summary>Fine-grained POS tag.</summary>
    public string? Tag { get; init; }

    /// <summary>Token index in sentence.</summary>
    public int Index { get; init; }

    /// <summary>Character start offset.</summary>
    public int StartChar { get; init; }

    /// <summary>Character end offset.</summary>
    public int EndChar { get; init; }

    /// <summary>Whether this is a stop word.</summary>
    public bool IsStopWord { get; init; }

    /// <summary>Whether this is punctuation.</summary>
    public bool IsPunct { get; init; }
}
```

### 4.3 DependencyTree Records

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Parsing;

/// <summary>
/// Node in a dependency tree.
/// </summary>
public record DependencyNode
{
    /// <summary>Token at this node.</summary>
    public required Token Token { get; init; }

    /// <summary>Dependency relation to parent.</summary>
    public string? Relation { get; init; }

    /// <summary>Child nodes.</summary>
    public IReadOnlyList<DependencyNode>? Children { get; init; }

    /// <summary>
    /// Gets all descendants of this node.
    /// </summary>
    public IEnumerable<DependencyNode> GetDescendants()
    {
        if (Children == null) yield break;

        foreach (var child in Children)
        {
            yield return child;
            foreach (var descendant in child.GetDescendants())
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Gets the subtree text.
    /// </summary>
    public string GetSubtreeText()
    {
        var tokens = new List<Token> { Token };
        tokens.AddRange(GetDescendants().Select(n => n.Token));
        return string.Join(" ", tokens.OrderBy(t => t.Index).Select(t => t.Text));
    }
}

/// <summary>
/// A dependency relation between tokens.
/// </summary>
public record DependencyRelation
{
    /// <summary>Head token (governor).</summary>
    public required Token Head { get; init; }

    /// <summary>Dependent token.</summary>
    public required Token Dependent { get; init; }

    /// <summary>Relation type (e.g., "nsubj", "dobj", "prep").</summary>
    public required string Relation { get; init; }
}

/// <summary>
/// Common dependency relation types.
/// </summary>
public static class DependencyRelations
{
    public const string ROOT = "ROOT";
    public const string NSUBJ = "nsubj";      // nominal subject
    public const string NSUBJPASS = "nsubjpass"; // passive subject
    public const string DOBJ = "dobj";        // direct object
    public const string IOBJ = "iobj";        // indirect object
    public const string POBJ = "pobj";        // prepositional object
    public const string PREP = "prep";        // preposition
    public const string AMOD = "amod";        // adjectival modifier
    public const string ADVMOD = "advmod";    // adverbial modifier
    public const string COMPOUND = "compound"; // compound
    public const string CONJ = "conj";        // conjunct
    public const string CC = "cc";            // coordinating conjunction
    public const string PUNCT = "punct";      // punctuation
    public const string AUX = "aux";          // auxiliary
    public const string AUXPASS = "auxpass";  // passive auxiliary
    public const string ATTR = "attr";        // attribute
    public const string RELCL = "relcl";      // relative clause
    public const string XCOMP = "xcomp";      // clausal complement
}
```

### 4.4 SemanticRole Records

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Parsing;

/// <summary>
/// A semantic frame representing a predicate and its arguments.
/// </summary>
public record SemanticFrame
{
    /// <summary>
    /// The predicate (verb/action).
    /// </summary>
    public required Token Predicate { get; init; }

    /// <summary>
    /// Semantic arguments.
    /// </summary>
    public required IReadOnlyList<SemanticArgument> Arguments { get; init; }

    /// <summary>
    /// Gets argument by role.
    /// </summary>
    public SemanticArgument? GetArgument(SemanticRole role)
    {
        return Arguments.FirstOrDefault(a => a.Role == role);
    }

    /// <summary>
    /// Gets the agent (ARG0).
    /// </summary>
    public SemanticArgument? Agent => GetArgument(SemanticRole.ARG0);

    /// <summary>
    /// Gets the patient/theme (ARG1).
    /// </summary>
    public SemanticArgument? Patient => GetArgument(SemanticRole.ARG1);
}

/// <summary>
/// A semantic argument in a frame.
/// </summary>
public record SemanticArgument
{
    /// <summary>Semantic role.</summary>
    public required SemanticRole Role { get; init; }

    /// <summary>Tokens comprising this argument.</summary>
    public required IReadOnlyList<Token> Tokens { get; init; }

    /// <summary>Argument text.</summary>
    public string Text => string.Join(" ", Tokens.Select(t => t.Text));

    /// <summary>Head token of the argument.</summary>
    public Token? Head { get; init; }

    /// <summary>Start character offset.</summary>
    public int StartOffset => Tokens.Min(t => t.StartChar);

    /// <summary>End character offset.</summary>
    public int EndOffset => Tokens.Max(t => t.EndChar);
}

/// <summary>
/// Semantic roles (PropBank-style).
/// </summary>
public enum SemanticRole
{
    /// <summary>Agent (doer of action).</summary>
    ARG0,

    /// <summary>Patient/Theme (thing affected).</summary>
    ARG1,

    /// <summary>Instrument, benefactive, attribute.</summary>
    ARG2,

    /// <summary>Starting point, benefactive, instrument.</summary>
    ARG3,

    /// <summary>Ending point.</summary>
    ARG4,

    /// <summary>Direction.</summary>
    ARGM_DIR,

    /// <summary>Location.</summary>
    ARGM_LOC,

    /// <summary>Manner.</summary>
    ARGM_MNR,

    /// <summary>Temporal.</summary>
    ARGM_TMP,

    /// <summary>Extent.</summary>
    ARGM_EXT,

    /// <summary>Purpose.</summary>
    ARGM_PRP,

    /// <summary>Cause.</summary>
    ARGM_CAU,

    /// <summary>Negation.</summary>
    ARGM_NEG,

    /// <summary>Modal.</summary>
    ARGM_MOD
}
```

---

## 5. Implementation Logic

### 5.1 SpaCy Sentence Parser

```csharp
namespace Lexichord.Nlu.ClaimExtraction.Parsing;

/// <summary>
/// SpaCy-based sentence parser via Python interop.
/// </summary>
public class SpacySentenceParser : ISentenceParser, IDisposable
{
    private readonly PythonEngine _python;
    private readonly Dictionary<string, dynamic> _models = new();
    private readonly IMemoryCache _cache;
    private readonly ILogger<SpacySentenceParser> _logger;

    public IReadOnlyList<string> SupportedLanguages => new[] { "en", "de", "fr", "es" };

    public async Task<ParseResult> ParseAsync(
        string text,
        ParseOptions? options = null,
        CancellationToken ct = default)
    {
        options ??= new ParseOptions();

        return await Task.Run(() =>
        {
            var sentences = new List<ParsedSentence>();
            var sw = Stopwatch.StartNew();

            using (Py.GIL())
            {
                var nlp = GetModel(options.Language);
                dynamic doc = nlp(text);

                int sentIndex = 0;
                foreach (var sent in doc.sents)
                {
                    var parsed = ParseSentenceInternal(sent, sentIndex, options);
                    if (parsed != null)
                    {
                        sentences.Add(parsed);
                    }
                    sentIndex++;
                }
            }

            sw.Stop();

            return new ParseResult
            {
                Sentences = sentences,
                Duration = sw.Elapsed,
                Stats = new ParseStats
                {
                    TotalSentences = sentences.Count,
                    TotalTokens = sentences.Sum(s => s.Tokens.Count),
                    AverageTokensPerSentence = sentences.Any()
                        ? (float)sentences.Average(s => s.Tokens.Count)
                        : 0
                }
            };
        }, ct);
    }

    private ParsedSentence? ParseSentenceInternal(
        dynamic sent,
        int index,
        ParseOptions options)
    {
        string text = sent.text.ToString();

        // Skip if too long
        if (text.Length > options.MaxSentenceLength)
        {
            return null;
        }

        // Check cache
        if (options.UseCache)
        {
            var cacheKey = $"parse:{ComputeHash(text)}";
            if (_cache.TryGetValue(cacheKey, out ParsedSentence? cached))
            {
                return cached;
            }
        }

        // Parse tokens
        var tokens = new List<Token>();
        foreach (var token in sent)
        {
            tokens.Add(new Token
            {
                Text = token.text.ToString(),
                Lemma = token.lemma_.ToString(),
                POS = token.pos_.ToString(),
                Tag = token.tag_.ToString(),
                Index = (int)token.i,
                StartChar = (int)token.idx,
                EndChar = (int)(token.idx + token.__len__()),
                IsStopWord = (bool)token.is_stop,
                IsPunct = (bool)token.is_punct
            });
        }

        // Parse dependencies
        List<DependencyRelation>? dependencies = null;
        DependencyNode? root = null;

        if (options.IncludeDependencies)
        {
            dependencies = new List<DependencyRelation>();
            var tokenDict = tokens.ToDictionary(t => t.Index);

            foreach (var token in sent)
            {
                int headIdx = (int)token.head.i;
                int depIdx = (int)token.i;
                string rel = token.dep_.ToString();

                if (headIdx != depIdx && tokenDict.ContainsKey(headIdx))
                {
                    dependencies.Add(new DependencyRelation
                    {
                        Head = tokenDict[headIdx],
                        Dependent = tokenDict[depIdx],
                        Relation = rel
                    });
                }

                if (rel == "ROOT")
                {
                    root = BuildDependencyTree(tokenDict[depIdx], dependencies, tokenDict);
                }
            }
        }

        // Parse semantic roles (if enabled and available)
        List<SemanticFrame>? frames = null;
        if (options.IncludeSRL)
        {
            frames = ExtractSemanticFrames(sent, tokens);
        }

        var parsed = new ParsedSentence
        {
            Text = text,
            Tokens = tokens,
            Dependencies = dependencies,
            DependencyRoot = root,
            SemanticFrames = frames,
            StartOffset = (int)sent.start_char,
            EndOffset = (int)sent.end_char,
            Index = index
        };

        // Cache result
        if (options.UseCache)
        {
            var cacheKey = $"parse:{ComputeHash(text)}";
            _cache.Set(cacheKey, parsed, TimeSpan.FromMinutes(30));
        }

        return parsed;
    }

    private DependencyNode BuildDependencyTree(
        Token token,
        List<DependencyRelation> relations,
        Dictionary<int, Token> tokenDict)
    {
        var children = relations
            .Where(r => r.Head == token)
            .Select(r => BuildDependencyTree(r.Dependent, relations, tokenDict))
            .ToList();

        var relation = relations.FirstOrDefault(r => r.Dependent == token)?.Relation;

        return new DependencyNode
        {
            Token = token,
            Relation = relation,
            Children = children
        };
    }

    private List<SemanticFrame>? ExtractSemanticFrames(dynamic sent, List<Token> tokens)
    {
        // SpaCy doesn't have built-in SRL, but we can use rule-based extraction
        // based on dependency patterns
        var frames = new List<SemanticFrame>();

        var verbs = tokens.Where(t => t.POS == "VERB" && !t.IsPunct).ToList();

        foreach (var verb in verbs)
        {
            var arguments = new List<SemanticArgument>();

            // Find subject (ARG0)
            // ... (rule-based extraction based on dependencies)

            if (arguments.Any())
            {
                frames.Add(new SemanticFrame
                {
                    Predicate = verb,
                    Arguments = arguments
                });
            }
        }

        return frames.Any() ? frames : null;
    }

    private dynamic GetModel(string language)
    {
        if (_models.TryGetValue(language, out var model))
        {
            return model;
        }

        using (Py.GIL())
        {
            dynamic spacy = Py.Import("spacy");
            var modelName = language switch
            {
                "en" => "en_core_web_sm",
                "de" => "de_core_news_sm",
                "fr" => "fr_core_news_sm",
                "es" => "es_core_news_sm",
                _ => "en_core_web_sm"
            };

            model = spacy.load(modelName);
            _models[language] = model;
            return model;
        }
    }

    private static string ComputeHash(string text)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash)[..16];
    }

    public void Dispose()
    {
        _python?.Shutdown();
    }
}
```

---

## 6. Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                    Sentence Parsing Flow                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│    ┌──────────────────┐                                         │
│    │   Input Text     │                                         │
│    │                  │                                         │
│    │ "The GET /users  │                                         │
│    │ endpoint accepts │                                         │
│    │ a limit param."  │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │  Sentence        │                                         │
│    │  Segmentation    │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │  Tokenization    │                                         │
│    │  + POS Tagging   │                                         │
│    │                  │                                         │
│    │ [The DET]        │                                         │
│    │ [GET PROPN]      │                                         │
│    │ [/users PROPN]   │                                         │
│    │ [endpoint NOUN]  │                                         │
│    │ [accepts VERB]   │                                         │
│    │ [a DET]          │                                         │
│    │ [limit NOUN]     │                                         │
│    │ [param NOUN]     │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │  Dependency      │                                         │
│    │  Parsing         │                                         │
│    │                  │                                         │
│    │        accepts   │                                         │
│    │        /    \    │                                         │
│    │    nsubj   dobj  │                                         │
│    │      |       |   │                                         │
│    │  endpoint  param │                                         │
│    │      |       |   │                                         │
│    │   compound  amod │                                         │
│    │      |       |   │                                         │
│    │   /users   limit │                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │  Semantic Role   │                                         │
│    │  Labeling        │                                         │
│    │                  │                                         │
│    │ PRED: accepts    │                                         │
│    │ ARG0: endpoint   │                                         │
│    │ ARG1: limit param│                                         │
│    └────────┬─────────┘                                         │
│             │                                                    │
│             ▼                                                    │
│    ┌──────────────────┐                                         │
│    │  ParsedSentence  │                                         │
│    │  + Tokens        │                                         │
│    │  + Dependencies  │                                         │
│    │  + SemanticFrames│                                         │
│    └──────────────────┘                                         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## 7. Unit Testing Requirements

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6f")]
public class SentenceParserTests
{
    private readonly ISentenceParser _parser;

    [Fact]
    public async Task ParseAsync_SimpleSentence_ReturnsTokens()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().HaveCount(1);
        result.Sentences[0].Tokens.Should().HaveCount(5); // Including period
    }

    [Fact]
    public async Task ParseAsync_MultipleSentences_SegmentsCorrectly()
    {
        // Arrange
        var text = "First sentence. Second sentence. Third sentence.";

        // Act
        var result = await _parser.ParseAsync(text);

        // Assert
        result.Sentences.Should().HaveCount(3);
    }

    [Fact]
    public async Task ParseAsync_WithDependencies_BuildsTree()
    {
        // Arrange
        var text = "The endpoint accepts a parameter.";
        var options = new ParseOptions { IncludeDependencies = true };

        // Act
        var result = await _parser.ParseAsync(text, options);

        // Assert
        var sentence = result.Sentences[0];
        sentence.Dependencies.Should().NotBeEmpty();
        sentence.DependencyRoot.Should().NotBeNull();
    }

    [Fact]
    public async Task ParsedSentence_GetRootVerb_ReturnsVerb()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var result = await _parser.ParseAsync(text);

        // Act
        var verb = result.Sentences[0].GetRootVerb();

        // Assert
        verb.Should().NotBeNull();
        verb!.Lemma.Should().Be("accept");
    }

    [Fact]
    public async Task ParsedSentence_GetSubject_ReturnsSubject()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var result = await _parser.ParseAsync(text);

        // Act
        var subject = result.Sentences[0].GetSubject();

        // Assert
        subject.Should().NotBeNull();
        subject!.Text.Should().Be("endpoint");
    }

    [Fact]
    public async Task ParsedSentence_GetDirectObject_ReturnsObject()
    {
        // Arrange
        var text = "The endpoint accepts parameters.";
        var result = await _parser.ParseAsync(text);

        // Act
        var dobj = result.Sentences[0].GetDirectObject();

        // Assert
        dobj.Should().NotBeNull();
        dobj!.Text.Should().Be("parameters");
    }

    [Fact]
    public async Task ParseAsync_UseCache_ReturnsCached()
    {
        // Arrange
        var text = "Cached sentence.";
        var options = new ParseOptions { UseCache = true };

        // Act
        var result1 = await _parser.ParseAsync(text, options);
        var result2 = await _parser.ParseAsync(text, options);

        // Assert - same object from cache
        result1.Sentences[0].Id.Should().Be(result2.Sentences[0].Id);
    }

    [Fact]
    public async Task ParseAsync_LongSentence_SkipsIfExceedsMax()
    {
        // Arrange
        var longSentence = new string('a', 600) + ".";
        var options = new ParseOptions { MaxSentenceLength = 500 };

        // Act
        var result = await _parser.ParseAsync(longSentence, options);

        // Assert
        result.Sentences.Should().BeEmpty();
    }
}
```

---

## 8. Acceptance Criteria (QA)

| # | Criterion |
| :- | :-------- |
| 1 | Sentences are correctly segmented. |
| 2 | Tokens include POS tags and lemmas. |
| 3 | Dependency tree correctly identifies relations. |
| 4 | GetRootVerb() returns main verb. |
| 5 | GetSubject() returns nominal subject. |
| 6 | GetDirectObject() returns direct object. |
| 7 | Parse caching works correctly. |
| 8 | Long sentences are skipped per MaxSentenceLength. |
| 9 | Multiple languages supported. |
| 10 | Processing 100 sentences completes in <5 seconds. |

---

## 9. Deliverable Checklist

| # | Deliverable | Status |
| :- | :---------- | :----- |
| 1 | `ISentenceParser` interface | [ ] |
| 2 | `ParsedSentence` record | [ ] |
| 3 | `Token` record | [ ] |
| 4 | `DependencyNode` record | [ ] |
| 5 | `DependencyRelation` record | [ ] |
| 6 | `SemanticFrame` record | [ ] |
| 7 | `SemanticArgument` record | [ ] |
| 8 | `SpacySentenceParser` implementation | [ ] |
| 9 | Parse caching | [ ] |
| 10 | Unit tests | [ ] |

---

## 10. Changelog Entry

```markdown
### Added (v0.5.6f)

- `ISentenceParser` interface for linguistic parsing
- `ParsedSentence` record with tokens and dependencies
- `DependencyNode` and `DependencyRelation` for parse trees
- `SemanticFrame` and `SemanticArgument` for SRL
- `SpacySentenceParser` via Python interop
- Sentence segmentation and tokenization
- Parse result caching
- Multi-language support (en, de, fr, es)
```

---
