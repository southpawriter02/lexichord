// =============================================================================
// File: MultiSnippetViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for displaying multiple snippets from a single chunk.
// =============================================================================
// LOGIC: Manages expand/collapse state and snippet visibility.
//   - Primary snippet is always visible.
//   - Additional snippets are shown when expanded.
//   - Commands toggle and collapse the expanded state.
//   - Expander text updates based on state and count.
// =============================================================================
// VERSION: v0.5.6d (Multi-Snippet Results)
// DEPENDENCIES:
//   - ISnippetService (v0.5.6a)
//   - Snippet (v0.5.6a)
//   - TextChunk (v0.4.3a)
//   - SnippetOptions (v0.5.6a)
// =============================================================================

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for displaying multiple snippets from a single chunk.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="MultiSnippetViewModel"/> manages an expandable multi-snippet display
/// for search results. It initially shows one primary snippet and allows users to
/// expand to see additional matching regions.
/// </para>
/// <para>
/// <b>Primary Snippet:</b> The first snippet extracted from the chunk, which has
/// the highest match density. This is always visible regardless of expand state.
/// </para>
/// <para>
/// <b>Additional Snippets:</b> Up to two more snippets from different regions of
/// the same chunk. These are visible only when <see cref="IsExpanded"/> is true.
/// </para>
/// <para>
/// <b>Threading:</b> All initialization should occur on the UI thread to prevent
/// race conditions with property change notifications.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.6d as part of Multi-Snippet Results.
/// </para>
/// </remarks>
public partial class MultiSnippetViewModel : ObservableObject
{
    private readonly ISnippetService _snippetService;
    private readonly ILogger<MultiSnippetViewModel>? _logger;
    private readonly TextChunk _chunk;
    private readonly string _query;
    private readonly SnippetOptions _options;
    private readonly int _maxSnippets;

    /// <summary>
    /// The primary snippet (always visible).
    /// </summary>
    [ObservableProperty]
    private Snippet _primarySnippet = Snippet.Empty;

    /// <summary>
    /// Additional snippets (visible when expanded).
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<Snippet> _additionalSnippets = new();

    /// <summary>
    /// Whether the additional snippets are visible.
    /// </summary>
    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Total number of match regions in the chunk.
    /// </summary>
    [ObservableProperty]
    private int _totalMatchCount;

    /// <summary>
    /// Gets whether there are additional snippets to show.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="AdditionalSnippets"/> contains one or more items;
    /// otherwise, <c>false</c>.
    /// </value>
    public bool HasAdditionalSnippets => AdditionalSnippets.Count > 0;

    /// <summary>
    /// Gets the count of hidden snippets.
    /// </summary>
    /// <value>
    /// The number of snippets in <see cref="AdditionalSnippets"/>.
    /// </value>
    public int HiddenSnippetCount => AdditionalSnippets.Count;

    /// <summary>
    /// Gets the display text for the expander button.
    /// </summary>
    /// <value>
    /// "Hide additional matches" when expanded; otherwise, "Show X more match(es)".
    /// </value>
    public string ExpanderText => IsExpanded
        ? "Hide additional matches"
        : $"Show {HiddenSnippetCount} more {(HiddenSnippetCount == 1 ? "match" : "matches")}";

    /// <summary>
    /// Creates a new <see cref="MultiSnippetViewModel"/>.
    /// </summary>
    /// <param name="snippetService">Service for extracting snippets.</param>
    /// <param name="chunk">The text chunk to extract snippets from.</param>
    /// <param name="query">The search query for highlighting.</param>
    /// <param name="options">Snippet extraction options.</param>
    /// <param name="maxSnippets">Maximum number of snippets to extract (default: 3).</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="snippetService"/>, <paramref name="chunk"/>,
    /// <paramref name="query"/>, or <paramref name="options"/> is <c>null</c>.
    /// </exception>
    public MultiSnippetViewModel(
        ISnippetService snippetService,
        TextChunk chunk,
        string query,
        SnippetOptions options,
        int maxSnippets = 3,
        ILogger<MultiSnippetViewModel>? logger = null)
    {
        _snippetService = snippetService ?? throw new ArgumentNullException(nameof(snippetService));
        _chunk = chunk ?? throw new ArgumentNullException(nameof(chunk));
        _query = query ?? throw new ArgumentNullException(nameof(query));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _maxSnippets = maxSnippets;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the ViewModel by extracting snippets from the chunk.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method should be called after construction to populate the snippets.
    /// It extracts up to <c>maxSnippets</c> snippets from the chunk using the
    /// provided query and options.
    /// </para>
    /// <para>
    /// The first snippet becomes <see cref="PrimarySnippet"/>, and any additional
    /// snippets are added to <see cref="AdditionalSnippets"/>.
    /// </para>
    /// </remarks>
    public void Initialize()
    {
        var snippets = _snippetService.ExtractMultipleSnippets(
            _chunk, _query, _options, _maxSnippets);

        if (snippets.Count > 0)
        {
            PrimarySnippet = snippets[0];

            for (var i = 1; i < snippets.Count; i++)
            {
                AdditionalSnippets.Add(snippets[i]);
            }
        }

        TotalMatchCount = snippets.Sum(s => s.Highlights.Count);

        // Notify computed properties.
        OnPropertyChanged(nameof(HasAdditionalSnippets));
        OnPropertyChanged(nameof(HiddenSnippetCount));
        OnPropertyChanged(nameof(ExpanderText));

        _logger?.LogDebug(
            "Initialized with {Count} snippets",
            snippets.Count);
    }

    /// <summary>
    /// Toggles the expanded state.
    /// </summary>
    [RelayCommand]
    private void ToggleExpanded()
    {
        IsExpanded = !IsExpanded;
        OnPropertyChanged(nameof(ExpanderText));

        _logger?.LogDebug(
            "Expanded state changed to {IsExpanded}",
            IsExpanded);
    }

    /// <summary>
    /// Collapses the additional snippets.
    /// </summary>
    [RelayCommand]
    private void Collapse()
    {
        IsExpanded = false;
        OnPropertyChanged(nameof(ExpanderText));

        _logger?.LogDebug("Collapsed additional snippets");
    }
}
