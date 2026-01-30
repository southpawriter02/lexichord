using Avalonia.Controls;
using Avalonia.Media;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Editor.ViewModels;

namespace Lexichord.Modules.Editor.Views;

/// <summary>
/// View for displaying violation details in a tooltip.
/// </summary>
/// <remarks>
/// LOGIC: ViolationTooltipView provides XAML rendering for the tooltip:
/// - Shows severity icon with colored border
/// - Displays rule name and message
/// - Optionally shows recommendation and explanation
/// - Supports navigation between multiple violations
///
/// The view creates its own ViewModel and forwards navigation events
/// to the parent service.
///
/// Version: v0.2.4c
/// </remarks>
public partial class ViolationTooltipView : UserControl
{
    private readonly ViolationTooltipViewModel _viewModel;

    /// <summary>
    /// Event raised when navigation is requested.
    /// </summary>
    public event EventHandler<NavigateViolationEventArgs>? NavigateRequested;

    /// <summary>
    /// Initializes a new instance of the <see cref="ViolationTooltipView"/> class.
    /// </summary>
    public ViolationTooltipView()
    {
        InitializeComponent();

        _viewModel = new ViolationTooltipViewModel();
        _viewModel.NavigateRequested += (s, e) => NavigateRequested?.Invoke(this, e);
        DataContext = _viewModel;
    }

    /// <summary>
    /// Sets the violation to display.
    /// </summary>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="recommendation">Optional recommendation text.</param>
    /// <param name="explanation">Optional explanation text.</param>
    /// <param name="borderColor">Color for the severity indicator.</param>
    /// <param name="iconPath">SVG path for severity icon.</param>
    /// <param name="currentIndex">Current index (1-based) for multi-violation nav.</param>
    /// <param name="totalCount">Total violations for multi-violation nav.</param>
    public void SetViolation(
        string ruleName,
        string message,
        string? recommendation,
        string? explanation,
        Color borderColor,
        string iconPath,
        int currentIndex,
        int totalCount)
    {
        _viewModel.Update(
            ruleName,
            message,
            recommendation,
            explanation,
            new SolidColorBrush(borderColor),
            Geometry.Parse(iconPath),
            currentIndex,
            totalCount);
    }
}
