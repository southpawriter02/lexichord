namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Configuration options for terminology filtering behavior.
/// </summary>
/// <remarks>
/// LOGIC: Provides configurable defaults for filter controls:
/// - Debounce settings for text search to prevent excessive filtering
/// - Default visibility toggle for inactive terms
/// - Default category and severity filters
///
/// These options are bound via IOptions{FilterOptions} pattern and can be
/// configured in appsettings.json or during service registration.
///
/// Version: v0.2.5b
/// </remarks>
public sealed record FilterOptions
{
    /// <summary>
    /// Gets the configuration section name for binding.
    /// </summary>
    public const string SectionName = "Lexichord:Terminology:Filter";

    /// <summary>
    /// Gets or sets the debounce delay in milliseconds for text search.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents excessive filtering during rapid typing.
    /// Default: 300ms - balances responsiveness with performance.
    /// Range: 50-1000ms recommended.
    /// </remarks>
    public int DebounceMilliseconds { get; init; } = 300;

    /// <summary>
    /// Gets or sets whether inactive terms are shown by default.
    /// </summary>
    /// <remarks>
    /// LOGIC: When false (default), only active terms are displayed.
    /// Users can toggle this in the UI to include inactive terms.
    /// </remarks>
    public bool ShowInactive { get; init; } = false;

    /// <summary>
    /// Gets or sets the default category filter, or null for all categories.
    /// </summary>
    /// <remarks>
    /// LOGIC: When set, only terms in this category are shown by default.
    /// Null means show all categories.
    /// </remarks>
    public string? DefaultCategory { get; init; }

    /// <summary>
    /// Gets or sets the default severity filter, or null for all severities.
    /// </summary>
    /// <remarks>
    /// LOGIC: When set, only terms with this severity are shown by default.
    /// Null means show all severities.
    /// </remarks>
    public string? DefaultSeverity { get; init; }
}
