# LCS-SBD-021: Scope Breakdown — The Rulebook (Style Module Genesis)

## Document Control

| Field            | Value                                                        |
| :--------------- | :----------------------------------------------------------- |
| **Document ID**  | LCS-SBD-021                                                  |
| **Version**      | v0.2.1                                                       |
| **Status**       | Draft                                                        |
| **Last Updated** | 2026-01-26                                                   |
| **Depends On**   | v0.1.3 (Editor Module), v0.0.7 (Event Bus), v0.0.4 (IModule) |

---

## 1. Executive Summary

### 1.1 The Vision

The Rulebook (Style Module Genesis) represents the **philosophical heart of Lexichord** — the embodiment of the "Concordance" philosophy: **rules over improvisation**. This module transforms Lexichord from a mere "Markdown Editor" into a **Governed Writing Environment** where style consistency is enforced, not suggested.

Just as a conductor ensures every instrument plays in harmony, the Style Module ensures every word adheres to the writer's chosen style guide. This is not about restricting creativity — it is about freeing writers from the cognitive burden of style enforcement so they can focus on what matters: the content.

### 1.2 The Concordance Philosophy

> "A concordance is an alphabetical list of the principal words used in a book or body of work, listing every instance of each word with its immediate context."

In Lexichord, "Concordance" extends this concept:

- **Rules are explicit:** No ambiguity about what is correct.
- **Consistency is automatic:** Writers don't need to remember style choices.
- **Customization is encouraged:** Your rules, your way (WriterPro tier).
- **Violations are opportunities:** Every squiggle is a learning moment.

### 1.3 Business Value

- **Product Differentiation:** No Markdown editor offers embedded style governance.
- **Upsell Path:** Core tier gets standard rules; WriterPro unlocks custom YAML.
- **Enterprise Value:** Teams/Enterprise will add shared style sheets, rule inheritance.
- **Stickiness:** Writers who configure their style rules won't leave easily.
- **Foundation for AI:** Style rules become constraints for AI writing assistance (v0.3.x+).

### 1.4 Dependencies on Previous Versions

| Component           | Source  | Usage                                                         |
| :------------------ | :------ | :------------------------------------------------------------ |
| IModule             | v0.0.4a | Style module implements IModule interface                     |
| ModuleLoader        | v0.0.4b | Discovers and loads Style module at startup                   |
| IMediator/Event Bus | v0.0.7  | Publish StyleViolationDetectedEvent                           |
| IConfiguration      | v0.0.3d | Load style configuration paths via IOptions<LexichordOptions> |
| IEditorService      | v0.1.3a | Access document content for style analysis                    |
| ManuscriptViewModel | v0.1.3a | Wire violation squiggles to editor                            |
| Serilog             | v0.0.3b | Log rule loading and violation detection                      |

---

## 2. Sub-Part Specifications

### 2.1 v0.2.1a: Module Scaffolding

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-021a                  |
| **Title**        | Module Scaffolding        |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Create the `Lexichord.Modules.Style` project with proper structure, add reference to `Lexichord.Abstractions`, implement `IModule`, and register in Host.

**Key Deliverables:**

- Create `Lexichord.Modules.Style` class library project
- Add project reference to `Lexichord.Abstractions`
- Implement `StyleModule : IModule` with proper lifecycle
- Configure output to `./Modules/` directory
- Define `IStyleEngine` core interface in Abstractions

**Key Interfaces:**

```csharp
public interface IStyleEngine
{
    Task<IReadOnlyList<StyleViolation>> AnalyzeAsync(
        string content,
        StyleSheet styleSheet,
        CancellationToken cancellationToken = default);

    StyleSheet GetActiveStyleSheet();
    void SetActiveStyleSheet(StyleSheet styleSheet);
    event EventHandler<StyleSheetChangedEventArgs>? StyleSheetChanged;
}
```

**Dependencies:**

- v0.0.4a: IModule (module contract)
- v0.0.4b: ModuleLoader (discovery and loading)

---

### 2.2 v0.2.1b: Rule Object Model

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-021b                  |
| **Title**        | Rule Object Model         |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | Core                      |

**Goal:** Define the core domain objects that represent style rules, categories, and violations. This is the vocabulary of the Concordance philosophy.

