// -----------------------------------------------------------------------
// <copyright file="ParagraphParserTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;

using Lexichord.Modules.Agents.Simplifier;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="ParagraphParser"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4d")]
public class ParagraphParserTests
{
    private readonly ParagraphParser _sut;

    public ParagraphParserTests()
    {
        var logger = NullLoggerFactory.Instance.CreateLogger<ParagraphParser>();
        _sut = new ParagraphParser(logger);
    }

    // ── Constructor Tests ─────────────────────────────────────────────────────

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => new ParagraphParser(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    // ── Parse Tests: Basic ────────────────────────────────────────────────────

    [Fact]
    public void Parse_NullContent_ThrowsArgumentNullException()
    {
        // Arrange & Act
        var act = () => _sut.Parse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("content");
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = _sut.Parse(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmptyList()
    {
        // Arrange & Act
        var result = _sut.Parse("   \n\n   \t  ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Parse_SingleParagraph_ReturnsSingleParagraph()
    {
        // Arrange
        const string content = "This is a single paragraph.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Index.Should().Be(0);
        result[0].Text.Should().Be(content);
        result[0].Type.Should().Be(ParagraphType.Normal);
        result[0].StartOffset.Should().Be(0);
        result[0].EndOffset.Should().Be(content.Length);
    }

    [Fact]
    public void Parse_TwoParagraphs_SeparatedByBlankLine()
    {
        // Arrange
        const string content = "First paragraph.\n\nSecond paragraph.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(2);

        result[0].Index.Should().Be(0);
        result[0].Text.Should().Be("First paragraph.");
        result[0].Type.Should().Be(ParagraphType.Normal);

        result[1].Index.Should().Be(1);
        result[1].Text.Should().Be("Second paragraph.");
        result[1].Type.Should().Be(ParagraphType.Normal);
    }

    [Fact]
    public void Parse_MultipleParagraphs_PreservesOffsets()
    {
        // Arrange
        const string content = "Para one.\n\nPara two.\n\nPara three.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(3);

        // Verify offsets allow content reconstruction
        foreach (var para in result)
        {
            var extracted = content[para.StartOffset..para.EndOffset];
            extracted.Should().Be(para.Text);
        }
    }

    [Fact]
    public void Parse_MultiLineParagraph_CombinesLines()
    {
        // Arrange
        const string content = "Line one\nLine two\nLine three";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Line one\nLine two\nLine three");
    }

    // ── Parse Tests: Line Endings ─────────────────────────────────────────────

    [Fact]
    public void Parse_UnixLineEndings_ParsesCorrectly()
    {
        // Arrange
        const string content = "Para one.\n\nPara two.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("Para one.");
        result[1].Text.Should().Be("Para two.");
    }

    [Fact]
    public void Parse_WindowsLineEndings_ParsesCorrectly()
    {
        // Arrange
        const string content = "Para one.\r\n\r\nPara two.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(2);
        result[0].Text.Should().Be("Para one.");
        result[1].Text.Should().Be("Para two.");
    }

    [Fact]
    public void Parse_MixedLineEndings_ParsesCorrectly()
    {
        // Arrange
        const string content = "Para one.\r\n\nPara two.\n\r\nPara three.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(3);
    }

    // ── Parse Tests: Headings ─────────────────────────────────────────────────

    [Theory]
    [InlineData("# Heading 1")]
    [InlineData("## Heading 2")]
    [InlineData("### Heading 3")]
    [InlineData("#### Heading 4")]
    [InlineData("##### Heading 5")]
    [InlineData("###### Heading 6")]
    public void Parse_AtxHeading_DetectsAsHeading(string content)
    {
        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Heading);
    }

    [Fact]
    public void Parse_HeadingWithoutSpace_DetectsAsNormal()
    {
        // Arrange: # without space after is not a valid heading
        const string content = "#NotAHeading";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Normal);
    }

    [Fact]
    public void Parse_HeadingWithLeadingWhitespace_DetectsAsHeading()
    {
        // Arrange
        const string content = "  ## Indented Heading";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Heading);
    }

    // ── Parse Tests: Code Blocks ──────────────────────────────────────────────

    [Fact]
    public void Parse_FencedCodeBlock_DetectsAsCodeBlock()
    {
        // Arrange
        const string content = "```\ncode here\n```";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.CodeBlock);
    }

    [Fact]
    public void Parse_FencedCodeBlockWithLanguage_DetectsAsCodeBlock()
    {
        // Arrange
        const string content = "```csharp\nvar x = 1;\n```";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.CodeBlock);
    }

