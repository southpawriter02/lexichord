using FluentAssertions;

using Lexichord.Abstractions.Contracts;

using Xunit;

namespace Lexichord.Tests.Unit.Abstractions;

/// <summary>
/// Tests for the ILicenseContext interface.
/// </summary>
public class ILicenseContextTests
{
    [Fact]
    public void ILicenseContext_DefinesGetCurrentTier()
    {
        // Arrange
        var interfaceType = typeof(ILicenseContext);
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m =>
            m.Name == "GetCurrentTier" &&
            m.ReturnType == typeof(LicenseTier) &&
            m.GetParameters().Length == 0);
    }

    [Fact]
    public void ILicenseContext_DefinesIsFeatureEnabled()
    {
        // Arrange
        var interfaceType = typeof(ILicenseContext);
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m =>
            m.Name == "IsFeatureEnabled" &&
            m.ReturnType == typeof(bool) &&
            m.GetParameters().Length == 1 &&
            m.GetParameters()[0].ParameterType == typeof(string));
    }

    [Fact]
    public void ILicenseContext_DefinesGetExpirationDate()
    {
        // Arrange
        var interfaceType = typeof(ILicenseContext);
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m =>
            m.Name == "GetExpirationDate" &&
            m.ReturnType == typeof(DateTime?));
    }

    [Fact]
    public void ILicenseContext_DefinesGetLicenseeName()
    {
        // Arrange
        var interfaceType = typeof(ILicenseContext);
        var methods = interfaceType.GetMethods();

        // Assert
        methods.Should().Contain(m =>
            m.Name == "GetLicenseeName" &&
            m.ReturnType == typeof(string));
    }
}
