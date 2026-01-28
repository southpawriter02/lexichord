# LCS-CL-004a: IModule Interface

## Version Information

| Field        | Value        |
| :----------- | :----------- |
| Version      | v0.0.4a      |
| Feature Name | The Contract |
| Release Date | 2026-01-28   |
| Status       | ✅ Complete  |

---

## Summary

Defined the core module contract (`IModule` interface), metadata record (`ModuleInfo`),
abstract base class (`ModuleBase`), and MediatR lifecycle events for Lexichord's
modular monolith architecture.

---

## What's New

### Module Contract

- **`IModule` Interface** — Core contract for all Lexichord feature modules:
    - `Info` property for module metadata
    - `RegisterServices(IServiceCollection)` for DI registration
    - `InitializeAsync(IServiceProvider)` for post-DI initialization

- **`ModuleInfo` Record** — Immutable metadata containing:
    - Id, Name, Version, Author, Description
    - Optional `Dependencies` list (null-safe, defaults to empty)
    - Formatted `ToString()` output

- **`ModuleBase` Abstract Class** — Optional helper:
    - Default no-op `InitializeAsync`
    - Enforces `Info` and `RegisterServices` implementation

### Lifecycle Events (MediatR)

| Event                   | Purpose                        |
| :---------------------- | :----------------------------- |
| `ModuleLoadedEvent`     | Published on successful load   |
| `ModuleLoadFailedEvent` | Published on load failure      |
| `ModuleUnloadedEvent`   | Reserved for future hot-reload |

---

## Files Created

| File                                                              | Purpose             |
| :---------------------------------------------------------------- | :------------------ |
| `src/Lexichord.Abstractions/Contracts/IModule.cs`                 | Core interface      |
| `src/Lexichord.Abstractions/Contracts/ModuleInfo.cs`              | Metadata record     |
| `src/Lexichord.Abstractions/Contracts/ModuleBase.cs`              | Optional base class |
| `src/Lexichord.Abstractions/Events/ModuleEvents.cs`               | Lifecycle events    |
| `tests/Lexichord.Tests.Unit/Abstractions/ModuleInfoTests.cs`      | 5 unit tests        |
| `tests/Lexichord.Tests.Unit/Abstractions/ModuleBaseTests.cs`      | 3 unit tests        |
| `tests/Lexichord.Tests.Unit/Abstractions/IModuleContractTests.cs` | 1 contract test     |

---

## Files Modified

| File                                                       | Change                                         |
| :--------------------------------------------------------- | :--------------------------------------------- |
| `src/Lexichord.Abstractions/Lexichord.Abstractions.csproj` | Added DI.Abstractions, MediatR.Contracts       |
| `tests/Lexichord.Tests.Unit/Lexichord.Tests.Unit.csproj`   | Added Microsoft.Extensions.DependencyInjection |

---

## NuGet Packages Added

```xml
<!-- Lexichord.Abstractions -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
<PackageReference Include="MediatR.Contracts" Version="2.0.1" />

<!-- Lexichord.Tests.Unit -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
```

---

## Testing

**9 unit tests** covering:

- ModuleInfo record creation, dependencies, null handling, ToString, equality
- ModuleBase default InitializeAsync, IModule implementation, Info property
- IModule interface shape validation using reflection

---

## Usage Example

```csharp
public sealed class MyModule : ModuleBase
{
    public override ModuleInfo Info => new(
        Id: "my-module",
        Name: "My Custom Module",
        Version: new Version(1, 0, 0),
        Author: "Author Name",
        Description: "Custom functionality"
    );

    public override void RegisterServices(IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
    }
}
```

---

## Related Documents

- **Design Specification**: [LCS-DES-004a.md](../specs/v0.0.x/v0.0.4/LCS-DES-004a.md)
- **Parent Version**: [LCS-DES-004-INDEX.md](../specs/v0.0.x/v0.0.4/LCS-DES-004-INDEX.md)
- **Scope Breakdown**: [LCS-SBD-004.md](../specs/v0.0.x/v0.0.4/LCS-SBD-004.md)
