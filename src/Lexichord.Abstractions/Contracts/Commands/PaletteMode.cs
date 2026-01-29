namespace Lexichord.Abstractions.Contracts.Commands;

/// <summary>
/// Modes of the Command Palette.
/// </summary>
/// <remarks>
/// LOGIC: Determines what content the palette displays and searches.
/// - Commands: Registered application commands from ICommandRegistry
/// - Files: Workspace files from IFileIndexService (v0.1.5c)
/// - Symbols: Document symbols like headings (future)
/// - GoToLine: Line number navigation (future)
/// </remarks>
public enum PaletteMode
{
    /// <summary>
    /// Show commands from ICommandRegistry.
    /// </summary>
    Commands,

    /// <summary>
    /// Show files from IFileIndexService.
    /// </summary>
    Files,

    /// <summary>
    /// Show symbols in current document (future).
    /// </summary>
    Symbols,

    /// <summary>
    /// Go to specific line number (future).
    /// </summary>
    GoToLine
}
