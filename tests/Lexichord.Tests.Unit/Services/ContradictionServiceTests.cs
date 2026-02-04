// =============================================================================
// File: ContradictionServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContradictionService.
// =============================================================================
// VERSION: v0.5.9e (Contradiction Detection & Resolution)
// LOGIC: Tests the IContradictionService implementation including:
//   - FlagAsync: Contradiction creation and duplicate detection
//   - GetPendingAsync: Query pending contradictions
//   - ResolveAsync: Apply resolution decisions
//   - DismissAsync: Mark as false positive
//   - Event publishing
// =============================================================================

using System.Data.Common;
using Dapper;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Services;

/// <summary>
/// Tests for <see cref="ContradictionService"/>.
/// </summary>
public class ContradictionServiceTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactory = new();
    private readonly Mock<ICanonicalManager> _canonicalManager = new();
    private readonly Mock<IChunkRepository> _chunkRepository = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ILogger<ContradictionService>> _logger = new();
    private readonly Mock<DbConnection> _dbConnection = new();

    private ContradictionService CreateService()
    {
        return new ContradictionService(
            _connectionFactory.Object,
            _canonicalManager.Object,
            _chunkRepository.Object,
            _mediator.Object,
            _logger.Object);
    }

    /// <summary>
    /// Tests that the constructor requires all dependencies.
    /// </summary>
    [Fact]
    public void Constructor_NullDependencies_ThrowsArgumentNullException()
    {
        // Assert
        Assert.Throws<ArgumentNullException>(() => new ContradictionService(
            null!, _canonicalManager.Object, _chunkRepository.Object, _mediator.Object, _logger.Object));

        Assert.Throws<ArgumentNullException>(() => new ContradictionService(
            _connectionFactory.Object, null!, _chunkRepository.Object, _mediator.Object, _logger.Object));

        Assert.Throws<ArgumentNullException>(() => new ContradictionService(
            _connectionFactory.Object, _canonicalManager.Object, null!, _mediator.Object, _logger.Object));

        Assert.Throws<ArgumentNullException>(() => new ContradictionService(
            _connectionFactory.Object, _canonicalManager.Object, _chunkRepository.Object, null!, _logger.Object));

        Assert.Throws<ArgumentNullException>(() => new ContradictionService(
            _connectionFactory.Object, _canonicalManager.Object, _chunkRepository.Object, _mediator.Object, null!));
    }

    /// <summary>
    /// Tests that FlagAsync throws when chunk IDs are the same.
    /// </summary>
    [Fact]
    public async Task FlagAsync_SameChunkIds_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();
        var chunkId = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.FlagAsync(chunkId, chunkId, 0.9f, 0.85f));

        Assert.Contains("itself", ex.Message);
    }

    /// <summary>
    /// Tests that DismissAsync throws for null or empty reason.
    /// </summary>
    [Fact]
    public async Task DismissAsync_NullReason_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - null throws ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.DismissAsync(Guid.NewGuid(), null!, "admin"));

        // Empty throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.DismissAsync(Guid.NewGuid(), "", "admin"));

        // Null dismissedBy throws ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.DismissAsync(Guid.NewGuid(), "reason", null!));
    }

    /// <summary>
    /// Tests that AutoResolveAsync throws for null or empty reason.
    /// </summary>
    [Fact]
    public async Task AutoResolveAsync_NullReason_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - null throws ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.AutoResolveAsync(Guid.NewGuid(), null!));

        // Empty throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.AutoResolveAsync(Guid.NewGuid(), ""));
    }

    /// <summary>
    /// Tests that BeginReviewAsync throws for null or empty reviewer ID.
    /// </summary>
    [Fact]
    public async Task BeginReviewAsync_NullReviewerId_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert - null throws ArgumentNullException
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.BeginReviewAsync(Guid.NewGuid(), null!));

        // Empty throws ArgumentException
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await service.BeginReviewAsync(Guid.NewGuid(), ""));
    }

    /// <summary>
    /// Tests that ResolveAsync throws when CreateSynthesis has no content.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_CreateSynthesisWithoutContent_ThrowsInvalidOperationException()
    {
        // Arrange
        var service = CreateService();
        var resolution = new ContradictionResolution(
            Type: ContradictionResolutionType.CreateSynthesis,
            Rationale: "Test",
            ResolvedAt: DateTimeOffset.UtcNow,
            ResolvedBy: "admin");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await service.ResolveAsync(Guid.NewGuid(), resolution));

        Assert.Contains("synthesized content", ex.Message);
    }

    /// <summary>
    /// Tests that ResolveAsync throws for null resolution.
    /// </summary>
    [Fact]
    public async Task ResolveAsync_NullResolution_ThrowsArgumentNullException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await service.ResolveAsync(Guid.NewGuid(), null!));
    }
}

