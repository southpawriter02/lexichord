# LCS-INF-004b: The Discovery Engine (ModuleLoader)

## 1. Metadata & Categorization

| Field                | Value                                    | Description                                           |
| :------------------- | :--------------------------------------- | :---------------------------------------------------- |
| **Feature ID**       | `INF-004b`                               | Infrastructure - Module Discovery & Loading           |
| **Feature Name**     | The Discovery Engine (ModuleLoader)      | Discovers and loads module assemblies                 |
| **Target Version**   | `v0.0.4b`                                | Second sub-part of v0.0.4                             |
| **Module Scope**     | `Lexichord.Host`                         | Primary application executable                        |
| **Swimlane**         | `Infrastructure`                         | The Podium (Platform)                                 |
| **License Tier**     | `Core`                                   | Foundation (Required for all tiers)                   |
| **Author**           | System Architect                         |                                                       |
| **Status**           | **Draft**                                | Pending implementation                                |
| **Last Updated**     | 2026-01-26                               |                                                       |

---

## 2. Executive Summary

### 2.1 The Requirement

The module loading system requires a discovery engine that:

- Scans the `./Modules/` directory for DLL files at startup.
- Uses reflection to find types implementing `IModule`.
- Checks license requirements before loading.
- Orchestrates the two-phase lifecycle (registration, then initialization).
- Handles failures gracefully without crashing the application.

### 2.2 The Proposed Solution

We **SHALL** implement the `ModuleLoader` service in `Lexichord.Host.Services` that:

1. Scans `{AppDir}/Modules/*.dll` for module assemblies.
2. Uses `AssemblyLoadContext` for isolated assembly loading.
3. Finds `IModule` implementations via reflection.
4. Manages the module lifecycle (register services, then initialize).
5. Tracks loaded and failed modules for diagnostics.

---

## 3. Implementation Tasks

### Task 1.1: IModuleLoader Interface

**File:** `src/Lexichord.Abstractions/Contracts/IModuleLoader.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service responsible for discovering and loading modules.
/// </summary>
/// <remarks>
/// LOGIC: The module loader orchestrates the complete module lifecycle:
/// 1. Discovery: Scan ./Modules/ directory for DLLs
/// 2. Loading: Load assemblies into isolated contexts
/// 3. Reflection: Find IModule implementations
/// 4. License Check: Verify module is authorized for current tier
/// 5. Registration: Call RegisterServices for each authorized module
/// 6. Initialization: Call InitializeAsync after DI container is built
///
/// Design Decisions:
/// - Interface in Abstractions allows mocking in tests
/// - Implementation in Host (only Host knows about assembly loading)
/// - LoadedModules/FailedModules exposed for diagnostics
/// - Async methods for potentially slow I/O operations
/// </remarks>
public interface IModuleLoader
{
    /// <summary>
    /// Gets the collection of successfully loaded modules.
    /// </summary>
    /// <remarks>
    /// LOGIC: This collection is populated after DiscoverAndLoadAsync completes.
    /// Modules appear here only if:
    /// - Assembly loaded successfully
    /// - IModule implementation found
    /// - License check passed
    /// - RegisterServices completed without exception
    /// </remarks>
    IReadOnlyList<IModule> LoadedModules { get; }

    /// <summary>
    /// Gets information about modules that failed to load.
    /// </summary>
    /// <remarks>
    /// LOGIC: This collection captures all failure cases:
    /// - Assembly load failures (missing dependencies, bad format)
    /// - No IModule implementation found (recorded but not a failure)
    /// - License check failures (logged, module skipped)
    /// - RegisterServices exceptions (logged, module skipped)
    /// - InitializeAsync exceptions (logged, module marked failed)
    /// </remarks>
    IReadOnlyList<ModuleLoadFailure> FailedModules { get; }

    /// <summary>
    /// Discovers and loads all modules from the modules directory.
    /// </summary>
    /// <param name="services">The service collection for module registration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: This method performs Phase 1 of module loading:
    /// 1. Scan ./Modules/*.dll
    /// 2. Load each assembly
    /// 3. Find IModule types
    /// 4. Check license requirements
    /// 5. Instantiate modules
    /// 6. Call RegisterServices()
    ///
    /// Called BEFORE ServiceProvider is built.
    /// </remarks>
    Task DiscoverAndLoadAsync(IServiceCollection services, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initializes all loaded modules after the service provider is built.
    /// </summary>
    /// <param name="provider">The service provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: This method performs Phase 2 of module loading:
    /// 1. Iterate through LoadedModules
    /// 2. Call InitializeAsync() on each
    /// 3. Log success/failure for each module
    ///
    /// Called AFTER ServiceProvider is built.
    /// Failures are logged but don't crash the application.
    /// </remarks>
    Task InitializeModulesAsync(IServiceProvider provider, CancellationToken cancellationToken = default);
}
```

