# LCS-DES-088d: Design Specification — Localization Accuracy

## 1. Metadata & Categorization

| Field | Value | Description |
| :--- | :--- | :--- |
| **Feature ID** | `TST-088d` | Sub-part of TST-088 |
| **Feature Name** | `Translation Quality Test Suite` | Localization accuracy tests |
| **Target Version** | `v0.8.8d` | Fourth sub-part of v0.8.8 |
| **Module Scope** | `Lexichord.Tests.Publishing` | Test project |
| **Swimlane** | `Governance` | Part of Publishing vertical |
| **License Tier** | `Core` | Testing available to all |
| **Feature Gate Key** | N/A | No gating for tests |
| **Author** | Lead Architect | |
| **Status** | `Draft` | |
| **Last Updated** | `2026-01-27` | |
| **Parent Document** | [LCS-DES-088-INDEX](./LCS-DES-088-INDEX.md) | |
| **Scope Breakdown** | [LCS-SBD-088 Section 3.4](./LCS-SBD-088.md#34-v088d-localization-accuracy) | |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord's localization features enable multi-language documentation. Without quality verification:

- Markdown formatting could be corrupted during translation
- Code blocks could be incorrectly translated
- Technical terms could be inconsistently rendered
- Locale-specific formatting (dates, numbers) could be incorrect
- Translation quality could silently degrade

> **Goal:** Verify that translated content maintains formatting integrity and achieves acceptable quality scores (BLEU >= 0.85 for European languages, >= 0.80 for Asian languages).

### 2.2 The Proposed Solution

Implement a comprehensive localization test suite that:

1. Tests Markdown formatting preservation across all translations
2. Verifies code blocks remain untranslated
3. Measures translation quality using BLEU scores against reference translations
4. Validates locale-specific formatting (numbers, dates, currencies)
5. Tests RTL (Right-to-Left) language support
6. Ensures Unicode content is properly handled

---

## 3. Architecture & Modular Strategy

### 3.1 Dependencies

#### 3.1.1 Systems Under Test

| Interface | Source Version | Purpose |
| :--- | :--- | :--- |
| `ILocalizationService` | v0.8.x | Translation service |
| `IMarkdownParser` | v0.1.3b | Markdown preservation validation |

#### 3.1.2 NuGet Packages

| Package | Version | Purpose |
| :--- | :--- | :--- |
| `xunit` | 2.9.x | Test framework |
| `FluentAssertions` | 6.x | Fluent assertions |

### 3.2 Licensing Behavior

No licensing required. Tests run in development/CI environments only.

---

## 4. Data Contract (The API)

### 4.1 BLEU Score Calculator Interface

```csharp
namespace Lexichord.Tests.Publishing.Localization;

/// <summary>
/// Interface for calculating translation quality scores.
/// </summary>
public interface ITranslationQualityScorer
{
    /// <summary>
    /// Calculate BLEU score comparing candidate translations to references.
    /// </summary>
    /// <param name="candidates">Translated texts to evaluate.</param>
    /// <param name="references">Reference translations for comparison.</param>
    /// <param name="maxNgramOrder">Maximum n-gram order (default 4).</param>
    /// <returns>BLEU score between 0.0 and 1.0.</returns>
    double CalculateBleuScore(
        IEnumerable<string> candidates,
        IEnumerable<string> references,
        int maxNgramOrder = 4);
}
```

### 4.2 Test Class Structure

```csharp
namespace Lexichord.Tests.Publishing.Localization;

/// <summary>
/// Tests for Markdown formatting preservation during translation.
/// </summary>
[Trait("Category", "Localization")]
[Trait("Version", "v0.8.8d")]
public class TranslationFormattingTests
{
    private readonly ILocalizationService _localization;
}

/// <summary>
/// Tests for translation quality using BLEU scores.
/// </summary>
[Trait("Category", "Localization")]
[Trait("Version", "v0.8.8d")]
public class TranslationQualityTests
{
    private readonly ILocalizationService _localization;
    private readonly ITranslationQualityScorer _scorer;
}

/// <summary>
/// Tests for locale-specific formatting (numbers, dates, currencies).
/// </summary>
[Trait("Category", "Localization")]
[Trait("Version", "v0.8.8d")]
public class LocaleSpecificTests
{
    private readonly ILocalizationService _localization;
}
```

---

## 5. Implementation Logic

### 5.1 BLEU Score Calculation

The BLEU (Bilingual Evaluation Understudy) score measures translation quality by comparing n-gram overlap between candidate translations and reference translations.

```text
BLEU CALCULATION:
|
+-- Tokenize candidate and reference texts
|
+-- For n = 1 to maxNgramOrder:
|   +-- Extract n-grams from candidate
|   +-- Extract n-grams from reference
|   +-- Count matching n-grams
|   +-- Calculate precision: matches / candidate_ngrams
|
+-- Calculate brevity penalty:
|   +-- If candidate_length >= reference_length: BP = 1
|   +-- Else: BP = exp(1 - reference_length / candidate_length)
|
+-- Calculate geometric mean of precisions
|
+-- BLEU = BP * geometric_mean(precisions)
```

### 5.2 Formatting Preservation Logic

```text
FORMATTING PRESERVATION CHECK:
|
+-- Parse source Markdown
|   +-- Extract heading markers (#)
|   +-- Extract bold markers (**)
|   +-- Extract italic markers (*)
|   +-- Extract code markers (` and ```)
|   +-- Extract link structure [text](url)
|   +-- Extract table structure (|, ---)
|
+-- Translate content
|
+-- Parse translated Markdown
|   +-- Same extractions
|
+-- Compare structures
|   +-- Heading count matches
|   +-- Bold/italic count matches
|   +-- Code blocks unchanged
|   +-- URLs preserved
|   +-- Table structure intact
```

---

## 6. Test Scenarios

### 6.1 TranslationFormattingTests

```csharp
[Trait("Category", "Localization")]
[Trait("Version", "v0.8.8d")]
public class TranslationFormattingTests
{
    private readonly ILocalizationService _localization;

    public TranslationFormattingTests()
    {
        _localization = new LocalizationService(/* config */);
    }

    #region Heading Preservation Tests

    [Theory]
    [InlineData("en", "de")]
    [InlineData("en", "fr")]
    [InlineData("en", "es")]
    [InlineData("en", "ja")]
    [InlineData("en", "zh")]
    public async Task Translate_Heading_PreservesMarker(string source, string target)
    {
        var markdown = "# Important Heading";

        var result = await _localization.TranslateAsync(markdown, source, target);

        result.Should().StartWith("# ");
        result.Should().NotStartWith("## ");
        result.Should().NotContain("\n#");
    }

    [Theory]
    [InlineData("## Second Level")]
    [InlineData("### Third Level")]
    [InlineData("#### Fourth Level")]
    public async Task Translate_MultiLevelHeadings_PreservesLevels(string heading)
    {
        var result = await _localization.TranslateAsync(heading, "en", "de");

        var expectedPrefix = heading.Substring(0, heading.IndexOf(' ') + 1);
        result.Should().StartWith(expectedPrefix);
    }

    [Fact]
    public async Task Translate_DocumentWithMultipleHeadings_PreservesAll()
    {
        var markdown = @"# Title
## Section 1
Content here.
## Section 2
More content.
### Subsection
Details.";

        var result = await _localization.TranslateAsync(markdown, "en", "de");

        var h1Count = Regex.Matches(result, @"^# ", RegexOptions.Multiline).Count;
        var h2Count = Regex.Matches(result, @"^## ", RegexOptions.Multiline).Count;
        var h3Count = Regex.Matches(result, @"^### ", RegexOptions.Multiline).Count;

        h1Count.Should().Be(1);
        h2Count.Should().Be(2);
        h3Count.Should().Be(1);
    }

    #endregion

    #region Inline Formatting Tests

    [Theory]
    [InlineData("**bold text**", "**")]
    [InlineData("*italic text*", "*")]
    [InlineData("***bold italic***", "***")]
    [InlineData("~~strikethrough~~", "~~")]
    public async Task Translate_InlineFormatting_PreservesMarkers(
        string input, string expectedMarker)
    {
        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain(expectedMarker);
        // Count opening and closing markers
        var markerCount = Regex.Matches(result, Regex.Escape(expectedMarker)).Count;
        markerCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task Translate_MixedFormatting_PreservesAll()
    {
        var input = "This has **bold**, *italic*, and `code` formatting.";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("**");
        result.Should().Contain("*");
        result.Should().Contain("`");
    }

    [Fact]
    public async Task Translate_NestedFormatting_PreservesStructure()
    {
        var input = "This is **bold with *nested italic* inside**.";

        var result = await _localization.TranslateAsync(input, "en", "fr");

        result.Should().Contain("**");
        result.Should().Contain("*");
    }

    #endregion

    #region Code Block Tests

    [Fact]
    public async Task Translate_InlineCode_PreservesExactContent()
    {
        var input = "Use `console.log()` for debugging.";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("`console.log()`");
    }

    [Theory]
    [InlineData("csharp", "var x = 1;")]
    [InlineData("javascript", "const foo = () => {};")]
    [InlineData("python", "def hello():\n    print('world')")]
    [InlineData("sql", "SELECT * FROM users;")]
    public async Task Translate_FencedCodeBlock_PreservesExactly(
        string language, string code)
    {
        var input = $"```{language}\n{code}\n```";

        var result = await _localization.TranslateAsync(input, "en", "ja");

        result.Should().Contain($"```{language}");
        result.Should().Contain(code);
        result.Should().Contain("```");
    }

    [Fact]
    public async Task Translate_CodeBlockWithComments_PreservesCode()
    {
        var input = @"```csharp
// This is a comment that could be translated
public class Example
{
    public void Method()
    {
        Console.WriteLine(""Hello"");
    }
}
```";

        var result = await _localization.TranslateAsync(input, "en", "de");

        // Code should be preserved exactly
        result.Should().Contain("public class Example");
        result.Should().Contain("Console.WriteLine");
    }

    [Fact]
    public async Task Translate_MultipleCodeBlocks_PreservesAll()
    {
        var input = @"First example:
```python
x = 1
```

Second example:
```javascript
const y = 2;
```";

        var result = await _localization.TranslateAsync(input, "en", "fr");

        result.Should().Contain("```python");
        result.Should().Contain("x = 1");
        result.Should().Contain("```javascript");
        result.Should().Contain("const y = 2;");
    }

    #endregion

    #region Link Preservation Tests

    [Theory]
    [InlineData("[Link](https://example.com)", "https://example.com")]
    [InlineData("[Docs](https://docs.example.com/api)", "https://docs.example.com/api")]
    [InlineData("![Image](https://example.com/img.png)", "https://example.com/img.png")]
    public async Task Translate_Links_PreservesUrls(string input, string expectedUrl)
    {
        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain(expectedUrl);
        result.Should().Contain("](");
    }

    [Fact]
    public async Task Translate_LinkWithTitle_PreservesStructure()
    {
        var input = "[Link](https://example.com \"Title Text\")";

        var result = await _localization.TranslateAsync(input, "en", "fr");

        result.Should().Contain("https://example.com");
        result.Should().MatchRegex(@"\[.*\]\(https://example\.com.*\)");
    }

    [Fact]
    public async Task Translate_ReferenceLink_PreservesReference()
    {
        var input = @"[Link Text][ref]

[ref]: https://example.com";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("[ref]");
        result.Should().Contain("[ref]: https://example.com");
    }

    #endregion

    #region Table Preservation Tests

    [Fact]
    public async Task Translate_SimpleTable_PreservesStructure()
    {
        var input = @"| Column A | Column B |
|----------|----------|
| Value 1  | Value 2  |";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("|");
        result.Should().Contain("---");

        // Count pipes to verify structure
        var pipeCount = result.Count(c => c == '|');
        pipeCount.Should().BeGreaterOrEqualTo(8); // 2 rows * 4 pipes minimum
    }

    [Fact]
    public async Task Translate_TableWithAlignment_PreservesAlignment()
    {
        var input = @"| Left | Center | Right |
|:-----|:------:|------:|
| A    | B      | C     |";

        var result = await _localization.TranslateAsync(input, "en", "fr");

        result.Should().Contain(":--");  // Left or center
        result.Should().Contain("--:");  // Right
    }

    [Fact]
    public async Task Translate_TableWithFormatting_PreservesBoth()
    {
        var input = @"| **Bold Header** | *Italic Header* |
|-----------------|-----------------|
| `code`          | normal          |";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("**");
        result.Should().Contain("*");
        result.Should().Contain("`");
    }

    #endregion

    #region RTL Language Tests

    [Theory]
    [InlineData("ar")]  // Arabic
    [InlineData("he")]  // Hebrew
    [InlineData("fa")]  // Persian
    public async Task Translate_ToRtlLanguage_MaintainsStructure(string rtlLocale)
    {
        var markdown = @"# Title
Some **bold** paragraph with [a link](https://example.com).

```javascript
const x = 1;
```";

        var result = await _localization.TranslateAsync(markdown, "en", rtlLocale);

        // Structure should be maintained
        result.Should().Contain("# ");
        result.Should().Contain("**");
        result.Should().Contain("[");
        result.Should().Contain("](");
        result.Should().Contain("```javascript");
        result.Should().Contain("const x = 1;");
    }

    [Fact]
    public async Task Translate_ArabicContent_PreservesEncoding()
    {
        var arabic = "\u0645\u0631\u062d\u0628\u0627 \u0628\u0627\u0644\u0639\u0627\u0644\u0645"; // "Hello World" in Arabic

        var result = await _localization.TranslateAsync(arabic, "ar", "en");

        result.Should().NotContain("\ufffd"); // No replacement characters
        result.Should().NotContain("???");
        result.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Blockquote and List Tests

    [Fact]
    public async Task Translate_Blockquote_PreservesMarker()
    {
        var input = @"> This is a quote.
> It spans multiple lines.";

        var result = await _localization.TranslateAsync(input, "en", "de");

        var quoteMarkerCount = Regex.Matches(result, @"^>", RegexOptions.Multiline).Count;
        quoteMarkerCount.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task Translate_UnorderedList_PreservesBullets()
    {
        var input = @"- Item one
- Item two
- Item three";

        var result = await _localization.TranslateAsync(input, "en", "fr");

        var bulletCount = Regex.Matches(result, @"^- ", RegexOptions.Multiline).Count;
        bulletCount.Should().Be(3);
    }

    [Fact]
    public async Task Translate_OrderedList_PreservesNumbers()
    {
        var input = @"1. First item
2. Second item
3. Third item";

        var result = await _localization.TranslateAsync(input, "en", "es");

        result.Should().Contain("1.");
        result.Should().Contain("2.");
        result.Should().Contain("3.");
    }

    [Fact]
    public async Task Translate_NestedList_PreservesIndentation()
    {
        var input = @"- Parent
  - Child
    - Grandchild";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("- ");
        result.Should().Contain("  - ");
    }

    #endregion

    #region Horizontal Rule and Break Tests

    [Fact]
    public async Task Translate_HorizontalRule_PreservesMarker()
    {
        var input = @"Section 1

---

Section 2";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().MatchRegex(@"---+");
    }

    #endregion
}
```

### 6.2 TranslationQualityTests

```csharp
[Trait("Category", "Localization")]
[Trait("Version", "v0.8.8d")]
public class TranslationQualityTests
{
    private readonly ILocalizationService _localization;
    private readonly ITranslationQualityScorer _scorer;

    public TranslationQualityTests()
    {
        _localization = new LocalizationService(/* config */);
        _scorer = new BleuScoreCalculator();
    }

    #region BLEU Score Threshold Tests

    [Theory]
    [InlineData("en", "de", 0.85)]
    [InlineData("en", "fr", 0.85)]
    [InlineData("en", "es", 0.85)]
    [InlineData("en", "it", 0.85)]
    [InlineData("en", "pt", 0.85)]
    [InlineData("en", "nl", 0.85)]
    public async Task Translate_EuropeanLanguages_MeetsQualityThreshold(
        string sourceLocale, string targetLocale, double minScore)
    {
        var testCorpus = await LoadTestCorpus(sourceLocale);
        var referenceTranslations = await LoadReferenceTranslations(targetLocale);

        var translations = new List<string>();
        foreach (var source in testCorpus)
        {
            var translation = await _localization.TranslateAsync(
                source, sourceLocale, targetLocale);
            translations.Add(translation);
        }

        var score = _scorer.CalculateBleuScore(translations, referenceTranslations);

        score.Should().BeGreaterOrEqualTo(minScore,
            $"Translation quality from {sourceLocale} to {targetLocale} " +
            $"should meet minimum BLEU score of {minScore}");
    }

    [Theory]
    [InlineData("en", "ja", 0.80)]
    [InlineData("en", "zh", 0.80)]
    [InlineData("en", "ko", 0.80)]
    public async Task Translate_AsianLanguages_MeetsQualityThreshold(
        string sourceLocale, string targetLocale, double minScore)
    {
        var testCorpus = await LoadTestCorpus(sourceLocale);
        var referenceTranslations = await LoadReferenceTranslations(targetLocale);

        var translations = new List<string>();
        foreach (var source in testCorpus)
        {
            var translation = await _localization.TranslateAsync(
                source, sourceLocale, targetLocale);
            translations.Add(translation);
        }

        var score = _scorer.CalculateBleuScore(translations, referenceTranslations);

        score.Should().BeGreaterOrEqualTo(minScore);
    }

    [Theory]
    [InlineData("en", "ar", 0.75)]
    [InlineData("en", "he", 0.75)]
    public async Task Translate_RtlLanguages_MeetsQualityThreshold(
        string sourceLocale, string targetLocale, double minScore)
    {
        var testCorpus = await LoadTestCorpus(sourceLocale);
        var referenceTranslations = await LoadReferenceTranslations(targetLocale);

        var translations = new List<string>();
        foreach (var source in testCorpus)
        {
            var translation = await _localization.TranslateAsync(
                source, sourceLocale, targetLocale);
            translations.Add(translation);
        }

        var score = _scorer.CalculateBleuScore(translations, referenceTranslations);

        score.Should().BeGreaterOrEqualTo(minScore);
    }

    #endregion

    #region Technical Term Consistency Tests

    [Fact]
    public async Task Translate_TechnicalTerms_MaintainsConsistency()
    {
        var terms = new Dictionary<string, string>
        {
            ["Git repository"] = "Git-Repository",
            ["pull request"] = "Pull-Request",
            ["commit"] = "Commit",
            ["branch"] = "Branch",
            ["merge"] = "Merge"
        };

        foreach (var (english, expectedGerman) in terms)
        {
            var result = await _localization.TranslateAsync(
                $"Create a new {english}.", "en", "de");

            result.Should().Contain(expectedGerman,
                $"Technical term '{english}' should be consistently translated");
        }
    }

    [Fact]
    public async Task Translate_ProductNames_PreservesExactly()
    {
        var productNames = new[] { "Lexichord", "GitHub", "MkDocs", "Docusaurus" };

        foreach (var name in productNames)
        {
            var input = $"Install {name} to get started.";
            var result = await _localization.TranslateAsync(input, "en", "de");

            result.Should().Contain(name,
                $"Product name '{name}' should be preserved exactly");
        }
    }

    [Fact]
    public async Task Translate_CodeIdentifiers_NeverTranslated()
    {
        var identifiers = new[] { "calculateTotal", "getUserById", "handleError" };

        foreach (var identifier in identifiers)
        {
            var input = $"Call the `{identifier}` function.";
            var result = await _localization.TranslateAsync(input, "en", "ja");

            result.Should().Contain(identifier);
        }
    }

    #endregion

    #region Pluralization Tests

    [Theory]
    [InlineData("1 file", "de", "1 Datei")]
    [InlineData("2 files", "de", "2 Dateien")]
    [InlineData("1 commit", "fr", "1 commit")]
    [InlineData("5 commits", "fr", "5 commits")]
    [InlineData("1 error", "es", "1 error")]
    [InlineData("3 errors", "es", "3 errores")]
    public async Task Translate_Pluralization_HandlesCorrectly(
        string input, string targetLocale, string expectedPattern)
    {
        var result = await _localization.TranslateAsync(input, "en", targetLocale);

        result.Should().Contain(expectedPattern);
    }

    #endregion

    #region Context-Sensitive Translation Tests

    [Fact]
    public async Task Translate_SameWordDifferentContext_TranslatesAppropriately()
    {
        // "Run" can mean different things
        var runCode = "Run the code.";
        var runTests = "Run all tests.";

        var resultCode = await _localization.TranslateAsync(runCode, "en", "de");
        var resultTests = await _localization.TranslateAsync(runTests, "en", "de");

        // Both should be translated (not empty)
        resultCode.Should().NotBeNullOrWhiteSpace();
        resultTests.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Helper Methods

    private async Task<IReadOnlyList<string>> LoadTestCorpus(string locale)
    {
        // Load test corpus from embedded resources or files
        return await Task.FromResult(TestCorpus.GetCorpus(locale));
    }

    private async Task<IReadOnlyList<string>> LoadReferenceTranslations(string locale)
    {
        // Load reference translations from embedded resources or files
        return await Task.FromResult(ReferenceTranslations.GetTranslations(locale));
    }

    #endregion
}
```

### 6.3 LocaleSpecificTests

```csharp
[Trait("Category", "Localization")]
[Trait("Version", "v0.8.8d")]
public class LocaleSpecificTests
{
    private readonly ILocalizationService _localization;

    public LocaleSpecificTests()
    {
        _localization = new LocalizationService(/* config */);
    }

    #region Number Formatting Tests

    [Theory]
    [InlineData("en-US", 1234.56, "1,234.56")]
    [InlineData("de-DE", 1234.56, "1.234,56")]
    [InlineData("fr-FR", 1234.56, "1 234,56")]
    [InlineData("ja-JP", 1234.56, "1,234.56")]
    [InlineData("ar-SA", 1234.56, "\u0661\u066c\u0662\u0663\u0664\u066b\u0665\u0666")]
    public void FormatNumber_LocaleSpecific_UsesCorrectFormat(
        string locale, double number, string expected)
    {
        var result = _localization.FormatNumber(number, locale);

        // Allow for slight formatting variations
        result.Replace("\u00a0", " ").Should().Be(expected.Replace("\u00a0", " "));
    }

    [Theory]
    [InlineData("en-US", 1000000, "1,000,000")]
    [InlineData("de-DE", 1000000, "1.000.000")]
    [InlineData("en-IN", 1000000, "10,00,000")]
    public void FormatLargeNumber_LocaleSpecific_UsesCorrectGrouping(
        string locale, int number, string expected)
    {
        var result = _localization.FormatNumber(number, locale);
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("en-US", -1234.56, "-1,234.56")]
    [InlineData("de-DE", -1234.56, "-1.234,56")]
    public void FormatNegativeNumber_LocaleSpecific_UsesCorrectFormat(
        string locale, double number, string expected)
    {
        var result = _localization.FormatNumber(number, locale);
        result.Should().Be(expected);
    }

    #endregion

    #region Date Formatting Tests

    [Theory]
    [InlineData("en-US", "January 27, 2026")]
    [InlineData("en-GB", "27 January 2026")]
    [InlineData("de-DE", "27. Januar 2026")]
    [InlineData("fr-FR", "27 janvier 2026")]
    [InlineData("ja-JP", "2026\u5e741\u670827\u65e5")]
    [InlineData("zh-CN", "2026\u5e741\u670827\u65e5")]
    public void FormatDate_LocaleSpecific_UsesCorrectFormat(
        string locale, string expected)
    {
        var date = new DateTime(2026, 1, 27);

        var result = _localization.FormatDate(date, locale);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("en-US", "1/27/2026")]
    [InlineData("en-GB", "27/01/2026")]
    [InlineData("de-DE", "27.01.2026")]
    [InlineData("fr-FR", "27/01/2026")]
    [InlineData("ja-JP", "2026/01/27")]
    public void FormatShortDate_LocaleSpecific_UsesCorrectFormat(
        string locale, string expected)
    {
        var date = new DateTime(2026, 1, 27);

        var result = _localization.FormatShortDate(date, locale);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("en-US", "3:30 PM")]
    [InlineData("en-GB", "15:30")]
    [InlineData("de-DE", "15:30")]
    [InlineData("fr-FR", "15:30")]
    public void FormatTime_LocaleSpecific_UsesCorrectFormat(
        string locale, string expected)
    {
        var time = new TimeSpan(15, 30, 0);

        var result = _localization.FormatTime(time, locale);

        result.Should().Be(expected);
    }

    #endregion

    #region Currency Formatting Tests

    [Theory]
    [InlineData("en-US", 1234.56, "USD", "$1,234.56")]
    [InlineData("de-DE", 1234.56, "EUR", "1.234,56 \u20ac")]
    [InlineData("en-GB", 1234.56, "GBP", "\u00a31,234.56")]
    [InlineData("ja-JP", 1234, "JPY", "\uffe51,234")]
    public void FormatCurrency_LocaleSpecific_UsesCorrectFormat(
        string locale, double amount, string currency, string expected)
    {
        var result = _localization.FormatCurrency(amount, currency, locale);

        // Allow for slight variations in spacing
        result.Replace("\u00a0", " ").Should().Be(expected.Replace("\u00a0", " "));
    }

    [Fact]
    public void FormatCurrency_NegativeAmount_FormatsCorrectly()
    {
        var result = _localization.FormatCurrency(-1234.56, "USD", "en-US");

        result.Should().Contain("-");
        result.Should().Contain("$");
        result.Should().Contain("1,234.56");
    }

    #endregion

    #region Unicode Content Tests

    [Theory]
    [InlineData("\u4e2d\u6587\u6d4b\u8bd5")]  // Chinese: "Chinese test"
    [InlineData("\u65e5\u672c\u8a9e\u30c6\u30b9\u30c8")]  // Japanese: "Japanese test"
    [InlineData("\ud55c\uad6d\uc5b4 \ud14c\uc2a4\ud2b8")]  // Korean: "Korean test"
    [InlineData("\u0422\u0435\u0441\u0442")]  // Russian: "Test"
    [InlineData("\u03b4\u03bf\u03ba\u03b9\u03bc\u03ae")]  // Greek: "test"
    [InlineData("\u05d8\u05e2\u05e1\u05d8")]  // Hebrew: "test"
    [InlineData("\u0627\u062e\u062a\u0628\u0627\u0631")]  // Arabic: "test"
    public async Task Translate_UnicodeContent_PreservesEncoding(string content)
    {
        var markdown = $"# Title\n\n{content}";

        var result = await _localization.TranslateAsync(markdown, "auto", "en");

        result.Should().NotContain("\ufffd"); // No replacement characters
        result.Should().NotContain("???");
        result.Length.Should().BeGreaterThan(10);
    }

    [Fact]
    public async Task Translate_Emoji_PreservesCharacters()
    {
        var input = "This is great! \U0001F60A \U0001F680 \U0001F4DD";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().Contain("\U0001F60A"); // Smiling face
        result.Should().Contain("\U0001F680"); // Rocket
        result.Should().Contain("\U0001F4DD"); // Memo
    }

    [Fact]
    public async Task Translate_MixedScripts_HandlesCorrectly()
    {
        var input = "The Japanese word \u3053\u3093\u306b\u3061\u306f means hello.";

        var result = await _localization.TranslateAsync(input, "en", "de");

        result.Should().NotContain("\ufffd");
        result.Should().Contain("\u3053\u3093\u306b\u3061\u306f"); // Preserves Japanese
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public async Task Translate_EmptyString_ReturnsEmpty()
    {
        var result = await _localization.TranslateAsync("", "en", "de");
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Translate_WhitespaceOnly_PreservesWhitespace()
    {
        var result = await _localization.TranslateAsync("   ", "en", "de");
        result.Should().Be("   ");
    }

    [Fact]
    public async Task Translate_SingleCharacter_TranslatesOrPreserves()
    {
        var result = await _localization.TranslateAsync("A", "en", "de");
        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Translate_VeryLongText_CompletesSuccessfully()
    {
        var longText = string.Join(" ", Enumerable.Repeat(
            "This is a sentence that will be repeated many times.", 100));

        var result = await _localization.TranslateAsync(longText, "en", "de");

        result.Should().NotBeNullOrEmpty();
        result.Length.Should().BeGreaterThan(longText.Length / 2); // Reasonable length
    }

    #endregion
}
```

---

## 7. Test Fixtures

### 7.1 Test Corpus

```csharp
namespace Lexichord.Tests.Publishing.TestFixtures;

/// <summary>
/// Standard test corpus for translation quality evaluation.
/// </summary>
public static class TestCorpus
{
    /// <summary>
    /// Get test corpus for a given source locale.
    /// </summary>
    public static IReadOnlyList<string> GetCorpus(string locale) => locale switch
    {
        "en" => EnglishCorpus,
        _ => throw new ArgumentException($"No corpus for locale: {locale}")
    };

    private static readonly IReadOnlyList<string> EnglishCorpus = new[]
    {
        "Welcome to the documentation.",
        "This guide will help you get started.",
        "Follow the installation instructions carefully.",
        "The configuration file is located in the root directory.",
        "Make sure to back up your data before proceeding.",
        "The API supports both GET and POST requests.",
        "Error handling is critical for robust applications.",
        "Performance optimization should be done after profiling.",
        "Unit tests ensure code quality and prevent regressions.",
        "The release notes contain important upgrade information."
    };
}

/// <summary>
/// Reference translations for BLEU score calculation.
/// </summary>
public static class ReferenceTranslations
{
    /// <summary>
    /// Get reference translations for a target locale.
    /// </summary>
    public static IReadOnlyList<string> GetTranslations(string locale) => locale switch
    {
        "de" => GermanTranslations,
        "fr" => FrenchTranslations,
        "es" => SpanishTranslations,
        "ja" => JapaneseTranslations,
        "zh" => ChineseTranslations,
        _ => throw new ArgumentException($"No references for locale: {locale}")
    };

    private static readonly IReadOnlyList<string> GermanTranslations = new[]
    {
        "Willkommen in der Dokumentation.",
        "Diese Anleitung hilft Ihnen beim Einstieg.",
        "Befolgen Sie die Installationsanweisungen sorgf\u00e4ltig.",
        "Die Konfigurationsdatei befindet sich im Stammverzeichnis.",
        "Sichern Sie Ihre Daten, bevor Sie fortfahren.",
        "Die API unterst\u00fctzt sowohl GET- als auch POST-Anfragen.",
        "Fehlerbehandlung ist entscheidend f\u00fcr robuste Anwendungen.",
        "Leistungsoptimierung sollte nach dem Profiling erfolgen.",
        "Unit-Tests stellen Codequalit\u00e4t sicher und verhindern Regressionen.",
        "Die Release Notes enthalten wichtige Upgrade-Informationen."
    };

    private static readonly IReadOnlyList<string> FrenchTranslations = new[]
    {
        "Bienvenue dans la documentation.",
        "Ce guide vous aidera \u00e0 d\u00e9marrer.",
        "Suivez attentivement les instructions d'installation.",
        "Le fichier de configuration se trouve dans le r\u00e9pertoire racine.",
        "Assurez-vous de sauvegarder vos donn\u00e9es avant de continuer.",
        "L'API prend en charge les requ\u00eates GET et POST.",
        "La gestion des erreurs est essentielle pour les applications robustes.",
        "L'optimisation des performances doit \u00eatre effectu\u00e9e apr\u00e8s le profilage.",
        "Les tests unitaires assurent la qualit\u00e9 du code et pr\u00e9viennent les r\u00e9gressions.",
        "Les notes de version contiennent des informations importantes sur la mise \u00e0 niveau."
    };

    private static readonly IReadOnlyList<string> SpanishTranslations = new[]
    {
        "Bienvenido a la documentaci\u00f3n.",
        "Esta gu\u00eda te ayudar\u00e1 a empezar.",
        "Sigue las instrucciones de instalaci\u00f3n cuidadosamente.",
        "El archivo de configuraci\u00f3n se encuentra en el directorio ra\u00edz.",
        "Aseg\u00farate de hacer una copia de seguridad de tus datos antes de continuar.",
        "La API admite solicitudes GET y POST.",
        "El manejo de errores es fundamental para aplicaciones robustas.",
        "La optimizaci\u00f3n del rendimiento debe hacerse despu\u00e9s del an\u00e1lisis.",
        "Las pruebas unitarias aseguran la calidad del c\u00f3digo y previenen regresiones.",
        "Las notas de la versi\u00f3n contienen informaci\u00f3n importante sobre la actualizaci\u00f3n."
    };

    private static readonly IReadOnlyList<string> JapaneseTranslations = new[]
    {
        "\u30c9\u30ad\u30e5\u30e1\u30f3\u30c8\u3078\u3088\u3046\u3053\u305d\u3002",
        "\u3053\u306e\u30ac\u30a4\u30c9\u306f\u59cb\u3081\u308b\u306e\u306b\u5f79\u7acb\u3061\u307e\u3059\u3002",
        "\u30a4\u30f3\u30b9\u30c8\u30fc\u30eb\u624b\u9806\u306b\u6ce8\u610f\u6df1\u304f\u5f93\u3063\u3066\u304f\u3060\u3055\u3044\u3002",
        "\u8a2d\u5b9a\u30d5\u30a1\u30a4\u30eb\u306f\u30eb\u30fc\u30c8\u30c7\u30a3\u30ec\u30af\u30c8\u30ea\u306b\u3042\u308a\u307e\u3059\u3002",
        "\u7d9a\u884c\u3059\u308b\u524d\u306b\u30c7\u30fc\u30bf\u3092\u30d0\u30c3\u30af\u30a2\u30c3\u30d7\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "API\u306fGET\u3068POST\u30ea\u30af\u30a8\u30b9\u30c8\u306e\u4e21\u65b9\u3092\u30b5\u30dd\u30fc\u30c8\u3057\u3066\u3044\u307e\u3059\u3002",
        "\u30a8\u30e9\u30fc\u51e6\u7406\u306f\u5805\u7262\u306a\u30a2\u30d7\u30ea\u30b1\u30fc\u30b7\u30e7\u30f3\u306b\u4e0d\u53ef\u6b20\u3067\u3059\u3002",
        "\u30d1\u30d5\u30a9\u30fc\u30de\u30f3\u30b9\u6700\u9069\u5316\u306f\u30d7\u30ed\u30d5\u30a1\u30a4\u30ea\u30f3\u30b0\u5f8c\u306b\u884c\u3046\u3079\u304d\u3067\u3059\u3002",
        "\u30e6\u30cb\u30c3\u30c8\u30c6\u30b9\u30c8\u306f\u30b3\u30fc\u30c9\u54c1\u8cea\u3092\u78ba\u4fdd\u3057\u3001\u30ea\u30b0\u30ec\u30c3\u30b7\u30e7\u30f3\u3092\u9632\u304e\u307e\u3059\u3002",
        "\u30ea\u30ea\u30fc\u30b9\u30ce\u30fc\u30c8\u306b\u306f\u91cd\u8981\u306a\u30a2\u30c3\u30d7\u30b0\u30ec\u30fc\u30c9\u60c5\u5831\u304c\u542b\u307e\u308c\u3066\u3044\u307e\u3059\u3002"
    };

    private static readonly IReadOnlyList<string> ChineseTranslations = new[]
    {
        "\u6b22\u8fce\u9605\u8bfb\u6587\u6863\u3002",
        "\u672c\u6307\u5357\u5c06\u5e2e\u52a9\u60a8\u5165\u95e8\u3002",
        "\u8bf7\u4ed4\u7ec6\u6309\u7167\u5b89\u88c5\u8bf4\u660e\u8fdb\u884c\u64cd\u4f5c\u3002",
        "\u914d\u7f6e\u6587\u4ef6\u4f4d\u4e8e\u6839\u76ee\u5f55\u3002",
        "\u5728\u7ee7\u7eed\u4e4b\u524d\uff0c\u8bf7\u786e\u4fdd\u5907\u4efd\u60a8\u7684\u6570\u636e\u3002",
        "API\u652f\u6301GET\u548cPOST\u8bf7\u6c42\u3002",
        "\u9519\u8bef\u5904\u7406\u5bf9\u4e8e\u5065\u58ee\u7684\u5e94\u7528\u7a0b\u5e8f\u81f3\u5173\u91cd\u8981\u3002",
        "\u6027\u80fd\u4f18\u5316\u5e94\u5728\u5206\u6790\u4e4b\u540e\u8fdb\u884c\u3002",
        "\u5355\u5143\u6d4b\u8bd5\u786e\u4fdd\u4ee3\u7801\u8d28\u91cf\u5e76\u9632\u6b62\u56de\u5f52\u3002",
        "\u53d1\u5e03\u8bf4\u660e\u5305\u542b\u91cd\u8981\u7684\u5347\u7ea7\u4fe1\u606f\u3002"
    };
}
```

### 7.2 BLEU Score Calculator

```csharp
namespace Lexichord.Tests.Publishing.Localization;

/// <summary>
/// Calculates BLEU (Bilingual Evaluation Understudy) score for translation quality.
/// </summary>
public class BleuScoreCalculator : ITranslationQualityScorer
{
    /// <inheritdoc />
    public double CalculateBleuScore(
        IEnumerable<string> candidates,
        IEnumerable<string> references,
        int maxNgramOrder = 4)
    {
        var candidateList = candidates.ToList();
        var referenceList = references.ToList();

        if (candidateList.Count != referenceList.Count)
            throw new ArgumentException("Candidate and reference counts must match");

        if (candidateList.Count == 0)
            return 0.0;

        var precisions = new double[maxNgramOrder];
        var candidateLengthTotal = 0;
        var referenceLengthTotal = 0;

        for (int n = 1; n <= maxNgramOrder; n++)
        {
            var matchCount = 0;
            var candidateCount = 0;

            for (int i = 0; i < candidateList.Count; i++)
            {
                var candidateNgrams = GetNgrams(candidateList[i], n);
                var referenceNgrams = GetNgrams(referenceList[i], n);

                var refNgramCounts = referenceNgrams
                    .GroupBy(ng => ng)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var ngram in candidateNgrams)
                {
                    candidateCount++;
                    if (refNgramCounts.TryGetValue(ngram, out var refCount) && refCount > 0)
                    {
                        matchCount++;
                        refNgramCounts[ngram]--;
                    }
                }

                if (n == 1)
                {
                    candidateLengthTotal += TokenizeWords(candidateList[i]).Count;
                    referenceLengthTotal += TokenizeWords(referenceList[i]).Count;
                }
            }

            precisions[n - 1] = candidateCount > 0
                ? (double)matchCount / candidateCount
                : 0;
        }

        // Handle zero precisions
        if (precisions.All(p => p == 0))
            return 0.0;

        // Calculate brevity penalty
        var brevityPenalty = candidateLengthTotal >= referenceLengthTotal
            ? 1.0
            : Math.Exp(1 - (double)referenceLengthTotal / candidateLengthTotal);

        // Calculate geometric mean of precisions (with smoothing for zeros)
        var logPrecisionSum = precisions
            .Select(p => p > 0 ? Math.Log(p) : Math.Log(1e-10))
            .Sum();
        var geometricMean = Math.Exp(logPrecisionSum / maxNgramOrder);

        return brevityPenalty * geometricMean;
    }

    private static IEnumerable<string> GetNgrams(string text, int n)
    {
        var words = TokenizeWords(text);
        for (int i = 0; i <= words.Count - n; i++)
        {
            yield return string.Join(" ", words.Skip(i).Take(n));
        }
    }

    private static List<string> TokenizeWords(string text)
    {
        return Regex.Split(text.ToLowerInvariant(), @"\W+")
            .Where(w => !string.IsNullOrEmpty(w))
            .ToList();
    }
}
```

---

## 8. Observability & Logging

| Level | Message Template |
| :--- | :--- |
| Debug | `"Translating from {SourceLocale} to {TargetLocale}"` |
| Debug | `"Formatting check: {ElementType} count: {Expected} vs {Actual}"` |
| Info | `"BLEU score for {SourceLocale}->{TargetLocale}: {Score:F4}"` |
| Warning | `"Translation quality below threshold: {Score:F4} < {Threshold:F4}"` |
| Error | `"Translation failed: {Error}"` |

---

## 9. Security & Safety

| Risk | Level | Mitigation |
| :--- | :--- | :--- |
| PII in test data | None | Use synthetic test corpus only |
| Cultural sensitivity | Low | Use neutral technical content |
| Reference data bias | Medium | Use multiple reference sources |

---

## 10. Acceptance Criteria

### 10.1 Functional Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 1 | Markdown with headings | Translating | Heading markers preserved |
| 2 | Markdown with code block | Translating | Code content unchanged |
| 3 | Markdown with links | Translating | URLs preserved |
| 4 | English to German | Calculating BLEU | Score >= 0.85 |
| 5 | English to Japanese | Calculating BLEU | Score >= 0.80 |
| 6 | Number 1234.56 | Formatting for de-DE | Returns "1.234,56" |
| 7 | Date 2026-01-27 | Formatting for en-US | Returns "January 27, 2026" |

### 10.2 CI Criteria

| # | Given | When | Then |
| :--- | :--- | :--- | :--- |
| 8 | BLEU score below threshold | CI runs tests | Build fails |
| 9 | All localization tests pass | CI runs tests | Build succeeds |

---

## 11. Deliverable Checklist

| # | Deliverable | Status |
| :--- | :--- | :--- |
| 1 | `TranslationFormattingTests.cs` with 35+ tests | [ ] |
| 2 | `TranslationQualityTests.cs` with 25+ tests | [ ] |
| 3 | `LocaleSpecificTests.cs` with 30+ tests | [ ] |
| 4 | `BleuScoreCalculator.cs` | [ ] |
| 5 | `TestCorpus.cs` fixtures | [ ] |
| 6 | `ReferenceTranslations.cs` fixtures | [ ] |
| 7 | Test trait configuration | [ ] |

---

## 12. Verification Commands

```bash
# Run all localization tests
dotnet test --filter "Version=v0.8.8d" --logger "console;verbosity=detailed"

# Run only formatting preservation tests
dotnet test --filter "FullyQualifiedName~TranslationFormattingTests"

# Run only BLEU score tests
dotnet test --filter "FullyQualifiedName~TranslationQualityTests"

# Run only locale-specific tests
dotnet test --filter "FullyQualifiedName~LocaleSpecificTests"

# Run tests for specific language pair
dotnet test --filter "Version=v0.8.8d" -- TestRunParameters.Parameter(name=\"targetLocale\", value=\"de\")
```

---

## Document History

| Version | Date | Author | Changes |
| :--- | :--- | :--- | :--- |
| 1.0 | 2026-01-27 | Lead Architect | Initial draft |
