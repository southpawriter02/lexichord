# LCS-CL-008b: Database Health Check

**Version:** v0.0.8b  
**Category:** Infrastructure  
**Feature Name:** Database Health Check ("The Hello World Golden Skeleton")  
**Date:** 2026-01-29

---

## Summary

Implements SQLite-based database health monitoring for the StatusBar module. Replaces stub implementations from v0.0.8a with real functionality that tracks application uptime, records periodic heartbeats, and displays system health status in the UI.

---

## New Features

### SQLite Health Infrastructure

- **HealthRepository** — Full SQLite implementation for health data
    - Creates `system_health` table on initialization
    - Uses singleton row pattern (id=1) for session data
    - Records startup time and schema version
    - Tracks heartbeat timestamps
    - Calculates uptime from in-memory startup time
    - Parameterized queries for SQL injection prevention

- **HeartbeatService** — Timer-based heartbeat recording
    - 60-second heartbeat interval
    - Records initial heartbeat on start
    - Tracks consecutive failures
    - Logs critical warning after 5 consecutive failures
    - Proper IDisposable implementation
    - HealthChanged event for status updates

### StatusBarViewModel Enhancements

- **FormatUptime(TimeSpan)** — Static method for human-readable uptime
    - Formats as "H:MM:SS" for under 24 hours
    - Formats as "Nd H:MM:SS" for 1+ days
    - Zero-pads minutes and seconds

- **Heartbeat Staleness Detection** — Warning indicator after 2+ minutes
    - Monitors last heartbeat timestamp
    - Yellow indicator for stale heartbeats
    - Demonstrates degraded state detection

- **RefreshDatabaseStatusCommand** — Manual refresh capability
    - Allows user-triggered health check
    - Updates all database indicators

### Domain Event

- **SystemHealthChangedEvent** — MediatR notification for health changes
    - HealthStatus enum: Healthy, Warning, Unhealthy
    - Includes message and timestamp
    - Enables cross-module health monitoring

---

## Files Added

### Lexichord.Abstractions

| File                                                            | Description         |
| :-------------------------------------------------------------- | :------------------ |
| `src/Lexichord.Abstractions/Events/SystemHealthChangedEvent.cs` | Health change event |

### Unit Tests

| File                                                                    | Description      |
| :---------------------------------------------------------------------- | :--------------- |
| `tests/Lexichord.Tests.Unit/Modules/StatusBar/HealthRepositoryTests.cs` | Repository tests |
| `tests/Lexichord.Tests.Unit/Modules/StatusBar/HeartbeatServiceTests.cs` | Heartbeat tests  |
| `tests/Lexichord.Tests.Unit/Modules/StatusBar/UptimeFormattingTests.cs` | Formatting tests |

## Files Modified

| File                                                                 | Description                   |
| :------------------------------------------------------------------- | :---------------------------- |
| `src/Lexichord.Modules.StatusBar/Lexichord.Modules.StatusBar.csproj` | Added Microsoft.Data.Sqlite   |
| `src/Lexichord.Modules.StatusBar/Services/IHealthRepository.cs`      | Expanded interface            |
| `src/Lexichord.Modules.StatusBar/Services/HealthRepository.cs`       | Full SQLite implementation    |
| `src/Lexichord.Modules.StatusBar/Services/IHeartbeatService.cs`      | Added IDisposable, properties |
| `src/Lexichord.Modules.StatusBar/Services/HeartbeatService.cs`       | Full timer implementation     |
| `src/Lexichord.Modules.StatusBar/ViewModels/StatusBarViewModel.cs`   | Uptime, staleness, formatting |

---

## Architecture

### Health Data Flow

```
1. StatusBarModule.InitializeAsync()
   ├─ HealthRepository.RecordStartupAsync()
   │  └─ Creates/updates system_health row
   └─ HeartbeatService.Start()
      └─ Records initial heartbeat

2. Every 60 seconds (timer):
   └─ HeartbeatService.OnTimerElapsed()
      └─ HealthRepository.RecordHeartbeatAsync()
         └─ Updates last_heartbeat column

3. StatusBarViewModel (every second):
   └─ UpdateUptimeAsync()
      └─ HealthRepository.GetSystemUptimeAsync()
         └─ Returns DateTime.UtcNow - _startupTime
```

### SQLite Schema

```sql
CREATE TABLE IF NOT EXISTS system_health (
    id INTEGER PRIMARY KEY CHECK (id = 1),
    started_at TEXT NOT NULL,
    last_heartbeat TEXT NOT NULL,
    database_version INTEGER NOT NULL
);
```

### Heartbeat Staleness Detection

```
Normal: heartbeat age < 2 minutes → Green indicator
Warning: heartbeat age > 2 minutes → Yellow indicator
Error: database unreachable → Red indicator
```

---

## Usage

### Checking Uptime Programmatically

```csharp
var uptime = await healthRepository.GetSystemUptimeAsync();
var formatted = StatusBarViewModel.FormatUptime(uptime);
// "1d 2:30:45" or "0:05:30"
```

### Subscribing to Health Changes

```csharp
heartbeatService.HealthChanged += (sender, isHealthy) =>
{
    if (!isHealthy)
    {
        _logger.LogWarning("System health degraded");
    }
};
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run all StatusBar tests
dotnet test --filter "FullyQualifiedName~StatusBar"

# 3. Run specific test classes
dotnet test --filter "FullyQualifiedName~HealthRepository"
dotnet test --filter "FullyQualifiedName~HeartbeatService"
dotnet test --filter "FullyQualifiedName~UptimeFormatting"

# 4. Verify SQLite database after run
cat ~/Library/Application\ Support/Lexichord/health.db | sqlite3 "SELECT * FROM system_health"
```

---

## Test Summary

| Test Class               | Tests  | Status |
| :----------------------- | :----- | :----- |
| StatusBarModuleTests     | 7      | ✅     |
| StatusBarRegionViewTests | 2      | ✅     |
| HealthRepositoryTests    | 8      | ✅     |
| HeartbeatServiceTests    | 11     | ✅     |
| UptimeFormattingTests    | 17     | ✅     |
| **Total**                | **45** | **✅** |

---

## Dependencies

- **From v0.0.8a:** StatusBarModule, IShellRegionView infrastructure
- **From v0.0.7a:** MediatR.Contracts for INotification
- **New:** Microsoft.Data.Sqlite 9.0.0 for SQLite database

## Enables

- **v0.0.8c:** Vault status monitoring (IVaultStatusService implementation)
- **v0.0.8d:** Golden skeleton integration testing
- **Future:** Database migration framework, multi-node health aggregation
