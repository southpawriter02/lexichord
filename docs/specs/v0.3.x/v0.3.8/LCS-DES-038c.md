# LCS-DES-038c: Design Specification — Passive Voice Test Suite

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `TST-038c` | Sub-part of TST-038 |
| **Feature Name** | `Passive Voice Detection Test Suite` | Algorithm verification tests |
| **Target Version** | `v0.3.8c` | Third sub-part of v0.3.8 |
| **Module Scope** | `Lexichord.Tests.Style` | Test project |
| **Swimlane** | `Governance` | Part of Style vertical |
| **License Tier** | `Core` | Testing available to all |
| **Feature Gate Key** | N/A | No gating for tests |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-26` | |
| **Parent Document** | [LCS-DES-038-INDEX](./LCS-DES-038-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-038 §3.3](./LCS-SBD-038.md#33-v038c-passive-voice-test-suite) | |

---

## 2. Executive Summary

### 2.1 The Requirement

The passive voice detector and weasel word scanner introduced in v0.3.4 must accurately identify passive constructions while minimizing false positives. Without verification:

- Passive constructions could be missed, reducing detection accuracy
- Active voice sentences could be flagged incorrectly, frustrating users
- Linking verb patterns (e.g., "The meeting was at 3pm.") could trigger false positives
- Weasel words could be detected in inappropriate contexts

> **Goal:** Verify that "The code was written by the user." is detected as passive, while "The user wrote the code." is correctly identified as active voice, achieving 95%+ detection accuracy.

### 2.2 The Proposed Solution

Create a labeled test corpus of sentences and implement a test suite that:

1. Validates passive voice detection against known passive constructions
2. Verifies active voice sentences are NOT flagged (false positive prevention)
3. Tests common false positive patterns (linking verbs, adjective complements)
4. Validates weasel word detection for hedging language
5. Establishes accuracy metrics (95%+ detection, < 5% false positive rate)
6. Integrates with CI to prevent accuracy regressions

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `IPassiveVoiceDetector` | v0.3.4b | Passive voice detection under test |
| `IVoiceScanner` | v0.3.4c | Weasel word scanning under test |
| `PassiveVoiceMatch` | v0.3.4b | Match result record |
| `WeaselWordMatch` | v0.3.4c | Weasel word result record |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `xunit` | 2.9.x | Test framework |
| `FluentAssertions` | 6.x | Fluent assertions |
| `Moq` | 4.x | Mocking dependencies |

### 3.2 Licensing Behavior

No licensing required. Tests run in development/CI environments only.

---

## 4. Data Contract (The API)

### 4.1 Labeled Sentence Corpus Definition

```csharp
namespace Lexichord.Tests.Style.TestFixtures;

/// <summary>
/// A labeled sentence for passive voice detection testing.
/// </summary>
public record LabeledSentence
{
    /// <summary>The sentence text to analyze.</summary>
    public required string Text { get; init; }

    /// <summary>Whether this sentence is passive voice.</summary>
    public required bool IsPassive { get; init; }

    /// <summary>Category for organization and filtering.</summary>
    public required SentenceCategory Category { get; init; }

    /// <summary>Optional notes about the classification.</summary>
    public string? Notes { get; init; }

    /// <summary>Expected passive verb phrase if passive.</summary>
    public string? ExpectedMatch { get; init; }
}

/// <summary>
/// Categories for organizing test sentences.
/// </summary>
public enum SentenceCategory
{
    /// <summary>Clear passive voice constructions.</summary>
    ClearPassive,

    /// <summary>Clear active voice constructions.</summary>
    ClearActive,

    /// <summary>Linking verb patterns that should NOT be flagged.</summary>
    LinkingVerb,

    /// <summary>Adjective complement patterns that should NOT be flagged.</summary>
    AdjectiveComplement,

    /// <summary>Progressive active patterns that should NOT be flagged.</summary>
    ProgressiveActive,

