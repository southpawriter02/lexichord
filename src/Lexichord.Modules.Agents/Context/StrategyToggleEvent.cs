// -----------------------------------------------------------------------
// <copyright file="StrategyToggleEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.Context;

/// <summary>
/// MediatR notification published when a context strategy is enabled or disabled.
/// Enables UI synchronization of strategy toggle switches in the Context Preview panel.
/// </summary>
/// <remarks>
/// <para>
/// Published by <see cref="ContextOrchestrator"/> when
/// <see cref="IContextOrchestrator.SetStrategyEnabled"/> is called.
/// Handlers may include:
/// </para>
/// <list type="bullet">
///   <item><description>Context Preview panel ViewModel (v0.7.2d) — updates toggle switch state</description></item>
///   <item><description>Settings persistence — records user preferences</description></item>
/// </list>
/// <para>
/// <strong>Introduced in:</strong> v0.7.2c as part of the Context Orchestrator.
/// </para>
/// </remarks>
/// <param name="StrategyId">
/// The <see cref="Lexichord.Abstractions.Agents.Context.IContextStrategy.StrategyId"/>
/// of the strategy that was toggled.
/// </param>
/// <param name="IsEnabled">
/// <c>true</c> if the strategy was enabled; <c>false</c> if it was disabled.
/// </param>
/// <example>
/// <code>
/// // Handling in UI ViewModel
/// public class ContextPreviewHandler : INotificationHandler&lt;StrategyToggleEvent&gt;
/// {
///     public Task Handle(StrategyToggleEvent notification, CancellationToken ct)
///     {
///         var fragment = Fragments.FirstOrDefault(
///             f =&gt; f.StrategyId == notification.StrategyId);
///         if (fragment is not null)
///         {
///             fragment.IsEnabled = notification.IsEnabled;
///         }
///         return Task.CompletedTask;
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="IContextOrchestrator.SetStrategyEnabled"/>
public sealed record StrategyToggleEvent(
    string StrategyId,
    bool IsEnabled) : INotification;
