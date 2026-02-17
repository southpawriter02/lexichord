// -----------------------------------------------------------------------
// <copyright file="ReadingTimeTests.cs" company="Lexichord">
//     Copyright (c) Lexichord. All rights reserved.
//     Licensed under the MIT License.
// </copyright>
// -----------------------------------------------------------------------

using FluentAssertions;
using Lexichord.Abstractions.Agents.MetadataExtraction;
using Lexichord.Abstractions.Contracts;
using Lexichord.Abstractions.Contracts.Editor;
using Lexichord.Abstractions.Contracts.LLM;
using Lexichord.Modules.Agents.MetadataExtraction;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Lexichord.Tests.Unit.Modules.Agents.MetadataExtraction;

/// <summary>
/// Unit tests for reading time calculation in <see cref="MetadataExtractor"/>.
/// </summary>
[Trait("Category", "Unit")]
[Trait("SubPart", "v0.7.6b")]
public class ReadingTimeTests
{
    private readonly MetadataExtractor _extractor;

    public ReadingTimeTests()
    {
        // Set up minimal mocks for the extractor
        var chatService = new Mock<IChatCompletionService>();
        var promptRenderer = new Mock<IPromptRenderer>();
        var templateRepository = new Mock<IPromptTemplateRepository>();
        var fileService = new Mock<IFileService>();
        var licenseContext = new Mock<ILicenseContext>();
        var mediator = new Mock<IMediator>();
        var logger = NullLogger<MetadataExtractor>.Instance;

        _extractor = new MetadataExtractor(
            chatService.Object,
            promptRenderer.Object,
            templateRepository.Object,
            fileService.Object,
            licenseContext.Object,
            mediator.Object,
            logger);
    }

    // ── Basic Calculation ───────────────────────────────────────────────

