namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Represents a key binding configuration.
/// </summary>
/// <remarks>
/// LOGIC: KeyBinding maps a keyboard gesture to a command, with optional
/// context filtering. The binding can be either a default (from CommandDefinition)
/// or user-defined (from keybindings.json).
///
/// Gesture Format:
/// - Uses string format: "Ctrl+Shift+P", "Alt+F4", "F5"
/// - Modifiers: Ctrl, Shift, Alt, Meta (or Win/Cmd)
/// - Platform-independent - parsed at runtime
///
/// Context-Aware Bindings:
/// - When is null: Global binding (always available)
/// - When is set: Binding only active when context matches
///
/// Standard contexts:
/// - "editorFocus": Active when text editor has focus
/// - "explorerFocus": Active when file explorer has focus
/// - "searchFocus": Active when search input has focus
/// - "paletteOpen": Active when command palette is visible
///
/// Disabled Bindings:
/// - User can set gesture to "-" in keybindings.json to disable a default binding
/// - IsDisabled will be true, Gesture will be "-"
/// </remarks>
/// <param name="CommandId">The command this binding triggers.</param>
/// <param name="Gesture">The keyboard shortcut string (e.g., "Ctrl+S").</param>
public record KeyBinding(
    string CommandId,
    string? Gesture
)
{
    /// <summary>
    /// Gets or sets the context condition for this binding.
    /// </summary>
    /// <remarks>
    /// LOGIC: Binding only active when context matches.
    /// Null means global (always available).
    /// Custom contexts can be defined by modules.
    /// </remarks>
    public string? When { get; init; }

    /// <summary>
    /// Gets whether this is a user-defined binding (vs default).
    /// </summary>
    /// <remarks>
    /// LOGIC: User-defined bindings come from keybindings.json.
    /// They override defaults and are persisted when SaveBindingsAsync is called.
    /// </remarks>
    public bool IsUserDefined { get; init; }

    /// <summary>
    /// Gets whether this binding is explicitly disabled.
    /// </summary>
    /// <remarks>
    /// LOGIC: User can set gesture to "-" in keybindings.json to disable a binding.
    /// When disabled, the gesture is not used for key event handling.
    /// </remarks>
    public bool IsDisabled { get; init; }
}

/// <summary>
/// Event args for key binding changes.
/// </summary>
public class KeyBindingChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the affected command ID.
    /// </summary>
    public required string CommandId { get; init; }

    /// <summary>
    /// Gets the previous gesture string (null if was unbound).
    /// </summary>
    public string? OldGesture { get; init; }

    /// <summary>
    /// Gets the new gesture string (null if now unbound).
    /// </summary>
    public string? NewGesture { get; init; }

    /// <summary>
    /// Gets whether this was a user-initiated change.
    /// </summary>
    public bool IsUserChange { get; init; }
}
