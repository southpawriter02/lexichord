// =============================================================================
// File: SearchActionsServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SearchActionsService search result operations.
// =============================================================================
// LOGIC: Verifies all ISearchActionsService functionality:
//   - Constructor null-parameter validation (5 dependencies).
//   - CopyResultsAsync: formats results and returns correct result.
//   - CopyResultsAsync: denies citation format without license.
//   - ExportResultsAsync: serializes to all formats correctly.
//   - ExportResultsAsync: denies export without Writer Pro license.
//   - ExportResultsAsync: publishes SearchResultsExportedEvent.
//   - OpenAllDocumentsAsync: opens documents and tracks counts.
//   - OpenAllDocumentsAsync: skips already-open documents.
//   - OpenAllDocumentsAsync: handles failures gracefully.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="SearchActionsService"/>.
/// Verifies copy, export, and open-all operations with license gating.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.7d")]
public class SearchActionsServiceTests
{
    // LOGIC: Mock dependencies for constructor injection.
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<ICitationService> _citationServiceMock;
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<SearchActionsService>> _loggerMock;

    // LOGIC: System under test.
    private readonly SearchActionsService _sut;

    // LOGIC: Feature code used for license checks.
    private const string FeatureCode = "RAG-SEARCH-ACTIONS";

    public SearchActionsServiceTests()
    {
        _licenseContextMock = new Mock<ILicenseContext>();
        _citationServiceMock = new Mock<ICitationService>();
        _editorServiceMock = new Mock<IEditorService>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<SearchActionsService>>();

        // LOGIC: Default setup — license is enabled (Writer Pro).
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCode))
            .Returns(true);

        // LOGIC: Default citation formatting returns a simple formatted string.
        _citationServiceMock
            .Setup(x => x.CreateCitation(It.IsAny<SearchHit>()))
            .Returns((SearchHit hit) => CreateTestCitation(hit.Document.FilePath));

        _citationServiceMock
            .Setup(x => x.FormatCitation(It.IsAny<Citation>(), It.IsAny<CitationStyle>()))
            .Returns((Citation c, CitationStyle s) => $"[{c.FileName} ({s})]");

        _sut = new SearchActionsService(
            _licenseContextMock.Object,
            _citationServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    // =========================================================================
    // Test Helpers
    // =========================================================================

    private static SearchResultSet CreateTestResults(int groupCount = 2, int hitsPerGroup = 3)
    {
        var groups = new List<SearchResultGroup>();

        for (var g = 0; g < groupCount; g++)
        {
            var hits = new List<SearchHit>();
            for (var h = 0; h < hitsPerGroup; h++)
            {
                hits.Add(CreateTestHit($"/docs/doc{g}.md", $"Content {g}-{h}"));
            }

            groups.Add(new SearchResultGroup(
                DocumentPath: $"/docs/doc{g}.md",
                DocumentTitle: $"Document {g}",
                Hits: hits));
        }

        return new SearchResultSet(
            Groups: groups,
            TotalHits: groupCount * hitsPerGroup,
            TotalDocuments: groupCount,
            Query: "test query");
    }

    private static SearchHit CreateTestHit(string path, string content)
    {
        return new SearchHit
        {
            Chunk = new TextChunk(
                Content: content,
                StartOffset: 0,
                EndOffset: content.Length,
                Metadata: new ChunkMetadata(Heading: null, Index: 0, Level: 0)),
            Document = new Document(
                Id: Guid.NewGuid(),
                ProjectId: Guid.NewGuid(),
                FilePath: path,
                Title: Path.GetFileNameWithoutExtension(path),
                Hash: "abc123",
                Status: DocumentStatus.Indexed,
                IndexedAt: DateTime.UtcNow,
                FailureReason: null),
            Score = 0.85f
        };
    }

    private static Citation CreateTestCitation(string path = "/docs/test.md")
    {
        return new Citation(
            ChunkId: Guid.NewGuid(),
            DocumentPath: path,
            DocumentTitle: Path.GetFileNameWithoutExtension(path),
            StartOffset: 0,
            EndOffset: 100,
            Heading: null,
            LineNumber: 1,
            IndexedAt: DateTime.UtcNow);
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchActionsService(
            null!,
            _citationServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("licenseContext", act);
    }

    [Fact]
    public void Constructor_NullCitationService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchActionsService(
            _licenseContextMock.Object,
            null!,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("citationService", act);
    }

    [Fact]
    public void Constructor_NullEditorService_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchActionsService(
            _licenseContextMock.Object,
            _citationServiceMock.Object,
            null!,
            _mediatorMock.Object,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("editorService", act);
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchActionsService(
            _licenseContextMock.Object,
            _citationServiceMock.Object,
            _editorServiceMock.Object,
            null!,
            _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("mediator", act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SearchActionsService(
            _licenseContextMock.Object,
            _citationServiceMock.Object,
            _editorServiceMock.Object,
            _mediatorMock.Object,
            null!);
        Assert.Throws<ArgumentNullException>("logger", act);
    }

    // =========================================================================
    // CopyResultsAsync
    // =========================================================================

    [Fact]
    public async Task CopyResultsAsync_NullResults_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "results",
            () => _sut.CopyResultsAsync(null!, SearchActionCopyFormat.PlainText));
    }

    [Fact]
    public async Task CopyResultsAsync_PlainText_DoesNotCheckLicense()
    {
        // Arrange
        var results = CreateTestResults(1, 1);

        // Act
        // Note: Clipboard not available in test environment, but method should succeed
        var result = await _sut.CopyResultsAsync(results, SearchActionCopyFormat.PlainText);

        // Assert - PlainText format should not trigger license check
        _licenseContextMock.Verify(
            x => x.IsFeatureEnabled(FeatureCode),
            Times.Never);
    }

    [Fact]
    public async Task CopyResultsAsync_CitationFormatted_ChecksLicense()
    {
        // Arrange
        var results = CreateTestResults(1, 1);

        // Act
        await _sut.CopyResultsAsync(results, SearchActionCopyFormat.CitationFormatted);

        // Assert - CitationFormatted format should trigger license check
        _licenseContextMock.Verify(
            x => x.IsFeatureEnabled(FeatureCode),
            Times.Once);
    }

    [Fact]
    public async Task CopyResultsAsync_CitationFormatted_NoLicense_ReturnsFailed()
    {
        // Arrange
        var results = CreateTestResults(1, 1);
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCode))
            .Returns(false);

        // Act
        var result = await _sut.CopyResultsAsync(results, SearchActionCopyFormat.CitationFormatted);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Writer Pro", result.ErrorMessage);
    }

    [Fact]
    public async Task CopyResultsAsync_EmptyResults_ReturnsSuccessWithZeroItems()
    {
        // Arrange
        var emptyResults = new SearchResultSet(
            Groups: Array.Empty<SearchResultGroup>(),
            TotalHits: 0,
            TotalDocuments: 0);

        // Act
        var result = await _sut.CopyResultsAsync(emptyResults, SearchActionCopyFormat.PlainText);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.ItemCount);
    }

    // =========================================================================
    // ExportResultsAsync
    // =========================================================================

    [Fact]
    public async Task ExportResultsAsync_NullResults_ThrowsArgumentNullException()
    {
        // Arrange
        var options = new SearchExportOptions(SearchActionExportFormat.Json);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "results",
            () => _sut.ExportResultsAsync(null!, options));
    }

    [Fact]
    public async Task ExportResultsAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var results = CreateTestResults();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "options",
            () => _sut.ExportResultsAsync(results, null!));
    }

    [Fact]
    public async Task ExportResultsAsync_NoLicense_ReturnsFailed()
    {
        // Arrange
        var results = CreateTestResults();
        var options = new SearchExportOptions(SearchActionExportFormat.Json);
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCode))
            .Returns(false);

        // Act
        var result = await _sut.ExportResultsAsync(results, options);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Writer Pro", result.ErrorMessage);
    }

    [Fact]
    public async Task ExportResultsAsync_NoLicense_PublishesFailedEvent()
    {
        // Arrange
        var results = CreateTestResults();
        var options = new SearchExportOptions(SearchActionExportFormat.Json);
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCode))
            .Returns(false);

        // Act
        await _sut.ExportResultsAsync(results, options);

        // Assert
        _mediatorMock.Verify(
            x => x.Publish(
                It.Is<SearchResultsExportedEvent>(e => !e.Success),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(SearchActionExportFormat.Json, ".json")]
    [InlineData(SearchActionExportFormat.Csv, ".csv")]
    [InlineData(SearchActionExportFormat.Markdown, ".md")]
    [InlineData(SearchActionExportFormat.BibTeX, ".bib")]
    public async Task ExportResultsAsync_AllFormats_ProducesCorrectExtension(
        SearchActionExportFormat format, string expectedExtension)
    {
        // Arrange
        var results = CreateTestResults(1, 1);
        var tempDir = Path.Combine(Path.GetTempPath(), "lexichord-test-" + Guid.NewGuid());
        Directory.CreateDirectory(tempDir);

        try
        {
            var outputPath = Path.Combine(tempDir, $"export{expectedExtension}");
            var options = new SearchExportOptions(format, OutputPath: outputPath);

            // Act
            var result = await _sut.ExportResultsAsync(results, options);

            // Assert
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(outputPath, result.OutputPath);
            Assert.True(result.BytesWritten > 0);
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
    }

    [Fact]
    public async Task ExportResultsAsync_Success_PublishesEvent()
    {
        // Arrange
        var results = CreateTestResults(1, 1);
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.json");
        var options = new SearchExportOptions(SearchActionExportFormat.Json, OutputPath: tempPath);

        try
        {
            // Act
            await _sut.ExportResultsAsync(results, options);

            // Assert
            _mediatorMock.Verify(
                x => x.Publish(
                    It.Is<SearchResultsExportedEvent>(e => e.Success),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    // =========================================================================
    // OpenAllDocumentsAsync
    // =========================================================================

    [Fact]
    public async Task OpenAllDocumentsAsync_NullResults_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            "results",
            () => _sut.OpenAllDocumentsAsync(null!));
    }

    [Fact]
    public async Task OpenAllDocumentsAsync_EmptyResults_ReturnsSuccessWithZeroCounts()
    {
        // Arrange
        var emptyResults = new SearchResultSet(
            Groups: Array.Empty<SearchResultGroup>(),
            TotalHits: 0,
            TotalDocuments: 0);

        // Act
        var result = await _sut.OpenAllDocumentsAsync(emptyResults);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.OpenedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.FailedPaths);
    }

    [Fact]
    public async Task OpenAllDocumentsAsync_DocumentsOpened_ReturnsCorrectCounts()
    {
        // Arrange
        var results = CreateTestResults(3, 1);
        _editorServiceMock
            .Setup(x => x.GetDocumentByPath(It.IsAny<string>()))
            .Returns((IManuscriptViewModel?)null); // Not already open
        _editorServiceMock
            .Setup(x => x.OpenDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(Mock.Of<IManuscriptViewModel>());

        // Act
        var result = await _sut.OpenAllDocumentsAsync(results);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.OpenedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.FailedPaths);
    }

    [Fact]
    public async Task OpenAllDocumentsAsync_AlreadyOpen_SkipsAndCounts()
    {
        // Arrange
        var results = CreateTestResults(2, 1);
        _editorServiceMock
            .Setup(x => x.GetDocumentByPath(It.IsAny<string>()))
            .Returns(Mock.Of<IManuscriptViewModel>()); // Already open

        // Act
        var result = await _sut.OpenAllDocumentsAsync(results);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(0, result.OpenedCount);
        Assert.Equal(2, result.SkippedCount);
        Assert.Empty(result.FailedPaths);
    }

    [Fact]
    public async Task OpenAllDocumentsAsync_SomeFailures_ReturnsPartialResult()
    {
        // Arrange
        var results = CreateTestResults(3, 1);
        var callCount = 0;

        _editorServiceMock
            .Setup(x => x.GetDocumentByPath(It.IsAny<string>()))
            .Returns((IManuscriptViewModel?)null);

        _editorServiceMock
            .Setup(x => x.OpenDocumentAsync(It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 2)
                {
                    throw new FileNotFoundException("File not found");
                }
                return Mock.Of<IManuscriptViewModel>();
            });

        // Act
        var result = await _sut.OpenAllDocumentsAsync(results);

        // Assert
        Assert.True(result.Success); // Still partial success
        Assert.Equal(2, result.OpenedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Single(result.FailedPaths);
        Assert.True(result.HasErrors);
    }
}
