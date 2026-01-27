# LCS-DES-038a: Design Specification — Fuzzy Test Suite

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `TST-038a` | Sub-part of TST-038 |
| **Feature Name** | `Fuzzy Matching Accuracy Test Suite` | Algorithm verification tests |
| **Target Version** | `v0.3.8a` | First sub-part of v0.3.8 |
| **Module Scope** | `Lexichord.Tests.Style` | Test project |
| **Swimlane** | `Governance` | Part of Style vertical |
| **License Tier** | `Core` | Testing available to all |
| **Feature Gate Key** | N/A | No gating for tests |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-26` | |
| **Parent Document** | [LCS-DES-038-INDEX](./LCS-DES-038-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-038 §3.1](./LCS-SBD-038.md#31-v038a-fuzzy-test-suite) | |

---

## 2. Executive Summary

### 2.1 The Requirement

The Levenshtein distance algorithm and fuzzy scanner introduced in v0.3.1 must produce accurate results to reliably detect typos and term variations. Without verification tests:

- Distance calculations could silently regress
- Threshold matching could fail for edge cases
- False positives/negatives could degrade user experience

> **Goal:** Verify that "whitelist" matches "white-list" (Distance 1) but NOT "wait-list" (Distance 3) at a 0.8 similarity threshold.

### 2.2 The Proposed Solution

Implement a comprehensive test suite that:

1. Tests raw Levenshtein distance calculations against known values
2. Verifies threshold-based matching behavior
3. Covers common typo patterns (transposition, omission, insertion)
4. Tests edge cases (empty strings, case sensitivity, unicode)
5. Integrates with CI to prevent regressions

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `LevenshteinDistanceService` | v0.3.1a | Distance calculation algorithm |
| `IFuzzyScanner` | v0.3.1c | Fuzzy matching scanner |
| `ITerminologyRepository` | v0.2.2b | Term data for integration tests |

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

### 4.1 Test Class Structure

```csharp
namespace Lexichord.Tests.Style.Fuzzy;

/// <summary>
/// Accuracy tests for the Levenshtein distance algorithm.
/// Verifies distance calculations match expected values for known inputs.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8a")]
public class LevenshteinDistanceTests
{
    private readonly LevenshteinDistanceService _sut;

    public LevenshteinDistanceTests()
    {
        _sut = new LevenshteinDistanceService();
    }

    // Test methods...
}

/// <summary>
/// Accuracy tests for the fuzzy matching scanner.
/// Verifies threshold-based matching produces expected results.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8a")]
public class FuzzyMatchingAccuracyTests
{
    private readonly IFuzzyScanner _sut;

    public FuzzyMatchingAccuracyTests()
    {
        _sut = new FuzzyScanner(new LevenshteinDistanceService());
    }

    // Test methods...
}
```

---

## 5. Implementation Logic

### 5.1 Levenshtein Distance Verification

The Levenshtein distance measures the minimum number of single-character edits (insertions, deletions, substitutions) required to transform one string into another.

#### 5.1.1 Distance Calculation Test Data

| Source | Target | Expected Distance | Operation |
| :--- | :--- | :--- | :--- |
| "whitelist" | "whitelist" | 0 | Exact match |
| "whitelist" | "whitelst" | 1 | Omission (i) |
| "whitelist" | "white-list" | 1 | Insertion (-) |
| "whitelist" | "whitelsit" | 2 | Transposition |
| "whitelist" | "wait-list" | 3 | Multiple changes |
| "" | "abc" | 3 | Empty source |
| "abc" | "" | 3 | Empty target |
| "" | "" | 0 | Both empty |

#### 5.1.2 Similarity Score Formula

```text
Similarity = 1.0 - (Distance / Max(SourceLength, TargetLength))

Examples:
- "whitelist" (9) vs "white-list" (10): 1 - (1/10) = 0.9
- "whitelist" (9) vs "wait-list" (9):   1 - (3/9)  = 0.67
```

### 5.2 Threshold Matching Logic

```text
MATCH at threshold T:
│
├── Calculate Levenshtein distance
├── Calculate similarity score
├── IF similarity >= T
│   └── RETURN true (match)
└── ELSE
    └── RETURN false (no match)

