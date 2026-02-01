using Lexichord.Abstractions.Contracts;
using Lexichord.Modules.RAG.Chunking;
using Microsoft.Extensions.Logging;
using Moq;

namespace Lexichord.Tests.Unit.Modules.RAG.Chunking;

/// <summary>
/// Unit tests for <see cref="FixedSizeChunkingStrategy"/>.
/// Verifies the fixed-size chunking algorithm with various configurations.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3b")]
public class FixedSizeChunkingStrategyTests
{
    private readonly Mock<ILogger<FixedSizeChunkingStrategy>> _loggerMock;
    private readonly FixedSizeChunkingStrategy _strategy;

    public FixedSizeChunkingStrategyTests()
    {
        _loggerMock = new Mock<ILogger<FixedSizeChunkingStrategy>>();
        _strategy = new FixedSizeChunkingStrategy(_loggerMock.Object);
    }

    #region Mode Property Tests

    [Fact]
    public void Mode_ReturnsFixedSize()
    {
        // Assert
        _strategy.Mode.Should().Be(ChunkingMode.FixedSize,
            because: "the strategy implements fixed-size chunking");
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

    #endregion

    #region Split - Single Chunk Tests

    [Fact]
    public void Split_ContentSmallerThanTarget_ReturnsSingleChunk()
    {
        // Arrange
        const string content = "Short content.";
        var options = new ChunkingOptions { TargetSize = 1000, Overlap = 100 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1, because: "content smaller than target is a single chunk");
        result[0].Content.Should().Be("Short content.");
    }

    [Fact]
    public void Split_ContentExactlyTargetSize_ReturnsSingleChunk()
    {
        // Arrange
        var content = new string('a', 1000);
        var options = new ChunkingOptions { TargetSize = 1000, Overlap = 100 };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(1,
            because: "content exactly at target size needs no additional chunks");
    }

    #endregion

    #region Split - Overlap Tests

    [Fact]
    public void Split_CreatesOverlappingChunks()
    {
        // Arrange
        var content = "word1 word2 word3 word4 word5 word6 word7 word8 word9 word10";
        var options = new ChunkingOptions
        {
            TargetSize = 25,
            Overlap = 10,
            RespectWordBoundaries = true
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThan(1,
            because: "content exceeds target size and creates multiple chunks");

        // Verify overlap: content from end of chunk N should appear at start of chunk N+1
        for (var i = 0; i < result.Count - 1; i++)
        {
            var currentEnd = result[i].EndOffset;
            var nextStart = result[i + 1].StartOffset;

            // The next chunk should start before the current chunk ends (overlap)
            nextStart.Should().BeLessThanOrEqualTo(currentEnd,
                because: "chunks should overlap");
        }
    }

    [Fact]
    public void Split_ZeroOverlap_CreatesContiguousChunks()
    {
        // Arrange
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 25,
            Overlap = 0,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCount(4,
            because: "100 chars / 25 target with no overlap = 4 chunks");

        // Verify contiguous: each chunk starts where the previous ended
        for (var i = 1; i < result.Count; i++)
        {
            result[i].StartOffset.Should().Be(result[i - 1].EndOffset,
                because: "with zero overlap, chunks are contiguous");
        }
    }

    #endregion

    #region Split - Word Boundary Tests

    [Fact]
    public void Split_RespectsWordBoundaries()
    {
        // Arrange
        var content = "The quick brown fox jumps over the lazy dog again and again.";
        var options = new ChunkingOptions
        {
            TargetSize = 20,
            Overlap = 0,
            RespectWordBoundaries = true
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        // Each chunk boundary (except start=0 and end=content.Length) should be at a word boundary
        foreach (var chunk in result)
        {
            // Start offset should be 0 or preceded by whitespace
            if (chunk.StartOffset > 0)
            {
                var prevChar = content[chunk.StartOffset - 1];
                char.IsWhiteSpace(prevChar).Should().BeTrue(
                    $"because chunk starting at {chunk.StartOffset} should follow whitespace, " +
                    $"but found '{prevChar}' before it");
            }

            // End offset should be at content.Length or at a non-word character
            if (chunk.EndOffset < content.Length)
            {
                var atEnd = content[chunk.EndOffset - 1];
                // The character at end-1 should be a space (we split after the space)
                // OR the character at end should be a space (we split before the space)
                var atNext = content[chunk.EndOffset];
                (char.IsWhiteSpace(atEnd) || char.IsWhiteSpace(atNext)).Should().BeTrue(
                    $"because chunk ending at {chunk.EndOffset} should end at word boundary");
            }
        }
    }

    [Fact]
    public void Split_DisabledWordBoundaries_SplitsExactly()
    {
        // Arrange
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 0,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        // First chunks should be exactly target size
        result[0].Content.Should().HaveLength(30,
            because: "disabled word boundaries splits at exact target");
        result[1].Content.Should().HaveLength(30);
        result[2].Content.Should().HaveLength(30);
        // Last chunk gets remainder
        result[3].Content.Should().HaveLength(10);
    }

    [Fact]
    public void Split_LongWordExceedingTarget_SplitsMidWord()
    {
        // Arrange
        var longWord = new string('x', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 5,
            RespectWordBoundaries = true
        };

        // Act
        var result = _strategy.Split(longWord, options);

        // Assert
        result.Should().HaveCountGreaterThan(1,
            because: "even with word boundaries enabled, progress must be made");
    }

    #endregion

    #region Split - Whitespace Handling Tests

    [Fact]
    public void Split_TrimsWhitespace_ByDefault()
    {
        // Arrange
        var content = "  Text with leading and trailing spaces  ";
        var options = new ChunkingOptions
        {
            TargetSize = 1000,
            Overlap = 0,
            PreserveWhitespace = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].Content.Should().NotStartWith(" ",
            because: "leading whitespace is trimmed by default");
        result[0].Content.Should().NotEndWith(" ",
            because: "trailing whitespace is trimmed by default");
    }

    [Fact]
    public void Split_PreservesWhitespace_WhenEnabled()
    {
        // Arrange
        var content = "  preserved  ";
        var options = new ChunkingOptions
        {
            TargetSize = 1000,
            Overlap = 0,
            PreserveWhitespace = true
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].Content.Should().Be("  preserved  ",
            because: "whitespace is preserved when enabled");
    }

    [Fact]
    public void Split_WhitespaceOnlyContent_ReturnsEmptyList_WhenNotIncludingEmpty()
    {
        // Arrange
        var content = "     ";
        var options = new ChunkingOptions
        {
            TargetSize = 100,
            Overlap = 0,
            IncludeEmptyChunks = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().BeEmpty(
            because: "whitespace-only content with trimming produces no meaningful chunks");
    }

    #endregion

    #region Split - Metadata Tests

    [Fact]
    public void Split_SetsCorrectMetadata_Index()
    {
        // Arrange
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 0,
            RespectWordBoundaries = false
        };

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
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 0,
            RespectWordBoundaries = false
        };

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
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 0,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].Metadata.IsFirst.Should().BeTrue(because: "first chunk is first");
        result[0].Metadata.IsLast.Should().BeFalse(because: "first chunk is not last");

        result[^1].Metadata.IsFirst.Should().BeFalse(because: "last chunk is not first");
        result[^1].Metadata.IsLast.Should().BeTrue(because: "last chunk is last");
    }

    [Fact]
    public void Split_SingleChunk_IsFirst_And_IsLast()
    {
        // Arrange
        var content = "Short.";
        var options = new ChunkingOptions { TargetSize = 1000, Overlap = 0 };

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
    public void Split_SetsCorrectOffsets()
    {
        // Arrange
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 0,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result[0].StartOffset.Should().Be(0);
        result[0].EndOffset.Should().Be(30);

        result[1].StartOffset.Should().Be(30);
        result[1].EndOffset.Should().Be(60);

        result[2].StartOffset.Should().Be(60);
        result[2].EndOffset.Should().Be(90);

        result[3].StartOffset.Should().Be(90);
        result[3].EndOffset.Should().Be(100);
    }

    [Fact]
    public void Split_WithOverlap_SetsCorrectOffsets()
    {
        // Arrange
        var content = new string('a', 100);
        var options = new ChunkingOptions
        {
            TargetSize = 30,
            Overlap = 10,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        // First chunk: 0-30
        result[0].StartOffset.Should().Be(0);
        result[0].EndOffset.Should().Be(30);

        // Second chunk starts at 30 - 10 = 20
        result[1].StartOffset.Should().Be(20);
    }

    #endregion

    #region Split - Unicode Tests

    [Fact]
    public void Split_UnicodeCharacters_HandledCorrectly()
    {
        // Arrange - using multi-byte Unicode characters
        var content = "日本語テキスト処理のテスト";  // Japanese text
        var options = new ChunkingOptions
        {
            TargetSize = 5,
            Overlap = 0,
            RespectWordBoundaries = false
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThan(1,
            because: "content exceeds target size");

        // Verify character-based splitting, not byte-based
        result[0].Content.Should().HaveLength(5,
            because: "splitting should be character-based");

        // All chunks should reconstruct to original
        var reconstructed = string.Concat(result.Select(c => c.Content));
        reconstructed.Should().Be(content,
            because: "Unicode content should be preserved exactly");
    }

    [Fact]
    public void Split_EmojiCharacters_HandledCorrectly()
    {
        // Arrange - emojis are multi-codepoint characters
        var content = "Hello 👋 World 🌍 Test 🧪";
        var options = new ChunkingOptions
        {
            TargetSize = 10,
            Overlap = 0,
            RespectWordBoundaries = true
        };

        // Act
        var result = _strategy.Split(content, options);

        // Assert
        result.Should().HaveCountGreaterThan(1);

        // Emojis should not be corrupted
        foreach (var chunk in result)
        {
            chunk.Content.Should().NotContain("�",
                because: "emojis should not be corrupted");
        }
    }

    #endregion

    #region Split - Configuration Variations (Theory)

    [Theory]
    [InlineData(500, 50)]
    [InlineData(1000, 100)]
    [InlineData(2000, 200)]
    public void Split_VariousConfigurations_ProducesValidChunks(int targetSize, int overlap)
    {
        // Arrange
        var content = new string('x', targetSize * 3); // 3x target size
        var options = new ChunkingOptions
        {
            TargetSize = targetSize,
            Overlap = overlap,
            RespectWordBoundaries = false
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

    #region Constructor Tests

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new FixedSizeChunkingStrategy(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    #endregion
}
