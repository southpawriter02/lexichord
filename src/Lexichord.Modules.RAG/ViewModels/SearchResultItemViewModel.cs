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
//   - Copy commands: Clipboard operations for citations and content (v0.5.2d).
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchHit, TextChunk, Document
//   - v0.4.6b: ScoreColor, QueryTerms for UI highlighting
//   - v0.4.6c: IReferenceNavigationService (future)
//   - v0.5.2a: Citation, ICitationService
//   - v0.5.2d: ICitationClipboardService, CitationStyle
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
/// <b>Copy Commands (v0.5.2d):</b>
/// <list type="bullet">
///   <item><description>CopyCitationCommand: Copies formatted citation using preferred style.</description></item>
///   <item><description>CopyAsCitationStyleCommand: Copies citation in specified style.</description></item>
///   <item><description>CopyChunkTextCommand: Copies raw chunk content.</description></item>
///   <item><description>CopyDocumentPathCommand: Copies document path.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6a, enhanced in v0.4.6b with ScoreColor and QueryTerms,
/// enhanced in v0.5.2d with copy commands.
/// </para>
/// </remarks>
public partial class SearchResultItemViewModel : ObservableObject
{
    private readonly SearchHit _hit;
    private readonly string _query;
    private readonly ILogger<SearchResultItemViewModel>? _logger;
    private readonly Action<SearchHit>? _navigateAction;
    private readonly ICitationService? _citationService;
    private readonly ICitationClipboardService? _clipboardService;

    // LOGIC: Cached citation created on-demand for copy operations (v0.5.2d).
    private Citation? _cachedCitation;

    // LOGIC: Regex pattern for extracting query terms.
    // Matches quoted phrases or individual words.
    // Pattern: "([^"]+)" captures quoted phrases, (\S+) captures unquoted words.
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
    /// <param name="citationService">
    /// Optional citation service for creating citations (v0.5.2d).
    /// If null, copy commands are disabled.
    /// </param>
    /// <param name="clipboardService">
    /// Optional clipboard service for copy operations (v0.5.2d).
    /// If null, copy commands are disabled.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="hit"/> is null.</exception>
    public SearchResultItemViewModel(
        SearchHit hit,
        Action<SearchHit>? navigateAction = null,
        string? query = null,
        ICitationService? citationService = null,
        ICitationClipboardService? clipboardService = null,
        ILogger<SearchResultItemViewModel>? logger = null)
    {
        _hit = hit ?? throw new ArgumentNullException(nameof(hit));
        _navigateAction = navigateAction;
        _query = query ?? string.Empty;
        _citationService = citationService;
        _clipboardService = clipboardService;
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

    /// <summary>
    /// Gets the full chunk text for copy operations.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns the complete chunk content without truncation.
    /// Used by <see cref="CopyChunkTextCommand"/> (v0.5.2d).
    /// </remarks>
    public string ChunkText => _hit.Chunk.Content;

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

    // =========================================================================
    // Copy Commands (v0.5.2d)
    // =========================================================================

    /// <summary>
    /// Gets whether copy commands are available for this result.
    /// </summary>
    /// <remarks>
    /// LOGIC: Copy commands require both ICitationService (for creating the citation)
    /// and ICitationClipboardService (for clipboard operations). If either is
    /// missing, copy commands are disabled (v0.5.2d).
    /// </remarks>
    private bool CanCopy => _citationService is not null && _clipboardService is not null;

    /// <summary>
    /// Gets or creates a citation for this search result.
    /// </summary>
    /// <returns>The cached or newly created citation.</returns>
    /// <remarks>
    /// LOGIC: Creates the citation on first access and caches it for subsequent
    /// copy operations. The citation is created via ICitationService.CreateCitation
    /// which calculates line numbers and resolves relative paths (v0.5.2a).
    /// </remarks>
    private Citation GetOrCreateCitation()
    {
        if (_cachedCitation is not null)
            return _cachedCitation;

        if (_citationService is null)
            throw new InvalidOperationException("Citation service not available");

        _cachedCitation = _citationService.CreateCitation(_hit);
        return _cachedCitation;
    }

    /// <summary>
    /// Copies a formatted citation using the user's preferred style.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses ICitationClipboardService.CopyCitationAsync with style=null
    /// to trigger preferred style resolution from CitationFormatterRegistry.
    /// License gating is applied at the service layer (v0.5.2d).
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanCopy))]
    private async Task CopyCitationAsync()
    {
        if (_clipboardService is null)
        {
            _logger?.LogWarning("CopyCitation called but clipboard service not available");
            return;
        }

        _logger?.LogDebug(
            "Copying citation for {Document} using preferred style",
            DocumentName);

        try
        {
            var citation = GetOrCreateCitation();
            await _clipboardService.CopyCitationAsync(citation);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to copy citation for {Document}", DocumentName);
        }
    }

    /// <summary>
    /// Copies a formatted citation using the specified style.
    /// </summary>
    /// <param name="style">The citation style to use for formatting.</param>
    /// <remarks>
    /// LOGIC: Uses ICitationClipboardService.CopyCitationAsync with the explicit
    /// style parameter. Called from the "Copy as..." submenu items (v0.5.2d).
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanCopy))]
    private async Task CopyAsCitationStyleAsync(CitationStyle style)
    {
        if (_clipboardService is null)
        {
            _logger?.LogWarning("CopyAsCitationStyle called but clipboard service not available");
            return;
        }

        _logger?.LogDebug(
            "Copying citation for {Document} as {Style}",
            DocumentName, style);

        try
        {
            var citation = GetOrCreateCitation();
            await _clipboardService.CopyCitationAsync(citation, style);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to copy citation as {Style} for {Document}", style, DocumentName);
        }
    }

    /// <summary>
    /// Copies the raw chunk text to the clipboard.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses ICitationClipboardService.CopyChunkTextAsync to copy the
    /// unformatted source content. Not license-gated (v0.5.2d).
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanCopy))]
    private async Task CopyChunkTextAsync()
    {
        if (_clipboardService is null)
        {
            _logger?.LogWarning("CopyChunkText called but clipboard service not available");
            return;
        }

        _logger?.LogDebug(
            "Copying chunk text ({Length} chars) for {Document}",
            ChunkText.Length, DocumentName);

        try
        {
            var citation = GetOrCreateCitation();
            await _clipboardService.CopyChunkTextAsync(citation, ChunkText);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to copy chunk text for {Document}", DocumentName);
        }
    }

    /// <summary>
    /// Copies the document path to the clipboard.
    /// </summary>
    /// <remarks>
    /// LOGIC: Uses ICitationClipboardService.CopyDocumentPathAsync to copy
    /// the absolute file path. Not license-gated (v0.5.2d).
    /// </remarks>
    [RelayCommand(CanExecute = nameof(CanCopy))]
    private async Task CopyDocumentPathAsync()
    {
        if (_clipboardService is null)
        {
            _logger?.LogWarning("CopyDocumentPath called but clipboard service not available");
            return;
        }

        _logger?.LogDebug("Copying document path for {Document}", DocumentName);

        try
        {
            var citation = GetOrCreateCitation();
            await _clipboardService.CopyDocumentPathAsync(citation);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to copy document path for {Document}", DocumentName);
        }
    }
}
