// -----------------------------------------------------------------------
// <copyright file="DiffTextBox.axaml.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace Lexichord.Modules.Agents.Simplifier.Controls;

/// <summary>
/// Side-by-side diff text display control using DiffPlex.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This control displays original and simplified text in a
/// two-column layout with color-coded highlighting for changes:
/// </para>
/// <list type="bullet">
///   <item><description>Red background for deleted text (left panel)</description></item>
///   <item><description>Green background for added text (right panel)</description></item>
///   <item><description>Line numbers in the gutter</description></item>
///   <item><description>Synchronized scrolling between panels</description></item>
/// </list>
/// <para>
/// <b>DiffPlex Integration:</b>
/// Uses <see cref="SideBySideDiffBuilder"/> to compute line-level differences
/// between original and simplified text. The diff model is converted to
/// <see cref="DiffLine"/> records for display.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <seealso cref="SimplificationPreviewViewModel"/>
public partial class DiffTextBox : UserControl
{
    #region Styled Properties

    /// <summary>
    /// Defines the <see cref="OriginalText"/> property.
    /// </summary>
    public static readonly StyledProperty<string> OriginalTextProperty =
        AvaloniaProperty.Register<DiffTextBox, string>(nameof(OriginalText), string.Empty);

    /// <summary>
    /// Defines the <see cref="SimplifiedText"/> property.
    /// </summary>
    public static readonly StyledProperty<string> SimplifiedTextProperty =
        AvaloniaProperty.Register<DiffTextBox, string>(nameof(SimplifiedText), string.Empty);

    #endregion

    #region Private Fields

