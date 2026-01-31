# LCS-CL-002d: Changelog — Window State Persistence

## Document Control

| Field            | Value                                                  |
| :--------------- | :----------------------------------------------------- |
| **Document ID**  | LCS-CL-002d                                            |
| **Version**      | v0.0.2d                                                |
| **Date**         | 2026-01-28                                             |
| **Author**       | System                                                 |
| **Related Spec** | [LCS-DES-002d](../specs/v0.0.x/v0.0.2/LCS-DES-002d.md) |

---

## Summary

Implemented window state persistence — position, size, maximized state, and theme are saved to JSON and restored on next launch.

---

## Changes

### Files Created

| File                                            | Purpose                            |
| :---------------------------------------------- | :--------------------------------- |
| `Abstractions/Contracts/WindowStateRecord.cs`   | Record: X, Y, Width, Height, Theme |
| `Abstractions/Contracts/IWindowStateService.cs` | Service interface                  |
| `Host/Services/WindowStateService.cs`           | JSON file persistence in AppData   |

### Files Modified

| File                             | Change                           |
| :------------------------------- | :------------------------------- |
| `Host/App.axaml.cs`              | Creates/wires WindowStateService |
| `Host/Views/MainWindow.axaml.cs` | Save on close, restore on open   |

---

## Persistence Location

| Platform | Path                                       |
| :------- | :----------------------------------------- |
| macOS    | `~/Library/Application Support/Lexichord/` |
| Windows  | `%APPDATA%\Lexichord\`                     |
| Linux    | `~/.config/Lexichord/`                     |

**File:** `appstate.json`

---

## Acceptance Criteria Verification

| Criterion                              | Status  |
| :------------------------------------- | :------ |
| `dotnet build` succeeds                | ✅ Pass |
| Window position restored               | ✅ Pass |
| Window size restored                   | ✅ Pass |
| Maximized state restored               | ✅ Pass |
| Theme preference restored              | ✅ Pass |
| Off-screen positions → center fallback | ✅ Pass |
| Corrupted JSON → graceful recovery     | ✅ Pass |

---

## Notes

- Avalonia 11.x doesn't expose `RestoreBounds` — maximized windows save default size
- JSON uses camelCase property names
- Service is fault-tolerant: never throws on I/O errors
