// =============================================================================
// File: CitationServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for CitationService citation creation, formatting,
//              validation, and line number calculation.
// =============================================================================
// LOGIC: Verifies all CitationService functionality:
//   - Constructor null-parameter validation (4 dependencies).
//   - CreateCitation: builds Citation from SearchHit with correct fields.
//   - CreateCitation: falls back to filename when Document.Title is null.
//   - CreateCitation: extracts heading from chunk metadata.
//   - CreateCitation: handles missing source file gracefully (null LineNumber).
//   - CreateCitation: publishes CitationCreatedEvent.
//   - CreateCitation: sets RelativePath when workspace is open.
//   - CreateCitations: batch creation from multiple hits.
//   - CreateCitations: skips null entries with warning.
//   - FormatCitation: Inline format with/without heading.
//   - FormatCitation: Footnote format with/without line number.
//   - FormatCitation: Markdown format with/without line number.
//   - FormatCitation: license gating returns path only for Core users.
//   - FormatCitation: throws on invalid style.
//   - ValidateCitationAsync: returns true for unchanged file.
//   - ValidateCitationAsync: returns false for modified file.
//   - ValidateCitationAsync: returns false for missing file.
//   - CalculateLineNumber: correct line from offset positions.
//   - CalculateLineNumber: returns null for missing file.
//   - CalculateLineNumber: returns null for out-of-bounds offset.
// =============================================================================

using Lexichord.Abstractions.Constants;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Abstractions.Events;
using Lexichord.Modules.RAG.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="CitationService"/>.
/// Verifies citation creation, formatting, validation, and line number calculation.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.2a")]
public class CitationServiceTests : IDisposable
{
    // LOGIC: Mock dependencies for constructor injection.
    private readonly Mock<IWorkspaceService> _workspaceMock;
    private readonly Mock<ILicenseContext> _licenseContextMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly Mock<ILogger<CitationService>> _loggerMock;

    // LOGIC: Shared test data.
    private static readonly Guid TestDocId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid TestProjectId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    // LOGIC: Temp file management for line number calculation tests.
    private readonly List<string> _tempFiles = new();

