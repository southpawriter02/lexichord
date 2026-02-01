using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Chunking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Chunking;

/// <summary>
/// Unit tests for <see cref="ParagraphChunkingStrategy"/>.
/// Verifies the paragraph-based chunking algorithm with merging and fallback logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3c")]
public class ParagraphChunkingStrategyTests
{
    private readonly Mock<ILogger<ParagraphChunkingStrategy>> _loggerMock;
    private readonly Mock<ILogger<FixedSizeChunkingStrategy>> _fallbackLoggerMock;
    private readonly FixedSizeChunkingStrategy _fallbackStrategy;
    private readonly ParagraphChunkingStrategy _strategy;

    public ParagraphChunkingStrategyTests()
    {
        _loggerMock = new Mock<ILogger<ParagraphChunkingStrategy>>();
        _fallbackLoggerMock = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        _fallbackStrategy = new FixedSizeChunkingStrategy(_fallbackLoggerMock.Object);
        _strategy = new ParagraphChunkingStrategy(_loggerMock.Object, _fallbackStrategy);
    }

    #region Mode Property Tests

    [Fact]
    public void Mode_ReturnsParagraph()
    {
        // Assert
        _strategy.Mode.Should().Be(ChunkingMode.Paragraph,
            because: "the strategy implements paragraph-based chunking");
    }

    #endregion

    #region Split - Empty/Null Content Tests

    [Fact]
    public void Split_NullContent_ReturnsEmptyList()
    {
        // Act
        var result = _strategy.Split(null!, ChunkingOptions.Default);

        // Assert
        result.Should().BeEmpty(because: "null content produces no chunks");
    }

    [Fact]
    public void Split_EmptyContent_ReturnsEmptyList()
    {
        // Act
        var result = _strategy.Split(string.Empty, ChunkingOptions.Default);

        // Assert
        result.Should().BeEmpty(because: "empty content produces no chunks");
    }

    [Fact]
    public void Split_NullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => _strategy.Split("content", null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Split_WhitespaceOnlyContent_ReturnsEmptyList()
    {
        // Arrange
        var content = "     \n\n    \n\n     ";

        // Act
        var result = _strategy.Split(content, ChunkingOptions.Default);

        // Assert
        result.Should().BeEmpty(
            because: "whitespace-only content produces no meaningful chunks");
    }

    #endregion

    #region Split - Single Paragraph Tests

    [Fact]
    public void Split_SingleParagraph_ReturnsSingleChunk()
    {
        // Arrange
        const string content = "This is a single paragraph without any double newlines.";
        var options = new ChunkingOptions { TargetSize = 1000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1, because: "content without double newlines is a single chunk");
        result[0].Content.Should().Be(content);
    }

    [Fact]
    public void Split_SingleParagraphWithSingleNewlines_ReturnsSingleChunk()
    {
        // Arrange
        const string content = "Line one.\nLine two.\nLine three.";
        var options = new ChunkingOptions { TargetSize = 1000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1,
            because: "single newlines do not split paragraphs");
        result[0].Content.Should().Be(content);
    }

    #endregion

    #region Split - Paragraph Separation Tests

    [Fact]
    public void Split_TwoParagraphs_CreatesTwoChunks_WhenEachExceedsMinSize()
    {
        // Arrange
        var paragraph1 = new string('a', 300);  // Exceeds MinSize (200)
        var paragraph2 = new string('b', 300);  // Exceeds MinSize (200)
        var content = $"{paragraph1}\n\n{paragraph2}";
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 200, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2,
            because: "each paragraph exceeds MinSize so they become separate chunks");
        result[0].Content.Should().Be(paragraph1);
        result[1].Content.Should().Be(paragraph2);
    }

