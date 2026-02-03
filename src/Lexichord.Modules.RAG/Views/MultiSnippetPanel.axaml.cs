// =============================================================================
// File: MultiSnippetPanel.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for MultiSnippetPanel.
// =============================================================================
// LOGIC: Minimal code-behind; main logic is in MultiSnippetViewModel.
//   - Defines styled properties for external configuration.
//   - Initializes component from XAML.
// =============================================================================
// VERSION: v0.5.6d (Multi-Snippet Results)
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Panel for displaying multiple snippets with expand/collapse functionality.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MultiSnippetPanel"/> provides an expandable view of multiple
/// snippets from a single search result. It displays a primary snippet that
/// is always visible, with an expander button to reveal additional snippets.
/// </para>
/// <para>
/// <b>Data Binding:</b> The panel expects a <see cref="ViewModels.MultiSnippetViewModel"/>
/// as its <see cref="Control.DataContext"/>. The ViewModel manages snippet
/// extraction and expand/collapse state.
/// </para>
/// <para>
/// <b>Styling:</b> The panel uses dynamic resource references for theming:
/// <list type="bullet">
///   <item><description><c>BorderSubtle</c>: Divider between snippets</description></item>
///   <item><description><c>TextSecondary</c>: Expander button text</description></item>
///   <item><description><c>TextPrimary</c>: Expander button text on hover</description></item>
///   <item><description><c>SurfaceHover</c>: Expander button background on hover</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6d as part of Multi-Snippet Results.
/// </para>
/// </remarks>
public partial class MultiSnippetPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="MultiSnippetPanel"/>.
    /// </summary>
    public MultiSnippetPanel()
    {
        InitializeComponent();
    }
}
