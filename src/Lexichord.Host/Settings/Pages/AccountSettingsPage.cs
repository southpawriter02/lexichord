using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Host.Settings.Pages;

/// <summary>
/// Settings page for account and license management.
/// </summary>
/// <remarks>
/// LOGIC: Implements ISettingsPage to provide the Account category in the Settings dialog.
/// Uses DI to resolve the ViewModel for the account settings view.
/// 
/// Version: v0.1.6c
/// </remarks>
public sealed class AccountSettingsPage : ISettingsPage
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountSettingsPage"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    public AccountSettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    public string CategoryId => "account";

    /// <inheritdoc/>
    public string DisplayName => "Account";

    /// <inheritdoc/>
    public string? ParentCategoryId => null; // Root category

    /// <inheritdoc/>
    public string? Icon => "Person";

    /// <inheritdoc/>
    public int SortOrder => 0; // First in list (before Appearance)

    /// <inheritdoc/>
    public LicenseTier RequiredTier => LicenseTier.Core;

    /// <inheritdoc/>
    public IReadOnlyList<string> SearchKeywords =>
    [
        "license", "key", "activate", "tier", "subscription",
        "account", "upgrade", "deactivate", "writerpro", "enterprise"
    ];

    /// <inheritdoc/>
    public object CreateView()
    {
        var viewModel = _serviceProvider.GetRequiredService<AccountSettingsViewModel>();
        return new AccountSettingsView { DataContext = viewModel };
    }
}
