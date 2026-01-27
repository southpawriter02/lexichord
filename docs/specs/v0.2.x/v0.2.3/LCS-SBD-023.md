# LCS-SBD-023: Scope Breakdown — The Critic (Linter Engine)

## Document Control

| Field            | Value                                                                             |
| :--------------- | :-------------------------------------------------------------------------------- |
| **Document ID**  | LCS-SBD-023                                                                       |
| **Version**      | v0.2.3                                                                            |
| **Status**       | Draft                                                                             |
| **Last Updated** | 2026-01-26                                                                        |
| **Depends On**   | v0.2.2 (Terminology Database), v0.2.1 (Rule Object Model), v0.1.3 (Editor Module) |

---

## 1. Executive Summary

### 1.1 The Vision

The Critic (Linter Engine) is the **analytical brain of Lexichord's governance system**. It transforms passive text editing into an active, rule-driven writing experience by continuously analyzing document content against defined style rules and terminology standards.

This module introduces reactive programming patterns via System.Reactive (Rx.NET) to create a performant, non-blocking linting pipeline that operates seamlessly in the background while the user writes.

### 1.2 Business Value

- **Real-Time Feedback:** Writers receive immediate guidance on style violations without manual checks.
- **Performance:** Reactive debouncing prevents UI lag even during rapid typing.
- **Scalability:** Architecture supports thousands of rules without degrading user experience.
- **Foundation:** Provides the violation data that v0.2.4 (Editor Integration) will visualize.
- **Enterprise Ready:** Supports corporate style guides with consistent enforcement.

### 1.3 Dependencies on Previous Versions

| Component              | Source  | Usage                                          |
| :--------------------- | :------ | :--------------------------------------------- |
| StyleRule              | v0.2.1b | Rule definitions to scan against               |
| ViolationSeverity      | v0.2.1b | Severity levels for violations                 |
| ITerminologyRepository | v0.2.2b | Database of terms to enforce                   |
| ITerminologyService    | v0.2.2d | Term CRUD operations                           |
| LexiconChangedEvent    | v0.2.2d | Invalidate rule cache on changes               |
| IManuscriptViewModel   | v0.1.3a | Document content source                        |
| DocumentChangedEvent   | v0.1.3a | Trigger for linting pipeline                   |
| IMediator / Event Bus  | v0.0.7a | Publish LintingCompletedEvent                  |
| IConfiguration         | v0.0.3d | Linter settings (via IOptions<LintingOptions>) |
| Serilog / ILogger<T>   | v0.0.3b | Performance and diagnostic logging             |

---

## 2. Sub-Part Specifications

### 2.1 v0.2.3a: Reactive Pipeline

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-023a                  |
| **Title**        | Reactive Pipeline         |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Install System.Reactive (Rx.NET) and create a `LintingOrchestrator` that subscribes to the Editor's `TextChanged` event stream, establishing the reactive foundation for the linting pipeline.

**Key Deliverables:**

- Install `System.Reactive` NuGet package
- Create `ILintingOrchestrator` interface in Abstractions
- Implement `LintingOrchestrator` with observable subscription
- Wire subscription to Editor's DocumentChangedEvent
- Implement IDisposable pattern for clean subscription management

**Key Interfaces:**

```csharp
public interface ILintingOrchestrator : IDisposable
{
    void StartLinting(IManuscriptViewModel document);
    void StopLinting(string documentId);
    IObservable<LintingResult> LintingResults { get; }
    bool IsLinting(string documentId);
}
```

**Dependencies:**

- v0.1.3a: IManuscriptViewModel (document content)
- v0.0.7: IMediator (event subscription)

---

### 2.2 v0.2.3b: Debounce Logic

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-023b                  |
| **Title**        | Debounce Logic            |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Configure a `Throttle` operator (300ms default) on the event stream to prevent regex scans on every keystroke, ensuring UI responsiveness during rapid typing.

