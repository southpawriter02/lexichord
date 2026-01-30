namespace Lexichord.Abstractions.Contracts.Linting;

/// <summary>
/// Provides style violations to the rendering layer.
/// </summary>
/// <remarks>
/// LOGIC: IViolationProvider acts as the bridge between the linting pipeline
/// and the editor rendering. It maintains the current set of violations for
/// a document and notifies renderers when violations change.
///
/// Data Flow:
/// 1. LintingOrchestrator produces LintResult
/// 2. ViolationAggregator transforms to AggregatedStyleViolation[]
/// 3. ViolationProvider receives via UpdateViolations()
/// 4. StyleViolationRenderer queries via GetViolationsInRange()
///
/// Threading: All methods must be thread-safe. Violations are read by the
/// UI thread during rendering, but may be updated from background lint threads.
///
/// Lifecycle: Created per-document, disposed when document closes.
///
/// Version: v0.2.4a
/// </remarks>
public interface IViolationProvider : IDisposable
{
    /// <summary>
    /// Raised when the violation collection changes.
    /// </summary>
    /// <remarks>
    /// LOGIC: Renderers subscribe to this event to know when to invalidate.
    /// The event args contain counts to enable efficient status bar updates.
    /// </remarks>
    event EventHandler<ViolationsChangedEventArgs>? ViolationsChanged;

    /// <summary>
    /// Gets all violations for the document.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns an immutable snapshot of the current violations.
    /// Safe to enumerate while updates are happening.
    /// </remarks>
    IReadOnlyList<AggregatedStyleViolation> AllViolations { get; }

    /// <summary>
    /// Updates the violation collection with new violations.
    /// </summary>
    /// <param name="violations">The new set of violations.</param>
    /// <remarks>
    /// LOGIC: Replaces the entire collection and raises ViolationsChanged.
    /// This is the primary update path after a lint operation completes.
    ///
    /// Thread Safety: Can be called from any thread. Internally synchronizes
    /// access and marshals the event to the UI thread if needed.
    /// </remarks>
    void UpdateViolations(IReadOnlyList<AggregatedStyleViolation> violations);

    /// <summary>
    /// Clears all violations.
    /// </summary>
    /// <remarks>
    /// LOGIC: Called when document is closed or linting is disabled.
    /// Raises ViolationsChanged with ChangeType.Cleared.
    /// </remarks>
    void Clear();

    /// <summary>
    /// Gets violations that overlap a character range.
    /// </summary>
    /// <param name="startOffset">Start of range (inclusive, 0-indexed).</param>
    /// <param name="endOffset">End of range (exclusive).</param>
    /// <returns>All violations that overlap the specified range.</returns>
    /// <remarks>
    /// LOGIC: Used by the renderer to get only violations visible in
    /// the current viewport or line. Optimizes rendering by avoiding
    /// iteration over the entire violation list.
    ///
    /// A violation overlaps if any part of it falls within [start, end).
    /// </remarks>
    IReadOnlyList<AggregatedStyleViolation> GetViolationsInRange(
        int startOffset,
        int endOffset);

    /// <summary>
    /// Gets a violation at a specific offset.
    /// </summary>
    /// <param name="offset">Character offset (0-indexed).</param>
    /// <returns>The first violation containing the offset, or null.</returns>
    /// <remarks>
    /// LOGIC: Used for hover tooltips and click-to-navigate.
    /// Returns the first match if multiple violations overlap the offset.
    /// </remarks>
    AggregatedStyleViolation? GetViolationAt(int offset);
}
