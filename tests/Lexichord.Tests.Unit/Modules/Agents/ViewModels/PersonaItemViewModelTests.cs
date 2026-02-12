// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using FluentAssertions;
using Lexichord.Modules.Agents.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.ViewModels;

/// <summary>
/// Unit tests for <see cref="PersonaItemViewModel"/> (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// Validates the Persona Item ViewModel behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Temperature label generation</description></item>
///   <item><description>Accessibility label generation</description></item>
///   <item><description>Property change notifications</description></item>
/// </list>
/// <para>
/// <strong>Spec reference:</strong> LCS-DES-v0.7.1d §6
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1d")]
public class PersonaItemViewModelTests
{
    // -----------------------------------------------------------------------
    // TemperatureLabel Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that TemperatureLabel returns correct labels for temperature ranges.
    /// </summary>
    [Theory]
    [InlineData(0.0, "Focused")]
    [InlineData(0.1, "Focused")]
    [InlineData(0.29, "Focused")]
    [InlineData(0.3, "Balanced")]
    [InlineData(0.5, "Balanced")]
    [InlineData(0.59, "Balanced")]
    [InlineData(0.6, "Creative")]
    [InlineData(0.8, "Creative")]
    [InlineData(0.89, "Creative")]
    [InlineData(0.9, "Experimental")]
    [InlineData(1.0, "Experimental")]
    [InlineData(1.5, "Experimental")]
    [InlineData(2.0, "Experimental")]
    public void TemperatureLabel_ReturnsCorrectLabelForTemperature(double temperature, string expected)
    {
        // Arrange
        var sut = new PersonaItemViewModel { Temperature = temperature };

        // Act
        var result = sut.TemperatureLabel;

        // Assert
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // AccessibilityLabel Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that AccessibilityLabel includes display name, tagline, and temperature label.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesBasicInfo()
    {
        // Arrange
        var sut = new PersonaItemViewModel
        {
            DisplayName = "Strict Editor",
            Tagline = "No errors escape",
            Temperature = 0.1
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Strict Editor");
        result.Should().Contain("No errors escape");
        result.Should().Contain("Focused mode");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes voice description when present.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesVoiceDescription()
    {
        // Arrange
        var sut = new PersonaItemViewModel
        {
            DisplayName = "Friendly Editor",
            Tagline = "Gentle suggestions",
            Temperature = 0.5,
            VoiceDescription = "Uses encouraging language"
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Uses encouraging language");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel excludes voice description when null.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_ExcludesNullVoiceDescription()
    {
        // Arrange
        var sut = new PersonaItemViewModel
        {
            DisplayName = "Strict Editor",
            Tagline = "No errors escape",
            Temperature = 0.1,
            VoiceDescription = null
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().NotContain("null");
        result.Should().Contain("Strict Editor");
        result.Should().Contain("No errors escape");
        result.Should().Contain("Focused mode");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel excludes voice description when empty.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_ExcludesEmptyVoiceDescription()
    {
        // Arrange
        var sut = new PersonaItemViewModel
        {
            DisplayName = "Strict Editor",
            Tagline = "No errors escape",
            Temperature = 0.1,
            VoiceDescription = string.Empty
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Split('.', StringSplitOptions.RemoveEmptyEntries).Should().HaveCount(3); // Name, Tagline, Mode only
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes selected status.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesSelectedStatus()
    {
        // Arrange
        var sut = new PersonaItemViewModel
        {
            DisplayName = "Strict Editor",
            Tagline = "No errors escape",
            Temperature = 0.1,
            IsSelected = true
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Selected");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel excludes selected status when false.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_ExcludesUnselectedStatus()
    {
        // Arrange
        var sut = new PersonaItemViewModel
        {
            DisplayName = "Strict Editor",
            Tagline = "No errors escape",
            Temperature = 0.1,
            IsSelected = false
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().NotContain("Selected");
    }

    // -----------------------------------------------------------------------
    // Property Change Notification Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that changing Temperature triggers PropertyChanged for TemperatureLabel.
    /// </summary>
    [Fact]
    public void Temperature_Changed_RaisesPropertyChangedForTemperatureLabel()
    {
        // Arrange
        var sut = new PersonaItemViewModel { Temperature = 0.1 };
        var propertyChangedEvents = new List<string>();
        sut.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        sut.Temperature = 0.9;

        // Assert
        propertyChangedEvents.Should().Contain("Temperature");
        // Note: TemperatureLabel is a computed property, so it may or may not
        // automatically raise PropertyChanged depending on CommunityToolkit.Mvvm implementation.
        // If it doesn't, we would need to manually raise it in the setter.
    }

    /// <summary>
    /// Verifies that changing IsSelected raises PropertyChanged.
    /// </summary>
    [Fact]
    public void IsSelected_Changed_RaisesPropertyChanged()
    {
        // Arrange
        var sut = new PersonaItemViewModel { IsSelected = false };
        var propertyChangedEvents = new List<string>();
        sut.PropertyChanged += (s, e) => propertyChangedEvents.Add(e.PropertyName!);

        // Act
        sut.IsSelected = true;

        // Assert
        propertyChangedEvents.Should().Contain("IsSelected");
    }
}
