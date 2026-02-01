using Lexichord.Abstractions.Contracts;

namespace Lexichord.Tests.Unit.Abstractions.Chunking;

/// <summary>
/// Unit tests for the chunking abstraction types defined in v0.4.3a:
/// <see cref="ChunkingMode"/>, <see cref="IChunkingStrategy"/>,
/// <see cref="TextChunk"/>, <see cref="ChunkMetadata"/>,
/// <see cref="ChunkingOptions"/>, and <see cref="ChunkingPresets"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("Feature", "v0.4.3a")]
public class ChunkingAbstractionsTests
{
    #region TextChunk Tests

    [Fact]
    public void TextChunk_Length_CalculatedCorrectly()
    {
        // Arrange
        var chunk = new TextChunk("Hello", 10, 15, new ChunkMetadata(0));

        // Assert
        chunk.Length.Should().Be(5, because: "Length is EndOffset (15) minus StartOffset (10)");
    }

    [Fact]
    public void TextChunk_Preview_TruncatesLongContent()
    {
        // Arrange
        var longContent = new string('a', 200);
        var chunk = new TextChunk(longContent, 0, 200, new ChunkMetadata(0));

        // Assert
        chunk.Preview.Should().HaveLength(103, because: "preview truncates to 100 chars plus '...'");
        chunk.Preview.Should().EndWith("...", because: "truncated previews end with ellipsis");
    }

    [Fact]
    public void TextChunk_Preview_ReturnsFullContentWhenShort()
    {
        // Arrange
        var content = "Short text.";
        var chunk = new TextChunk(content, 0, content.Length, new ChunkMetadata(0));

        // Assert
        chunk.Preview.Should().Be(content, because: "content under 100 chars is returned in full");
    }

    [Fact]
    public void TextChunk_Preview_ReturnsFullContentAtExactly100Chars()
    {
        // Arrange
        var content = new string('b', 100);
        var chunk = new TextChunk(content, 0, 100, new ChunkMetadata(0));

        // Assert
        chunk.Preview.Should().Be(content, because: "content at exactly 100 chars is not truncated");
        chunk.Preview.Should().HaveLength(100);
    }

    [Fact]
    public void TextChunk_HasContent_TrueForNonWhitespace()
    {
        // Arrange
        var chunk = new TextChunk("Hello world", 0, 11, new ChunkMetadata(0));

        // Assert
        chunk.HasContent.Should().BeTrue(because: "non-whitespace content is meaningful");
    }

    [Fact]
    public void TextChunk_HasContent_FalseForWhitespaceOnly()
    {
        // Arrange
        var chunk = new TextChunk("   \t\n  ", 0, 7, new ChunkMetadata(0));

        // Assert
        chunk.HasContent.Should().BeFalse(because: "whitespace-only content is not meaningful");
    }

    [Fact]
    public void TextChunk_HasContent_FalseForEmptyString()
    {
        // Arrange
        var chunk = new TextChunk("", 0, 0, new ChunkMetadata(0));

        // Assert
        chunk.HasContent.Should().BeFalse(because: "empty string has no meaningful content");
    }

    [Fact]
    public void TextChunk_WithSameValues_AreEqual()
    {
        // Arrange
        var metadata = new ChunkMetadata(0);
        var chunk1 = new TextChunk("Hello", 0, 5, metadata);
        var chunk2 = new TextChunk("Hello", 0, 5, metadata);

        // Assert
        chunk1.Should().Be(chunk2, because: "records with identical values should be equal");
    }

    [Fact]
    public void TextChunk_WithDifferentContent_AreNotEqual()
    {
        // Arrange
        var metadata = new ChunkMetadata(0);
        var chunk1 = new TextChunk("Hello", 0, 5, metadata);
        var chunk2 = new TextChunk("World", 0, 5, metadata);

        // Assert
        chunk1.Should().NotBe(chunk2, because: "records with different Content should not be equal");
    }

