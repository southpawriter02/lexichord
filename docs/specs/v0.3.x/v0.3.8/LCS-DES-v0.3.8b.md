# LCS-DES-038b: Design Specification — Readability Test Suite

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `TST-038b` | Sub-part of TST-038 |
| **Feature Name** | `Readability Metrics Accuracy Test Suite` | Algorithm verification tests |
| **Target Version** | `v0.3.8b` | Second sub-part of v0.3.8 |
| **Module Scope** | `Lexichord.Tests.Style` | Test project |
| **Swimlane** | `Governance` | Part of Style vertical |
| **License Tier** | `Core` | Testing available to all |
| **Feature Gate Key** | N/A | No gating for tests |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-26` | |
| **Parent Document** | [LCS-DES-038-INDEX](./LCS-DES-038-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-038 §3.2](./LCS-SBD-038.md#32-v038b-readability-test-suite) | |

---

## 2. Executive Summary

### 2.1 The Requirement

The readability algorithms introduced in v0.3.3 must produce accurate scores that match industry-standard reference implementations. Without verification:

- Flesch-Kincaid Grade Level calculations could silently drift from correct values
- Syllable counting heuristics could miscount, affecting all readability metrics
- Sentence tokenization errors could inflate/deflate sentence counts
- Users would receive inaccurate feedback about their writing's complexity

> **Goal:** Verify that the Flesch-Kincaid Grade Level for the Gettysburg Address matches the known reference value of 11.2 within ±0.1.

### 2.2 The Proposed Solution

Create a "Standard Corpus" of texts with known readability scores and implement a test suite that:

1. Validates all three readability formulas (FK, Fog, FRE) against reference values
2. Tests syllable counting accuracy against known word syllable counts
3. Verifies sentence tokenization handles abbreviations correctly
4. Establishes accuracy tolerances (±0.1 for grade levels, ±1.0 for reading ease)
5. Integrates with CI to prevent accuracy regressions

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `ISentenceTokenizer` | v0.3.3a | Sentence tokenization accuracy |
| `ISyllableCounter` | v0.3.3b | Syllable counting accuracy |
| `IReadabilityService` | v0.3.3c | Readability formula calculations |
| `ReadabilityMetrics` | v0.3.3c | Metrics record verification |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `xunit` | 2.9.x | Test framework |
| `FluentAssertions` | 6.x | Fluent assertions with tolerance |
| `Moq` | 4.x | Mocking dependencies |

### 3.2 Licensing Behavior

No licensing required. Tests run in development/CI environments only.

---

## 4. Data Contract (The API)

### 4.1 Standard Corpus Definition

```csharp
namespace Lexichord.Tests.Style.TestFixtures;

/// <summary>
/// A reference text with known readability scores for validation.
/// </summary>
public record CorpusEntry
{
    /// <summary>Unique identifier for the entry.</summary>
    public required string Id { get; init; }

    /// <summary>Human-readable name.</summary>
    public required string Name { get; init; }

    /// <summary>Source attribution.</summary>
    public string? Source { get; init; }

    /// <summary>The text content to analyze.</summary>
    public required string Text { get; init; }

    /// <summary>Expected readability metrics.</summary>
    public required ExpectedMetrics Expected { get; init; }

    /// <summary>Optional notes about the entry.</summary>
    public string? Notes { get; init; }
}

/// <summary>
/// Expected readability metric values for verification.
/// </summary>
public record ExpectedMetrics
{
    public double FleschKincaidGradeLevel { get; init; }
    public double GunningFogIndex { get; init; }
    public double FleschReadingEase { get; init; }
    public int WordCount { get; init; }
    public int SentenceCount { get; init; }
    public int SyllableCount { get; init; }
    public int? ComplexWordCount { get; init; }
}
```

### 4.2 Test Class Structure

```csharp
namespace Lexichord.Tests.Style.Readability;

/// <summary>
/// Accuracy tests for readability formula calculations.
/// Verifies FK, Fog, and FRE scores match known reference values.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8b")]
public class ReadabilityAccuracyTests
{
    private readonly IReadabilityService _sut;
    private readonly StandardCorpus _corpus;

