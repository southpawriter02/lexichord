using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Lexichord.Abstractions.Contracts.Linting;

#region Enums

/// <summary>
/// Defines the scope of violations displayed in the Problems Panel.
/// </summary>
/// <remarks>
/// LOGIC: Scope modes control which documents' violations are shown.
/// v0.2.6a implements CurrentFile only; other modes are placeholders
/// for future versions (v0.2.6c).
///
/// Version: v0.2.6a
/// </remarks>
public enum ScopeModeType
{
    /// <summary>Shows violations for the currently active document only.</summary>
    CurrentFile = 0,

    /// <summary>Shows violations for all open documents. (Future: v0.2.6c)</summary>
    OpenFiles = 1,

    /// <summary>Shows violations for the entire workspace/project. (Future: v0.2.6c)</summary>
    Workspace = 2
}

#endregion

#region Problem Item Interface

/// <summary>
/// Represents a single problem/violation item in the Problems Panel.
/// </summary>
/// <remarks>
/// LOGIC: Abstract interface for problem items to enable testability
/// and decouple the UI from the concrete ViewModel implementation.
///
/// Properties are mapped from <see cref="AggregatedStyleViolation"/>.
///
/// Version: v0.2.6a
/// </remarks>
public interface IProblemItem
{
    /// <summary>
    /// Gets the unique identifier for this problem.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Gets the line number where the problem starts (1-indexed).
    /// </summary>
    int Line { get; }

    /// <summary>
    /// Gets the column number where the problem starts (1-indexed).
    /// </summary>
    int Column { get; }

    /// <summary>
    /// Gets the human-readable problem message.
    /// </summary>
    string Message { get; }

    /// <summary>
    /// Gets the ID of the rule that was violated.
    /// </summary>
    string RuleId { get; }

    /// <summary>
    /// Gets the severity of the problem.
    /// </summary>
    ViolationSeverity Severity { get; }

    /// <summary>
    /// Gets the text that triggered the violation.
    /// </summary>
    string ViolatingText { get; }

    /// <summary>
    /// Gets the icon character for the severity level.
    /// </summary>
    string SeverityIcon { get; }

    /// <summary>
    /// Gets the document identifier this problem belongs to.
    /// </summary>
    string DocumentId { get; }

    /// <summary>
    /// Gets the character offset where the problem starts.
    /// </summary>
    int StartOffset { get; }
}

#endregion

#region Problem Group Interface

/// <summary>
/// Represents a group of problems organized by severity.
/// </summary>
/// <remarks>
/// LOGIC: Groups allow collapsible organization in the UI.
/// Each severity level (Error, Warning, Info, Hint) has its own group.
///
/// Version: v0.2.6a
/// </remarks>
public interface IProblemGroup : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the severity level for this group.
    /// </summary>
    ViolationSeverity Severity { get; }

    /// <summary>
    /// Gets the display name for this group (e.g., "Errors", "Warnings").
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the count of problems in this group.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets or sets whether this group is expanded in the UI.
    /// </summary>
    bool IsExpanded { get; set; }

    /// <summary>
    /// Gets the collection of problem items in this group.
    /// </summary>
    ReadOnlyObservableCollection<IProblemItem> Items { get; }

    /// <summary>
    /// Gets the severity icon for the group header.
    /// </summary>
    string SeverityIcon { get; }

    /// <summary>
    /// Gets the formatted header text including count.
    /// </summary>
    string HeaderText { get; }
}

#endregion

#region Problems Panel ViewModel Interface

/// <summary>
/// ViewModel interface for the Problems Panel sidebar view.
/// </summary>
/// <remarks>
/// LOGIC: The Problems Panel displays style violations grouped by severity.
/// It receives updates via LintingCompletedEvent and transforms violations
/// into displayable problem items.
///
/// Data Flow:
/// 1. LintingOrchestrator completes scan
/// 2. Publishes LintingCompletedEvent via IMediator
/// 3. ProblemsPanelViewModel.Handle() receives event
/// 4. Checks ScopeMode == CurrentFile
/// 5. Verifies event.DocumentId matches active document
/// 6. Maps violations to ProblemItemViewModels
/// 7. Groups by severity
/// 8. Updates counts
/// 9. UI refreshes via PropertyChanged
///
/// Version: v0.2.6a
/// </remarks>
public interface IProblemsPanelViewModel : INotifyPropertyChanged, IDisposable
{
    /// <summary>
    /// Gets the current scope mode for violation display.
    /// </summary>
    /// <remarks>
    /// LOGIC: v0.2.6a only supports CurrentFile mode.
    /// Future versions will add OpenFiles and Workspace modes.
    /// </remarks>
    ScopeModeType ScopeMode { get; }

    /// <summary>
    /// Gets the total count of all problems across all groups.
    /// </summary>
    int TotalCount { get; }

    /// <summary>
    /// Gets the count of error-severity problems.
    /// </summary>
    int ErrorCount { get; }

    /// <summary>
    /// Gets the count of warning-severity problems.
    /// </summary>
    int WarningCount { get; }

    /// <summary>
    /// Gets the count of info-severity problems.
    /// </summary>
    int InfoCount { get; }

    /// <summary>
    /// Gets the count of hint-severity problems.
    /// </summary>
    int HintCount { get; }

    /// <summary>
    /// Gets whether the panel is currently loading/updating.
    /// </summary>
    bool IsLoading { get; }

    /// <summary>
    /// Gets the collection of problem groups organized by severity.
    /// </summary>
    ReadOnlyObservableCollection<IProblemGroup> Groups { get; }

    /// <summary>
    /// Gets or sets the currently selected problem item.
    /// </summary>
    /// <remarks>
    /// LOGIC: Selection will be used for navigation in v0.2.6b.
    /// </remarks>
    IProblemItem? SelectedItem { get; set; }

    /// <summary>
    /// Gets the identifier of the document currently being displayed.
    /// </summary>
    string? ActiveDocumentId { get; }
}

#endregion
