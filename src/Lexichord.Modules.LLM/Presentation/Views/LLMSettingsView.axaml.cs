// -----------------------------------------------------------------------
// <copyright file="LLMSettingsView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;
using Avalonia.Interactivity;
using Lexichord.Modules.LLM.Presentation.ViewModels;

namespace Lexichord.Modules.LLM.Presentation.Views;

/// <summary>
/// View for the LLM Settings page.
/// </summary>
/// <remarks>
/// <para>
/// This view displays the LLM provider configuration interface, allowing users to:
/// </para>
/// <list type="bullet">
///   <item><description>View all available LLM providers</description></item>
///   <item><description>Configure API keys for each provider</description></item>
///   <item><description>Test provider connections</description></item>
///   <item><description>Set a default provider</description></item>
/// </list>
/// <para>
/// The view automatically loads providers when it becomes visible.
/// </para>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public partial class LLMSettingsView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LLMSettingsView"/> class.
    /// </summary>
    public LLMSettingsView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Load providers when the view is displayed
        if (DataContext is LLMSettingsViewModel viewModel)
        {
            await viewModel.LoadProvidersAsync();
        }
    }

    /// <inheritdoc/>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        // Dispose ViewModel when view is unloaded
        if (DataContext is LLMSettingsViewModel viewModel)
        {
            viewModel.Dispose();
        }
    }
}
