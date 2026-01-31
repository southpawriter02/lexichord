// <copyright file="SpiderChartSeriesBuilderTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Style.Services;
using LiveChartsCore.SkiaSharpView;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="SpiderChartSeriesBuilder"/>.
/// </summary>
/// <remarks>
/// <para>LOGIC: v0.3.5b - Tests cover series construction, theme handling, and empty data cases.</para>
/// </remarks>
[Trait("Feature", "v0.3.5b")]
[Trait("Module", "Style")]
public class SpiderChartSeriesBuilderTests
{
    private readonly Mock<ILogger<SpiderChartSeriesBuilder>> _loggerMock;
    private readonly SpiderChartSeriesBuilder _sut;

    public SpiderChartSeriesBuilderTests()
    {
        _loggerMock = new Mock<ILogger<SpiderChartSeriesBuilder>>();
        _sut = new SpiderChartSeriesBuilder(_loggerMock.Object);
    }

    #region BuildCurrentSeries Tests

    [Fact]
    public void BuildCurrentSeries_WithValidData_ReturnsPolySeries()
    {
        // Arrange
        var chartData = CreateTestChartData();

        // Act
        var result = _sut.BuildCurrentSeries(chartData);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<PolarLineSeries<double>>();
        var polySeries = result as PolarLineSeries<double>;
        polySeries!.Name.Should().Be("Current");
        polySeries.Values.Should().NotBeNull();
        polySeries.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void BuildCurrentSeries_WithEmptyData_ReturnsNull()
    {
        // Arrange
        var chartData = new ResonanceChartData([], DateTimeOffset.UtcNow);

        // Act
        var result = _sut.BuildCurrentSeries(chartData);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildCurrentSeries_RespectsThemeSetting(bool isDarkTheme)
    {
        // Arrange
        var chartData = CreateTestChartData();

        // Act
        var result = _sut.BuildCurrentSeries(chartData, isDarkTheme);

        // Assert
        result.Should().NotBeNull();
        // Note: We can't easily verify colors without accessing SkiaSharp internals,
        // but we verify the series is created successfully with both theme settings.
    }

    [Fact]
    public void BuildCurrentSeries_HasGeometryMarkers()
    {
        // Arrange
        var chartData = CreateTestChartData();

        // Act
        var result = _sut.BuildCurrentSeries(chartData);

        // Assert
        var polySeries = result as PolarLineSeries<double>;
        polySeries!.GeometrySize.Should().BeGreaterThan(0);
    }

    #endregion

    #region BuildTargetSeries Tests

    [Fact]
    public void BuildTargetSeries_WithValidData_ReturnsPolySeries()
    {
        // Arrange
        var targetData = CreateTestChartData();
        var profileName = "Technical Writer";

        // Act
        var result = _sut.BuildTargetSeries(targetData, profileName);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeOfType<PolarLineSeries<double>>();
        var polySeries = result as PolarLineSeries<double>;
        polySeries!.Name.Should().Contain("Target");
        polySeries.Name.Should().Contain(profileName);
        polySeries.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void BuildTargetSeries_WithEmptyData_ReturnsNull()
    {
        // Arrange
        var targetData = new ResonanceChartData([], DateTimeOffset.UtcNow);

        // Act
        var result = _sut.BuildTargetSeries(targetData, "Profile");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void BuildTargetSeries_HasNoGeometryMarkers()
    {
        // Arrange
        var targetData = CreateTestChartData();

        // Act
        var result = _sut.BuildTargetSeries(targetData, "Profile");

        // Assert
        var polySeries = result as PolarLineSeries<double>;
        polySeries!.GeometrySize.Should().Be(0);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void BuildTargetSeries_RespectsThemeSetting(bool isDarkTheme)
    {
        // Arrange
        var targetData = CreateTestChartData();

        // Act
        var result = _sut.BuildTargetSeries(targetData, "Profile", isDarkTheme);

        // Assert
        result.Should().NotBeNull();
    }

    #endregion

    #region Test Helpers

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

    #endregion
}
