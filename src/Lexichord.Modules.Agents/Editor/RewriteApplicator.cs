// -----------------------------------------------------------------------
// <copyright file="RewriteApplicator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Applies rewrite results to the document with undo support and
//   preview capability (v0.7.3d). Implements IRewriteApplicator.
//
//   Coordinates between:
//     - IEditorService: sync text manipulation (DeleteText, InsertText)
//     - IUndoRedoService?: optional labeled operation tracking
//     - IMediator: event publishing for observability
//     - RewriteUndoableOperation: encapsulates undo/redo state
//
//   Preview mode allows users to try a rewrite before committing. Preview
//   state is tracked via a private PreviewState record with a lock for
//   thread safety and a 5-minute timeout for auto-cancellation.
//
//   Spec adaptations:
//     - IUndoRedoService is nullable (mirrors IRewriteApplicator? in handler)
//     - ReplaceTextAsync → DeleteText + InsertText (sync, wrapped in undo group)
//     - GetTextAsync → GetDocumentByPath()?.Content with GetDocumentText() fallback
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Undo;
using Lexichord.Modules.Agents.Editor.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Applies rewrite results to the document with undo support and preview capability.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="RewriteApplicator"/> is the concrete implementation of
/// <see cref="IRewriteApplicator"/> (forward-declared in v0.7.3b). It coordinates
/// text manipulation via <see cref="IEditorService"/>, operation tracking via
/// <see cref="IUndoRedoService"/> (when available), and event publishing via
/// <see cref="IMediator"/>.
/// </para>
/// <para>
/// <b>Nullable IUndoRedoService:</b> The undo service is accepted as nullable
/// because no concrete implementation exists yet (referenced as v0.1.4a in the
/// roadmap). When null, rewrites are still applied using the editor's built-in
/// undo (via <see cref="IEditorService.BeginUndoGroup"/>/<see cref="IEditorService.EndUndoGroup"/>),
/// but labeled operation tracking is unavailable.
/// </para>
/// <para>
/// <b>Preview Mode:</b> Allows users to see a rewrite before committing. Preview
/// text is applied to the document but NOT pushed to the undo stack. The user
/// can commit (creates undo entry) or cancel (restores original text). Previews
/// auto-cancel after 5 minutes.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
/// <seealso cref="IRewriteApplicator"/>
/// <seealso cref="RewriteUndoableOperation"/>
/// <seealso cref="RewriteCommandHandler"/>
public sealed class RewriteApplicator : IRewriteApplicator, IDisposable
{
    // ── Dependencies ────────────────────────────────────────────────────
    private readonly IEditorService _editorService;
    private readonly IUndoRedoService? _undoRedoService;
    private readonly IMediator _mediator;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<RewriteApplicator> _logger;

    // ── Preview State ───────────────────────────────────────────────────
    // LOGIC: Preview state is guarded by _previewLock for thread safety.
    // The preview timeout CTS auto-cancels after PreviewTimeoutMinutes.
    private PreviewState? _currentPreview;
    private readonly object _previewLock = new();
    private CancellationTokenSource? _previewTimeoutCts;

    /// <summary>
    /// The number of minutes before a preview auto-cancels.
    /// </summary>
    /// <remarks>
    /// LOGIC: Prevents orphaned previews if the user navigates away or
    /// forgets to commit/cancel. After timeout, the original text is restored.
    /// </remarks>
    internal const int PreviewTimeoutMinutes = 5;

    /// <summary>
    /// Initializes a new instance of <see cref="RewriteApplicator"/>.
    /// </summary>
    /// <param name="editorService">Editor service for document text manipulation.</param>
    /// <param name="undoRedoService">
    /// Optional undo/redo service for labeled operation tracking.
    /// Null until a concrete implementation is provided (referenced as v0.1.4a).
    /// </param>
    /// <param name="mediator">MediatR mediator for publishing lifecycle events.</param>
    /// <param name="loggerFactory">Logger factory for creating typed loggers.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="editorService"/>, <paramref name="mediator"/>,
    /// or <paramref name="loggerFactory"/> is null.
    /// </exception>
    public RewriteApplicator(
        IEditorService editorService,
        IUndoRedoService? undoRedoService,
        IMediator mediator,
        ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(editorService);
        ArgumentNullException.ThrowIfNull(mediator);
        ArgumentNullException.ThrowIfNull(loggerFactory);

        _editorService = editorService;
        _undoRedoService = undoRedoService;
        _mediator = mediator;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<RewriteApplicator>();
    }

