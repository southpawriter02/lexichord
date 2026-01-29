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
/// a sensible out-of-box experience.
///
/// v0.1.3d: Full implementation with font fallback, zoom support, and validation.
/// </remarks>
public record EditorSettings
{
    /// <summary>Settings section name for persistence.</summary>
    public const string SectionName = "Editor";

    #region Font Settings

    /// <summary>Preferred font family for code editing.</summary>
    public string FontFamily { get; init; } = "Cascadia Code";

    /// <summary>Current font size in points.</summary>
    public double FontSize { get; init; } = 14.0;

    /// <summary>Minimum allowed font size.</summary>
    public double MinFontSize { get; init; } = 8.0;

    /// <summary>Maximum allowed font size.</summary>
    public double MaxFontSize { get; init; } = 72.0;

    /// <summary>Font size increment for zoom operations.</summary>
    public double ZoomIncrement { get; init; } = 2.0;

    #endregion

    #region Indentation Settings

    /// <summary>Convert tabs to spaces when typing.</summary>
    public bool UseSpacesForTabs { get; init; } = true;

    /// <summary>Tab width in spaces.</summary>
    public int TabSize { get; init; } = 4;

    /// <summary>Number of spaces per indentation level.</summary>
    public int IndentSize { get; init; } = 4;

    /// <summary>Automatically indent new lines.</summary>
    public bool AutoIndent { get; init; } = true;

    #endregion

    #region Display Settings

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

    /// <summary>Highlight matching brackets.</summary>
    public bool HighlightMatchingBrackets { get; init; } = true;

    /// <summary>Column position for vertical ruler.</summary>
    public int VerticalRulerPosition { get; init; } = 80;

    /// <summary>Show vertical ruler at column position.</summary>
    public bool ShowVerticalRuler { get; init; } = false;

    /// <summary>Enable smooth scrolling animation.</summary>
    public bool SmoothScrolling { get; init; } = true;

    #endregion

    #region Cursor Settings

    /// <summary>Enable cursor blinking.</summary>
    public bool BlinkCursor { get; init; } = true;

    /// <summary>Cursor blink rate in milliseconds.</summary>
    public int CursorBlinkRate { get; init; } = 530;

    #endregion

    #region Font Fallback

    /// <summary>
    /// Ordered list of fallback fonts to try if preferred font is unavailable.
    /// </summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public static IReadOnlyList<string> FallbackFonts { get; } = new[]
    {
        "Cascadia Code",
        "Cascadia Mono",
        "Fira Code",
        "JetBrains Mono",
        "Source Code Pro",
        "Consolas",
        "Monaco",
        "Menlo",
        "DejaVu Sans Mono",
        "Liberation Mono",
        "Ubuntu Mono",
        "monospace"
    };

    #endregion

    #region Validation

    /// <summary>
    /// Returns a validated copy of settings with values clamped to valid ranges.
    /// </summary>
    /// <returns>A new EditorSettings instance with validated values.</returns>
    public EditorSettings Validated()
    {
        return this with
        {
            FontSize = Math.Clamp(FontSize, MinFontSize, MaxFontSize),
            TabSize = Math.Clamp(TabSize, 1, 16),
            IndentSize = Math.Clamp(IndentSize, 1, 16),
            VerticalRulerPosition = Math.Clamp(VerticalRulerPosition, 0, 200),
            CursorBlinkRate = Math.Clamp(CursorBlinkRate, 100, 2000)
        };
    }

    #endregion
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
    /// <param name="propertyName">Name of the changed property, or null if multiple changed.</param>
    public EditorSettingsChangedEventArgs(
        EditorSettings? oldSettings = null,
        EditorSettings? newSettings = null,
        string? propertyName = null)
    {
        OldSettings = oldSettings;
        NewSettings = newSettings ?? new EditorSettings();
        PropertyName = propertyName;
    }

    /// <summary>Name of the property that changed, or null if multiple properties changed.</summary>
    public string? PropertyName { get; }

    /// <summary>Settings before change, if available.</summary>
    public EditorSettings? OldSettings { get; }

    /// <summary>Settings after change.</summary>
    public EditorSettings NewSettings { get; }
}
