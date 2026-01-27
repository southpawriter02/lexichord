# Lexichord Publisher Roadmap (v0.8.1 - v0.8.8)

In v0.7.x, we built "The Specialists" — purpose-built agents for editing, simplification, and style enforcement. In v0.8.x, we introduce **The Publisher**: domain-specific features for software documentation. This phase establishes Git integration, release notes generation, diff visualization, static site scaffolding, and PDF export for technical writers.

**Architectural Note:** This version introduces `Lexichord.Modules.Publisher` as the technical documentation toolkit. The module integrates with Git repositories, documentation frameworks (MkDocs, Docusaurus), and export pipelines. **License Gating:** Basic Git viewing is WriterPro, Release Notes Agent and advanced export require Teams tier.

**Total Sub-Parts:** 32 distinct implementation steps.

---

## v0.8.1: The Repository Reader (Git Foundation)
**Goal:** Integrate Git repository access to read history, commits, tags, and branches without executing CLI commands.

*   **v0.8.1a:** **Git Abstractions.** Define repository access interfaces in Abstractions:
    ```csharp
    public interface IGitRepositoryService
    {
        Task<GitRepository?> OpenRepositoryAsync(string workspacePath, CancellationToken ct);
        bool IsGitRepository(string path);
    }

    public record GitRepository(
        string RootPath,
        string CurrentBranch,
        GitRemote? Origin,
        bool HasUncommittedChanges
    );

    public record GitRemote(string Name, string Url, GitRemoteType Type);
    public enum GitRemoteType { GitHub, GitLab, Bitbucket, Azure, Other }

    public interface IGitHistoryService
    {
        Task<IReadOnlyList<GitCommit>> GetCommitsAsync(GitCommitQuery query, CancellationToken ct);
        Task<IReadOnlyList<GitTag>> GetTagsAsync(CancellationToken ct);
        Task<IReadOnlyList<GitBranch>> GetBranchesAsync(CancellationToken ct);
    }
    ```
*   **v0.8.1b:** **LibGit2Sharp Integration.** Implement Git access using LibGit2Sharp:
    ```csharp
    public class LibGit2RepositoryService(ILogger<LibGit2RepositoryService> logger) : IGitRepositoryService
    {
        public Task<GitRepository?> OpenRepositoryAsync(string workspacePath, CancellationToken ct)
        {
            var repoPath = Repository.Discover(workspacePath);
            if (repoPath is null) return Task.FromResult<GitRepository?>(null);

            using var repo = new Repository(repoPath);
            return Task.FromResult<GitRepository?>(new GitRepository(
                RootPath: repo.Info.WorkingDirectory,
                CurrentBranch: repo.Head.FriendlyName,
                Origin: MapRemote(repo.Network.Remotes.FirstOrDefault(r => r.Name == "origin")),
                HasUncommittedChanges: repo.RetrieveStatus().IsDirty
            ));
        }
    }
    ```
    *   Lazy initialization to avoid blocking startup.
    *   Dispose repository handles properly to avoid file locks.
    *   Handle bare repositories and worktrees.
*   **v0.8.1c:** **Commit Query Model.** Define flexible commit querying:
    ```csharp
    public record GitCommitQuery(
        string? Branch = null,
        string? FromTag = null,
        string? ToTag = null,
        DateTime? Since = null,
        DateTime? Until = null,
        string? Author = null,
        string? PathFilter = null,
        int MaxCount = 100
    );

    public record GitCommit(
        string Sha,
        string ShortSha,
        string Message,
        string MessageShort,
        GitSignature Author,
        GitSignature Committer,
        DateTime AuthoredAt,
        IReadOnlyList<string> ParentShas,
        IReadOnlyList<GitFileChange>? Changes = null
    );

    public record GitSignature(string Name, string Email);
    public record GitFileChange(string Path, GitChangeKind Kind, int? Additions, int? Deletions);
    public enum GitChangeKind { Added, Modified, Deleted, Renamed, Copied }
    ```
*   **v0.8.1d:** **Git Status Panel.** Create repository status view:
    *   Display current branch with indicator (clean/dirty).
    *   Show recent commits (last 10) with short messages.
    *   List available tags for release selection.
    *   "Refresh" button to update status.
    *   Register in `ShellRegion.BottomPanel` via `IRegionManager` (v0.1.1b).

---

## v0.8.2: The Commit Browser (History Exploration)
**Goal:** Build a visual interface for exploring Git history and selecting commits for documentation.

