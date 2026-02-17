// =============================================================================
// File: FixRequestedEventArgs.cs
// Project: Lexichord.Abstractions
// Description: Event arguments for fix request events from the Unified Issues Panel.
// =============================================================================
// LOGIC: Published when the user requests to apply a fix in the Unified Issues Panel.
//   Contains the issue being fixed and the specific fix to apply. Used by UI to
//   coordinate fix application with the panel ViewModel.
//
// v0.7.5g: Unified Issues Panel
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation.Events;

/// <summary>
/// Event arguments for when the user requests to apply a fix from the Unified Issues Panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is raised by UI controls (e.g., "Apply Fix" button) in the
/// Unified Issues Panel when the user clicks to apply a fix for a specific issue.
/// The event carries all information needed to apply the fix:
/// </para>
/// <list type="bullet">
///   <item><description><see cref="Issue"/>: The unified issue being addressed</description></item>
///   <item><description><see cref="Fix"/>: The specific fix to apply (may differ from BestFix if user selected an alternative)</description></item>
///   <item><description><see cref="Timestamp"/>: When the request was made</description></item>
/// </list>
/// <para>
/// <b>Handler Responsibility:</b>
/// The <c>UnifiedIssuesPanelViewModel</c> subscribes to this event (when raised from the view)
/// and applies the fix using <c>IEditorService</c> APIs. The handler should:
/// <list type="number">
///   <item><description>Validate the fix is still applicable (document may have changed)</description></item>
///   <item><description>Apply the fix via <c>BeginUndoGroup</c>/<c>DeleteText</c>/<c>InsertText</c>/<c>EndUndoGroup</c></description></item>
///   <item><description>Trigger a validation refresh to update the issue list</description></item>
///   <item><description>Mark the issue as resolved in the UI</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
/// <seealso cref="UnifiedIssue"/>
/// <seealso cref="UnifiedFix"/>
/// <seealso cref="IssueDismissedEventArgs"/>
public sealed class FixRequestedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixRequestedEventArgs"/> class.
    /// </summary>
    /// <param name="issue">The unified issue being fixed.</param>
    /// <param name="fix">The specific fix to apply.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issue"/> or <paramref name="fix"/> is null.
    /// </exception>
    public FixRequestedEventArgs(UnifiedIssue issue, UnifiedFix fix)
    {
        ArgumentNullException.ThrowIfNull(issue);
        ArgumentNullException.ThrowIfNull(fix);

        Issue = issue;
        Fix = fix;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unified issue being fixed.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedIssue"/> that this fix addresses.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the full issue context including location, severity,
    /// category, and all available fixes. The handler may use this to verify the
    /// fix is still relevant before applying.
    /// </remarks>
    public UnifiedIssue Issue { get; }

    /// <summary>
    /// Gets the specific fix to apply.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedFix"/> containing the replacement text and location.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> This may be the <see cref="UnifiedIssue.BestFix"/> or an
    /// alternative fix selected by the user from the <see cref="UnifiedIssue.Fixes"/> list.
    /// The fix contains all information needed for application:
    /// <list type="bullet">
    ///   <item><description><see cref="UnifiedFix.Location"/>: Where to apply</description></item>
    ///   <item><description><see cref="UnifiedFix.OldText"/>: Text being replaced</description></item>
    ///   <item><description><see cref="UnifiedFix.NewText"/>: Replacement text</description></item>
    ///   <item><description><see cref="UnifiedFix.Type"/>: How to apply (Replacement, Insertion, Deletion)</description></item>
    /// </list>
    /// </remarks>
    public UnifiedFix Fix { get; }

    /// <summary>
    /// Gets the timestamp when the fix was requested.
    /// </summary>
    /// <value>
    /// UTC timestamp of when the user clicked to apply the fix.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for logging, analytics, and potential timeout handling
    /// if the fix application is delayed.
    /// </remarks>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Creates a new <see cref="FixRequestedEventArgs"/> for the issue's best fix.
    /// </summary>
    /// <param name="issue">The unified issue to fix.</param>
    /// <returns>
    /// A new <see cref="FixRequestedEventArgs"/> using <see cref="UnifiedIssue.BestFix"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issue"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the issue has no available fix (<see cref="UnifiedIssue.BestFix"/> is null).
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory for the common case where the user applies
    /// the best (highest confidence) fix without selecting an alternative.
    /// </remarks>
    public static FixRequestedEventArgs CreateForBestFix(UnifiedIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        if (issue.BestFix is null)
        {
            throw new InvalidOperationException(
                $"Cannot create fix request: issue {issue.IssueId} has no available fix.");
        }

        return new FixRequestedEventArgs(issue, issue.BestFix);
    }
}