    /// <summary>Get-passive constructions.</summary>
    GetPassive,

    /// <summary>Modal passive constructions.</summary>
    ModalPassive,

    /// <summary>Edge cases and complex sentences.</summary>
    EdgeCase
}
```

### 4.2 Test Class Structure

```csharp
namespace Lexichord.Tests.Style.Voice;

/// <summary>
/// Accuracy tests for passive voice detection.
/// Verifies detection of passive constructions and prevention of false positives.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8c")]
public class PassiveVoiceDetectorTests
{
    private readonly IPassiveVoiceDetector _sut;
    private readonly LabeledSentenceCorpus _corpus;

    public PassiveVoiceDetectorTests()
    {
        _sut = new PassiveVoiceDetector();
        _corpus = LabeledSentenceCorpus.Load();
    }

    // Test methods...
}

/// <summary>
/// Accuracy tests for weasel word detection.
/// Verifies detection of hedging language and adverbs.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8c")]
public class WeaselWordScannerTests
{
    private readonly IVoiceScanner _sut;

    public WeaselWordScannerTests()
    {
        _sut = new WeaselWordScanner();
    }

    // Test methods...
}
```

---

## 5. Implementation Logic

### 5.1 Passive Voice Patterns

Passive voice occurs when the subject receives the action rather than performing it. The general pattern is:

```text
PASSIVE VOICE FORMULA:
form of "be" + past participle (+ optional "by" phrase)

COMMON PATTERNS:
├── was/were + past participle: "was written", "were found"
├── is/are + being + past participle: "is being processed"
├── has/have + been + past participle: "has been completed"
├── will be + past participle: "will be reviewed"
├── Modal + be + past participle: "should be tested", "must be approved"
└── Get-passive: "got promoted", "get fixed"
```

### 5.2 False Positive Patterns

These patterns look similar but are NOT passive voice:

```text
FALSE POSITIVE PATTERNS (should NOT detect):
├── Linking verbs: "was happy", "is tall", "were ready"
│   └── Pattern: be + adjective (no past participle)
├── State descriptions: "The door was open", "The meeting was at 3pm"
│   └── Pattern: be + prepositional phrase or state
├── Progressive active: "is running", "was working"
│   └── Pattern: be + present participle (-ing)
├── Adjective complements: "was interesting", "were exciting"
│   └── Pattern: be + participial adjective (describes state)
└── Existential: "There was a problem", "There were issues"
    └── Pattern: There + be + noun phrase
```

### 5.3 Decision Logic

```text
IS THIS PASSIVE VOICE?
│
├── Contains "be" verb (am/is/are/was/were/been/being)?
│   └── NO → NOT passive
│
├── Followed by past participle?
│   └── NO → Check for get-passive or NOT passive
│
├── Past participle is a verb (not pure adjective)?
│   ├── "written", "completed", "fixed" → Verbal (PASSIVE)
│   └── "open", "happy", "interesting" → Adjectival (NOT passive)
│
├── Context indicates action was performed?
│   └── YES → PASSIVE VOICE
│
└── DEFAULT: Check against known patterns
```

---

## 6. Test Scenarios

### 6.1 Labeled Sentence Corpus

```csharp
namespace Lexichord.Tests.Style.TestFixtures;

/// <summary>
/// Curated corpus of labeled sentences for passive voice testing.
/// Each sentence is manually classified as passive or active.
/// </summary>
public static class LabeledSentenceCorpus
{
    #region Clear Passive Voice Sentences

