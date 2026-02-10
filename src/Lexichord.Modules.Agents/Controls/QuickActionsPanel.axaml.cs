// -----------------------------------------------------------------------
// <copyright file="QuickActionsPanel.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Controls;

/// <summary>
/// Code-behind for the quick actions floating panel control.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: Minimal code-behind — all logic is handled by
/// <see cref="ViewModels.QuickActionsPanelViewModel"/>. The control
/// is positioned via Canvas.Top/Canvas.Left bindings and visibility
/// is driven by the ViewModel's <c>IsVisible</c> property.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7d as part of the Quick Actions Panel feature.</para>
/// </remarks>
public partial class QuickActionsPanel : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuickActionsPanel"/> class.
    /// </summary>
    public QuickActionsPanel()
    {
        InitializeComponent();
    }
}
