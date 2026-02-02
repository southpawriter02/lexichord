// =============================================================================
// File: SearchResultItemViewModel.cs
// Project: Lexichord.Modules.RAG
// Description: ViewModel for individual search result items in the Reference Panel.
// =============================================================================
// LOGIC: Wraps a SearchHit to expose UI-friendly properties for display.
//   - DocumentName: Extracted from Document.FilePath (basename).
//   - PreviewText: Truncated chunk content (max 200 chars).
//   - ScoreDisplay: Formatted score (e.g., "87%").
//   - SectionHeading: From Chunk.Heading metadata (if present).
//   - NavigateCommand: Opens the source document at the chunk's offset.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.4.5a: SearchHit, TextChunk, Document
//   - v0.4.6c: IReferenceNavigationService (future)
// =============================================================================

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
/// text truncation, score formatting, and navigation to the source document.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6b as part of the Search Result Item View.
/// </para>
/// </remarks>
public partial class SearchResultItemViewModel : ObservableObject
{
    private readonly SearchHit _hit;
    private readonly ILogger<SearchResultItemViewModel>? _logger;
    private readonly Action<SearchHit>? _navigateAction;

    /// <summary>
    /// Creates a new <see cref="SearchResultItemViewModel"/> instance.
    /// </summary>
    /// <param name="hit">The underlying search hit.</param>
    /// <param name="navigateAction">
    /// Optional action to invoke when navigating to this result.
    /// If null, navigation is disabled.
    /// </param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    public SearchResultItemViewModel(
        SearchHit hit,
        Action<SearchHit>? navigateAction = null,
        ILogger<SearchResultItemViewModel>? logger = null)
    {
        _hit = hit;
        _navigateAction = navigateAction;
        _logger = logger;
    }

    /// <summary>
    /// Gets the underlying search hit.
    /// </summary>
    public SearchHit Hit => _hit;

    /// <summary>
    /// Gets the source document's display name (file basename).
    /// </summary>
    public string DocumentName => Path.GetFileName(_hit.Document.FilePath);

    /// <summary>
    /// Gets a truncated preview of the chunk content.
    /// </summary>
    public string PreviewText => _hit.GetPreview(200);

    /// <summary>
    /// Gets the formatted relevance score (e.g., "87%").
    /// </summary>
    public string ScoreDisplay => _hit.ScorePercent;

    /// <summary>
    /// Gets the raw relevance score (0.0 to 1.0).
    /// </summary>
    public float Score => _hit.Score;

    /// <summary>
    /// Gets the section heading from chunk metadata, or null if not present.
    /// </summary>
    public string? SectionHeading => _hit.Chunk.Metadata.Heading;

    /// <summary>
    /// Gets whether this result has a section heading.
    /// </summary>
    public bool HasSectionHeading => _hit.Chunk.Metadata.HasHeading;

    /// <summary>
    /// Gets the character offset where this chunk starts in the source document.
    /// </summary>
    public int StartOffset => _hit.Chunk.StartOffset;

    /// <summary>
    /// Gets the character offset where this chunk ends in the source document.
    /// </summary>
    public int EndOffset => _hit.Chunk.EndOffset;

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
