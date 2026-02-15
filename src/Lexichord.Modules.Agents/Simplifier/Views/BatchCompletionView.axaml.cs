// -----------------------------------------------------------------------
// <copyright file="BatchCompletionView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Controls;

namespace Lexichord.Modules.Agents.Simplifier.Views;

/// <summary>
/// Code-behind for the Batch Completion View.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This is the completion summary dialog for batch simplification operations.
/// It displays the results of the operation and provides next steps:
/// </para>
/// <list type="bullet">
///   <item><description>Success/cancelled/failed status with appropriate styling</description></item>
///   <item><description>Before/after readability metrics comparison</description></item>
///   <item><description>Paragraph counts (simplified, skipped, total)</description></item>
///   <item><description>Grade level improvement summary</description></item>
///   <item><description>Processing time and token usage</description></item>
///   <item><description>Undo hint for reverting all changes</description></item>
///   <item><description>View details button for paragraph-level results</description></item>
/// </list>
/// <para>
/// <b>Status Display:</b>
/// <list type="bullet">
///   <item><description><b>Success:</b> Green checkmark, full metrics display</description></item>
///   <item><description><b>Cancelled:</b> Yellow X, partial metrics if available</description></item>
///   <item><description><b>Failed:</b> Red exclamation, error message display</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4d as part of the Batch Simplification feature.
/// </para>
/// </remarks>
/// <seealso cref="BatchCompletionViewModel"/>
/// <seealso cref="BatchProgressView"/>
public partial class BatchCompletionView : UserControl
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BatchCompletionView"/> class.
    /// </summary>
    public BatchCompletionView()
    {
        InitializeComponent();
    }
}
