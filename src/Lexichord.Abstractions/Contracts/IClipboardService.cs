// -----------------------------------------------------------------------
// <copyright file="IClipboardService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: General-purpose clipboard service interface (v0.7.6c).
//   Provides platform-agnostic clipboard operations for text content.
//
//   Note: This is a general abstraction separate from ICitationClipboardService,
//   which is domain-specific to the Citation Engine (v0.5.2d).
//
//   Methods:
//     - SetTextAsync: Copies text to the clipboard
//     - GetTextAsync: Retrieves text from the clipboard
//     - ClearAsync: Clears clipboard contents
//     - ContainsTextAsync: Checks if clipboard contains text
//
//   Introduced in: v0.7.6c
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// General-purpose service for system clipboard operations.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The <see cref="IClipboardService"/> provides a platform-agnostic
/// abstraction over the system clipboard. It supports basic text operations including
/// copy, paste, and clear functionality.
/// </para>
/// <para>
/// <b>Relationship to ICitationClipboardService:</b>
/// This interface is a general-purpose clipboard abstraction, distinct from
/// <see cref="ICitationClipboardService"/> which provides domain-specific
/// citation formatting and copy operations. Use <see cref="IClipboardService"/>
/// for generic text clipboard operations; use <see cref="ICitationClipboardService"/>
/// for citation-specific operations.
/// </para>
/// <para>
/// <b>Platform Implementation:</b>
/// The default implementation uses Avalonia's <c>IClipboard</c> interface for
/// cross-platform compatibility. Implementations must handle UI thread marshalling
/// as clipboard access is thread-sensitive in most UI frameworks.
/// </para>
/// <para>
/// <b>Thread Safety:</b>
/// Implementations must be thread-safe. Clipboard operations may be invoked from
/// background threads and should be marshalled to the UI thread as needed.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.6c as part of the Summarizer Agent Export Formats.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Copy formatted summary to clipboard
/// var summaryText = FormatSummaryAsMarkdown(summary);
/// await clipboardService.SetTextAsync(summaryText, ct);
///
/// // Check if clipboard has content before pasting
/// if (await clipboardService.ContainsTextAsync(ct))
/// {
///     var text = await clipboardService.GetTextAsync(ct);
///     // Process pasted content
/// }
/// </code>
/// </example>
/// <seealso cref="ICitationClipboardService"/>
public interface IClipboardService
{
    /// <summary>
    /// Copies text to the system clipboard.
    /// </summary>
    /// <param name="text">The text to copy. Must not be <c>null</c>.</param>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that completes when the text has been copied.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="text"/> is <c>null</c>.
    /// </exception>
    /// <remarks>
    /// <b>LOGIC:</b> Replaces any existing clipboard content with the provided text.
    /// The operation is asynchronous to support UI thread marshalling on some platforms.
    /// Empty strings are valid and will clear text content while preserving other
    /// clipboard data types.
    /// </remarks>
    Task SetTextAsync(string text, CancellationToken ct = default);

    /// <summary>
    /// Retrieves text from the system clipboard.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with the clipboard text content, or <c>null</c> if
    /// the clipboard doesn't contain text.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Returns the text content from the clipboard if available.
    /// Returns <c>null</c> if:
    /// <list type="bullet">
    /// <item><description>The clipboard is empty</description></item>
    /// <item><description>The clipboard contains non-text data (images, files, etc.)</description></item>
    /// <item><description>The clipboard is locked by another application</description></item>
    /// </list>
    /// </remarks>
    Task<string?> GetTextAsync(CancellationToken ct = default);

    /// <summary>
    /// Clears all content from the system clipboard.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that completes when the clipboard has been cleared.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Removes all content (text, images, files) from the clipboard.
    /// Use with caution as this may remove user data that wasn't related to
    /// the current operation.
    /// </remarks>
    Task ClearAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if the clipboard contains text content.
    /// </summary>
    /// <param name="ct">Cancellation token for cooperative cancellation.</param>
    /// <returns>
    /// A task that completes with <c>true</c> if the clipboard contains text;
    /// otherwise <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Lightweight check for text availability without retrieving content.
    /// Useful for enabling/disabling paste actions in UI.
    /// </remarks>
    Task<bool> ContainsTextAsync(CancellationToken ct = default);
}
