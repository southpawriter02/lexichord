# LCS-DES-004d: The Sandbox Module (Proof of Concept)

## 1. Metadata & Categorization

| Field                | Value                                    | Description                                           |
| :------------------- | :--------------------------------------- | :---------------------------------------------------- |
| **Feature ID**       | `INF-004d`                               | Infrastructure - Sandbox Module Implementation        |
| **Feature Name**     | The Sandbox Module (Proof of Concept)    | Test module proving the module architecture works     |
| **Target Version**   | `v0.0.4d`                                | Fourth sub-part of v0.0.4                             |
| **Module Scope**     | `Lexichord.Modules.Sandbox`              | First feature module project                          |
| **Swimlane**         | `Infrastructure`                         | The Podium (Platform)                                 |
| **License Tier**     | `Core`                                   | Foundation (accessible to all users)                  |
| **Author**           | System Architect                         |                                                       |
| **Status**           | **Draft**                                | Pending implementation                                |
| **Last Updated**     | 2026-01-26                               |                                                       |

---

## 2. Executive Summary

### 2.1 The Requirement

The module loading architecture requires validation:

- Does ModuleLoader discover DLLs correctly?
- Does reflection find IModule implementations?
- Does RegisterServices register services in the Host's DI container?
- Does InitializeAsync execute with access to the full service provider?
- Does the architecture enforce the "Modules only reference Abstractions" rule?

### 2.2 The Proposed Solution

We **SHALL** create `Lexichord.Modules.Sandbox` as the first proof-of-concept module:

1. A complete `IModule` implementation demonstrating the lifecycle.
2. A simple `ISandboxService` that can be resolved from the Host.
3. Project configuration that outputs to the `./Modules/` directory.
4. Architecture tests verifying the module has no Host reference.

This module serves as both validation and a template for future modules.

---

## 3. Implementation Tasks

### Task 1.1: Create Sandbox Project

**File:** `src/Lexichord.Modules.Sandbox/Lexichord.Modules.Sandbox.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>Lexichord.Modules.Sandbox</RootNamespace>

    <!-- LOGIC: Module metadata for assembly info -->
    <AssemblyTitle>Lexichord Sandbox Module</AssemblyTitle>
    <Description>Architecture validation and proof-of-concept module</Description>
    <Authors>Lexichord Team</Authors>

    <!-- LOGIC: Output to Modules directory so ModuleLoader discovers it -->
    <!-- This path is relative to the project directory -->
    <OutputPath>..\..\Modules\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
    <AppendRuntimeIdentifierToOutputPath>false</AppendRuntimeIdentifierToOutputPath>

    <!-- LOGIC: Treat warnings as errors for module quality -->
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <WarningLevel>9999</WarningLevel>
  </PropertyGroup>

  <ItemGroup>
    <!-- LOGIC: Modules ONLY reference Abstractions, NEVER Host -->
    <!-- This is enforced by architecture tests -->
    <ProjectReference Include="..\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />
  </ItemGroup>

  <ItemGroup>
    <!-- LOGIC: Required for IServiceCollection in RegisterServices -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />

    <!-- LOGIC: Required for ILogger<T> in services -->
    <PackageReference Include="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>

</Project>
```

---

### Task 1.2: ISandboxService Interface

**File:** `src/Lexichord.Modules.Sandbox/Contracts/ISandboxService.cs`

