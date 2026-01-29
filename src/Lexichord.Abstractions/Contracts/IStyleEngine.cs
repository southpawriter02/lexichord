namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines the contract for the style analysis engine.
/// </summary>
/// <remarks>
/// LOGIC: IStyleEngine is the primary entry point for style checking.
/// It maintains the "active" style sheet and analyzes content for violations.
///
/// Lifecycle:
/// 1. StyleModule loads default rules via IStyleSheetLoader
/// 2. StyleModule calls SetActiveStyleSheet() with the loaded sheet
/// 3. Consumer calls AnalyzeAsync() to check content
/// 4. IStyleConfigurationWatcher detects changes, triggers reload
/// 5. SetActiveStyleSheet() updates and raises StyleSheetChanged
///
/// Design Decisions:
/// - Thread-safe: multiple documents can analyze concurrently
/// - Singleton: single instance manages the active sheet
/// - Event-driven: sheet changes notify subscribers for re-analysis
/// </remarks>
/// <example>
/// <code>
/// // Analyze content with the active style sheet
/// var violations = await styleEngine.AnalyzeAsync(documentText);
///
/// // Or analyze with a specific sheet (for preview)
/// var customSheet = await loader.LoadFromFileAsync("custom.yaml");
/// var violations = await styleEngine.AnalyzeAsync(documentText, customSheet);
/// </code>
/// </example>
public interface IStyleEngine
{
    /// <summary>
    /// Analyzes content using the currently active style sheet.
    /// </summary>
    /// <param name="content">The text content to analyze.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of violations found.</returns>
    /// <remarks>
    /// LOGIC: This is the primary analysis method for live documents.
    /// Uses the active style sheet set by SetActiveStyleSheet().
    /// Returns empty list if no active sheet or content is null/empty.
    /// </remarks>
    Task<IReadOnlyList<StyleViolation>> AnalyzeAsync(
        string content,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Analyzes content using a specific style sheet.
    /// </summary>
    /// <param name="content">The text content to analyze.</param>
    /// <param name="styleSheet">The style sheet to use for analysis.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A read-only list of violations found.</returns>
    /// <remarks>
    /// LOGIC: Used for preview or when analyzing with non-active sheets.
    /// Does not affect the active sheet or raise events.
    /// </remarks>
    Task<IReadOnlyList<StyleViolation>> AnalyzeAsync(
        string content,
        StyleSheet styleSheet,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the currently active style sheet.
    /// </summary>
    /// <returns>The active style sheet, or <see cref="StyleSheet.Empty"/> if none set.</returns>
    /// <remarks>
    /// LOGIC: Thread-safe read of the active sheet.
    /// Returns StyleSheet.Empty before initialization.
    /// </remarks>
    StyleSheet GetActiveStyleSheet();

    /// <summary>
    /// Sets the active style sheet.
    /// </summary>
    /// <param name="styleSheet">The style sheet to activate.</param>
    /// <exception cref="ArgumentNullException">Thrown if styleSheet is null.</exception>
    /// <remarks>
    /// LOGIC: Thread-safe update of the active sheet.
    /// Raises <see cref="StyleSheetChanged"/> event after update.
    /// Consumers (e.g., open documents) should re-analyze on this event.
    /// </remarks>
    void SetActiveStyleSheet(StyleSheet styleSheet);

    /// <summary>
    /// Occurs when the active style sheet changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Raised by SetActiveStyleSheet() after the sheet is updated.
    /// Consumers should subscribe to trigger re-analysis of open documents.
    /// Event includes both old and new sheets for comparison.
    /// </remarks>
    event EventHandler<StyleSheetChangedEventArgs>? StyleSheetChanged;
}

/// <summary>
/// Event arguments for <see cref="IStyleEngine.StyleSheetChanged"/>.
/// </summary>
public sealed class StyleSheetChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the previously active style sheet.
    /// </summary>
    public required StyleSheet OldStyleSheet { get; init; }

    /// <summary>
    /// Gets the newly active style sheet.
    /// </summary>
    public required StyleSheet NewStyleSheet { get; init; }

    /// <summary>
    /// Gets the source of the change.
    /// </summary>
    public required StyleSheetChangeSource ChangeSource { get; init; }
}

/// <summary>
/// Indicates the source of a style sheet change.
/// </summary>
public enum StyleSheetChangeSource
{
    /// <summary>Initial load during module initialization.</summary>
    Initialization,

    /// <summary>User explicitly loaded a different sheet.</summary>
    UserAction,

    /// <summary>File watcher detected changes and triggered reload.</summary>
    FileChange,

    /// <summary>Programmatic API call.</summary>
    Api
}