    [Fact]
    public void Parse_TildeFencedCodeBlock_DetectsAsCodeBlock()
    {
        // Arrange
        const string content = "~~~\ncode here\n~~~";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.CodeBlock);
    }

    [Fact]
    public void Parse_FencedCodeBlockPreservesContent()
    {
        // Arrange
        const string content = "```\nline 1\nline 2\n```";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Text.Should().Contain("line 1");
        result[0].Text.Should().Contain("line 2");
    }

    [Fact]
    public void Parse_IndentedCodeBlock_DetectsAsCodeBlock()
    {
        // Arrange
        const string content = "    code line 1\n    code line 2";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.CodeBlock);
    }

    [Fact]
    public void Parse_TabIndentedCodeBlock_DetectsAsCodeBlock()
    {
        // Arrange
        const string content = "\tcode line 1\n\tcode line 2";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.CodeBlock);
    }

    [Fact]
    public void Parse_CodeBlockBetweenParagraphs_ParsesAllThree()
    {
        // Arrange
        const string content = "Before.\n\n```\ncode\n```\n\nAfter.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(3);
        result[0].Type.Should().Be(ParagraphType.Normal);
        result[1].Type.Should().Be(ParagraphType.CodeBlock);
        result[2].Type.Should().Be(ParagraphType.Normal);
    }

    // ── Parse Tests: Blockquotes ──────────────────────────────────────────────

    [Fact]
    public void Parse_Blockquote_DetectsAsBlockquote()
    {
        // Arrange
        const string content = "> This is a quote.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Blockquote);
    }

    [Fact]
    public void Parse_MultiLineBlockquote_DetectsAsBlockquote()
    {
        // Arrange
        const string content = "> Line one.\n> Line two.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Blockquote);
    }

    [Fact]
    public void Parse_BlockquoteWithoutSpace_DetectsAsBlockquote()
    {
        // Arrange: >quote (no space) is still valid
        const string content = ">Quote without space";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Blockquote);
    }

    // ── Parse Tests: List Items ───────────────────────────────────────────────

    [Theory]
    [InlineData("- Item")]
    [InlineData("* Item")]
    [InlineData("+ Item")]
    public void Parse_UnorderedListItem_DetectsAsListItem(string content)
    {
        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.ListItem);
    }

    [Theory]
    [InlineData("1. Item")]
    [InlineData("1) Item")]
    [InlineData("99. Item")]
    public void Parse_OrderedListItem_DetectsAsListItem(string content)
    {
        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.ListItem);
    }

    [Fact]
    public void Parse_ListWithoutSpace_DetectsAsNormal()
    {
        // Arrange: -item (no space) is not a list item
        const string content = "-NoSpace";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.Normal);
    }

    [Fact]
    public void Parse_IndentedListItem_DetectsAsListItem()
    {
        // Arrange
        const string content = "  - Indented item";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Type.Should().Be(ParagraphType.ListItem);
    }

    // ── Parse Tests: Mixed Content ────────────────────────────────────────────

    [Fact]
    public void Parse_MixedContent_DetectsAllTypes()
    {
        // Arrange
        const string content = @"# Heading

Normal paragraph here.

> A blockquote.

- List item

```
code block
```";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(5);
        result[0].Type.Should().Be(ParagraphType.Heading);
        result[1].Type.Should().Be(ParagraphType.Normal);
        result[2].Type.Should().Be(ParagraphType.Blockquote);
        result[3].Type.Should().Be(ParagraphType.ListItem);
        result[4].Type.Should().Be(ParagraphType.CodeBlock);
    }

    [Fact]
    public void Parse_RealWorldDocument_ParsesCorrectly()
    {
        // Arrange
        const string content = @"# Introduction

This document explains the implementation details of the batch
simplification feature in Lexichord.

## Overview

The batch simplification service processes documents paragraph
by paragraph, applying readability improvements.

### Key Features

- Paragraph-by-paragraph processing
- Progress tracking with time estimates
- Cancellation support
- Single undo for entire batch

```csharp
var result = await batchService.SimplifyDocumentAsync(
    documentPath, target, options, progress, ct);
```

> Note: This feature requires the WriterPro license tier.

See the next section for implementation details.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        // Expected paragraphs:
        // 1. # Introduction (Heading)
        // 2. This document explains... (Normal)
        // 3. ## Overview (Heading)
        // 4. The batch simplification... (Normal)
        // 5. ### Key Features (Heading)
        // 6-9. Four list items (each - item is a separate paragraph)
        // 10. Code block
        // 11. Blockquote
        // 12. See the next section... (Normal)
        result.Should().HaveCount(9);

        // Headings
        result.Count(p => p.Type == ParagraphType.Heading).Should().Be(3);

        // Normal paragraphs
        result.Count(p => p.Type == ParagraphType.Normal).Should().Be(3);

        // List items (each - item on separate line is grouped together)
        result.Count(p => p.Type == ParagraphType.ListItem).Should().Be(1);

        // Code block
        result.Count(p => p.Type == ParagraphType.CodeBlock).Should().Be(1);

        // Blockquote
        result.Count(p => p.Type == ParagraphType.Blockquote).Should().Be(1);
    }

    // ── Parse Tests: Computed Properties ──────────────────────────────────────

    [Fact]
    public void Parse_Paragraph_ApproximateWordCountIsAccurate()
    {
        // Arrange
        const string content = "This is a test paragraph with ten words here.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].ApproximateWordCount.Should().Be(9);
    }

    [Fact]
    public void Parse_Paragraph_TextPreviewTruncatesCorrectly()
    {
        // Arrange
        const string content = "This is a very long paragraph that exceeds fifty characters in length.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].TextPreview.Should().EndWith("...");
        result[0].TextPreview.Length.Should().BeLessOrEqualTo(50);
    }

    [Fact]
    public void Parse_ShortParagraph_TextPreviewNotTruncated()
    {
        // Arrange
        const string content = "Short text.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].TextPreview.Should().Be("Short text.");
    }

    [Fact]
    public void Parse_Paragraph_IsNormalReturnsCorrectly()
    {
        // Arrange
        const string content = "Normal paragraph.\n\n# Heading";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result[0].IsNormal.Should().BeTrue();
        result[1].IsNormal.Should().BeFalse();
    }

    [Fact]
    public void Parse_Paragraph_IsStructuralReturnsCorrectly()
    {
        // Arrange
        const string content = "Normal paragraph.\n\n# Heading\n\n```\ncode\n```";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result[0].IsStructural.Should().BeFalse();
        result[1].IsStructural.Should().BeTrue();
        result[2].IsStructural.Should().BeTrue();
    }

    // ── Parse Tests: Edge Cases ───────────────────────────────────────────────

    [Fact]
    public void Parse_MultipleBlankLinesBetweenParagraphs_ParsesCorrectly()
    {
        // Arrange
        const string content = "Para one.\n\n\n\nPara two.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_TrailingNewlines_ParsesCorrectly()
    {
        // Arrange
        const string content = "Single paragraph.\n\n\n";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Single paragraph.");
    }

    [Fact]
    public void Parse_LeadingNewlines_ParsesCorrectly()
    {
        // Arrange
        const string content = "\n\n\nSingle paragraph.";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Single paragraph.");
    }

    [Fact]
    public void Parse_UnclosedFencedCodeBlock_TreatsRestAsCode()
    {
        // Arrange
        const string content = "Before.\n\n```\ncode without closing fence";

        // Act
        var result = _sut.Parse(content);

        // Assert
        result.Should().HaveCount(2);
        result[0].Type.Should().Be(ParagraphType.Normal);
        result[1].Type.Should().Be(ParagraphType.CodeBlock);
    }
}
