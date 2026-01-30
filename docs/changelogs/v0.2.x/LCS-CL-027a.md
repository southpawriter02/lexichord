# Changelog: v0.2.7a Async Offloading

**Version:** 0.2.7a
**Codename:** Polish (Part 1)
**Date:** 2026-01-30
**Design Spec:** [LCS-DES-027a](../../specs/v0.2.x/v0.2.7/LCS-DES-027a.md)

---

## Overview

Introduces the `IThreadMarshaller` abstraction for safe UI thread dispatching, enabling all linting operations to execute on background threads with results marshalled back to the UI thread. This is the foundation for the v0.2.7 performance polish milestone.

---

## Changes

### Abstraction Layer

#### New Files

| File | Description |
| :--- | :---------- |
| [IThreadMarshaller.cs](file:///src/Lexichord.Abstractions/Contracts/IThreadMarshaller.cs) | Testable abstraction for UI thread dispatching |

#### New Types

- `IThreadMarshaller` — Platform-agnostic contract for marshalling operations between background and UI threads
  - `InvokeOnUIThreadAsync(Action)` — Dispatch action to UI thread and await completion
  - `InvokeOnUIThreadAsync<T>(Func<T>)` — Dispatch function to UI thread and return result
  - `PostToUIThread(Action)` — Fire-and-forget dispatch to UI thread
  - `IsOnUIThread` — Property indicating current thread context
  - `AssertUIThread(string)` — Debug-only assertion for UI thread context
  - `AssertBackgroundThread(string)` — Debug-only assertion for background thread context

---

### Implementation Layer

#### New Files

| File | Description |
| :--- | :---------- |
| [AvaloniaThreadMarshaller.cs](file:///src/Lexichord.Modules.Style/Threading/AvaloniaThreadMarshaller.cs) | Production implementation using Avalonia Dispatcher.UIThread |

#### Key Features

- **Direct Execution Optimization**: If already on UI thread, actions execute directly without dispatch overhead
- **Debug Thread Assertions**: `[Conditional("DEBUG")]` assertions verify correct thread context without release build cost
- **Exception Safety**: `PostToUIThread` wraps fire-and-forget actions in try-catch with error logging
- **Structured Logging**: All cross-thread dispatches logged at Debug level with thread ID context

---

### Integration Layer

#### Modified Files

| File | Change |
| :--- | :----- |
| [StyleModule.cs](file:///src/Lexichord.Modules.Style/StyleModule.cs) | Registered `IThreadMarshaller` as singleton, bumped version to v0.2.7 |

---

### Unit Tests

#### New Files

| File | Test Count |
| :--- | :--------- |
| [TestThreadMarshaller.cs](file:///tests/Lexichord.Tests.Unit/Modules/Style/Threading/TestThreadMarshaller.cs) | Test double for IThreadMarshaller |
| [ThreadMarshallerTests.cs](file:///tests/Lexichord.Tests.Unit/Modules/Style/Threading/ThreadMarshallerTests.cs) | 15 tests |

#### Test Coverage

- Action invocation and execution verification
- Function invocation with return value
- Fire-and-forget posting
- UI thread assertion (pass and fail cases)
- Background thread assertion (pass and fail cases)
- IsOnUIThread toggle behavior
- Null argument handling
- Exception propagation

---

## Verification

```
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~ThreadMarshaller" --verbosity minimal
```

---

## Dependencies

| Interface | Version | Used By |
| :-------- | :------ | :------ |
| `IThreadMarshaller` | v0.2.7a | LintingOrchestrator, ViolationProvider |
| `Dispatcher.UIThread` | Avalonia 11.x | AvaloniaThreadMarshaller |
| `ILogger<T>` | v0.0.3b | Diagnostic logging |

---

## Notes

- Thread assertions compile out in Release builds via `[Conditional("DEBUG")]`
- TestThreadMarshaller executes all actions synchronously for deterministic test behavior
- AvaloniaThreadMarshaller is registered as singleton since Dispatcher.UIThread is process-global
