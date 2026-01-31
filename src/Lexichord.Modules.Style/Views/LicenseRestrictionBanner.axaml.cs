// <copyright file="LicenseRestrictionBanner.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for LicenseRestrictionBanner user control.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1d - Displays an informational banner when premium features
/// require a license upgrade. The banner can be dismissed and shows
/// an "Upgrade" button to trigger the upgrade flow.
///
/// Version: v0.3.1d
/// </remarks>
public partial class LicenseRestrictionBanner : UserControl
{
    /// <summary>
    /// Defines the Message styled property.
    /// </summary>
    public static readonly StyledProperty<string> MessageProperty =
        AvaloniaProperty.Register<LicenseRestrictionBanner, string>(
            nameof(Message),
            "This feature requires a Writer Pro license.");

    /// <summary>
    /// Defines the RequiredTier styled property.
    /// </summary>
    public static readonly StyledProperty<LicenseTier> RequiredTierProperty =
        AvaloniaProperty.Register<LicenseRestrictionBanner, LicenseTier>(
            nameof(RequiredTier),
            LicenseTier.WriterPro);

    /// <summary>
    /// Defines the FeatureCode styled property.
    /// </summary>
    public static readonly StyledProperty<string?> FeatureCodeProperty =
        AvaloniaProperty.Register<LicenseRestrictionBanner, string?>(nameof(FeatureCode));

    /// <summary>
    /// Defines the IsDismissed styled property.
    /// </summary>
    public static readonly StyledProperty<bool> IsDismissedProperty =
        AvaloniaProperty.Register<LicenseRestrictionBanner, bool>(nameof(IsDismissed));

    /// <summary>
    /// Routed event raised when user clicks the Upgrade button.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> UpgradeRequestedEvent =
        RoutedEvent.Register<LicenseRestrictionBanner, RoutedEventArgs>(
            nameof(UpgradeRequested),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Routed event raised when user dismisses the banner.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> DismissedEvent =
        RoutedEvent.Register<LicenseRestrictionBanner, RoutedEventArgs>(
            nameof(Dismissed),
            RoutingStrategies.Bubble);

    /// <summary>
    /// Initializes a new instance of the <see cref="LicenseRestrictionBanner"/> class.
    /// </summary>
    public LicenseRestrictionBanner()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the message displayed in the banner.
    /// </summary>
    public string Message
    {
        get => GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the required license tier.
    /// </summary>
    public LicenseTier RequiredTier
    {
        get => GetValue(RequiredTierProperty);
        set => SetValue(RequiredTierProperty, value);
    }

    /// <summary>
    /// Gets or sets the feature code for tracking purposes.
    /// </summary>
    public string? FeatureCode
    {
        get => GetValue(FeatureCodeProperty);
        set => SetValue(FeatureCodeProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the banner has been dismissed.
    /// </summary>
    public bool IsDismissed
    {
        get => GetValue(IsDismissedProperty);
        set => SetValue(IsDismissedProperty, value);
    }

    /// <summary>
    /// Event raised when user clicks the Upgrade button.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? UpgradeRequested
    {
        add => AddHandler(UpgradeRequestedEvent, value);
        remove => RemoveHandler(UpgradeRequestedEvent, value);
    }

    /// <summary>
    /// Event raised when user dismisses the banner.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? Dismissed
    {
        add => AddHandler(DismissedEvent, value);
        remove => RemoveHandler(DismissedEvent, value);
    }

    private void OnUpgradeClicked(object? sender, RoutedEventArgs e)
    {
        var args = new RoutedEventArgs(UpgradeRequestedEvent, this);
        RaiseEvent(args);
    }

    private void OnDismissClicked(object? sender, RoutedEventArgs e)
    {
        IsDismissed = true;
        IsVisible = false;

        var args = new RoutedEventArgs(DismissedEvent, this);
        RaiseEvent(args);
    }
}
