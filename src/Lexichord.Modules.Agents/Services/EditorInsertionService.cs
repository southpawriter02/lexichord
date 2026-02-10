// -----------------------------------------------------------------------
// <copyright file="EditorInsertionService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Avalonia.Threading;
using Lexichord.Abstractions.Contracts.Editor;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Implementation of <see cref="IEditorInsertionService"/> with preview support.
/// </summary>
/// <remarks>
/// <para>
/// LOGIC: Orchestrates AI-generated text insertion into the editor, supporting
/// both immediate and preview-before-commit workflows. All document modifications
/// are dispatched to the UI thread and wrapped in undo groups for single-step
/// reversal.
/// </para>
/// <para><b>Introduced in:</b> v0.6.7b as part of the Inline Suggestions feature.</para>
/// </remarks>
public class EditorInsertionService : IEditorInsertionService, IDisposable
{
    private readonly IEditorService _editorService;
    private readonly ILogger<EditorInsertionService> _logger;

    private string? _previewText;
    private TextSpan? _previewLocation;
    private bool _isPreviewActive;

    /// <summary>
    /// Initializes a new instance of the EditorInsertionService.
    /// </summary>
    public EditorInsertionService(
        IEditorService editorService,
        ILogger<EditorInsertionService> logger)
    {
        _editorService = editorService ?? throw new ArgumentNullException(nameof(editorService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc/>
    public bool IsPreviewActive => _isPreviewActive;

    /// <inheritdoc/>
    public string? CurrentPreviewText => _previewText;

    /// <inheritdoc/>
    public TextSpan? CurrentPreviewLocation => _previewLocation;

    /// <inheritdoc/>
    public event EventHandler<PreviewStateChangedEventArgs>? PreviewStateChanged;

    /// <inheritdoc/>
    public async Task InsertAtCursorAsync(string text, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        _logger.LogDebug("InsertAtCursorAsync: {CharCount} chars", text.Length);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // ─────────────────────────────────────────────────────────────
            // Get Current Cursor Position
            // ─────────────────────────────────────────────────────────────
            var position = _editorService.CaretOffset;
            if (position < 0)
            {
                throw new InvalidOperationException(
                    "Cannot determine cursor position.");
            }

            _logger.LogDebug("Inserting at position {Position}", position);

            // ─────────────────────────────────────────────────────────────
            // Begin Undo Group for Single-Step Reversal
            // ─────────────────────────────────────────────────────────────
            _editorService.BeginUndoGroup("AI Insertion");

            try
            {
                // ─────────────────────────────────────────────────────────
                // Perform Insertion
                // ─────────────────────────────────────────────────────────
                _editorService.InsertText(position, text);

                // ─────────────────────────────────────────────────────────
                // Move Cursor to End of Inserted Text
                // ─────────────────────────────────────────────────────────
                _editorService.CaretOffset = position + text.Length;

                _logger.LogInformation(
                    "Text inserted: {CharCount} chars at position {Position}",
                    text.Length, position);
            }
            finally
            {
                // ─────────────────────────────────────────────────────────
                // End Undo Group
                // ─────────────────────────────────────────────────────────
                _editorService.EndUndoGroup();
            }
        });
    }

    /// <inheritdoc/>
    public async Task ReplaceSelectionAsync(string text, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        _logger.LogDebug("ReplaceSelectionAsync: {CharCount} chars", text.Length);

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // ─────────────────────────────────────────────────────────────
            // Validate Selection
            // ─────────────────────────────────────────────────────────────
            if (!_editorService.HasSelection)
            {
                throw new InvalidOperationException("No selection is active.");
            }

            var selectionStart = _editorService.SelectionStart;
            var selectionLength = _editorService.SelectionLength;

            _logger.LogDebug(
                "Replacing selection at {Start}, length {Length}",
                selectionStart, selectionLength);

            // ─────────────────────────────────────────────────────────────
            // Begin Undo Group
            // ─────────────────────────────────────────────────────────────
            _editorService.BeginUndoGroup("AI Replacement");

            try
            {
                // ─────────────────────────────────────────────────────────
                // Delete Selection and Insert Replacement
                // ─────────────────────────────────────────────────────────
                _editorService.DeleteText(selectionStart, selectionLength);
                _editorService.InsertText(selectionStart, text);

                // ─────────────────────────────────────────────────────────
                // Clear Selection, Position Cursor
                // ─────────────────────────────────────────────────────────
                _editorService.ClearSelection();
                _editorService.CaretOffset = selectionStart + text.Length;

                _logger.LogInformation(
                    "Selection replaced: {OldLength} → {NewLength} chars",
                    selectionLength, text.Length);
            }
            finally
            {
                _editorService.EndUndoGroup();
            }
        });
    }

    /// <inheritdoc/>
    public async Task ShowPreviewAsync(
        string text,
        TextSpan location,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(location);

        _logger.LogDebug(
            "ShowPreviewAsync: {CharCount} chars at {Location}",
            text.Length, location);

        // ─────────────────────────────────────────────────────────────────
        // Dismiss Any Existing Preview
        // ─────────────────────────────────────────────────────────────────
        if (_isPreviewActive)
        {
            _logger.LogDebug("Dismissing existing preview");
            await RejectPreviewAsync(ct);
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            // ─────────────────────────────────────────────────────────────
            // Store Preview State
            // ─────────────────────────────────────────────────────────────
            _previewText = text;
            _previewLocation = location;
            _isPreviewActive = true;

            // ─────────────────────────────────────────────────────────────
            // Notify UI to Display Overlay
            // ─────────────────────────────────────────────────────────────
            PreviewStateChanged?.Invoke(this, new PreviewStateChangedEventArgs
            {
                IsActive = true,
                PreviewText = text,
                Location = location
            });

            _logger.LogDebug("Preview activated at location {Location}", location);
        });
    }

    /// <inheritdoc/>
    public async Task<bool> AcceptPreviewAsync(CancellationToken ct = default)
    {
        if (!_isPreviewActive || _previewText == null || _previewLocation == null)
        {
            _logger.LogDebug("AcceptPreviewAsync: No active preview");
            return false;
        }

        var text = _previewText;
        var location = _previewLocation;

        _logger.LogDebug("Accepting preview: {CharCount} chars", text.Length);

        // ─────────────────────────────────────────────────────────────────
        // Clear Preview State First
        // ─────────────────────────────────────────────────────────────────
        ClearPreviewState();

        // ─────────────────────────────────────────────────────────────────
        // Commit to Document Based on Location Type
        // ─────────────────────────────────────────────────────────────────
        if (location.Length == 0)
        {
            // Insertion at position
            await InsertAtPositionAsync(text, location.Start, ct);
        }
        else
        {
            // Replacement of existing text
            await ReplaceRangeAsync(text, location, ct);
        }

        _logger.LogInformation("Preview accepted and committed");
        return true;
    }

    /// <inheritdoc/>
    public Task RejectPreviewAsync(CancellationToken ct = default)
    {
        if (!_isPreviewActive)
        {
            _logger.LogDebug("RejectPreviewAsync: No active preview");
            return Task.CompletedTask;
        }

        _logger.LogDebug("Rejecting preview");
        ClearPreviewState();

        return Task.CompletedTask;
    }

    private void ClearPreviewState()
    {
        _previewText = null;
        _previewLocation = null;
        _isPreviewActive = false;

        PreviewStateChanged?.Invoke(this, new PreviewStateChangedEventArgs
        {
            IsActive = false
        });
    }

    private async Task InsertAtPositionAsync(
        string text,
        int position,
        CancellationToken ct)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _editorService.BeginUndoGroup("AI Insertion");
            try
            {
                _editorService.InsertText(position, text);
                _editorService.CaretOffset = position + text.Length;
            }
            finally
            {
                _editorService.EndUndoGroup();
            }
        });
    }

    private async Task ReplaceRangeAsync(
        string text,
        TextSpan location,
        CancellationToken ct)
    {
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            _editorService.BeginUndoGroup("AI Replacement");
            try
            {
                _editorService.DeleteText(location.Start, location.Length);
                _editorService.InsertText(location.Start, text);
                _editorService.CaretOffset = location.Start + text.Length;
            }
            finally
            {
                _editorService.EndUndoGroup();
            }
        });
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_isPreviewActive)
        {
            ClearPreviewState();
        }
    }
}
