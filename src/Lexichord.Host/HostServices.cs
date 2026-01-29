using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Infrastructure;
using Lexichord.Host.Services;
using System;
using System.Collections.Generic;

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
    /// Builds the application configuration from multiple sources.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The built configuration.</returns>
    /// <remarks>
    /// LOGIC: Configuration sources are loaded in order of increasing precedence:
    /// 1. appsettings.json (base settings, always present)
    /// 2. appsettings.{Environment}.json (environment overrides, optional)
    /// 3. Environment variables (deployment overrides, LEXICHORD_ prefix)
    /// 4. Command-line arguments (runtime overrides, highest priority)
    ///
    /// Environment is determined by:
    /// 1. LEXICHORD_ENVIRONMENT environment variable (preferred)
    /// 2. DOTNET_ENVIRONMENT environment variable (fallback)
    /// 3. "Production" (default)
    /// </remarks>
    public static IConfiguration BuildConfiguration(string[] args)
    {
        // LOGIC: Determine environment from multiple sources
        var environment = Environment.GetEnvironmentVariable("LEXICHORD_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? "Production";

        return new ConfigurationBuilder()
            // Set base path to application directory (where exe lives)
            .SetBasePath(AppContext.BaseDirectory)

            // 1. Base configuration (required)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)

            // 2. Environment-specific configuration (optional)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: true)

            // 3. Environment variables with LEXICHORD_ prefix
            // e.g., LEXICHORD_DEBUGMODE=true becomes Lexichord:DebugMode
            .AddEnvironmentVariables(prefix: "LEXICHORD_")

            // 4. Command-line arguments (highest precedence)
            .AddCommandLine(args, new Dictionary<string, string>
            {
                // Map CLI switches to configuration keys
                { "--debug-mode", "Lexichord:DebugMode" },
                { "-d", "Lexichord:DebugMode" },
                { "--log-level", "Serilog:MinimumLevel:Default" },
                { "-l", "Serilog:MinimumLevel:Default" },
                { "--data-path", "Lexichord:DataPath" },
                { "--environment", "Lexichord:Environment" },
                { "-e", "Lexichord:Environment" },
                { "--show-devtools", "Debug:ShowDevTools" }
            })
            .Build();
    }

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
        // LOGIC: Register configuration as singleton for raw access
        services.AddSingleton<IConfiguration>(configuration);

        // LOGIC: Register configuration options using the Options pattern
        // This enables strongly-typed access to configuration sections
        services.Configure<LexichordOptions>(
            configuration.GetSection(LexichordOptions.SectionName));
        services.Configure<DebugOptions>(
            configuration.GetSection(DebugOptions.SectionName));
        services.Configure<FeatureFlagOptions>(
            configuration.GetSection(FeatureFlagOptions.SectionName));

        // LOGIC: Register Serilog as the ILogger<T> implementation
        // This enables constructor injection of ILogger<T> in all services
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(dispose: true);
        });

        // LOGIC (v0.0.7a): Register MediatR for in-process messaging
        // This enables loose coupling between modules via commands, queries, and events
        services.AddMediatRServices();

        // LOGIC: Register core Host services as Singletons
        // These services maintain state across the application lifetime
        services.AddSingleton<IThemeManager, ThemeManager>();
        services.AddSingleton<IWindowStateService, WindowStateService>();
        services.AddSingleton<ICrashReportService, CrashReportService>();

        // LOGIC: Register license context as singleton
        // License state is application-wide and should not change per-request
        // v0.0.4c: Stub implementation returning Core tier
        // v1.x: Will be replaced with real license validation
        services.AddSingleton<ILicenseContext, HardcodedLicenseContext>();

        // LOGIC (v0.0.8a): Register shell region manager for module views
        // Manages views contributed by modules to shell regions (Top, Left, Center, Right, Bottom)
        services.AddSingleton<IShellRegionManager, ShellRegionManager>();

        // LOGIC: Register service locator for XAML-instantiated components
        // Marked as obsolete to discourage direct usage
        #pragma warning disable CS0618 // Intentionally using obsolete interface
        services.AddSingleton<IServiceLocator>(sp => new ServiceLocator(sp));
        #pragma warning restore CS0618

        return services;
    }
}

