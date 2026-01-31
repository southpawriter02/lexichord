// <copyright file="ReadabilityHudWidget.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using Avalonia.Controls;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for the Readability HUD Widget.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.3d - Minimal code-behind for control initialization.</para>
/// <para>All logic is handled by <see cref="ViewModels.ReadabilityHudViewModel"/>.</para>
/// </remarks>
public partial class ReadabilityHudWidget : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ReadabilityHudWidget"/> class.
    /// </summary>
    public ReadabilityHudWidget()
    {
        InitializeComponent();
    }
}
