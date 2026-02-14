// -----------------------------------------------------------------------
// <copyright file="ContextPreviewView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Chat.Views;

/// <summary>
/// Code-behind for the Context Preview view.
/// </summary>
/// <remarks>
/// <para>
/// This view displays the real-time context assembly results including
/// fragments, token budget, and strategy toggles. It provides a read-only
/// preview of the context that will be injected into AI agent prompts.
/// </para>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2d as part of the Context Preview Panel.
/// </para>
/// </remarks>
/// <seealso cref="ViewModels.ContextPreviewViewModel"/>
public partial class ContextPreviewView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContextPreviewView"/> class.
    /// </summary>
    public ContextPreviewView()
    {
        InitializeComponent();
    }
}