    public static readonly LabeledSentence[] PassiveSentences = new[]
    {
        // Simple passive (was/were + past participle)
        new LabeledSentence
        {
            Text = "The code was written by the user.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "was written",
            Notes = "Classic passive with 'by' phrase"
        },
        new LabeledSentence
        {
            Text = "The report was submitted yesterday.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "was submitted",
            Notes = "Passive without 'by' phrase"
        },
        new LabeledSentence
        {
            Text = "The bug was fixed in version 2.0.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "was fixed",
            Notes = "Technical passive"
        },
        new LabeledSentence
        {
            Text = "Mistakes were made.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "were made",
            Notes = "Famous passive construction"
        },
        new LabeledSentence
        {
            Text = "The documents were reviewed by the committee.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "were reviewed",
            Notes = "Plural passive"
        },

        // Being passive (is/are + being + past participle)
        new LabeledSentence
        {
            Text = "The data is being processed.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "is being processed",
            Notes = "Progressive passive"
        },
        new LabeledSentence
        {
            Text = "The files are being uploaded.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "are being uploaded",
            Notes = "Plural progressive passive"
        },

        // Perfect passive (has/have + been + past participle)
        new LabeledSentence
        {
            Text = "The feature has been implemented.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "has been implemented",
            Notes = "Perfect passive"
        },
        new LabeledSentence
        {
            Text = "All tests have been completed.",
            IsPassive = true,
            Category = SentenceCategory.ClearPassive,
            ExpectedMatch = "have been completed",
            Notes = "Plural perfect passive"
        },

        // Modal passive (will/should/must/can + be + past participle)
        new LabeledSentence
        {
            Text = "The changes will be reviewed.",
            IsPassive = true,
            Category = SentenceCategory.ModalPassive,
            ExpectedMatch = "will be reviewed",
            Notes = "Future passive"
        },
        new LabeledSentence
        {
            Text = "This code should be tested.",
            IsPassive = true,
            Category = SentenceCategory.ModalPassive,
            ExpectedMatch = "should be tested",
            Notes = "Modal passive with should"
        },
        new LabeledSentence
        {
            Text = "The pull request must be approved.",
            IsPassive = true,
            Category = SentenceCategory.ModalPassive,
            ExpectedMatch = "must be approved",
            Notes = "Modal passive with must"
        },
        new LabeledSentence
        {
            Text = "The issue can be resolved.",
            IsPassive = true,
            Category = SentenceCategory.ModalPassive,
            ExpectedMatch = "can be resolved",
            Notes = "Modal passive with can"
        },

        // Get-passive
        new LabeledSentence
        {
            Text = "The employee got promoted.",
            IsPassive = true,
            Category = SentenceCategory.GetPassive,
            ExpectedMatch = "got promoted",
            Notes = "Get-passive"
        },
        new LabeledSentence
        {
            Text = "The bug will get fixed soon.",
            IsPassive = true,
            Category = SentenceCategory.GetPassive,
            ExpectedMatch = "get fixed",
            Notes = "Get-passive with modal"
        },
    };

    #endregion

    #region Clear Active Voice Sentences

    public static readonly LabeledSentence[] ActiveSentences = new[]
    {
        new LabeledSentence
        {
            Text = "The user wrote the code.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "Active counterpart to 'was written'"
        },
        new LabeledSentence
        {
            Text = "I submitted the report yesterday.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "First person active"
        },
        new LabeledSentence
        {
            Text = "The team fixed the bug in version 2.0.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "Active with clear subject"
        },
        new LabeledSentence
        {
            Text = "Someone made mistakes.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "Active with indefinite subject"
        },
        new LabeledSentence
        {
            Text = "The system processes data.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "Present tense active"
        },
        new LabeledSentence
        {
            Text = "We implemented the feature.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "First person plural active"
        },
        new LabeledSentence
        {
            Text = "The developer reviews the code.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "Present tense active"
        },
        new LabeledSentence
        {
            Text = "The manager approved the request.",
            IsPassive = false,
            Category = SentenceCategory.ClearActive,
            Notes = "Past tense active"
        },
    };

    #endregion

    #region False Positive Patterns (Should NOT detect)

