// <copyright file="ReadabilityHudViewModelTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.ViewModels;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for ReadabilityHudViewModel (v0.3.3d).
/// </summary>
/// <remarks>
/// LOGIC: Tests cover:
/// - Constructor validation and initialization
/// - UpdateAsync behavior with readability analysis
/// - Color mapping for all metrics
/// - Grade level interpretation
/// - License gating behavior
/// - Reset functionality
/// - PropertyChanged notifications
/// </remarks>
public sealed class ReadabilityHudViewModelTests
{
    #region Test Fixtures

    private readonly Mock<IReadabilityService> _mockReadabilityService;
    private readonly Mock<ILicenseService> _mockLicenseService;
    private readonly Mock<ILogger<ReadabilityHudViewModel>> _mockLogger;

    public ReadabilityHudViewModelTests()
    {
        _mockReadabilityService = new Mock<IReadabilityService>();
        _mockLicenseService = new Mock<ILicenseService>();
        _mockLogger = new Mock<ILogger<ReadabilityHudViewModel>>();
    }

    private ReadabilityHudViewModel CreateViewModel(bool isLicensed = true)
    {
        _mockLicenseService
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.ReadabilityHud))
            .Returns(isLicensed);

        return new ReadabilityHudViewModel(
            _mockReadabilityService.Object,
            _mockLicenseService.Object,
            _mockLogger.Object);
    }

    private ReadabilityMetrics CreateMetrics(
        double gradeLevel = 8.0,
        double fogIndex = 10.0,
        double readingEase = 65.0,
        int wordCount = 100,
        int sentenceCount = 5,
        int syllableCount = 150,
        int complexWordCount = 10) =>
            new()
            {
                FleschKincaidGradeLevel = gradeLevel,
                GunningFogIndex = fogIndex,
                FleschReadingEase = readingEase,
                WordCount = wordCount,
                SentenceCount = sentenceCount,
                SyllableCount = syllableCount,
                ComplexWordCount = complexWordCount
            };

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_InitializesWithNoMetrics()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        vm.HasMetrics.Should().BeFalse();
        vm.IsAnalyzing.Should().BeFalse();
    }

    [Fact]
    public void Constructor_InitializesZeroMetricValues()
    {
        // Arrange & Act
        var vm = CreateViewModel();

        // Assert
        vm.FleschKincaidGradeLevel.Should().Be(0);
        vm.GunningFogIndex.Should().Be(0);
        vm.FleschReadingEase.Should().Be(0);
        vm.WordCount.Should().Be(0);
        vm.SentenceCount.Should().Be(0);
        vm.WordsPerSentence.Should().Be(0);
    }

    [Fact]
    public void Constructor_ChecksLicenseStatus()
    {
        // Arrange & Act
        var vm = CreateViewModel(isLicensed: true);

        // Assert
        vm.IsLicensed.Should().BeTrue();
        _mockLicenseService.Verify(
            x => x.IsFeatureEnabled(FeatureCodes.ReadabilityHud),
            Times.Once);
    }

    [Fact]
    public void Constructor_ThrowsOnNullReadabilityService()
    {
        // Arrange, Act & Assert
        var act = () => new ReadabilityHudViewModel(
            null!,
            _mockLicenseService.Object,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("readabilityService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLicenseService()
    {
        // Arrange, Act & Assert
        var act = () => new ReadabilityHudViewModel(
            _mockReadabilityService.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Arrange, Act & Assert
        var act = () => new ReadabilityHudViewModel(
            _mockReadabilityService.Object,
            _mockLicenseService.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_UpdatesAllMetricProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(
            gradeLevel: 8.5,
            fogIndex: 10.2,
            readingEase: 65.3,
            wordCount: 150,
            sentenceCount: 10);

        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text for analysis");

        // Assert
        vm.FleschKincaidGradeLevel.Should().Be(8.5);
        vm.GunningFogIndex.Should().Be(10.2);
        vm.FleschReadingEase.Should().Be(65.3);
        vm.WordCount.Should().Be(150);
        vm.SentenceCount.Should().Be(10);
        vm.HasMetrics.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAsync_SetsHasMetricsFalse_WhenWordCountIsZero()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(wordCount: 0);

        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("  ");

        // Assert - empty text causes Reset
        vm.HasMetrics.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ResetsMetrics_WhenTextIsEmpty()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics();
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        await vm.UpdateAsync("Sample text");

        // Act
        await vm.UpdateAsync("");

        // Assert
        vm.HasMetrics.Should().BeFalse();
        vm.FleschKincaidGradeLevel.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAsync_ResetsMetrics_WhenTextIsNull()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics();
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        await vm.UpdateAsync("Sample text");

        // Act
        await vm.UpdateAsync(null!);

        // Assert
        vm.HasMetrics.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_SetsIsAnalyzingDuringOperation()
    {
        // Arrange
        var vm = CreateViewModel();
        var tcs = new TaskCompletionSource<ReadabilityMetrics>();
        bool wasAnalyzing = false;

        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                wasAnalyzing = vm.IsAnalyzing;
                await Task.Delay(10); // Small delay
                return CreateMetrics();
            });

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        wasAnalyzing.Should().BeTrue();
        vm.IsAnalyzing.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ThrowsOnCancellation()
    {
        // Arrange
        var vm = CreateViewModel();
        var cts = new CancellationTokenSource();

        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken ct) =>
            {
                cts.Cancel();
                ct.ThrowIfCancellationRequested();
                return CreateMetrics();
            });

        // Act & Assert
        await vm.Invoking(async v => await v.UpdateAsync("Sample text", cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task UpdateAsync_HandlesServiceException()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics();
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        await vm.UpdateAsync("Sample text");
        vm.HasMetrics.Should().BeTrue();

        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        // Act - should not throw
        await vm.UpdateAsync("New text causing exception");

        // Assert - metrics should be reset after exception
        vm.HasMetrics.Should().BeFalse();
        vm.IsAnalyzing.Should().BeFalse();
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public void Constructor_IsLicensedFalse_WhenFeatureNotEnabled()
    {
        // Arrange & Act
        var vm = CreateViewModel(isLicensed: false);

        // Assert
        vm.IsLicensed.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_Skip_WhenNotLicensed()
    {
        // Arrange
        var vm = CreateViewModel(isLicensed: false);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        _mockReadabilityService.Verify(
            x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        vm.HasMetrics.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_RefreshesLicenseStatus()
    {
        // Arrange
        var vm = CreateViewModel(isLicensed: true);

        // Simulate license becoming invalid
        _mockLicenseService
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.ReadabilityHud))
            .Returns(false);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.IsLicensed.Should().BeFalse();
        _mockReadabilityService.Verify(
            x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Display Property Tests

    [Fact]
    public async Task GradeLevelDisplay_FormatsWithOneDecimal()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(gradeLevel: 8.56);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.GradeLevelDisplay.Should().Be("8.6");
    }

    [Fact]
    public void GradeLevelDisplay_ReturnsDash_WhenNoMetrics()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.GradeLevelDisplay.Should().Be("--");
    }

    [Fact]
    public async Task FogIndexDisplay_FormatsWithOneDecimal()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(fogIndex: 12.34);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.FogIndexDisplay.Should().Be("12.3");
    }

    [Fact]
    public async Task ReadingEaseDisplay_FormatsAsInteger()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(readingEase: 72.7);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.ReadingEaseDisplay.Should().Be("73");
    }

    #endregion

    #region Interpretation Tests

    [Theory]
    [InlineData(95, "Very Easy")]
    [InlineData(90, "Very Easy")]
    [InlineData(85, "Easy")]
    [InlineData(80, "Easy")]
    [InlineData(75, "Fairly Easy")]
    [InlineData(70, "Fairly Easy")]
    [InlineData(65, "Standard")]
    [InlineData(60, "Standard")]
    [InlineData(55, "Fairly Difficult")]
    [InlineData(50, "Fairly Difficult")]
    [InlineData(35, "Difficult")]
    [InlineData(30, "Difficult")]
    [InlineData(20, "Very Confusing")]
    [InlineData(10, "Very Confusing")]
    public async Task InterpretationDisplay_ReturnsCorrectCategory(
        double readingEase, string expectedInterpretation)
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(readingEase: readingEase);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.InterpretationDisplay.Should().Be(expectedInterpretation);
    }

    [Fact]
    public void InterpretationDisplay_ReturnsNoData_WhenNoMetrics()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.InterpretationDisplay.Should().Be("No Data");
    }

    [Theory]
    [InlineData(3, "Elementary")]
    [InlineData(5, "Elementary")]
    [InlineData(6, "Middle School")]
    [InlineData(8, "Middle School")]
    [InlineData(9, "High School")]
    [InlineData(12, "High School")]
    [InlineData(13, "College")]
    [InlineData(16, "College")]
    [InlineData(17, "Graduate")]
    [InlineData(20, "Graduate")]
    public async Task GradeLevelDescription_ReturnsCorrectLevel(
        double gradeLevel, string expectedDescription)
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(gradeLevel: gradeLevel);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.GradeLevelDescription.Should().Be(expectedDescription);
    }

    #endregion

    #region Color Mapping Tests

    [Theory]
    [InlineData(5, "#22C55E")]   // 0-6: Green (Easy)
    [InlineData(6, "#22C55E")]
    [InlineData(7, "#84CC16")]   // 7-9: Light Green
    [InlineData(9, "#84CC16")]
    [InlineData(10, "#EAB308")]  // 10-12: Yellow
    [InlineData(12, "#EAB308")]
    [InlineData(13, "#F97316")]  // 13-15: Orange
    [InlineData(15, "#F97316")]
    [InlineData(16, "#EF4444")]  // 16+: Red
    [InlineData(20, "#EF4444")]
    public async Task GradeLevelColor_ReturnsCorrectColor(
        double gradeLevel, string expectedColor)
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(gradeLevel: gradeLevel);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.GradeLevelColor.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData(90, "#22C55E")]  // 80-100: Green (Very Easy)
    [InlineData(80, "#22C55E")]
    [InlineData(70, "#84CC16")]  // 60-79: Light Green
    [InlineData(60, "#84CC16")]
    [InlineData(50, "#EAB308")]  // 40-59: Yellow
    [InlineData(40, "#EAB308")]
    [InlineData(30, "#F97316")]  // 20-39: Orange
    [InlineData(20, "#F97316")]
    [InlineData(15, "#EF4444")]  // 0-19: Red
    [InlineData(5, "#EF4444")]
    public async Task ReadingEaseColor_ReturnsCorrectColor(
        double readingEase, string expectedColor)
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(readingEase: readingEase);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.ReadingEaseColor.Should().Be(expectedColor);
    }

    [Theory]
    [InlineData(5, "#22C55E")]   // 0-8: Green (Easy)
    [InlineData(8, "#22C55E")]
    [InlineData(9, "#EAB308")]   // 9-12: Yellow (Medium)
    [InlineData(12, "#EAB308")]
    [InlineData(13, "#EF4444")]  // 13+: Red (Hard)
    [InlineData(18, "#EF4444")]
    public async Task FogIndexColor_ReturnsCorrectColor(
        double fogIndex, string expectedColor)
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(fogIndex: fogIndex);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        vm.FogIndexColor.Should().Be(expectedColor);
    }

    [Fact]
    public void ColorProperties_ReturnDefaultGray_WhenNoMetrics()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.GradeLevelColor.Should().Be("#9CA3AF");
        vm.ReadingEaseColor.Should().Be("#9CA3AF");
        vm.FogIndexColor.Should().Be("#9CA3AF");
    }

    #endregion

    #region Reset Tests

    [Fact]
    public async Task Reset_ClearsAllMetrics()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics();
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        await vm.UpdateAsync("Sample text");
        vm.HasMetrics.Should().BeTrue();

        // Act
        vm.Reset();

        // Assert
        vm.FleschKincaidGradeLevel.Should().Be(0);
        vm.GunningFogIndex.Should().Be(0);
        vm.FleschReadingEase.Should().Be(0);
        vm.WordCount.Should().Be(0);
        vm.SentenceCount.Should().Be(0);
        vm.WordsPerSentence.Should().Be(0);
        vm.HasMetrics.Should().BeFalse();
        vm.IsAnalyzing.Should().BeFalse();
    }

    [Fact]
    public async Task Reset_UpdatesDisplayProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics(gradeLevel: 10.5, fogIndex: 12.3, readingEase: 55.0);
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        await vm.UpdateAsync("Sample text");

        // Act
        vm.Reset();

        // Assert
        vm.GradeLevelDisplay.Should().Be("--");
        vm.FogIndexDisplay.Should().Be("--");
        vm.ReadingEaseDisplay.Should().Be("--");
        vm.InterpretationDisplay.Should().Be("No Data");
    }

    #endregion

    #region PropertyChanged Tests

    [Fact]
    public async Task UpdateAsync_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics();
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        await vm.UpdateAsync("Sample text");

        // Assert
        changedProperties.Should().Contain("FleschKincaidGradeLevel");
        changedProperties.Should().Contain("GunningFogIndex");
        changedProperties.Should().Contain("FleschReadingEase");
        changedProperties.Should().Contain("WordCount");
        changedProperties.Should().Contain("SentenceCount");
        changedProperties.Should().Contain("HasMetrics");
        changedProperties.Should().Contain("GradeLevelDisplay");
        changedProperties.Should().Contain("FogIndexDisplay");
        changedProperties.Should().Contain("ReadingEaseDisplay");
        changedProperties.Should().Contain("InterpretationDisplay");
        changedProperties.Should().Contain("GradeLevelColor");
        changedProperties.Should().Contain("ReadingEaseColor");
        changedProperties.Should().Contain("FogIndexColor");
    }

    [Fact]
    public async Task Reset_RaisesPropertyChangedForAllProperties()
    {
        // Arrange
        var vm = CreateViewModel();
        var metrics = CreateMetrics();
        _mockReadabilityService
            .Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // First populate with data so Reset actually changes HasMetrics
        await vm.UpdateAsync("Sample text");
        vm.HasMetrics.Should().BeTrue();

        var changedProperties = new List<string>();
        vm.PropertyChanged += (_, e) => changedProperties.Add(e.PropertyName!);

        // Act
        vm.Reset();

        // Assert
        changedProperties.Should().Contain("HasMetrics");
        changedProperties.Should().Contain("GradeLevelDisplay");
        changedProperties.Should().Contain("FogIndexDisplay");
        changedProperties.Should().Contain("ReadingEaseDisplay");
        changedProperties.Should().Contain("InterpretationDisplay");
        changedProperties.Should().Contain("GradeLevelColor");
    }

    #endregion

    #region Interface Implementation Tests

    [Fact]
    public void ImplementsIReadabilityHudViewModel()
    {
        // Arrange
        var vm = CreateViewModel();

        // Assert
        vm.Should().BeAssignableTo<IReadabilityHudViewModel>();
    }

    #endregion
}
