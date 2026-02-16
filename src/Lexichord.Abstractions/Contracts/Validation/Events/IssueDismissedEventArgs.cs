// =============================================================================
// File: IssueDismissedEventArgs.cs
// Project: Lexichord.Abstractions
// Description: Event arguments for issue dismissal events from the Unified Issues Panel.
// =============================================================================
// LOGIC: Published when the user dismisses (suppresses) an issue in the Unified Issues
//   Panel without applying a fix. Dismissed issues are visually marked as suppressed
//   but remain in the list for reference.
//
// v0.7.5g: Unified Issues Panel
// =============================================================================

namespace Lexichord.Abstractions.Contracts.Validation.Events;

/// <summary>
/// Event arguments for when the user dismisses (suppresses) an issue from the Unified Issues Panel.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is raised when the user clicks "Dismiss" or "X" on an issue
/// in the Unified Issues Panel. Dismissing an issue:
/// <list type="bullet">
///   <item><description>Does NOT modify the document</description></item>
///   <item><description>Marks the issue as suppressed in the UI (grayed out)</description></item>
///   <item><description>Optionally persists the suppression to user settings</description></item>
///   <item><description>Allows the user to see what they've dismissed</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// <list type="bullet">
///   <item><description>False positives: User determines the issue is not relevant</description></item>
///   <item><description>Intentional violations: User wants to deviate from style rules</description></item>
///   <item><description>Low priority: User defers the issue for later attention</description></item>
///   <item><description>Context-specific: Issue is valid but not applicable in this case</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Analytics:</b>
/// Subscribers can track dismissal patterns to identify rules that generate
/// frequent false positives or have low user acceptance.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is immutable and thread-safe.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
/// <seealso cref="UnifiedIssue"/>
/// <seealso cref="FixRequestedEventArgs"/>
public sealed class IssueDismissedEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueDismissedEventArgs"/> class.
    /// </summary>
    /// <param name="issue">The unified issue that was dismissed.</param>
    /// <param name="reason">Optional reason for dismissal provided by the user.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issue"/> is null.
    /// </exception>
    public IssueDismissedEventArgs(UnifiedIssue issue, string? reason = null)
    {
        ArgumentNullException.ThrowIfNull(issue);

        Issue = issue;
        Reason = reason;
        Timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the unified issue that was dismissed.
    /// </summary>
    /// <value>
    /// The <see cref="UnifiedIssue"/> that the user chose to suppress.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Contains the full issue context including:
    /// <list type="bullet">
    ///   <item><description><see cref="UnifiedIssue.SourceId"/>: Rule or validation code</description></item>
    ///   <item><description><see cref="UnifiedIssue.Category"/>: Issue category for analytics</description></item>
    ///   <item><description><see cref="UnifiedIssue.Severity"/>: Original severity level</description></item>
    ///   <item><description><see cref="UnifiedIssue.Message"/>: Issue description</description></item>
    /// </list>
    /// This information can be used to persist suppressions (e.g., by rule ID + location).
    /// </remarks>
    public UnifiedIssue Issue { get; }

    /// <summary>
    /// Gets the optional reason for dismissal provided by the user.
    /// </summary>
    /// <value>
    /// A user-provided explanation for why the issue was dismissed,
    /// or <c>null</c> if no reason was provided.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Some UI implementations may prompt the user for a reason
    /// when dismissing issues. This data is valuable for:
    /// <list type="bullet">
    ///   <item><description>Improving rule accuracy (identifying false positives)</description></item>
    ///   <item><description>Team documentation (why exceptions were made)</description></item>
    ///   <item><description>Future reference (remind user why they dismissed)</description></item>
    /// </list>
    /// </remarks>
    public string? Reason { get; }

    /// <summary>
    /// Gets the timestamp when the issue was dismissed.
    /// </summary>
    /// <value>
    /// UTC timestamp of when the user dismissed the issue.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for:
    /// <list type="bullet">
    ///   <item><description>Logging and audit trails</description></item>
    ///   <item><description>Analytics (time to dismissal after seeing issue)</description></item>
    ///   <item><description>Suppression expiration (time-limited suppressions)</description></item>
    /// </list>
    /// </remarks>
    public DateTime Timestamp { get; }

    /// <summary>
    /// Creates a new <see cref="IssueDismissedEventArgs"/> without a reason.
    /// </summary>
    /// <param name="issue">The unified issue that was dismissed.</param>
    /// <returns>A new <see cref="IssueDismissedEventArgs"/> with no reason.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issue"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory for quick dismissals where the user
    /// does not provide an explanation.
    /// </remarks>
    public static IssueDismissedEventArgs Create(UnifiedIssue issue) =>
        new(issue);

    /// <summary>
    /// Creates a new <see cref="IssueDismissedEventArgs"/> with a reason.
    /// </summary>
    /// <param name="issue">The unified issue that was dismissed.</param>
    /// <param name="reason">The user's reason for dismissal.</param>
    /// <returns>A new <see cref="IssueDismissedEventArgs"/> with the provided reason.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="issue"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method for dismissals with user-provided context,
    /// typically from a dialog prompt.
    /// </remarks>
    public static IssueDismissedEventArgs CreateWithReason(UnifiedIssue issue, string reason) =>
        new(issue, reason);
}
