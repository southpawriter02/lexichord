# Lexichord Resonance Roadmap (v0.3.1 - v0.3.8)

In v0.2.x, we built a "Rule Enforcer" (Regex Linter). In v0.3.x, we upgrade this to a "Writing Coach." This phase introduces algorithmic analysis (Fuzzy Matching, Readability Scoring, Voice Profiling) and advanced data management.

**Architectural Note:** This version introduces the first **"Soft Gates"** for licensing. Basic Regex linting is free (Core), but Fuzzy Matching and Tone Analysis are paid (Writer Pro).

**Total Sub-Parts:** 32 distinct implementation steps.

## v0.3.1: The Fuzzy Engine (Advanced Matching)
**Goal:** Detect typos and variations of forbidden terms (e.g., catching "white-list" or "whitelist" when "allowlist" is preferred, even if the regex doesn't explicitly list every variation).
*   **v0.3.1a:** **Algorithm Integration.** Import `Fastenshtein` or `FuzzySharp` NuGet packages into `Lexichord.Modules.Style`. Implement a `LevenshteinDistanceService` helper.
*   **v0.3.1b:** **Repository Update.** Modify `style_terms` schema to add `fuzzy_threshold` (decimal, default 0.8). Update `ITerminologyRepository` to allow fetching terms where `fuzzy_enabled = true`.
*   **v0.3.1c:** **The Fuzzy Scanner.** Implement a secondary scan loop. **Performance Gate:** Only run fuzzy matching on words *not* already flagged by the Regex linter to avoid double-counting.
    *   *Optimization:* Tokenize the document into a `HashSet<string>` of unique words first, then compare those unique words against the dictionary. This prevents re-calculating distance for the word "the" 500 times.
*   **v0.3.1d:** **License Gating.** Wrap the `FuzzyScanner` call in a License Check.
    *   *Logic:* `if (License.Tier < WriterPro) return;`

## v0.3.2: The Dictionary Manager (CRUD UI)
**Goal:** Give the user a proper interface to manage their Lexicon.
*   **v0.3.2a:** **DataGrid Infrastructure.** Create the `LexiconView` using `Avalonia.Controls.DataGrid`. Bind columns: *Term, Category, Severity, Tags*. Implement column sorting and filtering.
*   **v0.3.2b:** **Term Editor Dialog.** Create a specialized UserControl `TermEditorView`. Include:
    *   Pattern Input (Text).
    *   "Is Regex?" Checkbox.
    *   "Recommendation" Input (for the tooltip/fix).
    *   "Fuzzy Match" Toggle.
*   **v0.3.2c:** **Validation Logic.** Implement `FluentValidation` rules for the dialog.
    *   *Rule:* Pattern cannot be empty.
    *   *Rule:* If "Is Regex" is true, `Regex.IsMatch` must not throw an exception.
*   **v0.3.2d:** **Bulk Import/Export.** Implement a CSV Parser (`CsvHelper`). Create a wizard to map CSV columns to DB fields. Allow export to JSON for backup.

## v0.3.3: The Readability Engine (Metrics)
**Goal:** Algorithmic calculation of text complexity.
*   **v0.3.3a:** **Sentence Tokenizer.** Implement a robust text splitter that respects abbreviations (e.g., "Mr. Smith" does not end a sentence). Use standard `RuleBasedSentenceSplitter` logic.
*   **v0.3.3b:** **Syllable Counter.** Implement a heuristic syllable counter (counting vowel groups, handling silent 'e').
    *   *Test Case:* "Queue" (1 syllable), "Documentation" (5 syllables).
*   **v0.3.3c:** **The Calculator.** Implement `IReadabilityService`. Calculate:
    *   **Flesch-Kincaid Grade Level.**
    *   **Gunning Fog Index.**
    *   **Average Sentence Length.**
*   **v0.3.3d:** **The HUD Widget.** Create a small floating status bar widget (bottom right of Editor) showing: *"Grade 8.4 | 15 words/sent"*.

## v0.3.4: The Voice Profiler (Tone Analysis)
**Goal:** Moving beyond "Errors" to "Style." Detecting Passive Voice, Adverbs, and Weasel Words.
*   **v0.3.4a:** **Profile Definition.** Create `VoiceProfile` record: `{ TargetGradeLevel, MaxSentenceLength, AllowPassiveVoice, ForbiddenCategories }`. Seed defaults: "Technical" (Strict), "Marketing" (Loose).
*   **v0.3.4b:** **Passive Voice Detector.** Implement a dedicated analyzer using PoS (Part of Speech) tagging patterns or a robust Regex library for common passive constructions (e.g., "was *ed" patterns).
*   **v0.3.4c:** **Adverb/Weasel Scanner.** Create a static list of "weak" words (e.g., "very", "basically", "sort of"). Flag these as `Severity.Info` (Blue squiggly).
*   **v0.3.4d:** **Profile Selector UI.** Add a dropdown in the Status Bar to switch the active Profile (e.g., swapping from "Blog Post" to "API Docs" changes the rule set instantly).

## v0.3.5: The Resonance Dashboard (Visualization)
**Goal:** Visualizing the "Dissonance" between the text and the target profile.
*   **v0.3.5a:** **Charting Integration.** Install `LiveCharts2` (Avalonia). Create a new Tool Panel: "Resonance".
*   **v0.3.5b:** **The Spider Chart.** Implement a Radar Chart showing 5 axes: *Directness, Simplicity, Confidence, Formality, Emotion*.
    *   *Logic:* Map the metric results (passive voice count, grade level) to these abstract axes (0.0 to 1.0).
*   **v0.3.5c:** **Real-Time Updates.** Bind the chart data to the `LintingCompletedEvent`. Animate the chart changes smoothly as the user types.
*   **v0.3.5d:** **Target Overlays.** Draw a "Ghost" polygon on the radar chart representing the *Target Profile*.
    *   *Visual:* The user tries to make their "Current Data" polygon match the "Target" polygon shape.

## v0.3.6: The Global Dictionary (Project Settings)
**Goal:** Handling overrides (Project vs. Global rules).
*   **v0.3.6a:** **The Layered Configuration.** Implement a `ConfigurationProvider` that merges dictionaries.
    *   Priority: `Project/.lexichord/style.yaml` > `User Profile DB` > `System Defaults`.
*   **v0.3.6b:** **Conflict Resolution.** Logic to handle when a Project Rule says "Allow" but Global says "Forbid". (Project always wins).
*   **v0.3.6c:** **Override UI.** In the "Problems Panel," add a context menu: *"Ignore this rule for this project"*.
*   **v0.3.6d:** **Ignored Files.** Implement `.lexichordignore`. The Linter must read this file (glob patterns) and skip matching files (e.g., `dist/*.js`, `**/*.min.css`).

## v0.3.7: The Performance Tuning (Async Pipelines)
**Goal:** Ensure 60FPS typing speed despite heavy analysis.
*   **v0.3.7a:** **Background Buffering.** Implement a `Buffer` on the analysis pipeline. If the user types fast, discard intermediate analysis requests. Only process the *latest* snapshot after 500ms of idle time.
*   **v0.3.7b:** **Parallelization.** Run the `RegexScanner` and `FuzzyScanner` and `ReadabilityCalculator` in `Task.WhenAll()`.
*   **v0.3.7c:** **Virtualization.** Ensure the "Problems Panel" uses `VirtualizingStackPanel`. If there are 5,000 errors, the UI must not freeze rendering the list.
*   **v0.3.7d:** **Memory Leak Check.** Profile the app with `dotMemory`. Ensure that closing a tab releases the `LintingOrchestrator` subscription and doesn't leave the analyzer running on a closed document.

## v0.3.8: The Hardening (Unit Testing)
**Goal:** Verify the algorithms are accurate.
*   **v0.3.8a:** **Fuzzy Test Suite.** Assert that "whitelist" matches "white-list" (Distance 1) but not "wait-list" (Distance 2, if threshold is tight).
*   **v0.3.8b:** **Readability Test Suite.** Create a "Standard Corpus" (e.g., the Gettysburg Address). Assert that the Flesch-Kincaid score matches known values +/- 0.1.
*   **v0.3.8c:** **Passive Voice Test Suite.** Feed the analyzer specific sentences: "The code was written by the user." -> Detect. "The user wrote the code." -> Clean.
*   **v0.3.8d:** **Benchmark Baseline.** Record execution time for scanning 10,000 words. Set a CI failure threshold if this exceeds 200ms in future builds.

---

### Implementation Guide: Sample Workflow for v0.3.3c (The Calculator)

**LCS-01 (Design Composition)**
*   **Interface:**
    ```csharp
    public record ReadabilityMetrics(double FleschKincaid, double GunningFog, int WordCount, int SentenceCount);

    public interface IReadabilityService {
        ReadabilityMetrics Analyze(string text);
    }
    ```
*   **Logic:** Use `Regex` to strip punctuation before word counting. Use specific heuristic for syllable counting (regex for vowel clusters).

**LCS-02 (Rehearsal Strategy)**
*   **Test:** "The cat sat on the mat."
    *   Words: 6. Sentences: 1. Syllables: 6.
    *   Assert Flesch-Kincaid is approx 0.0 (Kindergarten).
*   **Test:** "Constructing sophisticated architectural paradigms."
    *   Assert high syllable count.

**LCS-03 (Performance Log)**
1.  **Implement `SyllableCounter`:**
    ```csharp
    // Heuristic: Count vowel groups not at the end of word (unless 'le')
    public int CountSyllables(string word) { ... }
    ```
2.  **Implement `ReadabilityService`:**
    *   Combine `WordTokenizer` and `SyllableCounter`.
    *   Apply standard formula: `0.39 * (words/sentences) + 11.8 * (syllables/words) - 15.59`.
3.  **Register:** Add to DI Container in `StyleModule.cs`.