// -----------------------------------------------------------------------
// <copyright file="RewriteStartedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when the rewrite command pipeline
//   begins execution (v0.7.3b). Published by RewriteCommandHandler after
//   license verification passes and before the EditorAgent is invoked.
//
//   Consumers:
//     - RewriteCommandViewModel (progress UI state)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Agents.Editor;
using MediatR;

namespace Lexichord.Modules.Agents.Editor.Events;

/// <summary>
/// Published when a rewrite command pipeline begins execution.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="RewriteCommandHandler"/> after license verification
/// passes. Carries the rewrite intent, character count, and document path
/// for observability and UI state management.
/// </para>
/// <para><b>Introduced in:</b> v0.7.3b</para>
/// </remarks>
/// <param name="Intent">The rewrite intent being executed.</param>
/// <param name="CharacterCount">Number of characters in the selected text.</param>
/// <param name="DocumentPath">Path to the document being edited, if available.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record RewriteStartedEvent(
    RewriteIntent Intent,
    int CharacterCount,
    string? DocumentPath,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="RewriteStartedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="intent">The rewrite intent being executed.</param>
    /// <param name="characterCount">Number of characters in the selected text.</param>
    /// <param name="documentPath">Path to the document being edited.</param>
    /// <returns>A new <see cref="RewriteStartedEvent"/>.</returns>
    public static RewriteStartedEvent Create(
        RewriteIntent intent,
        int characterCount,
        string? documentPath) =>
        new(intent, characterCount, documentPath, DateTime.UtcNow);
}
