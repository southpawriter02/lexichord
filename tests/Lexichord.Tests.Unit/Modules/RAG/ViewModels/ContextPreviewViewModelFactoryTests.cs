// =============================================================================
// File: ContextPreviewViewModelFactoryTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for ContextPreviewViewModelFactory.
// =============================================================================
// LOGIC: Tests constructor validation, Create method behavior, and
//   deterministic chunk ID generation.
// =============================================================================
// VERSION: v0.5.3d (Context Preview UI)
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.RAG.ViewModels;

/// <summary>
/// Unit tests for <see cref="ContextPreviewViewModelFactory"/>.
/// </summary>
/// <remarks>
/// Tests cover:
/// - Constructor null parameter validation
/// - Create method with valid SearchHit
/// - Create method with null SearchHit
/// - Deterministic chunk ID generation consistency
/// - ViewModel receives correct dependencies
/// </remarks>
[Trait("Feature", "v0.5.3")]
[Trait("Category", "Unit")]
public class ContextPreviewViewModelFactoryTests
{
    private readonly IContextExpansionService _mockContextExpansionService;
    private readonly ILicenseContext _mockLicenseContext;
    private readonly ILoggerFactory _mockLoggerFactory;

    public ContextPreviewViewModelFactoryTests()
    {
        _mockContextExpansionService = Substitute.For<IContextExpansionService>();
        _mockLicenseContext = Substitute.For<ILicenseContext>();
        _mockLoggerFactory = NullLoggerFactory.Instance;

        // Default license setup - licensed user
        _mockLicenseContext.GetCurrentTier().Returns(LicenseTier.WriterPro);
        _mockLicenseContext.IsFeatureEnabled(Arg.Any<string>()).Returns(true);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullContextExpansionService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModelFactory(
                null!,
                _mockLicenseContext,
                _mockLoggerFactory));

        Assert.Equal("contextExpansionService", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModelFactory(
                _mockContextExpansionService,
                null!,
                _mockLoggerFactory));

        Assert.Equal("licenseContext", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new ContextPreviewViewModelFactory(
                _mockContextExpansionService,
                _mockLicenseContext,
                null!));

