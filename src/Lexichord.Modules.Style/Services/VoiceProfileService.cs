// <copyright file="VoiceProfileService.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.ComponentModel.DataAnnotations;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.Style.Services;

/// <summary>
/// Service for managing Voice Profiles and tracking the active profile.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4a - The active profile determines which style constraints
/// are applied during writing analysis. Profile changes publish ProfileChangedEvent
/// via MediatR to enable re-analysis with new constraints.
///
/// Design decisions:
/// - Built-in profiles are defined in code (BuiltInProfiles) and merged with custom DB profiles
/// - Active profile is cached in memory and defaults to Technical on startup
/// - Custom profile creation requires Teams+ license (checked via ILicenseContext)
/// - All profile lists are cached until invalidated by create/update/delete operations
/// </remarks>
public class VoiceProfileService : IVoiceProfileService
{
    private readonly IVoiceProfileRepository _repository;
    private readonly ILicenseContext _licenseContext;
    private readonly IMediator _mediator;
    private readonly ILogger<VoiceProfileService> _logger;

    private VoiceProfile? _cachedActiveProfile;
    private IReadOnlyList<VoiceProfile>? _cachedProfiles;
    private readonly object _cacheLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="VoiceProfileService"/> class.
    /// </summary>
    /// <param name="repository">Voice profile repository.</param>
    /// <param name="licenseContext">License context for feature gating.</param>
    /// <param name="mediator">MediatR mediator for publishing events.</param>
    /// <param name="logger">Logger instance.</param>
    public VoiceProfileService(
        IVoiceProfileRepository repository,
        ILicenseContext licenseContext,
        IMediator mediator,
        ILogger<VoiceProfileService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _licenseContext = licenseContext ?? throw new ArgumentNullException(nameof(licenseContext));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<VoiceProfile>> GetAllProfilesAsync(CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            if (_cachedProfiles is not null)
            {
                return _cachedProfiles;
            }
        }

        _logger.LogDebug("Loading voice profiles from repository");

        var customProfiles = await _repository.GetAllAsync(ct);
        var allProfiles = BuiltInProfiles.All
            .Concat(customProfiles)
            .OrderBy(p => p.SortOrder)
            .ThenBy(p => p.Name)
            .ToList()
            .AsReadOnly();

        lock (_cacheLock)
        {
            _cachedProfiles = allProfiles;
        }

        _logger.LogDebug(
            "Loaded {Count} voice profiles ({BuiltInCount} built-in, {CustomCount} custom)",
            allProfiles.Count, BuiltInProfiles.All.Count, customProfiles.Count);

        return allProfiles;
    }

    /// <inheritdoc />
    public async Task<VoiceProfile?> GetProfileAsync(Guid id, CancellationToken ct = default)
    {
        var profiles = await GetAllProfilesAsync(ct);
        return profiles.FirstOrDefault(p => p.Id == id);
    }

