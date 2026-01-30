using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts.Linting;

namespace Lexichord.Modules.Editor.ViewModels;

/// <summary>
/// ViewModel for the violation tooltip view.
/// </summary>
/// <remarks>
/// LOGIC: ViolationTooltipViewModel provides data binding for the tooltip UI:
/// - Displays rule name, message, recommendation, and explanation
/// - Shows severity icon and border color
/// - Supports navigation between multiple violations at the same position
///
/// The ViewModel is updated via the Update() method when violations change,
/// rather than using traditional property setters from binding.
///
/// Version: v0.2.4c
/// </remarks>
public partial class ViolationTooltipViewModel : ObservableObject
{
    /// <summary>
    /// Event raised when navigation is requested.
    /// </summary>
    public event EventHandler<NavigateViolationEventArgs>? NavigateRequested;

    /// <summary>
    /// Gets or sets the rule name to display.
    /// </summary>
    [ObservableProperty]
    private string _ruleName = string.Empty;

    /// <summary>
    /// Gets or sets the violation message.
    /// </summary>
    [ObservableProperty]
    private string _message = string.Empty;

    /// <summary>
    /// Gets or sets the recommendation text.
    /// </summary>
    [ObservableProperty]
    private string? _recommendation;

    /// <summary>
    /// Gets or sets the explanation text.
    /// </summary>
    [ObservableProperty]
    private string? _explanation;

    /// <summary>
    /// Gets or sets the border brush for the tooltip.
    /// </summary>
    [ObservableProperty]
    private IBrush _borderColor = Brushes.Gray;

    /// <summary>
    /// Gets or sets the severity icon geometry.
    /// </summary>
    [ObservableProperty]
    private Geometry? _iconPath;

    /// <summary>
    /// Gets or sets the current violation index (1-based).
    /// </summary>
    [ObservableProperty]
    private int _currentIndex = 1;

    /// <summary>
    /// Gets or sets the total number of violations.
    /// </summary>
    [ObservableProperty]
    private int _totalCount = 1;

    /// <summary>
    /// Gets whether a recommendation is available.
    /// </summary>
    public bool HasRecommendation => !string.IsNullOrEmpty(Recommendation);

    /// <summary>
    /// Gets whether an explanation is available.
    /// </summary>
    public bool HasExplanation => !string.IsNullOrEmpty(Explanation);

    /// <summary>
    /// Gets whether there are multiple violations to navigate.
    /// </summary>
    public bool HasMultiple => TotalCount > 1;

    /// <summary>
    /// Updates the ViewModel with violation data.
    /// </summary>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="message">The violation message.</param>
    /// <param name="recommendation">The recommendation text.</param>
    /// <param name="explanation">The explanation text.</param>
    /// <param name="borderColor">The border color brush.</param>
    /// <param name="iconPath">The severity icon geometry.</param>
    /// <param name="currentIndex">Current index (1-based).</param>
    /// <param name="totalCount">Total violations at position.</param>
    public void Update(
        string ruleName,
        string message,
        string? recommendation,
        string? explanation,
        IBrush borderColor,
        Geometry? iconPath,
        int currentIndex,
        int totalCount)
    {
        RuleName = ruleName;
        Message = message;
        Recommendation = recommendation;
        Explanation = explanation;
        BorderColor = borderColor;
        IconPath = iconPath;
        CurrentIndex = currentIndex;
        TotalCount = totalCount;

        // LOGIC: Notify computed properties that depend on the updated values
        OnPropertyChanged(nameof(HasRecommendation));
        OnPropertyChanged(nameof(HasExplanation));
        OnPropertyChanged(nameof(HasMultiple));
    }

    /// <summary>
    /// Navigates to the previous violation.
    /// </summary>
    [RelayCommand]
    private void NavigatePrevious()
    {
        NavigateRequested?.Invoke(this, new NavigateViolationEventArgs
        {
            Direction = NavigateDirection.Previous
        });
    }

    /// <summary>
    /// Navigates to the next violation.
    /// </summary>
    [RelayCommand]
    private void NavigateNext()
    {
        NavigateRequested?.Invoke(this, new NavigateViolationEventArgs
        {
            Direction = NavigateDirection.Next
        });
    }
}
