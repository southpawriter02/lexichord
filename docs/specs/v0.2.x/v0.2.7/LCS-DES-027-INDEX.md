# LCS-DES-027-INDEX: Polish (Performance & Edge Cases)

## Document Control

| Field            | Value                                                                       |
| :--------------- | :-------------------------------------------------------------------------- |
| **Document ID**  | LCS-DES-027-INDEX                                                           |
| **Version**      | v0.2.7                                                                      |
| **Codename**     | Polish (Performance & Edge Cases)                                           |
| **Status**       | Draft                                                                       |
| **Last Updated** | 2026-01-27                                                                  |
| **Owner**        | Lead Architect                                                              |
| **Depends On**   | v0.2.3 (Linter Engine), v0.2.4 (Editor Integration), v0.1.3 (Editor Module) |

---

## Executive Summary

**Polish** (v0.2.7) is the **performance hardening milestone** that transforms the linting system from a functional prototype into a production-ready, bulletproof component. This version ensures the linter operates seamlessly across all edge cases while maintaining UI responsiveness even under extreme conditions.

### Business Value

- **Reliability:** Writers trust that Lexichord handles their largest, most complex documents.
- **Accuracy:** Reduced false positives from code samples improves linter credibility.
- **Performance:** 60fps typing experience maintained even during background analysis.
- **Technical Debt:** Clean async patterns prevent future threading bugs.
- **AI Foundation:** v0.3.x AI features will inherit this robust infrastructure.

### Success Criteria

1. All regex scanning executes off UI thread (0ms blocking).
2. 60fps maintained during 5MB file editing.
3. Zero false positives from code blocks and frontmatter.
4. Full scan of 5MB document < 2 seconds.

---

## Related Documents

| Document ID  | Title                  | Description                                       |
| :----------- | :--------------------- | :------------------------------------------------ |
| LCS-SBD-027  | Scope Breakdown        | Work breakdown and task planning                  |
| LCS-DES-027a | Async Offloading       | Task.Run execution with UI thread marshalling     |
| LCS-DES-027b | Code Block Ignoring    | Fenced and inline code exclusion from linting     |
| LCS-DES-027c | Frontmatter Ignoring   | YAML/TOML/JSON frontmatter exclusion from linting |
| LCS-DES-027d | Large File Stress Test | Performance validation with 5MB+ documents        |

---

## Architecture Overview

### High-Level Component Diagram

````
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              Polish (v0.2.7)                                    │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                         Threading Layer                                   │  │
│  │  ┌────────────────────┐  ┌──────────────────────────────────────────┐     │  │
│  │  │ IThreadMarshaller  │  │ AvaloniaThreadMarshaller                 │     │  │
│  │  │  (Abstraction)     │  │  (Dispatcher.UIThread.InvokeAsync)       │     │  │
│  │  └───────────┬────────┘  └───────────────────────────────────────────┘     │  │
│  │              │                                                            │  │
│  │  ┌───────────┴────────────────────────────────────────────────────────┐   │  │
│  │  │                        Task.Run Wrapper                            │   │  │
│  │  │   (All regex scanning operations on ThreadPool)                    │   │  │
│  │  └────────────────────────────────────────────────────────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                         Content Filter Pipeline                           │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐                         │  │
│  │  │YamlFrontmatterFilter│  │MarkdownCodeBlockFilt│                         │  │
│  │  │  Priority: 100      │→ │  Priority: 200      │→ Scanner                │  │
│  │  │  (---/+++/{} at 0)  │  │  (```/`/indented)   │                         │  │
│  │  └─────────────────────┘  └─────────────────────┘                         │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
│  ┌───────────────────────────────────────────────────────────────────────────┐  │
│  │                         Performance Infrastructure                        │  │
│  │  ┌─────────────────────┐  ┌─────────────────────┐  ┌──────────────────┐   │  │
│  │  │ IPerformanceMonitor │  │ TestCorpusGenerator │  │ BenchmarkHarness │   │  │
│  │  │  (Metrics Collect)  │  │  (5MB Test Files)   │  │  (Stress Tests)  │   │  │
│  │  └─────────────────────┘  └─────────────────────┘  └──────────────────┘   │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
````

