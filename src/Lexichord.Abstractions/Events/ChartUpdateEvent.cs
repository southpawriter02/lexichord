// <copyright file="ChartUpdateEvent.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when the Resonance Dashboard chart should update.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5c - MediatR notification enabling loose coupling between
/// the update service and chart consumers.</para>
/// <para>Subscribers can use this to refresh cached data, update UI, or
/// trigger additional analysis.</para>
/// </remarks>
/// <param name="Trigger">The source of the update request.</param>
/// <example>
/// <code>
/// public class ChartRefreshHandler : INotificationHandler&lt;ChartUpdateEvent&gt;
/// {
///     public Task Handle(ChartUpdateEvent notification, CancellationToken ct)
///     {
///         _logger.LogDebug("Chart update triggered by {Trigger}", notification.Trigger);
///         return _chartService.RefreshAsync(ct);
///     }
/// }
/// </code>
/// </example>
public sealed record ChartUpdateEvent(UpdateTrigger Trigger) : INotification;
