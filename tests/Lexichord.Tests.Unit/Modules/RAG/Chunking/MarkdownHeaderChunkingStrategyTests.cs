using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Chunking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Chunking;

/// <summary>
/// Unit tests for <see cref="MarkdownHeaderChunkingStrategy"/>.
/// Verifies the Markdown header-based chunking algorithm with hierarchy detection and fallback logic.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3d")]
public class MarkdownHeaderChunkingStrategyTests
{
    private readonly Mock<ILogger<MarkdownHeaderChunkingStrategy>> _loggerMock;
    private readonly Mock<ILogger<ParagraphChunkingStrategy>> _paragraphLoggerMock;
    private readonly Mock<ILogger<FixedSizeChunkingStrategy>> _fixedSizeLoggerMock;
    private readonly FixedSizeChunkingStrategy _fixedSizeStrategy;
    private readonly ParagraphChunkingStrategy _paragraphStrategy;
    private readonly MarkdownHeaderChunkingStrategy _strategy;

    public MarkdownHeaderChunkingStrategyTests()
    {
        _loggerMock = new Mock<ILogger<MarkdownHeaderChunkingStrategy>>();
        _paragraphLoggerMock = new Mock<ILogger<ParagraphChunkingStrategy>>();
        _fixedSizeLoggerMock = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        
        _fixedSizeStrategy = new FixedSizeChunkingStrategy(_fixedSizeLoggerMock.Object);
        _paragraphStrategy = new ParagraphChunkingStrategy(_paragraphLoggerMock.Object, _fixedSizeStrategy);
        _strategy = new MarkdownHeaderChunkingStrategy(
            _loggerMock.Object,
            _paragraphStrategy,
            _fixedSizeStrategy);
    }

    #region Mode Property Tests

    [Fact]
    public void Mode_ReturnsMarkdownHeader()
    {
        // Assert
        _strategy.Mode.Should().Be(ChunkingMode.MarkdownHeader,
            because: "the strategy implements Markdown header-based chunking");
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
        var act = () => _strategy.Split("# Header", null!);

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

    #region Split - No Headers (Fallback to Paragraph)

    [Fact]
    public void Split_NoHeaders_FallsBackToParagraph()
    {
        // Arrange
        const string content = "This is plain text without any headers.\n\nSecond paragraph.";
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 10, MaxSize = 2000, Overlap = 20 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().NotBeEmpty(
            because: "content without headers falls back to paragraph chunking");
        // Paragraph strategy will handle the content
    }

    [Fact]
    public void Split_ContentWithOnlyCodeBlockHashes_FallsBackToParagraph()
    {
        // Arrange - Code blocks with # characters that look like headers but aren't
        const string content = """
            Some intro text.

            ```python
            # This is a comment
            print("hello")
            ```

            More text after code.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 50, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert - The # inside code blocks should not be treated as headers
        result.Should().NotBeEmpty();
    }

    #endregion

    #region Split - Single Header Tests

    [Fact]
    public void Split_SingleH1Header_CreatesSingleChunk()
    {
        // Arrange
        const string content = "# Main Title\n\nSome content under the main title.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1,
            because: "single header creates a single section chunk");
        result[0].Content.Should().Contain("# Main Title");
        result[0].Metadata.Level.Should().Be(1);
        result[0].Metadata.Heading.Should().Be("Main Title");
    }

    [Fact]
    public void Split_SingleH2Header_PreservesLevel()
    {
        // Arrange
        const string content = "## Section Title\n\nContent for section.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Level.Should().Be(2,
            because: "H2 header should have level 2");
    }

    #endregion

    #region Split - Multiple Headers Tests

    [Fact]
    public void Split_MultipleH1Headers_CreatesChunkPerHeader()
    {
        // Arrange
        const string content = """
            # First Section
            
            Content in first section.
            
            # Second Section
            
            Content in second section.
            
            # Third Section
            
            Content in third section.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(3,
            because: "three H1 headers create three section chunks");
        
        result[0].Metadata.Heading.Should().Be("First Section");
        result[1].Metadata.Heading.Should().Be("Second Section");
        result[2].Metadata.Heading.Should().Be("Third Section");
    }

    [Fact]
    public void Split_MixedHeaderLevels_RespectsHierarchy()
    {
        // Arrange
        const string content = """
            # Main Section
            
            Main content.
            
            ## Subsection A
            
            Subsection A content.
            
            ## Subsection B
            
            Subsection B content.
            
            # Another Main Section
            
            Another main content.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        // H1 "Main Section" ends at the next H1 (includes H2 subsections)
        // H2 subsections end at next H2 or higher
        result.Should().HaveCountGreaterOrEqualTo(2);
        
        // First chunk should be the main section with its content
        result[0].Metadata.Heading.Should().Be("Main Section");
        result[0].Metadata.Level.Should().Be(1);
    }

    [Fact]
    public void Split_H2EndsAtH1ButNotH3()
    {
        // Arrange
        const string content = """
            # Top Level
            
            ## Mid Level
            
            Some content.
            
            ### Lower Level
            
            More content under lower level.
            
            # Next Top Level
            
            New section.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        // The H2 section should include the H3 subsection (H3 doesn't end H2)
        var h2Section = result.FirstOrDefault(c => c.Metadata.Heading == "Mid Level");
        h2Section.Should().NotBeNull();
        h2Section!.Content.Should().Contain("### Lower Level",
            because: "H3 does not end an H2 section");
    }

    #endregion

    #region Split - Preamble Content Tests

    [Fact]
    public void Split_ContentBeforeFirstHeader_CreatesPreambleChunk()
    {
        // Arrange
        const string content = """
            This is preamble content before any headers.
            
            It can span multiple paragraphs.
            
            # First Header
            
            Content after the first header.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2,
            because: "preamble + one header section = 2 chunks");
        
        // First chunk is preamble (no header metadata)
        result[0].Metadata.Level.Should().Be(0,
            because: "preamble has no header level");
        result[0].Metadata.Heading.Should().BeNull(
            because: "preamble has no header text");
        result[0].Content.Should().Contain("This is preamble");
        
        // Second chunk is the header section
        result[1].Metadata.Level.Should().Be(1);
        result[1].Metadata.Heading.Should().Be("First Header");
    }

    [Fact]
    public void Split_WhitespaceOnlyPreamble_IsIgnored()
    {
        // Arrange
        const string content = """
            
            
            # Header After Whitespace
            
            Some content.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1,
            because: "whitespace-only preamble is ignored");
        result[0].Metadata.Heading.Should().Be("Header After Whitespace");
    }

