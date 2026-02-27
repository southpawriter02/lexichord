// -----------------------------------------------------------------------
// <copyright file="EmbeddedResourceValidationWorkflowLoaderTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Templates;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="EmbeddedResourceValidationWorkflowLoader"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests embedded resource loading and YAML parsing for the three pre-built
/// validation workflow templates. These are integration-style unit tests that
/// exercise the real embedded resources from the Lexichord.Modules.Agents assembly.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §10
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7h")]
public class EmbeddedResourceValidationWorkflowLoaderTests
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly EmbeddedResourceValidationWorkflowLoader _sut;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes the loader with a mock logger. The loader reads real
    /// embedded YAML resources from the Lexichord.Modules.Agents assembly.
    /// </summary>
    public EmbeddedResourceValidationWorkflowLoaderTests()
    {
        var loggerMock = new Mock<ILogger<EmbeddedResourceValidationWorkflowLoader>>();
        _sut = new EmbeddedResourceValidationWorkflowLoader(loggerMock.Object);
    }

    // ── On-Save Validation Tests ────────────────────────────────────────

    /// <summary>
    /// Verifies that the on-save-validation YAML is parsed into a complete
    /// definition with the expected ID, name, trigger, and step count.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_OnSaveValidation_ParsesCorrectly()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("on-save-validation");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("on-save-validation");
        result.Name.Should().Be("On-Save Validation");
        result.Trigger.Should().Be(ValidationWorkflowTrigger.OnSave);
        result.Steps.Should().HaveCount(3);
        result.IsPrebuilt.Should().BeTrue();
        result.TimeoutMinutes.Should().Be(5);
        result.ExpectedDurationMinutes.Should().Be(2);
    }

    // ── Pre-Publish Gate Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that the pre-publish-gate YAML is parsed with all 7 steps
    /// in the correct order.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_PrePublishGate_ParsesAllSteps()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("pre-publish-gate");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("pre-publish-gate");
        result.Name.Should().Be("Pre-Publish Gate");
        result.Trigger.Should().Be(ValidationWorkflowTrigger.PrePublish);
        result.Steps.Should().HaveCount(7);

        var stepIds = result.Steps.Select(s => s.Id).ToList();
        stepIds.Should().ContainInOrder(
            "schema-validation",
            "grammar-check",
            "consistency-check",
            "kg-alignment",
            "reference-validation",
            "publish-gate",
            "kg-sync");
    }

    // ── Nightly Health Check Tests ──────────────────────────────────────

    /// <summary>
    /// Verifies that the nightly-health-check YAML is parsed with the
    /// ScheduledNightly trigger and expected timeout.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_NightlyHealthCheck_ParsesSchedule()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("nightly-health-check");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("nightly-health-check");
        result.Name.Should().Be("Nightly Health Check");
        result.Trigger.Should().Be(ValidationWorkflowTrigger.ScheduledNightly);
        result.Steps.Should().HaveCount(5);
        result.TimeoutMinutes.Should().Be(60);
        result.ExpectedDurationMinutes.Should().Be(30);
    }

    // ── Unknown ID Tests ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that loading a nonexistent workflow ID returns null rather
    /// than throwing an exception.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_UnknownId_ReturnsNull()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("does-not-exist");

        // Assert
        result.Should().BeNull();
    }

    // ── License Requirement Tests ───────────────────────────────────────

    /// <summary>
    /// Verifies that the on-save-validation has WriterPro+ license requirement.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_OnSave_ParsesLicenseRequirement()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("on-save-validation");

        // Assert
        result.Should().NotBeNull();
        result!.LicenseRequirement.Core.Should().BeFalse();
        result.LicenseRequirement.WriterPro.Should().BeTrue();
        result.LicenseRequirement.Teams.Should().BeTrue();
        result.LicenseRequirement.Enterprise.Should().BeTrue();
    }

    // ── Performance Targets Tests ───────────────────────────────────────

    /// <summary>
    /// Verifies that performance targets are parsed from the YAML.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_OnSave_ParsesPerformanceTargets()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("on-save-validation");

        // Assert
        result.Should().NotBeNull();
        result!.PerformanceTargets.Should().NotBeNull();
        result.PerformanceTargets.Should().ContainKey("max_duration_ms");
        result.PerformanceTargets!["max_duration_ms"].Should().Be(120000);
    }

    // ── IsPrebuilt Flag Tests ───────────────────────────────────────────

    /// <summary>
    /// Verifies that all loaded workflows have IsPrebuilt set to true.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_AllPrebuilt_SetsIsPrebuiltTrue()
    {
        // Act
        var onSave = await _sut.LoadWorkflowAsync("on-save-validation");
        var prePublish = await _sut.LoadWorkflowAsync("pre-publish-gate");
        var nightly = await _sut.LoadWorkflowAsync("nightly-health-check");

        // Assert
        onSave!.IsPrebuilt.Should().BeTrue();
        prePublish!.IsPrebuilt.Should().BeTrue();
        nightly!.IsPrebuilt.Should().BeTrue();
    }

    // ── Step Options Tests ──────────────────────────────────────────────

    /// <summary>
    /// Verifies that step options are parsed from the YAML.
    /// </summary>
    [Fact]
    public async Task LoadWorkflowAsync_PrePublish_ParsesStepOptions()
    {
        // Act
        var result = await _sut.LoadWorkflowAsync("pre-publish-gate");

        // Assert
        result.Should().NotBeNull();
        var publishGate = result!.Steps.First(s => s.Id == "publish-gate");
        publishGate.Options.Should().NotBeNull();
        publishGate.Options.Should().ContainKey("require_all_previous_pass");
    }
}
