// -----------------------------------------------------------------------
// <copyright file="StrategyPriorityTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Context;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents.Context;

/// <summary>
/// Unit tests for <see cref="StrategyPriority"/> static class.
/// </summary>
/// <remarks>
/// Tests verify that priority constants have the correct values.
/// Introduced in v0.7.2a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.2a")]
public class StrategyPriorityTests
{
    #region Constant Values

    [Fact]
    public void Critical_Equals100()
    {
        // Assert
        StrategyPriority.Critical.Should().Be(100);
    }

    [Fact]
    public void High_Equals80()
    {
        // Assert
        StrategyPriority.High.Should().Be(80);
    }

    [Fact]
    public void Medium_Equals60()
    {
        // Assert
        StrategyPriority.Medium.Should().Be(60);
    }

    [Fact]
    public void Low_Equals40()
    {
        // Assert
        StrategyPriority.Low.Should().Be(40);
    }

    [Fact]
    public void Optional_Equals20()
    {
        // Assert
        StrategyPriority.Optional.Should().Be(20);
    }

    #endregion

    #region Priority Ordering

    [Fact]
    public void PrioritiesAreOrderedDescending()
    {
        // Assert - Higher priority values should be greater
        StrategyPriority.Critical.Should().BeGreaterThan(StrategyPriority.High);
        StrategyPriority.High.Should().BeGreaterThan(StrategyPriority.Medium);
        StrategyPriority.Medium.Should().BeGreaterThan(StrategyPriority.Low);
        StrategyPriority.Low.Should().BeGreaterThan(StrategyPriority.Optional);
    }

    #endregion
}
