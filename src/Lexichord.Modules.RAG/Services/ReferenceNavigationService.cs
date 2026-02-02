// =============================================================================
// File: ReferenceNavigationService.cs
// Project: Lexichord.Modules.RAG
// Description: Implementation of reference navigation using the editor service.
// =============================================================================
// LOGIC: Navigates from RAG search results to source documents in the editor.
//   1. Extracts document path and offsets from SearchHit.
//   2. Opens the document if not already open via IEditorService.
//   3. Delegates scrolling and highlighting to IEditorNavigationService.
//   4. Publishes ReferenceNavigatedEvent for telemetry on success.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.1.3a: IEditorService (document open/lookup)
//   - v0.2.6b: IEditorNavigationService (offset navigation, highlighting)
//   - v0.0.7a: IMediator (event publishing)
//   - v0.4.5a: SearchHit (search result record)
//   - v0.4.6c: ReferenceNavigatedEvent, IReferenceNavigationService
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Modules.RAG.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Implementation of reference navigation using the editor service.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ReferenceNavigationService"/> coordinates navigation from RAG
/// search results to source documents in the editor. It bridges the gap between
/// the RAG module's <see cref="SearchHit"/> records and the editor's navigation
/// infrastructure (<see cref="IEditorNavigationService"/>).
/// </para>
/// <para>
/// <b>Navigation Flow:</b>
/// <list type="number">
///   <item><description>Validate input (null checks, path validation).</description></item>
///   <item><description>Look up document by path via <see cref="IEditorService.GetDocumentByPath"/>.</description></item>
///   <item><description>If not open, open via <see cref="IEditorService.OpenDocumentAsync"/>.</description></item>
///   <item><description>Delegate to <see cref="IEditorNavigationService.NavigateToOffsetAsync"/> for scroll/highlight.</description></item>
///   <item><description>On success, publish <see cref="ReferenceNavigatedEvent"/> via <see cref="IMediator"/>.</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is stateless and safe for concurrent use.
/// All mutable state is managed by the underlying editor services.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.4.6c as part of Source Navigation.
/// </para>
/// </remarks>
public sealed class ReferenceNavigationService : IReferenceNavigationService
{
    private readonly IEditorService _editorService;
    private readonly IEditorNavigationService _editorNavigationService;
    private readonly IMediator _mediator;
    private readonly ILogger<ReferenceNavigationService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReferenceNavigationService"/> class.
    /// </summary>
    /// <param name="editorService">Service for document lookup and opening.</param>
    /// <param name="editorNavigationService">Service for offset-based navigation and highlighting.</param>
    /// <param name="mediator">MediatR instance for publishing telemetry events.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/>, <paramref name="editorNavigationService"/>,
    /// <paramref name="mediator"/>, or <paramref name="logger"/> is null.
    /// </exception>
    public ReferenceNavigationService(
        IEditorService editorService,
        IEditorNavigationService editorNavigationService,
        IMediator mediator,
        ILogger<ReferenceNavigationService> logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _editorNavigationService = editorNavigationService ?? throw new ArgumentNullException(nameof(editorNavigationService));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ReferenceNavigationService initialized");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Extracts document path and chunk offsets from the SearchHit,
    /// then delegates to <see cref="NavigateToOffsetAsync"/>. On success,
    /// publishes a <see cref="ReferenceNavigatedEvent"/> for telemetry.
    ///
    /// Version: v0.4.6c
    /// </remarks>
    public async Task<bool> NavigateToHitAsync(SearchHit hit, CancellationToken ct = default)
    {
        // LOGIC: Guard against null hit.
        if (hit == null)
        {
            _logger.LogWarning("NavigateToHitAsync called with null hit");
            return false;
        }

        // LOGIC: Extract document path from the hit.
        var documentPath = hit.Document?.FilePath;
        if (string.IsNullOrEmpty(documentPath))
        {
            _logger.LogWarning("SearchHit has no document path");
            return false;
        }

        // LOGIC: Calculate offset and length from chunk metadata.
        var offset = hit.Chunk?.StartOffset ?? 0;
        var endOffset = hit.Chunk?.EndOffset ?? 0;
        var length = endOffset - offset;
        if (length < 0) length = 0;

        _logger.LogDebug(
            "Navigating to hit: Document={Document}, Offset={Offset}, Length={Length}",
            documentPath, offset, length);

        // LOGIC: Delegate to offset-based navigation.
        var result = await NavigateToOffsetAsync(documentPath, offset, length, ct);

        // LOGIC: Publish telemetry event on successful navigation.
        if (result)
        {
            await _mediator.Publish(new ReferenceNavigatedEvent
            {
                DocumentPath = documentPath,
                Offset = offset,
                Length = length,
                Score = hit.Score
            }, ct);

            _logger.LogDebug(
                "Published ReferenceNavigatedEvent: Document={Document}, Score={Score}",
                documentPath, hit.Score);
        }

        return result;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// LOGIC: Opens the document if not already open, then uses the existing
    /// <see cref="IEditorNavigationService"/> to scroll and highlight.
    ///
    /// Version: v0.4.6c
    /// </remarks>
    public async Task<bool> NavigateToOffsetAsync(
        string documentPath,
        int offset,
        int length = 0,
        CancellationToken ct = default)
    {
        // LOGIC: Guard against empty path.
        if (string.IsNullOrEmpty(documentPath))
        {
            _logger.LogWarning("NavigateToOffsetAsync called with empty path");
            return false;
        }

        // LOGIC: Clamp negative offsets to zero.
        if (offset < 0)
        {
            _logger.LogWarning("Invalid negative offset: {Offset}, clamping to 0", offset);
            offset = 0;
        }

        try
        {
            // LOGIC: Step 1 — Check if document is already open.
            var document = _editorService.GetDocumentByPath(documentPath);

            if (document is null)
            {
                // LOGIC: Step 2 — Open the document from disk.
                _logger.LogDebug("Opening document: {Path}", documentPath);
                document = await _editorService.OpenDocumentAsync(documentPath);

                if (document is null)
                {
                    _logger.LogWarning("Failed to open document: {Path}", documentPath);
                    return false;
                }
            }

            // LOGIC: Step 3 — Get the document ID for the navigation service.
            var documentId = document.DocumentId;

            _logger.LogDebug(
                "Navigating to offset: DocumentId={DocumentId}, Offset={Offset}, Length={Length}",
                documentId, offset, length);

            // LOGIC: Step 4 — Delegate to IEditorNavigationService for scroll/highlight.
            var navigationResult = await _editorNavigationService.NavigateToOffsetAsync(
                documentId, offset, length, ct);

            if (!navigationResult.Success)
            {
                _logger.LogWarning(
                    "Editor navigation failed: {Path}:{Offset} — {Error}",
                    documentPath, offset, navigationResult.ErrorMessage);
                return false;
            }

            _logger.LogInformation(
                "Navigation successful: {Path}:{Offset}",
                documentPath, offset);

            return true;
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Document not found: {Path}", documentPath);
            return false;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogDebug("Navigation cancelled: {Path}", documentPath);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Navigation failed: {Path}:{Offset}", documentPath, offset);
            return false;
        }
    }
}