    public ReadabilityAccuracyTests()
    {
        _sut = new ReadabilityService(
            new SentenceTokenizer(),
            new SyllableCounter(),
            new DocumentTokenizer());
        _corpus = StandardCorpus.Load();
    }

    // Test methods...
}
```

---

## 5. Implementation Logic

### 5.1 Readability Formulas (Reference)

#### 5.1.1 Flesch-Kincaid Grade Level

```text
FK = 0.39 × (words/sentences) + 11.8 × (syllables/words) - 15.59
```

**Interpretation:** U.S. school grade level (0 = Kindergarten, 12 = Senior year)

#### 5.1.2 Gunning Fog Index

```text
GF = 0.4 × ((words/sentences) + 100 × (complex_words/words))

where: complex_words = words with 3+ syllables, excluding:
  - Common suffixes: -es, -ed, -ing
  - Proper nouns (not implemented in basic version)
```

**Interpretation:** Years of formal education required

#### 5.1.3 Flesch Reading Ease

```text
FRE = 206.835 - 1.015 × (words/sentences) - 84.6 × (syllables/words)
```

**Interpretation:** Score 0-100 (higher = easier)

### 5.2 Standard Corpus Entries

```json
{
  "corpus": [
    {
      "id": "gettysburg",
      "name": "Gettysburg Address (opening)",
      "source": "Abraham Lincoln, 1863",
      "text": "Four score and seven years ago our fathers brought forth on this continent, a new nation, conceived in Liberty, and dedicated to the proposition that all men are created equal.",
      "expected": {
        "fleschKincaidGradeLevel": 11.2,
        "gunningFogIndex": 14.1,
        "fleschReadingEase": 63.9,
        "wordCount": 30,
        "sentenceCount": 1,
        "syllableCount": 46
      },
      "notes": "Classic formal 19th century prose"
    },
    {
      "id": "simple-sentence",
      "name": "The cat sat on the mat",
      "source": "Test corpus",
      "text": "The cat sat on the mat.",
      "expected": {
        "fleschKincaidGradeLevel": -0.5,
        "gunningFogIndex": 2.4,
        "fleschReadingEase": 116.1,
        "wordCount": 6,
        "sentenceCount": 1,
        "syllableCount": 6
      },
      "notes": "Extremely simple text, FK can be negative for very simple prose"
    },
    {
      "id": "hemingway",
      "name": "Hemingway excerpt",
      "source": "The Old Man and the Sea, 1952",
      "text": "He was an old man who fished alone in a skiff in the Gulf Stream and he had gone eighty-four days now without taking a fish.",
      "expected": {
        "fleschKincaidGradeLevel": 4.0,
        "gunningFogIndex": 5.8,
        "fleschReadingEase": 92.0,
        "wordCount": 28,
        "sentenceCount": 1,
        "syllableCount": 34
      },
      "notes": "Famous for simple, direct prose"
    },
    {
      "id": "technical-complex",
      "name": "Technical documentation",
      "source": "Test corpus",
      "text": "Implementing sophisticated architectural paradigms requires substantial theoretical knowledge and practical experience with distributed computing infrastructure.",
      "expected": {
        "fleschKincaidGradeLevel": 18.5,
        "gunningFogIndex": 22.0,
        "fleschReadingEase": 8.0,
        "wordCount": 15,
        "sentenceCount": 1,
        "syllableCount": 42,
        "complexWordCount": 8
      },
      "notes": "Deliberately complex for testing upper bounds"
    },
    {
      "id": "multi-sentence",
      "name": "Multi-sentence paragraph",
      "source": "Test corpus",
      "text": "The quick brown fox jumps over the lazy dog. This sentence contains every letter of the alphabet. It is commonly used for testing purposes.",
      "expected": {
        "fleschKincaidGradeLevel": 4.5,
        "gunningFogIndex": 7.2,
        "fleschReadingEase": 75.0,
        "wordCount": 25,
        "sentenceCount": 3,
        "syllableCount": 32
      },
      "notes": "Tests multi-sentence handling"
    }
  ]
}
```

### 5.3 Syllable Count Reference Data

| Word | Syllables | Notes |
| :--- | :--- | :--- |
| "the" | 1 | Common word |
| "cat" | 1 | CVC pattern |
| "queue" | 1 | Silent letters |
| "fire" | 1 | Silent e |
| "table" | 2 | -le ending |
| "water" | 2 | Two syllables |
| "beautiful" | 3 | -ful suffix |
| "animal" | 3 | Three syllables |
| "dictionary" | 4 | Four syllables |
| "documentation" | 5 | -tion suffix |

---

## 6. Test Scenarios

### 6.1 SyllableCounterAccuracyTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8b")]
public class SyllableCounterAccuracyTests
{
    private readonly ISyllableCounter _sut = new SyllableCounter();

    #region Known Syllable Counts

    [Theory]
    [InlineData("the", 1)]
    [InlineData("a", 1)]
    [InlineData("cat", 1)]
    [InlineData("dog", 1)]
    [InlineData("sat", 1)]
    [InlineData("mat", 1)]
    public void CountSyllables_SingleSyllableWords_ReturnsOne(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("table", 2)]
    [InlineData("water", 2)]
    [InlineData("apple", 2)]
    [InlineData("people", 2)]
    [InlineData("little", 2)]
    public void CountSyllables_TwoSyllableWords_ReturnsTwo(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("beautiful", 3)]
    [InlineData("animal", 3)]
    [InlineData("library", 3)]
    [InlineData("separate", 3)]
    [InlineData("family", 3)]
    public void CountSyllables_ThreeSyllableWords_ReturnsThree(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("dictionary", 4)]
    [InlineData("vocabulary", 5)]
    [InlineData("documentation", 5)]
    [InlineData("abbreviation", 5)]
    [InlineData("refrigerator", 5)]
    public void CountSyllables_PolysyllabicWords_ReturnsExpected(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    #endregion

    #region Silent Letter Handling

    [Theory]
    [InlineData("queue", 1)]      // Silent ueue
    [InlineData("fire", 1)]       // Silent e
    [InlineData("like", 1)]       // Silent e
    [InlineData("make", 1)]       // Silent e
    [InlineData("aisle", 1)]      // Silent letters
    public void CountSyllables_SilentLetters_HandlesCorrectly(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    #endregion

    #region Suffix Handling

    [Theory]
    [InlineData("jumped", 1)]     // -ed after consonant
    [InlineData("loaded", 2)]     // -ed after 'd'
    [InlineData("wanted", 2)]     // -ed after 't'
    [InlineData("played", 1)]     // -ed after vowel
    public void CountSyllables_EdSuffix_HandlesCorrectly(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("makes", 1)]      // -es after consonant
    [InlineData("boxes", 2)]      // -es after x (new syllable)
    [InlineData("watches", 2)]    // -es after ch (new syllable)
    [InlineData("misses", 2)]     // -es after s (new syllable)
    public void CountSyllables_EsSuffix_HandlesCorrectly(
        string word, int expected)
    {
        var result = _sut.CountSyllables(word);
        result.Should().Be(expected);
    }

    #endregion

    #region Complex Word Detection

    [Theory]
    [InlineData("beautiful", true)]       // 3 syllables
    [InlineData("understanding", true)]   // 4 syllables
    [InlineData("documentation", true)]   // 5 syllables
    [InlineData("simple", false)]         // 2 syllables
    [InlineData("running", false)]        // 2 syllables
    [InlineData("the", false)]            // 1 syllable
    public void IsComplexWord_ReturnsExpected(string word, bool expected)
    {
        var result = _sut.IsComplexWord(word);
        result.Should().Be(expected);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void CountSyllables_EmptyString_ReturnsOne()
    {
        // Minimum syllable count is 1
        var result = _sut.CountSyllables("");
        result.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void CountSyllables_SingleVowel_ReturnsOne()
    {
        var result = _sut.CountSyllables("a");
        result.Should().Be(1);
    }

    [Fact]
    public void CountSyllables_CaseInsensitive()
    {
        var lower = _sut.CountSyllables("beautiful");
        var upper = _sut.CountSyllables("BEAUTIFUL");
        var mixed = _sut.CountSyllables("Beautiful");

        lower.Should().Be(upper).And.Be(mixed);
    }

    #endregion
}
```

### 6.2 SentenceTokenizerAccuracyTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8b")]
public class SentenceTokenizerAccuracyTests
{
    private readonly ISentenceTokenizer _sut = new SentenceTokenizer();

    #region Basic Sentence Splitting

    [Theory]
    [InlineData("Hello world.", 1)]
    [InlineData("Hello. World.", 2)]
    [InlineData("Hello! World?", 2)]
    [InlineData("One. Two. Three.", 3)]
    public void Tokenize_BasicSentences_ReturnsCorrectCount(
        string text, int expected)
    {
        var result = _sut.Tokenize(text);
        result.Should().HaveCount(expected);
    }

    #endregion

    #region Abbreviation Handling

    [Theory]
    [InlineData("Mr. Smith went home.", 1)]
    [InlineData("Dr. Jones and Mrs. Smith met.", 1)]
    [InlineData("She works at Acme Inc. in New York.", 1)]
    [InlineData("I spoke with Prof. Brown, Ph.D. today.", 1)]
    public void Tokenize_WithAbbreviations_DoesNotSplitFalsely(
        string text, int expected)
    {
        var result = _sut.Tokenize(text);
        result.Should().HaveCount(expected);
    }

    [Theory]
    [InlineData("The U.S.A. is a country. It is large.", 2)]
    [InlineData("See Fig. 1. It shows the data.", 2)]
    [InlineData("Call at 3 p.m. tomorrow. Be ready.", 2)]
    public void Tokenize_AbbreviationsFollowedBySentence_SplitsCorrectly(
        string text, int expected)
    {
        var result = _sut.Tokenize(text);
        result.Should().HaveCount(expected);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Tokenize_Ellipsis_DoesNotSplitOnEachDot()
    {
        var result = _sut.Tokenize("Wait... I have an idea.");
        result.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\n\t")]
    public void Tokenize_EmptyOrWhitespace_ReturnsEmpty(string text)
    {
        var result = _sut.Tokenize(text);
        result.Should().BeEmpty();
    }

    #endregion

    #region Word Count Verification

    [Fact]
    public void Tokenize_ReturnsCorrectWordCounts()
    {
        var result = _sut.Tokenize("The quick brown fox. Jumps over.");

        result.Should().HaveCount(2);
        result[0].WordCount.Should().Be(4);
        result[1].WordCount.Should().Be(2);
    }

    #endregion
}
```

### 6.3 ReadabilityAccuracyTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8b")]
public class ReadabilityAccuracyTests
{
    private readonly IReadabilityService _sut;
    private readonly StandardCorpus _corpus;

    public ReadabilityAccuracyTests()
    {
        _sut = new ReadabilityService(
            new SentenceTokenizer(),
            new SyllableCounter(),
            new DocumentTokenizer());
        _corpus = StandardCorpus.Load();
    }

    #region Flesch-Kincaid Grade Level Tests

    [Fact]
    public void Analyze_GettysburgAddress_FleschKincaidMatchesReference()
    {
        // The Gettysburg Address has a well-documented readability level
        var entry = _corpus.Get("gettysburg");

        var result = _sut.Analyze(entry.Text);

        result.FleschKincaidGradeLevel.Should().BeApproximately(
            entry.Expected.FleschKincaidGradeLevel, 0.1,
            "Flesch-Kincaid must match reference within ±0.1");
    }

    [Fact]
    public void Analyze_SimpleSentence_FleschKincaidIsVeryLow()
    {
        var entry = _corpus.Get("simple-sentence");

        var result = _sut.Analyze(entry.Text);

        result.FleschKincaidGradeLevel.Should().BeLessThan(2.0,
            "Simple sentences should have very low grade levels");
    }

    [Fact]
    public void Analyze_HemingwayExcerpt_FleschKincaidMatchesReference()
    {
        // Hemingway is famous for simple, direct prose
        var entry = _corpus.Get("hemingway");

        var result = _sut.Analyze(entry.Text);

        result.FleschKincaidGradeLevel.Should().BeApproximately(
            entry.Expected.FleschKincaidGradeLevel, 0.5,
            "Hemingway prose should be around 4th grade level");
    }

    [Fact]
    public void Analyze_TechnicalComplex_FleschKincaidIsVeryHigh()
    {
        var entry = _corpus.Get("technical-complex");

        var result = _sut.Analyze(entry.Text);

        result.FleschKincaidGradeLevel.Should().BeGreaterThan(15.0,
            "Technical jargon should have very high grade level");
    }

    #endregion

    #region Gunning Fog Index Tests

    [Fact]
    public void Analyze_GettysburgAddress_GunningFogMatchesReference()
    {
        var entry = _corpus.Get("gettysburg");

        var result = _sut.Analyze(entry.Text);

        result.GunningFogIndex.Should().BeApproximately(
            entry.Expected.GunningFogIndex, 0.2,
            "Gunning Fog must match reference within ±0.2");
    }

    [Fact]
    public void Analyze_SimpleSentence_GunningFogIsVeryLow()
    {
        var entry = _corpus.Get("simple-sentence");

        var result = _sut.Analyze(entry.Text);

        result.GunningFogIndex.Should().BeLessThan(4.0,
            "Simple sentences should have very low Fog index");
    }

    #endregion

    #region Flesch Reading Ease Tests

    [Fact]
    public void Analyze_GettysburgAddress_FleschReadingEaseMatchesReference()
    {
        var entry = _corpus.Get("gettysburg");

        var result = _sut.Analyze(entry.Text);

        result.FleschReadingEase.Should().BeApproximately(
            entry.Expected.FleschReadingEase, 1.0,
            "Flesch Reading Ease must match reference within ±1.0");
    }

    [Fact]
    public void Analyze_SimpleSentence_FleschReadingEaseIsVeryHigh()
    {
        var entry = _corpus.Get("simple-sentence");

        var result = _sut.Analyze(entry.Text);

        result.FleschReadingEase.Should().BeGreaterThan(90.0,
            "Simple sentences should have very high reading ease");
    }

    [Fact]
    public void Analyze_TechnicalComplex_FleschReadingEaseIsVeryLow()
    {
        var entry = _corpus.Get("technical-complex");

        var result = _sut.Analyze(entry.Text);

        result.FleschReadingEase.Should().BeLessThan(30.0,
            "Technical jargon should have very low reading ease");
    }

    #endregion

    #region Word and Sentence Count Tests

    [Theory]
    [InlineData("gettysburg", 30, 1)]
    [InlineData("simple-sentence", 6, 1)]
    [InlineData("multi-sentence", 25, 3)]
    public void Analyze_CountsMatchReference(
        string corpusId, int expectedWords, int expectedSentences)
    {
        var entry = _corpus.Get(corpusId);

        var result = _sut.Analyze(entry.Text);

        result.WordCount.Should().Be(expectedWords);
        result.SentenceCount.Should().Be(expectedSentences);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Analyze_EmptyText_ReturnsEmptyMetrics()
    {
        var result = _sut.Analyze("");

        result.Should().Be(ReadabilityMetrics.Empty);
    }

    [Fact]
    public void Analyze_WhitespaceOnly_ReturnsEmptyMetrics()
    {
        var result = _sut.Analyze("   \n\t  ");

        result.WordCount.Should().Be(0);
        result.SentenceCount.Should().Be(0);
    }

    [Fact]
    public void Analyze_SingleWord_ReturnsMinimumMetrics()
    {
        var result = _sut.Analyze("Hello");

        result.WordCount.Should().Be(1);
        result.SentenceCount.Should().Be(1);
        result.FleschKincaidGradeLevel.Should().BeGreaterThanOrEqualTo(0);
    }

    #endregion

    #region Full Corpus Validation

    [Theory]
    [MemberData(nameof(GetCorpusEntries))]
    public void Analyze_AllCorpusEntries_WithinTolerance(CorpusEntry entry)
    {
        var result = _sut.Analyze(entry.Text);

        result.FleschKincaidGradeLevel.Should().BeApproximately(
            entry.Expected.FleschKincaidGradeLevel, 0.5,
            $"{entry.Name}: FK grade level out of tolerance");

        result.GunningFogIndex.Should().BeApproximately(
            entry.Expected.GunningFogIndex, 0.5,
            $"{entry.Name}: Gunning Fog out of tolerance");

        result.FleschReadingEase.Should().BeApproximately(
            entry.Expected.FleschReadingEase, 2.0,
            $"{entry.Name}: Reading Ease out of tolerance");
    }

    public static IEnumerable<object[]> GetCorpusEntries()
    {
        var corpus = StandardCorpus.Load();
        foreach (var entry in corpus.Entries)
        {
            yield return new object[] { entry };
        }
    }

    #endregion

    #region Interpretation Tests

    [Theory]
    [InlineData(0.5, "Kindergarten")]
    [InlineData(5.0, "Elementary School")]
    [InlineData(8.0, "Middle School")]
    [InlineData(11.0, "High School")]
    [InlineData(14.0, "College")]
    [InlineData(18.0, "Graduate Level")]
    public void GradeLevelInterpretation_ReturnsCorrectDescription(
        double gradeLevel, string expected)
    {
        var metrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = gradeLevel
        };

        metrics.GradeLevelInterpretation.Should().Contain(expected);
    }

    #endregion
}
```

### 6.4 Standard Corpus Loader

```csharp
namespace Lexichord.Tests.Style.TestFixtures;

/// <summary>
/// Loads and provides access to the standard corpus of reference texts.
/// </summary>
public sealed class StandardCorpus
{
    private readonly Dictionary<string, CorpusEntry> _entries;

    private StandardCorpus(IEnumerable<CorpusEntry> entries)
    {
        _entries = entries.ToDictionary(e => e.Id, StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<CorpusEntry> Entries => _entries.Values;

    public CorpusEntry Get(string id) =>
        _entries.TryGetValue(id, out var entry)
            ? entry
            : throw new KeyNotFoundException($"Corpus entry '{id}' not found");

    public string GetText(string id) => Get(id).Text;
    public ExpectedMetrics GetExpected(string id) => Get(id).Expected;

    /// <summary>
    /// Loads the standard corpus from embedded resources or inline data.
    /// </summary>
    public static StandardCorpus Load()
    {
        var entries = new List<CorpusEntry>
        {
            new()
            {
                Id = "gettysburg",
                Name = "Gettysburg Address (opening)",
                Source = "Abraham Lincoln, 1863",
                Text = "Four score and seven years ago our fathers brought forth on this continent, a new nation, conceived in Liberty, and dedicated to the proposition that all men are created equal.",
                Expected = new ExpectedMetrics
                {
                    FleschKincaidGradeLevel = 11.2,
                    GunningFogIndex = 14.1,
                    FleschReadingEase = 63.9,
                    WordCount = 30,
                    SentenceCount = 1,
                    SyllableCount = 46
                }
            },
            new()
            {
                Id = "simple-sentence",
                Name = "The cat sat on the mat",
                Source = "Test corpus",
                Text = "The cat sat on the mat.",
                Expected = new ExpectedMetrics
                {
                    FleschKincaidGradeLevel = -0.5,
                    GunningFogIndex = 2.4,
                    FleschReadingEase = 116.1,
                    WordCount = 6,
                    SentenceCount = 1,
                    SyllableCount = 6
                }
            },
            new()
            {
                Id = "hemingway",
                Name = "Hemingway excerpt",
                Source = "The Old Man and the Sea, 1952",
                Text = "He was an old man who fished alone in a skiff in the Gulf Stream and he had gone eighty-four days now without taking a fish.",
                Expected = new ExpectedMetrics
                {
                    FleschKincaidGradeLevel = 4.0,
                    GunningFogIndex = 5.8,
                    FleschReadingEase = 92.0,
                    WordCount = 28,
                    SentenceCount = 1,
                    SyllableCount = 34
                }
            },
            new()
            {
                Id = "technical-complex",
                Name = "Technical documentation",
                Source = "Test corpus",
                Text = "Implementing sophisticated architectural paradigms requires substantial theoretical knowledge and practical experience with distributed computing infrastructure.",
                Expected = new ExpectedMetrics
                {
                    FleschKincaidGradeLevel = 18.5,
                    GunningFogIndex = 22.0,
                    FleschReadingEase = 8.0,
                    WordCount = 15,
                    SentenceCount = 1,
                    SyllableCount = 42,
                    ComplexWordCount = 8
                }
            },
            new()
            {
                Id = "multi-sentence",
                Name = "Multi-sentence paragraph",
                Source = "Test corpus",
                Text = "The quick brown fox jumps over the lazy dog. This sentence contains every letter of the alphabet. It is commonly used for testing purposes.",
                Expected = new ExpectedMetrics
                {
                    FleschKincaidGradeLevel = 4.5,
                    GunningFogIndex = 7.2,
                    FleschReadingEase = 75.0,
                    WordCount = 25,
                    SentenceCount = 3,
                    SyllableCount = 32
                }
            }
        };

        return new StandardCorpus(entries);
    }
}
```

---

## 7. UI/UX Specifications

**Not applicable.** This is a test-only specification with no user-facing UI components.

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Testing readability: '{TextId}' - FK={FK}, Fog={Fog}, FRE={FRE}"` |
| Info | `"Readability test suite completed: {PassCount}/{TotalCount} passed"` |
| Error | `"Accuracy test failed for '{TextId}': expected FK={Expected}, got FK={Actual}"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Test data injection | None | Tests use hardcoded corpus |
| Sensitive data in tests | None | Public domain texts only |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Gettysburg Address text | Calculating FK | Returns 11.2 ±0.1 |
| 2 | Gettysburg Address text | Calculating Fog | Returns 14.1 ±0.2 |
| 3 | Gettysburg Address text | Calculating FRE | Returns 63.9 ±1.0 |
| 4 | "The cat sat on the mat." | Calculating FK | Returns < 2.0 |
| 5 | Word "queue" | Counting syllables | Returns 1 |
| 6 | Word "documentation" | Counting syllables | Returns 5 |
| 7 | "Mr. Smith went home." | Tokenizing | Returns 1 sentence |
| 8 | Empty text | Analyzing | Returns empty metrics |

### 10.2 CI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | Accuracy > tolerance | CI runs tests | Build fails |
| 10 | All within tolerance | CI runs tests | Build succeeds |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `SyllableCounterAccuracyTests.cs` | [ ] |
| 2 | `SentenceTokenizerAccuracyTests.cs` | [ ] |
| 3 | `ReadabilityAccuracyTests.cs` | [ ] |
| 4 | `StandardCorpus.cs` loader class | [ ] |
| 5 | `CorpusEntry.cs` record definition | [ ] |
| 6 | Test trait configuration | [ ] |
| 7 | CI filter for `Version=v0.3.8b` | [ ] |

---

## 12. Verification Commands

```bash
# Run all readability accuracy tests
dotnet test --filter "Version=v0.3.8b" --logger "console;verbosity=detailed"

# Run only syllable counter tests
dotnet test --filter "FullyQualifiedName~SyllableCounterAccuracyTests"

# Run only sentence tokenizer tests
dotnet test --filter "FullyQualifiedName~SentenceTokenizerAccuracyTests"

# Run only readability formula tests
dotnet test --filter "FullyQualifiedName~ReadabilityAccuracyTests"

# Run with coverage
dotnet test --filter "Version=v0.3.8b" --collect:"XPlat Code Coverage"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-26 | Lead Architect | Initial draft |