EXAMPLES (threshold = 0.8):
├── "whitelist" vs "white-list" (sim=0.9) → MATCH
├── "whitelist" vs "wait-list" (sim=0.67) → NO MATCH
└── "whitelist" vs "whitelist" (sim=1.0) → MATCH
```

---

## 6. Test Scenarios

### 6.1 LevenshteinDistanceTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8a")]
public class LevenshteinDistanceTests
{
    private readonly LevenshteinDistanceService _sut = new();

    #region Exact Match Tests

    [Fact]
    public void Calculate_IdenticalStrings_ReturnsZero()
    {
        var result = _sut.Calculate("whitelist", "whitelist");
        result.Should().Be(0);
    }

    [Theory]
    [InlineData("a", "a", 0)]
    [InlineData("test", "test", 0)]
    [InlineData("documentation", "documentation", 0)]
    public void Calculate_IdenticalStrings_AlwaysReturnsZero(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    #endregion

    #region Single Edit Tests

    [Theory]
    [InlineData("whitelist", "whitelst", 1)]   // Omission
    [InlineData("whitelist", "whitelists", 1)] // Insertion
    [InlineData("whitelist", "whitelast", 1)]  // Substitution
    public void Calculate_SingleEdit_ReturnsOne(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    #endregion

    #region Hyphenation Variation Tests

    [Theory]
    [InlineData("whitelist", "white-list", 1)]
    [InlineData("email", "e-mail", 1)]
    [InlineData("online", "on-line", 1)]
    [InlineData("blacklist", "black-list", 1)]
    public void Calculate_HyphenVariation_ReturnsOne(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    #endregion

    #region Multiple Edit Tests

    [Theory]
    [InlineData("whitelist", "wait-list", 3)]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("saturday", "sunday", 3)]
    public void Calculate_MultipleEdits_ReturnsExpectedDistance(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    #endregion

    #region Transposition Tests

    [Theory]
    [InlineData("the", "teh", 2)]       // Classic typo (2 swaps in Levenshtein)
    [InlineData("receive", "recieve", 2)]
    [InlineData("weird", "wierd", 2)]
    public void Calculate_Transposition_ReturnsExpectedDistance(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    #endregion

    #region Common Typo Pattern Tests

    [Theory]
    [InlineData("separate", "seperate", 1)]      // Common misspelling
    [InlineData("definitely", "definately", 2)]  // Common misspelling
    [InlineData("occurrence", "occurrance", 2)]  // Common misspelling
    [InlineData("accommodate", "accomodate", 1)] // Common misspelling
    public void Calculate_CommonTypos_ReturnsExpectedDistance(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    #endregion

    #region Edge Case Tests

    [Theory]
    [InlineData("", "", 0)]
    [InlineData("", "abc", 3)]
    [InlineData("abc", "", 3)]
    public void Calculate_EmptyStrings_ReturnsCorrectDistance(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    [Fact]
    public void Calculate_NullSource_ThrowsArgumentNullException()
    {
        var act = () => _sut.Calculate(null!, "test");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Calculate_NullTarget_ThrowsArgumentNullException()
    {
        var act = () => _sut.Calculate("test", null!);
        act.Should().Throw<ArgumentNullException>();
    }

    #endregion

    #region Case Sensitivity Tests

    [Theory]
    [InlineData("Whitelist", "whitelist", 1)]  // Case differs
    [InlineData("WhiteList", "whitelist", 2)]  // CamelCase
    public void Calculate_DifferentCase_CountsAsEdits(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(source, target);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("Whitelist", "whitelist", 0)]
    [InlineData("WhiteList", "whitelist", 0)]
    [InlineData("WHITE-LIST", "white-list", 0)]
    public void CalculateIgnoreCase_DifferentCase_ReturnsZero(
        string source, string target, int expected)
    {
        var result = _sut.Calculate(
            source.ToLowerInvariant(),
            target.ToLowerInvariant());
        result.Should().Be(expected);
    }

    #endregion

    #region Symmetry Tests

    [Theory]
    [InlineData("whitelist", "wait-list")]
    [InlineData("kitten", "sitting")]
    [InlineData("abc", "xyz")]
    public void Calculate_IsSymmetric(string source, string target)
    {
        var forward = _sut.Calculate(source, target);
        var reverse = _sut.Calculate(target, source);
        forward.Should().Be(reverse, "Levenshtein distance must be symmetric");
    }

    #endregion
}
```

### 6.2 FuzzyMatchingAccuracyTests

