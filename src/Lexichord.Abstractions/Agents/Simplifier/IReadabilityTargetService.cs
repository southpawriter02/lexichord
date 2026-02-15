// -----------------------------------------------------------------------
// <copyright file="IReadabilityTargetService.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Abstractions.Contracts;

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Service for determining and validating readability targets for text simplification.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> The Readability Target Service is the foundation of the Simplifier Agent.
/// It determines what readability level to target when simplifying text, based on:
/// <list type="bullet">
///   <item><description><b>Voice Profile:</b> Project-level settings from the active voice profile</description></item>
///   <item><description><b>Audience Presets:</b> Pre-configured settings for common audience types</description></item>
///   <item><description><b>Explicit Parameters:</b> Caller-specified grade level and constraints</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Target Resolution Priority:</b>
/// <list type="number">
///   <item><description>Explicit parameters (if provided via <see cref="GetTargetAsync"/> with explicit grade level)</description></item>
///   <item><description>Specified preset (if presetId provided)</description></item>
///   <item><description>Voice Profile settings (if active profile has TargetGradeLevel)</description></item>
///   <item><description>Default target (Grade 8, 20 word sentences, avoid jargon)</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Built-In Presets:</b>
/// <list type="bullet">
///   <item><description><b>general-public:</b> Grade 8, 20 word max, avoid jargon</description></item>
///   <item><description><b>technical:</b> Grade 12, 25 word max, explain jargon</description></item>
///   <item><description><b>executive:</b> Grade 10, 18 word max, avoid jargon</description></item>
///   <item><description><b>international:</b> Grade 6, 15 word max, avoid jargon</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Custom Presets:</b>
/// WriterPro and Teams tiers can create, update, and delete custom presets. Custom presets
/// are persisted via <see cref="ISettingsService"/> and loaded on service initialization.
/// </para>
/// <para>
/// <b>Target Validation:</b>
/// Before simplification, the <see cref="ValidateTarget"/> method can check whether a target
/// is achievable given the source text's current readability metrics. This helps users
/// understand whether their target is realistic and provides actionable warnings.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent feature.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Get target from a preset
/// var target = await targetService.GetTargetAsync(presetId: "general-public");
/// Console.WriteLine($"Target: Grade {target.TargetGradeLevel}, Max {target.MaxSentenceLength} words/sentence");
///
/// // Get target from voice profile (no preset specified)
/// var profileTarget = await targetService.GetTargetAsync();
/// Console.WriteLine($"Source: {profileTarget.Source}");
///
/// // Get explicit target
/// var explicitTarget = await targetService.GetTargetAsync(
///     targetGradeLevel: 6.0,
///     maxSentenceLength: 15);
/// Console.WriteLine($"Explicit target: Grade {explicitTarget.TargetGradeLevel}");
///
/// // Validate before simplifying
/// var validation = await targetService.ValidateTarget(sourceText, target);
/// if (validation.Achievability == TargetAchievability.Unlikely)
/// {
///     Console.WriteLine($"Warning: Target may not be achievable. Consider: {validation.SuggestedPreset}");
/// }
/// </code>
/// </example>
/// <seealso cref="AudiencePreset"/>
/// <seealso cref="ReadabilityTarget"/>
/// <seealso cref="TargetValidationResult"/>
public interface IReadabilityTargetService
{
    /// <summary>
    /// Gets all available presets (built-in and custom).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A collection of all available <see cref="AudiencePreset"/> instances,
    /// ordered with built-in presets first, then custom presets alphabetically.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Returns all presets regardless of license tier. Tier-gated features
    /// (like creating custom presets) are checked at the point of use, not retrieval.
    /// This allows all users to see what presets are available for upgrade messaging.
    /// </para>
    /// <para>
    /// <b>Performance:</b> This method should be fast as presets are cached in memory.
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<AudiencePreset>> GetAllPresetsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific preset by its Id.
    /// </summary>
    /// <param name="presetId">The unique identifier of the preset.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// The <see cref="AudiencePreset"/> with the specified Id, or <c>null</c> if not found.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="presetId"/> is null, empty, or whitespace.</exception>
    /// <remarks>
    /// <b>LOGIC:</b> Searches both built-in and custom presets for the specified Id.
    /// Returns <c>null</c> rather than throwing if the preset doesn't exist to allow
    /// callers to handle missing presets gracefully.
    /// </remarks>
    Task<AudiencePreset?> GetPresetByIdAsync(string presetId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the readability target for a simplification operation.
    /// </summary>
    /// <param name="presetId">Optional preset Id to use for target resolution.</param>
    /// <param name="targetGradeLevel">Optional explicit target grade level (overrides preset/profile).</param>
    /// <param name="maxSentenceLength">Optional explicit max sentence length (overrides preset/profile).</param>
    /// <param name="avoidJargon">Optional explicit jargon handling (overrides preset/profile).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="ReadabilityTarget"/> resolved from the specified parameters, preset,
    /// Voice Profile, or defaults.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Resolution priority (highest to lowest):
    /// <list type="number">
    ///   <item><description>Explicit parameters (if <paramref name="targetGradeLevel"/> is specified)</description></item>
    ///   <item><description>Preset settings (if <paramref name="presetId"/> is specified and found)</description></item>
    ///   <item><description>Voice Profile settings (if active profile has TargetGradeLevel configured)</description></item>
    ///   <item><description>Default target (Grade 8, 20 words, avoid jargon)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Partial Overrides:</b>
    /// When explicit parameters are provided alongside a preset, the explicit values
    /// take precedence for those specific properties, while other properties use preset values.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // From preset
    /// var target1 = await service.GetTargetAsync(presetId: "general-public");
    ///
    /// // From explicit values
    /// var target2 = await service.GetTargetAsync(targetGradeLevel: 6.0, maxSentenceLength: 15);
    ///
    /// // From voice profile (default)
    /// var target3 = await service.GetTargetAsync();
    ///
    /// // Preset with override
    /// var target4 = await service.GetTargetAsync(
    ///     presetId: "general-public",
    ///     maxSentenceLength: 12);  // Override just this property
    /// </code>
    /// </example>
    Task<ReadabilityTarget> GetTargetAsync(
        string? presetId = null,
        double? targetGradeLevel = null,
        int? maxSentenceLength = null,
        bool? avoidJargon = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates whether a readability target is achievable for the given source text.
    /// </summary>
    /// <param name="sourceText">The text to be simplified.</param>
    /// <param name="target">The target readability to achieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>
    /// A <see cref="TargetValidationResult"/> indicating achievability and any warnings.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sourceText"/> or <paramref name="target"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sourceText"/> is empty or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Analyzes the source text using <see cref="IReadabilityService"/> and
    /// compares metrics against the target to determine achievability:
    /// <list type="bullet">
    ///   <item><description><b>AlreadyMet:</b> Source grade level ≤ target grade level</description></item>
    ///   <item><description><b>Achievable:</b> Grade delta ≤ 3 and no major concerns</description></item>
    ///   <item><description><b>Challenging:</b> Grade delta 3-5 or significant vocabulary/length concerns</description></item>
    ///   <item><description><b>Unlikely:</b> Grade delta > 5 or fundamental simplification barriers</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Warnings Generated:</b>
    /// <list type="bullet">
    ///   <item><description>High complex word ratio (>30%)</description></item>
    ///   <item><description>Long average sentence length (>target + 10 words)</description></item>
    ///   <item><description>Extreme grade level reduction (>4 levels)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    Task<TargetValidationResult> ValidateTarget(
        string sourceText,
        ReadabilityTarget target,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new custom preset.
    /// </summary>
    /// <param name="preset">The preset to create.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The created preset with any server-side modifications.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preset"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="preset"/> is invalid (see <see cref="AudiencePreset.IsValid"/>).</exception>
    /// <exception cref="InvalidOperationException">Thrown when a preset with the same Id already exists.</exception>
    /// <exception cref="LicenseTierException">Thrown when the current license tier doesn't support custom presets (requires WriterPro or Teams).</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Creates a custom preset and persists it via <see cref="ISettingsService"/>.
    /// The preset's <see cref="AudiencePreset.IsBuiltIn"/> property is forced to <c>false</c>
    /// regardless of the input value.
    /// </para>
    /// <para>
    /// <b>License Requirement:</b> WriterPro or Teams tier required.
    /// </para>
    /// </remarks>
    Task<AudiencePreset> CreateCustomPresetAsync(
        AudiencePreset preset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing custom preset.
    /// </summary>
    /// <param name="preset">The preset with updated values.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The updated preset.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="preset"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="preset"/> is invalid.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the preset doesn't exist or is a built-in preset.</exception>
    /// <exception cref="LicenseTierException">Thrown when the current license tier doesn't support custom presets.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Updates a custom preset's properties. Built-in presets cannot be modified.
    /// The preset Id cannot be changed; create a new preset if a different Id is needed.
    /// </para>
    /// <para>
    /// <b>License Requirement:</b> WriterPro or Teams tier required.
    /// </para>
    /// </remarks>
    Task<AudiencePreset> UpdateCustomPresetAsync(
        AudiencePreset preset,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a custom preset.
    /// </summary>
    /// <param name="presetId">The Id of the preset to delete.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><c>true</c> if the preset was deleted; <c>false</c> if it didn't exist.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="presetId"/> is null, empty, or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when attempting to delete a built-in preset.</exception>
    /// <exception cref="LicenseTierException">Thrown when the current license tier doesn't support custom presets.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Removes a custom preset from the collection and persists the change.
    /// Built-in presets cannot be deleted.
    /// </para>
    /// <para>
    /// <b>License Requirement:</b> WriterPro or Teams tier required.
    /// </para>
    /// </remarks>
    Task<bool> DeleteCustomPresetAsync(string presetId, CancellationToken cancellationToken = default);
}
