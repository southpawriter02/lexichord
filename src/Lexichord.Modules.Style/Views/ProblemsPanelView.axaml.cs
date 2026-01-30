using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Lexichord.Modules.Style.ViewModels;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for the Problems Panel view.
/// </summary>
/// <remarks>
/// LOGIC: Minimal code-behind following MVVM pattern.
/// Most logic is in ProblemsPanelViewModel. Code-behind only
/// handles UI events that need to invoke ViewModel commands.
///
/// v0.2.6b adds double-click handler for navigation.
///
/// Version: v0.2.6b
/// </remarks>
public partial class ProblemsPanelView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProblemsPanelView"/> class.
    /// </summary>
    public ProblemsPanelView()
    {
        InitializeComponent();
    }

    #region v0.2.6b Navigation Support

    /// <summary>
    /// Handles double-click on a problem item to navigate to its location.
    /// </summary>
    /// <param name="sender">The border element that was double-clicked.</param>
    /// <param name="e">The event arguments.</param>
    /// <remarks>
    /// LOGIC: Extracts the ProblemItemViewModel from the sender's
    /// DataContext and invokes the NavigateToViolationCommand.
    ///
    /// Version: v0.2.6b
    /// </remarks>
    private void OnProblemItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Border { DataContext: ProblemItemViewModel item })
        {
            return;
        }

        if (DataContext is ProblemsPanelViewModel viewModel)
        {
            viewModel.NavigateToViolationCommand.Execute(item);
        }
    }

    #endregion
}
