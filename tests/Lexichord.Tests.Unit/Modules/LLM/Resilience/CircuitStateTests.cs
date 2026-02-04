// -----------------------------------------------------------------------
// <copyright file="CircuitStateTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Resilience;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Resilience;

/// <summary>
/// Unit tests for <see cref="CircuitState"/>.
/// </summary>
public class CircuitStateTests
{
    #region Enum Value Tests

    /// <summary>
    /// Tests that Closed has the expected underlying value.
    /// </summary>
    [Fact]
    public void Closed_ShouldHaveValueZero()
    {
        // Assert
        ((int)CircuitState.Closed).Should().Be(0);
    }

    /// <summary>
    /// Tests that Open has the expected underlying value.
    /// </summary>
    [Fact]
    public void Open_ShouldHaveValueOne()
    {
        // Assert
        ((int)CircuitState.Open).Should().Be(1);
    }

    /// <summary>
    /// Tests that HalfOpen has the expected underlying value.
    /// </summary>
    [Fact]
    public void HalfOpen_ShouldHaveValueTwo()
    {
        // Assert
        ((int)CircuitState.HalfOpen).Should().Be(2);
    }

    /// <summary>
    /// Tests that Isolated has the expected underlying value.
    /// </summary>
    [Fact]
    public void Isolated_ShouldHaveValueThree()
    {
        // Assert
        ((int)CircuitState.Isolated).Should().Be(3);
    }

    #endregion

    #region Enum Member Count Tests

    /// <summary>
    /// Tests that the enum has exactly 4 members.
    /// </summary>
    [Fact]
    public void CircuitState_ShouldHaveFourMembers()
    {
        // Act
        var values = Enum.GetValues<CircuitState>();

        // Assert
        values.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that all expected values are present.
    /// </summary>
    [Fact]
    public void CircuitState_ShouldContainAllExpectedValues()
    {
        // Act
        var values = Enum.GetValues<CircuitState>();

        // Assert
        values.Should().Contain(CircuitState.Closed);
        values.Should().Contain(CircuitState.Open);
        values.Should().Contain(CircuitState.HalfOpen);
        values.Should().Contain(CircuitState.Isolated);
    }

    #endregion

    #region String Representation Tests

    /// <summary>
    /// Tests that Closed converts to expected string.
    /// </summary>
    [Fact]
    public void Closed_ToString_ShouldReturnClosed()
    {
        // Assert
        CircuitState.Closed.ToString().Should().Be("Closed");
    }

    /// <summary>
    /// Tests that Open converts to expected string.
    /// </summary>
    [Fact]
    public void Open_ToString_ShouldReturnOpen()
    {
        // Assert
        CircuitState.Open.ToString().Should().Be("Open");
    }

    /// <summary>
    /// Tests that HalfOpen converts to expected string.
    /// </summary>
    [Fact]
    public void HalfOpen_ToString_ShouldReturnHalfOpen()
    {
        // Assert
        CircuitState.HalfOpen.ToString().Should().Be("HalfOpen");
    }

    /// <summary>
    /// Tests that Isolated converts to expected string.
    /// </summary>
    [Fact]
    public void Isolated_ToString_ShouldReturnIsolated()
    {
        // Assert
        CircuitState.Isolated.ToString().Should().Be("Isolated");
    }

    #endregion

    #region Parse Tests

    /// <summary>
    /// Tests that parsing "Closed" returns CircuitState.Closed.
    /// </summary>
    [Fact]
    public void Parse_Closed_ShouldReturnClosedState()
    {
        // Act
        var result = Enum.Parse<CircuitState>("Closed");

        // Assert
        result.Should().Be(CircuitState.Closed);
    }

    /// <summary>
    /// Tests that parsing "Open" returns CircuitState.Open.
    /// </summary>
    [Fact]
    public void Parse_Open_ShouldReturnOpenState()
    {
        // Act
        var result = Enum.Parse<CircuitState>("Open");

        // Assert
        result.Should().Be(CircuitState.Open);
    }

    /// <summary>
    /// Tests that parsing "HalfOpen" returns CircuitState.HalfOpen.
    /// </summary>
    [Fact]
    public void Parse_HalfOpen_ShouldReturnHalfOpenState()
    {
        // Act
        var result = Enum.Parse<CircuitState>("HalfOpen");

        // Assert
        result.Should().Be(CircuitState.HalfOpen);
    }

    /// <summary>
    /// Tests that parsing "Isolated" returns CircuitState.Isolated.
    /// </summary>
    [Fact]
    public void Parse_Isolated_ShouldReturnIsolatedState()
    {
        // Act
        var result = Enum.Parse<CircuitState>("Isolated");

        // Assert
        result.Should().Be(CircuitState.Isolated);
    }

    /// <summary>
    /// Tests that parsing an invalid value throws ArgumentException.
    /// </summary>
    [Fact]
    public void Parse_InvalidValue_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Enum.Parse<CircuitState>("Invalid");
        act.Should().Throw<ArgumentException>();
    }

    #endregion

    #region Default Value Tests

    /// <summary>
    /// Tests that the default value of CircuitState is Closed.
    /// </summary>
    [Fact]
    public void DefaultValue_ShouldBeClosed()
    {
        // Act
        CircuitState defaultState = default;

        // Assert
        defaultState.Should().Be(CircuitState.Closed);
    }

    #endregion

    #region Comparison Tests

    /// <summary>
    /// Tests that enum values can be compared for equality.
    /// </summary>
    [Fact]
    public void Equality_SameValues_ShouldBeEqual()
    {
        // Arrange
        CircuitState state1 = CircuitState.Open;
        CircuitState state2 = CircuitState.Open;

        // Assert
        state1.Should().Be(state2);
        (state1 == state2).Should().BeTrue();
    }

    /// <summary>
    /// Tests that different enum values are not equal.
    /// </summary>
    [Fact]
    public void Equality_DifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        CircuitState state1 = CircuitState.Open;
        CircuitState state2 = CircuitState.Closed;

        // Assert
        state1.Should().NotBe(state2);
        (state1 != state2).Should().BeTrue();
    }

    #endregion
}
