// -----------------------------------------------------------------------
// <copyright file="TuningUndoableOperation.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: An undoable operation for Tuning Agent fix applications (v0.7.5c).
//   Captures the original text, applied text, and document offset to enable
//   full undo/redo cycles for accepted suggestions.
//
//   Text manipulation uses IEditorService sync APIs:
//     - BeginUndoGroup(DisplayName) — atomic undo grouping
//     - DeleteText(offset, length)  — remove existing text
//     - InsertText(offset, text)    — insert replacement text
//     - EndUndoGroup()              — close undo group
//
//   Pattern follows RewriteUndoableOperation (v0.7.3d).
//
//   Spec adaptation: The spec uses ReplaceTextAsync(path, span, text, ct)
//   which does not exist on IEditorService. Adapted to use the sync
//   DeleteText + InsertText pattern.
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Undo;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Agents.Tuning;

/// <summary>
/// An undoable operation for Tuning Agent fix suggestion applications.
/// </summary>
/// <remarks>
/// <para>
/// Captures the original text, applied text (suggested or modified), and document
/// offset at construction time. <see cref="UndoAsync"/> restores the original text,
/// and <see cref="RedoAsync"/> reapplies the fix.
/// </para>
/// <para>
/// Text manipulation uses <see cref="IEditorService"/> sync APIs wrapped in
/// <see cref="IEditorService.BeginUndoGroup"/>/<see cref="IEditorService.EndUndoGroup"/>
/// for atomic undo behavior at the editor level.
/// </para>
/// <para>
/// <b>Spec adaptation:</b> The spec references <c>ReplaceTextAsync</c> and
/// <c>BeginUndoGroupAsync</c> which do not exist on <see cref="IEditorService"/>.
/// This implementation uses the sync <see cref="IEditorService.DeleteText"/> +
/// <see cref="IEditorService.InsertText"/> pattern (v0.6.7b).
/// </para>
/// <para><b>Introduced in:</b> v0.7.5c</para>
/// </remarks>
/// <seealso cref="IUndoableOperation"/>
/// <seealso cref="TuningPanelViewModel"/>
public class TuningUndoableOperation : IUndoableOperation
{
    // ── Immutable State ────────────────────────────────────────────────
    private readonly int _offset;
    private readonly string _originalText;
    private readonly string _appliedText;
    private readonly IEditorService _editorService;
    private readonly ILogger<TuningUndoableOperation>? _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="TuningUndoableOperation"/>.
    /// </summary>
    /// <param name="offset">The 0-based character offset where the text starts.</param>
    /// <param name="originalText">The original text before the fix was applied.</param>
    /// <param name="appliedText">The text that was applied (suggested or user-modified).</param>
    /// <param name="ruleId">The rule ID for the display name.</param>
    /// <param name="editorService">Editor service for text manipulation.</param>
    /// <param name="logger">Optional logger for debug diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="originalText"/>, <paramref name="appliedText"/>,
    /// or <paramref name="editorService"/> is null.
    /// </exception>
    public TuningUndoableOperation(
        int offset,
        string originalText,
        string appliedText,
        string ruleId,
        IEditorService editorService,
        ILogger<TuningUndoableOperation>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(originalText);
        ArgumentNullException.ThrowIfNull(appliedText);
        ArgumentNullException.ThrowIfNull(editorService);

        _offset = offset;
        _originalText = originalText;
        _appliedText = appliedText;
        _editorService = editorService;
        _logger = logger;

        // LOGIC: Use the rule ID in the display name for undo history context.
        DisplayName = $"Tuning Fix ({ruleId})";
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Generated via <see cref="Guid.NewGuid"/> at construction time.
    /// Unique across all operations for event correlation and logging.
    /// </remarks>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Format is "Tuning Fix ({RuleId})". Shown in the undo history
    /// and Edit menu as "Undo Tuning Fix (rule-id)".
    /// </remarks>
    public string DisplayName { get; }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Set to <see cref="DateTime.UtcNow"/> at construction time.
    /// Used for chronological display in undo history.
    /// </remarks>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Applies the fix by replacing the original text with the applied text.
    /// Wraps the operation in an editor undo group for atomic Ctrl+Z support.
    /// </remarks>
    public Task ExecuteAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Executing tuning fix {OperationId}: replacing {OriginalLength} chars with {AppliedLength} chars at offset {Offset}",
            Id, _originalText.Length, _appliedText.Length, _offset);

        ReplaceText(_offset, _originalText.Length, _appliedText);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Restores the original text. After ExecuteAsync, the text at _offset
    /// has length _appliedText.Length — replace it with the original.
    /// </remarks>
    public Task UndoAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Undoing tuning fix {OperationId}: restoring {OriginalLength} chars at offset {Offset}",
            Id, _originalText.Length, _offset);

        ReplaceText(_offset, _appliedText.Length, _originalText);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    /// <remarks>
    /// LOGIC: Re-applies the fix. After UndoAsync, the text is back to the original —
    /// replace it with the applied text again.
    /// </remarks>
    public Task RedoAsync(CancellationToken ct = default)
    {
        _logger?.LogDebug(
            "Redoing tuning fix {OperationId}: reapplying {AppliedLength} chars at offset {Offset}",
            Id, _appliedText.Length, _offset);

        ReplaceText(_offset, _originalText.Length, _appliedText);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets a debug-friendly summary of the operation.
    /// </summary>
    public override string ToString() =>
        $"TuningOperation({Id}): {_originalText.Length} -> {_appliedText.Length} chars at [{_offset}]";

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
