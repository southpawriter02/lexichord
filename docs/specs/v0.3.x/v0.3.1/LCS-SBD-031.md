# LCS-SBD: Scope Breakdown - v0.3.1

**Target Version:** `v0.3.1`
**Codename:** The Fuzzy Engine (Advanced Matching)
**Timeline:** Sprint 3.1 (Algorithmic Analysis)
**Owner:** Lead Architect
**Prerequisites:** v0.2.7 complete (Governance Polish, Thread Safety).

## 1. Executive Summary

**v0.3.1** introduces **fuzzy string matching** to the Style Engine. While v0.2.x used exact regex patterns to catch violations, v0.3.1 adds approximate matching to detect typos and variations of forbidden terms (e.g., catching "white-list" or "whitelist" when "allowlist" is preferred).

This is the first feature with **soft licensing gates**—Core users get the Regex linter, but Fuzzy Matching is a Writer Pro feature.

**Success Metrics:**

1. Fuzzy matching detects variations with edit distance ≤ threshold.
2. Performance remains acceptable (< 100ms for 1,000 unique words).
3. License gating prevents Core users from triggering fuzzy scans.
4. No double-counting of violations (regex + fuzzy matching same word).

If this foundation is flawed, the Voice Profiler (v0.3.4) and Resonance Dashboard (v0.3.5) will lack the analysis infrastructure.

---

## 2. Sub-Part Specifications

### v0.3.1a: Algorithm Integration

**Goal:** Integrate a fuzzy string matching algorithm via NuGet package.

- **Task 1.1: Package Selection**
    - Evaluate `FuzzySharp` (Levenshtein + partial match) vs `Fastenshtein` (pure Levenshtein, faster).
    - Decision: Use `FuzzySharp` 2.x for its richer API (supports partial ratios, weighted).
    - Add to `Lexichord.Modules.Style` project.
- **Task 1.2: Distance Service Interface**
    - Create `IFuzzyMatchService` interface in `Lexichord.Abstractions`.
    - Methods: `CalculateDistance(string source, string target)`, `IsMatch(string source, string target, double threshold)`.
- **Task 1.3: Distance Service Implementation**
    - Create `FuzzyMatchService` in `Modules.Style/Services`.
    - Wrap `FuzzySharp.Fuzz.Ratio()` for percentage-based matching.
    - Normalize inputs: convert to lowercase, trim whitespace.
- **Task 1.4: DI Registration**
    - Register `FuzzyMatchService` as singleton in `StyleModule.cs`.

**Definition of Done:**

- `IFuzzyMatchService` interface exists in Abstractions.
- Unit tests verify: "whitelist" matches "white-list" (≥ 80%), "whitelist" does not match "waitlist" (< 80%).
- Service registered in DI container.

---

### v0.3.1b: Repository Update

**Goal:** Extend the terminology database to support fuzzy matching configuration.

- **Task 1.1: Schema Migration**
    - Create FluentMigrator migration: `AddFuzzyColumnsToStyleTerms`.
    - Add columns:
        - `fuzzy_enabled` (BOOLEAN, default FALSE)
        - `fuzzy_threshold` (DECIMAL, default 0.80)
- **Task 1.2: Entity Update**
    - Update `StyleTerm` entity to include `FuzzyEnabled` and `FuzzyThreshold` properties.
    - Ensure backward compatibility (existing terms default to fuzzy disabled).
- **Task 1.3: Repository Extension**
    - Add method to `ITerminologyRepository`: `GetFuzzyEnabledTermsAsync()`.
    - Query: `SELECT * FROM style_terms WHERE fuzzy_enabled = true`.
- **Task 1.4: Seed Data Update**
    - Update seed data to enable fuzzy matching for common substitution terms.
    - Examples: "whitelist" → "allowlist", "blacklist" → "blocklist".

**Definition of Done:**

- Migration runs successfully on existing databases.
- `GetFuzzyEnabledTermsAsync()` returns only fuzzy-configured terms.
- Seed data includes at least 5 fuzzy-enabled terms.

---

### v0.3.1c: The Fuzzy Scanner

**Goal:** Implement a secondary scan loop that performs fuzzy matching on document words.

