// =============================================================================
// File: SearchLicenseGuardTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SearchLicenseGuard license validation logic.
// =============================================================================
// LOGIC: Verifies the three license-checking modes:
//   - EnsureSearchAuthorized(): throws on insufficient tier.
//   - TryAuthorizeSearchAsync(): publishes denial event, returns bool.
//   - IsSearchAvailable: property check without side effects.
//   - Constructor null-parameter validation.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Search;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Search;

/// <summary>
/// Unit tests for <see cref="SearchLicenseGuard"/>.
/// Verifies license tier validation, exception throwing, and event publishing.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.5b")]
public class SearchLicenseGuardTests
{
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SearchLicenseGuard>> _loggerMock;

    public SearchLicenseGuardTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SearchLicenseGuard>>();
    }

    /// <summary>
    /// Creates a <see cref="SearchLicenseGuard"/> using the test mocks.
    /// </summary>
    private SearchLicenseGuard CreateGuard() =>
        new(_licenseContextMock.Object, _mediatorMock.Object, _loggerMock.Object);

    /// <summary>
    /// Configures the license context mock to return the specified tier.
    /// </summary>
    private void SetupTier(LicenseTier tier) =>
        _licenseContextMock.Setup(x => x.GetCurrentTier()).Returns(tier);

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchLicenseGuard(null!, _mediatorMock.Object, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("licenseContext");
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchLicenseGuard(_licenseContextMock.Object, null!, _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("mediator");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchLicenseGuard(_licenseContextMock.Object, _mediatorMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateGuard();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region CurrentTier Property Tests

    [Theory]
    [InlineData(LicenseTier.Core)]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public void CurrentTier_ReturnsValueFromLicenseContext(LicenseTier tier)
    {
        // Arrange
        SetupTier(tier);
        var guard = CreateGuard();

        // Act
        var result = guard.CurrentTier;

        // Assert
        result.Should().Be(tier,
            because: "CurrentTier should delegate to ILicenseContext.GetCurrentTier()");
    }

    #endregion

    #region IsSearchAvailable Property Tests

    [Theory]
    [InlineData(LicenseTier.Core, false)]
    [InlineData(LicenseTier.WriterPro, true)]
    [InlineData(LicenseTier.Teams, true)]
    [InlineData(LicenseTier.Enterprise, true)]
    public void IsSearchAvailable_ReturnsCorrectValueForTier(LicenseTier tier, bool expected)
    {
        // Arrange
        SetupTier(tier);
        var guard = CreateGuard();

        // Act
        var result = guard.IsSearchAvailable;

        // Assert
        result.Should().Be(expected,
            because: $"tier {tier} should {(expected ? "" : "not ")}have search access");
    }

    #endregion

    #region EnsureSearchAuthorized Tests

    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public void EnsureSearchAuthorized_AuthorizedTier_DoesNotThrow(LicenseTier tier)
    {
        // Arrange
        SetupTier(tier);
        var guard = CreateGuard();

        // Act & Assert
        var act = () => guard.EnsureSearchAuthorized();

        act.Should().NotThrow(
            because: $"tier {tier} meets the WriterPro minimum requirement");
    }

    [Fact]
    public void EnsureSearchAuthorized_CoreTier_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();

        // Act & Assert
        var act = () => guard.EnsureSearchAuthorized();

        act.Should().Throw<FeatureNotLicensedException>(
            because: "Core tier is below the WriterPro requirement");
    }

    [Fact]
    public void EnsureSearchAuthorized_CoreTier_ExceptionContainsRequiredTier()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();

        // Act & Assert
        var act = () => guard.EnsureSearchAuthorized();

        act.Should().Throw<FeatureNotLicensedException>()
            .Which.RequiredTier.Should().Be(LicenseTier.WriterPro,
                because: "the exception should indicate WriterPro as the required tier");
    }

    [Fact]
    public void EnsureSearchAuthorized_CoreTier_ExceptionMessageContainsSemanticSearch()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();

        // Act & Assert
        var act = () => guard.EnsureSearchAuthorized();

        act.Should().Throw<FeatureNotLicensedException>()
            .WithMessage("*Semantic Search*",
                because: "the exception message should identify the denied feature");
    }

    #endregion

    #region TryAuthorizeSearchAsync Tests

    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public async Task TryAuthorizeSearchAsync_AuthorizedTier_ReturnsTrue(LicenseTier tier)
    {
        // Arrange
        SetupTier(tier);
        var guard = CreateGuard();

        // Act
        var result = await guard.TryAuthorizeSearchAsync();

        // Assert
        result.Should().BeTrue(
            because: $"tier {tier} meets the WriterPro minimum requirement");
    }

    [Theory]
    [InlineData(LicenseTier.WriterPro)]
    [InlineData(LicenseTier.Teams)]
    [InlineData(LicenseTier.Enterprise)]
    public async Task TryAuthorizeSearchAsync_AuthorizedTier_DoesNotPublishEvent(LicenseTier tier)
    {
        // Arrange
        SetupTier(tier);
        var guard = CreateGuard();

        // Act
        await guard.TryAuthorizeSearchAsync();

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<SearchDeniedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "no denial event should be published for authorized tiers");
    }

    [Fact]
    public async Task TryAuthorizeSearchAsync_CoreTier_ReturnsFalse()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();

        // Act
        var result = await guard.TryAuthorizeSearchAsync();

        // Assert
        result.Should().BeFalse(
            because: "Core tier is below the WriterPro requirement");
    }

    [Fact]
    public async Task TryAuthorizeSearchAsync_CoreTier_PublishesSearchDeniedEvent()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();

        // Act
        await guard.TryAuthorizeSearchAsync();

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SearchDeniedEvent>(e =>
                    e.CurrentTier == LicenseTier.Core &&
                    e.RequiredTier == LicenseTier.WriterPro &&
                    e.FeatureName == "Semantic Search"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "a SearchDeniedEvent should be published with the correct tier information");
    }

    [Fact]
    public async Task TryAuthorizeSearchAsync_UnauthorizedTier_EventHasTimestamp()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();
        var before = DateTimeOffset.UtcNow;

        // Act
        await guard.TryAuthorizeSearchAsync();

        var after = DateTimeOffset.UtcNow;

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(
                It.Is<SearchDeniedEvent>(e =>
                    e.Timestamp >= before && e.Timestamp <= after),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "the denial event should have a timestamp close to the current time");
    }

    [Fact]
    public async Task TryAuthorizeSearchAsync_PassesCancellationToken()
    {
        // Arrange
        SetupTier(LicenseTier.Core);
        var guard = CreateGuard();
        using var cts = new CancellationTokenSource();

        // Act
        await guard.TryAuthorizeSearchAsync(cts.Token);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<SearchDeniedEvent>(), cts.Token),
            Times.Once,
            "the cancellation token should be forwarded to the mediator");
    }

    #endregion
}
