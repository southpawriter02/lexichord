# Lexichord Governance Roadmap (v0.2.1 - v0.2.7)

This phase introduces the first true domain-specific functionality. We are moving from a "Markdown Editor" (v0.1.x) to a "Governed Writing Environment." This module (`Lexichord.Modules.Style`) is the heart of the "Concordance" philosophy: rules over improvisation.

**Total Sub-Parts:** 28 distinct implementation steps.

## v0.2.1: The Rulebook (Style Module Genesis)
**Goal:** Establish the `Style` module and the ability to define rules in code and configuration.
*   **v0.2.1a:** **Module Scaffolding.** Create `Lexichord.Modules.Style`. Add reference to `Lexichord.Abstractions`. Register the module in the Host.
*   **v0.2.1b:** **The Rule Object Model.** Define the domain objects in Abstractions: `StyleRule`, `RuleCategory` (Terminology, Formatting, Syntax), and `ViolationSeverity` (Error, Warning, Info, Hint).
*   **v0.2.1c:** **YAML Deserializer.** Install `YamlDotNet`. Implement `StyleSheetLoader`. Create a default `lexichord.yaml` embedded resource containing standard rules (e.g., "Use 'allowlist' instead of 'whitelist'").
*   **v0.2.1d:** **Configuration Watcher.** Implement a `FileSystemWatcher` specifically for the project's root `.lexichord/style.yaml`. If the user edits the YAML file, the rules should reload in memory automatically without restarting the app.

## v0.2.2: The Lexicon (Terminology Database)
**Goal:** Persistent storage for thousands of terms, more robust than a YAML file.
*   **v0.2.2a:** **Schema Migration.** Create `Migration_002_StyleSchema.cs`. Create table `style_terms` with columns: `id`, `term_pattern` (text), `match_case` (bool), `recommendation` (text), `category` (text), `severity` (int).
*   **v0.2.2b:** **Repository Layer.** Implement `ITerminologyRepository`. Use Dapper to create efficient `SELECT` queries.
    *   *Performance Note:* Implement `GetAllActiveTerms()` with memory caching (`IMemoryCache`), creating a cached `HashSet` for rapid lookups.
*   **v0.2.2c:** **Seeding Service.** Create a logic step that checks if the DB is empty on startup. If so, seed it with the "Microsoft Manual of Style" common basics (approx. 50 terms) so the user sees immediate value.
*   **v0.2.2d:** **CRUD Service.** Implement `ITerminologyService` allowing the addition, update, and soft-deletion of terms. Publish `LexiconChangedEvent` via MediatR when modifications occur.

## v0.2.3: The Critic (The Linter Engine)
**Goal:** The logic that actually analyzes text. *Note: This does not visualize anything yet.*
*   **v0.2.3a:** **Reactive Pipeline.** Install `System.Reactive` (Rx.NET). Create a `LintingOrchestrator` that subscribes to the Editor's `TextChanged` event.
*   **v0.2.3b:** **Debounce Logic.** Configure a `Throttle` (e.g., 300ms) on the event stream. We must not run regex scans on every keystroke, or the UI will lag.
*   **v0.2.3c:** **The Scanner (Regex).** Implement the core loop: Iterate through active `StyleRules` and `Terms`. Run `Regex.Matches()` against the current document content.
    *   *Optimization:* Only scan the *visible viewport* or the *modified paragraph* for large documents (>100 pages).
*   **v0.2.3d:** **Violation Aggregator.** Transform Regex matches into `StyleViolation` objects (Index, Length, Message, Severity). Publish `LintingCompletedEvent` with the results.

## v0.2.4: The Red Pen (Editor Integration)
**Goal:** Visualizing errors in the AvalonEdit control.
*   **v0.2.4a:** **Rendering Transformer.** Create `StyleViolationRenderer` inheriting from `DocumentColorizingTransformer` (AvalonEdit API).
*   **v0.2.4b:** **The Squiggly Line.** Implement the drawing context logic to draw a wavy line under the text at the `StyleViolation` coordinates.
    *   *Colors:* Red for `Error`, Orange for `Warning`, Blue for `Info`.
