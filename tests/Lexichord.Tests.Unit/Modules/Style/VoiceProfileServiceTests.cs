using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.Style.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for VoiceProfileService.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.4a - These tests verify profile operations, caching, license gating, and event publishing.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.4a")]
public class VoiceProfileServiceTests
{
    private readonly Mock<IVoiceProfileRepository> _repositoryMock = new();
    private readonly Mock<ILicenseContext> _licenseContextMock = new();
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly ILogger<VoiceProfileService> _logger = NullLogger<VoiceProfileService>.Instance;

    #region Constructor Tests

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VoiceProfileService(null!, _licenseContextMock.Object, _mediatorMock.Object, _logger));
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VoiceProfileService(_repositoryMock.Object, null!, _mediatorMock.Object, _logger));
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VoiceProfileService(_repositoryMock.Object, _licenseContextMock.Object, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new VoiceProfileService(_repositoryMock.Object, _licenseContextMock.Object, _mediatorMock.Object, null!));
    }

    [Fact]
    public void Constructor_ValidParameters_CreatesInstance()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region GetAllProfilesAsync Tests

    [Fact]
    public async Task GetAllProfilesAsync_IncludesBuiltInProfiles()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();

        // Act
        var result = await service.GetAllProfilesAsync();

        // Assert
        result.Should().HaveCount(5); // 5 built-in profiles
        result.Should().Contain(p => p.Name == "Technical");
        result.Should().Contain(p => p.Name == "Marketing");
        result.Should().Contain(p => p.Name == "Academic");
        result.Should().Contain(p => p.Name == "Narrative");
        result.Should().Contain(p => p.Name == "Casual");
    }

    [Fact]
    public async Task GetAllProfilesAsync_IncludesCustomProfiles()
    {
        // Arrange
        var customProfile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Custom Test",
            IsBuiltIn = false,
            SortOrder = 100
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile> { customProfile });

        var service = CreateService();

        // Act
        var result = await service.GetAllProfilesAsync();

        // Assert
        result.Should().HaveCount(6); // 5 built-in + 1 custom
        result.Should().Contain(p => p.Name == "Custom Test");
    }

    [Fact]
    public async Task GetAllProfilesAsync_CachesResults()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();

        // Act - call twice
        await service.GetAllProfilesAsync();
        await service.GetAllProfilesAsync();

        // Assert - repository only called once due to caching
        _repositoryMock.Verify(
            r => r.GetAllAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region GetActiveProfileAsync Tests

    [Fact]
    public async Task GetActiveProfileAsync_DefaultsToTechnical()
    {
        // Arrange
        var service = CreateService();

        // Act
        var result = await service.GetActiveProfileAsync();

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Technical");
    }

    [Fact]
    public async Task GetActiveProfileAsync_CachesActiveProfile()
    {
        // Arrange
        var service = CreateService();

        // Act - call twice
        var first = await service.GetActiveProfileAsync();
        var second = await service.GetActiveProfileAsync();

        // Assert - same instance returned
        first.Should().BeSameAs(second);
    }

    #endregion

    #region SetActiveProfileAsync Tests

    [Fact]
    public async Task SetActiveProfileAsync_ValidId_UpdatesActiveProfile()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();
        var marketingId = BuiltInProfiles.Marketing.Id;

        // Act
        await service.SetActiveProfileAsync(marketingId);
        var result = await service.GetActiveProfileAsync();

        // Assert
        result.Name.Should().Be("Marketing");
    }

    [Fact]
    public async Task SetActiveProfileAsync_PublishesProfileChangedEvent()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();
        var marketingId = BuiltInProfiles.Marketing.Id;

        // Act
        await service.SetActiveProfileAsync(marketingId);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.Is<ProfileChangedEvent>(e =>
                e.NewProfileId == marketingId &&
                e.NewProfileName == "Marketing"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SetActiveProfileAsync_InvalidId_ThrowsArgumentException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SetActiveProfileAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task SetActiveProfileAsync_SameProfile_NoEventPublished()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();
        var technicalId = BuiltInProfiles.Technical.Id;

        // Act - set to same profile (default is Technical)
        await service.SetActiveProfileAsync(technicalId);

        // Assert - no event for no-op
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<ProfileChangedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region CreateProfileAsync Tests

    [Fact]
    public async Task CreateProfileAsync_WithLicense_CreatesProfile()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.CustomProfiles))
            .Returns(true);
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();
        var newProfile = new VoiceProfile
        {
            Name = "My Custom Profile",
            MaxSentenceLength = 25
        };

        // Act
        var result = await service.CreateProfileAsync(newProfile);

        // Assert
        result.Name.Should().Be("My Custom Profile");
        result.Id.Should().NotBeEmpty();
        result.IsBuiltIn.Should().BeFalse();
        result.SortOrder.Should().Be(100);

        _repositoryMock.Verify(
            r => r.CreateAsync(It.Is<VoiceProfile>(p =>
                p.Name == "My Custom Profile" &&
                p.IsBuiltIn == false),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateProfileAsync_WithoutLicense_ThrowsInvalidOperationException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.CustomProfiles))
            .Returns(false);

        var service = CreateService();
        var newProfile = new VoiceProfile { Name = "My Custom Profile" };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateProfileAsync(newProfile));
        ex.Message.Should().Contain("Teams");
    }

    [Fact]
    public async Task CreateProfileAsync_DuplicateName_ThrowsInvalidOperationException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.CustomProfiles))
            .Returns(true);
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();
        var newProfile = new VoiceProfile { Name = "Technical" }; // Built-in name

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateProfileAsync(newProfile));
        ex.Message.Should().Contain("already exists");
    }

    [Fact]
    public async Task CreateProfileAsync_InvalidProfile_ThrowsValidationException()
    {
        // Arrange
        _licenseContextMock.Setup(l => l.IsFeatureEnabled(FeatureCodes.CustomProfiles))
            .Returns(true);
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();
        var newProfile = new VoiceProfile
        {
            Name = "", // Invalid - empty name
            MaxSentenceLength = 25
        };

        // Act & Assert
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(
            () => service.CreateProfileAsync(newProfile));
    }

    #endregion

    #region UpdateProfileAsync Tests

    [Fact]
    public async Task UpdateProfileAsync_CustomProfile_UpdatesSuccessfully()
    {
        // Arrange
        var customProfile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Custom",
            Description = "Updated description",
            IsBuiltIn = false,
            MaxSentenceLength = 30
        };

        var service = CreateService();

        // Act
        await service.UpdateProfileAsync(customProfile);

        // Assert
        _repositoryMock.Verify(
            r => r.UpdateAsync(customProfile, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateProfileAsync_BuiltInProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        var builtInProfile = BuiltInProfiles.Technical;

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateProfileAsync(builtInProfile));
        ex.Message.Should().Contain("built-in");
    }

    #endregion

    #region DeleteProfileAsync Tests

    [Fact]
    public async Task DeleteProfileAsync_CustomProfile_DeletesSuccessfully()
    {
        // Arrange
        var customProfile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Custom",
            IsBuiltIn = false
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile> { customProfile });

        var service = CreateService();

        // Act
        await service.DeleteProfileAsync(customProfile.Id);

        // Assert
        _repositoryMock.Verify(
            r => r.DeleteAsync(customProfile.Id, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DeleteProfileAsync_BuiltInProfile_ThrowsInvalidOperationException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteProfileAsync(BuiltInProfiles.Technical.Id));
        ex.Message.Should().Contain("built-in");
    }

    [Fact]
    public async Task DeleteProfileAsync_ActiveProfile_ResetsToDefault()
    {
        // Arrange
        var customProfile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Custom",
            IsBuiltIn = false
        };
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile> { customProfile });

        var service = CreateService();

        // Set custom as active
        await service.SetActiveProfileAsync(customProfile.Id);

        // Act - delete the active profile
        await service.DeleteProfileAsync(customProfile.Id);
        var activeAfterDelete = await service.GetActiveProfileAsync();

        // Assert - should reset to default (Technical)
        activeAfterDelete.Name.Should().Be("Technical");
    }

    #endregion

    #region ResetToDefaultAsync Tests

    [Fact]
    public async Task ResetToDefaultAsync_SetsActiveToTechnical()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VoiceProfile>());

        var service = CreateService();

        // First set to Marketing
        await service.SetActiveProfileAsync(BuiltInProfiles.Marketing.Id);

        // Act
        await service.ResetToDefaultAsync();
        var result = await service.GetActiveProfileAsync();

        // Assert
        result.Name.Should().Be("Technical");
    }

    #endregion

    #region Helper Methods

    private VoiceProfileService CreateService() =>
        new(_repositoryMock.Object, _licenseContextMock.Object, _mediatorMock.Object, _logger);

    #endregion
}
