// <copyright file="UpgradePromptDialog.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Diagnostics;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for UpgradePromptDialog window.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1d - Modal dialog that displays a feature comparison table
/// and prompts the user to upgrade their license tier.
///
/// Features:
/// - Shows current tier status
/// - Displays Core vs Writer Pro feature comparison
/// - "Upgrade Now" opens external URL to purchase page
/// - "Maybe Later" dismisses the dialog
///
/// Version: v0.3.1d
/// </remarks>
public partial class UpgradePromptDialog : Window
{
    private static ILicenseContext? _licenseContext;

    /// <summary>
    /// The URL to the upgrade/pricing page.
    /// </summary>
    private const string UpgradeUrl = "https://lexichord.app/pricing";

    /// <summary>
    /// Defines the RequestedFeature styled property.
    /// </summary>
    public static readonly StyledProperty<string?> RequestedFeatureProperty =
        AvaloniaProperty.Register<UpgradePromptDialog, string?>(nameof(RequestedFeature));

    /// <summary>
    /// Defines the CurrentTierText direct property.
    /// </summary>
    public static readonly DirectProperty<UpgradePromptDialog, string> CurrentTierTextProperty =
        AvaloniaProperty.RegisterDirect<UpgradePromptDialog, string>(
            nameof(CurrentTierText),
            o => o.CurrentTierText);

    private string _currentTierText = "Current: Core Edition";

    /// <summary>
    /// Initializes a new instance of the <see cref="UpgradePromptDialog"/> class.
    /// </summary>
    public UpgradePromptDialog()
    {
        InitializeComponent();
        UpdateCurrentTierText();
    }

    /// <summary>
    /// Initializes the license context for upgrade prompts.
    /// </summary>
    /// <param name="licenseContext">The license context to use.</param>
    public static void Initialize(ILicenseContext licenseContext)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
    }

    /// <summary>
    /// Gets or sets the feature that triggered this upgrade prompt.
    /// </summary>
    public string? RequestedFeature
    {
        get => GetValue(RequestedFeatureProperty);
        set => SetValue(RequestedFeatureProperty, value);
    }

    /// <summary>
    /// Gets the text describing the current license tier.
    /// </summary>
    public string CurrentTierText
    {
        get => _currentTierText;
        private set => SetAndRaise(CurrentTierTextProperty, ref _currentTierText, value);
    }

    /// <summary>
    /// Shows the upgrade dialog as modal.
    /// </summary>
    /// <param name="owner">The owner window.</param>
    /// <param name="requestedFeature">Optional feature code that triggered the dialog.</param>
    /// <returns>A task that completes when the dialog is closed.</returns>
    public static async Task ShowAsync(Window owner, string? requestedFeature = null)
    {
        var dialog = new UpgradePromptDialog
        {
            RequestedFeature = requestedFeature
        };

        await dialog.ShowDialog(owner);
    }

    private void UpdateCurrentTierText()
    {
        var tier = _licenseContext?.GetCurrentTier() ?? LicenseTier.Core;
        CurrentTierText = tier switch
        {
            LicenseTier.Core => "Current: Core Edition (Free)",
            LicenseTier.WriterPro => "Current: Writer Pro",
            LicenseTier.Teams => "Current: Teams",
            LicenseTier.Enterprise => "Current: Enterprise",
            _ => "Current: Unknown"
        };
    }

    private void OnUpgradeNowClicked(object? sender, RoutedEventArgs e)
    {
        // LOGIC: Open the upgrade URL in the default browser
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = UpgradeUrl,
                UseShellExecute = true
            });
        }
        catch
        {
            // LOGIC: Ignore failures to open URL - user can navigate manually
        }

        Close();
    }

    private void OnMaybeLaterClicked(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