    public CitationServiceTests()
    {
        _workspaceMock = new Mock<IWorkspaceService>();
        _licenseContextMock = new Mock<ILicenseContext>();
        _mediatorMock = new Mock<IMediator>();
        _loggerMock = new Mock<ILogger<CitationService>>();

        // LOGIC: Default setup — WriterPro license with no workspace open.
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.Citation))
            .Returns(true);
        _workspaceMock
            .Setup(x => x.IsWorkspaceOpen)
            .Returns(false);
    }

    public void Dispose()
    {
        // LOGIC: Clean up temp files created during tests.
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
    }

    // =========================================================================
    // Constructor Validation
    // =========================================================================

    [Fact]
    public void Constructor_NullWorkspace_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationService(
            null!, _licenseContextMock.Object, _mediatorMock.Object, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("workspace", act);
    }

    [Fact]
    public void Constructor_NullLicenseContext_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationService(
            _workspaceMock.Object, null!, _mediatorMock.Object, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("licenseContext", act);
    }

    [Fact]
    public void Constructor_NullMediator_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationService(
            _workspaceMock.Object, _licenseContextMock.Object, null!, _loggerMock.Object);
        Assert.Throws<ArgumentNullException>("mediator", act);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new CitationService(
            _workspaceMock.Object, _licenseContextMock.Object, _mediatorMock.Object, null!);
        Assert.Throws<ArgumentNullException>("logger", act);
    }

    // =========================================================================
    // CreateCitation
    // =========================================================================

    [Fact]
    public void CreateCitation_FromSearchHit_ReturnsCompleteCitation()
    {
        // Arrange
        var hit = CreateSearchHit(
            documentPath: "/docs/test.md",
            documentTitle: "Test Document",
            startOffset: 0);
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Equal("/docs/test.md", citation.DocumentPath);
        Assert.Equal("Test Document", citation.DocumentTitle);
        Assert.Equal(TestDocId, citation.ChunkId);
        Assert.Equal(0, citation.StartOffset);
        Assert.Equal(100, citation.EndOffset);
    }

    [Fact]
    public void CreateCitation_WithNullHit_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.CreateCitation(null!));
    }

    [Fact]
    public void CreateCitation_WithoutTitle_UsesFileName()
    {
        // Arrange
        var hit = CreateSearchHit(
            documentPath: "/docs/my-guide.md",
            documentTitle: null);
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Equal("my-guide.md", citation.DocumentTitle);
        Assert.Equal("my-guide.md", citation.FileName);
    }

    [Fact]
    public void CreateCitation_WithHeading_IncludesHeading()
    {
        // Arrange
        var hit = CreateSearchHit(heading: "Authentication");
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Equal("Authentication", citation.Heading);
        Assert.True(citation.HasHeading);
    }

    [Fact]
    public void CreateCitation_WithoutHeading_HasHeadingIsFalse()
    {
        // Arrange
        var hit = CreateSearchHit(heading: null);
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Null(citation.Heading);
        Assert.False(citation.HasHeading);
    }

    [Fact]
    public void CreateCitation_WithLineNumberFromFile_CalculatesCorrectly()
    {
        // Arrange
        var tempFile = CreateTempFile("Line 1\nLine 2\nLine 3");
        var hit = CreateSearchHit(
            documentPath: tempFile,
            startOffset: 7); // Start of "Line 2"
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.NotNull(citation.LineNumber);
        Assert.Equal(2, citation.LineNumber);
    }

    [Fact]
    public void CreateCitation_MissingFile_LineNumberIsNull()
    {
        // Arrange
        var hit = CreateSearchHit(
            documentPath: "/nonexistent/file.md",
            startOffset: 0);
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Null(citation.LineNumber);
        Assert.False(citation.HasLineNumber);
    }

    [Fact]
    public void CreateCitation_PublishesCitationCreatedEvent()
    {
        // Arrange
        var hit = CreateSearchHit();
        var sut = CreateService();

        // Act
        sut.CreateCitation(hit);

        // Assert
        _mediatorMock.Verify(
            m => m.Publish(It.IsAny<CitationCreatedEvent>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void CreateCitation_WithWorkspaceOpen_SetsRelativePath()
    {
        // Arrange
        _workspaceMock.Setup(x => x.IsWorkspaceOpen).Returns(true);
        _workspaceMock.Setup(x => x.CurrentWorkspace).Returns(
            new WorkspaceInfo("/workspace", "TestWorkspace", DateTimeOffset.UtcNow));

        var hit = CreateSearchHit(documentPath: "/workspace/docs/test.md");
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Equal("docs/test.md", citation.RelativePath);
    }

    [Fact]
    public void CreateCitation_WithWorkspaceClosed_RelativePathIsNull()
    {
        // Arrange
        _workspaceMock.Setup(x => x.IsWorkspaceOpen).Returns(false);

        var hit = CreateSearchHit();
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Null(citation.RelativePath);
    }

    [Fact]
    public void CreateCitation_SetsIndexedAtFromDocument()
    {
        // Arrange
        var indexedAt = new DateTime(2026, 1, 25, 10, 0, 0, DateTimeKind.Utc);
        var hit = CreateSearchHit(indexedAt: indexedAt);
        var sut = CreateService();

        // Act
        var citation = sut.CreateCitation(hit);

        // Assert
        Assert.Equal(indexedAt, citation.IndexedAt);
    }

    // =========================================================================
    // CreateCitations (batch)
    // =========================================================================

    [Fact]
    public void CreateCitations_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => sut.CreateCitations(null!));
    }

    [Fact]
    public void CreateCitations_MultipleHits_ReturnsCorrectCount()
    {
        // Arrange
        var hits = new[]
        {
            CreateSearchHit(documentPath: "/docs/a.md", documentTitle: "A"),
            CreateSearchHit(documentPath: "/docs/b.md", documentTitle: "B"),
            CreateSearchHit(documentPath: "/docs/c.md", documentTitle: "C")
        };
        var sut = CreateService();

        // Act
        var citations = sut.CreateCitations(hits);

        // Assert
        Assert.Equal(3, citations.Count);
        Assert.Equal("A", citations[0].DocumentTitle);
        Assert.Equal("B", citations[1].DocumentTitle);
        Assert.Equal("C", citations[2].DocumentTitle);
    }

    [Fact]
    public void CreateCitations_EmptyInput_ReturnsEmptyList()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var citations = sut.CreateCitations(Array.Empty<SearchHit>());

        // Assert
        Assert.Empty(citations);
    }

    // =========================================================================
    // FormatCitation — Inline Style
    // =========================================================================

    [Fact]
    public void FormatCitation_InlineWithHeading_IncludesSectionSymbol()
    {
        // Arrange
        var citation = CreateCitation(heading: "Authentication");
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Inline);

        // Assert
        Assert.Equal("[test.md, §Authentication]", result);
    }

    [Fact]
    public void FormatCitation_InlineWithoutHeading_OmitsHeading()
    {
        // Arrange
        var citation = CreateCitation(heading: null);
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Inline);

        // Assert
        Assert.Equal("[test.md]", result);
        Assert.DoesNotContain("§", result);
    }

    // =========================================================================
    // FormatCitation — Footnote Style
    // =========================================================================

    [Fact]
    public void FormatCitation_FootnoteWithLineNumber_IncludesLine()
    {
        // Arrange
        var citation = CreateCitation(lineNumber: 42);
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Footnote);

        // Assert
        Assert.Contains(":42", result);
        Assert.StartsWith("[^", result);
        Assert.Contains("]: /docs/test.md:42", result);
    }

    [Fact]
    public void FormatCitation_FootnoteWithoutLineNumber_OmitsLine()
    {
        // Arrange
        var citation = CreateCitation(lineNumber: null);
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Footnote);

        // Assert
        Assert.EndsWith("]: /docs/test.md", result);
    }

    [Fact]
    public void FormatCitation_FootnoteUsesShortChunkId()
    {
        // Arrange
        var chunkId = Guid.Parse("aabbccdd-1122-3344-5566-778899001122");
        var citation = CreateCitation(chunkId: chunkId);
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Footnote);

        // Assert
        // "N" format: "aabbccdd112233445566778899001122" → first 8 chars: "aabbccdd"
        Assert.StartsWith("[^aabbccdd]", result);
    }

    // =========================================================================
    // FormatCitation — Markdown Style
    // =========================================================================

    [Fact]
    public void FormatCitation_MarkdownWithLineNumber_IncludesFragment()
    {
        // Arrange
        var citation = CreateCitation(lineNumber: 42, title: "Test Document");
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Markdown);

        // Assert
        Assert.Equal("[Test Document](file:///docs/test.md#L42)", result);
    }

    [Fact]
    public void FormatCitation_MarkdownWithoutLineNumber_OmitsFragment()
    {
        // Arrange
        var citation = CreateCitation(lineNumber: null, title: "Test Document");
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Markdown);

        // Assert
        Assert.Equal("[Test Document](file:///docs/test.md)", result);
    }

    [Fact]
    public void FormatCitation_MarkdownWithSpacesInPath_PercentEncodes()
    {
        // Arrange
        var citation = CreateCitation(
            documentPath: "/docs/my guide/test file.md",
            title: "Test");
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Markdown);

        // Assert
        Assert.Contains("/docs/my%20guide/test%20file.md", result);
        Assert.DoesNotContain(" ", result.Split(']')[1]); // URL portion has no spaces
    }

    // =========================================================================
    // FormatCitation — License Gating
    // =========================================================================

    [Fact]
    public void FormatCitation_CoreUser_ReturnsDocumentPathOnly()
    {
        // Arrange
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.Citation))
            .Returns(false);

        var citation = CreateCitation(heading: "Auth", lineNumber: 42);
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Inline);

        // Assert
        Assert.Equal("/docs/test.md", result);
    }

    [Fact]
    public void FormatCitation_WriterProUser_ReturnsFormattedCitation()
    {
        // Arrange
        _licenseContextMock
            .Setup(x => x.IsFeatureEnabled(FeatureCodes.Citation))
            .Returns(true);

        var citation = CreateCitation(heading: "Auth");
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, CitationStyle.Inline);

        // Assert
        Assert.Equal("[test.md, §Auth]", result);
    }

    // =========================================================================
    // FormatCitation — Error Cases
    // =========================================================================

    [Fact]
    public void FormatCitation_NullCitation_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            sut.FormatCitation(null!, CitationStyle.Inline));
    }

    [Fact]
    public void FormatCitation_InvalidStyle_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var citation = CreateCitation();
        var sut = CreateService();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            sut.FormatCitation(citation, (CitationStyle)99));
    }

    // =========================================================================
    // FormatCitation — All Styles (Theory)
    // =========================================================================

    [Theory]
    [InlineData(CitationStyle.Inline, "[test.md, §Intro]")]
    [InlineData(CitationStyle.Footnote, "[^")]
    [InlineData(CitationStyle.Markdown, "[Test Document](file://")]
    public void FormatCitation_WithStyle_ContainsExpectedPattern(
        CitationStyle style, string expectedPattern)
    {
        // Arrange
        var citation = CreateCitation(
            heading: "Intro",
            title: "Test Document",
            lineNumber: 10);
        var sut = CreateService();

        // Act
        var result = sut.FormatCitation(citation, style);

        // Assert
        Assert.Contains(expectedPattern, result);
    }

    // =========================================================================
    // ValidateCitationAsync
    // =========================================================================

    [Fact]
    public async Task ValidateCitationAsync_FileUnchanged_ReturnsTrue()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        // Set the file's last write time to before IndexedAt
        File.SetLastWriteTimeUtc(tempFile, DateTime.UtcNow.AddHours(-2));
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-1));
        var sut = CreateService();

        // Act
        var result = await sut.ValidateCitationAsync(citation);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ValidateCitationAsync_FileModified_ReturnsFalse()
    {
        // Arrange
        var tempFile = CreateTempFile("test content");
        // File was written NOW, but IndexedAt was 2 hours ago
        var citation = CreateCitation(
            documentPath: tempFile,
            indexedAt: DateTime.UtcNow.AddHours(-2));
        var sut = CreateService();

        // Act
        var result = await sut.ValidateCitationAsync(citation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCitationAsync_FileMissing_ReturnsFalse()
    {
        // Arrange
        var citation = CreateCitation(documentPath: "/nonexistent/file.md");
        var sut = CreateService();

        // Act
        var result = await sut.ValidateCitationAsync(citation);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ValidateCitationAsync_NullCitation_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            sut.ValidateCitationAsync(null!));
    }

    // =========================================================================
    // CalculateLineNumber (internal)
    // =========================================================================

    [Theory]
    [InlineData("Line 1\nLine 2\nLine 3", 0, 1)]   // Start of file → line 1
    [InlineData("Line 1\nLine 2\nLine 3", 7, 2)]   // Start of "Line 2" → line 2
    [InlineData("Line 1\nLine 2\nLine 3", 14, 3)]  // Start of "Line 3" → line 3
    [InlineData("a\nb\nc\nd", 6, 4)]                // Start of "d" → line 4
    public void CalculateLineNumber_ReturnsCorrectLine(
        string content, int offset, int expectedLine)
    {
        // Arrange
        var tempFile = CreateTempFile(content);
        var sut = CreateService();

        // Act
        var result = sut.CalculateLineNumber(tempFile, offset);

        // Assert
        Assert.Equal(expectedLine, result);
    }

    [Fact]
    public void CalculateLineNumber_MissingFile_ReturnsNull()
    {
        // Arrange
        var sut = CreateService();

        // Act
        var result = sut.CalculateLineNumber("/nonexistent/file.md", 0);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateLineNumber_OffsetExceedsLength_ReturnsNull()
    {
        // Arrange
        var tempFile = CreateTempFile("short");
        var sut = CreateService();

        // Act
        var result = sut.CalculateLineNumber(tempFile, 9999);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CalculateLineNumber_OffsetZero_ReturnsLineOne()
    {
        // Arrange
        var tempFile = CreateTempFile("Hello World");
        var sut = CreateService();

        // Act
        var result = sut.CalculateLineNumber(tempFile, 0);

        // Assert
        Assert.Equal(1, result);
    }

    // =========================================================================
    // Citation Record Tests
    // =========================================================================

    [Fact]
    public void Citation_FileName_ReturnsCorrectFileName()
    {
        // Act
        var citation = CreateCitation(documentPath: "/docs/api/auth-guide.md");

        // Assert
        Assert.Equal("auth-guide.md", citation.FileName);
    }

    [Fact]
    public void Citation_HasHeading_ReturnsTrueWhenHeadingPresent()
    {
        // Act
        var citation = CreateCitation(heading: "Authentication");

        // Assert
        Assert.True(citation.HasHeading);
    }

    [Fact]
    public void Citation_HasHeading_ReturnsFalseWhenNull()
    {
        // Act
        var citation = CreateCitation(heading: null);

        // Assert
        Assert.False(citation.HasHeading);
    }

    [Fact]
    public void Citation_HasHeading_ReturnsFalseWhenEmpty()
    {
        // Act
        var citation = CreateCitation(heading: "");

        // Assert
        Assert.False(citation.HasHeading);
    }

    [Fact]
    public void Citation_HasLineNumber_ReturnsTrueWhenPositive()
    {
        // Act
        var citation = CreateCitation(lineNumber: 42);

        // Assert
        Assert.True(citation.HasLineNumber);
    }

    [Fact]
    public void Citation_HasLineNumber_ReturnsFalseWhenNull()
    {
        // Act
        var citation = CreateCitation(lineNumber: null);

        // Assert
        Assert.False(citation.HasLineNumber);
    }

    [Fact]
    public void Citation_HasLineNumber_ReturnsFalseWhenZero()
    {
        // Act
        var citation = CreateCitation(lineNumber: 0);

        // Assert
        Assert.False(citation.HasLineNumber);
    }

    // =========================================================================
    // CitationStyle Enum Tests
    // =========================================================================

    [Theory]
    [InlineData(CitationStyle.Inline, 0)]
    [InlineData(CitationStyle.Footnote, 1)]
    [InlineData(CitationStyle.Markdown, 2)]
    public void CitationStyle_HasCorrectValues(CitationStyle style, int expectedValue)
    {
        Assert.Equal(expectedValue, (int)style);
    }

    [Fact]
    public void CitationStyle_HasThreeValues()
    {
        var values = Enum.GetValues<CitationStyle>();
        Assert.Equal(3, values.Length);
    }

    // =========================================================================
    // CitationCreatedEvent Tests
    // =========================================================================

    [Fact]
    public void CitationCreatedEvent_ContainsCitation()
    {
        // Arrange
        var citation = CreateCitation();
        var timestamp = DateTime.UtcNow;

        // Act
        var evt = new CitationCreatedEvent(citation, timestamp);

        // Assert
        Assert.Same(citation, evt.Citation);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    /// <summary>
    /// Creates a configured CitationService instance with mocked dependencies.
    /// </summary>
    private CitationService CreateService()
    {
        return new CitationService(
            _workspaceMock.Object,
            _licenseContextMock.Object,
            _mediatorMock.Object,
            _loggerMock.Object);
    }

    /// <summary>
    /// Creates a SearchHit with configurable test data.
    /// </summary>
    private SearchHit CreateSearchHit(
        string documentPath = "/docs/test.md",
        string? documentTitle = "Test Document",
        Guid? chunkId = null,
        int startOffset = 0,
        int endOffset = 100,
        string? heading = null,
        DateTime? indexedAt = null)
    {
        var document = new Document(
            Id: TestDocId,
            ProjectId: TestProjectId,
            FilePath: documentPath,
            Title: documentTitle!,
            Hash: "abc123",
            Status: DocumentStatus.Indexed,
            IndexedAt: indexedAt ?? new DateTime(2026, 1, 25, 10, 0, 0, DateTimeKind.Utc),
            FailureReason: null);

        var metadata = new ChunkMetadata(
            Index: 0,
            Heading: heading,
            Level: 0)
        {
            TotalChunks = 1
        };

        var chunk = new TextChunk(
            Content: "Test content for citation unit tests.",
            StartOffset: startOffset,
            EndOffset: endOffset,
            Metadata: metadata);

        return new SearchHit
        {
            Chunk = chunk,
            Document = document,
            Score = 0.85f
        };
    }

    /// <summary>
    /// Creates a Citation with configurable test data.
    /// </summary>
    private static Citation CreateCitation(
        Guid? chunkId = null,
        string documentPath = "/docs/test.md",
        string title = "Test Document",
        string? heading = null,
        int? lineNumber = 10,
        DateTime? indexedAt = null)
    {
        return new Citation(
            ChunkId: chunkId ?? TestDocId,
            DocumentPath: documentPath,
            DocumentTitle: title,
            StartOffset: 0,
            EndOffset: 100,
            Heading: heading,
            LineNumber: lineNumber,
            IndexedAt: indexedAt ?? new DateTime(2026, 1, 25, 10, 0, 0, DateTimeKind.Utc));
    }

    /// <summary>
    /// Creates a temporary file with the specified content and tracks it for cleanup.
    /// </summary>
    private string CreateTempFile(string content)
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }
}