    [Fact]
    public void TextChunk_SupportsWithExpression()
    {
        // Arrange
        var original = new TextChunk("Hello", 0, 5, new ChunkMetadata(0));

        // Act
        var updated = original with { Content = "World" };

        // Assert
        updated.Content.Should().Be("World", because: "with-expression updates the specified property");
        updated.StartOffset.Should().Be(0, because: "unchanged properties are preserved");
        updated.EndOffset.Should().Be(5, because: "unchanged properties are preserved");
    }

    #endregion

    #region ChunkMetadata Tests

    [Fact]
    public void ChunkMetadata_IsFirst_TrueForIndexZero()
    {
        // Arrange
        var metadata = new ChunkMetadata(0) { TotalChunks = 5 };

        // Assert
        metadata.IsFirst.Should().BeTrue(because: "index 0 is always the first chunk");
        metadata.IsLast.Should().BeFalse(because: "index 0 is not the last of 5 chunks");
    }

    [Fact]
    public void ChunkMetadata_IsLast_TrueForLastIndex()
    {
        // Arrange
        var metadata = new ChunkMetadata(4) { TotalChunks = 5 };

        // Assert
        metadata.IsFirst.Should().BeFalse(because: "index 4 is not the first chunk");
        metadata.IsLast.Should().BeTrue(because: "index 4 is the last of 5 chunks (0-indexed)");
    }

    [Fact]
    public void ChunkMetadata_RelativePosition_CalculatedCorrectly()
    {
        // Arrange
        var metadata = new ChunkMetadata(2) { TotalChunks = 4 };

        // Assert
        metadata.RelativePosition.Should().Be(0.5, because: "index 2 of 4 chunks is at 50%");
    }

    [Fact]
    public void ChunkMetadata_RelativePosition_ZeroWhenTotalChunksZero()
    {
        // Arrange
        var metadata = new ChunkMetadata(0);

        // Assert
        metadata.RelativePosition.Should().Be(0.0,
            because: "relative position defaults to 0.0 when TotalChunks is not set");
    }

    [Fact]
    public void ChunkMetadata_IsLast_FalseWhenTotalChunksZero()
    {
        // Arrange
        var metadata = new ChunkMetadata(0);

        // Assert
        metadata.IsLast.Should().BeFalse(
            because: "IsLast requires TotalChunks > 0 to be true");
    }

    [Fact]
    public void ChunkMetadata_HasHeading_TrueWhenHeadingSet()
    {
        // Arrange
        var metadata = new ChunkMetadata(0, "Introduction", 1);

        // Assert
        metadata.HasHeading.Should().BeTrue(because: "a non-empty heading is present");
        metadata.Heading.Should().Be("Introduction");
        metadata.Level.Should().Be(1);
    }

    [Fact]
    public void ChunkMetadata_HasHeading_FalseWhenNull()
    {
        // Arrange
        var metadata = new ChunkMetadata(0);

        // Assert
        metadata.HasHeading.Should().BeFalse(because: "Heading defaults to null");
        metadata.Heading.Should().BeNull();
    }

    [Fact]
    public void ChunkMetadata_HasHeading_FalseWhenEmpty()
    {
        // Arrange
        var metadata = new ChunkMetadata(0, "", 0);

        // Assert
        metadata.HasHeading.Should().BeFalse(because: "empty string is treated as no heading");
    }

    [Fact]
    public void ChunkMetadata_DefaultValues_AreCorrect()
    {
        // Arrange
        var metadata = new ChunkMetadata(3);

        // Assert
        metadata.Index.Should().Be(3);
        metadata.Heading.Should().BeNull(because: "Heading defaults to null");
        metadata.Level.Should().Be(0, because: "Level defaults to 0");
        metadata.TotalChunks.Should().Be(0, because: "TotalChunks defaults to 0");
    }

