# LCS-SBD-027: Scope Breakdown — Polish (Performance & Edge Cases)

## Document Control

| Field            | Value                                                                      |
| :--------------- | :------------------------------------------------------------------------- |
| **Document ID**  | LCS-SBD-027                                                                |
| **Version**      | v0.2.7                                                                     |
| **Status**       | Draft                                                                      |
| **Last Updated** | 2026-01-26                                                                 |
| **Depends On**   | v0.2.3 (Linter Engine), v0.2.4 (Editor Integration), v0.1.3 (Editor Module) |

---

## 1. Executive Summary

### 1.1 The Vision

Polish (Performance & Edge Cases) is the **performance hardening milestone** that transforms the linting system from a functional prototype into a production-ready, bulletproof component. This version ensures the linter operates seamlessly across all edge cases while maintaining UI responsiveness even under extreme conditions.

Before AI features arrive in v0.3.x, the linting infrastructure must handle:
- **Large files:** 5MB+ Markdown documents without freezing the UI.
- **Code samples:** Variable names in code blocks must not trigger false positives.
- **Metadata:** YAML frontmatter common in static site generators must be ignored.
- **Async safety:** All regex scanning must occur off the UI thread.

This is defensive engineering — ensuring the linter never crashes or degrades the writing experience.

### 1.2 Business Value

- **Reliability:** Writers trust that Lexichord handles their largest, most complex documents.
- **Accuracy:** Reduced false positives from code samples improves linter credibility.
- **Performance:** 60fps typing experience maintained even during background analysis.
- **Technical Debt:** Clean async patterns prevent future threading bugs.
- **AI Foundation:** v0.3.x AI features will inherit this robust infrastructure.

### 1.3 Dependencies on Previous Versions

| Component | Source | Usage |
|:----------|:-------|:------|
| LintingOrchestrator | v0.2.3a | Orchestrates async linting pipeline |
| ILintingConfiguration | v0.2.3b | Debounce and performance settings |
| RegexStyleScanner | v0.2.3c | Pattern matching engine to run off-thread |
| IViolationProvider | v0.2.4a | Updates violations on UI thread |
| StyleViolationRenderer | v0.2.4a | Renders violations (requires UI thread) |
| ManuscriptViewModel | v0.1.3a | Document content source |
| TextEditor (AvalonEdit) | v0.1.3a | Text content and viewport info |
| IConfigurationService | v0.0.3d | Performance tuning settings |
| Serilog | v0.0.3b | Performance and diagnostic logging |

---

## 2. Sub-Part Specifications

### 2.1 v0.2.7a: Async Offloading

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-027a |
| **Title** | Async Offloading |
| **Module** | `Lexichord.Modules.Style` |
| **License Tier** | Core |

**Goal:** Ensure all regex scanning operations execute on `Task.Run` background threads, with results marshalled back to the UI thread via `Dispatcher.UIThread` only for rendering updates.

**Key Deliverables:**
- Audit `LintingOrchestrator` for UI thread blocking operations
- Wrap `RegexStyleScanner.ScanAsync` in `Task.Run` for true background execution
- Implement `Dispatcher.UIThread.InvokeAsync` for violation updates
- Add thread-safety assertions in debug builds
- Create performance benchmarks for async vs sync patterns

**Key Interfaces:**
```csharp
public interface IThreadMarshaller
{
    Task InvokeOnUIThreadAsync(Action action);
    Task<T> InvokeOnUIThreadAsync<T>(Func<T> func);
    bool IsOnUIThread { get; }
    void AssertUIThread(string operation);
    void AssertBackgroundThread(string operation);
}
```

**Dependencies:**
- v0.2.3a: LintingOrchestrator (pipeline coordinator)
- v0.2.3c: RegexStyleScanner (scanning operations)
- v0.2.4a: IViolationProvider (UI updates)

---

### 2.2 v0.2.7b: Code Block Ignoring

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-027b |
| **Title** | Code Block Ignoring |
| **Module** | `Lexichord.Modules.Style` |
| **License Tier** | Core |