### Thread Flow Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                            Async Offloading Flow                                │
└─────────────────────────────────────────────────────────────────────────────────┘

  ┌──────────────┐
  │  UI Thread   │  User types → TextEditor.ContentChanged
  └──────┬───────┘
         │
         ▼
  ┌──────────────┐
  │ Rx Throttle  │  Debounce 300ms (adaptive 200-1000ms)
  │  (Scheduler) │
  └──────┬───────┘
         │
         ▼  Task.Run
  ┌──────────────────────────────────────────────────────────────────┐
  │                        ThreadPool                                │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐   │
  │  │ ContentFilter   │→ │ RegexScanner    │→ │ ViolationAgg.   │   │
  │  │ (YamlFM, Code)  │  │ (Pattern Match) │  │ (Collect Hits)  │   │
  │  └─────────────────┘  └─────────────────┘  └─────────────────┘   │
  └──────────────────────────────────┬───────────────────────────────┘
                                     │
                                     ▼  Dispatcher.UIThread.InvokeAsync
  ┌──────────────────────────────────────────────────────────────────┐
  │  UI Thread                                                       │
  │  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐   │
  │  │ViolationProvider│→ │StyleViolation   │→ │ Render Squigg.  │   │
  │  │ UpdateViol()    │  │ Renderer        │  │ (AvaloniaEdit)  │   │
  │  └─────────────────┘  └─────────────────┘  └─────────────────┘   │
  └──────────────────────────────────────────────────────────────────┘
