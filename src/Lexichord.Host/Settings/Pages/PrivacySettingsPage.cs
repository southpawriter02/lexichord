using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Settings.Pages;

/// <summary>
/// Settings page for privacy preferences (telemetry and crash reporting).
/// </summary>
/// <remarks>
/// LOGIC: Implements ISettingsPage to provide the Privacy category in the Settings dialog.
/// Uses DI to resolve the ViewModel for the privacy settings view.
///
/// Version: v0.1.7d
/// </remarks>
public sealed class PrivacySettingsPage : ISettingsPage
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="PrivacySettingsPage"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public PrivacySettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public string CategoryId => "privacy";

    /// <inheritdoc/>
    public string DisplayName => "Privacy";

    /// <inheritdoc/>
    public string? ParentCategoryId => null;

    /// <inheritdoc/>
    public string? Icon => "Shield";

    /// <inheritdoc/>
    public int SortOrder => 50;

    /// <inheritdoc/>
    public LicenseTier RequiredTier => LicenseTier.Core;

    /// <inheritdoc/>
    public IReadOnlyList<string> SearchKeywords => ["privacy", "telemetry", "crash", "reporting", "data", "analytics"];

    /// <inheritdoc/>
    public object CreateView()
    {
        var viewModel = _serviceProvider.GetRequiredService<PrivacySettingsViewModel>();
        return new PrivacySettingsView { DataContext = viewModel };
    }
}