    public static readonly LabeledSentence[] FalsePositivePatterns = new[]
    {
        // Linking verbs with adjectives
        new LabeledSentence
        {
            Text = "The meeting was at 3pm.",
            IsPassive = false,
            Category = SentenceCategory.LinkingVerb,
            Notes = "'was' as linking verb with prepositional phrase"
        },
        new LabeledSentence
        {
            Text = "The door was open.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "'was' + adjective, not past participle"
        },
        new LabeledSentence
        {
            Text = "She was happy.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "Simple adjective complement"
        },
        new LabeledSentence
        {
            Text = "The team was ready.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "'ready' is adjective, not participle"
        },
        new LabeledSentence
        {
            Text = "The results were clear.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "'clear' is adjective"
        },
        new LabeledSentence
        {
            Text = "He is tall.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "Simple adjective predicate"
        },

        // State descriptions
        new LabeledSentence
        {
            Text = "The office was on the fifth floor.",
            IsPassive = false,
            Category = SentenceCategory.LinkingVerb,
            Notes = "Location description"
        },
        new LabeledSentence
        {
            Text = "The conference was in May.",
            IsPassive = false,
            Category = SentenceCategory.LinkingVerb,
            Notes = "Time description"
        },

        // Progressive active (present participle, not past)
        new LabeledSentence
        {
            Text = "The system is running.",
            IsPassive = false,
            Category = SentenceCategory.ProgressiveActive,
            Notes = "Progressive with -ing, not passive"
        },
        new LabeledSentence
        {
            Text = "The developer was working on the bug.",
            IsPassive = false,
            Category = SentenceCategory.ProgressiveActive,
            Notes = "Past progressive active"
        },
        new LabeledSentence
        {
            Text = "They are writing documentation.",
            IsPassive = false,
            Category = SentenceCategory.ProgressiveActive,
            Notes = "Present progressive active"
        },

        // Participial adjectives (look like passives but describe state)
        new LabeledSentence
        {
            Text = "The movie was interesting.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "'interesting' as adjective, not passive"
        },
        new LabeledSentence
        {
            Text = "The news was exciting.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "'exciting' as adjective"
        },
        new LabeledSentence
        {
            Text = "The task was challenging.",
            IsPassive = false,
            Category = SentenceCategory.AdjectiveComplement,
            Notes = "'challenging' as adjective"
        },

        // Existential constructions
        new LabeledSentence
        {
            Text = "There was a problem.",
            IsPassive = false,
            Category = SentenceCategory.EdgeCase,
            Notes = "Existential 'there was'"
        },
        new LabeledSentence
        {
            Text = "There were several issues.",
            IsPassive = false,
            Category = SentenceCategory.EdgeCase,
            Notes = "Existential 'there were'"
        },
    };

    #endregion

    #region Edge Cases

    public static readonly LabeledSentence[] EdgeCases = new[]
    {
        // Sentence with both passive and active clauses
        new LabeledSentence
        {
            Text = "The report was written, and the team reviewed it.",
            IsPassive = true,
            Category = SentenceCategory.EdgeCase,
            ExpectedMatch = "was written",
            Notes = "Mixed: first clause passive, second active"
        },

        // Past participle that's commonly used as adjective
        new LabeledSentence
        {
            Text = "I am tired.",
            IsPassive = false,
            Category = SentenceCategory.EdgeCase,
            Notes = "'tired' as adjective (state), not passive"
        },
        new LabeledSentence
        {
            Text = "The window was broken.",
            IsPassive = true,
            Category = SentenceCategory.EdgeCase,
            ExpectedMatch = "was broken",
            Notes = "Ambiguous: could be state or passive (interpret as passive)"
        },

        // Infinitive passive
        new LabeledSentence
        {
            Text = "The code needs to be refactored.",
            IsPassive = true,
            Category = SentenceCategory.EdgeCase,
            ExpectedMatch = "to be refactored",
            Notes = "Infinitive passive construction"
        },
    };

    #endregion

