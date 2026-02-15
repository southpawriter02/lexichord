// -----------------------------------------------------------------------
// <copyright file="IRewriteApplicator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Forward-declared interface for the rewrite applicator (v0.7.3d).
//   Defines the contract for applying rewrite results to the document with
//   undo/redo support. The implementation will be provided in v0.7.3d.
//
//   In v0.7.3b, RewriteCommandHandler accepts IRewriteApplicator? (nullable)
//   and skips document application when no applicator is registered.
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Applies rewrite results to the document with undo/redo support.
/// </summary>
/// <remarks>
/// <para>
/// This interface is <b>forward-declared</b> in v0.7.3b for use by
/// <see cref="RewriteCommandHandler"/>. The concrete implementation
/// (<c>RewriteApplicator</c>) will be provided in v0.7.3d (Undo/Redo Integration).
/// </para>
/// <para>
/// Until v0.7.3d is implemented, <see cref="RewriteCommandHandler"/> accepts
/// this as an optional dependency and skips document application when null.
/// </para>
/// <para><b>Forward-declared in:</b> v0.7.3b</para>
/// <para><b>Implemented in:</b> v0.7.3d</para>
/// </remarks>
public interface IRewriteApplicator
{
    /// <summary>
    /// Applies a rewrite result to the document with undo support.
    /// </summary>
    /// <param name="documentPath">Path to the document being edited.</param>
    /// <param name="selectionSpan">The span of the original selection.</param>
    /// <param name="result">The rewrite result to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the rewrite was successfully applied; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Creates a <c>RewriteUndoableOperation</c>, executes it to replace
    /// the text, and pushes it onto the <c>IUndoRedoService</c> stack.
    /// </remarks>
    Task<bool> ApplyRewriteAsync(
        string documentPath,
        TextSpan selectionSpan,
        RewriteResult result,
        CancellationToken ct = default);

    /// <summary>
    /// Previews a rewrite without committing to the undo stack.
    /// </summary>
    /// <param name="documentPath">Path to the document being edited.</param>
    /// <param name="selectionSpan">The span of the original selection.</param>
    /// <param name="previewText">The preview text to display.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Replaces text in the document but does not push to the undo stack.
    /// Call <see cref="CommitPreviewAsync"/> to finalize or
    /// <see cref="CancelPreviewAsync"/> to revert.
    /// </remarks>
    Task PreviewRewriteAsync(
        string documentPath,
        TextSpan selectionSpan,
        string previewText,
        CancellationToken ct = default);

    /// <summary>
    /// Commits the current preview to the undo stack.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Creates the undo operation for the previewed text and pushes
    /// it to the undo stack without re-executing the text replacement.
    /// </remarks>
    Task CommitPreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Cancels the current preview and restores the original text.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Replaces the preview text with the original text stored
    /// during <see cref="PreviewRewriteAsync"/>. No undo entry is created.
    /// </remarks>
    Task CancelPreviewAsync(CancellationToken ct = default);
}
