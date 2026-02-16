// =============================================================================
// File: IssuePresentationGroup.cs
// Project: Lexichord.Modules.Agents
// Description: Groups issues by severity for display in the Unified Issues Panel.
// =============================================================================
// LOGIC: Represents a collapsible group of issues organized by severity level.
//   Each group contains a list of IssuePresentation items and provides UI state
//   for expand/collapse behavior and count display.
//
// v0.7.5g: Unified Issues Panel
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts.Knowledge.Validation.Integration;
using Lexichord.Abstractions.Contracts.Validation;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// Represents a group of validation issues organized by severity level.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This class groups issues for display in the Unified Issues Panel.
/// Issues are grouped by severity (Error, Warning, Info, Hint) with each group
/// appearing as a collapsible section in the UI. Key responsibilities:
/// </para>
/// <list type="bullet">
///   <item><description>Holds an <see cref="ObservableCollection{T}"/> of <see cref="IssuePresentation"/> items</description></item>
///   <item><description>Tracks expand/collapse state for the group section</description></item>
///   <item><description>Provides display properties (label, icon, count)</description></item>
///   <item><description>Enables filtering within the group</description></item>
/// </list>
/// <para>
/// <b>Lifecycle:</b>
/// Created by <see cref="UnifiedIssuesPanelViewModel"/> when refreshing the panel
/// with new validation results. Not DI-registered — instances are created manually.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is designed for UI thread access only.
/// Property changes raise <see cref="ObservableObject.PropertyChanged"/> events
/// for data binding.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5g as part of the Unified Issues Panel feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a group for errors
/// var errorGroup = new IssuePresentationGroup(
///     UnifiedSeverity.Error,
///     "Errors",
///     errorIssues.Select(i => new IssuePresentation(i)));
///
/// // Collapse the group
/// errorGroup.IsExpanded = false;
///
/// // Access items
/// foreach (var item in errorGroup.Items)
/// {
///     Console.WriteLine($"  {item.Issue.Message}");
/// }
/// </code>
/// </example>
/// <seealso cref="IssuePresentation"/>
/// <seealso cref="UnifiedIssuesPanelViewModel"/>
/// <seealso cref="UnifiedSeverity"/>
public sealed partial class IssuePresentationGroup : ObservableObject
{
    // ── Logger (optional for debugging) ─────────────────────────────────
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="IssuePresentationGroup"/> class.
    /// </summary>
    /// <param name="severity">The severity level for this group.</param>
    /// <param name="label">The display label (e.g., "Errors", "Warnings").</param>
    /// <param name="issues">The issues to include in this group.</param>
    /// <param name="logger">Optional logger for debugging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="label"/> or <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Creates the group with:
    /// <list type="bullet">
    ///   <item><description>Severity-based icon selection</description></item>
    ///   <item><description>Issues wrapped in an <see cref="ObservableCollection{T}"/></description></item>
    ///   <item><description>Groups start expanded by default</description></item>
    /// </list>
    /// </remarks>
    public IssuePresentationGroup(
        UnifiedSeverity severity,
        string label,
        IEnumerable<IssuePresentation> issues,
        ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(issues);

        _logger = logger;

        Severity = severity;
        Label = label;
        Icon = GetIconForSeverity(severity);
        Items = new ObservableCollection<IssuePresentation>(issues);

        _logger?.LogDebug(
            "Created IssuePresentationGroup: Severity={Severity}, Count={Count}",
            severity, Items.Count);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="IssuePresentationGroup"/> class
    /// from a list of <see cref="UnifiedIssue"/> records.
    /// </summary>
    /// <param name="severity">The severity level for this group.</param>
    /// <param name="label">The display label.</param>
    /// <param name="issues">The unified issues to include.</param>
    /// <param name="logger">Optional logger for debugging.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="label"/> or <paramref name="issues"/> is null.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience constructor that wraps each <see cref="UnifiedIssue"/>
    /// in an <see cref="IssuePresentation"/> instance.
    /// </remarks>
    public IssuePresentationGroup(
        UnifiedSeverity severity,
        string label,
        IEnumerable<UnifiedIssue> issues,
        ILogger? logger = null)
        : this(severity, label, issues.Select(i => new IssuePresentation(i)), logger)
    {
    }

    // ── Observable Properties ───────────────────────────────────────────

    /// <summary>
    /// Gets or sets whether the group is expanded in the UI.
    /// </summary>
    /// <value>
    /// <c>true</c> if expanded (showing items); <c>false</c> if collapsed.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Groups start expanded by default. When collapsed, only
    /// the group header with count badge is visible. Toggled by clicking the
    /// group header or using keyboard navigation.
    /// </remarks>
    [ObservableProperty]
    private bool _isExpanded = true;

    // ── Immutable Properties ────────────────────────────────────────────

    /// <summary>
    /// Gets the severity level for this group.
    /// </summary>
    /// <value>
    /// One of <see cref="UnifiedSeverity"/> values: Error, Warning, Info, Hint.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Determines the visual styling (color, icon) and sort order
    /// of this group in the panel. Error groups appear first, followed by
    /// Warning, Info, and Hint.
    /// </remarks>
    public UnifiedSeverity Severity { get; }

    /// <summary>
    /// Gets the display label for the group header.
    /// </summary>
    /// <value>
    /// A human-readable label such as "Errors", "Warnings", "Info", or "Hints".
    /// </value>
    public string Label { get; }

    /// <summary>
    /// Gets the icon name for visual representation.
    /// </summary>
    /// <value>
    /// An icon identifier for the UI, e.g., "ErrorIcon", "WarningIcon".
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The icon name is resolved by the UI layer to an actual
    /// icon resource. Mapping:
    /// <list type="bullet">
    ///   <item><description>Error → "ErrorIcon" (typically red circle with X)</description></item>
    ///   <item><description>Warning → "WarningIcon" (typically yellow triangle)</description></item>
    ///   <item><description>Info → "InfoIcon" (typically blue circle with i)</description></item>
    ///   <item><description>Hint → "HintIcon" (typically gray lightbulb)</description></item>
    /// </list>
    /// </remarks>
    public string Icon { get; }

    /// <summary>
    /// Gets the collection of issues in this group.
    /// </summary>
    /// <value>
    /// An <see cref="ObservableCollection{T}"/> of <see cref="IssuePresentation"/>
    /// items that belong to this severity group.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> The collection is observable to support dynamic updates
    /// when issues are dismissed or when filters change. UI binds to this
    /// collection for rendering individual issue items.
    /// </remarks>
    public ObservableCollection<IssuePresentation> Items { get; }

    // ── Computed Properties ─────────────────────────────────────────────

    /// <summary>
    /// Gets the number of issues in this group.
    /// </summary>
    /// <value>
    /// The count of items in <see cref="Items"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Displayed in the group header as a badge (e.g., "Errors (3)").
    /// Note: This property does NOT automatically update when Items changes.
    /// Call <see cref="NotifyCountChanged"/> after modifying Items.
    /// </remarks>
    public int Count => Items.Count;

    /// <summary>
    /// Gets whether this group has any issues.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Count"/> is greater than zero.
    /// </value>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Gets the number of auto-fixable issues in this group.
    /// </summary>
    /// <value>
    /// Count of items where <see cref="IssuePresentation.CanAutoApply"/> is <c>true</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used to enable/disable group-level "Fix All" buttons.
    /// </remarks>
    public int AutoFixableCount => Items.Count(i => i.CanAutoApply);

    /// <summary>
    /// Gets the number of non-suppressed issues in this group.
    /// </summary>
    /// <value>
    /// Count of items where <see cref="IssuePresentation.IsSuppressed"/> is <c>false</c>.
    /// </value>
    public int ActiveCount => Items.Count(i => !i.IsSuppressed);

    // ── Public Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Notifies that the <see cref="Count"/> property has changed.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Call this method after adding or removing items from
    /// <see cref="Items"/> to update the count display in the UI.
    /// </remarks>
    public void NotifyCountChanged()
    {
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(HasItems));
        OnPropertyChanged(nameof(AutoFixableCount));
        OnPropertyChanged(nameof(ActiveCount));

        _logger?.LogDebug(
            "Group count updated: Severity={Severity}, Count={Count}, Active={Active}",
            Severity, Count, ActiveCount);
    }