**Key Deliverables:**

- Implement `ILintingConfiguration` for debounce settings
- Configure Rx `Throttle` operator on text change stream
- Support configurable debounce duration (100ms - 1000ms)
- Implement smart debounce that resets on continued typing
- Add cancellation support for interrupted lint operations

**Key Interfaces:**

```csharp
public interface ILintingConfiguration
{
    TimeSpan DebounceInterval { get; }
    TimeSpan MaxLintDuration { get; }
    bool LintOnlyVisibleRange { get; }
    int MaxViolationsPerDocument { get; }
}
```

**Dependencies:**

- v0.2.3a: LintingOrchestrator (applies throttle)
- v0.0.3d: IConfiguration + IOptions<LintingOptions> (persist settings)

---

### 2.3 v0.2.3c: The Scanner (Regex)

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-023c                  |
| **Title**        | The Scanner (Regex)       |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Implement the core scanning loop that iterates through active `StyleRules` and `Terms`, running `Regex.Matches()` against document content with optimization for large documents.

**Key Deliverables:**

- Create `IStyleScanner` interface
- Implement `RegexStyleScanner` with pattern matching
- Cache compiled Regex patterns for performance
- Implement viewport-only scanning for large documents (>100 pages)
- Implement modified-paragraph-only scanning mode
- Add timeout protection for runaway regex patterns (ReDoS prevention)

**Key Interfaces:**

```csharp
public interface IStyleScanner
{
    Task<IReadOnlyList<ScanMatch>> ScanAsync(
        string content,
        IEnumerable<StyleRule> rules,
        ScanOptions options,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScanMatch>> ScanRangeAsync(
        string content,
        int startOffset,
        int endOffset,
        IEnumerable<StyleRule> rules,
        CancellationToken cancellationToken = default);
}
```

**Dependencies:**

- v0.2.1b: StyleRule (rule definitions)
- v0.2.2b: ITerminologyRepository (term patterns)

---

### 2.4 v0.2.3d: Violation Aggregator

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-023d                  |
| **Title**        | Violation Aggregator      |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Transform Regex matches into `StyleViolation` objects containing Index, Length, Message, and Severity. Publish `LintingCompletedEvent` with aggregated results.

**Key Deliverables:**

- Create `StyleViolation` record in Abstractions
- Implement `IViolationAggregator` interface
- Transform ScanMatch results into StyleViolation objects
- Deduplicate overlapping violations (keep highest severity)
- Sort violations by position for ordered display
- Publish `LintingCompletedEvent` via MediatR

**Key Interfaces:**

```csharp
public interface IViolationAggregator
{
    IReadOnlyList<StyleViolation> Aggregate(
        IEnumerable<ScanMatch> matches,
        string documentId);

    void ClearViolations(string documentId);
    IReadOnlyList<StyleViolation> GetViolations(string documentId);
}
```

**Dependencies:**

- v0.2.3c: IStyleScanner (scan results)
- v0.2.1b: ViolationSeverity (severity levels)
- v0.0.7: IMediator (event publishing)

---

## 3. Implementation Checklist

