using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Settings.Pages;

/// <summary>
/// Settings page for update channel and version management.
/// </summary>
/// <remarks>
/// LOGIC: Implements ISettingsPage to provide the Updates category in the Settings dialog.
/// Uses DI to resolve the ViewModel for the updates settings view.
///
/// Version: v0.1.6d
/// </remarks>
public sealed class UpdatesSettingsPage : ISettingsPage
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdatesSettingsPage"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public UpdatesSettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public string CategoryId => "updates";

    /// <inheritdoc/>
    public string DisplayName => "Updates";

    /// <inheritdoc/>
    public string? ParentCategoryId => null; // Root category

    /// <inheritdoc/>
    public string? Icon => "Update";

    /// <inheritdoc/>
    public int SortOrder => 2; // After Account (0) and Appearance (1)

    /// <inheritdoc/>
    public LicenseTier RequiredTier => LicenseTier.Core;

    /// <inheritdoc/>
    public IReadOnlyList<string> SearchKeywords =>
    [
        "update", "version", "channel", "stable", "insider",
        "beta", "release", "upgrade", "check"
    ];

    /// <inheritdoc/>
    public object CreateView()
    {
        var viewModel = _serviceProvider.GetRequiredService<UpdatesSettingsViewModel>();
        return new UpdatesSettingsView { DataContext = viewModel };
    }
}
