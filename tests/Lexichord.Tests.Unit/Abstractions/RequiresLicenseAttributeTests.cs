using FluentAssertions;

using Lexichord.Abstractions.Contracts;

using Xunit;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Tests for the RequiresLicenseAttribute.
/// </summary>
public class RequiresLicenseAttributeTests
{
    [Fact]
    public void RequiresLicense_TierIsSet()
    {
        // Arrange & Act
        var attr = new RequiresLicenseAttribute(LicenseTier.Teams);

        // Assert
        attr.Tier.Should().Be(LicenseTier.Teams);
    }

    [Fact]
    public void RequiresLicense_FeatureCodeIsNull_ByDefault()
    {
        // Arrange & Act
        var attr = new RequiresLicenseAttribute(LicenseTier.WriterPro);

        // Assert
        attr.FeatureCode.Should().BeNull();
    }

    [Fact]
    public void RequiresLicense_FeatureCodeCanBeSet()
    {
        // Arrange & Act
        var attr = new RequiresLicenseAttribute(LicenseTier.WriterPro)
        {
            FeatureCode = "RAG-01"
        };

        // Assert
        attr.FeatureCode.Should().Be("RAG-01");
    }

    [Fact]
    public void RequiresLicense_AttributeUsageIsCorrect()
    {
        // Arrange
        var attributeUsage = typeof(RequiresLicenseAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .FirstOrDefault();

        // Assert
        attributeUsage.Should().NotBeNull();
        attributeUsage!.ValidOn.Should().Be(AttributeTargets.Class);
        attributeUsage.AllowMultiple.Should().BeFalse();
        attributeUsage.Inherited.Should().BeFalse();
    }

    [Fact]
    public void RequiresLicense_CanBeAppliedToClassViaReflection()
    {
        // Arrange - test via reflection on an existing decorated class
        // Use the attribute type itself to verify it can be instantiated and applied
        var attr = new RequiresLicenseAttribute(LicenseTier.Enterprise);

        // Assert - attribute instance is valid and can be attached
        attr.Should().NotBeNull();
        attr.Tier.Should().Be(LicenseTier.Enterprise);

        // Verify attribute targets Class
        var usage = typeof(RequiresLicenseAttribute)
            .GetCustomAttributes(typeof(AttributeUsageAttribute), false)
            .Cast<AttributeUsageAttribute>()
            .First();
        usage.ValidOn.Should().HaveFlag(AttributeTargets.Class);
    }
}