*   **v0.2.4c:** **Hover Tooltips.** Implement `MouseHover` logic in the Editor. Calculate the offset under the mouse; if it intersects with a Violation, show a Tooltip with the `Recommendation`.
*   **v0.2.4d:** **Right-Click "Fix" Menu.** Extend the Context Menu. If the violation has a `Recommendation`, add a menu item: *"Change to '{Suggestion}'"*. Wiring this command replaces the text in the document.

## v0.2.5: The Librarian (Management UI)
**Goal:** A GUI for users to manage the rules without writing SQL.
*   **v0.2.5a:** **Terminology Grid View.** Create a new Tab/Dock view called "Lexicon". Use `Avalonia.DataGrid` to list all terms. Support sorting by Severity and Category.
*   **v0.2.5b:** **Search & Filter.** Add a search bar to filter terms. Add checkboxes to toggle "Show Deprecated" or "Show Forbidden".
*   **v0.2.5c:** **Term Editor Dialog.** Create a modal dialog to Add/Edit a term.
    *   *Validation:* Ensure the Regex pattern is valid. If invalid, show a validation error preventing Save.
*   **v0.2.5d:** **Bulk Import/Export.** Implement logic to import terms from CSV/Excel and export the current database to a JSON file (for sharing logic between teams).

## v0.2.6: The Sidebar (Real-Time Feedback)
**Goal:** A dedicated panel showing the "Health" of the document.
*   **v0.2.6a:** **Problems Panel.** Create a sidebar view similar to VS Code's "Problems" tab. Bind it to the latest `LintingCompletedEvent`.
*   **v0.2.6b:** **Navigation Sync.** Double-clicking an error in the Problems Panel should scroll the Editor to that specific line and column.
*   **v0.2.6c:** **Scorecard Widget.** A simple UI component at the top of the panel showing "Total Errors: X".
    *   *Gamification:* Show a "Compliance Score" (100% - penalty points).
*   **v0.2.6d:** **Filter Scope.** Toggle between "Current File" and "Open Files" (Project-wide linting). *Note: Project-wide linting should be run as a background Task.*

## v0.2.7: The Polish (Performance & Edge Cases)
**Goal:** Ensure the linter doesn't crash the app on edge cases.
*   **v0.2.7a:** **Async Offloading.** Ensure the `Regex` scanning happens on a `Task.Run` thread, not the UI thread. Marshall the results back to the UI thread (`Dispatcher.UIThread`) only for rendering.
*   **v0.2.7b:** **Code Block Ignoring.** Update the scanner to detect Markdown Code Blocks (```` ``` ````). **Skip** linting inside code blocks. (We don't want to flag variable names `whitelist_enabled` in code samples as style violations).
*   **v0.2.7c:** **Frontmatter Ignoring.** Update scanner to skip YAML Frontmatter (metadata at the top of Markdown files).
*   **v0.2.7d:** **Large File Stress Test.** Open a 5MB Markdown file (e.g., the CommonMark spec). Verify the UI remains responsive (60fps) while typing. Tune the Debounce and Chunking settings if necessary.

---

### Implementation Guide: Sample Workflow for v0.2.4d (Context Menu Fix)

**LCS-01 (Design Composition)**
*   **Context:** User right-clicks a squiggly line.
*   **Requirement:** Menu must show "Fix" options only if a suggestion exists in the DB.
*   **Interaction:** Clicking "Fix" performs an atomic replace operation in AvalonEdit.

**LCS-02 (Rehearsal Strategy)**
*   **Test (Integration):** Load document "Hello whitelist". DB has rule "whitelist -> allowlist".
*   **Action:** Simulate `GetContextMenuItems(offset)`.
*   **Assert:** Menu contains item "Change to 'allowlist'".
*   **Action:** Invoke command.
*   **Assert:** Document text becomes "Hello allowlist".

**LCS-03 (Performance Log)**
1.  **Modify `EditorView.axaml.cs`:**
    ```csharp
    private void OnContextMenuOpening(object sender, CancelEventArgs e) {
        var offset = _editor.CaretOffset;
        var violation = _violationService.GetViolationAt(offset);
        if (violation != null && violation.Suggestion != null) {
            var item = new MenuItem { Header = $"Change to '{violation.Suggestion}'" };
            item.Click += (_, _) => _editor.Document.Replace(violation.Start, violation.Length, violation.Suggestion);
            _contextMenu.Items.Insert(0, item);
        }
    }
    ```
2.  **Telemetry:** Log `Style.FixApplied` event to track which rules are most commonly violated and fixed.