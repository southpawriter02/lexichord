// <copyright file="IssueCategoryChartView.axaml.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Avalonia.Controls;

namespace Lexichord.Modules.Style.Views;

/// <summary>
/// Code-behind for the Issues by Category chart view.
/// </summary>
/// <remarks>
/// LOGIC: v0.6.4b - Minimal code-behind for the horizontal bar chart visualization.
/// The view displays issue counts grouped by category using LiveCharts2 RowSeries.
/// </remarks>
public partial class IssueCategoryChartView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IssueCategoryChartView"/> class.
    /// </summary>
    public IssueCategoryChartView()
    {
        InitializeComponent();
    }
}