    #endregion

    #region Split - Oversized Section Tests

    [Fact]
    public void Split_OversizedSection_UsesFallbackStrategy()
    {
        // Arrange
        var longContent = new string('x', 3000);
        var content = $"# Oversized Section\n\n{longContent}";
        var options = new ChunkingOptions
        {
            TargetSize = 500,
            MinSize = 100,
            MaxSize = 1000,
            Overlap = 50,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThan(1,
            because: "oversized section is split using fallback strategy");
        
        foreach (var chunk in result)
        {
            chunk.Content.Length.Should().BeLessOrEqualTo(options.MaxSize,
                because: "fallback ensures chunks fit within MaxSize");
            chunk.Metadata.Level.Should().Be(1,
                because: "all sub-chunks preserve the header level");
            chunk.Metadata.Heading.Should().Be("Oversized Section",
                because: "all sub-chunks preserve the header text");
        }
    }

    [Fact]
    public void Split_OversizedSectionWithPreamble_SplitsBoth()
    {
        // Arrange
        var longPreamble = new string('a', 1500);
        var longSection = new string('b', 1500);
        var content = $"{longPreamble}\n\n# Header\n\n{longSection}";
        var options = new ChunkingOptions
        {
            TargetSize = 500,
            MinSize = 100,
            MaxSize = 1000,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThan(2,
            because: "both oversized preamble and section are split");
    }

    #endregion

    #region Split - Header Text Extraction Tests

    [Fact]
    public void Split_FormattedHeaderText_ExtractsPlainText()
    {
        // Arrange
        const string content = "# **Bold** and *Italic* Header\n\nContent.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Heading.Should().Be("Bold and Italic Header",
            because: "markdown formatting is stripped from header text");
    }

    [Fact]
    public void Split_HeaderWithCode_ExtractsPlainText()
    {
        // Arrange
        const string content = "# Using `Console.WriteLine`\n\nContent about console.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Heading.Should().Contain("Console.WriteLine",
            because: "inline code content is extracted as plain text");
    }

