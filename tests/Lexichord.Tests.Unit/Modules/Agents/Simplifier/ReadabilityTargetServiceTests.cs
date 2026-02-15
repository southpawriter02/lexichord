// -----------------------------------------------------------------------
// <copyright file="ReadabilityTargetServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.Simplifier;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="ReadabilityTargetService"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4a")]
public class ReadabilityTargetServiceTests
{
    private readonly Mock<IVoiceProfileService> _voiceProfileServiceMock;
    private readonly Mock<IReadabilityService> _readabilityServiceMock;
    private readonly Mock<ISettingsService> _settingsServiceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly ILogger<ReadabilityTargetService> _logger;
    private readonly ReadabilityTargetService _sut;

    public ReadabilityTargetServiceTests()
    {
        _voiceProfileServiceMock = new Mock<IVoiceProfileService>();
        _readabilityServiceMock = new Mock<IReadabilityService>();
        _settingsServiceMock = new Mock<ISettingsService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _logger = NullLogger<ReadabilityTargetService>.Instance;

        // LOGIC: Default setup - no custom presets stored
        _settingsServiceMock
            .Setup(s => s.Get(ReadabilityTargetService.CustomPresetsSettingsKey, It.IsAny<List<AudiencePreset>>()))
            .Returns(new List<AudiencePreset>());

        // LOGIC: Default setup - WriterPro tier for custom preset tests
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.CustomAudiencePresets))
            .Returns(true);

        // LOGIC: Default voice profile with grade level configured
        var defaultProfile = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Technical",
            TargetGradeLevel = 11.0,
            GradeLevelTolerance = 2.0,
            MaxSentenceLength = 25
        };
        _voiceProfileServiceMock
            .Setup(v => v.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultProfile);

        _sut = new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _logger);
    }

    // ── Constructor Validation Tests ────────────────────────────────────

    [Fact]
    public void Constructor_NullVoiceProfileService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReadabilityTargetService(
            null!,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("voiceProfileService");
    }

    [Fact]
    public void Constructor_NullReadabilityService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            null!,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readabilityService");
    }

    [Fact]
    public void Constructor_NullSettingsService_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            null!,
            _licenseContextMock.Object,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("settingsService");
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            null!,
            _logger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act
        var act = () => new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── GetAllPresetsAsync Tests ────────────────────────────────────────

    [Fact]
    public async Task GetAllPresetsAsync_NoCustomPresets_ReturnsBuiltInOnly()
    {
        // Act
        var presets = await _sut.GetAllPresetsAsync();

        // Assert
        presets.Should().HaveCount(4);
        presets.Should().AllSatisfy(p => p.IsBuiltIn.Should().BeTrue());
        presets.Select(p => p.Id).Should().Contain(BuiltInPresets.GeneralPublicId);
        presets.Select(p => p.Id).Should().Contain(BuiltInPresets.TechnicalId);
        presets.Select(p => p.Id).Should().Contain(BuiltInPresets.ExecutiveId);
        presets.Select(p => p.Id).Should().Contain(BuiltInPresets.InternationalId);
    }

    [Fact]
    public async Task GetAllPresetsAsync_WithCustomPresets_ReturnsAllOrdered()
    {
        // Arrange
        var customPreset = new AudiencePreset(
            Id: "custom-test",
            Name: "Custom Test",
            TargetGradeLevel: 9.0,
            MaxSentenceLength: 22,
            AvoidJargon: true,
            IsBuiltIn: false);

        _settingsServiceMock
            .Setup(s => s.Get(ReadabilityTargetService.CustomPresetsSettingsKey, It.IsAny<List<AudiencePreset>>()))
            .Returns(new List<AudiencePreset> { customPreset });

        var sut = new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _logger);

        // Act
        var presets = await sut.GetAllPresetsAsync();

        // Assert
        presets.Should().HaveCount(5);
        presets.Take(4).Should().AllSatisfy(p => p.IsBuiltIn.Should().BeTrue());
        presets.Last().Id.Should().Be("custom-test");
    }

    // ── GetPresetByIdAsync Tests ────────────────────────────────────────

    [Fact]
    public async Task GetPresetByIdAsync_BuiltInId_ReturnsPreset()
    {
        // Act
        var preset = await _sut.GetPresetByIdAsync(BuiltInPresets.GeneralPublicId);

        // Assert
        preset.Should().NotBeNull();
        preset!.Id.Should().Be(BuiltInPresets.GeneralPublicId);
        preset.Name.Should().Be("General Public");
        preset.TargetGradeLevel.Should().Be(8.0);
    }

    [Fact]
    public async Task GetPresetByIdAsync_CaseInsensitive_ReturnsPreset()
    {
        // Act
        var preset = await _sut.GetPresetByIdAsync("GENERAL-PUBLIC");

        // Assert
        preset.Should().NotBeNull();
        preset!.Id.Should().Be(BuiltInPresets.GeneralPublicId);
    }

    [Fact]
    public async Task GetPresetByIdAsync_UnknownId_ReturnsNull()
    {
        // Act
        var preset = await _sut.GetPresetByIdAsync("unknown-preset");

        // Assert
        preset.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetPresetByIdAsync_EmptyId_ThrowsArgumentException(string? presetId)
    {
        // Act
        var act = () => _sut.GetPresetByIdAsync(presetId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("presetId");
    }

    // ── GetTargetAsync Tests ────────────────────────────────────────────

    [Fact]
    public async Task GetTargetAsync_ExplicitGradeLevel_ReturnsExplicitTarget()
    {
        // Act
        var target = await _sut.GetTargetAsync(targetGradeLevel: 6.0);

        // Assert
        target.TargetGradeLevel.Should().Be(6.0);
        target.Source.Should().Be(ReadabilityTargetSource.Explicit);
        target.SourcePresetId.Should().BeNull();
    }

    [Fact]
    public async Task GetTargetAsync_ExplicitWithAllParams_ReturnsExplicitTarget()
    {
        // Act
        var target = await _sut.GetTargetAsync(
            targetGradeLevel: 7.0,
            maxSentenceLength: 15,
            avoidJargon: false);

        // Assert
        target.TargetGradeLevel.Should().Be(7.0);
        target.MaxSentenceLength.Should().Be(15);
        target.AvoidJargon.Should().BeFalse();
        target.Source.Should().Be(ReadabilityTargetSource.Explicit);
    }

    [Fact]
    public async Task GetTargetAsync_PresetId_ReturnsPresetTarget()
    {
        // Act
        var target = await _sut.GetTargetAsync(presetId: BuiltInPresets.TechnicalId);

        // Assert
        target.TargetGradeLevel.Should().Be(12.0);
        target.MaxSentenceLength.Should().Be(25);
        target.AvoidJargon.Should().BeFalse();
        target.Source.Should().Be(ReadabilityTargetSource.Preset);
        target.SourcePresetId.Should().Be(BuiltInPresets.TechnicalId);
    }

    [Fact]
    public async Task GetTargetAsync_PresetWithOverrides_OverridesSpecificProperties()
    {
        // Act
        var target = await _sut.GetTargetAsync(
            presetId: BuiltInPresets.GeneralPublicId,
            maxSentenceLength: 12);

        // Assert
        target.TargetGradeLevel.Should().Be(8.0); // From preset
        target.MaxSentenceLength.Should().Be(12); // Override
        target.AvoidJargon.Should().BeTrue();     // From preset
        target.Source.Should().Be(ReadabilityTargetSource.Preset);
    }

    [Fact]
    public async Task GetTargetAsync_UnknownPreset_FallsBackToProfile()
    {
        // Arrange - default profile has TargetGradeLevel 11.0

        // Act
        var target = await _sut.GetTargetAsync(presetId: "unknown-preset");

        // Assert
        target.TargetGradeLevel.Should().Be(11.0);
        target.Source.Should().Be(ReadabilityTargetSource.VoiceProfile);
    }

    [Fact]
    public async Task GetTargetAsync_NoParams_UsesVoiceProfile()
    {
        // Arrange - default profile has TargetGradeLevel 11.0

        // Act
        var target = await _sut.GetTargetAsync();

        // Assert
        target.TargetGradeLevel.Should().Be(11.0);
        target.GradeLevelTolerance.Should().Be(2.0);
        target.MaxSentenceLength.Should().Be(25);
        target.Source.Should().Be(ReadabilityTargetSource.VoiceProfile);
    }

    [Fact]
    public async Task GetTargetAsync_NoProfileGradeLevel_UsesDefaults()
    {
        // Arrange
        var profileWithoutGrade = new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "No Grade",
            TargetGradeLevel = null // No grade level configured
        };
        _voiceProfileServiceMock
            .Setup(v => v.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profileWithoutGrade);

        var sut = new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _logger);

        // Act
        var target = await sut.GetTargetAsync();

        // Assert
        target.TargetGradeLevel.Should().Be(8.0); // Default
        target.MaxSentenceLength.Should().Be(20); // Default
        target.AvoidJargon.Should().BeTrue();     // Default
        target.Source.Should().Be(ReadabilityTargetSource.Default);
    }

    // ── ValidateTarget Tests ────────────────────────────────────────────

    [Fact]
    public async Task ValidateTarget_NullSourceText_ThrowsArgumentNullException()
    {
        // Arrange
        var target = ReadabilityTarget.FromPreset(BuiltInPresets.GeneralPublic);

        // Act
        var act = () => _sut.ValidateTarget(null!, target);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("sourceText");
    }

    [Fact]
    public async Task ValidateTarget_NullTarget_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.ValidateTarget("Some text", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("target");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ValidateTarget_EmptyText_ThrowsArgumentException(string text)
    {
        // Arrange
        var target = ReadabilityTarget.FromPreset(BuiltInPresets.GeneralPublic);

        // Act
        var act = () => _sut.ValidateTarget(text, target);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("sourceText");
    }

    [Fact]
    public async Task ValidateTarget_SourceBelowTarget_ReturnsAlreadyMet()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 6.0,
            WordCount = 100,
            SentenceCount = 10
        };
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var target = ReadabilityTarget.FromPreset(BuiltInPresets.GeneralPublic); // Grade 8

        // Act
        var result = await _sut.ValidateTarget("Test text", target);

        // Assert
        result.Achievability.Should().Be(TargetAchievability.AlreadyMet);
        result.IsAlreadyMet.Should().BeTrue();
        result.IsAchievable.Should().BeTrue();
        result.SourceGradeLevel.Should().Be(6.0);
        result.TargetGradeLevel.Should().Be(8.0);
    }

    [Fact]
    public async Task ValidateTarget_SmallDelta_ReturnsAchievable()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 10.0,
            WordCount = 100,
            SentenceCount = 10,
            ComplexWordCount = 10 // 10% complex words
        };
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var target = ReadabilityTarget.FromPreset(BuiltInPresets.GeneralPublic); // Grade 8

        // Act
        var result = await _sut.ValidateTarget("Test text", target);

        // Assert
        result.Achievability.Should().Be(TargetAchievability.Achievable);
        result.IsAchievable.Should().BeTrue();
        result.GradeLevelDelta.Should().Be(2.0);
        result.HasWarnings.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTarget_ModerateDelta_ReturnsChallenging()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 13.0,
            WordCount = 100,
            SentenceCount = 10,
            ComplexWordCount = 35 // 35% complex words - triggers warning
        };
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var target = ReadabilityTarget.FromPreset(BuiltInPresets.GeneralPublic); // Grade 8

        // Act
        var result = await _sut.ValidateTarget("Test text", target);

        // Assert
        result.Achievability.Should().Be(TargetAchievability.Challenging);
        result.IsAchievable.Should().BeTrue();
        result.GradeLevelDelta.Should().Be(5.0);
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Contains("complex word"));
    }

    [Fact]
    public async Task ValidateTarget_LargeDelta_ReturnsUnlikely()
    {
        // Arrange
        var metrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 16.0,
            WordCount = 100,
            SentenceCount = 3, // Very long sentences (~33 words each)
            ComplexWordCount = 40 // 40% complex words
        };
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var target = ReadabilityTarget.FromPreset(BuiltInPresets.International); // Grade 6

        // Act
        var result = await _sut.ValidateTarget("Test text", target);

        // Assert
        result.Achievability.Should().Be(TargetAchievability.Unlikely);
        result.IsAchievable.Should().BeFalse();
        result.GradeLevelDelta.Should().Be(10.0);
        result.HasWarnings.Should().BeTrue();
        result.SuggestedPreset.Should().NotBeNull();
    }

    [Fact]
    public async Task ValidateTarget_SuggestsAppropriatePreset()
    {
        // Arrange - Grade 16 source should suggest Technical preset
        var metrics = new ReadabilityMetrics
        {
            FleschKincaidGradeLevel = 16.0,
            WordCount = 100,
            SentenceCount = 5,
            ComplexWordCount = 40
        };
        _readabilityServiceMock
            .Setup(r => r.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var target = ReadabilityTarget.FromPreset(BuiltInPresets.International); // Grade 6

        // Act
        var result = await _sut.ValidateTarget("Test text", target);

        // Assert
        result.SuggestedPreset.Should().Be(BuiltInPresets.TechnicalId);
    }

    // ── CreateCustomPresetAsync Tests ───────────────────────────────────

    [Fact]
    public async Task CreateCustomPresetAsync_ValidPreset_CreatesAndPersists()
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "custom-new",
            Name: "Custom New",
            TargetGradeLevel: 9.0,
            MaxSentenceLength: 22,
            AvoidJargon: true);

        // Act
        var created = await _sut.CreateCustomPresetAsync(preset);

        // Assert
        created.Id.Should().Be("custom-new");
        created.IsBuiltIn.Should().BeFalse();

        _settingsServiceMock.Verify(
            s => s.Set(ReadabilityTargetService.CustomPresetsSettingsKey, It.IsAny<List<AudiencePreset>>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateCustomPresetAsync_NullPreset_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.CreateCustomPresetAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("preset");
    }

    [Fact]
    public async Task CreateCustomPresetAsync_InvalidPreset_ThrowsArgumentException()
    {
        // Arrange
        var invalid = new AudiencePreset(
            Id: "", // Invalid
            Name: "Test",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var act = () => _sut.CreateCustomPresetAsync(invalid);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("preset");
    }

    [Fact]
    public async Task CreateCustomPresetAsync_DuplicateBuiltInId_ThrowsInvalidOperationException()
    {
        // Arrange
        var duplicate = new AudiencePreset(
            Id: BuiltInPresets.GeneralPublicId,
            Name: "Duplicate",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var act = () => _sut.CreateCustomPresetAsync(duplicate);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task CreateCustomPresetAsync_DuplicateCustomId_ThrowsInvalidOperationException()
    {
        // Arrange
        var first = new AudiencePreset(
            Id: "duplicate-test",
            Name: "First",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);
        await _sut.CreateCustomPresetAsync(first);

        var second = new AudiencePreset(
            Id: "duplicate-test",
            Name: "Second",
            TargetGradeLevel: 9.0,
            MaxSentenceLength: 22,
            AvoidJargon: false);

        // Act
        var act = () => _sut.CreateCustomPresetAsync(second);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task CreateCustomPresetAsync_NoLicense_ThrowsLicenseTierException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.CustomAudiencePresets))
            .Returns(false);

        var sut = new ReadabilityTargetService(
            _voiceProfileServiceMock.Object,
            _readabilityServiceMock.Object,
            _settingsServiceMock.Object,
            _licenseContextMock.Object,
            _logger);

        var preset = new AudiencePreset(
            Id: "custom",
            Name: "Custom",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var act = () => sut.CreateCustomPresetAsync(preset);

        // Assert
        await act.Should().ThrowAsync<LicenseTierException>()
            .Where(e => e.RequiredTier == LicenseTier.WriterPro);
    }

    // ── UpdateCustomPresetAsync Tests ───────────────────────────────────

    [Fact]
    public async Task UpdateCustomPresetAsync_ExistingPreset_UpdatesAndPersists()
    {
        // Arrange
        var original = new AudiencePreset(
            Id: "update-test",
            Name: "Original",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);
        await _sut.CreateCustomPresetAsync(original);

        var updated = original with { Name = "Updated", MaxSentenceLength = 25 };

        // Act
        var result = await _sut.UpdateCustomPresetAsync(updated);

        // Assert
        result.Name.Should().Be("Updated");
        result.MaxSentenceLength.Should().Be(25);

        _settingsServiceMock.Verify(
            s => s.Set(ReadabilityTargetService.CustomPresetsSettingsKey, It.IsAny<List<AudiencePreset>>()),
            Times.Exactly(2)); // Once for create, once for update
    }

    [Fact]
    public async Task UpdateCustomPresetAsync_BuiltInPreset_ThrowsInvalidOperationException()
    {
        // Arrange
        var builtIn = BuiltInPresets.GeneralPublic with { Name = "Modified" };

        // Act
        var act = () => _sut.UpdateCustomPresetAsync(builtIn);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Fact]
    public async Task UpdateCustomPresetAsync_NonExistentPreset_ThrowsInvalidOperationException()
    {
        // Arrange
        var nonExistent = new AudiencePreset(
            Id: "non-existent",
            Name: "Test",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);

        // Act
        var act = () => _sut.UpdateCustomPresetAsync(nonExistent);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*not found*");
    }

    // ── DeleteCustomPresetAsync Tests ───────────────────────────────────

    [Fact]
    public async Task DeleteCustomPresetAsync_ExistingPreset_DeletesAndPersists()
    {
        // Arrange
        var preset = new AudiencePreset(
            Id: "delete-test",
            Name: "To Delete",
            TargetGradeLevel: 8.0,
            MaxSentenceLength: 20,
            AvoidJargon: true);
        await _sut.CreateCustomPresetAsync(preset);

        // Act
        var deleted = await _sut.DeleteCustomPresetAsync("delete-test");

        // Assert
        deleted.Should().BeTrue();

        var found = await _sut.GetPresetByIdAsync("delete-test");
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCustomPresetAsync_NonExistentPreset_ReturnsFalse()
    {
        // Act
        var deleted = await _sut.DeleteCustomPresetAsync("non-existent");

        // Assert
        deleted.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCustomPresetAsync_BuiltInPreset_ThrowsInvalidOperationException()
    {
        // Act
        var act = () => _sut.DeleteCustomPresetAsync(BuiltInPresets.GeneralPublicId);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*built-in*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteCustomPresetAsync_EmptyId_ThrowsArgumentException(string? presetId)
    {
        // Act
        var act = () => _sut.DeleteCustomPresetAsync(presetId!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithParameterName("presetId");
    }
}
