// -----------------------------------------------------------------------
// <copyright file="PromptTemplateTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.LLM;

/// <summary>
/// Unit tests for <see cref="PromptTemplate"/> record.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Version", "v0.6.3a")]
public class PromptTemplateTests
{
    /// <summary>
    /// Tests that Create factory with minimal args sets defaults.
    /// </summary>
    [Fact]
    public void Create_WithMinimalArgs_ShouldSetDefaults()
    {
        // Act
        var template = PromptTemplate.Create(
            "test-id",
            "Test Template",
            "System prompt",
            "{{input}}");

        // Assert
        template.TemplateId.Should().Be("test-id");
        template.Name.Should().Be("Test Template");
        template.Description.Should().BeEmpty();
        template.SystemPromptTemplate.Should().Be("System prompt");
        template.UserPromptTemplate.Should().Be("{{input}}");
        template.RequiredVariables.Should().BeEmpty();
        template.OptionalVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that Create factory sets variables correctly.
    /// </summary>
    [Fact]
    public void Create_WithVariables_ShouldSetVariables()
    {
        // Act
        var template = PromptTemplate.Create(
            "test",
            "Test",
            "{{a}} {{b}}",
            "{{c}}",
            requiredVariables: new[] { "a", "b" },
            optionalVariables: new[] { "c", "d" });

        // Assert
        template.RequiredVariables.Should().BeEquivalentTo(new[] { "a", "b" });
        template.OptionalVariables.Should().BeEquivalentTo(new[] { "c", "d" });
    }

    /// <summary>
    /// Tests that AllVariables combines required and optional.
    /// </summary>
    [Fact]
    public void AllVariables_ShouldCombineRequiredAndOptional()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "{{a}} {{b}}", "{{c}}",
            requiredVariables: new[] { "a", "b" },
            optionalVariables: new[] { "c", "d" });

