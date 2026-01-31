# LCS-CL-035c: Real-Time Chart Updates

## Version

v0.3.5c

## Release Date

2026-01-31

## Overview

Implements a reactive update pipeline for the Resonance Dashboard that automatically refreshes the spider chart when document analysis completes. Uses Rx-based debouncing (300ms) to coalesce rapid updates during active typing while providing immediate response for profile changes.

## Changes

### New Components

#### Abstractions (`Lexichord.Abstractions`)

| File                         | Type                 | Description                                                                         |
| ---------------------------- | -------------------- | ----------------------------------------------------------------------------------- |
| `UpdateTrigger.cs`           | Enum                 | Categorizes update sources (ReadabilityAnalyzed, ProfileChanged, ForceUpdate, etc.) |
| `ChartUpdateEvent.cs`        | MediatR Notification | Published when chart updates are dispatched                                         |
| `ChartUpdateEventArgs.cs`    | Record               | Contains trigger type, timing info, and immediacy flag                              |
| `IResonanceUpdateService.cs` | Interface            | Manages update pipeline with lifecycle and force-refresh                            |

#### Service Implementation (`Lexichord.Modules.Style`)

| File                        | Type    | Description                                     |
| --------------------------- | ------- | ----------------------------------------------- |
| `ResonanceUpdateService.cs` | Service | Implements reactive pipeline with Rx debouncing |

### Modified Components

| File                             | Change Description                                   |
| -------------------------------- | ---------------------------------------------------- |
| `StyleModule.cs`                 | Added DI registration for `IResonanceUpdateService`  |
| `ResonanceDashboardViewModel.cs` | Integrated update service subscription and lifecycle |

## Technical Details

### Debouncing Strategy

- **Analysis events** (ReadabilityAnalyzed, VoiceAnalysisCompleted): 300ms debounce via Rx `Throttle`
- **Profile changes**: Immediate dispatch (no debounce)
- **Force update**: Immediate dispatch with cache invalidation

### Reactive Pipeline

```
MediatR Events → ResonanceUpdateService
    │
    ├─ ReadabilityAnalyzedEvent → [300ms Throttle] → DispatchUpdate
    │
    └─ ProfileChangedEvent ───────────────────────→ DispatchUpdate (immediate)
                                                          │
                                                          ▼
                                              IObservable<ChartUpdateEventArgs>
                                                          │
                                                          ▼
                                              ResonanceDashboardViewModel
                                                          │
                                                          ▼
                                                  RefreshChartAsync()
```

### License Gating

Updates only process when `FeatureCodes.ResonanceDashboard` is enabled (Writer Pro tier).

### Lifecycle Management

- `StartListening()`: Activates debounce subscription
- `StopListening()`: Disposes subscription, clears pending updates
- `Dispose()`: Full cleanup with disposal tracking

## Testing

### New Test File

- `ResonanceUpdateServiceTests.cs` - 15 unit tests covering:
    - Lifecycle (idempotent start/stop, disposal)
    - Debouncing (coalesces rapid events)
    - Immediate dispatch (profile changes, force update)
    - License gating (no dispatch when unlicensed)
    - MediatR publishing

### Test Results

```
Passed: 16 tests
Failed: 0
Skipped: 0
Duration: 3.2s
```

## Dependencies

- Existing `System.Reactive` package (already referenced)
- No new NuGet dependencies

## Migration Notes

- No breaking changes
- Existing `ChartDataService.DataUpdated` event remains for compatibility
- ViewModel now subscribes to both legacy event and new update service

## Related Specifications

- Parent: [LCS-DES-035-INDEX.md](./LCS-DES-035-INDEX.md) (Resonance Dashboard)
- Scope: [LCS-SBD-035.md](./LCS-SBD-035.md)
- Previous: [LCS-CL-035b.md](./LCS-CL-035b.md) (Spider Chart)