    /// <summary>
    /// Toggles the expansion state of the group.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience method for keyboard navigation (Enter/Space on header).
    /// </remarks>
    public void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;

        _logger?.LogDebug(
            "Group expansion toggled: Severity={Severity}, IsExpanded={IsExpanded}",
            Severity, IsExpanded);
    }

    // ── Private Helpers ─────────────────────────────────────────────────

    /// <summary>
    /// Maps a severity level to an icon name.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The icon name for UI rendering.</returns>
    private static string GetIconForSeverity(UnifiedSeverity severity) =>
        severity switch
        {
            UnifiedSeverity.Error => "ErrorIcon",
            UnifiedSeverity.Warning => "WarningIcon",
            UnifiedSeverity.Info => "InfoIcon",
            UnifiedSeverity.Hint => "HintIcon",
            _ => "DefaultIcon"
        };

    // ── Factory Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Creates an error group from unified issues.
    /// </summary>
    /// <param name="issues">The error-level issues.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new <see cref="IssuePresentationGroup"/> for errors.</returns>
    public static IssuePresentationGroup Errors(
        IEnumerable<UnifiedIssue> issues,
        ILogger? logger = null) =>
        new(UnifiedSeverity.Error, "Errors", issues, logger);

    /// <summary>
    /// Creates a warning group from unified issues.
    /// </summary>
    /// <param name="issues">The warning-level issues.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new <see cref="IssuePresentationGroup"/> for warnings.</returns>
    public static IssuePresentationGroup Warnings(
        IEnumerable<UnifiedIssue> issues,
        ILogger? logger = null) =>
        new(UnifiedSeverity.Warning, "Warnings", issues, logger);

    /// <summary>
    /// Creates an info group from unified issues.
    /// </summary>
    /// <param name="issues">The info-level issues.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new <see cref="IssuePresentationGroup"/> for info items.</returns>
    public static IssuePresentationGroup Infos(
        IEnumerable<UnifiedIssue> issues,
        ILogger? logger = null) =>
        new(UnifiedSeverity.Info, "Info", issues, logger);

    /// <summary>
    /// Creates a hints group from unified issues.
    /// </summary>
    /// <param name="issues">The hint-level issues.</param>
    /// <param name="logger">Optional logger.</param>
    /// <returns>A new <see cref="IssuePresentationGroup"/> for hints.</returns>
    public static IssuePresentationGroup Hints(
        IEnumerable<UnifiedIssue> issues,
        ILogger? logger = null) =>
        new(UnifiedSeverity.Hint, "Hints", issues, logger);
}
