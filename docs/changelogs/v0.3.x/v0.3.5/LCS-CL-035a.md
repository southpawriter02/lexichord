# LCS-CL-035a: Charting Infrastructure

## Document Control

| Field           | Value                                                     |
| :-------------- | :-------------------------------------------------------- |
| **Version**     | v0.3.5a                                                   |
| **Status**      | ✅ Complete                                               |
| **Created**     | 2026-01-30                                                |
| **Parent Spec** | [LCS-DES-035a](../../specs/v0.3.x/v0.3.5/LCS-DES-035a.md) |

---

## Summary

Integrated the LiveCharts2 charting library infrastructure for the Resonance Dashboard, enabling visualization of writing metrics in a spider/radar chart format.

---

## Changes

### New Interfaces (Lexichord.Abstractions)

| Interface                | Purpose                                         |
| ------------------------ | ----------------------------------------------- |
| `IChartDataService`      | Chart data aggregation with thread-safe caching |
| `IResonanceAxisProvider` | Axis configuration provider contract            |

### New Records (Lexichord.Abstractions)

| Record                      | Purpose                                     |
| --------------------------- | ------------------------------------------- |
| `ResonanceChartData`        | Immutable chart data transfer object        |
| `ResonanceDataPoint`        | Single axis data with normalized/raw values |
| `ResonanceAxisDefinition`   | Axis metadata with normalization logic      |
| `ChartDataUpdatedEventArgs` | Event args for chart data updates           |

### New Services (Lexichord.Modules.Style)

| Class                     | Purpose                                                |
| ------------------------- | ------------------------------------------------------ |
| `ChartDataService`        | Aggregates metrics from upstream services with caching |
| `DefaultAxisProvider`     | Provides 6 default axes for spider chart               |
| `ChartThemeConfiguration` | Light/Dark theme color palettes                        |
| `ChartColors`             | Chart color palette record                             |

---

## New Dependencies

| Package                                 | Version     | Purpose                          |
| --------------------------------------- | ----------- | -------------------------------- |
| `LiveChartsCore.SkiaSharpView.Avalonia` | 2.0.0-rc6.1 | Avalonia-native charting library |

---

## DI Registration

Added to `StyleModule.RegisterServices`:

```csharp
services.AddSingleton<IChartDataService, ChartDataService>();
services.AddSingleton<IResonanceAxisProvider, DefaultAxisProvider>();
```

---

## Tests Added

| Test Class                     | Tests | Description                                 |
| ------------------------------ | ----- | ------------------------------------------- |
| `ChartDataServiceTests`        | 10    | Caching, invalidation, events, cancellation |
| `DefaultAxisProviderTests`     | 6     | Axis configuration validation               |
| `ResonanceAxisDefinitionTests` | 10    | Normalization algorithm edge cases          |

**Total:** 26 new tests

---

## Deferred to v0.3.5b

- `ResonanceChartControl.axaml`
- `ResonanceChartViewModel`
- LiveCharts visual configuration