    [Fact]
    public void CalculateReadingTime_EmptyContent_ReturnsZero()
    {
        // Act
        var result = _extractor.CalculateReadingTime(string.Empty);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateReadingTime_NullContent_ReturnsZero()
    {
        // Act
        var result = _extractor.CalculateReadingTime(null!);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateReadingTime_WhitespaceContent_ReturnsZero()
    {
        // Act
        var result = _extractor.CalculateReadingTime("   \n\t  ");

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void CalculateReadingTime_ShortContent_ReturnsMinimumOneMinute()
    {
        // Arrange - 10 words = 10/200 = 0.05 minutes, should round up to 1
        var content = "This is a short test sentence with ten words total.";

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void CalculateReadingTime_200Words_ReturnsOneMinute()
    {
        // Arrange - Exactly 200 words at 200 WPM = 1 minute
        // Use 40 sentences of 5 words each to avoid sentence length multiplier
        // (avg sentence length 5 < 25, so no multiplier applies)
        var sentence = "One two three four five. ";
        var repetitions = 40; // 40 sentences × 5 words = 200 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions));

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void CalculateReadingTime_400Words_ReturnsTwoMinutes()
    {
        // Arrange - 400 words at 200 WPM = 2 minutes
        // Use 80 sentences of 5 words each to avoid sentence length multiplier
        var sentence = "One two three four five. ";
        var repetitions = 80; // 80 sentences × 5 words = 400 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions));

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().Be(2);
    }

    // ── Words Per Minute Adjustment ─────────────────────────────────────

    [Fact]
    public void CalculateReadingTime_SlowerReadingSpeed_IncreasesTime()
    {
        // Arrange - 200 words at 100 WPM = 2 minutes
        // Use 40 sentences of 5 words each to avoid sentence length multiplier
        var sentence = "One two three four five. ";
        var repetitions = 40; // 40 sentences × 5 words = 200 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions));

        // Act
        var result = _extractor.CalculateReadingTime(content, wordsPerMinute: 100);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void CalculateReadingTime_FasterReadingSpeed_DecreasesTime()
    {
        // Arrange - 400 words at 400 WPM = 1 minute
        // Use 80 sentences of 5 words each to avoid sentence length multiplier
        var sentence = "One two three four five. ";
        var repetitions = 80; // 80 sentences × 5 words = 400 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions));

        // Act
        var result = _extractor.CalculateReadingTime(content, wordsPerMinute: 400);

        // Assert
        result.Should().Be(1);
    }

    [Fact]
    public void CalculateReadingTime_WordsPerMinuteBelowRange_ClampedTo100()
    {
        // Arrange - 200 words, WPM clamped to 100 = 2 minutes
        // Use 40 sentences of 5 words each to avoid sentence length multiplier
        var sentence = "One two three four five. ";
        var repetitions = 40; // 40 sentences × 5 words = 200 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions));

        // Act
        var result = _extractor.CalculateReadingTime(content, wordsPerMinute: 50);

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void CalculateReadingTime_WordsPerMinuteAboveRange_ClampedTo400()
    {
        // Arrange - 800 words, WPM clamped to 400 = 2 minutes
        // Use 160 sentences of 5 words each to avoid sentence length multiplier
        var sentence = "One two three four five. ";
        var repetitions = 160; // 160 sentences × 5 words = 800 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions));

        // Act
        var result = _extractor.CalculateReadingTime(content, wordsPerMinute: 1000);

        // Assert
        result.Should().Be(2);
    }

    // ── Code Block Adjustment ───────────────────────────────────────────

    [Fact]
    public void CalculateReadingTime_WithCodeBlocks_AddsExtraTime()
    {
        // Arrange - 200 words + 1 code block (adds 0.5 min)
        var content = string.Join(" ", Enumerable.Repeat("word", 200)) +
                      "\n```csharp\nvar x = 1;\n```";

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(2); // 1 min base + 0.5 min code = 1.5, rounds to 2
    }

    [Fact]
    public void CalculateReadingTime_WithMultipleCodeBlocks_AddsMoreTime()
    {
        // Arrange - 200 words + 3 code blocks (adds 1.5 min)
        var content = string.Join(" ", Enumerable.Repeat("word", 200)) +
                      "\n```js\nconst x = 1;\n```" +
                      "\n```python\nx = 1\n```" +
                      "\n```csharp\nvar x = 1;\n```";

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(3); // 1 min base + 1.5 min code = 2.5, rounds to 3
    }

    [Fact]
    public void CalculateReadingTime_WithInlineCode_AddsExtraTime()
    {
        // Arrange - Content with inline code
        var content = "Use the `print()` function and the `input()` function " +
                      string.Join(" ", Enumerable.Repeat("word", 200));

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        // Inline code blocks add time too
        result.Should().BeGreaterThanOrEqualTo(2);
    }

    // ── Table Adjustment ────────────────────────────────────────────────

    [Fact]
    public void CalculateReadingTime_WithTable_AddsExtraTime()
    {
        // Arrange - 200 words + a table
        var content = string.Join(" ", Enumerable.Repeat("word", 200)) +
                      "\n| Col1 | Col2 |\n| --- | --- |\n| val1 | val2 |\n| val3 | val4 |";

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(2);
    }

    // ── Image Adjustment ────────────────────────────────────────────────

    [Fact]
    public void CalculateReadingTime_WithImages_AddsExtraTime()
    {
        // Arrange - 200 words + 2 images (adds 0.4 min)
        var content = string.Join(" ", Enumerable.Repeat("word", 200)) +
                      "\n![Alt text](image1.png)\n![Another image](image2.png)";

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().BeGreaterThanOrEqualTo(2);
    }

    // ── Ceiling Behavior ────────────────────────────────────────────────

    [Fact]
    public void CalculateReadingTime_AlwaysRoundsUp()
    {
        // Arrange - 201 words at 200 WPM = 1.005 minutes, should round up to 2
        // Use 40 sentences of 5 words each (200 words) + 1 extra word = 201 words
        var sentence = "One two three four five. ";
        var repetitions = 40; // 40 sentences × 5 words = 200 words
        var content = string.Concat(Enumerable.Repeat(sentence, repetitions)) + "extra";

        // Act
        var result = _extractor.CalculateReadingTime(content);

        // Assert
        result.Should().Be(2);
    }
}