*   **v0.8.2a:** **Commit List View.** Create `CommitListView.axaml`:
    *   Virtualized list of commits with avatar, author, date, message.
    *   Filter by branch, date range, author.
    *   Search commits by message content.
    *   Click to select commit for details.
    *   Multi-select for range operations.
    ```csharp
    public partial class CommitListViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<CommitViewModel> _commits = [];
        [ObservableProperty] private CommitViewModel? _selectedCommit;
        [ObservableProperty] private string _filterText = string.Empty;
        [ObservableProperty] private string? _selectedBranch;

        [RelayCommand]
        private async Task LoadCommitsAsync(GitCommitQuery query, CancellationToken ct);
    }
    ```
*   **v0.8.2b:** **Commit Detail View.** Show full commit information:
    *   Full commit message with formatting.
    *   Author and committer information.
    *   List of changed files with add/delete counts.
    *   Parent commit links for navigation.
    *   "View Diff" button for detailed changes.
*   **v0.8.2c:** **Tag Selection UI.** Enable tag-based range selection:
    *   Dropdown listing all tags (sorted by date descending).
    *   "From Tag" and "To Tag" selectors.
    *   Auto-calculate commit count between tags.
    *   Validation: Ensure "From" is ancestor of "To".
    *   Quick presets: "Last Release", "Last 2 Releases".
*   **v0.8.2d:** **Conventional Commit Parser.** Parse structured commit messages:
    ```csharp
    public interface IConventionalCommitParser
    {
        ConventionalCommit? Parse(string message);
    }

    public record ConventionalCommit(
        string Type,           // feat, fix, docs, style, refactor, test, chore
        string? Scope,         // (api), (ui), (core)
        string Description,
        string? Body,
        IReadOnlyList<string> Footers,
        bool IsBreakingChange
    );
    ```
    *   Support standard types: feat, fix, docs, style, refactor, perf, test, build, ci, chore.
    *   Extract scope from parentheses.
    *   Detect breaking changes from `!` or `BREAKING CHANGE:` footer.

---

## v0.8.3: The Release Notes Agent (Changelog Generation)
**Goal:** Build an AI-powered agent that generates structured release notes from Git history.

*   **v0.8.3a:** **Release Notes Model.** Define output structure:
    ```csharp
    public record ReleaseNotes(
        string Version,
        DateTime ReleaseDate,
        string Summary,
        IReadOnlyList<ReleaseSection> Sections,
        IReadOnlyList<Contributor> Contributors,
        string? MigrationNotes,
        IReadOnlyList<string>? BreakingChanges
    );

    public record ReleaseSection(
        string Title,           // "Features", "Bug Fixes", "Documentation"
        IReadOnlyList<ReleaseItem> Items
    );

    public record ReleaseItem(
        string Description,
        string? Scope,
        string? CommitSha,
        string? PullRequestUrl,
        string? IssueUrl
    );

    public record Contributor(string Name, string Email, int CommitCount);
    ```
*   **v0.8.3b:** **Release Notes Agent Implementation.** Create `ReleaseNotesAgent`:
    ```csharp
    [RequiresLicense(LicenseTier.Teams)]
    [AgentDefinition("release-notes", "Release Notes Agent", "Generates changelogs from Git history")]
    public class ReleaseNotesAgent(
        IChatCompletionService llm,
        IPromptRenderer renderer,
        IGitHistoryService gitHistory,
        IConventionalCommitParser commitParser,
        IPromptTemplateRepository templates,
        ILogger<ReleaseNotesAgent> logger) : BaseAgent(llm, renderer, templates, logger)
    {
        public async Task<ReleaseNotes> GenerateReleaseNotesAsync(
            string fromTag, string toTag, ReleaseNotesOptions options, CancellationToken ct)
        {
            // 1. Get commits between tags
            var commits = await gitHistory.GetCommitsAsync(
                new GitCommitQuery(FromTag: fromTag, ToTag: toTag), ct);

            // 2. Parse conventional commits
            var parsed = commits
                .Select(c => (Commit: c, Conventional: commitParser.Parse(c.Message)))
                .ToList();

            // 3. Group by type
            var grouped = GroupByType(parsed);

            // 4. Generate descriptions via LLM
            var context = new Dictionary<string, object>
            {
                ["version"] = toTag,
                ["commit_groups"] = FormatCommitGroups(grouped),
                ["contributor_count"] = commits.Select(c => c.Author.Email).Distinct().Count()
            };

            var response = await InvokeAsync(new AgentRequest(
                "Generate release notes from these commits", null, null), ct);

            return ParseReleaseNotes(response.Content, toTag, commits);
        }
    }
    ```