    /// <summary>
    /// Gets all labeled sentences for comprehensive testing.
    /// </summary>
    public static IEnumerable<LabeledSentence> GetAll() =>
        PassiveSentences
            .Concat(ActiveSentences)
            .Concat(FalsePositivePatterns)
            .Concat(EdgeCases);

    /// <summary>
    /// Loads the full corpus.
    /// </summary>
    public static LabeledSentenceCorpus Load() => new(GetAll());
}
```

### 6.2 PassiveVoiceDetectorTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8c")]
public class PassiveVoiceDetectorTests
{
    private readonly IPassiveVoiceDetector _sut = new PassiveVoiceDetector();

    #region Critical Test Cases (from Roadmap)

    [Fact]
    public void IsPassive_CodeWasWrittenByUser_DetectsPassive()
    {
        // This is THE critical test case from the roadmap:
        // "The code was written by the user." -> Detect

        var result = _sut.IsPassive("The code was written by the user.");

        result.Should().BeTrue(
            "'The code was written by the user.' must be detected as passive");
    }

    [Fact]
    public void IsPassive_UserWroteTheCode_DetectsActive()
    {
        // This is THE critical test case from the roadmap:
        // "The user wrote the code." -> Clean

        var result = _sut.IsPassive("The user wrote the code.");

        result.Should().BeFalse(
            "'The user wrote the code.' must NOT be detected as passive");
    }

    #endregion

    #region Simple Passive Patterns (was/were + past participle)

    [Theory]
    [InlineData("The code was written by the user.", true)]
    [InlineData("The report was submitted yesterday.", true)]
    [InlineData("The bug was fixed in version 2.0.", true)]
    [InlineData("Mistakes were made.", true)]
    [InlineData("The documents were reviewed by the committee.", true)]
    public void IsPassive_SimplePassivePatterns_DetectsCorrectly(
        string sentence, bool expected)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().Be(expected);
    }

    #endregion

    #region Progressive Passive (is/are + being + past participle)

    [Theory]
    [InlineData("The data is being processed.", true)]
    [InlineData("The files are being uploaded.", true)]
    [InlineData("The project is being reviewed.", true)]
    public void IsPassive_ProgressivePassive_DetectsCorrectly(
        string sentence, bool expected)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().Be(expected);
    }

    #endregion

    #region Perfect Passive (has/have + been + past participle)

    [Theory]
    [InlineData("The feature has been implemented.", true)]
    [InlineData("All tests have been completed.", true)]
    [InlineData("The issue has been resolved.", true)]
    public void IsPassive_PerfectPassive_DetectsCorrectly(
        string sentence, bool expected)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().Be(expected);
    }

    #endregion

    #region Modal Passive (will/should/must/can + be + past participle)

    [Theory]
    [InlineData("The changes will be reviewed.", true)]
    [InlineData("This code should be tested.", true)]
    [InlineData("The pull request must be approved.", true)]
    [InlineData("The issue can be resolved.", true)]
    [InlineData("The feature might be delayed.", true)]
    public void IsPassive_ModalPassive_DetectsCorrectly(
        string sentence, bool expected)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().Be(expected);
    }

    #endregion

    #region Get-Passive

    [Theory]
    [InlineData("The employee got promoted.", true)]
    [InlineData("The bug will get fixed soon.", true)]
    [InlineData("He got fired last week.", true)]
    public void IsPassive_GetPassive_DetectsCorrectly(
        string sentence, bool expected)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().Be(expected);
    }

    #endregion

    #region Active Voice (should NOT detect)

    [Theory]
    [InlineData("The user wrote the code.")]
    [InlineData("I submitted the report yesterday.")]
    [InlineData("The team fixed the bug in version 2.0.")]
    [InlineData("Someone made mistakes.")]
    [InlineData("The system processes data.")]
    [InlineData("We implemented the feature.")]
    [InlineData("The developer reviews the code.")]
    [InlineData("The manager approved the request.")]
    public void IsPassive_ActiveVoice_ReturnsFalse(string sentence)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().BeFalse(
            $"'{sentence}' is active voice and should not be flagged");
    }

    #endregion

    #region False Positive Prevention (Linking Verbs)

    [Theory]
    [InlineData("The meeting was at 3pm.")]
    [InlineData("The door was open.")]
    [InlineData("She was happy.")]
    [InlineData("The team was ready.")]
    [InlineData("The results were clear.")]
    [InlineData("He is tall.")]
    [InlineData("The office was on the fifth floor.")]
    [InlineData("The conference was in May.")]
    public void IsPassive_LinkingVerbPatterns_ReturnsFalse(string sentence)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().BeFalse(
            $"'{sentence}' uses a linking verb and should not be flagged as passive");
    }

    #endregion

    #region False Positive Prevention (Progressive Active)

    [Theory]
    [InlineData("The system is running.")]
    [InlineData("The developer was working on the bug.")]
    [InlineData("They are writing documentation.")]
    [InlineData("The tests are passing.")]
    [InlineData("She was coding all night.")]
    public void IsPassive_ProgressiveActive_ReturnsFalse(string sentence)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().BeFalse(
            $"'{sentence}' is progressive active and should not be flagged");
    }

    #endregion

    #region False Positive Prevention (Participial Adjectives)

    [Theory]
    [InlineData("The movie was interesting.")]
    [InlineData("The news was exciting.")]
    [InlineData("The task was challenging.")]
    [InlineData("The presentation was boring.")]
    [InlineData("The experience was overwhelming.")]
    public void IsPassive_ParticipialAdjectives_ReturnsFalse(string sentence)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().BeFalse(
            $"'{sentence}' uses a participial adjective, not passive voice");
    }

    #endregion

    #region False Positive Prevention (Existential)

    [Theory]
    [InlineData("There was a problem.")]
    [InlineData("There were several issues.")]
    [InlineData("There is a bug in the code.")]
    public void IsPassive_ExistentialConstruction_ReturnsFalse(string sentence)
    {
        var result = _sut.IsPassive(sentence);
        result.Should().BeFalse(
            $"'{sentence}' is existential construction, not passive");
    }

    #endregion

    #region Corpus-Based Accuracy Testing

    [Theory]
    [MemberData(nameof(GetPassiveSentences))]
    public void IsPassive_AllPassiveSentences_DetectedAsPassive(LabeledSentence sentence)
    {
        var result = _sut.IsPassive(sentence.Text);
        result.Should().BeTrue(
            $"'{sentence.Text}' is labeled as passive ({sentence.Category}) but was not detected");
    }

    [Theory]
    [MemberData(nameof(GetActiveSentences))]
    public void IsPassive_AllActiveSentences_NotDetectedAsPassive(LabeledSentence sentence)
    {
        var result = _sut.IsPassive(sentence.Text);
        result.Should().BeFalse(
            $"'{sentence.Text}' is labeled as active ({sentence.Category}) but was incorrectly flagged");
    }

    public static IEnumerable<object[]> GetPassiveSentences()
    {
        foreach (var sentence in LabeledSentenceCorpus.PassiveSentences)
        {
            yield return new object[] { sentence };
        }
    }

    public static IEnumerable<object[]> GetActiveSentences()
    {
        foreach (var sentence in LabeledSentenceCorpus.ActiveSentences
            .Concat(LabeledSentenceCorpus.FalsePositivePatterns))
        {
            yield return new object[] { sentence };
        }
    }

    #endregion

    #region Match Details

    [Fact]
    public void Analyze_PassiveSentence_ReturnsMatchWithDetails()
    {
        var result = _sut.Analyze("The code was written by the user.");

        result.Should().HaveCount(1);
        result[0].VerbPhrase.Should().Be("was written");
        result[0].StartIndex.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Analyze_ActiveSentence_ReturnsEmpty()
    {
        var result = _sut.Analyze("The user wrote the code.");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_MultipleClauses_FindsAllPassive()
    {
        var result = _sut.Analyze(
            "The code was written by the developer, and the tests were run by the CI system.");

        result.Should().HaveCount(2);
        result.Should().Contain(m => m.VerbPhrase == "was written");
        result.Should().Contain(m => m.VerbPhrase == "were run");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void IsPassive_EmptyString_ReturnsFalse()
    {
        var result = _sut.IsPassive("");
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPassive_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.IsPassive(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void IsPassive_WhitespaceOnly_ReturnsFalse()
    {
        var result = _sut.IsPassive("   \n\t  ");
        result.Should().BeFalse();
    }

    [Fact]
    public void IsPassive_CaseInsensitive()
    {
        var lower = _sut.IsPassive("the code was written.");
        var upper = _sut.IsPassive("THE CODE WAS WRITTEN.");
        var mixed = _sut.IsPassive("The Code Was Written.");

        lower.Should().Be(upper).And.Be(mixed);
    }

    #endregion
}
```