    /// <inheritdoc />
    public bool IsPreviewActive
    {
        get
        {
            lock (_previewLock)
            {
                return _currentPreview is not null;
            }
        }
    }

    /// <inheritdoc />
    public async Task<bool> ApplyRewriteAsync(
        string documentPath,
        TextSpan selectionSpan,
        RewriteResult result,
        CancellationToken ct = default)
    {
        // LOGIC: Reject failed results early — never apply partial or empty rewrites.
        if (!result.Success)
        {
            _logger.LogWarning(
                "Cannot apply failed rewrite result for intent {Intent}: {ErrorMessage}",
                result.Intent, result.ErrorMessage);
            return false;
        }

        // LOGIC: Cancel any active preview before applying a new rewrite.
        // This prevents conflicting text states in the document.
        if (IsPreviewActive)
        {
            _logger.LogDebug("Cancelling active preview before applying rewrite");
            await CancelPreviewAsync(ct);
        }

        try
        {
            _logger.LogInformation(
                "Applying {Intent} rewrite to {DocumentPath}: {OriginalLength} -> {RewrittenLength} chars at span [{Start}..{End})",
                result.Intent, documentPath,
                result.OriginalText.Length, result.RewrittenText.Length,
                selectionSpan.Start, selectionSpan.End);

            // LOGIC: Create the undoable operation that encapsulates the text replacement.
            // The operation stores all state needed for undo/redo cycles.
            var operation = new RewriteUndoableOperation(
                documentPath,
                selectionSpan,
                result.OriginalText,
                result.RewrittenText,
                result.Intent,
                _editorService,
                _loggerFactory.CreateLogger<RewriteUndoableOperation>());

            // LOGIC: Execute the text replacement (DeleteText + InsertText wrapped in undo group).
            await operation.ExecuteAsync(ct);

            // LOGIC: Push to the undo service for labeled operation tracking (if available).
            // When IUndoRedoService is null, the editor's built-in undo still works via
            // BeginUndoGroup/EndUndoGroup in the operation.
            if (_undoRedoService is not null)
            {
                _undoRedoService.Push(operation);

                _logger.LogDebug(
                    "Pushed rewrite operation {OperationId} to undo stack: {DisplayName}",
                    operation.Id, operation.DisplayName);
            }
            else
            {
                _logger.LogDebug(
                    "IUndoRedoService not registered. Operation {OperationId} applied via editor undo group only.",
                    operation.Id);
            }

            // LOGIC: Publish event for observability, analytics, and downstream processing.
            await _mediator.Publish(
                RewriteAppliedEvent.Create(
                    documentPath,
                    result.OriginalText,
                    result.RewrittenText,
                    result.Intent,
                    operation.Id),
                ct);

            _logger.LogDebug(
                "Published RewriteAppliedEvent for operation {OperationId}",
                operation.Id);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to apply {Intent} rewrite to {DocumentPath}",
                result.Intent, documentPath);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task PreviewRewriteAsync(
        string documentPath,
        TextSpan selectionSpan,
        string previewText,
        CancellationToken ct = default)
    {
        // LOGIC: Cancel any existing preview before starting a new one.
        // Only one preview can be active at a time.
        if (IsPreviewActive)
        {
            _logger.LogDebug("Cancelling existing preview before starting new one");
            await CancelPreviewAsync(ct);
        }

        _logger.LogDebug(
            "Starting preview at span [{Start}..{End}) in {DocumentPath}",
            selectionSpan.Start, selectionSpan.End, documentPath);

        // LOGIC: Retrieve the original text before replacing.
        // Uses GetDocumentByPath for path-specific lookup with GetDocumentText fallback.
        var originalText = GetOriginalText(documentPath, selectionSpan);

        if (originalText is null)
        {
            _logger.LogWarning(
                "Cannot start preview: unable to retrieve original text for {DocumentPath} at span [{Start}..{End})",
                documentPath, selectionSpan.Start, selectionSpan.End);
            return;
        }

        // LOGIC: Store preview state under lock for thread safety.
        lock (_previewLock)
        {
            _currentPreview = new PreviewState(
                DocumentPath: documentPath,
                OriginalSpan: selectionSpan,
                OriginalText: originalText,
                PreviewText: previewText,
                StartTime: DateTime.UtcNow);
        }

        // LOGIC: Apply the preview text to the document (not pushed to undo stack).
        // We still use undo group so the editor's built-in undo can revert if needed.
        ReplaceText(selectionSpan.Start, selectionSpan.Length, previewText);

        // LOGIC: Start the preview timeout timer. Auto-cancels after PreviewTimeoutMinutes.
        CancelPreviewTimeout();
        _previewTimeoutCts = new CancellationTokenSource();
        _ = StartPreviewTimeoutAsync(_previewTimeoutCts.Token);

        // LOGIC: Publish event for UI state updates (show preview indicator bar).
        await _mediator.Publish(
            RewritePreviewStartedEvent.Create(documentPath, previewText),
            ct);

        _logger.LogDebug(
            "Preview started with {TimeoutMinutes}-minute timeout",
            PreviewTimeoutMinutes);
    }

    /// <inheritdoc />
    public async Task CommitPreviewAsync(CancellationToken ct = default)
    {
        PreviewState? preview;
        lock (_previewLock)
        {
            preview = _currentPreview;
            _currentPreview = null;
        }

        if (preview is null)
        {
            _logger.LogWarning("No active preview to commit");
            return;
        }

        // LOGIC: Stop the timeout timer — the preview is being committed.
        CancelPreviewTimeout();

        _logger.LogDebug(
            "Committing preview as permanent change in {DocumentPath}",
            preview.DocumentPath);

        // LOGIC: Create an undoable operation for the committed preview.
        // Don't execute it (text already applied during preview), just track it.
        var operation = new RewriteUndoableOperation(
            preview.DocumentPath,
            preview.OriginalSpan,
            preview.OriginalText,
            preview.PreviewText,
            RewriteIntent.Custom, // Preview commits are effectively custom intent
            _editorService,
            _loggerFactory.CreateLogger<RewriteUndoableOperation>());

        // LOGIC: Push to undo stack without re-executing (text already in document).
        if (_undoRedoService is not null)
        {
            _undoRedoService.Push(operation);

            _logger.LogDebug(
                "Pushed committed preview operation {OperationId} to undo stack",
                operation.Id);
        }
        else
        {
            _logger.LogDebug(
                "IUndoRedoService not registered. Committed preview {OperationId} not tracked in undo stack.",
                operation.Id);
        }

        // LOGIC: Publish event for UI state updates (hide preview indicator bar).
        await _mediator.Publish(
            RewritePreviewCommittedEvent.Create(preview.DocumentPath, operation.Id),
            ct);
    }

    /// <inheritdoc />
    public async Task CancelPreviewAsync(CancellationToken ct = default)
    {
        PreviewState? preview;
        lock (_previewLock)
        {
            preview = _currentPreview;
            _currentPreview = null;
        }

        // LOGIC: No-op if no preview is active (idempotent).
        if (preview is null)
        {
            return;
        }

        // LOGIC: Stop the timeout timer.
        CancelPreviewTimeout();

        _logger.LogDebug(
            "Cancelling preview in {DocumentPath}, restoring original text",
            preview.DocumentPath);

        // LOGIC: Calculate the current span based on preview text length
        // (the document now contains preview text, which may differ in length).
        var currentSpan = new TextSpan(
            preview.OriginalSpan.Start,
            preview.PreviewText.Length);

        // LOGIC: Restore the original text. No undo entry is created.
        ReplaceText(currentSpan.Start, currentSpan.Length, preview.OriginalText);

        // LOGIC: Publish event for UI state updates (hide preview indicator bar).
        await _mediator.Publish(
            RewritePreviewCancelledEvent.Create(preview.DocumentPath),
            ct);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // LOGIC: Cancel any active preview timeout to prevent orphaned tasks.
        CancelPreviewTimeout();
    }

    // ── Private Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Retrieves the text at the specified span from the document.
    /// </summary>
    /// <param name="documentPath">Path to the document.</param>
    /// <param name="span">The text span to retrieve.</param>
    /// <returns>The text at the span, or null if the document cannot be accessed.</returns>
    /// <remarks>
    /// LOGIC: Spec adaptation — replaces the spec's GetTextAsync(path, span, ct)
    /// with GetDocumentByPath()?.Content?.Substring() with GetDocumentText() fallback.
    /// </remarks>
    private string? GetOriginalText(string documentPath, TextSpan span)
    {
        try
        {
            // LOGIC: Try path-specific document lookup first.
            var document = _editorService.GetDocumentByPath(documentPath);
            var content = document?.Content;

            // LOGIC: Fallback to active document text if path lookup fails.
            if (content is null)
            {
                _logger.LogDebug(
                    "GetDocumentByPath returned null for {DocumentPath}, falling back to GetDocumentText",
                    documentPath);
                content = _editorService.GetDocumentText();
            }

            if (content is null)
            {
                _logger.LogWarning("No document content available for {DocumentPath}", documentPath);
                return null;
            }

            // LOGIC: Bounds check before substring extraction.
            if (span.Start < 0 || span.Start + span.Length > content.Length)
            {
                _logger.LogWarning(
                    "Span [{Start}..{End}) is out of bounds for document content (length {ContentLength})",
                    span.Start, span.End, content.Length);
                return null;
            }

            return content.Substring(span.Start, span.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error retrieving text from {DocumentPath} at span [{Start}..{End})",
                documentPath, span.Start, span.End);
            return null;
        }
    }

    /// <summary>
    /// Performs an atomic text replacement using the editor's sync APIs.
    /// </summary>
    /// <param name="offset">The 0-based character offset to start replacing at.</param>
    /// <param name="deleteLength">Number of characters to delete.</param>
    /// <param name="insertText">The text to insert after deletion.</param>
    /// <remarks>
    /// LOGIC: Wraps DeleteText + InsertText in BeginUndoGroup/EndUndoGroup
    /// for atomic undo at the editor level.
    /// </remarks>
    private void ReplaceText(int offset, int deleteLength, string insertText)
    {
        _editorService.BeginUndoGroup("AI Rewrite Preview");
        try
        {
            _editorService.DeleteText(offset, deleteLength);
            _editorService.InsertText(offset, insertText);
        }
        finally
        {
            _editorService.EndUndoGroup();
        }
    }

    /// <summary>
    /// Starts the preview timeout timer.
    /// </summary>
    /// <param name="ct">Cancellation token that cancels when the preview is committed or cancelled.</param>
    /// <remarks>
    /// LOGIC: Waits for <see cref="PreviewTimeoutMinutes"/> minutes, then auto-cancels
    /// the preview if it's still active. This prevents orphaned previews if the user
    /// navigates away or forgets to commit/cancel.
    /// </remarks>
    private async Task StartPreviewTimeoutAsync(CancellationToken ct)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(PreviewTimeoutMinutes), ct);

