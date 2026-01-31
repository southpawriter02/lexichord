// <copyright file="ResonanceChartData.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Complete data for rendering a Resonance spider chart.
/// </summary>
/// <param name="DataPoints">Ordered list of axis values (clockwise from top).</param>
/// <param name="ComputedAt">Timestamp when data was computed.</param>
/// <param name="SourceDocumentId">ID of the document this data is for.</param>
/// <remarks>
/// <para>LOGIC: Immutable record for thread-safe chart data transfer.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public record ResonanceChartData(
    IReadOnlyList<ResonanceDataPoint> DataPoints,
    DateTimeOffset ComputedAt,
    Guid? SourceDocumentId = null)
{
    /// <summary>
    /// Gets empty chart data with zero values.
    /// </summary>
    /// <remarks>
    /// LOGIC: Singleton instance for uninitialized or error states.
    /// </remarks>
    public static ResonanceChartData Empty => new(
        DataPoints: Array.Empty<ResonanceDataPoint>(),
        ComputedAt: DateTimeOffset.MinValue);

    /// <summary>
    /// Gets all normalized values as an array (for chart series binding).
    /// </summary>
    /// <returns>Array of normalized values in axis order.</returns>
    public double[] GetNormalizedValues() =>
        DataPoints.Select(p => p.NormalizedValue).ToArray();

    /// <summary>
    /// Gets all axis names as an array (for chart labels).
    /// </summary>
    /// <returns>Array of axis names in order.</returns>
    public string[] GetAxisNames() =>
        DataPoints.Select(p => p.AxisName).ToArray();
}

/// <summary>
/// A single data point on the spider chart.
/// </summary>
/// <param name="AxisName">Display name of the axis.</param>
/// <param name="NormalizedValue">Value normalized to 0-100 scale.</param>
/// <param name="RawValue">Original metric value before normalization.</param>
/// <param name="Unit">Unit of the raw value (e.g., "grade", "%").</param>
/// <param name="Description">Explanation shown in tooltip.</param>
/// <remarks>
/// <para>LOGIC: Encapsulates both raw and normalized values for display flexibility.</para>
/// <para>Introduced in v0.3.5a.</para>
/// </remarks>
public record ResonanceDataPoint(
    string AxisName,
    double NormalizedValue,
    double RawValue,
    string? Unit = null,
    string? Description = null)
{
    /// <summary>
    /// Gets formatted string for tooltip display.
    /// </summary>
    /// <remarks>
    /// LOGIC: Includes unit suffix when available for context.
    /// </remarks>
    public string TooltipText => Unit is not null
        ? $"{AxisName}: {RawValue:0.#} {Unit}"
        : $"{AxisName}: {RawValue:0.#}";
}
