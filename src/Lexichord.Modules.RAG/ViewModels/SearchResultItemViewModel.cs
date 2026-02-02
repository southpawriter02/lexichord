// =============================================================================
// File: SearchResultItemViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for individual search result items in the Reference Panel.
// =============================================================================
// LOGIC: Wraps a SearchHit to expose UI-friendly properties for display.
//   - DocumentName: Extracted from Document.FilePath (basename).
//   - PreviewText: Truncated chunk content (max 200 chars).
//   - ScoreDisplay: Formatted score (e.g., "87%").
//   - ScoreColor: Relevance category for badge styling (v0.4.6b).
//   - QueryTerms: Parsed query terms for highlighting (v0.4.6b).
//   - SectionHeading: From Chunk.Heading metadata (if present).
//   - NavigateCommand: Opens the source document at the chunk's offset.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchHit, TextChunk, Document
//   - v0.4.6b: ScoreColor, QueryTerms for UI highlighting
//   - v0.4.6c: IReferenceNavigationService (future)
// =============================================================================

using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.ViewModels;

/// <summary>
/// ViewModel for an individual search result item in the Reference Panel.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SearchResultItemViewModel"/> wraps a <see cref="SearchHit"/> to expose
/// UI-friendly properties for display in the search results list. It handles
/// text truncation, score formatting, query term parsing, and navigation to
/// the source document.
/// </para>
/// <para>
/// <b>Score Categories (v0.4.6b):</b>
/// <list type="bullet">
///   <item><description>HighRelevance: Score >= 0.90</description></item>
///   <item><description>MediumRelevance: Score >= 0.80</description></item>
///   <item><description>LowRelevance: Score >= 0.70</description></item>
///   <item><description>MinimalRelevance: Score &lt; 0.70</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a, enhanced in v0.4.6b with ScoreColor and QueryTerms.
/// </para>
/// </remarks>
public partial class SearchResultItemViewModel : ObservableObject
{
    private readonly SearchHit _hit;
    private readonly string _query;
    private readonly ILogger<SearchResultItemViewModel>? _logger;
    private readonly Action<SearchHit>? _navigateAction;