    [Fact]
    public void ChunkMetadata_WithSameValues_AreEqual()
    {
        // Arrange
        var meta1 = new ChunkMetadata(0, "Test", 1) { TotalChunks = 5 };
        var meta2 = new ChunkMetadata(0, "Test", 1) { TotalChunks = 5 };

        // Assert
        meta1.Should().Be(meta2, because: "records with identical values should be equal");
    }

    [Fact]
    public void ChunkMetadata_SupportsWithExpression()
    {
        // Arrange
        var original = new ChunkMetadata(0, "Intro", 1) { TotalChunks = 10 };

        // Act
        var updated = original with { Index = 5 };

        // Assert
        updated.Index.Should().Be(5, because: "with-expression updates Index");
        updated.Heading.Should().Be("Intro", because: "unchanged properties are preserved");
        updated.TotalChunks.Should().Be(10, because: "TotalChunks is preserved");
    }

    [Fact]
    public void ChunkMetadata_RelativePosition_FirstChunk()
    {
        // Arrange
        var metadata = new ChunkMetadata(0) { TotalChunks = 10 };

        // Assert
        metadata.RelativePosition.Should().Be(0.0,
            because: "index 0 of 10 is at position 0.0");
    }

    [Fact]
    public void ChunkMetadata_IsFirst_And_IsLast_ForSingleChunk()
    {
        // Arrange
        var metadata = new ChunkMetadata(0) { TotalChunks = 1 };

        // Assert
        metadata.IsFirst.Should().BeTrue(because: "the only chunk is both first and last");
        metadata.IsLast.Should().BeTrue(because: "the only chunk is both first and last");
    }

    #endregion

    #region ChunkingOptions Tests

    [Fact]
    public void ChunkingOptions_Default_IsValid()
    {
        // Act & Assert
        ChunkingOptions.Default.Invoking(o => o.Validate())
            .Should().NotThrow(because: "default options should always be valid");
    }

