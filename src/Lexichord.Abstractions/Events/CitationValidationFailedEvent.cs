// =============================================================================
// File: CitationValidationFailedEvent.cs
// Project: Lexichord.Abstractions
// Description: MediatR notification published when citation validation fails.
// =============================================================================
// LOGIC: Published by CitationValidator when a citation is determined to be
//   stale (source modified after indexing) or missing (source file deleted).
//   Enables downstream consumers to:
//   - Update stale indicator UI components (v0.5.2c)
//   - Track validation failures for analytics (v0.5.4d, future)
//   - Log validation failures for Enterprise audit trails (future)
//   NOT published for Valid or Error statuses — only for actionable failures.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using MediatR;

namespace Lexichord.Abstractions.Events;

/// <summary>
/// Published when a citation validation detects a stale or missing source.
/// </summary>
/// <remarks>
/// <para>
/// This event is published by <see cref="ICitationValidator.ValidateAsync"/>
/// when the validation result has a status of <see cref="CitationValidationStatus.Stale"/>
/// or <see cref="CitationValidationStatus.Missing"/>. It is NOT published for
/// <see cref="CitationValidationStatus.Valid"/> or <see cref="CitationValidationStatus.Error"/>
/// results.
/// </para>
/// <para>
/// <b>Consumers:</b>
/// <list type="bullet">
///   <item><description>Stale indicator UI updates (v0.5.2c)</description></item>
///   <item><description>Citation analytics tracking (v0.5.4d, future)</description></item>
///   <item><description>Enterprise audit logging (future)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.5.2c as part of the Citation Engine (Stale Detection).
/// </para>
/// </remarks>
/// <param name="Result">
/// The validation result that triggered this event. Contains the citation,
/// validation status, file modification timestamp, and any error details.
/// <see cref="CitationValidationResult.IsStale"/> or
/// <see cref="CitationValidationResult.IsMissing"/> will be <c>true</c>.
/// </param>
/// <param name="Timestamp">
/// UTC timestamp of when the validation was performed. This reflects the moment
/// the validation completed, not when the event was processed by handlers.
/// </param>
public record CitationValidationFailedEvent(
    CitationValidationResult Result,
    DateTime Timestamp) : INotification;
