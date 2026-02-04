// =============================================================================
// File: CanonicalManagerTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CanonicalManager service.
// =============================================================================
// VERSION: v0.5.9c (Canonical Record Management)
// =============================================================================

using System.Data;
using System.Data.Common;
using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CanonicalManager"/>.
/// </summary>
public sealed class CanonicalManagerTests
{
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<CanonicalManager>> _loggerMock;

    public CanonicalManagerTests()
    {
        _connectionFactoryMock = new Mock<IDbConnectionFactory>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<CanonicalManager>>();

        // Default: license enabled
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(true);
    }

    private CanonicalManager CreateSut()
    {
        return new CanonicalManager(
            _connectionFactoryMock.Object,
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    private static Chunk CreateTestChunk(
        Guid? id = null,
        Guid? documentId = null,
        int chunkIndex = 0,
        string? content = null)
    {
        return new Chunk(
            Id: id ?? Guid.NewGuid(),
            DocumentId: documentId ?? Guid.NewGuid(),
            Content: content ?? $"Test content for chunk {chunkIndex}",
            Embedding: new float[] { 0.1f, 0.2f, 0.3f },
            ChunkIndex: chunkIndex,
            StartOffset: chunkIndex * 100,
            EndOffset: (chunkIndex + 1) * 100);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CanonicalManager(
                null!,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("connectionFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CanonicalManager(
                _connectionFactoryMock.Object,
                null!,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullMediator_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CanonicalManager(
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                null!,
                _loggerMock.Object));

        Assert.Equal("mediator", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new CanonicalManager(
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        // Act
        var sut = CreateSut();

        // Assert
        Assert.NotNull(sut);
    }

    #endregion

    #region License Gating Tests

    [Fact]
    public async Task CreateCanonicalAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(false);

        var sut = CreateSut();
        var chunk = CreateTestChunk();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(() =>
            sut.CreateCanonicalAsync(chunk));

        Assert.Equal(LicenseTier.WriterPro, ex.RequiredTier);
    }

    [Fact]
    public async Task MergeIntoCanonicalAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(false);

        var sut = CreateSut();
        var variant = CreateTestChunk();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(() =>
            sut.MergeIntoCanonicalAsync(Guid.NewGuid(), variant, RelationshipType.Equivalent, 0.95f));

        Assert.Equal(LicenseTier.WriterPro, ex.RequiredTier);
    }

    [Fact]
    public async Task PromoteVariantAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(false);

        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(() =>
            sut.PromoteVariantAsync(Guid.NewGuid(), Guid.NewGuid(), "Test reason"));

        Assert.Equal(LicenseTier.WriterPro, ex.RequiredTier);
    }

    [Fact]
    public async Task DetachVariantAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(false);

        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(() =>
            sut.DetachVariantAsync(Guid.NewGuid()));

        Assert.Equal(LicenseTier.WriterPro, ex.RequiredTier);
    }

    [Fact]
    public async Task RecordProvenanceAsync_WithoutLicense_ThrowsFeatureNotLicensedException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.SemanticDeduplication))
            .Returns(false);

        var sut = CreateSut();
        var provenance = ChunkProvenance.Create(Guid.NewGuid(), Guid.NewGuid(), "location");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<FeatureNotLicensedException>(() =>
            sut.RecordProvenanceAsync(Guid.NewGuid(), provenance));

        Assert.Equal(LicenseTier.WriterPro, ex.RequiredTier);
    }

    #endregion

    #region CreateCanonicalAsync Tests

    [Fact]
    public async Task CreateCanonicalAsync_WithNullChunk_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.CreateCanonicalAsync(null!));
    }

    #endregion

    #region MergeIntoCanonicalAsync Tests

    [Fact]
    public async Task MergeIntoCanonicalAsync_WithEmptyCanonicalId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var variant = CreateTestChunk();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.MergeIntoCanonicalAsync(Guid.Empty, variant, RelationshipType.Equivalent, 0.95f));

        Assert.Equal("canonicalId", ex.ParamName);
    }

    [Fact]
    public async Task MergeIntoCanonicalAsync_WithNullVariant_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.MergeIntoCanonicalAsync(Guid.NewGuid(), null!, RelationshipType.Equivalent, 0.95f));
    }

    #endregion

    #region GetCanonicalForChunkAsync Tests

    [Fact]
    public async Task GetCanonicalForChunkAsync_WithEmptyChunkId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetCanonicalForChunkAsync(Guid.Empty));

        Assert.Equal("chunkId", ex.ParamName);
    }

    #endregion

    #region GetVariantsAsync Tests

    [Fact]
    public async Task GetVariantsAsync_WithEmptyCanonicalId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetVariantsAsync(Guid.Empty));

        Assert.Equal("canonicalId", ex.ParamName);
    }

    #endregion

    #region PromoteVariantAsync Tests

    [Fact]
    public async Task PromoteVariantAsync_WithEmptyCanonicalId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.PromoteVariantAsync(Guid.Empty, Guid.NewGuid(), "Test reason"));

        Assert.Equal("canonicalId", ex.ParamName);
    }

    [Fact]
    public async Task PromoteVariantAsync_WithEmptyNewChunkId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.PromoteVariantAsync(Guid.NewGuid(), Guid.Empty, "Test reason"));

        Assert.Equal("newCanonicalChunkId", ex.ParamName);
    }

    [Fact]
    public async Task PromoteVariantAsync_WithNullReason_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.PromoteVariantAsync(Guid.NewGuid(), Guid.NewGuid(), null!));
    }

    [Fact]
    public async Task PromoteVariantAsync_WithEmptyReason_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.PromoteVariantAsync(Guid.NewGuid(), Guid.NewGuid(), ""));
    }

    [Fact]
    public async Task PromoteVariantAsync_WithWhitespaceReason_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.PromoteVariantAsync(Guid.NewGuid(), Guid.NewGuid(), "   "));
    }

    #endregion

    #region DetachVariantAsync Tests

    [Fact]
    public async Task DetachVariantAsync_WithEmptyVariantChunkId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.DetachVariantAsync(Guid.Empty));

        Assert.Equal("variantChunkId", ex.ParamName);
    }

    #endregion

    #region RecordProvenanceAsync Tests

    [Fact]
    public async Task RecordProvenanceAsync_WithEmptyChunkId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var provenance = ChunkProvenance.Create(Guid.NewGuid(), Guid.NewGuid(), "location");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.RecordProvenanceAsync(Guid.Empty, provenance));

        Assert.Equal("chunkId", ex.ParamName);
    }

    [Fact]
    public async Task RecordProvenanceAsync_WithNullProvenance_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.RecordProvenanceAsync(Guid.NewGuid(), null!));
    }

    #endregion

    #region GetProvenanceAsync Tests

    [Fact]
    public async Task GetProvenanceAsync_WithEmptyCanonicalId_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.GetProvenanceAsync(Guid.Empty));

        Assert.Equal("canonicalId", ex.ParamName);
    }

    #endregion
}
