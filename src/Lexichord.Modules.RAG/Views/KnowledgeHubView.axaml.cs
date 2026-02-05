// <copyright file="KnowledgeHubView.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Avalonia.Controls;
using Lexichord.Modules.RAG.ViewModels;

namespace Lexichord.Modules.RAG.Views;

/// <summary>
/// Code-behind for the Knowledge Hub view.
/// </summary>
/// <remarks>
/// LOGIC: v0.6.4a - Minimal code-behind for the Knowledge Hub unified dashboard.
/// The view provides:
/// - Statistics sidebar with index metrics
/// - Embedded search interface placeholder
/// - Recent searches history
/// - Quick action buttons
/// </remarks>
public partial class KnowledgeHubView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="KnowledgeHubView"/> class.
    /// </summary>
    public KnowledgeHubView()
    {
        InitializeComponent();
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // Initialize ViewModel when DataContext is set
        if (DataContext is KnowledgeHubViewModel vm)
        {
            _ = vm.InitializeAsync();
        }
    }
}
