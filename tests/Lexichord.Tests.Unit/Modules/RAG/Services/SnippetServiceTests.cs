// =============================================================================
// File: SnippetServiceTests.cs
// Project: Lexichord.Tests.Unit
// Description: Unit tests for SnippetService (v0.5.6a).
// =============================================================================
// LOGIC: Verifies snippet extraction pipeline:
//   - Constructor null-parameter validation (3 dependencies).
//   - ExtractSnippet centering on query matches.
//   - ExtractSnippet respects max length constraint.
//   - Empty content handling.
//   - Fallback behavior when no matches found.
//   - Multi-snippet extraction returns non-overlapping snippets.
//   - Batch extraction processes all chunks.
// =============================================================================

using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.RAG;
using Lexichord.Modules.RAG.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Services;

/// <summary>
/// Unit tests for <see cref="SnippetService"/>.
/// Verifies constructor validation, snippet extraction logic, and edge cases.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.5.6a")]
public class SnippetServiceTests
{
    private readonly Mock<IQueryAnalyzer> _queryAnalyzerMock;
    private readonly Mock<ISentenceBoundaryDetector> _sentenceDetectorMock;
    private readonly Mock<ILogger<SnippetService>> _loggerMock;

    public SnippetServiceTests()
    {
        _queryAnalyzerMock = new Mock<IQueryAnalyzer>();
        _sentenceDetectorMock = new Mock<ISentenceBoundaryDetector>();
        _loggerMock = new Mock<ILogger<SnippetService>>();

        // LOGIC: Default sentence detector returns input positions.
        _sentenceDetectorMock
            .Setup(x => x.FindSentenceStart(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((_, p) => Math.Max(0, p));
        _sentenceDetectorMock
            .Setup(x => x.FindSentenceEnd(It.IsAny<string>(), It.IsAny<int>()))
            .Returns<string, int>((t, p) => Math.Min(t?.Length ?? 0, p));
    }

    /// <summary>
    /// Creates a <see cref="SnippetService"/> using the test mocks.
    /// </summary>
    private SnippetService CreateService() =>
        new(
            _queryAnalyzerMock.Object,
            _sentenceDetectorMock.Object,
            _loggerMock.Object);

    /// <summary>
    /// Creates a test <see cref="TextChunk"/> with the given content.
    /// </summary>
    private static TextChunk CreateChunk(string content, int index = 0) =>
        new(
            content,
            StartOffset: 0,
            EndOffset: content.Length,
            new ChunkMetadata(Index: index));

    #region Constructor Tests

    [Fact]
    public void Constructor_NullQueryAnalyzer_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnippetService(
            null!,
            _sentenceDetectorMock.Object,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("queryAnalyzer");
    }

    [Fact]
    public void Constructor_NullSentenceDetector_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnippetService(
            _queryAnalyzerMock.Object,
            null!,
            _loggerMock.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("sentenceDetector");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new SnippetService(
            _queryAnalyzerMock.Object,
            _sentenceDetectorMock.Object,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_ValidDependencies_DoesNotThrow()
    {
        // Act & Assert
        var act = () => CreateService();

        act.Should().NotThrow(because: "all dependencies are provided");
    }

    #endregion

    #region ExtractSnippet Tests

    [Fact]
    public void ExtractSnippet_EmptyContent_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();
        var chunk = CreateChunk(string.Empty);

        // Act
        var snippet = service.ExtractSnippet(chunk, "query", SnippetOptions.Default);

        // Assert
        snippet.Should().Be(Snippet.Empty);
    }

    [Fact]
    public void ExtractSnippet_WhitespaceContent_ReturnsEmpty()
    {
        // Arrange
        var service = CreateService();
        var chunk = CreateChunk("   \t\n  ");

        // Act
        var snippet = service.ExtractSnippet(chunk, "query", SnippetOptions.Default);

        // Assert
        snippet.Should().Be(Snippet.Empty);
    }

    [Fact]
    public void ExtractSnippet_CentersOnQueryMatch()
    {
        // Arrange
        var content = "Start text. The authentication flow requires tokens. End text.";
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze("authentication"))
            .Returns(new QueryAnalysis(
                "authentication",
                new[] { "authentication" },
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.5f));

        // Act
        var snippet = service.ExtractSnippet(chunk, "authentication", SnippetOptions.Default);

        // Assert
        snippet.Text.Should().Contain("authentication");
        snippet.HasHighlights.Should().BeTrue();
    }

    [Fact]
    public void ExtractSnippet_RespectsMaxLength()
    {
        // Arrange
        var content = new string('x', 1000);
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze(It.IsAny<string>()))
            .Returns(new QueryAnalysis(
                "",
                Array.Empty<string>(),
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.0f));

        // Act
        var snippet = service.ExtractSnippet(chunk, "query", new SnippetOptions(MaxLength: 200));

        // Assert
        // +3 for "..." truncation marker
        snippet.Text.Length.Should().BeLessOrEqualTo(203);
    }

    [Fact]
    public void ExtractSnippet_NoMatches_ReturnsFallback()
    {
        // Arrange
        var content = "Some content without the search term.";
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze("different"))
            .Returns(new QueryAnalysis(
                "different",
                new[] { "different" },
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.5f));

        // Act
        var snippet = service.ExtractSnippet(chunk, "different", SnippetOptions.Default);

        // Assert
        snippet.HasHighlights.Should().BeFalse();
        snippet.Text.Should().StartWith("Some content");
    }

