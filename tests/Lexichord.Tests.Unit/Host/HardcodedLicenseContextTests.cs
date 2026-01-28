using FluentAssertions;

using Lexichord.Abstractions.Contracts;
using Lexichord.Host.Services;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Lexichord.Tests.Unit.Host;

/// <summary>
/// Tests for HardcodedLicenseContext.
/// </summary>
public class HardcodedLicenseContextTests
{
    [Fact]
    public void GetCurrentTier_ReturnsCore()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act
        var result = sut.GetCurrentTier();

        // Assert
        result.Should().Be(LicenseTier.Core);
    }

    [Fact]
    public void IsFeatureEnabled_AlwaysReturnsTrue()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act & Assert
        sut.IsFeatureEnabled("RAG-01").Should().BeTrue();
        sut.IsFeatureEnabled("AGT-05").Should().BeTrue();
        sut.IsFeatureEnabled("NONEXISTENT").Should().BeTrue();
        sut.IsFeatureEnabled("").Should().BeTrue();
    }

    [Fact]
    public void GetExpirationDate_ReturnsNull()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act
        var result = sut.GetExpirationDate();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetLicenseeName_ReturnsDevelopmentLicense()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Act
        var result = sut.GetLicenseeName();

        // Assert
        result.Should().Be("Development License");
    }

    [Fact]
    public void Constructor_WithLogger_DoesNotThrow()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<HardcodedLicenseContext>>();

        // Act & Assert
        var act = () => new HardcodedLicenseContext(mockLogger.Object);
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithoutLogger_DoesNotThrow()
    {
        // Act & Assert
        var act = () => new HardcodedLicenseContext();
        act.Should().NotThrow();
    }

    [Fact]
    public void ImplementsILicenseContext()
    {
        // Arrange
        var sut = new HardcodedLicenseContext();

        // Assert
        sut.Should().BeAssignableTo<ILicenseContext>();
    }
}
