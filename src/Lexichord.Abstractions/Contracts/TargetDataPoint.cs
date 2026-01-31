// <copyright file="TargetDataPoint.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A single target point on the overlay with optional tolerance band.
/// </summary>
/// <param name="AxisName">Name of the axis this target applies to.</param>
/// <param name="NormalizedValue">Target value normalized to 0-100.</param>
/// <param name="RawValue">The raw constraint value from the profile.</param>
/// <param name="ToleranceMin">Minimum acceptable normalized value (optional).</param>
/// <param name="ToleranceMax">Maximum acceptable normalized value (optional).</param>
/// <param name="Description">Explanation of this target.</param>
/// <remarks>
/// <para>LOGIC: v0.3.5d - Extends overlay data with tolerance band support.</para>
/// <para>Tolerance bands define acceptable ranges around the target value.</para>
/// <para>When both ToleranceMin and ToleranceMax are set, a tolerance band
/// can be rendered on the chart as a shaded region.</para>
/// </remarks>
public record TargetDataPoint(
    string AxisName,
    double NormalizedValue,
    double RawValue,
    double? ToleranceMin = null,
    double? ToleranceMax = null,
    string? Description = null)
{
    /// <summary>
    /// Gets whether this axis has a tolerance band defined.
    /// </summary>
    /// <remarks>
    /// LOGIC: Both min and max must be present for a valid tolerance band.
    /// </remarks>
    public bool HasToleranceBand => ToleranceMin.HasValue && ToleranceMax.HasValue;

    /// <summary>
    /// Gets the unit label for display (e.g., "%", "words", "grade").
    /// </summary>
    public string? Unit { get; init; }
}
