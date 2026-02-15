// -----------------------------------------------------------------------
// <copyright file="DeviationsDetectedEventArgs.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Contracts.Agents;

/// <summary>
/// Event arguments for the <see cref="IStyleDeviationScanner.DeviationsDetected"/> event.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Published when new deviations are detected, either from a fresh scan
/// or from real-time linting updates. The UI subscribes to this event to update
/// deviation lists without polling.
/// </para>
/// <para>
/// <b>Thread Safety:</b> This class is thread-safe as all properties are read-only
/// after construction.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5a as part of the Style Deviation Scanner feature.
/// </para>
/// </remarks>
public sealed class DeviationsDetectedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the path to the document with new deviations.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Identifies which document triggered this event. UI components
    /// filter events to show only deviations for the active document.
    /// </remarks>
    public required string DocumentPath { get; init; }

    /// <summary>
    /// Gets the newly detected deviations.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> For fresh scans, this contains all deviations found.
    /// For incremental updates (<see cref="IsIncremental"/> = true), this contains
    /// only the new deviations since the last update.
    /// </remarks>
    public required IReadOnlyList<StyleDeviation> NewDeviations { get; init; }

    /// <summary>
    /// Gets the total deviation count including previously detected deviations.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> For UI badge display. May differ from <see cref="NewDeviations"/>.Count
    /// when <see cref="IsIncremental"/> is true.
    /// </remarks>
    public required int TotalDeviationCount { get; init; }

    /// <summary>
    /// Gets whether this is from an incremental update rather than a fresh scan.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When true, <see cref="NewDeviations"/> contains only deviations
    /// detected since the last event. The UI should append these rather than replace
    /// the existing list.
    /// </remarks>
    public bool IsIncremental { get; init; }

    /// <summary>
    /// Gets the count of new deviations in this event.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property equivalent to <see cref="NewDeviations"/>.Count.
    /// </remarks>
    public int NewDeviationCount => NewDeviations.Count;

    /// <summary>
    /// Gets whether this event contains any new deviations.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for conditional UI updates.
    /// </remarks>
    public bool HasNewDeviations => NewDeviations.Count > 0;
}
