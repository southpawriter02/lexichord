// -----------------------------------------------------------------------
// <copyright file="TargetValidationResult.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents the result of validating whether a readability target is achievable
/// given the source text's current readability metrics.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> This record is the output of
/// <see cref="IReadabilityTargetService.ValidateTarget"/>. It analyzes whether
/// the source text can realistically be simplified to meet the target grade level
/// and provides specific warnings or blockers if issues are detected.
/// </para>
/// <para>
/// <b>Validation Checks:</b>
/// <list type="bullet">
///   <item><description><b>Already Simple:</b> Source is already at or below target grade level</description></item>
///   <item><description><b>Extreme Simplification:</b> Target requires >4 grade level reduction</description></item>
///   <item><description><b>Complex Vocabulary:</b> High percentage of complex words may limit simplification</description></item>
///   <item><description><b>Long Sentences:</b> Many sentences exceed target length</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Achievability Levels:</b>
/// <list type="bullet">
///   <item><description><see cref="TargetAchievability.Achievable"/>: Target can be reached with standard simplification</description></item>
///   <item><description><see cref="TargetAchievability.Challenging"/>: Target may require significant rewriting</description></item>
///   <item><description><see cref="TargetAchievability.Unlikely"/>: Target is unlikely to be achieved without major restructuring</description></item>
///   <item><description><see cref="TargetAchievability.AlreadyMet"/>: Source already meets or exceeds target</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
/// <param name="Achievability">Overall assessment of whether the target is achievable.</param>
/// <param name="SourceGradeLevel">The current Flesch-Kincaid grade level of the source text.</param>
/// <param name="TargetGradeLevel">The target Flesch-Kincaid grade level.</param>
/// <param name="GradeLevelDelta">The difference between source and target (positive = needs simplification).</param>
/// <param name="Warnings">List of warnings about potential simplification challenges.</param>
/// <param name="SuggestedPreset">If the target seems unrealistic, suggests a more appropriate preset.</param>
/// <example>
/// <code>
/// // Validate target before starting simplification
/// var validation = await targetService.ValidateTarget(sourceText, target);
///
/// switch (validation.Achievability)
/// {
///     case TargetAchievability.AlreadyMet:
///         Console.WriteLine("Text already meets readability target!");
///         break;
///
///     case TargetAchievability.Achievable:
///         Console.WriteLine("Proceeding with simplification...");
///         break;
///
///     case TargetAchievability.Challenging:
///         Console.WriteLine("Warning: This will require significant changes.");
///         foreach (var warning in validation.Warnings)
///         {
///             Console.WriteLine($"  - {warning}");
///         }
///         break;
///
///     case TargetAchievability.Unlikely:
///         if (validation.SuggestedPreset != null)
///         {
///             Console.WriteLine($"Consider using preset: {validation.SuggestedPreset}");
///         }
///         break;
/// }
/// </code>
/// </example>
/// <seealso cref="IReadabilityTargetService"/>
/// <seealso cref="ReadabilityTarget"/>
/// <seealso cref="TargetAchievability"/>
public record TargetValidationResult(
    TargetAchievability Achievability,
    double SourceGradeLevel,
    double TargetGradeLevel,
    double GradeLevelDelta,
    IReadOnlyList<string> Warnings,
    string? SuggestedPreset = null)
{
    /// <summary>
    /// Gets a value indicating whether the target is achievable (either easily or with challenges).
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Achievability"/> is <see cref="TargetAchievability.Achievable"/>,
    /// <see cref="TargetAchievability.Challenging"/>, or <see cref="TargetAchievability.AlreadyMet"/>;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Convenience property for checking if simplification should proceed.
    /// Even "Challenging" targets are considered achievable, just with warnings.
    /// </remarks>
    public bool IsAchievable =>
        Achievability == TargetAchievability.Achievable ||
        Achievability == TargetAchievability.Challenging ||
        Achievability == TargetAchievability.AlreadyMet;

    /// <summary>
    /// Gets a value indicating whether the source text already meets the target.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Achievability"/> is <see cref="TargetAchievability.AlreadyMet"/>;
    /// otherwise, <c>false</c>.
    /// </value>
    /// <remarks>
    /// <b>LOGIC:</b> Used to skip simplification when the text is already simple enough.
    /// </remarks>
    public bool IsAlreadyMet => Achievability == TargetAchievability.AlreadyMet;

    /// <summary>
    /// Gets a value indicating whether there are any warnings to display.
    /// </summary>
    /// <value>
    /// <c>true</c> if <see cref="Warnings"/> contains one or more items; otherwise, <c>false</c>.
    /// </value>
    public bool HasWarnings => Warnings.Count > 0;

    /// <summary>
    /// Creates a validation result indicating the target is already met.
    /// </summary>
    /// <param name="sourceGradeLevel">The source text's current grade level.</param>
    /// <param name="targetGradeLevel">The target grade level.</param>
    /// <returns>A new <see cref="TargetValidationResult"/> with <see cref="TargetAchievability.AlreadyMet"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method used when source grade level is at or below the target.
    /// </remarks>
    public static TargetValidationResult AlreadyMet(double sourceGradeLevel, double targetGradeLevel)
    {
        return new TargetValidationResult(
            Achievability: TargetAchievability.AlreadyMet,
            SourceGradeLevel: sourceGradeLevel,
            TargetGradeLevel: targetGradeLevel,
            GradeLevelDelta: sourceGradeLevel - targetGradeLevel,
            Warnings: Array.Empty<string>());
    }

    /// <summary>
    /// Creates a validation result indicating the target is achievable.
    /// </summary>
    /// <param name="sourceGradeLevel">The source text's current grade level.</param>
    /// <param name="targetGradeLevel">The target grade level.</param>
    /// <param name="warnings">Optional warnings about the simplification.</param>
    /// <returns>A new <see cref="TargetValidationResult"/> with <see cref="TargetAchievability.Achievable"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method used when simplification is feasible with standard approaches.
    /// </remarks>
    public static TargetValidationResult Achievable(
        double sourceGradeLevel,
        double targetGradeLevel,
        IReadOnlyList<string>? warnings = null)
    {
        return new TargetValidationResult(
            Achievability: TargetAchievability.Achievable,
            SourceGradeLevel: sourceGradeLevel,
            TargetGradeLevel: targetGradeLevel,
            GradeLevelDelta: sourceGradeLevel - targetGradeLevel,
            Warnings: warnings ?? Array.Empty<string>());
    }

    /// <summary>
    /// Creates a validation result indicating the target is challenging but possible.
    /// </summary>
    /// <param name="sourceGradeLevel">The source text's current grade level.</param>
    /// <param name="targetGradeLevel">The target grade level.</param>
    /// <param name="warnings">Warnings about simplification challenges.</param>
    /// <returns>A new <see cref="TargetValidationResult"/> with <see cref="TargetAchievability.Challenging"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method used when significant rewriting will be required.
    /// </remarks>
    public static TargetValidationResult Challenging(
        double sourceGradeLevel,
        double targetGradeLevel,
        IReadOnlyList<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        return new TargetValidationResult(
            Achievability: TargetAchievability.Challenging,
            SourceGradeLevel: sourceGradeLevel,
            TargetGradeLevel: targetGradeLevel,
            GradeLevelDelta: sourceGradeLevel - targetGradeLevel,
            Warnings: warnings);
    }

    /// <summary>
    /// Creates a validation result indicating the target is unlikely to be achieved.
    /// </summary>
    /// <param name="sourceGradeLevel">The source text's current grade level.</param>
    /// <param name="targetGradeLevel">The target grade level.</param>
    /// <param name="warnings">Warnings explaining why the target is unlikely.</param>
    /// <param name="suggestedPreset">A more realistic preset to suggest.</param>
    /// <returns>A new <see cref="TargetValidationResult"/> with <see cref="TargetAchievability.Unlikely"/>.</returns>
    /// <remarks>
    /// <b>LOGIC:</b> Factory method used when the target would require major restructuring
    /// that's unlikely to succeed. Includes a suggested alternative preset when appropriate.
    /// </remarks>
    public static TargetValidationResult Unlikely(
        double sourceGradeLevel,
        double targetGradeLevel,
        IReadOnlyList<string> warnings,
        string? suggestedPreset = null)
    {
        ArgumentNullException.ThrowIfNull(warnings);

        return new TargetValidationResult(
            Achievability: TargetAchievability.Unlikely,
            SourceGradeLevel: sourceGradeLevel,
            TargetGradeLevel: targetGradeLevel,
            GradeLevelDelta: sourceGradeLevel - targetGradeLevel,
            Warnings: warnings,
            SuggestedPreset: suggestedPreset);
    }
}

/// <summary>
/// Indicates the achievability of a readability target given the source text.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Used by <see cref="TargetValidationResult"/> to provide a clear
/// signal about whether simplification should proceed and what to expect.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
public enum TargetAchievability
{
    /// <summary>
    /// The target is achievable with standard simplification techniques.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The grade level delta is small (≤3) and there are no significant
    /// vocabulary or sentence length concerns.
    /// </remarks>
    Achievable,

    /// <summary>
    /// The target is challenging but possible with significant rewriting.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The grade level delta is moderate (3-5) or there are vocabulary/
    /// sentence length concerns that will require extra attention.
    /// </remarks>
    Challenging,

    /// <summary>
    /// The target is unlikely to be achieved without major restructuring.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The grade level delta is large (>5) or the text has characteristics
    /// that fundamentally resist simplification (e.g., technical terminology that can't
    /// be replaced).
    /// </remarks>
    Unlikely,

    /// <summary>
    /// The source text already meets or exceeds the target readability.
    /// </summary>
    /// <remarks>
    /// <b>LOGIC:</b> The source grade level is at or below the target grade level.
    /// No simplification is needed.
    /// </remarks>
    AlreadyMet
}
