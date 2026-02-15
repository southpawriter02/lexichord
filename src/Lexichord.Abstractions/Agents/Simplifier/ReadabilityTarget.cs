// -----------------------------------------------------------------------
// <copyright file="ReadabilityTarget.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the calculated readability target for a simplification operation.
/// Contains all parameters needed by the Simplifier Agent to calibrate text transformation.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> A ReadabilityTarget is the resolved output of the
/// <see cref="IReadabilityTargetService.GetTargetAsync"/> method. It combines
/// parameters from Voice Profile settings, audience presets, or explicit overrides
/// into a single target configuration used by the Simplifier Agent.
/// </para>
/// <para>
/// <b>Target Resolution:</b>
/// The target is resolved from one of three sources, in priority order:
/// <list type="number">
///   <item><description><b>Explicit Override:</b> Caller specifies exact parameters</description></item>
///   <item><description><b>Audience Preset:</b> Pre-configured audience-specific settings</description></item>
///   <item><description><b>Voice Profile:</b> Project-level settings from active voice profile</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Source Tracking:</b>
/// The <see cref="Source"/> property indicates which source was used to resolve the target,
/// useful for logging and debugging.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
/// <param name="TargetGradeLevel">Target Flesch-Kincaid grade level for the simplified text.</param>
/// <param name="GradeLevelTolerance">Acceptable variance above/below target (e.g., 2.0 means target ± 2 grades).</param>
/// <param name="MaxSentenceLength">Maximum recommended sentence length in words.</param>
/// <param name="AvoidJargon">If true, replace jargon with plain language; if false, explain jargon in context.</param>
/// <param name="Source">Indicates where the target parameters were resolved from.</param>
/// <param name="SourcePresetId">When <see cref="Source"/> is <see cref="ReadabilityTargetSource.Preset"/>, contains the preset Id.</param>
/// <example>
/// <code>
/// // Getting a target from the service
/// var target = await targetService.GetTargetAsync(presetId: "general-public");
///
/// // Using target properties
/// Console.WriteLine($"Target grade level: {target.TargetGradeLevel}");
/// Console.WriteLine($"Acceptable range: {target.MinAcceptableGrade} to {target.MaxAcceptableGrade}");
///
/// // Checking the source
/// switch (target.Source)
/// {
///     case ReadabilityTargetSource.VoiceProfile:
///         Console.WriteLine("Using voice profile settings");
///         break;
///     case ReadabilityTargetSource.Preset:
///         Console.WriteLine($"Using preset: {target.SourcePresetId}");
///         break;
///     case ReadabilityTargetSource.Explicit:
///         Console.WriteLine("Using explicit override");
///         break;
/// }
/// </code>
/// </example>
/// <seealso cref="IReadabilityTargetService"/>
/// <seealso cref="AudiencePreset"/>
/// <seealso cref="ReadabilityTargetSource"/>
public record ReadabilityTarget(
    double TargetGradeLevel,
    double GradeLevelTolerance,
    int MaxSentenceLength,
    bool AvoidJargon,
    ReadabilityTargetSource Source,
    string? SourcePresetId = null)
{
    /// <summary>
    /// Gets the minimum acceptable grade level based on target and tolerance.
    /// </summary>
    /// <value>
    /// The target grade level minus the tolerance, with a minimum of 1.0.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Clamped to 1.0 as the minimum valid Flesch-Kincaid grade level.
    /// Used by <see cref="IReadabilityTargetService.ValidateTarget"/> to determine
    /// if a simplification result is within acceptable range.
    /// </remarks>
    public double MinAcceptableGrade => Math.Max(1.0, TargetGradeLevel - GradeLevelTolerance);

    /// <summary>
    /// Gets the maximum acceptable grade level based on target and tolerance.
    /// </summary>
    /// <value>
    /// The target grade level plus the tolerance, with a maximum of 20.0.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Clamped to 20.0 as a reasonable maximum grade level.
    /// Used by <see cref="IReadabilityTargetService.ValidateTarget"/> to determine
    /// if a simplification result is within acceptable range.
    /// </remarks>
    public double MaxAcceptableGrade => Math.Min(20.0, TargetGradeLevel + GradeLevelTolerance);

    /// <summary>
    /// Determines whether the specified grade level is within the acceptable range.
    /// </summary>
    /// <param name="gradeLevel">The grade level to check.</param>
    /// <returns>
    /// <c>true</c> if <paramref name="gradeLevel"/> is between <see cref="MinAcceptableGrade"/>
    /// and <see cref="MaxAcceptableGrade"/> (inclusive); otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience method for checking if a simplification result
    /// achieved the target readability. Equivalent to checking:
    /// <c>gradeLevel >= MinAcceptableGrade &amp;&amp; gradeLevel &lt;= MaxAcceptableGrade</c>
    /// </remarks>
    /// <example>
    /// <code>
    /// var target = await targetService.GetTargetAsync(presetId: "general-public");
    /// var metrics = readabilityService.Analyze(simplifiedText);
    ///
    /// if (target.IsGradeLevelAcceptable(metrics.FleschKincaidGradeLevel))
    /// {
    ///     Console.WriteLine("Simplification achieved target readability!");
    /// }
    /// </code>
    /// </example>
    public bool IsGradeLevelAcceptable(double gradeLevel)
    {
        return gradeLevel >= MinAcceptableGrade && gradeLevel <= MaxAcceptableGrade;
    }

    /// <summary>
    /// Creates a ReadabilityTarget from an <see cref="AudiencePreset"/>.
    /// </summary>
    /// <param name="preset">The audience preset to create a target from.</param>
    /// <param name="tolerance">Optional tolerance override; defaults to 2.0 if not specified.</param>
    /// <returns>A new <see cref="ReadabilityTarget"/> with parameters from the preset.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preset"/> is null.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method used by <see cref="IReadabilityTargetService"/> when
    /// resolving a target from a preset. The <see cref="Source"/> is set to
    /// <see cref="ReadabilityTargetSource.Preset"/> and <see cref="SourcePresetId"/>
    /// is populated with the preset's Id.
    /// </remarks>
    public static ReadabilityTarget FromPreset(AudiencePreset preset, double tolerance = 2.0)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return new ReadabilityTarget(
            TargetGradeLevel: preset.TargetGradeLevel,
            GradeLevelTolerance: tolerance,
            MaxSentenceLength: preset.MaxSentenceLength,
            AvoidJargon: preset.AvoidJargon,
            Source: ReadabilityTargetSource.Preset,
            SourcePresetId: preset.Id);
    }

    /// <summary>
    /// Creates a ReadabilityTarget from explicit parameters.
    /// </summary>
    /// <param name="targetGradeLevel">Target Flesch-Kincaid grade level.</param>
    /// <param name="maxSentenceLength">Maximum sentence length in words.</param>
    /// <param name="avoidJargon">Whether to avoid jargon.</param>
    /// <param name="tolerance">Grade level tolerance (defaults to 2.0).</param>
    /// <returns>A new <see cref="ReadabilityTarget"/> with explicit parameters.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method used when the caller provides explicit parameters
    /// rather than using a preset or voice profile. The <see cref="Source"/> is set to
    /// <see cref="ReadabilityTargetSource.Explicit"/>.
    /// </remarks>
    public static ReadabilityTarget FromExplicit(
        double targetGradeLevel,
        int maxSentenceLength,
        bool avoidJargon,
        double tolerance = 2.0)
    {
        return new ReadabilityTarget(
            TargetGradeLevel: targetGradeLevel,
            GradeLevelTolerance: tolerance,
            MaxSentenceLength: maxSentenceLength,
            AvoidJargon: avoidJargon,
            Source: ReadabilityTargetSource.Explicit,
            SourcePresetId: null);
    }
}

/// <summary>
/// Indicates the source from which a <see cref="ReadabilityTarget"/> was resolved.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Used for logging, debugging, and UI display to show users
/// where their readability settings came from.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
public enum ReadabilityTargetSource
{
    /// <summary>
    /// Target was resolved from the active Voice Profile settings.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When no preset is specified and the active Voice Profile has
    /// <c>TargetGradeLevel</c> configured, those settings are used.
    /// </remarks>
    VoiceProfile,

    /// <summary>
    /// Target was resolved from an audience preset (built-in or custom).
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When a preset Id is specified to
    /// <see cref="IReadabilityTargetService.GetTargetAsync"/>, the preset's settings are used.
    /// </remarks>
    Preset,

    /// <summary>
    /// Target was specified with explicit parameters by the caller.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> When explicit grade level and other parameters are passed to
    /// <see cref="IReadabilityTargetService.GetTargetAsync"/>, they are used directly.
    /// </remarks>
    Explicit,

    /// <summary>
    /// Target used default values because no Voice Profile or preset was available.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> Fallback source when no Voice Profile is active and no preset is specified.
    /// Uses reasonable defaults (Grade 8, 20 word sentences, avoid jargon).
    /// </remarks>
    Default
}
