// -----------------------------------------------------------------------
// <copyright file="ValidationWorkflowRegistryTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using Lexichord.Modules.Agents.Workflows.Validation.Templates;
using Microsoft.Extensions.Logging;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="ValidationWorkflowRegistry"/>.
/// </summary>
/// <remarks>
/// <para>
/// Tests the three-tier lookup (cache → storage → loader), pre-built mutation
/// guards, and merging of pre-built with custom workflows.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-h §10
/// </para>
/// </remarks>
[Trait("Category", "Workflows")]
[Trait("Version", "v0.7.7h")]
public class ValidationWorkflowRegistryTests
{
    // ── Fields ───────────────────────────────────────────────────────────

    private readonly Mock<IValidationWorkflowStorage> _storageMock;
    private readonly Mock<IValidationWorkflowLoader> _loaderMock;
    private readonly ValidationWorkflowRegistry _sut;

    // ── Constructor ─────────────────────────────────────────────────────

    /// <summary>
    /// Initializes test dependencies with mocked storage, loader, and logger.
    /// </summary>
    public ValidationWorkflowRegistryTests()
    {
        _storageMock = new Mock<IValidationWorkflowStorage>();
        _loaderMock = new Mock<IValidationWorkflowLoader>();
        var loggerMock = new Mock<ILogger<ValidationWorkflowRegistry>>();

        _sut = new ValidationWorkflowRegistry(
            _storageMock.Object,
            _loaderMock.Object,
            loggerMock.Object);
    }

    // ── GetWorkflowAsync Tests ──────────────────────────────────────────