---

### Task 1.2: ModuleLoadFailure Record

**File:** `src/Lexichord.Abstractions/Contracts/ModuleLoadFailure.cs`

```csharp
namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Contains information about a module that failed to load.
/// </summary>
/// <remarks>
/// LOGIC: This record captures diagnostic information for module failures.
/// Used for:
/// - Logging failed modules during startup
/// - Displaying error information in diagnostics UI
/// - Troubleshooting module loading issues
///
/// FailureReason is human-readable; Exception provides technical details.
/// </remarks>
/// <param name="AssemblyPath">The file path of the assembly that failed.</param>
/// <param name="ModuleName">The name of the module type, if discovered before failure.</param>
/// <param name="FailureReason">Human-readable description of why loading failed.</param>
/// <param name="Exception">The exception that caused the failure, if any.</param>
public record ModuleLoadFailure(
    string AssemblyPath,
    string? ModuleName,
    string FailureReason,
    Exception? Exception
)
{
    /// <summary>
    /// Returns a formatted string representation of the failure.
    /// </summary>
    public override string ToString() =>
        ModuleName is not null
            ? $"{ModuleName} ({AssemblyPath}): {FailureReason}"
            : $"{AssemblyPath}: {FailureReason}";
}
```

---

### Task 1.3: ModuleLoader Implementation

**File:** `src/Lexichord.Host/Services/ModuleLoader.cs`