```csharp
namespace Lexichord.Modules.Sandbox.Contracts;

/// <summary>
/// Simple service interface demonstrating module service registration.
/// </summary>
/// <remarks>
/// LOGIC: This interface exists to prove that modules can:
/// 1. Define their own service interfaces
/// 2. Register implementations in the Host's DI container
/// 3. Have those services resolved by other parts of the application
///
/// In a real module, this would be a meaningful service interface
/// (e.g., ITuningEngine, IMemoryService, IAgentOrchestrator).
/// </remarks>
public interface ISandboxService
{
    /// <summary>
    /// Gets the module name as reported by the service.
    /// </summary>
    /// <returns>The module name string.</returns>
    /// <remarks>
    /// LOGIC: Simple method to verify the service was resolved correctly.
    /// Returns a value that proves the Sandbox module's service is active.
    /// </remarks>
    string GetModuleName();

    /// <summary>
    /// Performs a simple operation to verify the service is functional.
    /// </summary>
    /// <param name="input">Input string to echo.</param>
    /// <returns>The input with module signature appended.</returns>
    /// <remarks>
    /// LOGIC: Demonstrates the service can perform actual work.
    /// Used in integration tests to verify end-to-end functionality.
    /// </remarks>
    string Echo(string input);

    /// <summary>
    /// Gets the timestamp when the module was initialized.
    /// </summary>
    /// <returns>The initialization timestamp.</returns>
    /// <remarks>
    /// LOGIC: Proves InitializeAsync was called and the service
    /// has access to state set during initialization.
    /// </remarks>
    DateTime GetInitializationTime();
}
```

---

### Task 1.3: SandboxService Implementation

**File:** `src/Lexichord.Modules.Sandbox/Services/SandboxService.cs`

```csharp
using Lexichord.Modules.Sandbox.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Sandbox.Services;

/// <summary>
/// Implementation of ISandboxService for architecture validation.
/// </summary>
/// <remarks>
/// LOGIC: This service demonstrates:
/// - Constructor injection works for module services
/// - ILogger<T> is resolvable (proves DI integration)
/// - Service state persists between calls (singleton behavior)
///
/// Design Pattern:
/// - Uses primary constructor for dependency injection
/// - Implements ISandboxService interface
/// - Registered as Singleton in SandboxModule.RegisterServices
/// </remarks>
public sealed class SandboxService(ILogger<SandboxService> logger) : ISandboxService
{
    private DateTime _initializationTime = DateTime.MinValue;

    /// <summary>
    /// Sets the initialization time (called by SandboxModule.InitializeAsync).
    /// </summary>
    /// <param name="time">The initialization timestamp.</param>
    /// <remarks>
    /// LOGIC: This method is called by the module during InitializeAsync
    /// to prove that initialization occurs after service resolution is available.
    /// Internal visibility keeps it accessible to the module but not external callers.
    /// </remarks>
    internal void SetInitializationTime(DateTime time)
    {
        _initializationTime = time;
        logger.LogDebug("Sandbox service initialization time set to {Time}", time);
    }

    /// <inheritdoc/>
    public string GetModuleName()
    {
        logger.LogDebug("GetModuleName called");
        return "Lexichord.Modules.Sandbox";
    }

    /// <inheritdoc/>
    public string Echo(string input)
    {
        logger.LogDebug("Echo called with input: {Input}", input);
        return $"[Sandbox] {input}";
    }

    /// <inheritdoc/>
    public DateTime GetInitializationTime()
    {
        logger.LogDebug("GetInitializationTime called, returning {Time}", _initializationTime);
        return _initializationTime;
    }
}
```

---

### Task 1.4: SandboxModule Implementation

**File:** `src/Lexichord.Modules.Sandbox/SandboxModule.cs`

