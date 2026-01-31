# LCS-DES-004a: The Contract (IModule Interface)

## 1. Metadata & Categorization

| Field                | Value                                    | Description                                           |
| :------------------- | :--------------------------------------- | :---------------------------------------------------- |
| **Feature ID**       | `INF-004a`                               | Infrastructure - Module Contract Definition           |
| **Feature Name**     | The Contract (IModule Interface)         | Defines the module lifecycle contract                 |
| **Target Version**   | `v0.0.4a`                                | First sub-part of v0.0.4                              |
| **Module Scope**     | `Lexichord.Abstractions`                 | Shared contracts library                              |
| **Swimlane**         | `Infrastructure`                         | The Podium (Platform)                                 |
| **License Tier**     | `Core`                                   | Foundation (Required for all tiers)                   |
| **Author**           | System Architect                         |                                                       |
| **Status**           | **Draft**                                | Pending implementation                                |
| **Last Updated**     | 2026-01-26                               |                                                       |

---

## 2. Executive Summary

### 2.1 The Requirement

Lexichord's modular monolith architecture requires a standardized contract that all feature modules must implement. Without this contract:

- Modules cannot be discovered by the Host.
- The module lifecycle cannot be managed consistently.
- Service registration timing becomes unpredictable.
- Module metadata (version, author, dependencies) is unavailable.

### 2.2 The Proposed Solution

We **SHALL** define the `IModule` interface in `Lexichord.Abstractions.Contracts` that serves as the single entry point for all feature modules. This contract enforces a consistent lifecycle: discovery, registration, initialization.

---

## 3. Implementation Tasks

### Task 1.1: IModule Interface

**File:** `src/Lexichord.Abstractions/Contracts/IModule.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the contract for Lexichord feature modules.
/// </summary>
/// <remarks>
/// LOGIC: Modules are the building blocks of Lexichord's modular monolith architecture.
/// Each module is a separate assembly that:
/// - Lives in the ./Modules/ directory
/// - Implements this interface
/// - Registers its services during startup
/// - Initializes after the DI container is built
///
/// Module Loading Lifecycle:
/// 1. ModuleLoader discovers the assembly in ./Modules/
/// 2. Reflection finds the IModule implementation
/// 3. [RequiresLicense] attribute is checked against current license
/// 4. Module is instantiated (parameterless constructor)
/// 5. RegisterServices() is called with the ServiceCollection
/// 6. After all modules register, ServiceProvider is built
/// 7. InitializeAsync() is called with the ServiceProvider
///
/// Design Decisions:
/// - Parameterless constructor required for Activator.CreateInstance()
/// - RegisterServices is synchronous (no async service registration in M.E.DI)
/// - InitializeAsync allows async warm-up (cache loading, connection pooling)
/// - ModuleInfo provides metadata for logging, UI display, and dependency resolution
/// </remarks>
/// <example>
/// <code>
/// [RequiresLicense(LicenseTier.WriterPro)]
/// public class TuningModule : IModule
/// {
///     public ModuleInfo Info => new(
///         Id: "tuning",
///         Name: "Tuning Engine",
///         Version: new Version(1, 0, 0),
///         Author: "Lexichord Team",
///         Description: "Grammar and style checking engine"
///     );
///
///     public void RegisterServices(IServiceCollection services)
///     {
///         services.AddSingleton&lt;ITuningEngine, TuningEngine&gt;();
///         services.AddScoped&lt;ILinterService, LinterService&gt;();
///     }
///
///     public async Task InitializeAsync(IServiceProvider provider)
///     {
///         var logger = provider.GetRequiredService&lt;ILogger&lt;TuningModule&gt;&gt;();
///         logger.LogInformation("Tuning module initialized");
///
///         // Load dictionaries, warm up caches, etc.
///         var engine = provider.GetRequiredService&lt;ITuningEngine&gt;();
///         await engine.WarmUpAsync();
///     }
/// }
/// </code>
/// </example>
public interface IModule
{
    /// <summary>
    /// Gets the module metadata.
    /// </summary>
    /// <remarks>
    /// LOGIC: ModuleInfo provides essential metadata for:
    /// - Logging (module name, version in log entries)
    /// - UI display (showing loaded modules to user)
    /// - Dependency resolution (future: load order based on Dependencies)
    /// - Diagnostics (which version is loaded, who authored it)
    /// </remarks>
    ModuleInfo Info { get; }

    /// <summary>
    /// Registers module services in the DI container.
    /// </summary>
    /// <param name="services">The service collection to register services with.</param>
    /// <remarks>
    /// LOGIC: This method is called BEFORE the ServiceProvider is built.
    /// - Register services using the standard DI patterns
    /// - Do NOT resolve services here (ServiceProvider doesn't exist yet)
    /// - Do NOT perform I/O or async operations (use InitializeAsync instead)
    /// - Keep this method fast; it blocks application startup
    ///
    /// Registration Guidelines:
    /// - Singleton: Stateless services, caches, configuration
    /// - Scoped: Per-document, per-request services
    /// - Transient: Factories, validators, stateless utilities
    /// </remarks>
    void RegisterServices(IServiceCollection services);

    /// <summary>
    /// Initializes the module after the DI container is built.
    /// </summary>
    /// <param name="provider">The service provider for resolving dependencies.</param>
    /// <returns>A task representing the async initialization.</returns>
    /// <remarks>
    /// LOGIC: This method is called AFTER the ServiceProvider is built.
    /// - Resolve services from the provider
    /// - Perform async initialization (load data, warm caches)
    /// - Subscribe to events if needed
    /// - Failures here are logged but don't crash the application
    ///
    /// Common initialization tasks:
    /// - Loading configuration from files
    /// - Warming up caches with frequently-used data
    /// - Establishing connections (database, external APIs)
    /// - Subscribing to MediatR events
    /// </remarks>
    Task InitializeAsync(IServiceProvider provider);
}
```

