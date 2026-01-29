using Avalonia;
using MediatR;
using Microsoft.Extensions.Logging;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using AvaloniaThemeVariant = Avalonia.Styling.ThemeVariant;

namespace Lexichord.Host.Services;

/// <summary>
/// Manages runtime theme switching for the Lexichord application.
/// </summary>
/// <remarks>
/// LOGIC: This implementation uses Avalonia's built-in theme variant system.
/// When RequestedThemeVariant is changed, Avalonia automatically:
/// 1. Looks for theme variants in registered ResourceDictionaries
/// 2. Swaps all resources bound with {DynamicResource}
/// 3. Re-renders affected controls
///
/// Our Colors.Dark.axaml and Colors.Light.axaml define matching brush keys,
/// so all controls update seamlessly when the variant changes.
///
/// Version: v0.1.6b - Updated with async methods, EffectiveTheme, and MediatR events.
/// </remarks>
public sealed class ThemeManager : IThemeManager
{
    private readonly Application _application;
    private readonly ILogger<ThemeManager> _logger;
    private readonly IMediator _mediator;
    private ThemeMode _currentTheme = ThemeMode.System;

    /// <summary>
    /// Initializes a new instance of the ThemeManager.
    /// </summary>
    /// <param name="application">The Avalonia application instance (injected via DI).</param>
    /// <param name="logger">The logger instance for diagnostics.</param>
    /// <param name="mediator">The MediatR instance for publishing events.</param>
    public ThemeManager(
        Application application,
        ILogger<ThemeManager> logger,
        IMediator mediator)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        _logger.LogDebug("ThemeManager initialized");

        // LOGIC: Subscribe to platform theme changes for System mode
        if (_application.PlatformSettings is not null)
        {
            _application.PlatformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
        }
    }

    /// <inheritdoc/>
    public ThemeMode CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public ThemeVariant EffectiveTheme => GetEffectiveThemeVariant();

    /// <inheritdoc/>
    public event EventHandler<ThemeChangedEventArgs>? ThemeChanged;

    /// <inheritdoc/>
    public async Task SetThemeAsync(ThemeMode theme)
    {
        if (_currentTheme == theme)
        {
            _logger.LogDebug("Theme already set to {Theme}, ignoring", theme);
            return;
        }

        var oldTheme = _currentTheme;
        _currentTheme = theme;
        ApplyTheme(theme);

        var effectiveTheme = EffectiveTheme;
        var eventArgs = new ThemeChangedEventArgs(oldTheme, theme, effectiveTheme);

        // Raise the standard event
        ThemeChanged?.Invoke(this, eventArgs);

        // Publish MediatR notification for cross-module awareness
        await _mediator.Publish(new ThemeChangedEvent
        {
            OldTheme = oldTheme,
            NewTheme = theme,
            EffectiveTheme = effectiveTheme
        });

        _logger.LogInformation(
            "Theme changed from {OldTheme} to {NewTheme} (effective: {EffectiveTheme})",
            oldTheme,
            theme,
            effectiveTheme);
    }

    /// <inheritdoc/>
    public ThemeVariant GetSystemTheme()
    {
        // LOGIC: Query Avalonia's platform settings for current color scheme
        var colorScheme = _application.PlatformSettings?.GetColorValues();

        // LOGIC: PlatformThemeVariant is an enum that matches Avalonia's ThemeVariant names
        // We check if the platform is using dark mode
        var isDark = colorScheme?.ThemeVariant == Avalonia.Platform.PlatformThemeVariant.Dark;

        return isDark ? ThemeVariant.Dark : ThemeVariant.Light;
    }

    /// <summary>
    /// Gets the effective theme variant being displayed.
    /// </summary>
    private ThemeVariant GetEffectiveThemeVariant()
    {
        if (_currentTheme != ThemeMode.System)
        {
            return _currentTheme == ThemeMode.Dark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }

        // LOGIC: Resolve system theme
        return GetSystemTheme();
    }

    /// <summary>
    /// Applies the specified theme to the application.
    /// </summary>
    /// <param name="mode">The theme mode to apply.</param>
    private void ApplyTheme(ThemeMode mode)
    {
        // LOGIC: Map our ThemeMode to Avalonia's ThemeVariant
        _application.RequestedThemeVariant = mode switch
        {
            ThemeMode.Dark => AvaloniaThemeVariant.Dark,
            ThemeMode.Light => AvaloniaThemeVariant.Light,
            ThemeMode.System => AvaloniaThemeVariant.Default,
            _ => AvaloniaThemeVariant.Default
        };
    }

    /// <summary>
    /// Handles platform color values changed event.
    /// </summary>
    private async void OnPlatformColorValuesChanged(
        object? sender,
        Avalonia.Platform.PlatformColorValues e)
    {
        // LOGIC: Only re-raise event if we're in System mode
        if (_currentTheme == ThemeMode.System)
        {
            _logger.LogDebug("Platform color values changed while in System mode");

            var effectiveTheme = GetSystemTheme();
            var eventArgs = new ThemeChangedEventArgs(
                ThemeMode.System,
                ThemeMode.System,
                effectiveTheme);

            ThemeChanged?.Invoke(this, eventArgs);

            // Publish MediatR notification
            await _mediator.Publish(new ThemeChangedEvent
            {
                OldTheme = ThemeMode.System,
                NewTheme = ThemeMode.System,
                EffectiveTheme = effectiveTheme
            });
        }
    }
}