```csharp
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace Lexichord.Host.Services;

/// <summary>
/// Discovers and loads modules from the Modules directory.
/// </summary>
/// <remarks>
/// LOGIC: The ModuleLoader is the heart of Lexichord's modular architecture.
/// It scans the ./Modules/ directory for DLLs, uses reflection to find IModule
/// implementations, checks license requirements, and orchestrates the module lifecycle.
///
/// Key Design Decisions:
/// - Modules are loaded into the default AssemblyLoadContext (isolation via separation)
/// - License checks happen BEFORE module instantiation (fail fast)
/// - Failed modules don't crash the application (fail-safe pattern)
/// - Module order is discovery order (dependency ordering is future enhancement)
///
/// Thread Safety:
/// - DiscoverAndLoadAsync should be called once during startup
/// - LoadedModules/FailedModules are read-only after discovery completes
/// - InitializeModulesAsync should be called once after DI container is built
/// </remarks>
public sealed class ModuleLoader : IModuleLoader
{
    private readonly ILogger<ModuleLoader> _logger;
    private readonly ILicenseContext _licenseContext;
    private readonly string _modulesPath;

    private readonly List<IModule> _loadedModules = [];
    private readonly List<ModuleLoadFailure> _failedModules = [];

    /// <summary>
    /// Creates a new ModuleLoader instance.
    /// </summary>
    /// <param name="logger">Logger for module loading events.</param>
    /// <param name="licenseContext">License context for tier checks.</param>
    /// <param name="modulesPath">Optional custom modules directory path.</param>
    /// <remarks>
    /// LOGIC: modulesPath parameter allows testing with custom directories.
    /// In production, defaults to {AppDir}/Modules/.
    /// </remarks>
    public ModuleLoader(
        ILogger<ModuleLoader> logger,
        ILicenseContext licenseContext,
        string? modulesPath = null)
    {
        _logger = logger;
        _licenseContext = licenseContext;
        _modulesPath = modulesPath ?? Path.Combine(AppContext.BaseDirectory, "Modules");
    }

    /// <inheritdoc/>
    public IReadOnlyList<IModule> LoadedModules => _loadedModules.AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<ModuleLoadFailure> FailedModules => _failedModules.AsReadOnly();

    /// <inheritdoc/>
    public async Task DiscoverAndLoadAsync(
        IServiceCollection services,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Starting module discovery in {ModulesPath}", _modulesPath);

        // LOGIC: Create modules directory if it doesn't exist
        // This prevents startup failures when no modules are installed yet
        if (!Directory.Exists(_modulesPath))
        {
            _logger.LogWarning("Modules directory not found, creating: {ModulesPath}", _modulesPath);
            Directory.CreateDirectory(_modulesPath);
            return;
        }

        var dllFiles = Directory.GetFiles(_modulesPath, "*.dll");
        _logger.LogDebug("Found {Count} DLL files in modules directory", dllFiles.Length);

        if (dllFiles.Length == 0)
        {
            _logger.LogInformation("No module DLLs found in {ModulesPath}", _modulesPath);
            return;
        }

        var currentTier = _licenseContext.GetCurrentTier();
        _logger.LogInformation("Current license tier: {Tier}", currentTier);

        foreach (var dllPath in dllFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await LoadModuleFromAssemblyAsync(dllPath, services, currentTier);
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Module discovery complete in {Duration}ms. Loaded: {LoadedCount}, Failed: {FailedCount}",
            stopwatch.ElapsedMilliseconds, _loadedModules.Count, _failedModules.Count);
    }

    private async Task LoadModuleFromAssemblyAsync(
        string dllPath,
        IServiceCollection services,
        LicenseTier currentTier)
    {
        var fileName = Path.GetFileName(dllPath);
        _logger.LogDebug("Processing assembly: {FileName}", fileName);

        try
        {
            // LOGIC: Load assembly into default context
            // We don't use isolated contexts because modules share types with Host
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(dllPath);
            _logger.LogDebug("Assembly {FileName} loaded successfully", fileName);

            // LOGIC: Find all types implementing IModule
            // A single assembly can contain multiple modules (rare but supported)
            var moduleTypes = assembly.GetTypes()
                .Where(t => typeof(IModule).IsAssignableFrom(t)
                         && t.IsClass
                         && !t.IsAbstract)
                .ToList();

            if (moduleTypes.Count == 0)
            {
                // LOGIC: Not an error - many DLLs in Modules/ are dependencies
                _logger.LogDebug("No IModule implementations found in {FileName}", fileName);
                return;
            }

            _logger.LogDebug("Found {Count} IModule types in {FileName}", moduleTypes.Count, fileName);

            foreach (var moduleType in moduleTypes)
            {
                await LoadModuleTypeAsync(moduleType, services, currentTier, dllPath);
            }
        }
        catch (ReflectionTypeLoadException ex)
        {
            // LOGIC: This occurs when assembly references missing types
            var loaderExceptions = string.Join("; ",
                ex.LoaderExceptions?.Select(e => e?.Message) ?? []);

            _logger.LogError(ex,
                "Failed to load types from assembly {FileName}. Loader exceptions: {LoaderExceptions}",
                fileName, loaderExceptions);

            _failedModules.Add(new ModuleLoadFailure(
                dllPath,
                null,
                $"Type loading failed: {loaderExceptions}",
                ex));
        }
        catch (BadImageFormatException ex)
        {
            // LOGIC: DLL is not a valid .NET assembly (might be native DLL)
            _logger.LogWarning(
                "Skipping non-.NET assembly: {FileName}. This may be a native dependency.",
                fileName);

            // Don't add to failures - native DLLs in Modules/ are expected
        }
        catch (FileLoadException ex)
        {
            _logger.LogError(ex, "Failed to load assembly: {FileName}", fileName);
            _failedModules.Add(new ModuleLoadFailure(
                dllPath,
                null,
                $"File load failed: {ex.Message}",
                ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error loading assembly: {FileName}", fileName);
            _failedModules.Add(new ModuleLoadFailure(
                dllPath,
                null,
                $"Unexpected error: {ex.Message}",
                ex));
        }
    }

    private async Task LoadModuleTypeAsync(
        Type moduleType,
        IServiceCollection services,
        LicenseTier currentTier,
        string dllPath)
    {
        var moduleName = moduleType.Name;
        _logger.LogDebug("Processing module type: {ModuleName}", moduleName);

        try
        {
            // LOGIC: Check license requirements BEFORE instantiation
            // This prevents unauthorized modules from executing any code
            var licenseAttr = moduleType.GetCustomAttribute<RequiresLicenseAttribute>();
            var requiredTier = licenseAttr?.Tier ?? LicenseTier.Core;

            if (requiredTier > currentTier)
            {
                _logger.LogWarning(
                    "Skipping module {ModuleName} due to license restrictions. " +
                    "Required: {RequiredTier}, Current: {CurrentTier}",
                    moduleName, requiredTier, currentTier);

                _failedModules.Add(new ModuleLoadFailure(
                    dllPath,
                    moduleName,
                    $"License tier {requiredTier} required, current tier is {currentTier}",
                    null));
                return;
            }

            // LOGIC: Feature code check for granular gating (future use)
            if (licenseAttr?.FeatureCode is { } featureCode &&
                !_licenseContext.IsFeatureEnabled(featureCode))
            {
                _logger.LogWarning(
                    "Skipping module {ModuleName} due to feature restriction. " +
                    "Feature: {FeatureCode} is not enabled.",
                    moduleName, featureCode);

                _failedModules.Add(new ModuleLoadFailure(
                    dllPath,
                    moduleName,
                    $"Feature {featureCode} is not enabled in current license",
                    null));
                return;
            }

            // LOGIC: Create module instance (requires parameterless constructor)
            var module = (IModule)Activator.CreateInstance(moduleType)!;

            _logger.LogInformation(
                "Loading module: {ModuleName} v{Version} by {Author}",
                module.Info.Name, module.Info.Version, module.Info.Author);

            // LOGIC: Let module register its services
            module.RegisterServices(services);
            _logger.LogDebug("Module {ModuleName} registered services", module.Info.Name);

            _loadedModules.Add(module);
            _logger.LogInformation("Module {ModuleName} loaded successfully", module.Info.Name);
        }
        catch (MissingMethodException ex)
        {
            _logger.LogError(ex,
                "Module {ModuleName} missing parameterless constructor", moduleName);

            _failedModules.Add(new ModuleLoadFailure(
                dllPath,
                moduleName,
                "Module must have a parameterless constructor",
                ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load module type: {ModuleName}", moduleName);
            _failedModules.Add(new ModuleLoadFailure(
                dllPath,
                moduleName,
                $"Module instantiation failed: {ex.Message}",
                ex));
        }
    }

    /// <inheritdoc/>
    public async Task InitializeModulesAsync(
        IServiceProvider provider,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing {Count} loaded modules", _loadedModules.Count);
        var totalStopwatch = Stopwatch.StartNew();

        foreach (var module in _loadedModules)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var moduleStopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogDebug("Initializing module: {ModuleName}", module.Info.Name);
                await module.InitializeAsync(provider);
                moduleStopwatch.Stop();

                _logger.LogInformation(
                    "Module {ModuleName} initialized in {Duration}ms",
                    module.Info.Name, moduleStopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                moduleStopwatch.Stop();

                // LOGIC: Log error but continue initializing other modules
                // A single module failure shouldn't prevent other features from working
                _logger.LogError(ex,
                    "Failed to initialize module {ModuleName} after {Duration}ms",
                    module.Info.Name, moduleStopwatch.ElapsedMilliseconds);
            }
        }

        totalStopwatch.Stop();
        _logger.LogInformation(
            "Module initialization complete in {Duration}ms",
            totalStopwatch.ElapsedMilliseconds);
    }
}
```