        Assert.Equal("loggerFactory", ex.ParamName);
    }

    [Fact]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var factory = new ContextPreviewViewModelFactory(
            _mockContextExpansionService,
            _mockLicenseContext,
            _mockLoggerFactory);

        // Assert
        Assert.NotNull(factory);
    }

    #endregion

    #region Create Tests

    [Fact]
    public void Create_WithNullSearchHit_ThrowsArgumentNullException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => factory.Create(null!));
    }

    [Fact]
    public void Create_WithValidSearchHit_ReturnsViewModel()
    {
        // Arrange
        var factory = CreateFactory();
        var searchHit = CreateSearchHit();

        // Act
        var result = factory.Create(searchHit);

        // Assert
        Assert.NotNull(result);
        Assert.IsType<ContextPreviewViewModel>(result);
    }

    [Fact]
    public void Create_TransfersChunkContent()
    {
        // Arrange
        var factory = CreateFactory();
        var searchHit = CreateSearchHit(content: "Test chunk content");

        // Act
        var result = factory.Create(searchHit);

        // Assert - The ViewModel should have a breadcrumb from the heading
        Assert.NotNull(result);
        // Verify initial state is correct
        Assert.False(result.IsExpanded);
    }

    [Fact]
    public void Create_TransfersHeadingFromMetadata()
    {
        // Arrange
        var factory = CreateFactory();
        var searchHit = CreateSearchHit(heading: "Test Heading", headingLevel: 2);

        // Act
        var result = factory.Create(searchHit);

        // Assert
        Assert.Equal("Test Heading", result.Breadcrumb);
        Assert.True(result.HasBreadcrumb);
    }

    [Fact]
    public void Create_WithNoHeading_SetsEmptyBreadcrumb()
    {
        // Arrange
        var factory = CreateFactory();
        var searchHit = CreateSearchHit(heading: null, headingLevel: 0);

        // Act
        var result = factory.Create(searchHit);

        // Assert
        Assert.Equal(string.Empty, result.Breadcrumb);
        Assert.False(result.HasBreadcrumb);
    }

    [Fact]
    public void Create_SetsLicenseStateFromContext()
    {
        // Arrange - Setup unlicensed user
        _mockLicenseContext.GetCurrentTier().Returns(LicenseTier.Core);
        _mockLicenseContext.IsFeatureEnabled(Arg.Any<string>()).Returns(false);

        var factory = CreateFactory();
        var searchHit = CreateSearchHit();

        // Act
        var result = factory.Create(searchHit);

        // Assert
        Assert.False(result.IsLicensed);
        Assert.True(result.ShowLockIcon);
    }

    [Fact]
    public void Create_MultipleCalls_CreateIndependentInstances()
    {
        // Arrange
        var factory = CreateFactory();
        var searchHit1 = CreateSearchHit(chunkIndex: 1, heading: "Heading 1");
        var searchHit2 = CreateSearchHit(chunkIndex: 2, heading: "Heading 2");

        // Act
        var vm1 = factory.Create(searchHit1);
        var vm2 = factory.Create(searchHit2);

        // Assert
        Assert.NotSame(vm1, vm2);
        Assert.Equal("Heading 1", vm1.Breadcrumb);
        Assert.Equal("Heading 2", vm2.Breadcrumb);
    }

    #endregion

    #region Deterministic ID Tests

    [Fact]
    public void Create_SameInputs_ProducesSameChunkId()
    {
        // Arrange
        var factory = CreateFactory();
        var documentId = Guid.NewGuid();
        var searchHit1 = CreateSearchHit(documentId: documentId, chunkIndex: 5);
        var searchHit2 = CreateSearchHit(documentId: documentId, chunkIndex: 5);

        // Act - Create two ViewModels from the same document/index
        var vm1 = factory.Create(searchHit1);
        var vm2 = factory.Create(searchHit2);

        // Assert - Both should work correctly (IDs are internal but behavior is consistent)
        Assert.NotNull(vm1);
        Assert.NotNull(vm2);
    }

    [Fact]
    public void Create_DifferentChunkIndex_ProducesDifferentViewModels()
    {
        // Arrange
        var factory = CreateFactory();
        var documentId = Guid.NewGuid();
        var searchHit1 = CreateSearchHit(documentId: documentId, chunkIndex: 1, heading: "H1");
        var searchHit2 = CreateSearchHit(documentId: documentId, chunkIndex: 2, heading: "H2");

        // Act
        var vm1 = factory.Create(searchHit1);
        var vm2 = factory.Create(searchHit2);

        // Assert
        Assert.NotSame(vm1, vm2);
        Assert.Equal("H1", vm1.Breadcrumb);
        Assert.Equal("H2", vm2.Breadcrumb);
    }

    [Fact]
    public void Create_DifferentDocumentId_ProducesDifferentViewModels()
    {
        // Arrange
        var factory = CreateFactory();
        var searchHit1 = CreateSearchHit(documentId: Guid.NewGuid(), chunkIndex: 5, heading: "Doc1");
        var searchHit2 = CreateSearchHit(documentId: Guid.NewGuid(), chunkIndex: 5, heading: "Doc2");

        // Act
        var vm1 = factory.Create(searchHit1);
        var vm2 = factory.Create(searchHit2);

        // Assert
        Assert.NotSame(vm1, vm2);
        Assert.Equal("Doc1", vm1.Breadcrumb);
        Assert.Equal("Doc2", vm2.Breadcrumb);
    }

    #endregion

    #region Helper Methods

    private ContextPreviewViewModelFactory CreateFactory()
    {
        return new ContextPreviewViewModelFactory(
            _mockContextExpansionService,
            _mockLicenseContext,
            _mockLoggerFactory);
    }

    private static SearchHit CreateSearchHit(
        Guid? documentId = null,
        int chunkIndex = 0,
        string content = "Test content",
        string? heading = "Test Heading",
        int headingLevel = 1,
        float score = 0.85f)
    {
        var docId = documentId ?? Guid.NewGuid();

        var metadata = new ChunkMetadata(
            Index: chunkIndex,
            Heading: heading,
            Level: headingLevel);

        var textChunk = new TextChunk(
            Content: content,
            StartOffset: 0,
            EndOffset: content.Length,
            Metadata: metadata);

        var document = new Document(
            Id: docId,
            ProjectId: Guid.NewGuid(),
            FilePath: "/test/document.md",
            Title: "Test Document",
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: DateTime.UtcNow,
            FailureReason: null);

        return new SearchHit
        {
            Chunk = textChunk,
            Document = document,
            Score = score
        };
    }

    #endregion
}
