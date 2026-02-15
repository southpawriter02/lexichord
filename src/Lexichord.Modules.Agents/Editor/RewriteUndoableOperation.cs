// -----------------------------------------------------------------------
// <copyright file="RewriteUndoableOperation.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: An undoable operation for AI text rewrites (v0.7.3d).
//   Captures the original and rewritten text along with the document path
//   and selection span to enable full undo/redo cycles.
//
//   Text manipulation uses IEditorService sync APIs:
//     - BeginUndoGroup(DisplayName) — atomic undo grouping
//     - DeleteText(offset, length)  — remove existing text
//     - InsertText(offset, text)    — insert replacement text
//     - EndUndoGroup()              — close undo group
//
//   This wraps the editor's built-in undo system for atomic operations
//   while IUndoRedoService (when available) provides labeled operation
//   tracking for the undo history UI.
//
//   Spec adaptation: The spec uses ReplaceTextAsync(path, span, text, ct)
//   which does not exist on IEditorService. Adapted to use the sync
//   DeleteText + InsertText pattern from EditorInsertionService (v0.6.7b).
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Undo;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// An undoable operation for AI text rewrites.
/// </summary>
/// <remarks>
/// <para>
/// Captures the original text, rewritten text, document path, and selection span
/// at construction time. The <see cref="ExecuteAsync"/> method replaces the original
/// text with the rewritten version, <see cref="UndoAsync"/> restores the original,
/// and <see cref="RedoAsync"/> reapplies the rewrite.
/// </para>
/// <para>
/// Text manipulation uses <see cref="IEditorService"/> sync APIs wrapped in
/// <see cref="IEditorService.BeginUndoGroup"/>/<see cref="IEditorService.EndUndoGroup"/>
/// for atomic undo behavior at the editor level. The <see cref="DisplayName"/> property
/// provides the label shown in the undo history (e.g., "AI Rewrite (Formal)").
/// </para>
/// <para>
/// <b>Spec adaptation:</b> The spec references <c>ReplaceTextAsync(path, span, text, ct)</c>
/// which does not exist on <see cref="IEditorService"/>. This implementation uses
/// <see cref="IEditorService.DeleteText"/> + <see cref="IEditorService.InsertText"/>
/// (sync APIs from v0.6.7b).
/// </para>
/// <para><b>Introduced in:</b> v0.7.3d</para>
/// </remarks>
public class RewriteUndoableOperation : IUndoableOperation
{
    // ── Immutable State ────────────────────────────────────────────────
    private readonly string _documentPath;
    private readonly TextSpan _originalSpan;
    private readonly string _originalText;
    private readonly string _rewrittenText;
    private readonly RewriteIntent _intent;
    private readonly IEditorService _editorService;
    private readonly ILogger<RewriteUndoableOperation>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="RewriteUndoableOperation"/>.
    /// </summary>
    /// <param name="documentPath">Path to the document being edited.</param>
    /// <param name="originalSpan">The span of the original selection.</param>
    /// <param name="originalText">The original text before the rewrite.</param>
    /// <param name="rewrittenText">The rewritten text to apply.</param>
    /// <param name="intent">The rewrite intent that was applied.</param>
    /// <param name="editorService">Editor service for text manipulation.</param>
    /// <param name="logger">Optional logger for debug diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="documentPath"/>, <paramref name="originalText"/>,
    /// <paramref name="rewrittenText"/>, or <paramref name="editorService"/> is null.
    /// </exception>
    public RewriteUndoableOperation(
        string documentPath,
        TextSpan originalSpan,
        string originalText,
        string rewrittenText,
        RewriteIntent intent,
        IEditorService editorService,
        ILogger<RewriteUndoableOperation>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(documentPath);
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(rewrittenText);
        ArgumentNullException.ThrowIfNull(editorService);

        _documentPath = documentPath;
        _originalSpan = originalSpan;
        _originalText = originalText;
        _rewrittenText = rewrittenText;
        _intent = intent;
        _editorService = editorService;
        _logger = logger;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Generated via <see cref="Guid.NewGuid"/> at construction time.
    /// Unique across all operations for event correlation and logging.
    /// </remarks>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Format is "AI Rewrite ({Intent})" where Intent is one of
    /// Formal, Simplified, Expanded, or Custom. Shown in the undo history
    /// and Edit menu as "Undo AI Rewrite (Formal)".
    /// </remarks>
    public string DisplayName => $"AI Rewrite ({_intent})";

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Set to <see cref="DateTime.UtcNow"/> at construction time.
    /// Used for chronological display in undo history (e.g., "2 minutes ago").
    /// </remarks>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Replaces the original text with the rewritten text using:
    /// <list type="number">
    ///   <item><description><see cref="IEditorService.BeginUndoGroup"/> with <see cref="DisplayName"/></description></item>
    ///   <item><description><see cref="IEditorService.DeleteText"/> at the original span position</description></item>
    ///   <item><description><see cref="IEditorService.InsertText"/> with the rewritten text</description></item>
    ///   <item><description><see cref="IEditorService.EndUndoGroup"/> to close the atomic group</description></item>
    /// </list>
    /// </remarks>
    public Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Executing rewrite {OperationId}: replacing {OriginalLength} chars with {RewrittenLength} chars at offset {Offset}",
            Id, _originalText.Length, _rewrittenText.Length, _originalSpan.Start);

