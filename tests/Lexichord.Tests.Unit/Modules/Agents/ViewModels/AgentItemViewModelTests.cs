// Copyright (c) 2024-2026 Lexichord. All rights reserved.
// Licensed under the Lexichord License v1.0.
// See LICENSE file in the project root for full license information.

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.Agents.ViewModels;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.ViewModels;

/// <summary>
/// Unit tests for <see cref="AgentItemViewModel"/> (v0.7.1d).
/// </summary>
/// <remarks>
/// <para>
/// Validates the Agent Item ViewModel behavior for:
/// </para>
/// <list type="bullet">
///   <item><description>Computed property logic (TierBadgeText, ShowTierBadge, IsLocked, etc.)</description></item>
///   <item><description>Default persona selection</description></item>
///   <item><description>Capabilities summary formatting</description></item>
///   <item><description>Accessibility label generation</description></item>
/// </list>
/// <para>
/// <strong>Spec reference:</strong> LCS-DES-v0.7.1d §5
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1d")]
public class AgentItemViewModelTests
{
    // -----------------------------------------------------------------------
    // DefaultPersona Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that DefaultPersona returns the first persona in the list.
    /// </summary>
    [Fact]
    public void DefaultPersona_WithPersonas_ReturnsFirstPersona()
    {
        // Arrange
        var sut = new AgentItemViewModel();
        var persona1 = new PersonaItemViewModel { PersonaId = "strict", DisplayName = "Strict" };
        var persona2 = new PersonaItemViewModel { PersonaId = "friendly", DisplayName = "Friendly" };
        sut.Personas.Add(persona1);
        sut.Personas.Add(persona2);

        // Act
        var result = sut.DefaultPersona;

        // Assert
        result.Should().BeSameAs(persona1);
    }

    /// <summary>
    /// Verifies that DefaultPersona returns null when no personas exist.
    /// </summary>
    [Fact]
    public void DefaultPersona_WithoutPersonas_ReturnsNull()
    {
        // Arrange
        var sut = new AgentItemViewModel();

        // Act
        var result = sut.DefaultPersona;

        // Assert
        result.Should().BeNull();
    }

    // -----------------------------------------------------------------------
    // TierBadgeText Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that TierBadgeText returns correct text for each tier.
    /// </summary>
    [Theory]
    [InlineData(LicenseTier.Core, "")]
    [InlineData(LicenseTier.WriterPro, "PRO")]
    [InlineData(LicenseTier.Teams, "TEAMS")]
    [InlineData(LicenseTier.Enterprise, "ENTERPRISE")]
    public void TierBadgeText_ReturnsCorrectTextForTier(LicenseTier tier, string expected)
    {
        // Arrange
        var sut = new AgentItemViewModel { RequiredTier = tier };

        // Act
        var result = sut.TierBadgeText;

        // Assert
        result.Should().Be(expected);
    }

    // -----------------------------------------------------------------------
    // ShowTierBadge Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that ShowTierBadge returns false for Core tier.
    /// </summary>
    [Fact]
    public void ShowTierBadge_CoreTier_ReturnsFalse()
    {
        // Arrange
        var sut = new AgentItemViewModel { RequiredTier = LicenseTier.Core };

        // Act
        var result = sut.ShowTierBadge;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ShowTierBadge returns true for paid tiers.
    /// </summary>
    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public void ShowTierBadge_PaidTier_ReturnsTrue(LicenseTier tier)
    {
        // Arrange
        var sut = new AgentItemViewModel { RequiredTier = tier };

        // Act
        var result = sut.ShowTierBadge;

        // Assert
        result.Should().BeTrue();
    }

    // -----------------------------------------------------------------------
    // IsLocked Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that IsLocked returns true when CanAccess is false and tier is paid.
    /// </summary>
    [Fact]
    public void IsLocked_CannotAccessPaidTier_ReturnsTrue()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            CanAccess = false,
            RequiredTier = LicenseTier.WriterPro
        };

        // Act
        var result = sut.IsLocked;

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsLocked returns false when CanAccess is true.
    /// </summary>
    [Fact]
    public void IsLocked_CanAccess_ReturnsFalse()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            CanAccess = true,
            RequiredTier = LicenseTier.WriterPro
        };

        // Act
        var result = sut.IsLocked;

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsLocked returns false for Core tier even if CanAccess is false.
    /// </summary>
    [Fact]
    public void IsLocked_CoreTier_ReturnsFalse()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            CanAccess = false,
            RequiredTier = LicenseTier.Core
        };

        // Act
        var result = sut.IsLocked;

        // Assert
        result.Should().BeFalse();
    }

    // -----------------------------------------------------------------------
    // CapabilitiesSummary Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that CapabilitiesSummary calls ToDisplayString().
    /// </summary>
    [Fact]
    public void CapabilitiesSummary_ReturnsFormattedCapabilities()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Capabilities = AgentCapabilities.Chat | AgentCapabilities.DocumentContext
        };

        // Act
        var result = sut.CapabilitiesSummary;

        // Assert
        result.Should().Contain("Chat");
        // LOGIC: DocumentContext capability maps to "Document" display string
        result.Should().Contain("Document");
    }

    // -----------------------------------------------------------------------
    // AccessibilityLabel Tests
    // -----------------------------------------------------------------------

    /// <summary>
    /// Verifies that AccessibilityLabel includes name and description.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesNameAndDescription()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Name = "Test Agent",
            Description = "Test Description",
            Capabilities = AgentCapabilities.None,
            RequiredTier = LicenseTier.Core
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Test Agent");
        result.Should().Contain("Test Description");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes capabilities when present.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesCapabilities()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Name = "Test Agent",
            Description = "Test Description",
            Capabilities = AgentCapabilities.Chat,
            RequiredTier = LicenseTier.Core
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Capabilities");
        result.Should().Contain("Chat");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes tier when ShowTierBadge is true.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesTierForPaidTier()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Name = "Test Agent",
            Description = "Test Description",
            Capabilities = AgentCapabilities.None,
            RequiredTier = LicenseTier.WriterPro
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Required tier: PRO");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes locked status.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesLockedStatus()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Name = "Test Agent",
            Description = "Test Description",
            Capabilities = AgentCapabilities.None,
            RequiredTier = LicenseTier.WriterPro,
            CanAccess = false
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Locked");
        result.Should().Contain("Upgrade required");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes favorite status.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesFavoriteStatus()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Name = "Test Agent",
            Description = "Test Description",
            Capabilities = AgentCapabilities.None,
            RequiredTier = LicenseTier.Core,
            IsFavorite = true
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Favorite");
    }

    /// <summary>
    /// Verifies that AccessibilityLabel includes selected status.
    /// </summary>
    [Fact]
    public void AccessibilityLabel_IncludesSelectedStatus()
    {
        // Arrange
        var sut = new AgentItemViewModel
        {
            Name = "Test Agent",
            Description = "Test Description",
            Capabilities = AgentCapabilities.None,
            RequiredTier = LicenseTier.Core,
            IsSelected = true
        };

        // Act
        var result = sut.AccessibilityLabel;

        // Assert
        result.Should().Contain("Selected");
    }
}