```csharp
[Trait("Category", "Unit")]
[Trait("Version", "v0.3.8a")]
public class FuzzyMatchingAccuracyTests
{
    private readonly IFuzzyScanner _sut;

    public FuzzyMatchingAccuracyTests()
    {
        _sut = new FuzzyScanner(new LevenshteinDistanceService());
    }

    #region Threshold Matching Tests

    [Theory]
    [InlineData("whitelist", "white-list", 0.8, true)]   // 0.9 > 0.8
    [InlineData("whitelist", "white-list", 0.9, true)]   // 0.9 >= 0.9
    [InlineData("whitelist", "white-list", 0.95, false)] // 0.9 < 0.95
    public void IsMatch_AtThreshold_ReturnsExpected(
        string term, string candidate, double threshold, bool expected)
    {
        var result = _sut.IsMatch(term, candidate, threshold);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("whitelist", "wait-list", 0.8, false)]   // 0.67 < 0.8
    [InlineData("whitelist", "wait-list", 0.6, true)]    // 0.67 > 0.6
    [InlineData("whitelist", "wait-list", 0.7, false)]   // 0.67 < 0.7
    public void IsMatch_DifferentWord_RespectsThreshold(
        string term, string candidate, double threshold, bool expected)
    {
        var result = _sut.IsMatch(term, candidate, threshold);
        result.Should().Be(expected);
    }

    #endregion

    #region The Critical Test Case

    [Fact]
    public void IsMatch_WhitelistVariations_MatchesCorrectly()
    {
        // This is THE critical test case from the roadmap:
        // "whitelist" matches "white-list" (Distance 1) but not "wait-list" (Distance 3)

        const double threshold = 0.8;

        // Should match: hyphen variation
        _sut.IsMatch("whitelist", "white-list", threshold)
            .Should().BeTrue("'white-list' is a hyphen variation of 'whitelist'");

        // Should NOT match: different word entirely
        _sut.IsMatch("whitelist", "wait-list", threshold)
            .Should().BeFalse("'wait-list' is a different word, not a typo");
    }

    #endregion

    #region Similarity Score Tests

    [Theory]
    [InlineData("whitelist", "whitelist", 1.0)]
    [InlineData("whitelist", "white-list", 0.9)]   // 1 - 1/10
    [InlineData("whitelist", "wait-list", 0.667)]  // 1 - 3/9
    [InlineData("abc", "xyz", 0.0)]                // 1 - 3/3
    public void GetSimilarity_ReturnsExpectedScore(
        string source, string target, double expected)
    {
        var result = _sut.GetSimilarity(source, target);
        result.Should().BeApproximately(expected, 0.01);
    }

    #endregion

    #region Typo Pattern Tests

    [Theory]
    [MemberData(nameof(TypoPatterns.TranspositionErrors), MemberType = typeof(TypoPatterns))]
    public void IsMatch_TranspositionTypos_MatchAtReasonableThreshold(
        string correct, string typo, int _)
    {
        // Transposition typos should match at 0.7 threshold
        var result = _sut.IsMatch(correct, typo, threshold: 0.7);
        result.Should().BeTrue($"'{typo}' should match '{correct}' as a transposition typo");
    }

    [Theory]
    [MemberData(nameof(TypoPatterns.OmissionErrors), MemberType = typeof(TypoPatterns))]
    public void IsMatch_OmissionTypos_MatchAtReasonableThreshold(
        string correct, string typo, int _)
    {
        // Omission typos (missing letter) should match at 0.8 threshold
        var result = _sut.IsMatch(correct, typo, threshold: 0.8);
        result.Should().BeTrue($"'{typo}' should match '{correct}' as an omission typo");
    }

    [Theory]
    [MemberData(nameof(TypoPatterns.HyphenationVariations), MemberType = typeof(TypoPatterns))]
    public void IsMatch_HyphenVariations_MatchAtReasonableThreshold(
        string unhyphenated, string hyphenated, int _)
    {
        // Hyphenation variations should match at 0.8 threshold
        var result = _sut.IsMatch(unhyphenated, hyphenated, threshold: 0.8);
        result.Should().BeTrue($"'{hyphenated}' should match '{unhyphenated}' as a hyphen variation");
    }

    #endregion

    #region Document Scanning Tests

    [Fact]
    public void Scan_DocumentWithTypos_FindsAllMatches()
    {
        // Arrange
        var terms = new[] { "whitelist", "blacklist" };
        var document = "The white-list feature and black-list feature work well.";

        // Act
        var result = _sut.ScanDocument(document, terms, threshold: 0.8);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(m => m.Term == "whitelist" && m.Match == "white-list");
        result.Should().Contain(m => m.Term == "blacklist" && m.Match == "black-list");
    }

    [Fact]
    public void Scan_DocumentWithNoTypos_ReturnsEmpty()
    {
        // Arrange
        var terms = new[] { "whitelist", "blacklist" };
        var document = "The allowlist and blocklist features work well.";

        // Act
        var result = _sut.ScanDocument(document, terms, threshold: 0.8);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("", "test", 0.8, false)]
    [InlineData("test", "", 0.8, false)]
    public void IsMatch_EmptyString_ReturnsFalse(
        string term, string candidate, double threshold, bool expected)
    {
        var result = _sut.IsMatch(term, candidate, threshold);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("test", "test", -0.1)]
    [InlineData("test", "test", 1.1)]
    public void IsMatch_InvalidThreshold_ThrowsArgumentOutOfRange(
        string term, string candidate, double threshold)
    {
        var act = () => _sut.IsMatch(term, candidate, threshold);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    #endregion
}
```

