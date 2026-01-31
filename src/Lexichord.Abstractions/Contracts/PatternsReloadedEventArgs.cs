namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments raised when ignore patterns are reloaded.
/// </summary>
/// <remarks>
/// LOGIC: This event is raised when the .lexichordignore file is:
/// - Initially loaded during workspace open
/// - Hot-reloaded due to file changes
/// - Manually refreshed via API
///
/// Version: v0.3.6d
/// </remarks>
/// <param name="PatternCount">The number of active patterns after reload.</param>
/// <param name="TruncatedCount">
/// Number of patterns that were truncated due to license limits.
/// Zero for Writer Pro tier.
/// </param>
/// <param name="Source">
/// Path to the ignore file that was loaded, or null if patterns were
/// registered programmatically.
/// </param>
public record PatternsReloadedEventArgs(
    int PatternCount,
    int TruncatedCount,
    string? Source
);
