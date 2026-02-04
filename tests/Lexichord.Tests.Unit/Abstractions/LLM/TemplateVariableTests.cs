// -----------------------------------------------------------------------
// <copyright file="TemplateVariableTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="TemplateVariable"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3a")]
public class TemplateVariableTests
{
    /// <summary>
    /// Tests that Required factory creates a required variable.
    /// </summary>
    [Fact]
    public void Required_WithValidName_ShouldCreateRequiredVariable()
    {
        // Act
        var variable = TemplateVariable.Required("user_input", "The user's question");

        // Assert
        variable.Name.Should().Be("user_input");
        variable.IsRequired.Should().BeTrue();
        variable.Description.Should().Be("The user's question");
        variable.DefaultValue.Should().BeNull();
    }

    /// <summary>
    /// Tests that Required factory works without description.
    /// </summary>
    [Fact]
    public void Required_WithoutDescription_ShouldCreateRequiredVariable()
    {
        // Act
        var variable = TemplateVariable.Required("context");

        // Assert
        variable.Name.Should().Be("context");
        variable.IsRequired.Should().BeTrue();
        variable.Description.Should().BeNull();
        variable.HasDescription.Should().BeFalse();
    }

    /// <summary>
    /// Tests that Optional factory creates an optional variable.
    /// </summary>
    [Fact]
    public void Optional_WithDefaultValue_ShouldCreateOptionalVariable()
    {
        // Act
        var variable = TemplateVariable.Optional("language", "Target language", "English");

        // Assert
        variable.Name.Should().Be("language");
        variable.IsRequired.Should().BeFalse();
        variable.Description.Should().Be("Target language");
        variable.DefaultValue.Should().Be("English");
        variable.HasDefaultValue.Should().BeTrue();
    }

    /// <summary>
    /// Tests that Optional factory works without default value.
    /// </summary>
    [Fact]
    public void Optional_WithoutDefaultValue_ShouldCreateOptionalVariable()
    {
        // Act
        var variable = TemplateVariable.Optional("format");

        // Assert
        variable.Name.Should().Be("format");
        variable.IsRequired.Should().BeFalse();
        variable.DefaultValue.Should().BeNull();
        variable.HasDefaultValue.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasDefaultValue returns correct value.
    /// </summary>
    [Fact]
    public void HasDefaultValue_WithNullDefault_ShouldReturnFalse()
    {
        // Arrange
        var variable = new TemplateVariable("test", false, "desc", null);

        // Assert
        variable.HasDefaultValue.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasDescription returns correct value.
    /// </summary>
    [Fact]
    public void HasDescription_WithDescription_ShouldReturnTrue()
    {
        // Arrange
        var variable = TemplateVariable.Required("test", "A test variable");

        // Assert
        variable.HasDescription.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasDescription returns false for whitespace description.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasDescription_WithEmptyOrWhitespace_ShouldReturnFalse(string? description)
    {
        // Arrange
        var variable = new TemplateVariable("test", true, description);

        // Assert
        variable.HasDescription.Should().BeFalse();
    }

    /// <summary>
    /// Tests that constructor throws on null name.
    /// </summary>
    [Fact]
    public void Constructor_WithNullName_ShouldThrowArgumentException()
    {
        // Act
        var action = () => new TemplateVariable(null!, true);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("Name");
    }

    /// <summary>
    /// Tests that constructor throws on empty name.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Constructor_WithEmptyOrWhitespaceName_ShouldThrowArgumentException(string name)
    {
        // Act
        var action = () => new TemplateVariable(name, true);

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("Name");
    }

    /// <summary>
    /// Tests that TemplateVariable has value equality.
    /// </summary>
    [Fact]
    public void TemplateVariable_ShouldHaveValueEquality()
    {
        // Arrange
        var var1 = TemplateVariable.Required("input", "desc");
        var var2 = TemplateVariable.Required("input", "desc");

        // Assert
        var1.Should().Be(var2);
        var1.GetHashCode().Should().Be(var2.GetHashCode());
    }

    /// <summary>
    /// Tests that different TemplateVariables are not equal.
    /// </summary>
    [Fact]
    public void TemplateVariable_WithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var var1 = TemplateVariable.Required("input");
        var var2 = TemplateVariable.Optional("input");

        // Assert
        var1.Should().NotBe(var2);
    }

    /// <summary>
    /// Tests that direct construction works correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAllParameters_ShouldCreateVariable()
    {
        // Act
        var variable = new TemplateVariable("test", true, "Description", "default");

        // Assert
        variable.Name.Should().Be("test");
        variable.IsRequired.Should().BeTrue();
        variable.Description.Should().Be("Description");
        variable.DefaultValue.Should().Be("default");
    }
}