    [Fact]
    public void ChunkingOptions_Default_HasExpectedValues()
    {
        // Arrange
        var options = ChunkingOptions.Default;

        // Assert
        options.TargetSize.Should().Be(1000, because: "default target size is 1000 characters");
        options.Overlap.Should().Be(100, because: "default overlap is 100 characters");
        options.MinSize.Should().Be(200, because: "default minimum size is 200 characters");
        options.MaxSize.Should().Be(2000, because: "default maximum size is 2000 characters");
        options.RespectWordBoundaries.Should().BeTrue(because: "word boundaries are respected by default");
        options.PreserveWhitespace.Should().BeFalse(because: "whitespace is trimmed by default");
        options.IncludeEmptyChunks.Should().BeFalse(because: "empty chunks are filtered by default");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForInvalidTargetSize()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = 0 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*TargetSize*");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForNegativeTargetSize()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = -1 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*TargetSize*");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForNegativeOverlap()
    {
        // Arrange
        var options = new ChunkingOptions { Overlap = -1 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*Overlap*");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForOverlapExceedingTarget()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = 100, Overlap = 150 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*Overlap*");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForOverlapEqualToTarget()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = 100, Overlap = 100 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*Overlap*",
                because: "overlap equal to target size would cause infinite loops");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForNegativeMinSize()
    {
        // Arrange
        var options = new ChunkingOptions { MinSize = -1 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*MinSize*");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForMaxSizeLessThanOrEqualToMinSize()
    {
        // Arrange
        var options = new ChunkingOptions { MinSize = 500, MaxSize = 500 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*MaxSize*",
                because: "MaxSize must be strictly greater than MinSize");
    }

    [Fact]
    public void ChunkingOptions_Validate_ThrowsForTargetSizeExceedingMaxSize()
    {
        // Arrange
        var options = new ChunkingOptions { TargetSize = 3000, MaxSize = 2000 };

        // Act & Assert
        options.Invoking(o => o.Validate())
            .Should().Throw<ArgumentException>()
            .WithMessage("*TargetSize*");
    }

    [Fact]
    public void ChunkingOptions_SupportsWithExpression()
    {
        // Arrange
        var original = ChunkingOptions.Default;

        // Act
        var updated = original with { TargetSize = 500 };

        // Assert
        updated.TargetSize.Should().Be(500, because: "with-expression updates TargetSize");
        updated.Overlap.Should().Be(100, because: "unchanged properties are preserved");
        updated.MaxSize.Should().Be(2000, because: "unchanged properties are preserved");
    }

    [Fact]
    public void ChunkingOptions_WithSameValues_AreEqual()
    {
        // Arrange
        var options1 = new ChunkingOptions { TargetSize = 500, Overlap = 50 };
        var options2 = new ChunkingOptions { TargetSize = 500, Overlap = 50 };

        // Assert
        options1.Should().Be(options2, because: "records with identical values should be equal");
    }

    [Fact]
    public void ChunkingOptions_EqualRecords_HaveEqualHashCodes()
    {
        // Arrange
        var options1 = new ChunkingOptions { TargetSize = 500, Overlap = 50 };
        var options2 = new ChunkingOptions { TargetSize = 500, Overlap = 50 };

        // Assert
        options1.GetHashCode().Should().Be(options2.GetHashCode(),
            because: "equal records must produce equal hash codes");
    }

    #endregion

    #region ChunkingMode Tests

    [Theory]
    [InlineData(ChunkingMode.FixedSize)]
    [InlineData(ChunkingMode.Paragraph)]
    [InlineData(ChunkingMode.MarkdownHeader)]
    [InlineData(ChunkingMode.Semantic)]
    public void ChunkingMode_AllValuesAreDefined(ChunkingMode mode)
    {
        // Assert
        Enum.IsDefined(typeof(ChunkingMode), mode).Should().BeTrue(
            because: $"{mode} should be a valid ChunkingMode value");
    }

    [Fact]
    public void ChunkingMode_HasCorrectNumberOfValues()
    {
        // Arrange
        var values = Enum.GetValues<ChunkingMode>();

        // Assert
        values.Should().HaveCount(4,
            because: "ChunkingMode defines FixedSize, Paragraph, MarkdownHeader, and Semantic");
    }

    [Theory]
    [InlineData(ChunkingMode.FixedSize, 0)]
    [InlineData(ChunkingMode.Paragraph, 1)]
    [InlineData(ChunkingMode.MarkdownHeader, 2)]
    [InlineData(ChunkingMode.Semantic, 3)]
    public void ChunkingMode_HasCorrectIntegerValues(ChunkingMode mode, int expectedValue)
    {
        // Assert
        ((int)mode).Should().Be(expectedValue,
            because: $"{mode} should have integer value {expectedValue}");
    }

    #endregion

    #region ChunkingPresets Tests

    [Fact]
    public void ChunkingPresets_AllPresetsAreValid()
    {
        // Act & Assert
        ChunkingPresets.HighPrecision.Invoking(o => o.Validate()).Should().NotThrow();
        ChunkingPresets.Balanced.Invoking(o => o.Validate()).Should().NotThrow();
        ChunkingPresets.HighContext.Invoking(o => o.Validate()).Should().NotThrow();
        ChunkingPresets.Code.Invoking(o => o.Validate()).Should().NotThrow();
    }

    [Fact]
    public void ChunkingPresets_HighPrecision_HasExpectedValues()
    {
        // Arrange
        var preset = ChunkingPresets.HighPrecision;

        // Assert
        preset.TargetSize.Should().Be(500, because: "high precision uses small chunks");
        preset.Overlap.Should().Be(50);
        preset.MinSize.Should().Be(100);
        preset.MaxSize.Should().Be(1000);
    }

    [Fact]
    public void ChunkingPresets_Balanced_IsSameAsDefault()
    {
        // Assert
        ChunkingPresets.Balanced.Should().Be(ChunkingOptions.Default,
            because: "Balanced preset delegates to ChunkingOptions.Default");
    }

    [Fact]
    public void ChunkingPresets_HighContext_HasExpectedValues()
    {
        // Arrange
        var preset = ChunkingPresets.HighContext;

        // Assert
        preset.TargetSize.Should().Be(2000, because: "high context uses large chunks");
        preset.Overlap.Should().Be(200);
        preset.MinSize.Should().Be(500);
        preset.MaxSize.Should().Be(4000);
    }

    [Fact]
    public void ChunkingPresets_Code_DisablesWordBoundariesAndPreservesWhitespace()
    {
        // Arrange
        var preset = ChunkingPresets.Code;

        // Assert
        preset.RespectWordBoundaries.Should().BeFalse(
            because: "code chunks should not adjust boundaries at word edges");
        preset.PreserveWhitespace.Should().BeTrue(
            because: "code indentation and formatting should be preserved");
        preset.TargetSize.Should().Be(1500);
        preset.Overlap.Should().Be(50);
        preset.MinSize.Should().Be(200);
        preset.MaxSize.Should().Be(3000);
    }

    #endregion

    #region IChunkingStrategy Contract Tests

    [Fact]
    public void IChunkingStrategy_CanBeMocked()
    {
        // Arrange
        var mock = new Mock<IChunkingStrategy>();

        // Assert
        mock.Object.Should().NotBeNull();
        mock.Object.Should().BeAssignableTo<IChunkingStrategy>(
            because: "the interface should be mockable for unit testing");
    }

    [Fact]
    public void IChunkingStrategy_Mock_ReturnsConfiguredMode()
    {
        // Arrange
        var mock = new Mock<IChunkingStrategy>();
        mock.Setup(s => s.Mode).Returns(ChunkingMode.FixedSize);

        // Act
        var mode = mock.Object.Mode;

        // Assert
        mode.Should().Be(ChunkingMode.FixedSize,
            because: "mock should return the configured mode");
    }

    [Fact]
    public void IChunkingStrategy_Mock_ReturnsConfiguredChunks()
    {
        // Arrange
        var expectedChunks = new List<TextChunk>
        {
            new("Hello", 0, 5, new ChunkMetadata(0) { TotalChunks = 2 }),
            new("World", 6, 11, new ChunkMetadata(1) { TotalChunks = 2 })
        };

        var mock = new Mock<IChunkingStrategy>();
        mock.Setup(s => s.Split(It.IsAny<string>(), It.IsAny<ChunkingOptions>()))
            .Returns(expectedChunks);

        // Act
        var result = mock.Object.Split("Hello World", ChunkingOptions.Default);

        // Assert
        result.Should().HaveCount(2, because: "mock returns the configured chunk list");
        result[0].Content.Should().Be("Hello");
        result[1].Content.Should().Be("World");
    }

    [Fact]
    public void IChunkingStrategy_HasModeProperty()
    {
        // Arrange
        var interfaceType = typeof(IChunkingStrategy);

        // Act
        var modeProperty = interfaceType.GetProperty(nameof(IChunkingStrategy.Mode));

        // Assert
        modeProperty.Should().NotBeNull(because: "IChunkingStrategy defines a Mode property");
        modeProperty!.PropertyType.Should().Be(typeof(ChunkingMode));
        modeProperty.CanRead.Should().BeTrue();
    }

    [Fact]
    public void IChunkingStrategy_HasSplitMethod()
    {
        // Arrange
        var interfaceType = typeof(IChunkingStrategy);

        // Act
        var splitMethod = interfaceType.GetMethod(nameof(IChunkingStrategy.Split));

        // Assert
        splitMethod.Should().NotBeNull(because: "IChunkingStrategy defines a Split method");
        splitMethod!.ReturnType.Should().Be(typeof(IReadOnlyList<TextChunk>));
        splitMethod.GetParameters().Should().HaveCount(2,
            because: "Split takes content and options parameters");
    }

    #endregion
}
