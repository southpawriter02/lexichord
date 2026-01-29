using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Workspace.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Workspace;

/// <summary>
/// Module for workspace management functionality.
/// </summary>
/// <remarks>
/// LOGIC: WorkspaceModule is discovered and loaded by the Host's ModuleLoader.
/// It demonstrates the module pattern:
/// - Project references only Lexichord.Abstractions
/// - Implements IModule interface
/// - Registers services during RegisterServices phase
/// - Registers views during InitializeAsync phase
///
/// Output: ./Modules/Lexichord.Modules.Workspace.dll
/// </remarks>
[RequiresLicense(LicenseTier.Core)]
public sealed class WorkspaceModule : IModule
{
    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "workspace",
        Name: "Workspace",
        Version: new Version(0, 1, 2),
        Author: "Lexichord Team",
        Description: "Project explorer and workspace management for Lexichord"
    );

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This method is called BEFORE the ServiceProvider is built.
    /// Register all module services here. Do NOT resolve services.
    /// </remarks>
    public void RegisterServices(IServiceCollection services)
    {
        // Workspace state management
        services.AddSingleton<IWorkspaceService, WorkspaceService>();

        // File system watcher (robust implementation from v0.1.2b)
        services.AddSingleton<IFileSystemWatcher, RobustFileSystemWatcher>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: This method is called AFTER the ServiceProvider is built.
    /// Resolve services and perform async initialization here.
    /// </remarks>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        var logger = provider.GetRequiredService<ILogger<WorkspaceModule>>();
        logger.LogInformation("Initializing {ModuleName} module v{Version}", Info.Name, Info.Version);

        try
        {
            // Verify workspace service is resolvable
            var workspaceService = provider.GetRequiredService<IWorkspaceService>();
            logger.LogDebug("WorkspaceService resolved successfully, IsWorkspaceOpen: {IsOpen}",
                workspaceService.IsWorkspaceOpen);

            // NOTE: ProjectExplorerView registration will be added in v0.1.2c
            // when the view and view model are implemented

            logger.LogInformation("{ModuleName} module initialized successfully", Info.Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize {ModuleName} module", Info.Name);
            throw;
        }

        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task ShutdownAsync()
    {
        // Services with IDisposable are cleaned up by DI container
        await Task.CompletedTask;
    }
}
