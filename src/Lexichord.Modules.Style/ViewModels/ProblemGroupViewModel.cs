using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Linting;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.ViewModels;

/// <summary>
/// ViewModel for a severity group in the Problems Panel.
/// </summary>
/// <remarks>
/// LOGIC: Groups problems by severity level for organized display.
/// Each severity (Error, Warning, Info, Hint) has its own collapsible group.
///
/// Design Decisions:
/// - ObservableCollection for dynamic UI updates
/// - ReadOnlyObservableCollection for external access
/// - IsExpanded state for collapse/expand functionality
/// - Count derived from Items.Count for consistency
///
/// Version: v0.2.6a
/// </remarks>
public sealed partial class ProblemGroupViewModel : ObservableObject, IProblemGroup
{
    private readonly ILogger<ProblemGroupViewModel>? _logger;
    private readonly ObservableCollection<IProblemItem> _items;

    #region Observable Properties

    /// <summary>
    /// Gets or sets whether this group is expanded in the UI.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded = true;

    #endregion

    #region Properties

    /// <inheritdoc/>
    public ViolationSeverity Severity { get; }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Human-readable display names for each severity:
    /// - Error → "Errors"
    /// - Warning → "Warnings"
    /// - Info → "Info"
    /// - Hint → "Hints"
    /// </remarks>
    public string DisplayName { get; }

    /// <inheritdoc/>
    public int Count => _items.Count;

    /// <inheritdoc/>
    public ReadOnlyObservableCollection<IProblemItem> Items { get; }

    /// <summary>
    /// Gets the severity icon for the group header.
    /// </summary>
    /// <remarks>
    /// LOGIC: Same icons as ProblemItemViewModel for visual consistency.
    /// </remarks>
    public string SeverityIcon => Severity switch
    {
        ViolationSeverity.Error => "⛔",
        ViolationSeverity.Warning => "⚠",
        ViolationSeverity.Info => "ℹ",
        ViolationSeverity.Hint => "💡",
        _ => "•"
    };

    /// <summary>
    /// Gets the formatted header text including count.
    /// </summary>
    /// <remarks>
    /// LOGIC: Format: "Errors (3)" for display in group header.
    /// </remarks>
    public string HeaderText => $"{DisplayName} ({Count})";

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemGroupViewModel"/> class.
    /// </summary>
    /// <param name="severity">The severity level for this group.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <remarks>
    /// LOGIC: Creates an empty group that can be populated with AddItem.
    /// Groups start expanded by default for visibility.
    ///
    /// Version: v0.2.6a
    /// </remarks>
    public ProblemGroupViewModel(
        ViolationSeverity severity,
        ILogger<ProblemGroupViewModel>? logger = null)
    {
        _logger = logger;
        Severity = severity;
        DisplayName = GetDisplayName(severity);
        _items = new ObservableCollection<IProblemItem>();
        Items = new ReadOnlyObservableCollection<IProblemItem>(_items);

        _logger?.LogTrace("Created ProblemGroupViewModel for severity {Severity}", severity);
    }

    #endregion

    #region Methods

    /// <summary>
    /// Adds a problem item to this group.
    /// </summary>
    /// <param name="item">The problem item to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when item is null.</exception>
    /// <remarks>
    /// LOGIC: Adds item and raises PropertyChanged for Count and HeaderText.
    /// </remarks>
    public void AddItem(IProblemItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _items.Add(item);
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(HeaderText));

        _logger?.LogTrace(
            "Added item {ItemId} to {Severity} group, new count: {Count}",
            item.Id, Severity, Count);
    }

    /// <summary>
    /// Clears all items from this group.
    /// </summary>
    /// <remarks>
    /// LOGIC: Removes all items and raises PropertyChanged for Count and HeaderText.
    /// </remarks>
    public void Clear()
    {
        var previousCount = _items.Count;
        _items.Clear();
        OnPropertyChanged(nameof(Count));
        OnPropertyChanged(nameof(HeaderText));

        _logger?.LogTrace(
            "Cleared {Severity} group, removed {Count} items",
            Severity, previousCount);
    }

    /// <summary>
    /// Gets the display name for a severity level.
    /// </summary>
    /// <param name="severity">The severity level.</param>
    /// <returns>The human-readable display name.</returns>
    private static string GetDisplayName(ViolationSeverity severity) => severity switch
    {
        ViolationSeverity.Error => "Errors",
        ViolationSeverity.Warning => "Warnings",
        ViolationSeverity.Info => "Info",
        ViolationSeverity.Hint => "Hints",
        _ => "Other"
    };

    #endregion
}
