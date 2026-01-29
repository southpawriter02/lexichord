namespace Lexichord.Modules.Workspace.Views;

using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Lexichord.Modules.Workspace.Models;
using Lexichord.Modules.Workspace.ViewModels;
using System.Globalization;

/// <summary>
/// Code-behind for the Project Explorer view.
/// </summary>
/// <remarks>
/// LOGIC: Handles UI-specific events that require code-behind:
/// - Double-click to open files
/// - Keyboard navigation for rename mode
/// - Focus management for edit textbox
/// </remarks>
public partial class ProjectExplorerView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProjectExplorerView"/> class.
    /// </summary>
    public ProjectExplorerView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets the view model.
    /// </summary>
    private ProjectExplorerViewModel? ViewModel => DataContext as ProjectExplorerViewModel;

    /// <summary>
    /// Handles double-click on tree items to open files.
    /// </summary>
    private async void OnTreeDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (ViewModel?.SelectedNode is { IsDirectory: false, IsPlaceholder: false })
        {
            await ViewModel.OpenSelectedFileCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Handles keyboard input during rename mode.
    /// </summary>
    private void OnEditKeyDown(object? sender, KeyEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (textBox.DataContext is not FileTreeNode node)
            return;

        switch (e.Key)
        {
            case Key.Enter:
                // Commit the rename
                CommitRename(node);
                e.Handled = true;
                break;

            case Key.Escape:
                // Cancel the rename
                node.CancelEdit();
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Handles losing focus during rename mode (auto-commit).
    /// </summary>
    private void OnEditLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox)
            return;

        if (textBox.DataContext is not FileTreeNode node)
            return;

        if (node.IsEditing)
        {
            // Auto-commit on focus loss
            CommitRename(node);
        }
    }

    /// <summary>
    /// Commits a rename operation.
    /// </summary>
    /// <param name="node">The node being renamed.</param>
    private void CommitRename(FileTreeNode node)
    {
        // TODO: v0.1.2d will implement actual file rename
        // For now, just cancel the edit mode
        node.CancelEdit();
    }
}

/// <summary>
/// Converts a boolean to a brush color (for folder vs file icons).
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isDirectory && parameter is string colorParam)
        {
            // Return appropriate color based on type
            return colorParam switch
            {
                "folder" when isDirectory => new SolidColorBrush(Color.Parse("#DCAB6B")), // Folder gold
                "folder" when !isDirectory => new SolidColorBrush(Color.Parse("#808080")), // File gray
                _ => Brushes.Gray
            };
        }

        return Brushes.Gray;
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
