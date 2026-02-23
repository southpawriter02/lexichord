// -----------------------------------------------------------------------
// <copyright file="SyncWorkflowStepTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.Agents.Workflows.Validation;
using Microsoft.Extensions.Logging;
using Moq;
using SyncDirection = Lexichord.Abstractions.Contracts.Knowledge.Sync.SyncDirection;
using InfraSyncContext = Lexichord.Abstractions.Contracts.Knowledge.Sync.SyncContext;

namespace Lexichord.Tests.Unit.Modules.Agents.Workflows;

/// <summary>
/// Unit tests for <see cref="SyncWorkflowStep"/>,
/// <see cref="SyncWorkflowStepFactory"/>, and supporting types.
/// </summary>
/// <remarks>
/// <para>
/// Tests the sync step lifecycle including factory creation, configuration
/// validation, sync execution across all three directions, conflict strategy
/// mapping, skip-on-validation-failure logic, timeout enforcement, and
/// disabled-step auto-pass behavior.
/// </para>
/// <para>
/// <b>Specification:</b> LCS-DES-077-KG-g §7
/// </para>
/// </remarks>
[Trait("Category", "Unit")]
[Trait("Version", "v0.7.7g")]
public class SyncWorkflowStepTests
{
    private readonly Mock<ISyncService> _syncServiceMock;
    private readonly Mock<ILogger<SyncWorkflowStep>> _stepLoggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;

    public SyncWorkflowStepTests()
    {
        _syncServiceMock = new Mock<ISyncService>();
        _stepLoggerMock = new Mock<ILogger<SyncWorkflowStep>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();

        // LOGIC: Configure logger factory to return typed loggers
        _loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
    }

    // ── Test 1: Factory creates step with valid options ──────────────────

    /// <summary>
    /// Verifies that the factory creates a step with correct configuration.
    /// </summary>
    /// <remarks>Spec §7: CreateStep_ValidOptions</remarks>
    [Fact]
    public void CreateStep_ValidOptions_StepCreatedSuccessfully()
    {
        // Arrange
        var factory = new SyncWorkflowStepFactory(
            _syncServiceMock.Object,
            _loggerFactoryMock.Object);

        var options = new SyncWorkflowStepOptions
        {
            Direction = SyncDirection.DocumentToGraph,
            ConflictStrategy = ConflictStrategy.PreferNewer,
            SkipIfValidationFailed = true,
            TimeoutMs = 30000,
            Description = "Post-validation sync",
            Order = 5
        };

        // Act
        var step = factory.CreateStep("test-sync", "Test Sync", options);

        // Assert
        step.Should().NotBeNull();
        step.Id.Should().Be("test-sync");
        step.Name.Should().Be("Test Sync");
        step.Direction.Should().Be(SyncDirection.DocumentToGraph);
        step.ConflictStrategy.Should().Be(ConflictStrategy.PreferNewer);
        step.SkipIfValidationFailed.Should().BeTrue();
        step.TimeoutMs.Should().Be(30000);
        step.Description.Should().Be("Post-validation sync");
        step.Order.Should().Be(5);
        step.IsEnabled.Should().BeTrue();
    }

    // ── Test 2: Configuration validation passes ─────────────────────────

    /// <summary>
    /// Verifies that a validly-configured step passes configuration validation.
    /// </summary>
    /// <remarks>Spec §7: ValidateConfiguration_Valid</remarks>
    [Fact]
    public void ValidateConfiguration_Valid_NoErrors()
    {
        // Arrange
        var step = CreateStep();

        // Act
        var errors = step.ValidateConfiguration();

        // Assert
        errors.Should().BeEmpty();
    }

    // ── Test 3: DocumentToGraph sync ─────────────────────────────────────