        // LOGIC: Wrap in undo group for atomic editor-level undo.
        // The BeginUndoGroup/EndUndoGroup ensures that the delete + insert
        // are treated as a single Ctrl+Z operation by the editor.
        ReplaceText(_originalSpan.Start, _originalText.Length, _rewrittenText);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Restores the original text by calculating the current span
    /// based on the rewritten text length (since the rewrite may have changed
    /// the text length), then replacing it with the original text.
    /// </remarks>
    public Task UndoAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Undoing rewrite {OperationId}: restoring {OriginalLength} chars of original text at offset {Offset}",
            Id, _originalText.Length, _originalSpan.Start);

        // LOGIC: After ExecuteAsync, the text at _originalSpan.Start has length
        // _rewrittenText.Length. Replace it back with the original text.
        ReplaceText(_originalSpan.Start, _rewrittenText.Length, _originalText);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Re-applies the rewrite by replacing the original text span
    /// (which was restored by <see cref="UndoAsync"/>) with the rewritten text.
    /// Uses the same span as <see cref="ExecuteAsync"/> since undo restored
    /// the original length.
    /// </remarks>
    public Task RedoAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Redoing rewrite {OperationId}: reapplying {RewrittenLength} chars of rewritten text at offset {Offset}",
            Id, _rewrittenText.Length, _originalSpan.Start);

        // LOGIC: After UndoAsync, the text is back to original length.
        // Replace it with the rewritten text again.
        ReplaceText(_originalSpan.Start, _originalText.Length, _rewrittenText);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a debug-friendly summary of the operation.
    /// </summary>
    /// <returns>A string describing the operation type, ID, intent, and character counts.</returns>
    public override string ToString() =>
        $"RewriteOperation({Id}): {_intent}, {_originalText.Length} -> {_rewrittenText.Length} chars at [{_originalSpan.Start}..{_originalSpan.End})";

    // ── Private Helpers ────────────────────────────────────────────────

    /// <summary>
    /// Performs an atomic text replacement using the editor's sync APIs.
    /// </summary>
    /// <param name="offset">The 0-based character offset to start replacing at.</param>
    /// <param name="deleteLength">Number of characters to delete.</param>
    /// <param name="insertText">The text to insert after deletion.</param>
    /// <remarks>
    /// LOGIC: Wraps DeleteText + InsertText in BeginUndoGroup/EndUndoGroup
    /// for atomic undo at the editor level. This ensures the editor's built-in
    /// Ctrl+Z treats the delete + insert as a single operation.
    ///
    /// Spec adaptation: Replaces the spec's ReplaceTextAsync(path, span, text, ct)
    /// with the actual IEditorService sync APIs (v0.6.7b).
    /// </remarks>
    private void ReplaceText(int offset, int deleteLength, string insertText)
    {
        _editorService.BeginUndoGroup(DisplayName);
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
}
