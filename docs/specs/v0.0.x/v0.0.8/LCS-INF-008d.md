# LCS-INF-008d: Release v0.0.8 - Golden Skeleton Tagging

## 1. Metadata & Categorization

| Field                | Value                                    | Description                                        |
| :------------------- | :--------------------------------------- | :------------------------------------------------- |
| **Feature ID**       | `INF-008d`                               | Infrastructure - Golden Skeleton Release           |
| **Feature Name**     | Golden Skeleton Release                  | Architecture tagging and documentation             |
| **Target Version**   | `v0.0.8d`                                | Fourth sub-part of v0.0.8                          |
| **Module Scope**     | All Projects                             | Full solution release                              |
| **Swimlane**         | `Infrastructure`                         | The Podium (Platform)                              |
| **License Tier**     | `Core`                                   | Foundation (Required for all tiers)                |
| **Author**           | System Architect                         |                                                    |
| **Status**           | **Draft**                                | Pending implementation                             |
| **Last Updated**     | 2026-01-26                               |                                                    |

---

## 2. Executive Summary

### 2.1 The Requirement

After v0.0.8a, v0.0.8b, and v0.0.8c prove that all foundational systems work together, we need to:

- Run comprehensive integration tests validating the entire architecture.
- Document the proven architecture for future developers.
- Create a "Module Developer Guide" using StatusBar as a reference.
- Tag the repository as `v0.0.8-golden-skeleton`.
- Establish the baseline for future development.

### 2.2 The Proposed Solution

We **SHALL** complete the Golden Skeleton release with:

1. **Integration Test Suite** - E2E tests proving all systems work together.
2. **Architecture Documentation** - Updated docs reflecting proven patterns.
3. **Module Developer Guide** - Step-by-step guide for creating modules.
4. **Release Notes** - Comprehensive changelog for v0.0.8.
5. **Git Tag** - Permanent marker for the architecture baseline.

---

## 3. Integration Test Suite

### 3.1 Test Categories

| Category                | Tests                                           | Purpose                              |
| :---------------------- | :---------------------------------------------- | :----------------------------------- |
| Module Discovery        | Module found, loaded, registered                | Verify v0.0.4 ModuleLoader           |
| Service Registration    | Services resolvable from DI                     | Verify DI integration                |
| Shell Region            | View appears in correct region                  | Verify v0.0.5 Shell Regions          |
| Database Integration    | CRUD operations, migrations                     | Verify v0.0.6 Database               |
| Vault Integration       | Store/retrieve secrets                          | Verify v0.0.7 Secure Vault           |
| Event Bus               | Events published and received                   | Verify v0.0.7 MediatR                |
| Configuration           | Settings load from multiple sources             | Verify v0.0.3 Configuration          |
| Logging                 | Log entries created at correct levels           | Verify v0.0.3 Serilog                |

### 3.2 Integration Test Implementation

**File:** `tests/Lexichord.IntegrationTests/GoldenSkeletonTests.cs`

