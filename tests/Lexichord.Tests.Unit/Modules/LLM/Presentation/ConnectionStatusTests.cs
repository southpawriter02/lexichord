// -----------------------------------------------------------------------
// <copyright file="ConnectionStatusTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.LLM.Presentation;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.LLM.Presentation;

/// <summary>
/// Unit tests for <see cref="ConnectionStatus"/> enum.
/// </summary>
/// <remarks>
/// Tests cover:
/// <list type="bullet">
///   <item><description>Enum value assignments</description></item>
///   <item><description>Default value behavior</description></item>
///   <item><description>All expected values exist</description></item>
/// </list>
/// <para>
/// <b>Version:</b> v0.6.1d
/// </para>
/// </remarks>
public class ConnectionStatusTests
{
    /// <summary>
    /// Tests that Unknown has value 0.
    /// </summary>
    [Fact]
    public void Unknown_ShouldHaveValueZero()
    {
        // Assert
        ((int)ConnectionStatus.Unknown).Should().Be(0);
    }

    /// <summary>
    /// Tests that Checking has value 1.
    /// </summary>
    [Fact]
    public void Checking_ShouldHaveValueOne()
    {
        // Assert
        ((int)ConnectionStatus.Checking).Should().Be(1);
    }

    /// <summary>
    /// Tests that Connected has value 2.
    /// </summary>
    [Fact]
    public void Connected_ShouldHaveValueTwo()
    {
        // Assert
        ((int)ConnectionStatus.Connected).Should().Be(2);
    }

    /// <summary>
    /// Tests that Failed has value 3.
    /// </summary>
    [Fact]
    public void Failed_ShouldHaveValueThree()
    {
        // Assert
        ((int)ConnectionStatus.Failed).Should().Be(3);
    }

    /// <summary>
    /// Tests that default value is Unknown.
    /// </summary>
    [Fact]
    public void Default_ShouldBeUnknown()
    {
        // Arrange
        ConnectionStatus defaultValue = default;

        // Assert
        defaultValue.Should().Be(ConnectionStatus.Unknown);
    }

    /// <summary>
    /// Tests that all expected values exist.
    /// </summary>
    [Fact]
    public void Enum_ShouldHaveAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<ConnectionStatus>();
        values.Should().HaveCount(4);
        values.Should().Contain(ConnectionStatus.Unknown);
        values.Should().Contain(ConnectionStatus.Checking);
        values.Should().Contain(ConnectionStatus.Connected);
        values.Should().Contain(ConnectionStatus.Failed);
    }

    /// <summary>
    /// Tests that enum can be cast from int.
    /// </summary>
    [Theory]
    [InlineData(0, ConnectionStatus.Unknown)]
    [InlineData(1, ConnectionStatus.Checking)]
    [InlineData(2, ConnectionStatus.Connected)]
    [InlineData(3, ConnectionStatus.Failed)]
    public void CastFromInt_ShouldReturnExpectedValue(int input, ConnectionStatus expected)
    {
        // Act
        var result = (ConnectionStatus)input;

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests that enum names are as expected.
    /// </summary>
    [Theory]
    [InlineData(ConnectionStatus.Unknown, "Unknown")]
    [InlineData(ConnectionStatus.Checking, "Checking")]
    [InlineData(ConnectionStatus.Connected, "Connected")]
    [InlineData(ConnectionStatus.Failed, "Failed")]
    public void ToString_ShouldReturnExpectedName(ConnectionStatus status, string expectedName)
    {
        // Act
        var result = status.ToString();

        // Assert
        result.Should().Be(expectedName);
    }

    /// <summary>
    /// Tests that enum can be parsed from string.
    /// </summary>
    [Theory]
    [InlineData("Unknown", ConnectionStatus.Unknown)]
    [InlineData("Checking", ConnectionStatus.Checking)]
    [InlineData("Connected", ConnectionStatus.Connected)]
    [InlineData("Failed", ConnectionStatus.Failed)]
    public void Parse_ShouldReturnExpectedValue(string input, ConnectionStatus expected)
    {
        // Act
        var result = Enum.Parse<ConnectionStatus>(input);

        // Assert
        result.Should().Be(expected);
    }
}
