// -----------------------------------------------------------------------
// <copyright file="AudiencePreset.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

namespace Lexichord.Abstractions.Agents.Simplifier;

/// <summary>
/// Represents an audience preset configuration for the Simplifier Agent.
/// Defines target readability parameters for a specific audience type.
/// </summary>
/// <remarks>
/// <para>
/// <b>LOGIC:</b> Audience presets provide pre-configured readability targets for
/// common audience types. Each preset defines a target grade level, maximum sentence
/// length, and guidance on jargon usage. The Simplifier Agent uses these parameters
/// to calibrate text simplification.
/// </para>
/// <para>
/// <b>Built-In Presets:</b>
/// <list type="bullet">
///   <item><description><b>General Public (Grade 8):</b> Broad accessibility, avoid jargon</description></item>
///   <item><description><b>Technical (Grade 12):</b> Professional audience, explain jargon</description></item>
///   <item><description><b>Executive (Grade 10):</b> Concise summaries, avoid jargon</description></item>
///   <item><description><b>International/ESL (Grade 6):</b> Simple language, avoid jargon</description></item>
/// </list>
/// </para>
/// <para>
/// <b>Custom Presets:</b>
/// WriterPro and Teams tiers can create, update, and delete custom presets via
/// <see cref="IReadabilityTargetService.CreateCustomPresetAsync"/>,
/// <see cref="IReadabilityTargetService.UpdateCustomPresetAsync"/>, and
/// <see cref="IReadabilityTargetService.DeleteCustomPresetAsync"/>.
/// </para>
/// <para>
/// <b>Introduced in:</b> v0.7.4a as part of the Simplifier Agent Readability Target Service.
/// </para>
/// </remarks>
/// <param name="Id">Unique identifier for the preset (e.g., "general-public", "custom-medical").</param>
/// <param name="Name">Human-readable display name (e.g., "General Public", "Medical Documentation").</param>
/// <param name="TargetGradeLevel">Target Flesch-Kincaid grade level (typically 6-12).</param>
/// <param name="MaxSentenceLength">Maximum recommended sentence length in words.</param>
/// <param name="AvoidJargon">If true, replace jargon with plain language; if false, explain jargon in context.</param>
/// <param name="Description">Optional description explaining when to use this preset.</param>
/// <param name="IsBuiltIn">Indicates whether this is a built-in preset (not editable by users).</param>
/// <example>
/// <code>
/// // Creating a custom preset for medical documentation
/// var medicalPreset = new AudiencePreset(
///     Id: "custom-medical",
///     Name: "Medical Documentation",
///     TargetGradeLevel: 10.0,
///     MaxSentenceLength: 20,
///     AvoidJargon: false,  // Explain medical terms rather than avoiding them
///     Description: "For patient-facing medical documentation with term explanations",
///     IsBuiltIn: false);
///
/// // Checking preset properties
/// if (preset.IsBuiltIn)
/// {
///     Console.WriteLine($"Built-in preset: {preset.Name}");
/// }
///
/// // Using preset values
/// if (preset.AvoidJargon)
/// {
///     Console.WriteLine("Replace technical terms with plain language");
/// }
/// else
/// {
///     Console.WriteLine("Explain technical terms in context");
/// }
/// </code>
/// </example>
/// <seealso cref="IReadabilityTargetService"/>
/// <seealso cref="ReadabilityTarget"/>
public record AudiencePreset(
    string Id,
    string Name,
    double TargetGradeLevel,
    int MaxSentenceLength,
    bool AvoidJargon,
    string? Description = null,
    bool IsBuiltIn = false)
{
    /// <summary>
    /// Validates that the preset has valid parameters.
    /// </summary>
    /// <returns><c>true</c> if all parameters are within valid ranges; otherwise, <c>false</c>.</returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Validation rules:
    /// <list type="bullet">
    ///   <item><description><see cref="Id"/> must not be null, empty, or whitespace</description></item>
    ///   <item><description><see cref="Name"/> must not be null, empty, or whitespace</description></item>
    ///   <item><description><see cref="TargetGradeLevel"/> must be between 1 and 20 (inclusive)</description></item>
    ///   <item><description><see cref="MaxSentenceLength"/> must be between 5 and 50 (inclusive)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public bool IsValid()
    {
        // LOGIC: Id must not be empty
        if (string.IsNullOrWhiteSpace(Id))
            return false;

        // LOGIC: Name must not be empty
        if (string.IsNullOrWhiteSpace(Name))
            return false;

        // LOGIC: Grade level must be in reasonable range (1-20)
        if (TargetGradeLevel < 1 || TargetGradeLevel > 20)
            return false;

        // LOGIC: Sentence length must be in reasonable range (5-50)
        if (MaxSentenceLength < 5 || MaxSentenceLength > 50)
            return false;

        return true;
    }

    /// <summary>
    /// Gets a detailed validation result explaining any validation failures.
    /// </summary>
    /// <returns>
    /// A tuple containing a boolean indicating validity and a list of validation error messages.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> Unlike <see cref="IsValid"/>, this method provides detailed error messages
    /// for each validation failure, useful for displaying validation errors to users in
    /// the UI or logging.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// var (isValid, errors) = preset.Validate();
    /// if (!isValid)
    /// {
    ///     foreach (var error in errors)
    ///     {
    ///         Console.WriteLine($"Validation error: {error}");
    ///     }
    /// }
    /// </code>
    /// </example>
    public (bool IsValid, IReadOnlyList<string> Errors) Validate()
    {
        var errors = new List<string>();

        // LOGIC: Validate Id
        if (string.IsNullOrWhiteSpace(Id))
            errors.Add("Preset Id is required and cannot be empty.");

        // LOGIC: Validate Name
        if (string.IsNullOrWhiteSpace(Name))
            errors.Add("Preset Name is required and cannot be empty.");

        // LOGIC: Validate TargetGradeLevel range
        if (TargetGradeLevel < 1)
            errors.Add($"Target grade level ({TargetGradeLevel}) must be at least 1.");
        else if (TargetGradeLevel > 20)
            errors.Add($"Target grade level ({TargetGradeLevel}) must not exceed 20.");

        // LOGIC: Validate MaxSentenceLength range
        if (MaxSentenceLength < 5)
            errors.Add($"Maximum sentence length ({MaxSentenceLength}) must be at least 5 words.");
        else if (MaxSentenceLength > 50)
            errors.Add($"Maximum sentence length ({MaxSentenceLength}) must not exceed 50 words.");

        return (errors.Count == 0, errors.AsReadOnly());
    }

    /// <summary>
    /// Creates a copy of this preset with a new Id, suitable for creating a custom version.
    /// </summary>
    /// <param name="newId">The new Id for the cloned preset.</param>
    /// <returns>A new <see cref="AudiencePreset"/> instance with the specified Id and <see cref="IsBuiltIn"/> set to false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newId"/> is null, empty, or whitespace.</exception>
    /// <remarks>
    /// <para>
    /// <b>LOGIC:</b> This method is used when a user wants to create a custom preset based on
    /// a built-in preset. The new preset has <see cref="IsBuiltIn"/> set to <c>false</c> so
    /// it can be edited and deleted.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Clone a built-in preset to customize
    /// var generalPublic = BuiltInPresets.GeneralPublic;
    /// var customPreset = generalPublic.CloneWithId("custom-blog-audience");
    ///
    /// // Modify the clone
    /// customPreset = customPreset with { MaxSentenceLength = 15 };
    /// </code>
    /// </example>
    public AudiencePreset CloneWithId(string newId)
    {
        if (string.IsNullOrWhiteSpace(newId))
            throw new ArgumentException("New Id cannot be null, empty, or whitespace.", nameof(newId));

        return this with
        {
            Id = newId,
            IsBuiltIn = false
        };
    }
}