```csharp
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Sandbox.Contracts;
using Lexichord.Modules.Sandbox.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Sandbox;

/// <summary>
/// Proof-of-concept module demonstrating the Lexichord module architecture.
/// </summary>
/// <remarks>
/// LOGIC: This module serves multiple purposes:
///
/// 1. Architecture Validation:
///    - Proves ModuleLoader discovers modules in ./Modules/
///    - Proves reflection finds IModule implementations
///    - Proves RegisterServices registers services in Host DI
///    - Proves InitializeAsync runs after DI container is built
///
/// 2. Template for Future Modules:
///    - Shows proper IModule implementation pattern
///    - Demonstrates service registration
///    - Shows async initialization pattern
///    - Documents the module lifecycle
///
/// 3. Development Testing:
///    - Provides a "canary" to detect module loading issues
///    - Logging messages confirm each lifecycle phase
///    - ISandboxService can be resolved to verify DI integration
///
/// License:
/// - No [RequiresLicense] attribute means Core tier (always loaded)
/// - All users get this module regardless of subscription
/// </remarks>
public sealed class SandboxModule : IModule
{
    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: ModuleInfo provides metadata for:
    /// - Startup logs: "Loading module: Sandbox v0.0.1 by Lexichord Team"
    /// - Diagnostics UI: Showing loaded modules
    /// - Dependency resolution: Id used for dependency references
    /// </remarks>
    public ModuleInfo Info => new(
        Id: "sandbox",
        Name: "Sandbox Module",
        Version: new Version(0, 0, 1),
        Author: "Lexichord Team",
        Description: "Architecture validation and proof-of-concept module for the Lexichord module system"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This method is called during Phase 1 of module loading:
    /// - BEFORE ServiceProvider is built
    /// - Cannot resolve services here (no IServiceProvider yet)
    /// - Should be fast (blocks application startup)
    /// - Register services with appropriate lifetimes
    ///
    /// Service Registration:
    /// - ISandboxService as Singleton: State shared across application
    /// - Uses factory to get concrete type for internal method access
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Register as Singleton so state persists
        // Using AddSingleton<TInterface, TImplementation> pattern
        services.AddSingleton<ISandboxService, SandboxService>();

        // LOGIC: Also register the concrete type for internal access
        // This allows InitializeAsync to get the concrete type
        services.AddSingleton<SandboxService>(provider =>
            (SandboxService)provider.GetRequiredService<ISandboxService>());
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This method is called during Phase 2 of module loading:
    /// - AFTER ServiceProvider is built
    /// - CAN resolve any registered service
    /// - Used for async initialization (cache warming, connections)
    /// - Failures are logged but don't crash the application
    ///
    /// Initialization Tasks:
    /// 1. Get logger for this module
    /// 2. Get the sandbox service we registered
    /// 3. Set initialization time to prove lifecycle works
    /// 4. Log success message
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        // LOGIC: Get logger for module-level logging
        var logger = provider.GetRequiredService<ILogger<SandboxModule>>();
        logger.LogDebug("Sandbox module InitializeAsync starting");

        // LOGIC: Resolve our registered service to prove DI works
        var service = provider.GetRequiredService<SandboxService>();

        // LOGIC: Set initialization time via internal method
        // This proves:
        // 1. We can resolve services we registered
        // 2. InitializeAsync has access to the built provider
        // 3. Service state persists after initialization
        service.SetInitializationTime(DateTime.UtcNow);

        // LOGIC: Simulate async initialization work
        // Real modules might load dictionaries, warm caches, etc.
        await Task.Delay(10); // Minimal delay to prove async works

        logger.LogInformation(
            "Sandbox module initialized successfully. " +
            "ISandboxService is ready for resolution.");
    }
}
```

---

### Task 1.5: Solution File Update

Add the Sandbox project to the solution:

```bash
dotnet sln add src/Lexichord.Modules.Sandbox/Lexichord.Modules.Sandbox.csproj
```

---

### Task 1.6: Build Configuration

**Updates to `Directory.Build.props`** (ensure modules use consistent versions):

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <!-- LOGIC: Centralized package versions for all projects -->
  <ItemGroup>
    <PackageReference Update="Microsoft.Extensions.DependencyInjection" Version="9.0.0" />
    <PackageReference Update="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0" />
    <PackageReference Update="Microsoft.Extensions.Logging.Abstractions" Version="9.0.0" />
  </ItemGroup>
