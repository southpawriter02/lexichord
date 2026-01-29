namespace Lexichord.Host.Configuration;

using System.Text.Json.Serialization;

/// <summary>
/// Schema for keybindings.json file.
/// </summary>
/// <remarks>
/// LOGIC: This is the schema for user-customizable keybindings.
/// File location: ~/.config/Lexichord/keybindings.json (Linux)
///                ~/Library/Application Support/Lexichord/keybindings.json (macOS)
///                %APPDATA%/Lexichord/keybindings.json (Windows)
///
/// Example:
/// {
///   "bindings": [
///     { "command": "file.save", "key": "Ctrl+Shift+S" },
///     { "command": "editor.copy", "key": "Ctrl+C", "when": "editorFocus" },
///     { "command": "commandPalette.open", "key": "-" } // Disabled
///   ]
/// }
/// </remarks>
public record KeybindingsFile
{
    /// <summary>
    /// Gets the list of binding entries.
    /// </summary>
    [JsonPropertyName("bindings")]
    public List<KeybindingEntry> Bindings { get; init; } = [];
}

/// <summary>
/// A single keybinding entry in keybindings.json.
/// </summary>
public record KeybindingEntry
{
    /// <summary>
    /// Gets or sets the command ID (e.g., "file.save").
    /// </summary>
    [JsonPropertyName("command")]
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the key shortcut (e.g., "Ctrl+S" or "-" to disable).
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional context condition.
    /// </summary>
    [JsonPropertyName("when")]
    public string? When { get; init; }
}
