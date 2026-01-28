using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using Lexichord.Host.Infrastructure.Behaviors;
using Lexichord.Host.Infrastructure.Options;

namespace Lexichord.Host.Infrastructure;

/// <summary>
/// Extension methods for registering MediatR services.
/// </summary>
/// <remarks>
/// LOGIC: This class centralizes all MediatR configuration. The design goals are:
///
/// 1. **Single Point of Configuration**: All MediatR setup in one place.
/// 2. **Assembly Scanning**: Automatically discover handlers from modules.
/// 3. **Pipeline Behaviors**: Register behaviors in correct execution order.
/// 4. **Extensibility**: Accept additional assemblies for module support.
/// </remarks>
public static class MediatRServiceExtensions
{
    /// <summary>
    /// Adds MediatR services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="moduleAssemblies">Additional assemblies to scan for handlers.</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// LOGIC: Assembly scanning order:
    /// 1. Lexichord.Host (always included - contains infrastructure handlers)
    /// 2. Module assemblies (passed as parameters)
    ///
    /// Pipeline behavior registration order determines execution order:
    /// - First registered = outermost in pipeline
    /// - Last registered = closest to handler
    ///
    /// For v0.0.7a, we register MediatR only. Pipeline behaviors are added in
    /// v0.0.7c (LoggingBehavior) and v0.0.7d (ValidationBehavior).
    /// </remarks>
    public static IServiceCollection AddMediatRServices(
        this IServiceCollection services,
        params Assembly[] moduleAssemblies)
    {
        // Configure logging behavior options from configuration
        services.AddOptions<LoggingBehaviorOptions>()
            .BindConfiguration(LoggingBehaviorOptions.SectionName);

        // LOGIC: Collect all assemblies to scan for handlers
        var assembliesToScan = new List<Assembly>
        {
            // Always include Host assembly
            typeof(MediatRServiceExtensions).Assembly
        };

        // Add module assemblies
        assembliesToScan.AddRange(moduleAssemblies);

        // Register MediatR
        services.AddMediatR(configuration =>
        {
            // Scan assemblies for handlers
            configuration.RegisterServicesFromAssemblies(assembliesToScan.ToArray());

            // LOGIC: Pipeline behaviors execute in registration order
            // LoggingBehavior is FIRST (outermost) to capture total duration
            configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            // v0.0.7d: configuration.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        return services;
    }

    /// <summary>
    /// Adds MediatR services with automatic module assembly discovery.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="moduleDirectory">Directory containing module assemblies.</param>
    /// <param name="modulePattern">Glob pattern for module DLLs (default: "Lexichord.Module.*.dll").</param>
    /// <returns>The service collection for method chaining.</returns>
    /// <remarks>
    /// LOGIC: This overload discovers module assemblies dynamically at startup.
    /// Useful when modules are loaded as plugins from a directory.
    ///
    /// NOTE: This is for future use with the plugin system (v0.0.8+).
    /// </remarks>
    public static IServiceCollection AddMediatRServicesWithDiscovery(
        this IServiceCollection services,
        string moduleDirectory,
        string modulePattern = "Lexichord.Module.*.dll")
    {
        var moduleAssemblies = new List<Assembly>();

        if (Directory.Exists(moduleDirectory))
        {
            var dllFiles = Directory.GetFiles(moduleDirectory, modulePattern);

            foreach (var dllPath in dllFiles)
            {
                try
                {
                    var assembly = Assembly.LoadFrom(dllPath);
                    moduleAssemblies.Add(assembly);
                }
                catch (Exception)
                {
                    // LOGIC: Log warning but continue - one bad module shouldn't
                    // prevent others from loading
                }
            }
        }

        return services.AddMediatRServices(moduleAssemblies.ToArray());
    }
}