**Goal:** Detect Markdown fenced code blocks (``` delimiters) and inline code (`backticks`) and exclude these regions from linting. Variable names like `whitelist_enabled` in code samples must not trigger style violations.

**Key Deliverables:**
- Create `IContentFilter` interface for pre-scan content processing
- Implement `MarkdownCodeBlockFilter` to detect and mask code regions
- Support fenced code blocks (```) with optional language identifiers
- Support inline code (`backticks`)
- Handle nested and escaped backticks correctly
- Track excluded regions for offset adjustment

**Key Interfaces:**
```csharp
public interface IContentFilter
{
    FilteredContent Filter(string content, FilterOptions options);
    bool CanFilter(string fileExtension);
    int Priority { get; }
}

public record FilteredContent(
    string ProcessedContent,
    IReadOnlyList<ExcludedRegion> ExcludedRegions,
    string OriginalContent
);

public record ExcludedRegion(
    int StartOffset,
    int EndOffset,
    ExclusionReason Reason
);
```

**Dependencies:**
- v0.2.3c: RegexStyleScanner (receives filtered content)
- v0.2.3d: ViolationAggregator (offset adjustment)

---

### 2.3 v0.2.7c: Frontmatter Ignoring

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-027c |
| **Title** | Frontmatter Ignoring |
| **Module** | `Lexichord.Modules.Style` |
| **License Tier** | Core |

**Goal:** Detect and skip YAML frontmatter blocks at the beginning of Markdown files (content between `---` delimiters). Frontmatter metadata fields should not trigger style violations.

**Key Deliverables:**
- Implement `YamlFrontmatterFilter` as `IContentFilter`
- Detect frontmatter only at document start (must begin at offset 0)
- Handle multiline YAML content between `---` delimiters
- Support TOML frontmatter (`+++` delimiters) for Hugo compatibility
- Support JSON frontmatter (`{` ... `}` at document start)
- Add configuration option to enable/disable frontmatter filtering

**Key Interfaces:**
```csharp
public record FrontmatterInfo(
    FrontmatterType Type,
    int StartOffset,
    int EndOffset,
    string RawContent
);

public enum FrontmatterType
{
    None,
    Yaml,
    Toml,
    Json
}
```

**Dependencies:**
- v0.2.7b: IContentFilter interface
- v0.2.3c: RegexStyleScanner (receives filtered content)

---

### 2.4 v0.2.7d: Large File Stress Test

| Field | Value |
|:------|:------|
| **Sub-Part ID** | INF-027d |
| **Title** | Large File Stress Test |
| **Module** | `Lexichord.Modules.Style` |
| **License Tier** | Core |

**Goal:** Validate that the linting system maintains 60fps UI responsiveness when editing a 5MB Markdown file (such as the CommonMark specification). Tune debounce timing, implement chunked scanning, and optimize memory usage as needed.

**Key Deliverables:**
- Create stress test harness with 5MB+ Markdown files
- Implement chunked scanning for large documents (>1MB)
- Add viewport-priority scanning (visible content first)
- Tune debounce interval (300ms baseline, adaptive scaling)
- Implement incremental/dirty-region scanning
- Memory profiling and optimization
- Frame rate monitoring during edit operations

**Key Interfaces:**
```csharp
public interface IPerformanceMonitor
{
    void StartOperation(string operationName);
    void EndOperation(string operationName);
    PerformanceMetrics GetMetrics();
    void ReportFrameDrop(int droppedFrames);
    bool IsPerformanceDegraded { get; }
}

public record PerformanceMetrics(
    double AverageScanDurationMs,
    double MaxScanDurationMs,
    int FrameDropCount,
    double MemoryUsageMb,
    int ScansPerMinute
);
```

**Dependencies:**
- v0.2.7a: Async offloading (non-blocking scans)
- v0.2.7b: Code block filtering (reduced scan content)
- v0.2.7c: Frontmatter filtering (reduced scan content)
- v0.2.3b: ILintingConfiguration (debounce tuning)

---

## 3. Implementation Checklist

