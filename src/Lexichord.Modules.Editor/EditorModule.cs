using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.Editor.Services;
using Lexichord.Modules.Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor;

/// <summary>
/// Module registration for the Editor module.
/// </summary>
/// <remarks>
/// LOGIC: Registers all editor-related services and views.
/// The Editor module provides:
/// - High-performance text editing via AvaloniaEdit
/// - Document lifecycle management
/// - Syntax highlighting (v0.1.3b)
/// - Search and replace (v0.1.3c)
/// - Editor configuration (v0.1.3d)
/// </remarks>
public class EditorModule : IModule
{
    private ILogger<EditorModule>? _logger;

    /// <inheritdoc/>
    public ModuleInfo Info => new(
        Id: "editor",
        Name: "Editor",
        Version: new Version(0, 1, 3),
        Author: "Lexichord Team",
        Description: "High-performance text editor with syntax highlighting"
    );

    /// <inheritdoc/>
    public void RegisterServices(IServiceCollection services)
    {
        // LOGIC: Register editor services as singletons
        services.AddSingleton<IEditorConfigurationService, EditorConfigurationService>();
        services.AddSingleton<IEditorService, EditorService>();

        // LOGIC: Register views and view models as transient (new instance per document)
        services.AddTransient<ManuscriptViewModel>();
    }

    /// <inheritdoc/>
    public async Task InitializeAsync(IServiceProvider provider)
    {
        _logger = provider.GetRequiredService<ILogger<EditorModule>>();
        _logger.LogInformation("Initializing Editor module v{Version}", Info.Version);

        // LOGIC: Load editor configuration
        var configService = provider.GetRequiredService<IEditorConfigurationService>();
        await configService.LoadSettingsAsync();

        _logger.LogInformation("Editor module initialized successfully");
    }
}