### 6.3 Test Fixtures

```csharp
namespace Lexichord.Tests.Style.TestFixtures;

/// <summary>
/// Common typo patterns for fuzzy matching accuracy tests.
/// Each entry contains (correct, typo, expectedDistance).
/// </summary>
public static class TypoPatterns
{
    /// <summary>
    /// Transposition errors (swapped adjacent letters).
    /// Note: Levenshtein counts these as 2 operations (delete + insert).
    /// </summary>
    public static IEnumerable<object[]> TranspositionErrors => new[]
    {
        new object[] { "the", "teh", 2 },
        new object[] { "receive", "recieve", 2 },
        new object[] { "weird", "wierd", 2 },
        new object[] { "friend", "freind", 2 },
        new object[] { "believe", "beleive", 2 },
    };

    /// <summary>
    /// Omission errors (missing letters).
    /// </summary>
    public static IEnumerable<object[]> OmissionErrors => new[]
    {
        new object[] { "across", "acros", 1 },
        new object[] { "occurred", "occured", 1 },
        new object[] { "beginning", "begining", 1 },
        new object[] { "necessary", "necesary", 1 },
        new object[] { "tomorrow", "tommorow", 1 },
    };

    /// <summary>
    /// Insertion errors (extra letters).
    /// </summary>
    public static IEnumerable<object[]> InsertionErrors => new[]
    {
        new object[] { "separate", "seperate", 1 },
        new object[] { "definitely", "definately", 2 },
        new object[] { "occurrence", "occurrance", 2 },
        new object[] { "accommodate", "accomodate", 1 },
    };

    /// <summary>
    /// Hyphenation variations (should match).
    /// </summary>
    public static IEnumerable<object[]> HyphenationVariations => new[]
    {
        new object[] { "whitelist", "white-list", 1 },
        new object[] { "blacklist", "black-list", 1 },
        new object[] { "email", "e-mail", 1 },
        new object[] { "online", "on-line", 1 },
        new object[] { "realtime", "real-time", 1 },
    };

    /// <summary>
    /// Words that should NOT match at reasonable thresholds.
    /// </summary>
    public static IEnumerable<object[]> NonMatches => new[]
    {
        new object[] { "whitelist", "wait-list", 3 },
        new object[] { "affect", "effect", 2 },
        new object[] { "accept", "except", 2 },
        new object[] { "their", "there", 2 },
        new object[] { "than", "then", 1 },
    };
}
```

---

## 7. UI/UX Specifications

**Not applicable.** This is a test-only specification with no user-facing UI components.

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Testing distance: '{Source}' vs '{Target}' = {Distance}"` |
| Info | `"Fuzzy test suite completed: {PassCount}/{TotalCount} passed"` |
| Error | `"Accuracy test failed: expected {Expected}, got {Actual}"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| Test data injection | None | Tests use hardcoded values |
| Sensitive data in tests | None | No PII in test fixtures |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Identical strings | Calculating distance | Returns 0 |
| 2 | "whitelist" and "white-list" | Calculating distance | Returns 1 |
| 3 | "whitelist" and "wait-list" | Calculating distance | Returns 3 |
| 4 | "whitelist" and "white-list" at 0.8 threshold | Matching | Returns true |
| 5 | "whitelist" and "wait-list" at 0.8 threshold | Matching | Returns false |
| 6 | Empty string input | Calculating distance | Returns length of other string |
| 7 | Null input | Calculating distance | Throws ArgumentNullException |
| 8 | Invalid threshold (< 0 or > 1) | Matching | Throws ArgumentOutOfRangeException |

### 10.2 CI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 9 | Any accuracy test fails | CI runs tests | Build fails |
| 10 | All accuracy tests pass | CI runs tests | Build succeeds |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `LevenshteinDistanceTests.cs` | [ ] |
| 2 | `FuzzyMatchingAccuracyTests.cs` | [ ] |
| 3 | `TypoPatterns.cs` test fixture | [ ] |
| 4 | Test trait configuration | [ ] |
| 5 | CI filter for `Version=v0.3.8a` | [ ] |

---

## 12. Verification Commands

```bash
# Run all fuzzy accuracy tests
dotnet test --filter "Version=v0.3.8a" --logger "console;verbosity=detailed"

# Run only distance calculation tests
dotnet test --filter "FullyQualifiedName~LevenshteinDistanceTests"

# Run only matching tests
dotnet test --filter "FullyQualifiedName~FuzzyMatchingAccuracyTests"

# Run with coverage
dotnet test --filter "Version=v0.3.8a" --collect:"XPlat Code Coverage"
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-26 | Lead Architect | Initial draft |