---

### Task 1.4: Host Integration

**Updates to `App.axaml.cs`:**

```csharp
public override void OnFrameworkInitializationCompleted()
{
    if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
    {
        var args = desktop.Args ?? [];
        _configuration = HostServices.BuildConfiguration(args);
        SerilogExtensions.ConfigureSerilog(_configuration);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(_configuration);
        services.ConfigureServices(_configuration);

        // LOGIC: Create license context (stub for v0.0.4)
        var licenseContext = new HardcodedLicenseContext();
        services.AddSingleton<ILicenseContext>(licenseContext);

        // LOGIC: Create temporary service provider for logger
        // Required because ModuleLoader needs ILogger before full DI is built
        var tempProvider = services.BuildServiceProvider();
        var moduleLoaderLogger = tempProvider.GetRequiredService<ILogger<ModuleLoader>>();

        // LOGIC: Create module loader and discover modules
        var moduleLoader = new ModuleLoader(moduleLoaderLogger, licenseContext);

        // LOGIC: Phase 1 - Discover modules and register their services
        // This must complete before BuildServiceProvider is called
        moduleLoader.DiscoverAndLoadAsync(services).GetAwaiter().GetResult();

        // LOGIC: Register the module loader itself for diagnostics
        services.AddSingleton<IModuleLoader>(moduleLoader);

        // Build final service provider with all module services
        _serviceProvider = services.BuildServiceProvider();

        // LOGIC: Phase 2 - Initialize modules with full DI container
        // Modules can now resolve any registered service
        moduleLoader.InitializeModulesAsync(_serviceProvider).GetAwaiter().GetResult();

        // Register exception handlers, create window, etc.
        RegisterExceptionHandlers();
        desktop.MainWindow = CreateMainWindow();
        ApplyPersistedSettings();
    }

    base.OnFrameworkInitializationCompleted();
}
```

