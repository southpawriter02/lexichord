namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Options for registering a document in the center dock region.
/// </summary>
/// <param name="ActivateOnRegister">Whether to activate the document after registration. Default: true.</param>
/// <param name="IsPinned">Whether the document tab is pinned. Default: false.</param>
/// <remarks>
/// LOGIC: Provides configuration options for document registration without exposing
/// Dock.Avalonia-specific types. Modules use this to customize document behavior
/// while remaining decoupled from the docking implementation.
/// </remarks>
public record DocumentRegistrationOptions(
    bool ActivateOnRegister = true,
    bool IsPinned = false
);
