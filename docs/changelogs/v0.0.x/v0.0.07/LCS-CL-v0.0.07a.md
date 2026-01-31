# LCS-CL-007a: MediatR Bootstrap

**Version:** v0.0.7a  
**Category:** Infrastructure  
**Feature Name:** MediatR Bootstrap  
**Date:** 2026-01-28

---

## Summary

Establishes the MediatR in-process messaging infrastructure for Lexichord. This sub-part installs the MediatR package, defines CQRS marker interfaces (`ICommand`, `IQuery`, `IDomainEvent`), and configures DI with assembly scanning for handler discovery.

---

## New Features

### Marker Interfaces

- **ICommand\<TResponse\>** — Commands that change application state
    - Extends `MediatR.IRequest<TResponse>`
    - Single handler requirement
    - May cause side effects

- **ICommand** — Commands without return value
    - Extends `ICommand<MediatR.Unit>`
    - For operations where success = no exception

- **IQuery\<TResponse\>** — Queries that read application state
    - Extends `MediatR.IRequest<TResponse>`
    - Must NOT cause side effects
    - Always returns data

- **IDomainEvent** — Events that notify of state changes
    - Extends `MediatR.INotification`
    - Zero or more handlers
    - Properties: `EventId`, `OccurredAt`, `CorrelationId`

### DI Registration

- **AddMediatRServices()** — Extension method registering MediatR with assembly scanning
    - Always scans Host assembly
    - Accepts additional module assemblies as parameters
    - Placeholder for pipeline behaviors (v0.0.7c, v0.0.7d)

---

## Files Added

| File                                                                    | Description                                    |
| :---------------------------------------------------------------------- | :--------------------------------------------- |
| `src/Lexichord.Abstractions/Messaging/ICommand.cs`                      | Command marker interfaces                      |
| `src/Lexichord.Abstractions/Messaging/IQuery.cs`                        | Query marker interface                         |
| `src/Lexichord.Abstractions/Messaging/IDomainEvent.cs`                  | Domain event interface with tracing properties |
| `src/Lexichord.Host/Infrastructure/MediatRServiceExtensions.cs`         | DI registration extension methods              |
| `tests/Lexichord.Tests.Unit/Host/Messaging/MediatRRegistrationTests.cs` | DI registration unit tests                     |
| `tests/Lexichord.Tests.Unit/Host/Messaging/MessagingInterfaceTests.cs`  | Interface contract unit tests                  |

## Files Modified

| File                                                     | Description                              |
| :------------------------------------------------------- | :--------------------------------------- |
| `Directory.Build.props`                                  | Added `MediatRVersion` property (12.4.0) |
| `src/Lexichord.Host/Lexichord.Host.csproj`               | Added MediatR package reference          |
| `src/Lexichord.Host/HostServices.cs`                     | Added `AddMediatRServices()` call        |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj` | Added MediatR package reference          |

---

## Usage

### Send a Command

```csharp
public record CreateDocumentCommand : ICommand<DocumentId>
{
    public string Title { get; init; }
}

public class CreateDocumentHandler : IRequestHandler<CreateDocumentCommand, DocumentId>
{
    public async Task<DocumentId> Handle(CreateDocumentCommand request, CancellationToken ct)
    {
        // Create document logic
        return new DocumentId(Guid.NewGuid());
    }
}

// Usage
var result = await mediator.Send(new CreateDocumentCommand { Title = "My Doc" });
```

### Send a Query

```csharp
public record GetDocumentQuery : IQuery<DocumentDto>
{
    public DocumentId Id { get; init; }
}

// Usage
var document = await mediator.Send(new GetDocumentQuery { Id = docId });
```

### Publish an Event

```csharp
public record DocumentCreatedEvent : IDomainEvent
{
    public Guid EventId { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
    public string? CorrelationId { get; init; }
    public DocumentId DocumentId { get; init; }
}

// Usage
await mediator.Publish(new DocumentCreatedEvent
{
    EventId = Guid.NewGuid(),
    OccurredAt = DateTimeOffset.UtcNow,
    DocumentId = docId
});
```

---

## Verification Commands

```bash
# 1. Build solution
dotnet build

# 2. Run MediatR unit tests
dotnet test tests/Lexichord.Tests.Unit --filter "FullyQualifiedName~Messaging"

# 3. Verify MediatR package installed
dotnet list src/Lexichord.Host package | grep MediatR
```

---

## Test Summary

| Test Class               | Tests  | Status |
| :----------------------- | :----- | :----- |
| MediatRRegistrationTests | 5      | ✅     |
| MessagingInterfaceTests  | 10     | ✅     |
| **Total**                | **15** | **✅** |

---

## Dependencies

- **From v0.0.3a:** DI Container (for service registration)
- **From v0.0.4:** Module System (for module assembly scanning)
- **NuGet:** MediatR 12.4.0, MediatR.Contracts 2.0.1

## Enables

- **v0.0.7b:** Handler Base Classes
- **v0.0.7c:** Logging Behavior
- **v0.0.7d:** Validation Behavior