- **Task 1.1: Document Tokenizer**
    - Create `IDocumentTokenizer` interface.
    - Implement `DocumentTokenizer` that:
        - Splits text into words (regex: `\b\w+\b`).
        - Returns `HashSet<string>` of unique words (for performance).
        - Normalizes to lowercase.
- **Task 1.2: Fuzzy Scanner Interface**
    - Create `IFuzzyScanner` interface in Abstractions.
    - Method: `ScanAsync(string documentText, CancellationToken ct)`.
    - Returns: `IReadOnlyList<StyleViolation>`.
- **Task 1.3: Fuzzy Scanner Implementation**
    - Create `FuzzyScanner` in Modules.Style.
    - Logic:
        1. Tokenize document into unique words.
        2. Load fuzzy-enabled terms from repository.
        3. For each unique word:
            - Check if already flagged by regex (skip to avoid double-count).
            - Compare against each fuzzy term using `IFuzzyMatchService`.
            - If match ratio ≥ term threshold, create `StyleViolation`.
    - Optimization: Use parallel processing for large documents.
- **Task 1.4: Orchestrator Integration**
    - Update `ILintingOrchestrator` to call `IFuzzyScanner` after regex scan.
    - New event: `FuzzyScanCompletedEvent` (optional, for metrics).
    - Aggregate results with regex violations in final `LintingCompletedEvent`.

**Definition of Done:**

- Fuzzy scanner detects "white-list" when "whitelist" is a fuzzy term.
- No duplicate violations (same word flagged by both regex and fuzzy).
- Performance: < 100ms for 1,000 unique words against 50 fuzzy terms.

---

### v0.3.1d: License Gating

**Goal:** Restrict fuzzy scanning to Writer Pro tier and above.

- **Task 1.1: Feature Gate Definition**
    - Add feature key: `Feature.FuzzyMatching` to feature registry.
    - Map to minimum tier: `LicenseTier.WriterPro`.
- **Task 1.2: Gate Implementation**
    - Wrap `FuzzyScanner.ScanAsync()` in license check.
    - Logic:
        ```csharp
        if (!_licenseContext.HasFeature(Feature.FuzzyMatching))
            return Array.Empty<StyleViolation>();
        ```
- **Task 1.3: UI Indication**
    - In Terminology Editor (v0.2.5b), disable "Fuzzy Match" toggle for Core users.
    - Show tooltip: "Upgrade to Writer Pro to enable fuzzy matching."
- **Task 1.4: Settings Integration**
    - Add global toggle in Settings → Style → Advanced: "Enable Fuzzy Matching".
    - This toggle is only visible/editable for Writer Pro users.

**Definition of Done:**

- Core-tier users never see fuzzy match violations.
- Writer Pro users see fuzzy violations when enabled.
- UI gracefully indicates feature availability.

---

## 3. Implementation Checklist (for Developer)

| Step       | Description                                                               | Status |
| :--------- | :------------------------------------------------------------------------ | :----- |
| **0.3.1a** | `FuzzySharp` NuGet package added to `Modules.Style` project.              | [ ]    |
| **0.3.1a** | `IFuzzyMatchService` interface defined in Abstractions.                   | [ ]    |
| **0.3.1a** | `FuzzyMatchService` implementation with `Fuzz.Ratio()` wrapper.           | [ ]    |
| **0.3.1a** | Unit tests: "whitelist" matches "white-list" (≥ 80%).                     | [ ]    |
| **0.3.1b** | FluentMigrator migration adds `fuzzy_enabled`, `fuzzy_threshold` columns. | [ ]    |
| **0.3.1b** | `StyleTerm` entity updated with new properties.                           | [ ]    |
| **0.3.1b** | `GetFuzzyEnabledTermsAsync()` method added to repository.                 | [ ]    |
| **0.3.1b** | Seed data includes fuzzy-enabled terms.                                   | [ ]    |
| **0.3.1c** | `IDocumentTokenizer` interface and implementation.                        | [ ]    |
| **0.3.1c** | `IFuzzyScanner` interface defined in Abstractions.                        | [ ]    |
| **0.3.1c** | `FuzzyScanner` implementation with double-count prevention.               | [ ]    |
| **0.3.1c** | Orchestrator integration calls fuzzy scanner.                             | [ ]    |
| **0.3.1c** | Performance test: < 100ms for 1,000 words / 50 terms.                     | [ ]    |
| **0.3.1d** | `Feature.FuzzyMatching` added to feature registry.                        | [ ]    |
| **0.3.1d** | License check in `FuzzyScanner.ScanAsync()`.                              | [ ]    |
| **0.3.1d** | UI disables fuzzy toggle for Core users.                                  | [ ]    |

