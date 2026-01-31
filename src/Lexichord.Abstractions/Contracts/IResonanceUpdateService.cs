// <copyright file="IResonanceUpdateService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Manages the reactive update pipeline for the Resonance Dashboard.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5c - Coordinates chart updates in response to analysis events.</para>
/// <list type="bullet">
///   <item>Debounces rapid analysis events (300ms) to prevent UI jitter</item>
///   <item>Provides immediate dispatch for profile changes</item>
///   <item>Supports manual refresh via <see cref="ForceUpdateAsync"/></item>
/// </list>
/// <para>Lifecycle: Call <see cref="StartListening"/> when dashboard becomes visible,
/// <see cref="StopListening"/> when hidden to conserve resources.</para>
/// </remarks>
/// <example>
/// <code>
/// // In ViewModel constructor
/// _updateSubscription = _updateService.UpdateRequested
///     .Subscribe(args => RefreshChartAsync(args.Trigger));
/// 
/// // When dashboard shown
/// _updateService.StartListening();
/// 
/// // When dashboard hidden
/// _updateService.StopListening();
/// </code>
/// </example>
public interface IResonanceUpdateService : IDisposable
{
    /// <summary>
    /// Gets an observable that emits when the chart should update.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: ViewModel subscribes to this observable for chart refresh.</para>
    /// <para>Events are debounced for analysis triggers, immediate for profile changes.</para>
    /// </remarks>
    IObservable<ChartUpdateEventArgs> UpdateRequested { get; }

    /// <summary>
    /// Gets whether the service is currently listening for events.
    /// </summary>
    bool IsListening { get; }

    /// <summary>
    /// Starts listening for analysis and profile events.
    /// </summary>
    /// <remarks>
    /// LOGIC: Safe to call multiple times - subsequent calls are no-ops.
    /// </remarks>
    void StartListening();

    /// <summary>
    /// Stops listening for events and disposes active subscriptions.
    /// </summary>
    /// <remarks>
    /// LOGIC: Safe to call multiple times - subsequent calls are no-ops.
    /// </remarks>
    void StopListening();

    /// <summary>
    /// Forces an immediate chart update, bypassing debounce.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the async operation.</returns>
    /// <remarks>
    /// LOGIC: For manual refresh button or programmatic refresh needs.
    /// </remarks>
    Task ForceUpdateAsync(CancellationToken ct = default);
}
