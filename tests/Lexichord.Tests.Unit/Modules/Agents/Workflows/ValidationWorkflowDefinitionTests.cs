// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowDefinitionTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Templates;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="ValidationWorkflowDefinition"/>,
/// <see cref="ValidationWorkflowLicenseRequirement"/>,
/// <see cref="ValidationWorkflowTrigger"/>, and
/// <see cref="ValidationWorkflowStepDef"/> records.
/// </summary>
/// <remarks>
/// <para>
/// Tests default values, enum completeness, and record immutability.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §3
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7h")]
public class ValidationWorkflowDefinitionTests
{
    /// <summary>
    /// Verifies that <see cref="ValidationWorkflowDefinition"/> has correct
    /// default values for optional properties.
    /// </summary>
    [Fact]
    public void WorkflowDefinition_Defaults_AreCorrect()
    {
        // Act
        var definition = new ValidationWorkflowDefinition
        {
            Id = "test",
            Name = "Test",
            Version = "1.0.0",
            Steps = new List<ValidationWorkflowStepDef>(),
            LicenseRequirement = new ValidationWorkflowLicenseRequirement()
        };

        // Assert
        definition.EnabledByDefault.Should().BeTrue();
        definition.TimeoutMinutes.Should().Be(10);
        definition.IsPrebuilt.Should().BeFalse();
        definition.Trigger.Should().Be(ValidationWorkflowTrigger.Manual);
        definition.Description.Should().BeNull();
        definition.ExpectedDurationMinutes.Should().BeNull();
        definition.PerformanceTargets.Should().BeNull();
        definition.ModifiedAt.Should().BeNull();
        definition.CreatedBy.Should().BeNull();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationWorkflowLicenseRequirement"/> has
    /// correct default tier settings (Teams + Enterprise only).
    /// </summary>
    [Fact]
    public void LicenseRequirement_DefaultTiers()
    {
        // Act
        var requirement = new ValidationWorkflowLicenseRequirement();

        // Assert
        requirement.Core.Should().BeFalse();
        requirement.WriterPro.Should().BeFalse();
        requirement.Teams.Should().BeTrue();
        requirement.Enterprise.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="ValidationWorkflowTrigger"/> contains all
    /// five expected values.
    /// </summary>
    [Fact]
    public void TriggerEnum_AllValuesExist()
    {
        // Act
        var values = Enum.GetValues<ValidationWorkflowTrigger>();

        // Assert
        values.Should().HaveCount(5);
        values.Should().Contain(ValidationWorkflowTrigger.Manual);
        values.Should().Contain(ValidationWorkflowTrigger.OnSave);
        values.Should().Contain(ValidationWorkflowTrigger.PrePublish);
        values.Should().Contain(ValidationWorkflowTrigger.ScheduledNightly);
        values.Should().Contain(ValidationWorkflowTrigger.Custom);
    }

    /// <summary>
    /// Verifies that <see cref="ValidationWorkflowStepDef"/> has correct
    /// default values for optional properties.
    /// </summary>
    [Fact]
    public void StepDefinition_DefaultsAreCorrect()
    {
        // Act
        var step = new ValidationWorkflowStepDef
        {
            Id = "test-step",
            Name = "Test Step",
            Type = "Schema"
        };

        // Assert
        step.Enabled.Should().BeTrue();
        step.Order.Should().Be(0);
        step.TimeoutMs.Should().BeNull();
        step.Options.Should().BeNull();
    }
}
