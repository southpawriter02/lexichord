using FluentAssertions;

using Lexichord.Abstractions.Contracts;

using Xunit;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Tests for the LicenseTier enum.
/// </summary>
public class LicenseTierTests
{
    [Fact]
    public void LicenseTier_HierarchyIsCorrect()
    {
        // Assert - Higher tiers have higher numeric values
        ((int)LicenseTier.Core).Should().BeLessThan((int)LicenseTier.WriterPro);
        ((int)LicenseTier.WriterPro).Should().BeLessThan((int)LicenseTier.Teams);
        ((int)LicenseTier.Teams).Should().BeLessThan((int)LicenseTier.Enterprise);
    }

    [Fact]
    public void LicenseTier_CoreIsDefault()
    {
        // Arrange & Act
        var defaultTier = default(LicenseTier);

        // Assert
        defaultTier.Should().Be(LicenseTier.Core);
    }

    [Fact]
    public void LicenseTier_ExplicitValues()
    {
        // Assert - Values are explicitly defined for stable serialization
        ((int)LicenseTier.Core).Should().Be(0);
        ((int)LicenseTier.WriterPro).Should().Be(1);
        ((int)LicenseTier.Teams).Should().Be(2);
        ((int)LicenseTier.Enterprise).Should().Be(3);
    }

    [Theory]
    [InlineData(LicenseTier.Core, LicenseTier.Core, true)]
    [InlineData(LicenseTier.Core, LicenseTier.WriterPro, true)]
    [InlineData(LicenseTier.WriterPro, LicenseTier.Core, false)]
    [InlineData(LicenseTier.Teams, LicenseTier.Enterprise, true)]
    [InlineData(LicenseTier.Enterprise, LicenseTier.Teams, false)]
    public void LicenseTier_ComparisonWorks(LicenseTier required, LicenseTier current, bool expectedAccess)
    {
        // Act
        var hasAccess = required <= current;

        // Assert
        hasAccess.Should().Be(expectedAccess);
    }
}
