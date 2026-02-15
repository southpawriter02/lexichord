// -----------------------------------------------------------------------
// <copyright file="SimplificationResponseParserTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.Simplifier;
using Lexichord.Modules.Agents.Simplifier;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.Simplifier;

/// <summary>
/// Unit tests for <see cref="SimplificationResponseParser"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.4b")]
public class SimplificationResponseParserTests
{
    private readonly SimplificationResponseParser _sut;

    public SimplificationResponseParserTests()
    {
        _sut = new SimplificationResponseParser(
            NullLogger<SimplificationResponseParser>.Instance);
    }

    // ── Parse - Well-Formed Response Tests ────────────────────────────────

    [Fact]
    public void Parse_WellFormedResponse_ExtractsAllBlocks()
    {
        // Arrange
        var response = """
            ```simplified
            This is the simplified text.
            It has multiple lines.
            ```

            ```changes
            - "complex phrase" → "simple phrase" | Type: WordSimplification | Reason: Replaced complex word
            - "long sentence here" → "short version" | Type: SentenceSplit | Reason: Split for clarity
            ```

            ```glossary
            - "technical term" → "plain explanation"
            - "jargon" → "common word"
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().Contain("This is the simplified text.");
        result.SimplifiedText.Should().Contain("It has multiple lines.");
        result.Changes.Should().HaveCount(2);
        result.Glossary.Should().NotBeNull();
        result.Glossary.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_WithoutGlossary_ReturnsNullGlossary()
    {
        // Arrange
        var response = """
            ```simplified
            Simplified text here.
            ```

            ```changes
            - "original" → "simplified" | Type: WordSimplification | Reason: Made simpler
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().Be("Simplified text here.");
        result.Changes.Should().HaveCount(1);
        result.Glossary.Should().BeNull();
        result.HasGlossary.Should().BeFalse();
    }

    [Fact]
    public void Parse_MalformedChanges_ReturnsEmptyList()
    {
        // Arrange
        var response = """
            ```simplified
            Simplified text.
            ```

            ```changes
            This is not a valid change format
            Neither is this one
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().Be("Simplified text.");
        result.Changes.Should().BeEmpty();
        result.HasChanges.Should().BeFalse();
    }

    [Fact]
    public void Parse_NoCodeBlocks_FallbackToRawContent()
    {
        // Arrange
        var response = "Just some plain text without any code blocks.";

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().Be("Just some plain text without any code blocks.");
        result.Changes.Should().BeEmpty();
        result.Glossary.Should().BeNull();
    }

    [Fact]
    public void Parse_EmptyResponse_ReturnsEmptyResult()
    {
        // Arrange
        var response = "";

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().BeEmpty();
        result.Changes.Should().BeEmpty();
        result.Glossary.Should().BeNull();
    }

    [Fact]
    public void Parse_WhitespaceResponse_ReturnsEmptyResult()
    {
        // Arrange
        var response = "   \n\t  ";

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().BeEmpty();
        result.Changes.Should().BeEmpty();
    }

