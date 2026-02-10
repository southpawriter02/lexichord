// -----------------------------------------------------------------------
// <copyright file="ISelectionContextService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Services;

/// <summary>
/// Bridges selected text in the editor to the Co-pilot chat interface.
/// </summary>
/// <remarks>
/// <para>
/// This service coordinates the flow of selected text from the editor to the
/// Co-pilot chat panel. It handles license verification, default prompt
/// generation, and focus management.
/// </para>
/// <para>
/// The service automatically generates contextually appropriate prompts based
/// on the selection characteristics (length, content type, structure).
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Typical usage from a command
/// if (_selectionService.HasActiveSelection)
/// {
///     var selection = _editorService.GetSelectedText();
///     await _selectionService.SendSelectionToCoPilotAsync(selection);
/// }
/// </code>
/// </example>
public interface ISelectionContextService
{
    /// <summary>
    /// Sends selected text to the Co-pilot chat with a generated default prompt.
    /// </summary>
    /// <param name="selection">The selected text from the editor.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="Lexichord.Abstractions.Agents.LicenseTierException">
    /// Thrown when the user does not have WriterPro license.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when selection is null or empty.
    /// </exception>
    Task SendSelectionToCoPilotAsync(string selection, CancellationToken ct = default);

    /// <summary>
    /// Generates an appropriate default prompt based on selection characteristics.
    /// </summary>
    /// <param name="selection">The selected text to analyze.</param>
    /// <returns>A contextually appropriate prompt string.</returns>
    /// <remarks>
    /// Prompt selection logic:
    /// <list type="bullet">
    /// <item>Short selection (&lt;50 chars): "Explain this:"</item>
    /// <item>Code-like selection: "Review this code:"</item>
    /// <item>Long selection (&gt;500 chars): "Summarize this:"</item>
    /// <item>Default: "Improve this:"</item>
    /// </list>
    /// </remarks>
    string GenerateDefaultPrompt(string selection);

    /// <summary>
    /// Gets whether the editor currently has an active text selection.
    /// </summary>
    bool HasActiveSelection { get; }

    /// <summary>
    /// Gets the currently selected text, or null if no selection.
    /// </summary>
    string? CurrentSelection { get; }

    /// <summary>
    /// Clears the current selection context from the Co-pilot.
    /// </summary>
    void ClearSelectionContext();
}
