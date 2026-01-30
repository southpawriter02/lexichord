using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Editor.Services.QuickFix;

/// <summary>
/// Provides quick-fix actions for style violations.
/// </summary>
/// <remarks>
/// LOGIC: QuickFixService bridges linting and editor:
///
/// 1. Queries IViolationProvider for violations at caret
/// 2. Filters to violations with Suggestion property
/// 3. Creates QuickFixAction for each
/// 4. ApplyQuickFix uses callback for editor integration
///
/// Thread Safety: All methods run on UI thread.
///
/// Version: v0.2.4d
/// </remarks>
public sealed class QuickFixService : IQuickFixService
{
    private readonly IViolationProvider _violationProvider;
    private readonly ILogger<QuickFixService> _logger;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="QuickFixService"/> class.
    /// </summary>
    /// <param name="violationProvider">Provider for style violations.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    public QuickFixService(
        IViolationProvider violationProvider,
        ILogger<QuickFixService> logger)
    {
        _violationProvider = violationProvider ?? throw new ArgumentNullException(nameof(violationProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public event EventHandler<QuickFixAppliedEventArgs>? QuickFixApplied;

    /// <inheritdoc />
    public IReadOnlyList<QuickFixAction> GetQuickFixesAtOffset(int offset)
    {
        ThrowIfDisposed();

        var violations = _violationProvider.GetViolationsAtOffset(offset);

        _logger.LogDebug(
            "GetQuickFixesAtOffset({Offset}): {Count} violations found",
            offset,
            violations.Count);

        var fixes = new List<QuickFixAction>();

        foreach (var violation in violations)
        {
            var fix = GetQuickFixForViolation(violation);
            if (fix is not null)
            {
                fixes.Add(fix);
            }
        }

        _logger.LogDebug(
            "GetQuickFixesAtOffset({Offset}): {Count} fixes available",
            offset,
            fixes.Count);

        return fixes;
    }

    /// <inheritdoc />
    public QuickFixAction? GetQuickFixForViolation(AggregatedStyleViolation violation)
    {
        ThrowIfDisposed();

        if (violation is null)
            return null;

        if (!violation.HasSuggestion)
        {
            _logger.LogDebug(
                "Violation {ViolationId} has no suggestion",
                violation.Id);
            return null;
        }

        // LOGIC: Build user-friendly title for context menu
        var title = BuildFixTitle(violation);

        return new QuickFixAction
        {
            ViolationId = violation.Id,
            Title = title,
            ReplacementText = violation.Suggestion!,
            StartOffset = violation.StartOffset,
            Length = violation.Length
        };
    }

    /// <inheritdoc />
    public void ApplyQuickFix(QuickFixAction action, Action<int, int, string> replaceCallback)
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentNullException.ThrowIfNull(replaceCallback);
        ThrowIfDisposed();

        _logger.LogInformation(
            "Applying quick-fix: replacing [{Start}, {End}) with '{Replacement}'",
            action.StartOffset,
            action.EndOffset,
            action.ReplacementText);

        // LOGIC: Delegate to editor for proper undo support
        replaceCallback(action.StartOffset, action.Length, action.ReplacementText);

        // LOGIC: Raise event for re-linting
        var eventArgs = new QuickFixAppliedEventArgs
        {
            ViolationId = action.ViolationId,
            ReplacementText = action.ReplacementText,
            Offset = action.StartOffset,
            OriginalLength = action.Length
        };

        OnQuickFixApplied(eventArgs);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _logger.LogDebug("QuickFixService disposed");
    }

    /// <summary>
    /// Builds a user-friendly title for the fix menu item.
    /// </summary>
    private static string BuildFixTitle(AggregatedStyleViolation violation)
    {
        // LOGIC: Format as "Replace 'X' with 'Y'" or just "Use 'Y'" if text is too long
        var originalText = violation.ViolatingText;
        var suggestion = violation.Suggestion!;

        if (originalText.Length > 20)
        {
            return $"Use '{suggestion}'";
        }

        return $"Replace '{originalText}' with '{suggestion}'";
    }

    /// <summary>
    /// Raises the QuickFixApplied event.
    /// </summary>
    private void OnQuickFixApplied(QuickFixAppliedEventArgs args)
    {
        QuickFixApplied?.Invoke(this, args);
    }

    /// <summary>
    /// Throws if the service has been disposed.
    /// </summary>
    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}