    /// <summary>
    /// Verifies that DocumentToGraph sync delegates to ISyncService correctly.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_DocumentToGraph</remarks>
    [Fact]
    public async Task ExecuteSync_DocumentToGraph_Succeeds()
    {
        // Arrange
        var step = CreateStep(direction: SyncDirection.DocumentToGraph);
        SetupSyncServiceSuccess(entitiesCount: 3);

        var context = CreateSyncContext();

        // Act
        var result = await step.ExecuteSyncAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Direction.Should().Be(SyncDirection.DocumentToGraph);
        result.ItemsSynced.Should().Be(3);
        result.SyncLogs.Should().NotBeEmpty();
        result.StatusMessage.Should().Contain("3 items synced");
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.IsAny<InfraSyncContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 4: GraphToDocument sync ─────────────────────────────────────

    /// <summary>
    /// Verifies that GraphToDocument sync works correctly.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_GraphToDocument</remarks>
    [Fact]
    public async Task ExecuteSync_GraphToDocument_Succeeds()
    {
        // Arrange
        var step = CreateStep(direction: SyncDirection.GraphToDocument);
        SetupSyncServiceSuccess(entitiesCount: 2);

        var context = CreateSyncContext();

        // Act
        var result = await step.ExecuteSyncAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Direction.Should().Be(SyncDirection.GraphToDocument);
        result.ItemsSynced.Should().Be(2);
    }

    // ── Test 5: Bidirectional sync ───────────────────────────────────────

    /// <summary>
    /// Verifies that Bidirectional sync works correctly.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_Bidirectional</remarks>
    [Fact]
    public async Task ExecuteSync_Bidirectional_Succeeds()
    {
        // Arrange
        var step = CreateStep(direction: SyncDirection.Bidirectional);
        SetupSyncServiceSuccess(entitiesCount: 5);

        var context = CreateSyncContext();

        // Act
        var result = await step.ExecuteSyncAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Direction.Should().Be(SyncDirection.Bidirectional);
        result.ItemsSynced.Should().Be(5);
    }

    // ── Test 6: PreferDocument strategy ──────────────────────────────────

    /// <summary>
    /// Verifies that PreferDocument conflict strategy maps to UseDocument.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_PreferDocument</remarks>
    [Fact]
    public async Task ExecuteSync_PreferDocument_UsesCorrectStrategy()
    {
        // Arrange
        var step = CreateStep(conflictStrategy: ConflictStrategy.PreferDocument);
        SetupSyncServiceSuccess();

        var context = CreateSyncContext();

        // Act
        await step.ExecuteSyncAsync(context);

        // Assert — verify the infrastructure context uses UseDocument
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.Is<InfraSyncContext>(
                    c => c.DefaultConflictStrategy == ConflictResolutionStrategy.UseDocument),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 7: PreferGraph strategy ────────────────────────────────────

    /// <summary>
    /// Verifies that PreferGraph conflict strategy maps to UseGraph.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_PreferGraph</remarks>
    [Fact]
    public async Task ExecuteSync_PreferGraph_UsesCorrectStrategy()
    {
        // Arrange
        var step = CreateStep(conflictStrategy: ConflictStrategy.PreferGraph);
        SetupSyncServiceSuccess();

        var context = CreateSyncContext();

        // Act
        await step.ExecuteSyncAsync(context);

        // Assert
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.Is<InfraSyncContext>(
                    c => c.DefaultConflictStrategy == ConflictResolutionStrategy.UseGraph),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 8: PreferNewer strategy ────────────────────────────────────

    /// <summary>
    /// Verifies that PreferNewer conflict strategy maps to Merge.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_PreferNewer</remarks>
    [Fact]
    public async Task ExecuteSync_PreferNewer_UsesCorrectStrategy()
    {
        // Arrange
        var step = CreateStep(conflictStrategy: ConflictStrategy.PreferNewer);
        SetupSyncServiceSuccess();

        var context = CreateSyncContext();

        // Act
        await step.ExecuteSyncAsync(context);

        // Assert
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.Is<InfraSyncContext>(
                    c => c.DefaultConflictStrategy == ConflictResolutionStrategy.Merge),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 9: Merge strategy ──────────────────────────────────────────

    /// <summary>
    /// Verifies that Merge conflict strategy maps to Merge.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_Merge</remarks>
    [Fact]
    public async Task ExecuteSync_Merge_UsesCorrectStrategy()
    {
        // Arrange
        var step = CreateStep(conflictStrategy: ConflictStrategy.Merge);
        SetupSyncServiceSuccess();

        var context = CreateSyncContext();

        // Act
        await step.ExecuteSyncAsync(context);

        // Assert
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.Is<InfraSyncContext>(
                    c => c.DefaultConflictStrategy == ConflictResolutionStrategy.Merge),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Test 10: Conflict detection ─────────────────────────────────────

    /// <summary>
    /// Verifies that conflicts from SyncResult are surfaced in SyncStepResult.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_ConflictDetected</remarks>
    [Fact]
    public async Task ExecuteSync_ConflictDetected_ReportsConflicts()
    {
        // Arrange
        var step = CreateStep();
        _syncServiceMock
            .Setup(s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.IsAny<InfraSyncContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult
            {
                Status = SyncOperationStatus.SuccessWithConflicts,
                Conflicts = new List<global::Lexichord.Abstractions.Contracts.Knowledge.Sync.SyncConflict>
                {
                    new()
                    {
                        ConflictTarget = "Entity:Person.Name",
                        DocumentValue = "John Smith",
                        GraphValue = "John Doe",
                        DetectedAt = DateTimeOffset.UtcNow,
                        Type = ConflictType.ValueMismatch,
                        Description = "Name differs between document and graph"
                    }
                }
            });

        var context = CreateSyncContext();

        // Act
        var result = await step.ExecuteSyncAsync(context);

        // Assert — still success (SuccessWithConflicts), but conflicts reported
        result.Success.Should().BeTrue();
        result.ConflictsDetected.Should().Be(1);
        result.UnresolvedConflicts.Should().HaveCount(1);
        result.UnresolvedConflicts[0].Property.Should().Be("Entity:Person.Name");
        result.UnresolvedConflicts[0].DocumentValue.Should().Be("John Smith");
    }

    // ── Test 11: Skip on validation failure ─────────────────────────────

    /// <summary>
    /// Verifies that the step skips when prior validation steps have failed
    /// and SkipIfValidationFailed is true.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_SkipOnValidationFailure</remarks>
    [Fact]
    public async Task ExecuteAsync_SkipOnValidationFailure_SkipsWithErrors()
    {
        // Arrange
        var step = CreateStep(skipIfValidationFailed: true);
        var context = new ValidationWorkflowContext
        {
            WorkspaceId = Guid.NewGuid(),
            DocumentId = "doc-test-001",
            DocumentContent = "Test document content",
            DocumentType = "markdown",
            Trigger = ValidationTrigger.Manual,
            PreviousResults = new List<ValidationStepResult>
            {
                new()
                {
                    StepId = "schema-check",
                    IsValid = false,
                    Errors = new List<ValidationStepError>
                    {
                        new("r1", "ERR001", "Schema violation")
                    },
                    Warnings = new List<ValidationStepWarning>()
                }
            }
        };

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("Skipped due to validation failures");
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.IsAny<InfraSyncContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Test 12: Timeout enforcement ────────────────────────────────────

    /// <summary>
    /// Verifies that the step enforces timeout via CancellationTokenSource.
    /// </summary>
    /// <remarks>Spec §7: ExecuteSync_Timeout</remarks>
    [Fact]
    public async Task ExecuteAsync_Timeout_ReturnsError()
    {
        // Arrange — create a step with a very short timeout
        var step = CreateStep(timeoutMs: 1);
        _syncServiceMock
            .Setup(s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.IsAny<InfraSyncContext>(),
                It.IsAny<CancellationToken>()))
            .Returns(async (Document _, InfraSyncContext _, CancellationToken ct) =>
            {
                // Simulate a long-running operation
                await Task.Delay(5000, ct);
                return new SyncResult { Status = SyncOperationStatus.Success };
            });

        var context = CreateValidationContext();

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("timed out");
    }

    // ── Bonus Test 1: Disabled step auto-passes ─────────────────────────

    /// <summary>
    /// Verifies that a disabled step returns success without syncing.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_DisabledStep_AutoPasses()
    {
        // Arrange
        var step = CreateStep();
        step.IsEnabled = false;
        var context = CreateValidationContext();

        // Act
        var result = await step.ExecuteAsync(context);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Contain("disabled");
        _syncServiceMock.Verify(
            s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.IsAny<InfraSyncContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── Bonus Test 2: Configuration validation catches empty fields ─────

    /// <summary>
    /// Verifies that ValidateConfiguration catches empty Id and Name,
    /// and negative timeout.
    /// </summary>
    [Fact]
    public void ValidateConfiguration_EmptyFields_ReturnsErrors()
    {
        // Arrange
        var step = new SyncWorkflowStep(
            id: "",
            name: "",
            direction: SyncDirection.DocumentToGraph,
            syncService: _syncServiceMock.Object,
            logger: _stepLoggerMock.Object,
            timeoutMs: -1);

        // Act
        var errors = step.ValidateConfiguration();

        // Assert — should catch: empty Id, empty Name, negative timeout
        errors.Should().HaveCount(3);
        errors.Should().Contain(e => e.Message.Contains("Sync step ID"));
        errors.Should().Contain(e => e.Message.Contains("Sync step name"));
        errors.Should().Contain(e => e.Message.Contains("Timeout"));
    }

    // ── Helper Methods ──────────────────────────────────────────────────

    /// <summary>
    /// Creates a <see cref="SyncWorkflowStep"/> for testing.
    /// </summary>
    private SyncWorkflowStep CreateStep(
        SyncDirection direction = SyncDirection.DocumentToGraph,
        ConflictStrategy conflictStrategy = ConflictStrategy.PreferNewer,
        bool skipIfValidationFailed = true,
        int? timeoutMs = 60000) =>
        new(
            id: "test-sync",
            name: "Test Sync Step",
            direction: direction,
            syncService: _syncServiceMock.Object,
            logger: _stepLoggerMock.Object,
            description: "Test sync step for unit tests",
            conflictStrategy: conflictStrategy,
            skipIfValidationFailed: skipIfValidationFailed,
            timeoutMs: timeoutMs);

    /// <summary>
    /// Creates a <see cref="SyncWorkflowContext"/> for testing.
    /// </summary>
    private static SyncWorkflowContext CreateSyncContext() =>
        new()
        {
            WorkspaceId = Guid.NewGuid(),
            DocumentId = "doc-test-001",
            DocumentContent = "# Test Document\n\nThis is test content for sync."
        };

    /// <summary>
    /// Creates a <see cref="ValidationWorkflowContext"/> for testing.
    /// </summary>
    private static ValidationWorkflowContext CreateValidationContext() =>
        new()
        {
            WorkspaceId = Guid.NewGuid(),
            DocumentId = "doc-test-001",
            DocumentContent = "# Test Document\n\nThis is test content for sync.",
            DocumentType = "markdown",
            Trigger = ValidationTrigger.Manual
        };

    /// <summary>
    /// Sets up the ISyncService mock to return a successful result.
    /// </summary>
    private void SetupSyncServiceSuccess(int entitiesCount = 1)
    {
        var entities = Enumerable.Range(0, entitiesCount)
            .Select(_ => new KnowledgeEntity
            {
                Id = Guid.NewGuid(),
                Name = "TestEntity",
                Type = "Concept"
            })
            .ToList();

        _syncServiceMock
            .Setup(s => s.SyncDocumentToGraphAsync(
                It.IsAny<Document>(),
                It.IsAny<InfraSyncContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SyncResult
            {
                Status = SyncOperationStatus.Success,
                EntitiesAffected = entities
            });
    }
}
