namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Provides quick-fix actions for style violations.
/// </summary>
/// <remarks>
/// LOGIC: IQuickFixService bridges the linting layer with the editor UI:
///
/// Data Flow:
/// 1. User right-clicks or presses Ctrl+. at a position
/// 2. ContextMenuIntegration calls GetQuickFixesAtOffset()
/// 3. QuickFixService queries IViolationProvider for violations
/// 4. For violations with Suggestion, creates QuickFixAction
/// 5. User selects a fix from context menu
/// 6. ApplyQuickFix() replaces text and raises event
///
/// Threading: All methods are called on UI thread.
///
/// Lifecycle: Created per-document, disposed when document closes.
///
/// Version: v0.2.4d
/// </remarks>
public interface IQuickFixService : IDisposable
{
    /// <summary>
    /// Raised when a quick-fix is successfully applied.
    /// </summary>
    /// <remarks>
    /// LOGIC: ManuscriptView subscribes to trigger re-linting
    /// after document is modified by a fix.
    /// </remarks>
    event EventHandler<QuickFixAppliedEventArgs>? QuickFixApplied;

    /// <summary>
    /// Gets all available quick-fixes at a document offset.
    /// </summary>
    /// <param name="offset">Character offset (0-indexed).</param>
    /// <returns>List of available fixes, may be empty.</returns>
    /// <remarks>
    /// LOGIC: Queries IViolationProvider.GetViolationsAtOffset(),
    /// then filters to only violations with Suggestion property set.
    /// </remarks>
    IReadOnlyList<QuickFixAction> GetQuickFixesAtOffset(int offset);

    /// <summary>
    /// Gets the quick-fix for a specific violation.
    /// </summary>
    /// <param name="violation">The violation to get a fix for.</param>
    /// <returns>The fix action, or null if no suggestion available.</returns>
    QuickFixAction? GetQuickFixForViolation(AggregatedStyleViolation violation);

    /// <summary>
    /// Applies a quick-fix to the document.
    /// </summary>
    /// <param name="action">The fix action to apply.</param>
    /// <param name="replaceCallback">
    /// Callback to perform the text replacement: (startOffset, length, newText).
    /// This should use the editor's Replace() method for proper undo support.
    /// </param>
    /// <remarks>
    /// LOGIC: Uses callback pattern to allow the view to perform the edit
    /// through the editor control, ensuring undo/redo integration.
    /// Raises QuickFixApplied after successful replacement.
    /// </remarks>
    void ApplyQuickFix(QuickFixAction action, Action<int, int, string> replaceCallback);
}

/// <summary>
/// Event args for when a quick-fix is applied.
/// </summary>
/// <remarks>
/// LOGIC: Contains information about the applied fix for:
/// - Re-linting the document
/// - Logging/telemetry
/// - Potential future undo grouping
///
/// Version: v0.2.4d
/// </remarks>
public class QuickFixAppliedEventArgs : EventArgs
{
    /// <summary>
    /// The violation that was fixed.
    /// </summary>
    public required string ViolationId { get; init; }

    /// <summary>
    /// The replacement text that was applied.
    /// </summary>
    public required string ReplacementText { get; init; }

    /// <summary>
    /// The document offset where the fix was applied.
    /// </summary>
    public required int Offset { get; init; }

    /// <summary>
    /// The original length of text that was replaced.
    /// </summary>
    public required int OriginalLength { get; init; }
}