/// <summary>
/// Tests for <see cref="Contradiction"/> record.
/// </summary>
public class ContradictionTests
{
    /// <summary>
    /// Tests the Create factory method.
    /// </summary>
    [Fact]
    public void Create_ReturnsNewContradiction()
    {
        // Arrange
        var chunkAId = Guid.NewGuid();
        var chunkBId = Guid.NewGuid();

        // Act
        var contradiction = Contradiction.Create(
            chunkAId: chunkAId,
            chunkBId: chunkBId,
            similarityScore: 0.85f,
            confidence: 0.9f,
            reason: "Test reason");

        // Assert
        Assert.Equal(Guid.Empty, contradiction.Id); // Database assigns
        Assert.Equal(chunkAId, contradiction.ChunkAId);
        Assert.Equal(chunkBId, contradiction.ChunkBId);
        Assert.Equal(0.85f, contradiction.SimilarityScore);
        Assert.Equal(0.9f, contradiction.ClassificationConfidence);
        Assert.Equal("Test reason", contradiction.ContradictionReason);
        Assert.Equal(ContradictionStatus.Pending, contradiction.Status);
        Assert.Equal("DeduplicationService", contradiction.DetectedBy);
        Assert.True(contradiction.IsPending);
        Assert.False(contradiction.IsResolved);
        Assert.False(contradiction.IsTerminal);
    }

    /// <summary>
    /// Tests the HasHighConfidence property.
    /// </summary>
    [Theory]
    [InlineData(0.79f, false)]
    [InlineData(0.80f, true)]
    [InlineData(0.90f, true)]
    public void HasHighConfidence_ReturnsCorrectValue(float confidence, bool expected)
    {
        // Arrange
        var contradiction = Contradiction.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.85f,
            confidence);

        // Assert
        Assert.Equal(expected, contradiction.HasHighConfidence);
    }

    /// <summary>
    /// Tests the IsTerminal property for different statuses.
    /// </summary>
    [Theory]
    [InlineData(ContradictionStatus.Pending, false)]
    [InlineData(ContradictionStatus.UnderReview, false)]
    [InlineData(ContradictionStatus.Resolved, true)]
    [InlineData(ContradictionStatus.Dismissed, true)]
    [InlineData(ContradictionStatus.AutoResolved, true)]
    public void IsTerminal_ReturnsCorrectValue(ContradictionStatus status, bool expected)
    {
        // Arrange
        var contradiction = new Contradiction(
            Id: Guid.NewGuid(),
            ChunkAId: Guid.NewGuid(),
            ChunkBId: Guid.NewGuid(),
            SimilarityScore: 0.85f,
            ClassificationConfidence: 0.9f,
            ContradictionReason: null,
            Status: status,
            DetectedAt: DateTimeOffset.UtcNow,
            DetectedBy: "Test");

        // Assert
        Assert.Equal(expected, contradiction.IsTerminal);
    }
}

/// <summary>
/// Tests for <see cref="ContradictionResolution"/> record.
/// </summary>
public class ContradictionResolutionTests
{
    /// <summary>
    /// Tests the KeepOlder factory method.
    /// </summary>
    [Fact]
    public void KeepOlder_CreatesCorrectResolution()
    {
        // Arrange
        var retainedId = Guid.NewGuid();
        var archivedId = Guid.NewGuid();

        // Act
        var resolution = ContradictionResolution.KeepOlder(
            "Old source is authoritative",
            "admin",
            retainedId,
            archivedId);

        // Assert
        Assert.Equal(ContradictionResolutionType.KeepOlder, resolution.Type);
        Assert.Equal("Old source is authoritative", resolution.Rationale);
        Assert.Equal("admin", resolution.ResolvedBy);
        Assert.Equal(retainedId, resolution.RetainedChunkId);
        Assert.Equal(archivedId, resolution.ArchivedChunkId);
        Assert.True(resolution.IsKeepOneSide);
        Assert.False(resolution.IsSynthesis);
        Assert.False(resolution.IsDestructive);
    }

