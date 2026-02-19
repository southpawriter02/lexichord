// =============================================================================
// File: EnhancedConflictResolverTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for EnhancedConflictResolver service.
// =============================================================================
// v0.7.6h: Conflict Resolver (CKVS Phase 4c)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Knowledge.Sync;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict;
using Lexichord.Abstractions.Contracts.Knowledge.Sync.Conflict.Events;
using Lexichord.Modules.Knowledge.Sync.Conflict;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Knowledge.Sync.Conflict;

/// <summary>
/// Unit tests for <see cref="EnhancedConflictResolver"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6h")]
public class EnhancedConflictResolverTests
{
    private readonly Mock<IConflictMerger> _mockMerger;
    private readonly ConflictStore _conflictStore;
    private readonly Mock<ILicenseContext> _mockLicenseContext;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ILogger<EnhancedConflictResolver>> _mockLogger;
    private readonly EnhancedConflictResolver _sut;

    public EnhancedConflictResolverTests()
    {
        _mockMerger = new Mock<IConflictMerger>();
        _conflictStore = new ConflictStore(new Mock<ILogger<ConflictStore>>().Object);
        _mockLicenseContext = new Mock<ILicenseContext>();
        _mockMediator = new Mock<IMediator>();
        _mockLogger = new Mock<ILogger<EnhancedConflictResolver>>();

        _sut = new EnhancedConflictResolver(
            _mockMerger.Object,
            _conflictStore,
            _mockLicenseContext.Object,
            _mockMediator.Object,
            _mockLogger.Object);
    }

    #region ResolveConflictAsync Tests - License Gating