```

### Content Filter Pipeline

````
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         ContentFilterPipeline                                   │
└─────────────────────────────────────────────────────────────────────────────────┘

  Raw Document Content
         │
         ▼
  ┌─────────────────────────────────────────────┐
  │ 1. YamlFrontmatterFilter (Priority 100)     │
  │    - Detects --- (YAML) at offset 0         │
  │    - Detects +++ (TOML) at offset 0         │
  │    - Detects { } (JSON) at offset 0         │
  │    → Adds ExcludedRegion(type=Frontmatter)  │
  └─────────────────────────────────────────────┘
         │
         ▼
  ┌─────────────────────────────────────────────┐
  │ 2. MarkdownCodeBlockFilter (Priority 200)   │
  │    - Detects ``` fenced code blocks         │
  │    - Detects ` inline code spans            │
  │    - State machine tracks open/close        │
  │    → Adds ExcludedRegion(type=CodeBlock)    │
  └─────────────────────────────────────────────┘
         │
         ▼
  FilteredContent {
    ProcessedContent: original content
    ExcludedRegions: [
      { 0-45, Frontmatter },
      { 100-250, FencedCodeBlock },
      { 300-320, InlineCode }
    ]
  }
         │
         ▼
  ┌─────────────────────────────────────────────┐
  │ 3. RegexStyleScanner                        │
  │    - Runs patterns on full content          │
  │    - Filters matches by ExcludedRegions     │
  │    - Binary search for O(log n) lookup      │
  └─────────────────────────────────────────────┘
         │
         ▼
  Violations (code/frontmatter hits removed)
````

---

## Dependencies

### Upstream Dependencies

| Component                   | Source Version | Usage in v0.2.7                               |
| :-------------------------- | :------------- | :-------------------------------------------- |
| `LintingOrchestrator`       | v0.2.3a        | Enhanced with Task.Run and thread marshalling |
| `ILintingConfiguration`     | v0.2.3b        | Debounce and performance settings             |
| `RegexStyleScanner`         | v0.2.3c        | Pattern matching engine to run off-thread     |
| `ViolationAggregator`       | v0.2.3d        | Offset adjustment for excluded regions        |
| `IViolationProvider`        | v0.2.4a        | Updates violations on UI thread               |
| `StyleViolationRenderer`    | v0.2.4a        | Renders violations (requires UI thread)       |
| `ManuscriptViewModel`       | v0.1.3a        | Document content source                       |
| `TextEditor (AvalonEdit)`   | v0.1.3a        | Text content and viewport info                |
| `IConfiguration / IOptions` | v0.0.3d        | Performance tuning settings                   |
| `ILogger<T> / Serilog`      | v0.0.3b        | Performance and diagnostic logging            |
| `Dispatcher.UIThread`       | Avalonia 11.x  | UI thread marshalling                         |

### External Dependencies

| Component       | Version | Usage                                 |
| :-------------- | :------ | :------------------------------------ |
| System.Reactive | 6.x     | Rx throttling and observable pipeline |
| BenchmarkDotNet | 0.13.x  | Performance benchmarking (tests only) |

---

## License Gating Strategy

Polish is a **Core** feature available to all tiers:

| Feature                | Core (Free) | Writer | WriterPro |
| :--------------------- | :---------- | :----- | :-------- |
| Async Offloading       | ✅ Yes      | ✅ Yes | ✅ Yes    |
| Code Block Ignoring    | ✅ Yes      | ✅ Yes | ✅ Yes    |
| Frontmatter Ignoring   | ✅ Yes      | ✅ Yes | ✅ Yes    |
| Large File Support     | ✅ Yes      | ✅ Yes | ✅ Yes    |
| Performance Metrics UI | ❌ No       | ❌ No  | ✅ Yes    |

---

## Key Interfaces Summary

| Interface               | Purpose                                        | Module                        |
| :---------------------- | :--------------------------------------------- | :---------------------------- |
| `IThreadMarshaller`     | Abstract thread marshalling for testability    | Lexichord.Abstractions        |
| `IContentFilter`        | Pre-scan content filtering (code, frontmatter) | Lexichord.Abstractions        |
| `IPerformanceMonitor`   | Metrics collection for adaptive debounce       | Lexichord.Modules.Style       |
| `IPerformanceBenchmark` | Stress testing infrastructure (test-only)      | Lexichord.Modules.Style.Tests |

---

## Implementation Checklist Summary

| Sub-Part  | Focus Area                | Key Deliverables                                 | Est. Hours |
| :-------- | :------------------------ | :----------------------------------------------- | :--------- |
| v0.2.7a   | Async Offloading          | IThreadMarshaller, Task.Run, UI marshalling      | 14h        |
| v0.2.7b   | Code Block Ignoring       | MarkdownCodeBlockFilter, fenced + inline parsing | 16h        |
| v0.2.7c   | Frontmatter Ignoring      | YamlFrontmatterFilter, YAML/TOML/JSON detection  | 10h        |
| v0.2.7d   | Large File Stress Test    | TestCorpusGenerator, benchmarks, 5MB validation  | 21h        |
|           | **Integration & Testing** | Integration tests, performance regression suite  | 9h         |
| **Total** |                           |                                                  | **70h**    |

---

## Success Criteria Summary

| Metric                        | Target    | Measurement Method                   |
| :---------------------------- | :-------- | :----------------------------------- |
| UI thread blocking (scan)     | 0ms       | Profiler - no scan code on UI thread |
| Frame rate during 5MB edit    | 60fps     | Frame counter during rapid typing    |
| Scan latency (5MB viewport)   | < 100ms   | Stopwatch in ScanRangeAsync          |
| Scan latency (5MB full)       | < 2000ms  | Stopwatch in ScanAsync               |
| False positives (code block)  | 0         | Manual test with code samples        |
| False positives (frontmatter) | 0         | Manual test with Jekyll/Hugo files   |
| Memory usage (5MB file)       | < 50MB    | Memory profiler                      |
| Debounce responsiveness       | 300-500ms | User perception testing              |

---

## Test Coverage Summary

| Test Type         | Focus Area                                     | Coverage Target |
| :---------------- | :--------------------------------------------- | :-------------- |
| Unit Tests        | IThreadMarshaller, IContentFilter              | 90%             |
| Unit Tests        | MarkdownCodeBlockFilter, YamlFrontmatterFilter | 95%             |
| Integration Tests | End-to-end async pipeline                      | Key paths       |
| Stress Tests      | 5MB document editing, scrolling, typing        | Performance     |
| Manual Tests      | Real Jekyll/Hugo/Gatsby files                  | Full coverage   |

---

## What This Enables

After v0.2.7, Lexichord will support:

- **Production Ready:** Linting system handles all real-world edge cases
- **Large Documents:** Technical writers can edit 5MB+ spec documents
- **Developer Workflow:** Code samples in documentation won't trigger false positives
- **Static Site Generators:** Jekyll, Hugo, Gatsby frontmatter properly ignored
- **Smooth UX:** No UI freezes or jank during linting operations
- **AI Foundation:** v0.3.x AI features inherit bulletproof async infrastructure
- **Enterprise Scale:** Ready for corporate documentation workflows

---

## Risks & Mitigations

| Risk                                      | Impact   | Probability | Mitigation                                |
| :---------------------------------------- | :------- | :---------- | :---------------------------------------- |
| UI thread blocking causes app freeze      | Critical | Medium      | Strict async patterns; thread assertions  |
| Code block regex too greedy/slow          | High     | Medium      | Timeout protection; precompiled patterns  |
| Nested code blocks confuse parser         | Medium   | Low         | State machine parser; comprehensive tests |
| Large file scanning causes OOM            | High     | Low         | Streaming/chunked approach; memory limits |
| Adaptive debounce too aggressive          | Medium   | Medium      | Configurable bounds; user feedback        |
| Frame drops during scroll with violations | High     | Medium      | Virtualized rendering; batch updates      |

---

## Document History

| Version | Date       | Author         | Changes                         |
| :------ | :--------- | :------------- | :------------------------------ |
| 0.1     | 2026-01-27 | Lead Architect | Initial INDEX creation from SBD |