### 6.3 WeaselWordScannerTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8c")]
public class WeaselWordScannerTests
{
    private readonly IVoiceScanner _sut = new WeaselWordScanner();

    #region Intensity Adverbs

    [Theory]
    [InlineData("This is very important.", "very")]
    [InlineData("The code is really useful.", "really")]
    [InlineData("This is extremely critical.", "extremely")]
    [InlineData("The feature is highly requested.", "highly")]
    public void Scan_IntensityAdverbs_DetectsExpectedWord(
        string sentence, string expectedWeaselWord)
    {
        var result = _sut.Scan(sentence);
        result.Should().Contain(w => w.Word == expectedWeaselWord);
    }

    #endregion

    #region Hedging Words

    [Theory]
    [InlineData("The code is basically done.", "basically")]
    [InlineData("It sort of works.", "sort of")]
    [InlineData("This is quite good.", "quite")]
    [InlineData("The feature is fairly complete.", "fairly")]
    [InlineData("It somewhat improves performance.", "somewhat")]
    public void Scan_HedgingWords_DetectsExpectedWord(
        string sentence, string expectedWeaselWord)
    {
        var result = _sut.Scan(sentence);
        result.Should().Contain(w => w.Word == expectedWeaselWord);
    }

    #endregion

    #region Vague Qualifiers