---

## 4. Decision Tree: Module Loading

```text
START: "Should this assembly be loaded as a module?"
│
├── Does the DLL exist in ./Modules/ directory?
│   ├── NO --> Skip (not in modules folder)
│   └── YES --> Continue
│
├── Can the assembly be loaded?
│   ├── NO (BadImageFormatException) --> Skip silently (native DLL)
│   ├── NO (FileLoadException) --> Log error, add to FailedModules
│   ├── NO (ReflectionTypeLoadException) --> Log error, add to FailedModules
│   └── YES --> Continue
│
├── Does it contain a type implementing IModule?
│   ├── NO --> Skip silently (dependency DLL, not a module)
│   └── YES --> Continue (may have multiple IModule types)
│
├── For each IModule type:
│   │
│   ├── Does the type have [RequiresLicense]?
│   │   ├── NO --> Treat as Core tier (always authorized)
│   │   └── YES --> Check RequiredTier <= CurrentTier
│   │       ├── Authorized --> Continue
│   │       └── Not Authorized --> Log warning, add to FailedModules, skip
│   │
│   ├── Does the type have parameterless constructor?
│   │   ├── NO --> Log error, add to FailedModules, skip
│   │   └── YES --> Create instance via Activator.CreateInstance
│   │
│   ├── Does RegisterServices complete without exception?
│   │   ├── NO --> Log error, add to FailedModules, skip
│   │   └── YES --> Add to LoadedModules
│   │
│   └── [After DI built] Does InitializeAsync complete without exception?
│       ├── NO --> Log error (module stays in LoadedModules)
│       └── YES --> Log success
│
└── END
```

