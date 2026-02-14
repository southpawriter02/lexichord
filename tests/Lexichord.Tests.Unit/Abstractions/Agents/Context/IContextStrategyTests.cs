// -----------------------------------------------------------------------
// <copyright file="IContextStrategyTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="IContextStrategy"/> interface contract.
/// </summary>
/// <remarks>
/// Tests verify that the interface defines the expected contract via reflection.
/// Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class IContextStrategyTests
{
    #region Interface Contract

    /// <summary>
    /// Verifies that IContextStrategy is a public interface.
    /// </summary>
    [Fact]
    public void IContextStrategy_IsPublicInterface()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Assert
        type.IsInterface.Should().BeTrue();
        type.IsPublic.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that StrategyId property exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategy_HasStrategyIdProperty()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Act
        var property = type.GetProperty(nameof(IContextStrategy.StrategyId));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(string));
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that DisplayName property exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategy_HasDisplayNameProperty()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Act
        var property = type.GetProperty(nameof(IContextStrategy.DisplayName));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(string));
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that Priority property exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategy_HasPriorityProperty()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Act
        var property = type.GetProperty(nameof(IContextStrategy.Priority));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(int));
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that MaxTokens property exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategy_HasMaxTokensProperty()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Act
        var property = type.GetProperty(nameof(IContextStrategy.MaxTokens));

        // Assert
        property.Should().NotBeNull();
        property!.PropertyType.Should().Be(typeof(int));
        property.CanRead.Should().BeTrue();
        property.CanWrite.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that GatherAsync method exists with correct signature.
    /// </summary>
    [Fact]
    public void IContextStrategy_HasGatherAsyncMethod()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Act
        var method = type.GetMethod(nameof(IContextStrategy.GatherAsync));

        // Assert
        method.Should().NotBeNull();
        method!.ReturnType.Should().Be(typeof(Task<ContextFragment?>));

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(2);
        parameters[0].ParameterType.Should().Be(typeof(ContextGatheringRequest));
        parameters[1].ParameterType.Should().Be(typeof(CancellationToken));
    }

    /// <summary>
    /// Verifies that IContextStrategy has exactly 5 members (4 properties + 1 method).
    /// </summary>
    [Fact]
    public void IContextStrategy_HasExactlyFiveMembers()
    {
        // Arrange
        var type = typeof(IContextStrategy);

        // Act
        var properties = type.GetProperties();
        var methods = type.GetMethods().Where(m => !m.IsSpecialName).ToArray(); // Exclude property getters

        // Assert
        properties.Should().HaveCount(4); // StrategyId, DisplayName, Priority, MaxTokens
        methods.Should().HaveCount(1); // GatherAsync
    }

    #endregion
}
