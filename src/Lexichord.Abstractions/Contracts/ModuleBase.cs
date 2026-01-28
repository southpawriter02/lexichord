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