---

## 5. Unit Testing Requirements

### 5.1 Test: Empty Modules Directory

```csharp
[TestFixture]
[Category("Unit")]
public class ModuleLoaderDiscoveryTests
{
    private Mock<ILogger<ModuleLoader>> _mockLogger = null!;
    private Mock<ILicenseContext> _mockLicense = null!;
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<ModuleLoader>>();
        _mockLicense = new Mock<ILicenseContext>();
        _mockLicense.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);
        _mockLicense.Setup(x => x.IsFeatureEnabled(It.IsAny<string>())).Returns(true);

        _tempDir = Path.Combine(Path.GetTempPath(), $"ModuleLoaderTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Test]
    public async Task DiscoverAndLoadAsync_EmptyDirectory_LoadsNoModules()
    {
        // Arrange
        var services = new ServiceCollection();
        var sut = new ModuleLoader(_mockLogger.Object, _mockLicense.Object, _tempDir);

        // Act
        await sut.DiscoverAndLoadAsync(services);

        // Assert
        Assert.That(sut.LoadedModules, Is.Empty);
        Assert.That(sut.FailedModules, Is.Empty);
    }

    [Test]
    public async Task DiscoverAndLoadAsync_NonExistentDirectory_CreatesDirectory()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_tempDir, "nonexistent");
        var services = new ServiceCollection();
        var sut = new ModuleLoader(_mockLogger.Object, _mockLicense.Object, nonExistentPath);

        // Act
        await sut.DiscoverAndLoadAsync(services);

        // Assert
        Assert.That(Directory.Exists(nonExistentPath), Is.True);
        Assert.That(sut.LoadedModules, Is.Empty);
    }

    [Test]
    public async Task DiscoverAndLoadAsync_CancellationRequested_ThrowsOperationCanceled()
    {
        // Arrange
        var services = new ServiceCollection();
        var sut = new ModuleLoader(_mockLogger.Object, _mockLicense.Object, _tempDir);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        Assert.ThrowsAsync<OperationCanceledException>(
            () => sut.DiscoverAndLoadAsync(services, cts.Token));
    }
}
```

### 5.2 Test: License Filtering

```csharp
[TestFixture]
[Category("Unit")]
public class ModuleLoaderLicenseTests
{
    [Test]
    public void LoadModule_RequiresHigherTier_SkipsModule()
    {
        // This test requires a compiled test module assembly
        // See integration tests for full coverage

        // Arrange
        var mockLogger = new Mock<ILogger<ModuleLoader>>();
        var mockLicense = new Mock<ILicenseContext>();
        mockLicense.Setup(x => x.GetCurrentTier()).Returns(LicenseTier.Core);

        // Verify license check is called
        var sut = new ModuleLoader(mockLogger.Object, mockLicense.Object);

        // Assert
        mockLicense.Verify(x => x.GetCurrentTier(), Times.Never);
        // GetCurrentTier is called during DiscoverAndLoadAsync, not construction
    }

    [Test]
    public void ModuleLoadFailure_LicenseReason_FormatsCorrectly()
    {
        // Arrange
        var failure = new ModuleLoadFailure(
            AssemblyPath: "/path/to/module.dll",
            ModuleName: "PremiumModule",
            FailureReason: "License tier Teams required, current tier is Core",
            Exception: null
        );

        // Act
        var result = failure.ToString();

        // Assert
        Assert.That(result, Does.Contain("PremiumModule"));
        Assert.That(result, Does.Contain("License tier Teams required"));
    }
}
```

### 5.3 Test: Module Registration

