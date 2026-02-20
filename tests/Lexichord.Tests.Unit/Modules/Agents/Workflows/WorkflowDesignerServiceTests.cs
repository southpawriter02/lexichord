// -----------------------------------------------------------------------
// <copyright file="WorkflowDesignerServiceTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.Workflows;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="WorkflowDesignerService"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the core workflow designer service functionality including
/// workflow creation, validation (errors and warnings), YAML export/import,
/// and the complete validation rule set.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7a §12.1
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7a")]
public class WorkflowDesignerServiceTests
{
    private readonly Mock<IAgentRegistry> _registryMock;
    private readonly Mock<IConfigurationService> _configMock;
    private readonly WorkflowDesignerService _sut;

    public WorkflowDesignerServiceTests()
    {
        _registryMock = new Mock<IAgentRegistry>();
        _configMock = new Mock<IConfigurationService>();
        var loggerMock = new Mock<ILogger<WorkflowDesignerService>>();

        // Setup available agents as IAgent mocks
        var editorAgent = new Mock<IAgent>();
        editorAgent.Setup(a => a.AgentId).Returns("editor");
        editorAgent.Setup(a => a.Name).Returns("Editor");
        editorAgent.Setup(a => a.Description).Returns("Grammar");

        var simplifierAgent = new Mock<IAgent>();
        simplifierAgent.Setup(a => a.AgentId).Returns("simplifier");
        simplifierAgent.Setup(a => a.Name).Returns("Simplifier");
        simplifierAgent.Setup(a => a.Description).Returns("Readability");

        _registryMock.Setup(r => r.AvailableAgents).Returns(new[]
        {
            editorAgent.Object,
            simplifierAgent.Object,
        });

        _sut = new WorkflowDesignerService(
            _registryMock.Object,
            _configMock.Object,
            loggerMock.Object);
    }

    /// <summary>
    /// Verifies that CreateNew generates unique workflow IDs.
    /// </summary>
    [Fact]
    public void CreateNew_GeneratesUniqueId()
    {
        var workflow1 = _sut.CreateNew("Test 1");
        var workflow2 = _sut.CreateNew("Test 2");

        workflow1.WorkflowId.Should().NotBe(workflow2.WorkflowId);
    }

    /// <summary>
    /// Verifies that CreateNew sets name and empty steps.
    /// </summary>
    [Fact]
    public void CreateNew_SetsNameAndEmptySteps()
    {
        var workflow = _sut.CreateNew("My Workflow");

        workflow.Name.Should().Be("My Workflow");
        workflow.Steps.Should().BeEmpty();
        workflow.Metadata.IsBuiltIn.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that validation rejects empty workflows.
    /// </summary>
    [Fact]
    public void Validate_EmptyWorkflow_ReturnsError()
    {
        var workflow = _sut.CreateNew("Test");

        var result = _sut.Validate(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "EMPTY_WORKFLOW");
    }

    /// <summary>
    /// Verifies that validation rejects workflows with no name.
    /// </summary>
    [Fact]
    public void Validate_MissingName_ReturnsError()
    {
        var workflow = new WorkflowDefinition(
            "wf-1", "", "", null,
            new[] { CreateStep("editor") },
            CreateMetadata());

        var result = _sut.Validate(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "MISSING_NAME");
    }

    /// <summary>
    /// Verifies that validation rejects unknown agents.
    /// </summary>
    [Fact]
    public void Validate_UnknownAgent_ReturnsError()
    {
        var workflow = new WorkflowDefinition(
            "wf-1", "Test", "", null,
            new[] { CreateStep("unknown-agent") },
            CreateMetadata());

        var result = _sut.Validate(workflow);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Code == "UNKNOWN_AGENT");
    }

    /// <summary>
    /// Verifies that a valid workflow passes validation.
    /// </summary>
    [Fact]
    public void Validate_ValidWorkflow_ReturnsValid()
    {
        var workflow = new WorkflowDefinition(
            "wf-1", "Test", "Description", null,
            new[] { CreateStep("editor"), CreateStep("simplifier") },
            CreateMetadata());

        var result = _sut.Validate(workflow);

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that single-step workflows produce a warning.
    /// </summary>
    [Fact]
    public void Validate_SingleStep_ReturnsWarning()
    {
        var workflow = new WorkflowDefinition(
            "wf-1", "Test", "", null,
            new[] { CreateStep("editor") },
            CreateMetadata());

        var result = _sut.Validate(workflow);

        result.IsValid.Should().BeTrue();
        result.Warnings.Should().Contain(w => w.Code == "SINGLE_STEP");
    }

    /// <summary>
    /// Verifies that ExportToYaml produces valid YAML containing key fields.
    /// </summary>
    [Fact]
    public void ExportToYaml_ValidWorkflow_ProducesValidYaml()
    {
        var workflow = new WorkflowDefinition(
            "wf-1", "Test Workflow", "A test", "file-code",
            new[]
            {
                new WorkflowStepDefinition("s1", "editor", "strict", "Review text", 1, null, null, null),
                new WorkflowStepDefinition("s2", "simplifier", null, null, 2,
                    new WorkflowStepCondition("", ConditionType.PreviousSuccess), null, null),
            },
            CreateMetadata());

        var yaml = _sut.ExportToYaml(workflow);

        yaml.Should().Contain("workflow_id: wf-1");
        yaml.Should().Contain("name: Test Workflow");
        yaml.Should().Contain("agent_id: editor");
        yaml.Should().Contain("persona_id: strict");
        yaml.Should().Contain("type: PreviousSuccess");
    }

    /// <summary>
    /// Verifies that valid YAML imports correctly.
    /// </summary>
    [Fact]
    public void ImportFromYaml_ValidYaml_ParsesCorrectly()
    {
        var yaml = @"
workflow_id: ""wf-test""
name: ""Imported Workflow""
description: ""Test import""
steps:
  - step_id: ""s1""
    agent_id: ""editor""
    order: 1
metadata:
  author: ""test""
  is_built_in: false
  required_tier: Teams
";

        var workflow = _sut.ImportFromYaml(yaml);

        workflow.WorkflowId.Should().Be("wf-test");
        workflow.Name.Should().Be("Imported Workflow");
        workflow.Steps.Should().HaveCount(1);
        workflow.Steps[0].AgentId.Should().Be("editor");
    }

    /// <summary>
    /// Verifies that invalid YAML throws WorkflowImportException.
    /// </summary>
    [Fact]
    public void ImportFromYaml_InvalidYaml_ThrowsException()
    {
        var invalidYaml = "not: valid: yaml: content:";

        var act = () => _sut.ImportFromYaml(invalidYaml);

        act.Should().Throw<WorkflowImportException>();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private WorkflowStepDefinition CreateStep(string agentId) =>
        new($"step-{Guid.NewGuid():N}"[..12], agentId, null, null, 1, null, null, null);

    private WorkflowMetadata CreateMetadata() =>
        new("test", DateTime.UtcNow, DateTime.UtcNow, "1.0", Array.Empty<string>(),
            WorkflowCategory.Custom, false, LicenseTier.Teams);
}
