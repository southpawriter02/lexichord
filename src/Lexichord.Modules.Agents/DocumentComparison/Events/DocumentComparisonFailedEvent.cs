// -----------------------------------------------------------------------
// <copyright file="DocumentComparisonFailedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a document comparison fails
//   (v0.7.6d). Published by DocumentComparer when an error occurs during
//   file loading, LLM invocation, or response parsing.
//
//   Consumers:
//     - UI components (error state, error message display)
//     - Analytics/telemetry handlers (error tracking)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.DocumentComparison.Events;

/// <summary>
/// Published when a document comparison fails.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <see cref="DocumentComparer"/> when an error occurs during
/// file loading, LLM invocation, or response parsing. Carries the error message,
/// document paths, and timestamp for error handling and observability.
/// </para>
/// <para>
/// <b>Failure Causes:</b>
/// <list type="bullet">
/// <item><description>File not found (original or new document)</description></item>
/// <item><description>Permission denied when reading files</description></item>
/// <item><description>LLM service unavailable or timeout</description></item>
/// <item><description>Invalid JSON response from LLM</description></item>
/// <item><description>User cancellation</description></item>
/// <item><description>License restriction (feature not enabled)</description></item>
/// <item><description>Git operation failed (for git version comparison)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
/// <item><description>UI components for displaying error state and messages</description></item>
/// <item><description>Analytics/telemetry handlers for error tracking</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d as part of Document Comparison</para>
/// </remarks>
/// <param name="OriginalPath">Path to the original (older) document.</param>
/// <param name="NewPath">Path to the new (current) document.</param>
/// <param name="ErrorMessage">Description of what went wrong.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record DocumentComparisonFailedEvent(
    string? OriginalPath,
    string? NewPath,
    string ErrorMessage,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="DocumentComparisonFailedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="originalPath">Path to the original document, or <c>null</c> for content-based comparison.</param>
    /// <param name="newPath">Path to the new document, or <c>null</c> for content-based comparison.</param>
    /// <param name="errorMessage">Description of what went wrong.</param>
    /// <returns>A new <see cref="DocumentComparisonFailedEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method that sets the timestamp to the current UTC time.
    /// Prefer using this method over the constructor for consistency.
    /// </remarks>
    public static DocumentComparisonFailedEvent Create(
        string? originalPath,
        string? newPath,
        string errorMessage) =>
        new(originalPath, newPath, errorMessage, DateTime.UtcNow);

    /// <summary>
    /// Creates a new <see cref="DocumentComparisonFailedEvent"/> from an exception.
    /// </summary>
    /// <param name="originalPath">Path to the original document, or <c>null</c> for content-based comparison.</param>
    /// <param name="newPath">Path to the new document, or <c>null</c> for content-based comparison.</param>
    /// <param name="exception">The exception that caused the failure.</param>
    /// <returns>A new <see cref="DocumentComparisonFailedEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience factory method that extracts the error message from an exception.
    /// Uses the exception's Message property as the error message.
    /// </remarks>
    public static DocumentComparisonFailedEvent FromException(
        string? originalPath,
        string? newPath,
        Exception exception) =>
        new(originalPath, newPath, exception.Message, DateTime.UtcNow);
}
