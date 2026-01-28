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