    [Fact]
    public void Split_MultipleParagraphs_SplitsOnDoubleNewlines()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 5).Select(i => new string((char)('a' + i - 1), 250));
        var content = string.Join("\n\n", paragraphs);
        var options = new ChunkingOptions { TargetSize = 300, MinSize = 200, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(5,
            because: "each paragraph exceeds MinSize and is below TargetSize");
    }

    [Fact]
    public void Split_WindowsLineEndings_SplitsCorrectly()
    {
        // Arrange
        var paragraph1 = "Windows paragraph one.";
        var paragraph2 = "Windows paragraph two.";
        var content = $"{paragraph1}\r\n\r\n{paragraph2}";
        var options = new ChunkingOptions { TargetSize = 30, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2,
            because: "Windows line endings (CRLF CRLF) split paragraphs");
        result[0].Content.Should().Be(paragraph1);
        result[1].Content.Should().Be(paragraph2);
    }

    [Fact]
    public void Split_MixedLineEndings_SplitsCorrectly()
    {
        // Arrange
        var content = "Para one.\n\nPara two.\r\n\r\nPara three.";
        var options = new ChunkingOptions { TargetSize = 15, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(3,
            because: "both Unix and Windows line endings should split paragraphs");
    }

    #endregion

    #region Split - Short Paragraph Merging Tests

    [Fact]
    public void Split_ShortParagraphs_MergesUntilTargetSize()
    {
        // Arrange
        var paragraph1 = "Short one.";    // 10 chars
        var paragraph2 = "Short two.";    // 10 chars
        var paragraph3 = "Short three.";  // 12 chars
        var content = $"{paragraph1}\n\n{paragraph2}\n\n{paragraph3}";
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 50, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1,
            because: "all paragraphs merge into single chunk under TargetSize");
        result[0].Content.Should().Contain(paragraph1);
        result[0].Content.Should().Contain(paragraph2);
        result[0].Content.Should().Contain(paragraph3);
    }

    [Fact]
    public void Split_MergedParagraphs_PreserveDoubleNewlineSeparator()
    {
        // Arrange
        var content = "Para one.\n\nPara two.\n\nPara three.";
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 50, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Para one.\n\nPara two.\n\nPara three.",
            because: "merged paragraphs use double newline as separator");
    }

    [Fact]
    public void Split_BufferFlushes_WhenTargetSizeExceeded()
    {
        // Arrange
        var paragraph1 = new string('a', 60);
        var paragraph2 = new string('b', 60);
        var paragraph3 = new string('c', 60);
        var content = $"{paragraph1}\n\n{paragraph2}\n\n{paragraph3}";
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 50, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(3,
            because: "buffer flushes when adding next paragraph would exceed TargetSize");
    }

    #endregion

    #region Split - Long Paragraph Splitting Tests

    [Fact]
    public void Split_LongParagraph_UsesFallbackStrategy()
    {
        // Arrange
        var longParagraph = new string('x', 3000);  // Exceeds MaxSize (2000)
        var options = new ChunkingOptions
        {
            TargetSize = 1000,
            MinSize = 200,
            MaxSize = 2000,
            Overlap = 100,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(longParagraph, options);

        // Assert
        result.Should().HaveCountGreaterThan(1,
            because: "paragraphs exceeding MaxSize are split by fallback strategy");

        foreach (var chunk in result)
        {
            chunk.Content.Should().NotBeEmpty();
            chunk.Content.Length.Should().BeLessOrEqualTo(options.MaxSize,
                because: "fallback splits paragraph into chunks at MaxSize");
        }
    }

    [Fact]
    public void Split_LongParagraphAfterShortParagraphs_FlushesBufferFirst()
    {
        // Arrange
        var shortParagraph = new string('a', 100);
        var longParagraph = new string('x', 3000);
        var content = $"{shortParagraph}\n\n{longParagraph}";
        var options = new ChunkingOptions
        {
            TargetSize = 1000,
            MinSize = 200,
            MaxSize = 2000,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThan(2,
            because: "short paragraph is flushed, then long paragraph is split");
        result[0].Content.Should().Be(shortParagraph,
            because: "buffer with short paragraph is flushed before fallback");
    }

    [Fact]
    public void Split_MixedLengthParagraphs_HandlesCorrectly()
    {
        // Arrange
        var short1 = "Short paragraph one.";
        var long1 = new string('y', 2500);
        var short2 = "Short paragraph two.";
        var content = $"{short1}\n\n{long1}\n\n{short2}";
        var options = new ChunkingOptions
        {
            TargetSize = 500,
            MinSize = 50,
            MaxSize = 2000,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterOrEqualTo(3,
            because: "short, long (split), short creates multiple chunks");
        result[0].Content.Should().Be(short1,
            because: "first short paragraph becomes its own chunk");
        result.Last().Content.Should().Be(short2,
            because: "last short paragraph becomes its own chunk");
    }

    #endregion

    #region Split - Metadata Tests

    [Fact]
    public void Split_SetsCorrectMetadata_Index()
    {
        // Arrange
        var content = "Para 1.\n\nPara 2.\n\nPara 3.";
        var options = new ChunkingOptions { TargetSize = 20, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        for (var i = 0; i < result.Count; i++)
        {
            result[i].Metadata.Index.Should().Be(i,
                because: "chunk index should match position in list");
        }
    }

    [Fact]
    public void Split_SetsCorrectMetadata_TotalChunks()
    {
        // Arrange
        var content = "Para 1.\n\nPara 2.\n\nPara 3.";
        var options = new ChunkingOptions { TargetSize = 20, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        foreach (var chunk in result)
        {
            chunk.Metadata.TotalChunks.Should().Be(result.Count,
                because: "all chunks should know the total count");
        }
    }

    [Fact]
    public void Split_SetsCorrectMetadata_IsFirst_And_IsLast()
    {
        // Arrange
        var content = "Para 1.\n\nPara 2.\n\nPara 3.";
        var options = new ChunkingOptions { TargetSize = 20, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(2, "need multiple chunks for this test");

        result[0].Metadata.IsFirst.Should().BeTrue(because: "first chunk is first");
        result[0].Metadata.IsLast.Should().BeFalse(because: "first chunk is not last");

        result[^1].Metadata.IsFirst.Should().BeFalse(because: "last chunk is not first");
        result[^1].Metadata.IsLast.Should().BeTrue(because: "last chunk is last");
    }

    [Fact]
    public void Split_SingleChunk_IsFirst_And_IsLast()
    {
        // Arrange
        var content = "Single paragraph content.";
        var options = new ChunkingOptions { TargetSize = 1000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].Metadata.IsFirst.Should().BeTrue();
        result[0].Metadata.IsLast.Should().BeTrue(
            because: "a single chunk is both first and last");
    }

    #endregion

    #region Split - Offset Tests

    [Fact]
    public void Split_SetsCorrectOffsets_SimpleParagraphs()
    {
        // Arrange
        var content = "Para 1.\n\nPara 2.";
        var options = new ChunkingOptions { TargetSize = 15, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2);

        // First paragraph: "Para 1." at position 0
        result[0].StartOffset.Should().Be(0);
        result[0].EndOffset.Should().Be(7);

        // Second paragraph: "Para 2." at position 9 (after \n\n)
        result[1].StartOffset.Should().Be(9);
        result[1].EndOffset.Should().Be(16);
    }

    [Fact]
    public void Split_MergedParagraphs_SetsSpanningOffsets()
    {
        // Arrange
        var content = "A.\n\nB.\n\nC.";
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 50, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].StartOffset.Should().Be(0,
            because: "merged chunk starts at first paragraph");
        result[0].EndOffset.Should().Be(content.Length,
            because: "merged chunk ends at last paragraph");
    }

    #endregion

    #region Split - Whitespace Handling Tests

    [Fact]
    public void Split_TrimsWhitespace_ByDefault()
    {
        // Arrange
        var content = "  Paragraph with spaces.  \n\n  Another paragraph.  ";
        var options = new ChunkingOptions
        {
            TargetSize = 50,
            MinSize = 10,
            MaxSize = 2000,
            Overlap = 0,
            PreserveWhitespace = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        foreach (var chunk in result)
        {
            chunk.Content.Should().NotStartWith(" ",
                because: "leading whitespace is trimmed by default");
            chunk.Content.Should().NotEndWith(" ",
                because: "trailing whitespace is trimmed by default");
        }
    }

    [Fact]
    public void Split_PreservesWhitespace_WhenEnabled()
    {
        // Arrange
        var content = "  preserved  ";
        var options = new ChunkingOptions
        {
            TargetSize = 1000,
            PreserveWhitespace = true
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].Content.Should().Be("  preserved  ",
            because: "whitespace is preserved when enabled");
    }

    #endregion

    #region Split - Empty Paragraph Handling Tests

    [Fact]
    public void Split_EmptyParagraphs_AreSkipped_ByDefault()
    {
        // Arrange - Multiple consecutive \n\n should be treated as empty paragraph separators
        var content = "Paragraph 1.\n\n\n\n\n\nParagraph 2.";
        var options = new ChunkingOptions { TargetSize = 20, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2,
            because: "empty segments between separators are skipped");
    }

    [Fact]
    public void Split_LeadingAndTrailingNewlines_AreHandled()
    {
        // Arrange
        var content = "\n\nActual paragraph.\n\n";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Content.Should().Be("Actual paragraph.");
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ParagraphChunkingStrategy(null!, _fallbackStrategy);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullFallbackStrategy_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ParagraphChunkingStrategy(_loggerMock.Object, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fallbackStrategy");
    }

    #endregion

    #region Split - Edge Cases

    [Fact]
    public void Split_VeryLongContent_HandlesWithoutStackOverflow()
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 1000).Select(i => $"Paragraph {i} content.");
        var content = string.Join("\n\n", paragraphs);
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 100, MaxSize = 2000, Overlap = 50 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().NotBeEmpty();

        // Verify all content is covered  
        var totalContentLength = result.Sum(c => c.Content.Length);
        totalContentLength.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Split_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange
        var content = "日本語の段落。\n\n第二段落です。";
        var options = new ChunkingOptions { TargetSize = 10, MinSize = 3, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2,
            because: "Japanese text paragraphs are split correctly");
        result[0].Content.Should().Be("日本語の段落。");
        result[1].Content.Should().Be("第二段落です。");
    }

    [Fact]
    public void Split_EmojiContent_HandledCorrectly()
    {
        // Arrange
        var content = "First paragraph 👋🌍\n\nSecond paragraph 🎉✨";
        var options = new ChunkingOptions { TargetSize = 25, MinSize = 5, MaxSize = 2000, Overlap = 0 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2);
        result[0].Content.Should().Contain("👋");
        result[1].Content.Should().Contain("🎉");
    }

    [Theory]
    [InlineData(500, 100, 1000)]
    [InlineData(1000, 200, 2000)]
    [InlineData(2000, 500, 4000)]
    public void Split_VariousConfigurations_ProducesValidChunks(int targetSize, int minSize, int maxSize)
    {
        // Arrange
        var paragraphs = Enumerable.Range(1, 10).Select(i => new string((char)('a' + (i % 26)), targetSize / 2));
        var content = string.Join("\n\n", paragraphs);
        var options = new ChunkingOptions
        {
            TargetSize = targetSize,
            MinSize = minSize,
            MaxSize = maxSize
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().NotBeEmpty();

        foreach (var chunk in result)
        {
            chunk.Content.Should().NotBeNullOrEmpty();
            chunk.StartOffset.Should().BeGreaterOrEqualTo(0);
            chunk.EndOffset.Should().BeLessOrEqualTo(content.Length);
            chunk.EndOffset.Should().BeGreaterThan(chunk.StartOffset);
        }
    }

    #endregion
}
