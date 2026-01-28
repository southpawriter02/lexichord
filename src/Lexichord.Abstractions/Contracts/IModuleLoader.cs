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