    // LOGIC: Regex pattern for extracting query terms.
    // Matches quoted phrases or individual words.
    private static readonly Regex QueryTermPattern = new(
        @"""([^""]+)""|(\S+)",
        RegexOptions.Compiled);

    /// <summary>
    /// Creates a new <see cref="SearchResultItemViewModel"/> instance.
    /// </summary>
    /// <param name="hit">The underlying search hit. Cannot be null.</param>
    /// <param name="navigateAction">
    /// Optional action to invoke when navigating to this result.
    /// If null, navigation is disabled.
    /// </param>
    /// <param name="query">
    /// Optional original search query for term highlighting.
    /// If null or empty, no terms will be highlighted.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hit"/> is null.</exception>
    public SearchResultItemViewModel(
        SearchHit hit,
        Action<SearchHit>? navigateAction = null,
        string? query = null,
        ILogger<SearchResultItemViewModel>? logger = null)
    {
        _hit = hit ?? throw new ArgumentNullException(nameof(hit));
        _navigateAction = navigateAction;
        _query = query ?? string.Empty;
        _logger = logger;
    }

    // =========================================================================
    // Core Properties
    // =========================================================================

    /// <summary>
    /// Gets the underlying search hit.
    /// </summary>
    public SearchHit Hit => _hit;

    /// <summary>
    /// Gets the original search query.
    /// </summary>
    /// <value>
    /// The query string used for this search, or empty string if not provided.
    /// </value>
    public string Query => _query;

    /// <summary>
    /// Gets the source document's display name (file basename).
    /// </summary>
    public string DocumentName => Path.GetFileName(_hit.Document.FilePath);

    /// <summary>
    /// Gets the full path to the source document.
    /// </summary>
    /// <remarks>
    /// Used for tooltips on the document name display.
    /// </remarks>
    public string DocumentPath => _hit.Document.FilePath;

    /// <summary>
    /// Gets a truncated preview of the chunk content.
    /// </summary>
    public string PreviewText => _hit.GetPreview(200);

    // =========================================================================
    // Score Properties
    // =========================================================================

    /// <summary>
    /// Gets the formatted relevance score (e.g., "87%").
    /// </summary>
    public string ScoreDisplay => _hit.ScorePercent;

    /// <summary>
    /// Gets the raw relevance score (0.0 to 1.0).
    /// </summary>
    public float Score => _hit.Score;

    /// <summary>
    /// Gets the score as an integer percentage (0-100).
    /// </summary>
    /// <remarks>
    /// LOGIC: Multiplies the raw score by 100 and rounds to nearest integer.
    /// Used for display purposes and badge styling.
    /// </remarks>
    public int ScorePercent => (int)Math.Round(_hit.Score * 100);

    /// <summary>
    /// Gets the relevance category for score badge styling.
    /// </summary>
    /// <value>
    /// A string identifying the relevance category:
    /// <list type="bullet">
    ///   <item><description>"HighRelevance": Score >= 0.90</description></item>
    ///   <item><description>"MediumRelevance": Score >= 0.80</description></item>
    ///   <item><description>"LowRelevance": Score >= 0.70</description></item>
    ///   <item><description>"MinimalRelevance": Score &lt; 0.70</description></item>
    /// </list>
    /// </value>
    /// <remarks>
    /// <para>
    /// LOGIC: Maps the raw score to a category string that can be used with
    /// Avalonia style selectors via the StringEqualsConverter.
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.6b for dynamic score badge coloring.
    /// </para>
    /// </remarks>
    public string ScoreColor => _hit.Score switch
    {
        >= 0.90f => "HighRelevance",
        >= 0.80f => "MediumRelevance",
        >= 0.70f => "LowRelevance",
        _ => "MinimalRelevance"
    };

    // =========================================================================
    // Section Properties
    // =========================================================================

    /// <summary>
    /// Gets the section heading from chunk metadata, or null if not present.
    /// </summary>
    public string? SectionHeading => _hit.Chunk.Metadata.Heading;

    /// <summary>
    /// Gets whether this result has a section heading.
    /// </summary>
    public bool HasSectionHeading => _hit.Chunk.Metadata.HasHeading;

    // =========================================================================
    // Offset Properties
    // =========================================================================

    /// <summary>
    /// Gets the character offset where this chunk starts in the source document.
    /// </summary>
    public int StartOffset => _hit.Chunk.StartOffset;

    /// <summary>
    /// Gets the character offset where this chunk ends in the source document.
    /// </summary>
    public int EndOffset => _hit.Chunk.EndOffset;

    // =========================================================================
    // Query Term Highlighting (v0.4.6b)
    // =========================================================================

    /// <summary>
    /// Gets the parsed query terms for highlighting in preview text.
    /// </summary>
    /// <value>
    /// A list of individual terms extracted from the query. Quoted phrases
    /// are treated as single terms. Empty if no query was provided.
    /// </value>
    /// <remarks>
    /// <para>
    /// LOGIC: Parses the query string to extract individual terms. Supports:
    /// <list type="bullet">
    ///   <item><description>Simple words: "hello world" → ["hello", "world"]</description></item>
    ///   <item><description>Quoted phrases: '"hello world"' → ["hello world"]</description></item>
    ///   <item><description>Mixed: 'foo "bar baz"' → ["foo", "bar baz"]</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Introduced in:</b> v0.4.6b for query term highlighting.
    /// </para>
    /// </remarks>
    public IReadOnlyList<string> QueryTerms => ParseQueryTerms(_query);

    /// <summary>
    /// Parses a query string into individual terms for highlighting.
    /// </summary>
    /// <param name="query">The query string to parse.</param>
    /// <returns>A list of terms extracted from the query.</returns>
    private static IReadOnlyList<string> ParseQueryTerms(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return Array.Empty<string>();

        var terms = new List<string>();
        var matches = QueryTermPattern.Matches(query);

        foreach (Match match in matches)
        {
            // LOGIC: Group 1 captures quoted phrases (without quotes).
            // Group 2 captures unquoted words.
            var term = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value;

            if (!string.IsNullOrWhiteSpace(term))
            {
                terms.Add(term);
            }
        }

        return terms.AsReadOnly();
    }

    // =========================================================================
    // Navigation Command
    // =========================================================================

    /// <summary>
    /// Navigates to the source document at the chunk's location.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanNavigate))]
    private void Navigate()
    {
        if (_navigateAction is null)
        {
            _logger?.LogWarning("Navigate called but no navigation action configured");
            return;
        }

        _logger?.LogDebug(
            "Navigating to {Document} at offset {Offset}",
            DocumentName,
            StartOffset);

        _navigateAction(_hit);
    }

    /// <summary>
    /// Gets whether navigation is available for this result.
    /// </summary>
    private bool CanNavigate => _navigateAction is not null;
}

