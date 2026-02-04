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

        // LOGIC: Register assembly resolver to find module dependencies in Modules directory.
        // v0.6.3b: When modules are loaded, the CLR needs to find their NuGet dependencies.
        // Dependencies are copied to the Modules directory alongside the module DLLs.
        AssemblyLoadContext.Default.Resolving += OnAssemblyResolving;
    }

    /// <summary>
    /// Handles assembly resolution for module dependencies.
    /// </summary>
    /// <remarks>
    /// LOGIC: When the CLR fails to find an assembly, this handler looks in the Modules
    /// directory. This allows module dependencies (NuGet packages) to be loaded from
    /// the same location as the modules themselves.
    /// </remarks>
    private Assembly? OnAssemblyResolving(AssemblyLoadContext context, AssemblyName assemblyName)
    {
        // LOGIC: Look for the assembly in the Modules directory.
        var assemblyPath = Path.Combine(_modulesPath, $"{assemblyName.Name}.dll");

        if (File.Exists(assemblyPath))
        {
            // NOTE: Do NOT log here - logging can trigger assembly resolution which
            // causes re-entrancy and deadlocks.
            return context.LoadFromAssemblyPath(assemblyPath);
        }

        // LOGIC: Assembly not found in Modules directory, let default resolution continue.
        return null;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IModule> LoadedModules => _loadedModules.AsReadOnly();

    /// <inheritdoc/>
    public IReadOnlyList<ModuleLoadFailure> FailedModules => _failedModules.AsReadOnly();

    /// <inheritdoc/>
    public Task DiscoverAndLoadAsync(
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
            return Task.CompletedTask;
        }

        // LOGIC: Only load Lexichord.Modules.*.dll files, not dependency DLLs.
        // v0.6.3b: Dependencies are copied to Modules/ for assembly resolution,
        // but only actual module DLLs should be scanned for IModule implementations.
        var dllFiles = Directory.GetFiles(_modulesPath, "Lexichord.Modules.*.dll");
        _logger.LogDebug("Found {Count} module DLL files in modules directory", dllFiles.Length);

        if (dllFiles.Length == 0)
        {
            _logger.LogInformation("No module DLLs found in {ModulesPath}", _modulesPath);
            return Task.CompletedTask;
        }

        var currentTier = _licenseContext.GetCurrentTier();
        _logger.LogInformation("Current license tier: {Tier}", currentTier);

        foreach (var dllPath in dllFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadModuleFromAssembly(dllPath, services, currentTier);
        }

        stopwatch.Stop();
        _logger.LogInformation(
            "Module discovery complete in {Duration}ms. Loaded: {LoadedCount}, Failed: {FailedCount}",
            stopwatch.ElapsedMilliseconds, _loadedModules.Count, _failedModules.Count);

        return Task.CompletedTask;
    }

    private void LoadModuleFromAssembly(
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
                LoadModuleType(moduleType, services, currentTier, dllPath);
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
        catch (BadImageFormatException)
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

    private void LoadModuleType(
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

        for (var i = 0; i < _loadedModules.Count; i++)
        {
            var module = _loadedModules[i];
            cancellationToken.ThrowIfCancellationRequested();

            var moduleStopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("Initializing module {Index}/{Total}: {ModuleName}",
                    i + 1, _loadedModules.Count, module.Info.Name);
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
