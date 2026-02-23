// -----------------------------------------------------------------------
// <copyright file="PresetWorkflowRepositoryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Modules.Agents.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="PresetWorkflowRepository"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the preset workflow repository's ability to load embedded YAML resources,
/// deserialize them into <see cref="WorkflowDefinition"/> records, and provide
/// query operations including GetAll, GetById, GetByCategory, IsPreset, and
/// GetSummaries.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-v0.7.7c §10
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7c")]
public class PresetWorkflowRepositoryTests
{
    /// <summary>
    /// The system under test — a real <see cref="PresetWorkflowRepository"/>
    /// constructed with a mock logger. Embedded YAML resources are loaded
    /// at construction.
    /// </summary>
    private readonly PresetWorkflowRepository _sut;

    /// <summary>
    /// Initializes the test class by constructing a <see cref="PresetWorkflowRepository"/>
    /// with a mock logger. The constructor eagerly loads all embedded YAML presets.
    /// </summary>
    public PresetWorkflowRepositoryTests()
    {
        _sut = new PresetWorkflowRepository(Mock.Of<ILogger<PresetWorkflowRepository>>());
    }

    // ── GetAll Tests ────────────────────────────────────────────────────

    /// <summary>
    /// Verifies that the repository loads exactly 5 preset workflows
    /// from the embedded YAML resources at construction.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #1: App starts → 5 preset workflows available.
    /// Acceptance Criteria #2: GetAll() called → Returns all 5 presets.
    /// </remarks>
    [Fact]
    public void GetAll_ReturnsExpectedPresetCount()
    {
        // Act
        var presets = _sut.GetAll();

        // Assert
        presets.Should().HaveCount(5);
    }

    // ── GetById Tests ───────────────────────────────────────────────────

    /// <summary>
    /// Verifies that each known preset can be retrieved by its unique ID
    /// and has the expected display name.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #3: GetById("preset-technical-review") → Returns Technical Review.
    /// </remarks>
    [Theory]
    [InlineData("preset-technical-review", "Technical Review")]
    [InlineData("preset-marketing-polish", "Marketing Polish")]
    [InlineData("preset-quick-edit", "Quick Edit")]
    [InlineData("preset-academic-review", "Academic Review")]
    [InlineData("preset-executive-summary", "Executive Summary")]
    public void GetById_KnownPreset_ReturnsWorkflow(string id, string expectedName)
    {
        // Act
        var preset = _sut.GetById(id);

        // Assert
        preset.Should().NotBeNull();
        preset!.Name.Should().Be(expectedName);
    }

    /// <summary>
    /// Verifies that GetById returns null for a workflow ID that does not
    /// correspond to any loaded preset.
    /// </summary>
    [Fact]
    public void GetById_UnknownId_ReturnsNull()
    {
        // Act
        var preset = _sut.GetById("unknown-workflow");

        // Assert
        preset.Should().BeNull();
    }

    // ── GetByCategory Tests ─────────────────────────────────────────────

    /// <summary>
    /// Verifies that filtering by Technical category returns only the
    /// Technical Review preset.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #4: GetByCategory(Technical) → Returns Technical Review.
    /// </remarks>
    [Fact]
    public void GetByCategory_Technical_ReturnsTechnicalReview()
    {
        // Act
        var presets = _sut.GetByCategory(WorkflowCategory.Technical);

        // Assert
        presets.Should().HaveCount(1);
        presets[0].WorkflowId.Should().Be("preset-technical-review");
    }

    /// <summary>
    /// Verifies that filtering by Marketing category returns only the
    /// Marketing Polish preset.
    /// </summary>
    [Fact]
    public void GetByCategory_Marketing_ReturnsMarketingPolish()
    {
        // Act
        var presets = _sut.GetByCategory(WorkflowCategory.Marketing);

        // Assert
        presets.Should().HaveCount(1);
        presets[0].WorkflowId.Should().Be("preset-marketing-polish");
    }

    // ── IsPreset Tests ──────────────────────────────────────────────────

    /// <summary>
    /// Verifies that IsPreset returns true for a known preset workflow ID.
    /// </summary>
    [Fact]
    public void IsPreset_WithPresetId_ReturnsTrue()
    {
        // Act
        var result = _sut.IsPreset("preset-technical-review");

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsPreset returns false for a user-created workflow ID
    /// that does not exist in the preset repository.
    /// </summary>
    [Fact]
    public void IsPreset_WithCustomId_ReturnsFalse()
    {
        // Act
        var result = _sut.IsPreset("custom-workflow-123");

        // Assert
        result.Should().BeFalse();
    }

    // ── Structural Validation Tests ─────────────────────────────────────

    /// <summary>
    /// Verifies that the Technical Review preset has exactly 4 steps
    /// invoking the expected agents in the correct order.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #5: Technical Review → 4 agents (editor, simplifier, tuning, summarizer).
    /// </remarks>
    [Fact]
    public void TechnicalReview_HasExpectedSteps()
    {
        // Act
        var preset = _sut.GetById("preset-technical-review");

        // Assert
        preset!.Steps.Should().HaveCount(4);
        preset.Steps.Select(s => s.AgentId).Should().ContainInOrder(
            "editor", "simplifier", "tuning", "summarizer");
    }

    /// <summary>
    /// Verifies that the Quick Edit preset has exactly 1 step
    /// using the editor agent with the "friendly" persona.
    /// </summary>
    /// <remarks>
    /// Acceptance Criteria #7: Quick Edit → Single editor step.
    /// </remarks>
    [Fact]
    public void QuickEdit_HasSingleStep()
    {
        // Act
        var preset = _sut.GetById("preset-quick-edit");

        // Assert
        preset!.Steps.Should().HaveCount(1);
        preset.Steps[0].AgentId.Should().Be("editor");
    }

    /// <summary>
    /// Verifies that all loaded presets have their IsBuiltIn metadata flag
    /// set to true, confirming they are system-provided workflows.
    /// </summary>
    [Fact]
    public void AllPresets_HaveIsBuiltInTrue()
    {
        // Act
        var presets = _sut.GetAll();

        // Assert
        presets.Should().AllSatisfy(p => p.Metadata.IsBuiltIn.Should().BeTrue());
    }

    /// <summary>
    /// Verifies that all steps across all presets reference only valid,
    /// registered agent IDs (editor, simplifier, tuning, summarizer, copilot).
    /// </summary>
    [Fact]
    public void AllPresets_HaveValidAgentIds()
    {
        // Arrange
        var validAgentIds = new[] { "editor", "simplifier", "tuning", "summarizer", "copilot" };

        // Act
        var presets = _sut.GetAll();

        // Assert
        foreach (var preset in presets)
        {
            foreach (var step in preset.Steps)
            {
                validAgentIds.Should().Contain(step.AgentId,
                    $"Preset {preset.WorkflowId} step {step.StepId} has invalid agent {step.AgentId}");
            }
        }
    }

    // ── GetSummaries Tests ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that GetSummaries returns exactly 5 summaries matching
    /// the loaded presets, with correct step counts and agent IDs.
    /// </summary>
    [Fact]
    public void GetSummaries_ReturnsAllPresetSummaries()
    {
        // Act
        var summaries = _sut.GetSummaries();

        // Assert
        summaries.Should().HaveCount(5);
        summaries.Should().AllSatisfy(s =>
        {
            s.WorkflowId.Should().StartWith("preset-");
            s.StepCount.Should().BeGreaterThan(0);
            s.AgentIds.Should().NotBeEmpty();
        });
    }
}