            // LOGIC: If we reach here without cancellation, the preview timed out.
            if (!ct.IsCancellationRequested && IsPreviewActive)
            {
                _logger.LogInformation(
                    "Preview timed out after {TimeoutMinutes} minutes, auto-cancelling",
                    PreviewTimeoutMinutes);

                await CancelPreviewAsync(CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            // LOGIC: Expected when the preview is committed or cancelled before timeout.
        }
    }

    /// <summary>
    /// Cancels the preview timeout timer if active.
    /// </summary>
    private void CancelPreviewTimeout()
    {
        _previewTimeoutCts?.Cancel();
        _previewTimeoutCts?.Dispose();
        _previewTimeoutCts = null;
    }

    /// <summary>
    /// Internal state for tracking a preview operation.
    /// </summary>
    /// <param name="DocumentPath">Path to the document showing the preview.</param>
    /// <param name="OriginalSpan">The original selection span before preview.</param>
    /// <param name="OriginalText">The original text before preview was applied.</param>
    /// <param name="PreviewText">The preview text currently shown in the document.</param>
    /// <param name="StartTime">UTC timestamp when the preview was started.</param>
    private record PreviewState(
        string DocumentPath,
        TextSpan OriginalSpan,
        string OriginalText,
        string PreviewText,
        DateTime StartTime);
}
