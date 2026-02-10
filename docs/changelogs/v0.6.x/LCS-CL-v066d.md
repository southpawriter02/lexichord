# v0.6.6d — Usage Tracking

**Phase:** Conversational Usage Metrics  
**Status:** Complete  
**Date:** 2026-02-09  

---

## Summary

Implements per-conversation and session-level usage tracking with MediatR event publishing, license-gated monthly summaries, and a throttled ViewModel for UI display. Tracks token consumption, estimated costs, and agent invocation metadata across conversations and sessions.

## New Files

### Abstractions (`src/Lexichord.Abstractions/Agents/Events/`)

| File | Description |
|------|-------------|
| `AgentInvocationEvent.cs` | MediatR `INotification` record with `AgentId`, `Model`, `PromptTokens`, `CompletionTokens`, `Duration`, `Streamed`, computed `TotalTokens`, auto-set `Timestamp`, and optional `ConversationId` |

### Implementation (`src/Lexichord.Modules.Agents/`)

| File | Description |
|------|-------------|
| `Chat/Services/InvocationContext.cs` | Record capturing invocation metadata (AgentId, Model, Duration, Streamed) |
| `Chat/Services/UsageRecordedEventArgs.cs` | EventArgs with `ThisInvocation`, `ConversationTotal`, `SessionTotal` |
| `Chat/Services/UsageSummaryTypes.cs` | `MonthlyUsageSummary`, `AgentUsageSummary`, `ModelUsageSummary` records and `ExportFormat` enum |
| `Chat/Services/SessionUsageCoordinator.cs` | Thread-safe singleton accumulating session-level usage across conversations |
| `Chat/Services/UsageTracker.cs` | Scoped service with conversation/session accumulation, MediatR event publishing, license-gated persistence, and `UsageRecorded` event |
| `Chat/Persistence/UsageRecord.cs` | POCO entity for in-memory usage storage with computed `Month` key |
| `Chat/Persistence/UsageRepository.cs` | In-memory repository with thread-safe `RecordAsync`, `GetMonthlySummaryAsync` (with agent/model breakdown), and `ExportAsync` (CSV/JSON) |
| `Chat/ViewModels/UsageDisplayViewModel.cs` | `ObservableObject`-based ViewModel with throttled 500ms updates, configurable thresholds, and `UsageDisplayState` enum (Normal/Warning/Critical) |
| `Chat/Events/Handlers/AgentInvocationHandler.cs` | `INotificationHandler<AgentInvocationEvent>` forwarding to `ITelemetryService` for breadcrumb tracking |

### Tests (`tests/Lexichord.Tests.Unit/Modules/Agents/Chat/Services/`)

| File | Tests |
|------|-------|
| `UsageTrackingTests.cs` | 15 (10 UsageTracker + 2 AgentInvocationEvent + 3 SessionUsageCoordinator) |

## Modified Files

| File | Change |
|------|--------|
| `Lexichord.Modules.Agents.csproj` | Added `MediatR 12.4.0` NuGet package |
| `Extensions/AgentsServiceCollectionExtensions.cs` | Added `AddUsageTracking()` extension (Singleton coordinator, Scoped tracker/repo, Transient ViewModel/handler) |
| `AgentsModule.cs` | Added `services.AddUsageTracking()` call |

## Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| In-memory `List<UsageRecord>` over EF Core | No EF Core dependency in Agents module; consistent with v0.6.6c persistence approach |
| `ITelemetryService` over `ITelemetryClient` | Correct interface per dependency matrix (v0.1.7d) |
| `ObservableObject` over `ViewModelBase` | CommunityToolkit.Mvvm already referenced (v0.6.4d) |
| `Action<Action>` dispatch parameter | Same testability pattern as `StreamingChatHandler` (v0.6.5c) |
| `GetCurrentTier()` over `Tier` property | Moq default interface member fix from v0.6.6c |
| MediatR 12.4.0 (not 12.4.1) | Version alignment with existing test project dependency |

## Verification

- **Build:** 0 errors, 0 warnings across all projects
- **Tests:** 15/15 passed (0 failed, 0 skipped)
- **Regression:** 8059/8059 passed (33 skipped, 1 pre-existing RAG flaky test)
