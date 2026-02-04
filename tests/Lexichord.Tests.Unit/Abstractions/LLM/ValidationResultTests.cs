// -----------------------------------------------------------------------
// <copyright file="ValidationResultTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="ValidationResult"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3a")]
public class ValidationResultTests
{
    /// <summary>
    /// Tests that Success factory creates a valid result.
    /// </summary>
    [Fact]
    public void Success_ShouldCreateValidResult()
    {
        // Act
        var result = ValidationResult.Success();

        // Assert
        result.IsValid.Should().BeTrue();
        result.MissingVariables.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.HasWarnings.Should().BeFalse();
        result.HasMissingVariables.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Failure factory creates an invalid result.
    /// </summary>
    [Fact]
    public void Failure_ShouldCreateInvalidResult()
    {
        // Arrange
        var missing = new[] { "var1", "var2" };

        // Act
        var result = ValidationResult.Failure(missing);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().BeEquivalentTo(missing);
        result.Warnings.Should().BeEmpty();
        result.HasMissingVariables.Should().BeTrue();
    }

    /// <summary>
    /// Tests that WithWarnings creates a valid result with warnings.
    /// </summary>
    [Fact]
    public void WithWarnings_ShouldCreateValidResultWithWarnings()
    {
        // Arrange
        var warnings = new[] { "Unused variable: extra" };

        // Act
        var result = ValidationResult.WithWarnings(warnings);

        // Assert
        result.IsValid.Should().BeTrue();
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain("Unused variable: extra");
        result.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Failure with warnings creates an invalid result with warnings.
    /// </summary>
    [Fact]
    public void Failure_WithWarnings_ShouldCreateInvalidResultWithWarnings()
    {
        // Arrange
        var missing = new[] { "required_var" };
        var warnings = new[] { "Optional variable not used" };

        // Act
        var result = ValidationResult.Failure(missing, warnings);

        // Assert
        result.IsValid.Should().BeFalse();
        result.MissingVariables.Should().Contain("required_var");
        result.HasWarnings.Should().BeTrue();
        result.Warnings.Should().Contain("Optional variable not used");
    }

    /// <summary>
    /// Tests that ErrorMessage contains missing variables when invalid.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenInvalid_ShouldContainMissingVariables()
    {
        // Arrange
        var result = ValidationResult.Failure(new[] { "input", "context" });

        // Assert
        result.ErrorMessage.Should().Contain("input");
        result.ErrorMessage.Should().Contain("context");
        result.ErrorMessage.Should().Contain("Missing required variables");
    }

    /// <summary>
    /// Tests that ErrorMessage is empty when valid.
    /// </summary>
    [Fact]
    public void ErrorMessage_WhenValid_ShouldBeEmpty()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Assert
        result.ErrorMessage.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that ThrowIfInvalid throws when invalid.
    /// </summary>
    [Fact]
    public void ThrowIfInvalid_WhenInvalid_ShouldThrowTemplateValidationException()
    {
        // Arrange
        var result = ValidationResult.Failure(new[] { "missing_var" });

        // Act
        var action = () => result.ThrowIfInvalid("test-template");

        // Assert
        action.Should().Throw<TemplateValidationException>()
            .WithMessage("*'test-template'*")
            .Where(ex => ex.MissingVariables.Contains("missing_var"));
    }

    /// <summary>
    /// Tests that ThrowIfInvalid does not throw when valid.
    /// </summary>
    [Fact]
    public void ThrowIfInvalid_WhenValid_ShouldNotThrow()
    {
        // Arrange
        var result = ValidationResult.Success();

        // Act
        var action = () => result.ThrowIfInvalid("test-template");

        // Assert
        action.Should().NotThrow();
    }

    /// <summary>
    /// Tests that ThrowIfInvalid includes template ID in exception.
    /// </summary>
    [Fact]
    public void ThrowIfInvalid_ShouldIncludeTemplateIdInException()
    {
        // Arrange
        var result = ValidationResult.Failure(new[] { "var1" });

        // Act
        var action = () => result.ThrowIfInvalid("my-template-id");

        // Assert
        action.Should().Throw<TemplateValidationException>()
            .Where(ex => ex.TemplateId == "my-template-id");
    }

    /// <summary>
    /// Tests that Failure throws on null missing variables.
    /// </summary>
    [Fact]
    public void Failure_WithNullMissingVariables_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => ValidationResult.Failure(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that WithWarnings throws on null warnings.
    /// </summary>
    [Fact]
    public void WithWarnings_WithNullWarnings_ShouldThrowArgumentNullException()
    {
        // Act
        var action = () => ValidationResult.WithWarnings(null!);

        // Assert
        action.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that Failure with warnings throws on null parameters.
    /// </summary>
    [Fact]
    public void Failure_WithNullParameters_ShouldThrowArgumentNullException()
    {
        // Act
        var action1 = () => ValidationResult.Failure(null!, new[] { "warning" });
        var action2 = () => ValidationResult.Failure(new[] { "var" }, null!);

        // Assert
        action1.Should().Throw<ArgumentNullException>();
        action2.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that ValidationResult has value equality.
    /// </summary>
    [Fact]
    public void ValidationResult_ShouldHaveValueEquality()
    {
        // Arrange
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();

        // Assert
        result1.Should().Be(result2);
        result1.GetHashCode().Should().Be(result2.GetHashCode());
    }

    /// <summary>
    /// Tests that MissingVariables defaults to empty when null in constructor.
    /// </summary>
    [Fact]
    public void Constructor_WithNullMissingVariables_ShouldDefaultToEmpty()
    {
        // Act
        var result = new ValidationResult(true, null!, null!);

        // Assert
        result.MissingVariables.Should().NotBeNull();
        result.MissingVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Warnings defaults to empty when null in constructor.
    /// </summary>
    [Fact]
    public void Constructor_WithNullWarnings_ShouldDefaultToEmpty()
    {
        // Act
        var result = new ValidationResult(true, Array.Empty<string>(), null!);

        // Assert
        result.Warnings.Should().NotBeNull();
        result.Warnings.Should().BeEmpty();
    }
}