*   **v0.8.3c:** **Release Notes Templates.** Create output format templates:
    ```yaml
    template_id: "release-notes-generator"
    system_prompt: |
      You are a technical writer creating release notes for software developers.

      Format the release notes as follows:
      1. Start with a brief summary (2-3 sentences) of the release highlights
      2. Group changes by category: Features, Bug Fixes, Performance, Documentation, Other
      3. Each item should be a clear, user-facing description (not raw commit messages)
      4. Mention breaking changes prominently if any exist
      5. List contributors at the end

      {{#style_rules}}
      Follow these style guidelines:
      {{style_rules}}
      {{/style_rules}}
    user_prompt: |
      Generate release notes for version {{version}}.

      Commits grouped by type:
      {{commit_groups}}

      Total contributors: {{contributor_count}}
    ```
*   **v0.8.3d:** **Release Notes Wizard UI.** Create guided workflow:
    *   Step 1: Select tag range (from/to dropdowns).
    *   Step 2: Preview commits to include (with edit capability).
    *   Step 3: Configure output format (Markdown, Keep-a-Changelog, Custom).
    *   Step 4: Generate and review draft.
    *   Step 5: Export to CHANGELOG.md or clipboard.
    *   "Regenerate Section" for partial updates.

---

## v0.8.4: The Comparison View (Diff Visualization)
**Goal:** Build a side-by-side diff viewer showing current content vs. AI proposals with accept/reject controls.

*   **v0.8.4a:** **Diff Engine.** Implement text differencing:
    ```csharp
    public interface IDiffEngine
    {
        DiffResult ComputeDiff(string original, string modified, DiffOptions options);
    }

    public record DiffResult(
        IReadOnlyList<DiffHunk> Hunks,
        int Additions,
        int Deletions,
        float SimilarityRatio
    );

    public record DiffHunk(
        int OriginalStart,
        int OriginalLength,
        int ModifiedStart,
        int ModifiedLength,
        IReadOnlyList<DiffLine> Lines
    );

    public record DiffLine(DiffLineType Type, string Content, int? OriginalLineNumber, int? ModifiedLineNumber);
    public enum DiffLineType { Context, Addition, Deletion }

    public record DiffOptions(
        int ContextLines = 3,
        bool IgnoreWhitespace = false,
        bool WordLevel = false
    );
    ```
    *   Use Myers diff algorithm for line-level changes.
    *   Support word-level diff for fine-grained comparison.
    *   Optimize for large files (streaming diff computation).
*   **v0.8.4b:** **Side-by-Side View.** Create `ComparisonView.axaml`:
    *   Two synchronized editor panes (original left, modified right).
    *   Line numbers for both panes.
    *   Color coding: green for additions, red for deletions.
    *   Synchronized scrolling between panes.
    *   Minimap showing change locations.
    ```csharp
    public partial class ComparisonViewModel : ObservableObject
    {
        [ObservableProperty] private string _originalContent = string.Empty;
        [ObservableProperty] private string _modifiedContent = string.Empty;
        [ObservableProperty] private DiffResult? _diffResult;
        [ObservableProperty] private int _currentHunkIndex;

        [RelayCommand] private void NextHunk();
        [RelayCommand] private void PreviousHunk();
        [RelayCommand] private void AcceptHunk(DiffHunk hunk);
        [RelayCommand] private void RejectHunk(DiffHunk hunk);
        [RelayCommand] private void AcceptAll();
        [RelayCommand] private void RejectAll();
    }
    ```
*   **v0.8.4c:** **Inline Diff Mode.** Alternative unified diff display:
    *   Single pane with inline additions/deletions.
    *   Strikethrough for deleted text, highlight for added.
    *   Toggle between side-by-side and inline modes.
    *   Better for small changes or narrow screens.
*   **v0.8.4d:** **Accept/Reject Controls.** Per-hunk and bulk actions:
    *   Per-hunk buttons: "Accept" / "Reject" in gutter.
    *   Keyboard shortcuts: `A` (accept), `R` (reject), `N` (next), `P` (previous).
    *   Bulk actions: "Accept All", "Reject All" in toolbar.
    *   Partial accept: Select lines within hunk to accept.
    *   Undo stack for review actions.
    *   Publish `DiffHunkAcceptedEvent` / `DiffHunkRejectedEvent`.

