// -----------------------------------------------------------------------
// <copyright file="InlineDiffView.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Lexichord.Modules.Agents.Simplifier.Controls;

/// <summary>
/// Inline (unified) diff view control using DiffPlex.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This control displays changes in a compact unified diff format
/// with +/- line markers, similar to git diff output:
/// </para>
/// <list type="bullet">
///   <item><description>Removed lines prefixed with "-" and red background</description></item>
///   <item><description>Added lines prefixed with "+" and green background</description></item>
///   <item><description>Unchanged context lines with no prefix</description></item>
///   <item><description>Line numbers in the gutter</description></item>
/// </list>
/// <para>
/// <b>DiffPlex Integration:</b>
/// Uses <see cref="InlineDiffBuilder"/> to compute unified differences
/// between original and simplified text.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <seealso cref="SimplificationPreviewViewModel"/>
/// <seealso cref="DiffTextBox"/>
public partial class InlineDiffView : UserControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="OriginalText"/> property.
    /// </summary>
    public static readonly StyledProperty<string> OriginalTextProperty =
        AvaloniaProperty.Register<InlineDiffView, string>(nameof(OriginalText), string.Empty);

    /// <summary>
    /// Defines the <see cref="SimplifiedText"/> property.
    /// </summary>
    public static readonly StyledProperty<string> SimplifiedTextProperty =
        AvaloniaProperty.Register<InlineDiffView, string>(nameof(SimplifiedText), string.Empty);

    #endregion

    #region Private Fields

    private readonly Differ _differ = new();
    private readonly InlineDiffBuilder _diffBuilder;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="InlineDiffView"/> class.
    /// </summary>
    public InlineDiffView()
    {
        // LOGIC: Initialize DiffPlex inline diff builder
        _diffBuilder = new InlineDiffBuilder(_differ);

        DiffLines = new ObservableCollection<InlineDiffLine>();

        InitializeComponent();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the original text to compare.
    /// </summary>
    /// <value>The text before simplification.</value>
    public string OriginalText
    {
        get => GetValue(OriginalTextProperty);
        set => SetValue(OriginalTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the simplified text to compare.
    /// </summary>
    /// <value>The text after simplification.</value>
    public string SimplifiedText
    {
        get => GetValue(SimplifiedTextProperty);
        set => SetValue(SimplifiedTextProperty, value);
    }

    /// <summary>
    /// Gets the diff lines for display.
    /// </summary>
    public ObservableCollection<InlineDiffLine> DiffLines { get; }

    /// <summary>
    /// Gets the count of added lines.
    /// </summary>
    public int AdditionCount => DiffLines.Count(l => l.Type == ChangeType.Inserted);

    /// <summary>
    /// Gets the count of deleted lines.
    /// </summary>
    public int DeletionCount => DiffLines.Count(l => l.Type == ChangeType.Deleted);

    /// <summary>
    /// Gets the total number of lines.
    /// </summary>
    public int TotalLines => DiffLines.Count;

    #endregion

    #region Overrides

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // LOGIC: Rebuild diff when either text changes
        if (change.Property == OriginalTextProperty ||
            change.Property == SimplifiedTextProperty)
        {
            RebuildDiff();
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Rebuilds the inline diff visualization when text changes.
    /// </summary>
    private void RebuildDiff()
    {
        DiffLines.Clear();

        if (string.IsNullOrEmpty(OriginalText) && string.IsNullOrEmpty(SimplifiedText))
        {
            return;
        }

        // LOGIC: Compute inline diff using DiffPlex
        var diffModel = _diffBuilder.BuildDiffModel(
            OriginalText ?? string.Empty,
            SimplifiedText ?? string.Empty);

        // LOGIC: Convert to InlineDiffLine records
        int lineNumber = 0;
        foreach (var line in diffModel.Lines)
        {
            // LOGIC: Increment line number only for non-imaginary lines
            if (line.Type != ChangeType.Imaginary)
            {
                lineNumber++;
            }

            DiffLines.Add(new InlineDiffLine
            {
                LineNumber = line.Type == ChangeType.Imaginary ? string.Empty : lineNumber.ToString(),
                Text = line.Text ?? string.Empty,
                Type = line.Type,
                Marker = GetMarker(line.Type),
                MarkerForeground = GetMarkerForeground(line.Type),
                Background = GetBackgroundBrush(line.Type)
            });
        }
    }

    /// <summary>
    /// Gets the marker character for a change type.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <returns>"+", "-", or space.</returns>
    private static string GetMarker(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.Deleted => "-",
            ChangeType.Inserted => "+",
            ChangeType.Modified => "~",
            _ => " "
        };
    }

    /// <summary>
    /// Gets the marker foreground color for a change type.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <returns>The appropriate foreground brush.</returns>
    private static IBrush GetMarkerForeground(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.Deleted => new SolidColorBrush(Color.FromRgb(239, 68, 68)), // #EF4444
            ChangeType.Inserted => new SolidColorBrush(Color.FromRgb(34, 197, 94)), // #22C55E
            ChangeType.Modified => new SolidColorBrush(Color.FromRgb(234, 179, 8)), // #EAB308
            _ => Brushes.Gray
        };
    }

    /// <summary>
    /// Gets the background brush for a change type.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <returns>The appropriate background brush.</returns>
    private static IBrush GetBackgroundBrush(ChangeType changeType)
    {
        return changeType switch
        {
            ChangeType.Deleted => new SolidColorBrush(Color.FromArgb(48, 239, 68, 68)), // #30EF4444
            ChangeType.Inserted => new SolidColorBrush(Color.FromArgb(48, 34, 197, 94)), // #3022C55E
            ChangeType.Modified => new SolidColorBrush(Color.FromArgb(32, 234, 179, 8)), // #20EAB308
            _ => Brushes.Transparent
        };
    }

    #endregion
}

/// <summary>
/// Represents a single line in the inline diff view.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record encapsulates all data needed to render a single
/// line in the inline diff display, including line number, marker (+/-),
/// text content, and computed colors.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
public sealed class InlineDiffLine
{
    /// <summary>
    /// Gets or sets the line number to display.
    /// </summary>
    /// <value>The line number, or empty string for imaginary lines.</value>
    public string LineNumber { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text content of the line.
    /// </summary>
    /// <value>The line text.</value>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of change for this line.
    /// </summary>
    /// <value>The <see cref="ChangeType"/> from DiffPlex.</value>
    public ChangeType Type { get; set; }

    /// <summary>
    /// Gets or sets the marker character (+, -, or space).
    /// </summary>
    /// <value>The marker to display.</value>
    public string Marker { get; set; } = " ";

    /// <summary>
    /// Gets or sets the marker foreground brush.
    /// </summary>
    /// <value>The brush to use for the marker color.</value>
    public IBrush MarkerForeground { get; set; } = Brushes.Gray;

    /// <summary>
    /// Gets or sets the background brush for this line.
    /// </summary>
    /// <value>The brush to use for the line background.</value>
    public IBrush Background { get; set; } = Brushes.Transparent;
}
