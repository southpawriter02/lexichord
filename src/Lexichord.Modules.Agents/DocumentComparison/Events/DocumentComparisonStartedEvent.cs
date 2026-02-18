// -----------------------------------------------------------------------
// <copyright file="DocumentComparisonStartedEvent.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------
// LOGIC: MediatR notification published when a document comparison begins
//   execution (v0.7.6d). Published by DocumentComparer after option validation
//   passes and before the LLM is invoked for semantic analysis.
//
//   Consumers:
//     - UI components (progress state, loading indicators)
//     - Analytics/telemetry handlers (future)
// -----------------------------------------------------------------------

using MediatR;

namespace Lexichord.Modules.Agents.DocumentComparison.Events;

/// <summary>
/// Published when a document comparison begins execution.
/// </summary>
/// <remarks>
/// <para>
/// Published by the <see cref="DocumentComparer"/> after option validation passes
/// and before the LLM is invoked for semantic analysis. Carries document paths,
/// character counts, and timestamp for observability and UI state management.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
/// <item><description>UI components for showing loading/progress state</description></item>
/// <item><description>Analytics/telemetry handlers for tracking comparison operations</description></item>
/// </list>
/// </para>
/// <para><b>Introduced in:</b> v0.7.6d as part of Document Comparison</para>
/// </remarks>
/// <param name="OriginalPath">Path to the original (older) document.</param>
/// <param name="NewPath">Path to the new (current) document.</param>
/// <param name="OriginalCharacterCount">Number of characters in the original document.</param>
/// <param name="NewCharacterCount">Number of characters in the new document.</param>
/// <param name="Timestamp">UTC timestamp when the event was created.</param>
public record DocumentComparisonStartedEvent(
    string? OriginalPath,
    string? NewPath,
    int OriginalCharacterCount,
    int NewCharacterCount,
    DateTime Timestamp) : INotification
{
    /// <summary>
    /// Creates a new <see cref="DocumentComparisonStartedEvent"/> with the current UTC timestamp.
    /// </summary>
    /// <param name="originalPath">Path to the original document, or <c>null</c> for content-based comparison.</param>
    /// <param name="newPath">Path to the new document, or <c>null</c> for content-based comparison.</param>
    /// <param name="originalCharacterCount">Number of characters in the original document.</param>
    /// <param name="newCharacterCount">Number of characters in the new document.</param>
    /// <returns>A new <see cref="DocumentComparisonStartedEvent"/> instance.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method that sets the timestamp to the current UTC time.
    /// Prefer using this method over the constructor for consistency.
    /// </remarks>
    public static DocumentComparisonStartedEvent Create(
        string? originalPath,
        string? newPath,
        int originalCharacterCount,
        int newCharacterCount) =>
        new(originalPath, newPath, originalCharacterCount, newCharacterCount, DateTime.UtcNow);

    /// <summary>
    /// Gets the total character count across both documents.
    /// </summary>
    /// <value>
    /// The sum of <see cref="OriginalCharacterCount"/> and <see cref="NewCharacterCount"/>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Useful for estimating comparison complexity and LLM token usage.
    /// </remarks>
    public int TotalCharacterCount => OriginalCharacterCount + NewCharacterCount;
}
