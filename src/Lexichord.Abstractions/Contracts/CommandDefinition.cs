namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines a command that can be registered with the Command Registry.
/// </summary>
/// <remarks>
/// LOGIC: CommandDefinition is the fundamental unit of the command system.
/// It uses a record type for immutability and value-based equality.
///
/// Command ID Conventions:
/// - Format: "module.action" (e.g., "editor.save", "workspace.openFolder")
/// - Lowercase with dots as separators
/// - Module prefix should match the registering module's name
/// - Action should be a verb describing the operation
///
/// Category Conventions:
/// - Use standard categories: "File", "Edit", "View", "Selection", "Go", "Help"
/// - Custom categories allowed but should be documented
/// - Used for grouping in Command Palette and menus
///
/// Icon Conventions:
/// - Use Material Design icon names from Material.Icons.Avalonia
/// - Examples: "ContentSave", "FolderOpen", "Magnify", "Undo"
/// - Null means no icon (text only display)
///
/// Shortcut Format:
/// - Uses platform-independent string format: "Ctrl+S", "Ctrl+Shift+P"
/// - Parsed by KeyBindingService at runtime
/// - Use "Ctrl" (maps to Cmd on macOS), "Alt", "Shift"
/// </remarks>
/// <param name="Id">Unique identifier following "module.action" convention.</param>
/// <param name="Title">Display title shown in Command Palette and menus.</param>
/// <param name="Category">Category for grouping (e.g., "File", "Edit", "View").</param>
/// <param name="DefaultShortcut">Default keyboard shortcut string (e.g., "Ctrl+S"). User can override.</param>
/// <param name="Execute">Action to execute when command is invoked.</param>
public record CommandDefinition(
    string Id,
    string Title,
    string Category,
    string? DefaultShortcut,
    Action<object?> Execute
)
{
    /// <summary>
    /// Gets or sets the command description for tooltips and documentation.
    /// </summary>
    /// <remarks>
    /// LOGIC: Should be a complete sentence describing what the command does.
    /// Example: "Save the current document to disk."
    /// </remarks>
    public string? Description { get; init; }

    /// <summary>
    /// Gets or sets the Material icon kind for display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Icon names from Material.Icons.Avalonia package.
    /// Reference: https://pictogrammers.com/library/mdi/
    /// Examples: "ContentSave", "FolderOpen", "Magnify"
    /// </remarks>
    public string? IconKind { get; init; }

    /// <summary>
    /// Gets or sets the predicate to determine if command can execute.
    /// </summary>
    /// <remarks>
    /// LOGIC: Evaluated before execution. If returns false, command is
    /// considered disabled and will not execute.
    ///
    /// Common patterns:
    /// - Check if active document exists
    /// - Check if document is dirty
    /// - Check if selection exists
    /// - Check if workspace is open
    ///
    /// If null, command is always executable.
    /// </remarks>
    public Func<bool>? CanExecute { get; init; }

    /// <summary>
    /// Gets or sets the context in which this command is available.
    /// </summary>
    /// <remarks>
    /// LOGIC: Contexts allow commands to be active only in specific
    /// parts of the UI. Used by KeyBindingService to filter which
    /// commands can be triggered by keyboard shortcuts.
    ///
    /// Standard contexts:
    /// - null: Global (always available)
    /// - "editorFocus": Active when text editor has focus
    /// - "explorerFocus": Active when file explorer has focus
    /// - "searchFocus": Active when search input has focus
    /// - "paletteOpen": Active when command palette is visible
    ///
    /// Custom contexts can be defined by modules.
    /// </remarks>
    public string? Context { get; init; }

    /// <summary>
    /// Gets or sets additional tags for search enhancement.
    /// </summary>
    /// <remarks>
    /// LOGIC: Alternative search terms that match this command.
    /// Improves discoverability in Command Palette.
    ///
    /// Example for "Save" command:
    /// Tags = ["write", "store", "persist", "disk"]
    ///
    /// User typing "disk" would find "Save" command.
    /// </remarks>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Gets or sets whether the command should appear in menus.
    /// </summary>
    /// <remarks>
    /// LOGIC: Some commands are internal or palette-only.
    /// Default true for most commands.
    /// </remarks>
    public bool ShowInMenu { get; init; } = true;

    /// <summary>
    /// Gets or sets whether the command should appear in the Command Palette.
    /// </summary>
    /// <remarks>
    /// LOGIC: Some commands are menu-only or internal.
    /// Default true for most commands.
    /// </remarks>
    public bool ShowInPalette { get; init; } = true;

    /// <summary>
    /// Validates the command definition.
    /// </summary>
    /// <returns>List of validation errors, or empty if valid.</returns>
    public IReadOnlyList<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Command Id is required.");

        if (!string.IsNullOrWhiteSpace(Id) && !Id.Contains('.'))
            errors.Add("Command Id should follow 'module.action' convention.");

        if (string.IsNullOrWhiteSpace(Title))
            errors.Add("Command Title is required.");

        if (string.IsNullOrWhiteSpace(Category))
            errors.Add("Command Category is required.");

        if (Execute is null)
            errors.Add("Command Execute action is required.");

        return errors;
    }
}