```csharp
[TestFixture]
[Category("Unit")]
public class ModuleLoaderRegistrationTests
{
    [Test]
    public async Task InitializeModulesAsync_NoModules_CompletesSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ModuleLoader>>();
        var mockLicense = new Mock<ILicenseContext>();
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDir);

        try
        {
            var sut = new ModuleLoader(mockLogger.Object, mockLicense.Object, tempDir);
            var provider = new ServiceCollection().BuildServiceProvider();

            // Act
            await sut.InitializeModulesAsync(provider);

            // Assert
            Assert.That(sut.LoadedModules, Is.Empty);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Test]
    public void LoadedModules_BeforeDiscovery_ReturnsEmptyList()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ModuleLoader>>();
        var mockLicense = new Mock<ILicenseContext>();
        var sut = new ModuleLoader(mockLogger.Object, mockLicense.Object, "/nonexistent");

        // Act
        var result = sut.LoadedModules;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void FailedModules_BeforeDiscovery_ReturnsEmptyList()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<ModuleLoader>>();
        var mockLicense = new Mock<ILicenseContext>();
        var sut = new ModuleLoader(mockLogger.Object, mockLicense.Object, "/nonexistent");

        // Act
        var result = sut.FailedModules;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }
}
```

### 5.4 Test: Error Handling

```csharp
[TestFixture]
[Category("Unit")]
public class ModuleLoaderErrorHandlingTests
{
    [Test]
    public void ModuleLoadFailure_WithException_IncludesExceptionDetails()
    {
        // Arrange
        var exception = new InvalidOperationException("Test exception");
        var failure = new ModuleLoadFailure(
            AssemblyPath: "/path/to/broken.dll",
            ModuleName: "BrokenModule",
            FailureReason: "Module instantiation failed: Test exception",
            Exception: exception
        );

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(failure.AssemblyPath, Is.EqualTo("/path/to/broken.dll"));
            Assert.That(failure.ModuleName, Is.EqualTo("BrokenModule"));
            Assert.That(failure.FailureReason, Does.Contain("Test exception"));
            Assert.That(failure.Exception, Is.SameAs(exception));
        });
    }

    [Test]
    public void ModuleLoadFailure_WithoutModuleName_FormatsCorrectly()
    {
        // Arrange
        var failure = new ModuleLoadFailure(
            AssemblyPath: "/path/to/invalid.dll",
            ModuleName: null,
            FailureReason: "Assembly load failed",
            Exception: null
        );

        // Act
        var result = failure.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("/path/to/invalid.dll: Assembly load failed"));
    }
}
```

---

## 6. Observability & Logging

| Level       | Context      | Message Template                                                                          |
| :---------- | :----------- | :---------------------------------------------------------------------------------------- |
| Information | ModuleLoader | `Starting module discovery in {ModulesPath}`                                              |
| Warning     | ModuleLoader | `Modules directory not found, creating: {ModulesPath}`                                    |
| Debug       | ModuleLoader | `Found {Count} DLL files in modules directory`                                            |
| Information | ModuleLoader | `No module DLLs found in {ModulesPath}`                                                   |
| Information | ModuleLoader | `Current license tier: {Tier}`                                                            |
| Debug       | ModuleLoader | `Processing assembly: {FileName}`                                                         |
| Debug       | ModuleLoader | `Assembly {FileName} loaded successfully`                                                 |
| Debug       | ModuleLoader | `No IModule implementations found in {FileName}`                                          |
| Debug       | ModuleLoader | `Found {Count} IModule types in {FileName}`                                               |
| Debug       | ModuleLoader | `Processing module type: {ModuleName}`                                                    |
| Warning     | ModuleLoader | `Skipping module {ModuleName} due to license restrictions...`                             |
| Warning     | ModuleLoader | `Skipping module {ModuleName} due to feature restriction...`                              |
| Information | ModuleLoader | `Loading module: {ModuleName} v{Version} by {Author}`                                     |
| Debug       | ModuleLoader | `Module {ModuleName} registered services`                                                 |
| Information | ModuleLoader | `Module {ModuleName} loaded successfully`                                                 |
| Error       | ModuleLoader | `Failed to load types from assembly {FileName}. Loader exceptions: {LoaderExceptions}`    |
| Warning     | ModuleLoader | `Skipping non-.NET assembly: {FileName}. This may be a native dependency.`                |
| Error       | ModuleLoader | `Failed to load assembly: {FileName}`                                                     |
| Error       | ModuleLoader | `Unexpected error loading assembly: {FileName}`                                           |
| Error       | ModuleLoader | `Module {ModuleName} missing parameterless constructor`                                   |
| Error       | ModuleLoader | `Failed to load module type: {ModuleName}`                                                |
| Information | ModuleLoader | `Module discovery complete in {Duration}ms. Loaded: {LoadedCount}, Failed: {FailedCount}` |
| Information | ModuleLoader | `Initializing {Count} loaded modules`                                                     |
| Debug       | ModuleLoader | `Initializing module: {ModuleName}`                                                       |
| Information | ModuleLoader | `Module {ModuleName} initialized in {Duration}ms`                                         |
| Error       | ModuleLoader | `Failed to initialize module {ModuleName} after {Duration}ms`                             |
| Information | ModuleLoader | `Module initialization complete in {Duration}ms`                                          |