    /// <summary>
    /// Verifies that a workflow loaded via the loader is cached and returned
    /// from cache on subsequent calls, without hitting the loader again.
    /// </summary>
    [Fact]
    public async Task GetWorkflowAsync_FromLoader_LoadsAndCaches()
    {
        // Arrange
        var definition = CreateDefinition("on-save-validation", "On-Save Validation");
        _storageMock.Setup(s => s.GetWorkflowAsync("on-save-validation", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationWorkflowDefinition?)null);
        _loaderMock.Setup(l => l.LoadWorkflowAsync("on-save-validation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        // Act — first call goes through loader
        var result1 = await _sut.GetWorkflowAsync("on-save-validation");
        // Act — second call should hit cache
        var result2 = await _sut.GetWorkflowAsync("on-save-validation");

        // Assert
        result1.Should().BeSameAs(definition);
        result2.Should().BeSameAs(definition);
        _loaderMock.Verify(l => l.LoadWorkflowAsync("on-save-validation", It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Verifies that a workflow found in storage is returned directly
    /// without consulting the loader.
    /// </summary>
    [Fact]
    public async Task GetWorkflowAsync_FromStorage_ReturnsStoredDefinition()
    {
        // Arrange
        var definition = CreateDefinition("custom-workflow", "Custom Workflow");
        _storageMock.Setup(s => s.GetWorkflowAsync("custom-workflow", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        // Act
        var result = await _sut.GetWorkflowAsync("custom-workflow");

        // Assert
        result.Should().BeSameAs(definition);
        _loaderMock.Verify(
            l => l.LoadWorkflowAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// Verifies that an <see cref="InvalidOperationException"/> is thrown
    /// when the workflow is not found in any tier.
    /// </summary>
    [Fact]
    public async Task GetWorkflowAsync_NotFound_ThrowsInvalidOperation()
    {
        // Arrange
        _storageMock.Setup(s => s.GetWorkflowAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationWorkflowDefinition?)null);
        _loaderMock.Setup(l => l.LoadWorkflowAsync("nonexistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationWorkflowDefinition?)null);

        // Act & Assert
        var act = () => _sut.GetWorkflowAsync("nonexistent");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*nonexistent*");
    }

    // ── ListPrebuiltAsync Tests ─────────────────────────────────────────

    /// <summary>
    /// Verifies that ListPrebuiltAsync attempts to load all three pre-built
    /// workflow IDs from the loader.
    /// </summary>
    [Fact]
    public async Task ListPrebuiltAsync_ReturnsThreeWorkflows()
    {
        // Arrange
        _loaderMock.Setup(l => l.LoadWorkflowAsync("on-save-validation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDefinition("on-save-validation", "On-Save Validation"));
        _loaderMock.Setup(l => l.LoadWorkflowAsync("pre-publish-gate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDefinition("pre-publish-gate", "Pre-Publish Gate"));
        _loaderMock.Setup(l => l.LoadWorkflowAsync("nightly-health-check", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDefinition("nightly-health-check", "Nightly Health Check"));

        // Act
        var result = await _sut.ListPrebuiltAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Select(w => w.Id).Should().Contain(new[]
        {
            "on-save-validation",
            "pre-publish-gate",
            "nightly-health-check"
        });
    }

    // ── ListWorkflowsAsync Tests ────────────────────────────────────────

    /// <summary>
    /// Verifies that ListWorkflowsAsync merges pre-built and custom workflows.
    /// </summary>
    [Fact]
    public async Task ListWorkflowsAsync_MergesPrebuiltAndCustom()
    {
        // Arrange — 1 pre-built, 1 custom
        _loaderMock.Setup(l => l.LoadWorkflowAsync("on-save-validation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDefinition("on-save-validation", "On-Save Validation"));
        _loaderMock.Setup(l => l.LoadWorkflowAsync("pre-publish-gate", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationWorkflowDefinition?)null);
        _loaderMock.Setup(l => l.LoadWorkflowAsync("nightly-health-check", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ValidationWorkflowDefinition?)null);

        var customWorkflow = CreateDefinition("my-custom", "My Custom Workflow", isPrebuilt: false);
        _storageMock.Setup(s => s.ListWorkflowsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ValidationWorkflowDefinition> { customWorkflow });

        // Act
        var result = await _sut.ListWorkflowsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(w => w.Id == "on-save-validation");
        result.Should().Contain(w => w.Id == "my-custom");
    }

    // ── RegisterWorkflowAsync Tests ─────────────────────────────────────

    /// <summary>
    /// Verifies that RegisterWorkflowAsync delegates to storage and forces
    /// IsPrebuilt to false.
    /// </summary>
    [Fact]
    public async Task RegisterWorkflowAsync_ValidWorkflow_DelegatesToStorage()
    {
        // Arrange
        var workflow = CreateDefinition("new-workflow", "New Workflow", isPrebuilt: true);
        _storageMock.Setup(s => s.SaveWorkflowAsync(It.IsAny<ValidationWorkflowDefinition>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("new-workflow");

        // Act
        var id = await _sut.RegisterWorkflowAsync(workflow);

        // Assert
        id.Should().Be("new-workflow");
        _storageMock.Verify(
            s => s.SaveWorkflowAsync(
                It.Is<ValidationWorkflowDefinition>(w => w.IsPrebuilt == false),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── DeleteWorkflowAsync Tests ───────────────────────────────────────

    /// <summary>
    /// Verifies that deleting a pre-built workflow throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task DeleteWorkflowAsync_PrebuiltWorkflow_ThrowsInvalidOperation()
    {
        // Act & Assert
        var act = () => _sut.DeleteWorkflowAsync("on-save-validation");
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pre-built*");
    }

    /// <summary>
    /// Verifies that updating a pre-built workflow throws InvalidOperationException.
    /// </summary>
    [Fact]
    public async Task UpdateWorkflowAsync_PrebuiltWorkflow_ThrowsInvalidOperation()
    {
        // Arrange
        var workflow = CreateDefinition("on-save-validation", "Modified Name");

        // Act & Assert
        var act = () => _sut.UpdateWorkflowAsync("on-save-validation", workflow);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*pre-built*");
    }

    // ── License Requirement Tests ───────────────────────────────────────

    /// <summary>
    /// Verifies the on-save validation license requirement: WriterPro+.
    /// </summary>
    [Fact]
    public async Task LicenseRequirement_OnSave_RequiresWriterPro()
    {
        // Arrange
        var definition = CreateDefinition("on-save-validation", "On-Save Validation") with
        {
            LicenseRequirement = new ValidationWorkflowLicenseRequirement
            {
                Core = false,
                WriterPro = true,
                Teams = true,
                Enterprise = true
            }
        };
        _loaderMock.Setup(l => l.LoadWorkflowAsync("on-save-validation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        // Act
        var result = await _sut.GetWorkflowAsync("on-save-validation");

        // Assert
        result.LicenseRequirement.Core.Should().BeFalse();
        result.LicenseRequirement.WriterPro.Should().BeTrue();
        result.LicenseRequirement.Teams.Should().BeTrue();
        result.LicenseRequirement.Enterprise.Should().BeTrue();
    }

    /// <summary>
    /// Verifies the pre-publish gate license requirement: Teams+.
    /// </summary>
    [Fact]
    public async Task LicenseRequirement_PrePublish_RequiresTeams()
    {
        // Arrange
        var definition = CreateDefinition("pre-publish-gate", "Pre-Publish Gate") with
        {
            LicenseRequirement = new ValidationWorkflowLicenseRequirement
            {
                Core = false,
                WriterPro = false,
                Teams = true,
                Enterprise = true
            }
        };
        _loaderMock.Setup(l => l.LoadWorkflowAsync("pre-publish-gate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);

        // Act
        var result = await _sut.GetWorkflowAsync("pre-publish-gate");

        // Assert
        result.LicenseRequirement.Core.Should().BeFalse();
        result.LicenseRequirement.WriterPro.Should().BeFalse();
        result.LicenseRequirement.Teams.Should().BeTrue();
        result.LicenseRequirement.Enterprise.Should().BeTrue();
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a minimal <see cref="ValidationWorkflowDefinition"/> for testing.
    /// </summary>
    private static ValidationWorkflowDefinition CreateDefinition(
        string id,
        string name,
        bool isPrebuilt = true) =>
        new()
        {
            Id = id,
            Name = name,
            Version = "1.0.0",
            IsPrebuilt = isPrebuilt,
            Steps = new List<ValidationWorkflowStepDef>
            {
                new()
                {
                    Id = "test-step",
                    Name = "Test Step",
                    Type = "Schema",
                    Order = 1
                }
            },
            LicenseRequirement = new ValidationWorkflowLicenseRequirement()
        };
}