---

## v0.8.5: The Static Site Bridge (Documentation Frameworks)
**Goal:** Generate configuration files for popular documentation frameworks (MkDocs, Docusaurus).

*   **v0.8.5a:** **Framework Detection.** Auto-detect documentation framework:
    ```csharp
    public interface IDocFrameworkDetector
    {
        DocFramework? DetectFramework(string workspacePath);
    }

    public record DocFramework(
        DocFrameworkType Type,
        string ConfigPath,
        string? Version,
        IReadOnlyDictionary<string, object>? CurrentConfig
    );

    public enum DocFrameworkType { MkDocs, Docusaurus, Sphinx, GitBook, VuePress, Hugo, None }
    ```
    *   Check for `mkdocs.yml`, `docusaurus.config.js`, `conf.py`, etc.
    *   Parse existing configuration if present.
    *   Suggest framework based on file structure if none detected.
*   **v0.8.5b:** **MkDocs Generator.** Create MkDocs configuration:
    ```csharp
    public interface IMkDocsGenerator
    {
        MkDocsConfig GenerateConfig(MkDocsGeneratorOptions options);
        string RenderYaml(MkDocsConfig config);
    }

    public record MkDocsConfig(
        string SiteName,
        string? SiteDescription,
        string? SiteUrl,
        string? RepoUrl,
        MkDocsTheme Theme,
        IReadOnlyList<MkDocsNavItem> Nav,
        IReadOnlyList<string>? Plugins,
        IReadOnlyDictionary<string, object>? Extra
    );

    public record MkDocsNavItem(string Title, string? Path, IReadOnlyList<MkDocsNavItem>? Children);
    public record MkDocsTheme(string Name, string? Palette, IReadOnlyList<string>? Features);
    ```
    *   Scan workspace for Markdown files.
    *   Generate nav structure from folder hierarchy.
    *   Support Material theme with sensible defaults.
    *   Include common plugins: search, minify.
*   **v0.8.5c:** **Docusaurus Generator.** Create Docusaurus configuration:
    ```csharp
    public interface IDocusaurusGenerator
    {
        DocusaurusConfig GenerateConfig(DocusaurusGeneratorOptions options);
        string RenderJs(DocusaurusConfig config);
        void GenerateSidebars(string outputPath, IReadOnlyList<DocTreeNode> tree);
    }
    ```
    *   Generate `docusaurus.config.js` with site metadata.
    *   Generate `sidebars.js` from file structure.
    *   Support versioning configuration.
    *   Include blog and docs plugin setup.
*   **v0.8.5d:** **Scaffold Wizard UI.** Guided framework setup:
    *   Step 1: Select framework (MkDocs, Docusaurus, etc.).
    *   Step 2: Configure site metadata (name, URL, repo).
    *   Step 3: Select theme and features.
    *   Step 4: Preview generated configuration.
    *   Step 5: Write files to workspace.
    *   "Update Existing" mode for incremental changes.

---

## v0.8.6: The PDF Exporter (Proofing Pipeline)
**Goal:** Export documents to PDF with style annotations for management review.

*   **v0.8.6a:** **PDF Generation Abstractions.** Define export interfaces:
    ```csharp
    public interface IPdfExporter
    {
        Task<byte[]> ExportAsync(PdfExportRequest request, CancellationToken ct);
        Task ExportToFileAsync(PdfExportRequest request, string outputPath, CancellationToken ct);
    }

    public record PdfExportRequest(
        string DocumentPath,
        PdfExportOptions Options,
        IReadOnlyList<PdfAnnotation>? Annotations = null
    );

    public record PdfExportOptions(
        PdfPageSize PageSize = PdfPageSize.A4,
        PdfOrientation Orientation = PdfOrientation.Portrait,
        PdfMargins? Margins = null,
        bool IncludeTableOfContents = true,
        bool IncludePageNumbers = true,
        string? HeaderText = null,
        string? FooterText = null,
        string? CssOverride = null
    );

    public record PdfAnnotation(
        TextSpan Location,
        string Text,
        PdfAnnotationType Type,
        string? Author
    );

    public enum PdfAnnotationType { Comment, Highlight, StyleViolation, Suggestion }
    ```