---

## 4. Dependency Matrix

### 4.1 Required Interfaces (from earlier versions)

| Interface                | Source Version | Purpose                              |
| :----------------------- | :------------- | :----------------------------------- |
| `StyleTerm`              | v0.2.2a        | Entity model for terminology         |
| `ITerminologyRepository` | v0.2.2b        | Database access for terms            |
| `ILintingOrchestrator`   | v0.2.3a        | Reactive linting coordinator         |
| `IStyleScanner`          | v0.2.3c        | Existing regex scanner interface     |
| `StyleViolation`         | v0.2.1b        | Violation result record              |
| `ILicenseContext`        | v0.0.4c        | Read-only license tier access        |
| `LicenseTier`            | v0.0.4c        | Core/WriterPro/Teams/Enterprise enum |

### 4.2 New Interfaces (defined in v0.3.1)

| Interface            | Defined In | Module       | Purpose                        |
| :------------------- | :--------- | :----------- | :----------------------------- |
| `IFuzzyMatchService` | v0.3.1a    | Abstractions | String distance calculation    |
| `IDocumentTokenizer` | v0.3.1c    | Abstractions | Text tokenization for scanning |
| `IFuzzyScanner`      | v0.3.1c    | Abstractions | Fuzzy violation detection      |

### 4.3 NuGet Packages (New)

| Package      | Version | Purpose                            |
| :----------- | :------ | :--------------------------------- |
| `FuzzySharp` | 2.x     | Levenshtein distance + fuzzy ratio |

---

## 5. Risks & Mitigations

- **Risk:** FuzzySharp performance degrades with large term dictionaries.
    - _Mitigation:_ Pre-filter terms by first character; use parallel processing.
- **Risk:** False positives from fuzzy matching legitimate words.
    - _Mitigation:_ Default threshold of 80% is strict; user can adjust per-term.
- **Risk:** Double-counting violations (regex + fuzzy match same word).
    - _Mitigation:_ Pass regex results to fuzzy scanner; skip already-flagged words.
- **Risk:** Migration fails on production database.
    - _Mitigation:_ Test migration on copy of production data; columns default to sensible values.
- **Risk:** License bypass through direct service instantiation.
    - _Mitigation:_ Gate check inside service, not just at DI level.

---

## 6. Decision Trees

### 6.1 Should Fuzzy Scan Run?

```text
START: "Should fuzzy scanning run on this document?"
│
├── Is user license >= WriterPro?
│   ├── NO → Return empty results (feature gated)
│   └── YES → Continue
│
├── Is global "Enable Fuzzy Matching" toggle ON?
│   ├── NO → Return empty results
│   └── YES → Continue
│
├── Are there any fuzzy-enabled terms in the lexicon?
│   ├── NO → Return empty results (nothing to match)
│   └── YES → Proceed with scan
│
└── Run fuzzy scanner...
```

### 6.2 Per-Word Matching Decision

```text
START: "Should this word be fuzzy-matched?"
│
├── Is word already flagged by regex scanner?
│   └── YES → SKIP (avoid double-count)
│
├── For each fuzzy-enabled term:
│   └── Calculate Fuzz.Ratio(word, term.Pattern)
│       ├── Ratio >= term.FuzzyThreshold?
│       │   ├── YES → Create StyleViolation
│       │   └── NO → Continue to next term
│
└── No matches → Word is clean
```

---

## 7. User Stories

