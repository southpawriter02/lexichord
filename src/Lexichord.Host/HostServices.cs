using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

namespace Lexichord.Host;

/// <summary>
/// Static helper for configuring Host services and DI container.
/// </summary>
/// <remarks>
/// LOGIC: All service registration is centralized here to provide a single source of truth
/// for the application's dependency graph. This enables:
/// - Easy testing via service replacement
/// - Clear visibility of dependencies
/// - Module integration in v0.0.4
/// </remarks>
public static class HostServices
{
    /// <summary>
    /// Configures all Host services in the DI container.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The configured service collection for chaining.</returns>
    /// <example>
    /// <code>
    /// var services = new ServiceCollection();
    /// services.ConfigureServices(configuration);
    /// var provider = services.BuildServiceProvider();
    /// </code>
    /// </example>
    public static IServiceCollection ConfigureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // LOGIC: Register Serilog as the ILogger<T> implementation
        // This enables constructor injection of ILogger<T> in all services
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // LOGIC: Register core Host services as Singletons
        // These services maintain state across the application lifetime
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IWindowStateService, WindowStateService>();
        services.AddSingleton<ICrashReportService, CrashReportService>();

        // LOGIC: Register service locator for XAML-instantiated components
        // Marked as obsolete to discourage direct usage
        #pragma warning disable CS0618 // Intentionally using obsolete interface
        services.AddSingleton<IServiceLocator>(sp => new ServiceLocator(sp));
        #pragma warning restore CS0618

        return services;
    }
}
