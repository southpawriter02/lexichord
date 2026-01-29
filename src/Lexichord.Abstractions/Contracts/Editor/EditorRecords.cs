namespace Lexichord.Abstractions.Contracts.Editor;

/// <summary>
/// Represents the caret (cursor) position in the text editor.
/// </summary>
/// <param name="Line">1-based line number.</param>
/// <param name="Column">1-based column number.</param>
/// <param name="Offset">0-based character offset from start of document.</param>
public readonly record struct CaretPosition(int Line, int Column, int Offset)
{
    /// <summary>
    /// Default position at start of document.
    /// </summary>
    public static readonly CaretPosition Default = new(1, 1, 0);
}

/// <summary>
/// Represents a text selection in the editor.
/// </summary>
/// <param name="StartOffset">0-based start offset of selection.</param>
/// <param name="EndOffset">0-based end offset of selection (exclusive).</param>
/// <param name="Length">Length of selected text.</param>
/// <param name="SelectedText">The selected text content.</param>
public readonly record struct TextSelection(int StartOffset, int EndOffset, int Length, string SelectedText)
{
    /// <summary>
    /// Empty selection.
    /// </summary>
    public static readonly TextSelection Empty = new(0, 0, 0, string.Empty);

    /// <summary>
    /// Gets whether the selection is empty.
    /// </summary>
    public bool IsEmpty => Length == 0;
}

/// <summary>
/// Editor configuration settings.
/// </summary>
/// <remarks>
/// LOGIC: Contains all configurable editor options. Default values provide
/// a sensible out-of-box experience. Full implementation in v0.1.3d.
/// </remarks>
public record EditorSettings
{
    /// <summary>Default font for code editing.</summary>
    public string FontFamily { get; init; } = "Cascadia Code, Consolas, monospace";

    /// <summary>Default font size in points.</summary>
    public double FontSize { get; init; } = 14.0;

    /// <summary>Show line numbers in gutter.</summary>
    public bool ShowLineNumbers { get; init; } = true;

    /// <summary>Wrap long lines.</summary>
    public bool WordWrap { get; init; } = true;

    /// <summary>Highlight the current line.</summary>
    public bool HighlightCurrentLine { get; init; } = true;

    /// <summary>Show whitespace characters (tabs/spaces).</summary>
    public bool ShowWhitespace { get; init; } = false;

    /// <summary>Show end-of-line markers.</summary>
    public bool ShowEndOfLine { get; init; } = false;

    /// <summary>Convert tabs to spaces when typing.</summary>
    public bool UseSpacesForTabs { get; init; } = true;

    /// <summary>Number of spaces per indentation level.</summary>
    public int IndentSize { get; init; } = 4;

    /// <summary>Tab width in spaces.</summary>
    public int TabSize { get; init; } = 4;
}

/// <summary>
/// Event arguments for editor settings changes.
/// </summary>
public class EditorSettingsChangedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EditorSettingsChangedEventArgs"/> class.
    /// </summary>
    /// <param name="oldSettings">Settings before change.</param>
    /// <param name="newSettings">Settings after change.</param>
    public EditorSettingsChangedEventArgs(EditorSettings? oldSettings = null, EditorSettings? newSettings = null)
    {
        OldSettings = oldSettings;
        NewSettings = newSettings;
    }

    /// <summary>Settings before change, if available.</summary>
    public EditorSettings? OldSettings { get; }

    /// <summary>Settings after change, if available.</summary>
    public EditorSettings? NewSettings { get; }
}
