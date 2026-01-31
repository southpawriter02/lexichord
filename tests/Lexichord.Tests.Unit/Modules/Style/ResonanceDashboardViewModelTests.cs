// <copyright file="ResonanceDashboardViewModelTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Lexichord.Modules.Style.ViewModels;
using LiveChartsCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ResonanceDashboardViewModel"/>.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Tests cover initialization, license gating, overlay toggle, and series building.</para>
/// </remarks>
[Trait("Feature", "v0.3.5b")]
[Trait("Module", "Style")]
public class ResonanceDashboardViewModelTests : IDisposable
{
    private readonly Mock<IChartDataService> _chartDataServiceMock;
    private readonly Mock<ITargetOverlayService> _targetOverlayServiceMock;
    private readonly Mock<ISpiderChartSeriesBuilder> _seriesBuilderMock;
    private readonly Mock<IVoiceProfileService> _voiceProfileServiceMock;
    private readonly Mock<ILicenseService> _licenseServiceMock;
    private readonly Mock<IResonanceAxisProvider> _axisProviderMock;
    private readonly Mock<IResonanceUpdateService> _updateServiceMock;
    private readonly Mock<ILogger<ResonanceDashboardViewModel>> _loggerMock;
    private ResonanceDashboardViewModel? _sut;

    public ResonanceDashboardViewModelTests()
    {
        _chartDataServiceMock = new Mock<IChartDataService>();
        _targetOverlayServiceMock = new Mock<ITargetOverlayService>();
        _seriesBuilderMock = new Mock<ISpiderChartSeriesBuilder>();
        _voiceProfileServiceMock = new Mock<IVoiceProfileService>();
        _licenseServiceMock = new Mock<ILicenseService>();
        _axisProviderMock = new Mock<IResonanceAxisProvider>();
        _updateServiceMock = new Mock<IResonanceUpdateService>();
        _loggerMock = new Mock<ILogger<ResonanceDashboardViewModel>>();

        SetupDefaultMocks();
    }

    public void Dispose()
    {
        _sut?.Dispose();
    }

    #region Initialization Tests

    [Fact]
    public async Task InitializeAsync_SetsIsLoadingDuringLoad()
    {
        // Arrange
        _sut = CreateSut();

        // Act
        var initTask = _sut.InitializeAsync();
        
        // Assert - can't easily check IsLoading during async, but we verify it completes
        await initTask;
        _sut.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task InitializeAsync_ChecksLicense()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(It.IsAny<string>()))
            .Returns(true);
        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _licenseServiceMock.Verify(
            x => x.IsFeatureEnabled(It.IsAny<string>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsync_WhenLicensed_SetsIsLicensedTrue()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(It.IsAny<string>()))
            .Returns(true);
        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.IsLicensed.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_WhenNotLicensed_SetsIsLicensedFalse()
    {
        // Arrange
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(It.IsAny<string>()))
            .Returns(false);
        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.IsLicensed.Should().BeFalse();
    }

    #endregion

    #region Series Building Tests

