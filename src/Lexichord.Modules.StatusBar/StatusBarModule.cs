using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.StatusBar.Services;
using Lexichord.Modules.StatusBar.ViewModels;
using Lexichord.Modules.StatusBar.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar;

/// <summary>
/// The StatusBar module - canonical reference implementation for Lexichord modules.
/// </summary>
/// <remarks>
/// LOGIC: This module demonstrates the complete module lifecycle:
/// 1. ModuleLoader discovers StatusBar.dll in ./Modules/
/// 2. Reflection finds StatusBarModule implementing IModule
/// 3. RegisterServices() is called before DI container is built
/// 4. InitializeAsync() is called after DI container is built
///
/// IMPORTANT: This module must NOT reference Lexichord.Host.
/// All interaction with Host is through Abstractions interfaces.
/// </remarks>
public class StatusBarModule : IModule
{
    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "statusbar",
        Name: "Status Bar",
        Version: new Version(0, 0, 8),
        Author: "Lexichord Team",
        Description: "System status bar displaying health, vault, and event status"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This method is called BEFORE the ServiceProvider is built.
    /// Register all module services here. Do NOT resolve services.
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // Views - Transient because each request gets a new instance
        services.AddTransient<StatusBarView>();
        services.AddTransient<ApiKeyDialog>();

        // ViewModels - Transient to match their views
        services.AddTransient<StatusBarViewModel>();
        services.AddTransient<ApiKeyDialogViewModel>();

        // Services - Singleton for shared state
        services.AddSingleton<IHealthRepository, HealthRepository>();
        services.AddSingleton<IHeartbeatService, HeartbeatService>();
        services.AddSingleton<IVaultStatusService, VaultStatusService>();

        // Shell Region registration
        // LOGIC: This registers the StatusBar view for the Bottom region.
        // The ShellRegionManager will resolve this and add it to MainWindow.
        services.AddSingleton<IShellRegionView, StatusBarRegionView>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This method is called AFTER the ServiceProvider is built.
    /// Resolve services and perform async initialization here.
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<StatusBarModule>>();
        logger.LogInformation("Initializing {ModuleName} module", Info.Name);

        try
        {
            // Initialize database (v0.0.8b)
            await InitializeDatabaseAsync(provider, logger);

            // Start heartbeat service (v0.0.8b)
            StartHeartbeatService(provider, logger);

            // Verify vault status (v0.0.8c)
            await VerifyVaultStatusAsync(provider, logger);

            logger.LogInformation("{ModuleName} module initialized successfully", Info.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize {ModuleName} module", Info.Name);
            // Don't rethrow - allow application to continue with degraded status bar
        }
    }

    private static async Task InitializeDatabaseAsync(
        IServiceProvider provider,
        ILogger logger)
    {
        var healthRepo = provider.GetRequiredService<IHealthRepository>();
        await healthRepo.RecordStartupAsync();

        var version = await healthRepo.GetDatabaseVersionAsync();
        logger.LogDebug("Database version: {Version}", version);
    }

    private static void StartHeartbeatService(
        IServiceProvider provider,
        ILogger logger)
    {
        var heartbeat = provider.GetRequiredService<IHeartbeatService>();
        heartbeat.Start();
        logger.LogInformation("Heartbeat service started with {Interval} interval",
            heartbeat.Interval);
    }

    private static async Task VerifyVaultStatusAsync(
        IServiceProvider provider,
        ILogger logger)
    {
        var vaultService = provider.GetRequiredService<IVaultStatusService>();
        var status = await vaultService.GetVaultStatusAsync();
        logger.LogInformation("Vault status: {Status}", status);
    }
}
