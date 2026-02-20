// -----------------------------------------------------------------------
// <copyright file="WorkflowDesignerViewModelTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.ViewModels;
using Lexichord.Modules.Agents.Workflows;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="WorkflowDesignerViewModel"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the ViewModel layer including command execution, step management,
/// license gating, and validation integration.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7a §12.2
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7a")]
public class WorkflowDesignerViewModelTests
{
    private readonly Mock<IWorkflowDesignerService> _serviceMock;
    private readonly Mock<IAgentRegistry> _registryMock;
    private readonly Mock<ILicenseContext> _licenseMock;
    private readonly WorkflowDesignerViewModel _sut;

    public WorkflowDesignerViewModelTests()
    {
        _serviceMock = new Mock<IWorkflowDesignerService>();
        _registryMock = new Mock<IAgentRegistry>();
        _licenseMock = new Mock<ILicenseContext>();

        _licenseMock.Setup(l => l.Tier).Returns(LicenseTier.Teams);
        _registryMock.Setup(r => r.AvailableAgents).Returns(CreateTestAgents());

        // Setup service CreateNew to return a proper workflow
        _serviceMock.Setup(s => s.CreateNew(It.IsAny<string>()))
            .Returns((string name) => new WorkflowDefinition(
                $"wf-{Guid.NewGuid():N}"[..12], name, string.Empty, null,
                Array.Empty<WorkflowStepDefinition>(),
                new WorkflowMetadata("user", DateTime.UtcNow, DateTime.UtcNow,
                    "1.0", Array.Empty<string>(), WorkflowCategory.General, false, LicenseTier.Teams)));

        _sut = new WorkflowDesignerViewModel(
            _serviceMock.Object,
            _registryMock.Object,
            _licenseMock.Object);
    }

    /// <summary>
    /// Verifies that AvailableAgents is populated from the registry.
    /// </summary>
    [Fact]
    public void AvailableAgents_PopulatedFromRegistry()
    {
        _sut.AvailableAgents.Should().HaveCount(2);
        _sut.AvailableAgents.Should().Contain(a => a.AgentId == "editor");
        _sut.AvailableAgents.Should().Contain(a => a.AgentId == "simplifier");
    }

    /// <summary>
    /// Verifies that AddStepCommand adds a step to the collection.
    /// </summary>
    [Fact]
    public void AddStepCommand_AddsStepToCollection()
    {
        _sut.NewWorkflowCommand.Execute(null);

        _sut.AddStepCommand.Execute("editor");

        _sut.Steps.Should().HaveCount(1);
        _sut.Steps[0].AgentId.Should().Be("editor");
    }

    /// <summary>
    /// Verifies that AddStepCommand sets the selected step.
    /// </summary>
    [Fact]
    public void AddStepCommand_SetsSelectedStep()
    {
        _sut.NewWorkflowCommand.Execute(null);

        _sut.AddStepCommand.Execute("editor");

        _sut.SelectedStep.Should().NotBeNull();
        _sut.SelectedStep!.AgentId.Should().Be("editor");
    }

    /// <summary>
    /// Verifies that AddStepCommand increments order for each step.
    /// </summary>
    [Fact]
    public void AddStepCommand_IncrementsOrder()
    {
        _sut.NewWorkflowCommand.Execute(null);

        _sut.AddStepCommand.Execute("editor");
        _sut.AddStepCommand.Execute("simplifier");

        _sut.Steps[0].Order.Should().Be(1);
        _sut.Steps[1].Order.Should().Be(2);
    }

    /// <summary>
    /// Verifies that RemoveStepCommand removes a step from the collection.
    /// </summary>
    [Fact]
    public void RemoveStepCommand_RemovesFromCollection()
    {
        _sut.NewWorkflowCommand.Execute(null);
        _sut.AddStepCommand.Execute("editor");
        var step = _sut.Steps[0];

        _sut.RemoveStepCommand.Execute(step);

        _sut.Steps.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ReorderStepCommand updates order properties.
    /// </summary>
    [Fact]
    public void ReorderStepCommand_UpdatesOrderProperties()
    {
        _sut.NewWorkflowCommand.Execute(null);
        _sut.AddStepCommand.Execute("editor");
        _sut.AddStepCommand.Execute("simplifier");

        _sut.ReorderStepCommand.Execute((0, 1));

        _sut.Steps[0].AgentId.Should().Be("simplifier");
        _sut.Steps[1].AgentId.Should().Be("editor");
        _sut.Steps[0].Order.Should().Be(1);
        _sut.Steps[1].Order.Should().Be(2);
    }

    /// <summary>
    /// Verifies that HasUnsavedChanges becomes true after modification.
    /// </summary>
    [Fact]
    public void HasUnsavedChanges_TrueAfterModification()
    {
        _sut.NewWorkflowCommand.Execute(null);
        _sut.HasUnsavedChanges.Should().BeFalse();

        _sut.AddStepCommand.Execute("editor");

        _sut.HasUnsavedChanges.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that CanEdit is false when license tier is below Teams.
    /// </summary>
    [Fact]
    public void CanEdit_FalseWhenNotTeamsTier()
    {
        _licenseMock.Setup(l => l.Tier).Returns(LicenseTier.WriterPro);
        var vm = new WorkflowDesignerViewModel(
            _serviceMock.Object,
            _registryMock.Object,
            _licenseMock.Object);

        vm.CanEdit.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that ValidateCommand updates the validation result.
    /// </summary>
    [Fact]
    public void ValidateCommand_UpdatesValidationResult()
    {
        _sut.NewWorkflowCommand.Execute(null);
        _sut.AddStepCommand.Execute("editor");
        _serviceMock.Setup(s => s.Validate(It.IsAny<WorkflowDefinition>()))
            .Returns(new WorkflowValidationResult(true, Array.Empty<WorkflowValidationError>(),
                Array.Empty<WorkflowValidationWarning>()));

        _sut.ValidateCommand.Execute(null);

        _sut.ValidationResult.Should().NotBeNull();
        _sut.ValidationResult!.IsValid.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private IReadOnlyList<IAgent> CreateTestAgents()
    {
        var editorAgent = new Mock<IAgent>();
        editorAgent.Setup(a => a.AgentId).Returns("editor");
        editorAgent.Setup(a => a.Name).Returns("Editor");
        editorAgent.Setup(a => a.Description).Returns("Grammar");

        var simplifierAgent = new Mock<IAgent>();
        simplifierAgent.Setup(a => a.AgentId).Returns("simplifier");
        simplifierAgent.Setup(a => a.Name).Returns("Simplifier");
        simplifierAgent.Setup(a => a.Description).Returns("Readability");

        return new[] { editorAgent.Object, simplifierAgent.Object };
    }
}
