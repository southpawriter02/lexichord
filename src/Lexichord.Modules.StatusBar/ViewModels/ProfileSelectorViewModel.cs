// <copyright file="ProfileSelectorViewModel.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Lexichord.Abstractions.Contracts;
using Microsoft.Extensions.Logging;

namespace Lexichord.Modules.StatusBar.ViewModels;

/// <summary>
/// ViewModel for the voice profile selector widget in the status bar.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.4d - Displays the currently active voice profile and allows
/// profile switching via context menu. Features:</para>
/// <list type="bullet">
///   <item>Compact display showing profile name with icon</item>
///   <item>Rich tooltip with full profile configuration details</item>
///   <item>Context menu populated with all available profiles</item>
///   <item>Profile updates after selection (no external event subscription needed)</item>
/// </list>
/// <para>Thread-safe: all operations are UI-thread safe via CommunityToolkit.Mvvm.</para>
/// </remarks>
public partial class ProfileSelectorViewModel : ObservableObject, IProfileSelectorViewModel
{
    private readonly IVoiceProfileService _profileService;
    private readonly ILogger<ProfileSelectorViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProfileSelectorViewModel"/> class.
    /// </summary>
    /// <param name="profileService">The voice profile service for profile management.</param>
    /// <param name="logger">The logger instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is null.</exception>
    public ProfileSelectorViewModel(
        IVoiceProfileService profileService,
        ILogger<ProfileSelectorViewModel> logger)
    {
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _logger.LogDebug("ProfileSelectorViewModel created");

        // Initialize async (fire-and-forget with error handling)
        _ = RefreshAsync();
    }

    #region Observable Properties

    /// <summary>
    /// Backing field for the current profile name.
    /// </summary>
    [ObservableProperty]
    private string _currentProfileName = "Loading...";

    /// <summary>
    /// Backing field for the profile tooltip.
    /// </summary>
    [ObservableProperty]
    private string _profileTooltip = "Loading profile information...";

    /// <summary>
    /// Backing field for the has active profile flag.
    /// </summary>
    [ObservableProperty]
    private bool _hasActiveProfile;

    /// <summary>
    /// Backing field for the available profiles list.
    /// </summary>
    [ObservableProperty]
    private IReadOnlyList<VoiceProfile> _availableProfiles = [];

    /// <summary>
    /// Backing field for the is loading flag.
    /// </summary>
    [ObservableProperty]
    private bool _isLoading = true;

    #endregion

    #region IProfileSelectorViewModel Implementation

    /// <inheritdoc/>
    ICommand IProfileSelectorViewModel.SelectProfileCommand => SelectProfileCommand;

    #endregion

    #region Commands

    /// <summary>
    /// Command to select a specific voice profile.
    /// </summary>
    /// <remarks>
    /// <para>LOGIC: Accepts a Guid parameter representing the profile ID to select.</para>
    /// <para>After selection, the ViewModel refreshes to display the new profile.</para>
    /// </remarks>
    [RelayCommand]
    private async Task SelectProfileAsync(Guid profileId)
    {
        _logger.LogDebug("SelectProfileAsync called with profileId: {ProfileId}", profileId);

        try
        {
            IsLoading = true;
            await _profileService.SetActiveProfileAsync(profileId);
            _logger.LogInformation("Profile changed to {ProfileId}", profileId);

            // Refresh to update display
            await RefreshAsync();
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid profile ID: {ProfileId}", profileId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to select profile {ProfileId}", profileId);
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Public Methods

    /// <inheritdoc/>
    public async Task RefreshAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Refreshing profile selector");

        try
        {
            IsLoading = true;

            // Load available profiles
            var profiles = await _profileService.GetAllProfilesAsync(ct);
            AvailableProfiles = profiles;
            _logger.LogDebug("Loaded {Count} available profiles", profiles.Count);

            // Get active profile
            var activeProfile = await _profileService.GetActiveProfileAsync(ct);
            UpdateDisplay(activeProfile);

            HasActiveProfile = true;
            _logger.LogDebug("Active profile: {ProfileName}", activeProfile.Name);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("RefreshAsync cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refresh profile selector");
            CurrentProfileName = "Error";
            ProfileTooltip = "Failed to load profile information";
            HasActiveProfile = false;
        }
        finally
        {
            IsLoading = false;
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Updates the display properties for the given profile.
    /// </summary>
    /// <param name="profile">The profile to display.</param>
    /// <remarks>
    /// <para>LOGIC: Generates tooltip content per specification format:</para>
    /// <code>
    /// ───────────────────────────
    /// Profile: {Name}
    /// {Description}
    /// ───────────────────────────
    /// Grade Level:      {target ± tolerance}
    /// Max Sentence:     {length} words
    /// Passive Voice:    {allowed/forbidden}
    /// Flag Adverbs:     {Yes/No}
    /// Flag Weasels:     {Yes/No}
    /// ───────────────────────────
    /// Click to change profile
    /// </code>
    /// </remarks>
    private void UpdateDisplay(VoiceProfile profile)
    {
        CurrentProfileName = profile.Name;
        ProfileTooltip = GenerateTooltip(profile);
    }

    /// <summary>
    /// Generates the rich tooltip content for a profile.
    /// </summary>
    /// <param name="profile">The profile to generate tooltip for.</param>
    /// <returns>The formatted tooltip string.</returns>
    private static string GenerateTooltip(VoiceProfile profile)
    {
        var passiveText = profile.AllowPassiveVoice
            ? $"Allowed (max {profile.MaxPassiveVoicePercentage:F0}%)"
            : "Forbidden";

        var gradeText = profile.TargetGradeLevel.HasValue
            ? $"{profile.TargetGradeLevel:F1} ± {profile.GradeLevelTolerance:F1}"
            : "Any";

        var description = !string.IsNullOrWhiteSpace(profile.Description)
            ? $"{profile.Description}\n"
            : string.Empty;

        return
            $"───────────────────────────\n" +
            $"Profile: {profile.Name}\n" +
            $"{description}" +
            $"───────────────────────────\n" +
            $"Grade Level:      {gradeText}\n" +
            $"Max Sentence:     {profile.MaxSentenceLength} words\n" +
            $"Passive Voice:    {passiveText}\n" +
            $"Flag Adverbs:     {(profile.FlagAdverbs ? "Yes" : "No")}\n" +
            $"Flag Weasels:     {(profile.FlagWeaselWords ? "Yes" : "No")}\n" +
            $"───────────────────────────\n" +
            $"Click to change profile";
    }

    #endregion
}
