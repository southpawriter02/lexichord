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
        await Task.Delay(10);

        logger.LogInformation(
            "Sandbox module initialized successfully. " +
            "ISandboxService is ready for resolution.");
    }
}
