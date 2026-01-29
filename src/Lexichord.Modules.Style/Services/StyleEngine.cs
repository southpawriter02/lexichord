using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Implementation of <see cref="IStyleEngine"/> providing style analysis capabilities.
/// </summary>
/// <remarks>
/// LOGIC: StyleEngine is the core of the Style module.
/// It maintains the active style sheet and analyzes content for violations.
///
/// Thread Safety:
/// - _activeSheet protected by lock for concurrent access
/// - AnalyzeAsync is reentrant-safe (reads sheet once, then processes)
///
/// Current Implementation (v0.2.1a):
/// - Skeleton returning empty results
/// - Full analysis implementation in v0.2.1b
/// </remarks>
public sealed class StyleEngine : IStyleEngine
{
    private readonly ILogger<StyleEngine> _logger;
    private readonly object _sheetLock = new();
    private StyleSheet _activeSheet = StyleSheet.Empty;

    /// <inheritdoc/>
    public event EventHandler<StyleSheetChangedEventArgs>? StyleSheetChanged;

    /// <summary>
    /// Initializes a new instance of <see cref="StyleEngine"/>.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public StyleEngine(ILogger<StyleEngine> logger)
    {
        _logger = logger;
        _logger.LogDebug("StyleEngine initialized with empty style sheet");
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<StyleViolation>> AnalyzeAsync(
        string content,
        CancellationToken cancellationToken = default)
    {
        var sheet = GetActiveStyleSheet();
        return AnalyzeAsync(content, sheet, cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: v0.2.1a skeleton - returns empty violations.
    /// Full implementation in v0.2.1b will:
    /// 1. Iterate through all rules in the sheet
    /// 2. Apply pattern matching (regex, literal, word list)
    /// 3. Collect violations with position information
    /// 4. Return sorted by position
    /// </remarks>
    public Task<IReadOnlyList<StyleViolation>> AnalyzeAsync(
        string content,
        StyleSheet styleSheet,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(styleSheet);

        // LOGIC: Handle null/empty content gracefully
        if (string.IsNullOrEmpty(content))
        {
            _logger.LogDebug("AnalyzeAsync called with empty content, returning no violations");
            return Task.FromResult<IReadOnlyList<StyleViolation>>([]);
        }

        // LOGIC: Handle empty style sheet
        if (styleSheet.Rules.Count == 0)
        {
            _logger.LogDebug("AnalyzeAsync called with empty style sheet, returning no violations");
            return Task.FromResult<IReadOnlyList<StyleViolation>>([]);
        }

        _logger.LogDebug(
            "Analyzing content ({Length} chars) against {RuleCount} rules",
            content.Length,
            styleSheet.Rules.Count);

        // TODO v0.2.1b: Implement actual pattern matching
        // For now, return empty list (skeleton implementation)
        return Task.FromResult<IReadOnlyList<StyleViolation>>([]);
    }

    /// <inheritdoc/>
    public StyleSheet GetActiveStyleSheet()
    {
        lock (_sheetLock)
        {
            return _activeSheet;
        }
    }

    /// <inheritdoc/>
    public void SetActiveStyleSheet(StyleSheet styleSheet)
    {
        ArgumentNullException.ThrowIfNull(styleSheet);

        StyleSheet oldSheet;
        lock (_sheetLock)
        {
            oldSheet = _activeSheet;
            _activeSheet = styleSheet;
        }

        _logger.LogInformation(
            "Active style sheet changed from '{OldName}' to '{NewName}' ({RuleCount} rules)",
            oldSheet.Name,
            styleSheet.Name,
            styleSheet.Rules.Count);

        // LOGIC: Raise event outside of lock to prevent deadlock
        OnStyleSheetChanged(oldSheet, styleSheet, StyleSheetChangeSource.Api);
    }

    /// <summary>
    /// Raises the <see cref="StyleSheetChanged"/> event.
    /// </summary>
    private void OnStyleSheetChanged(
        StyleSheet oldSheet,
        StyleSheet newSheet,
        StyleSheetChangeSource source)
    {
        var args = new StyleSheetChangedEventArgs
        {
            OldStyleSheet = oldSheet,
            NewStyleSheet = newSheet,
            ChangeSource = source
        };

        StyleSheetChanged?.Invoke(this, args);
    }
}
