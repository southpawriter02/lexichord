// -----------------------------------------------------------------------
// <copyright file="IContextStrategyFactoryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Lexichord.Abstractions.Contracts;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="IContextStrategyFactory"/> interface contract.
/// </summary>
/// <remarks>
/// Tests verify that the interface defines the expected contract via reflection.
/// Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class IContextStrategyFactoryTests
{
    #region Interface Contract

    /// <summary>
    /// Verifies that IContextStrategyFactory is a public interface.
    /// </summary>
    [Fact]
    public void IContextStrategyFactory_IsPublicInterface()
    {
        // Arrange
        var type = typeof(IContextStrategyFactory);

        // Assert
        type.IsInterface.Should().BeTrue();
        type.IsPublic.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that AvailableStrategyIds property exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategyFactory_HasAvailableStrategyIdsProperty()
    {
        // Arrange
        var type = typeof(IContextStrategyFactory);

        // Act
        var property = type.GetProperty(nameof(IContextStrategyFactory.AvailableStrategyIds));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(IReadOnlyList<string>));
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that CreateStrategy method exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategyFactory_HasCreateStrategyMethod()
    {
        // Arrange
        var type = typeof(IContextStrategyFactory);

        // Act
        var method = type.GetMethod(nameof(IContextStrategyFactory.CreateStrategy));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IContextStrategy));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(string));
        parameters[0].Name.Should().Be("strategyId");
    }

    /// <summary>
    /// Verifies that CreateAllStrategies method exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategyFactory_HasCreateAllStrategiesMethod()
    {
        // Arrange
        var type = typeof(IContextStrategyFactory);

        // Act
        var method = type.GetMethod(nameof(IContextStrategyFactory.CreateAllStrategies));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(IReadOnlyList<IContextStrategy>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(0);
    }

    /// <summary>
    /// Verifies that IsAvailable method exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategyFactory_HasIsAvailableMethod()
    {
        // Arrange
        var type = typeof(IContextStrategyFactory);

        // Act
        var method = type.GetMethod(nameof(IContextStrategyFactory.IsAvailable));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(bool));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(string));
        parameters[0].Name.Should().Be("strategyId");
        parameters[1].ParameterType.Should().Be(typeof(LicenseTier));
        parameters[1].Name.Should().Be("tier");
    }

    /// <summary>
    /// Verifies that IContextStrategyFactory has exactly 4 members (1 property + 3 methods).
    /// </summary>
    [Fact]
    public void IContextStrategyFactory_HasExactlyFourMembers()
    {
        // Arrange
        var type = typeof(IContextStrategyFactory);

        // Act
        var properties = type.GetProperties();
        var methods = type.GetMethods().Where(m => !m.IsSpecialName).ToArray(); // Exclude property getters

        // Assert
        properties.Should().HaveCount(1); // AvailableStrategyIds
        methods.Should().HaveCount(3); // CreateStrategy, CreateAllStrategies, IsAvailable
    }

    #endregion
}
