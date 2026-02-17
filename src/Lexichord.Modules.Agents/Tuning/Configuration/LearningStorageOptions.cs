// -----------------------------------------------------------------------
// <copyright file="LearningStorageOptions.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Modules.Agents.Tuning.Configuration;

/// <summary>
/// Configuration options for the Learning Loop's SQLite storage backend.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> These options control the behavior of the SQLite feedback store:
/// <list type="bullet">
///   <item><description>Database file location (defaults to platform-specific app data directory)</description></item>
///   <item><description>Data retention period for automatic cleanup</description></item>
///   <item><description>Pattern cache size limits</description></item>
///   <item><description>Minimum frequency threshold for pattern extraction</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Configuration:</b> Options can be configured via DI using
/// <c>IOptions&lt;LearningStorageOptions&gt;</c> or programmatically via
/// <see cref="Lexichord.Modules.Agents.Extensions.TuningServiceCollectionExtensions.AddLearningLoop"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.5d as part of the Learning Loop feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// services.AddLearningLoop(options =>
/// {
///     options.DatabasePath = "/custom/path/learning.db";
///     options.RetentionDays = 180;
///     options.MaxPatternCacheSize = 500;
/// });
/// </code>
/// </example>
public class LearningStorageOptions
{
    /// <summary>
    /// Full path to the SQLite database file.
    /// </summary>
    /// <value>
    /// Default: <c>null</c> (uses platform-specific default location).
    /// </value>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> When null, the database is created at the platform-specific default:
    /// <list type="bullet">
    ///   <item><description>Windows: <c>%APPDATA%/Lexichord/Learning/learning.db</c></description></item>
    ///   <item><description>macOS: <c>~/Library/Application Support/Lexichord/Learning/learning.db</c></description></item>
    ///   <item><description>Linux: <c>~/.config/Lexichord/Learning/learning.db</c></description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string? DatabasePath { get; set; }

    /// <summary>
    /// Number of days to retain feedback records.
    /// </summary>
    /// <value>Default: 365 days.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Records older than this are deleted during retention enforcement.
    /// Set to 0 to disable age-based retention (keep all records).
    /// </remarks>
    public int RetentionDays { get; set; } = 365;

    /// <summary>
    /// Maximum number of pattern cache entries per rule.
    /// </summary>
    /// <value>Default: 100 entries per rule.</value>
    /// <remarks>
    /// <b>LOGIC:</b> When the pattern cache exceeds this limit for a rule,
    /// the least frequent patterns are evicted during cache updates.
    /// </remarks>
    public int MaxPatternCacheSize { get; set; } = 100;

    /// <summary>
    /// Minimum frequency for a pattern to be included in learning context.
    /// </summary>
    /// <value>Default: 2 occurrences.</value>
    /// <remarks>
    /// <b>LOGIC:</b> Patterns with fewer occurrences than this threshold are
    /// considered noise and excluded from <see cref="Lexichord.Abstractions.Contracts.Agents.LearningContext"/>.
    /// </remarks>
    public int PatternMinFrequency { get; set; } = 2;
}