---

## 7. Definition of Done

- [ ] `IModuleLoader` interface exists in `Lexichord.Abstractions.Contracts`
- [ ] `IModuleLoader.LoadedModules` property defined
- [ ] `IModuleLoader.FailedModules` property defined
- [ ] `IModuleLoader.DiscoverAndLoadAsync()` method defined
- [ ] `IModuleLoader.InitializeModulesAsync()` method defined
- [ ] `ModuleLoadFailure` record exists with all fields
- [ ] `ModuleLoadFailure.ToString()` formats correctly
- [ ] `ModuleLoader` implementation in `Lexichord.Host.Services`
- [ ] `ModuleLoader` scans `{AppDir}/Modules/*.dll`
- [ ] `ModuleLoader` uses `AssemblyLoadContext.Default.LoadFromAssemblyPath()`
- [ ] `ModuleLoader` finds types implementing `IModule` via reflection
- [ ] `ModuleLoader` checks `[RequiresLicense]` before instantiation
- [ ] `ModuleLoader` checks `FeatureCode` if specified
- [ ] `ModuleLoader` calls `RegisterServices()` for authorized modules
- [ ] `ModuleLoader` calls `InitializeAsync()` after DI build
- [ ] `ModuleLoader` handles `ReflectionTypeLoadException` gracefully
- [ ] `ModuleLoader` handles `BadImageFormatException` gracefully
- [ ] `ModuleLoader` handles `MissingMethodException` gracefully
- [ ] `ModuleLoader` continues on module failure (fail-safe)
- [ ] `App.axaml.cs` integrates ModuleLoader during startup
- [ ] All unit tests passing
- [ ] All log messages use structured templates

---

## 8. Verification Commands

```bash
# Build Host to verify compilation
dotnet build src/Lexichord.Host

# Verify ModuleLoader exists
grep -r "public sealed class ModuleLoader" src/Lexichord.Host/

# Verify IModuleLoader interface exists
grep -r "public interface IModuleLoader" src/Lexichord.Abstractions/

# Create empty Modules directory and run
mkdir -p bin/Debug/net9.0/Modules
dotnet run --project src/Lexichord.Host

# Check logs for module discovery messages:
# - "Starting module discovery in..."
# - "No module DLLs found in..."

# Run unit tests
dotnet test --filter "FullyQualifiedName~ModuleLoaderDiscoveryTests"
dotnet test --filter "FullyQualifiedName~ModuleLoaderLicenseTests"
dotnet test --filter "FullyQualifiedName~ModuleLoaderRegistrationTests"
dotnet test --filter "FullyQualifiedName~ModuleLoaderErrorHandlingTests"
```