*   **v0.8.6b:** **Markdown to PDF Pipeline.** Implement export workflow:
    ```csharp
    public class MarkdownPdfExporter(
        IMarkdownParser markdownParser,
        IHtmlRenderer htmlRenderer,
        IPdfRenderer pdfRenderer,
        ILinterBridge linter,
        ILogger<MarkdownPdfExporter> logger) : IPdfExporter
    {
        public async Task<byte[]> ExportAsync(PdfExportRequest request, CancellationToken ct)
        {
            // 1. Parse Markdown to AST
            var ast = await markdownParser.ParseFileAsync(request.DocumentPath, ct);

            // 2. Render to HTML
            var html = htmlRenderer.Render(ast, new HtmlRenderOptions
            {
                IncludeCss = true,
                CssOverride = request.Options.CssOverride
            });

            // 3. Inject annotations as margin comments
            if (request.Annotations?.Count > 0)
            {
                html = InjectAnnotations(html, request.Annotations);
            }

            // 4. Convert HTML to PDF
            return await pdfRenderer.RenderAsync(html, request.Options, ct);
        }
    }
    ```
    *   Use `Markdig` for Markdown parsing (from v0.1.3b).
    *   Use `PuppeteerSharp` or `wkhtmltopdf` for PDF rendering.
    *   Support syntax highlighting in code blocks.
*   **v0.8.6c:** **Style Annotations.** Include linting results as PDF annotations:
    *   Fetch violations from `ILinterBridge` (v0.7.5a).
    *   Render as margin comments with severity icons.
    *   Color-coded: Red (Error), Yellow (Warning), Blue (Info).
    *   Optional: Include AI suggestions from agents.
    *   Toggle annotation visibility in export options.
*   **v0.8.6d:** **Export Preview & Settings UI.** Create export dialog:
    *   Live preview of first page.
    *   Page size and orientation selectors.
    *   Margin adjustments with visual preview.
    *   Toggle: TOC, page numbers, annotations.
    *   Header/footer text configuration.
    *   "Export" button with progress indicator.
    *   Recent export history for quick re-export.

---

## v0.8.7: The Documentation Linter (Tech Writer Quality)
**Goal:** Extend linting with documentation-specific rules for technical writers.

*   **v0.8.7a:** **Documentation Rules.** Define tech writing-specific lint rules:
    ```csharp
    public record DocumentationLintRule(
        string RuleId,
        string Name,
        string Description,
        DocLintCategory Category,
        LintSeverity DefaultSeverity,
        Func<DocumentContext, IEnumerable<LintViolation>> Check
    );

    public enum DocLintCategory
    {
        Structure,          // Heading hierarchy, section length
        Consistency,        // Terminology, formatting patterns
        Completeness,       // Missing sections, TODOs, placeholders
        Accessibility,      // Alt text, reading level
        TechnicalAccuracy   // Code block validation, link checking
    }
    ```
*   **v0.8.7b:** **Built-in Documentation Rules.** Implement core rules:
    *   **DOC001:** Heading hierarchy (no skipped levels: H1 → H3).
    *   **DOC002:** Section length (warn if section > 500 words without subheading).
    *   **DOC003:** Missing alt text on images.
    *   **DOC004:** Broken internal links.
    *   **DOC005:** TODO/FIXME markers in published docs.
    *   **DOC006:** Code block language specification required.
    *   **DOC007:** Consistent list formatting (bullets vs. numbers).
    *   **DOC008:** Frontmatter required fields (title, description).
    ```csharp
    public class HeadingHierarchyRule : IDocumentationLintRule
    {
        public string RuleId => "DOC001";

        public IEnumerable<LintViolation> Check(DocumentContext context)
        {
            var headings = context.Ast.Descendants<HeadingBlock>().ToList();
            for (int i = 1; i < headings.Count; i++)
            {
                var prev = headings[i - 1].Level;
                var curr = headings[i].Level;
                if (curr > prev + 1)
                {
                    yield return new LintViolation(
                        RuleId, $"Skipped heading level: H{prev} to H{curr}",
                        GetLocation(headings[i]), LintSeverity.Warning);
                }
            }
        }
    }
    ```
