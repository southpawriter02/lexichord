// <copyright file="ResonanceAxisDefinition.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Defines a single axis on the Resonance spider chart.
/// </summary>
/// <param name="Name">Display name of the axis.</param>
/// <param name="MetricKey">Key to look up raw value from metrics.</param>
/// <param name="Unit">Unit suffix for display (e.g., "grade", "%").</param>
/// <param name="Description">Tooltip description explaining the metric.</param>
/// <param name="MinValue">Minimum expected raw value (for normalization).</param>
/// <param name="MaxValue">Maximum expected raw value (for normalization).</param>
/// <param name="InvertScale">If true, lower raw values produce higher normalized values.</param>
/// <remarks>
/// <para>LOGIC: Self-contained normalization logic per axis.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public record ResonanceAxisDefinition(
    string Name,
    string MetricKey,
    string? Unit = null,
    string? Description = null,
    double MinValue = 0,
    double MaxValue = 100,
    bool InvertScale = false)
{
    /// <summary>
    /// Normalizes a raw value to the 0-100 scale.
    /// </summary>
    /// <param name="rawValue">The raw metric value.</param>
    /// <returns>Normalized value between 0 and 100.</returns>
    /// <remarks>
    /// LOGIC: Clamps to valid range, then scales linearly.
    /// Inverts if needed (e.g., lower grade level = better accessibility).
    /// </remarks>
    public double Normalize(double rawValue)
    {
        var range = MaxValue - MinValue;
        if (range <= 0)
        {
            return 0;
        }

        // Clamp to valid range
        var clamped = Math.Clamp(rawValue, MinValue, MaxValue);

        // Calculate normalized value (0-100)
        var normalized = (clamped - MinValue) / range * 100;

        // Invert if needed (e.g., lower passive voice % is better)
        return InvertScale ? 100 - normalized : normalized;
    }
}
