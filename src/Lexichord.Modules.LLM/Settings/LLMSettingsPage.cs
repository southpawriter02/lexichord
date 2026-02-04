// -----------------------------------------------------------------------
// <copyright file="LLMSettingsPage.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.LLM.Presentation.ViewModels;
using Lexichord.Modules.LLM.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace Lexichord.Modules.LLM.Settings;

/// <summary>
/// Settings page for LLM provider configuration.
/// </summary>
/// <remarks>
/// <para>
/// Implements <see cref="ISettingsPage"/> to provide the LLM Providers category
/// in the Settings dialog. This page allows users to:
/// </para>
/// <list type="bullet">
///   <item><description>View available LLM providers</description></item>
///   <item><description>Configure API keys for each provider</description></item>
///   <item><description>Test provider connections</description></item>
///   <item><description>Select a default provider</description></item>
/// </list>
/// <para>
/// <b>License Gating:</b> The settings page is visible to all users (Core tier),
/// but API key configuration requires WriterPro or higher.
/// </para>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public sealed class LLMSettingsPage : ISettingsPage
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="LLMSettingsPage"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider for resolving dependencies.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="serviceProvider"/> is null.
    /// </exception>
    public LLMSettingsPage(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Uses "llm.providers" to indicate this is an LLM-related category.
    /// Future sub-pages might include "llm.models", "llm.advanced", etc.
    /// </remarks>
    public string CategoryId => "llm.providers";

    /// <inheritdoc/>
    /// <remarks>
    /// Display name shown in the settings navigation tree.
    /// </remarks>
    public string DisplayName => "LLM Providers";

    /// <inheritdoc/>
    /// <remarks>
    /// Root-level category (no parent). In a future version, this could be
    /// nested under an "AI & Integrations" parent category.
    /// </remarks>
    public string? ParentCategoryId => null;

    /// <inheritdoc/>
    /// <remarks>
    /// Robot icon represents AI/LLM functionality. Falls back to a default
    /// icon if "Robot" is not available in the icon set.
    /// </remarks>
    public string? Icon => "Robot";

    /// <inheritdoc/>
    /// <remarks>
    /// Sorted after Account (0) and Appearance (50) but before Editor (100).
    /// This places LLM settings in a prominent position since it's a key feature.
    /// </remarks>
    public int SortOrder => 75;

    /// <inheritdoc/>
    /// <remarks>
    /// The page is visible to all users (Core tier). However, configuration
    /// features (saving API keys, setting default) require WriterPro+.
    /// This allows Core users to see what features are available with an upgrade.
    /// </remarks>
    public LicenseTier RequiredTier => LicenseTier.Core;

    /// <inheritdoc/>
    /// <remarks>
    /// Keywords for settings search functionality.
    /// </remarks>
    public IReadOnlyList<string> SearchKeywords =>
    [
        "llm", "ai", "api", "key", "openai", "anthropic", "gpt", "claude",
        "provider", "chat", "completion", "model", "language model"
    ];

    /// <inheritdoc/>
    /// <remarks>
    /// <para>
    /// Creates a new instance of the LLM settings view with its ViewModel.
    /// The ViewModel is resolved from DI and automatically loads providers
    /// when the view is displayed.
    /// </para>
    /// <para>
    /// A new instance is created each time to ensure proper lifecycle management.
    /// </para>
    /// </remarks>
    public object CreateView()
    {
        var viewModel = _serviceProvider.GetRequiredService<LLMSettingsViewModel>();
        return new LLMSettingsView { DataContext = viewModel };
    }
}
