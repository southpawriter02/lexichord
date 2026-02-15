// -----------------------------------------------------------------------
// <copyright file="IRewriteApplicator.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: Contract for applying rewrite results to the document with
//   undo/redo support and preview capability (v0.7.3d).
//
//   Forward-declared in v0.7.3b for use by RewriteCommandHandler.
//   Concrete implementation (RewriteApplicator) provided in v0.7.3d.
//
//   In v0.7.3b, RewriteCommandHandler accepts IRewriteApplicator? (nullable)
//   and skips document application when no applicator is registered.
//   With v0.7.3d, the applicator is registered and the handler delegates
//   document application to it.
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts.Editor;

namespace Lexichord.Modules.Agents.Editor;

/// <summary>
/// Applies rewrite results to the document with undo/redo support and preview capability.
/// </summary>
/// <remarks>
/// <para>
/// This interface was <b>forward-declared</b> in v0.7.3b for use by
/// <see cref="RewriteCommandHandler"/>. The concrete implementation
/// (<see cref="RewriteApplicator"/>) is provided in v0.7.3d (Undo/Redo Integration).
/// </para>
/// <para>
/// <see cref="RewriteCommandHandler"/> accepts this as an optional dependency
/// (<c>IRewriteApplicator?</c>) and delegates document application when available.
/// </para>
/// <para><b>Forward-declared in:</b> v0.7.3b</para>
/// <para><b>Implemented in:</b> v0.7.3d</para>
/// </remarks>
public interface IRewriteApplicator
{
    /// <summary>
    /// Gets whether a preview is currently active.
    /// </summary>
    /// <remarks>
    /// LOGIC: Returns true between <see cref="PreviewRewriteAsync"/> and either
    /// <see cref="CommitPreviewAsync"/> or <see cref="CancelPreviewAsync"/>.
    /// Used by UI to show preview indicators and disable conflicting operations.
    /// <para><b>Added in:</b> v0.7.3d</para>
    /// </remarks>
    bool IsPreviewActive { get; }

    /// <summary>
    /// Applies a rewrite result to the document with undo support.
    /// </summary>
    /// <param name="documentPath">Path to the document being edited.</param>
    /// <param name="selectionSpan">The span of the original selection.</param>
    /// <param name="result">The rewrite result to apply.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the rewrite was successfully applied; false otherwise.</returns>
    /// <remarks>
    /// LOGIC: Creates a <see cref="RewriteUndoableOperation"/>, executes it to replace
    /// the text, and pushes it onto the <c>IUndoRedoService</c> stack (if available).
    /// Cancels any active preview before applying. Publishes
    /// <see cref="Events.RewriteAppliedEvent"/> on success.
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
    /// LOGIC: Stores the original text, replaces the document text with
    /// <paramref name="previewText"/>, and starts a 5-minute preview timeout.
    /// The preview is NOT pushed to the undo stack. Call <see cref="CommitPreviewAsync"/>
    /// to finalize or <see cref="CancelPreviewAsync"/> to revert.
    /// Publishes <see cref="Events.RewritePreviewStartedEvent"/>.
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
    /// it to the undo stack without re-executing the text replacement
    /// (text was already replaced during <see cref="PreviewRewriteAsync"/>).
    /// Publishes <see cref="Events.RewritePreviewCommittedEvent"/>.
    /// No-op if no preview is active.
    /// </remarks>
    Task CommitPreviewAsync(CancellationToken ct = default);

    /// <summary>
    /// Cancels the current preview and restores the original text.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <remarks>
    /// LOGIC: Replaces the preview text with the original text stored
    /// during <see cref="PreviewRewriteAsync"/>. No undo entry is created.
    /// Publishes <see cref="Events.RewritePreviewCancelledEvent"/>.
    /// No-op if no preview is active.
    /// </remarks>
    Task CancelPreviewAsync(CancellationToken ct = default);
}
