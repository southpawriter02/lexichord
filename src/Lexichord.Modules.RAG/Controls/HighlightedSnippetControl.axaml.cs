// =============================================================================
// File: HighlightedSnippetControl.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for HighlightedSnippetControl.
// =============================================================================
// LOGIC: Minimal code-behind; main logic is in HighlightedSnippetViewModel.
//   - Defines Snippet and HighlightThemeStyle styled properties for XAML binding.
//   - Creates ViewModel with injected IHighlightRenderer.
// =============================================================================
// VERSION: v0.5.6b (Query Term Highlighting)
// =============================================================================

using Avalonia;
using Avalonia.Controls;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Rendering;
using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Modules.RAG.Controls;

/// <summary>
/// Control for displaying snippets with styled query term highlighting.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="HighlightedSnippetControl"/> displays <see cref="Snippet"/> content
/// with visual highlighting of query matches. It uses <see cref="IHighlightRenderer"/>
/// to convert highlight positions into styled text runs.
/// </para>
/// <para>
/// <b>Difference from HighlightedTextBlock:</b>
/// <list type="bullet">
///   <item><description><see cref="HighlightedTextBlock"/> (v0.4.6b) uses regex matching at render time.</description></item>
///   <item><description><see cref="HighlightedSnippetControl"/> (v0.5.6b) uses pre-calculated positions with type-aware styling.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Accessibility:</b> The control is keyboard navigable and supports text selection.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6b as part of The Answer Preview feature.
/// </para>
/// </remarks>
public partial class HighlightedSnippetControl : UserControl
{
    /// <summary>
    /// Defines the <see cref="Snippet"/> styled property.
    /// </summary>
    public static readonly StyledProperty<Snippet?> SnippetProperty =
        AvaloniaProperty.Register<HighlightedSnippetControl, Snippet?>(nameof(Snippet));

    /// <summary>
    /// Defines the <see cref="HighlightThemeStyle"/> styled property.
    /// </summary>
    /// <remarks>
    /// Named HighlightThemeStyle to avoid conflict with inherited StyledElement.Theme.
    /// </remarks>
    public static readonly StyledProperty<HighlightTheme> HighlightThemeStyleProperty =
        AvaloniaProperty.Register<HighlightedSnippetControl, HighlightTheme>(
            nameof(HighlightThemeStyle),
            defaultValue: HighlightTheme.Light);

    /// <summary>
    /// Defines the <see cref="Renderer"/> styled property.
    /// </summary>
    public static readonly StyledProperty<IHighlightRenderer?> RendererProperty =
        AvaloniaProperty.Register<HighlightedSnippetControl, IHighlightRenderer?>(nameof(Renderer));

    /// <summary>
    /// Gets or sets the snippet to display.
    /// </summary>
    public Snippet? Snippet
    {
        get => GetValue(SnippetProperty);
        set => SetValue(SnippetProperty, value);
    }

    /// <summary>
    /// Gets or sets the highlight theme style.
    /// </summary>
    /// <remarks>
    /// Named HighlightThemeStyle to avoid conflict with inherited StyledElement.Theme.
    /// </remarks>
    public HighlightTheme HighlightThemeStyle
    {
        get => GetValue(HighlightThemeStyleProperty);
        set => SetValue(HighlightThemeStyleProperty, value);
    }

    /// <summary>
    /// Gets or sets the highlight renderer.
    /// </summary>
    public IHighlightRenderer? Renderer
    {
        get => GetValue(RendererProperty);
        set => SetValue(RendererProperty, value);
    }

    private HighlightedSnippetViewModel? _viewModel;

    /// <summary>
    /// Static constructor to register property change handlers.
    /// </summary>
    static HighlightedSnippetControl()
    {
        SnippetProperty.Changed.AddClassHandler<HighlightedSnippetControl>(
            (x, _) => x.UpdateViewModel());
        HighlightThemeStyleProperty.Changed.AddClassHandler<HighlightedSnippetControl>(
            (x, _) => x.UpdateViewModel());
        RendererProperty.Changed.AddClassHandler<HighlightedSnippetControl>(
            (x, _) => x.OnRendererChanged());
    }

    /// <summary>
    /// Initializes a new instance of <see cref="HighlightedSnippetControl"/>.
    /// </summary>
    public HighlightedSnippetControl()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Called when the Renderer property changes.
    /// </summary>
    private void OnRendererChanged()
    {
        if (Renderer is not null)
        {
            _viewModel = new HighlightedSnippetViewModel(Renderer);
            DataContext = _viewModel;
            UpdateViewModel();
        }
    }

    /// <summary>
    /// Updates the ViewModel with current property values.
    /// </summary>
    private void UpdateViewModel()
    {
        if (_viewModel is null) return;
        
        _viewModel.Snippet = Snippet;
        _viewModel.Theme = HighlightThemeStyle;
    }
}