    [Theory]
    [InlineData("Various options are available.", "various")]
    [InlineData("There are several approaches.", "several")]
    [InlineData("Numerous issues were found.", "numerous")]
    [InlineData("Many users reported problems.", "many")]
    public void Scan_VagueQualifiers_DetectsExpectedWord(
        string sentence, string expectedWeaselWord)
    {
        var result = _sut.Scan(sentence);
        result.Should().Contain(w => w.Word == expectedWeaselWord);
    }

    #endregion

    #region Clean Sentences (no weasel words)

    [Theory]
    [InlineData("This is important.")]
    [InlineData("The code is complete.")]
    [InlineData("It works.")]
    [InlineData("The feature improves performance.")]
    [InlineData("Users reported five problems.")]
    public void Scan_CleanSentences_ReturnsEmpty(string sentence)
    {
        var result = _sut.Scan(sentence);
        result.Should().BeEmpty();
    }

    #endregion

    #region Multiple Weasel Words

    [Fact]
    public void Scan_MultipleWeaselWords_FindsAll()
    {
        var sentence = "This is really very basically quite important.";

        var result = _sut.Scan(sentence);

        result.Should().HaveCountGreaterThanOrEqualTo(3);
        result.Should().Contain(w => w.Word == "really");
        result.Should().Contain(w => w.Word == "very");
        result.Should().Contain(w => w.Word == "basically");
    }

    #endregion

    #region Word Boundaries

    [Theory]
    [InlineData("The delivery was on time.", false)] // "very" not in "delivery"
    [InlineData("Overall, the results are good.", false)] // "all" not flagged in "overall"
    public void Scan_WordBoundaries_DoesNotMatchPartialWords(
        string sentence, bool shouldHaveMatches)
    {
        var result = _sut.Scan(sentence);

        if (shouldHaveMatches)
            result.Should().NotBeEmpty();
        else
            result.Should().BeEmpty();
    }

