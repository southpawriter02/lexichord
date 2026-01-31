# LCS-CL-007b: Shared Domain Events

**Version:** v0.0.7b  
**Category:** Infrastructure  
**Feature Name:** Shared Domain Events  
**Date:** 2026-01-28

---

## Summary

Defines shared domain events that enable loose coupling between Lexichord modules. Publishers can notify subscribers of state changes without direct dependencies.

---

## New Features

### Base Record

- **DomainEventBase** — Abstract base record for all domain events
    - `EventId` — Auto-generated Guid for event deduplication
    - `OccurredAt` — UTC timestamp of when the event occurred
    - `CorrelationId` — Optional distributed tracing support
    - Implements `IDomainEvent` from v0.0.7a

### Content Events

- **ContentCreatedEvent** — Published when content is created
    - `ContentId` — Unique identifier (string)
    - `ContentType` — Enum specifying content category
    - `Title` — Human-readable title
    - `Description` — Optional synopsis
    - `CreatedBy` — User identifier
    - `Metadata` — Optional key-value pairs

- **ContentType** enum — Document, Chapter, Project, Note, Template, StyleGuide, WorldbuildingElement, Reference

### Settings Events

- **SettingsChangedEvent** — Published when settings change
    - `SettingKey` — Dot-notation key (e.g., "appearance.theme")
    - `OldValue` / `NewValue` — Previous and new values
    - `Scope` — Setting scope level
    - `ModuleId` — Optional module identifier
    - `ChangedBy` — User or system identifier
    - `Reason` — Optional change reason

- **SettingScope** enum — Application, User, Module, Project, Document

---

## Files Added

| File                                                                          | Description                |
| :---------------------------------------------------------------------------- | :------------------------- |
| `src/Lexichord.Abstractions/Events/DomainEventBase.cs`                        | Abstract base record       |
| `src/Lexichord.Abstractions/Events/ContentType.cs`                            | Content type enumeration   |
| `src/Lexichord.Abstractions/Events/ContentCreatedEvent.cs`                    | Content creation event     |
| `src/Lexichord.Abstractions/Events/SettingScope.cs`                           | Setting scope enumeration  |
| `src/Lexichord.Abstractions/Events/SettingsChangedEvent.cs`                   | Settings change event      |
| `tests/Lexichord.Tests.Unit/Abstractions/Events/ContentCreatedEventTests.cs`  | ContentCreatedEvent tests  |
| `tests/Lexichord.Tests.Unit/Abstractions/Events/SettingsChangedEventTests.cs` | SettingsChangedEvent tests |
| `tests/Lexichord.Tests.Unit/Abstractions/Events/DomainEventBaseTests.cs`      | DomainEventBase tests      |

---

## Usage

### Publishing Content Created Event

```csharp
await _mediator.Publish(new ContentCreatedEvent
{
    ContentId = document.Id.ToString(),
    ContentType = ContentType.Document,
    Title = document.Title,
    Description = document.Synopsis,
    CreatedBy = currentUser.Id,
    CorrelationId = correlationId,
    Metadata = new Dictionary<string, string>
    {
        ["wordCount"] = document.WordCount.ToString(),
        ["format"] = document.Format.ToString()
    }
});
```

### Publishing Settings Changed Event

```csharp
await _mediator.Publish(new SettingsChangedEvent
{
    SettingKey = "appearance.theme",
    OldValue = "Dark",
    NewValue = "Light",
    Scope = SettingScope.User,
    ChangedBy = currentUser.Id,
    CorrelationId = correlationId
});
```

### Handling Events

```csharp
public class IndexContentHandler : INotificationHandler<ContentCreatedEvent>
{
    public async Task Handle(ContentCreatedEvent evt, CancellationToken ct)
    {
        // Filter by content type if needed
        if (evt.ContentType is ContentType.Template or ContentType.StyleGuide)
            return;

        // Process the event
        await _indexer.IndexAsync(evt.ContentId, evt.Title, ct);
    }
}
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run Event unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Event"

# 3. Verify events implement IDomainEvent
grep -l "DomainEventBase" src/Lexichord.Abstractions/Events/*.cs
```

---

## Test Summary

| Test Class                | Tests  | Status |
| :------------------------ | :----- | :----- |
| ContentCreatedEventTests  | 8      | ✅     |
| SettingsChangedEventTests | 11     | ✅     |
| DomainEventBaseTests      | 10     | ✅     |
| **Total**                 | **29** | **✅** |

---

## Dependencies

- **From v0.0.7a:** `IDomainEvent` interface in `Lexichord.Abstractions.Messaging`
- **NuGet:** MediatR.Contracts 2.0.1

## Enables

- **v0.0.7c:** Logging Pipeline Behavior (logs event publishing)
- **v0.0.7d:** Validation Pipeline Behavior
- **v0.0.8:** Settings Service (uses SettingsChangedEvent)
- **v0.2.x:** Document Module (uses ContentCreatedEvent)
