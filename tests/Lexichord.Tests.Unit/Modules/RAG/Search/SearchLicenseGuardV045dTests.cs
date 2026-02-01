// =============================================================================
// File: SearchLicenseGuardV045dTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for v0.4.5d enhancements to SearchLicenseGuard:
//              public constants and GetUpgradeMessage().
// =============================================================================
// LOGIC: Verifies the v0.4.5d additions to SearchLicenseGuard:
//   - FeatureName is a public const string accessible to external consumers.
//   - RequiredTier is a public const LicenseTier accessible to external consumers.
//   - GetUpgradeMessage() returns tier-specific upgrade guidance.
//   - Core tier receives upgrade encouragement.
//   - Authorized tiers (WriterPro+) receive confirmation of access.
//   - All tiers produce non-empty messages.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Search;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for v0.4.5d enhancements to <see cref="SearchLicenseGuard"/>.
/// Verifies public constants and the <see cref="SearchLicenseGuard.GetUpgradeMessage"/> method.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5d")]
public class SearchLicenseGuardV045dTests
{
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;

    public SearchLicenseGuardV045dTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
    }

    /// <summary>
    /// Creates a <see cref="SearchLicenseGuard"/> with the specified tier.
    /// </summary>
    private SearchLicenseGuard CreateGuard(LicenseTier tier)
    {
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(tier);
        return new SearchLicenseGuard(
            _licenseContextMock.Object,
            _mediatorMock.Object,
            NullLogger<SearchLicenseGuard>.Instance);
    }

    #region Public Constants Tests

    [Fact]
    public void FeatureName_IsPublicConst_EqualsSemanticSearch()
    {
        // Assert
        // LOGIC: FeatureName must be publicly accessible so UI consumers and event
        // handlers can reference the canonical feature name without string duplication.
        SearchLicenseGuard.FeatureName.Should().Be("Semantic Search",
            because: "the feature name constant should be 'Semantic Search'");
    }

    [Fact]
    public void RequiredTier_IsPublicConst_EqualsWriterPro()
    {
        // Assert
        // LOGIC: RequiredTier must be publicly accessible so UI consumers can
        // display the required tier in upgrade prompts.
        SearchLicenseGuard.RequiredTier.Should().Be(LicenseTier.WriterPro,
            because: "semantic search requires the WriterPro tier");
    }

    #endregion

    #region GetUpgradeMessage Tests

    [Fact]
    public void GetUpgradeMessage_CoreTier_ContainsUpgradeGuidance()
    {
        // Arrange
        var guard = CreateGuard(LicenseTier.Core);

        // Act
        var message = guard.GetUpgradeMessage();

        // Assert
        message.Should().Contain("Upgrade to Writer Pro",
            because: "Core tier users should be told to upgrade to Writer Pro");
        message.Should().Contain("semantic search",
            because: "the message should mention the feature being gated");
    }

    [Fact]
    public void GetUpgradeMessage_WriterProTier_IndicatesAccessAvailable()
    {
        // Arrange
        var guard = CreateGuard(LicenseTier.WriterPro);

        // Act
        var message = guard.GetUpgradeMessage();

        // Assert
        message.Should().Contain("available",
            because: "WriterPro users should see that search is available");
        message.Should().NotContain("Upgrade",
            because: "WriterPro users should not be prompted to upgrade");
    }

    [Fact]
    public void GetUpgradeMessage_TeamsTier_IndicatesAccessAvailable()
    {
        // Arrange
        var guard = CreateGuard(LicenseTier.Teams);

        // Act
        var message = guard.GetUpgradeMessage();

        // Assert
        message.Should().Contain("available",
            because: "Teams users should see that search is available");
    }

    [Fact]
    public void GetUpgradeMessage_EnterpriseTier_IndicatesAccessAvailable()
    {
        // Arrange
        var guard = CreateGuard(LicenseTier.Enterprise);

        // Act
        var message = guard.GetUpgradeMessage();

        // Assert
        message.Should().Contain("available",
            because: "Enterprise users should see that search is available");
    }

    [Theory]
    [InlineData(LicenseTier.Core)]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public void GetUpgradeMessage_AllTiers_ReturnsNonEmptyString(LicenseTier tier)
    {
        // Arrange
        var guard = CreateGuard(tier);

        // Act
        var message = guard.GetUpgradeMessage();

        // Assert
        message.Should().NotBeNullOrWhiteSpace(
            because: $"tier {tier} should produce a meaningful upgrade message");
    }

    #endregion
}