    private readonly Differ _differ = new();
    private readonly SideBySideDiffBuilder _diffBuilder;
    private ScrollViewer? _originalScrollViewer;
    private ScrollViewer? _simplifiedScrollViewer;
    private bool _isSyncingScroll;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="DiffTextBox"/> class.
    /// </summary>
    public DiffTextBox()
    {
        // LOGIC: Initialize DiffPlex side-by-side diff builder
        _diffBuilder = new SideBySideDiffBuilder(_differ);

        OriginalDiffLines = new ObservableCollection<DiffLine>();
        SimplifiedDiffLines = new ObservableCollection<DiffLine>();

        InitializeComponent();
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the original text to display.
    /// </summary>
    /// <value>The text before simplification.</value>
    public string OriginalText
    {
        get => GetValue(OriginalTextProperty);
        set => SetValue(OriginalTextProperty, value);
    }

    /// <summary>
    /// Gets or sets the simplified text to display.
    /// </summary>
    /// <value>The text after simplification.</value>
    public string SimplifiedText
    {
        get => GetValue(SimplifiedTextProperty);
        set => SetValue(SimplifiedTextProperty, value);
    }

    /// <summary>
    /// Gets the diff lines for the original text panel.
    /// </summary>
    public ObservableCollection<DiffLine> OriginalDiffLines { get; }

    /// <summary>
    /// Gets the diff lines for the simplified text panel.
    /// </summary>
    public ObservableCollection<DiffLine> SimplifiedDiffLines { get; }

    /// <summary>
    /// Gets the word count of the original text.
    /// </summary>
    public int OriginalWordCount => CountWords(OriginalText);

    /// <summary>
    /// Gets the word count of the simplified text.
    /// </summary>
    public int SimplifiedWordCount => CountWords(SimplifiedText);

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

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // LOGIC: Get scroll viewer references for synchronized scrolling
        _originalScrollViewer = this.FindControl<ScrollViewer>("OriginalScrollViewer");
        _simplifiedScrollViewer = this.FindControl<ScrollViewer>("SimplifiedScrollViewer");

        if (_originalScrollViewer != null)
        {
            _originalScrollViewer.ScrollChanged += OnOriginalScrollChanged;
        }

        if (_simplifiedScrollViewer != null)
        {
            _simplifiedScrollViewer.ScrollChanged += OnSimplifiedScrollChanged;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Rebuilds the diff visualization when text changes.
    /// </summary>
    private void RebuildDiff()
    {
        OriginalDiffLines.Clear();
        SimplifiedDiffLines.Clear();

        if (string.IsNullOrEmpty(OriginalText) && string.IsNullOrEmpty(SimplifiedText))
        {
            return;
        }

        // LOGIC: Compute side-by-side diff using DiffPlex
        var diffModel = _diffBuilder.BuildDiffModel(
            OriginalText ?? string.Empty,
            SimplifiedText ?? string.Empty);

        // LOGIC: Convert old (original) pane to DiffLine records
        int originalLineNumber = 0;
        foreach (var line in diffModel.OldText.Lines)
        {
            if (line.Type != ChangeType.Imaginary)
            {
                originalLineNumber++;
            }

            OriginalDiffLines.Add(new DiffLine
            {
                LineNumber = line.Type == ChangeType.Imaginary ? string.Empty : originalLineNumber.ToString(),
                Text = line.Text ?? string.Empty,
                Type = line.Type,
                Background = GetBackgroundBrush(line.Type, isOriginal: true)
            });
        }

        // LOGIC: Convert new (simplified) pane to DiffLine records
        int simplifiedLineNumber = 0;
        foreach (var line in diffModel.NewText.Lines)
        {
            if (line.Type != ChangeType.Imaginary)
            {
                simplifiedLineNumber++;
            }

            SimplifiedDiffLines.Add(new DiffLine
            {
                LineNumber = line.Type == ChangeType.Imaginary ? string.Empty : simplifiedLineNumber.ToString(),
                Text = line.Text ?? string.Empty,
                Type = line.Type,
                Background = GetBackgroundBrush(line.Type, isOriginal: false)
            });
        }
    }

    /// <summary>
    /// Gets the background brush for a diff line based on its change type.
    /// </summary>
    /// <param name="changeType">The type of change.</param>
    /// <param name="isOriginal">Whether this is the original (left) panel.</param>
    /// <returns>The appropriate background brush.</returns>
    private static IBrush GetBackgroundBrush(ChangeType changeType, bool isOriginal)
    {
        return changeType switch
        {
            // LOGIC: Deletions show in red on the original (left) side
            ChangeType.Deleted => new SolidColorBrush(Color.FromArgb(48, 239, 68, 68)), // #30EF4444

            // LOGIC: Insertions show in green on the simplified (right) side
            ChangeType.Inserted => new SolidColorBrush(Color.FromArgb(48, 34, 197, 94)), // #3022C55E

            // LOGIC: Modifications show in yellow
            ChangeType.Modified => new SolidColorBrush(Color.FromArgb(32, 234, 179, 8)), // #20EAB308

            // LOGIC: Imaginary lines (padding for alignment) are slightly dimmed
            ChangeType.Imaginary => new SolidColorBrush(Color.FromArgb(16, 128, 128, 128)), // #10808080

            // LOGIC: Unchanged lines have no background
            _ => Brushes.Transparent
        };
    }

    /// <summary>
    /// Counts words in text.
    /// </summary>
    /// <param name="text">The text to count words in.</param>
    /// <returns>The word count.</returns>
    private static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        // LOGIC: Split on whitespace and count non-empty tokens
        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>
    /// Handles scroll changes on the original panel to sync the simplified panel.
    /// </summary>
    private void OnOriginalScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isSyncingScroll || _simplifiedScrollViewer == null)
        {
            return;
        }

        _isSyncingScroll = true;
        try
        {
            _simplifiedScrollViewer.Offset = _originalScrollViewer!.Offset;
        }
        finally
        {
            _isSyncingScroll = false;
        }
    }

    /// <summary>
    /// Handles scroll changes on the simplified panel to sync the original panel.
    /// </summary>
    private void OnSimplifiedScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (_isSyncingScroll || _originalScrollViewer == null)
        {
            return;
        }

        _isSyncingScroll = true;
        try
        {
            _originalScrollViewer.Offset = _simplifiedScrollViewer!.Offset;
        }
        finally
        {
            _isSyncingScroll = false;
        }
    }

    #endregion
}

/// <summary>
/// Represents a single line in the diff view.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record encapsulates all data needed to render a single
/// line in the diff display, including line number, text content, change type,
/// and computed background color.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
public sealed class DiffLine
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
    /// Gets or sets the background brush for this line.
    /// </summary>
    /// <value>The brush to use for the line background.</value>
    public IBrush Background { get; set; } = Brushes.Transparent;
}
