namespace Lexichord.Abstractions.Layout;

/// <summary>
/// Options for registering a tool pane in a dock region.
/// </summary>
/// <param name="ActivateOnRegister">Whether to activate the tool after registration. Default: true.</param>
/// <param name="CanClose">Whether the tool can be closed by the user. Default: true.</param>
/// <param name="MinWidth">Minimum width constraint in pixels. Default: null (uses region default).</param>
/// <param name="MinHeight">Minimum height constraint in pixels. Default: null (uses region default).</param>
/// <remarks>
/// LOGIC: Provides configuration options for tool registration without exposing
/// Dock.Avalonia-specific types. Modules use this to customize tool behavior
/// while remaining decoupled from the docking implementation.
/// </remarks>
public record ToolRegistrationOptions(
    bool ActivateOnRegister = true,
    bool CanClose = true,
    double? MinWidth = null,
    double? MinHeight = null
);