    [Fact]
    public async Task ResolveConflictAsync_CoreTier_ReturnsFailure()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Core);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("License tier", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveConflictAsync_WriterProTier_LowSeverity_Succeeds()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(conflict.DocumentValue, result.ResolvedValue);
    }

    [Fact]
    public async Task ResolveConflictAsync_WriterProTier_MediumSeverity_NonManual_Fails()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Medium);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveConflictAsync_WriterProTier_Manual_Succeeds()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.High);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.Manual);

        // Assert
        Assert.False(result.Succeeded); // Manual requires intervention
        Assert.Equal("Conflict requires manual intervention", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveConflictAsync_WriterProTier_Merge_Fails()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.Merge);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("License tier", result.ErrorMessage);
    }

    [Fact]
    public async Task ResolveConflictAsync_TeamsTier_MediumSeverity_Succeeds()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Medium);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ResolveConflictAsync_TeamsTier_HighSeverity_NonManual_Fails()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.High);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task ResolveConflictAsync_EnterpriseTier_HighSeverity_Succeeds()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.High);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.True(result.Succeeded);
    }

    #endregion

    #region ResolveConflictAsync Tests - Strategy Execution

    [Fact]
    public async Task ResolveConflictAsync_UseDocument_ReturnsDocumentValue()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(conflict.DocumentValue, result.ResolvedValue);
        Assert.Equal(ConflictResolutionStrategy.UseDocument, result.Strategy);
        Assert.True(result.IsAutomatic);
    }

    [Fact]
    public async Task ResolveConflictAsync_UseGraph_ReturnsGraphValue()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.UseGraph);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(conflict.GraphValue, result.ResolvedValue);
        Assert.Equal(ConflictResolutionStrategy.UseGraph, result.Strategy);
    }

    [Fact]
    public async Task ResolveConflictAsync_DiscardDocument_ReturnsGraphValue()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.DiscardDocument);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(conflict.GraphValue, result.ResolvedValue);
    }

    [Fact]
    public async Task ResolveConflictAsync_DiscardGraph_ReturnsDocumentValue()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.DiscardGraph);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(conflict.DocumentValue, result.ResolvedValue);
    }

    [Fact]
    public async Task ResolveConflictAsync_Manual_RequiresIntervention()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.Manual);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Equal(ConflictResolutionStrategy.Manual, result.Strategy);
        Assert.False(result.IsAutomatic);
    }

    [Fact]
    public async Task ResolveConflictAsync_Merge_Success_ReturnsMergedValue()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        var mergeResult = new MergeResult
        {
            Success = true,
            MergedValue = "merged-value",
            UsedStrategy = MergeStrategy.Combine,
            Confidence = 0.9f,
            MergeType = MergeType.Intelligent
        };

        _mockMerger
            .Setup(m => m.MergeAsync(
                conflict.DocumentValue,
                conflict.GraphValue,
                It.IsAny<MergeContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeResult);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.Merge);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal("merged-value", result.ResolvedValue);
        Assert.Equal(ConflictResolutionStrategy.Merge, result.Strategy);
    }

    [Fact]
    public async Task ResolveConflictAsync_Merge_Failure_ReturnsFailure()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        var mergeResult = MergeResult.RequiresManual("Cannot merge incompatible values");

        _mockMerger
            .Setup(m => m.MergeAsync(
                conflict.DocumentValue,
                conflict.GraphValue,
                It.IsAny<MergeContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeResult);

        // Act
        var result = await _sut.ResolveConflictAsync(
            conflict, ConflictResolutionStrategy.Merge);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Cannot merge", result.ErrorMessage);
    }

    #endregion

    #region ResolveConflictAsync Tests - Event Publishing

    [Fact]
    public async Task ResolveConflictAsync_Success_PublishesEvent()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        await _sut.ResolveConflictAsync(conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<ConflictResolvedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveConflictAsync_Failure_DoesNotPublishEvent()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Core);

        // Act
        await _sut.ResolveConflictAsync(conflict, ConflictResolutionStrategy.UseDocument);

        // Assert
        _mockMediator.Verify(
            m => m.Publish(It.IsAny<ConflictResolvedEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region ResolveConflictsAsync Tests

    [Fact]
    public async Task ResolveConflictsAsync_MultipleConflicts_ResolvesAll()
    {
        // Arrange
        var conflicts = Enumerable.Range(1, 5)
            .Select(_ => CreateTestConflict(ConflictSeverity.Low))
            .ToList();
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var results = await _sut.ResolveConflictsAsync(
            conflicts, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.Equal(5, results.Count);
        Assert.All(results, r => Assert.True(r.Succeeded));
    }

    [Fact]
    public async Task ResolveConflictsAsync_MixedResults_ReturnsAll()
    {
        // Arrange
        var lowConflict = CreateTestConflict(ConflictSeverity.Low);
        var highConflict = CreateTestConflict(ConflictSeverity.High);
        var conflicts = new List<SyncConflict> { lowConflict, highConflict };
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act
        var results = await _sut.ResolveConflictsAsync(
            conflicts, ConflictResolutionStrategy.UseDocument);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.True(results[0].Succeeded); // Low severity
        Assert.False(results[1].Succeeded); // High severity blocked
    }

    [Fact]
    public async Task ResolveConflictsAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var conflicts = new List<SyncConflict> { CreateTestConflict(ConflictSeverity.Low) };
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ResolveConflictsAsync(conflicts, ConflictResolutionStrategy.UseDocument, cts.Token));
    }

    #endregion

    #region MergeConflictAsync Tests

    [Fact]
    public async Task MergeConflictAsync_TeamsTier_CallsMerger()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Medium);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Teams);

        var mergeResult = MergeResult.DocumentWins("doc-value");
        _mockMerger
            .Setup(m => m.MergeAsync(
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<MergeContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeResult);

        // Act
        var result = await _sut.MergeConflictAsync(conflict);

        // Assert
        Assert.True(result.Success);
        _mockMerger.Verify(
            m => m.MergeAsync(
                conflict.DocumentValue,
                conflict.GraphValue,
                It.IsAny<MergeContext>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task MergeConflictAsync_WriterProTier_ReturnsFailure()
    {
        // Arrange
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro);

        // Act
        var result = await _sut.MergeConflictAsync(conflict);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Teams license", result.ErrorMessage);
        Assert.Equal(MergeStrategy.RequiresManualMerge, result.UsedStrategy);
    }

    #endregion

    #region GetUnresolvedConflictsAsync Tests

    [Fact]
    public async Task GetUnresolvedConflictsAsync_ReturnsConflictsFromStore()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var conflict = CreateTestConflict(ConflictSeverity.Medium);
        _conflictStore.Add(conflict, documentId);

        // Act
        var result = await _sut.GetUnresolvedConflictsAsync(documentId);

        // Assert
        Assert.Single(result);
    }

    [Fact]
    public async Task GetUnresolvedConflictsAsync_NoConflicts_ReturnsEmpty()
    {
        // Arrange
        var documentId = Guid.NewGuid();

        // Act
        var result = await _sut.GetUnresolvedConflictsAsync(documentId);

        // Assert
        Assert.Empty(result);
    }

    #endregion

    #region ResolveAllAsync Tests

    [Fact]
    public async Task ResolveAllAsync_NoConflicts_ReturnsNoChanges()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveAllAsync(documentId, null);

        // Assert
        Assert.Equal(SyncOperationStatus.NoChanges, result.Status);
    }

    [Fact]
    public async Task ResolveAllAsync_AllResolved_ReturnsSuccess()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _conflictStore.Add(conflict, documentId);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        var options = new ConflictResolutionOptions
        {
            DefaultStrategy = ConflictResolutionStrategy.UseDocument,
            AutoResolveLow = true
        };

        // Act
        var result = await _sut.ResolveAllAsync(documentId, options);

        // Assert
        Assert.Equal(SyncOperationStatus.Success, result.Status);
        Assert.Empty(result.Conflicts);
    }

    [Fact]
    public async Task ResolveAllAsync_PartialResolution_ReturnsSuccessWithConflicts()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var lowConflict = CreateTestConflict(ConflictSeverity.Low);
        var highConflict = CreateTestConflict(ConflictSeverity.High);
        _conflictStore.Add(lowConflict, documentId);
        _conflictStore.Add(highConflict, documentId);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        var options = new ConflictResolutionOptions
        {
            DefaultStrategy = ConflictResolutionStrategy.UseDocument,
            AutoResolveLow = true,
            AutoResolveMedium = false,
            AutoResolveHigh = false
        };

        // Act
        var result = await _sut.ResolveAllAsync(documentId, options);

        // Assert
        Assert.Equal(SyncOperationStatus.SuccessWithConflicts, result.Status);
        Assert.Single(result.Conflicts); // High severity not auto-resolved
    }

    [Fact]
    public async Task ResolveAllAsync_AllFailed_ReturnsFailed()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var highConflict = CreateTestConflict(ConflictSeverity.High);
        _conflictStore.Add(highConflict, documentId);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.WriterPro); // Can't resolve High

        var options = new ConflictResolutionOptions
        {
            DefaultStrategy = ConflictResolutionStrategy.UseDocument,
            AutoResolveLow = true,
            AutoResolveMedium = true,
            AutoResolveHigh = true
        };

        // Act
        var result = await _sut.ResolveAllAsync(documentId, options);

        // Assert
        Assert.Equal(SyncOperationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task ResolveAllAsync_UsesDefaultOptions_WhenNullProvided()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        var conflict = CreateTestConflict(ConflictSeverity.Low);
        _conflictStore.Add(conflict, documentId);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // LOGIC: Default options use Merge strategy, so we need to mock the merger
        var mergeResult = new MergeResult
        {
            Success = true,
            MergedValue = "merged-value",
            UsedStrategy = MergeStrategy.Combine,
            Confidence = 0.9f,
            MergeType = MergeType.Intelligent
        };
        _mockMerger
            .Setup(m => m.MergeAsync(
                It.IsAny<object>(),
                It.IsAny<object>(),
                It.IsAny<MergeContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mergeResult);

        // Act
        var result = await _sut.ResolveAllAsync(documentId, null);

        // Assert
        // Default options: AutoResolveLow = true, DefaultStrategy = Merge
        Assert.Equal(SyncOperationStatus.Success, result.Status);
    }

    [Fact]
    public async Task ResolveAllAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _conflictStore.Add(CreateTestConflict(ConflictSeverity.Low), documentId);
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _sut.ResolveAllAsync(documentId, null, cts.Token));
    }

    [Fact]
    public async Task ResolveAllAsync_RecordsDuration()
    {
        // Arrange
        var documentId = Guid.NewGuid();
        _mockLicenseContext.Setup(x => x.Tier).Returns(LicenseTier.Enterprise);

        // Act
        var result = await _sut.ResolveAllAsync(documentId, null);

        // Assert
        Assert.True(result.Duration >= TimeSpan.Zero);
    }

    #endregion

    #region Helper Methods

    private static SyncConflict CreateTestConflict(ConflictSeverity severity)
    {
        return new SyncConflict
        {
            ConflictTarget = $"Entity:Test-{Guid.NewGuid():N}.Property",
            DocumentValue = "doc-value",
            GraphValue = "graph-value",
            DetectedAt = DateTimeOffset.UtcNow,
            Type = ConflictType.ValueMismatch,
            Severity = severity
        };
    }

    #endregion
}
