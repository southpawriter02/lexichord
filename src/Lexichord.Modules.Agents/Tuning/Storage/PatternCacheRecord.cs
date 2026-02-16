// -----------------------------------------------------------------------
// <copyright file="PatternCacheRecord.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Tuning.Storage;

/// <summary>
/// Internal record type for SQLite row mapping of pattern cache data.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Maps directly to the <c>pattern_cache</c> table in the SQLite database.
/// Pattern cache records are computed aggregations of feedback data, grouping by
/// (RuleId, PatternType, OriginalPattern, SuggestedPattern) and counting occurrences.
/// </para>
/// <para>
/// <b>Composite Primary Key:</b> (RuleId, PatternType, OriginalPattern, SuggestedPattern).
/// This means each unique original→suggested mapping per rule and type is stored once
/// with an accumulated count and success rate.
/// </para>
/// <para>
/// <b>Visibility:</b> Internal to <c>Lexichord.Modules.Agents</c>. Accessible by
/// test projects via <c>InternalsVisibleTo</c>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
internal record PatternCacheRecord
{
    /// <summary>
    /// The style rule identifier.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// The type of pattern: "Accepted" or "Rejected".
    /// </summary>
    public required string PatternType { get; init; }

    /// <summary>
    /// The normalized original text pattern.
    /// </summary>
    public required string OriginalPattern { get; init; }

    /// <summary>
    /// The normalized suggested text pattern.
    /// </summary>
    public required string SuggestedPattern { get; init; }

    /// <summary>
    /// Number of times this pattern was observed.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Success rate for this pattern (accepted+modified / total).
    /// </summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// ISO 8601 timestamp of the last update.
    /// </summary>
    public required string LastUpdated { get; init; }
}