**Key Deliverables:**

- Define `StyleRule` record with pattern matching support
- Define `RuleCategory` enum: Terminology, Formatting, Syntax
- Define `ViolationSeverity` enum: Error, Warning, Info, Hint
- Define `StyleViolation` record with position and suggestion
- Define `StyleSheet` aggregate containing rule collections

**Key Interfaces:**

```csharp
public record StyleRule(
    string Id,
    string Name,
    string Description,
    RuleCategory Category,
    ViolationSeverity DefaultSeverity,
    string Pattern,
    PatternType PatternType,
    string? Suggestion,
    bool IsEnabled);

public enum RuleCategory { Terminology, Formatting, Syntax }
public enum ViolationSeverity { Error, Warning, Info, Hint }
```

**Dependencies:**

- v0.2.1a: Module project (location for domain objects)

---

### 2.3 v0.2.1c: YAML Deserializer

| Field            | Value                               |
| :--------------- | :---------------------------------- |
| **Sub-Part ID**  | INF-021c                            |
| **Title**        | YAML Deserializer                   |
| **Module**       | `Lexichord.Modules.Style`           |
| **License Tier** | Core (embedded), WriterPro (custom) |

**Goal:** Install YamlDotNet, implement `StyleSheetLoader` service that deserializes YAML rule definitions, and create a default `lexichord.yaml` embedded resource with standard rules.

**Key Deliverables:**

- Install `YamlDotNet` NuGet package
- Implement `IStyleSheetLoader` interface
- Implement `YamlStyleSheetLoader` with embedded resource support
- Create `lexichord.yaml` embedded resource with 25+ standard rules
- Validation for YAML schema (report malformed rule files)

**Key Interfaces:**

```csharp
public interface IStyleSheetLoader
{
    Task<StyleSheet> LoadFromFileAsync(string filePath, CancellationToken ct = default);
    Task<StyleSheet> LoadFromStreamAsync(Stream stream, CancellationToken ct = default);
    Task<StyleSheet> LoadEmbeddedDefaultAsync(CancellationToken ct = default);
    StyleSheetLoadResult ValidateYaml(string yamlContent);
}
```

**Dependencies:**

- v0.2.1b: Domain objects (StyleSheet, StyleRule)
- v0.0.3d: IConfiguration + IOptions<LexichordOptions> (locate style files)

---

### 2.4 v0.2.1d: Configuration Watcher

| Field            | Value                     |
| :--------------- | :------------------------ |
| **Sub-Part ID**  | INF-021d                  |
| **Title**        | Configuration Watcher     |
| **Module**       | `Lexichord.Modules.Style` |
| **License Tier** | WriterPro                 |

**Goal:** Implement `FileSystemWatcher` for project root `.lexichord/style.yaml`, enabling auto-reload of rules without application restart. This enables the "live editing" workflow.

**Key Deliverables:**

- Implement `IStyleConfigurationWatcher` interface
- Implement `FileSystemStyleWatcher` with debouncing
- Wire to `IStyleEngine.SetActiveStyleSheet()` on file change
- Publish `StyleSheetReloadedEvent` via MediatR
- Handle file errors gracefully (revert to previous valid rules)

**Key Interfaces:**

```csharp
public interface IStyleConfigurationWatcher : IDisposable
{
    void StartWatching(string projectRoot);
    void StopWatching();
    bool IsWatching { get; }
    string? WatchedPath { get; }
    event EventHandler<StyleFileChangedEventArgs>? FileChanged;
    event EventHandler<StyleWatcherErrorEventArgs>? WatcherError;
}
```

**Dependencies:**

- v0.2.1c: StyleSheetLoader (reload rules)
- v0.0.7: Event Bus (publish reload events)
- License: WriterPro (custom rules feature)

---

## 3. Implementation Checklist

