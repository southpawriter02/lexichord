// <copyright file="TargetOverlayServiceTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="TargetOverlayService"/>.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Tests cover overlay computation, caching, and cache invalidation.</para>
/// </remarks>
[Trait("Feature", "v0.3.5b")]
[Trait("Module", "Style")]
public class TargetOverlayServiceTests
{
    private readonly Mock<IResonanceAxisProvider> _axisProviderMock;
    private readonly Mock<ILogger<TargetOverlayService>> _loggerMock;
    private readonly TargetOverlayService _sut;

    public TargetOverlayServiceTests()
    {
        _axisProviderMock = new Mock<IResonanceAxisProvider>();
        _loggerMock = new Mock<ILogger<TargetOverlayService>>();

        SetupDefaultAxes();

        _sut = new TargetOverlayService(_axisProviderMock.Object, _loggerMock.Object);
    }

    #region GetOverlayAsync Tests

    [Fact]
    public async Task GetOverlayAsync_WithValidProfile_ReturnsOverlay()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Act
        var result = await _sut.GetOverlayAsync(profile);

        // Assert
        result.Should().NotBeNull();
        result!.ProfileId.Should().Be(profile.Id.ToString());
        result.ProfileName.Should().Be(profile.Name);
        result.DataPoints.Should().HaveCount(6);
        result.HasData.Should().BeTrue();
    }

    [Fact]
    public async Task GetOverlayAsync_ComputesNormalizedValues()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Act
        var result = await _sut.GetOverlayAsync(profile);

        // Assert
        result.Should().NotBeNull();
        foreach (var point in result!.DataPoints)
        {
            point.NormalizedValue.Should().BeInRange(0, 100);
        }
    }

    [Fact]
    public async Task GetOverlayAsync_CachesResult()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Act
        var result1 = await _sut.GetOverlayAsync(profile);
        var result2 = await _sut.GetOverlayAsync(profile);

        // Assert
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    public async Task GetOverlayAsync_DifferentProfiles_ReturnsDifferentOverlays()
    {
        // Arrange
        var profile1 = CreateTestProfile("Profile 1");
        var profile2 = CreateTestProfile("Profile 2");

        // Act
        var result1 = await _sut.GetOverlayAsync(profile1);
        var result2 = await _sut.GetOverlayAsync(profile2);

        // Assert
        result1!.ProfileId.Should().NotBe(result2!.ProfileId);
    }

    [Fact]
    public async Task GetOverlayAsync_SetsComputedTimestamp()
    {
        // Arrange
        var profile = CreateTestProfile();
        var before = DateTimeOffset.UtcNow;

        // Act
        var result = await _sut.GetOverlayAsync(profile);
        var after = DateTimeOffset.UtcNow;

        // Assert
        result!.ComputedAt.Should().BeOnOrAfter(before);
        result.ComputedAt.Should().BeOnOrBefore(after);
    }

    [Fact]
    public async Task GetOverlayAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        var profile = CreateTestProfile();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.GetOverlayAsync(profile, cts.Token));
    }

    #endregion

    #region Cache Invalidation Tests

    [Fact]
    public async Task InvalidateCache_RemovesCachedOverlay()
    {
        // Arrange
        var profile = CreateTestProfile();
        var result1 = await _sut.GetOverlayAsync(profile);

        // Act
        _sut.InvalidateCache(profile.Id.ToString());
        var result2 = await _sut.GetOverlayAsync(profile);

        // Assert
        result1.Should().NotBeSameAs(result2);
    }

    [Fact]
    public async Task InvalidateAllCaches_RemovesAllCachedOverlays()
    {
        // Arrange
        var profile1 = CreateTestProfile("Profile 1");
        var profile2 = CreateTestProfile("Profile 2");
        await _sut.GetOverlayAsync(profile1);
        await _sut.GetOverlayAsync(profile2);

        // Act
        _sut.InvalidateAllCaches();
        var result1 = await _sut.GetOverlayAsync(profile1);
        var result2 = await _sut.GetOverlayAsync(profile2);

        // Assert
        // New overlays should be computed (different timestamps)
        result1.Should().NotBeNull();
        result2.Should().NotBeNull();
    }

    [Fact]
    public void InvalidateCache_WithNonExistentProfile_DoesNotThrow()
    {
        // Act & Assert
        var action = () => _sut.InvalidateCache(Guid.NewGuid().ToString());
        action.Should().NotThrow();
    }

    #endregion

    #region v0.3.5d GetOverlaySync Tests

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void GetOverlaySync_WithValidProfile_ReturnsOverlay()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Act
        var result = _sut.GetOverlaySync(profile);

        // Assert
        result.Should().NotBeNull();
        result!.ProfileId.Should().Be(profile.Id.ToString());
        result.ProfileName.Should().Be(profile.Name);
    }

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void GetOverlaySync_ReturnsCachedOverlay()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Act
        var result1 = _sut.GetOverlaySync(profile);
        var result2 = _sut.GetOverlaySync(profile);

        // Assert
        result1.Should().BeSameAs(result2);
    }

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public async Task GetOverlaySync_SharesCacheWithAsync()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Pre-populate cache via async method
        var asyncResult = await _sut.GetOverlayAsync(profile);

        // Act - sync should return same cached instance
        var syncResult = _sut.GetOverlaySync(profile);

        // Assert
        asyncResult.Should().BeSameAs(syncResult);
    }

    #endregion

    #region v0.3.5d TargetDataPoint Tests

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void TargetDataPoint_HasToleranceBand_WhenBothBoundsSet()
    {
        // Arrange
        var dataPoint = new TargetDataPoint(
            AxisName: "Test",
            NormalizedValue: 50,
            RawValue: 50,
            ToleranceMin: 40,
            ToleranceMax: 60);

        // Act & Assert
        dataPoint.HasToleranceBand.Should().BeTrue();
    }

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void TargetDataPoint_NoToleranceBand_WhenMissingMin()
    {
        // Arrange
        var dataPoint = new TargetDataPoint(
            AxisName: "Test",
            NormalizedValue: 50,
            RawValue: 50,
            ToleranceMax: 60);

        // Act & Assert
        dataPoint.HasToleranceBand.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void TargetDataPoint_NoToleranceBand_WhenMissingMax()
    {
        // Arrange
        var dataPoint = new TargetDataPoint(
            AxisName: "Test",
            NormalizedValue: 50,
            RawValue: 50,
            ToleranceMin: 40);

        // Act & Assert
        dataPoint.HasToleranceBand.Should().BeFalse();
    }

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void TargetOverlay_HasAnyToleranceBands_ReturnsTrueWhenPresent()
    {
        // Arrange
        var overlay = new TargetOverlay(
            ProfileId: Guid.NewGuid().ToString(),
            ProfileName: "Test",
            DataPoints:
            [
                new TargetDataPoint("Axis1", 50, 50),
                new TargetDataPoint("Axis2", 60, 60, ToleranceMin: 50, ToleranceMax: 70)
            ],
            ComputedAt: DateTimeOffset.UtcNow);

        // Act & Assert
        overlay.HasAnyToleranceBands.Should().BeTrue();
    }

    [Fact]
    [Trait("Feature", "v0.3.5d")]
    public void TargetOverlay_HasAnyToleranceBands_ReturnsFalseWhenNone()
    {
        // Arrange
        var overlay = new TargetOverlay(
            ProfileId: Guid.NewGuid().ToString(),
            ProfileName: "Test",
            DataPoints:
            [
                new TargetDataPoint("Axis1", 50, 50),
                new TargetDataPoint("Axis2", 60, 60)
            ],
            ComputedAt: DateTimeOffset.UtcNow);

        // Act & Assert
        overlay.HasAnyToleranceBands.Should().BeFalse();
    }

    #endregion

    #region Test Helpers

    private void SetupDefaultAxes()
    {
        var axes = new List<ResonanceAxisDefinition>
        {
            new("Reading Ease", "FleschReadingEase", Unit: "score", MinValue: 0, MaxValue: 100),
            new("Passive Voice", "PassiveVoicePercentage", Unit: "%", MinValue: 0, MaxValue: 100, InvertScale: true),
            new("Weak Words", "WeakWordDensity", Unit: "%", MinValue: 0, MaxValue: 100, InvertScale: true),
            new("Grade Level", "FleschKincaidGrade", Unit: "grade", MinValue: 0, MaxValue: 18, InvertScale: true),
            new("Words/Sentence", "AverageWordsPerSentence", Unit: "words", MinValue: 0, MaxValue: 35),
            new("Sentence Variance", "SentenceLengthVariance", Unit: "variance", MinValue: 0, MaxValue: 100)
        };

        _axisProviderMock.Setup(x => x.GetAxes()).Returns(axes);
    }

    private static VoiceProfile CreateTestProfile(string name = "Test Profile")
    {
        return new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "Test profile for unit tests",
            TargetGradeLevel = 10.0,
            MaxSentenceLength = 25,
            MaxPassiveVoicePercentage = 10.0,
            FlagAdverbs = true,
            FlagWeaselWords = true
        };
    }

    #endregion
}
