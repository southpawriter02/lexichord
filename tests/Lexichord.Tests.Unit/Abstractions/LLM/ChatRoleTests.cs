// -----------------------------------------------------------------------
// <copyright file="ChatRoleTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatRole"/> enum.
/// </summary>
public class ChatRoleTests
{
    /// <summary>
    /// Verifies that all expected ChatRole values are defined.
    /// </summary>
    [Fact]
    public void ChatRole_ShouldHaveExpectedValues()
    {
        // Assert
        Enum.GetValues<ChatRole>().Should().HaveCount(4);
        Enum.IsDefined(ChatRole.System).Should().BeTrue();
        Enum.IsDefined(ChatRole.User).Should().BeTrue();
        Enum.IsDefined(ChatRole.Assistant).Should().BeTrue();
        Enum.IsDefined(ChatRole.Tool).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that ChatRole values have expected integer representations.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, 0)]
    [InlineData(ChatRole.User, 1)]
    [InlineData(ChatRole.Assistant, 2)]
    [InlineData(ChatRole.Tool, 3)]
    public void ChatRole_ShouldHaveExpectedIntegerValue(ChatRole role, int expectedValue)
    {
        // Act
        var actualValue = (int)role;

        // Assert
        actualValue.Should().Be(expectedValue);
    }
}