---

### Task 1.2: ModuleInfo Record

**File:** `src/Lexichord.Abstractions/Contracts/ModuleInfo.cs`

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Contains metadata about a module.
/// </summary>
/// <remarks>
/// LOGIC: ModuleInfo is a record for immutability and value equality.
/// This metadata is used throughout the application for:
/// - Logging: "Loading module: {Name} v{Version} by {Author}"
/// - UI: Displaying loaded modules in settings/about screen
/// - Diagnostics: Troubleshooting which module versions are active
/// - Future: Dependency graph resolution based on Dependencies list
///
/// Design Decisions:
/// - Record type for immutability (metadata shouldn't change at runtime)
/// - Id uses lowercase, no spaces (machine-readable identifier)
/// - Name is human-readable (displayed in UI)
/// - Version uses System.Version for semantic versioning
/// - Dependencies is optional (most modules are independent)
/// </remarks>
/// <param name="Id">Unique identifier for the module (lowercase, no spaces). Example: "tuning", "memory", "agents"</param>
/// <param name="Name">Human-readable display name. Example: "Tuning Engine", "Vector Memory"</param>
/// <param name="Version">Semantic version of the module.</param>
/// <param name="Author">Module author or team name.</param>
/// <param name="Description">Brief description of module functionality.</param>
/// <param name="Dependencies">List of module IDs this module depends on. Used for load ordering.</param>
/// <example>
/// <code>
/// // Simple module without dependencies
/// var info = new ModuleInfo(
///     Id: "sandbox",
///     Name: "Sandbox Module",
///     Version: new Version(0, 0, 1),
///     Author: "Lexichord Team",
///     Description: "Test module for architecture validation"
/// );
///
/// // Module with dependencies
/// var info = new ModuleInfo(
///     Id: "agents",
///     Name: "AI Agents Ensemble",
///     Version: new Version(1, 0, 0),
///     Author: "Lexichord Team",
///     Description: "AI-powered writing agents",
///     Dependencies: ["memory", "llm-providers"]
/// );
/// </code>
/// </example>
public record ModuleInfo(
    string Id,
    string Name,
    Version Version,
    string Author,
    string Description,
    IReadOnlyList<string>? Dependencies = null
)
{
    /// <summary>
    /// Gets the module dependencies, or empty list if none.
    /// </summary>
    /// <remarks>
    /// LOGIC: Ensures Dependencies is never null for easier consumption.
    /// Callers can always iterate without null checks.
    /// </remarks>
    public IReadOnlyList<string> Dependencies { get; init; } = Dependencies ?? [];

    /// <summary>
    /// Returns a formatted string representation of the module info.
    /// </summary>
    /// <returns>A string in the format "Name vVersion by Author".</returns>
    public override string ToString() => $"{Name} v{Version} by {Author}";
}
```

---

### Task 1.3: Module Lifecycle Events

**File:** `src/Lexichord.Abstractions/Events/ModuleEvents.cs`

```csharp
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a module is successfully loaded.
/// </summary>
/// <remarks>
/// LOGIC: This event is published after a module's InitializeAsync() completes successfully.
/// Subscribers can use this for:
/// - Logging module load events centrally
/// - Updating UI to show module status
/// - Triggering post-load actions (e.g., module-specific migrations)
///
/// Note: This event will be published via MediatR in v0.0.7.
/// For now, the event record is defined for future use.
/// </remarks>
/// <param name="ModuleInfo">The loaded module's metadata.</param>
/// <param name="LoadDuration">Time taken to load and initialize the module.</param>
public record ModuleLoadedEvent(
    ModuleInfo ModuleInfo,
    TimeSpan LoadDuration
) : INotification;

