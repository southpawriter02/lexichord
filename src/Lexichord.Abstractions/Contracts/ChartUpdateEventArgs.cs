// <copyright file="ChartUpdateEventArgs.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Event arguments for chart update notifications via <see cref="IResonanceUpdateService"/>.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5c - Provides context about the update event for UI handling.</para>
/// <para>Includes timing information for diagnostics and performance monitoring.</para>
/// </remarks>
public sealed record ChartUpdateEventArgs
{
    /// <summary>
    /// Gets or sets the source of the update request.
    /// </summary>
    public required UpdateTrigger Trigger { get; init; }

    /// <summary>
    /// Gets or sets whether the update bypassed debouncing.
    /// </summary>
    /// <remarks>
    /// LOGIC: True for <see cref="UpdateTrigger.ProfileChanged"/> and <see cref="UpdateTrigger.ForceUpdate"/>.
    /// </remarks>
    public required bool WasImmediate { get; init; }

    /// <summary>
    /// Gets or sets when the original event was received.
    /// </summary>
    /// <remarks>LOGIC: Used for debounce timing diagnostics.</remarks>
    public required DateTimeOffset EventReceivedAt { get; init; }

    /// <summary>
    /// Gets or sets when the update was dispatched.
    /// </summary>
    /// <remarks>LOGIC: For debounced events, this is after the debounce window closes.</remarks>
    public required DateTimeOffset DispatchedAt { get; init; }
}
