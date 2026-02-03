// =============================================================================
// File: CitationClipboardService.cs
// Project: Lexichord.Modules.RAG
// Description: Clipboard operations for citations with license gating and telemetry.
// =============================================================================
// LOGIC: Implements ICitationClipboardService to copy citation data to clipboard.
//   - CopyCitationAsync: Formats citation via ICitationService.FormatCitation,
//     copies to clipboard, and publishes CitationCopiedEvent.
//   - CopyChunkTextAsync: Copies raw chunk content without formatting.
//   - CopyDocumentPathAsync: Copies path as string or file:// URI.
//   - License gating is delegated to ICitationService.FormatCitation.
//   - Uses Avalonia's TopLevel-based clipboard access pattern.
//   - All operations publish CitationCopiedEvent for telemetry.
// =============================================================================
// DEPENDENCIES (referenced by version):
//   - v0.5.2a: Citation, ICitationService
//   - v0.5.2b: CitationFormatterRegistry, CitationStyle
//   - v0.5.2d: CitationCopiedEvent, CitationCopyFormat
// =============================================================================

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.RAG.Services;

/// <summary>
/// Clipboard operations for citations with license gating and telemetry.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="CitationClipboardService"/> implements <see cref="ICitationClipboardService"/>
/// and provides clipboard operations for copying citation data in various formats.
/// It integrates with the formatting layer, respects license gating, and publishes
/// telemetry events.
/// </para>
/// <para>
/// <b>Clipboard Access:</b> Uses Avalonia's <see cref="IClipboard"/> via the
/// application lifetime's main window. Operations are no-ops if no window is available.
/// </para>
/// <para>
/// <b>License Gating:</b> Delegated to <see cref="ICitationService.FormatCitation"/>.
/// Core users receive the document path only when copying formatted citations.
/// </para>
/// <para>
/// <b>Telemetry:</b> All successful copy operations publish a <see cref="CitationCopiedEvent"/>
/// via MediatR for analytics and toast notification handlers.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This service is registered as a singleton. Clipboard operations
/// should be invoked on the UI thread; the service does not perform thread marshalling.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2d as part of the Citation Engine.
/// </para>
/// </remarks>
public sealed class CitationClipboardService : ICitationClipboardService
{
    private readonly ICitationService _citationService;
    private readonly CitationFormatterRegistry _formatterRegistry;
    private readonly IMediator _mediator;
    private readonly ILogger<CitationClipboardService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CitationClipboardService"/> class.
    /// </summary>
    /// <param name="citationService">
    /// Citation service for formatting citations. Used by <see cref="CopyCitationAsync"/>.
    /// </param>
    /// <param name="formatterRegistry">
    /// Formatter registry for retrieving the user's preferred citation style.
    /// </param>
    /// <param name="mediator">
    /// MediatR mediator for publishing <see cref="CitationCopiedEvent"/> notifications.
    /// </param>
    /// <param name="logger">
    /// Logger for structured diagnostic output during clipboard operations.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is null.
    /// </exception>
    public CitationClipboardService(
        ICitationService citationService,
        CitationFormatterRegistry formatterRegistry,
        IMediator mediator,
        ILogger<CitationClipboardService> logger)
    {
        _citationService = citationService ?? throw new ArgumentNullException(nameof(citationService));
        _formatterRegistry = formatterRegistry ?? throw new ArgumentNullException(nameof(formatterRegistry));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Citation copy flow:
    /// <list type="number">
    ///   <item><description>Validate that citation is not null.</description></item>
    ///   <item><description>If style is null, retrieve user's preferred style from registry.</description></item>
    ///   <item><description>Format citation via ICitationService.FormatCitation (license gating applied there).</description></item>
    ///   <item><description>Copy formatted string to clipboard.</description></item>
    ///   <item><description>Publish CitationCopiedEvent with FormattedCitation format.</description></item>
    /// </list>
    /// </remarks>
    public async Task CopyCitationAsync(Citation citation, CitationStyle? style = null, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(citation);

        // LOGIC: Determine style — use provided style or fall back to user preference.
        var effectiveStyle = style ?? await _formatterRegistry.GetPreferredStyleAsync(ct);

        _logger.LogDebug(
            "Copying citation for {DocumentPath} as {Style}",
            citation.DocumentPath, effectiveStyle);

        // LOGIC: Format citation via ICitationService (license gating applied there).
        var formattedCitation = _citationService.FormatCitation(citation, effectiveStyle);

        _logger.LogDebug(
            "Formatted citation: {FormattedCitation}",
            formattedCitation);

        // LOGIC: Copy to clipboard.
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            _logger.LogWarning("Clipboard not available — cannot copy citation");
            return;
        }

        await clipboard.SetTextAsync(formattedCitation);

        _logger.LogInformation(
            "Copied {Style} citation to clipboard for {DocumentPath}",
            effectiveStyle, citation.DocumentPath);

        // LOGIC: Publish telemetry event.
        await _mediator.Publish(
            new CitationCopiedEvent(
                citation,
                CitationCopyFormat.FormattedCitation,
                effectiveStyle,
                DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Chunk text copy flow:
    /// <list type="number">
    ///   <item><description>Validate parameters are not null/empty.</description></item>
    ///   <item><description>Copy raw text to clipboard (not license-gated).</description></item>
    ///   <item><description>Publish CitationCopiedEvent with ChunkText format.</description></item>
    /// </list>
    /// </remarks>
    public async Task CopyChunkTextAsync(Citation citation, string chunkText, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(citation);
        ArgumentNullException.ThrowIfNull(chunkText);

        _logger.LogDebug(
            "Copying chunk text ({Length} chars) for {DocumentPath}",
            chunkText.Length, citation.DocumentPath);

        // LOGIC: Copy to clipboard (not license-gated).
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            _logger.LogWarning("Clipboard not available — cannot copy chunk text");
            return;
        }

        await clipboard.SetTextAsync(chunkText);

        _logger.LogInformation(
            "Copied chunk text ({Length} chars) to clipboard for {DocumentPath}",
            chunkText.Length, citation.DocumentPath);

        // LOGIC: Publish telemetry event.
        await _mediator.Publish(
            new CitationCopiedEvent(
                citation,
                CitationCopyFormat.ChunkText,
                Style: null,
                DateTime.UtcNow),
            ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Document path copy flow:
    /// <list type="number">
    ///   <item><description>Validate citation is not null.</description></item>
    ///   <item><description>Format path as string or file:// URI based on asFileUri flag.</description></item>
    ///   <item><description>Copy path to clipboard (not license-gated).</description></item>
    ///   <item><description>Publish CitationCopiedEvent with DocumentPath or FileUri format.</description></item>
    /// </list>
    /// </remarks>
    public async Task CopyDocumentPathAsync(Citation citation, bool asFileUri = false, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(citation);

        var format = asFileUri ? CitationCopyFormat.FileUri : CitationCopyFormat.DocumentPath;
        var path = asFileUri
            ? $"file://{citation.DocumentPath.Replace(" ", "%20")}"
            : citation.DocumentPath;

        _logger.LogDebug(
            "Copying document path as {Format}: {Path}",
            format, path);

        // LOGIC: Copy to clipboard (not license-gated).
        var clipboard = GetClipboard();
        if (clipboard is null)
        {
            _logger.LogWarning("Clipboard not available — cannot copy document path");
            return;
        }

        await clipboard.SetTextAsync(path);

        _logger.LogInformation(
            "Copied document path to clipboard as {Format}",
            format);

        // LOGIC: Publish telemetry event.
        await _mediator.Publish(
            new CitationCopiedEvent(
                citation,
                format,
                Style: null,
                DateTime.UtcNow),
            ct);
    }

    // =========================================================================
    // Private Helpers
    // =========================================================================

    /// <summary>
    /// Gets the system clipboard from the application lifetime.
    /// </summary>
    /// <returns>
    /// The <see cref="IClipboard"/> instance, or null if no main window is available.
    /// </returns>
    /// <remarks>
    /// LOGIC: Avalonia's clipboard is accessed via TopLevel.GetTopLevel(window)?.Clipboard
    /// or via the main window. This method abstracts that access pattern and handles
    /// the case where the application is not fully initialized.
    /// </remarks>
    private static IClipboard? GetClipboard()
    {
        // LOGIC: Access clipboard via the main window's TopLevel.
        // This is the standard pattern for clipboard access in Avalonia.
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow?.Clipboard;
        }

        return null;
    }
}