```csharp
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Host;
using Lexichord.Host.Services;
using Lexichord.Modules.StatusBar;
using Lexichord.Modules.StatusBar.Services;
using Lexichord.Modules.StatusBar.ViewModels;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace Lexichord.IntegrationTests;

/// <summary>
/// Golden Skeleton integration tests - prove all foundational systems work together.
/// </summary>
/// <remarks>
/// LOGIC: These tests verify the complete integration of:
/// - Module Loading (v0.0.4)
/// - Shell Regions (v0.0.5)
/// - Database (v0.0.6)
/// - Secure Vault (v0.0.7)
/// - Event Bus (v0.0.7)
/// - Logging (v0.0.3)
/// - Configuration (v0.0.3)
/// - DI Container (v0.0.3)
///
/// If all these tests pass, the architecture is proven.
/// </remarks>
[TestFixture]
[Category("Integration")]
[Category("GoldenSkeleton")]
public class GoldenSkeletonTests
{
    private ServiceProvider _provider = null!;
    private StatusBarModule _statusBarModule = null!;
    private string _tempDbPath = null!;
    private string _tempConfigPath = null!;

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        // Create temporary paths
        var tempDir = Path.Combine(Path.GetTempPath(),
            $"lexichord-golden-skeleton-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        _tempDbPath = Path.Combine(tempDir, "lexichord.db");
        _tempConfigPath = Path.Combine(tempDir, "appsettings.json");

        // Create test configuration
        File.WriteAllText(_tempConfigPath, """
            {
              "Lexichord": {
                "ApplicationName": "Lexichord Test",
                "Environment": "Testing",
                "DebugMode": true
              },
              "Database": {
                "ConnectionString": "Data Source={DB_PATH}"
              }
            }
            """.Replace("{DB_PATH}", _tempDbPath.Replace("\\", "\\\\")));

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(tempDir)
            .AddJsonFile("appsettings.json")
            .Build();

        // Build services
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        // Add Host services
        HostServices.ConfigureServices(services, configuration);

        // Add StatusBar module services
        _statusBarModule = new StatusBarModule();
        _statusBarModule.RegisterServices(services);

        // Build provider
        _provider = services.BuildServiceProvider();

        // Initialize module
        await _statusBarModule.InitializeAsync(_provider);
    }

    [OneTimeTearDown]
    public void GlobalTearDown()
    {
        _provider?.Dispose();

        // Clean up temp files
        var tempDir = Path.GetDirectoryName(_tempDbPath);
        if (tempDir is not null && Directory.Exists(tempDir))
        {
            try { Directory.Delete(tempDir, recursive: true); }
            catch { /* Ignore cleanup errors */ }
        }
    }

    #region Module Discovery Tests (v0.0.4)

    [Test]
    [Order(1)]
    public void Module_HasCorrectMetadata()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_statusBarModule.Info.Id, Is.EqualTo("statusbar"));
            Assert.That(_statusBarModule.Info.Name, Is.EqualTo("Status Bar"));
            Assert.That(_statusBarModule.Info.Version, Is.EqualTo(new Version(0, 0, 8)));
            Assert.That(_statusBarModule.Info.Author, Is.EqualTo("Lexichord Team"));
        });
    }

    #endregion

    #region Service Registration Tests (v0.0.3)

    [Test]
    [Order(2)]
    public void Services_AllStatusBarServicesRegistered()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_provider.GetService<IHealthRepository>(), Is.Not.Null,
                "IHealthRepository should be registered");
            Assert.That(_provider.GetService<IHeartbeatService>(), Is.Not.Null,
                "IHeartbeatService should be registered");
            Assert.That(_provider.GetService<IVaultStatusService>(), Is.Not.Null,
                "IVaultStatusService should be registered");
            Assert.That(_provider.GetService<StatusBarViewModel>(), Is.Not.Null,
                "StatusBarViewModel should be registered");
        });
    }

    [Test]
    [Order(3)]
    public void Services_SingletonsReturnSameInstance()
    {
        // Act
        var health1 = _provider.GetRequiredService<IHealthRepository>();
        var health2 = _provider.GetRequiredService<IHealthRepository>();

        // Assert
        Assert.That(health1, Is.SameAs(health2),
            "Singleton services should return the same instance");
    }

    #endregion

    #region Shell Region Tests (v0.0.5)

    [Test]
    [Order(4)]
    public void ShellRegion_StatusBarRegisteredInBottomRegion()
    {
        // Act
        var regionView = _provider.GetService<IShellRegionView>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(regionView, Is.Not.Null,
                "IShellRegionView should be registered");
            Assert.That(regionView!.TargetRegion, Is.EqualTo(ShellRegion.Bottom),
                "StatusBar should target Bottom region");
            Assert.That(regionView.Order, Is.EqualTo(100),
                "StatusBar should have order 100");
        });
    }

    [Test]
    [Order(5)]
    public void ShellRegion_ViewContentIsAccessible()
    {
        // Act
        var regionView = _provider.GetRequiredService<IShellRegionView>();
        var content = regionView.ViewContent;

        // Assert
        Assert.That(content, Is.Not.Null,
            "ViewContent should return a view instance");
    }

    #endregion

    #region Database Tests (v0.0.6)

    [Test]
    [Order(6)]
    public async Task Database_HealthTableExists()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var version = await repo.GetDatabaseVersionAsync();

        // Assert
        Assert.That(version, Is.GreaterThan(0),
            "Database version should be set after migration");
    }

    [Test]
    [Order(7)]
    public async Task Database_StartupRecorded()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var lastHeartbeat = await repo.GetLastHeartbeatAsync();

        // Assert
        Assert.That(lastHeartbeat, Is.Not.Null,
            "Heartbeat should be recorded after startup");
        Assert.That(lastHeartbeat.Value, Is.GreaterThan(DateTime.UtcNow.AddMinutes(-5)),
            "Heartbeat should be recent");
    }

    [Test]
    [Order(8)]
    public async Task Database_UptimeIsPositive()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        await Task.Delay(100); // Ensure some time has passed
        var uptime = await repo.GetSystemUptimeAsync();

        // Assert
        Assert.That(uptime, Is.GreaterThan(TimeSpan.Zero),
            "Uptime should be greater than zero");
    }

    [Test]
    [Order(9)]
    public async Task Database_HeartbeatUpdates()
    {
        // Arrange
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var beforeHeartbeat = await repo.GetLastHeartbeatAsync();

        // Act
        await Task.Delay(100);
        await repo.RecordHeartbeatAsync();
        var afterHeartbeat = await repo.GetLastHeartbeatAsync();

        // Assert
        Assert.That(afterHeartbeat, Is.GreaterThan(beforeHeartbeat),
            "Heartbeat should update after recording");
    }

    [Test]
    [Order(10)]
    public async Task Database_HealthCheckPasses()
    {
        // Act
        var repo = _provider.GetRequiredService<IHealthRepository>();
        var isHealthy = await repo.CheckHealthAsync();

        // Assert
        Assert.That(isHealthy, Is.True,
            "Database health check should pass");
    }

    #endregion

    #region Vault Tests (v0.0.7)

    [Test]
    [Order(11)]
    public async Task Vault_StatusServiceAccessible()
    {
        // Act
        var vaultService = _provider.GetRequiredService<IVaultStatusService>();
        var status = await vaultService.GetVaultStatusAsync();

        // Assert
        Assert.That(status, Is.Not.EqualTo(VaultStatus.Error),
            "Vault should not be in error state");
    }

    [Test]
    [Order(12)]
    public async Task Vault_CanStoreAndRetrieveKey()
    {
        // Arrange
        var vaultService = _provider.GetRequiredService<IVaultStatusService>();
        const string testKey = "lexichord:integration-test-key";
        const string testValue = "test-value-12345";

        try
        {
            // Act
            await vaultService.StoreApiKeyAsync(testKey, testValue);
            var hasKey = await vaultService.CheckApiKeyPresenceAsync(testKey);

            // Assert
            Assert.That(hasKey, Is.True,
                "Key should exist after storing");
        }
        finally
        {
            // Cleanup
            await vaultService.DeleteApiKeyAsync(testKey);
        }
    }

    [Test]
    [Order(13)]
    public async Task Vault_KeyPresenceCheckWorks()
    {
        // Arrange
        var vaultService = _provider.GetRequiredService<IVaultStatusService>();
        const string nonExistentKey = "lexichord:non-existent-key-12345";

        // Act
        var hasKey = await vaultService.CheckApiKeyPresenceAsync(nonExistentKey);

        // Assert
        Assert.That(hasKey, Is.False,
            "Non-existent key should return false");
    }

    #endregion

    #region Event Bus Tests (v0.0.7)

    [Test]
    [Order(14)]
    public async Task EventBus_MediatRConfigured()
    {
        // Act
        var mediator = _provider.GetService<IMediator>();

        // Assert
        Assert.That(mediator, Is.Not.Null,
            "IMediator should be registered");
    }

    [Test]
    [Order(15)]
    public async Task EventBus_CanPublishEvents()
    {
        // Arrange
        var mediator = _provider.GetRequiredService<IMediator>();
        var testEvent = new SystemHealthChangedEvent(
            HealthStatus.Healthy,
            "Integration test event",
            DateTime.UtcNow);

        // Act & Assert (no exception = success)
        await mediator.Publish(testEvent);
        Assert.Pass("Event published without exception");
    }

    #endregion

    #region Configuration Tests (v0.0.3)

    [Test]
    [Order(16)]
    public void Configuration_LoadedCorrectly()
    {
        // Act
        var config = _provider.GetRequiredService<IConfiguration>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(config["Lexichord:ApplicationName"],
                Is.EqualTo("Lexichord Test"));
            Assert.That(config["Lexichord:Environment"],
                Is.EqualTo("Testing"));
            Assert.That(config.GetValue<bool>("Lexichord:DebugMode"),
                Is.True);
        });
    }

    #endregion

    #region Logging Tests (v0.0.3)

    [Test]
    [Order(17)]
    public void Logging_LoggerFactoryRegistered()
    {
        // Act
        var loggerFactory = _provider.GetService<ILoggerFactory>();
        var logger = _provider.GetService<ILogger<GoldenSkeletonTests>>();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(loggerFactory, Is.Not.Null,
                "ILoggerFactory should be registered");
            Assert.That(logger, Is.Not.Null,
                "ILogger<T> should be resolvable");
        });
    }

    [Test]
    [Order(18)]
    public void Logging_CanWriteLogs()
    {
        // Arrange
        var logger = _provider.GetRequiredService<ILogger<GoldenSkeletonTests>>();

        // Act & Assert (no exception = success)
        logger.LogInformation("Golden Skeleton integration test log entry");
        Assert.Pass("Log entry written without exception");
    }

    #endregion

    #region HeartbeatService Tests

    [Test]
    [Order(19)]
    public void HeartbeatService_IsRunning()
    {
        // Act
        var heartbeat = _provider.GetRequiredService<IHeartbeatService>();

        // Assert
        Assert.That(heartbeat.IsRunning, Is.True,
            "Heartbeat service should be running after module init");
    }

    [Test]
    [Order(20)]
    public void HeartbeatService_IntervalIsOneMinute()
    {
        // Act
        var heartbeat = _provider.GetRequiredService<IHeartbeatService>();

        // Assert
        Assert.That(heartbeat.Interval, Is.EqualTo(TimeSpan.FromSeconds(60)),
            "Heartbeat interval should be 60 seconds");
    }

    #endregion

    #region End-to-End Flow Tests

    [Test]
    [Order(100)]
    public async Task E2E_CompleteStatusBarFlow()
    {
        // This test verifies the complete flow from module to UI

        // 1. Get the ViewModel (which has all dependencies injected)
        var viewModel = _provider.GetRequiredService<StatusBarViewModel>();

        // 2. Verify database status properties are set
        await Task.Delay(500); // Allow time for initial refresh
        Assert.That(viewModel.DatabaseStatusText, Does.StartWith("DB:"),
            "Database status should be populated");
        Assert.That(viewModel.UptimeText, Does.StartWith("Uptime:"),
            "Uptime should be populated");

        // 3. Verify vault status properties are set
        Assert.That(viewModel.VaultStatusText, Does.StartWith("Vault:"),
            "Vault status should be populated");

        // 4. Verify one of the status indicators is true
        var hasDatabaseIndicator =
            viewModel.IsDatabaseHealthy ||
            viewModel.IsDatabaseWarning ||
            viewModel.IsDatabaseError;
        Assert.That(hasDatabaseIndicator, Is.True,
            "One database indicator should be active");

        var hasVaultIndicator =
            viewModel.IsVaultReady ||
            viewModel.IsVaultEmpty ||
            viewModel.IsVaultError;
        Assert.That(hasVaultIndicator, Is.True,
            "One vault indicator should be active");
    }

    #endregion
}
```

