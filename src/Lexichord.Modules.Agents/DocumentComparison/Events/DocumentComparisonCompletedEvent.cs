// -----------------------------------------------------------------------
// <copyright file="DocumentComparisonCompletedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a document comparison completes
//   successfully (v0.7.6d). Published by DocumentComparer after the LLM
//   response is parsed and changes are processed.
//
//   Consumers:
//     - UI components (completion state, metrics display)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.DocumentComparison.Events;

/// <summary>
/// Published when a document comparison completes successfully.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <see cref="DocumentComparer"/> after the LLM response is parsed
/// and changes are processed. Carries completion metrics including change count,
/// magnitude, duration, and document paths for observability.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
/// <item><description>UI components for updating completion state and displaying metrics</description></item>
/// <item><description>Analytics/telemetry handlers for tracking comparison performance</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d as part of Document Comparison</para>
/// </remarks>
/// <param name="OriginalPath">Path to the original (older) document.</param>
/// <param name="NewPath">Path to the new (current) document.</param>
/// <param name="ChangeCount">Number of changes detected.</param>
/// <param name="ChangeMagnitude">Overall change magnitude (0.0 to 1.0).</param>
/// <param name="Duration">Total time elapsed for the comparison operation.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record DocumentComparisonCompletedEvent(
    string? OriginalPath,
    string? NewPath,
    int ChangeCount,
    double ChangeMagnitude,
    TimeSpan Duration,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="DocumentComparisonCompletedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="originalPath">Path to the original document, or <c>null</c> for content-based comparison.</param>
    /// <param name="newPath">Path to the new document, or <c>null</c> for content-based comparison.</param>
    /// <param name="changeCount">Number of changes detected.</param>
    /// <param name="changeMagnitude">Overall change magnitude (0.0 to 1.0).</param>
    /// <param name="duration">Total time elapsed for the comparison operation.</param>
    /// <returns>A new <see cref="DocumentComparisonCompletedEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method that sets the timestamp to the current UTC time.
    /// Prefer using this method over the constructor for consistency.
    /// </remarks>
    public static DocumentComparisonCompletedEvent Create(
        string? originalPath,
        string? newPath,
        int changeCount,
        double changeMagnitude,
        TimeSpan duration) =>
        new(originalPath, newPath, changeCount, changeMagnitude, duration, DateTime.UtcNow);

    /// <summary>
    /// Gets whether the compared documents were identical.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="ChangeCount"/> is 0 and <see cref="ChangeMagnitude"/> is 0.0;
    /// otherwise <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for quick identical-check without examining the full result.
    /// </remarks>
    public bool AreIdentical => ChangeCount == 0 && ChangeMagnitude == 0.0;

    /// <summary>
    /// Gets the magnitude as a formatted percentage string.
    /// </summary>
    /// <value>
    /// The change magnitude formatted as a percentage (e.g., "45%").
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for display purposes.
    /// </remarks>
    public string MagnitudePercentage => $"{ChangeMagnitude:P0}";
}