| ID    | Role            | Story                                                                             | Acceptance Criteria                                    |
| :---- | :-------------- | :-------------------------------------------------------------------------------- | :----------------------------------------------------- |
| US-01 | Writer Pro User | As a paid user, I want typos of forbidden terms detected even if I misspell them. | "white-list" flagged when "whitelist" is a fuzzy term. |
| US-02 | Writer Pro User | As a paid user, I want to configure fuzzy match sensitivity per term.             | Threshold slider in Term Editor affects matching.      |
| US-03 | Core User       | As a free user, I understand fuzzy matching is a premium feature.                 | Feature is visibly disabled with upgrade prompt.       |
| US-04 | All Users       | As a user, I don't want the same word flagged twice by both regex and fuzzy.      | No duplicate violations in Problems Panel.             |
| US-05 | Developer       | As a developer, I want fuzzy scanning to not slow down typing.                    | Scan completes in < 100ms for typical documents.       |

---

## 8. Use Cases

### UC-01: Fuzzy Violation Detection

**Preconditions:**

- User has Writer Pro license.
- "whitelist" is configured as a fuzzy term with threshold 0.80.
- User's document contains "white-list".

**Flow:**

1. User opens/edits document containing "white-list".
2. Linting orchestrator triggers analysis.
3. Regex scanner runs first—"white-list" is NOT in regex patterns.
4. Fuzzy scanner runs:
   a. Tokenizes document, finds word "white-list".
   b. Loads fuzzy terms, finds "whitelist".
   c. Calculates: `Fuzz.Ratio("white-list", "whitelist")` = 90%.
   d. 90% ≥ 80% threshold → Creates violation.
5. `LintingCompletedEvent` includes fuzzy violation.
6. Problems panel shows: "Consider using 'allowlist' instead of 'white-list'".

**Postconditions:**

- Violation is visible in Problems panel.
- Squiggly underline appears under "white-list" in editor.

---

### UC-02: License Gate Enforcement

**Preconditions:**

- User has Core (free) license.
- Fuzzy terms exist in the lexicon.

**Flow:**

1. User opens document containing potential fuzzy matches.
2. Linting orchestrator triggers analysis.
3. Regex scanner runs, finds some violations.
4. Fuzzy scanner checks license: `HasFeature(FuzzyMatching)` → FALSE.
5. Fuzzy scanner returns empty array (no processing).
6. Only regex violations appear in results.

**Postconditions:**

- No fuzzy violations shown.
- User is not charged for a feature they don't have.

---

## 9. Unit Testing Requirements

### 9.1 FuzzyMatchService Tests

```csharp
[Trait("Category", "Unit")]
public class FuzzyMatchServiceTests
{
    [Theory]
    [InlineData("whitelist", "white-list", 90)]  // High match
    [InlineData("whitelist", "whitelist", 100)]  // Exact match
    [InlineData("whitelist", "blacklist", 60)]   // Different word
    [InlineData("whitelist", "waitlist", 75)]    // Similar but different
    public void CalculateDistance_ReturnsExpectedRatio(
        string source, string target, int expectedMinRatio)
    {
        var sut = new FuzzyMatchService();
        var result = sut.CalculateRatio(source, target);
        result.Should().BeGreaterThanOrEqualTo(expectedMinRatio);
    }

    [Fact]
    public void IsMatch_WhenAboveThreshold_ReturnsTrue()
    {
        var sut = new FuzzyMatchService();
        sut.IsMatch("whitelist", "white-list", 0.80).Should().BeTrue();
    }

    [Fact]
    public void IsMatch_WhenBelowThreshold_ReturnsFalse()
    {
        var sut = new FuzzyMatchService();
        sut.IsMatch("whitelist", "completely different", 0.80).Should().BeFalse();
    }
}
```

### 9.2 FuzzyScanner Tests

