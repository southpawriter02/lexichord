# LCS-DES-034b: Design Specification — Passive Voice Detector

## 1. Metadata & Categorization

| Field | Value |
| :--- | :--- |
| **Document ID** | LCS-DES-034b |
| **Feature ID** | STY-034b |
| **Feature Name** | Passive Voice Detection Analyzer |
| **Target Version** | v0.3.4b |
| **Module Scope** | Lexichord.Modules.Style |
| **Swimlane** | Governance |
| **License Tier** | Writer Pro |
| **Feature Gate Key** | `FeatureFlags.Style.VoiceProfiler` |
| **Status** | Draft |
| **Last Updated** | 2026-01-26 |
| **Parent Document** | [LCS-DES-034-INDEX](./LCS-DES-034-INDEX.md) |
| **Scope Breakdown** | [LCS-SBD-034 §3.2](./LCS-SBD-034.md#32-v034b-passive-voice-detector) |

---

## 2. Executive Summary

### 2.1 The Requirement

Technical writers need to identify passive voice constructions that weaken their prose. Simple "find word" searches miss context—"The door is closed" (state) shouldn't be flagged, but "The code was written by the developer" (action) should.

> **Core Need:** Detect passive voice patterns while distinguishing true passives from adjective constructions.

### 2.2 The Proposed Solution

Implement an `IPassiveVoiceDetector` that:

1. Uses regex patterns to identify "to be" + past participle constructions
2. Applies heuristics to distinguish passive verbs from predicate adjectives
3. Returns `PassiveVoiceMatch` records with confidence scores and suggestions
4. Integrates with `ISentenceTokenizer` for sentence-level analysis

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Upstream Dependencies

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `ISentenceTokenizer` | v0.3.3a | Split text into sentences for analysis |
| `SentenceInfo` | v0.3.3a | Sentence position and metadata |

#### 3.1.2 NuGet Packages

None required. Uses `System.Text.RegularExpressions` from BCL.

### 3.2 Licensing Behavior

Inherits from parent feature. The detector itself has no license check; gating occurs at `VoiceAnalysisService` level.

---

## 4. Data Contract (The API)

### 4.1 IPassiveVoiceDetector Interface

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Detects passive voice constructions in text.
/// </summary>
/// <remarks>
/// <para>The detector distinguishes between true passive voice ("was written")
/// and predicate adjectives ("is closed").</para>
/// <para>Detection patterns include:</para>
/// <list type="bullet">
/// <item>"to be" verbs + past participle: was written, is made, are given</item>
/// <item>Modal passive: will be completed, can be done</item>
/// <item>"get" passive: got fired, getting promoted</item>
/// </list>
/// </remarks>
public interface IPassiveVoiceDetector
{
    /// <summary>
    /// Analyzes text and returns all detected passive voice occurrences.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>List of passive voice matches with positions and suggestions.</returns>
    IReadOnlyList<PassiveVoiceMatch> Detect(string text);

    /// <summary>
    /// Checks if a single sentence contains passive voice.
    /// </summary>
    /// <param name="sentence">The sentence to check.</param>
    /// <returns>True if passive voice is detected.</returns>
    bool ContainsPassiveVoice(string sentence);

    /// <summary>
    /// Gets detection statistics for a block of text.
    /// </summary>
    /// <param name="text">The text to analyze.</param>
    /// <returns>Statistics including counts and percentages.</returns>
    PassiveVoiceStats GetStatistics(string text);
}
```

### 4.2 PassiveVoiceMatch Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Information about a detected passive voice construction.
/// </summary>
public record PassiveVoiceMatch
{
    /// <summary>
    /// The complete sentence containing passive voice.
    /// </summary>
    public required string Sentence { get; init; }

    /// <summary>
    /// Start index of the sentence in the source text.
    /// </summary>
    public int StartIndex { get; init; }

    /// <summary>
    /// End index of the sentence in the source text.
    /// </summary>
    public int EndIndex { get; init; }

    /// <summary>
    /// The specific passive construction detected (e.g., "was written").
    /// </summary>
    public required string PassiveConstruction { get; init; }

    /// <summary>
    /// Start index of the passive construction within the sentence.
    /// </summary>
    public int ConstructionStartIndex { get; init; }

    /// <summary>
    /// End index of the passive construction within the sentence.
    /// </summary>
    public int ConstructionEndIndex { get; init; }

    /// <summary>
    /// Suggested active voice alternative, if determinable.
    /// </summary>
    public string? Suggestion { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0) for the detection.
    /// Higher confidence indicates stronger pattern match.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>0.9+ : Clear passive with "by [agent]" phrase</item>
    /// <item>0.7-0.9 : Standard passive pattern</item>
    /// <item>0.5-0.7 : Possible passive, may be adjective</item>
    /// <item>&lt;0.5 : Low confidence, likely adjective</item>
    /// </list>
    /// </remarks>
    public double Confidence { get; init; }

    /// <summary>
    /// The type of passive construction detected.
    /// </summary>
    public PassiveType Type { get; init; }
}

/// <summary>
/// Types of passive voice constructions.
/// </summary>
public enum PassiveType
{
    /// <summary>"to be" + past participle: was written, is made</summary>
    ToBe,

    /// <summary>Modal + be + past participle: will be completed</summary>
    Modal,

    /// <summary>"get" passive: got fired, getting promoted</summary>
    Get,

    /// <summary>Progressive passive: is being reviewed</summary>
    Progressive
}
```

### 4.3 PassiveVoiceStats Record

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Statistics about passive voice usage in a text.
/// </summary>
public record PassiveVoiceStats
{
    /// <summary>Total number of sentences analyzed.</summary>
    public int TotalSentences { get; init; }

    /// <summary>Number of sentences containing passive voice.</summary>
    public int PassiveSentences { get; init; }

    /// <summary>Percentage of sentences with passive voice.</summary>
    public double PassivePercentage =>
        TotalSentences > 0 ? 100.0 * PassiveSentences / TotalSentences : 0;

    /// <summary>Individual passive matches.</summary>
    public IReadOnlyList<PassiveVoiceMatch> Matches { get; init; } = [];
}
```

---

## 5. Implementation Logic

### 5.1 Detection Patterns

```csharp
/// <summary>
/// Regex patterns for passive voice detection.
/// </summary>
internal static class PassivePatterns
{
    /// <summary>
    /// "to be" verbs (all conjugations).
    /// </summary>
    public const string ToBeVerbs = @"am|is|are|was|were|be|been|being";

    /// <summary>
    /// Modal verbs that can precede passive constructions.
    /// </summary>
    public const string Modals = @"will|would|shall|should|can|could|may|might|must";

    /// <summary>
    /// Regular past participles ending in -ed.
    /// </summary>
    public const string RegularParticiple = @"\w+ed";

    /// <summary>
    /// Pattern: "to be" + past participle
    /// Matches: was written, is made, are given, were taken
    /// </summary>
    public static readonly Regex ToBePassive = new(
        $@"\b({ToBeVerbs})\s+({RegularParticiple}|{IrregularParticiples})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Pattern: Modal + be + past participle
    /// Matches: will be completed, can be done, must be finished
    /// </summary>
    public static readonly Regex ModalPassive = new(
        $@"\b({Modals})\s+be\s+({RegularParticiple}|{IrregularParticiples})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Pattern: Progressive passive with "being"
    /// Matches: is being reviewed, was being discussed
    /// </summary>
    public static readonly Regex ProgressivePassive = new(
        $@"\b({ToBeVerbs})\s+being\s+({RegularParticiple}|{IrregularParticiples})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Pattern: "get" passive
    /// Matches: got fired, getting promoted, get hired
    /// </summary>
    public static readonly Regex GetPassive = new(
        $@"\b(get|gets|got|gotten|getting)\s+({RegularParticiple}|{IrregularParticiples})\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Pattern: "by [agent]" confirmation
    /// Matches: by the team, by a developer, by an expert
    /// </summary>
    public static readonly Regex ByAgent = new(
        @"\bby\s+(the|a|an|my|our|their|his|her|its|\w+)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Irregular past participles (common verbs).
    /// </summary>
    public const string IrregularParticiples =
        @"done|made|given|taken|seen|known|found|told|thought|become|begun|" +
        @"broken|chosen|come|drawn|driven|eaten|fallen|flown|forgotten|frozen|" +
        @"gotten|gone|grown|hidden|held|hurt|kept|laid|led|left|lent|let|lain|" +
        @"lit|lost|meant|met|paid|put|quit|read|ridden|risen|run|said|sent|set|" +
        @"shaken|shone|shot|shown|shut|sung|sunk|sat|slept|slid|spoken|spent|" +
        @"spun|spread|stood|stolen|struck|sworn|swept|swum|swung|taught|torn|" +
        @"thrown|understood|woken|worn|won|withdrawn|written";
}
```

### 5.2 Adjective Disambiguation

```text
PROBLEM: "The door is closed" vs "The code was written"
- First is a STATE (adjective) — door is currently in closed state
- Second is an ACTION (passive) — someone wrote the code

DISAMBIGUATION HEURISTICS:

1. "by [agent]" Test
   - If sentence contains "by the/a/an [noun]" → PASSIVE
   - "The code was written by the developer" → PASSIVE
   - "The door is closed" (no "by") → AMBIGUOUS

2. Progressive Form Test
   - If "being + participle" → PASSIVE (ongoing action)
   - "The code is being reviewed" → PASSIVE
   - "The door is being closed" → PASSIVE (not just state)

3. Common State Adjectives
   - Certain participles are commonly used as state adjectives:
   - closed, open, finished, done, tired, bored, excited, interested,
     worried, confused, pleased, satisfied, disappointed, concerned,
     involved, married, divorced, broken, lost, stuck, gone
   - If participle in this list AND no "by [agent]" → likely ADJECTIVE

4. Preceding Adverb Test
   - State adjectives often follow "very", "quite", "extremely"
   - "The door is very closed" (ungrammatical for state) → PASSIVE
   - "She is very tired" (grammatical) → ADJECTIVE

5. Context Verbs
   - Verbs like "seems", "appears", "looks", "remains" suggest adjective
   - "The door seems closed" → ADJECTIVE
   - "She looks tired" → ADJECTIVE
```

### 5.3 Detection Flow Diagram

```mermaid
flowchart TD
    A[Input: text string] --> B[Tokenize into sentences]
    B --> C{For each sentence}
    C --> D[Apply ToBePassive pattern]
    D --> E{Match found?}
    E -->|No| F[Apply ModalPassive pattern]
    F --> G{Match found?}
    G -->|No| H[Apply GetPassive pattern]
    H --> I{Match found?}
    I -->|No| J[No passive detected]
    I -->|Yes| K[Check disambiguation]
    E -->|Yes| K
    G -->|Yes| K

    K --> L{Is likely adjective?}
    L -->|Yes| M{Confidence > 0.5?}
    M -->|No| J
    M -->|Yes| N[Add as low confidence]
    L -->|No| O{Has "by agent"?}
    O -->|Yes| P[Add as high confidence]
    O -->|No| Q[Add as medium confidence]

    N --> R[Continue to next sentence]
    P --> R
    Q --> R
    J --> R
    R --> C

    C -->|Done| S[Return matches]
```

### 5.4 Confidence Scoring

```csharp
/// <summary>
/// Calculates confidence score for a passive voice detection.
/// </summary>
private double CalculateConfidence(
    string sentence,
    string participle,
    PassiveType type)
{
    var confidence = 0.7; // Base confidence

    // Boost: "by [agent]" phrase present
    if (PassivePatterns.ByAgent.IsMatch(sentence))
    {
        confidence += 0.25;
    }

    // Boost: Progressive form (is being)
    if (type == PassiveType.Progressive)
    {
        confidence += 0.15;
    }

    // Reduce: Common state adjective
    if (CommonStateAdjectives.Contains(participle))
    {
        confidence -= 0.25;
    }

    // Reduce: State-describing context verbs
    if (HasStateContextVerb(sentence))
    {
        confidence -= 0.2;
    }

    // Boost: Multiple passive constructions in sentence
    var passiveCount = CountPassiveConstructions(sentence);
    if (passiveCount > 1)
    {
        confidence += 0.1;
    }

    return Math.Clamp(confidence, 0.0, 1.0);
}
```

---

## 6. Decision Trees

### 6.1 Is This Passive Voice?

```text
START: "Is this construction passive voice?"
│
├── Does sentence match any passive pattern?
│   ├── ToBePassive: "(to be) + (participle)"
│   ├── ModalPassive: "(modal) be (participle)"
│   ├── ProgressivePassive: "(to be) being (participle)"
│   └── GetPassive: "get/got (participle)"
│
├── NO matches → NOT passive voice
│
├── YES → Check disambiguation:
│   │
│   ├── Is the participle in CommonStateAdjectives?
│   │   └── YES → Reduce confidence by 0.25
│   │
│   ├── Does sentence contain "by [agent]"?
│   │   └── YES → Increase confidence by 0.25
│   │
│   ├── Does sentence contain state verb (seems, appears, looks)?
│   │   └── YES → Reduce confidence by 0.2
│   │
│   ├── Is this progressive passive (being + participle)?
│   │   └── YES → Increase confidence by 0.15
│   │
│   └── Calculate final confidence score
│
├── Confidence < 0.5 → NOT passive (likely adjective)
│
└── Confidence >= 0.5 → IS passive voice
```

---

## 7. UI/UX Specifications

**None for this sub-part.** Results are displayed via the existing violation/squiggly system.

Violation appearance is controlled by profile settings:
- **AllowPassiveVoice = false** → `Severity.Warning` (yellow squiggly)
- **AllowPassiveVoice = true, over threshold** → `Severity.Info` (blue squiggly)

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Analyzing {SentenceCount} sentences for passive voice"` |
| Trace | `"Checking sentence: '{SentencePreview}...'"` |
| Trace | `"Pattern match: {PatternType} - '{Construction}'"` |
| Trace | `"Passive voice detected: '{Construction}' confidence {Confidence:F2}"` |
| Trace | `"Disambiguation: adjective={IsAdjective}, hasAgent={HasAgent}"` |
| Debug | `"Detection completed: {PassiveCount} passive sentences in {ElapsedMs}ms"` |
| Warning | `"Low confidence detection ({Confidence:F2}): '{Sentence}'"` |

---

## 9. Acceptance Criteria

### 9.1 Detection Accuracy

| # | Input Sentence | Expected | Type |
| :--- | :--- | :--- | :--- |
| 1 | "The code was written by the developer." | PASSIVE | ToBe |
| 2 | "The report was submitted yesterday." | PASSIVE | ToBe |
| 3 | "The book has been read by millions." | PASSIVE | ToBe |
| 4 | "The project is being reviewed." | PASSIVE | Progressive |
| 5 | "The cake was eaten." | PASSIVE | ToBe |
| 6 | "The file will be deleted." | PASSIVE | Modal |
| 7 | "She got fired last week." | PASSIVE | Get |
| 8 | "The developer wrote the code." | NOT passive | - |
| 9 | "She submitted the report." | NOT passive | - |
| 10 | "The door is closed." | NOT passive | Adjective |
| 11 | "She seemed tired." | NOT passive | Adjective |
| 12 | "The window appears broken." | NOT passive | Adjective |

### 9.2 Confidence Scoring

| # | Sentence | Expected Confidence |
| :--- | :--- | :--- |
| 1 | "The code was written by the developer." | > 0.9 (has "by") |
| 2 | "The report was submitted." | 0.7 - 0.85 |
| 3 | "The door is closed." | < 0.5 (adjective) |
| 4 | "The code is being reviewed." | > 0.8 (progressive) |

---

## 10. Unit Tests

```csharp
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.4b")]
public class PassiveVoiceDetectorTests
{
    private readonly PassiveVoiceDetector _sut;

    public PassiveVoiceDetectorTests()
    {
        var tokenizer = new SentenceTokenizer();
        _sut = new PassiveVoiceDetector(
            tokenizer,
            NullLogger<PassiveVoiceDetector>.Instance);
    }

    [Theory]
    [InlineData("The code was written by the developer.", true)]
    [InlineData("The report was submitted yesterday.", true)]
    [InlineData("The book has been read by millions.", true)]
    [InlineData("The project is being reviewed.", true)]
    [InlineData("The cake was eaten.", true)]
    [InlineData("The file will be deleted.", true)]
    [InlineData("She got fired last week.", true)]
    public void ContainsPassiveVoice_PassiveSentences_ReturnsTrue(
        string sentence, bool expected)
    {
        var result = _sut.ContainsPassiveVoice(sentence);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("The developer wrote the code.", false)]
    [InlineData("She submitted the report.", false)]
    [InlineData("Millions have read the book.", false)]
    [InlineData("The team is reviewing the project.", false)]
    [InlineData("They ate the cake.", false)]
    public void ContainsPassiveVoice_ActiveSentences_ReturnsFalse(
        string sentence, bool expected)
    {
        var result = _sut.ContainsPassiveVoice(sentence);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("The door is closed.")]
    [InlineData("She seemed tired.")]
    [InlineData("The window appears broken.")]
    [InlineData("He looks worried.")]
    [InlineData("They remain concerned.")]
    public void ContainsPassiveVoice_Adjectives_ReturnsFalse(string sentence)
    {
        var result = _sut.ContainsPassiveVoice(sentence);
        result.Should().BeFalse();
    }

    [Fact]
    public void Detect_WithByAgent_ReturnsHighConfidence()
    {
        var text = "The code was written by the developer.";

        var matches = _sut.Detect(text);

        matches.Should().HaveCount(1);
        matches[0].Confidence.Should().BeGreaterThan(0.9);
        matches[0].PassiveConstruction.Should().Be("was written");
    }

    [Fact]
    public void Detect_ProgressivePassive_ReturnsHighConfidence()
    {
        var text = "The code is being reviewed by the team.";

        var matches = _sut.Detect(text);

        matches.Should().HaveCount(1);
        matches[0].Confidence.Should().BeGreaterThan(0.85);
        matches[0].Type.Should().Be(PassiveType.Progressive);
    }

    [Fact]
    public void Detect_CommonAdjective_ReturnsLowConfidenceOrNone()
    {
        var text = "The door is closed.";

        var matches = _sut.Detect(text);

        // Should either not match or match with low confidence
        if (matches.Count > 0)
        {
            matches[0].Confidence.Should().BeLessThan(0.5);
        }
    }

    [Fact]
    public void Detect_MultipleSentences_ReturnsAllMatches()
    {
        var text = "The code was written. The test was run. The bug was fixed.";

        var matches = _sut.Detect(text);

        matches.Should().HaveCount(3);
    }

    [Fact]
    public void Detect_ReturnsCorrectPositions()
    {
        var text = "The code was written by the developer.";

        var matches = _sut.Detect(text);

        matches[0].StartIndex.Should().Be(0);
        matches[0].EndIndex.Should().Be(text.Length);
        matches[0].Sentence.Should().Be(text);
    }

    [Fact]
    public void Detect_ModalPassive_DetectsCorrectly()
    {
        var text = "The report will be completed tomorrow.";

        var matches = _sut.Detect(text);

        matches.Should().HaveCount(1);
        matches[0].Type.Should().Be(PassiveType.Modal);
        matches[0].PassiveConstruction.Should().Contain("be completed");
    }

    [Fact]
    public void Detect_GetPassive_DetectsCorrectly()
    {
        var text = "She got promoted last month.";

        var matches = _sut.Detect(text);

        matches.Should().HaveCount(1);
        matches[0].Type.Should().Be(PassiveType.Get);
        matches[0].PassiveConstruction.Should().Contain("got promoted");
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectPercentage()
    {
        var text = "The code was written. The developer tested it. " +
                   "The bug was fixed. The team deployed the fix.";

        var stats = _sut.GetStatistics(text);

        stats.TotalSentences.Should().Be(4);
        stats.PassiveSentences.Should().Be(2); // "was written", "was fixed"
        stats.PassivePercentage.Should().Be(50.0);
    }

    [Fact]
    public void Detect_EmptyText_ReturnsEmptyList()
    {
        var matches = _sut.Detect("");
        matches.Should().BeEmpty();
    }

    [Fact]
    public void Detect_NoPassive_ReturnsEmptyList()
    {
        var text = "The developer wrote great code. The team loved it.";

        var matches = _sut.Detect(text);

        matches.Should().BeEmpty();
    }
}
```

---

## 11. Code Example

### 11.1 PassiveVoiceDetector Implementation

```csharp
namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Detects passive voice constructions using pattern matching
/// with adjective disambiguation.
/// </summary>
public class PassiveVoiceDetector : IPassiveVoiceDetector
{
    private readonly ISentenceTokenizer _sentenceTokenizer;
    private readonly ILogger<PassiveVoiceDetector> _logger;

    private static readonly HashSet<string> CommonStateAdjectives = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "closed", "open", "finished", "done", "tired", "bored", "excited",
        "interested", "worried", "confused", "pleased", "satisfied",
        "disappointed", "concerned", "involved", "married", "divorced",
        "broken", "lost", "stuck", "gone", "prepared", "dressed",
        "crowded", "packed", "loaded", "filled", "empty"
    };

    private static readonly Regex StateContextVerbs = new(
        @"\b(seems?|appears?|looks?|remains?|feels?|sounds?)\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PassiveVoiceDetector(
        ISentenceTokenizer sentenceTokenizer,
        ILogger<PassiveVoiceDetector> logger)
    {
        _sentenceTokenizer = sentenceTokenizer;
        _logger = logger;
    }

    public IReadOnlyList<PassiveVoiceMatch> Detect(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<PassiveVoiceMatch>();
        }

        var sentences = _sentenceTokenizer.Tokenize(text);
        var matches = new List<PassiveVoiceMatch>();

        _logger.LogDebug("Analyzing {SentenceCount} sentences for passive voice",
            sentences.Count);

        var stopwatch = Stopwatch.StartNew();

        foreach (var sentence in sentences)
        {
            if (TryDetectPassive(sentence, out var match))
            {
                matches.Add(match);
            }
        }

        stopwatch.Stop();
        _logger.LogDebug(
            "Detection completed: {PassiveCount} passive sentences in {ElapsedMs}ms",
            matches.Count, stopwatch.ElapsedMilliseconds);

        return matches.AsReadOnly();
    }

    public bool ContainsPassiveVoice(string sentence)
    {
        var sentenceInfo = new SentenceInfo(sentence, 0, sentence.Length, 0);
        return TryDetectPassive(sentenceInfo, out var match) && match.Confidence >= 0.5;
    }

    public PassiveVoiceStats GetStatistics(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new PassiveVoiceStats
            {
                TotalSentences = 0,
                PassiveSentences = 0,
                Matches = []
            };
        }

        var sentences = _sentenceTokenizer.Tokenize(text);
        var matches = Detect(text);

        return new PassiveVoiceStats
        {
            TotalSentences = sentences.Count,
            PassiveSentences = matches.Count,
            Matches = matches
        };
    }

    private bool TryDetectPassive(SentenceInfo sentence, out PassiveVoiceMatch match)
    {
        match = default!;

        _logger.LogTrace("Checking sentence: '{SentencePreview}...'",
            sentence.Text.Length > 50
                ? sentence.Text[..50]
                : sentence.Text);

        // Try each pattern in order of specificity
        if (TryMatchPattern(sentence, PassivePatterns.ProgressivePassive,
                PassiveType.Progressive, out match))
        {
            return true;
        }

        if (TryMatchPattern(sentence, PassivePatterns.ModalPassive,
                PassiveType.Modal, out match))
        {
            return true;
        }

        if (TryMatchPattern(sentence, PassivePatterns.ToBePassive,
                PassiveType.ToBe, out match))
        {
            return true;
        }

        if (TryMatchPattern(sentence, PassivePatterns.GetPassive,
                PassiveType.Get, out match))
        {
            return true;
        }

        return false;
    }

    private bool TryMatchPattern(
        SentenceInfo sentence,
        Regex pattern,
        PassiveType type,
        out PassiveVoiceMatch match)
    {
        match = default!;

        var regexMatch = pattern.Match(sentence.Text);
        if (!regexMatch.Success)
        {
            return false;
        }

        var construction = regexMatch.Value;
        var participle = regexMatch.Groups[^1].Value; // Last captured group

        _logger.LogTrace("Pattern match: {PatternType} - '{Construction}'",
            type, construction);

        // Calculate confidence with disambiguation
        var confidence = CalculateConfidence(sentence.Text, participle, type);

        // Skip low-confidence matches (likely adjectives)
        if (confidence < 0.5)
        {
            _logger.LogTrace(
                "Disambiguation: likely adjective (confidence={Confidence:F2})",
                confidence);
            return false;
        }

        _logger.LogTrace(
            "Passive voice detected: '{Construction}' confidence {Confidence:F2}",
            construction, confidence);

        match = new PassiveVoiceMatch
        {
            Sentence = sentence.Text,
            StartIndex = sentence.StartIndex,
            EndIndex = sentence.EndIndex,
            PassiveConstruction = construction,
            ConstructionStartIndex = regexMatch.Index,
            ConstructionEndIndex = regexMatch.Index + regexMatch.Length,
            Confidence = confidence,
            Type = type,
            Suggestion = GenerateSuggestion(sentence.Text, type)
        };

        return true;
    }

    private double CalculateConfidence(
        string sentence,
        string participle,
        PassiveType type)
    {
        var confidence = 0.7; // Base confidence

        var hasAgent = PassivePatterns.ByAgent.IsMatch(sentence);
        var isAdjective = CommonStateAdjectives.Contains(participle);
        var hasStateVerb = StateContextVerbs.IsMatch(sentence);

        _logger.LogTrace(
            "Disambiguation: adjective={IsAdjective}, hasAgent={HasAgent}",
            isAdjective, hasAgent);

        // Boost: "by [agent]" phrase present
        if (hasAgent)
        {
            confidence += 0.25;
        }

        // Boost: Progressive form
        if (type == PassiveType.Progressive)
        {
            confidence += 0.15;
        }

        // Reduce: Common state adjective
        if (isAdjective)
        {
            confidence -= 0.25;
        }

        // Reduce: State-describing context verbs
        if (hasStateVerb)
        {
            confidence -= 0.2;
        }

        // Adjective without agent: very likely adjective use
        if (isAdjective && !hasAgent)
        {
            confidence -= 0.15;
        }

        var finalConfidence = Math.Clamp(confidence, 0.0, 1.0);

        if (finalConfidence < 0.6 && finalConfidence >= 0.5)
        {
            _logger.LogWarning(
                "Low confidence detection ({Confidence:F2}): '{Sentence}'",
                finalConfidence, sentence);
        }

        return finalConfidence;
    }

    private static string GenerateSuggestion(string sentence, PassiveType type)
    {
        if (PassivePatterns.ByAgent.IsMatch(sentence))
        {
            return "Consider rewriting with the agent as the subject. " +
                   "Example: 'The developer wrote the code' instead of " +
                   "'The code was written by the developer'.";
        }

        return type switch
        {
            PassiveType.Progressive =>
                "Consider using active voice for clearer, more direct prose.",
            PassiveType.Modal =>
                "Consider specifying who will perform the action.",
            PassiveType.Get =>
                "Consider rephrasing to clarify who performed the action.",
            _ =>
                "Consider using active voice for stronger, more direct prose."
        };
    }
}
```

---

## 12. DI Registration

```csharp
// In StyleModule.cs
services.AddSingleton<IPassiveVoiceDetector, PassiveVoiceDetector>();
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-26 | Lead Architect | Initial draft |
