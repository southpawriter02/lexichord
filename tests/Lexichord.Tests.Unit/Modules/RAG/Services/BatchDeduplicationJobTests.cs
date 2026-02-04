// =============================================================================
// File: BatchDeduplicationJobTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for BatchDeduplicationJob service.
// =============================================================================
// VERSION: v0.5.9g (Batch Retroactive Deduplication)
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="BatchDeduplicationJob"/>.
/// </summary>
/// <remarks>
/// These tests focus on constructor validation, license gating, and early-return paths.
/// Full integration tests requiring database interactions are covered in integration test projects.
/// </remarks>
public sealed class BatchDeduplicationJobTests
{
    private readonly Mock<ISimilarityDetector> _similarityDetectorMock;
    private readonly Mock<IRelationshipClassifier> _relationshipClassifierMock;
    private readonly Mock<ICanonicalManager> _canonicalManagerMock;
    private readonly Mock<IContradictionService> _contradictionServiceMock;
    private readonly Mock<IDbConnectionFactory> _connectionFactoryMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<BatchDeduplicationJob>> _loggerMock;

    public BatchDeduplicationJobTests()
    {
        _similarityDetectorMock = new Mock<ISimilarityDetector>();
        _relationshipClassifierMock = new Mock<IRelationshipClassifier>();
        _canonicalManagerMock = new Mock<ICanonicalManager>();
        _contradictionServiceMock = new Mock<IContradictionService>();
        _connectionFactoryMock = new Mock<IDbConnectionFactory>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<BatchDeduplicationJob>>();

        // Default: license enabled
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.BatchDeduplication))
            .Returns(true);
    }

    private BatchDeduplicationJob CreateSut()
    {
        return new BatchDeduplicationJob(
            _similarityDetectorMock.Object,
            _relationshipClassifierMock.Object,
            _canonicalManagerMock.Object,
            _contradictionServiceMock.Object,
            _connectionFactoryMock.Object,
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSimilarityDetector_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationJob(
                null!,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("similarityDetector", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullRelationshipClassifier_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                null!,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("relationshipClassifier", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullCanonicalManager_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                null!,
                _contradictionServiceMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("canonicalManager", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContradictionService_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                null!,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                _loggerMock.Object));

        Assert.Equal("contradictionService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullConnectionFactory_ThrowsArgumentNullException()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
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
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
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
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
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
            new BatchDeduplicationJob(
                _similarityDetectorMock.Object,
                _relationshipClassifierMock.Object,
                _canonicalManagerMock.Object,
                _contradictionServiceMock.Object,
                _connectionFactoryMock.Object,
                _licenseContextMock.Object,
                _mediatorMock.Object,
                null!));

        Assert.Equal("logger", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidDependencies_Succeeds()
    {
        var sut = CreateSut();
        Assert.NotNull(sut);
    }

    #endregion

    #region ExecuteAsync Tests

    [Fact]
    public async Task ExecuteAsync_WhenUnlicensed_ThrowsInvalidOperationException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.BatchDeduplication))
            .Returns(false);

        var sut = CreateSut();
        var options = BatchDeduplicationOptions.Default;

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ExecuteAsync(options));

        Assert.Contains("Teams tier", ex.Message);
    }

    #endregion

    #region ResumeAsync Tests

    [Fact]
    public async Task ResumeAsync_WhenUnlicensed_ThrowsInvalidOperationException()
    {
        // Arrange
        _licenseContextMock
            .Setup(l => l.IsFeatureEnabled(FeatureCodes.BatchDeduplication))
            .Returns(false);

        var sut = CreateSut();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResumeAsync(Guid.NewGuid()));

        Assert.Contains("Teams tier", ex.Message);
    }

    #endregion

    #region BatchDeduplicationOptions Validation Tests

    [Theory]
    [InlineData(5)]
    [InlineData(1001)]
    public async Task ExecuteAsync_WithInvalidBatchSize_ThrowsArgumentException(int batchSize)
    {
        var sut = CreateSut();
        var options = new BatchDeduplicationOptions { BatchSize = batchSize };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(options));

        Assert.Contains("BatchSize", ex.Message);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.5f)]
    public async Task ExecuteAsync_WithInvalidSimilarityThreshold_ThrowsArgumentException(float threshold)
    {
        var sut = CreateSut();
        var options = new BatchDeduplicationOptions { SimilarityThreshold = threshold };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(options));

        Assert.Contains("SimilarityThreshold", ex.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6000)]
    public async Task ExecuteAsync_WithInvalidBatchDelayMs_ThrowsArgumentException(int delayMs)
    {
        var sut = CreateSut();
        var options = new BatchDeduplicationOptions { BatchDelayMs = delayMs };

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            sut.ExecuteAsync(options));

        Assert.Contains("BatchDelayMs", ex.Message);
    }

    #endregion
}