    [Fact]
    public void Parse_NullResponse_ThrowsArgumentNullException()
    {
        // Act
        var act = () => _sut.Parse(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("llmResponse");
    }

    // ── Parse - Multiple Changes Tests ────────────────────────────────────

    [Fact]
    public void Parse_MultipleChanges_ParsesAll()
    {
        // Arrange
        var response = """
            ```simplified
            Simple text.
            ```

            ```changes
            - "first original" → "first simplified" | Type: WordSimplification | Reason: First reason
            - "second original" → "second simplified" | Type: SentenceSplit | Reason: Second reason
            - "third original" → "third simplified" | Type: PassiveToActive | Reason: Third reason
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Changes.Should().HaveCount(3);
        result.Changes[0].OriginalText.Should().Be("first original");
        result.Changes[0].SimplifiedText.Should().Be("first simplified");
        result.Changes[1].OriginalText.Should().Be("second original");
        result.Changes[2].OriginalText.Should().Be("third original");
    }

    // ── Parse - Change Type Parsing Tests ────────────────────────────────

    [Theory]
    [InlineData("SentenceSplit", SimplificationChangeType.SentenceSplit)]
    [InlineData("sentence-split", SimplificationChangeType.SentenceSplit)]
    [InlineData("SENTENCESPLIT", SimplificationChangeType.SentenceSplit)]
    [InlineData("JargonReplacement", SimplificationChangeType.JargonReplacement)]
    [InlineData("jargon-replacement", SimplificationChangeType.JargonReplacement)]
    [InlineData("PassiveToActive", SimplificationChangeType.PassiveToActive)]
    [InlineData("passive-to-active", SimplificationChangeType.PassiveToActive)]
    [InlineData("WordSimplification", SimplificationChangeType.WordSimplification)]
    [InlineData("word-simplification", SimplificationChangeType.WordSimplification)]
    [InlineData("ClauseReduction", SimplificationChangeType.ClauseReduction)]
    [InlineData("TransitionAdded", SimplificationChangeType.TransitionAdded)]
    [InlineData("RedundancyRemoved", SimplificationChangeType.RedundancyRemoved)]
    [InlineData("Combined", SimplificationChangeType.Combined)]
    public void Parse_ChangeType_CaseInsensitive(string typeString, SimplificationChangeType expected)
    {
        // Arrange
        var response = $"""
            ```simplified
            Text.
            ```

            ```changes
            - "original" → "simplified" | Type: {typeString} | Reason: Test reason
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Changes.Should().HaveCount(1);
        result.Changes[0].ChangeType.Should().Be(expected);
    }

    [Fact]
    public void Parse_UnknownChangeType_DefaultsToCombined()
    {
        // Arrange
        var response = """
            ```simplified
            Text.
            ```

            ```changes
            - "original" → "simplified" | Type: SomeUnknownType | Reason: Test reason
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Changes.Should().HaveCount(1);
        result.Changes[0].ChangeType.Should().Be(SimplificationChangeType.Combined);
    }

    // ── Parse - Arrow Format Tests ────────────────────────────────────

    [Theory]
    [InlineData("→")]
    [InlineData("->")]
    [InlineData("=>")]
    public void Parse_AcceptsArrowVariations(string arrow)
    {
        // Arrange
        var response = $"""
            ```simplified
            Text.
            ```

            ```changes
            - "original" {arrow} "simplified" | Type: WordSimplification | Reason: Test
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Changes.Should().HaveCount(1);
        result.Changes[0].OriginalText.Should().Be("original");
        result.Changes[0].SimplifiedText.Should().Be("simplified");
    }

    // ── Parse - Glossary Tests ────────────────────────────────────

    [Fact]
    public void Parse_GlossaryEntries_ParsedCorrectly()
    {
        // Arrange
        var response = """
            ```simplified
            Text.
            ```

            ```glossary
            - "synergy" → "working together"
            - "leverage" → "use"
            - "paradigm" → "example"
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Glossary.Should().NotBeNull();
        result.Glossary!["synergy"].Should().Be("working together");
        result.Glossary["leverage"].Should().Be("use");
        result.Glossary["paradigm"].Should().Be("example");
    }

    [Fact]
    public void Parse_GlossaryIsCaseInsensitive()
    {
        // Arrange
        var response = """
            ```simplified
            Text.
            ```

            ```glossary
            - "Term" → "definition"
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Glossary.Should().ContainKey("term");
        result.Glossary.Should().ContainKey("TERM");
        result.Glossary.Should().ContainKey("Term");
    }

    [Fact]
    public void Parse_EmptyGlossaryBlock_ReturnsNull()
    {
        // Arrange
        var response = """
            ```simplified
            Text.
            ```

            ```glossary
            No valid entries here
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Glossary.Should().BeNull();
    }

    // ── Parse - Block Name Case Insensitivity Tests ────────────────────

    [Fact]
    public void Parse_SimplifiedBlockCaseInsensitive()
    {
        // Arrange
        var response = """
            ```SIMPLIFIED
            Case insensitive text.
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.SimplifiedText.Should().Be("Case insensitive text.");
    }

    [Fact]
    public void Parse_ChangesBlockCaseInsensitive()
    {
        // Arrange
        var response = """
            ```simplified
            Text.
            ```

            ```CHANGES
            - "a" → "b" | Type: WordSimplification | Reason: Test
            ```
            """;

        // Act
        var result = _sut.Parse(response);

        // Assert
        result.Changes.Should().HaveCount(1);
    }

    // ── SimplificationParseResult Factory Tests ────────────────────

    [Fact]
    public void SimplificationParseResult_WithTextOnly_CreatesCorrectly()
    {
        // Act
        var result = SimplificationParseResult.WithTextOnly("Just text");

        // Assert
        result.SimplifiedText.Should().Be("Just text");
        result.Changes.Should().BeEmpty();
        result.Glossary.Should().BeNull();
        result.HasChanges.Should().BeFalse();
        result.HasGlossary.Should().BeFalse();
    }

    [Fact]
    public void SimplificationParseResult_Empty_CreatesCorrectly()
    {
        // Act
        var result = SimplificationParseResult.Empty();

        // Assert
        result.SimplifiedText.Should().BeEmpty();
        result.Changes.Should().BeEmpty();
        result.Glossary.Should().BeNull();
    }
}