    [Fact]
    public void ExtractSnippet_HighlightPositionsAreRelativeToSnippet()
    {
        // Arrange
        var content = "The test word appears here.";
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze("test"))
            .Returns(new QueryAnalysis(
                "test",
                new[] { "test" },
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.5f));

        // Act
        var snippet = service.ExtractSnippet(chunk, "test", SnippetOptions.Default);

        // Assert
        snippet.HasHighlights.Should().BeTrue();
        var highlight = snippet.Highlights.First();
        // The highlight position should allow extracting the term from snippet text
        snippet.Text.Substring(highlight.Start, highlight.Length)
            .Should().BeEquivalentTo("test");
    }

    [Fact]
    public void ExtractSnippet_SetsIsTruncatedFlags()
    {
        // Arrange
        var content = new string('a', 500);
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze(It.IsAny<string>()))
            .Returns(new QueryAnalysis(
                "",
                Array.Empty<string>(),
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.0f));

        // Act
        var snippet = service.ExtractSnippet(chunk, "query", new SnippetOptions(MaxLength: 100));

        // Assert
        snippet.IsTruncated.Should().BeTrue();
        snippet.IsTruncatedEnd.Should().BeTrue();
    }

    #endregion

    #region ExtractMultipleSnippets Tests

    [Fact]
    public void ExtractMultipleSnippets_EmptyContent_ReturnsSingleEmpty()
    {
        // Arrange
        var service = CreateService();
        var chunk = CreateChunk(string.Empty);

        // Act
        var snippets = service.ExtractMultipleSnippets(chunk, "query", SnippetOptions.Default);

        // Assert
        snippets.Should().HaveCount(1);
        snippets[0].Should().Be(Snippet.Empty);
    }

    [Fact]
    public void ExtractMultipleSnippets_ReturnsNonOverlapping()
    {
        // Arrange
        var content = "First auth here. " + new string(' ', 200) + " Second auth here.";
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze("auth"))
            .Returns(new QueryAnalysis(
                "auth",
                new[] { "auth" },
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.5f));

        // Act
        var snippets = service.ExtractMultipleSnippets(
            chunk, "auth", new SnippetOptions(MaxLength: 50), maxSnippets: 3);

        // Assert
        // Verify no overlaps between snippets
        for (var i = 0; i < snippets.Count; i++)
        {
            for (var j = i + 1; j < snippets.Count; j++)
            {
                var s1End = snippets[i].StartOffset + snippets[i].Length;
                var s2Start = snippets[j].StartOffset;
                var s2End = snippets[j].StartOffset + snippets[j].Length;
                var s1Start = snippets[i].StartOffset;

                (s1End <= s2Start || s2End <= s1Start).Should().BeTrue(
                    $"Snippets {i} and {j} should not overlap");
            }
        }
    }

    [Fact]
    public void ExtractMultipleSnippets_RespectsMaxSnippets()
    {
        // Arrange
        var content = "First keyword. Second keyword. Third keyword. Fourth keyword.";
        var chunk = CreateChunk(content);
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze("keyword"))
            .Returns(new QueryAnalysis(
                "keyword",
                new[] { "keyword" },
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.5f));

        // Act
        var snippets = service.ExtractMultipleSnippets(
            chunk, "keyword", new SnippetOptions(MaxLength: 20), maxSnippets: 2);

        // Assert
        snippets.Count.Should().BeLessOrEqualTo(2);
    }

    #endregion

    #region ExtractBatch Tests

    [Fact]
    public void ExtractBatch_ProcessesAllChunks()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Content with auth token.", index: 0),
            CreateChunk("Another chunk with auth.", index: 1),
            CreateChunk("Third chunk without match.", index: 2)
        };
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze("auth"))
            .Returns(new QueryAnalysis(
                "auth",
                new[] { "auth" },
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.5f));

        // Act
        var result = service.ExtractBatch(chunks, "auth", SnippetOptions.Default);

        // Assert
        // LOGIC: Batch returns one snippet per chunk with deterministic IDs.
        result.Should().HaveCount(3);
        result.Values.Count(s => s.HasHighlights).Should().Be(2,
            because: "two chunks contain 'auth'");
    }

    [Fact]
    public void ExtractBatch_HandlesEmptyChunks()
    {
        // Arrange
        var chunks = new[]
        {
            CreateChunk("Content", index: 0),
            CreateChunk(string.Empty, index: 1),
            CreateChunk("More content", index: 2)
        };
        var service = CreateService();

        _queryAnalyzerMock
            .Setup(x => x.Analyze(It.IsAny<string>()))
            .Returns(new QueryAnalysis(
                "",
                Array.Empty<string>(),
                Array.Empty<QueryEntity>(),
                QueryIntent.Factual,
                0.0f));

        // Act
        var result = service.ExtractBatch(chunks, "query", SnippetOptions.Default);

        // Assert
        result.Should().HaveCount(3);
        // LOGIC: Empty chunk should produce Snippet.Empty
        result.Values.Count(s => s == Snippet.Empty).Should().Be(1);
    }

    #endregion

    #region ISnippetService Interface Tests

    [Fact]
    public void Service_ImplementsISnippetService()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().BeAssignableTo<ISnippetService>(
            because: "SnippetService implements the ISnippetService interface");
    }

    #endregion
}
