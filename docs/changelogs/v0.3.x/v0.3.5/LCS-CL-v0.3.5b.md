# LCS-CL-035b: Spider Chart with Configurable Axes

## Document Control

| Field           | Value                                                     |
| :-------------- | :-------------------------------------------------------- |
| **Version**     | v0.3.5b                                                   |
| **Status**      | ✅ Complete                                               |
| **Created**     | 2026-01-30                                                |
| **Parent Spec** | [LCS-DES-035b](../../specs/v0.3.x/v0.3.5/LCS-DES-035b.md) |

---

## Summary

Implemented the Spider Chart visualization for the Resonance Dashboard, displaying writing style metrics with configurable axes and target overlays based on voice profiles.

---

## Changes

### New Interfaces (Lexichord.Modules.Style)

| Interface                      | Purpose                                      |
| ------------------------------ | -------------------------------------------- |
| `ISpiderChartSeriesBuilder`    | Builds polar chart series for current/target |
| `ITargetOverlayService`        | Computes target overlay from voice profile   |
| `IResonanceDashboardViewModel` | ViewModel contract for the dashboard view    |

### New Records (Lexichord.Abstractions)

| Record          | Purpose                                       |
| --------------- | --------------------------------------------- |
| `TargetOverlay` | Immutable target values computed from profile |

### New Services (Lexichord.Modules.Style)

| Class                      | Purpose                                                 |
| -------------------------- | ------------------------------------------------------- |
| `SpiderChartSeriesBuilder` | Creates LiveCharts polar series with theme-aware colors |
| `TargetOverlayService`     | Computes/caches overlays per profile                    |

### New ViewModel (Lexichord.Modules.Style)

| Class                         | Purpose                                    |
| ----------------------------- | ------------------------------------------ |
| `ResonanceDashboardViewModel` | Manages chart series, axes, overlay toggle |

### New View (Lexichord.Modules.Style)

| Class                    | Purpose                                          |
| ------------------------ | ------------------------------------------------ |
| `ResonanceDashboardView` | AXAML polar chart with toggle and license gating |

---

## Feature Code Added

| Code                  | Tier       | Feature                    |
| --------------------- | ---------- | -------------------------- |
| `RESONANCE_DASHBOARD` | Writer Pro | Resonance Dashboard access |

---

## DI Registration

Added to `StyleModule.RegisterServices`:

```csharp
services.AddSingleton<ISpiderChartSeriesBuilder, SpiderChartSeriesBuilder>();
services.AddSingleton<ITargetOverlayService, TargetOverlayService>();
services.AddSingleton<IResonanceDashboardViewModel, ResonanceDashboardViewModel>();
services.AddTransient<ResonanceDashboardView>();
```

---

## Tests Added

| Test Class                         | Tests | Description                                |
| ---------------------------------- | ----- | ------------------------------------------ |
| `SpiderChartSeriesBuilderTests`    | 10    | Series construction, theme handling        |
| `TargetOverlayServiceTests`        | 9     | Overlay computation, caching, invalidation |
| `ResonanceDashboardViewModelTests` | 11    | Initialization, licensing, series binding  |

**Total:** 30 new tests

---

## Dependencies

No new package dependencies. Builds on LiveCharts2 from v0.3.5a.

---

## Design Decisions

1. **Interface Location**: `ISpiderChartSeriesBuilder` and `IResonanceDashboardViewModel` placed in `Lexichord.Modules.Style` (not Abstractions) to avoid LiveCharts dependency in the Abstractions project.

2. **Target Overlay Caching**: Thread-safe `ConcurrentDictionary` used for profile-keyed overlay cache with explicit invalidation methods.

3. **Theme-Aware Colors**: Series colors adapt to light/dark theme via ViewModel theme detection.
