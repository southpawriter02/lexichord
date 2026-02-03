// =============================================================================
// File: CitationFormatterRegistry.cs
// Project: Lexichord.Modules.RAG
// Description: Registry for citation formatters with user preference support.
// =============================================================================
// LOGIC: Collects all registered ICitationFormatter implementations and provides
//   style lookup, user preference management, and preferred formatter access (v0.5.2b).
//   - Constructor receives IEnumerable<ICitationFormatter> (all DI-registered formatters).
//   - Builds a dictionary mapping CitationStyle -> ICitationFormatter for O(1) lookup.
//   - All: Exposes all available formatters for settings UI display.
//   - GetPreferredStyle: Reads the user's preferred style from ISystemSettingsRepository.
//     Falls back to Inline if the stored value is invalid or missing.
//   - SetPreferredStyleAsync: Persists the user's preferred style to settings.
//   - GetFormatter: Returns the formatter for a specific style.
//   - GetPreferredFormatter: Convenience method combining GetPreferredStyle + GetFormatter.
//   - Thread-safe: the dictionary is built once in the constructor and is read-only.
//   - Design adaptation: Uses ISystemSettingsRepository (v0.0.5d) instead of the spec's
//     ISettingsService, matching the actual codebase pattern.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Registry for citation formatters with user preference support.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CitationFormatterRegistry"/> acts as a facade over all registered
/// <see cref="ICitationFormatter"/> implementations. It provides:
/// <list type="bullet">
///   <item><description>Style-to-formatter lookup via <see cref="GetFormatter"/>.</description></item>
///   <item><description>User preference persistence via <see cref="GetPreferredStyle"/> and <see cref="SetPreferredStyleAsync"/>.</description></item>
///   <item><description>Preferred formatter access via <see cref="GetPreferredFormatter"/>.</description></item>
///   <item><description>Full formatter enumeration via <see cref="All"/> for settings UI.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>DI Registration:</b> All <see cref="ICitationFormatter"/> implementations are
/// registered individually in the DI container. The registry receives them as
/// <see cref="IEnumerable{ICitationFormatter}"/> and builds an internal dictionary.
/// </para>
/// <para>
/// <b>Settings Persistence:</b> User preferences are stored via
/// <see cref="ISystemSettingsRepository"/> (v0.0.5d) under the keys defined in
/// <see cref="CitationSettingsKeys"/>. The default style is <c>Inline</c>.
/// </para>
/// <para>
/// <b>Thread Safety:</b> The formatter dictionary is built once during construction
/// and is read-only thereafter. Settings access delegates to the thread-safe
/// <see cref="ISystemSettingsRepository"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2b as part of the Citation Engine.
/// </para>
/// </remarks>
public sealed class CitationFormatterRegistry
{
    private readonly IReadOnlyDictionary<CitationStyle, ICitationFormatter> _formatters;
    private readonly ISystemSettingsRepository _settings;
    private readonly ILogger<CitationFormatterRegistry> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CitationFormatterRegistry"/> class.
    /// </summary>
    /// <param name="formatters">
    /// All registered citation formatters. Each formatter must have a unique
    /// <see cref="ICitationFormatter.Style"/> value. Duplicate styles will cause
    /// the last-registered formatter to win.
    /// </param>
    /// <param name="settings">
    /// Settings repository for persisting user preferences (default style, etc.).
    /// </param>
    /// <param name="logger">
    /// Logger for structured diagnostic output during registry operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public CitationFormatterRegistry(
        IEnumerable<ICitationFormatter> formatters,
        ISystemSettingsRepository settings,
        ILogger<CitationFormatterRegistry> logger)
    {
        ArgumentNullException.ThrowIfNull(formatters);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // LOGIC: Build the style-to-formatter lookup dictionary.
        // If multiple formatters claim the same style, the last one wins.
        // This is logged as a warning since it likely indicates a DI misconfiguration.
        var formatterDict = new Dictionary<CitationStyle, ICitationFormatter>();
        foreach (var formatter in formatters)
        {
            if (formatterDict.ContainsKey(formatter.Style))
            {
                _logger.LogWarning(
                    "Duplicate formatter registered for style {Style}: {FormatterType} replaces {ExistingType}",
                    formatter.Style,
                    formatter.GetType().Name,
                    formatterDict[formatter.Style].GetType().Name);
            }

            formatterDict[formatter.Style] = formatter;
        }

        _formatters = formatterDict;

        _logger.LogDebug(
            "CitationFormatterRegistry initialized with {Count} formatters: {Styles}",
            _formatters.Count,
            string.Join(", ", _formatters.Keys));
    }

    /// <summary>
    /// Gets all available formatters.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns a snapshot of all registered formatters for display in the
    /// settings UI. The order may vary; consumers should sort by
    /// <see cref="ICitationFormatter.Style"/> for consistent display order.
    /// </remarks>
    /// <value>
    /// A read-only collection of all registered <see cref="ICitationFormatter"/> instances.
    /// </value>
    public IReadOnlyCollection<ICitationFormatter> All => _formatters.Values.ToList();

    /// <summary>
    /// Gets the user's preferred citation style.
    /// </summary>
    /// <returns>
    /// The <see cref="CitationStyle"/> stored in user preferences, or
    /// <see cref="CitationStyle.Inline"/> if the stored value is missing or invalid.
    /// </returns>
    /// <remarks>
    /// LOGIC: Reads the <see cref="CitationSettingsKeys.DefaultStyle"/> key from
    /// <see cref="ISystemSettingsRepository"/>. If the stored string cannot be parsed
    /// as a <see cref="CitationStyle"/> enum value, falls back to <c>Inline</c>
    /// and logs a warning.
    /// </remarks>
    public async Task<CitationStyle> GetPreferredStyleAsync(CancellationToken ct = default)
    {
        var styleName = await _settings.GetValueAsync(
            CitationSettingsKeys.DefaultStyle,
            CitationStyle.Inline.ToString(),
            ct);

        _logger.LogDebug(
            "Retrieved preferred citation style setting: '{StyleName}'",
            styleName);

        if (Enum.TryParse<CitationStyle>(styleName, ignoreCase: true, out var style))
        {
            return style;
        }

        _logger.LogWarning(
            "Invalid citation style preference '{StyleName}', using Inline",
            styleName);
        return CitationStyle.Inline;
    }

    /// <summary>
    /// Sets the user's preferred citation style.
    /// </summary>
    /// <param name="style">
    /// The citation style to set as the user's preference.
    /// </param>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// A task that completes when the preference is persisted.
    /// </returns>
    /// <remarks>
    /// LOGIC: Persists the string representation of the <see cref="CitationStyle"/>
    /// to <see cref="ISystemSettingsRepository"/> under the
    /// <see cref="CitationSettingsKeys.DefaultStyle"/> key.
    /// </remarks>
    public async Task SetPreferredStyleAsync(CitationStyle style, CancellationToken ct = default)
    {
        await _settings.SetValueAsync(
            CitationSettingsKeys.DefaultStyle,
            style.ToString(),
            "User's preferred citation format style",
            ct);

        _logger.LogDebug("Citation style preference set to {Style}", style);
    }

    /// <summary>
    /// Gets formatter for the specified style.
    /// </summary>
    /// <param name="style">
    /// The citation style to retrieve a formatter for.
    /// </param>
    /// <returns>
    /// The <see cref="ICitationFormatter"/> registered for the specified style.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when no formatter is registered for the specified <paramref name="style"/>.
    /// </exception>
    /// <remarks>
    /// LOGIC: Performs an O(1) dictionary lookup. Throws if the style is not found,
    /// which indicates a DI registration gap (all three built-in styles should always
    /// be registered).
    /// </remarks>
    public ICitationFormatter GetFormatter(CitationStyle style)
    {
        if (_formatters.TryGetValue(style, out var formatter))
        {
            return formatter;
        }

        _logger.LogError(
            "No formatter registered for citation style {Style}",
            style);

        throw new ArgumentException(
            $"No formatter registered for style {style}", nameof(style));
    }

    /// <summary>
    /// Gets the formatter for the user's preferred style.
    /// </summary>
    /// <param name="ct">
    /// Cancellation token for cooperative cancellation.
    /// </param>
    /// <returns>
    /// The <see cref="ICitationFormatter"/> for the user's preferred style.
    /// Falls back to the <see cref="CitationStyle.Inline"/> formatter if the
    /// preferred style is not available.
    /// </returns>
    /// <remarks>
    /// LOGIC: Combines <see cref="GetPreferredStyleAsync"/> and <see cref="GetFormatter"/>
    /// for convenience. Used by <c>ICitationClipboardService</c> (v0.5.2d) when
    /// the user copies a citation using the default action.
    /// </remarks>
    public async Task<ICitationFormatter> GetPreferredFormatterAsync(CancellationToken ct = default)
    {
        var style = await GetPreferredStyleAsync(ct);
        return GetFormatter(style);
    }
}
