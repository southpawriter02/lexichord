// =============================================================================
// File: IndexingProgressView.axaml.cs
// Project: Lexichord.Modules.RAG
// Description: Code-behind for the indexing progress toast overlay.
// Version: v0.4.7c
// =============================================================================

using Avalonia.Controls;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the indexing progress toast overlay.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="IndexingProgressView"/> is a toast-style overlay that displays real-time
/// progress during indexing operations. It is positioned at the bottom-right of the
/// application window and auto-dismisses after completion.
/// </para>
/// <para>
/// <b>Data Context:</b> <see cref="ViewModels.IndexingProgressViewModel"/>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.7c as part of Indexing Progress.
/// </para>
/// </remarks>
public partial class IndexingProgressView : UserControl
{
    /// <summary>
    /// Initializes a new instance of <see cref="IndexingProgressView"/>.
    /// </summary>
    public IndexingProgressView()
    {
        InitializeComponent();
    }
}
