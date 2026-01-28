using Avalonia;
using Avalonia.Styling;
using Lexichord.Abstractions.Contracts;

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
/// </remarks>
public sealed class ThemeManager : IThemeManager
{
    private readonly Application _application;
    private ThemeMode _currentTheme = ThemeMode.System;

    /// <summary>
    /// Initializes a new instance of the ThemeManager.
    /// </summary>
    /// <param name="application">The Avalonia application instance (injected via DI).</param>
    public ThemeManager(Application application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));

        // LOGIC: Subscribe to platform theme changes for System mode
        if (_application.PlatformSettings is not null)
        {
            _application.PlatformSettings.ColorValuesChanged += OnPlatformColorValuesChanged;
        }
    }

    /// <inheritdoc/>
    public ThemeMode CurrentTheme => _currentTheme;

    /// <inheritdoc/>
    public event EventHandler<ThemeMode>? ThemeChanged;

    /// <inheritdoc/>
    public void SetTheme(ThemeMode mode)
    {
        if (_currentTheme == mode)
            return;

        _currentTheme = mode;
        ApplyTheme(mode);
        ThemeChanged?.Invoke(this, mode);
    }

    /// <inheritdoc/>
    public void ToggleTheme()
    {
        // LOGIC: Toggle based on effective theme, not preference
        var effective = GetEffectiveTheme();
        var newMode = effective == ThemeMode.Dark ? ThemeMode.Light : ThemeMode.Dark;
        SetTheme(newMode);
    }

    /// <inheritdoc/>
    public ThemeMode GetEffectiveTheme()
    {
        if (_currentTheme != ThemeMode.System)
            return _currentTheme;

        // LOGIC: Resolve system theme by checking Avalonia's actual theme variant
        return _application.ActualThemeVariant == ThemeVariant.Dark
            ? ThemeMode.Dark
            : ThemeMode.Light;
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
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.System => ThemeVariant.Default,
            _ => ThemeVariant.Default
        };
    }

    /// <summary>
    /// Handles platform color values changed event.
    /// </summary>
    private void OnPlatformColorValuesChanged(object? sender, Avalonia.Platform.PlatformColorValues e)
    {
        // LOGIC: Only re-raise event if we're in System mode
        if (_currentTheme == ThemeMode.System)
        {
            ThemeChanged?.Invoke(this, ThemeMode.System);
        }
    }
}
