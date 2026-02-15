// -----------------------------------------------------------------------
// <copyright file="ResimplificationRequestedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Abstractions.Agents.Simplifier.Events;

/// <summary>
/// Published when the user requests re-simplification with different settings.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This event is published by the <c>SimplificationPreviewViewModel</c>
/// when the user changes the audience preset or strategy and clicks "Re-simplify".
/// This allows the preview to be updated with new simplification results without
/// closing and reopening the UI.
/// </para>
/// <para>
/// <b>Use Cases:</b>
/// </para>
/// <list type="bullet">
///   <item><description>User finds the initial simplification too aggressive and wants a more conservative approach</description></item>
///   <item><description>User wants to target a different audience (e.g., switching from "General Public" to "Technical")</description></item>
///   <item><description>User wants to regenerate with different prompt settings</description></item>
/// </list>
/// <para>
/// <b>Introduced in:</b> v0.7.4c as part of the Simplifier Agent Preview/Diff UI.
/// </para>
/// </remarks>
/// <param name="DocumentPath">
/// The file path of the document being simplified.
/// May be <c>null</c> for untitled documents.
/// </param>
/// <param name="OriginalText">
/// The original text to simplify. This is the same text that was used in the
/// previous simplification attempt.
/// </param>
/// <param name="NewPresetId">
/// The ID of the new audience preset to use. If <c>null</c>, the previous
/// preset is retained (useful when only changing strategy).
/// </param>
/// <param name="NewStrategy">
/// The new simplification strategy to use. If <c>null</c>, the previous
/// strategy is retained (useful when only changing preset).
/// </param>
/// <example>
/// <code>
/// // Publishing when user changes preset
/// await _mediator.Publish(new ResimplificationRequestedEvent(
///     DocumentPath: "/path/to/document.md",
///     OriginalText: "The complex original text...",
///     NewPresetId: "technical",
///     NewStrategy: null));
///
/// // Publishing when user changes strategy
/// await _mediator.Publish(new ResimplificationRequestedEvent(
///     DocumentPath: "/path/to/document.md",
///     OriginalText: "The complex original text...",
///     NewPresetId: null,
///     NewStrategy: SimplificationStrategy.Conservative));
///
/// // Handling the event to trigger new simplification
/// public class ResimplificationHandler : INotificationHandler&lt;ResimplificationRequestedEvent&gt;
/// {
///     public async Task Handle(ResimplificationRequestedEvent notification, CancellationToken ct)
///     {
///         var target = notification.NewPresetId is not null
///             ? await _targetService.GetTargetAsync(presetId: notification.NewPresetId, ct: ct)
///             : await _targetService.GetTargetAsync(ct: ct);
///
///         var request = new SimplificationRequest
///         {
///             OriginalText = notification.OriginalText,
///             Target = target,
///             Strategy = notification.NewStrategy ?? SimplificationStrategy.Balanced
///         };
///
///         var result = await _pipeline.SimplifyAsync(request, ct);
///         // Update preview with new result...
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="SimplificationAcceptedEvent"/>
/// <seealso cref="SimplificationRejectedEvent"/>
public record ResimplificationRequestedEvent(
    string? DocumentPath,
    string OriginalText,
    string? NewPresetId,
    SimplificationStrategy? NewStrategy = null) : INotification
{
    /// <summary>
    /// Gets a value indicating whether the preset is being changed.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="NewPresetId"/> is not <c>null</c>; otherwise, <c>false</c>.
    /// </value>
    public bool IsPresetChange => NewPresetId is not null;

    /// <summary>
    /// Gets a value indicating whether the strategy is being changed.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="NewStrategy"/> is not <c>null</c>; otherwise, <c>false</c>.
    /// </value>
    public bool IsStrategyChange => NewStrategy is not null;
}
