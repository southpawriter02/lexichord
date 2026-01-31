# LCS-CL-035d: Target Overlays

## Document Control

| Field           | Value                                                     |
| :-------------- | :-------------------------------------------------------- |
| **Version**     | v0.3.5d                                                   |
| **Status**      | ✅ Complete                                               |
| **Created**     | 2026-01-31                                                |
| **Parent Spec** | [LCS-DES-035d](../../specs/v0.3.x/v0.3.5/LCS-DES-035d.md) |

---

## Summary

Enhanced target overlay system with dedicated `TargetDataPoint` record supporting tolerance bands and synchronous accessor for improved UI binding.

---

## Changes

### New Records (Lexichord.Abstractions)

| Record            | Purpose                                                  |
| ----------------- | -------------------------------------------------------- |
| `TargetDataPoint` | Target overlay data with optional tolerance band support |

### Modified Records (Lexichord.Abstractions)

| Record          | Change                                                                                      |
| --------------- | ------------------------------------------------------------------------------------------- |
| `TargetOverlay` | Changed `DataPoints` type to `IReadOnlyList<TargetDataPoint>`, added `HasAnyToleranceBands` |

### Modified Interfaces (Lexichord.Abstractions)

| Interface               | Change                          |
| ----------------------- | ------------------------------- |
| `ITargetOverlayService` | Added `GetOverlaySync()` method |

### Modified Services (Lexichord.Modules.Style)

| Class                  | Change                                                             |
| ---------------------- | ------------------------------------------------------------------ |
| `TargetOverlayService` | Implemented `GetOverlaySync()`, updated to build `TargetDataPoint` |

### Modified ViewModels (Lexichord.Modules.Style)

| Class                         | Change                                                                             |
| ----------------------------- | ---------------------------------------------------------------------------------- |
| `ResonanceDashboardViewModel` | Added conversion from `TargetDataPoint` to `ResonanceDataPoint` for series builder |

---

## New Features

### TargetDataPoint Record

```csharp
public record TargetDataPoint(
    string AxisName,
    double NormalizedValue,
    double RawValue,
    double? ToleranceMin = null,
    double? ToleranceMax = null,
    string? Description = null)
{
    public bool HasToleranceBand => ToleranceMin.HasValue && ToleranceMax.HasValue;
    public string? Unit { get; init; }
}
```

### GetOverlaySync Method

```csharp
// Synchronous accessor for immediate UI binding
TargetOverlay? GetOverlaySync(VoiceProfile profile);
```

---

## Tests Added

| Test Class                  | New Tests | Description                                             |
| --------------------------- | --------- | ------------------------------------------------------- |
| `TargetOverlayServiceTests` | 8         | GetOverlaySync caching, TargetDataPoint tolerance bands |

**Total v0.3.5d tests:** 8

---

## Dependencies

No new package dependencies.

---

## Design Decisions

1. **Separate Record**: Created dedicated `TargetDataPoint` record rather than adding tolerance band fields to existing `ResonanceDataPoint` to maintain clear separation between current metrics and target constraints.

2. **Synchronous Accessor**: Added `GetOverlaySync()` for immediate UI updates without async overhead when cache is hit.

3. **Type Conversion**: ViewModel converts `TargetDataPoint` to `ResonanceDataPoint` for chart series builder compatibility, avoiding changes to the existing series builder interface.