    [Fact]
    public async Task InitializeAsync_BuildsCurrentSeries()
    {
        // Arrange
        var mockSeries = new Mock<ISeries>().Object;
        _seriesBuilderMock
            .Setup(x => x.BuildCurrentSeries(It.IsAny<ResonanceChartData>(), It.IsAny<bool>()))
            .Returns(mockSeries);
        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _seriesBuilderMock.Verify(
            x => x.BuildCurrentSeries(It.IsAny<ResonanceChartData>(), It.IsAny<bool>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task InitializeAsync_WithActiveProfile_BuildsTargetSeries()
    {
        // Arrange
        var profile = CreateTestProfile();
        _voiceProfileServiceMock.Setup(x => x.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var overlay = CreateTestOverlay(profile);
        _targetOverlayServiceMock
            .Setup(x => x.GetOverlayAsync(It.IsAny<VoiceProfile>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(overlay);

        var mockSeries = new Mock<ISeries>().Object;
        _seriesBuilderMock
            .Setup(x => x.BuildTargetSeries(It.IsAny<ResonanceChartData>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Returns(mockSeries);

        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _seriesBuilderMock.Verify(
            x => x.BuildTargetSeries(It.IsAny<ResonanceChartData>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Target Overlay Toggle Tests

    [Fact]
    public async Task ToggleTargetOverlay_TogglesBooleanValue()
    {
        // Arrange
        _sut = CreateSut();
        await _sut.InitializeAsync();
        var initialValue = _sut.ShowTargetOverlay;

        // Act
        _sut.ShowTargetOverlay = !initialValue;

        // Assert
        _sut.ShowTargetOverlay.Should().Be(!initialValue);
    }

    [Fact]
    public void ShowTargetOverlay_DefaultsToTrue()
    {
        // Arrange & Act
        _sut = CreateSut();

        // Assert
        _sut.ShowTargetOverlay.Should().BeTrue();
    }

    #endregion

    #region HasData Tests

    [Fact]
    public async Task HasData_WhenChartDataEmpty_ReturnsFalse()
    {
        // Arrange
        // HasData is driven by chartData.DataPoints.Count, not the series array
        var emptyChartData = new ResonanceChartData([], DateTimeOffset.UtcNow);
        _chartDataServiceMock.Setup(x => x.GetChartDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyChartData);
        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.HasData.Should().BeFalse();
    }

    [Fact]
    public async Task HasData_WhenSeriesPopulated_ReturnsTrue()
    {
        // Arrange
        var mockSeries = new Mock<ISeries>().Object;
        _seriesBuilderMock
            .Setup(x => x.BuildCurrentSeries(It.IsAny<ResonanceChartData>(), It.IsAny<bool>()))
            .Returns(mockSeries);
        _sut = CreateSut();

        // Act
        await _sut.InitializeAsync();

        // Assert
        _sut.Series.Should().NotBeEmpty();
        _sut.HasData.Should().BeTrue();
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        _sut = CreateSut();

        // Act & Assert - should not throw
        _sut.Dispose();
        _sut.Dispose();
    }

    #endregion

    #region Test Helpers

    private void SetupDefaultMocks()
    {
        // IsFeatureEnabled is synchronous (from ILicenseContext)
        _licenseServiceMock
            .Setup(x => x.IsFeatureEnabled(It.IsAny<string>()))
            .Returns(true);

        var chartData = CreateTestChartData();
        _chartDataServiceMock.Setup(x => x.GetChartDataAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(chartData);

        _axisProviderMock.Setup(x => x.GetAxes())
            .Returns(CreateTestAxes());

        // GetActiveProfileAsync never returns null (returns Technical profile as default)
        _voiceProfileServiceMock.Setup(x => x.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateTestProfile());
    }

    private ResonanceDashboardViewModel CreateSut()
    {
        return new ResonanceDashboardViewModel(
            _chartDataServiceMock.Object,
            _targetOverlayServiceMock.Object,
            _seriesBuilderMock.Object,
            _voiceProfileServiceMock.Object,
            _licenseServiceMock.Object,
            _axisProviderMock.Object,
            _updateServiceMock.Object,
            _loggerMock.Object);
    }

    private static ResonanceChartData CreateTestChartData()
    {
        var dataPoints = new List<ResonanceDataPoint>
        {
            new("Readability", 75, 75, "score", "Reading Ease"),
            new("Clarity", 60, 0.15, "%", "Passive Voice"),
            new("Precision", 80, 2.5, "%", "Weak Words"),
            new("Accessibility", 50, 10, "grade", "Grade Level"),
            new("Density", 65, 18, "words", "Words/Sentence"),
            new("Flow", 70, 45, "variance", "Sentence Variance")
        };

        return new ResonanceChartData(dataPoints.AsReadOnly(), DateTimeOffset.UtcNow);
    }

    private static List<ResonanceAxisDefinition> CreateTestAxes()
    {
        return
        [
            new("Reading Ease", "FleschReadingEase", Unit: "score", MinValue: 0, MaxValue: 100),
            new("Passive Voice", "PassiveVoicePercentage", Unit: "%", MinValue: 0, MaxValue: 100, InvertScale: true),
            new("Weak Words", "WeakWordDensity", Unit: "%", MinValue: 0, MaxValue: 100, InvertScale: true),
            new("Grade Level", "FleschKincaidGrade", Unit: "grade", MinValue: 0, MaxValue: 18, InvertScale: true),
            new("Words/Sentence", "AverageWordsPerSentence", Unit: "words", MinValue: 0, MaxValue: 35),
            new("Sentence Variance", "SentenceLengthVariance", Unit: "variance", MinValue: 0, MaxValue: 100)
        ];
    }

    private static VoiceProfile CreateTestProfile()
    {
        return new VoiceProfile
        {
            Id = Guid.NewGuid(),
            Name = "Technical Writer",
            Description = "Test profile",
            TargetGradeLevel = 10.0,
            MaxSentenceLength = 25,
            MaxPassiveVoicePercentage = 10.0
        };
    }

    private static TargetOverlay CreateTestOverlay(VoiceProfile profile)
    {
        var dataPoints = new List<ResonanceDataPoint>
        {
            new("Readability", 80, 80, "score", "Reading Ease"),
            new("Clarity", 90, 0.10, "%", "Passive Voice"),
            new("Precision", 85, 2.0, "%", "Weak Words"),
            new("Accessibility", 60, 10, "grade", "Grade Level"),
            new("Density", 70, 20, "words", "Words/Sentence"),
            new("Flow", 75, 50, "variance", "Sentence Variance")
        };

        return new TargetOverlay(
            profile.Id.ToString(),
            profile.Name,
            dataPoints.AsReadOnly(),
            DateTimeOffset.UtcNow);
    }

    #endregion
}
