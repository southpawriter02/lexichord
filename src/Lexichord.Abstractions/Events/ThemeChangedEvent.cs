using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when the application theme changes.
/// </summary>
/// <remarks>
/// LOGIC: This MediatR notification enables cross-module reactive theme updates.
/// Modules that need to adjust their appearance or behavior based on theme
/// can handle this event.
///
/// **When Published:**
/// - After SetThemeAsync() successfully applies a new theme.
/// - When the OS theme changes and CurrentTheme is System.
///
/// **Expected Handlers:**
/// - Editor modules: Update syntax highlighting colors.
/// - Chart modules: Update visualization palettes.
/// - Icon managers: Switch between light/dark icon sets.
///
/// **Handler Responsibilities:**
/// - Handlers SHOULD check EffectiveTheme for the actual applied theme.
/// - Handlers SHOULD NOT perform heavy operations synchronously.
/// - Handlers SHOULD cache theme-dependent resources for performance.
///
/// Version: v0.1.6b
/// </remarks>
public record ThemeChangedEvent : DomainEventBase
{
    /// <summary>
    /// Gets the previous theme mode.
    /// </summary>
    public ThemeMode OldTheme { get; init; }

    /// <summary>
    /// Gets the new theme mode.
    /// </summary>
    public ThemeMode NewTheme { get; init; }

    /// <summary>
    /// Gets the effective theme variant that is now applied.
    /// </summary>
    /// <remarks>
    /// LOGIC: When NewTheme is System, this reflects the actual OS theme.
    /// This is the most useful property for handlers that need to know
    /// whether to use light or dark styling.
    /// </remarks>
    public ThemeVariant EffectiveTheme { get; init; }
}