</Project>
```

---

## 4. Decision Tree: Creating a New Module

```text
START: "How do I create a new feature module?"
│
├── Step 1: Create Project
│   └── dotnet new classlib -n Lexichord.Modules.{Name}
│       Example: Lexichord.Modules.Tuning
│
├── Step 2: Configure .csproj
│   │
│   ├── Set OutputPath to ../../Modules/
│   │   <OutputPath>..\..\Modules\</OutputPath>
│   │   <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
│   │
│   ├── Add ProjectReference to Abstractions ONLY
│   │   <ProjectReference Include="..\Lexichord.Abstractions\..." />
│   │
│   ├── NEVER add reference to Lexichord.Host
│   │   (Architecture tests will fail)
│   │
│   └── Add required package references
│       - Microsoft.Extensions.DependencyInjection.Abstractions
│       - Microsoft.Extensions.Logging.Abstractions
│
├── Step 3: Create Module Class
│   │
│   ├── Implement IModule interface
│   │   public class {Name}Module : IModule
│   │
│   ├── Define ModuleInfo property
│   │   - Id: lowercase, no spaces
│   │   - Name: human-readable
│   │   - Version: semantic version
│   │   - Author, Description
│   │
│   ├── Add [RequiresLicense] if not Core tier
│   │   [RequiresLicense(LicenseTier.WriterPro)]
│   │
│   ├── Implement RegisterServices()
│   │   - Register your services with IServiceCollection
│   │   - Use appropriate lifetimes (Singleton/Scoped/Transient)
│   │
│   └── Implement InitializeAsync()
│       - Resolve services if needed
│       - Perform async initialization
│       - Log completion
│
├── Step 4: Create Service Interfaces
│   └── Define in Contracts/ folder
│       Example: ISandboxService.cs
│
├── Step 5: Create Service Implementations
│   └── Define in Services/ folder
│       Example: SandboxService.cs
│
├── Step 6: Add to Solution
│   └── dotnet sln add src/Lexichord.Modules.{Name}
│
├── Step 7: Build and Verify
│   │
│   ├── dotnet build src/Lexichord.Modules.{Name}
│   │
│   ├── Verify DLL appears in ./Modules/
│   │   ls Modules/Lexichord.Modules.{Name}.dll
│   │
│   └── Run Host and check logs
│       "Loading module: {Name} v1.0.0 by {Author}"
│
└── Step 8: Run Architecture Tests
    └── dotnet test --filter "Category=Architecture"
        Verify: "Modules_ShouldNotReference_Host"