    /// <summary>
    /// Tests the KeepNewer factory method.
    /// </summary>
    [Fact]
    public void KeepNewer_CreatesCorrectResolution()
    {
        // Act
        var resolution = ContradictionResolution.KeepNewer(
            "Newer is correct",
            "admin",
            Guid.NewGuid(),
            Guid.NewGuid());

        // Assert
        Assert.Equal(ContradictionResolutionType.KeepNewer, resolution.Type);
        Assert.True(resolution.IsKeepOneSide);
    }

    /// <summary>
    /// Tests the KeepBoth factory method.
    /// </summary>
    [Fact]
    public void KeepBoth_CreatesCorrectResolution()
    {
        // Act
        var resolution = ContradictionResolution.KeepBoth("Both valid in context", "admin");

        // Assert
        Assert.Equal(ContradictionResolutionType.KeepBoth, resolution.Type);
        Assert.False(resolution.IsKeepOneSide);
        Assert.False(resolution.IsSynthesis);
        Assert.False(resolution.IsDestructive);
    }

    /// <summary>
    /// Tests the Synthesize factory method.
    /// </summary>
    [Fact]
    public void Synthesize_CreatesCorrectResolution()
    {
        // Act
        var resolution = ContradictionResolution.Synthesize(
            "Combined both sources",
            "admin",
            "The synthesized content...");

        // Assert
        Assert.Equal(ContradictionResolutionType.CreateSynthesis, resolution.Type);
        Assert.Equal("The synthesized content...", resolution.SynthesizedContent);
        Assert.True(resolution.IsSynthesis);
        Assert.False(resolution.IsDestructive);
    }

    /// <summary>
    /// Tests the DeleteBoth factory method.
    /// </summary>
    [Fact]
    public void DeleteBoth_CreatesCorrectResolution()
    {
        // Act
        var resolution = ContradictionResolution.DeleteBoth("Both invalid", "admin");

        // Assert
        Assert.Equal(ContradictionResolutionType.DeleteBoth, resolution.Type);
        Assert.True(resolution.IsDestructive);
        Assert.False(resolution.IsKeepOneSide);
        Assert.False(resolution.IsSynthesis);
    }
}

/// <summary>
/// Tests for <see cref="ContradictionDetectedEvent"/>.
/// </summary>
public class ContradictionDetectedEventTests
{
    /// <summary>
    /// Tests the FromContradiction factory method.
    /// </summary>
    [Fact]
    public void FromContradiction_CreatesCorrectEvent()
    {
        // Arrange
        var contradiction = new Contradiction(
            Id: Guid.NewGuid(),
            ChunkAId: Guid.NewGuid(),
            ChunkBId: Guid.NewGuid(),
            SimilarityScore: 0.87f,
            ClassificationConfidence: 0.92f,
            ContradictionReason: "Conflicting dates",
            Status: ContradictionStatus.Pending,
            DetectedAt: DateTimeOffset.UtcNow,
            DetectedBy: "DeduplicationService",
            ProjectId: Guid.NewGuid());

        // Act
        var evt = ContradictionDetectedEvent.FromContradiction(contradiction);

        // Assert
        Assert.Equal(contradiction.Id, evt.ContradictionId);
        Assert.Equal(contradiction.ChunkAId, evt.ChunkAId);
        Assert.Equal(contradiction.ChunkBId, evt.ChunkBId);
        Assert.Equal(contradiction.SimilarityScore, evt.SimilarityScore);
        Assert.Equal(contradiction.ClassificationConfidence, evt.Confidence);
        Assert.Equal(contradiction.ContradictionReason, evt.Reason);
        Assert.Equal(contradiction.ProjectId, evt.ProjectId);
        Assert.Equal(contradiction.DetectedAt, evt.DetectedAt);
        Assert.True(evt.IsHighConfidence);
    }

    /// <summary>
    /// Tests IsHighConfidence threshold.
    /// </summary>
    [Theory]
    [InlineData(0.79f, false)]
    [InlineData(0.80f, true)]
    public void IsHighConfidence_ReturnsCorrectValue(float confidence, bool expected)
    {
        // Arrange
        var evt = new ContradictionDetectedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            0.85f,
            confidence,
            null,
            null,
            DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(expected, evt.IsHighConfidence);
    }
}

