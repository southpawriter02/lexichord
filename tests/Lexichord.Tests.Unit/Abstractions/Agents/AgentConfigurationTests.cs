// -----------------------------------------------------------------------
// <copyright file="AgentConfigurationTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts.LLM;
using Xunit;

namespace Lexichord.Tests.Unit.Abstractions.Agents;

/// <summary>
/// Unit tests for <see cref="AgentConfiguration"/> record.
/// </summary>
/// <remarks>
/// Tests cover validation rules, persona lookup methods, default persona behavior,
/// record equality, and handling of duplicate persona IDs.
/// Introduced in v0.7.1a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1a")]
public class AgentConfigurationTests
{
    /// <summary>
    /// Verifies that a configuration with valid data passes validation.
    /// </summary>
    [Fact]
    public void AgentConfiguration_WithValidData_PassesValidation()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var errors = config.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that configurations with invalid AgentIds fail validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("Invalid ID")]
    [InlineData("UPPERCASE")]
    [InlineData("has spaces")]
    public void AgentConfiguration_WithInvalidAgentId_FailsValidation(string agentId)
    {
        // Arrange
        var config = CreateValidConfiguration() with { AgentId = agentId };

        // Act
        var errors = config.Validate();

        // Assert
        errors.Should().Contain(e => e.Contains("AgentId"));
    }

    /// <summary>
    /// Verifies that configurations with duplicate persona IDs fail validation.
    /// </summary>
    [Fact]
    public void AgentConfiguration_WithDuplicatePersonaIds_FailsValidation()
    {
        // Arrange
        var config = CreateValidConfiguration() with
        {
            Personas = new[]
            {
                new AgentPersona("duplicate", "First", "Tag", null, 0.5),
                new AgentPersona("duplicate", "Second", "Tag", null, 0.3)
            }
        };

        // Act
        var errors = config.Validate();

        // Assert
        errors.Should().Contain(e => e.Contains("Duplicate persona ID"));
    }

    /// <summary>
    /// Verifies that configurations with same values have equivalent properties.
    /// </summary>
    /// <remarks>
    /// NOTE: Record equality compares collection references, not contents.
    /// This test verifies structural equivalence instead of reference equality.
    /// </remarks>
    [Fact]
    public void AgentConfiguration_Equality_SameValues_AreEqual()
    {
        // Arrange
        var config1 = CreateValidConfiguration();
        var config2 = CreateValidConfiguration();

        // Assert
        config1.AgentId.Should().Be(config2.AgentId);
        config1.Name.Should().Be(config2.Name);
        config1.Description.Should().Be(config2.Description);
        config1.Icon.Should().Be(config2.Icon);
        config1.TemplateId.Should().Be(config2.TemplateId);
        config1.Capabilities.Should().Be(config2.Capabilities);
        config1.RequiredTier.Should().Be(config2.RequiredTier);
        config1.Personas.Should().HaveCount(config2.Personas.Count);
        config1.DefaultPersona?.PersonaId.Should().Be(config2.DefaultPersona?.PersonaId);
    }

    /// <summary>
    /// Verifies that GetPersona() returns the correct persona by ID.
    /// </summary>
    [Fact]
    public void AgentConfiguration_GetPersona_ReturnsCorrectPersona()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var persona = config.GetPersona("strict");

        // Assert
        persona.Should().NotBeNull();
        persona!.DisplayName.Should().Be("Strict Editor");
    }

    /// <summary>
    /// Verifies that GetPersona() returns null for unknown persona IDs.
    /// </summary>
    [Fact]
    public void AgentConfiguration_GetPersona_UnknownId_ReturnsNull()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var persona = config.GetPersona("nonexistent");

        // Assert
        persona.Should().BeNull();
    }

    /// <summary>
    /// Verifies that DefaultPersona returns the first persona in the collection.
    /// </summary>
    [Fact]
    public void AgentConfiguration_DefaultPersona_ReturnsFirst()
    {
        // Arrange
        var config = CreateValidConfiguration();

        // Act
        var defaultPersona = config.DefaultPersona;

        // Assert
        defaultPersona.Should().NotBeNull();
        defaultPersona!.PersonaId.Should().Be("strict");
    }

    /// <summary>
    /// Verifies that DefaultPersona returns null when no personas are defined.
    /// </summary>
    [Fact]
    public void AgentConfiguration_WithNoPersonas_DefaultPersonaIsNull()
    {
        // Arrange
        var config = CreateValidConfiguration() with { Personas = Array.Empty<AgentPersona>() };

        // Act
        var defaultPersona = config.DefaultPersona;

        // Assert
        defaultPersona.Should().BeNull();
    }

    /// <summary>
    /// Creates a valid agent configuration for testing purposes.
    /// </summary>
    /// <returns>A valid <see cref="AgentConfiguration"/> instance.</returns>
    private static AgentConfiguration CreateValidConfiguration() =>
        new(
            AgentId: "test-agent",
            Name: "Test Agent",
            Description: "A test agent",
            Icon: "test-icon",
            TemplateId: "test-template",
            Capabilities: AgentCapabilities.Chat,
            DefaultOptions: new ChatOptions(Model: "gpt-4o", Temperature: 0.5, MaxTokens: 1024),
            Personas: new[]
            {
                new AgentPersona("strict", "Strict Editor", "No errors", null, 0.1),
                new AgentPersona("friendly", "Friendly Editor", "Gentle", null, 0.5)
            });
}
