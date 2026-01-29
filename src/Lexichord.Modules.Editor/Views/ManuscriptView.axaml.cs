using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaEdit;
using AvaloniaEdit.Highlighting;
using Lexichord.Modules.Editor.ViewModels;

namespace Lexichord.Modules.Editor.Views;

/// <summary>
/// View for editing manuscript documents.
/// </summary>
/// <remarks>
/// LOGIC: ManuscriptView wraps AvaloniaEdit's TextEditor control and
/// provides two-way binding to ManuscriptViewModel. It handles:
/// - Text synchronization between editor and ViewModel
/// - Caret and selection tracking
/// - Keyboard shortcuts (Ctrl+S, Ctrl+F, etc.)
/// - Syntax highlighting application (v0.1.3b)
/// </remarks>
public partial class ManuscriptView : UserControl
{
    private ManuscriptViewModel? _viewModel;
    private bool _isUpdatingFromViewModel;
    private bool _isUpdatingFromEditor;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManuscriptView"/> class.
    /// </summary>
    public ManuscriptView()
    {
        InitializeComponent();

        // LOGIC: Subscribe to DataContext changes to wire up ViewModel
        DataContextChanged += OnDataContextChanged;

        // LOGIC: Wire up editor events
        TextEditor.TextChanged += OnEditorTextChanged;
        TextEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;
        TextEditor.TextArea.SelectionChanged += OnSelectionChanged;

        // LOGIC: Handle keyboard shortcuts
        KeyDown += OnKeyDown;
    }

    /// <summary>
    /// Sets the syntax highlighting definition.
    /// </summary>
    /// <param name="highlighting">The highlighting definition, or null for plain text.</param>
    public void SetHighlighting(IHighlightingDefinition? highlighting)
    {
        TextEditor.SyntaxHighlighting = highlighting;
    }

    /// <summary>
    /// Scrolls to and selects the specified text range.
    /// </summary>
    public void SelectAndScrollTo(int offset, int length)
    {
        TextEditor.Select(offset, length);
        TextEditor.TextArea.Caret.Offset = offset;
        TextEditor.ScrollToLine(TextEditor.Document.GetLineByOffset(offset).LineNumber);
    }

    /// <summary>
    /// Gets or sets the current text content.
    /// </summary>
    public string Text
    {
        get => TextEditor.Text;
        set
        {
            if (TextEditor.Text != value)
            {
                _isUpdatingFromViewModel = true;
                TextEditor.Text = value;
                _isUpdatingFromViewModel = false;
            }
        }
    }

    /// <summary>
    /// Focuses the text editor.
    /// </summary>
    public void FocusEditor()
    {
        TextEditor.Focus();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // LOGIC: Unsubscribe from old ViewModel
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        // LOGIC: Subscribe to new ViewModel
        _viewModel = DataContext as ManuscriptViewModel;
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            // LOGIC: Initialize editor with ViewModel content
            _isUpdatingFromViewModel = true;
            TextEditor.Text = _viewModel.Content;
            _isUpdatingFromViewModel = false;

            // LOGIC: Attach search service to editor (v0.1.3c)
            _viewModel.SearchService.AttachToEditor(TextEditor);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (_isUpdatingFromEditor)
            return;

        switch (e.PropertyName)
        {
            case nameof(ManuscriptViewModel.Content):
                _isUpdatingFromViewModel = true;
                if (TextEditor.Text != _viewModel!.Content)
                {
                    TextEditor.Text = _viewModel.Content;
                }
                _isUpdatingFromViewModel = false;
                break;

            case nameof(ManuscriptViewModel.CaretPosition):
                var pos = _viewModel!.CaretPosition;
                if (TextEditor.TextArea.Caret.Offset != pos.Offset)
                {
                    TextEditor.TextArea.Caret.Offset = pos.Offset;
                }
                break;

            case nameof(ManuscriptViewModel.SearchViewModel):
                // LOGIC: Wire up SearchOverlay DataContext when SearchViewModel is created (v0.1.3c)
                if (_viewModel?.SearchViewModel is not null)
                {
                    SearchOverlay.DataContext = _viewModel.SearchViewModel;
                }
                break;
        }
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingFromViewModel || _viewModel is null)
            return;

        _isUpdatingFromEditor = true;
        _viewModel.Content = TextEditor.Text;
        _isUpdatingFromEditor = false;
    }

    private void OnCaretPositionChanged(object? sender, EventArgs e)
    {
        if (_viewModel is null)
            return;

        var caret = TextEditor.TextArea.Caret;
        _viewModel.UpdateCaretPosition(
            caret.Line,
            caret.Column,
            caret.Offset);
    }

    private void OnSelectionChanged(object? sender, EventArgs e)
    {
        if (_viewModel is null)
            return;

        var selection = TextEditor.TextArea.Selection;
        if (selection.IsEmpty)
        {
            _viewModel.UpdateSelection(0, 0);
        }
        else
        {
            var segments = selection.Segments.ToList();
            if (segments.Count > 0)
            {
                var start = segments.Min(s => s.StartOffset);
                var end = segments.Max(s => s.EndOffset);
                _viewModel.UpdateSelection(start, end);
            }
        }
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        // LOGIC: Handle global shortcuts
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            switch (e.Key)
            {
                case Key.S:
                    // Save document
                    if (_viewModel?.SaveCommand.CanExecute(null) == true)
                    {
                        _viewModel.SaveCommand.Execute(null);
                    }
                    e.Handled = true;
                    break;

                case Key.F:
                    // Show search overlay (v0.1.3c)
                    _viewModel?.ShowSearchCommand?.Execute(null);
                    FocusSearchOverlay();
                    e.Handled = true;
                    break;

                case Key.H:
                    // Toggle replace visibility (v0.1.3c)
                    if (_viewModel?.IsSearchVisible == true)
                    {
                        _viewModel?.SearchViewModel?.ToggleReplaceCommand?.Execute(null);
                    }
                    else
                    {
                        _viewModel?.ShowSearchCommand?.Execute(null);
                        _viewModel?.SearchViewModel?.ToggleReplaceCommand?.Execute(null);
                        FocusSearchOverlay();
                    }
                    e.Handled = true;
                    break;

                case Key.G:
                    // Go to line
                    _viewModel?.GoToLineCommand?.Execute(null);
                    e.Handled = true;
                    break;
            }
        }
        else if (e.Key == Key.Escape)
        {
            // Hide search overlay
            if (_viewModel?.IsSearchVisible == true)
            {
                _viewModel?.HideSearchCommand?.Execute(null);
                TextEditor.Focus();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.F3)
        {
            // F3/Shift+F3 for find next/previous (v0.1.3c)
            if (_viewModel?.IsSearchVisible == true)
            {
                if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
                {
                    _viewModel?.SearchViewModel?.FindPreviousCommand?.Execute(null);
                }
                else
                {
                    _viewModel?.SearchViewModel?.FindNextCommand?.Execute(null);
                }
                e.Handled = true;
            }
        }
    }

    private void FocusSearchOverlay()
    {
        // LOGIC: Defer focus until search overlay is visible
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            SearchOverlay?.FocusSearchBox();
        }, Avalonia.Threading.DispatcherPriority.Background);
    }
}
