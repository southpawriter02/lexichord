using System.Diagnostics;
using FluentAssertions;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.Linting;
using Lexichord.Modules.Style.Services.Linting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Style.Linting;

/// <summary>
/// Unit tests for ProjectLintingService.
/// </summary>
/// <remarks>
/// LOGIC: Verifies project-wide linting with caching, progress reporting,
/// and cancellation support per LCS-DES-026d.
///
/// Version: v0.2.6d
/// </remarks>
public class ProjectLintingServiceTests
{
    private readonly Mock<IStyleEngine> _styleEngineMock;
    private readonly Mock<IEditorService> _editorServiceMock;
    private readonly Mock<ILintingConfiguration> _configMock;
    private readonly Mock<IFileSystemAccess> _fileSystemMock;
    private readonly Mock<ILogger<ProjectLintingService>> _loggerMock;
    private readonly ProjectLintingService _sut;

    public ProjectLintingServiceTests()
    {
        _styleEngineMock = new Mock<IStyleEngine>();
        _editorServiceMock = new Mock<IEditorService>();
        _configMock = new Mock<ILintingConfiguration>();
        _fileSystemMock = new Mock<IFileSystemAccess>();
        _loggerMock = new Mock<ILogger<ProjectLintingService>>();

        // Default configuration
        _configMock.Setup(x => x.TargetExtensions).Returns([".md", ".txt"]);

        _sut = new ProjectLintingService(
            _styleEngineMock.Object,
            _editorServiceMock.Object,
            _configMock.Object,
            _fileSystemMock.Object,
            _loggerMock.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullStyleEngine()
    {
        // Act
        var act = () => new ProjectLintingService(
            null!,
            _editorServiceMock.Object,
            _configMock.Object,
            _fileSystemMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("styleEngine");
    }

    [Fact]
    public void Constructor_ThrowsOnNullEditorService()
    {
        // Act
        var act = () => new ProjectLintingService(
            _styleEngineMock.Object,
            null!,
            _configMock.Object,
            _fileSystemMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("editorService");
    }

    [Fact]
    public void Constructor_ThrowsOnNullConfiguration()
    {
        // Act
        var act = () => new ProjectLintingService(
            _styleEngineMock.Object,
            _editorServiceMock.Object,
            null!,
            _fileSystemMock.Object,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("configuration");
    }

    [Fact]
    public void Constructor_ThrowsOnNullFileSystem()
    {
        // Act
        var act = () => new ProjectLintingService(
            _styleEngineMock.Object,
            _editorServiceMock.Object,
            _configMock.Object,
            null!,
            _loggerMock.Object);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("fileSystem");
    }

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        // Act
        var act = () => new ProjectLintingService(
            _styleEngineMock.Object,
            _editorServiceMock.Object,
            _configMock.Object,
            _fileSystemMock.Object,
            null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    #endregion

    #region LintOpenDocumentsAsync Tests

    [Fact]
    public async Task LintOpenDocumentsAsync_WithNoOpenDocuments_ReturnsEmptyResult()
    {
        // Arrange
        _editorServiceMock.Setup(x => x.GetOpenDocuments())
            .Returns(Array.Empty<IManuscriptViewModel>());

        // Act
        var result = await _sut.LintOpenDocumentsAsync();

        // Assert
        result.TotalDocuments.Should().Be(0);
        result.ProblemCount.Should().Be(0);
        result.ViolationsByDocument.Should().BeEmpty();
    }

    [Fact]
    public async Task LintOpenDocumentsAsync_LintsAllOpenDocuments()
    {
        // Arrange
        var doc1 = CreateMockDocument("doc-1", "Content 1");
        var doc2 = CreateMockDocument("doc-2", "Content 2");
        _editorServiceMock.Setup(x => x.GetOpenDocuments())
            .Returns([doc1.Object, doc2.Object]);

        _styleEngineMock.Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StyleViolation>());

        // Act
        var result = await _sut.LintOpenDocumentsAsync();

        // Assert
        result.TotalDocuments.Should().Be(2);
        result.ViolationsByDocument.Should().ContainKey("doc-1");
        result.ViolationsByDocument.Should().ContainKey("doc-2");
    }

    [Fact]
    public async Task LintOpenDocumentsAsync_AggregatesViolationCount()
    {
        // Arrange
        var doc1 = CreateMockDocument("doc-1", "Content 1");
        var doc2 = CreateMockDocument("doc-2", "Content 2");
        _editorServiceMock.Setup(x => x.GetOpenDocuments())
            .Returns([doc1.Object, doc2.Object]);

        var violations = new List<StyleViolation>
        {
            CreateTestViolation("Test violation")
        };

        _styleEngineMock.Setup(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(violations);

        // Act
        var result = await _sut.LintOpenDocumentsAsync();

        // Assert
        result.ProblemCount.Should().Be(2); // 1 violation per doc × 2 docs
    }

    [Fact]
    public async Task LintOpenDocumentsAsync_RespectsCancellation()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var doc1 = CreateMockDocument("doc-1", "Content 1");
        _editorServiceMock.Setup(x => x.GetOpenDocuments())
            .Returns([doc1.Object]);

        // Act
        var act = async () => await _sut.LintOpenDocumentsAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LintOpenDocumentsAsync_ContinuesOnDocumentError()
    {
        // Arrange
        var doc1 = CreateMockDocument("doc-1", "Content 1");
        var doc2 = CreateMockDocument("doc-2", "Content 2");
        _editorServiceMock.Setup(x => x.GetOpenDocuments())
            .Returns([doc1.Object, doc2.Object]);

        _styleEngineMock.SetupSequence(x => x.AnalyzeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Engine error"))
            .ReturnsAsync(new List<StyleViolation>());

        // Act
        var result = await _sut.LintOpenDocumentsAsync();

        // Assert
        result.TotalDocuments.Should().Be(2);
        result.ViolationsByDocument["doc-1"].Should().BeEmpty(); // Error returns empty
        result.ViolationsByDocument.Should().ContainKey("doc-2");
    }

    #endregion

    #region InvalidateFile Tests

    [Fact]
    public void InvalidateFile_RemovesCachedEntry()
    {
        // Arrange
        var testRule = CreateTestRule();
        var violations = new List<StyleViolation>
        {
            new(testRule, "Test", 0, 10, 1, 0, 1, 10, "test", null, ViolationSeverity.Warning)
        };
        _sut.CacheViolations("/path/to/file.md", violations);
        _sut.CacheCount.Should().Be(1);

        // Act
        _sut.InvalidateFile("/path/to/file.md");

        // Assert
        _sut.CacheCount.Should().Be(0);
    }

    [Fact]
    public void InvalidateFile_SafeForNonCachedFile()
    {
        // Act & Assert - should not throw
        _sut.InvalidateFile("/path/to/nonexistent.md");
    }

    #endregion

    #region InvalidateAll Tests

    [Fact]
    public void InvalidateAll_ClearsEntireCache()
    {
        // Arrange
        var violations = new List<StyleViolation>();
        _sut.CacheViolations("/path/file1.md", violations);
        _sut.CacheViolations("/path/file2.md", violations);
        _sut.CacheViolations("/path/file3.md", violations);
        _sut.CacheCount.Should().Be(3);

        // Act
        _sut.InvalidateAll();

        // Assert
        _sut.CacheCount.Should().Be(0);
    }

    #endregion

    #region Helper Methods

    private Mock<IManuscriptViewModel> CreateMockDocument(string documentId, string content)
    {
        var mock = new Mock<IManuscriptViewModel>();
        mock.Setup(x => x.DocumentId).Returns(documentId);
        mock.Setup(x => x.Content).Returns(content);
        return mock;
    }

    private static StyleViolation CreateTestViolation(string message)
    {
        var rule = CreateTestRule();
        return new StyleViolation(
            rule,
            message,
            0, 10,
            1, 0,
            1, 10,
            "matched",
            null,
            ViolationSeverity.Warning);
    }

    private static StyleRule CreateTestRule() => new(
        Id: "TST001",
        Name: "Test Rule",
        Description: "A test rule",
        Category: RuleCategory.Terminology,
        DefaultSeverity: ViolationSeverity.Warning,
        Pattern: "test",
        PatternType: PatternType.Literal,
        Suggestion: null);

    #endregion
}