/// <summary>
/// Published when a module fails to load.
/// </summary>
/// <remarks>
/// LOGIC: This event is published when module loading fails for any reason:
/// - Assembly not found
/// - IModule type not found
/// - License check failed
/// - RegisterServices threw exception
/// - InitializeAsync threw exception
///
/// Subscribers can use this for:
/// - Alerting users about missing features
/// - Logging failures for diagnostics
/// - Triggering fallback behavior
/// </remarks>
/// <param name="AssemblyPath">Path to the failed assembly.</param>
/// <param name="ModuleName">Name of the module, if known.</param>
/// <param name="FailureReason">Human-readable reason for failure.</param>
/// <param name="Exception">The exception that caused the failure, if any.</param>
public record ModuleLoadFailedEvent(
    string AssemblyPath,
    string? ModuleName,
    string FailureReason,
    Exception? Exception
) : INotification;

/// <summary>
/// Published when a module is unloaded (future use).
/// </summary>
/// <remarks>
/// LOGIC: Reserved for future hot-reload functionality.
/// In v0.0.4, modules are loaded once at startup and never unloaded.
/// This event is defined for API completeness and future compatibility.
/// </remarks>
/// <param name="ModuleInfo">The unloaded module's metadata.</param>
public record ModuleUnloadedEvent(
    ModuleInfo ModuleInfo
) : INotification;
```

---

### Task 1.4: Module Base Class (Optional Helper)

**File:** `src/Lexichord.Abstractions/Contracts/ModuleBase.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Base class for modules that provides common functionality.
/// </summary>
/// <remarks>
/// LOGIC: This abstract base class provides a template for module implementation.
/// It's optional - modules can implement IModule directly if preferred.
///
/// Benefits:
/// - Enforces ModuleInfo as abstract property
/// - Provides default empty InitializeAsync (many modules don't need it)
/// - Groups RegisterServices as abstract (must be implemented)
///
/// Design Decision:
/// - Not sealed to allow module inheritance chains (rare but possible)
/// - Default InitializeAsync completes immediately (no-op)
/// </remarks>
/// <example>
/// <code>
/// [RequiresLicense(LicenseTier.Core)]
/// public sealed class SandboxModule : ModuleBase
/// {
///     public override ModuleInfo Info => new(
///         Id: "sandbox",
///         Name: "Sandbox",
///         Version: new Version(0, 0, 1),
///         Author: "Lexichord Team",
///         Description: "Architecture validation module"
///     );
///
///     public override void RegisterServices(IServiceCollection services)
///     {
///         services.AddSingleton&lt;ISandboxService, SandboxService&gt;();
///     }
///
///     // InitializeAsync not overridden - uses default no-op
/// }
/// </code>
/// </example>
public abstract class ModuleBase : IModule
{
    /// <inheritdoc/>
    public abstract ModuleInfo Info { get; }

