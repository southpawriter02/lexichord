namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// Complete style configuration settings that can be loaded from
/// system defaults, user preferences, or project-level files.
/// </summary>
/// <remarks>
/// LOGIC: This record represents the full set of configurable style settings.
/// It is designed to be merged hierarchically from multiple sources:
///
/// 1. System defaults (embedded in application)
/// 2. User settings (from user profile/appsettings)
/// 3. Project settings (from .lexichord/style.yaml)
///
/// Merge Strategy:
/// - Scalar values: Higher source overrides lower source
/// - Nullable scalars: Non-null higher values override
/// - Lists (Additions, Exclusions, IgnoredRules): Concatenate and deduplicate
///
/// Thread Safety:
/// - Immutable record ensures thread safety
/// - Use with statements for modifications
///
/// Version: v0.3.6a
/// </remarks>
public record StyleConfiguration
{
    /// <summary>
    /// Configuration file version for compatibility checking.
    /// </summary>
    /// <remarks>
    /// LOGIC: Version is used to detect incompatible configuration formats.
    /// Current version is 1. Future versions may require migration.
    /// </remarks>
    public int Version { get; init; } = 1;

    // ═══════════════════════════════════════════════════════════════════════════
    // Profile Settings
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Default voice profile name to use for analysis.
    /// </summary>
    /// <remarks>
    /// LOGIC: Determines which built-in profile (Technical, Marketing, etc.)
    /// is used when no profile is explicitly selected.
    /// </remarks>
    public string? DefaultProfile { get; init; }

    /// <summary>
    /// Whether users can switch profiles within this context.
    /// </summary>
    /// <remarks>
    /// LOGIC: When false, the profile selector is disabled.
    /// Used for enforcing consistent style in team projects.
    /// </remarks>
    public bool AllowProfileSwitching { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════════════════
    // Readability Constraints
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Target Flesch-Kincaid grade level for content.
    /// </summary>
    /// <remarks>
    /// LOGIC: Content exceeding this grade level will trigger readability warnings.
    /// Null means no grade level enforcement.
    /// </remarks>
    public double? TargetGradeLevel { get; init; }

    /// <summary>
    /// Maximum words per sentence before warning.
    /// </summary>
    /// <remarks>
    /// LOGIC: Sentences exceeding this length trigger a warning.
    /// Null means no sentence length enforcement.
    /// </remarks>
    public int? MaxSentenceLength { get; init; }

    /// <summary>
    /// Grade level tolerance (±) for readability checks.
    /// </summary>
    /// <remarks>
    /// LOGIC: Content is acceptable if within TargetGradeLevel ± GradeLevelTolerance.
    /// Default of 2 means ±2 grade levels are acceptable.
    /// </remarks>
    public double GradeLevelTolerance { get; init; } = 2;

    // ═══════════════════════════════════════════════════════════════════════════
    // Voice Analysis Settings
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Maximum passive voice percentage before warning.
    /// </summary>
    /// <remarks>
    /// LOGIC: Content exceeding this percentage of passive voice constructions
    /// will trigger a voice analysis warning. Range: 0-100.
    /// </remarks>
    public double PassiveVoiceThreshold { get; init; } = 20;

    /// <summary>
    /// Whether to flag adverbs in analysis.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true, adverbs (words ending in -ly) are flagged
    /// as potential style improvements.
    /// </remarks>
    public bool FlagAdverbs { get; init; } = true;

    /// <summary>
    /// Whether to flag weasel words in analysis.
    /// </summary>
    /// <remarks>
    /// LOGIC: When true, vague or non-committal words (very, really, etc.)
    /// are flagged as potential style improvements.
    /// </remarks>
    public bool FlagWeaselWords { get; init; } = true;

    // ═══════════════════════════════════════════════════════════════════════════
    // Term Overrides
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Additional terms to flag in this context.
    /// </summary>
    /// <remarks>
    /// LOGIC: Project-specific terminology additions that supplement
    /// the global terminology database. These are merged (concatenated)
    /// across configuration layers.
    /// </remarks>
    public IReadOnlyList<TermAddition> TerminologyAdditions { get; init; } =
        Array.Empty<TermAddition>();

    /// <summary>
    /// Terms to exclude from flagging in this context.
    /// </summary>
    /// <remarks>
    /// LOGIC: Allows specific terms that would normally be flagged
    /// to be permitted in this project. Merged across layers.
    /// Example: "whitelist" might be excluded for legacy codebases.
    /// </remarks>
    public IReadOnlyList<string> TerminologyExclusions { get; init; } =
        Array.Empty<string>();

    /// <summary>
    /// Rule IDs to ignore in this context.
    /// </summary>
    /// <remarks>
    /// LOGIC: Specific rule identifiers (e.g., "TERM-whitelist")
    /// that should not produce violations. Merged across layers.
    /// </remarks>
    public IReadOnlyList<string> IgnoredRules { get; init; } =
        Array.Empty<string>();

    // ═══════════════════════════════════════════════════════════════════════════
    // Static Defaults
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Gets the system default configuration.
    /// </summary>
    /// <remarks>
    /// LOGIC: These defaults are used when no user or project configuration
    /// is available. They provide sensible out-of-box behavior.
    /// </remarks>
    public static StyleConfiguration Defaults => new()
    {
        Version = 1,
        DefaultProfile = "Technical",
        AllowProfileSwitching = true,
        TargetGradeLevel = 10,
        MaxSentenceLength = 25,
        GradeLevelTolerance = 2,
        PassiveVoiceThreshold = 20,
        FlagAdverbs = true,
        FlagWeaselWords = true
    };
}