```

---

## 5. Unit Testing Requirements

### 5.1 Test: SandboxModule Implementation

```csharp
[TestFixture]
[Category("Unit")]
public class SandboxModuleTests
{
    [Test]
    public void Info_ReturnsCorrectMetadata()
    {
        // Arrange
        var sut = new SandboxModule();

        // Act
        var info = sut.Info;

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.Id, Is.EqualTo("sandbox"));
            Assert.That(info.Name, Is.EqualTo("Sandbox Module"));
            Assert.That(info.Version, Is.EqualTo(new Version(0, 0, 1)));
            Assert.That(info.Author, Is.EqualTo("Lexichord Team"));
            Assert.That(info.Description, Does.Contain("proof-of-concept"));
            Assert.That(info.Dependencies, Is.Empty);
        });
    }

    [Test]
    public void RegisterServices_RegistersSandboxService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var sut = new SandboxModule();

        // Act
        sut.RegisterServices(services);
        var provider = services.BuildServiceProvider();
        var result = provider.GetService<ISandboxService>();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<SandboxService>());
    }

    [Test]
    public async Task InitializeAsync_SetsInitializationTime()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var sut = new SandboxModule();
        sut.RegisterServices(services);
        var provider = services.BuildServiceProvider();

        var beforeInit = DateTime.UtcNow;

        // Act
        await sut.InitializeAsync(provider);

        var afterInit = DateTime.UtcNow;
        var service = provider.GetRequiredService<ISandboxService>();
        var initTime = service.GetInitializationTime();

        // Assert
        Assert.That(initTime, Is.GreaterThanOrEqualTo(beforeInit));
        Assert.That(initTime, Is.LessThanOrEqualTo(afterInit));
    }

    [Test]
    public void SandboxModule_ImplementsIModule()
    {
        // Arrange
        var sut = new SandboxModule();

        // Assert
        Assert.That(sut, Is.InstanceOf<IModule>());
    }

    [Test]
    public void SandboxModule_HasNoRequiresLicenseAttribute()
    {
        // Arrange
        var moduleType = typeof(SandboxModule);

        // Act
        var licenseAttr = moduleType.GetCustomAttributes(
            typeof(RequiresLicenseAttribute), false);

        // Assert - No attribute means Core tier
        Assert.That(licenseAttr, Is.Empty);
    }
}
```

### 5.2 Test: SandboxService Implementation

```csharp
[TestFixture]
[Category("Unit")]
public class SandboxServiceTests
{
    private SandboxService _sut = null!;
    private Mock<ILogger<SandboxService>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<SandboxService>>();
        _sut = new SandboxService(_mockLogger.Object);
    }

    [Test]
    public void GetModuleName_ReturnsCorrectName()
    {
        // Act
        var result = _sut.GetModuleName();

        // Assert
        Assert.That(result, Is.EqualTo("Lexichord.Modules.Sandbox"));
    }

    [Test]
    public void Echo_AppendsModuleSignature()
    {
        // Arrange
        var input = "Hello, World!";

        // Act
        var result = _sut.Echo(input);

        // Assert
        Assert.That(result, Is.EqualTo("[Sandbox] Hello, World!"));
    }

    [Test]
    public void GetInitializationTime_BeforeSet_ReturnsMinValue()
    {
        // Act
        var result = _sut.GetInitializationTime();

        // Assert
        Assert.That(result, Is.EqualTo(DateTime.MinValue));
    }

    [Test]
    public void SetInitializationTime_UpdatesTime()
    {
        // Arrange
        var expectedTime = new DateTime(2026, 1, 26, 12, 0, 0, DateTimeKind.Utc);

        // Act
        _sut.SetInitializationTime(expectedTime);
        var result = _sut.GetInitializationTime();

        // Assert
        Assert.That(result, Is.EqualTo(expectedTime));
    }

    [Test]
    public void Echo_LogsDebugMessage()
    {
        // Act
        _sut.Echo("test");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Echo called")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
```

### 5.3 Test: Architecture Constraints

```csharp
[TestFixture]
[Category("Architecture")]
public class SandboxModuleArchitectureTests
{
    [Test]
    public void SandboxModule_DoesNotReference_Host()
    {
        // Arrange
        var assembly = typeof(SandboxModule).Assembly;

        // Act
        var references = assembly.GetReferencedAssemblies()
            .Select(r => r.Name)
            .ToList();

        // Assert
        Assert.That(references, Does.Not.Contain("Lexichord.Host"),
            "Sandbox module must not reference Lexichord.Host");
    }

    [Test]
    public void SandboxModule_DoesNotReference_OtherModules()
    {
        // Arrange
        var assembly = typeof(SandboxModule).Assembly;
        var assemblyName = assembly.GetName().Name;

        // Act
        var moduleReferences = assembly.GetReferencedAssemblies()
            .Where(r => r.Name?.StartsWith("Lexichord.Modules") == true)
            .Where(r => r.Name != assemblyName)
            .Select(r => r.Name)
            .ToList();

        // Assert
        Assert.That(moduleReferences, Is.Empty,
            "Sandbox module must not reference other modules");
    }

    [Test]
    public void SandboxModule_References_Abstractions()
    {
        // Arrange
        var assembly = typeof(SandboxModule).Assembly;

        // Act
        var references = assembly.GetReferencedAssemblies()
            .Select(r => r.Name)
            .ToList();

        // Assert
        Assert.That(references, Does.Contain("Lexichord.Abstractions"),
            "Sandbox module must reference Lexichord.Abstractions");
    }

    [Test]
    public void SandboxModule_HasParameterlessConstructor()
    {
        // Arrange
        var moduleType = typeof(SandboxModule);

        // Act
        var constructor = moduleType.GetConstructor(Type.EmptyTypes);

        // Assert
        Assert.That(constructor, Is.Not.Null,
            "SandboxModule must have a parameterless constructor for Activator.CreateInstance");
    }
}
```

### 5.4 Integration Test: End-to-End Module Loading

```csharp
[TestFixture]
[Category("Integration")]
public class SandboxModuleIntegrationTests
{
    [Test]
    public async Task ModuleLoader_LoadsSandboxModule_AndResolvesService()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        var licenseContext = new HardcodedLicenseContext();

        // Note: This test requires the Sandbox DLL to be in a Modules directory
        // In CI, this would use a test-specific modules directory

        var tempDir = Path.Combine(Path.GetTempPath(), $"ModuleTest_{Guid.NewGuid()}");
        var modulesDir = Path.Combine(tempDir, "Modules");
        Directory.CreateDirectory(modulesDir);

        try
        {
            // Copy the Sandbox DLL to test directory
            var sandboxDllPath = typeof(SandboxModule).Assembly.Location;
            var destPath = Path.Combine(modulesDir, Path.GetFileName(sandboxDllPath));
            File.Copy(sandboxDllPath, destPath);

            var tempProvider = services.BuildServiceProvider();
            var logger = tempProvider.GetRequiredService<ILogger<ModuleLoader>>();
            var moduleLoader = new ModuleLoader(logger, licenseContext, modulesDir);

            // Act - Phase 1: Discover and register
            await moduleLoader.DiscoverAndLoadAsync(services);

            // Rebuild provider with module services
            var provider = services.BuildServiceProvider();

            // Act - Phase 2: Initialize
            await moduleLoader.InitializeModulesAsync(provider);

            // Assert - Module was loaded
            Assert.That(moduleLoader.LoadedModules, Has.Count.EqualTo(1));
            Assert.That(moduleLoader.LoadedModules[0].Info.Id, Is.EqualTo("sandbox"));

            // Assert - Service is resolvable
            var sandboxService = provider.GetService<ISandboxService>();
            Assert.That(sandboxService, Is.Not.Null);
            Assert.That(sandboxService!.GetModuleName(), Is.EqualTo("Lexichord.Modules.Sandbox"));

            // Assert - Initialization occurred
            Assert.That(sandboxService.GetInitializationTime(), Is.GreaterThan(DateTime.MinValue));
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }
}
```

---

## 6. Observability & Logging

| Level       | Context        | Message Template                                                          |
| :---------- | :------------- | :------------------------------------------------------------------------ |
| Debug       | SandboxModule  | `Sandbox module InitializeAsync starting`                                 |
| Information | SandboxModule  | `Sandbox module initialized successfully. ISandboxService is ready...`    |
| Debug       | SandboxService | `Sandbox service initialization time set to {Time}`                       |
| Debug       | SandboxService | `GetModuleName called`                                                    |
| Debug       | SandboxService | `Echo called with input: {Input}`                                         |
| Debug       | SandboxService | `GetInitializationTime called, returning {Time}`                          |

---

## 7. File Structure

```
src/
├── Lexichord.Modules.Sandbox/
│   ├── Contracts/
│   │   └── ISandboxService.cs           # Service interface
│   ├── Services/
│   │   └── SandboxService.cs            # Service implementation
│   ├── SandboxModule.cs                 # IModule implementation
│   └── Lexichord.Modules.Sandbox.csproj # Project file
│
└── Modules/                              # Output directory
    └── Lexichord.Modules.Sandbox.dll     # Built module
```

---

## 8. Definition of Done

- [ ] `Lexichord.Modules.Sandbox` project created
- [ ] Project added to solution file
- [ ] Project targets net9.0
- [ ] Project outputs to `../../Modules/` directory
- [ ] Project references ONLY `Lexichord.Abstractions`
- [ ] Project has NO reference to `Lexichord.Host`
- [ ] `ISandboxService` interface defined with all methods
- [ ] `SandboxService` implements `ISandboxService`
- [ ] `SandboxService` uses primary constructor with `ILogger<T>`
- [ ] `SandboxModule` implements `IModule`
- [ ] `SandboxModule.Info` returns correct metadata
- [ ] `SandboxModule.RegisterServices` registers `ISandboxService`
- [ ] `SandboxModule.InitializeAsync` sets initialization time
- [ ] Module has NO `[RequiresLicense]` attribute (Core tier)
- [ ] `Lexichord.Modules.Sandbox.dll` appears in `./Modules/` after build
- [ ] Host logs "Loading module: Sandbox Module v0.0.1 by Lexichord Team"
- [ ] Host logs "Module Sandbox Module initialized"
- [ ] `ISandboxService` is resolvable from Host's DI container
- [ ] `ISandboxService.GetModuleName()` returns correct value
- [ ] `ISandboxService.GetInitializationTime()` returns valid timestamp
- [ ] Architecture test "Modules_ShouldNotReference_Host" passes
- [ ] Architecture test "Modules_ShouldNotReference_EachOther" passes
- [ ] All unit tests for SandboxModule passing
- [ ] All unit tests for SandboxService passing
- [ ] Integration test for end-to-end loading passing

---

## 9. Verification Commands

```bash
# Create the project
dotnet new classlib -n Lexichord.Modules.Sandbox -o src/Lexichord.Modules.Sandbox

# Add to solution
dotnet sln add src/Lexichord.Modules.Sandbox/Lexichord.Modules.Sandbox.csproj

# Add reference to Abstractions
dotnet add src/Lexichord.Modules.Sandbox reference src/Lexichord.Abstractions/Lexichord.Abstractions.csproj

# Build the module
dotnet build src/Lexichord.Modules.Sandbox

# Verify DLL is in Modules directory
ls Modules/
# Should show: Lexichord.Modules.Sandbox.dll

# Verify no Host reference
dotnet list src/Lexichord.Modules.Sandbox reference
# Should only show Lexichord.Abstractions

# Build entire solution
dotnet build

# Run Host and check logs
dotnet run --project src/Lexichord.Host
# Look for:
# - "Starting module discovery in..."
# - "Loading module: Sandbox Module v0.0.1 by Lexichord Team"
# - "Module Sandbox Module initialized"

# Run unit tests
dotnet test --filter "FullyQualifiedName~SandboxModuleTests"
dotnet test --filter "FullyQualifiedName~SandboxServiceTests"

# Run architecture tests
dotnet test --filter "Category=Architecture"

# Run integration tests
dotnet test --filter "Category=Integration"

# Verify service is resolvable (in debug or test)
# var service = App.Services.GetRequiredService<ISandboxService>();
# Console.WriteLine(service.GetModuleName()); // "Lexichord.Modules.Sandbox"
```

---

## 10. Appendix: Module Template

Use the Sandbox module as a template for creating new modules:

```csharp
// Template: src/Lexichord.Modules.{Name}/{Name}Module.cs

using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.{Name};

[RequiresLicense(LicenseTier.{Tier})] // Omit for Core tier
public sealed class {Name}Module : IModule
{
    public ModuleInfo Info => new(
        Id: "{name-lowercase}",
        Name: "{Display Name}",
        Version: new Version(1, 0, 0),
        Author: "Lexichord Team",
        Description: "{Brief description of the module}"
    );

    public void RegisterServices(IServiceCollection services)
    {
        // Register your services here
        services.AddSingleton<I{Name}Service, {Name}Service>();
    }

    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<{Name}Module>>();
        logger.LogInformation("{Name} module initialized");

        // Perform async initialization
        await Task.CompletedTask;
    }
}
```