    /// <inheritdoc/>
    public abstract void RegisterServices(IServiceCollection services);

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Default implementation returns completed task.
    /// Override in derived modules that need async initialization.
    /// </remarks>
    public virtual Task InitializeAsync(IServiceProvider provider) => Task.CompletedTask;
}
```

---

## 4. Decision Tree: Module Contract Usage

```text
START: "How do I create a new module?"
│
├── Create a new Class Library project
│   └── Name: Lexichord.Modules.{ModuleName}
│       Example: Lexichord.Modules.Tuning
│
├── Reference only Lexichord.Abstractions
│   └── NEVER reference Lexichord.Host
│       NEVER reference other Modules
│
├── Configure output path to ./Modules/
│   └── Add to .csproj:
│       <OutputPath>$(SolutionDir)Modules/</OutputPath>
│
├── Create class implementing IModule
│   ├── Option A: Implement IModule directly
│   │   └── Must implement: Info, RegisterServices, InitializeAsync
│   │
│   └── Option B: Inherit from ModuleBase
│       └── Must implement: Info, RegisterServices
│           (InitializeAsync optional)
│
├── Add [RequiresLicense] attribute if not Core tier
│   ├── Core: No attribute needed (default)
│   ├── WriterPro: [RequiresLicense(LicenseTier.WriterPro)]
│   ├── Teams: [RequiresLicense(LicenseTier.Teams)]
│   └── Enterprise: [RequiresLicense(LicenseTier.Enterprise)]
│
└── Register services in RegisterServices()
    ├── Singleton: Stateless, cached, shared
    ├── Scoped: Per-document, per-request
    └── Transient: Factories, validators
```

---

## 5. Unit Testing Requirements

### 5.1 Test: ModuleInfo Validation

```csharp
[TestFixture]
[Category("Unit")]
public class ModuleInfoTests
{
    [Test]
    public void ModuleInfo_WithAllFields_CreatesValidRecord()
    {
        // Arrange & Act
        var info = new ModuleInfo(
            Id: "test-module",
            Name: "Test Module",
            Version: new Version(1, 2, 3),
            Author: "Test Author",
            Description: "A test module for unit testing"
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Id, Is.EqualTo("test-module"));
            Assert.That(info.Name, Is.EqualTo("Test Module"));
            Assert.That(info.Version, Is.EqualTo(new Version(1, 2, 3)));
            Assert.That(info.Author, Is.EqualTo("Test Author"));
            Assert.That(info.Description, Is.EqualTo("A test module for unit testing"));
            Assert.That(info.Dependencies, Is.Empty);
        });
    }

    [Test]
    public void ModuleInfo_WithDependencies_StoresDependencyList()
    {
        // Arrange & Act
        var info = new ModuleInfo(
            Id: "dependent-module",
            Name: "Dependent Module",
            Version: new Version(1, 0, 0),
            Author: "Test Author",
            Description: "Module with dependencies",
            Dependencies: ["core-module", "utility-module"]
        );

        // Assert
        Assert.That(info.Dependencies, Has.Count.EqualTo(2));
        Assert.That(info.Dependencies, Contains.Item("core-module"));
        Assert.That(info.Dependencies, Contains.Item("utility-module"));
    }

    [Test]
    public void ModuleInfo_NullDependencies_ReturnsEmptyList()
    {
        // Arrange & Act
        var info = new ModuleInfo(
            Id: "simple-module",
            Name: "Simple Module",
            Version: new Version(1, 0, 0),
            Author: "Test Author",
            Description: "Module without dependencies",
            Dependencies: null
        );

        // Assert
        Assert.That(info.Dependencies, Is.Not.Null);
        Assert.That(info.Dependencies, Is.Empty);
    }

    [Test]
    public void ModuleInfo_ToString_ReturnsFormattedString()
    {
        // Arrange
        var info = new ModuleInfo(
            Id: "test",
            Name: "Test Module",
            Version: new Version(2, 1, 0),
            Author: "Lexichord Team",
            Description: "Test"
        );

        // Act
        var result = info.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("Test Module v2.1.0 by Lexichord Team"));
    }

    [Test]
    public void ModuleInfo_RecordEquality_WorksCorrectly()
    {
        // Arrange
        var info1 = new ModuleInfo("test", "Test", new Version(1, 0, 0), "Author", "Desc");
        var info2 = new ModuleInfo("test", "Test", new Version(1, 0, 0), "Author", "Desc");
        var info3 = new ModuleInfo("other", "Test", new Version(1, 0, 0), "Author", "Desc");

        // Assert
        Assert.That(info1, Is.EqualTo(info2));
        Assert.That(info1, Is.Not.EqualTo(info3));
    }
}
```

### 5.2 Test: ModuleBase Default Behavior

```csharp
[TestFixture]
[Category("Unit")]
public class ModuleBaseTests
{
    private sealed class TestModule : ModuleBase
    {
        public override ModuleInfo Info => new(
            Id: "test",
            Name: "Test",
            Version: new Version(1, 0, 0),
            Author: "Test",
            Description: "Test module"
        );

        public bool RegisterServicesCalled { get; private set; }

        public override void RegisterServices(IServiceCollection services)
        {
            RegisterServicesCalled = true;
        }
    }