| #         | Sub-Part | Task                                            | Est. Hours   |
| :-------- | :------- | :---------------------------------------------- | :----------- |
| 1         | v0.2.1a  | Create `Lexichord.Modules.Style` project        | 0.5          |
| 2         | v0.2.1a  | Add reference to `Lexichord.Abstractions`       | 0.25         |
| 3         | v0.2.1a  | Configure output to `./Modules/` directory      | 0.25         |
| 4         | v0.2.1a  | Define `IStyleEngine` interface in Abstractions | 1            |
| 5         | v0.2.1a  | Implement `StyleModule : IModule`               | 2            |
| 6         | v0.2.1a  | Register services in `RegisterServices()`       | 1            |
| 7         | v0.2.1a  | Unit tests for StyleModule lifecycle            | 2            |
| 8         | v0.2.1b  | Define `RuleCategory` enum                      | 0.5          |
| 9         | v0.2.1b  | Define `ViolationSeverity` enum                 | 0.5          |
| 10        | v0.2.1b  | Define `PatternType` enum                       | 0.5          |
| 11        | v0.2.1b  | Define `StyleRule` record                       | 2            |
| 12        | v0.2.1b  | Define `StyleViolation` record                  | 1            |
| 13        | v0.2.1b  | Define `StyleSheet` aggregate                   | 2            |
| 14        | v0.2.1b  | Unit tests for domain objects                   | 2            |
| 15        | v0.2.1c  | Install YamlDotNet NuGet package                | 0.25         |
| 16        | v0.2.1c  | Define `IStyleSheetLoader` interface            | 1            |
| 17        | v0.2.1c  | Implement `YamlStyleSheetLoader`                | 4            |
| 18        | v0.2.1c  | Create embedded `lexichord.yaml` with 25+ rules | 4            |
| 19        | v0.2.1c  | Implement YAML validation with error reporting  | 2            |
| 20        | v0.2.1c  | Unit tests for YAML deserialization             | 3            |
| 21        | v0.2.1d  | Define `IStyleConfigurationWatcher` interface   | 1            |
| 22        | v0.2.1d  | Implement `FileSystemStyleWatcher`              | 3            |
| 23        | v0.2.1d  | Implement debouncing (300ms default)            | 1            |
| 24        | v0.2.1d  | Wire to StyleEngine for auto-reload             | 1            |
| 25        | v0.2.1d  | Define and publish `StyleSheetReloadedEvent`    | 1            |
| 26        | v0.2.1d  | Handle file errors with graceful fallback       | 2            |
| 27        | v0.2.1d  | Unit tests for file watcher                     | 3            |
| 28        | All      | Integration tests for Style module              | 4            |
| **Total** |          |                                                 | **46 hours** |

---

## 4. Risks & Mitigations

| Risk                             | Impact | Probability | Mitigation                                    |
| :------------------------------- | :----- | :---------- | :-------------------------------------------- |
| YamlDotNet breaking changes      | High   | Low         | Pin specific version; wrap in adapter         |
| Regex patterns cause ReDoS       | High   | Medium      | Timeout pattern execution; complexity limits  |
| File watcher events flood        | Medium | Medium      | Debounce at 300ms; coalesce rapid changes     |
| Invalid YAML crashes app         | High   | Medium      | Validate before loading; fallback to defaults |
| Large rule sets slow analysis    | Medium | Low         | Lazy evaluation; parallel pattern matching    |
| User deletes style.yaml mid-edit | Low    | Medium      | Keep last valid rules in memory               |

---

## 5. Success Metrics

| Metric                        | Target  | Measurement                     |
| :---------------------------- | :------ | :------------------------------ |
| Module load time              | < 50ms  | Stopwatch in InitializeAsync    |
| YAML parse time (100 rules)   | < 100ms | Stopwatch in LoadFromFileAsync  |
| File change detection         | < 500ms | Time from save to reload event  |
| Analysis latency (1000 lines) | < 200ms | Stopwatch in AnalyzeAsync       |
| Memory per rule               | < 1KB   | Memory profiler                 |
| False positive rate           | < 5%    | Manual review of standard rules |

---

## 6. What This Enables

After v0.2.1, Lexichord will support:

- **Core Tier:** Embedded style rules for common writing standards
- **WriterPro Tier:** Custom `.lexichord/style.yaml` with live reload
- **Consistency:** Writers get immediate feedback on style violations
- **Foundation for v0.2.2:** Squiggle rendering, tooltips, quick fixes
- **Foundation for v0.2.3:** Real-time analysis as user types
- **Foundation for v0.3.x:** AI respects style rules during generation
- **Module Template:** Reference implementation for domain-specific modules
