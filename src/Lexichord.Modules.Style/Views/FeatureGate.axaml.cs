// <copyright file="FeatureGate.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for FeatureGate user control.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.1d - This control wraps content and displays a lock overlay when
/// the specified feature is not enabled for the current license tier.
///
/// Features:
/// - FeatureCode property to specify which feature to check
/// - Grays out content when feature is disabled
/// - Shows lock icon overlay with "Upgrade" prompt
/// - Clicking the overlay triggers an upgrade prompt event
///
/// Usage: Call FeatureGate.Initialize() from the Host project during app startup.
///
/// Version: v0.3.1d
/// </remarks>
public partial class FeatureGate : UserControl
{
    private static ILicenseContext? _licenseContext;

    /// <summary>
    /// Defines the FeatureCode styled property.
    /// </summary>
    public static readonly StyledProperty<string?> FeatureCodeProperty =
        AvaloniaProperty.Register<FeatureGate, string?>(nameof(FeatureCode));

    /// <summary>
    /// Defines the ShowUpgradeText styled property.
    /// </summary>
    public static readonly StyledProperty<bool> ShowUpgradeTextProperty =
        AvaloniaProperty.Register<FeatureGate, bool>(nameof(ShowUpgradeText), true);

    /// <summary>
    /// Defines the IsFeatureEnabled direct property.
    /// </summary>
    public static readonly DirectProperty<FeatureGate, bool> IsFeatureEnabledProperty =
        AvaloniaProperty.RegisterDirect<FeatureGate, bool>(
            nameof(IsFeatureEnabled),
            o => o.IsFeatureEnabled);

    /// <summary>
    /// Defines the ShowLockOverlay direct property.
    /// </summary>
    public static readonly DirectProperty<FeatureGate, bool> ShowLockOverlayProperty =
        AvaloniaProperty.RegisterDirect<FeatureGate, bool>(
            nameof(ShowLockOverlay),
            o => o.ShowLockOverlay);

    /// <summary>
    /// Defines the ContentOpacity direct property.
    /// </summary>
    public static readonly DirectProperty<FeatureGate, double> ContentOpacityProperty =
        AvaloniaProperty.RegisterDirect<FeatureGate, double>(
            nameof(ContentOpacity),
            o => o.ContentOpacity);

    /// <summary>
    /// Routed event raised when user clicks the lock overlay to request upgrade.
    /// </summary>
    public static readonly RoutedEvent<RoutedEventArgs> UpgradeRequestedEvent =
        RoutedEvent.Register<FeatureGate, RoutedEventArgs>(
            nameof(UpgradeRequested),
            RoutingStrategies.Bubble);

    private bool _isFeatureEnabled = true;
    private bool _showLockOverlay;
    private double _contentOpacity = 1.0;

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureGate"/> class.
    /// </summary>
    public FeatureGate()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes the license context for all FeatureGate instances.
    /// </summary>
    /// <param name="licenseContext">The license context to use.</param>
    /// <remarks>
    /// LOGIC: Call this from the Host project during app initialization.
    /// </remarks>
    public static void Initialize(ILicenseContext licenseContext)
    {
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
    }

    /// <summary>
    /// Gets or sets the feature code to check for license gating.
    /// </summary>
    public string? FeatureCode
    {
        get => GetValue(FeatureCodeProperty);
        set => SetValue(FeatureCodeProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the "Upgrade to unlock" text in the overlay.
    /// </summary>
    public bool ShowUpgradeText
    {
        get => GetValue(ShowUpgradeTextProperty);
        set => SetValue(ShowUpgradeTextProperty, value);
    }

    /// <summary>
    /// Gets whether the feature is currently enabled.
    /// </summary>
    public bool IsFeatureEnabled
    {
        get => _isFeatureEnabled;
        private set => SetAndRaise(IsFeatureEnabledProperty, ref _isFeatureEnabled, value);
    }

    /// <summary>
    /// Gets whether to show the lock overlay.
    /// </summary>
    public bool ShowLockOverlay
    {
        get => _showLockOverlay;
        private set => SetAndRaise(ShowLockOverlayProperty, ref _showLockOverlay, value);
    }

    /// <summary>
    /// Gets the content opacity (reduced when feature is disabled).
    /// </summary>
    public double ContentOpacity
    {
        get => _contentOpacity;
        private set => SetAndRaise(ContentOpacityProperty, ref _contentOpacity, value);
    }

    /// <summary>
    /// Event raised when user requests an upgrade by clicking the lock overlay.
    /// </summary>
    public event EventHandler<RoutedEventArgs>? UpgradeRequested
    {
        add => AddHandler(UpgradeRequestedEvent, value);
        remove => RemoveHandler(UpgradeRequestedEvent, value);
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == FeatureCodeProperty)
        {
            UpdateFeatureState();
        }
    }

    /// <inheritdoc/>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateFeatureState();
    }

    private void UpdateFeatureState()
    {
        var featureCode = FeatureCode;
        if (string.IsNullOrWhiteSpace(featureCode))
        {
            // LOGIC: No feature code = always enabled
            IsFeatureEnabled = true;
            ShowLockOverlay = false;
            ContentOpacity = 1.0;
            return;
        }

        if (_licenseContext == null)
        {
            // LOGIC: No license context = assume enabled (development/design mode)
            IsFeatureEnabled = true;
            ShowLockOverlay = false;
            ContentOpacity = 1.0;
            return;
        }

        var isEnabled = _licenseContext.IsFeatureEnabled(featureCode);
        IsFeatureEnabled = isEnabled;
        ShowLockOverlay = !isEnabled;
        ContentOpacity = isEnabled ? 1.0 : 0.4;
    }

    private void OnLockOverlayClicked(object? sender, PointerPressedEventArgs e)
    {
        // LOGIC: Raise the upgrade requested event for parent handlers
        var args = new RoutedEventArgs(UpgradeRequestedEvent, this);
        RaiseEvent(args);
    }
}
