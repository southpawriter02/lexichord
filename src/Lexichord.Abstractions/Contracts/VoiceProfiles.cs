// <copyright file="VoiceProfiles.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

namespace Lexichord.Abstractions.Contracts;

/// <summary>
/// A Voice Profile defines target style constraints for writing analysis.
/// Writers select a profile to receive feedback tailored to their content type.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4a - Voice profiles control grade level targets, passive voice tolerance,
/// adverb/weasel word flagging, and forbidden terminology categories.</para>
/// <para>Built-in profiles CANNOT be modified or deleted.</para>
/// <para>Custom profiles require Teams or Enterprise license.</para>
/// </remarks>
public record VoiceProfile
{
    /// <summary>
    /// Unique identifier for the profile.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Display name shown in the profile selector (e.g., "Technical", "Marketing").
    /// </summary>
    /// <remarks>Must be unique across all profiles.</remarks>
    public required string Name { get; init; }

    /// <summary>
    /// Description of when to use this profile.
    /// Shown in tooltips and settings.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Target Flesch-Kincaid grade level for this profile.
    /// Null means no specific grade level target.
    /// </summary>
    /// <example>
    /// Technical = 11.0, Marketing = 9.0, Academic = 13.0
    /// </example>
    public double? TargetGradeLevel { get; init; }

    /// <summary>
    /// Maximum acceptable deviation from target grade level.
    /// Text outside (target ± tolerance) will be flagged.
    /// </summary>
    public double GradeLevelTolerance { get; init; } = 2.0;

    /// <summary>
    /// Maximum recommended words per sentence.
    /// Sentences exceeding this are flagged as Info.
    /// </summary>
    public int MaxSentenceLength { get; init; } = 25;

    /// <summary>
    /// Whether passive voice is allowed in this profile.
    /// When false, ANY passive voice triggers a Warning.
    /// </summary>
    public bool AllowPassiveVoice { get; init; }

    /// <summary>
    /// Maximum percentage of passive voice sentences allowed.
    /// Only applies when <see cref="AllowPassiveVoice"/> is true.
    /// Exceeding this threshold triggers Info-level flags.
    /// </summary>
    public double MaxPassiveVoicePercentage { get; init; } = 10.0;

    /// <summary>
    /// Whether adverbs and intensifiers should be flagged.
    /// When true, words like "very", "really" show Info squigglies.
    /// </summary>
    public bool FlagAdverbs { get; init; } = true;

    /// <summary>
    /// Whether weasel words and hedges should be flagged.
    /// When true, words like "perhaps", "maybe" show Info squigglies.
    /// </summary>
    public bool FlagWeaselWords { get; init; } = true;

    /// <summary>
    /// Categories of terminology to forbid (from Lexicon).
    /// Terms in these categories will be flagged as violations.
    /// </summary>
    public IReadOnlyList<string> ForbiddenCategories { get; init; } = [];

    /// <summary>
    /// Whether this is a built-in profile that cannot be deleted or modified.
    /// </summary>
    public bool IsBuiltIn { get; init; }

    /// <summary>
    /// Sort order for display in the profile selector dropdown.
    /// Lower values appear first.
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>
    /// Validates that the profile has sensible constraint values.
    /// </summary>
    /// <param name="errors">The validation errors if validation fails.</param>
    /// <returns>True if valid; false with validation errors otherwise.</returns>
    public bool Validate(out IReadOnlyList<string> errors)
    {
        var errorList = new List<string>();

        if (string.IsNullOrWhiteSpace(Name))
        {
            errorList.Add("Profile name is required.");
        }

        if (Name?.Length > 50)
        {
            errorList.Add("Profile name must be 50 characters or less.");
        }

        if (TargetGradeLevel.HasValue && (TargetGradeLevel < 0 || TargetGradeLevel > 20))
        {
            errorList.Add("Target grade level must be between 0 and 20.");
        }

        if (GradeLevelTolerance < 0 || GradeLevelTolerance > 10)
        {
            errorList.Add("Grade level tolerance must be between 0 and 10.");
        }

        if (MaxSentenceLength < 5 || MaxSentenceLength > 100)
        {
            errorList.Add("Max sentence length must be between 5 and 100.");
        }

        if (MaxPassiveVoicePercentage < 0 || MaxPassiveVoicePercentage > 100)
        {
            errorList.Add("Max passive voice percentage must be between 0 and 100.");
        }

        errors = errorList.AsReadOnly();
        return errorList.Count == 0;
    }
}

/// <summary>
/// Service for managing Voice Profiles and tracking the currently active profile.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4a - The active profile determines which style constraints
/// are applied during analysis.</para>
/// <para>Profile changes publish ProfileChangedEvent via MediatR.</para>
/// </remarks>
public interface IVoiceProfileService
{
    /// <summary>
    /// Gets all available voice profiles, ordered by <see cref="VoiceProfile.SortOrder"/>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Immutable list of all profiles (built-in + custom).</returns>
    Task<IReadOnlyList<VoiceProfile>> GetAllProfilesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a specific profile by its unique identifier.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile if found; null otherwise.</returns>
    Task<VoiceProfile?> GetProfileAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Gets a profile by its name (case-insensitive).
    /// </summary>
    /// <param name="name">The profile name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile if found; null otherwise.</returns>
    Task<VoiceProfile?> GetProfileByNameAsync(string name, CancellationToken ct = default);

    /// <summary>
    /// Gets the currently active profile for the user.
    /// Returns the default profile (Technical) if none is set.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The active profile (never null).</returns>
    Task<VoiceProfile> GetActiveProfileAsync(CancellationToken ct = default);

    /// <summary>
    /// Sets the active profile and publishes ProfileChangedEvent.
    /// </summary>
    /// <param name="profileId">The ID of the profile to activate.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="ArgumentException">If profile ID is not found.</exception>
    Task SetActiveProfileAsync(Guid profileId, CancellationToken ct = default);

    /// <summary>
    /// Creates a new custom profile. Requires Teams+ license.
    /// </summary>
    /// <param name="profile">The profile to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created profile with generated ID.</returns>
    /// <exception cref="InvalidOperationException">If license doesn't allow custom profiles.</exception>
    /// <exception cref="ValidationException">If profile validation fails.</exception>
    Task<VoiceProfile> CreateProfileAsync(VoiceProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing custom profile.
    /// </summary>
    /// <param name="profile">The profile with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">If attempting to modify a built-in profile.</exception>
    Task UpdateProfileAsync(VoiceProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes a custom profile.
    /// </summary>
    /// <param name="id">The ID of the profile to delete.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">If attempting to delete a built-in profile.</exception>
    Task DeleteProfileAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Resets the active profile to the default (Technical).
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    Task ResetToDefaultAsync(CancellationToken ct = default);
}

/// <summary>
/// Repository interface for Voice Profile persistence.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4a - Handles CRUD operations for custom profiles only.
/// Built-in profiles are defined in code and not persisted.</para>
/// </remarks>
public interface IVoiceProfileRepository
{
    /// <summary>
    /// Gets all custom (non-built-in) profiles.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of custom profiles.</returns>
    Task<IReadOnlyList<VoiceProfile>> GetAllAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a profile by its ID.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The profile if found; null otherwise.</returns>
    Task<VoiceProfile?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Creates a new profile in the database.
    /// </summary>
    /// <param name="profile">The profile to create.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CreateAsync(VoiceProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing profile.
    /// </summary>
    /// <param name="profile">The profile with updated values.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateAsync(VoiceProfile profile, CancellationToken ct = default);

    /// <summary>
    /// Deletes a profile by its ID.
    /// </summary>
    /// <param name="id">The profile ID.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