        // Assert
        template.AllVariables.Should().BeEquivalentTo(new[] { "a", "b", "c", "d" });
    }

    /// <summary>
    /// Tests that VariableCount returns correct total.
    /// </summary>
    [Fact]
    public void VariableCount_ShouldReturnCorrectTotal()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "sys", "user",
            requiredVariables: new[] { "a", "b" },
            optionalVariables: new[] { "c" });

        // Assert
        template.VariableCount.Should().Be(3);
    }

    /// <summary>
    /// Tests that HasVariable returns true for existing variable.
    /// </summary>
    [Fact]
    public void HasVariable_WithExisting_ShouldReturnTrue()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "sys", "user",
            requiredVariables: new[] { "input" });

        // Assert
        template.HasVariable("input").Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasVariable is case-insensitive.
    /// </summary>
    [Fact]
    public void HasVariable_ShouldBeCaseInsensitive()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "sys", "user",
            requiredVariables: new[] { "user_input" });

        // Assert
        template.HasVariable("USER_INPUT").Should().BeTrue();
        template.HasVariable("User_Input").Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasVariable returns false for non-existing variable.
    /// </summary>
    [Fact]
    public void HasVariable_WithNonExisting_ShouldReturnFalse()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "sys", "user",
            requiredVariables: new[] { "input" });

        // Assert
        template.HasVariable("other").Should().BeFalse();
    }

    /// <summary>
    /// Tests that records with same values are equal.
    /// </summary>
    [Fact]
    public void Records_WithSameValues_ShouldBeEqual()
    {
        // Arrange
        var template1 = PromptTemplate.Create("id", "name", "sys", "user");
        var template2 = PromptTemplate.Create("id", "name", "sys", "user");

        // Assert
        template1.Should().Be(template2);
        template1.GetHashCode().Should().Be(template2.GetHashCode());
    }

    /// <summary>
    /// Tests that Create throws on invalid template ID.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidTemplateId_ShouldThrow(string? invalidId)
    {
        // Act
        var action = () => PromptTemplate.Create(invalidId!, "name", "sys", "user");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that Create throws on invalid name.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidName_ShouldThrow(string? invalidName)
    {
        // Act
        var action = () => PromptTemplate.Create("id", invalidName!, "sys", "user");

        // Assert
        action.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Tests that constructor validates template ID.
    /// </summary>
    [Fact]
    public void Constructor_WithNullTemplateId_ShouldThrow()
    {
        // Act
        var action = () => new PromptTemplate(
            null!, "name", "desc", "sys", "user",
            Array.Empty<string>(), Array.Empty<string>());

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("TemplateId");
    }

    /// <summary>
    /// Tests that constructor validates name.
    /// </summary>
    [Fact]
    public void Constructor_WithNullName_ShouldThrow()
    {
        // Act
        var action = () => new PromptTemplate(
            "id", null!, "desc", "sys", "user",
            Array.Empty<string>(), Array.Empty<string>());

        // Assert
        action.Should().Throw<ArgumentException>()
            .WithParameterName("Name");
    }

    /// <summary>
    /// Tests that null prompts default to empty strings.
    /// </summary>
    [Fact]
    public void Constructor_WithNullPrompts_ShouldDefaultToEmpty()
    {
        // Act
        var template = new PromptTemplate(
            "id", "name", null!, null!, null!,
            null!, null!);

        // Assert
        template.Description.Should().BeEmpty();
        template.SystemPromptTemplate.Should().BeEmpty();
        template.UserPromptTemplate.Should().BeEmpty();
        template.RequiredVariables.Should().BeEmpty();
        template.OptionalVariables.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that HasRequiredVariables returns correct value.
    /// </summary>
    [Fact]
    public void HasRequiredVariables_WithRequired_ShouldReturnTrue()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "sys", "user",
            requiredVariables: new[] { "input" });

        // Assert
        template.HasRequiredVariables.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasRequiredVariables returns false when empty.
    /// </summary>
    [Fact]
    public void HasRequiredVariables_WithoutRequired_ShouldReturnFalse()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "user");

        // Assert
        template.HasRequiredVariables.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasOptionalVariables returns correct value.
    /// </summary>
    [Fact]
    public void HasOptionalVariables_WithOptional_ShouldReturnTrue()
    {
        // Arrange
        var template = PromptTemplate.Create(
            "test", "Test", "sys", "user",
            optionalVariables: new[] { "extra" });

        // Assert
        template.HasOptionalVariables.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasOptionalVariables returns false when empty.
    /// </summary>
    [Fact]
    public void HasOptionalVariables_WithoutOptional_ShouldReturnFalse()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "user");

        // Assert
        template.HasOptionalVariables.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasSystemPrompt returns correct value.
    /// </summary>
    [Fact]
    public void HasSystemPrompt_WithSystemPrompt_ShouldReturnTrue()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "system prompt", "user");

        // Assert
        template.HasSystemPrompt.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasSystemPrompt returns false when empty.
    /// </summary>
    [Fact]
    public void HasSystemPrompt_WithEmptySystemPrompt_ShouldReturnFalse()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "", "user");

        // Assert
        template.HasSystemPrompt.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasUserPrompt returns correct value.
    /// </summary>
    [Fact]
    public void HasUserPrompt_WithUserPrompt_ShouldReturnTrue()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "user prompt");

        // Assert
        template.HasUserPrompt.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasUserPrompt returns false when empty.
    /// </summary>
    [Fact]
    public void HasUserPrompt_WithEmptyUserPrompt_ShouldReturnFalse()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "");

        // Assert
        template.HasUserPrompt.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasDescription returns correct value.
    /// </summary>
    [Fact]
    public void HasDescription_WithDescription_ShouldReturnTrue()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "user") with
        {
            Description = "A description"
        };

        // Assert
        template.HasDescription.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasDescription returns false when empty.
    /// </summary>
    [Fact]
    public void HasDescription_WithEmptyDescription_ShouldReturnFalse()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "user");

        // Assert
        template.HasDescription.Should().BeFalse();
    }

    /// <summary>
    /// Tests that template implements IPromptTemplate.
    /// </summary>
    [Fact]
    public void PromptTemplate_ShouldImplementIPromptTemplate()
    {
        // Arrange
        var template = PromptTemplate.Create("test", "Test", "sys", "user");

        // Assert
        template.Should().BeAssignableTo<IPromptTemplate>();
    }

    /// <summary>
    /// Tests that with-expressions work for modifications.
    /// </summary>
    [Fact]
    public void WithExpression_ShouldCreateModifiedCopy()
    {
        // Arrange
        var original = PromptTemplate.Create("test", "Test", "sys", "user");

        // Act
        var modified = original with { Description = "New description" };

        // Assert
        modified.Description.Should().Be("New description");
        original.Description.Should().BeEmpty();
        modified.TemplateId.Should().Be(original.TemplateId);
    }
}
