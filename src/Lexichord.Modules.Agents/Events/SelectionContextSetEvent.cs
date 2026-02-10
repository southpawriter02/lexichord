// -----------------------------------------------------------------------
// <copyright file="SelectionContextSetEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Events;

/// <summary>
/// Published when selection context is set in Co-pilot.
/// </summary>
/// <remarks>
/// <para>
/// This MediatR notification is published by <see cref="Commands.SelectionContextCommand"/>
/// after the selection has been successfully sent to the Co-pilot ViewModel.
/// It can be consumed by telemetry handlers or other services that need to
/// react to selection context changes.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.6.7a as part of the Selection Context feature.
/// </para>
/// </remarks>
/// <param name="SelectionLength">Number of characters in the selection.</param>
/// <param name="Timestamp">When the selection was sent to Co-pilot.</param>
public record SelectionContextSetEvent(
    int SelectionLength,
    DateTime Timestamp
) : INotification;
