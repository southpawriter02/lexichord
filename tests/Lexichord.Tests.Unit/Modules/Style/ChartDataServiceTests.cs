// <copyright file="ChartDataServiceTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="ChartDataService"/>.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.5a - Tests the chart data aggregation, caching, and event notification.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.5a")]
public class ChartDataServiceTests
{
    #region Test Setup

    private static ChartDataService CreateService(
        IReadabilityService? readabilityService = null,
        IPassiveVoiceDetector? passiveVoiceDetector = null,
        IWeakWordScanner? weakWordScanner = null,
        IVoiceProfileService? profileService = null,
        IResonanceAxisProvider? axisProvider = null)
    {
        return new ChartDataService(
            readabilityService ?? CreateMockReadabilityService().Object,
            passiveVoiceDetector ?? CreateMockPassiveVoiceDetector().Object,
            weakWordScanner ?? CreateMockWeakWordScanner().Object,
            profileService ?? CreateMockProfileService().Object,
            axisProvider ?? new DefaultAxisProvider(),
            NullLogger<ChartDataService>.Instance);
    }

    private static Mock<IReadabilityService> CreateMockReadabilityService()
    {
        var mock = new Mock<IReadabilityService>();
        mock.Setup(s => s.Analyze(It.IsAny<string>()))
            .Returns(new ReadabilityMetrics
            {
                FleschReadingEase = 72.4,
                FleschKincaidGradeLevel = 8.2,
                WordCount = 100,
                SentenceCount = 6,
                SyllableCount = 140,
                ComplexWordCount = 10
            });
        return mock;
    }

    private static Mock<IPassiveVoiceDetector> CreateMockPassiveVoiceDetector()
    {
        var mock = new Mock<IPassiveVoiceDetector>();
        var passiveCount = 1;
        var totalSentences = 6;
        mock.Setup(s => s.GetPassiveVoicePercentage(
                It.IsAny<string>(),
                out passiveCount,
                out totalSentences))
            .Returns(16.67);
        return mock;
    }

    private static Mock<IWeakWordScanner> CreateMockWeakWordScanner()
    {
        var mock = new Mock<IWeakWordScanner>();
        mock.Setup(s => s.GetStatistics(It.IsAny<string>(), It.IsAny<VoiceProfile>()))
            .Returns(new WeakWordStats(
                TotalWords: 100,
                TotalWeakWords: 3,
                CountByCategory: new Dictionary<WeakWordCategory, int>
                {
                    { WeakWordCategory.Adverb, 1 },
                    { WeakWordCategory.WeaselWord, 1 },
                    { WeakWordCategory.Filler, 1 }
                },
                Matches: Array.Empty<WeakWordMatch>()));
        return mock;
    }

    private static Mock<IVoiceProfileService> CreateMockProfileService()
    {
        var mock = new Mock<IVoiceProfileService>();
        mock.Setup(s => s.GetActiveProfileAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuiltInProfiles.Technical);
        return mock;
    }

    #endregion

    #region GetChartDataAsync Tests

    [Fact]
    public async Task GetChartDataAsync_ReturnsNormalizedValues()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = await sut.GetChartDataAsync();

        // Assert
        result.DataPoints.Should().NotBeEmpty();
        result.DataPoints.Should().AllSatisfy(p =>
        {
            p.NormalizedValue.Should().BeInRange(0, 100);
        });
    }

    [Fact]
    public async Task GetChartDataAsync_ReturnsAllAxes()
    {
        // Arrange
        var axisProvider = new DefaultAxisProvider();
        var expectedAxes = axisProvider.GetAxes();
        var sut = CreateService(axisProvider: axisProvider);

        // Act
        var result = await sut.GetChartDataAsync();

        // Assert
        result.DataPoints.Should().HaveCount(expectedAxes.Count);
        result.GetAxisNames().Should().BeEquivalentTo(expectedAxes.Select(a => a.Name));
    }

    [Fact]
    public async Task GetChartDataAsync_CachesResult()
    {
        // Arrange
        var mockReadability = CreateMockReadabilityService();
        var sut = CreateService(readabilityService: mockReadability.Object);

        // Act
        var result1 = await sut.GetChartDataAsync();
        var result2 = await sut.GetChartDataAsync();

        // Assert
        result1.Should().BeSameAs(result2);
        mockReadability.Verify(
            s => s.Analyze(It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChartDataAsync_SetsComputedAt()
    {
        // Arrange
        var before = DateTimeOffset.UtcNow;
        var sut = CreateService();

        // Act
        var result = await sut.GetChartDataAsync();
        var after = DateTimeOffset.UtcNow;

        // Assert
        result.ComputedAt.Should().BeOnOrAfter(before);
        result.ComputedAt.Should().BeOnOrBefore(after);
    }

    #endregion

    #region InvalidateCache Tests

    [Fact]
    public async Task InvalidateCache_ForcesRecomputation()
    {
        // Arrange
        var mockReadability = CreateMockReadabilityService();
        var sut = CreateService(readabilityService: mockReadability.Object);

        // Act
        var result1 = await sut.GetChartDataAsync();
        sut.InvalidateCache();
        var result2 = await sut.GetChartDataAsync();

        // Assert
        result1.Should().NotBeSameAs(result2);
        mockReadability.Verify(
            s => s.Analyze(It.IsAny<string>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task InvalidateCache_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var sut = CreateService();

        // Act & Assert
        sut.InvalidateCache();
        sut.InvalidateCache();
        sut.InvalidateCache();

        var result = await sut.GetChartDataAsync();
        result.Should().NotBeNull();
    }

    #endregion

    #region DataUpdated Event Tests

    [Fact]
    public async Task GetChartDataAsync_RaisesDataUpdatedEvent()
    {
        // Arrange
        var sut = CreateService();
        ChartDataUpdatedEventArgs? eventArgs = null;
        sut.DataUpdated += (_, args) => eventArgs = args;

        // Act
        await sut.GetChartDataAsync();

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.ChartData.Should().NotBeNull();
        eventArgs.ComputationTime.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public async Task GetChartDataAsync_CachedHit_DoesNotRaiseEvent()
    {
        // Arrange
        var sut = CreateService();
        var eventCount = 0;
        sut.DataUpdated += (_, _) => eventCount++;

        // Act
        await sut.GetChartDataAsync();
        await sut.GetChartDataAsync();
        await sut.GetChartDataAsync();

        // Assert
        eventCount.Should().Be(1, "event should only fire on cache miss");
    }

    #endregion

    #region Null/Empty Metrics Tests

    [Fact]
    public async Task GetChartDataAsync_HandlesEmptyMetrics()
    {
        // Arrange
        var mockReadability = new Mock<IReadabilityService>();
        mockReadability.Setup(s => s.Analyze(It.IsAny<string>()))
            .Returns(ReadabilityMetrics.Empty);

        var sut = CreateService(readabilityService: mockReadability.Object);

        // Act
        var result = await sut.GetChartDataAsync();

        // Assert
        result.DataPoints.Should().NotBeEmpty();
        // Axes with InvertScale=true should show 100 for a 0 raw value
        // Axes with InvertScale=false should show a value based on normalization
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetChartDataAsync_RespectsCanellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var sut = CreateService();

        // Act & Assert - TaskCanceledException inherits from OperationCanceledException
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.GetChartDataAsync(cts.Token));
    }

    #endregion
}
