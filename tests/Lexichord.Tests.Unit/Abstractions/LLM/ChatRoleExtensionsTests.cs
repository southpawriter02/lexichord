// -----------------------------------------------------------------------
// <copyright file="ChatRoleExtensionsTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ChatRoleExtensions"/> class.
/// </summary>
public class ChatRoleExtensionsTests
{
    /// <summary>
    /// Tests ToProviderString for OpenAI.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, "system")]
    [InlineData(ChatRole.User, "user")]
    [InlineData(ChatRole.Assistant, "assistant")]
    [InlineData(ChatRole.Tool, "tool")]
    public void ToProviderString_ForOpenAI_ShouldReturnCorrectStrings(ChatRole role, string expected)
    {
        // Act
        var result = role.ToProviderString("openai");

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests ToProviderString for Anthropic.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, "system")]
    [InlineData(ChatRole.User, "user")]
    [InlineData(ChatRole.Assistant, "assistant")]
    [InlineData(ChatRole.Tool, "tool_result")]
    public void ToProviderString_ForAnthropic_ShouldReturnCorrectStrings(ChatRole role, string expected)
    {
        // Act
        var result = role.ToProviderString("anthropic");

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests ToProviderString is case-insensitive for provider.
    /// </summary>
    [Theory]
    [InlineData("OpenAI")]
    [InlineData("OPENAI")]
    [InlineData("openai")]
    public void ToProviderString_ShouldBeCaseInsensitive(string provider)
    {
        // Act
        var result = ChatRole.System.ToProviderString(provider);

        // Assert
        result.Should().Be("system");
    }

    /// <summary>
    /// Tests ToProviderString throws on null provider.
    /// </summary>
    [Fact]
    public void ToProviderString_WithNullProvider_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatRole.User.ToProviderString(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests FromProviderString parses common role strings.
    /// </summary>
    [Theory]
    [InlineData("system", ChatRole.System)]
    [InlineData("user", ChatRole.User)]
    [InlineData("assistant", ChatRole.Assistant)]
    [InlineData("tool", ChatRole.Tool)]
    [InlineData("tool_result", ChatRole.Tool)]
    [InlineData("function", ChatRole.Tool)]
    public void FromProviderString_WithValidStrings_ShouldReturnCorrectRole(string roleString, ChatRole expected)
    {
        // Act
        var result = ChatRoleExtensions.FromProviderString(roleString);

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests FromProviderString is case-insensitive.
    /// </summary>
    [Theory]
    [InlineData("User")]
    [InlineData("USER")]
    [InlineData("user")]
    public void FromProviderString_ShouldBeCaseInsensitive(string roleString)
    {
        // Act
        var result = ChatRoleExtensions.FromProviderString(roleString);

        // Assert
        result.Should().Be(ChatRole.User);
    }

    /// <summary>
    /// Tests FromProviderString throws on unknown role.
    /// </summary>
    [Fact]
    public void FromProviderString_WithUnknownRole_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatRoleExtensions.FromProviderString("unknown");

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithMessage("*Unrecognized role*");
    }

    /// <summary>
    /// Tests FromProviderString throws on null.
    /// </summary>
    [Fact]
    public void FromProviderString_WithNull_ShouldThrowArgumentException()
    {
        // Act
        var action = () => ChatRoleExtensions.FromProviderString(null!);

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests GetDisplayName returns human-readable names.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, "System")]
    [InlineData(ChatRole.User, "User")]
    [InlineData(ChatRole.Assistant, "AI Assistant")]
    [InlineData(ChatRole.Tool, "Tool Result")]
    public void GetDisplayName_ShouldReturnHumanReadableNames(ChatRole role, string expected)
    {
        // Act
        var result = role.GetDisplayName();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests IsAiGenerated for each role.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, false)]
    [InlineData(ChatRole.User, false)]
    [InlineData(ChatRole.Assistant, true)]
    [InlineData(ChatRole.Tool, false)]
    public void IsAiGenerated_ShouldReturnCorrectValue(ChatRole role, bool expected)
    {
        // Act
        var result = role.IsAiGenerated();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests IsHumanInput for each role.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, false)]
    [InlineData(ChatRole.User, true)]
    [InlineData(ChatRole.Assistant, false)]
    [InlineData(ChatRole.Tool, false)]
    public void IsHumanInput_ShouldReturnCorrectValue(ChatRole role, bool expected)
    {
        // Act
        var result = role.IsHumanInput();

        // Assert
        result.Should().Be(expected);
    }

    /// <summary>
    /// Tests IsMetadata for each role.
    /// </summary>
    [Theory]
    [InlineData(ChatRole.System, true)]
    [InlineData(ChatRole.User, false)]
    [InlineData(ChatRole.Assistant, false)]
    [InlineData(ChatRole.Tool, true)]
    public void IsMetadata_ShouldReturnCorrectValue(ChatRole role, bool expected)
    {
        // Act
        var result = role.IsMetadata();

        // Assert
        result.Should().Be(expected);
    }
}