*   **v0.8.7c:** **Link Validator.** Check internal and external links:
    ```csharp
    public interface ILinkValidator
    {
        Task<LinkValidationResult> ValidateAsync(string documentPath, LinkValidationOptions options, CancellationToken ct);
    }

    public record LinkValidationResult(
        IReadOnlyList<ValidatedLink> Links,
        int ValidCount,
        int BrokenCount,
        int SkippedCount
    );

    public record ValidatedLink(
        string Url,
        LinkType Type,
        LinkStatus Status,
        string? ErrorMessage,
        int? HttpStatusCode
    );

    public enum LinkType { Internal, External, Anchor, Email }
    public enum LinkStatus { Valid, Broken, Timeout, Skipped }
    ```
    *   Validate internal links against file system.
    *   Validate external links with HEAD requests (configurable).
    *   Cache external link results (24-hour TTL).
    *   Report anchor links (#section) validity.
*   **v0.8.7d:** **Documentation Quality Dashboard.** Aggregate quality metrics:
    *   Per-document quality score (0-100).
    *   Violation breakdown by category.
    *   Trend chart: Quality over time (if Git history available).
    *   "Fix All" integration with Tuning Agent (v0.7.5).
    *   Export quality report as PDF or Markdown.

---

## v0.8.8: The Hardening (Quality & Performance)
**Goal:** Ensure the Publisher module is production-ready with comprehensive testing and optimization.

*   **v0.8.8a:** **Git Integration Tests.** Test repository operations:
    *   Create test repository with known history.
    *   Test commit queries: by date, author, path, tag range.
    *   Test conventional commit parsing edge cases.
    *   Test with large repositories (10K+ commits).
    *   Test with shallow clones and worktrees.
*   **v0.8.8b:** **Export Quality Tests.** Verify export fidelity:
    *   PDF rendering: Compare against golden PDFs.
    *   MkDocs config: Validate generated YAML is valid.
    *   Docusaurus config: Validate generated JS is syntactically correct.
    *   Link validation: Test against mock server with various responses.
    *   Test Unicode handling in all exports.
*   **v0.8.8c:** **Performance Optimization.** Establish baselines:
    *   Git history load: <2s for 1000 commits.
    *   Release notes generation: <10s (LLM time excluded).
    *   PDF export (10-page doc): <5s.
    *   Link validation: Parallel requests with rate limiting.
    *   Diff computation: <500ms for 10K-line files.
    *   Implement caching for repeated operations.
*   **v0.8.8d:** **Error Handling & Edge Cases.** Robust error management:
    *   Git errors: Repository not found, permission denied, corrupt index.
    *   PDF errors: Rendering failures, missing fonts, oversized images.
    *   Network errors: Link validation timeouts, DNS failures.
    *   User-friendly error messages with recovery suggestions.
    *   Graceful degradation for optional features.

---

## Dependencies on Prior Versions

| Component | Source Version | Usage in v0.8.x |
|:----------|:---------------|:----------------|
| `IAgent` / `BaseAgent` | v0.6.6a, v0.7.3b | Release Notes Agent base |
| `IChatCompletionService` | v0.6.1a | LLM for release notes |
| `IPromptRenderer` | v0.6.3b | Template rendering |
| `ILinterBridge` | v0.7.5a | Style annotations in PDF |
| `ILintingOrchestrator` | v0.2.3a | Documentation linting integration |
| `IMarkdownParser` | v0.1.3b | Markdown to AST conversion |
| `IRobustFileSystemWatcher` | v0.1.2b | Config file monitoring |
| `IRegionManager` | v0.1.1b | Panel registration |
| `ISettingsService` | v0.1.6a | Export preferences |
| `ILicenseContext` | v0.0.4c | Feature gating |
| `IMediator` | v0.0.7a | Event publishing |
| `Polly` | v0.0.5d | Retry for link validation |

---

## MediatR Events Introduced

| Event | Description |
|:------|:------------|
| `RepositoryOpenedEvent` | Git repository detected and opened |
| `RepositoryStatusChangedEvent` | Branch changed or commits added |
| `CommitsLoadedEvent` | Commit history query completed |
| `ReleaseNotesGeneratedEvent` | Release notes agent completed |
| `DiffComputedEvent` | Diff calculation completed |
| `DiffHunkAcceptedEvent` | User accepted a diff hunk |
| `DiffHunkRejectedEvent` | User rejected a diff hunk |
| `DocFrameworkScaffoldedEvent` | MkDocs/Docusaurus config generated |
| `PdfExportCompletedEvent` | PDF export finished |
| `LinkValidationCompletedEvent` | Link checking finished |
| `DocumentQualityCalculatedEvent` | Quality score computed |

---

## NuGet Packages Introduced

| Package | Version | Purpose |
|:--------|:--------|:--------|
| `LibGit2Sharp` | 0.30.x | Git repository access |
| `PuppeteerSharp` | 17.x | PDF rendering via Chromium |
| `DiffPlex` | 1.7.x | Text differencing algorithm |

---

## License Gating Summary

| Feature | Core | WriterPro | Teams | Enterprise |
|:--------|:-----|:----------|:------|:-----------|
| Git repository viewing | — | ✓ | ✓ | ✓ |
| Commit browser | — | ✓ | ✓ | ✓ |
| Conventional commit parsing | — | ✓ | ✓ | ✓ |
| Release Notes Agent | — | — | ✓ | ✓ |
| Diff comparison view | — | ✓ | ✓ | ✓ |
| MkDocs scaffolding | — | ✓ | ✓ | ✓ |
| Docusaurus scaffolding | — | — | ✓ | ✓ |
| PDF export (basic) | — | ✓ | ✓ | ✓ |
| PDF with annotations | — | — | ✓ | ✓ |
| Documentation linting | — | ✓ | ✓ | ✓ |
| Link validation | — | ✓ | ✓ | ✓ |
| Quality dashboard | — | — | ✓ | ✓ |

---

## Implementation Guide: Sample Workflow for v0.8.1b (LibGit2Sharp Integration)

**LCS-01 (Design Composition)**
*   **Interface:** `IGitRepositoryService` for repository discovery and metadata.
*   **Library:** LibGit2Sharp for native Git operations without CLI.
*   **Lifecycle:** Lazy load, dispose handles after operations.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Open valid repository. Assert metadata extracted correctly.
*   **Test:** Open non-repository folder. Assert null returned.
*   **Test:** Open repository with uncommitted changes. Assert `HasUncommittedChanges = true`.
*   **Test:** Verify no file locks remain after operation.

**LCS-03 (Performance Log)**
1.  **Implement `LibGit2RepositoryService`:**
    ```csharp
    public class LibGit2RepositoryService(ILogger<LibGit2RepositoryService> logger) : IGitRepositoryService
    {
        public Task<GitRepository?> OpenRepositoryAsync(string workspacePath, CancellationToken ct)
        {
            try
            {
                var repoPath = Repository.Discover(workspacePath);
                if (repoPath is null)
                {
                    logger.LogDebug("No Git repository found at {Path}", workspacePath);
                    return Task.FromResult<GitRepository?>(null);
                }

                using var repo = new Repository(repoPath);

                var origin = repo.Network.Remotes.FirstOrDefault(r => r.Name == "origin");
                var remote = origin is not null ? new GitRemote(
                    origin.Name,
                    origin.Url,
                    DetectRemoteType(origin.Url)
                ) : null;

                return Task.FromResult<GitRepository?>(new GitRepository(
                    RootPath: repo.Info.WorkingDirectory ?? repoPath,
                    CurrentBranch: repo.Head.FriendlyName,
                    Origin: remote,
                    HasUncommittedChanges: repo.RetrieveStatus().IsDirty
                ));
            }
            catch (RepositoryNotFoundException)
            {
                return Task.FromResult<GitRepository?>(null);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to open repository at {Path}", workspacePath);
                throw;
            }
        }

        public bool IsGitRepository(string path) => Repository.Discover(path) is not null;

        private static GitRemoteType DetectRemoteType(string url) => url switch
        {
            _ when url.Contains("github.com") => GitRemoteType.GitHub,
            _ when url.Contains("gitlab.com") => GitRemoteType.GitLab,
            _ when url.Contains("bitbucket.org") => GitRemoteType.Bitbucket,
            _ when url.Contains("dev.azure.com") || url.Contains("visualstudio.com") => GitRemoteType.Azure,
            _ => GitRemoteType.Other
        };
    }
    ```
2.  **Register:** Add to DI in `PublisherModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IGitRepositoryService, LibGit2RepositoryService>();
    services.AddSingleton<IGitHistoryService, LibGit2HistoryService>();
    ```

---

## Implementation Guide: Sample Workflow for v0.8.3b (Release Notes Agent)

**LCS-01 (Design Composition)**
*   **Class:** `ReleaseNotesAgent` extending `BaseAgent` with Git integration.
*   **Flow:** Query commits → Parse conventional commits → Group by type → Generate via LLM.
*   **Output:** Structured `ReleaseNotes` record with sections and contributors.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Generate notes for tag range with known commits. Assert all commits included.
*   **Test:** Generate notes with conventional commits. Assert proper categorization.
*   **Test:** Generate notes with breaking changes. Assert breaking changes highlighted.
*   **Test:** Generate notes for empty range. Assert graceful handling.

**LCS-03 (Performance Log)**
1.  **Implement `ReleaseNotesAgent`** (see v0.8.3b code block above).
2.  **Implement commit grouping:**
    ```csharp
    private Dictionary<string, List<(GitCommit, ConventionalCommit?)>> GroupByType(
        List<(GitCommit Commit, ConventionalCommit? Conventional)> commits)
    {
        var groups = new Dictionary<string, List<(GitCommit, ConventionalCommit?)>>
        {
            ["Features"] = [],
            ["Bug Fixes"] = [],
            ["Performance"] = [],
            ["Documentation"] = [],
            ["Other"] = []
        };

        foreach (var (commit, conventional) in commits)
        {
            var category = conventional?.Type switch
            {
                "feat" => "Features",
                "fix" => "Bug Fixes",
                "perf" => "Performance",
                "docs" => "Documentation",
                _ => "Other"
            };

            groups[category].Add((commit, conventional));
        }

        return groups.Where(g => g.Value.Count > 0).ToDictionary(g => g.Key, g => g.Value);
    }
    ```
3.  **Register:** Add to DI in `PublisherModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IAgent, ReleaseNotesAgent>();
    services.AddSingleton<IConventionalCommitParser, ConventionalCommitParser>();
    ```

---

## Implementation Guide: Sample Workflow for v0.8.6b (PDF Export)

**LCS-01 (Design Composition)**
*   **Interface:** `IPdfExporter` for document-to-PDF conversion.
*   **Pipeline:** Markdown → AST → HTML → PDF.
*   **Renderer:** PuppeteerSharp (headless Chromium) for accurate rendering.

**LCS-02 (Rehearsal Strategy)**
*   **Test:** Export simple Markdown. Assert PDF generated with correct page count.
*   **Test:** Export with code blocks. Assert syntax highlighting preserved.
*   **Test:** Export with annotations. Assert margin comments visible.
*   **Test:** Export with custom CSS. Assert styles applied.

**LCS-03 (Performance Log)**
1.  **Implement `MarkdownPdfExporter`:**
    ```csharp
    public class MarkdownPdfExporter(
        IMarkdownParser markdownParser,
        IHtmlTemplateService htmlTemplates,
        ILogger<MarkdownPdfExporter> logger) : IPdfExporter
    {
        private static readonly Lazy<Task<IBrowser>> _browser = new(InitBrowserAsync);

        public async Task<byte[]> ExportAsync(PdfExportRequest request, CancellationToken ct)
        {
            // 1. Read and parse Markdown
            var content = await File.ReadAllTextAsync(request.DocumentPath, ct);
            var html = markdownParser.ToHtml(content);

            // 2. Wrap in HTML template
            var fullHtml = htmlTemplates.Render("pdf-export", new
            {
                Content = html,
                Title = Path.GetFileNameWithoutExtension(request.DocumentPath),
                Css = request.Options.CssOverride ?? GetDefaultCss()
            });

            // 3. Inject annotations
            if (request.Annotations?.Count > 0)
            {
                fullHtml = InjectAnnotations(fullHtml, request.Annotations);
            }

            // 4. Render to PDF
            var browser = await _browser.Value;
            await using var page = await browser.NewPageAsync();
            await page.SetContentAsync(fullHtml);

            return await page.PdfDataAsync(new PdfOptions
            {
                Format = MapPageSize(request.Options.PageSize),
                Landscape = request.Options.Orientation == PdfOrientation.Landscape,
                PrintBackground = true,
                MarginOptions = MapMargins(request.Options.Margins),
                DisplayHeaderFooter = true,
                HeaderTemplate = request.Options.HeaderText ?? "",
                FooterTemplate = request.Options.IncludePageNumbers
                    ? "<div style='font-size:10px;text-align:center;width:100%'><span class='pageNumber'></span> / <span class='totalPages'></span></div>"
                    : ""
            });
        }

        private static async Task<IBrowser> InitBrowserAsync()
        {
            await new BrowserFetcher().DownloadAsync();
            return await Puppeteer.LaunchAsync(new LaunchOptions { Headless = true });
        }
    }
    ```
2.  **Register:** Add to DI in `PublisherModule.RegisterServices()`:
    ```csharp
    services.AddSingleton<IPdfExporter, MarkdownPdfExporter>();
    ```