| #         | Sub-Part | Task                                                  | Est. Hours   |
| :-------- | :------- | :---------------------------------------------------- | :----------- |
| 1         | v0.2.3a  | Install System.Reactive NuGet package                 | 0.5          |
| 2         | v0.2.3a  | Define ILintingOrchestrator interface in Abstractions | 1            |
| 3         | v0.2.3a  | Create LintingOrchestrator class                      | 4            |
| 4         | v0.2.3a  | Subscribe to DocumentChangedEvent                     | 2            |
| 5         | v0.2.3a  | Implement IDisposable with subscription cleanup       | 1            |
| 6         | v0.2.3a  | Unit tests for LintingOrchestrator                    | 3            |
| 7         | v0.2.3b  | Define ILintingConfiguration interface                | 1            |
| 8         | v0.2.3b  | Implement LintingConfiguration with defaults          | 2            |
| 9         | v0.2.3b  | Wire Throttle operator into observable pipeline       | 2            |
| 10        | v0.2.3b  | Implement configurable debounce duration              | 1            |
| 11        | v0.2.3b  | Implement cancellation token propagation              | 2            |
| 12        | v0.2.3b  | Unit tests for debounce behavior                      | 3            |
| 13        | v0.2.3c  | Define IStyleScanner interface                        | 1            |
| 14        | v0.2.3c  | Implement RegexStyleScanner base scanning             | 4            |
| 15        | v0.2.3c  | Implement compiled Regex caching                      | 2            |
| 16        | v0.2.3c  | Implement viewport-only scanning mode                 | 3            |
| 17        | v0.2.3c  | Implement modified-paragraph scanning mode            | 3            |
| 18        | v0.2.3c  | Add regex timeout protection (ReDoS)                  | 2            |
| 19        | v0.2.3c  | Unit tests for RegexStyleScanner                      | 4            |
| 20        | v0.2.3d  | Define StyleViolation record                          | 1            |
| 21        | v0.2.3d  | Define IViolationAggregator interface                 | 1            |
| 22        | v0.2.3d  | Implement ViolationAggregator                         | 3            |
| 23        | v0.2.3d  | Implement violation deduplication                     | 2            |
| 24        | v0.2.3d  | Implement position-based sorting                      | 1            |
| 25        | v0.2.3d  | Define LintingCompletedEvent                          | 1            |
| 26        | v0.2.3d  | Wire event publishing via MediatR                     | 2            |
| 27        | v0.2.3d  | Unit tests for ViolationAggregator                    | 3            |
| 28        | All      | Integration tests for full pipeline                   | 6            |
| **Total** |          |                                                       | **61 hours** |

---

## 4. Risks & Mitigations

| Risk                                     | Impact | Probability | Mitigation                                              |
| :--------------------------------------- | :----- | :---------- | :------------------------------------------------------ |
| Regex patterns cause ReDoS               | High   | Medium      | Timeout all regex execution; limit pattern complexity   |
| Memory pressure from many violations     | High   | Low         | Cap max violations per document; stream results         |
| Rx subscription leaks                    | Medium | Medium      | Strict IDisposable pattern; integration tests           |
| Debounce too aggressive (missed changes) | Medium | Low         | Configurable interval; smart debounce on silence        |
| Large document scanning blocks UI        | High   | Medium      | Async scanning on background thread; chunked processing |
| Rule cache invalidation race conditions  | Medium | Low         | Thread-safe cache; version-based invalidation           |

---

## 5. Success Metrics

| Metric                              | Target           | Measurement                                  |
| :---------------------------------- | :--------------- | :------------------------------------------- |
| Debounce response time              | 300ms +/- 10ms   | Stopwatch from last keystroke to scan start  |
| Scan latency (1K lines)             | < 50ms           | Stopwatch in ScanAsync                       |
| Scan latency (10K lines)            | < 200ms          | Stopwatch in ScanAsync                       |
| Scan latency (100K lines, viewport) | < 100ms          | Stopwatch with viewport optimization         |
| UI thread blocking                  | 0ms              | Profiler - all scanning on background thread |
| Memory per active document          | < 5MB violations | Memory profiler                              |
| Subscription cleanup                | 100%             | Verify no leaks after document close         |

---

## 6. What This Enables

After v0.2.3, Lexichord will support:

- **Real-Time Analysis:** Background style checking without UI impact
- **Reactive Architecture:** Foundation for all future real-time features
- **Violation Data:** Complete violation information ready for visualization (v0.2.4)
- **Scalable Rules:** Support for thousands of style rules efficiently
- **Enterprise Patterns:** Consistent style enforcement across documents
- **Performance Patterns:** Reusable debounce and async patterns for other modules
