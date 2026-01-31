// <copyright file="DefaultAxisProviderTests.cs" company="Lexichord">
// Copyright (c) Lexichord. All rights reserved.
// Licensed under the MIT License.
// </copyright>

using FluentAssertions;
using Lexichord.Modules.Style.Services;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style;

/// <summary>
/// Unit tests for <see cref="DefaultAxisProvider"/>.
/// </summary>
/// <remarks>
/// LOGIC: v0.3.5a - Tests axis definitions and normalization logic.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.3.5a")]
public class DefaultAxisProviderTests
{
    [Fact]
    public void GetAxes_ReturnsSixAxes()
    {
        // Arrange
        var sut = new DefaultAxisProvider();

        // Act
        var axes = sut.GetAxes();

        // Assert
        axes.Should().HaveCount(6);
    }

    [Fact]
    public void GetAxes_ContainsExpectedAxisNames()
    {
        // Arrange
        var sut = new DefaultAxisProvider();
        var expectedNames = new[] { "Readability", "Clarity", "Precision", "Accessibility", "Density", "Flow" };

        // Act
        var axes = sut.GetAxes();

        // Assert
        axes.Select(a => a.Name).Should().BeEquivalentTo(expectedNames);
    }

    [Theory]
    [InlineData("Clarity", true)]       // Lower passive voice = better
    [InlineData("Precision", true)]     // Lower weak words = better
    [InlineData("Accessibility", true)] // Lower grade level = better
    [InlineData("Readability", false)]  // Higher reading ease = better
    [InlineData("Density", false)]
    [InlineData("Flow", false)]
    public void GetAxes_HasCorrectInvertScale(string axisName, bool expectedInvert)
    {
        // Arrange
        var sut = new DefaultAxisProvider();

        // Act
        var axis = sut.GetAxes().FirstOrDefault(a => a.Name == axisName);

        // Assert
        axis.Should().NotBeNull();
        axis!.InvertScale.Should().Be(expectedInvert);
    }

    [Theory]
    [InlineData("FleschReadingEase", "Readability")]
    [InlineData("PassiveVoicePercentage", "Clarity")]
    [InlineData("WeakWordDensity", "Precision")]
    [InlineData("FleschKincaidGrade", "Accessibility")]
    public void GetAxes_HasCorrectMetricKeys(string metricKey, string axisName)
    {
        // Arrange
        var sut = new DefaultAxisProvider();

        // Act
        var axis = sut.GetAxes().FirstOrDefault(a => a.Name == axisName);

        // Assert
        axis.Should().NotBeNull();
        axis!.MetricKey.Should().Be(metricKey);
    }

    [Fact]
    public void GetAxes_IsSameInstance()
    {
        // Arrange
        var sut = new DefaultAxisProvider();

        // Act
        var axes1 = sut.GetAxes();
        var axes2 = sut.GetAxes();

        // Assert
        axes1.Should().BeSameAs(axes2, "should return cached instance");
    }
}
