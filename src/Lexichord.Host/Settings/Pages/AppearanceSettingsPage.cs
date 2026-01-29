using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Settings.Pages;

/// <summary>
/// Settings page for appearance preferences (theme selection).
/// </summary>
/// <remarks>
/// LOGIC: Implements ISettingsPage to provide the Appearance category in the Settings dialog.
/// Uses DI to resolve the ViewModel for the appearance settings view.
///
/// Version: v0.1.6b
/// </remarks>
public sealed class AppearanceSettingsPage : ISettingsPage
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppearanceSettingsPage"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public AppearanceSettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public string CategoryId => "appearance";

    /// <inheritdoc/>
    public string DisplayName => "Appearance";

    /// <inheritdoc/>
    public string? ParentCategoryId => null;

    /// <inheritdoc/>
    public string? Icon => "ColorPalette";

    /// <inheritdoc/>
    public int SortOrder => 10;

    /// <inheritdoc/>
    public LicenseTier RequiredTier => LicenseTier.Core;

    /// <inheritdoc/>
    public IReadOnlyList<string> SearchKeywords => ["theme", "dark", "light", "color", "mode", "system"];

    /// <inheritdoc/>
    public object CreateView()
    {
        var viewModel = _serviceProvider.GetRequiredService<AppearanceSettingsViewModel>();
        return new AppearanceSettingsView { DataContext = viewModel };
    }
}
