namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Editor theme for syntax highlighting colors.
/// </summary>
public enum EditorTheme
{
    /// <summary>Light theme with dark text on light background.</summary>
    Light,

    /// <summary>Dark theme with light text on dark background.</summary>
    Dark
}

/// <summary>
/// Reasons for highlighting definition changes.
/// </summary>
public enum HighlightingChangeReason
{
    /// <summary>Theme changed, colors updated.</summary>
    ThemeChanged,

    /// <summary>New definition registered.</summary>
    DefinitionRegistered,

    /// <summary>Definition unregistered.</summary>
    DefinitionUnregistered,

    /// <summary>All definitions reloaded.</summary>
    DefinitionsReloaded
}

/// <summary>
/// Event arguments for highlighting changes.
/// </summary>
public class HighlightingChangedEventArgs : EventArgs
{
    /// <summary>
    /// The reason for the highlighting change.
    /// </summary>
    public required HighlightingChangeReason Reason { get; init; }

    /// <summary>
    /// The name of the affected highlighting (null for all).
    /// </summary>
    public string? HighlightingName { get; init; }
}