```csharp
[Trait("Category", "Unit")]
public class FuzzyScannerTests
{
    [Fact]
    public async Task Scan_DetectsFuzzyMatch()
    {
        // Arrange
        var terms = new[] { CreateFuzzyTerm("whitelist", 0.80) };
        var sut = CreateScanner(terms, hasLicense: true);

        // Act
        var result = await sut.ScanAsync("The white-list is deprecated.");

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchedText.Should().Be("white-list");
    }

    [Fact]
    public async Task Scan_SkipsWordAlreadyFlaggedByRegex()
    {
        // Arrange
        var terms = new[] { CreateFuzzyTerm("whitelist", 0.80) };
        var regexResults = new[] { CreateViolation("whitelist") };
        var sut = CreateScanner(terms, regexResults, hasLicense: true);

        // Act (document contains exact "whitelist")
        var result = await sut.ScanAsync("The whitelist is deprecated.");

        // Assert
        result.Should().BeEmpty();  // Regex already caught it
    }

    [Fact]
    public async Task Scan_WithCoreLicense_ReturnsEmpty()
    {
        var sut = CreateScanner(terms: new[] { CreateFuzzyTerm("test", 0.80) }, hasLicense: false);
        var result = await sut.ScanAsync("Any test content here.");
        result.Should().BeEmpty();
    }
}
```

### 9.3 Performance Test

```csharp
[Trait("Category", "Performance")]
public class FuzzyScannerPerformanceTests
{
    [Fact]
    public async Task Scan_1000Words_CompletesIn100ms()
    {
        // Arrange
        var document = GenerateDocument(wordCount: 1000);
        var terms = GenerateFuzzyTerms(count: 50);
        var sut = CreateScanner(terms, hasLicense: true);

        // Act
        var sw = Stopwatch.StartNew();
        await sut.ScanAsync(document);
        sw.Stop();

        // Assert
        sw.ElapsedMilliseconds.Should().BeLessThan(100);
    }
}
```

---

## 10. Observability & Logging

| Level   | Source       | Message Template                                                     |
| :------ | :----------- | :------------------------------------------------------------------- |
| Debug   | FuzzyScanner | `Fuzzy scan starting: {UniqueWordCount} unique words`                |
| Debug   | FuzzyScanner | `Loaded {TermCount} fuzzy-enabled terms`                             |
| Trace   | FuzzyScanner | `Checking word '{Word}' against term '{Term}': ratio={Ratio}`        |
| Info    | FuzzyScanner | `Fuzzy scan completed: {ViolationCount} violations in {ElapsedMs}ms` |
| Debug   | FuzzyScanner | `Skipping fuzzy scan: license feature not available`                 |
| Warning | FuzzyScanner | `Fuzzy scan exceeded performance threshold: {ElapsedMs}ms`           |

---

## 11. Acceptance Criteria (QA)

| #   | Category            | Criterion                                                                    |
| :-- | :------------------ | :--------------------------------------------------------------------------- |
| 1   | **[Algorithm]**     | FuzzySharp package installed and service functional.                         |
| 2   | **[Matching]**      | "white-list" detected when "whitelist" is fuzzy term (threshold 80%).        |
| 3   | **[Threshold]**     | Words below threshold are NOT flagged (e.g., "waitlist" not matched at 80%). |
| 4   | **[No Duplicates]** | Same word is not flagged by both regex and fuzzy scanner.                    |
| 5   | **[Performance]**   | Scan of 1,000 unique words completes in < 100ms.                             |
| 6   | **[License Gate]**  | Core users never see fuzzy violations.                                       |
| 7   | **[License Gate]**  | Writer Pro users see fuzzy violations when enabled.                          |
| 8   | **[Database]**      | Migration adds columns without data loss.                                    |
| 9   | **[Settings]**      | Global fuzzy toggle visible only to licensed users.                          |

---

## 12. Verification Commands

```bash
# ═══════════════════════════════════════════════════════════════════════════
# v0.3.1 Verification
# ═══════════════════════════════════════════════════════════════════════════

# 1. Verify FuzzySharp package installed
dotnet list src/Lexichord.Modules.Style package | grep FuzzySharp

# 2. Run migrations
dotnet run --project src/Lexichord.Host -- --migrate

# 3. Verify new columns in database
# (use psql or database tool to check style_terms table)

# 4. Run unit tests
dotnet test --filter "Category=Unit&FullyQualifiedName~Fuzzy"

# 5. Run performance tests
dotnet test --filter "Category=Performance&FullyQualifiedName~FuzzyScanner"

# 6. Manual verification:
# a) Open app as Core user → Verify fuzzy toggle disabled
# b) Open app as Writer Pro → Verify fuzzy toggle enabled
# c) Add fuzzy term "whitelist" → Type "white-list" in document
# d) Verify violation appears in Problems panel
```
