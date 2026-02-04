// -----------------------------------------------------------------------
// <copyright file="TemplateValidationExceptionTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="TemplateValidationException"/> class.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3a")]
public class TemplateValidationExceptionTests
{
    /// <summary>
    /// Tests that default constructor creates exception with default message.
    /// </summary>
    [Fact]
    public void DefaultConstructor_ShouldCreateExceptionWithDefaultMessage()
    {
        // Act
        var exception = new TemplateValidationException();

        // Assert
        exception.Message.Should().Contain("Template validation failed");
        exception.TemplateId.Should().BeNull();
        exception.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that message constructor creates exception with specified message.
    /// </summary>
    [Fact]
    public void MessageConstructor_ShouldCreateExceptionWithMessage()
    {
        // Act
        var exception = new TemplateValidationException("Custom message");

        // Assert
        exception.Message.Should().Be("Custom message");
        exception.TemplateId.Should().BeNull();
        exception.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that message with inner exception constructor works.
    /// </summary>
    [Fact]
    public void MessageAndInnerExceptionConstructor_ShouldSetInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner");

        // Act
        var exception = new TemplateValidationException("Outer message", innerException);

        // Assert
        exception.Message.Should().Be("Outer message");
        exception.InnerException.Should().Be(innerException);
        exception.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that constructor with template ID and missing variables works.
    /// </summary>
    [Fact]
    public void Constructor_WithTemplateIdAndMissingVariables_ShouldSetProperties()
    {
        // Arrange
        var missingVars = new[] { "var1", "var2" };

        // Act
        var exception = new TemplateValidationException("test-template", missingVars);

        // Assert
        exception.TemplateId.Should().Be("test-template");
        exception.MissingVariables.Should().BeEquivalentTo(missingVars);
        exception.Message.Should().Contain("test-template");
        exception.Message.Should().Contain("var1");
        exception.Message.Should().Contain("var2");
    }

    /// <summary>
    /// Tests that constructor with inner exception works.
    /// </summary>
    [Fact]
    public void Constructor_WithInnerException_ShouldSetInnerException()
    {
        // Arrange
        var missingVars = new[] { "input" };
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new TemplateValidationException("my-template", missingVars, innerException);

        // Assert
        exception.TemplateId.Should().Be("my-template");
        exception.MissingVariables.Should().Contain("input");
        exception.InnerException.Should().Be(innerException);
    }

    /// <summary>
    /// Tests that message contains template ID.
    /// </summary>
    [Fact]
    public void Message_ShouldContainTemplateId()
    {
        // Act
        var exception = new TemplateValidationException("unique-template-id", Array.Empty<string>());

        // Assert
        exception.Message.Should().Contain("'unique-template-id'");
    }

    /// <summary>
    /// Tests that message contains missing variables.
    /// </summary>
    [Fact]
    public void Message_ShouldContainMissingVariables()
    {
        // Arrange
        var missingVars = new[] { "user_input", "context", "style_rules" };

        // Act
        var exception = new TemplateValidationException("template", missingVars);

        // Assert
        exception.Message.Should().Contain("user_input");
        exception.Message.Should().Contain("context");
        exception.Message.Should().Contain("style_rules");
    }

    /// <summary>
    /// Tests that message shows "(none)" when no missing variables.
    /// </summary>
    [Fact]
    public void Message_WithEmptyMissingVariables_ShouldShowNone()
    {
        // Act
        var exception = new TemplateValidationException("template", Array.Empty<string>());

        // Assert
        exception.Message.Should().Contain("(none)");
    }

    /// <summary>
    /// Tests that constructor throws on null template ID.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTemplateId_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => new TemplateValidationException(null!, Array.Empty<string>());

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that null missing variables defaults to empty.
    /// </summary>
    [Fact]
    public void Constructor_WithNullMissingVariables_ShouldDefaultToEmpty()
    {
        // Act
        var exception = new TemplateValidationException("template", (IReadOnlyList<string>)null!);

        // Assert
        exception.MissingVariables.Should().NotBeNull();
        exception.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that exception is derived from Exception.
    /// </summary>
    [Fact]
    public void TemplateValidationException_ShouldDeriveFromException()
    {
        // Arrange
        var exception = new TemplateValidationException("test", Array.Empty<string>());

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    /// <summary>
    /// Tests that exception can be thrown and caught.
    /// </summary>
    [Fact]
    public void TemplateValidationException_ShouldBeThrowableAndCatchable()
    {
        // Arrange
        IReadOnlyList<string> missingVars = new[] { "required_var" };

        // Act
        Action action = () => throw new TemplateValidationException("my-template", missingVars);

        // Assert
        action.Should().Throw<TemplateValidationException>()
            .Where(ex => ex.TemplateId == "my-template")
            .Where(ex => ex.MissingVariables.Contains("required_var"));
    }

    /// <summary>
    /// Tests that exception message format is correct.
    /// </summary>
    [Fact]
    public void Message_ShouldHaveCorrectFormat()
    {
        // Arrange
        var exception = new TemplateValidationException("test-template", new[] { "a", "b" });

        // Assert
        exception.Message.Should().Be(
            "Template 'test-template' validation failed. Missing required variables: a, b");
    }
}