| # | Sub-Part | Task | Est. Hours |
|:--|:---------|:-----|:-----------|
| 1 | v0.2.7a | Audit LintingOrchestrator for thread safety | 2 |
| 2 | v0.2.7a | Define IThreadMarshaller interface | 1 |
| 3 | v0.2.7a | Implement AvaloniaThreadMarshaller | 3 |
| 4 | v0.2.7a | Wrap ScanAsync in Task.Run | 2 |
| 5 | v0.2.7a | Marshall violation updates to UI thread | 2 |
| 6 | v0.2.7a | Add debug thread assertions | 1 |
| 7 | v0.2.7a | Unit tests for thread marshalling | 3 |
| 8 | v0.2.7b | Define IContentFilter interface | 1 |
| 9 | v0.2.7b | Define FilteredContent and ExcludedRegion records | 1 |
| 10 | v0.2.7b | Implement MarkdownCodeBlockFilter | 4 |
| 11 | v0.2.7b | Handle fenced code blocks (```) | 2 |
| 12 | v0.2.7b | Handle inline code (`backticks`) | 2 |
| 13 | v0.2.7b | Implement offset adjustment in ViolationAggregator | 2 |
| 14 | v0.2.7b | Unit tests for code block detection | 4 |
| 15 | v0.2.7c | Implement YamlFrontmatterFilter | 3 |
| 16 | v0.2.7c | Implement TOML frontmatter support | 1 |
| 17 | v0.2.7c | Implement JSON frontmatter support | 1 |
| 18 | v0.2.7c | Add frontmatter configuration option | 1 |
| 19 | v0.2.7c | Unit tests for frontmatter detection | 3 |
| 20 | v0.2.7d | Create stress test harness | 3 |
| 21 | v0.2.7d | Implement chunked scanning | 4 |
| 22 | v0.2.7d | Implement viewport-priority scanning | 3 |
| 23 | v0.2.7d | Implement adaptive debounce scaling | 2 |
| 24 | v0.2.7d | Implement incremental/dirty-region scanning | 4 |
| 25 | v0.2.7d | Create performance benchmarks | 3 |
| 26 | v0.2.7d | Memory optimization pass | 3 |
| 27 | v0.2.7d | Frame rate monitoring integration | 2 |
| 28 | All | Integration tests for filtered scanning | 4 |
| 29 | All | Performance regression tests | 3 |
| **Total** | | | **70 hours** |

---

## 4. Risks & Mitigations

| Risk | Impact | Probability | Mitigation |
|:-----|:-------|:------------|:-----------|
| UI thread blocking causes app freeze | Critical | Medium | Strict async patterns; thread assertions |
| Code block regex too greedy/slow | High | Medium | Timeout protection; precompiled patterns |
| Nested code blocks confuse parser | Medium | Low | State machine parser; comprehensive tests |
| Large file scanning causes OOM | High | Low | Streaming/chunked approach; memory limits |
| Adaptive debounce too aggressive | Medium | Medium | Configurable bounds; user feedback |
| Frame drops during scroll with violations | High | Medium | Virtualized rendering; batch updates |

---

## 5. Success Metrics

| Metric | Target | Measurement |
|:-------|:-------|:------------|
| UI thread blocking (scan operations) | 0ms | Profiler - no scan code on UI thread |
| Frame rate during 5MB file edit | 60fps | Frame counter during rapid typing |
| Scan latency (5MB, viewport only) | < 100ms | Stopwatch in ScanRangeAsync |
| Scan latency (5MB, full) | < 2000ms | Stopwatch in ScanAsync |
| False positives in code blocks | 0 | Manual test with code samples |
| False positives in frontmatter | 0 | Manual test with Jekyll/Hugo files |
| Memory usage (5MB file open) | < 50MB | Memory profiler |
| Debounce responsiveness | 300-500ms | User perception testing |

---

## 6. What This Enables

After v0.2.7, Lexichord will support:

- **Production Ready:** Linting system handles all real-world edge cases
- **Large Documents:** Technical writers can edit 5MB+ spec documents
- **Developer Workflow:** Code samples in documentation won't trigger false positives
- **Static Site Generators:** Jekyll, Hugo, Gatsby frontmatter properly ignored
- **Smooth UX:** No UI freezes or jank during linting operations
- **AI Foundation:** v0.3.x AI features inherit bulletproof async infrastructure
- **Enterprise Scale:** Ready for corporate documentation workflows
