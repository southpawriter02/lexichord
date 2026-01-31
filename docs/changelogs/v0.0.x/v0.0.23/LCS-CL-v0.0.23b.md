# LCS-CL-023b: Debounce Logic — Changelog

## Document Control

| Field           | Value                      |
| :-------------- | :------------------------- |
| **Document ID** | LCS-CL-023b                |
| **Version**     | v0.2.3b                    |
| **Feature**     | The Critic (Linter Engine) |
| **Sub-part**    | Debounce Logic             |
| **Status**      | Complete                   |
| **Date**        | 2026-01-29                 |

---

## Summary

This sub-part replaces the inline reactive `Throttle` operator with a dedicated `DebounceController` class, providing explicit state machine semantics for the linting pipeline's debounce behavior.

---

## Changes Made

### New Files

| File                                                                    | Purpose                                                              |
| :---------------------------------------------------------------------- | :------------------------------------------------------------------- |
| `Lexichord.Abstractions/Contracts/Linting/ILintingConfiguration.cs`     | Interface for linting configuration abstraction                      |
| `Lexichord.Abstractions/Contracts/Linting/DebounceState.cs`             | Enum for debounce state machine (Idle, Waiting, Scanning, Cancelled) |
| `Lexichord.Modules.Style/Services/Linting/DebounceController.cs`        | Dedicated debounce controller with state tracking                    |
| `Lexichord.Tests.Unit/Modules/Style/Linting/DebounceControllerTests.cs` | 14 unit tests for controller behavior                                |

### Modified Files

| File                                                               | Changes                                                 |
| :----------------------------------------------------------------- | :------------------------------------------------------ |
| `Lexichord.Modules.Style/Services/Linting/DocumentSubscription.cs` | Refactored to delegate debounce to `DebounceController` |
| `Lexichord.Modules.Style/Lexichord.Modules.Style.csproj`           | Added `InternalsVisibleTo` for test access              |

---

## Technical Details

### DebounceController State Machine

```
   [Content Change]         [Timer Expires]        [Scan Complete]
        │                        │                       │
        ▼                        ▼                       ▼
      IDLE  ──────────────►  WAITING  ──────────►  SCANNING  ──►  IDLE
        ▲                        │                       │
        │                        │ [New Content]         │ [New Content]
        │                        ▼                       │
        │                   CANCELLED ◄─────────────────-┘
        │                        │
        └────────────────────────┘
              [RequestScan()]
```

### Key Behaviors

1. **Throttled Triggering**: Uses `System.Reactive.Linq.Throttle` for configurable delay
2. **Preemptive Cancellation**: In-flight scans cancelled when new content arrives
3. **Token Propagation**: Each scan receives its own `CancellationToken`
4. **Thread-Safe State**: Lock-protected state transitions

---

## Test Coverage

| Test Class                 | Tests | Status  |
| :------------------------- | :---- | :------ |
| `DebounceControllerTests`  | 14    | ✅ Pass |
| `LintingOrchestratorTests` | 46    | ✅ Pass |

### New Test Cases

- `InitialState_IsIdle`
- `RequestScan_TransitionsToWaiting`
- `AfterDebounceDelay_TransitionsToScanning`
- `RapidEdits_ResetDebounceTimer`
- `CancelCurrent_WhenWaiting_TransitionsToCancelled`
- `CancelCurrent_WhenScanning_TransitionsToCancelled`
- `CancelCurrent_CancelsToken`
- `MarkCompleted_TransitionsBackToIdle`
- `MarkCancelled_TransitionsToCancelled`
- `Dispose_CancelsPendingOperations`
- `Dispose_CancelsInFlightScan`
- `Constructor_ThrowsOnNullCallback`
- `NewRequestDuringScanning_CancelsPreviousAndStartsNew`
- `ConfigurableDelay_Respected`

---

## Breaking Changes

None. Internal implementation refactor only.

---

## Dependencies

| Dependency      | Version | Purpose                               |
| :-------------- | :------ | :------------------------------------ |
| System.Reactive | 6.0.1   | Throttle operator for debounce timing |

---

## Related Documents

- [LCS-DES-023b](../specs/v0.2.x/v0.2.3/LCS-DES-023b.md) — Design Specification
- [LCS-CL-023a](./v0.2.x/LCS-CL-023a.md) — Reactive Pipeline (prerequisite)