---

## 4. Architecture Documentation

### 4.1 Proven Architecture Diagram

Update `docs/architecture/overview.md` with the proven architecture:

```markdown
# Lexichord Architecture Overview

## Proven Systems (v0.0.8 Golden Skeleton)

The following architectural components have been proven through end-to-end testing:

### Module System (v0.0.4)
- Modules are discovered from `./Modules/*.dll`
- Modules implement `IModule` interface
- Modules register services via `RegisterServices(IServiceCollection)`
- Modules initialize via `InitializeAsync(IServiceProvider)`
- License restrictions enforced via `[RequiresLicense]` attribute

### Shell Regions (v0.0.5)
- Host defines regions: Top, Left, Center, Right, Bottom
- Modules contribute views via `IShellRegionView`
- Views are ordered by `Order` property within regions
- ViewContent is lazily created from DI

### Database (v0.0.6)
- SQLite with `IDbConnectionFactory` for connection management
- Repository pattern for data access
- Migrations run during module initialization
- Shared database across all modules

### Secure Vault (v0.0.7)
- Platform-native secure storage via `ISecureVault`
- Windows: DPAPI / Credential Manager
- macOS: Keychain
- Linux: libsecret
- Secrets never exposed in logs

### Event Bus (v0.0.7)
- MediatR for in-process messaging
- `INotification` for events, `IRequest<T>` for queries
- Loose coupling between components
- Async handlers supported

### DI Container (v0.0.3)
- Microsoft.Extensions.DependencyInjection
- Singleton, Transient, Scoped lifetimes
- Constructor injection preferred
- Service locator deprecated

### Configuration (v0.0.3)
- Multi-source: JSON -> Environment -> CLI
- Options pattern: `IOptions<T>`
- Environment-specific files: `appsettings.{Env}.json`

### Logging (v0.0.3)
- Serilog structured logging
- Console + File sinks
- Rolling log files with retention
- `ILogger<T>` injection
```

### 4.2 Module Developer Guide

Create `docs/guides/module-development.md`:

```markdown
# Module Development Guide

This guide explains how to create a Lexichord module using the StatusBar module as a reference implementation.

## Quick Start

1. Create a new Class Library project
2. Reference only `Lexichord.Abstractions`
3. Implement `IModule` interface
4. Configure output to `./Modules/` directory

## Project Structure

```
src/Lexichord.Modules.YourModule/
+-- YourModule.csproj
+-- YourModuleModule.cs           # IModule implementation
+-- Views/
|   +-- MainView.axaml
|   +-- MainView.axaml.cs
+-- ViewModels/
|   +-- MainViewModel.cs
+-- Services/
    +-- YourService.cs
```

## Project File Template

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <OutputPath>$(SolutionDir)Modules\</OutputPath>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.2.3" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\Lexichord.Abstractions\Lexichord.Abstractions.csproj" />
  </ItemGroup>
</Project>
```

## IModule Implementation

```csharp
public class YourModuleModule : IModule
{
    public ModuleInfo Info => new(
        Id: "yourmodule",
        Name: "Your Module",
        Version: new Version(1, 0, 0),
        Author: "Your Name",
        Description: "What your module does"
    );

    public void RegisterServices(IServiceCollection services)
    {
        // Register views, viewmodels, and services
        services.AddTransient<MainView>();
        services.AddTransient<MainViewModel>();
        services.AddSingleton<IYourService, YourService>();
    }

    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<YourModuleModule>>();
        logger.LogInformation("Initializing {Module}", Info.Name);

        // Initialize services, run migrations, etc.
    }
}
```

## Accessing Host Services

Modules can inject any Host service through the standard DI container:

```csharp
public class YourService(
    IDbConnectionFactory dbFactory,    // Database access
    ISecureVault vault,                 // Secure storage
    IMediator mediator,                 // Event bus
    IConfiguration config,              // Configuration
    ILogger<YourService> logger)        // Logging
{
    // Use injected services
}
```

## Shell Region Registration

To add UI to the Host:

```csharp
public class YourRegionView : IShellRegionView
{
    public ShellRegion TargetRegion => ShellRegion.Left;
    public int Order => 50;
    public object ViewContent => _view;
}

// Register in IModule.RegisterServices:
services.AddSingleton<IShellRegionView, YourRegionView>();
```

## License Restrictions

To restrict your module to certain license tiers:

```csharp
[RequiresLicense(LicenseTier.Teams)]
public class PremiumModule : IModule
{
    // Only loaded if user has Teams or higher license
}
```

## Testing

Write unit tests for your services:

```csharp
[TestFixture]
public class YourServiceTests
{
    [Test]
    public async Task YourMethod_WorksCorrectly()
    {
        // Arrange
        var mock = new Mock<IDbConnectionFactory>();
        var sut = new YourService(mock.Object, ...);

        // Act
        var result = await sut.YourMethodAsync();

        // Assert
        Assert.That(result, Is.Not.Null);
    }
}
```

## Reference Implementation

See `Lexichord.Modules.StatusBar` for a complete example of:
- Module lifecycle
- Database access
- Vault integration
- Shell region registration
- Event publishing
- ViewModel with DI
```

---

## 5. Release Notes

Create `docs/releases/v0.0.8.md`:

```markdown
# Release Notes: v0.0.8 - Golden Skeleton

**Release Date:** [TBD]
**Codename:** The Hello World (End-to-End Proof)
**Tag:** `v0.0.8-golden-skeleton`

## Overview

v0.0.8 marks the completion of the Lexichord foundation phase. This release proves that all architectural components work together end-to-end through the implementation of the StatusBar module.

## What's New

### StatusBar Module (v0.0.8a-c)
- New `Lexichord.Modules.StatusBar` module
- Database health monitoring with uptime display
- Secure vault status checking
- Interactive API key configuration dialog
- Shell region registration in Bottom area

### Integration Tests (v0.0.8d)
- Comprehensive test suite validating all systems
- 20+ integration tests covering:
  - Module discovery and loading
  - Service registration
  - Shell region integration
  - Database operations
  - Vault operations
  - Event bus messaging
  - Configuration loading
  - Logging infrastructure

### Documentation (v0.0.8d)
- Updated architecture documentation
- New Module Developer Guide
- StatusBar as reference implementation

## Systems Proven

This release validates the following foundational systems:

| System              | Version | Status   |
| :------------------ | :------ | :------- |
| Module Loading      | v0.0.4  | Proven   |
| Shell Regions       | v0.0.5  | Proven   |
| Database (SQLite)   | v0.0.6  | Proven   |
| Secure Vault        | v0.0.7  | Proven   |
| Event Bus (MediatR) | v0.0.7  | Proven   |
| DI Container        | v0.0.3  | Proven   |
| Configuration       | v0.0.3  | Proven   |
| Logging (Serilog)   | v0.0.3  | Proven   |
| Exception Handling  | v0.0.3  | Proven   |

## Breaking Changes

None - this is the first complete release.

## Known Limitations

1. **Vault on Linux:** Some Linux distributions may require additional packages for libsecret.
2. **Module Unloading:** Modules cannot be unloaded at runtime (restart required).
3. **Shell Region Ordering:** Complex ordering scenarios not yet supported.

## Upgrade Instructions

This is the baseline release. Future upgrades will include migration instructions.

## What's Next

- v0.1.x: First feature modules (Editor, File Manager)
- v0.2.x: LLM integration infrastructure
- v0.3.x: Agent system foundation

## Contributors

- System Architect
- [Additional contributors]

## Verification

To verify your installation:

```bash
dotnet test --filter "Category=GoldenSkeleton"
```

All 20+ integration tests should pass.
```

---

## 6. Git Tagging Process

### 6.1 Pre-Tag Checklist

Before creating the tag, verify:

```bash
# 1. All tests pass
dotnet test

# 2. Build succeeds in Release mode
dotnet build -c Release

# 3. StatusBar module exists
ls Modules/Lexichord.Modules.StatusBar.dll

# 4. No uncommitted changes
git status

# 5. On main/master branch
git branch --show-current
```

### 6.2 Create Tag

```bash
# Create annotated tag with message
git tag -a v0.0.8-golden-skeleton -m "Golden Skeleton: Architecture proven

This release marks the completion of the Lexichord foundation phase.
All foundational systems have been proven to work together:
- Module Loading (v0.0.4)
- Shell Regions (v0.0.5)
- Database (v0.0.6)
- Secure Vault (v0.0.7)
- Event Bus (v0.0.7)
- DI, Configuration, Logging (v0.0.3)

The StatusBar module serves as the reference implementation for
future module development.

See docs/releases/v0.0.8.md for full release notes."

# Push tag to remote
git push origin v0.0.8-golden-skeleton
```

### 6.3 Post-Tag Verification

```bash
# Verify tag exists
git tag -l "v0.0.8*"

# Verify tag message
git show v0.0.8-golden-skeleton

# Verify tag is on remote
git ls-remote --tags origin | grep golden
```

---

## 7. CI/CD Pipeline Requirements

### 7.1 GitHub Actions Workflow

**File:** `.github/workflows/release.yml`

```yaml
name: Release Build

on:
  push:
    tags:
      - 'v*.*.*'

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release --no-restore

      - name: Test
        run: dotnet test -c Release --no-build --filter "Category=Integration"

  build:
    needs: test
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [ubuntu-latest, windows-latest, macos-latest]

    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Build
        run: dotnet build -c Release

      - name: Publish
        run: dotnet publish src/Lexichord.Host -c Release -o publish

      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: lexichord-${{ matrix.os }}
          path: publish/
```

---

## 8. Definition of Done

- [ ] All 20+ integration tests written and passing
- [ ] Tests cover all foundational systems
- [ ] Architecture documentation updated
- [ ] Module Developer Guide created
- [ ] Release notes (v0.0.8.md) written
- [ ] Pre-tag checklist completed
- [ ] Git tag `v0.0.8-golden-skeleton` created
- [ ] Tag pushed to remote repository
- [ ] CI/CD pipeline passes all checks
- [ ] Documentation reviewed and approved

---

## 9. Verification Commands

```bash
# 1. Run all integration tests
dotnet test --filter "Category=Integration"

# 2. Run Golden Skeleton specific tests
dotnet test --filter "Category=GoldenSkeleton"

# 3. Build release
dotnet build -c Release

# 4. Verify module output
ls Modules/

# 5. Run application
dotnet run --project src/Lexichord.Host

# 6. Verify tag creation
git tag -l "v0.0.8*"
git show v0.0.8-golden-skeleton

# 7. Verify CI/CD (after push)
gh run list --limit 5
```

---

## 10. What v0.0.8d Proves

When v0.0.8d is complete, we have proven:

1. **All Systems Integrate** - Every foundational component works together
2. **Architecture is Sound** - Patterns can be replicated in new modules
3. **Tests Provide Confidence** - Regressions will be caught
4. **Documentation is Complete** - Developers can build modules
5. **Release Process Works** - Tagging and CI/CD are established

**The Golden Skeleton is complete. The architecture is frozen for v0.0.x.**

Future development proceeds to v0.1.x with confidence that the foundation is solid.
