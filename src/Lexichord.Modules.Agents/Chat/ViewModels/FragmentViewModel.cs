// -----------------------------------------------------------------------
// <copyright file="FragmentViewModel.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Agents.Context;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Chat.ViewModels;

/// <summary>
/// ViewModel for a single context fragment in the Context Preview panel.
/// Displays fragment metadata, a content preview, and supports expand/collapse toggling.
/// </summary>
/// <remarks>
/// <para>
/// Each <see cref="FragmentViewModel"/> wraps a <see cref="ContextFragment"/>
/// record produced by a context strategy and included in the assembled result.
/// The ViewModel adds UI-specific features:
/// </para>
/// <list type="bullet">
///   <item><description>Content truncation with configurable preview length</description></item>
///   <item><description>Expand/collapse toggle for viewing full content</description></item>
///   <item><description>Formatted display strings for tokens and relevance</description></item>
///   <item><description>Source-specific icons for visual identification</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2d as part of the Context Preview Panel.
/// </para>
/// </remarks>
/// <seealso cref="ContextFragment"/>
/// <seealso cref="ContextPreviewViewModel"/>
internal sealed partial class FragmentViewModel : ObservableObject
{
    #region Constants

    /// <summary>
    /// Maximum number of characters to show in the content preview
    /// before truncating with an ellipsis.
    /// </summary>
    private const int PreviewLength = 200;

    /// <summary>
    /// Event ID for fragment expansion toggled.
    /// </summary>
    private const int EventIdExpansionToggled = 7102;

    /// <summary>
    /// Event ID for fragment content copied.
    /// </summary>
    private const int EventIdContentCopied = 7103;

    #endregion

    #region Fields

    private readonly ContextFragment _fragment;
    private readonly ILogger? _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="FragmentViewModel"/> class.
    /// </summary>
    /// <param name="fragment">The context fragment to display.</param>
    /// <param name="logger">Optional logger for diagnostic output.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fragment"/> is null.</exception>
    public FragmentViewModel(ContextFragment fragment, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(fragment);

        _fragment = fragment;
        _logger = logger;
    }

    #endregion

    #region Read-Only Properties

    /// <summary>
    /// Gets the unique identifier of the source strategy that produced this fragment.
    /// </summary>
    public string SourceId => _fragment.SourceId;

    /// <summary>
    /// Gets the human-readable label for the fragment.
    /// </summary>
    public string Label => _fragment.Label;

    /// <summary>
    /// Gets the full content of the fragment.
    /// </summary>
    public string FullContent => _fragment.Content;

    /// <summary>
    /// Gets the estimated token count for this fragment.
    /// </summary>
    public int TokenCount => _fragment.TokenEstimate;

    /// <summary>
    /// Gets the relevance score of this fragment (0.0 to 1.0).
    /// </summary>
    public float Relevance => _fragment.Relevance;

    /// <summary>
    /// Gets the formatted token count string for display.
    /// </summary>
    /// <example>"1,234 tokens"</example>
    public string TokenCountText => $"{TokenCount:N0} tokens";

    /// <summary>
    /// Gets the formatted relevance percentage string for display.
    /// </summary>
    /// <example>"85%"</example>
    public string RelevanceText => $"{Relevance:P0}";

    /// <summary>
    /// Gets a truncated preview of the fragment content.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns the full content if it fits within <see cref="PreviewLength"/>,
    /// otherwise returns the first <see cref="PreviewLength"/> characters followed
    /// by an ellipsis ("...").
    /// </remarks>
    public string TruncatedContent =>
        _fragment.Content.Length <= PreviewLength
            ? _fragment.Content
            : _fragment.Content[..PreviewLength] + "...";

    /// <summary>
    /// Gets the content to display based on the current expansion state.
    /// </summary>
    public string DisplayContent => IsExpanded ? FullContent : TruncatedContent;

    /// <summary>
    /// Gets a value indicating whether this fragment's content exceeds the preview length
    /// and therefore supports expand/collapse toggling.
    /// </summary>
    public bool NeedsExpansion => _fragment.Content.Length > PreviewLength;

    /// <summary>
    /// Gets the text for the expand/collapse button.
    /// </summary>
    public string ExpandButtonText => IsExpanded ? "Show Less" : "Show More";

    /// <summary>
    /// Gets the icon for the source strategy type.
    /// </summary>
    public string SourceIcon => SourceId switch
    {
        "document" => "📄",
        "selection" => "✂️",
        "cursor" => "📍",
        "heading" => "📑",
        "rag" => "🔍",
        "style" => "🎨",
        _ => "📋"
    };

    #endregion

    #region Observable Properties

    /// <summary>
    /// Gets or sets a value indicating whether the fragment content is expanded
    /// to show the full text instead of the truncated preview.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayContent))]
    [NotifyPropertyChangedFor(nameof(ExpandButtonText))]
    private bool _isExpanded;

    #endregion

    #region Commands

    /// <summary>
    /// Toggles the <see cref="IsExpanded"/> state between expanded and collapsed.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;

        _logger?.Log(
            LogLevel.Debug,
            new EventId(EventIdExpansionToggled, nameof(ToggleExpanded)),
            "Fragment {SourceId} expansion toggled: {IsExpanded}",
            SourceId,
            IsExpanded);
    }

    #endregion

    #region Partial Methods

    partial void OnIsExpandedChanged(bool value)
    {
        _logger?.Log(
            LogLevel.Trace,
            new EventId(EventIdExpansionToggled, nameof(OnIsExpandedChanged)),
            "Fragment {SourceId} IsExpanded changed to {Value}",
            SourceId,
            value);
    }

    #endregion
}
