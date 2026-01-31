// <copyright file="TargetOverlay.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Target overlay data derived from a voice profile for chart rendering.
/// </summary>
/// <param name="ProfileId">The voice profile ID this overlay is based on.</param>
/// <param name="ProfileName">Display name of the voice profile.</param>
/// <param name="DataPoints">Target data points matching axis order.</param>
/// <param name="ComputedAt">Timestamp when the overlay was computed.</param>
/// <remarks>
/// <para>LOGIC: Immutable record for thread-safe overlay data transfer.</para>
/// <para>Data points are pre-normalized to 0-100 scale.</para>
/// <para>Introduced in v0.3.5b.</para>
/// </remarks>
public record TargetOverlay(
    string ProfileId,
    string ProfileName,
    IReadOnlyList<ResonanceDataPoint> DataPoints,
    DateTimeOffset ComputedAt)
{
    /// <summary>
    /// Gets empty overlay indicating no target data.
    /// </summary>
    /// <remarks>
    /// LOGIC: Singleton instance for profiles with no defined targets.
    /// </remarks>
    public static TargetOverlay Empty => new(
        ProfileId: string.Empty,
        ProfileName: string.Empty,
        DataPoints: Array.Empty<ResonanceDataPoint>(),
        ComputedAt: DateTimeOffset.MinValue);

    /// <summary>
    /// Gets whether this overlay has any target data.
    /// </summary>
    public bool HasData => DataPoints.Count > 0;

    /// <summary>
    /// Gets all normalized values as an array (for chart series binding).
    /// </summary>
    /// <returns>Array of normalized target values in axis order.</returns>
    public double[] GetNormalizedValues() =>
        DataPoints.Select(p => p.NormalizedValue).ToArray();
}
