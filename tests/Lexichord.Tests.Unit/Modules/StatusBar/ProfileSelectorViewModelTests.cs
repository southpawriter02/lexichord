using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.StatusBar.ViewModels;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.StatusBar;

/// <summary>
/// Unit tests for ProfileSelectorViewModel.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4d - These tests verify the profile selector ViewModel correctly:
/// - Displays the current profile name after auto-initialization
/// - Generates rich tooltips with profile configuration
/// - Handles profile selection commands
/// - Updates display after profile switch
/// - Handles service errors gracefully
///
/// NOTE: The ViewModel constructor calls RefreshAsync() automatically via fire-and-forget,
/// so we add a small delay in tests to ensure initialization completes.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Category", "StatusBar")]
[Trait("Category", "v0.3.4d")]
public class ProfileSelectorViewModelTests
{
    // Test profile data
    private static readonly VoiceProfile TechnicalProfile = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
        Name = "Technical",
        Description = "Technical writing style",
        TargetGradeLevel = 12.0,
        GradeLevelTolerance = 2.0,
        MaxSentenceLength = 25,
        AllowPassiveVoice = true,
        MaxPassiveVoicePercentage = 20.0,
        FlagAdverbs = true,
        FlagWeaselWords = true,
        SortOrder = 0,
        IsBuiltIn = true
    };

    private static readonly VoiceProfile MarketingProfile = new()
    {
        Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
        Name = "Marketing",
        Description = "Marketing writing style",
        TargetGradeLevel = 8.0,
        GradeLevelTolerance = 1.5,
        MaxSentenceLength = 20,
        AllowPassiveVoice = false,
        MaxPassiveVoicePercentage = 0.0,
        FlagAdverbs = true,
        FlagWeaselWords = true,
        SortOrder = 1,
        IsBuiltIn = true
    };

    private readonly IVoiceProfileService _profileService;
    private readonly ILogger<ProfileSelectorViewModel> _logger;

    public ProfileSelectorViewModelTests()
    {
        _profileService = Substitute.For<IVoiceProfileService>();
        _logger = Substitute.For<ILogger<ProfileSelectorViewModel>>();

        // Default setup: return Technical as active profile
        _profileService.GetActiveProfileAsync(Arg.Any<CancellationToken>())
            .Returns(TechnicalProfile);
        _profileService.GetAllProfilesAsync(Arg.Any<CancellationToken>())
            .Returns(new List<VoiceProfile> { TechnicalProfile, MarketingProfile });
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenServiceIsNull()
    {
        // Act
        var act = () => new ProfileSelectorViewModel(null!, _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profileService");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act
        var act = () => new ProfileSelectorViewModel(_profileService, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task Constructor_InitializesProfileData_Automatically()
    {
        // Arrange & Act
        var sut = new ProfileSelectorViewModel(_profileService, _logger);

        // Allow auto-initialization to complete
        await Task.Delay(100);

        // Assert - constructor should have triggered RefreshAsync
        sut.CurrentProfileName.Should().Be("Technical");
        sut.HasActiveProfile.Should().BeTrue();
    }

    #endregion

    #region CurrentProfileName Tests

    [Fact]
    public async Task CurrentProfileName_ReturnsProfileName_AfterRefresh()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Assert
        sut.CurrentProfileName.Should().Be("Technical");
    }

    [Fact]
    public async Task CurrentProfileName_UpdatesAfterProfileChange()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init
        sut.CurrentProfileName.Should().Be("Technical");

        // Update mock to return Marketing profile
        _profileService.GetActiveProfileAsync(Arg.Any<CancellationToken>())
            .Returns(MarketingProfile);

        // Act
        await sut.RefreshAsync();

        // Assert
        sut.CurrentProfileName.Should().Be("Marketing");
    }

    #endregion

    #region HasActiveProfile Tests

    [Fact]
    public async Task HasActiveProfile_ReturnsTrue_AfterAutoInit()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Assert
        sut.HasActiveProfile.Should().BeTrue();
    }

    #endregion

    #region ProfileTooltip Tests

    [Fact]
    public async Task ProfileTooltip_ContainsProfileName_AfterRefresh()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Assert
        sut.ProfileTooltip.Should().Contain("Technical");
    }

    [Fact]
    public async Task ProfileTooltip_ContainsGradeLevel_AfterRefresh()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Assert
        sut.ProfileTooltip.Should().Contain("12"); // Grade level 12.0
        sut.ProfileTooltip.Should().Contain("Grade");
    }

    [Fact]
    public async Task ProfileTooltip_ContainsPassiveVoiceInfo_AfterRefresh()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Assert
        sut.ProfileTooltip.Should().Contain("Passive");
    }

    #endregion

    #region AvailableProfiles Tests

    [Fact]
    public async Task AvailableProfiles_ContainsAllProfiles_AfterAutoInit()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Assert
        sut.AvailableProfiles.Should().HaveCount(2);
        sut.AvailableProfiles.Should().ContainSingle(p => p.Name == "Technical");
        sut.AvailableProfiles.Should().ContainSingle(p => p.Name == "Marketing");
    }

    #endregion

    #region SelectProfileCommand Tests

    [Fact]
    public async Task SelectProfileCommand_SetsActiveProfile()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Act
        await sut.SelectProfileCommand.ExecuteAsync(MarketingProfile.Id);

        // Assert
        await _profileService.Received(1).SetActiveProfileAsync(
            MarketingProfile.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SelectProfileCommand_RefreshesAfterSelection()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        // Setup: update mock so subsequent refresh returns Marketing
        _profileService.GetActiveProfileAsync(Arg.Any<CancellationToken>())
            .Returns(MarketingProfile);

        // Act
        await sut.SelectProfileCommand.ExecuteAsync(MarketingProfile.Id);

        // Assert - should have called GetActiveProfileAsync again after selection
        await _profileService.Received().GetActiveProfileAsync(Arg.Any<CancellationToken>());
        sut.CurrentProfileName.Should().Be("Marketing");
    }

    [Fact]
    public async Task SelectProfileCommand_HandlesServiceError_Gracefully()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init

        _profileService.SetActiveProfileAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Service error"));

        // Act - should not throw
        var act = async () => await sut.SelectProfileCommand.ExecuteAsync(MarketingProfile.Id);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion

    #region RefreshAsync Tests

    [Fact]
    public async Task RefreshAsync_FetchesFromService()
    {
        // Arrange
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow auto-init
        _profileService.ClearReceivedCalls(); // Reset call tracking

        // Act
        await sut.RefreshAsync();

        // Assert - should have called service methods
        await _profileService.Received(1).GetActiveProfileAsync(Arg.Any<CancellationToken>());
        await _profileService.Received(1).GetAllProfilesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RefreshAsync_HandlesServiceError_Gracefully()
    {
        // Arrange - configure service to fail
        _profileService.GetActiveProfileAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Service unavailable"));

        // Act - creating ViewModel triggers auto-refresh which should handle error
        var sut = new ProfileSelectorViewModel(_profileService, _logger);
        await Task.Delay(100); // Allow time for error to be handled

        // Verify explicit call also doesn't throw
        var act = async () => await sut.RefreshAsync();
        await act.Should().NotThrowAsync();

        // Assert - profile name should indicate error state
        sut.CurrentProfileName.Should().NotBeNullOrEmpty();
    }

    #endregion
}

