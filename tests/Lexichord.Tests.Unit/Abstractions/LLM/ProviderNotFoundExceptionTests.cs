// -----------------------------------------------------------------------
// <copyright file="ProviderNotFoundExceptionTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ProviderNotFoundException"/>.
/// </summary>
public class ProviderNotFoundExceptionTests
{
    /// <summary>
    /// Tests default constructor creates instance with default message.
    /// </summary>
    [Fact]
    public void DefaultConstructor_ShouldCreateInstanceWithDefaultMessage()
    {
        // Act
        var exception = new ProviderNotFoundException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().NotBeNullOrEmpty();
        exception.ProviderName.Should().BeEmpty();
        exception.HasProviderName.Should().BeFalse();
    }

    /// <summary>
    /// Tests constructor with provider name only.
    /// </summary>
    [Fact]
    public void Constructor_WithProviderName_ShouldSetProviderAndMessage()
    {
        // Arrange
        const string providerName = "unknown-provider";

        // Act
        var exception = new ProviderNotFoundException(providerName);

        // Assert
        exception.ProviderName.Should().Be(providerName);
        exception.HasProviderName.Should().BeTrue();
        exception.Message.Should().Contain(providerName);
    }

    /// <summary>
    /// Tests constructor with provider name and custom message.
    /// </summary>
    [Fact]
    public void Constructor_WithProviderNameAndMessage_ShouldSetBoth()
    {
        // Arrange
        const string providerName = "custom-provider";
        const string message = "Custom error message";

        // Act
        var exception = new ProviderNotFoundException(providerName, message);

        // Assert
        exception.ProviderName.Should().Be(providerName);
        exception.Message.Should().Be(message);
    }

    /// <summary>
    /// Tests constructor with message and inner exception.
    /// </summary>
    [Fact]
    public void Constructor_WithMessageAndInnerException_ShouldSetInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Inner error");
        const string message = "Outer error";

        // Act
        var exception = new ProviderNotFoundException(message, inner);

        // Assert
        exception.InnerException.Should().Be(inner);
        exception.Message.Should().Be(message);
        exception.ProviderName.Should().BeEmpty();
    }

    /// <summary>
    /// Tests constructor with provider name, message, and inner exception.
    /// </summary>
    [Fact]
    public void Constructor_WithProviderNameMessageAndInnerException_ShouldSetAll()
    {
        // Arrange
        const string providerName = "test-provider";
        const string message = "Test error";
        var inner = new ArgumentException("Arg error");

        // Act
        var exception = new ProviderNotFoundException(providerName, message, inner);

        // Assert
        exception.ProviderName.Should().Be(providerName);
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(inner);
    }

    /// <summary>
    /// Tests that constructor with null provider name throws.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProviderName_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ProviderNotFoundException(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that constructor with null provider name and message throws.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProviderNameAndMessage_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ProviderNotFoundException(null!, "message");

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that constructor with null provider name, message, and inner throws.
    /// </summary>
    [Fact]
    public void Constructor_WithNullProviderNameMessageAndInner_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new ProviderNotFoundException(null!, "message", new Exception());

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that exception inherits from Exception (not ChatCompletionException).
    /// </summary>
    [Fact]
    public void Inheritance_ShouldDeriveFromException()
    {
        // Assert
        typeof(ProviderNotFoundException).Should().BeDerivedFrom<Exception>();
    }

    /// <summary>
    /// Tests that exception does NOT inherit from ChatCompletionException.
    /// </summary>
    [Fact]
    public void Inheritance_ShouldNotDeriveFromChatCompletionException()
    {
        // Assert
        typeof(ProviderNotFoundException).Should().NotBeDerivedFrom<ChatCompletionException>();
    }

    /// <summary>
    /// Tests catching exception as base Exception type.
    /// </summary>
    [Fact]
    public void CatchingAsBaseException_ShouldWork()
    {
        // Arrange
        Exception? caught = null;

        // Act
        try
        {
            throw new ProviderNotFoundException("test-provider");
        }
        catch (Exception ex)
        {
            caught = ex;
        }

        // Assert
        caught.Should().NotBeNull();
        caught.Should().BeOfType<ProviderNotFoundException>();
        ((ProviderNotFoundException)caught!).ProviderName.Should().Be("test-provider");
    }

    /// <summary>
    /// Tests default message format includes provider name.
    /// </summary>
    [Fact]
    public void DefaultMessage_ShouldIncludeProviderName()
    {
        // Arrange
        const string providerName = "my-custom-provider";

        // Act
        var exception = new ProviderNotFoundException(providerName);

        // Assert
        exception.Message.Should().Contain(providerName);
        exception.Message.Should().Contain("not registered");
    }

    /// <summary>
    /// Tests HasProviderName returns correct value for empty string.
    /// </summary>
    [Fact]
    public void HasProviderName_WithEmptyString_ShouldReturnFalse()
    {
        // Arrange
        var exception = new ProviderNotFoundException();

        // Assert
        exception.HasProviderName.Should().BeFalse();
    }

    /// <summary>
    /// Tests HasProviderName returns true for valid provider.
    /// </summary>
    [Fact]
    public void HasProviderName_WithValidProvider_ShouldReturnTrue()
    {
        // Arrange
        var exception = new ProviderNotFoundException("provider");

        // Assert
        exception.HasProviderName.Should().BeTrue();
    }
}
