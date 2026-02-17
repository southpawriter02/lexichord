// -----------------------------------------------------------------------
// <copyright file="FeedbackRecord.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Tuning.Storage;

/// <summary>
/// Internal record type for SQLite row mapping of feedback data.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Maps directly to the <c>feedback</c> table in the SQLite database.
/// All string properties correspond to TEXT columns; integer properties to INTEGER columns;
/// double properties to REAL columns. Timestamps are stored as ISO 8601 strings.
/// </para>
/// <para>
/// <b>Visibility:</b> Internal to <c>Lexichord.Modules.Agents</c>. Accessible by
/// test projects via <c>InternalsVisibleTo</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
internal record FeedbackRecord
{
    /// <summary>
    /// Primary key — the feedback GUID as string.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// The suggestion ID as string.
    /// </summary>
    public required string SuggestionId { get; init; }

    /// <summary>
    /// The deviation ID as string.
    /// </summary>
    public required string DeviationId { get; init; }

    /// <summary>
    /// The style rule identifier.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// The rule category.
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// The feedback decision as integer (maps to <see cref="Lexichord.Abstractions.Contracts.Agents.FeedbackDecision"/>).
    /// </summary>
    public required int Decision { get; init; }

    /// <summary>
    /// The original text (may be anonymized).
    /// </summary>
    public string? OriginalText { get; init; }

    /// <summary>
    /// The AI-suggested text (may be anonymized).
    /// </summary>
    public string? SuggestedText { get; init; }

    /// <summary>
    /// The final applied text (null for rejections).
    /// </summary>
    public string? FinalText { get; init; }

    /// <summary>
    /// The user's modification text (null unless Modified decision).
    /// </summary>
    public string? UserModification { get; init; }

    /// <summary>
    /// The original suggestion confidence score.
    /// </summary>
    public double OriginalConfidence { get; init; }

    /// <summary>
    /// ISO 8601 timestamp string.
    /// </summary>
    public required string Timestamp { get; init; }

    /// <summary>
    /// Optional user comment.
    /// </summary>
    public string? UserComment { get; init; }

    /// <summary>
    /// Anonymized user identifier.
    /// </summary>
    public string? AnonymizedUserId { get; init; }

    /// <summary>
    /// Whether this was a bulk operation (0 or 1).
    /// </summary>
    public int IsBulkOperation { get; init; }
}