    #endregion

    #region Match Details

    [Fact]
    public void Scan_WeaselWord_ReturnsMatchWithDetails()
    {
        var result = _sut.Scan("This is very important.");

        result.Should().HaveCount(1);
        result[0].Word.Should().Be("very");
        result[0].Category.Should().Be(WeaselWordCategory.IntensityAdverb);
        result[0].StartIndex.Should().BeGreaterThan(0);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Scan_EmptyString_ReturnsEmpty()
    {
        var result = _sut.Scan("");
        result.Should().BeEmpty();
    }

    [Fact]
    public void Scan_NullInput_ThrowsArgumentNullException()
    {
        var act = () => _sut.Scan(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Scan_CaseInsensitive()
    {
        var lower = _sut.Scan("this is very important.");
        var upper = _sut.Scan("THIS IS VERY IMPORTANT.");

        lower.Should().HaveCount(upper.Count);
    }

    #endregion
}
```

---

## 7. UI/UX Specifications

**Not applicable.** This is a test-only specification with no user-facing UI components.

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Testing passive detection: '{Sentence}' - IsPassive={IsPassive}"` |
| Info | `"Passive voice test suite completed: {PassCount}/{TotalCount} passed"` |
| Error | `"Passive detection test failed: expected {Expected}, got {Actual} for '{Sentence}'"` |
| Warning | `"False positive detected: '{Sentence}' incorrectly flagged as passive"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Test data injection | None | Tests use hardcoded sentences |
| Sensitive data in tests | None | Generic sentences only |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | "The code was written by the user." | Detecting passive | Returns true |
| 2 | "The user wrote the code." | Detecting passive | Returns false |
| 3 | "The meeting was at 3pm." | Detecting passive | Returns false (linking verb) |
| 4 | "The door was open." | Detecting passive | Returns false (adjective) |
| 5 | "The data is being processed." | Detecting passive | Returns true |
| 6 | "The feature has been implemented." | Detecting passive | Returns true |
| 7 | "This code should be tested." | Detecting passive | Returns true |
| 8 | "The system is running." | Detecting passive | Returns false (progressive active) |
| 9 | "This is very important." | Scanning weasel words | Detects "very" |
| 10 | "This is important." | Scanning weasel words | Returns empty |

### 10.2 Accuracy Criteria

| # | Metric | Target |
| :--- | :--- | :--- |
| 11 | Passive detection rate | 95%+ on labeled corpus |
| 12 | False positive rate | < 5% on active sentences |
| 13 | Weasel word detection | 100% on known weasel words |

### 10.3 CI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 14 | Any accuracy test fails | CI runs tests | Build fails |
| 15 | All accuracy tests pass | CI runs tests | Build succeeds |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `PassiveVoiceDetectorTests.cs` | [ ] |
| 2 | `WeaselWordScannerTests.cs` | [ ] |
| 3 | `LabeledSentenceCorpus.cs` test fixture | [ ] |
| 4 | `LabeledSentence.cs` record definition | [ ] |
| 5 | Test trait configuration | [ ] |
| 6 | CI filter for `Version=v0.3.8c` | [ ] |

---

## 12. Verification Commands

```bash
# Run all passive voice accuracy tests
dotnet test --filter "Version=v0.3.8c" --logger "console;verbosity=detailed"

# Run only passive voice detector tests
dotnet test --filter "FullyQualifiedName~PassiveVoiceDetectorTests"

# Run only weasel word scanner tests
dotnet test --filter "FullyQualifiedName~WeaselWordScannerTests"

# Run with coverage
dotnet test --filter "Version=v0.3.8c" --collect:"XPlat Code Coverage"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-26 | Lead Architect | Initial draft |