    [Test]
    public void ModuleBase_InitializeAsync_ReturnsCompletedTask()
    {
        // Arrange
        var sut = new TestModule();

        // Act
        var result = sut.InitializeAsync(Mock.Of<IServiceProvider>());

        // Assert
        Assert.That(result.IsCompleted, Is.True);
    }

    [Test]
    public void ModuleBase_ImplementsIModule()
    {
        // Arrange
        var sut = new TestModule();

        // Assert
        Assert.That(sut, Is.InstanceOf<IModule>());
    }

    [Test]
    public void ModuleBase_Info_ReturnsModuleInfo()
    {
        // Arrange
        var sut = new TestModule();

        // Act
        var info = sut.Info;

        // Assert
        Assert.That(info, Is.Not.Null);
        Assert.That(info.Id, Is.EqualTo("test"));
    }
}
```

### 5.3 Test: IModule Interface Contract

```csharp
[TestFixture]
[Category("Unit")]
public class IModuleContractTests
{
    [Test]
    public void IModule_Interface_DefinesRequiredMembers()
    {
        // Arrange
        var interfaceType = typeof(IModule);

        // Act
        var properties = interfaceType.GetProperties();
        var methods = interfaceType.GetMethods();

        // Assert - Info property exists
        Assert.That(properties.Any(p => p.Name == "Info" && p.PropertyType == typeof(ModuleInfo)),
            Is.True, "IModule must define Info property of type ModuleInfo");

        // Assert - RegisterServices method exists
        Assert.That(methods.Any(m => m.Name == "RegisterServices" &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(IServiceCollection)),
            Is.True, "IModule must define RegisterServices(IServiceCollection)");

        // Assert - InitializeAsync method exists
        Assert.That(methods.Any(m => m.Name == "InitializeAsync" &&
            m.ReturnType == typeof(Task) &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(IServiceProvider)),
            Is.True, "IModule must define InitializeAsync(IServiceProvider)");
    }
}
```

---

## 6. Observability & Logging

| Level       | Context        | Message Template                                              |
| :---------- | :------------- | :------------------------------------------------------------ |
| Information | Module         | `Module {ModuleName} services registered`                     |
| Information | Module         | `Module {ModuleName} initialization started`                  |
| Information | Module         | `Module {ModuleName} initialized in {Duration}ms`             |
| Warning     | Module         | `Module {ModuleName} initialization failed: {Error}`          |
| Debug       | Module         | `Module {ModuleName} registering {ServiceCount} services`     |

---

## 7. Definition of Done

- [ ] `IModule` interface exists in `Lexichord.Abstractions.Contracts`
- [ ] `IModule.Info` property returns `ModuleInfo`
- [ ] `IModule.RegisterServices(IServiceCollection)` method defined
- [ ] `IModule.InitializeAsync(IServiceProvider)` method defined
- [ ] `ModuleInfo` record exists with Id, Name, Version, Author, Description, Dependencies
- [ ] `ModuleInfo.Dependencies` defaults to empty list if null
- [ ] `ModuleInfo.ToString()` returns formatted string
- [ ] `ModuleBase` abstract class provides default InitializeAsync
- [ ] `ModuleLoadedEvent` defined for MediatR (future use)
- [ ] `ModuleLoadFailedEvent` defined for MediatR (future use)
- [ ] `ModuleUnloadedEvent` defined for MediatR (future use)
- [ ] XML documentation complete for all public members
- [ ] LOGIC comments explain design decisions
- [ ] Unit tests for ModuleInfo passing
- [ ] Unit tests for ModuleBase passing
- [ ] Unit tests for IModule contract passing
- [ ] No references to Lexichord.Host in Abstractions

---

## 8. Verification Commands

```bash
# Build Abstractions to verify compilation
dotnet build src/Lexichord.Abstractions

# Verify IModule interface exists
grep -r "public interface IModule" src/Lexichord.Abstractions/

# Verify ModuleInfo record exists
grep -r "public record ModuleInfo" src/Lexichord.Abstractions/

# Run unit tests
dotnet test --filter "FullyQualifiedName~ModuleInfoTests"
dotnet test --filter "FullyQualifiedName~ModuleBaseTests"
dotnet test --filter "FullyQualifiedName~IModuleContractTests"

# Verify no Host reference in Abstractions
dotnet list src/Lexichord.Abstractions reference
# Should show NO references or only external packages
```
