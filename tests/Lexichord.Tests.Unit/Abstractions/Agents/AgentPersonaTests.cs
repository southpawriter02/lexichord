// -----------------------------------------------------------------------
// <copyright file="AgentPersonaTests.cs" company="Lexichord">
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
/// Unit tests for <see cref="AgentPersona"/> record.
/// </summary>
/// <remarks>
/// Tests cover validation rules, temperature overrides via ApplyTo(),
/// record equality, and handling of optional fields.
/// Introduced in v0.7.1a.
/// </remarks>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.1a")]
public class AgentPersonaTests
{
    /// <summary>
    /// Verifies that a persona with valid data passes validation.
    /// </summary>
    [Fact]
    public void AgentPersona_WithValidData_PassesValidation()
    {
        // Arrange
        var persona = new AgentPersona("test-persona", "Test", "Tagline", null, 0.5);

        // Act
        var errors = persona.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that personas with invalid temperature values fail validation.
    /// </summary>
    [Theory]
    [InlineData(-0.1)]
    [InlineData(2.1)]
    [InlineData(3.0)]
    public void AgentPersona_WithInvalidTemperature_FailsValidation(double temperature)
    {
        // Arrange
        var persona = new AgentPersona("test", "Test", "Tag", null, temperature);

        // Act
        var errors = persona.Validate();

        // Assert
        errors.Should().Contain(e => e.Contains("Temperature"));
    }

    /// <summary>
    /// Verifies that ApplyTo() correctly overrides temperature while preserving other options.
    /// </summary>
    [Fact]
    public void AgentPersona_ApplyTo_OverridesTemperature()
    {
        // Arrange
        var persona = new AgentPersona("warm", "Warm", "Tag", null, 0.8);
        var baseOptions = new ChatOptions(Model: "gpt-4o", Temperature: 0.3, MaxTokens: 2048);

        // Act
        var modified = persona.ApplyTo(baseOptions);

        // Assert
        modified.Temperature.Should().Be(0.8);
        modified.Model.Should().Be("gpt-4o"); // Unchanged
        modified.MaxTokens.Should().Be(2048); // Unchanged
    }

    /// <summary>
    /// Verifies that record equality works correctly for personas with same values.
    /// </summary>
    [Fact]
    public void AgentPersona_Equality_SameValues_AreEqual()
    {
        // Arrange
        var persona1 = new AgentPersona("test", "Test", "Tag", "Override", 0.5, "Voice");
        var persona2 = new AgentPersona("test", "Test", "Tag", "Override", 0.5, "Voice");

        // Assert
        persona1.Should().Be(persona2);
    }

    /// <summary>
    /// Verifies that SystemPromptOverride stores the provided value correctly.
    /// </summary>
    [Fact]
    public void AgentPersona_WithSystemPromptOverride_StoresValue()
    {
        // Arrange
        var override_ = "You are a specialized assistant...";
        var persona = new AgentPersona("custom", "Custom", "Tag", override_, 0.5);

        // Assert
        persona.SystemPromptOverride.Should().Be(override_);
    }

    /// <summary>
    /// Verifies that personas with invalid PersonaIds fail validation.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("Invalid ID")]
    [InlineData("UPPERCASE")]
    [InlineData("has spaces")]
    public void AgentPersona_WithInvalidPersonaId_FailsValidation(string personaId)
    {
        // Arrange
        var persona = new AgentPersona(personaId, "Test", "Tag", null, 0.5);

        // Act
        var errors = persona.Validate();

        // Assert
        errors.Should().Contain(e => e.Contains("PersonaId"));
    }
}