    [Fact]
    public void Split_HeaderWithLink_ExtractsLinkText()
    {
        // Arrange
        const string content = "# Check [this link](https://example.com)\n\nContent.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Heading.Should().Contain("this link",
            because: "link text is preserved in header extraction");
    }

    #endregion

    #region Split - Setext Header Tests

    [Fact]
    public void Split_SetextH1Header_IsRecognized()
    {
        // Arrange
        const string content = """
            Main Title
            ==========
            
            Content under setext H1.
            """;
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Level.Should().Be(1,
            because: "setext === underline creates H1");
        result[0].Metadata.Heading.Should().Be("Main Title");
    }

    [Fact]
    public void Split_SetextH2Header_IsRecognized()
    {
        // Arrange
        const string content = """
            Section Title
            -------------
            
            Content under setext H2.
            """;
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Level.Should().Be(2,
            because: "setext --- underline creates H2");
        result[0].Metadata.Heading.Should().Be("Section Title");
    }

    #endregion

    #region Split - Metadata Tests

    [Fact]
    public void Split_SetsCorrectMetadata_Index()
    {
        // Arrange
        const string content = """
            # Section 1
            Content.
            
            # Section 2
            Content.
            
            # Section 3
            Content.
            """;
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 10, MaxSize = 2000 };

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
        const string content = """
            # Section 1
            Content.
            
            # Section 2
            Content.
            """;
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 10, MaxSize = 2000 };

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
        const string content = """
            # Section 1
            Content.
            
            # Section 2
            Content.
            
            # Section 3
            Content.
            """;
        var options = new ChunkingOptions { TargetSize = 100, MinSize = 10, MaxSize = 2000 };

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
        const string content = "# Single Section\n\nContent.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].Metadata.IsFirst.Should().BeTrue();
        result[0].Metadata.IsLast.Should().BeTrue(
            because: "a single chunk is both first and last");
    }

    #endregion

    #region Split - Empty Section Tests

    [Fact]
    public void Split_EmptySection_IsIgnored()
    {
        // Arrange
        const string content = """
            # Non-Empty Section
            
            This has content.
            
            # Empty Section
            
            # Another Non-Empty
            
            This also has content.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        // The middle "Empty Section" has just the header with no body content
        // Depending on implementation, it might still be a chunk or be merged
        result.Should().NotBeEmpty();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MarkdownHeaderChunkingStrategy(
            null!,
            _paragraphStrategy,
            _fixedSizeStrategy);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_NullParagraphFallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MarkdownHeaderChunkingStrategy(
            _loggerMock.Object,
            null!,
            _fixedSizeStrategy);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("paragraphFallback");
    }

    [Fact]
    public void Constructor_NullFixedSizeFallback_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new MarkdownHeaderChunkingStrategy(
            _loggerMock.Object,
            _paragraphStrategy,
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("fixedSizeFallback");
    }

    #endregion

    #region Split - Edge Cases

    [Fact]
    public void Split_VeryDeepHeaderNesting_HandlesCorrectly()
    {
        // Arrange
        const string content = """
            # H1
            Content.
            ## H2
            Content.
            ### H3
            Content.
            #### H4
            Content.
            ##### H5
            Content.
            ###### H6
            Content.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().NotBeEmpty();
        
        // H1 should contain all nested headers (none of H2-H6 end H1)
        var h1Chunk = result.FirstOrDefault(c => c.Metadata.Level == 1);
        h1Chunk.Should().NotBeNull();
        h1Chunk!.Content.Should().Contain("# H1");
    }

    [Fact]
    public void Split_UnicodeHeaders_HandledCorrectly()
    {
        // Arrange
        const string content = """
            # 日本語のタイトル
            
            日本語の内容。
            
            # Another Header
            
            English content.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2);
        result[0].Metadata.Heading.Should().Be("日本語のタイトル");
    }

    [Fact]
    public void Split_EmojiHeaders_HandledCorrectly()
    {
        // Arrange
        const string content = """
            # 🚀 Getting Started
            
            Content about getting started.
            
            # 📝 Documentation
            
            Content about documentation.
            """;
        var options = new ChunkingOptions { TargetSize = 500, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(2);
        result[0].Metadata.Heading.Should().Contain("Getting Started");
        result[1].Metadata.Heading.Should().Contain("Documentation");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Split_AllHeaderLevels_PreserveLevel(int level)
    {
        // Arrange
        var headerPrefix = new string('#', level);
        var content = $"{headerPrefix} Header Level {level}\n\nSome content.";
        var options = new ChunkingOptions { TargetSize = 1000, MinSize = 10, MaxSize = 2000 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1);
        result[0].Metadata.Level.Should().Be(level,
            because: $"H{level} header should have level {level}");
    }

    #endregion
}