    /// <inheritdoc />
    public async Task<VoiceProfile?> GetProfileByNameAsync(string name, CancellationToken ct = default)
    {
        var profiles = await GetAllProfilesAsync(ct);
        return profiles.FirstOrDefault(
            p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <inheritdoc />
    public Task<VoiceProfile> GetActiveProfileAsync(CancellationToken ct = default)
    {
        lock (_cacheLock)
        {
            if (_cachedActiveProfile is not null)
            {
                return Task.FromResult(_cachedActiveProfile);
            }
        }

        // LOGIC: Default to Technical profile on startup
        var profile = BuiltInProfiles.Default;

        lock (_cacheLock)
        {
            _cachedActiveProfile = profile;
        }

        _logger.LogDebug(
            "Profile {ProfileName} activated with constraints: Grade={Grade}, MaxSentence={MaxSentence}",
            profile.Name, profile.TargetGradeLevel, profile.MaxSentenceLength);

        return Task.FromResult(profile);
    }

    /// <inheritdoc />
    public async Task SetActiveProfileAsync(Guid profileId, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(profileId, ct)
            ?? throw new ArgumentException(
                $"Profile with ID {profileId} not found.",
                nameof(profileId));

        var currentProfile = await GetActiveProfileAsync(ct);

        // LOGIC: No-op if already active
        if (currentProfile.Id == profileId)
        {
            return;
        }

        var previousId = currentProfile.Id;

        // Update cache
        lock (_cacheLock)
        {
            _cachedActiveProfile = profile;
        }

        _logger.LogInformation(
            "Active profile changed: {PreviousProfile} -> {NewProfile}",
            currentProfile.Name, profile.Name);

        _logger.LogDebug(
            "Profile {ProfileName} activated with constraints: Grade={Grade}, MaxSentence={MaxSentence}",
            profile.Name, profile.TargetGradeLevel, profile.MaxSentenceLength);

        // Publish event
        await _mediator.Publish(
            new ProfileChangedEvent(previousId, profileId, profile.Name),
            ct);
    }

    /// <inheritdoc />
    public async Task<VoiceProfile> CreateProfileAsync(VoiceProfile profile, CancellationToken ct = default)
    {
        // LOGIC: License check - custom profiles require Teams+ license
        if (!_licenseContext.IsFeatureEnabled(FeatureCodes.CustomProfiles))
        {
            _logger.LogWarning(
                "Attempted to create custom profile without Teams license");
            throw new InvalidOperationException(
                "Custom voice profiles require a Teams or Enterprise license.");
        }

        // Validation
        if (!profile.Validate(out var errors))
        {
            throw new ValidationException(
                $"Profile validation failed: {string.Join(", ", errors)}");
        }

        // Uniqueness check
        var existing = await GetProfileByNameAsync(profile.Name, ct);
        if (existing is not null)
        {
            throw new InvalidOperationException(
                $"A profile named '{profile.Name}' already exists.");
        }

        // LOGIC: Create with new ID, ensure not marked as built-in
        var newProfile = profile with
        {
            Id = Guid.NewGuid(),
            IsBuiltIn = false,
            SortOrder = 100 // Custom profiles sort after built-in
        };

        await _repository.CreateAsync(newProfile, ct);

        // Invalidate cache
        InvalidateCache();

        _logger.LogInformation("Created custom profile: {ProfileName}", newProfile.Name);

        return newProfile;
    }

    /// <inheritdoc />
    public async Task UpdateProfileAsync(VoiceProfile profile, CancellationToken ct = default)
    {
        if (profile.IsBuiltIn)
        {
            throw new InvalidOperationException(
                "Cannot modify built-in profiles.");
        }

        if (!profile.Validate(out var errors))
        {
            throw new ValidationException(
                $"Profile validation failed: {string.Join(", ", errors)}");
        }

        await _repository.UpdateAsync(profile, ct);

        // Invalidate cache
        InvalidateCache();

        // LOGIC: If this was the active profile, update the active cache
        lock (_cacheLock)
        {
            if (_cachedActiveProfile?.Id == profile.Id)
            {
                _cachedActiveProfile = profile;
            }
        }

        _logger.LogInformation("Updated custom profile: {ProfileName}", profile.Name);
    }

    /// <inheritdoc />
    public async Task DeleteProfileAsync(Guid id, CancellationToken ct = default)
    {
        var profile = await GetProfileAsync(id, ct)
            ?? throw new ArgumentException(
                $"Profile with ID {id} not found.",
                nameof(id));

        if (profile.IsBuiltIn)
        {
            _logger.LogWarning(
                "Attempted to delete built-in profile: {ProfileName}",
                profile.Name);
            throw new InvalidOperationException(
                "Cannot delete built-in profiles.");
        }

        await _repository.DeleteAsync(id, ct);

        // Invalidate cache
        InvalidateCache();

        // LOGIC: If this was the active profile, reset to default
        bool wasActive;
        lock (_cacheLock)
        {
            wasActive = _cachedActiveProfile?.Id == id;
        }

        if (wasActive)
        {
            await ResetToDefaultAsync(ct);
        }

        _logger.LogInformation("Deleted custom profile: {ProfileName}", profile.Name);
    }

    /// <inheritdoc />
    public async Task ResetToDefaultAsync(CancellationToken ct = default)
    {
        await SetActiveProfileAsync(BuiltInProfiles.Default.Id, ct);
    }

    private void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedProfiles = null;
        }
    }
}