/// <summary>
/// Tests for <see cref="ContradictionResolvedEvent"/>.
/// </summary>
public class ContradictionResolvedEventTests
{
    /// <summary>
    /// Tests the ForDismissal factory method.
    /// </summary>
    [Fact]
    public void ForDismissal_CreatesCorrectEvent()
    {
        // Arrange
        var contradiction = new Contradiction(
            Id: Guid.NewGuid(),
            ChunkAId: Guid.NewGuid(),
            ChunkBId: Guid.NewGuid(),
            SimilarityScore: 0.85f,
            ClassificationConfidence: 0.7f,
            ContradictionReason: null,
            Status: ContradictionStatus.Pending,
            DetectedAt: DateTimeOffset.UtcNow,
            DetectedBy: "Test");

        // Act
        var evt = ContradictionResolvedEvent.ForDismissal(contradiction, "False positive", "admin");

        // Assert
        Assert.Equal(contradiction.Id, evt.ContradictionId);
        Assert.Equal(ContradictionStatus.Dismissed, evt.FinalStatus);
        Assert.Null(evt.ResolutionType);
        Assert.Equal("False positive", evt.Rationale);
        Assert.Equal("admin", evt.ResolvedBy);
        Assert.True(evt.IsDismissal);
        Assert.False(evt.IsAdminResolution);
        Assert.False(evt.IsAutoResolved);
    }

    /// <summary>
    /// Tests the ForAutoResolve factory method.
    /// </summary>
    [Fact]
    public void ForAutoResolve_CreatesCorrectEvent()
    {
        // Arrange
        var contradiction = new Contradiction(
            Id: Guid.NewGuid(),
            ChunkAId: Guid.NewGuid(),
            ChunkBId: Guid.NewGuid(),
            SimilarityScore: 0.85f,
            ClassificationConfidence: 0.7f,
            ContradictionReason: null,
            Status: ContradictionStatus.Pending,
            DetectedAt: DateTimeOffset.UtcNow,
            DetectedBy: "Test");

        // Act
        var evt = ContradictionResolvedEvent.ForAutoResolve(contradiction, "Document deleted");

        // Assert
        Assert.Equal(ContradictionStatus.AutoResolved, evt.FinalStatus);
        Assert.Null(evt.ResolutionType);
        Assert.Equal("Document deleted", evt.Rationale);
        Assert.Equal("System", evt.ResolvedBy);
        Assert.True(evt.IsAutoResolved);
        Assert.False(evt.IsDismissal);
        Assert.False(evt.IsAdminResolution);
    }

    /// <summary>
    /// Tests the FromContradiction factory method.
    /// </summary>
    [Fact]
    public void FromContradiction_CreatesCorrectEvent()
    {
        // Arrange
        var resolution = ContradictionResolution.KeepOlder(
            "Original is authoritative",
            "admin",
            Guid.NewGuid(),
            Guid.NewGuid());

        var contradiction = new Contradiction(
            Id: Guid.NewGuid(),
            ChunkAId: Guid.NewGuid(),
            ChunkBId: Guid.NewGuid(),
            SimilarityScore: 0.85f,
            ClassificationConfidence: 0.9f,
            ContradictionReason: "Conflict",
            Status: ContradictionStatus.Resolved,
            DetectedAt: DateTimeOffset.UtcNow,
            DetectedBy: "Test",
            Resolution: resolution);

        // Act
        var evt = ContradictionResolvedEvent.FromContradiction(contradiction);

        // Assert
        Assert.Equal(contradiction.Id, evt.ContradictionId);
        Assert.Equal(ContradictionStatus.Resolved, evt.FinalStatus);
        Assert.Equal(ContradictionResolutionType.KeepOlder, evt.ResolutionType);
        Assert.Equal(resolution.Rationale, evt.Rationale);
        Assert.Equal(resolution.ResolvedBy, evt.ResolvedBy);
        Assert.Equal(resolution.RetainedChunkId, evt.RetainedChunkId);
        Assert.Equal(resolution.ArchivedChunkId, evt.ArchivedChunkId);
        Assert.True(evt.IsAdminResolution);
        Assert.False(evt.IsDismissal);
        Assert.False(evt.IsAutoResolved);
        Assert.False(evt.IsDestructive);
    }

    /// <summary>
    /// Tests IsDestructive for DeleteBoth resolution.
    /// </summary>
    [Fact]
    public void IsDestructive_DeleteBoth_ReturnsTrue()
    {
        // Arrange
        var evt = new ContradictionResolvedEvent(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ContradictionStatus.Resolved,
            ContradictionResolutionType.DeleteBoth,
            "Invalid data",
            "admin",
            DateTimeOffset.UtcNow);

        // Assert
        Assert.True(evt.IsDestructive);
    }
}
