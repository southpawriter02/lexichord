namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Service for managing keyboard shortcuts.
/// </summary>
/// <remarks>
/// LOGIC: KeyBindingService manages the mapping between keyboard shortcuts
/// and commands. The binding resolution order is:
///
/// 1. User overrides from keybindings.json (highest priority)
/// 2. Command defaults from CommandDefinition.DefaultShortcut
///
/// Gesture String Format:
/// - Uses platform-independent string format: "Ctrl+S", "Ctrl+Shift+P"
/// - Supported modifiers: Ctrl/Control, Shift, Alt/Option, Meta/Win/Cmd
/// - Case-insensitive parsing
///
/// Context-Aware Bindings:
/// - Bindings can specify a "when" context condition
/// - Context is provided by the component handling the key event
/// - Examples: "editorFocus", "explorerFocus", "paletteOpen"
/// - Global bindings (when=null) work in all contexts
///
/// Conflict Resolution:
/// - Multiple commands can theoretically share a gesture
/// - Context filtering usually resolves conflicts
/// - Same-context conflicts are warned, first registered wins
///
/// Thread Safety:
/// - Read operations are thread-safe
/// - Write operations (SetBinding) lock internally
/// - Hot-reload debounced to prevent rapid updates
/// </remarks>
public interface IKeyBindingService
{
    /// <summary>
    /// Gets the effective binding gesture for a command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <returns>The gesture string (e.g., "Ctrl+S"), or null if no binding.</returns>
    /// <remarks>
    /// LOGIC: Returns user override if set, otherwise command default.
    /// Returns null if command is explicitly unbound or disabled.
    /// </remarks>
    string? GetBinding(string commandId);

    /// <summary>
    /// Gets the full binding information for a command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <returns>The key binding, or null if not bound.</returns>
    KeyBinding? GetFullBinding(string commandId);

    /// <summary>
    /// Gets all active key bindings.
    /// </summary>
    /// <returns>List of all bindings.</returns>
    IReadOnlyList<KeyBinding> GetAllBindings();

    /// <summary>
    /// Gets bindings that conflict with a gesture.
    /// </summary>
    /// <param name="gesture">The gesture string to check.</param>
    /// <param name="context">Optional context to filter by.</param>
    /// <returns>Command IDs bound to this gesture.</returns>
    IReadOnlyList<string> GetConflicts(string gesture, string? context = null);

    /// <summary>
    /// Checks if a gesture is available (not bound).
    /// </summary>
    /// <param name="gesture">The gesture string to check.</param>
    /// <param name="context">Optional context.</param>
    /// <param name="excludeCommandId">Command to exclude from conflict check.</param>
    /// <returns>True if gesture is not bound to another command.</returns>
    bool IsGestureAvailable(string gesture, string? context = null, string? excludeCommandId = null);

    /// <summary>
    /// Sets a custom binding for a command.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    /// <param name="gesture">The new gesture string, or null/"-" to unbind.</param>
    /// <param name="when">Optional context condition.</param>
    /// <remarks>
    /// LOGIC: Overrides default binding. Pass null or "-" to disable.
    /// Changes are persisted to keybindings.json.
    /// Raises BindingChanged event.
    /// </remarks>
    void SetBinding(string commandId, string? gesture, string? when = null);

    /// <summary>
    /// Removes a custom binding, reverting to default.
    /// </summary>
    /// <param name="commandId">The command ID.</param>
    void ResetBinding(string commandId);

    /// <summary>
    /// Resets all bindings to defaults.
    /// </summary>
    void ResetToDefaults();

    /// <summary>
    /// Loads bindings from keybindings.json.
    /// </summary>
    Task LoadBindingsAsync();

    /// <summary>
    /// Saves current bindings to keybindings.json.
    /// </summary>
    Task SaveBindingsAsync();

    /// <summary>
    /// Gets the path to keybindings.json.
    /// </summary>
    string KeybindingsFilePath { get; }

    /// <summary>
    /// Gets whether keybindings file exists.
    /// </summary>
    bool HasCustomBindings { get; }

    /// <summary>
    /// Event raised when a binding changes.
    /// </summary>
    event EventHandler<KeyBindingChangedEventArgs>? BindingChanged;

    /// <summary>
    /// Event raised when bindings are reloaded.
    /// </summary>
    event EventHandler? BindingsReloaded;
}
